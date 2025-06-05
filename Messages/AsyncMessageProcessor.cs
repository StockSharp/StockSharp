namespace StockSharp.Messages;

using Nito.AsyncEx;

/// <summary>
/// Async message processor helper.
/// </summary>
class AsyncMessageProcessor : Disposable
{
	private class MessageQueueItem
	{
		public MessageQueueItem(Message message)
		{
			Message = message ?? throw new ArgumentNullException(nameof(message));

			IsControl = Message.Type
				is MessageTypes.Reset
				or MessageTypes.Connect
				or MessageTypes.Disconnect;

			IsPing = Message.Type == MessageTypes.Time;

			IsLookup = Message.IsLookup();

			IsTransaction = Message.Type
				is MessageTypes.OrderRegister
				or MessageTypes.OrderReplace
				or MessageTypes.OrderCancel
				or MessageTypes.OrderGroupCancel;
		}

		public Message Message { get; }

		public bool IsProcessing { get; set; }

		public bool IsControl { get; }
		public bool IsPing { get; }
		public bool IsLookup { get; }
		public bool IsTransaction { get; }

        public CancellationTokenSource Cts { get; set; }
        public long UnsubscribeRequest { get; set; }

		public override string ToString() => Message.ToString();
	}

	private readonly SynchronizedList<MessageQueueItem> _messages = [];
	private readonly SynchronizedDictionary<MessageQueueItem, Task> _childTasks = [];
	private readonly SynchronizedDictionary<long, MessageQueueItem> _subscriptionItems = [];

	private readonly AsyncManualResetEvent _processMessageEvt = new(false);
	private CancellationTokenSource _globalCts = new();

	private bool _isConnectionStarted, _isDisconnecting;

	private readonly AsyncMessageAdapter _adapter;

	/// <summary>
	/// Initialize <see cref="AsyncMessageProcessor"/>.
	/// </summary>
	/// <param name="adapter"><see cref="AsyncMessageAdapter"/>.</param>
	public AsyncMessageProcessor(AsyncMessageAdapter adapter)
	{
		_adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
		// ReSharper disable once VirtualMemberCallInConstructor
		Task.Run(ProcessMessagesAsync);
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		base.DisposeManaged();

		_processMessageEvt.Set();

		_globalCts?.Cancel();
		_globalCts?.Dispose();
	}

	/// <summary>
	/// </summary>
	public bool EnqueueMessage(Message msg)
	{
		if (IsDisposed)
			throw new ObjectDisposedException(nameof(AsyncMessageProcessor));

		if (msg.Type != MessageTypes.Time)
			_adapter.AddVerboseLog("enqueue: {0}", msg.Type);

		lock (_messages.SyncRoot)
		{
			if (msg is ResetMessage)
			{
				_messages.Clear();
				CancelAndReplaceGlobalCts();
			}

			_messages.Add(new(msg));
		}

		_processMessageEvt.Set();

		return true;
	}

	private async Task ProcessMessagesAsync()
	{
		bool nextMessage()
		{
			MessageQueueItem item;

			lock (_messages.SyncRoot)
			{
				var isControlProcessing = false;
				var isPingProcessing = false;
				var isLookupProcessing = false;
				var isTransactionProcessing = false;
				var numProcessing = 0;

				foreach (var m in _messages.Where(m => m.IsProcessing))
				{
					isControlProcessing |= m.IsControl;
					isPingProcessing |= m.IsPing;
					isLookupProcessing |= m.IsLookup;
					isTransactionProcessing |= m.IsTransaction;
					++numProcessing;
				}

				// cant process anything in parallel while connect/disconnect/reset is processing
				if (isControlProcessing)
					return false;

				var nonProcessing = _messages.Where(i => !i.IsProcessing);

				//
				// priority order:
				//
				// controls messages	- 1
				// heartbeat(=ping)		- 2
				// unsubscribe			- 3
				// lookup				- 4
				// transactions			- 5
				// other				- 6
				//

				item = nonProcessing.FirstOrDefault(m => m.IsControl);

				if (item is null)
				{
					if (isPingProcessing)
					{
						// can't process parallel pings, applying filter
						nonProcessing = nonProcessing.Where(m => !m.IsPing);
					}
					else
						item = nonProcessing.FirstOrDefault(m => m.IsPing);
				}

				item ??= nonProcessing.FirstOrDefault(m => m.Message is ISubscriptionMessage { IsSubscribe: false });

				// all other message types are MaxParallelMessages tolerant
				if (item is null && numProcessing < _adapter.MaxParallelMessages)
				{
					if (isLookupProcessing)
					{
						// can't process parallel lookup, applying filter
						nonProcessing = nonProcessing.Where(m => !m.IsLookup);
					}
					else
						item = nonProcessing.FirstOrDefault(m => m.IsLookup);

					if (item is null)
					{
						if (isTransactionProcessing)
							item = nonProcessing.FirstOrDefault(m => !m.IsTransaction);
						else
							item = nonProcessing.FirstOrDefault(m => m.IsTransaction) ?? nonProcessing.FirstOrDefault();
					}
				}

				if (item is null)
					return false;

				if (item.IsProcessing)
					throw new InvalidOperationException($"processing is already started for {item.Message}");

				item.IsProcessing = true;
			}

			var msg = item.Message;

			async ValueTask wrapperInner()
			{
				var token = _globalCts.Token;

				if (token.IsCancellationRequested)
				{
					if (item.IsTransaction)
						_adapter.SendOutMessage(msg.CreateErrorResponse(new OperationCanceledException(), _adapter));

					return;
				}

				if (msg.Type != MessageTypes.Time)
					_adapter.AddVerboseLog("beginprocess: {0}", msg.Type);

				if (!item.IsControl)
				{
					if (!_isConnectionStarted || _isDisconnecting)
					{
						_adapter.AddDebugLog($"unable to process {msg.Type} in this state. connStarted={_isConnectionStarted}, disconnecting={_isDisconnecting}");
						return;
					}

					if (msg is ISubscriptionMessage subMsg)
					{
						if (subMsg.IsSubscribe)
						{
							var (cts, childToken) = token.CreateChildToken();
							token = childToken;
							item.Cts = cts;
							_subscriptionItems.Add(subMsg.TransactionId, item);
						}
						else
						{
							// in case a subscription still in "subscribe" state
							// (for example, for long historical data request)
							if (_subscriptionItems.TryGetAndRemove(subMsg.OriginalTransactionId, out var subItem))
							{
								subItem.UnsubscribeRequest = subMsg.TransactionId;
								subItem.Cts.Cancel();

								done();
								return;
							}
						}
					}
				}

				ValueTask _()
					=> msg switch
					{
						ConnectMessage m			=> ConnectAsync(m, token),
						DisconnectMessage m			=> DisconnectAsync(m),
						ResetMessage m				=> ResetAsync(m),

						SecurityLookupMessage m		=> _adapter.SecurityLookupAsync(m, token),
						PortfolioLookupMessage m	=> _adapter.PortfolioLookupAsync(m, token),
						BoardLookupMessage m		=> _adapter.BoardLookupAsync(m, token),

						TimeMessage m				=> _adapter.TimeAsync(m, token),

						OrderStatusMessage m		=> _adapter.OrderStatusAsync(m, token),

						OrderReplaceMessage m		=> _adapter.ReplaceOrderAsync(m, token),
						OrderRegisterMessage m		=> _adapter.RegisterOrderAsync(m, token),
						OrderCancelMessage m		=> _adapter.CancelOrderAsync(m, token),
						OrderGroupCancelMessage m	=> _adapter.CancelOrderGroupAsync(m, token),

						MarketDataMessage m			=> _adapter.MarketDataAsync(m, token),

						ChangePasswordMessage m		=> _adapter.ChangePasswordAsync(m, token),

						_ => _adapter.ProcessMessageAsync(msg, token)
					};

				void done()
				{
					if (!item.IsControl)
						_childTasks.Remove(item);

					_messages.Remove(item);
					_processMessageEvt.Set();
				}

				try
				{
					var vt = _();

					if (!vt.IsCompleted)
					{
						if (!item.IsControl)
							_childTasks.Add(item, vt.AsTask());

						await vt;

						if (!item.IsControl)
							_childTasks.Remove(item);
					}

					if (vt.IsFaulted)
						throw vt.AsTask().Exception;
					else if (vt.IsCanceled)
						throw new OperationCanceledException();

					if (msg.Type != MessageTypes.Time)
						_adapter.AddVerboseLog("endprocess: {0}", msg.Type);

					if (msg is ISubscriptionMessage subMsg && subMsg.IsSubscribe)
						_subscriptionItems.Remove(subMsg.TransactionId);
				}
				catch (Exception ex)
				{
					try
					{
						if (item.UnsubscribeRequest != default)
						{
							_adapter.SendOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = item.UnsubscribeRequest });
						}
						else
						{
							if (msg is ISubscriptionMessage)
							{
								if (token.IsCancellationRequested)
								{
									// cancellation not an error for subscriptions as well as all responses
									// must be reply for request only (see above item.UnsubscribeRequest logic)
									return;
								}

								_adapter.AddVerboseLog("endprocess: {0} ({1})", msg.Type, ex);

								await _adapter.FaultDelay.Delay(token);
							}

							_adapter.SendOutMessage(msg.CreateErrorResponse(ex, _adapter));
						}
					}
					catch (Exception ex2)
					{
						done();

						if (!token.IsCancellationRequested)
							_adapter.AddErrorLog(ex2);
					}
				}
				finally
				{
					done();
				}
			}

			async ValueTask wrapper()
			{
				try
				{
					await wrapperInner();
				}
				catch (Exception ex)
				{
					_adapter.AddErrorLog(ex);
				}
			}

#pragma warning disable CA2012
			_ = wrapper();
#pragma warning restore CA2012

			return true;
		}

		await Do.Invariant(async () =>
		{
			while (true)
			{
				await _processMessageEvt.WaitAsync();

				if (IsDisposeStarted)
					break;

				_processMessageEvt.Reset();

				try
				{
					while (nextMessage()) { }
				}
				catch (Exception e)
				{
					_adapter.AddErrorLog("error processing message: {0}", e);
				}
			}
		});
	}

	private ValueTask ConnectAsync(ConnectMessage msg, CancellationToken token)
	{
		if(_isConnectionStarted)
			throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

		_isConnectionStarted = true;

		return _adapter.ConnectAsync(msg, token);
	}

	private async ValueTask DisconnectAsync(DisconnectMessage msg)
	{
		if(!_isConnectionStarted)
			throw new InvalidOperationException("not connected");

		if(_isDisconnecting)
			throw new InvalidOperationException("already disconnecting");

		_isDisconnecting = true;

		CancelAndReplaceGlobalCts();

		if(!await WhenChildrenComplete(_adapter.DisconnectTimeout.CreateTimeoutToken()))
			throw new InvalidOperationException("unable to complete disconnect. some tasks are still running.");

		await _adapter.DisconnectAsync(msg, default);

		_isDisconnecting = _isConnectionStarted = false;
	}

	private async ValueTask ResetAsync(ResetMessage msg)
	{
		_isDisconnecting = true;

		// token is already canceled in EnqueueMessage
		await AsyncHelper.CatchHandle(
			() => WhenChildrenComplete(_adapter.DisconnectTimeout.CreateTimeoutToken()),
			_globalCts.Token);

		foreach (var (_, item) in _subscriptionItems.CopyAndClear())
			item.Cts.Cancel();

		await _adapter.ResetAsync(msg, default); // reset must not throw.

		_isDisconnecting = _isConnectionStarted = false;
	}

	private void CancelAndReplaceGlobalCts()
	{
		_globalCts.Cancel();
		_globalCts = new();
	}

	private async Task<bool> WhenChildrenComplete(CancellationToken token)
	{
		var tasks = _childTasks.CopyAndClear();

		var allComplete = true;

		await Task.WhenAll(tasks.Select(t => t.Value.WithCancellation(token))).CatchHandle(token, finalizer: () =>
		{
			var incomplete = tasks.Where(t => !t.Value.IsCompleted).Select(t => t.Key.ToString()).ToArray();
			if(incomplete.Any())
			{
				allComplete = false;
				_adapter.AddErrorLog("following tasks were not completed:\n" + incomplete.JoinN());
			}
		});

		return allComplete;
	}
}

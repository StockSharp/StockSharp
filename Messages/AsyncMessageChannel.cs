namespace StockSharp.Messages;

using Nito.AsyncEx;

/// <summary>
/// Async message channel that processes messages via <see cref="IMessageChannel.NewOutMessageAsync"/>.
/// </summary>
public class AsyncMessageChannel : Disposable, IMessageChannel
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
	private Task _processorTask;

	private bool _isConnectionStarted, _isDisconnecting;

	private readonly IMessageAdapter _adapter;

	/// <summary>
	/// Initializes a new instance of the <see cref="AsyncMessageChannel"/>.
	/// </summary>
	/// <param name="adapter"><see cref="IMessageAdapter"/>.</param>
	public AsyncMessageChannel(IMessageAdapter adapter)
	{
		_adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
	}

	private ChannelStates _state = ChannelStates.Stopped;

	/// <inheritdoc />
	public ChannelStates State
	{
		get => _state;
		private set
		{
			if (_state == value)
				return;

			_state = value;
			StateChanged?.Invoke();
		}
	}

	/// <inheritdoc />
	public event Action StateChanged;

	/// <inheritdoc />
	public void Open()
	{
		State = ChannelStates.Started;
		_processorTask = Task.Run(ProcessMessagesAsync);
	}

	/// <inheritdoc />
	public void Close()
	{
		State = ChannelStates.Stopping;

		try
		{
			_processMessageEvt.Set();
		}
		catch { }

		try
		{
			_globalCts?.Cancel();
		}
		finally
		{
			_globalCts?.Dispose();
			_globalCts = new();
		}

		foreach (var kv in _subscriptionItems.CopyAndClear())
		{
			var item = kv.Value;

			try
			{
				item.Cts?.Cancel();
			}
			finally
			{
				item.Cts?.Dispose();
			}
		}

		try
		{
			_processorTask?.Wait(TimeSpan.FromSeconds(5));
		}
		catch { }

		_messages.Clear();
		_childTasks.Clear();

		State = ChannelStates.Stopped;
	}

	/// <inheritdoc />
	public void Suspend()
	{
		State = ChannelStates.Suspended;
	}

	/// <inheritdoc />
	public void Resume()
	{
		State = ChannelStates.Started;
	}

	/// <inheritdoc />
	public void Clear()
	{
		_messages.Clear();
	}

	/// <inheritdoc />
	public void SendInMessage(Message message)
	{
		if (IsDisposed)
			throw new ObjectDisposedException(nameof(AsyncMessageChannel));

		if (!this.IsOpened())
			return;

		using (_messages.EnterScope())
		{
			if (message is ResetMessage)
			{
				_messages.Clear();
				CancelAndReplaceGlobalCts();
			}

			_messages.Add(new(message));
		}

		_processMessageEvt.Set();
	}

	/// <inheritdoc />
	public event Func<Message, CancellationToken, ValueTask> NewOutMessageAsync;

	private ValueTask RaiseNewOutMessage(Message message, CancellationToken cancellationToken)
	{
		return NewOutMessageAsync?.Invoke(message, cancellationToken) ?? default;
	}

	private async Task ProcessMessagesAsync()
	{
		bool nextMessage()
		{
			MessageQueueItem item;

			using (_messages.EnterScope())
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

						_ => RaiseNewOutMessage(msg, token)
					};

				void done()
				{
					if (!item.IsControl)
						_childTasks.Remove(item);

					// dispose per-subscription CTS when message completes
					item.Cts?.Dispose();

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

				if (State != ChannelStates.Started)
				{
					if (State == ChannelStates.Stopping)
						break;

					continue;
				}

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

	private async ValueTask ConnectAsync(ConnectMessage msg, CancellationToken token)
	{
		if(_isConnectionStarted)
			throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);

		await RaiseNewOutMessage(msg, token);

		_isConnectionStarted = true;
	}

	private async ValueTask DisconnectAsync(DisconnectMessage msg)
	{
		if(!_isConnectionStarted)
			throw new InvalidOperationException("not connected");

		if(_isDisconnecting)
			throw new InvalidOperationException("already disconnecting");

		_isDisconnecting = true;

		try
		{
			CancelAndReplaceGlobalCts();

			using (var cts = _adapter.DisconnectTimeout.CreateTimeout())
			{
				if (!await WhenChildrenComplete(cts.Token))
					throw new InvalidOperationException("unable to complete disconnect. some tasks are still running.");
			}

			await RaiseNewOutMessage(msg, default);

			_isConnectionStarted = false;
		}
		finally
		{
			_isDisconnecting = false;
		}
	}

	private async ValueTask ResetAsync(ResetMessage msg)
	{
		_isDisconnecting = true;

		// token is already canceled in SendInMessage
		await AsyncHelper.CatchHandle((Func<Task>)(async () =>
		{
            using var cts = _adapter.DisconnectTimeout.CreateTimeout();
            await WhenChildrenComplete(cts.Token);
        }), _globalCts.Token);

		foreach (var kv in _subscriptionItems.CopyAndClear())
		{
			var item = kv.Value;

			item.Cts.Cancel();
			item.Cts.Dispose();
		}

		try
		{
			await RaiseNewOutMessage(msg, default);
		}
		catch (Exception ex)
		{
			_adapter.AddErrorLog(ex);
		}

		_isDisconnecting = _isConnectionStarted = false;
	}

	private void CancelAndReplaceGlobalCts()
	{
		_globalCts.Cancel();
		_globalCts.Dispose();

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

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		Close();
		base.DisposeManaged();
	}

	/// <inheritdoc />
	public IMessageChannel Clone() => new AsyncMessageChannel(_adapter);

	object ICloneable.Clone() => Clone();
}

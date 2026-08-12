namespace StockSharp.Messages;

using Nito.AsyncEx;

/// <summary>
/// Async message channel that processes messages via <see cref="IMessageTransport.NewOutMessageAsync"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AsyncMessageChannel"/>.
/// </remarks>
/// <param name="adapter"><see cref="IMessageAdapter"/>.</param>
public class AsyncMessageChannel(IMessageAdapter adapter) : Disposable, IMessageChannel
{
	private class MessageQueueItem
	{
		private TaskCompletionSource<bool> _completion;
		private int _cleanupStarted;
		private int _isCompleted;

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

		public CancellationTokenSource ProcessSource { get; set; }
		public CancellationTokenSource Cts { get; set; }

		public Task CompletionTask
		{
			get
			{
				if (Volatile.Read(ref _isCompleted) != 0)
					return Task.CompletedTask;

				var created = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				var completion = Interlocked.CompareExchange(ref _completion, created, null) ?? created;

				if (Volatile.Read(ref _isCompleted) != 0)
					completion.TrySetResult(true);

				return completion.Task;
			}
		}

		public Task DisconnectPrerequisite { get; set; }

		public bool TryBeginCleanup() => Interlocked.Exchange(ref _cleanupStarted, 1) == 0;

		public void Complete()
		{
			if (Interlocked.Exchange(ref _isCompleted, 1) == 0)
				Volatile.Read(ref _completion)?.TrySetResult(true);
		}

		public override string ToString() => Message.ToString();
	}

	private readonly SynchronizedList<MessageQueueItem> _messages = [];
	private readonly SynchronizedDictionary<MessageQueueItem, Task> _childTasks = [];
	private readonly SynchronizedDictionary<long, MessageQueueItem> _subscriptionItems = [];

	private readonly AsyncManualResetEvent _processMessageEvt = new(false);
	private readonly Lock _stateLock = new();
	private CancellationTokenSource _globalCts = new();
	private Task _processorTask;

	private bool _isConnectionStarted, _isDisconnecting;

	private readonly IMessageAdapter _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
	private ChannelStates _state = ChannelStates.Stopped;

	/// <inheritdoc />
	public ChannelStates State
	{
		get => _state;
		private set
		{
			if (_state == value)
				return;

			if (!_state.ValidateChannelState(value))
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
		State = ChannelStates.Starting;

		CancellationToken token;

		lock (_stateLock)
			token = _globalCts.Token;

		_processorTask = Task.Run(() => ProcessMessagesAsync(token), token);

		State = ChannelStates.Started;
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

		CancelAndReplaceGlobalCts();

		foreach (var kv in _subscriptionItems.CopyAndClear())
		{
			var item = kv.Value;

			try
			{
				item.Cts?.Cancel();
			}
			catch { }
			finally
			{
				try { item.Cts?.Dispose(); }
				catch { }
			}
		}

		try
		{
			_processorTask?.Wait(TimeSpan.FromSeconds(5));
		}
		catch { }

		_messages.Clear();
		_childTasks.Clear();

		lock (_stateLock)
		{
			_isConnectionStarted = false;
			_isDisconnecting = false;
		}

		State = ChannelStates.Stopped;
	}

	/// <inheritdoc />
	public void Suspend()
	{
		State = ChannelStates.Suspending;
		State = ChannelStates.Suspended;
	}

	/// <inheritdoc />
	public void Resume()
	{
		State = ChannelStates.Starting;
		State = ChannelStates.Started;
	}

	/// <inheritdoc />
	public void Clear()
	{
		_messages.Clear();
	}

	/// <inheritdoc />
	public ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		if (IsDisposed)
			throw new ObjectDisposedException(nameof(AsyncMessageChannel));

		if (!this.IsOpened())
			return default;

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
		return default;
	}

	/// <inheritdoc />
	public event Func<Message, CancellationToken, ValueTask> NewOutMessageAsync;

	private ValueTask RaiseNewOutMessage(Message message, CancellationToken cancellationToken)
	{
		return NewOutMessageAsync.InvokeAsync(message, cancellationToken);
	}

	private Task ProcessMessagesAsync(CancellationToken token)
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

				if (item?.Message is DisconnectMessage)
				{
					var disconnectItem = item;
					List<Task> prerequisites = null;

					foreach (var priorItem in _messages)
					{
						if (ReferenceEquals(priorItem, disconnectItem))
							break;

						if (priorItem.Message is not ISubscriptionMessage { IsSubscribe: false })
							continue;

						if (!priorItem.IsProcessing)
						{
							item = priorItem;
							break;
						}

						(prerequisites ??= []).Add(priorItem.CompletionTask);
					}

					if (ReferenceEquals(item, disconnectItem))
					{
						disconnectItem.DisconnectPrerequisite = prerequisites?.Count switch
						{
							null or 0 => Task.CompletedTask,
							1 => prerequisites[0],
							_ => Task.WhenAll(prerequisites),
						};
					}
				}

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

				lock (_stateLock)
					item.ProcessSource = _globalCts;

				item.IsProcessing = true;
			}

			var msg = item.Message;

			async ValueTask wrapperInner()
			{
				var processSource = item.ProcessSource;
				var localToken = processSource.Token;
				long? registeredSubscriptionId = null;

				void done()
				{
					if (!item.TryBeginCleanup())
						return;

					try
					{
						if (!item.IsControl)
							_childTasks.Remove(item);

						if (registeredSubscriptionId is long subscriptionId)
							_subscriptionItems.Remove(subscriptionId);

						item.Cts?.Dispose();
					}
					catch (Exception ex)
					{
						_adapter.AddErrorLog(ex);
					}
					finally
					{
						using (_messages.EnterScope())
						{
							try
							{
								_messages.Remove(item);
							}
							finally
							{
								if (msg is ISubscriptionMessage)
									item.Complete();
							}
						}

						_processMessageEvt.Set();
					}
				}

				try
				{
					if (localToken.IsCancellationRequested)
					{
						if (item.IsTransaction)
							await _adapter.SendOutMessageAsync(msg.CreateErrorResponse(new OperationCanceledException(), _adapter), localToken);

						return;
					}

					if (msg.Type != MessageTypes.Time)
						_adapter.AddVerboseLog("beginprocess: {0}", msg.Type);

					if (!item.IsControl)
					{
						bool isConnectionStarted;
						bool isDisconnecting;

						lock (_stateLock)
						{
							isConnectionStarted = _isConnectionStarted;
							isDisconnecting = _isDisconnecting;
						}

						if (!isConnectionStarted || isDisconnecting)
						{
							_adapter.AddDebugLog($"unable to process {msg.Type} in this state. connStarted={isConnectionStarted}, disconnecting={isDisconnecting}");
							return;
						}

						if (msg is ISubscriptionMessage subMsg)
						{
							if (subMsg.IsSubscribe)
							{
								var (cts, childToken) = localToken.CreateChildToken();
								localToken = childToken;
								item.Cts = cts;
								_subscriptionItems.Add(subMsg.TransactionId, item);
								registeredSubscriptionId = subMsg.TransactionId;
							}
							else
							{
								// The unsubscribe item owns the acknowledgement for a long-running subscribe.
								// Wait for its full cleanup before acknowledging the queued unsubscribe.
								if (_subscriptionItems.TryGetAndRemove(subMsg.OriginalTransactionId, out var subItem))
								{
									var subscriptionCompletion = subItem.CompletionTask;

									try
									{
										subItem.Cts.Cancel();
									}
									catch (ObjectDisposedException)
									{
									}
									catch (Exception ex)
									{
										_adapter.AddErrorLog(ex);
									}

									try
									{
										await subscriptionCompletion.WithCancellation(localToken);
									}
									catch (OperationCanceledException) when (localToken.IsCancellationRequested)
									{
										return;
									}

									await _adapter.SendOutMessageAsync(subMsg.TransactionId.CreateSubscriptionResponse(), localToken);
									return;
								}
							}
						}
					}

					ValueTask _()
						=> msg switch
						{
							ConnectMessage m			=> ConnectAsync(m, localToken, processSource),
							DisconnectMessage m			=> DisconnectAsync(m, item),
							ResetMessage m				=> ResetAsync(m),

							_ => RaiseNewOutMessage(msg, localToken)
						};

					try
					{
						// A source-backed ValueTask can only be consumed once. Track and await the same Task.
						var task = _().AsTask();

						if (!task.IsCompleted)
						{
							if (!item.IsControl)
								_childTasks.Add(item, task);

							await task;

							if (!item.IsControl)
								_childTasks.Remove(item);
						}

						if (task.IsFaulted)
							throw task.Exception;
						else if (task.IsCanceled)
							throw new OperationCanceledException();

						if (msg.Type != MessageTypes.Time)
							_adapter.AddVerboseLog("endprocess: {0}", msg.Type);
					}
					catch (Exception ex)
					{
						var responseToken = item.IsControl ? item.ProcessSource.Token : localToken;

						try
						{
							if (item.IsControl)
							{
								lock (_stateLock)
								{
									if (responseToken.IsCancellationRequested || !ReferenceEquals(_globalCts, item.ProcessSource))
										return;
								}
							}

							if (msg is ISubscriptionMessage)
							{
								if (localToken.IsCancellationRequested)
								{
									// Cancellation is not an error for subscriptions. Fast-path unsubscribe
									// acknowledgements are emitted by the unsubscribe queue item.
									return;
								}

								_adapter.AddVerboseLog("endprocess: {0} ({1})", msg.Type, ex);

								await _adapter.FaultDelay.Delay(localToken);
							}

							await _adapter.SendOutMessageAsync(msg.CreateErrorResponse(ex, _adapter), responseToken);
						}
						catch (Exception ex2)
						{
							if (!responseToken.IsCancellationRequested)
								_adapter.AddErrorLog(ex2);
						}
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

		return Do.InvariantAsync(async () =>
		{
			while (true)
			{
				await _processMessageEvt.WaitAsync(token);

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

	private async ValueTask ConnectAsync(ConnectMessage msg, CancellationToken token, CancellationTokenSource processSource)
	{
		lock (_stateLock)
		{
			if (!ReferenceEquals(_globalCts, processSource) || processSource.IsCancellationRequested)
				throw new OperationCanceledException(processSource.Token);

			if (_isConnectionStarted)
				throw new InvalidOperationException(LocalizedStrings.NotDisconnectPrevTime);
		}

		await RaiseNewOutMessage(msg, token);

		lock (_stateLock)
		{
			if (!ReferenceEquals(_globalCts, processSource) || processSource.IsCancellationRequested)
				throw new OperationCanceledException(processSource.Token);

			_isConnectionStarted = true;
		}
	}

	private async ValueTask DisconnectAsync(DisconnectMessage msg, MessageQueueItem item)
	{
		var processSource = item.ProcessSource;

		lock (_stateLock)
		{
			if (!ReferenceEquals(_globalCts, processSource) || processSource.IsCancellationRequested)
				throw new OperationCanceledException(processSource.Token);

			if (!_isConnectionStarted)
				throw new InvalidOperationException("not connected");

			if (_isDisconnecting)
				throw new InvalidOperationException("already disconnecting");

			_isDisconnecting = true;
		}

		var prerequisite = item.DisconnectPrerequisite ?? Task.CompletedTask;
		var operationSource = processSource;

		try
		{
			using var timeoutCts = _adapter.DisconnectTimeout.CreateTimeout();
			using var prerequisiteCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, processSource.Token);
			CancellationTokenSource disconnectSource = null;

			try
			{
				try
				{
					await prerequisite.WithCancellation(prerequisiteCts.Token);
				}
				catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested && !processSource.IsCancellationRequested)
				{
					throw new TimeoutException("Unable to complete disconnect. Prior unsubscribe requests are still running.", ex);
				}
			}
			finally
			{
				// Only the disconnect that still owns this cancellation generation may replace it.
				// Close or Reset can already have installed a fresh source for subsequent processing.
				disconnectSource = TryCancelAndReplaceGlobalCts(processSource);

				if (disconnectSource is not null)
				{
					operationSource = disconnectSource;
					item.ProcessSource = disconnectSource;
				}
			}

			if (disconnectSource is null)
				throw new OperationCanceledException(processSource.Token);

			using var completionCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, disconnectSource.Token);

			if (!await WhenChildrenComplete(completionCts.Token))
			{
				lock (_stateLock)
				{
					if (disconnectSource.IsCancellationRequested || !ReferenceEquals(_globalCts, disconnectSource))
						throw new OperationCanceledException(disconnectSource.Token);
				}

				throw new InvalidOperationException("unable to complete disconnect. some tasks are still running.");
			}

			lock (_stateLock)
			{
				if (disconnectSource.IsCancellationRequested || !ReferenceEquals(_globalCts, disconnectSource))
					throw new OperationCanceledException(disconnectSource.Token);
			}

			await RaiseNewOutMessage(msg, disconnectSource.Token);

			lock (_stateLock)
			{
				if (disconnectSource.IsCancellationRequested || !ReferenceEquals(_globalCts, disconnectSource))
					throw new OperationCanceledException(disconnectSource.Token);

				_isConnectionStarted = false;
			}
		}
		finally
		{
			lock (_stateLock)
			{
				if (ReferenceEquals(_globalCts, operationSource))
					_isDisconnecting = false;
			}
		}
	}

	private async ValueTask ResetAsync(ResetMessage msg)
	{
		lock (_stateLock)
			_isDisconnecting = true;

		// token is already canceled in SendInMessage
		await AsyncHelper.CatchHandle((Func<Task>)(async () =>
		{
			using var cts = _adapter.DisconnectTimeout.CreateTimeout();
			await WhenChildrenComplete(cts.Token);
		}), default);

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

		lock (_stateLock)
			_isDisconnecting = _isConnectionStarted = false;
	}

	private void CancelAndReplaceGlobalCts()
	{
		// Atomically swap in a fresh source and cancel the old one. The old source is intentionally NOT
		// disposed here: the processor task may still be reading its token (see ProcessMessagesAsync), and
		// disposing it concurrently races into an ObjectDisposedException. A CancellationTokenSource without a
		// timer holds no unmanaged resource, so letting the GC reclaim the abandoned one once the processor
		// drains is safe and removes the use-after-dispose race.
		CancellationTokenSource old;

		lock (_stateLock)
		{
			old = _globalCts;
			_globalCts = new();
		}

		CancelGlobalCts(old);
	}

	private CancellationTokenSource TryCancelAndReplaceGlobalCts(CancellationTokenSource expected)
	{
		var replacement = new CancellationTokenSource();

		lock (_stateLock)
		{
			if (!ReferenceEquals(_globalCts, expected))
			{
				replacement.Dispose();
				return null;
			}

			_globalCts = replacement;
		}

		CancelGlobalCts(expected);
		return replacement;
	}

	private static void CancelGlobalCts(CancellationTokenSource source)
	{
		try
		{
			source?.Cancel();
		}
		catch { }
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

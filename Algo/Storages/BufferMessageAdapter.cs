namespace StockSharp.Algo.Storages;

/// <summary>
/// Buffered message adapter.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BufferMessageAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">Underlying adapter.</param>
/// <param name="settings">Storage settings.</param>
/// <param name="buffer">Storage buffer.</param>
/// <param name="snapshotRegistry">Snapshot storage registry.</param>
public class BufferMessageAdapter(IMessageAdapter innerAdapter, StorageCoreSettings settings, IStorageBuffer buffer, ISnapshotRegistry snapshotRegistry) : MessageAdapterWrapper(innerAdapter)
{
	private const int MaxPendingReplayMessages = 1_024;

	private sealed class PendingSnapshotReplay(Message[] snapshots)
	{
		public Message[] Snapshots { get; } = snapshots;
		public List<Message> BufferedMessages { get; } = [];
		public bool IsFlushing { get; set; }
		public bool IsCancelled { get; set; }
	}

	private sealed class RecentIdSet(int capacity)
	{
		private readonly Dictionary<long, LinkedListNode<long>> _nodes = [];
		private readonly LinkedList<long> _order = [];

		public void Add(long id)
		{
			if (_nodes.TryGetValue(id, out var existing))
			{
				_order.Remove(existing);
				_order.AddLast(existing);
				return;
			}

			var node = _order.AddLast(id);
			_nodes.Add(id, node);

			if (_nodes.Count <= capacity)
				return;

			var oldest = _order.First;
			_order.RemoveFirst();
			_nodes.Remove(oldest.Value);
		}

		public bool Remove(long id)
		{
			if (!_nodes.Remove(id, out var node))
				return false;

			_order.Remove(node);
			return true;
		}

		public void Clear()
		{
			_nodes.Clear();
			_order.Clear();
		}
	}

	private readonly SynchronizedSet<long> _orderStatusIds = [];
	private readonly SynchronizedDictionary<long, long> _cancellationTransactions = [];
	private readonly SynchronizedDictionary<long, long> _replaceTransactions = [];
	private readonly SynchronizedDictionary<long, long> _replaceTransactionsByTransId = [];
	private readonly Lock _snapshotSync = new();
	private readonly Dictionary<long, PendingSnapshotReplay> _pendingSnapshotReplays = [];
	private readonly RecentIdSet _normalizedResponseIds = new(MaxPendingReplayMessages);
	private long _droppedPendingReplayMessages;

	/// <summary>
	/// Storage buffer.
	/// </summary>
	public IStorageBuffer Buffer { get; } = buffer ?? throw new ArgumentNullException(nameof(buffer));

	/// <summary>
	/// Number of early live messages dropped while an inner adapter delayed the subscription state.
	/// </summary>
	public long DroppedPendingReplayMessages => Interlocked.Read(ref _droppedPendingReplayMessages);

	/// <summary>
	/// Snapshot storage registry.
	/// </summary>
	public ISnapshotRegistry SnapshotRegistry { get; } = snapshotRegistry;// ?? throw new ArgumentNullException(nameof(snapshotRegistry));

	/// <summary>
	/// Storage settings.
	/// </summary>
	public StorageCoreSettings Settings { get; } = settings ?? throw new ArgumentNullException(nameof(settings));

	/// <summary>
	/// To reset the state.
	/// </summary>
	private void Reset()
	{
		_orderStatusIds.Clear();
		_cancellationTransactions.Clear();
		_replaceTransactions.Clear();
		_replaceTransactionsByTransId.Clear();

		CancelAllPendingSnapshotReplays();

		StopStorageTimer();
	}

	private ISnapshotStorage<TKey, TMessage> GetSnapshotStorage<TKey, TMessage>(DataType dataType)
		where TMessage : Message
		=> (ISnapshotStorage<TKey, TMessage>)SnapshotRegistry.GetSnapshotStorage(dataType);

	private ISnapshotStorage<SecurityId, TMessage> GetSnapshotStorage<TMessage>(DataType dataType)
		where TMessage : Message
		=> GetSnapshotStorage<SecurityId, TMessage>(dataType);

	// Snapshot mode is a setting while a snapshot store is optional, so the setting alone never means
	// there is a store to read from or write to.
	private bool UseSnapshots => SnapshotRegistry != null && Settings.IsMode(StorageModes.Snapshot);

	/// <inheritdoc />
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		if (message is ISubscriptionMessage { IsSubscribe: true } subscription)
		{
			using (_snapshotSync.EnterScope())
				_normalizedResponseIds.Remove(subscription.TransactionId);
		}

		switch (message.Type)
		{
			case MessageTypes.Reset:
				Reset();
				Buffer.ProcessInMessage(message);
				break;

			case MessageTypes.Connect:
				//Buffer.Enabled = CanAutoStorage && (_storageProcessor.StorageRegistry != null || SupportBuffer);
				StartStorageTimer();
				break;

			case MessageTypes.Disconnect:
				CancelAllPendingSnapshotReplays();

				// Whatever arrived since the last round has no next round once the connection is down,
				// so it is written out here or it is lost.
				if (StopStorageTimer())
					await FlushAsync(cancellationToken);

				break;

			case MessageTypes.OrderStatus:
			{
				if (message.Adapter != null && message.Adapter != this)
					break;

				if (Buffer.EnabledTransactions)
				{
					await ProcessOrderStatusAsync((OrderStatusMessage)message, cancellationToken);
					return;
				}

				break;
			}

			case MessageTypes.OrderRegister:
			{
				if (Buffer.EnabledTransactions)
					Buffer.ProcessInMessage(message);

				break;
			}
			case MessageTypes.OrderReplace:
			{
				if (Buffer.EnabledTransactions)
				{
					var replaceMsg = (OrderReplaceMessage)message;

					// can be looped back from offline
					_replaceTransactions.TryAdd(replaceMsg.TransactionId, replaceMsg.OriginalTransactionId);

					Buffer.ProcessInMessage(replaceMsg);
				}

				break;
			}
			case MessageTypes.OrderCancel:
			{
				if (Buffer.EnabledTransactions)
				{
					var cancelMsg = (OrderCancelMessage)message;

					// can be looped back from offline
					_cancellationTransactions.TryAdd(cancelMsg.TransactionId, cancelMsg.OriginalTransactionId);
				}

				break;
			}
			case MessageTypes.MarketData:
			{
				var mdMsg = (MarketDataMessage)message;

				Buffer.ProcessInMessage(mdMsg);

				if (!mdMsg.IsSubscribe)
				{
					CancelPendingSnapshotReplay(mdMsg.OriginalTransactionId);

					break;
				}

				var snapshots = GetSnapshots(mdMsg);

				if (snapshots.Length == 0)
					break;

				var replay = new PendingSnapshotReplay(snapshots);

				using (_snapshotSync.EnterScope())
					_pendingSnapshotReplays.Add(mdMsg.TransactionId, replay);

				try
				{
					await base.OnSendInMessageAsync(message, cancellationToken);
				}
				catch
				{
					CancelPendingSnapshotReplay(mdMsg.TransactionId, replay);

					throw;
				}

				return;
			}
		}

		if (message == null)
			return;

		await base.OnSendInMessageAsync(message, cancellationToken);
	}

	private void CancelPendingSnapshotReplay(long subscriptionId, PendingSnapshotReplay expected = null)
	{
		using (_snapshotSync.EnterScope())
		{
			if (!_pendingSnapshotReplays.TryGetValue(subscriptionId, out var replay) ||
				expected != null && !ReferenceEquals(replay, expected))
				return;

			replay.IsCancelled = true;
			replay.BufferedMessages.Clear();
			_pendingSnapshotReplays.Remove(subscriptionId);
		}
	}

	private void CancelAllPendingSnapshotReplays()
	{
		using (_snapshotSync.EnterScope())
		{
			foreach (var replay in _pendingSnapshotReplays.Values)
			{
				replay.IsCancelled = true;
				replay.BufferedMessages.Clear();
			}

			_pendingSnapshotReplays.Clear();
			_normalizedResponseIds.Clear();
		}
	}

	private Message[] GetSnapshots(MarketDataMessage message)
	{
		if (!message.IsSubscribe || message.From != null || message.To != null || !UseSnapshots)
			return [];

		var snapshots = new List<Message>();

		void AddSnapshot<TMessage>(TMessage msg)
			where TMessage : Message, ISubscriptionIdMessage
		{
			msg.SetSubscriptionIds(subscriptionId: message.TransactionId);
			snapshots.Add(msg);
		}

		void AddAll<TMessage>(ISnapshotStorage<SecurityId, TMessage> storage)
			where TMessage : Message, ISubscriptionIdMessage
		{
			if (message.SecurityId == default)
			{
				foreach (var msg in storage.GetAll())
					AddSnapshot(msg);
			}
			else
			{
				var msg = storage.Get(message.SecurityId);

				if (msg != null)
					AddSnapshot(msg);
			}
		}

		if (message.DataType2 == DataType.Level1)
			AddAll(GetSnapshotStorage<Level1ChangeMessage>(message.DataType2));
		else if (message.DataType2 == DataType.MarketDepth)
			AddAll(GetSnapshotStorage<QuoteChangeMessage>(message.DataType2));

		return [.. snapshots];
	}

	/// <summary>
	/// Process <see cref="OrderStatusMessage"/>: serve what storage holds for it, then either pass the
	/// request on or close the subscription here.
	/// </summary>
	/// <param name="message">A message requesting current registered orders and trades.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	private async ValueTask ProcessOrderStatusAsync(OrderStatusMessage message, CancellationToken cancellationToken)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (!message.IsSubscribe)
		{
			await base.OnSendInMessageAsync(message, cancellationToken);
			return;
		}

		var transId = message.TransactionId;

		_orderStatusIds.Add(transId);

		var served = new List<ExecutionMessage>();
		var covered = false;

		if (!message.HasOrderId() && message.OriginalTransactionId == 0 /*&& Settings.DaysLoad > TimeSpan.Zero*/)
		{
			var from = message.From ?? CurrentTime.Date/* - Settings.DaysLoad*/;
			var to = message.To;

			var states = message.States.ToHashSet();
			var ordersIds = new HashSet<long>();

			bool Take(ExecutionMessage stored)
			{
				if (stored.HasOrderInfo)
				{
					if (!stored.IsMatch(message, states))
						return false;

					ordersIds.Add(stored.TransactionId);
				}
				else if (!ordersIds.Contains(stored.TransactionId))
					return false;

				stored.OriginalTransactionId = transId;
				stored.SetSubscriptionIds(subscriptionId: transId);
				served.Add(stored);
				return true;
			}

			if (UseSnapshots)
			{
				var storage = GetSnapshotStorage<string, ExecutionMessage>(DataType.Transactions);

				foreach (var snapshot in storage.GetAll(from, to))
				{
					if (Take(snapshot))
						from = snapshot.ServerTime;
				}

				covered = from >= to;
			}
			else if (Settings.IsMode(StorageModes.Incremental) && message.SecurityId != default)
			{
				var storage = Settings.GetStorage<ExecutionMessage>(message.SecurityId, DataType.Transactions);

				await foreach (var stored in storage.LoadAsync(from, to).WithEnforcedCancellation(cancellationToken))
					Take(stored);
			}
		}

		// A fully local subscription is acknowledged before its data. For a partial range, replay
		// stored state before opening the live stream so an immediate live update cannot be followed
		// by an older stored value.
		if (covered)
			await RaiseNewOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = transId }, cancellationToken);

		foreach (var msg in served)
			await RaiseNewOutMessageAsync(msg, cancellationToken);

		if (covered)
			await RaiseNewOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = transId }, cancellationToken);
		else
		{
			// do not fill From field to avoid muptiple requests
			// in SubscriptionOnlineMessageAdapter
			await base.OnSendInMessageAsync(message, cancellationToken);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		Buffer.ProcessOutMessage(message);

		if (message is SubscriptionResponseMessage normalizedResponse && TryConsumeNormalizedResponse(normalizedResponse.OriginalTransactionId))
		{
			if (!normalizedResponse.IsOk())
				await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);

			return;
		}

		if (message is DisconnectMessage)
			CancelAllPendingSnapshotReplays();
		else if (message is SubscriptionFinishedMessage finished)
			CancelPendingSnapshotReplay(finished.OriginalTransactionId);

		if (message is SubscriptionResponseMessage response &&
			TryStartPendingSnapshotReplay(response.OriginalTransactionId, normalizeResponse: false, out var responseReplay))
		{
			if (response.IsOk())
				await FlushPendingSnapshotReplayAsync(response.OriginalTransactionId, responseReplay, [message], cancellationToken);
			else
			{
				try
				{
					await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
				}
				finally
				{
					CancelPendingSnapshotReplay(response.OriginalTransactionId, responseReplay);
				}
			}

			return;
		}

		if (message is SubscriptionOnlineMessage online &&
			TryStartPendingSnapshotReplay(online.OriginalTransactionId, normalizeResponse: true, out var onlineReplay))
		{
			await FlushPendingSnapshotReplayAsync(online.OriginalTransactionId, onlineReplay,
			[
				new SubscriptionResponseMessage { OriginalTransactionId = online.OriginalTransactionId },
				message,
			], cancellationToken);
			return;
		}

		if (message is ISubscriptionIdMessage subscriptionMessage)
		{
			Message passThrough = message;
			var ids = subscriptionMessage.GetSubscriptionIds();
			var warnAboutDrop = false;

			using (_snapshotSync.EnterScope())
			{
				List<long> remainingIds = null;
				var hasPending = false;

				foreach (var id in ids.Distinct())
				{
					if (_pendingSnapshotReplays.TryGetValue(id, out var replay))
					{
						hasPending = true;
						var clone = message.Clone();
						((ISubscriptionIdMessage)clone).SetSubscriptionIds(subscriptionId: id);

						if (replay.BufferedMessages.Count >= MaxPendingReplayMessages)
						{
							replay.BufferedMessages.RemoveAt(0);
							warnAboutDrop |= Interlocked.Increment(ref _droppedPendingReplayMessages) == 1;
						}

						replay.BufferedMessages.Add(clone);
					}
					else
					{
						remainingIds ??= [];
						remainingIds.Add(id);
					}
				}

				if (hasPending)
				{
					if (remainingIds is null || remainingIds.Count == 0)
						passThrough = null;
					else
					{
						passThrough = message.Clone();
						((ISubscriptionIdMessage)passThrough).SetSubscriptionIds([.. remainingIds]);
					}
				}
			}

			if (warnAboutDrop)
				LogWarning("An inner adapter produced more than {0} live messages before confirming a subscription. Old messages are being dropped.", MaxPendingReplayMessages);

			if (passThrough is null)
				return;

			message = passThrough;
		}

		await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
	}

	private bool TryConsumeNormalizedResponse(long subscriptionId)
	{
		using (_snapshotSync.EnterScope())
			return _normalizedResponseIds.Remove(subscriptionId);
	}

	private bool TryStartPendingSnapshotReplay(long subscriptionId, bool normalizeResponse, out PendingSnapshotReplay replay)
	{
		using (_snapshotSync.EnterScope())
		{
			if (_pendingSnapshotReplays.TryGetValue(subscriptionId, out replay) && !replay.IsFlushing && !replay.IsCancelled)
			{
				replay.IsFlushing = true;

				if (normalizeResponse)
					_normalizedResponseIds.Add(subscriptionId);

				return true;
			}

			replay = null;
			return false;
		}
	}

	private bool IsPendingSnapshotReplayCancelled(PendingSnapshotReplay replay)
	{
		using (_snapshotSync.EnterScope())
			return replay.IsCancelled;
	}

	private async ValueTask FlushPendingSnapshotReplayAsync(long subscriptionId, PendingSnapshotReplay replay, Message[] stateMessages, CancellationToken cancellationToken)
	{
		try
		{
			for (var i = 0; i < stateMessages.Length; i++)
			{
				if (i > 0 && IsPendingSnapshotReplayCancelled(replay))
					return;

				await base.OnInnerAdapterNewOutMessageAsync(stateMessages[i], cancellationToken);
			}

			foreach (var snapshot in replay.Snapshots)
			{
				if (IsPendingSnapshotReplayCancelled(replay))
					return;

				await base.OnInnerAdapterNewOutMessageAsync(snapshot, cancellationToken);
			}

			while (true)
			{
				Message[] buffered;

				using (_snapshotSync.EnterScope())
				{
					if (replay.IsCancelled)
						return;

					if (replay.BufferedMessages.Count == 0)
					{
						if (_pendingSnapshotReplays.TryGetValue(subscriptionId, out var current) && ReferenceEquals(current, replay))
							_pendingSnapshotReplays.Remove(subscriptionId);

						return;
					}

					buffered = [.. replay.BufferedMessages];
					replay.BufferedMessages.Clear();
				}

				foreach (var bufferedMessage in buffered)
				{
					if (IsPendingSnapshotReplayCancelled(replay))
						return;

					await base.OnInnerAdapterNewOutMessageAsync(bufferedMessage, cancellationToken);
				}
			}
		}
		finally
		{
			using (_snapshotSync.EnterScope())
			{
				if (_pendingSnapshotReplays.TryGetValue(subscriptionId, out var current) && ReferenceEquals(current, replay))
					_pendingSnapshotReplays.Remove(subscriptionId);
			}
		}
	}

	private CancellationTokenSource _cts;
	private Task _storageTask;
	private readonly Lock _timerSync = new();

	private static readonly TimeSpan _storageInterval = TimeSpan.FromSeconds(10);

	// What the buffer hands over it forgets, so a write that fails would take the data with it. Each
	// batch is written on its own, and one that cannot be written goes back to be written next round.
	private async ValueTask WriteAsync(IEnumerable<Message> batch, Func<Task> write, CancellationToken cancellationToken)
	{
		try
		{
			await write();
		}
		catch (Exception ex)
		{
			Buffer.PutBack(batch);

			if (!cancellationToken.IsCancellationRequested)
				this.AddErrorLog(ex);
		}
	}

	/// <summary>
	/// Write out everything the buffer holds right now.
	/// </summary>
	private async ValueTask FlushAsync(CancellationToken cancellationToken)
	{
		var incremental = Settings.IsMode(StorageModes.Incremental);
		var snapshot = UseSnapshots;

		foreach (var pair in Buffer.GetTicks())
		{
			if (incremental)
				await WriteAsync(pair.Value, async () => await Settings.GetStorage<ExecutionMessage>(pair.Key, DataType.Ticks).SaveAsync(pair.Value, cancellationToken), cancellationToken);
		}

		foreach (var pair in Buffer.GetOrderLog())
		{
			if (incremental)
				await WriteAsync(pair.Value, async () => await Settings.GetStorage<ExecutionMessage>(pair.Key, DataType.OrderLog).SaveAsync(pair.Value, cancellationToken), cancellationToken);
		}

		foreach (var pair in Buffer.GetTransactions())
		{
			var secId = pair.Key;

			// failed order's response doesn't contain sec id
			if (secId == default)
				continue;

			if (incremental)
				await WriteAsync(pair.Value, async () => await Settings.GetStorage<ExecutionMessage>(secId, DataType.Transactions).SaveAsync(pair.Value, cancellationToken), cancellationToken);

			if (snapshot)
			{
				var snapshotStorage = GetSnapshotStorage<string, ExecutionMessage>(DataType.Transactions);

				foreach (var message in pair.Value)
				{
					// do not store cancellation commands into snapshot
					if (message.IsCancellation)
					{
						LogWarning("Cancellation transaction: {0}", message);
						continue;
					}

					var originTransId = message.OriginalTransactionId;

					if (originTransId == 0)
						continue;

					if (_cancellationTransactions.TryGetValue(originTransId, out var cancelledId))
					{
						// do not store cancellation errors
						if (!message.IsOk())
							continue;

						// override cancel trans id by original order's registration trans id
						originTransId = cancelledId;
					}
					else if (_orderStatusIds.Contains(originTransId))
					{
						// override status request trans id by original order's registration trans id
						originTransId = message.TransactionId;
					}
					else if (_replaceTransactions.TryGetAndRemove(originTransId, out var replacedId))
					{
						if (message.IsOk())
						{
							var replaced = (ExecutionMessage)snapshotStorage.Get(replacedId.To<string>());

							if (replaced == null)
								LogWarning("Replaced order {0} not found.", replacedId);
							else if (replaced.OrderState != OrderStates.Done)
							{
								// the storage hands out copies, so the ended state is written back
								replaced.OrderState = OrderStates.Done;
								snapshotStorage.Update(replaced);
							}
						}
					}

					message.SecurityId = secId;

					if (message.TransactionId == 0)
						message.TransactionId = originTransId;

					message.OriginalTransactionId = 0;

					if (message.TransactionId != 0)
						SaveTransaction(snapshotStorage, message);
				}
			}
		}

		foreach (var pair in Buffer.GetOrderBooks())
		{
			if (incremental)
				await WriteAsync(pair.Value, async () => await Settings.GetStorage<QuoteChangeMessage>(pair.Key, DataType.MarketDepth).SaveAsync(pair.Value, cancellationToken), cancellationToken);

			if (snapshot)
			{
				var snapshotStorage = GetSnapshotStorage<QuoteChangeMessage>(DataType.MarketDepth);

				foreach (var message in pair.Value)
					snapshotStorage.Update(message);
			}
		}

		foreach (var pair in Buffer.GetLevel1())
		{
			var messages = pair.Value.Where(m => m.HasChanges()).ToArray();

			if (incremental)
				await WriteAsync(messages, async () => await Settings.GetStorage<Level1ChangeMessage>(pair.Key, DataType.Level1).SaveAsync(messages, cancellationToken), cancellationToken);

			if (snapshot)
			{
				var snapshotStorage = GetSnapshotStorage<Level1ChangeMessage>(DataType.Level1);

				foreach (var message in messages)
					snapshotStorage.Update(message);
			}
		}

		foreach (var pair in Buffer.GetCandles())
		{
			await WriteAsync(pair.Value, async () => await Settings.GetStorage(pair.Key.secId, pair.Key.dataType).SaveAsync(pair.Value, cancellationToken), cancellationToken);
		}

		foreach (var pair in Buffer.GetPositionChanges())
		{
			var messages = pair.Value.Where(m => m.HasChanges()).ToArray();

			if (incremental)
				await WriteAsync(messages, async () => await Settings.GetStorage<PositionChangeMessage>(pair.Key, DataType.PositionChanges).SaveAsync(messages, cancellationToken), cancellationToken);

			if (snapshot)
			{
				var snapshotStorage = GetSnapshotStorage<(SecurityId, string, string), PositionChangeMessage>(DataType.PositionChanges);

				foreach (var message in messages)
					snapshotStorage.Update(message);
			}
		}

		var news = Buffer.GetNews().ToArray();

		if (news.Length > 0)
		{
			await WriteAsync(news, async () => await Settings.GetStorage<NewsMessage>(default, DataType.News).SaveAsync(news, cancellationToken), cancellationToken);
		}

		var boardStates = Buffer.GetBoardStates().ToArray();

		if (boardStates.Length > 0)
		{
			await WriteAsync(boardStates, async () => await Settings.GetStorage<BoardStateMessage>(default, DataType.BoardState).SaveAsync(boardStates, cancellationToken), cancellationToken);
		}
	}

	/// <summary>
	/// Start storage auto-save thread.
	/// </summary>
	private void StartStorageTimer()
	{
		using (_timerSync.EnterScope())
		{
			if (_cts != null || !Buffer.Enabled || Buffer.DisableStorageTimer)
				return;

			_cts = new();
			var token = _cts.Token;

			_storageTask = Task.Run(async () =>
			{
				while (!token.IsCancellationRequested)
				{
					try
					{
						await FlushAsync(token);

						await _storageInterval.Delay(token);
					}
					catch (Exception ex)
					{
						if (!token.IsCancellationRequested)
							this.AddErrorLog(ex);
					}
				}
			}, token);
		}
	}

	/// <summary>
	/// Stop the storage auto-save thread.
	/// </summary>
	/// <returns><see langword="true"/> if it was running.</returns>
	private bool StopStorageTimer()
	{
		using (_timerSync.EnterScope())
		{
			var cts = _cts;

			if (cts == null)
				return false;

			_cts = null;

			try
			{
				cts.Cancel();
			}
			catch { }

			try
			{
				_storageTask?.Wait(TimeSpan.FromSeconds(1));
			}
			catch { }

			try
			{
				cts.Dispose();
			}
			catch { }

			_storageTask = null;

			return true;
		}
	}

	private static void SaveTransaction(ISnapshotStorage snapshotStorage, ExecutionMessage message)
	{
		ExecutionMessage sepTrade = null;

		if (message.HasOrderInfo && message.HasTradeInfo)
		{
			sepTrade = new ExecutionMessage
			{
				SecurityId = message.SecurityId,
				ServerTime = message.ServerTime,
				TransactionId = message.TransactionId,
				DataTypeEx = message.DataTypeEx,
				TradeId = message.TradeId,
				TradeVolume = message.TradeVolume,
				TradePrice = message.TradePrice,
				TradeStatus = message.TradeStatus,
				TradeStringId = message.TradeStringId,
				OriginSide = message.OriginSide,
				Commission = message.Commission,
				IsSystem = message.IsSystem,
			};

			message.TradeId = null;
			message.TradeVolume = null;
			message.TradePrice = null;
			message.TradeStatus = null;
			message.TradeStringId = null;
			message.OriginSide = null;
		}

		snapshotStorage.Update(message);

		if (sepTrade != null)
			snapshotStorage.Update(sepTrade);
	}

	/// <summary>
	/// Create a copy of <see cref="BufferMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
	{
		return new BufferMessageAdapter(InnerAdapter.TypedClone(), Settings, Buffer.Clone(), SnapshotRegistry);
	}

	/// <inheritdoc />
	public override void Dispose()
	{
		StopStorageTimer();
		base.Dispose();
	}
}

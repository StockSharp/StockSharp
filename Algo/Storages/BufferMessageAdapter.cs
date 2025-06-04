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
public class BufferMessageAdapter(IMessageAdapter innerAdapter, StorageCoreSettings settings, StorageBuffer buffer, SnapshotRegistry snapshotRegistry) : MessageAdapterWrapper(innerAdapter)
{
	private readonly SynchronizedSet<long> _orderStatusIds = [];
	private readonly SynchronizedDictionary<long, long> _cancellationTransactions = [];
	private readonly SynchronizedDictionary<long, long> _replaceTransactions = [];
	private readonly SynchronizedDictionary<long, long> _replaceTransactionsByTransId = [];

	/// <summary>
	/// Storage buffer.
	/// </summary>
	public StorageBuffer Buffer { get; } = buffer ?? throw new ArgumentNullException(nameof(buffer));

	/// <summary>
	/// Snapshot storage registry.
	/// </summary>
	public SnapshotRegistry SnapshotRegistry { get; } = snapshotRegistry;// ?? throw new ArgumentNullException(nameof(snapshotRegistry));

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
	}

	private ISnapshotStorage<TKey, TMessage> GetSnapshotStorage<TKey, TMessage>(DataType dataType)
		where TMessage : Message
		=> (ISnapshotStorage<TKey, TMessage>)SnapshotRegistry.GetSnapshotStorage(dataType);

	private ISnapshotStorage<SecurityId, TMessage> GetSnapshotStorage<TMessage>(DataType dataType)
		where TMessage : Message
		=> GetSnapshotStorage<SecurityId, TMessage>(dataType);

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
	{
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

			case MessageTypes.OrderStatus:
			{
				if (message.Adapter != null && message.Adapter != this)
					break;

				if (Buffer.EnabledTransactions)
					message = ProcessOrderStatus((OrderStatusMessage)message);

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
					if (!_replaceTransactions.ContainsKey(replaceMsg.TransactionId))
						_replaceTransactions.Add(replaceMsg.TransactionId, replaceMsg.OriginalTransactionId);

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
					if (!_cancellationTransactions.ContainsKey(cancelMsg.TransactionId))
						_cancellationTransactions.Add(cancelMsg.TransactionId, cancelMsg.OriginalTransactionId);
				}

				break;
			}
			case MessageTypes.MarketData:
				ProcessMarketData((MarketDataMessage)message);
				break;
		}

		if (message == null)
			return true;

		return base.OnSendInMessage(message);
	}

	private void ProcessMarketData(MarketDataMessage message)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		Buffer.ProcessInMessage(message);

		if (message.IsSubscribe && message.From == null && message.To == null && Settings.IsMode(StorageModes.Snapshot))
		{
			void SendSnapshot<TMessage>(TMessage msg)
				where TMessage : Message, ISubscriptionIdMessage
			{
				msg.SetSubscriptionIds(subscriptionId: message.TransactionId);
				RaiseNewOutMessage(msg);
			}

			if (message.DataType2 == DataType.Level1)
			{
				var l1Storage = GetSnapshotStorage<Level1ChangeMessage>(message.DataType2);

				if (message.SecurityId == default)
				{
					foreach (var msg in l1Storage.GetAll())
						SendSnapshot(msg);
				}
				else
				{
					var level1Msg = l1Storage.Get(message.SecurityId);

					if (level1Msg != null)
					{
						//SendReply();
						SendSnapshot(level1Msg);
					}
				}
			}
			else if (message.DataType2 == DataType.MarketDepth)
			{
				var	quotesStorage = GetSnapshotStorage<QuoteChangeMessage>(message.DataType2);

				if (message.SecurityId == default)
				{
					foreach (var msg in quotesStorage.GetAll())
						SendSnapshot(msg);
				}
				else
				{
					var quotesMsg = quotesStorage.Get(message.SecurityId);

					if (quotesMsg != null)
					{
						//SendReply();
						SendSnapshot(quotesMsg);
					}
				}
			}
		}
	}

	/// <summary>
	/// Process <see cref="OrderStatusMessage"/>.
	/// </summary>
	/// <param name="message">A message requesting current registered orders and trades.</param>
	/// <returns>A message requesting current registered orders and trades.</returns>
	private OrderStatusMessage ProcessOrderStatus(OrderStatusMessage message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (!message.IsSubscribe)
			return message;

		var transId = message.TransactionId;

		_orderStatusIds.Add(transId);

		if (!message.HasOrderId() && message.OriginalTransactionId == 0 /*&& Settings.DaysLoad > TimeSpan.Zero*/)
		{
			var from = message.From ?? CurrentTime.UtcDateTime.Date/* - Settings.DaysLoad*/;
			var to = message.To;

			if (Settings.IsMode(StorageModes.Snapshot))
			{
				var states = message.States.ToSet();

				var ordersIds = new HashSet<long>();

				var storage = GetSnapshotStorage<string, ExecutionMessage>(DataType.Transactions);

				foreach (var snapshot in storage.GetAll(from, to))
				{
					if (snapshot.HasOrderInfo)
					{
						if (!snapshot.IsMatch(message, states))
							continue;

						ordersIds.Add(snapshot.TransactionId);
					}
					else if (!ordersIds.Contains(snapshot.TransactionId))
						continue;

					snapshot.OriginalTransactionId = transId;
					snapshot.SetSubscriptionIds(subscriptionId: transId);
					RaiseNewOutMessage(snapshot);

					from = snapshot.ServerTime;
				}

				if (from >= to)
					return null;

				// do not fill From field to avoid muptiple requests
				// in SubscriptionOnlineMessageAdapter
				//
				//message.From = from;
			}
			else if (Settings.IsMode(StorageModes.Incremental))
			{
				if (message.SecurityId != default)
				{
					// TODO restore last actual state from incremental messages

					//GetStorage<ExecutionMessage>(msg.SecurityId, ExecutionTypes.Transaction)
					//	.Load(from, to)
					//	.ForEach(RaiseStorageMessage);
				}
			}
		}

		return message;
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		Buffer.ProcessOutMessage(message);

		base.OnInnerAdapterNewOutMessage(message);
	}

	private Timer _timer;

	/// <summary>
	/// Start storage auto-save thread.
	/// </summary>
	private void StartStorageTimer()
	{
		if (_timer != null || !Buffer.Enabled || Buffer.DisableStorageTimer)
			return;

		var isProcessing = false;
		var sync = new SyncObject();

		_timer = ThreadingHelper.Timer(() =>
		{
			lock (sync)
			{
				if (isProcessing)
					return;

				isProcessing = true;
			}

			try
			{
				var incremental = Settings.IsMode(StorageModes.Incremental);
				var snapshot = Settings.IsMode(StorageModes.Snapshot);

				foreach (var pair in Buffer.GetTicks())
				{
					if (incremental)
						Settings.GetStorage<ExecutionMessage>(pair.Key, ExecutionTypes.Tick).Save(pair.Value);
				}

				foreach (var pair in Buffer.GetOrderLog())
				{
					if (incremental)
						Settings.GetStorage<ExecutionMessage>(pair.Key, ExecutionTypes.OrderLog).Save(pair.Value);
				}

				foreach (var pair in Buffer.GetTransactions())
				{
					var secId = pair.Key;

					// failed order's response doesn't contain sec id
					if (secId == default)
						continue;

					if (incremental)
						Settings.GetStorage<ExecutionMessage>(secId, ExecutionTypes.Transaction).Save(pair.Value);

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
									else
									{
										if (replaced.OrderState != OrderStates.Done)
											replaced.OrderState = OrderStates.Done;
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
						Settings.GetStorage<QuoteChangeMessage>(pair.Key, null).Save(pair.Value);

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
						Settings.GetStorage<Level1ChangeMessage>(pair.Key, null).Save(messages);

					if (Settings.IsMode(StorageModes.Snapshot))
					{
						var snapshotStorage = GetSnapshotStorage<Level1ChangeMessage>(DataType.Level1);

						foreach (var message in messages)
							snapshotStorage.Update(message);
					}
				}

				foreach (var pair in Buffer.GetCandles())
				{
					Settings.GetStorage(pair.Key.secId, pair.Key.dataType.MessageType, pair.Key.dataType.Arg).Save(pair.Value);
				}

				foreach (var pair in Buffer.GetPositionChanges())
				{
					var messages = pair.Value.Where(m => m.HasChanges()).ToArray();

					if (incremental)
						Settings.GetStorage<PositionChangeMessage>(pair.Key, null).Save(messages);

					if (snapshot)
					{
						var snapshotStorage = GetSnapshotStorage<(SecurityId, string, string), PositionChangeMessage >(DataType.PositionChanges);

						foreach (var message in messages)
							snapshotStorage.Update(message);
					}
				}

				var news = Buffer.GetNews().ToArray();

				if (news.Length > 0)
				{
					Settings.GetStorage<NewsMessage>(default, null).Save(news);
				}

				var boardStates = Buffer.GetBoardStates().ToArray();

				if (boardStates.Length > 0)
				{
					Settings.GetStorage<BoardStateMessage>(default, null).Save(boardStates);
				}
			}
			catch (Exception excp)
			{
				this.AddErrorLog(excp);
			}
			finally
			{
				lock (sync)
					isProcessing = false;
			}
		}).Interval(TimeSpan.FromSeconds(10));
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
	public override IMessageChannel Clone()
	{
		return new BufferMessageAdapter(InnerAdapter.TypedClone(), Settings, Buffer.Clone(), SnapshotRegistry);
	}
}
namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Logging;

	/// <summary>
	/// Buffered message adapter.
	/// </summary>
	public class BufferMessageAdapter : MessageAdapterWrapper
	{
		private readonly SynchronizedSet<long> _orderStatusIds = new SynchronizedSet<long>();
		private readonly SynchronizedDictionary<long, long> _orderIds = new SynchronizedDictionary<long, long>();
		private readonly SynchronizedDictionary<string, long> _orderStringIds = new SynchronizedDictionary<string, long>(StringComparer.InvariantCultureIgnoreCase);

		private readonly SynchronizedDictionary<long, long> _cancellationTransactions = new SynchronizedDictionary<long, long>();

		/// <summary>
		/// Initializes a new instance of the <see cref="BufferMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		/// <param name="settings">Storage settings.</param>
		/// <param name="buffer">Storage buffer.</param>
		/// <param name="snapshotRegistry">Snapshot storage registry.</param>
		public BufferMessageAdapter(IMessageAdapter innerAdapter, StorageCoreSettings settings, StorageBuffer buffer, SnapshotRegistry snapshotRegistry)
			: base(innerAdapter)
		{
			Settings = settings ?? throw new ArgumentNullException(nameof(settings));
			Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
			SnapshotRegistry = snapshotRegistry;// ?? throw new ArgumentNullException(nameof(snapshotRegistry));
		}

		/// <summary>
		/// Storage buffer.
		/// </summary>
		public StorageBuffer Buffer { get; }

		/// <summary>
		/// Snapshot storage registry.
		/// </summary>
		public SnapshotRegistry SnapshotRegistry { get; }

		/// <summary>
		/// Storage settings.
		/// </summary>
		public StorageCoreSettings Settings { get; }

		/// <summary>
		/// To reset the state.
		/// </summary>
		private void Reset()
		{
			_cancellationTransactions.Clear();
			_orderIds.Clear();
			_orderStringIds.Clear();
			_orderStatusIds.Clear();
		}

		private ISnapshotStorage GetSnapshotStorage(DataType dataType)
			=> SnapshotRegistry.GetSnapshotStorage(dataType.MessageType, dataType.Arg);

		/// <inheritdoc />
		protected override bool OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
					Reset();
					Buffer.SendInMessage(message);
					break;

				case MessageTypes.Connect:
					//Buffer.Enabled = CanAutoStorage && (_storageProcessor.StorageRegistry != null || SupportBuffer);
					StartStorageTimer();
					break;

				case MessageTypes.OrderStatus:
				{
					if (message.Adapter != null && message.Adapter != this)
						break;

					message = ProcessOrderStatus((OrderStatusMessage)message);
					break;
				}

				case MessageTypes.OrderCancel:
				{
					var cancelMsg = (OrderCancelMessage)message;

					// can be looped back from offline
					_cancellationTransactions.TryAdd(cancelMsg.TransactionId, cancelMsg.OriginalTransactionId);

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

			Buffer.SendInMessage(message);

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
					var l1Storage = GetSnapshotStorage(message.DataType2);

					if (message.SecurityId == default)
					{
						foreach (Level1ChangeMessage msg in l1Storage.GetAll())
							SendSnapshot(msg);
					}
					else
					{
						var level1Msg = (Level1ChangeMessage)l1Storage.Get(message.SecurityId);

						if (level1Msg != null)
						{
							//SendReply();
							SendSnapshot(level1Msg);
						}
					}
				}
				else if (message.DataType2 == DataType.MarketDepth)
				{
					var	quotesStorage = GetSnapshotStorage(message.DataType2);

					if (message.SecurityId == default)
					{
						foreach (QuoteChangeMessage msg in quotesStorage.GetAll())
							SendSnapshot(msg);
					}
					else
					{
						var quotesMsg = (QuoteChangeMessage)quotesStorage.Get(message.SecurityId);

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

			if (!message.HasOrderId() && message.OriginalTransactionId == 0 && Settings.DaysLoad > TimeSpan.Zero)
			{
				var from = message.From ?? DateTime.UtcNow.Date - Settings.DaysLoad;
				var to = message.To;

				if (Settings.IsMode(StorageModes.Snapshot))
				{
					var storage = (ISnapshotStorage<string, ExecutionMessage>)GetSnapshotStorage(DataType.Transactions);

					foreach (var snapshot in storage.GetAll(from, to))
					{
						if (snapshot.OrderId != null)
							_orderIds.TryAdd(snapshot.OrderId.Value, snapshot.TransactionId);
						else if (!snapshot.OrderStringId.IsEmpty())
							_orderStringIds.TryAdd(snapshot.OrderStringId, snapshot.TransactionId);

						snapshot.OriginalTransactionId = transId;
						snapshot.SetSubscriptionIds(subscriptionId: transId);
						RaiseNewOutMessage(snapshot);

						from = snapshot.ServerTime;
					}

					if (from >= to)
						return null;

					message.From = from;
				}
				else if (Settings.IsMode(StorageModes.Incremental))
				{
					if (!message.SecurityId.IsDefault())
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
			Buffer.SendOutMessage(message);

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

			var unkByOrderId = new Dictionary<long, List<ExecutionMessage>>();
			var unkByOrderStringId = new Dictionary<string, List<ExecutionMessage>>(StringComparer.InvariantCultureIgnoreCase);

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

						if (incremental)
							Settings.GetStorage<ExecutionMessage>(secId, ExecutionTypes.Transaction).Save(pair.Value);

						if (snapshot)
						{
							var snapshotStorage = GetSnapshotStorage(DataType.Transactions);

							foreach (var message in pair.Value)
							{
								// do not store cancellation commands into snapshot
								if (message.IsCancellation/* && message.TransactionId != 0*/)
								{
									continue;
								}

								var originTransId = message.OriginalTransactionId;

								if (message.TransactionId == 0 && originTransId == 0)
								{
									if (!message.HasTradeInfo)
										continue;

									long transId;

									if (message.OrderId != null)
									{
										if (!_orderIds.TryGetValue(message.OrderId.Value, out transId))
										{
											unkByOrderId.SafeAdd(message.OrderId.Value).Add(message);
											continue;
										}
									}
									else if (!message.OrderStringId.IsEmpty())
									{
										if (!_orderStringIds.TryGetValue(message.OrderStringId, out transId))
										{
											unkByOrderStringId.SafeAdd(message.OrderStringId).Add(message);
											continue;
										}
									}
									else
										continue;

									originTransId = transId;
								}
								else
								{
									if (originTransId != 0)
									{
										if (/*message.TransactionId == 0 && */_cancellationTransactions.TryGetValue(originTransId, out var temp))
										{
											// do not store cancellation errors
											if (message.Error != null)
												continue;

											// override cancel trans id by original order's registration trans id
											originTransId = temp;
										}

										if (_orderStatusIds.Contains(originTransId))
										{
											// override status request trans id by original order's registration trans id
											originTransId = message.TransactionId;
										}
									}

									if (originTransId != 0)
									{
										if (message.OrderId != null)
											_orderIds.TryAdd(message.OrderId.Value, originTransId);
										else if (message.OrderStringId != null)
											_orderStringIds.TryAdd(message.OrderStringId, originTransId);
									}
								}

								message.SecurityId = secId;

								if (message.TransactionId == 0)
									message.TransactionId = originTransId;

								message.OriginalTransactionId = 0;

								SaveTransaction(snapshotStorage, message);

								if (message.OrderId != null)
								{
									if (unkByOrderId.TryGetValue(message.OrderId.Value, out var suspended))
									{
										unkByOrderId.Remove(message.OrderId.Value);

										foreach (var trade in suspended)
										{
											trade.TransactionId = message.TransactionId;
											SaveTransaction(snapshotStorage, trade);
										}
									}
								}
								else if (!message.OrderStringId.IsEmpty())
								{
									if (unkByOrderStringId.TryGetValue(message.OrderStringId, out var suspended))
									{
										unkByOrderStringId.Remove(message.OrderStringId);

										foreach (var trade in suspended)
										{
											trade.TransactionId = message.TransactionId;
											SaveTransaction(snapshotStorage, trade);
										}
									}
								}
							}
						}
					}

					foreach (var pair in Buffer.GetOrderBooks())
					{
						if (incremental)
							Settings.GetStorage<QuoteChangeMessage>(pair.Key, null).Save(pair.Value);
						
						if (snapshot)
						{
							var snapshotStorage = GetSnapshotStorage(DataType.MarketDepth);

							foreach (var message in pair.Value)
								snapshotStorage.Update(message);
						}
					}

					foreach (var pair in Buffer.GetLevel1())
					{
						var messages = pair.Value.Where(m => m.Changes.Count > 0).ToArray();

						if (incremental)
							Settings.GetStorage<Level1ChangeMessage>(pair.Key, null).Save(messages);
						
						if (Settings.IsMode(StorageModes.Snapshot))
						{
							var snapshotStorage = GetSnapshotStorage(DataType.Level1);

							foreach (var message in messages)
								snapshotStorage.Update(message);
						}
					}

					foreach (var pair in Buffer.GetCandles())
					{
						Settings.GetStorage(pair.Key.Item1, pair.Key.Item2, pair.Key.Item3).Save(pair.Value);
					}

					foreach (var pair in Buffer.GetPositionChanges())
					{
						var messages = pair.Value.Where(m => m.Changes.Count > 0).ToArray();

						if (incremental)
							Settings.GetStorage<PositionChangeMessage>(pair.Key, null).Save(messages);
						
						if (snapshot)
						{
							var snapshotStorage = GetSnapshotStorage(DataType.PositionChanges);

							foreach (var message in messages)
								snapshotStorage.Update(message);
						}
					}

					var news = Buffer.GetNews().ToArray();

					if (news.Length > 0)
					{
						Settings.GetStorage<NewsMessage>(default, null).Save(news);
					}
				}
				catch (Exception excp)
				{
					excp.LogError();
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
					HasTradeInfo = true,
					SecurityId = message.SecurityId,
					ServerTime = message.ServerTime,
					TransactionId = message.TransactionId,
					ExecutionType = message.ExecutionType,
					TradeId = message.TradeId,
					TradeVolume = message.TradeVolume,
					TradePrice = message.TradePrice,
					TradeStatus = message.TradeStatus,
					TradeStringId = message.TradeStringId,
					OriginSide = message.OriginSide,
					Commission = message.Commission,
					IsSystem = message.IsSystem,
				};

				message.HasTradeInfo = false;
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
}
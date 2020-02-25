namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Candles.Compression;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Storage modes.
	/// </summary>
	[Flags]
	public enum StorageModes
	{
		/// <summary>
		/// None.
		/// </summary>
		None = 1,

		/// <summary>
		/// Incremental.
		/// </summary>
		Incremental = None << 1,

		/// <summary>
		/// Snapshot.
		/// </summary>
		Snapshot = Incremental << 1,
	}

	/// <summary>
	/// Storage processor.
	/// </summary>
	public class StorageProcessor : IPersistable
	{
		private readonly SynchronizedSet<long> _fullyProcessedSubscriptions = new SynchronizedSet<long>();
		private readonly SynchronizedSet<long> _orderStatusIds = new SynchronizedSet<long>();
		private readonly SynchronizedDictionary<long, long> _orderIds = new SynchronizedDictionary<long, long>();
		private readonly SynchronizedDictionary<string, long> _orderStringIds = new SynchronizedDictionary<string, long>(StringComparer.InvariantCultureIgnoreCase);

		private readonly SynchronizedDictionary<long, long> _cancellationTransactions = new SynchronizedDictionary<long, long>();

		/// <summary>
		/// Initializes a new instance of the <see cref="StorageProcessor"/>.
		/// </summary>
		/// <param name="storageRegistry">The storage of market data.</param>
		/// <param name="snapshotRegistry">Snapshot storage registry.</param>
		/// <param name="candleBuilderProvider">Candle builders provider.</param>
		public StorageProcessor(IStorageRegistry storageRegistry, SnapshotRegistry snapshotRegistry, CandleBuilderProvider candleBuilderProvider)
		{
			StorageRegistry = storageRegistry;// ?? throw new ArgumentNullException(nameof(storageRegistry));
			SnapshotRegistry = snapshotRegistry;// ?? throw new ArgumentNullException(nameof(snapshotRegistry));
			CandleBuilderProvider = candleBuilderProvider ?? throw new ArgumentNullException(nameof(candleBuilderProvider));
		}

		/// <summary>
		/// The storage of market data.
		/// </summary>
		public IStorageRegistry StorageRegistry { get; }

		/// <summary>
		/// Snapshot storage registry.
		/// </summary>
		public SnapshotRegistry SnapshotRegistry { get; }

		/// <summary>
		/// Candle builders provider.
		/// </summary>
		public CandleBuilderProvider CandleBuilderProvider { get; }

		/// <summary>
		/// The storage (database, file etc.).
		/// </summary>
		public IMarketDataDrive Drive { get; set; }

		/// <summary>
		/// Format.
		/// </summary>
		public StorageFormats Format { get; set; }

		private TimeSpan _daysLoad;

		/// <summary>
		/// Max days to load stored data.
		/// </summary>
		public TimeSpan DaysLoad
		{
			get => _daysLoad;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

				_daysLoad = value;
			}
		}

		/// <summary>
		/// Cache buildable from smaller time-frames candles.
		/// </summary>
		public bool CacheBuildableCandles { get; set; }

		/// <summary>
		/// Storage mode. By default is <see cref="StorageModes.Incremental"/>.
		/// </summary>
		public StorageModes Mode { get; set; } = StorageModes.Incremental;

		/// <summary>
		/// Storage buffer.
		/// </summary>
		public StorageBuffer Buffer { get; } = new StorageBuffer();

		private ISnapshotStorage GetSnapshotStorage(DataType dataType)
			=> SnapshotRegistry.GetSnapshotStorage(dataType.MessageType, dataType.Arg);

		private IMarketDataStorage<TMessage> GetStorage<TMessage>(SecurityId securityId, object arg)
			where TMessage : Message
		{
			return (IMarketDataStorage<TMessage>)GetStorage(securityId, typeof(TMessage), arg);
		}

		private IMarketDataStorage GetStorage(SecurityId securityId, Type messageType, object arg)
		{
			return StorageRegistry.GetStorage(securityId, messageType, arg, Drive, Format);
		}

		private IMarketDataStorage<CandleMessage> GetTimeFrameCandleMessageStorage(SecurityId securityId, TimeSpan timeFrame, bool allowBuildFromSmallerTimeFrame)
		{
			if (!allowBuildFromSmallerTimeFrame)
				return StorageRegistry.GetCandleMessageStorage(typeof(TimeFrameCandleMessage), securityId, timeFrame, Drive, Format);

			var storage = StorageRegistry.GetCandleMessageBuildableStorage(securityId, timeFrame, Drive, Format);

			if (CacheBuildableCandles)
				storage = new CacheableMarketDataStorage<CandleMessage>(storage, StorageRegistry.GetCandleMessageStorage(typeof(TimeFrameCandleMessage), securityId, timeFrame, Drive, Format));

			return storage;
		}

		/// <summary>
		/// To reset the state.
		/// </summary>
		public void Reset()
		{
			_cancellationTransactions.Clear();
			_orderIds.Clear();
			_orderStringIds.Clear();
			_orderStatusIds.Clear();
			_fullyProcessedSubscriptions.Clear();
		}

		/// <summary>
		/// Process <see cref="OrderStatusMessage"/>.
		/// </summary>
		/// <param name="message">A message requesting current registered orders and trades.</param>
		/// <param name="newOutMessage">New message event.</param>
		/// <returns>A message requesting current registered orders and trades.</returns>
		public OrderStatusMessage ProcessOrderStatus(OrderStatusMessage message, Action<Message> newOutMessage)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (!message.IsSubscribe)
				return message;

			var transId = message.TransactionId;

			_orderStatusIds.Add(transId);

			if (!message.HasOrderId() && message.OriginalTransactionId == 0 && DaysLoad > TimeSpan.Zero)
			{
				var from = message.From ?? DateTime.UtcNow.Date - DaysLoad;
				var to = message.To;

				if (Mode.Contains(StorageModes.Snapshot))
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
						newOutMessage(snapshot);

						from = snapshot.ServerTime;
					}

					if (from >= to)
						return null;

					message.From = from;
				}
				else if (Mode.Contains(StorageModes.Incremental))
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

		/// <summary>
		/// Process <see cref="OrderCancelMessage"/>.
		/// </summary>
		/// <param name="message">A message containing the data for the cancellation of the order.</param>
		/// <returns>A message containing the data for the cancellation of the order.</returns>
		public OrderCancelMessage ProcessOrderCancel(OrderCancelMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			// can be looped back from offline
			_cancellationTransactions.TryAdd(message.TransactionId, message.OriginalTransactionId);

			return message;
		}

		/// <summary>
		/// Process <see cref="MarketDataMessage"/>.
		/// </summary>
		/// <param name="message">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
		/// <param name="newOutMessage">New message event.</param>
		/// <returns>Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</returns>
		public MarketDataMessage ProcessMarketData(MarketDataMessage message, Action<Message> newOutMessage)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (message.From == null && DaysLoad == TimeSpan.Zero)
				return message;

			if (message.IsSubscribe)
			{
				var transactionId = message.TransactionId;

				var lastTime = LoadMessages(message, message.From, message.To, transactionId, newOutMessage);

				if (message.To != null && lastTime != null && message.To <= lastTime)
				{
					_fullyProcessedSubscriptions.Add(transactionId);
					newOutMessage(new SubscriptionFinishedMessage { OriginalTransactionId = transactionId });

					return null;
				}

				if (lastTime != null)
				{
					if (!(message.DataType == MarketDataTypes.MarketDepth && message.From == null && message.To == null))
					{
						var clone = (MarketDataMessage)message.Clone();
						clone.From = lastTime;
						message = clone;
						message.ValidateBounds();
					}
				}
			}
			else
			{
				if (_fullyProcessedSubscriptions.Remove(message.OriginalTransactionId))
				{
					newOutMessage(new SubscriptionResponseMessage
					{
						OriginalTransactionId = message.TransactionId,
					});

					return null;
				}
			}

			return message;
		}

		private DateTimeOffset? LoadMessages(MarketDataMessage msg, DateTimeOffset? from, DateTimeOffset? to, long transactionId, Action<Message> newOutMessage)
		{
			void SendReply() => newOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = transactionId });

			DateTimeOffset? lastTime = null;

			var secId = msg.SecurityId;

			switch (msg.DataType)
			{
				case MarketDataTypes.Level1:
					if (Mode.Contains(StorageModes.Snapshot))
					{
						var level1Msg = (Level1ChangeMessage)GetSnapshotStorage(DataType.Level1).Get(secId);

						if (level1Msg != null)
						{
							SendReply();

							lastTime = level1Msg.ServerTime;

							level1Msg.SetSubscriptionIds(subscriptionId: transactionId);
							newOutMessage(level1Msg);
						}
					}
					else if (Mode.Contains(StorageModes.Incremental))
						lastTime = LoadMessages(GetStorage<Level1ChangeMessage>(secId, null), from, to, TimeSpan.Zero, transactionId, SendReply, newOutMessage);
					
					break;

				case MarketDataTypes.MarketDepth:
					if (Mode.Contains(StorageModes.Snapshot))
					{
						var quotesMsg = (QuoteChangeMessage)GetSnapshotStorage(DataType.MarketDepth).Get(secId);

						if (quotesMsg != null)
						{
							SendReply();

							lastTime = quotesMsg.ServerTime;

							quotesMsg.SetSubscriptionIds(subscriptionId: transactionId);
							newOutMessage(quotesMsg);
						}
					}
					else if (Mode.Contains(StorageModes.Incremental))
						lastTime = LoadMessages(GetStorage<QuoteChangeMessage>(secId, null), from, to, TimeSpan.Zero, transactionId, SendReply, newOutMessage);
					
					break;

				case MarketDataTypes.Trades:
					lastTime = LoadMessages(GetStorage<ExecutionMessage>(secId, ExecutionTypes.Tick), from, to, DaysLoad, transactionId, SendReply, newOutMessage);
					break;

				case MarketDataTypes.OrderLog:
					lastTime = LoadMessages(GetStorage<ExecutionMessage>(secId, ExecutionTypes.OrderLog), from, to, DaysLoad, transactionId, SendReply, newOutMessage);
					break;

				case MarketDataTypes.News:
					lastTime = LoadMessages(StorageRegistry.GetNewsMessageStorage(Drive, Format), from, to, DaysLoad, transactionId, SendReply, newOutMessage);
					break;

				case MarketDataTypes.Board:
					lastTime = LoadMessages(StorageRegistry.GetBoardStateMessageStorage(Drive, Format), from, to, DaysLoad, transactionId, SendReply, newOutMessage);
					break;

				case MarketDataTypes.CandleTimeFrame:
					var tf = msg.GetTimeFrame();

					if (msg.IsCalcVolumeProfile)
					{
						IMarketDataStorage storage;

						switch (msg.BuildFrom)
						{
							case null:
							case MarketDataTypes.Trades:
								storage = GetStorage(secId, typeof(ExecutionMessage), ExecutionTypes.Tick);
								break;

							case MarketDataTypes.OrderLog:
								storage = GetStorage(secId, typeof(ExecutionMessage), ExecutionTypes.OrderLog);
								break;

							case MarketDataTypes.Level1:
								storage = GetStorage(secId, typeof(Level1ChangeMessage), null);
								break;

							case MarketDataTypes.MarketDepth:
								storage = GetStorage(secId, typeof(QuoteChangeMessage), null);
								break;

							default:
								throw new ArgumentOutOfRangeException(nameof(msg), msg.BuildFrom, LocalizedStrings.Str1219);
						}

						var range = GetRange(storage, from, to, TimeSpan.FromDays(2));

						if (range != null)
						{
							var mdMsg = (MarketDataMessage)msg.Clone();
							mdMsg.From = mdMsg.To = null;

							switch (msg.BuildFrom)
							{
								case null:
								case MarketDataTypes.Trades:
									lastTime = LoadMessages(((IMarketDataStorage<ExecutionMessage>)storage)
									                        .Load(range.Item1.Date, range.Item2.Date.EndOfDay())
									                        .ToCandles(mdMsg, candleBuilderProvider: CandleBuilderProvider), range.Item1, transactionId, SendReply, newOutMessage);

									break;

								case MarketDataTypes.OrderLog:
								{
									switch (msg.BuildField)
									{
										case null:
										case Level1Fields.LastTradePrice:
											lastTime = LoadMessages(((IMarketDataStorage<ExecutionMessage>)storage)
											                        .Load(range.Item1.Date, range.Item2.Date.EndOfDay())
											                        .ToCandles(mdMsg, candleBuilderProvider: CandleBuilderProvider), range.Item1, transactionId, SendReply, newOutMessage);

											break;
											
										// TODO
										//case Level1Fields.SpreadMiddle:
										//	lastTime = LoadMessages(((IMarketDataStorage<ExecutionMessage>)storage)
										//	    .Load(range.Item1.Date, range.Item2.Date.EndOfDay())
										//		.ToMarketDepths(OrderLogBuilders.Plaza2.CreateBuilder(security.ToSecurityId()))
										//	    .ToCandles(mdMsg, false, exchangeInfoProvider: exchangeInfoProvider), range.Item1, transactionId, SendReply, newOutMessage);
										//	break;
									}

									break;
								}

								case MarketDataTypes.Level1:
									switch (msg.BuildField)
									{
										case null:
										case Level1Fields.LastTradePrice:
											lastTime = LoadMessages(((IMarketDataStorage<Level1ChangeMessage>)storage)
											                        .Load(range.Item1.Date, range.Item2.Date.EndOfDay())
											                        .ToTicks()
											                        .ToCandles(mdMsg, candleBuilderProvider: CandleBuilderProvider), range.Item1, transactionId, SendReply, newOutMessage);
											break;

										case Level1Fields.BestBidPrice:
										case Level1Fields.BestAskPrice:
										case Level1Fields.SpreadMiddle:
											lastTime = LoadMessages(((IMarketDataStorage<Level1ChangeMessage>)storage)
											                        .Load(range.Item1.Date, range.Item2.Date.EndOfDay())
											                        .ToOrderBooks()
											                        .ToCandles(mdMsg, msg.BuildField.Value, candleBuilderProvider: CandleBuilderProvider), range.Item1, transactionId, SendReply, newOutMessage);
											break;
									}
									
									break;

								case MarketDataTypes.MarketDepth:
									lastTime = LoadMessages(((IMarketDataStorage<QuoteChangeMessage>)storage)
									                        .Load(range.Item1.Date, range.Item2.Date.EndOfDay())
									                        .ToCandles(mdMsg, msg.BuildField ?? Level1Fields.SpreadMiddle, candleBuilderProvider: CandleBuilderProvider), range.Item1, transactionId, SendReply, newOutMessage);
									break;

								default:
									throw new ArgumentOutOfRangeException(nameof(msg), msg.BuildFrom, LocalizedStrings.Str1219);
							}
						}
					}
					else
					{
						var days = DaysLoad;

						//if (tf.Ticks > 1)
						//{
						//	if (tf.TotalMinutes < 15)
						//		days = TimeSpan.FromTicks(tf.Ticks * 10000);
						//	else if (tf.TotalHours < 2)
						//		days = TimeSpan.FromTicks(tf.Ticks * 1000);
						//	else if (tf.TotalDays < 2)
						//		days = TimeSpan.FromTicks(tf.Ticks * 100);
						//	else
						//		days = TimeSpan.FromTicks(tf.Ticks * 50);	
						//}

						lastTime = LoadMessages(GetTimeFrameCandleMessageStorage(secId, tf, msg.AllowBuildFromSmallerTimeFrame), from, to, days, transactionId, SendReply, newOutMessage);
					}
					
					break;

				default:
				{
					if (msg.DataType.IsCandleDataType())
					{
						var storage = (IMarketDataStorage<CandleMessage>)GetStorage(secId, msg.DataType.ToCandleMessage(), msg.Arg);

						var range = GetRange(storage, from, to, DaysLoad);

						if (range != null)
						{
							var messages = storage.Load(range.Item1.Date, range.Item2.Date.EndOfDay());
							lastTime = LoadMessages(messages, range.Item1, transactionId, SendReply, newOutMessage);
						}
					}

					break;
					// throw new ArgumentOutOfRangeException(nameof(msg), msg.DataType, LocalizedStrings.Str721);
				}
			}

			return lastTime;
		}

		private static Tuple<DateTimeOffset, DateTimeOffset> GetRange(IMarketDataStorage storage, DateTimeOffset? from, DateTimeOffset? to, TimeSpan daysLoad)
		{
			var last = storage.Dates.LastOr();

			if (last == null)
				return null;

			if (to == null)
				to = last.Value;

			if (from == null)
				from = to.Value - daysLoad;

			return Tuple.Create(from.Value, to.Value);
		}

		private DateTimeOffset? LoadMessages<TMessage>(IMarketDataStorage<TMessage> storage, DateTimeOffset? from, DateTimeOffset? to, TimeSpan daysLoad, long transactionId, Action sendReply, Action<Message> newOutMessage) 
			where TMessage : Message, ISubscriptionIdMessage, IServerTimeMessage
		{
			var range = GetRange(storage, from, to, daysLoad);

			if (range == null)
				return null;

			var messages = storage.Load(range.Item1.Date, range.Item2.Date.EndOfDay());

			return LoadMessages(messages, range.Item1, transactionId, sendReply, newOutMessage);
		}

		private DateTimeOffset? LoadMessages<TMessage>(IEnumerable<TMessage> messages, DateTimeOffset lastTime, long transactionId, Action sendReply, Action<Message> newOutMessage)
			where TMessage : Message, ISubscriptionIdMessage, IServerTimeMessage
		{
			if (messages == null)
				throw new ArgumentNullException(nameof(messages));

			if (sendReply == null)
				throw new ArgumentNullException(nameof(sendReply));

			var replySent = false;

			foreach (var message in messages)
			{
				if (!replySent)
				{
					sendReply();
					replySent = true;
				}

				message.OriginalTransactionId = transactionId;
				message.SetSubscriptionIds(subscriptionId: transactionId);

				lastTime = message.ServerTime;

				newOutMessage(message);
			}

			return lastTime;
		}

		/// <summary>
		/// <see cref="StartStorageTimer"/>.
		/// </summary>
		public bool DisableStorageTimer { get; set; }

		private Timer _timer;

		/// <summary>
		/// Start storage auto-save thread.
		/// </summary>
		public void StartStorageTimer()
		{
			if (_timer != null || !Buffer.Enabled || DisableStorageTimer)
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
					foreach (var pair in Buffer.GetTicks())
					{
						GetStorage<ExecutionMessage>(pair.Key, ExecutionTypes.Tick).Save(pair.Value);
					}

					foreach (var pair in Buffer.GetOrderLog())
					{
						GetStorage<ExecutionMessage>(pair.Key, ExecutionTypes.OrderLog).Save(pair.Value);
					}

					foreach (var pair in Buffer.GetTransactions())
					{
						var secId = pair.Key;

						if (Mode.Contains(StorageModes.Incremental))
							GetStorage<ExecutionMessage>(secId, ExecutionTypes.Transaction).Save(pair.Value);

						if (Mode.Contains(StorageModes.Snapshot))
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
						if (Mode.Contains(StorageModes.Incremental))
							GetStorage<QuoteChangeMessage>(pair.Key, null).Save(pair.Value);
						
						if (Mode.Contains(StorageModes.Snapshot))
						{
							var snapshotStorage = GetSnapshotStorage(DataType.MarketDepth);

							foreach (var message in pair.Value)
								snapshotStorage.Update(message);
						}
					}

					foreach (var pair in Buffer.GetLevel1())
					{
						var messages = pair.Value.Where(m => m.Changes.Count > 0).ToArray();

						var dt = DateTime.Today;

						var historical = messages.Where(m => m.ServerTime < dt).ToArray();
						var today = messages.Where(m => m.ServerTime >= dt).ToArray();

						GetStorage<Level1ChangeMessage>(pair.Key, null).Save(historical);

						if (Mode.Contains(StorageModes.Incremental))
							GetStorage<Level1ChangeMessage>(pair.Key, null).Save(today);
						
						if (Mode.Contains(StorageModes.Snapshot))
						{
							var snapshotStorage = GetSnapshotStorage(DataType.Level1);

							foreach (var message in today)
								snapshotStorage.Update(message);
						}
					}

					foreach (var pair in Buffer.GetCandles())
					{
						GetStorage(pair.Key.Item1, pair.Key.Item2, pair.Key.Item3).Save(pair.Value);
					}

					foreach (var pair in Buffer.GetPositionChanges())
					{
						var messages = pair.Value.Where(m => m.Changes.Count > 0).ToArray();

						if (Mode.Contains(StorageModes.Incremental))
							GetStorage<PositionChangeMessage>(pair.Key, null).Save(messages);
						
						if (Mode.Contains(StorageModes.Snapshot))
						{
							var snapshotStorage = GetSnapshotStorage(DataType.PositionChanges);

							foreach (var message in messages)
								snapshotStorage.Update(message);
						}
					}

					var news = Buffer.GetNews().ToArray();

					if (news.Length > 0)
					{
						StorageRegistry.GetNewsMessageStorage(Drive, Format).Save(news);
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

		void IPersistable.Load(SettingsStorage storage)
		{
			Buffer.ForceLoad(storage.GetValue<SettingsStorage>(nameof(Buffer)));

			Mode = storage.GetValue(nameof(Mode), Mode);
			Format = storage.GetValue(nameof(Format), Format);
			DaysLoad = storage.GetValue(nameof(DaysLoad), DaysLoad);
			CacheBuildableCandles = storage.GetValue(nameof(CacheBuildableCandles), CacheBuildableCandles);
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Buffer), Buffer.Save());

			storage.SetValue(nameof(Mode), Mode);
			storage.SetValue(nameof(Format), Format);
			storage.SetValue(nameof(DaysLoad), DaysLoad);
			storage.SetValue(nameof(CacheBuildableCandles), CacheBuildableCandles);
		}
	}
}
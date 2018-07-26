#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: StorageMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
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
	/// Storage based message adapter.
	/// </summary>
	public class StorageMessageAdapter : BufferMessageAdapter
	{
		private readonly IStorageRegistry _storageRegistry;
		private readonly IEntityRegistry _entityRegistry;
		private readonly SnapshotRegistry _snapshotRegistry;

		private readonly SynchronizedSet<long> _fullyProcessedSubscriptions = new SynchronizedSet<long>();
		private readonly SynchronizedDictionary<long, long> _orderIds = new SynchronizedDictionary<long, long>();
		private readonly SynchronizedDictionary<string, long> _orderStringIds = new SynchronizedDictionary<string, long>(StringComparer.InvariantCultureIgnoreCase);
		private readonly SynchronizedDictionary<long, long> _cancellationTransactions = new SynchronizedDictionary<long, long>();
		private readonly SynchronizedSet<long> _orderStatusIds = new SynchronizedSet<long>();

		/// <summary>
		/// Initializes a new instance of the <see cref="StorageMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		/// <param name="entityRegistry">The storage of trade objects.</param>
		/// <param name="storageRegistry">The storage of market data.</param>
		/// <param name="snapshotRegistry">Snapshot storage registry.</param>
		public StorageMessageAdapter(IMessageAdapter innerAdapter, IEntityRegistry entityRegistry, IStorageRegistry storageRegistry, SnapshotRegistry snapshotRegistry)
			: base(innerAdapter)
		{
			_entityRegistry = entityRegistry ?? throw new ArgumentNullException(nameof(entityRegistry));
			_storageRegistry = storageRegistry ?? throw new ArgumentNullException(nameof(storageRegistry));
			_snapshotRegistry = snapshotRegistry ?? throw new ArgumentNullException(nameof(snapshotRegistry));

			var isProcessing = false;
			var sync = new SyncObject();

			var unkByOrderId = new Dictionary<long, List<ExecutionMessage>>();
			var unkByOrderStringId = new Dictionary<string, List<ExecutionMessage>>(StringComparer.InvariantCultureIgnoreCase);

			ThreadingHelper.Timer(() =>
			{
				lock (sync)
				{
					if (isProcessing)
						return;

					isProcessing = true;
				}

				try
				{
					foreach (var pair in GetTicks())
					{
						GetStorage<ExecutionMessage>(pair.Key, ExecutionTypes.Tick).Save(pair.Value);
					}

					foreach (var pair in GetOrderLog())
					{
						GetStorage<ExecutionMessage>(pair.Key, ExecutionTypes.OrderLog).Save(pair.Value);
					}

					foreach (var pair in GetTransactions())
					{
						var secId = pair.Key;

						if (Mode.Contains(StorageModes.Incremental))
							GetStorage<ExecutionMessage>(secId, ExecutionTypes.Transaction).Save(pair.Value);

						if (Mode.Contains(StorageModes.Snapshot))
						{
							var snapshotStorage = GetSnapshotStorage(typeof(ExecutionMessage), ExecutionTypes.Transaction);

							foreach (var message in pair.Value)
							{
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
									// do not store cancellation commands into snapshot
									if (message.IsCancelled && message.TransactionId != 0)
									{
										continue;
									}

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

					foreach (var pair in GetOrderBooks())
					{
						if (Mode.Contains(StorageModes.Incremental))
							GetStorage<QuoteChangeMessage>(pair.Key, null).Save(pair.Value);
						
						if (Mode.Contains(StorageModes.Snapshot))
						{
							var snapshotStorage = GetSnapshotStorage(typeof(QuoteChangeMessage), null);

							foreach (var message in pair.Value)
								snapshotStorage.Update(message);
						}
					}

					foreach (var pair in GetLevel1())
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
							var snapshotStorage = GetSnapshotStorage(typeof(Level1ChangeMessage), null);

							foreach (var message in today)
								snapshotStorage.Update(message);
						}
					}

					foreach (var pair in GetCandles())
					{
						GetStorage(pair.Key.Item1, pair.Key.Item2, pair.Key.Item3).Save(pair.Value);
					}

					foreach (var pair in GetPositionChanges())
					{
						var messages = pair.Value.Where(m => m.Changes.Count > 0).ToArray();

						if (Mode.Contains(StorageModes.Incremental))
							GetStorage<PositionChangeMessage>(pair.Key, null).Save(messages);
						
						if (Mode.Contains(StorageModes.Snapshot))
						{
							var snapshotStorage = GetSnapshotStorage(typeof(PositionChangeMessage), null);

							foreach (var message in messages)
								snapshotStorage.Update(message);
						}
					}

					var news = GetNews().ToArray();

					if (news.Length > 0)
					{
						_storageRegistry.GetNewsMessageStorage(Drive, Format).Save(news);
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
		/// The storage (database, file etc.).
		/// </summary>
		public IMarketDataDrive Drive { get; set; }

		private IMarketDataDrive DriveInternal => Drive ?? _storageRegistry.DefaultDrive;

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
		/// Support lookup messages.
		/// </summary>
		public bool SupportLookupMessages { get; set; } = true;

		/// <summary>
		/// Cache buildable from smaller time-frames candles.
		/// </summary>
		public bool CacheBuildableCandles { get; set; }

		/// <summary>
		/// Override previous security data by new values.
		/// </summary>
		public bool OverrideSecurityData { get; set; }

		private StorageModes _mode = StorageModes.Incremental;

		/// <summary>
		/// Storage mode. By default is <see cref="StorageModes.Incremental"/>.
		/// </summary>
		public StorageModes Mode
		{
			get => _mode;
			set
			{
				_mode = value;
				Enabled = value != StorageModes.None;
			}
		}

		/// <inheritdoc />
		public override IEnumerable<TimeSpan> TimeFrames
		{
			get
			{
				if (DriveInternal == null)
					return Enumerable.Empty<TimeSpan>();
				
				return DriveInternal
				       .AvailableSecurities
				       .SelectMany(GetTimeFrames)
				       .Distinct()
				       .OrderBy()
				       .ToArray();
			}
		}

		/// <inheritdoc />
		public override IEnumerable<TimeSpan> GetTimeFrames(SecurityId securityId)
		{
			if (DriveInternal == null)
				return Enumerable.Empty<TimeSpan>();

			return DriveInternal
			       .GetAvailableDataTypes(securityId, Format)
			       .Where(t => t.MessageType == typeof(TimeFrameCandleMessage))
			       .Select(t => (TimeSpan)t.Arg)
			       .Distinct()
			       .OrderBy()
			       .ToArray();
		}

		private ISnapshotStorage GetSnapshotStorage(Type messageType, object arg)
		{
			return _snapshotRegistry.GetSnapshotStorage(messageType, arg);
		}

		private IMarketDataStorage<TMessage> GetStorage<TMessage>(SecurityId securityId, object arg)
			where TMessage : Message
        {
			return (IMarketDataStorage<TMessage>)GetStorage(securityId, typeof(TMessage), arg);
		}

		private IMarketDataStorage GetStorage(SecurityId securityId, Type messageType, object arg)
		{
			return _storageRegistry.GetStorage(GetSecurity(securityId), messageType, arg, Drive, Format);
		}

		private IMarketDataStorage<CandleMessage> GetTimeFrameCandleMessageStorage(SecurityId securityId, TimeSpan timeFrame, bool allowBuildFromSmallerTimeFrame)
		{
			var security = GetSecurity(securityId);

			if (!allowBuildFromSmallerTimeFrame)
				return _storageRegistry.GetCandleMessageStorage(typeof(TimeFrameCandleMessage), security, timeFrame, Drive, Format);

			var storage = _storageRegistry.GetCandleMessageBuildableStorage(security, timeFrame, Drive, Format);

			if (CacheBuildableCandles)
				storage = new CacheableMarketDataStorage<CandleMessage>(storage, _storageRegistry.GetCandleMessageStorage(typeof(TimeFrameCandleMessage), security, timeFrame, Drive, Format));

			return storage;
		}

		private Security GetSecurity(SecurityId securityId)
		{
			var security = _entityRegistry.Securities.ReadBySecurityId(securityId) ?? TryCreateSecurity(securityId);

			if (security == null)
				throw new InvalidOperationException(LocalizedStrings.Str704Params.Put(securityId));

			return security;
		}

		/// <summary>
		/// Load save data from storage.
		/// </summary>
		[Obsolete("Use lookup messages.")]
		public void Load()
		{
			//var requiredSecurities = new List<SecurityId>();
			//var availableSecurities = DriveInternal.AvailableSecurities.ToHashSet();

			foreach (var board in _entityRegistry.ExchangeBoards)
				RaiseStorageMessage(board.ToMessage());

			foreach (var security in _entityRegistry.Securities)
			{
				var msg = security.ToMessage();

				//if (availableSecurities.Remove(msg.SecurityId))
				//{
				//	requiredSecurities.Add(msg.SecurityId);
				//}

				RaiseStorageMessage(msg);
			}

			foreach (var portfolio in _entityRegistry.Portfolios)
			{
				RaiseStorageMessage(portfolio.ToMessage());
				RaiseStorageMessage(portfolio.ToChangeMessage());
			}

			foreach (var position in _entityRegistry.Positions)
			{
				RaiseStorageMessage(position.ToChangeMessage());
			}
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		public override void SendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
					_fullyProcessedSubscriptions.Clear();
					_cancellationTransactions.Clear();
					_orderIds.Clear();
					_orderStringIds.Clear();
					_orderStatusIds.Clear();
					break;

				case MessageTypes.MarketData:
					ProcessMarketDataRequest((MarketDataMessage)message);
					break;

				case MessageTypes.SecurityLookup:
					ProcessSecurityLookup((SecurityLookupMessage)message);
					break;

				case MessageTypes.PortfolioLookup:
					ProcessPortfolioLookup((PortfolioLookupMessage)message);
					break;

				case MessageTypes.OrderStatus:
					ProcessOrderStatus((OrderStatusMessage)message);
					break;

				case MessageTypes.OrderCancel:
					ProcessOrderCancel((OrderCancelMessage)message);
					break;

				default:
					base.SendInMessage(message);
					break;
			}
		}

		private void ProcessSecurityLookup(SecurityLookupMessage msg)
		{
			if (!SupportLookupMessages || msg.IsBack || (msg.Adapter != null && msg.Adapter != this))
			{
				base.SendInMessage(msg);
				return;
			}

			foreach (var board in _entityRegistry.ExchangeBoards)
				RaiseStorageMessage(board.ToMessage());

			foreach (var security in _entityRegistry.Securities)
				RaiseStorageMessage(security.ToMessage());

			base.SendInMessage(msg);
		}

		private void ProcessPortfolioLookup(PortfolioLookupMessage msg)
		{
			if (!SupportLookupMessages || msg.IsBack || (msg.Adapter != null && msg.Adapter != this))
			{
				base.SendInMessage(msg);
				return;
			}

			foreach (var portfolio in _entityRegistry.Portfolios)
			{
				RaiseStorageMessage(portfolio.ToMessage());
				RaiseStorageMessage(portfolio.ToChangeMessage());
			}

			foreach (var position in _entityRegistry.Positions)
			{
				RaiseStorageMessage(position.ToChangeMessage());
			}

			base.SendInMessage(msg);
		}

		private void ProcessOrderStatus(OrderStatusMessage msg)
		{
			_orderStatusIds.Add(msg.TransactionId);

			if (!SupportLookupMessages || msg.IsBack || (msg.Adapter != null && msg.Adapter != this))
			{
				base.SendInMessage(msg);
				return;
			}

			if (msg.OrderId == null && msg.OrderStringId.IsEmpty() && msg.OrderTransactionId == 0 && DaysLoad > TimeSpan.Zero)
			{
				var from = msg.From ?? DateTime.UtcNow.Date - DaysLoad;
				var to = msg.To;

				if (Mode.Contains(StorageModes.Snapshot))
				{
					var storage = (ISnapshotStorage<string, ExecutionMessage>)GetSnapshotStorage(typeof(ExecutionMessage), ExecutionTypes.Transaction);

					foreach (var snapshot in storage.GetAll(from, to))
					{
						if (snapshot.OrderId != null)
							_orderIds.TryAdd(snapshot.OrderId.Value, snapshot.TransactionId);
						else if (!snapshot.OrderStringId.IsEmpty())
							_orderStringIds.TryAdd(snapshot.OrderStringId, snapshot.TransactionId);

						snapshot.OriginalTransactionId = msg.TransactionId;
						RaiseStorageMessage(snapshot);
					}
				}
				else if (Mode.Contains(StorageModes.Incremental))
				{
					if (!msg.SecurityId.IsDefault())
					{
						// TODO restore last actual state from incremental messages

						//GetStorage<ExecutionMessage>(msg.SecurityId, ExecutionTypes.Transaction)
						//	.Load(from, to)
						//	.ForEach(RaiseStorageMessage);
					}
				}
			}

			base.SendInMessage(msg);
		}

		private void ProcessOrderCancel(OrderCancelMessage msg)
		{
			_cancellationTransactions.Add(msg.TransactionId, msg.OrderTransactionId);
			base.SendInMessage(msg);
		}

		private void ProcessMarketDataRequest(MarketDataMessage msg)
		{
			if (msg.IsBack || (msg.From == null && DaysLoad == TimeSpan.Zero))
			{
				base.SendInMessage(msg);
				return;
			}

			if (msg.IsSubscribe)
			{
				var transactionId = msg.TransactionId;

				if (Enabled)
				{
					RaiseStorageMessage(new MarketDataMessage { OriginalTransactionId = transactionId });

					var lastTime = LoadMessages(msg, msg.From, msg.To, transactionId);

					if (msg.IsHistory || (msg.To != null && lastTime != null && msg.To <= lastTime))
					{
						_fullyProcessedSubscriptions.Add(transactionId);
						RaiseStorageMessage(new MarketDataFinishedMessage { OriginalTransactionId = transactionId });

						return;
					}

					Subscribe(msg);

					var clone = (MarketDataMessage)msg.Clone();

					if (lastTime != null)
						clone.From = lastTime;

					base.SendInMessage(clone.ValidateBounds());	
				}
				else
				{
					if (msg.IsHistory)
					{
						RaiseStorageMessage(new MarketDataMessage { OriginalTransactionId = transactionId });
						RaiseStorageMessage(new MarketDataFinishedMessage { OriginalTransactionId = transactionId });
					}
					else
						base.SendInMessage(msg);
				}
			}
			else
			{
				UnSubscribe(msg);

				if (_fullyProcessedSubscriptions.Remove(msg.OriginalTransactionId))
				{
					RaiseNewOutMessage(new MarketDataMessage
					{
						OriginalTransactionId = msg.TransactionId,
					});
				}
				else
					base.SendInMessage(msg);
			}
		}

		private DateTimeOffset? LoadMessages(MarketDataMessage msg, DateTimeOffset? from, DateTimeOffset? to, long transactionId)
		{
			DateTimeOffset? lastTime = null;

			switch (msg.DataType)
			{
				case MarketDataTypes.Level1:
					if (Mode.Contains(StorageModes.Snapshot))
					{
						var level1Msg = (Level1ChangeMessage)GetSnapshotStorage(typeof(Level1ChangeMessage), null).Get(msg.SecurityId);

						if (level1Msg != null)
						{
							lastTime = level1Msg.ServerTime;
							RaiseStorageMessage(level1Msg);
						}
					}
					else if (Mode.Contains(StorageModes.Incremental))
						lastTime = LoadMessages(GetStorage<Level1ChangeMessage>(msg.SecurityId, null), from, to, TimeSpan.Zero, null);
					
					break;

				case MarketDataTypes.MarketDepth:
					if (Mode.Contains(StorageModes.Snapshot))
					{
						var quotesMsg = (QuoteChangeMessage)GetSnapshotStorage(typeof(QuoteChangeMessage), null).Get(msg.SecurityId);

						if (quotesMsg != null)
						{
							lastTime = quotesMsg.ServerTime;
							RaiseStorageMessage(quotesMsg);
						}
					}
					else if (Mode.Contains(StorageModes.Incremental))
						lastTime = LoadMessages(GetStorage<QuoteChangeMessage>(msg.SecurityId, null), from, to, TimeSpan.Zero, null);
					
					break;

				case MarketDataTypes.Trades:
					lastTime = LoadMessages(GetStorage<ExecutionMessage>(msg.SecurityId, ExecutionTypes.Tick), from, to, DaysLoad, m => SetTransactionId(m, transactionId));
					break;

				case MarketDataTypes.OrderLog:
					lastTime = LoadMessages(GetStorage<ExecutionMessage>(msg.SecurityId, ExecutionTypes.OrderLog), from, to, DaysLoad, m => SetTransactionId(m, transactionId));
					break;

				case MarketDataTypes.News:
					lastTime = LoadMessages(_storageRegistry.GetNewsMessageStorage(Drive, Format), from, to, DaysLoad, m => SetTransactionId(m, transactionId));
					break;

				case MarketDataTypes.CandleTimeFrame:
					var tf = (TimeSpan)msg.Arg;

					if (msg.IsCalcVolumeProfile)
					{
						IMarketDataStorage storage;

						switch (msg.BuildFrom)
						{
							case null:
							case MarketDataTypes.Trades:
								storage = GetStorage(msg.SecurityId, typeof(ExecutionMessage), ExecutionTypes.Tick);
								break;

							case MarketDataTypes.OrderLog:
								storage = GetStorage(msg.SecurityId, typeof(ExecutionMessage), ExecutionTypes.OrderLog);
								break;

							case MarketDataTypes.Level1:
								storage = GetStorage(msg.SecurityId, typeof(Level1ChangeMessage), null);
								break;

							case MarketDataTypes.MarketDepth:
								storage = GetStorage(msg.SecurityId, typeof(QuoteChangeMessage), null);
								break;

							default:
								throw new ArgumentOutOfRangeException(nameof(msg), msg.BuildFrom, LocalizedStrings.Str1219);
						}

						var range = GetRange(storage, from, to, TimeSpan.FromDays(2));

						if (range != null)
						{
							var exchangeInfoProvider = _storageRegistry.ExchangeInfoProvider;

							var mdMsg = (MarketDataMessage)msg.Clone();
							mdMsg.From = mdMsg.To = null;

							switch (msg.BuildFrom)
							{
								case null:
								case MarketDataTypes.Trades:
									lastTime = LoadMessages(((IMarketDataStorage<ExecutionMessage>)storage)
										.Load(range.Item1.Date, range.Item2.Date.EndOfDay())
										.ToCandles(mdMsg, exchangeInfoProvider: exchangeInfoProvider), range.Item1, m => SetTransactionId(m, transactionId));

									break;

								case MarketDataTypes.OrderLog:
								{
									switch (msg.BuildField)
									{
										case null:
										case Level1Fields.LastTradePrice:
											lastTime = LoadMessages(((IMarketDataStorage<ExecutionMessage>)storage)
											    .Load(range.Item1.Date, range.Item2.Date.EndOfDay())
											    .ToCandles(mdMsg, exchangeInfoProvider: exchangeInfoProvider), range.Item1, m => SetTransactionId(m, transactionId));

											break;
											
										// TODO
										//case Level1Fields.SpreadMiddle:
										//	lastTime = LoadMessages(((IMarketDataStorage<ExecutionMessage>)storage)
										//	    .Load(range.Item1.Date, range.Item2.Date.EndOfDay())
										//		.ToMarketDepths(OrderLogBuilders.Plaza2.CreateBuilder(security.ToSecurityId()))
										//	    .ToCandles(mdMsg, false, exchangeInfoProvider: exchangeInfoProvider), range.Item1, m => SetTransactionId(m, transactionId));
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
												.ToCandles(mdMsg, exchangeInfoProvider: exchangeInfoProvider), range.Item1, m => SetTransactionId(m, transactionId));
											break;

										case Level1Fields.BestBidPrice:
										case Level1Fields.BestAskPrice:
										case Level1Fields.SpreadMiddle:
											lastTime = LoadMessages(((IMarketDataStorage<Level1ChangeMessage>)storage)
											    .Load(range.Item1.Date, range.Item2.Date.EndOfDay())
											    .ToOrderBooks()
											    .ToCandles(mdMsg, msg.BuildField.Value, exchangeInfoProvider: exchangeInfoProvider), range.Item1, m => SetTransactionId(m, transactionId));
											break;
									}
									
									break;

								case MarketDataTypes.MarketDepth:
									lastTime = LoadMessages(((IMarketDataStorage<QuoteChangeMessage>)storage)
										.Load(range.Item1.Date, range.Item2.Date.EndOfDay())
										.ToCandles(mdMsg, msg.BuildField ?? Level1Fields.SpreadMiddle, exchangeInfoProvider: exchangeInfoProvider), range.Item1, m => SetTransactionId(m, transactionId));
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

						lastTime = LoadMessages(GetTimeFrameCandleMessageStorage(msg.SecurityId, tf, msg.AllowBuildFromSmallerTimeFrame), from, to, days, m => SetTransactionId(m, transactionId));
					}
					
					break;

				case MarketDataTypes.CandlePnF:
					lastTime = LoadMessages(GetStorage<PnFCandleMessage>(msg.SecurityId, msg.Arg), from, to, DaysLoad, m => SetTransactionId(m, transactionId));
					break;

				case MarketDataTypes.CandleRange:
					lastTime = LoadMessages(GetStorage<RangeCandleMessage>(msg.SecurityId, msg.Arg), from, to, DaysLoad, m => SetTransactionId(m, transactionId));
					break;

				case MarketDataTypes.CandleRenko:
					lastTime = LoadMessages(GetStorage<RenkoCandleMessage>(msg.SecurityId, msg.Arg), from, to, DaysLoad, m => SetTransactionId(m, transactionId));
					break;

				case MarketDataTypes.CandleTick:
					lastTime = LoadMessages(GetStorage<TickCandleMessage>(msg.SecurityId, msg.Arg), from, to, DaysLoad, m => SetTransactionId(m, transactionId));
					break;

				case MarketDataTypes.CandleVolume:
					lastTime = LoadMessages(GetStorage<VolumeCandleMessage>(msg.SecurityId, msg.Arg), from, to, DaysLoad, m => SetTransactionId(m, transactionId));
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(msg), msg.DataType, LocalizedStrings.Str721);
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

		private DateTimeOffset? LoadMessages<TMessage>(IMarketDataStorage<TMessage> storage, DateTimeOffset? from, DateTimeOffset? to, TimeSpan daysLoad, Func<TMessage, DateTimeOffset> func) 
			where TMessage : Message
		{
			var range = GetRange(storage, from, to, daysLoad);

			if (range == null)
				return null;

			var messages = storage.Load(range.Item1.Date, range.Item2.Date.EndOfDay());

			return LoadMessages(messages, range.Item1, func);
		}

		private DateTimeOffset? LoadMessages<TMessage>(IEnumerable<TMessage> messages, DateTimeOffset lastTime, Func<TMessage, DateTimeOffset> func)
			where TMessage : Message
		{
			foreach (var message in messages)
			{
				if (func != null)
					lastTime = func.Invoke(message);

				RaiseStorageMessage(message);
			}

			return lastTime;
		}

		/// <summary>
		/// Process <see cref="MessageAdapterWrapper.InnerAdapter"/> output message.
		/// </summary>
		/// <param name="message">The message.</param>
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Security:
				{
					var secMsg = (SecurityMessage)message;
					var security = _entityRegistry.Securities.ReadBySecurityId(secMsg.SecurityId);

					if (security == null)
						security = secMsg.ToSecurity(_storageRegistry.ExchangeInfoProvider);
					else
						security.ApplyChanges(secMsg, _storageRegistry.ExchangeInfoProvider, OverrideSecurityData);

					_entityRegistry.Securities.Save(security, OverrideSecurityData);
					break;
				}
				case MessageTypes.Board:
				{
					var boardMsg = (BoardMessage)message;
					var board = _entityRegistry.ExchangeBoards.ReadById(boardMsg.Code);

					if (board == null)
					{
						board = _storageRegistry.ExchangeInfoProvider.GetOrCreateBoard(boardMsg.Code, code =>
						{
							var exchange = _storageRegistry
								.ExchangeInfoProvider
								.GetExchange(boardMsg.ExchangeCode) ?? boardMsg.ToExchange(new Exchange
								{
									Name = boardMsg.ExchangeCode
								});

							return boardMsg.ToBoard(new ExchangeBoard
							{
								Code = code,
								Exchange = exchange
							});
						});
					}
					else
					{
						// TODO apply changes
					}

					_entityRegistry.Exchanges.Save(board.Exchange);
					_entityRegistry.ExchangeBoards.Save(board);
					break;
				}

				case MessageTypes.Portfolio:
				{
					var portfolioMsg = (PortfolioMessage)message;
					var portfolio = _entityRegistry.Portfolios.ReadById(portfolioMsg.PortfolioName) ?? new Portfolio
					{
						Name = portfolioMsg.PortfolioName
					};

					portfolioMsg.ToPortfolio(portfolio, _storageRegistry.ExchangeInfoProvider);
					_entityRegistry.Portfolios.Save(portfolio);

					break;
				}

				case MessageTypes.PortfolioChange:
				{
					var portfolioMsg = (PortfolioChangeMessage)message;
					var portfolio = _entityRegistry.Portfolios.ReadById(portfolioMsg.PortfolioName) ?? new Portfolio
					{
						Name = portfolioMsg.PortfolioName
					};

					portfolio.ApplyChanges(portfolioMsg, _storageRegistry.ExchangeInfoProvider);
					_entityRegistry.Portfolios.Save(portfolio);

					break;
				}

				//case MessageTypes.Position:
				//{
				//	var positionMsg = (PositionMessage)message;
				//	var position = GetPosition(positionMsg.SecurityId, positionMsg.PortfolioName);

				//	if (position == null)
				//		break;

				//	if (!positionMsg.DepoName.IsEmpty())
				//		position.DepoName = positionMsg.DepoName;

				//	if (positionMsg.LimitType != null)
				//		position.LimitType = positionMsg.LimitType;

				//	if (!positionMsg.Description.IsEmpty())
				//		position.Description = positionMsg.Description;

				//	_entityRegistry.Positions.Save(position);

				//	break;
				//}

				case MessageTypes.PositionChange:
				{
					var positionMsg = (PositionChangeMessage)message;
					var position = GetPosition(positionMsg.SecurityId, positionMsg.PortfolioName);

					if (position == null)
						break;

					if (!positionMsg.DepoName.IsEmpty())
						position.DepoName = positionMsg.DepoName;

					if (positionMsg.LimitType != null)
						position.LimitType = positionMsg.LimitType;

					if (!positionMsg.Description.IsEmpty())
						position.Description = positionMsg.Description;

					position.ApplyChanges(positionMsg);
					_entityRegistry.Positions.Save(position);

					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		private Position GetPosition(SecurityId securityId, string portfolioName)
		{
			var security = !securityId.SecurityCode.IsEmpty() && !securityId.BoardCode.IsEmpty() ? _entityRegistry.Securities.ReadBySecurityId(securityId) : _entityRegistry.Securities.Lookup(new Security
			{
				Code = securityId.SecurityCode,
				Type = securityId.SecurityType
			}).FirstOrDefault();

			if (security == null)
				security = TryCreateSecurity(securityId);

			if (security == null)
				return null;

			var portfolio = _entityRegistry.Portfolios.ReadById(portfolioName);

			if (portfolio == null)
			{
				portfolio = new Portfolio
				{
					Name = portfolioName
				};

				_entityRegistry.Portfolios.Add(portfolio);
			}

			return _entityRegistry.Positions.ReadBySecurityAndPortfolio(security, portfolio) ?? new Position
			{
				Security = security,
				Portfolio = portfolio
			};
		}

		private Security TryCreateSecurity(SecurityId securityId)
		{
			if (securityId.SecurityCode.IsEmpty() || securityId.BoardCode.IsEmpty())
				return null;

			var security = new Security
			{
				Id = securityId.ToStringId(),
				Code = securityId.SecurityCode,
				Board = _storageRegistry.ExchangeInfoProvider.GetOrCreateBoard(securityId.BoardCode),
				//ExtensionInfo = new Dictionary<object, object>()
			};

			_entityRegistry.Securities.Add(security);

			return security;
		}

		private void RaiseStorageMessage(Message message)
		{
			message.TryInitLocalTime(this);

			RaiseNewOutMessage(message);
		}

		private static DateTimeOffset SetTransactionId(CandleMessage msg, long transactionId)
		{
			msg.OriginalTransactionId = transactionId;

			var lastTime = msg.CloseTime;

			if (lastTime.IsDefault())
			{
				lastTime = msg.OpenTime;

				if (msg is TimeFrameCandleMessage tfMsg)
				{
					if (tfMsg.TimeFrame.IsDefault())
						throw new InvalidOperationException("tf == 0");

					lastTime += tfMsg.TimeFrame;
				}
			}

			return lastTime;
		}

		private static DateTimeOffset SetTransactionId(NewsMessage msg, long transactionId)
		{
			msg.OriginalTransactionId = transactionId;
			return msg.ServerTime;
		}

		private static DateTimeOffset SetTransactionId(ExecutionMessage msg, long transactionId)
		{
			msg.OriginalTransactionId = transactionId;
			return msg.ServerTime;
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			if (Drive != null)
				storage.SetValue(nameof(Drive), Drive.SaveEntire(false));

			storage.SetValue(nameof(Mode), Mode);
			storage.SetValue(nameof(Format), Format);
			storage.SetValue(nameof(DaysLoad), DaysLoad);
			storage.SetValue(nameof(CacheBuildableCandles), CacheBuildableCandles);
			storage.SetValue(nameof(OverrideSecurityData), OverrideSecurityData);
			storage.SetValue(nameof(SupportLookupMessages), SupportLookupMessages);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			if (storage.ContainsKey(nameof(Drive)))
				Drive = storage.GetValue<SettingsStorage>(nameof(Drive)).LoadEntire<IMarketDataDrive>();

			Mode = storage.GetValue(nameof(Mode), Mode);
			Format = storage.GetValue(nameof(Format), Format);
			DaysLoad = storage.GetValue(nameof(DaysLoad), DaysLoad);
			CacheBuildableCandles = storage.GetValue(nameof(CacheBuildableCandles), CacheBuildableCandles);
			OverrideSecurityData = storage.GetValue(nameof(OverrideSecurityData), OverrideSecurityData);
			SupportLookupMessages = storage.GetValue(nameof(SupportLookupMessages), SupportLookupMessages);
		}

		/// <summary>
		/// Create a copy of <see cref="StorageMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new StorageMessageAdapter(InnerAdapter, _entityRegistry, _storageRegistry, _snapshotRegistry)
			{
				CacheBuildableCandles = CacheBuildableCandles,
				OverrideSecurityData = OverrideSecurityData,
				DaysLoad = DaysLoad,
				Format = Format,
				Drive = Drive,
				Mode = Mode,
				SupportLookupMessages = SupportLookupMessages,
			};
		}
	}
}
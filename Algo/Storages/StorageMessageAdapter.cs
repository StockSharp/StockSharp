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

	using MoreLinq;

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

		/// <summary>
		/// Initializes a new instance of the <see cref="StorageMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		/// <param name="entityRegistry">The storage of trade objects.</param>
		/// <param name="storageRegistry">The storage of market data.</param>
		public StorageMessageAdapter(IMessageAdapter innerAdapter, IEntityRegistry entityRegistry, IStorageRegistry storageRegistry)
			: base(innerAdapter)
		{
			if (entityRegistry == null)
				throw new ArgumentNullException(nameof(entityRegistry));

			if (storageRegistry == null)
				throw new ArgumentNullException(nameof(storageRegistry));

			_entityRegistry = entityRegistry;
			_storageRegistry = storageRegistry;

			Drive = _storageRegistry.DefaultDrive;

			var isProcessing = false;
			var sync = new SyncObject();

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
						if (Mode.Contains(StorageModes.Incremental))
							GetStorage<ExecutionMessage>(pair.Key, ExecutionTypes.Transaction).Save(pair.Value);
						
						if (Mode.Contains(StorageModes.Snapshot))
						{
							var snapshotStorage = GetSnapshotStorage(typeof(ExecutionMessage), ExecutionTypes.Transaction);

							foreach (var message in pair.Value)
								snapshotStorage.Update(message);
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
						if (Mode.Contains(StorageModes.Incremental))
							GetStorage<Level1ChangeMessage>(pair.Key, null).Save(pair.Value);
						
						if (Mode.Contains(StorageModes.Snapshot))
						{
							var snapshotStorage = GetSnapshotStorage(typeof(Level1ChangeMessage), null);

							foreach (var message in pair.Value)
								snapshotStorage.Update(message);
						}
					}

					foreach (var pair in GetCandles())
					{
						GetStorage(pair.Key.Item1, pair.Key.Item2, pair.Key.Item3).Save(pair.Value);
					}

					foreach (var pair in GetPositionChanges())
					{
						if (Mode.Contains(StorageModes.Incremental))
							GetStorage<PositionChangeMessage>(pair.Key, null).Save(pair.Value);
						
						if (Mode.Contains(StorageModes.Snapshot))
						{
							var snapshotStorage = GetSnapshotStorage(typeof(PositionChangeMessage), null);

							foreach (var message in pair.Value)
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

		private IMarketDataDrive _drive;

		/// <summary>
		/// The storage (database, file etc.).
		/// </summary>
		public IMarketDataDrive Drive
		{
			get => _drive;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_drive = value;
			}
		}

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
		/// Use timeframe candles instead of trades.
		/// </summary>
		public bool UseCandlesInsteadTrades { get; set; }

		/// <summary>
		/// Use candles with specified timeframe.
		/// </summary>
		public TimeSpan CandlesTimeFrame { get; set; }

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

		private ISnapshotStorage GetSnapshotStorage(Type messageType, object arg)
		{
			return _storageRegistry.GetSnapshotStorage(messageType, arg, Drive, Format);
		}

		private IMarketDataStorage<TMessage> GetStorage<TMessage>(SecurityId securityId, object arg)
			where TMessage : Message
        {
			return (IMarketDataStorage<TMessage>)GetStorage(securityId, typeof(TMessage), arg);
		}

		private IMarketDataStorage GetStorage(SecurityId securityId, Type messageType, object arg)
		{
			var security = _entityRegistry.Securities.ReadBySecurityId(securityId) ?? TryCreateSecurity(securityId);

			if (security == null)
				throw new InvalidOperationException(LocalizedStrings.Str704Params.Put(securityId));

			return _storageRegistry.GetStorage(security, messageType, arg, Drive, Format);
		}

		/// <summary>
		/// Load save data from storage.
		/// </summary>
		public void Load()
		{
			var requiredSecurities = new List<SecurityId>();
			var availableSecurities = Drive.AvailableSecurities.ToHashSet();

			foreach (var board in _entityRegistry.ExchangeBoards)
				RaiseStorageMessage(board.ToMessage());

			foreach (var security in _entityRegistry.Securities)
			{
				var msg = security.ToMessage();

				if (availableSecurities.Remove(msg.SecurityId))
				{
                    requiredSecurities.Add(msg.SecurityId);
				}

				RaiseStorageMessage(msg);
			}

			foreach (var portfolio in _entityRegistry.Portfolios)
			{
				RaiseStorageMessage(portfolio.ToMessage());
				RaiseStorageMessage(portfolio.ToChangeMessage());
			}

			foreach (var position in _entityRegistry.Positions)
			{
				//RaiseStorageMessage(position.ToMessage());
				RaiseStorageMessage(position.ToChangeMessage());
			}

			if (DaysLoad == TimeSpan.Zero)
				return;

			var today = DateTime.UtcNow.Date;

			var from = (DateTimeOffset)(today - DaysLoad);
			var to = DateTimeOffset.Now;

			//if (Mode.Contains(StorageModes.Snapshot))
			//{
			//	var storage = GetSnapshotStorage(typeof(ExecutionMessage), ExecutionTypes.Transaction);

			//	foreach (var secId in requiredSecurities)
			//	{
			//		var snapshot = storage.Get(secId);

			//		if (snapshot != null)
			//			RaiseStorageMessage(snapshot);
			//	}
			//}
			//else if (Mode.Contains(StorageModes.Incremental))
			//{
				// TODO

				foreach (var secId in requiredSecurities)
				{
					GetStorage<ExecutionMessage>(secId, ExecutionTypes.Transaction)
						.Load(from, to)
						.ForEach(RaiseStorageMessage);
				}	
			//}
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		public override void SendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.MarketData:
					ProcessMarketDataMessage((MarketDataMessage)message);
					break;

				default:
					base.SendInMessage(message);
					break;
			}
		}

		private void ProcessMarketDataMessage(MarketDataMessage msg)
		{
			if (msg.IsBack || (msg.From == null && DaysLoad == TimeSpan.Zero))
			{
				base.SendInMessage(msg);
				return;
			}

			if (msg.IsSubscribe)
			{
				if (Enabled)
				{
					var transactionId = msg.TransactionId;

					RaiseStorageMessage(new MarketDataMessage { OriginalTransactionId = transactionId, IsHistory = true });

					var lastTime = LoadMessages(msg, msg.From, msg.To, transactionId);

					RaiseStorageMessage(new MarketDataFinishedMessage { OriginalTransactionId = transactionId, IsHistory = true });

					if (msg.IsHistory)
						return;

					Subscribe(msg.SecurityId, CreateDataType(msg));

					var clone = (MarketDataMessage)msg.Clone();
					clone.From = lastTime;

					base.SendInMessage(clone.ValidateBounds());	
				}
				else
					base.SendInMessage(msg);
			}
			else
			{
				if (msg.IsHistory)
					return;

				UnSubscribe(msg.SecurityId, CreateDataType(msg));
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
						lastTime = LoadMessages(GetStorage<Level1ChangeMessage>(msg.SecurityId, null), from, to, null);
					
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
						lastTime = LoadMessages(GetStorage<QuoteChangeMessage>(msg.SecurityId, null), from, to, null);
					
					break;

				case MarketDataTypes.Trades:
					lastTime = !UseCandlesInsteadTrades 
						? LoadMessages(GetStorage<ExecutionMessage>(msg.SecurityId, ExecutionTypes.Tick), from, to, m => SetTransactionId(m, transactionId)) 
						: LoadTickMessages(msg.SecurityId, from, to, transactionId);
					break;

				case MarketDataTypes.OrderLog:
					lastTime = LoadMessages(GetStorage<ExecutionMessage>(msg.SecurityId, ExecutionTypes.OrderLog), from, to, m => SetTransactionId(m, transactionId));
					break;

				case MarketDataTypes.News:
					lastTime = LoadMessages(_storageRegistry.GetNewsMessageStorage(Drive, Format), from, to, m => SetTransactionId(m, transactionId));
					break;

				case MarketDataTypes.CandleTimeFrame:
					lastTime = LoadMessages(GetStorage<TimeFrameCandleMessage>(msg.SecurityId, msg.Arg), from, to, m => SetTransactionId(m, transactionId));
					break;

				case MarketDataTypes.CandlePnF:
					lastTime = LoadMessages(GetStorage<PnFCandleMessage>(msg.SecurityId, msg.Arg), from, to, m => SetTransactionId(m, transactionId));
					break;

				case MarketDataTypes.CandleRange:
					lastTime = LoadMessages(GetStorage<RangeCandleMessage>(msg.SecurityId, msg.Arg), from, to, m => SetTransactionId(m, transactionId));
					break;

				case MarketDataTypes.CandleRenko:
					lastTime = LoadMessages(GetStorage<RenkoCandleMessage>(msg.SecurityId, msg.Arg), from, to, m => SetTransactionId(m, transactionId));
					break;

				case MarketDataTypes.CandleTick:
					lastTime = LoadMessages(GetStorage<TickCandleMessage>(msg.SecurityId, msg.Arg), from, to, m => SetTransactionId(m, transactionId));
					break;

				case MarketDataTypes.CandleVolume:
					lastTime = LoadMessages(GetStorage<VolumeCandleMessage>(msg.SecurityId, msg.Arg), from, to, m => SetTransactionId(m, transactionId));
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(msg), msg.DataType, LocalizedStrings.Str721);
			}

			return lastTime;
		}

		private DateTimeOffset? LoadMessages<TMessage>(IMarketDataStorage<TMessage> storage, DateTimeOffset? from, DateTimeOffset? to, Func<TMessage, DateTimeOffset> func) 
			where TMessage : Message
		{
			var last = storage.Dates.LastOr();

			if (last == null)
				return null;

			if (from == null)
			{
				var days = DaysLoad;

				if (typeof(TMessage) == typeof(TimeFrameCandleMessage))
				{
					var tf = (TimeSpan)storage.Arg;

					if (tf.Ticks > 1)
					{
						if (tf.TotalMinutes < 15)
							days = TimeSpan.FromTicks(tf.Ticks * 10000);
						else if (tf.TotalHours < 2)
							days = TimeSpan.FromTicks(tf.Ticks * 1000);
						else if (tf.TotalDays < 2)
							days = TimeSpan.FromTicks(tf.Ticks * 100);
						else
							days = TimeSpan.FromTicks(tf.Ticks * 50);	
					}
				}
				else if (typeof(TMessage) == typeof(QuoteChangeMessage) || typeof(TMessage) == typeof(Level1ChangeMessage))
					days = TimeSpan.Zero;

				from = (to ?? last.Value) - days;
			}

			if (to == null)
				to = last.Value;

			var messages = storage.Load(from.Value.Date, to.Value.Date.EndOfDay());
			var lastTime = from.Value;

			foreach (var message in messages)
			{
				if (func != null)
					lastTime = func.Invoke(message);

				RaiseStorageMessage(message);
			}

			return lastTime;
		}

		private DateTimeOffset? LoadTickMessages(SecurityId securityId, DateTimeOffset? from, DateTimeOffset? to, long transactionId)
		{
			var tickStorage = GetStorage<ExecutionMessage>(securityId, ExecutionTypes.Tick);

			var last = tickStorage.Dates.LastOr();

			if (last == null)
				return null;

			if (from == null)
				from = (to ?? last.Value) - DaysLoad;

			if (to == null)
				to = last.Value;

			var tickDates = tickStorage.GetDates(from.Value.Date, to.Value.Date.EndOfDay()).ToArray();

			var candleStorage = GetStorage<TimeFrameCandleMessage>(securityId, CandlesTimeFrame);

			var ticksLastDate = tickStorage.GetToDate() ?? from.Value.Date;
			var candlesLastDate = candleStorage.GetToDate() ?? from.Value.Date;

			var toDate = ticksLastDate.Max(candlesLastDate).Min(to.Value.Date);
			var lastTime = from;

			for (var date = from.Value.Date; date <= toDate; date = date.AddDays(1))
			{
				var messages = tickDates.Contains(date)
					? tickStorage.Load(date) 
					: candleStorage.Load(date).ToTrades(0.001m);

				foreach (var msg in messages)
				{
					lastTime = msg.ServerTime;

					msg.OriginalTransactionId = transactionId;
					RaiseStorageMessage(msg);
				}
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
						security.ApplyChanges(secMsg, _storageRegistry.ExchangeInfoProvider);

					_entityRegistry.Securities.Save(security);
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

		private static DataType CreateDataType(MarketDataMessage msg)
		{
			switch (msg.DataType)
			{
				case MarketDataTypes.Level1:
					return DataType.Level1;

				case MarketDataTypes.MarketDepth:
					return DataType.MarketDepth;

				case MarketDataTypes.Trades:
					return DataType.Ticks;

				case MarketDataTypes.OrderLog:
					return DataType.OrderLog;

				case MarketDataTypes.News:
					return DataType.News;

				case MarketDataTypes.CandleTimeFrame:
					return DataType.TimeFrame((TimeSpan)msg.Arg);

				case MarketDataTypes.CandleTick:
					return DataType.Create(typeof(TickCandleMessage), msg.Arg);

				case MarketDataTypes.CandleVolume:
					return DataType.Create(typeof(VolumeCandleMessage), msg.Arg);

				case MarketDataTypes.CandleRange:
					return DataType.Create(typeof(RangeCandleMessage), msg.Arg);

				case MarketDataTypes.CandlePnF:
					return DataType.Create(typeof(PnFCandleMessage), msg.Arg);

				case MarketDataTypes.CandleRenko:
					return DataType.Create(typeof(RenkoCandleMessage), msg.Arg);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private static DateTimeOffset SetTransactionId(CandleMessage msg, long transactionId)
		{
			msg.OriginalTransactionId = transactionId;
			return msg.OpenTime;
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

			storage.SetValue(nameof(Drive), Drive.SaveEntire(false));
			storage.SetValue(nameof(Format), Format);
			storage.SetValue(nameof(UseCandlesInsteadTrades), UseCandlesInsteadTrades);
			storage.SetValue(nameof(CandlesTimeFrame), CandlesTimeFrame);
			storage.SetValue(nameof(DaysLoad), DaysLoad);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			if (storage.ContainsKey(nameof(Drive)))
				Drive = storage.GetValue<SettingsStorage>(nameof(Drive)).LoadEntire<IMarketDataDrive>();

			Format = storage.GetValue(nameof(Format), Format);
			UseCandlesInsteadTrades = storage.GetValue(nameof(UseCandlesInsteadTrades), UseCandlesInsteadTrades);
			CandlesTimeFrame = storage.GetValue(nameof(CandlesTimeFrame), CandlesTimeFrame);
			DaysLoad = storage.GetValue(nameof(DaysLoad), DaysLoad);
		}

		/// <summary>
		/// Create a copy of <see cref="StorageMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new StorageMessageAdapter(InnerAdapter, _entityRegistry, _storageRegistry);
		}
	}
}
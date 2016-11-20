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

	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

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
						GetStorage<ExecutionMessage>(pair.Key, ExecutionTypes.Transaction).Save(pair.Value);
					}

					foreach (var pair in GetOrderBooks())
					{
						GetStorage(pair.Key, typeof(QuoteChangeMessage), null).Save(pair.Value);
					}

					foreach (var pair in GetLevel1())
					{
						GetStorage(pair.Key, typeof(Level1ChangeMessage), null).Save(pair.Value);
					}

					foreach (var pair in GetCandles())
					{
						GetStorage(pair.Key.Item1, pair.Key.Item2, pair.Key.Item3).Save(pair.Value);
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
			get { return _drive; }
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
		/// Total days to load stored data.
		/// </summary>
		public TimeSpan DaysLoad
		{
			get { return _daysLoad; }
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value));

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
				RaiseStorageMessage(position.ToMessage());
				RaiseStorageMessage(position.ToChangeMessage());
			}

			if (DaysLoad == TimeSpan.Zero)
				return;

			var today = DateTime.UtcNow.Date;

			var from = (DateTimeOffset)(today - DaysLoad);
			var to = DateTimeOffset.Now;

			foreach (var secId in requiredSecurities)
			{
				GetStorage<ExecutionMessage>(secId, ExecutionTypes.Transaction)
					.Load(from, to)
					.ForEach(RaiseStorageMessage);
			}
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		public override void SendInMessage(Message message)
		{
			if (message.Type == MessageTypes.MarketData)
				ProcessMarketDataMessage((MarketDataMessage)message);

			base.SendInMessage(message);
		}

		private void ProcessMarketDataMessage(MarketDataMessage msg)
		{
			if (!msg.IsSubscribe || DaysLoad == TimeSpan.Zero)
				return;

			var today = DateTime.UtcNow.Date;

			var from = (DateTimeOffset)(today - DaysLoad);
			var to = DateTimeOffset.Now;

			switch (msg.DataType)
			{
				case MarketDataTypes.Level1:
					LoadMessages(GetStorage<Level1ChangeMessage>(msg.SecurityId, null), from, to);
					break;

				case MarketDataTypes.MarketDepth:
					LoadMessages(GetStorage<QuoteChangeMessage>(msg.SecurityId, null), from, to);
					break;

				case MarketDataTypes.Trades:
					if (!UseCandlesInsteadTrades)
						LoadMessages(GetStorage<ExecutionMessage>(msg.SecurityId, ExecutionTypes.Tick), from, to);
					else
						LoadMessages(msg.SecurityId, ExecutionTypes.Tick, from, to);
					break;

				case MarketDataTypes.OrderLog:
					LoadMessages(GetStorage<ExecutionMessage>(msg.SecurityId, ExecutionTypes.OrderLog), from, to);
					break;

				case MarketDataTypes.News:
					LoadMessages(_storageRegistry.GetNewsMessageStorage(Drive, Format), from, to);
					break;

				case MarketDataTypes.CandleTimeFrame:
					LoadMessages(GetStorage<TimeFrameCandleMessage>(msg.SecurityId, msg.Arg), from, to);
					break;

				case MarketDataTypes.CandlePnF:
					LoadMessages(GetStorage<PnFCandleMessage>(msg.SecurityId, msg.Arg), from, to);
					break;

				case MarketDataTypes.CandleRange:
					LoadMessages(GetStorage<RangeCandleMessage>(msg.SecurityId, msg.Arg), from, to);
					break;

				case MarketDataTypes.CandleRenko:
					LoadMessages(GetStorage<RenkoCandleMessage>(msg.SecurityId, msg.Arg), from, to);
					break;

				case MarketDataTypes.CandleTick:
					LoadMessages(GetStorage<TickCandleMessage>(msg.SecurityId, msg.Arg), from, to);
					break;

				case MarketDataTypes.CandleVolume:
					LoadMessages(GetStorage<VolumeCandleMessage>(msg.SecurityId, msg.Arg), from, to);
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(msg), msg.DataType, LocalizedStrings.Str721);
			}
		}

		private void LoadMessages<TMessage>(IMarketDataStorage<TMessage> storage, DateTimeOffset from, DateTimeOffset to)
			where TMessage : Message
		{
			storage
				.Load(from, to)
				.ForEach(RaiseStorageMessage);
		}

		private void LoadMessages(SecurityId securityId, object arg, DateTimeOffset from, DateTimeOffset to)
		{
			var tickStorage = GetStorage<ExecutionMessage>(securityId, arg);
			var tickDates = tickStorage.GetDates(from.DateTime, to.DateTime).ToArray();

			var candleStorage = GetStorage<TimeFrameCandleMessage>(securityId, CandlesTimeFrame);

			for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
			{
				if (tickDates.Contains(date))
				{
					tickStorage.Load(date).ForEach(RaiseStorageMessage);
				}
				else
				{
					candleStorage.Load(date).ToTrades(0.001m).ForEach(RaiseStorageMessage);
				}
			}

			RaiseStorageMessage(new HistoryInitializedMessage());
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
						security = secMsg.ToSecurity();
					else
						security.ApplyChanges(secMsg);

					_entityRegistry.Securities.Save(security);
					break;
				}
				case MessageTypes.Board:
				{
					var boardMsg = (BoardMessage)message;
					var board = _entityRegistry.ExchangeBoards.ReadById(boardMsg.Code);

					if (board == null)
					{
						board = ExchangeBoard.GetOrCreateBoard(boardMsg.Code, code =>
						{
							var exchange = boardMsg.ToExchange(new Exchange { Name = boardMsg.ExchangeCode });
							return boardMsg.ToBoard(new ExchangeBoard { Code = code, Exchange = exchange });
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
					var portfolio = _entityRegistry.Portfolios.ReadById(portfolioMsg.PortfolioName)
							?? new Portfolio { Name = portfolioMsg.PortfolioName };

					portfolioMsg.ToPortfolio(portfolio);
					_entityRegistry.Portfolios.Save(portfolio);

					break;
				}

				case MessageTypes.PortfolioChange:
				{
					var portfolioMsg = (PortfolioChangeMessage)message;
					var portfolio = _entityRegistry.Portfolios.ReadById(portfolioMsg.PortfolioName)
							?? new Portfolio { Name = portfolioMsg.PortfolioName };

					portfolio.ApplyChanges(portfolioMsg);
					_entityRegistry.Portfolios.Save(portfolio);

					break;
				}

				case MessageTypes.Position:
				{
					var positionMsg = (PositionMessage)message;
					var position = GetPosition(positionMsg.SecurityId, positionMsg.PortfolioName);

					if (position == null)
						break;

					if (!positionMsg.DepoName.IsEmpty())
						position.DepoName = positionMsg.DepoName;

					if (positionMsg.LimitType != null)
						position.LimitType = positionMsg.LimitType;

					if (!positionMsg.Description.IsEmpty())
						position.Description = positionMsg.Description;

					_entityRegistry.Positions.Save(position);

					break;
				}

				case MessageTypes.PositionChange:
				{
					var positionMsg = (PositionChangeMessage)message;
					var position = GetPosition(positionMsg.SecurityId, positionMsg.PortfolioName);

					if (position == null)
						break;

					position.ApplyChanges(positionMsg);
					_entityRegistry.Positions.Save(position);

					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		private Position GetPosition(SecurityId securityId, string portfolioName)
		{
			var security = !securityId.SecurityCode.IsEmpty() && !securityId.BoardCode.IsEmpty()
				? _entityRegistry.Securities.ReadBySecurityId(securityId)
				: _entityRegistry.Securities.Lookup(new Security { Code = securityId.SecurityCode, Type = securityId.SecurityType }).FirstOrDefault();

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

			return _entityRegistry.Positions.ReadBySecurityAndPortfolio(security, portfolio)
				?? new Position { Security = security, Portfolio = portfolio };
		}

		private Security TryCreateSecurity(SecurityId securityId)
		{
			if (securityId.SecurityCode.IsEmpty() || securityId.BoardCode.IsEmpty())
				return null;

			var security = new Security
			{
				Id = securityId.ToStringId(),
				Code = securityId.SecurityCode,
				Board = ExchangeBoard.GetOrCreateBoard(securityId.BoardCode),
				//ExtensionInfo = new Dictionary<object, object>()
			};

			_entityRegistry.Securities.Add(security);

			return security;
		}

		private void RaiseStorageMessage(Message message)
		{
			if (message.LocalTime.IsDefault())
				message.LocalTime = InnerAdapter.CurrentTime;

			RaiseNewOutMessage(message);
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

	/// <summary>
	/// Indicate history initialized message.
	/// </summary>
	public class HistoryInitializedMessage : Message
	{
		/// <summary>
		/// Message type.
		/// </summary>
		public static MessageTypes MessageType => ExtendedMessageTypes.HistoryInitialized;

		/// <summary>
		/// Initializes a new instance of the <see cref="HistoryInitializedMessage"/>.
		/// </summary>
		public HistoryInitializedMessage()
			: base(MessageType)
		{
		}
	}
}
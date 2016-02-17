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

	using StockSharp.BusinessEntities;
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

			ThreadingHelper.Timer(() =>
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

		private IMarketDataStorage<TMessage> GetStorage<TMessage>(SecurityId securityId, object arg)
			where TMessage : Message
        {
			return (IMarketDataStorage<TMessage>)GetStorage(securityId, typeof(TMessage), arg);
		}

		private IMarketDataStorage GetStorage(SecurityId securityId, Type messageType, object arg)
		{
			var security = _entityRegistry.Securities.ReadBySecurityId(securityId);

			if (security == null)
				security = TryCreateSecurity(securityId);

			if (security == null)
				throw new InvalidOperationException(Localization.LocalizedStrings.Str704Params.Put(securityId));

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

			var today = DateTime.Today;

			var from = (DateTimeOffset)(today - DaysLoad);
			var to = (DateTimeOffset)(today + TimeHelper.LessOneDay);

			foreach (var secId in requiredSecurities)
			{
				GetStorage<ExecutionMessage>(secId, ExecutionTypes.Tick)
					.Load(from, to)
					.ForEach(RaiseStorageMessage);

				GetStorage<ExecutionMessage>(secId, ExecutionTypes.Transaction)
					.Load(from, to)
					.ForEach(RaiseStorageMessage);

				GetStorage<ExecutionMessage>(secId, ExecutionTypes.OrderLog)
					.Load(from, to)
					.ForEach(RaiseStorageMessage);
			}

			//_storageRegistry.DefaultDrive.GetCandleTypes();
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
				ExtensionInfo = new Dictionary<object, object>()
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
}
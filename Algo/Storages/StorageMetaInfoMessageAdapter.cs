namespace StockSharp.Algo.Storages
{
	using System;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Meta-info storage based message adapter.
	/// </summary>
	public class StorageMetaInfoMessageAdapter : MessageAdapterWrapper
	{
		private readonly ISecurityStorage _securityStorage;
		private readonly IPositionStorage _positionStorage;
		private readonly IExchangeInfoProvider _exchangeInfoProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="StorageMetaInfoMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
		/// <param name="securityStorage">Securities meta info storage.</param>
		/// <param name="positionStorage">Position storage.</param>
		/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
		public StorageMetaInfoMessageAdapter(IMessageAdapter innerAdapter, ISecurityStorage securityStorage, IPositionStorage positionStorage, IExchangeInfoProvider exchangeInfoProvider)
			: base(innerAdapter)
		{
			_securityStorage = securityStorage ?? throw new ArgumentNullException(nameof(securityStorage));
			_positionStorage = positionStorage ?? throw new ArgumentNullException(nameof(positionStorage));
			_exchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(_exchangeInfoProvider));
		}

		/// <summary>
		/// Override previous security data by new values.
		/// </summary>
		public bool OverrideSecurityData { get; set; }

		/// <summary>
		/// Load save data from storage.
		/// </summary>
		[Obsolete("Use lookup messages.")]
		public void Load()
		{
			foreach (var board in _exchangeInfoProvider.Boards)
				RaiseNewOutMessage(board.ToMessage());

			foreach (var security in _securityStorage.LookupAll())
			{
				RaiseNewOutMessage(security.ToMessage());
			}

			foreach (var portfolio in _positionStorage.Portfolios)
			{
				RaiseNewOutMessage(portfolio.ToMessage());
				RaiseNewOutMessage(portfolio.ToChangeMessage());
			}

			foreach (var position in _positionStorage.Positions)
			{
				RaiseNewOutMessage(position.ToChangeMessage());
			}
		}

		/// <inheritdoc />
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.SecurityLookup:
					ProcessSecurityLookup((SecurityLookupMessage)message);
					break;

				case MessageTypes.BoardLookup:
					ProcessBoardLookup((BoardLookupMessage)message);
					break;

				case MessageTypes.PortfolioLookup:
					ProcessPortfolioLookup((PortfolioLookupMessage)message);
					break;

				default:
					base.OnSendInMessage(message);
					break;
			}
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Security:
				{
					var secMsg = (SecurityMessage)message;
					var security = _securityStorage.LookupById(secMsg.SecurityId);

					if (security == null)
						security = secMsg.ToSecurity(_exchangeInfoProvider);
					else
						security.ApplyChanges(secMsg, _exchangeInfoProvider, OverrideSecurityData);

					_securityStorage.Save(security, OverrideSecurityData);
					break;
				}
				case MessageTypes.Board:
				{
					var boardMsg = (BoardMessage)message;
					var board = _exchangeInfoProvider.GetExchangeBoard(boardMsg.Code);

					if (board == null)
					{
						board = _exchangeInfoProvider.GetOrCreateBoard(boardMsg.Code, code =>
						{
							var exchange = _exchangeInfoProvider.GetExchange(boardMsg.ExchangeCode) ?? boardMsg.ToExchange(new Exchange
							{
								Name = boardMsg.ExchangeCode
							});

							return new ExchangeBoard
							{
								Code = code,
								Exchange = exchange
							};
						});
					}

					board.ApplyChanges(boardMsg);

					_exchangeInfoProvider.Save(board.Exchange);
					_exchangeInfoProvider.Save(board);
					break;
				}

				case MessageTypes.Portfolio:
				{
					var portfolioMsg = (PortfolioMessage)message;
					var portfolio = _positionStorage.GetPortfolio(portfolioMsg.PortfolioName) ?? new Portfolio
					{
						Name = portfolioMsg.PortfolioName
					};

					portfolioMsg.ToPortfolio(portfolio, _exchangeInfoProvider);
					_positionStorage.Save(portfolio);

					break;
				}

				case MessageTypes.PortfolioChange:
				{
					var portfolioMsg = (PortfolioChangeMessage)message;
					var portfolio = _positionStorage.GetPortfolio(portfolioMsg.PortfolioName) ?? new Portfolio
					{
						Name = portfolioMsg.PortfolioName
					};

					portfolio.ApplyChanges(portfolioMsg, _exchangeInfoProvider);
					_positionStorage.Save(portfolio);

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
					_positionStorage.Save(position);

					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		private void ProcessSecurityLookup(SecurityLookupMessage msg)
		{
			if (msg == null)
				throw new ArgumentNullException(nameof(msg));

			if (msg.Adapter != null && msg.Adapter != this)
			{
				base.OnSendInMessage(msg);
				return;
			}

			foreach (var security in _securityStorage.Lookup(msg))
				RaiseNewOutMessage(security.ToMessage(originalTransactionId: msg.TransactionId));

			base.OnSendInMessage(msg);
		}

		private void ProcessBoardLookup(BoardLookupMessage msg)
		{
			if (msg == null)
				throw new ArgumentNullException(nameof(msg));

			if (msg.Adapter != null && msg.Adapter != this)
			{
				base.OnSendInMessage(msg);
				return;
			}

			foreach (var board in _exchangeInfoProvider.LookupBoards(msg.Like))
				RaiseNewOutMessage(board.ToMessage(msg.TransactionId));

			base.OnSendInMessage(msg);
		}

		private void ProcessPortfolioLookup(PortfolioLookupMessage msg)
		{
			if (msg == null)
				throw new ArgumentNullException(nameof(msg));

			if (!msg.IsSubscribe || (msg.Adapter != null && msg.Adapter != this))
			{
				base.OnSendInMessage(msg);
				return;
			}

			foreach (var portfolio in _positionStorage.Portfolios.Filter(msg))
			{
				RaiseNewOutMessage(portfolio.ToMessage(msg.TransactionId));
				RaiseNewOutMessage(portfolio.ToChangeMessage());
			}

			foreach (var position in _positionStorage.Positions.Filter(msg))
			{
				RaiseNewOutMessage(position.ToChangeMessage(msg.TransactionId));
			}

			if (msg.IsHistory)
				RaiseNewOutMessage(new PortfolioLookupResultMessage { OriginalTransactionId = msg.TransactionId });
			else
				base.OnSendInMessage(msg);
		}

		private Position GetPosition(SecurityId securityId, string portfolioName)
		{
			var security = (!securityId.SecurityCode.IsEmpty() && !securityId.BoardCode.IsEmpty() ? _securityStorage.LookupById(securityId) : _securityStorage.Lookup(new Security
			{
				Code = securityId.SecurityCode,
			}).FirstOrDefault()) ?? TryCreateSecurity(securityId);

			if (security == null)
				return null;

			var portfolio = _positionStorage.GetPortfolio(portfolioName);

			if (portfolio == null)
			{
				portfolio = new Portfolio
				{
					Name = portfolioName
				};

				_positionStorage.Save(portfolio);
			}

			return _positionStorage.GetPosition(portfolio, security) ?? new Position
			{
				Security = security,
				Portfolio = portfolio
			};
		}

		//private Security GetSecurity(SecurityId securityId)
		//{
		//	var security = _securityStorage.LookupById(securityId) ?? TryCreateSecurity(securityId);

		//	if (security == null)
		//		throw new InvalidOperationException(LocalizedStrings.Str704Params.Put(securityId));

		//	return security;
		//}

		private Security TryCreateSecurity(SecurityId securityId)
		{
			if (securityId.SecurityCode.IsEmpty() || securityId.BoardCode.IsEmpty())
				return null;

			var security = new Security
			{
				Id = securityId.ToStringId(),
				Code = securityId.SecurityCode,
				Board = _exchangeInfoProvider.GetOrCreateBoard(securityId.BoardCode),
				//ExtensionInfo = new Dictionary<object, object>()
			};

			_securityStorage.Save(security, false);

			return security;
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(OverrideSecurityData), OverrideSecurityData);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			OverrideSecurityData = storage.GetValue(nameof(OverrideSecurityData), OverrideSecurityData);
		}

		/// <summary>
		/// Create a copy of <see cref="StorageMetaInfoMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new StorageMetaInfoMessageAdapter(InnerAdapter, _securityStorage, _positionStorage, _exchangeInfoProvider)
			{
				OverrideSecurityData = OverrideSecurityData,
			};
		}
	}
}
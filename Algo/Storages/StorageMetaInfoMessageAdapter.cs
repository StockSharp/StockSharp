namespace StockSharp.Algo.Storages;

/// <summary>
/// Meta-info storage based message adapter.
/// </summary>
public class StorageMetaInfoMessageAdapter : MessageAdapterWrapper
{
	private readonly ISecurityStorage _securityStorage;
	private readonly IPositionStorage _positionStorage;
	private readonly IExchangeInfoProvider _exchangeInfoProvider;
	private readonly IStorageProcessor _storageProcessor;

	/// <summary>
	/// Initializes a new instance of the <see cref="StorageMetaInfoMessageAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
	/// <param name="securityStorage">Securities meta info storage.</param>
	/// <param name="positionStorage">Position storage.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	/// <param name="storageProcessor">Storage processor.</param>
	public StorageMetaInfoMessageAdapter(IMessageAdapter innerAdapter, ISecurityStorage securityStorage,
		IPositionStorage positionStorage, IExchangeInfoProvider exchangeInfoProvider, IStorageProcessor storageProcessor)
		: base(innerAdapter)
	{
		_securityStorage = securityStorage ?? throw new ArgumentNullException(nameof(securityStorage));
		_positionStorage = positionStorage ?? throw new ArgumentNullException(nameof(positionStorage));
		_exchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));
		_storageProcessor = storageProcessor ?? throw new ArgumentNullException(nameof(storageProcessor));
	}

	/// <summary>
	/// Override previous security data by new values.
	/// </summary>
	public bool OverrideSecurityData { get; set; }

	/// <inheritdoc />
	protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			//case MessageTypes.Reset:
			//	_storageProcessor.Reset();
			//	return base.OnSendInMessageAsync(message, cancellationToken);

			case MessageTypes.SecurityLookup:
				return ProcessSecurityLookup((SecurityLookupMessage)message, cancellationToken);

			case MessageTypes.BoardLookup:
				return ProcessBoardLookup((BoardLookupMessage)message, cancellationToken);

			case MessageTypes.PortfolioLookup:
				return ProcessPortfolioLookup((PortfolioLookupMessage)message, cancellationToken);

			case MessageTypes.MarketData:
				return ProcessMarketData((MarketDataMessage)message, cancellationToken);

			default:
				return base.OnSendInMessageAsync(message, cancellationToken);
		}
	}

	private ValueTask ProcessMarketData(MarketDataMessage message, CancellationToken cancellationToken)
	{
		message = _storageProcessor.ProcessMarketData(message, RaiseNewOutMessage);

		return message == null ? default : base.OnSendInMessageAsync(message, cancellationToken);
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
				var board = _exchangeInfoProvider.TryGetExchangeBoard(boardMsg.Code);

				if (board == null)
				{
					board = _exchangeInfoProvider.GetOrCreateBoard(boardMsg.Code, code =>
					{
						var exchange = _exchangeInfoProvider.TryGetExchange(boardMsg.ExchangeCode) ?? boardMsg.ToExchange(new Exchange
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

				var portfolio = _positionStorage.LookupByPortfolioName(portfolioMsg.PortfolioName) ?? new Portfolio
				{
					Name = portfolioMsg.PortfolioName
				};

				portfolioMsg.ToPortfolio(portfolio, _exchangeInfoProvider);
				_positionStorage.Save(portfolio);

				break;
			}

			case MessageTypes.PositionChange:
			{
				var positionMsg = (PositionChangeMessage)message;

				if (positionMsg.IsMoney())
				{
					var portfolio = _positionStorage.LookupByPortfolioName(positionMsg.PortfolioName) ?? new Portfolio
					{
						Name = positionMsg.PortfolioName
					};

					portfolio.ApplyChanges(positionMsg, _exchangeInfoProvider);
					_positionStorage.Save(portfolio);
				}
				else
				{
					var position = GetPosition(positionMsg.SecurityId, positionMsg.PortfolioName, positionMsg.StrategyId, positionMsg.Side);

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
				}

				break;
			}
		}

		base.OnInnerAdapterNewOutMessage(message);
	}

	private ValueTask ProcessSecurityLookup(SecurityLookupMessage msg, CancellationToken cancellationToken)
	{
		if (msg == null)
			throw new ArgumentNullException(nameof(msg));

		if (/*!msg.IsSubscribe || */(msg.Adapter != null && msg.Adapter != this))
			return base.OnSendInMessageAsync(msg, cancellationToken);

		var transId = msg.TransactionId;

		foreach (var security in _securityStorage.Lookup(msg))
			RaiseNewOutMessage(security.ToMessage(originalTransactionId: transId).SetSubscriptionIds(subscriptionId: transId));

		return base.OnSendInMessageAsync(msg, cancellationToken);
	}

	private ValueTask ProcessBoardLookup(BoardLookupMessage msg, CancellationToken cancellationToken)
	{
		if (msg == null)
			throw new ArgumentNullException(nameof(msg));

		if (!msg.IsSubscribe || (msg.Adapter != null && msg.Adapter != this))
			return base.OnSendInMessageAsync(msg, cancellationToken);

		var transId = msg.TransactionId;

		foreach (var board in _exchangeInfoProvider.LookupBoards2(msg))
			RaiseNewOutMessage(board.SetSubscriptionIds(subscriptionId: transId));

		return base.OnSendInMessageAsync(msg, cancellationToken);
	}

	private ValueTask ProcessPortfolioLookup(PortfolioLookupMessage msg, CancellationToken cancellationToken)
	{
		if (msg == null)
			throw new ArgumentNullException(nameof(msg));

		if (!msg.IsSubscribe || (msg.Adapter != null && msg.Adapter != this))
			return base.OnSendInMessageAsync(msg, cancellationToken);

		var now = CurrentTimeUtc;
		var transId = msg.TransactionId;

		void SendOut<TMessage>(TMessage outMsg)
			where TMessage : Message, ISubscriptionIdMessage
		{
			outMsg.SetSubscriptionIds(subscriptionId: transId);

			if (outMsg is IServerTimeMessage timeMsg)
				timeMsg.ServerTime = now;

			outMsg.OfflineMode = MessageOfflineModes.Ignore;
			RaiseNewOutMessage(outMsg);
		}

		if (msg.StrategyId.IsEmpty())
		{
			foreach (var portfolio in _positionStorage.Portfolios.Filter(msg))
			{
				SendOut(portfolio.ToMessage(transId));
				SendOut(portfolio.ToChangeMessage());
			}
		}
		
		foreach (var position in _positionStorage.Positions)
		{
			var posMsg = position.ToChangeMessage(transId);

			if (!posMsg.IsMatch(msg, false))
				continue;

			SendOut(posMsg);
		}

		return base.OnSendInMessageAsync(msg, cancellationToken);
	}

	private Position GetPosition(SecurityId securityId, string portfolioName, string strategyId, Sides? side)
	{
		var security = (!securityId.SecurityCode.IsEmpty() && !securityId.BoardCode.IsEmpty() ? _securityStorage.LookupById(securityId) : _securityStorage.Lookup(new Security
		{
			Code = securityId.SecurityCode,
		}).FirstOrDefault()) ?? TryCreateSecurity(securityId);

		if (security == null)
			return null;

		var portfolio = _positionStorage.LookupByPortfolioName(portfolioName);

		if (portfolio == null)
		{
			portfolio = new Portfolio
			{
				Name = portfolioName
			};

			_positionStorage.Save(portfolio);
		}

		return _positionStorage.GetPosition(portfolio, security, strategyId, side) ?? new Position
		{
			Security = security,
			Portfolio = portfolio,
			StrategyId = strategyId,
			Side = side,
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
			Board = _exchangeInfoProvider.GetOrCreateBoard(securityId.BoardCode),
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
	public override IMessageAdapter Clone()
	{
		return new StorageMetaInfoMessageAdapter(InnerAdapter.TypedClone(), _securityStorage, _positionStorage, _exchangeInfoProvider, _storageProcessor)
		{
			OverrideSecurityData = OverrideSecurityData,
		};
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="message"></param>
	public void SaveDirect(PositionChangeMessage message)
	{
		OnInnerAdapterNewOutMessage(message);
	}
}
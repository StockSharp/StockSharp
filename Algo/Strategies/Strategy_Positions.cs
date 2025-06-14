namespace StockSharp.Algo.Strategies;

using StockSharp.Algo.Positions;

partial class Strategy
{
	private void ProcessPositionChangeMessage(PositionChangeMessage message)
	{
		if (message.StrategyId != EnsureGetId())
			return;

		ProcessRisk(() => message);

		var connector = SafeGetConnector();

		var position = _positions.SafeAdd(CreatePositionKey(message.SecurityId, message.PortfolioName), key => new()
		{
			Security = connector.LookupById(key.secId),
			Portfolio = connector.LookupByPortfolioName(key.pfName),
			StrategyId = message.StrategyId,
		}, out var isNew);

		position.ApplyChanges(message);
		LogInfo(LocalizedStrings.NewPosition, $"{message.SecurityId}/{message.PortfolioName}={position.CurrentValue}");

		if (isNew)
			_newPosition?.Invoke(position);
		else
			_positionChanged?.Invoke(position);

		RaisePositionChanged(position.LocalTime);

		foreach (var id in message.GetSubscriptionIds())
		{
			if (_subscriptionsById.TryGetValue(id, out var subscription))
				PositionReceived?.Invoke(subscription, position);
		}
	}

	private void RaisePositionChanged(DateTimeOffset time)
	{
		this.Notify(nameof(Position));
		PositionChanged?.Invoke();

		StatisticManager.AddPosition(time, Position);
		StatisticManager.AddPnL(time, PnL, Commission);
	}

	private readonly CachedSynchronizedDictionary<(SecurityId secId, string pfName), Position> _positions = [];

	private static (SecurityId secId, string pfName) CreatePositionKey(SecurityId security, string portfolioName)
		=> (security, portfolioName.ThrowIfEmpty(nameof(portfolioName)).ToLowerInvariant());

	private static (SecurityId secId, string pfName) CreatePositionKey(Security security, Portfolio portfolio)
		=> CreatePositionKey(security.ToSecurityId(), portfolio.CheckOnNull(nameof(portfolio)).Name);

	/// <summary>
	/// Get position.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="portfolio">Portfolio.</param>
	/// <returns>Position.</returns>
	public decimal? GetPositionValue(Security security, Portfolio portfolio)
		=> _positions.TryGetValue(CreatePositionKey(security, portfolio))?.CurrentValue;

	/// <summary>
	/// Set position.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="portfolio">Portfolio.</param>
	/// <param name="value">Position.</param>
	public void SetPositionValue(Security security, Portfolio portfolio, decimal value)
		=> _positions.SafeAdd(CreatePositionKey(security, portfolio), _ => new()
		{
			Security = security,
			Portfolio = portfolio,
			StrategyId = EnsureGetId(),
		}).CurrentValue = value;

	/// <summary>
	/// The position aggregate value.
	/// </summary>
	[Browsable(false)]
	public decimal Position
	{
		get => Security == null || Portfolio == null ? 0m : GetPositionValue(Security, Portfolio) ?? 0;
		[Obsolete("Use SetPositionValue method.")]
		set { }
	}

	/// <summary>
	/// <see cref="Position"/> change event.
	/// </summary>
	[Obsolete("Use IPositionProvider.PositionChanged instead.")]
	public event Action PositionChanged;

	/// <inheritdoc />
	[Browsable(false)]
	public IEnumerable<Position> Positions => _positions.CachedValues;

	private Action<Position> _newPosition;

	event Action<Position> IPositionProvider.NewPosition
	{
		add => _newPosition += value;
		remove => _newPosition -= value;
	}

	private Action<Position> _positionChanged;

	event Action<Position> IPositionProvider.PositionChanged
	{
		add => _positionChanged += value;
		remove => _positionChanged -= value;
	}

	Position IPositionProvider.GetPosition(Portfolio portfolio, Security security, string strategyId, Sides? side, string clientCode, string depoName, TPlusLimits? limitType)
		=> _positions.TryGetValue(CreatePositionKey(security, portfolio));

	Portfolio IPortfolioProvider.LookupByPortfolioName(string name)
		=> SafeGetConnector().LookupByPortfolioName(name);

	IEnumerable<Portfolio> IPortfolioProvider.Portfolios
		=> Portfolio == null ? [] : [Portfolio];

	event Action<Portfolio> IPortfolioProvider.NewPortfolio
	{
		add { }
		remove { }
	}

	event Action<Portfolio> IPortfolioProvider.PortfolioChanged
	{
		add { }
		remove { }
	}
}
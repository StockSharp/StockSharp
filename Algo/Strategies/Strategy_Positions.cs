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

	// All strategy orders are sent with StrategyId=rootStrategy.StrategyId
	// Also, child strategies do not subscribe for positions
	// So, child strategies do not recieve and position updates.
	// This manager allows child strategies to track their own position based on order state/balance.
	private class ChildStrategyPositionManager : BaseLogReceiver, IPositionManager
	{
		readonly StrategyPositionManager _inner;
		readonly CachedSynchronizedDictionary<(SecurityId secId, string portName), decimal> _positions = [];

		public ChildStrategyPositionManager() => _inner = new StrategyPositionManager(true) { Parent = this };

		public decimal? GetPositionValue(SecurityId securityId, string portfolioName) => _positions.TryGetValue((securityId, portfolioName));

		public PositionChangeMessage ProcessMessage(Message message)
		{
			PositionChangeMessage result = null;

			switch (message.Type)
			{
				case MessageTypes.Reset:
					_positions.Clear();
					break;

				case MessageTypes.PositionChange:
					LogWarning("ignored: {0}", message);
					break;

				default:
					result = _inner.ProcessMessage(message);
					break;
			}


			if (result == null)
				return null;

			if (!result.Changes.TryGetValue(PositionChangeTypes.CurrentValue, out var curValue))
			{
				LogWarning("no changes for {0}/{1}", result.SecurityId, result.PortfolioName);
				return result;
			}

			var key = (result.SecurityId, result.PortfolioName);

			lock(_positions.SyncRoot)
				_positions[key] = (decimal)curValue;

			return result;
		}

		public void Reset() => ProcessMessage(new ResetMessage());
	}

	private readonly ChildStrategyPositionManager _positionManager;

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
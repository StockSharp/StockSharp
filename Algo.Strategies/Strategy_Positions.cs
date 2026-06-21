namespace StockSharp.Algo.Strategies;

using StockSharp.Algo.PositionManagement;

partial class Strategy
{
	// Positions subsystem ported from the monolith StrategyOld (StrategyOld_Positions.cs and
	// StrategyOld_TargetPosition.cs) onto the decomposed engine.
	//
	// The decomposed Strategy.cs already owns most of the positions surface inline:
	//   - the Position aggregate property and its (now non-obsolete) setter,
	//   - GetPositionValue(Security, Portfolio) (returning decimal instead of the monolith's decimal?),
	//   - the PositionPipeline-typed Positions property,
	//   - the IPositionProvider.Positions / NewPosition / PositionChanged / GetPosition members,
	//   - the obsolete public PositionChanged event,
	//   - ProcessStrategyPosition / RaisePositionChanged and the OnNewPosition / OnPositionChanged hooks.
	// Those are intentionally NOT re-declared here.
	//
	// This file adds the still-missing parts of the original public API:
	//   - SetPositionValue,
	//   - the OnPositionReceived(Position) virtual hook,
	//   - the IPortfolioProvider surface,
	//   - the full target-position API (SetTargetPosition / CancelTargetPosition / GetTargetPosition /
	//     TargetPositionManager / TargetAlgoFactory).

	#region SetPositionValue / OnPositionReceived

	/// <summary>
	/// Set position.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="portfolio">Portfolio.</param>
	/// <param name="value">Position.</param>
	/// <param name="time">Timestamp to assign into <see cref="Position.LocalTime"/> and <see cref="Position.ServerTime"/> if position is created anew.</param>
	public void SetPositionValue(Security security, Portfolio portfolio, decimal value, DateTime time)
		=> _posManager.SetPosition(security, portfolio, value, time);

	/// <summary>
	/// Position received.
	/// </summary>
	/// <param name="position"><see cref="Position"/></param>
	protected virtual void OnPositionReceived(Position position)
	{
	}

	#endregion

	#region IPortfolioProvider

	Portfolio IPortfolioProvider.LookupByPortfolioName(string name)
		=> _connector?.LookupByPortfolioName(name);

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

	#endregion

	#region Target position

	private PositionTargetManager _targetManager;

	private Func<Sides, decimal, IPositionModifyAlgo> _targetAlgoFactory = (side, vol) => new MarketOrderAlgo(side, vol);

	/// <summary>
	/// Factory to create position modify algorithms for <see cref="TargetPositionManager"/>.
	/// Default creates <see cref="MarketOrderAlgo"/>.
	/// </summary>
	[Browsable(false)]
	public Func<Sides, decimal, IPositionModifyAlgo> TargetAlgoFactory
	{
		get => _targetAlgoFactory;
		set => _targetAlgoFactory = value ?? throw new ArgumentNullException(nameof(value));
	}

	private PositionTargetManager GetOrCreateTargetManager()
	{
		if (_targetManager is not null)
			return _targetManager;

		// The decomposed Strategy implements ISubscriptionProvider, ITransactionProvider and
		// IMarketRuleContainer (each via its own partial), so the strategy itself plays all three
		// collaborator roles for the manager - exactly as the monolith passed "this, this, this".
		_targetManager = new(
			this, this, this,
			getPosition: (s, p) => GetPositionValue(s, p),
			orderFactory: () => new Order(),
			canTrade: () => IsFormedAndOnlineAndAllowTrading(),
			algoFactory: _targetAlgoFactory
		);

		_targetManager.Parent = this;

		return _targetManager;
	}

	/// <summary>
	/// Target position manager.
	/// </summary>
	[Browsable(false)]
	public PositionTargetManager TargetPositionManager => GetOrCreateTargetManager();

	/// <summary>
	/// Set target position for the strategy's <see cref="Security"/> and <see cref="Portfolio"/>.
	/// </summary>
	/// <param name="target">Target position value.</param>
	public void SetTargetPosition(decimal target)
		=> SetTargetPosition(Security, Portfolio, target);

	/// <summary>
	/// Set target position for specified security and portfolio.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="portfolio">Portfolio.</param>
	/// <param name="target">Target position value.</param>
	public void SetTargetPosition(Security security, Portfolio portfolio, decimal target)
	{
		if (security is null) throw new ArgumentNullException(nameof(security));
		if (portfolio is null) throw new ArgumentNullException(nameof(portfolio));

		GetOrCreateTargetManager().SetTarget(security, portfolio, target);
	}

	/// <summary>
	/// Cancel target position for the strategy's <see cref="Security"/> and <see cref="Portfolio"/>.
	/// </summary>
	public void CancelTargetPosition()
		=> CancelTargetPosition(Security, Portfolio);

	/// <summary>
	/// Cancel target position for specified security and portfolio.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="portfolio">Portfolio.</param>
	public void CancelTargetPosition(Security security, Portfolio portfolio)
	{
		if (security is null) throw new ArgumentNullException(nameof(security));
		if (portfolio is null) throw new ArgumentNullException(nameof(portfolio));

		GetOrCreateTargetManager().CancelTarget(security, portfolio);
	}

	/// <summary>
	/// Get target position for the strategy's <see cref="Security"/> and <see cref="Portfolio"/>.
	/// </summary>
	/// <returns>Target position, or null if not set.</returns>
	public decimal? GetTargetPosition()
		=> GetTargetPosition(Security, Portfolio);

	/// <summary>
	/// Get target position for specified security and portfolio.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="portfolio">Portfolio.</param>
	/// <returns>Target position, or null if not set.</returns>
	public decimal? GetTargetPosition(Security security, Portfolio portfolio)
	{
		if (security is null) throw new ArgumentNullException(nameof(security));
		if (portfolio is null) throw new ArgumentNullException(nameof(portfolio));

		return _targetManager?.GetTarget(security, portfolio);
	}

	#endregion
}

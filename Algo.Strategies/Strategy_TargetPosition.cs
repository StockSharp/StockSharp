namespace StockSharp.Algo.Strategies;

using StockSharp.Algo.PositionManagement;

partial class Strategy
{
	private PositionTargetManager _targetManager;

	private PositionTargetManager GetOrCreateTargetManager()
	{
		if (_targetManager is not null)
			return _targetManager;

		_targetManager = new(
			this, this, this,
			getPosition: (s, p) => GetPositionValue(s, p),
			orderFactory: () => new Order(),
			canTrade: () => IsFormedAndOnlineAndAllowTrading()
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
}

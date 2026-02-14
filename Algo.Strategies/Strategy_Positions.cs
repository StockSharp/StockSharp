namespace StockSharp.Algo.Strategies;

partial class Strategy
{
	private readonly StrategyPositionManager _posManager;

	private void OnManagerPositionProcessed(Position position, bool isNew)
	{
		ProcessPosition(position, isNew);
	}

	private void ProcessPosition(Position position, bool isNew)
	{
		ArgumentNullException.ThrowIfNull(position);

		_positionTracker.ProcessPosition(position);

		ProcessRisk(() => position.ToChangeMessage());

		LogInfo(LocalizedStrings.NewPosition, $"{position.Security}/{position.Portfolio}={position.CurrentValue}");

		if (isNew)
			_newPosition?.Invoke(position);
		else
			_positionChanged?.Invoke(position);

		RaisePositionChanged(position.LocalTime);

		foreach (var subscription in _subscriptionsById.SyncGet(p => p.Values.Where(s => s.SubscriptionMessage is PortfolioLookupMessage).ToArray()))
		{
			PositionReceived?.Invoke(subscription, position);

			if (subscription == PortfolioLookup)
				OnPositionReceived(position);
		}
	}

	/// <summary>
	/// Position received.
	/// </summary>
	/// <param name="position"><see cref="Position"/></param>
	protected virtual void OnPositionReceived(Position position)
	{
	}

	private void RaisePositionChanged(DateTime time)
	{
		this.Notify(nameof(Position));
		PositionChanged?.Invoke();

		StatisticManager.AddPosition(time, Position);
		StatisticManager.AddPnL(time, PnL, Commission);
	}

	/// <summary>
	/// Get position.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="portfolio">Portfolio.</param>
	/// <returns>Position.</returns>
	public decimal? GetPositionValue(Security security, Portfolio portfolio)
		=> _posManager.TryGetPosition(security, portfolio)?.CurrentValue;

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
	public IEnumerable<Position> Positions => _posManager.Positions;

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
		=> _posManager.TryGetPosition(security, portfolio);

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
namespace StockSharp.Algo.Strategies;

partial class Strategy
{
	private class StrategyPositionManager(Strategy strategy)
	{
		private readonly SyncObject _lock = new();

		private readonly Dictionary<(Security, Portfolio), Position> _positions = [];

		public Position TryGetPosition(Security security, Portfolio portfolio)
		{
			lock (_lock)
				return _positions.TryGetValue((security, portfolio));
		}

		public void SetPosition(Security security, Portfolio portfolio, decimal value)
		{
			lock (_lock)
				GetPosition(security, portfolio, out _).CurrentValue = value;
		}

		private Position GetPosition(Security security, Portfolio portfolio, out bool isNew)
			=> _positions.SafeAdd((security, portfolio), _ => new()
			{
				Security = security,
				Portfolio = portfolio,
				StrategyId = strategy.EnsureGetId(),
			}, out isNew);

		public Position[] Positions
		{
			get
			{
				lock (_lock)
					return [.. _positions.Values];
			}
		}

		public void Reset()
		{
			lock (_lock)
			{
				_positions.Clear();
			}
		}

		public void ProcessOrder(Order order)
		{
			ArgumentNullException.ThrowIfNull(order);

			var matched = order.GetMatchedVolume().Value;

			if (matched == 0)
				return;

			if (order.Side == Sides.Sell)
				matched = -matched;

			Position position;
			bool isNew;

			lock (_lock)
			{
				position = GetPosition(order.Security, order.Portfolio, out isNew);
				position.CurrentValue = (position.CurrentValue ?? 0) + matched;
				position.LocalTime = order.LocalTime;
				position.LastChangeTime = order.ServerTime;
			}

			strategy.ProcessPosition(position, isNew);
		}
	}

	private readonly StrategyPositionManager _posManager;

	private void ProcessPosition(Position position, bool isNew)
	{
		ArgumentNullException.ThrowIfNull(position);

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

	private void RaisePositionChanged(DateTimeOffset time)
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
	public void SetPositionValue(Security security, Portfolio portfolio, decimal value)
		=> _posManager.SetPosition(security, portfolio, value);

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
namespace StockSharp.Algo;

partial class MarketRuleHelper
{
	#region Portfolio rules

	private sealed class PortfolioRule : MarketRule<Portfolio, Portfolio>
	{
		private readonly Func<Portfolio, bool> _changed;
		private readonly Portfolio _portfolio;
		private readonly IPortfolioProvider _provider;

		public PortfolioRule(Portfolio portfolio, IPortfolioProvider provider, Func<Portfolio, bool> changed)
			: base(portfolio)
		{
			_changed = changed ?? throw new ArgumentNullException(nameof(changed));

			_portfolio = portfolio ?? throw new ArgumentNullException(nameof(portfolio));
			_provider = provider ?? throw new ArgumentNullException(nameof(provider));
			_provider.PortfolioChanged += OnPortfolioChanged;
		}

		private void OnPortfolioChanged(Portfolio portfolio)
		{
			if (portfolio == _portfolio && _changed(_portfolio))
				Activate(_portfolio);
		}

		protected override void DisposeManaged()
		{
			_provider.PortfolioChanged -= OnPortfolioChanged;
			base.DisposeManaged();
		}
	}

	/// <summary>
	/// To create a rule for the event of change portfolio .
	/// </summary>
	/// <param name="portfolio">The portfolio to be traced for the event of change.</param>
	/// <param name="provider">The provider of information about portfolios.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Portfolio, Portfolio> WhenChanged(this Portfolio portfolio, IPortfolioProvider provider)
	{
		if (portfolio == null)
			throw new ArgumentNullException(nameof(portfolio));

		return new PortfolioRule(portfolio, provider, pf => true)
		{
			Name = $"PF {portfolio.Name} change"
		};
	}

	/// <summary>
	/// To create a rule for the event of money decrease in portfolio below the specific level.
	/// </summary>
	/// <param name="portfolio">The portfolio to be traced for the event of money decrease below the specific level.</param>
	/// <param name="provider">The provider of information about portfolios.</param>
	/// <param name="money">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Portfolio, Portfolio> WhenMoneyLess(this Portfolio portfolio, IPortfolioProvider provider, Unit money)
	{
		if (portfolio == null)
			throw new ArgumentNullException(nameof(portfolio));

		if (money == null)
			throw new ArgumentNullException(nameof(money));

		var finishMoney = money.Type == UnitTypes.Limit ? money : portfolio.CurrentValue - money;

		return new PortfolioRule(portfolio, provider, pf => pf.CurrentValue < finishMoney)
		{
			Name = $"PF {portfolio.Name} < {finishMoney}"
		};
	}

	/// <summary>
	/// To create a rule for the event of money increase in portfolio above the specific level.
	/// </summary>
	/// <param name="portfolio">The portfolio to be traced for the event of money increase above the specific level.</param>
	/// <param name="provider">The provider of information about portfolios.</param>
	/// <param name="money">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Portfolio, Portfolio> WhenMoneyMore(this Portfolio portfolio, IPortfolioProvider provider, Unit money)
	{
		if (portfolio == null)
			throw new ArgumentNullException(nameof(portfolio));

		if (money == null)
			throw new ArgumentNullException(nameof(money));

		var finishMoney = money.Type == UnitTypes.Limit ? money : portfolio.CurrentValue + money;

		return new PortfolioRule(portfolio, provider, pf => pf.CurrentValue > finishMoney)
		{
			Name = $"PF {portfolio.Name} > {finishMoney}"
		};
	}

	#endregion

	#region Position rules

	private sealed class PositionRule : MarketRule<Position, Position>
	{
		private readonly Func<Position, bool> _changed;
		private readonly Position _position;
		private readonly IPositionProvider _provider;

		public PositionRule(Position position, IPositionProvider provider)
			: this(position, provider, p => true)
		{
			Name = LocalizedStrings.PositionChange + " " + position.Portfolio.Name;
		}

		public PositionRule(Position position, IPositionProvider provider, Func<Position, bool> changed)
			: base(position)
		{
			_changed = changed ?? throw new ArgumentNullException(nameof(changed));

			_position = position ?? throw new ArgumentNullException(nameof(position));
			_provider = provider ?? throw new ArgumentNullException(nameof(provider));
			_provider.PositionChanged += OnPositionChanged;
		}

		private void OnPositionChanged(Position position)
		{
			if (position == _position && _changed(_position))
				Activate(_position);
		}

		protected override void DisposeManaged()
		{
			_provider.PositionChanged -= OnPositionChanged;
			base.DisposeManaged();
		}
	}

	/// <summary>
	/// To create a rule for the event of position decrease below the specific level.
	/// </summary>
	/// <param name="position">The position to be traced for the event of decrease below the specific level.</param>
	/// <param name="provider">The position provider.</param>
	/// <param name="value">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Position, Position> WhenLess(this Position position, IPositionProvider provider, Unit value)
	{
		if (position == null)
			throw new ArgumentNullException(nameof(position));

		if (value == null)
			throw new ArgumentNullException(nameof(value));

		var finishPosition = value.Type == UnitTypes.Limit ? value : position.CurrentValue - value;

		return new PositionRule(position, provider, pf => pf.CurrentValue < finishPosition)
		{
			Name = $"Pos {position} < {value}"
		};
	}

	/// <summary>
	/// To create a rule for the event of position increase above the specific level.
	/// </summary>
	/// <param name="position">The position to be traced of the event of increase above the specific level.</param>
	/// <param name="provider">The position provider.</param>
	/// <param name="value">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Position, Position> WhenMore(this Position position, IPositionProvider provider, Unit value)
	{
		if (position == null)
			throw new ArgumentNullException(nameof(position));

		if (value == null)
			throw new ArgumentNullException(nameof(value));

		var finishPosition = value.Type == UnitTypes.Limit ? value : position.CurrentValue + value;

		return new PositionRule(position, provider, pf => pf.CurrentValue > finishPosition)
		{
			Name = $"Pos {position} > {value}"
		};
	}

	/// <summary>
	/// To create a rule for the position change event.
	/// </summary>
	/// <param name="position">The position to be traced for the change event.</param>
	/// <param name="provider">The position provider.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Position, Position> Changed(this Position position, IPositionProvider provider)
	{
		return new PositionRule(position, provider);
	}

	#endregion
}

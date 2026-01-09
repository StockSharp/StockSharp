namespace StockSharp.Algo.Strategies;

using StockSharp.Charting;
using StockSharp.Algo.Derivatives;

partial class Strategy
{
	/// <summary>
	/// To create the initialized order object.
	/// </summary>
	/// <param name="side">Order side.</param>
	/// <param name="price">The price. If <see langword="null" /> value is passed, the order is registered at market price.</param>
	/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Strategy.Volume"/> value is used.</param>
	/// <returns>The initialized order object.</returns>
	/// <remarks>
	/// The order is not registered, only the object is created.
	/// </remarks>
	public Order CreateOrder(Sides side, decimal price, decimal? volume = null)
	{
		var order = new Order
		{
			Portfolio = GetPortfolio(),
			Security = GetSecurity(),
			Side = side,
			Volume = volume ?? Volume,
		};

		if (price == 0)
		{
			//if (security.Board.IsSupportMarketOrders)
			order.Type = OrderTypes.Market;
			//else
			//	order.Price = strategy.GetMarketPrice(direction) ?? 0;
		}
		else
			order.Price = price;

		return order;
	}

	private const string _optionDeskKey = "OptionDesk";

	/// <summary>
	/// To get the <see cref="IOptionDesk"/>.
	/// </summary>
	/// <returns><see cref="IOptionDesk"/>.</returns>
	public IOptionDesk GetOptionDesk()
	{
		return Environment.GetValue<IOptionDesk>(_optionDeskKey);
	}

	/// <summary>
	/// To set the <see cref="IOptionDesk"/>.
	/// </summary>
	/// <param name="desk"><see cref="IOptionDesk"/>.</param>
	public void SetOptionDesk(IOptionDesk desk)
	{
		Environment.SetValue(_optionDeskKey, desk);
	}

	/// <summary>
	/// To get market data value for the strategy instrument.
	/// </summary>
	/// <typeparam name="T">The type of the market data field value.</typeparam>
	/// <param name="field">Market-data field.</param>
	/// <returns>The field value. If no data, the <see langword="null" /> will be returned.</returns>
	public T GetSecurityValue<T>(Level1Fields field)
	{
		return this.GetSecurityValue<T>(Security, field);
	}

	/// <summary>
	/// <see cref="IsFormed"/> and <see cref="IsOnline"/>.
	/// </summary>
	/// <returns>Check result.</returns>
	public bool IsFormedAndOnline() => IsFormed && IsOnline;

	/// <summary>
	/// <see cref="IsFormedAndOnline"/> and <see cref="TradingMode"/>.
	/// </summary>
	/// <param name="required">Required action.</param>
	/// <returns>Check result.</returns>
	public bool IsFormedAndOnlineAndAllowTrading(StrategyTradingModes required = StrategyTradingModes.Full)
	{
		if (!IsFormedAndOnline() || TradingMode == StrategyTradingModes.Disabled)
			return false;

		return required switch
		{
			StrategyTradingModes.Full => TradingMode == StrategyTradingModes.Full,
			StrategyTradingModes.CancelOrdersOnly => true,
			StrategyTradingModes.ReducePositionOnly => TradingMode != StrategyTradingModes.CancelOrdersOnly,
			_ => throw new ArgumentOutOfRangeException(nameof(required), required, LocalizedStrings.InvalidValue),
		};
	}

	/// <summary>
	/// Get <see cref="Security"/> or throw <see cref="InvalidOperationException"/> if not present.
	/// </summary>
	/// <returns><see cref="Security"/></returns>
	public Security GetSecurity()
		=> Security ?? throw new InvalidOperationException(LocalizedStrings.SecurityNotSpecified);

	/// <summary>
	/// Get <see cref="Portfolio"/> or throw <see cref="InvalidOperationException"/> if not present.
	/// </summary>
	/// <returns><see cref="Portfolio"/></returns>
	public Portfolio GetPortfolio()
		=> Portfolio ?? throw new InvalidOperationException(LocalizedStrings.PortfolioNotSpecified);

	private const string _keyChart = "Chart";

	/// <summary>
	/// To get the <see cref="IChart"/> associated with the passed strategy.
	/// </summary>
	/// <returns>Chart.</returns>
	public IChart GetChart()
	{
		return Environment.GetValue<IChart>(_keyChart);
	}

	/// <summary>
	/// To set a <see cref="IChart"/> for the strategy.
	/// </summary>
	/// <param name="chart">Chart.</param>
	public void SetChart(IChart chart)
	{
		Environment.SetValue(_keyChart, chart);
	}

	private const string _keyOptionPositionChart = "OptionPositionChart";

	/// <summary>
	/// To get the <see cref="IOptionPositionChart"/> associated with the passed strategy.
	/// </summary>
	/// <returns>Chart.</returns>
	public IOptionPositionChart GetOptionPositionChart()
	{
		return Environment.GetValue<IOptionPositionChart>(_keyOptionPositionChart);
	}

	/// <summary>
	/// To set a <see cref="IChart"/> for the strategy.
	/// </summary>
	/// <param name="chart">Chart.</param>
	public void SetOptionPositionChart(IOptionPositionChart chart)
	{
		Environment.SetValue(_keyOptionPositionChart, chart);
	}
}

namespace StockSharp.Algo.Strategies;

using StockSharp.Algo.Testing;
using StockSharp.Algo.Derivatives;

partial class Strategy
{
	/// <summary>
	/// Strategy name generator.
	/// </summary>
	[Browsable(false)]
	public StrategyNameGenerator NameGenerator { get; }

	/// <inheritdoc />
	public override string Name
	{
		get => base.Name;
		set
		{
			// A manual name assignment turns off auto-generation (mirrors the monolith behavior).
			NameGenerator.Value = value;
			base.Name = value;
		}
	}

	/// <summary>
	/// The strategy is executed in the history (backtesting) mode.
	/// </summary>
	[Browsable(false)]
	public bool IsBacktesting => Connector is HistoryEmulationConnector;

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

	/// <summary>
	/// Child strategies.
	/// </summary>
	[Browsable(false)]
	[Obsolete("Child strategies no longer supported.")]
	public INotifyList<Strategy> ChildStrategies { get; } = new SynchronizedList<Strategy>();

	/// <summary>
	/// <see cref="DrawOrderBook"/>.
	/// </summary>
	public event Action<Subscription, IOrderBookSource, IOrderBookMessage> OrderBookDrawing;

	/// <summary>
	/// <see cref="DrawOrderBookOrder"/>.
	/// </summary>
	public event Action<Subscription, IOrderBookSource, Order> OrderBookDrawingOrder;

	/// <summary>
	/// <see cref="DrawOrderBookOrderFail"/>.
	/// </summary>
	public event Action<Subscription, IOrderBookSource, OrderFail> OrderBookDrawingOrderFail;

	/// <summary>
	/// Draw order book.
	/// </summary>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <param name="source"><see cref="IOrderBookSource"/></param>
	/// <param name="book"><see cref="IOrderBookMessage"/></param>
	public void DrawOrderBook(Subscription subscription, IOrderBookSource source, IOrderBookMessage book)
	{
		if (subscription is null)	throw new ArgumentNullException(nameof(subscription));
		if (source is null)			throw new ArgumentNullException(nameof(source));
		if (book is null)			throw new ArgumentNullException(nameof(book));

		OrderBookDrawing?.Invoke(subscription, source, book);
	}

	/// <summary>
	/// Draw order book order.
	/// </summary>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <param name="source"><see cref="IOrderBookSource"/></param>
	/// <param name="order">Order.</param>
	public void DrawOrderBookOrder(Subscription subscription, IOrderBookSource source, Order order)
	{
		if (subscription is null)	throw new ArgumentNullException(nameof(subscription));
		if (source is null)			throw new ArgumentNullException(nameof(source));
		if (order is null)			throw new ArgumentNullException(nameof(order));

		OrderBookDrawingOrder?.Invoke(subscription, source, order);
	}

	/// <summary>
	/// Draw order book order fail.
	/// </summary>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <param name="source"><see cref="IOrderBookSource"/></param>
	/// <param name="fail">Order fail.</param>
	public void DrawOrderBookOrderFail(Subscription subscription, IOrderBookSource source, OrderFail fail)
	{
		if (subscription is null)	throw new ArgumentNullException(nameof(subscription));
		if (source is null)			throw new ArgumentNullException(nameof(source));
		if (fail is null)			throw new ArgumentNullException(nameof(fail));

		OrderBookDrawingOrderFail?.Invoke(subscription, source, fail);
	}

	private const string _optionDeskKey = "OptionDesk";

	/// <summary>
	/// To get the <see cref="IOptionDesk"/> associated with the strategy.
	/// </summary>
	/// <returns><see cref="IOptionDesk"/>.</returns>
	public IOptionDesk GetOptionDesk()
		=> Environment.GetValue<IOptionDesk>(_optionDeskKey);

	/// <summary>
	/// To set the <see cref="IOptionDesk"/>.
	/// </summary>
	/// <param name="desk"><see cref="IOptionDesk"/>.</param>
	public void SetOptionDesk(IOptionDesk desk)
		=> Environment.SetValue(_optionDeskKey, desk);

	private const string _optionPositionChartKey = "OptionPositionChart";

	/// <summary>
	/// To get the <see cref="IOptionPositionChart"/> associated with the strategy.
	/// </summary>
	/// <returns><see cref="IOptionPositionChart"/>.</returns>
	public IOptionPositionChart GetOptionPositionChart()
		=> Environment.GetValue<IOptionPositionChart>(_optionPositionChartKey);

	/// <summary>
	/// To set the <see cref="IOptionPositionChart"/>.
	/// </summary>
	/// <param name="chart"><see cref="IOptionPositionChart"/>.</param>
	public void SetOptionPositionChart(IOptionPositionChart chart)
		=> Environment.SetValue(_optionPositionChartKey, chart);

	/// <summary>
	/// Stop the strategy because of the specified error.
	/// </summary>
	/// <param name="error">Error.</param>
	public void Stop(Exception error)
	{
		LogError(error);

		LastError = error ?? throw new ArgumentNullException(nameof(error));
		Stop();
	}
}

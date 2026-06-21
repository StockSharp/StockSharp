namespace StockSharp.Algo.Strategies;

using System.Drawing;

using StockSharp.Algo.Indicators;
using StockSharp.Charting;

partial class Strategy
{
	// HighLevelCharting subsystem ported from the monolith StrategyOld.
	//
	// The monolith stores the active IChart inside its Environment settings storage
	// (StrategyOld_Extensions.GetChart/SetChart). The decomposed Strategy has no such
	// Environment member, so a minimal private backing field is provided here and the
	// same public GetChart/SetChart API is reproduced on top of it.
	private IChart _chart;
	private bool _drawingFeedHooked;
	private SynchronizedList<Order> _drawingOrders;
	private SynchronizedList<MyTrade> _drawingTrades;
	private readonly CachedSynchronizedList<IChartOrderElement> _ordersElems = [];
	private readonly CachedSynchronizedList<IChartTradeElement> _tradesElems = [];
	private readonly SynchronizedDictionary<Subscription, IChartElement> _subscriptionElems = [];
	private readonly SynchronizedDictionary<IIndicator, IChartIndicatorElement> _indElems = [];

	/// <summary>
	/// To get the <see cref="IChart"/> associated with the strategy.
	/// </summary>
	/// <returns>Chart.</returns>
	public IChart GetChart() => _chart;

	/// <summary>
	/// To set a <see cref="IChart"/> for the strategy.
	/// </summary>
	/// <param name="chart">Chart.</param>
	public void SetChart(IChart chart) => _chart = chart;

	/// <summary>
	/// Create chart area.
	/// </summary>
	/// <returns><see cref="IChartArea"/></returns>
	protected IChartArea CreateChartArea()
	{
		_chart ??= GetChart();
		return _chart?.AddArea();
	}

	/// <summary>
	/// Draw candles on chart.
	/// </summary>
	/// <param name="area"><see cref="IChartArea"/></param>
	/// <param name="subscription">Subscription handler.</param>
	/// <returns><see cref="IChartCandleElement"/></returns>
	protected IChartCandleElement DrawCandles<T>(IChartArea area, ISubscriptionHandler<T> subscription)
		=> DrawCandles(area, subscription.Subscription);

	/// <summary>
	/// Draw candles on chart.
	/// </summary>
	/// <param name="area"><see cref="IChartArea"/></param>
	/// <param name="subscription"><see cref="Subscription"/></param>
	/// <returns><see cref="IChartCandleElement"/></returns>
	protected IChartCandleElement DrawCandles(IChartArea area, Subscription subscription)
	{
		var elem = area.AddCandles();
		_subscriptionElems.Add(subscription, elem);
		return elem;
	}

	/// <summary>
	/// Draw indicator on chart.
	/// </summary>
	/// <param name="area"><see cref="IChartArea"/></param>
	/// <param name="indicator"><see cref="IIndicator"/></param>
	/// <param name="color"><see cref="IChartIndicatorElement.Color"/></param>
	/// <param name="additionalColor"><see cref="IChartIndicatorElement.AdditionalColor"/></param>
	/// <returns><see cref="IChartIndicatorElement"/></returns>
	protected IChartIndicatorElement DrawIndicator(IChartArea area, IIndicator indicator, Color? color = default, Color? additionalColor = default)
	{
		var elem = area.AddIndicator(indicator);

		if (color is not null)
			elem.Color = color.Value;

		if (additionalColor is not null)
			elem.AdditionalColor = additionalColor.Value;

		_indElems.Add(indicator, elem);

		return elem;
	}

	/// <summary>
	/// Draw trades on chart.
	/// </summary>
	/// <param name="area"><see cref="IChartArea"/></param>
	/// <returns><see cref="IChartTradeElement"/></returns>
	protected IChartTradeElement DrawOwnTrades(IChartArea area)
	{
		var elem = area.AddTrades();
		_drawingTrades = [];
		_tradesElems.Add(elem);
		EnsureDrawingFeed();
		return elem;
	}

	/// <summary>
	/// Draw orders on chart.
	/// </summary>
	/// <param name="area"><see cref="IChartArea"/></param>
	/// <returns><see cref="IChartOrderElement"/></returns>
	protected IChartOrderElement DrawOrders(IChartArea area)
	{
		var elem = area.AddOrders();
		_drawingOrders = [];
		_ordersElems.Add(elem);
		EnsureDrawingFeed();
		return elem;
	}

	// The monolith fed _drawingTrades / _drawingOrders from its connector own-trade/order
	// handlers (only for the OrderLookup subscription). The decomposed Strategy surfaces the
	// equivalent data via the OwnTradeReceived / OrderReceived events, so the drawing buffers
	// are filled from there. The hook is installed lazily and only once, the first time a
	// trade/order chart element is requested, so strategies that don't draw incur no overhead.
	private void EnsureDrawingFeed()
	{
		if (_drawingFeedHooked)
			return;

		_drawingFeedHooked = true;

		OwnTradeReceived += (_, trade) => _drawingTrades?.Add(trade);
		OrderReceived += (_, order) => _drawingOrders?.Add(order);
	}

	private void DrawFlush(Subscription subscription, Func<ICandleMessage> getCandle, List<IIndicatorValue> indValues)
	{
		if (subscription is null)	throw new ArgumentNullException(nameof(subscription));
		if (getCandle is null)		throw new ArgumentNullException(nameof(getCandle));
		if (indValues is null)		throw new ArgumentNullException(nameof(indValues));

		var trade = _drawingTrades?.CopyAndClear().FirstOrDefault();
		var order = _drawingOrders?.CopyAndClear().FirstOrDefault();

		if (_chart == null)
			return;

		var data = _chart.CreateData();
		var candle = getCandle();

		var item = data.Group(candle.OpenTime);

		if (_subscriptionElems.TryGetValue(subscription, out var candleElem))
			item.Add(candleElem, candle);

		foreach (var indValue in indValues)
		{
			if (_indElems.TryGetValue(indValue.Indicator, out var indElem))
				item.Add(indElem, indValue);
		}

		if (order is not null)
		{
			foreach (var ordersElem in _ordersElems.Cache)
				item.Add(ordersElem, order);
		}

		if (trade is not null)
		{
			foreach (var tradesElem in _tradesElems.Cache)
				item.Add(tradesElem, trade);
		}

		_chart.Draw(data);
	}
}

namespace StockSharp.Charting;

using Ecng.Configuration;

using static StockSharp.Charting.IChartDrawData;

/// <summary>
/// <see cref="IChart"/> extensions.
/// </summary>
public static class IChartExtensions
{
	/// <summary>
	/// <see cref="IIndicatorProvider"/>
	/// </summary>
	public static IIndicatorProvider IndicatorProvider => ConfigManager.GetService<IIndicatorProvider>();

	/// <summary>
	/// <see cref="IIndicatorProvider"/>
	/// </summary>
	public static IIndicatorProvider TryIndicatorProvider => ConfigManager.TryGetService<IIndicatorProvider>();

	/// <summary>
	/// <see cref="IChartIndicatorPainterProvider"/>
	/// </summary>
	public static IChartIndicatorPainterProvider IndicatorPainterProvider => ConfigManager.GetService<IChartIndicatorPainterProvider>();

	/// <summary>
	/// <see cref="IIndicatorProvider"/>
	/// </summary>
	public static IChartIndicatorPainterProvider TryIndicatorPainterProvider => ConfigManager.TryGetService<IChartIndicatorPainterProvider>();

	/// <summary>
	/// Fill <see cref="IChart.IndicatorTypes"/> using <see cref="IIndicatorProvider"/>.
	/// </summary>
	/// <param name="chart">Chart.</param>
	public static void FillIndicators(this IChart chart)
	{
		if (chart == null)
			throw new ArgumentNullException(nameof(chart));

		chart.IndicatorTypes.Clear();
		chart.IndicatorTypes.AddRange(IndicatorProvider.All.ExcludeObsolete());
	}

	/// <summary>
	/// The primary title for the X-axis.
	/// </summary>
	public const string DefaultXAxisId = "X";

	/// <summary>
	/// The primary title for the Y-axis.
	/// </summary>
	public const string DefaultYAxisId = "Y";

	/// <summary>
	/// Whether this axis can be removed from chart area.
	/// </summary>
	public static bool IsDefault(this IChartAxis axis)
		=> axis.Id == DefaultXAxisId || axis.Id == DefaultYAxisId;

	/// <summary>
	/// Put the chart data.
	/// </summary>
	/// <param name="item"><see cref="IChartDrawData.IChartDrawDataItem"/> instance.</param>
	/// <param name="element">The chart element.</param>
	/// <param name="value">The chart value.</param>
	/// <returns><see cref="IChartDrawData.IChartDrawDataItem"/> instance.</returns>
	public static IChartDrawDataItem Add(this IChartDrawDataItem item, IChartElement element, object value)
	{
		switch (element)
		{
			case null:
				throw new ArgumentNullException(nameof(element));
			case IChartCandleElement candleElem:
				return item.Add(candleElem, (ICandleMessage)value);
			case IChartIndicatorElement indElem:
				return item.Add(indElem, (IIndicatorValue)value);
			case IChartOrderElement orderElem:
			{
				var order = (Order)value;
				return item.Add(orderElem, order, order.State != OrderStates.Failed ? null : LocalizedStrings.Failed);
			}
			case IChartTradeElement tradeElem:
				return item.Add(tradeElem, (MyTrade)value);
			//case ChartActiveOrdersElement activeEleme:
			//	return Add(activeEleme, (ChartActiveOrderInfo)value);
			case IChartLineElement lineElem:
				return value switch
				{
					null => throw new ArgumentNullException(nameof(value)),
					double d => item.Add(lineElem, d),
					decimal d => item.Add(lineElem, (double)d),
					Tuple<double, double> t => item.Add(lineElem, t.Item1, t.Item2),
					_ => throw new ArgumentException(LocalizedStrings.UnsupportedType.Put(value.GetType().Name)),
				};
			case IChartBandElement belem:
				return value switch
				{
					null => throw new ArgumentNullException(nameof(value)),
					double d => item.Add(belem, d, 0),
					decimal d => item.Add(belem, d),
					Tuple<double, double> t => item.Add(belem, t.Item1, t.Item2),
					_ => throw new ArgumentException(LocalizedStrings.UnsupportedType.Put(value.GetType().Name)),
				};
			default:
				throw new ArgumentException(LocalizedStrings.UnsupportedType.Put(element));
		}
	}

	/// <summary>
	/// Put the candle data.
	/// </summary>
	/// <param name="item"><see cref="IChartDrawData.IChartDrawDataItem"/> instance.</param>
	/// <param name="element">The chart element representing a candle.</param>
	/// <param name="candle">The candle data.</param>
	/// <returns><see cref="IChartDrawData.IChartDrawDataItem"/> instance.</returns>
	public static IChartDrawDataItem Add(this IChartDrawDataItem item, IChartCandleElement element, ICandleMessage candle)
	{
		if (candle == null)
		{
			//throw new ArgumentNullException(nameof(candle));
			return item;
		}

		return item.Add(element, candle, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, candle.PriceLevels?.ToArray(), candle.State);
	}

	/// <summary>
	/// Put the candle data.
	/// </summary>
	/// <param name="item"><see cref="IChartDrawDataItem"/> instance.</param>
	/// <param name="element">The chart element representing a candle.</param>
	/// <param name="candle">Candle.</param>
	/// <param name="openPrice">Opening price.</param>
	/// <param name="highPrice">Highest price.</param>
	/// <param name="lowPrice">Lowest price.</param>
	/// <param name="closePrice">Closing price.</param>
	/// <param name="priceLevels">Price levels.</param>
	/// <param name="state">Candle state.</param>
	/// <returns><see cref="IChartDrawDataItem"/> instance.</returns>
	public static IChartDrawDataItem Add(this IChartDrawDataItem item, IChartCandleElement element, ICandleMessage candle, decimal openPrice, decimal highPrice, decimal lowPrice, decimal closePrice, CandlePriceLevel[] priceLevels, CandleStates state)
	{
		if (candle == null)
		{
			//throw new ArgumentNullException(nameof(candle));
			return item;
		}

		return item.Add(element, candle.DataType, candle.SecurityId, openPrice, highPrice, lowPrice, closePrice, priceLevels, state);
	}

	/// <summary>
	/// Put the order data.
	/// </summary>
	/// <param name="item"><see cref="IChartDrawDataItem"/> instance.</param>
	/// <param name="element">The chart element representing orders.</param>
	/// <param name="order">The order value.</param>
	/// <param name="errorMessage">Error registering/cancelling order.</param>
	/// <returns><see cref="IChartDrawDataItem"/> instance.</returns>
	public static IChartDrawDataItem Add(this IChartDrawDataItem item, IChartOrderElement element, Order order, string errorMessage = null)
	{
		if (order == null)
			return item;
		//	throw new ArgumentNullException(nameof(order));

		if (errorMessage.IsEmpty() && order.State == OrderStates.Failed)
			errorMessage = LocalizedStrings.Failed;

		return item.Add(element, order.TransactionId, null, order.Side, order.Price, order.Volume, errorMessage);
	}

	/// <summary>
	/// Put the order data.
	/// </summary>
	/// <param name="item"><see cref="IChartDrawDataItem"/> instance.</param>
	/// <param name="element">The chart element representing trades.</param>
	/// <param name="trade">The trade value.</param>
	/// <returns><see cref="IChartDrawData.IChartDrawDataItem"/> instance.</returns>
	public static IChartDrawDataItem Add(this IChartDrawDataItem item, IChartTradeElement element, MyTrade trade)
	{
		if (trade == null)
			return item;
		//	throw new ArgumentNullException(nameof(trade));

		var tick = trade.Trade;
		return item.Add(element, tick.Id ?? default, tick.StringId, trade.Order.Side, tick.Price, tick.Volume);
	}

	/// <summary>
	/// To use automatic range for the X-axis.
	/// </summary>
	/// <param name="area"><see cref="IChartArea"/></param>
	/// <returns>To use automatic range for the X-axis.</returns>
	public static bool GetAutoRange(this IChartArea area)
		=> area.XAxises.All(a => a.AutoRange);

	/// <summary>
	/// To use automatic range for the X-axis.
	/// </summary>
	/// <param name="area"><see cref="IChartArea"/></param>
	/// <param name="value">To use automatic range for the X-axis.</param>
	public static void SetAutoRange(this IChartArea area, bool value)
		=> area.XAxises.ForEach(a => a.AutoRange = value);

	/// <summary>
	/// The chart on which the element is drawn.
	/// </summary>
	/// <param name="elem"><see cref="IChartElement"/></param>
	/// <returns>The chart on which the element is drawn.</returns>
	public static IChart TryGetChart(this IChartElement elem)
		=> elem.CheckOnNull(nameof(elem)).ChartArea?.Chart;

	/// <summary>
	/// X axis this element currently attached to.
	/// </summary>
	/// <param name="elem"><see cref="IChartElement"/></param>
	/// <returns>X axis this element currently attached to.</returns>
	public static IChartAxis TryGetXAxis(this IChartElement elem)
		=> elem.CheckOnNull(nameof(elem)).ChartArea?.XAxises.FirstOrDefault(xa => xa.Id == elem.XAxisId);

	/// <summary>
	/// Y axis this element currently attached to.
	/// </summary>
	/// <param name="elem"><see cref="IChartElement"/></param>
	/// <returns>Y axis this element currently attached to.</returns>
	public static IChartAxis TryGetYAxis(this IChartElement elem)
		=> elem.CheckOnNull(nameof(elem)).ChartArea?.YAxises.FirstOrDefault(xa => xa.Id == elem.YAxisId);

	/// <summary>
	/// Get all elements.
	/// </summary>
	/// <param name="chart"><see cref="IChart"/></param>
	/// <returns>Chart elements.</returns>
	public static IEnumerable<IChartElement> GetElements(this IChart chart)
		=> chart.CheckOnNull(nameof(chart)).Areas.SelectMany(a => a.Elements);

	/// <summary>
	/// Get all elements.
	/// </summary>
	/// <typeparam name="T"><see cref="IChartElement"/> type.</typeparam>
	/// <param name="chart"><see cref="IChart"/></param>
	/// <returns>Chart elements.</returns>
	public static IEnumerable<T> GetElements<T>(this IChart chart)
		where T : IChartElement
		=> chart.GetElements().OfType<T>();

	/// <summary>
	/// To remove all areas from the chart.
	/// </summary>
	/// <param name="chart"><see cref="IChart"/></param>
	public static void ClearAreas(this IChart chart)
	{
		if (chart is null)
			throw new ArgumentNullException(nameof(chart));

		chart.Reset(chart.GetElements());

		foreach (var area in chart.Areas.ToArray())
			chart.RemoveArea(area);
	}
}
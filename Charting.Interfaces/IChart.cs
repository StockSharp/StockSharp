namespace StockSharp.Charting
{
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.ComponentModel;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface for work with the chart.
	/// </summary>
	public interface IChart : IThemeableChart
	{
		/// <summary>
		/// Chart areas.
		/// </summary>
		INotifyList<IChartArea> Areas { get; }

		/// <summary>
		/// To scroll <see cref="Areas"/> areas automatically when new data occurred. The default is enabled.
		/// </summary>
		bool IsAutoScroll { get; set; }

		/// <summary>
		/// To use automatic range for the X-axis. The default is off.
		/// </summary>
		bool IsAutoRange { get; set; }

		/// <summary>
		/// Type of X axis for this chart.
		/// </summary>
		ChartAxisType XAxisType { get; set; }

		/// <summary>
		/// The list of available indicators types.
		/// </summary>
		IList<IndicatorType> IndicatorTypes { get; }

		/// <summary>
		/// Show non formed indicators values.
		/// </summary>
		bool ShowNonFormedIndicators { get; set; }

		/// <summary>
		/// Show FPS.
		/// </summary>
		bool ShowPerfStats { get; set; }

		/// <summary>
		/// <see cref="IDispatcher"/>.
		/// </summary>
		IDispatcher ThreadDispatcher { get; }

		/// <summary>
		/// Create <see cref="IChartArea"/> instance.
		/// </summary>
		/// <returns><see cref="IChartArea"/> instance.</returns>
		IChartArea CreateArea();

		/// <summary>
		/// Create <see cref="IChartAxis"/> instance.
		/// </summary>
		/// <returns><see cref="IChartAxis"/> instance.</returns>
		IChartAxis CreateAxis();

		/// <summary>
		/// Create <see cref="IChartCandleElement"/> instance.
		/// </summary>
		/// <returns><see cref="IChartCandleElement"/> instance.</returns>
		IChartCandleElement CreateCandleElement();

		/// <summary>
		/// Create <see cref="IChartIndicatorElement"/> instance.
		/// </summary>
		/// <returns><see cref="IChartIndicatorElement"/> instance.</returns>
		IChartIndicatorElement CreateIndicatorElement();

		/// <summary>
		/// Create <see cref="IChartActiveOrdersElement"/> instance.
		/// </summary>
		/// <returns><see cref="IChartActiveOrdersElement"/> instance.</returns>
		IChartActiveOrdersElement CreateActiveOrdersElement();

		/// <summary>
		/// Create <see cref="IChartAnnotation"/> instance.
		/// </summary>
		/// <returns><see cref="IChartAnnotation"/> instance.</returns>
		IChartAnnotation CreateAnnotation();

		/// <summary>
		/// Create <see cref="IChartBandElement"/> instance.
		/// </summary>
		/// <returns><see cref="IChartBandElement"/> instance.</returns>
		IChartBandElement CreateBandElement();

		/// <summary>
		/// Create <see cref="IChartLineElement"/> instance.
		/// </summary>
		/// <returns><see cref="IChartLineElement"/> instance.</returns>
		IChartLineElement CreateLineElement();

		/// <summary>
		/// Create <see cref="IChartLineElement"/> instance.
		/// </summary>
		/// <returns><see cref="IChartLineElement"/> instance.</returns>
		IChartLineElement CreateBubbleElement();

		/// <summary>
		/// Create <see cref="IChartOrderElement"/> instance.
		/// </summary>
		/// <returns><see cref="IChartOrderElement"/> instance.</returns>
		IChartOrderElement CreateOrderElement();

		/// <summary>
		/// Create <see cref="IChartTradeElement"/> instance.
		/// </summary>
		/// <returns><see cref="IChartTradeElement"/> instance.</returns>
		IChartTradeElement CreateTradeElement();

		/// <summary>
		/// To add an area to the chart.
		/// </summary>
		/// <param name="area">Chart area.</param>
		void AddArea(IChartArea area);

		/// <summary>
		/// To remove the area from the chart.
		/// </summary>
		/// <param name="area">Chart area.</param>
		void RemoveArea(IChartArea area);

		/// <summary>
		/// To remove all areas from the chart.
		/// </summary>
		void ClearAreas();

		/// <summary>
		/// To add an element to the chart.
		/// </summary>
		/// <param name="area">Chart area.</param>
		/// <param name="element">The chart element.</param>
		void AddElement(IChartArea area, IChartElement element);

		/// <summary>
		/// To add an element to the chart.
		/// </summary>
		/// <param name="area">Chart area.</param>
		/// <param name="element">The chart element.</param>
		/// <param name="candleSeries">Candles series.</param>
		void AddElement(IChartArea area, IChartCandleElement element, CandleSeries candleSeries);

		/// <summary>
		/// To add an element to the chart.
		/// </summary>
		/// <param name="area">Chart area.</param>
		/// <param name="element">The chart element.</param>
		/// <param name="candleSeries">Candles series.</param>
		/// <param name="indicator">Indicator.</param>
		void AddElement(IChartArea area, IChartIndicatorElement element, CandleSeries candleSeries, IIndicator indicator);

		/// <summary>
		/// To add an element to the chart.
		/// </summary>
		/// <param name="area">Chart area.</param>
		/// <param name="element">The chart element.</param>
		/// <param name="security">Security.</param>
		void AddElement(IChartArea area, IChartOrderElement element, Security security);

		/// <summary>
		/// To add an element to the chart.
		/// </summary>
		/// <param name="area">Chart area.</param>
		/// <param name="element">The chart element.</param>
		/// <param name="security">Security.</param>
		void AddElement(IChartArea area, IChartTradeElement element, Security security);

		/// <summary>
		/// To remove the element from the chart.
		/// </summary>
		/// <param name="area">Chart area.</param>
		/// <param name="element">The chart element.</param>
		void RemoveElement(IChartArea area, IChartElement element);

		/// <summary>
		/// To get an indicator which is associated with <see cref="IChartIndicatorElement"/>.
		/// </summary>
		/// <param name="element">The chart element.</param>
		/// <returns>Indicator.</returns>
		IIndicator GetIndicator(IChartIndicatorElement element);

		/// <summary>
		/// To get the data source for <see cref="IChartElement"/>.
		/// </summary>
		/// <param name="element">The chart element.</param>
		/// <returns>Market-data source.</returns>
		object GetSource(IChartElement element);

		/// <summary>
		/// To reset the chart elements values drawn previously.
		/// </summary>
		/// <param name="elements">Chart elements.</param>
		void Reset(IEnumerable<IChartElement> elements);
	}
}
namespace StockSharp.Charting
{
	using System.Collections.Generic;

	using Ecng.Collections;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface for work with the chart.
	/// </summary>
	public interface IChart : IChartBuilder, IThemeableChart
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
namespace StockSharp.Charting
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.Messages;
	using StockSharp.Algo.Strategies;
	using StockSharp.Algo.Indicators;

	/// <summary>
	/// Extension class for <see cref="IChart"/>.
	/// </summary>
	public static class ChartingInterfacesExtensions
	{
		/// <summary>
		/// To draw the candle.
		/// </summary>
		/// <param name="chart">Chart.</param>
		/// <param name="element">The chart element representing a candle.</param>
		/// <param name="candle">Candle.</param>
		public static void Draw(this IChart chart, IChartCandleElement element, ICandleMessage candle)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));

			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

			var data = chart.CreateData();

			data
				.Group(candle.OpenTime)
					.Add(element, candle);

			chart.Draw(data);
		}

		/// <summary>
		/// To draw new data.
		/// </summary>
		/// <param name="chart">Chart.</param>
		/// <param name="time">The time stamp of the new data generation.</param>
		/// <param name="element">The chart element.</param>
		/// <param name="value">Value.</param>
		[Obsolete("Use the Draw method instead.")]
		public static void Draw(this IChart chart, DateTimeOffset time, IChartElement element, object value)
		{
			if (chart == null)
				throw new ArgumentNullException(nameof(chart));

			chart.Draw(time, new Dictionary<IChartElement, object> { { element, value } });
		}

		/// <summary>
		/// To process the new data.
		/// </summary>
		/// <param name="chart">Chart.</param>
		/// <param name="time">The time stamp of the new data generation.</param>
		/// <param name="values">New data.</param>
		[Obsolete("Use the Draw method instead.")]
		public static void Draw(this IChart chart, DateTimeOffset time, IDictionary<IChartElement, object> values)
		{
			if (chart == null)
				throw new ArgumentNullException(nameof(chart));

			chart.Draw(new[] { RefTuple.Create(time, values) });
		}

		/// <summary>
		/// To process the new data.
		/// </summary>
		/// <param name="chart">Chart.</param>
		/// <param name="values">New data.</param>
		[Obsolete("Use the Draw method instead.")]
		public static void Draw(this IChart chart, IEnumerable<RefPair<DateTimeOffset, IDictionary<IChartElement, object>>> values)
		{
			var data = chart.CreateData();

			foreach (var pair in values)
			{
				var item = data.Group(pair.First);

				foreach (var p in pair.Second)
					item.Add(p.Key, p.Value);
			}

			chart.Draw(data);
		}

		private const string _keyChart = "Chart";

		/// <summary>
		/// To get the <see cref="IChart"/> associated with the passed strategy.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <returns>Chart.</returns>
		public static IChart GetChart(this Strategy strategy)
		{
			if (strategy is null)
				throw new ArgumentNullException(nameof(strategy));

			return strategy.Environment.GetValue<IChart>(_keyChart);
		}

		/// <summary>
		/// To set a <see cref="IChart"/> for the strategy.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="chart">Chart.</param>
		public static void SetChart(this Strategy strategy, IChart chart)
		{
			if (strategy is null)
				throw new ArgumentNullException(nameof(strategy));

			strategy.Environment.SetValue(_keyChart, chart);
		}

		private const string _keyOptionPositionChart = "OptionPositionChart";

		/// <summary>
		/// To get the <see cref="IOptionPositionChart"/> associated with the passed strategy.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <returns>Chart.</returns>
		public static IOptionPositionChart GetOptionPositionChart(this Strategy strategy)
		{
			if (strategy is null)
				throw new ArgumentNullException(nameof(strategy));

			return strategy.Environment.GetValue<IOptionPositionChart>(_keyOptionPositionChart);
		}

		/// <summary>
		/// To set a <see cref="IChart"/> for the strategy.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="chart">Chart.</param>
		public static void SetOptionPositionChart(this Strategy strategy, IOptionPositionChart chart)
		{
			if (strategy is null)
				throw new ArgumentNullException(nameof(strategy));

			strategy.Environment.SetValue(_keyOptionPositionChart, chart);
		}

		/// <summary>
		/// Check the specified style is volume profile based.
		/// </summary>
		/// <param name="style">Style.</param>
		/// <returns>Check result.</returns>
		public static bool IsVolumeProfileChart(this ChartCandleDrawStyles style)
			=> style == ChartCandleDrawStyles.BoxVolume || style == ChartCandleDrawStyles.ClusterProfile;

		/// <summary>
		/// Create <see cref="IChartArea"/>.
		/// </summary>
		/// <param name="chart"><see cref="IChart"/></param>
		/// <returns><see cref="IChartArea"/></returns>
		public static IChartArea AddArea(this IChart chart)
		{
			if (chart is null)
				throw new ArgumentNullException(nameof(chart));

			var area = chart.CreateArea();
			chart.AddArea(area);
			return area;
		}

		/// <summary>
		/// Create <see cref="IChartCandleElement"/> element.
		/// </summary>
		/// <param name="area"><see cref="IChartArea"/></param>
		/// <returns><see cref="IChartCandleElement"/></returns>
		public static IChartCandleElement AddCandles(this IChartArea area)
		{
			if (area is null)
				throw new ArgumentNullException(nameof(area));

			var elem = area.Chart.CreateCandleElement();
			area.Chart.AddElement(area, elem);
			return elem;
		}

		/// <summary>
		/// Create <see cref="IChartTradeElement"/> element.
		/// </summary>
		/// <param name="area"><see cref="IChartArea"/></param>
		/// <returns><see cref="IChartTradeElement"/></returns>
		public static IChartTradeElement AddTrades(this IChartArea area)
		{
			if (area is null)
				throw new ArgumentNullException(nameof(area));

			var elem = area.Chart.CreateTradeElement();
			area.Chart.AddElement(area, elem);
			return elem;
		}

		/// <summary>
		/// Create <see cref="IChartOrderElement"/> element.
		/// </summary>
		/// <param name="area"><see cref="IChartArea"/></param>
		/// <returns><see cref="IChartOrderElement"/></returns>
		public static IChartOrderElement AddOrders(this IChartArea area)
		{
			if (area is null)
				throw new ArgumentNullException(nameof(area));

			var elem = area.Chart.CreateOrderElement();
			area.Chart.AddElement(area, elem);
			return elem;
		}

		/// <summary>
		/// Create <see cref="IChartIndicatorElement"/> element.
		/// </summary>
		/// <param name="area"><see cref="IChartArea"/></param>
		/// <param name="indicator"><see cref="IIndicator"/></param>
		/// <returns><see cref="IChartIndicatorElement"/></returns>
		public static IChartIndicatorElement AddIndicator(this IChartArea area, IIndicator indicator)
		{
			if (area is null)
				throw new ArgumentNullException(nameof(area));

			if (indicator is null)
				throw new ArgumentNullException(nameof(indicator));

			var elem = area.Chart.CreateIndicatorElement();
			elem.FullTitle = indicator.ToString();
			area.Chart.AddElement(area, elem);
			return elem;
		}
	}
}
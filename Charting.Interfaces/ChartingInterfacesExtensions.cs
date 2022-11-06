namespace StockSharp.Charting
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Strategies;
	using StockSharp.Localization;

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
		public static void Draw(this IChart chart, IChartCandleElement element, Candle candle)
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

		/// <summary>
		/// To return the area by the specified index. If the number of areas is smaller then the missing areas will be created automatically.
		/// </summary>
		/// <param name="chart">Chart.</param>
		/// <param name="index">The area index.</param>
		/// <returns>Area.</returns>
		public static IChartArea GetArea(this IChart chart, int index)
		{
			if (chart == null)
				throw new ArgumentNullException(nameof(chart));

			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			while (chart.Areas.Count < index + 1)
			{
				var area = chart.CreateArea();
				area.Title = LocalizedStrings.Panel + " " + index;
				area.XAxisType = chart.XAxisType;
				chart.Areas.Add(area);
			}

			return chart.Areas[index];
		}

		/// <summary>
		/// To get the chart associated with the passed strategy.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <returns>Chart.</returns>
		public static IChart GetChart(this Strategy strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			return strategy.Environment.GetValue<IChart>("Chart");
		}

		/// <summary>
		/// To set a chart for the strategy.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="chart">Chart.</param>
		public static void SetChart(this Strategy strategy, IChart chart)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			//if (chart == null)
			//	throw new ArgumentNullException(nameof(chart));

			strategy.Environment.SetValue("Chart", chart);
		}

		/// <summary>
		/// Check the specified style is volume profile based.
		/// </summary>
		/// <param name="style">Style.</param>
		/// <returns>Check result.</returns>
		public static bool IsVolumeProfileChart(this ChartCandleDrawStyles style)
			=> style == ChartCandleDrawStyles.BoxVolume || style == ChartCandleDrawStyles.ClusterProfile;
	}
}
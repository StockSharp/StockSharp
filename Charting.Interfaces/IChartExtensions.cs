namespace StockSharp.Charting
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;

	using StockSharp.Algo.Indicators;

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
		/// Exclude obsolete indicators.
		/// </summary>
		/// <param name="types">All indicator types.</param>
		/// <returns>Filtered collection.</returns>
		public static IEnumerable<IndicatorType> ExcludeObsolete(this IEnumerable<IndicatorType> types)
		{
			return types.Where(t => !t.Indicator.IsObsolete());
		}

		/// <summary>
		/// Fill <see cref="IChart.IndicatorTypes"/> using <see cref="IIndicatorProvider"/>.
		/// </summary>
		/// <param name="chart">Chart.</param>
		public static void FillIndicators(this IChart chart)
		{
			if (chart == null)
				throw new ArgumentNullException(nameof(chart));

			chart.IndicatorTypes.Clear();
			chart.IndicatorTypes.AddRange(IndicatorProvider.GetIndicatorTypes().ExcludeObsolete().Where(it => it.Indicator.IsIndicatorSupportedByChart()));
		}

		private static readonly Type[] _chartUnsupportedIndicators =
		{
			typeof(Level1Indicator),
			typeof(Covariance),
			typeof(Correlation),
		};

		/// <summary>
		/// Check if indicator is supported by chart.
		/// </summary>
		public static bool IsIndicatorSupportedByChart(this Type itype)
		{
			return
				typeof(IIndicator).IsAssignableFrom(itype) &&
				!_chartUnsupportedIndicators.Any(t => t.IsAssignableFrom(itype));
		}
	}
}
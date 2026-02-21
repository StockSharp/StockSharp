namespace StockSharp.Algo.Indicators;

using System;

/// <summary>
/// Compatibility extension methods for indicators.
/// </summary>
public static class IndicatorCompatExtensions
{
	/// <summary>
	/// Process a decimal value through the indicator.
	/// </summary>
	public static IIndicatorValue Process(this IIndicator indicator, decimal value)
		=> indicator.Process(new DecimalIndicatorValue(indicator, value, DateTime.UtcNow));

	/// <summary>
	/// Process a decimal value through the indicator with a specific time.
	/// </summary>
	public static IIndicatorValue Process(this IIndicator indicator, decimal value, DateTime time)
		=> indicator.Process(new DecimalIndicatorValue(indicator, value, time));
}

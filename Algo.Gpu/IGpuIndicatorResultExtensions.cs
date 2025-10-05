namespace StockSharp.Algo.Gpu;

/// <summary>
/// Extension methods for <see cref="IGpuIndicatorResult"/>.
/// </summary>
public static class IGpuIndicatorResultExtensions
{
	/// <summary>
	/// Convert <see cref="IGpuIndicatorResult.Time"/> to <see cref="IIndicatorValue.Time"/> for the specified indicator.
	/// </summary>
	/// <param name="result"><see cref="IGpuIndicatorResult"/></param>
	/// <returns><see cref="IIndicatorValue.Time"/></returns>
	public static DateTimeOffset GetTime(this IGpuIndicatorResult result)
		=> result.Time.To<DateTimeOffset>();

	/// <summary>
	/// Convert <see cref="IGpuIndicatorResult.IsFormed"/> to <see cref="IIndicatorValue.IsFormed"/> for the specified indicator.
	/// </summary>
	/// <param name="result"><see cref="IGpuIndicatorResult"/></param>
	/// <returns><see cref="IIndicatorValue.IsFormed"/></returns>
	public static bool GetIsFormed(this IGpuIndicatorResult result)
		=> result.IsFormed.To<bool>();
}
namespace StockSharp.Algo.Gpu;

/// <summary>
/// Interface for GPU indicator calculation results.
/// </summary>
public interface IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTimeOffset.Ticks"/>.
	/// </summary>
	long Time { get; }

	/// <summary>
	/// Is indicator formed (byte to be GPU-friendly).
	/// </summary>
	byte IsFormed { get; }

	/// <summary>
	/// Convert GPU result to appropriate indicator value.
	/// </summary>
	/// <param name="indicator">The indicator to create value for.</param>
	/// <returns>Indicator value compatible with the specified indicator type.</returns>
	IIndicatorValue ToValue(IIndicator indicator);
}
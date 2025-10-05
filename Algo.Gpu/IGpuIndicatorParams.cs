namespace StockSharp.Algo.Gpu;

/// <summary>
/// Base params contract for GPU indicator calculators.
/// </summary>
public interface IGpuIndicatorParams
{
	/// <summary>
	/// Copy parameters from the given indicator instance into this params struct.
	/// </summary>
	/// <param name="indicator"><see cref="IIndicator"/></param>
	void FromIndicator(IIndicator indicator);
}
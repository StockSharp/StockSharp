namespace StockSharp.Algo.Gpu;

/// <summary>
/// Primitive candle structure for GPU (data only, no methods).
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuCandle"/> struct.
/// </remarks>
/// <param name="time">Candle time.</param>
/// <param name="open">Open price.</param>
/// <param name="high">High price.</param>
/// <param name="low">Low price.</param>
/// <param name="close">Close price.</param>
/// <param name="volume">Candle volume.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuCandle(DateTime time, decimal open, decimal high, decimal low, decimal close, decimal volume)
{
	/// <summary>
	/// Candle time as <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time = time.Ticks;

	/// <summary>
	/// Open price.
	/// </summary>
	public float Open = (float)open;

	/// <summary>
	/// High price.
	/// </summary>
	public float High = (float)high;

	/// <summary>
	/// Low price.
	/// </summary>
	public float Low = (float)low;

	/// <summary>
	/// Close price.
	/// </summary>
	public float Close = (float)close;

	/// <summary>
	/// Candle volume.
	/// </summary>
	public float Volume = (float)volume;
}
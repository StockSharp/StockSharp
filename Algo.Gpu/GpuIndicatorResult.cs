namespace StockSharp.Algo.Gpu;

/// <summary>
/// Indicator calculation result on GPU.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuIndicatorResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Calculated value.
	/// </summary>
	public float Value;

	/// <summary>
	/// Is indicator formed (byte to be GPU-friendly).
	/// </summary>
	public byte IsFormed; // bool as byte for GPU

	readonly long IGpuIndicatorResult.Time => Time;
	readonly byte IGpuIndicatorResult.IsFormed => IsFormed;

	/// <summary>
	/// Convert GPU result to appropriate indicator value.
	/// </summary>
	/// <param name="indicator">The indicator to create value for.</param>
	/// <returns>Indicator value compatible with the specified indicator type.</returns>
	public readonly IIndicatorValue ToValue(IIndicator indicator)
	{
		var time = this.GetTime();
		var isFormed = this.GetIsFormed();

		if (Value.IsNaN())
		{
			return new DecimalIndicatorValue(indicator, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
			};
		}

		var value = (decimal)Value;

		return new DecimalIndicatorValue(indicator, value, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};
	}

	/// <inheritdoc />
	public readonly override string ToString()
		=> $"{this.GetTime()}: {Value}";
}
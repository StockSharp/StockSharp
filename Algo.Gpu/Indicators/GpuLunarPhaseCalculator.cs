namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Lunar Phase calculation.
/// </summary>
/// <remarks>
/// This indicator does not expose configurable parameters; the struct exists to satisfy the common GPU infrastructure.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct GpuLunarPhaseParams : IGpuIndicatorParams
{
	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
	}
}

/// <summary>
/// GPU calculator for the <see cref="LunarPhase"/> indicator.
/// </summary>
public class GpuLunarPhaseCalculator : GpuIndicatorCalculatorBase<LunarPhase, GpuLunarPhaseParams, GpuIndicatorResult>
{
	private const long EpochTicks = 630827792400000000L;
	private const double SynodicMonthDays = 29.530588853;
	private const double TicksPerDay = 864000000000d;

	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuLunarPhaseCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuLunarPhaseCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>>(LunarPhaseParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuLunarPhaseParams[] parameters)
	{
		ArgumentNullException.ThrowIfNull(candlesSeries);
		ArgumentNullException.ThrowIfNull(parameters);

		if (candlesSeries.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(candlesSeries));
		if (parameters.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(parameters));

		var seriesCount = candlesSeries.Length;

		// Flatten input
		var totalSize = 0;
		var seriesOffsets = new int[seriesCount];
		var seriesLengths = new int[seriesCount];

		for (var s = 0; s < seriesCount; s++)
		{
			seriesOffsets[s] = totalSize;
			var len = candlesSeries[s]?.Length ?? 0;
			seriesLengths[s] = len;
			totalSize += len;
		}

		var flatCandles = new GpuCandle[totalSize];
		var maxLen = 0;
		var offset = 0;
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			if (len > 0)
			{
				Array.Copy(candlesSeries[s], 0, flatCandles, offset, len);
				offset += len;
				if (len > maxLen)
					maxLen = len;
			}
		}

		using var inputBuffer = Accelerator.Allocate1D(flatCandles);
		using var offsetsBuffer = Accelerator.Allocate1D(seriesOffsets);
		using var lengthsBuffer = Accelerator.Allocate1D(seriesLengths);
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var extent = new Index3D(parameters.Length, seriesCount, maxLen);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		// Re-split [series][param][bar]
		var result = new GpuIndicatorResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuIndicatorResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuIndicatorResult[len];
				for (var i = 0; i < len; i++)
				{
					var globalIdx = seriesOffsets[s] + i;
					var resIdx = p * totalSize + globalIdx;
					arr[i] = flatResults[resIdx];
				}
				result[s][p] = arr;
			}
		}

		return result;
	}

	/// <summary>
	/// ILGPU kernel: Lunar Phase computation for multiple series and parameter sets. Results are stored as [param][globalIdx].
	/// </summary>
	private static void LunarPhaseParamsSeriesKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;
		var candleIdx = index.Z;

		var len = lengths[seriesIdx];
		if (candleIdx >= len)
			return;

		var offset = offsets[seriesIdx];
		var globalIdx = offset + candleIdx;
		var resIndex = paramIdx * flatCandles.Length + globalIdx;

		var candle = flatCandles[globalIdx];
		var phase = CalculatePhase(candle.Time);

		flatResults[resIndex] = new()
		{
			Time = candle.Time,
			Value = phase,
			IsFormed = 1,
		};
	}

	/// <summary>
	/// Calculate lunar phase index (0..7) for the specified timestamp ticks.
	/// </summary>
	/// <param name="ticks">Timestamp in <see cref="DateTimeOffset.Ticks"/>.</param>
	/// <returns>Lunar phase index as float for GPU result storage.</returns>
	private static float CalculatePhase(long ticks)
	{
		var daysSinceEpoch = (ticks - EpochTicks) / TicksPerDay;
		var normalized = daysSinceEpoch % SynodicMonthDays;
		if (normalized < 0)
			normalized += SynodicMonthDays;

		var scaled = normalized / SynodicMonthDays * 8.0;
		var index = (int)(scaled + 0.5) & 7;
		return index;
	}
}

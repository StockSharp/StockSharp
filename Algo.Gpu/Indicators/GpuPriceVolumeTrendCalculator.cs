namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Price Volume Trend (PVT) calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuPriceVolumeTrendParams"/> struct.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct GpuPriceVolumeTrendParams : IGpuIndicatorParams
{
	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
	}
}

/// <summary>
/// GPU calculator for Price Volume Trend (PVT).
/// </summary>
public class GpuPriceVolumeTrendCalculator : GpuIndicatorCalculatorBase<PriceVolumeTrend, GpuPriceVolumeTrendParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuPriceVolumeTrendParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuPriceVolumeTrendCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuPriceVolumeTrendCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuPriceVolumeTrendParams>>(PriceVolumeTrendParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuPriceVolumeTrendParams[] parameters)
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
		var offset = 0;
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			if (len > 0)
			{
				Array.Copy(candlesSeries[s], 0, flatCandles, offset, len);
				offset += len;
			}
		}

		using var inputBuffer = Accelerator.Allocate1D(flatCandles);
		using var offsetsBuffer = Accelerator.Allocate1D(seriesOffsets);
		using var lengthsBuffer = Accelerator.Allocate1D(seriesLengths);
		using var paramsBuffer = Accelerator.Allocate1D(parameters);
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
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
					var resIdx = p * flatCandles.Length + globalIdx;
					arr[i] = flatResults[resIdx];
				}
				result[s][p] = arr;
			}
		}

		return result;
	}

	/// <summary>
	/// ILGPU kernel: PVT computation for multiple series and parameter sets.
	/// One thread handles one (parameter, series) pair and iterates bars sequentially.
	/// </summary>
	private static void PriceVolumeTrendParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuPriceVolumeTrendParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var offset = offsets[seriesIdx];

		var prevClose = 0f;
		var pvt = 0f;

		for (var i = 0; i < len; i++)
		{
			var candleIndex = offset + i;
			var candle = flatCandles[candleIndex];
			var resIndex = paramIdx * flatCandles.Length + candleIndex;

			if (prevClose == 0f)
			{
				prevClose = candle.Close;
				flatResults[resIndex] = new() { Time = candle.Time, Value = 0f, IsFormed = 0 };
				continue;
			}

			var priceChange = (candle.Close - prevClose) / prevClose;
			var volumeContribution = candle.Volume * priceChange;

			pvt += volumeContribution;
			prevClose = candle.Close;

			flatResults[resIndex] = new() { Time = candle.Time, Value = pvt, IsFormed = 1 };
		}
	}
}

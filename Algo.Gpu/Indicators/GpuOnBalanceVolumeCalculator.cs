namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU On-Balance Volume calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuObvParams : IGpuIndicatorParams
{
	/// <summary>
	/// Copy parameters from the given indicator instance.
	/// </summary>
	/// <param name="indicator">Indicator instance.</param>
	public readonly void FromIndicator(IIndicator indicator)
	{
	}
}

/// <summary>
/// GPU calculator for On-Balance Volume (OBV).
/// </summary>
public class GpuOnBalanceVolumeCalculator : GpuIndicatorCalculatorBase<OnBalanceVolume, GpuObvParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuObvParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuOnBalanceVolumeCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuOnBalanceVolumeCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuObvParams>>(ObvParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuObvParams[] parameters)
	{
		ArgumentNullException.ThrowIfNull(candlesSeries);
		ArgumentNullException.ThrowIfNull(parameters);

		if (candlesSeries.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(candlesSeries));

		if (parameters.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(parameters));

		var seriesCount = candlesSeries.Length;

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
	/// ILGPU kernel: OBV computation for multiple series and multiple parameter sets.
	/// </summary>
	private static void ObvParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuObvParams> parameters)
	{
		_ = parameters;

		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var len = lengths[seriesIdx];

		if (len == 0)
			return;

		var offset = offsets[seriesIdx];
		var totalCandles = flatCandles.Length;
		var prevClose = 0f;
		var currentValue = 0f;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var value = currentValue;

			if (prevClose != 0f)
			{
				if (candle.Close > prevClose)
					value += candle.Volume;
				else if (candle.Close < prevClose)
					value -= candle.Volume;
			}

			var resIndex = paramIdx * totalCandles + globalIdx;
			flatResults[resIndex] = new() { Time = candle.Time, Value = value, IsFormed = 1 };

			prevClose = candle.Close;
			currentValue = value;
		}
	}
}

namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Positive Volume Index (PVI) calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuPositiveVolumeIndexParams"/> struct.
/// </remarks>
/// <param name="startValue">Initial PVI value.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuPositiveVolumeIndexParams(float startValue = 1000f) : IGpuIndicatorParams
{
	/// <summary>
	/// Initial index value.
	/// </summary>
	public float StartValue = startValue;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		// PositiveVolumeIndex indicator has no configurable parameters to extract.
	}
}

/// <summary>
/// GPU calculator for Positive Volume Index (PVI).
/// </summary>
public class GpuPositiveVolumeIndexCalculator : GpuIndicatorCalculatorBase<PositiveVolumeIndex, GpuPositiveVolumeIndexParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuPositiveVolumeIndexParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuPositiveVolumeIndexCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuPositiveVolumeIndexCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuPositiveVolumeIndexParams>>(PositiveVolumeIndexKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuPositiveVolumeIndexParams[] parameters)
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
	/// ILGPU kernel for PVI computation across multiple series and parameter sets.
	/// </summary>
	private static void PositiveVolumeIndexKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuPositiveVolumeIndexParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var offset = offsets[seriesIdx];
		var startValue = parameters[paramIdx].StartValue;
		if (startValue <= 0f)
			startValue = 1000f;

		var pvi = startValue;
		var prevClose = 0f;
		var prevVolume = 0f;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var close = candle.Close;
			var volume = candle.Volume;

			if (prevClose != 0f && prevVolume != 0f && volume > 0f)
			{
				if (volume > prevVolume)
				{
					var priceChange = (close - prevClose) / prevClose;
					pvi += pvi * priceChange;
				}
			}

			var globalIdx = offset + i;
			var resIndex = paramIdx * flatCandles.Length + globalIdx;
			flatResults[resIndex] = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = pvi,
				IsFormed = 1
			};

			prevClose = close;
			prevVolume = volume;
		}
	}
}

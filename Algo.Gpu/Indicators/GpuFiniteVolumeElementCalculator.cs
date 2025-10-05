namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Finite Volume Element (FVE) calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuFiniteVolumeElementParams"/> struct.
/// </remarks>
/// <param name="length">FVE length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuFiniteVolumeElementParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// FVE smoothing window length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is FiniteVolumeElement fve)
		{
			Unsafe.AsRef(in this).Length = fve.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Finite Volume Element (FVE) indicator.
/// </summary>
public class GpuFiniteVolumeElementCalculator : GpuIndicatorCalculatorBase<FiniteVolumeElement, GpuFiniteVolumeElementParams, GpuIndicatorResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuFiniteVolumeElementParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuFiniteVolumeElementCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuFiniteVolumeElementCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuFiniteVolumeElementParams>>(FiniteVolumeElementKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuFiniteVolumeElementParams[] parameters)
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
		using var paramsBuffer = Accelerator.Allocate1D(parameters);
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var extent = new Index3D(parameters.Length, seriesCount, maxLen);
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
	/// ILGPU kernel performing FVE calculation for multiple series and parameter sets.
	/// </summary>
	private static void FiniteVolumeElementKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuFiniteVolumeElementParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;
		var candleIdx = index.Z;

		var seriesLength = lengths[seriesIdx];
		if (candleIdx >= seriesLength)
			return;

		var offset = offsets[seriesIdx];
		var globalIdx = offset + candleIdx;
		var resIndex = paramIdx * flatCandles.Length + globalIdx;
		var candle = flatCandles[globalIdx];

		var result = new GpuIndicatorResult
		{
			Time = candle.Time,
			Value = float.NaN,
			IsFormed = 0,
		};

		var prm = parameters[paramIdx];
		var length = prm.Length;

		if (length <= 0)
		{
			flatResults[resIndex] = result;
			return;
		}

		if (candleIdx < length - 1)
		{
			flatResults[resIndex] = result;
			return;
		}

		var sum = 0f;

		for (var j = 0; j < length; j++)
		{
			var candleIndex = globalIdx - j;
			var c = flatCandles[candleIndex];
			sum += CalculateFve(c);
		}

		result.Value = sum / length * 100f;
		result.IsFormed = 1;
		flatResults[resIndex] = result;
	}

	private static float CalculateFve(in GpuCandle candle)
	{
		var range = candle.High - candle.Low;

		if (range != 0f && candle.Volume != 0f)
		{
			var ratio = (candle.Close - candle.Low) / range;
			var volumeFlow = candle.Volume * (2f * ratio - 1f);
			return volumeFlow / candle.Volume;
		}

		return 0f;
	}
}

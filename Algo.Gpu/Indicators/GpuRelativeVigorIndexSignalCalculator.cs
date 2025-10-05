namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Relative Vigor Index signal calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuRelativeVigorIndexSignalParams"/> struct.
/// </remarks>
/// <param name="length">Signal smoothing length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuRelativeVigorIndexSignalParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// Signal smoothing length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is RelativeVigorIndexSignal signal)
		{
			Unsafe.AsRef(in this).Length = signal.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Relative Vigor Index signal part.
/// </summary>
public class GpuRelativeVigorIndexSignalCalculator : GpuIndicatorCalculatorBase<RelativeVigorIndexSignal, GpuRelativeVigorIndexSignalParams, GpuIndicatorResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuRelativeVigorIndexSignalParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuRelativeVigorIndexSignalCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuRelativeVigorIndexSignalCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuRelativeVigorIndexSignalParams>>(RelativeVigorIndexSignalKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuRelativeVigorIndexSignalParams[] parameters)
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
	/// ILGPU kernel calculating Relative Vigor Index signal for multiple series and parameter sets.
	/// </summary>
	private static void RelativeVigorIndexSignalKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuRelativeVigorIndexSignalParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;
		var candleIdx = index.Z;

		var len = lengths[seriesIdx];
		if (candleIdx >= len)
			return;

		var offset = offsets[seriesIdx];
		var globalIdx = offset + candleIdx;

		var candle = flatCandles[globalIdx];
		var resIndex = paramIdx * flatCandles.Length + globalIdx;
		flatResults[resIndex] = new() { Time = candle.Time, Value = float.NaN, IsFormed = 0 };

		var prm = parameters[paramIdx];
		if (prm.Length != 4)
			return;

		const int averageLength = 4;
		var minIndex = (averageLength - 1) + (prm.Length - 1);
		if (candleIdx < minIndex)
			return;

		var avg0 = ComputeAverage(flatCandles, globalIdx - 3);
		var avg1 = ComputeAverage(flatCandles, globalIdx - 2);
		var avg2 = ComputeAverage(flatCandles, globalIdx - 1);
		var avg3 = ComputeAverage(flatCandles, globalIdx);

		var signal = (avg0 + 2f * avg1 + 2f * avg2 + avg3) / 6f;

		flatResults[resIndex] = new() { Time = candle.Time, Value = signal, IsFormed = 1 };
	}

	private static float ComputeAverage(ArrayView<GpuCandle> candles, int index)
	{
		var c0 = candles[index - 3];
		var c1 = candles[index - 2];
		var c2 = candles[index - 1];
		var c3 = candles[index];

		var valueUp = ((c0.Close - c0.Open) + 2f * (c1.Close - c1.Open) + 2f * (c2.Close - c2.Open) + (c3.Close - c3.Open)) / 6f;
		var valueDn = ((c0.High - c0.Low) + 2f * (c1.High - c1.Low) + 2f * (c2.High - c2.Low) + (c3.High - c3.Low)) / 6f;

		return valueDn == 0f ? valueUp : valueUp / valueDn;
	}
}

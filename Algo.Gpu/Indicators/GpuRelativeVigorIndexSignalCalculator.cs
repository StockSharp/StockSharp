namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Relative Vigor Index signal calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuRelativeVigorIndexSignalParams"/> struct.
/// </remarks>
/// <param name="length">Signal smoothing length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuRelativeVigorIndexSignalParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Signal smoothing length.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

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
	/// The CPU implementation buffers input values (extracted price) and computes a weighted average
	/// of the last Length values: (buf[0] + 2*buf[1] + 2*buf[2] + buf[3]) / 6.
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
		var signalLength = prm.Length;

		if (signalLength <= 0)
			return;

		// CPU IsFormed at Buffer.Count >= Length, i.e., bar signalLength-1
		if (candleIdx < signalLength - 1)
			return;

		var priceType = (Level1Fields)prm.PriceType;

		// CPU always uses Buffer[0..3] (the 4 oldest values in the buffer) with weights [1, 2, 2, 1] / 6,
		// regardless of buffer Length. The oldest value is at (globalIdx - signalLength + 1).
		var oldest = globalIdx - signalLength + 1;
		var v0 = ExtractPrice(flatCandles[oldest], priceType);
		var v1 = ExtractPrice(flatCandles[oldest + 1], priceType);
		var v2 = ExtractPrice(flatCandles[oldest + 2], priceType);
		var v3 = ExtractPrice(flatCandles[oldest + 3], priceType);

		var signal = (v0 + 2f * v1 + 2f * v2 + v3) / 6f;
		flatResults[resIndex] = new() { Time = candle.Time, Value = signal, IsFormed = 1 };
	}
}

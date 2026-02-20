namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Zero Lag Exponential Moving Average (ZLEMA) calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuZlemaParams"/> struct.
/// </remarks>
/// <param name="length">ZLEMA length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuZlemaParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// ZLEMA period length.
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

		if (indicator is ZeroLagExponentialMovingAverage zlema)
		{
			Unsafe.AsRef(in this).Length = zlema.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Zero Lag Exponential Moving Average (ZLEMA).
/// </summary>
public class GpuZlemaCalculator : GpuIndicatorCalculatorBase<ZeroLagExponentialMovingAverage, GpuZlemaParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuZlemaParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuZlemaCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuZlemaCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuZlemaParams>>(ZlemaParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuZlemaParams[] parameters)
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
	/// ILGPU kernel: ZLEMA computation for multiple series and parameter sets.
	/// One thread handles one (parameter, series) pair and iterates bars sequentially.
	/// </summary>
	private static void ZlemaParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuZlemaParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var length = prm.Length;
		if (length <= 0)
			length = 1;

		var lag = (length - 1) / 2;
		var alpha = 2f / (length + 1f);
		var oneMinusAlpha = 1f - alpha;
		var priceType = (Level1Fields)prm.PriceType;
		var prevZlema = 0f;

		for (var i = 0; i < len; i++)
		{
			var c = flatCandles[offset + i];
			var price = ExtractPrice(c, priceType);
			var resIndex = paramIdx * flatCandles.Length + (offset + i);
			flatResults[resIndex] = new() { Time = c.Time, Value = float.NaN, IsFormed = 0 };

			if (i < length - 1)
				continue;

			// CPU accesses buffer[lag] where buffer contains [i-length+1 .. i].
			// buffer[lag] = bar (i - length + 1 + lag) = bar (i - (length - 1 - lag)).
			var lagIdx = i - (length - 1 - lag);
			var lagPrice = lagIdx >= 0 ? ExtractPrice(flatCandles[offset + lagIdx], priceType) : price;
			var zlema = alpha * (2f * price - lagPrice) + oneMinusAlpha * prevZlema;
			prevZlema = zlema;
			flatResults[resIndex] = new() { Time = c.Time, Value = zlema, IsFormed = 1 };
		}
	}
}

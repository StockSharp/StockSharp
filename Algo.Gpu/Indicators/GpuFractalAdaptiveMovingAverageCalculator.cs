namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU FRAMA calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuFramaParams"/> struct.
/// </remarks>
/// <param name="length">FRAMA length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuFramaParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// FRAMA window length.
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

		if (indicator is FractalAdaptiveMovingAverage frama)
		{
			Unsafe.AsRef(in this).Length = frama.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Fractal Adaptive Moving Average (FRAMA).
/// </summary>
public class GpuFractalAdaptiveMovingAverageCalculator : GpuIndicatorCalculatorBase<FractalAdaptiveMovingAverage, GpuFramaParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuFramaParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuFractalAdaptiveMovingAverageCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuFractalAdaptiveMovingAverageCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuFramaParams>>(FramaParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuFramaParams[] parameters)
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
					var resIdx = p * totalSize + globalIdx;
					arr[i] = flatResults[resIdx];
				}
				result[s][p] = arr;
			}
		}

		return result;
	}

	/// <summary>
	/// ILGPU kernel: FRAMA computation for multiple series and multiple parameter sets. Results are stored as [param][globalIdx].
	/// </summary>
	private static void FramaParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuFramaParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var offset = offsets[seriesIdx];
		var prm = parameters[paramIdx];
		var length = prm.Length;
		var period = length / 3;

		var priceType = (Level1Fields)prm.PriceType;
		var totalSize = flatCandles.Length;
		var prevFrama = 0f;
		var hasPrev = false;

		for (var candleIdx = 0; candleIdx < len; candleIdx++)
		{
			var globalIdx = offset + candleIdx;
			var candle = flatCandles[globalIdx];
			var resIndex = paramIdx * totalSize + globalIdx;

			flatResults[resIndex] = new() { Time = candle.Time, Value = float.NaN, IsFormed = 0 };

			if (length <= 0 || period <= 0)
				continue;

			if (candleIdx < length - 1)
				continue;

			var start = globalIdx - length + 1;

			var n1 = CalculateDimension(flatCandles, start, period, priceType, period);
			var n2 = CalculateDimension(flatCandles, start + period, period, priceType, period);
			var remaining = length - (period * 2);
			var n3 = CalculateDimension(flatCandles, start + (period * 2), remaining, priceType, period);

			var logN1N2 = MathF.Log(n1 + n2);
			var logN3 = MathF.Log(n3);
			var d = (logN1N2 - logN3) / MathF.Log(2f);
			d = MathF.Max(MathF.Min(d, 2f), 1f);

			var price = ExtractPrice(candle, priceType);

			if (!hasPrev)
				prevFrama = 0f;

			var alpha = MathF.Exp(-4.6f * (d - 1f));
			var newFrama = alpha * price + (1f - alpha) * prevFrama;

			flatResults[resIndex] = new() { Time = candle.Time, Value = newFrama, IsFormed = 1 };
			prevFrama = newFrama;
			hasPrev = true;
		}
	}

	private static float CalculateDimension(
		ArrayView<GpuCandle> candles,
		int start,
		int count,
		Level1Fields priceType,
		int period)
	{
		if (count <= 0 || period <= 0)
			return 0f;

		var max = ExtractPrice(candles[start], priceType);
		var min = max;

		for (var i = 1; i < count; i++)
		{
			var price = ExtractPrice(candles[start + i], priceType);
			if (price > max)
				max = price;
			if (price < min)
				min = price;
		}

		return (max - min) / period;
	}
}

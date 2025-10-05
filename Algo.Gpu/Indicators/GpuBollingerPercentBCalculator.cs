namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Bollinger %b calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuBollingerPercentBParams"/> struct.
/// </remarks>
/// <param name="length">Bollinger Bands length.</param>
/// <param name="stdDevMultiplier">Standard deviation multiplier.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuBollingerPercentBParams(int length, float stdDevMultiplier, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Bollinger Bands window length.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// Standard deviation multiplier.
	/// </summary>
	public float StdDevMultiplier = stdDevMultiplier;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is BollingerPercentB percentB)
		{
			Unsafe.AsRef(in this).Length = percentB.Length;
			Unsafe.AsRef(in this).StdDevMultiplier = (float)percentB.StdDevMultiplier;
		}
	}
}

/// <summary>
/// GPU calculator for Bollinger %b indicator.
/// </summary>
public class GpuBollingerPercentBCalculator : GpuIndicatorCalculatorBase<BollingerPercentB, GpuBollingerPercentBParams, GpuIndicatorResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuBollingerPercentBParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuBollingerPercentBCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuBollingerPercentBCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuBollingerPercentBParams>>(BollingerPercentBParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuBollingerPercentBParams[] parameters)
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
		using var paramsBuffer = Accelerator.Allocate1D(parameters);
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var extent = new Index3D(parameters.Length, seriesCount, maxLen);
		_paramsSeriesKernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
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
	/// ILGPU kernel: Bollinger %b computation for multiple series and multiple parameter sets. Results are stored as [param][globalIdx].
	/// </summary>
	private static void BollingerPercentBParamsSeriesKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuBollingerPercentBParams> parameters)
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
		var L = prm.Length;
		if (L <= 0 || candleIdx < L - 1)
			return;

		var priceType = (Level1Fields)prm.PriceType;

		var sum = 0f;
		for (var j = 0; j < L; j++)
			sum += ExtractPrice(flatCandles[globalIdx - j], priceType);

		var mean = sum / L;

		var variance = 0f;
		for (var j = 0; j < L; j++)
		{
			var price = ExtractPrice(flatCandles[globalIdx - j], priceType);
			var diff = price - mean;
			variance += diff * diff;
		}

		var stdDev = (float)ILGPU.Algorithms.XMath.Sqrt(variance / L);
		var width = prm.StdDevMultiplier;
		var upperBand = mean + width * stdDev;
		var lowerBand = mean - width * stdDev;
		var bandWidth = upperBand - lowerBand;

		if (bandWidth == 0f)
			return;

		var currentPrice = ExtractPrice(candle, priceType);
		var percentB = (currentPrice - lowerBand) / bandWidth * 100f;

		flatResults[resIndex] = new() { Time = candle.Time, Value = percentB, IsFormed = 1 };
	}
}

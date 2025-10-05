namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Historical Volatility Ratio calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuHistoricalVolatilityRatioParams"/> struct.
/// </remarks>
/// <param name="shortPeriod">Short standard deviation period.</param>
/// <param name="longPeriod">Long standard deviation period.</param>
/// <param name="priceType">Price type to extract from candles.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuHistoricalVolatilityRatioParams(int shortPeriod, int longPeriod, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Short standard deviation period.
	/// </summary>
	public int ShortPeriod = shortPeriod;

	/// <summary>
	/// Long standard deviation period.
	/// </summary>
	public int LongPeriod = longPeriod;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is HistoricalVolatilityRatio hvr)
		{
			Unsafe.AsRef(in this).ShortPeriod = hvr.ShortPeriod;
			Unsafe.AsRef(in this).LongPeriod = hvr.LongPeriod;
		}
	}
}

/// <summary>
/// GPU calculator for Historical Volatility Ratio.
/// </summary>
public class GpuHistoricalVolatilityRatioCalculator : GpuIndicatorCalculatorBase<HistoricalVolatilityRatio, GpuHistoricalVolatilityRatioParams, GpuIndicatorResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuHistoricalVolatilityRatioParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuHistoricalVolatilityRatioCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuHistoricalVolatilityRatioCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuHistoricalVolatilityRatioParams>>(HistoricalVolatilityRatioKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuHistoricalVolatilityRatioParams[] parameters)
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
	/// ILGPU kernel for Historical Volatility Ratio computation. Results are stored as [param][globalIdx].
	/// </summary>
	private static void HistoricalVolatilityRatioKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuHistoricalVolatilityRatioParams> parameters)
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
		var shortLen = prm.ShortPeriod;
		var longLen = prm.LongPeriod;

		if (shortLen <= 0 || longLen <= 0)
			return;

		var maxLen = longLen > shortLen ? longLen : shortLen;
		if (candleIdx < maxLen - 1)
			return;

		var priceType = (Level1Fields)prm.PriceType;

		var shortSum = 0f;
		for (var j = 0; j < shortLen; j++)
			shortSum += ExtractPrice(flatCandles[globalIdx - j], priceType);
		var shortMean = shortSum / shortLen;

		var longSum = 0f;
		for (var j = 0; j < longLen; j++)
			longSum += ExtractPrice(flatCandles[globalIdx - j], priceType);
		var longMean = longSum / longLen;

		var shortVar = 0f;
		for (var j = 0; j < shortLen; j++)
		{
			var price = ExtractPrice(flatCandles[globalIdx - j], priceType);
			var diff = price - shortMean;
			shortVar += diff * diff;
		}
		shortVar /= shortLen;

		var longVar = 0f;
		for (var j = 0; j < longLen; j++)
		{
			var price = ExtractPrice(flatCandles[globalIdx - j], priceType);
			var diff = price - longMean;
			longVar += diff * diff;
		}
		longVar /= longLen;

		var shortSd = MathF.Sqrt(MathF.Max(shortVar, 0f));
		var longSd = MathF.Sqrt(MathF.Max(longVar, 0f));
		var ratio = longSd != 0f ? shortSd / longSd : 0f;

		flatResults[resIndex] = new() { Time = candle.Time, Value = ratio, IsFormed = 1 };
	}
}

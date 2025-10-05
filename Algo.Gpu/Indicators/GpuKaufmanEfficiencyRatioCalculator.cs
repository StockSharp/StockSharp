namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Kaufman Efficiency Ratio calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuKerParams"/> struct.
/// </remarks>
/// <param name="length">Indicator period length.</param>
/// <param name="priceType">Price type to extract from candles.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuKerParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Indicator period length.
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

		if (indicator is KaufmanEfficiencyRatio ker)
		{
			Unsafe.AsRef(in this).Length = ker.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Kaufman Efficiency Ratio indicator.
/// </summary>
public class GpuKaufmanEfficiencyRatioCalculator : GpuIndicatorCalculatorBase<KaufmanEfficiencyRatio, GpuKerParams, GpuIndicatorResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuKerParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuKaufmanEfficiencyRatioCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuKaufmanEfficiencyRatioCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuKerParams>>(KerParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuKerParams[] parameters)
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
	/// ILGPU kernel: Kaufman Efficiency Ratio computation for multiple series and parameter sets.
	/// </summary>
	private static void KerParamsSeriesKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuKerParams> parameters)
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
		var period = prm.Length;
		if (period <= 0)
			period = 1;

		if (candleIdx < period - 1)
			return;

		var priceType = (Level1Fields)prm.PriceType;
		var currentPrice = ExtractPrice(candle, priceType);
		var pastIdx = globalIdx - (period - 1);
		var pastPrice = ExtractPrice(flatCandles[pastIdx], priceType);
		var change = MathF.Abs(currentPrice - pastPrice);

		var volatility = 0f;
		var prevPrice = currentPrice;

		for (var j = 1; j < period; j++)
		{
			var price = ExtractPrice(flatCandles[globalIdx - j], priceType);
			volatility += MathF.Abs(prevPrice - price);
			prevPrice = price;
		}

		var value = volatility > 0f ? change / volatility : 0f;
		flatResults[resIndex] = new() { Time = candle.Time, Value = value, IsFormed = 1 };
	}
}

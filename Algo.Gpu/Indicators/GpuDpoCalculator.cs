namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Detrended Price Oscillator (DPO) calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuDpoParams"/> struct.
/// </remarks>
/// <param name="length">DPO length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuDpoParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// DPO SMA length.
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

		if (indicator is DetrendedPriceOscillator dpo)
		{
			Unsafe.AsRef(in this).Length = dpo.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Detrended Price Oscillator (DPO).
/// </summary>
public class GpuDpoCalculator : GpuIndicatorCalculatorBase<DetrendedPriceOscillator, GpuDpoParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<float>, ArrayView<int>, ArrayView<int>, ArrayView<GpuDpoParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuDpoCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuDpoCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
				<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<float>, ArrayView<int>, ArrayView<int>, ArrayView<GpuDpoParams>>(DpoParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuDpoParams[] parameters)
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
		using var smaCacheBuffer = Accelerator.Allocate1D<float>(totalSize * parameters.Length);
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, smaCacheBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
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
	/// ILGPU kernel: DPO computation for multiple series and parameter sets.
	/// One thread processes one (parameter, series) pair sequentially.
	/// </summary>
	private static void DpoParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<float> smaCache,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuDpoParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var L = prm.Length;
		if (L <= 0)
			L = 1;

		var lookBack = (L / 2) + 1;
		var priceType = (Level1Fields)prm.PriceType;

		float sum = 0f;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var price = ExtractPrice(candle, priceType);

			sum += price;
			if (i >= L)
				sum -= ExtractPrice(flatCandles[globalIdx - L], priceType);

			var resIndex = paramIdx * flatCandles.Length + globalIdx;
			flatResults[resIndex] = new() { Time = candle.Time, Value = float.NaN, IsFormed = 0 };

			if (i < L - 1)
				continue;

			var sma = sum / L;
			smaCache[resIndex] = sma;

			var bufferCount = i - (L - 1) + 1;
			if (bufferCount > L)
				bufferCount = L;

			if (bufferCount < L)
				continue;

			var earliestIdx = i - bufferCount + 1;
			var idxFromEarliest = bufferCount - 1 - lookBack;
			if (idxFromEarliest < 0)
				idxFromEarliest = 0;

			var smaIdx = earliestIdx + idxFromEarliest;
			var smaResIndex = paramIdx * flatCandles.Length + (offset + smaIdx);
			var pastSma = smaCache[smaResIndex];

			flatResults[resIndex] = new()
			{
				Time = candle.Time,
				Value = price - pastSma,
				IsFormed = 1
			};
		}
	}
}

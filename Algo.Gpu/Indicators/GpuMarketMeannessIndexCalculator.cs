namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Market Meanness Index calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuMarketMeannessIndexParams"/> struct.
/// </remarks>
/// <param name="length">Calculation window length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuMarketMeannessIndexParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Window length used for Market Meanness Index.
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

		if (indicator is MarketMeannessIndex mmi)
		{
			Unsafe.AsRef(in this).Length = mmi.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Market Meanness Index (MMI).
/// </summary>
public class GpuMarketMeannessIndexCalculator : GpuIndicatorCalculatorBase<MarketMeannessIndex, GpuMarketMeannessIndexParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMarketMeannessIndexParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuMarketMeannessIndexCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuMarketMeannessIndexCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMarketMeannessIndexParams>>(MarketMeannessIndexParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuMarketMeannessIndexParams[] parameters)
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
	/// ILGPU kernel: Market Meanness Index computation for multiple series and multiple parameter sets.
	/// One thread handles one (parameter, series) pair and iterates bars sequentially.
	/// </summary>
	private static void MarketMeannessIndexParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuMarketMeannessIndexParams> parameters)
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

		var priceType = (Level1Fields)prm.PriceType;
		var totalCandles = flatCandles.Length;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var resIndex = paramIdx * totalCandles + globalIdx;

			var result = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0
			};

			if (L <= 1)
			{
				result.Value = 0f;
				result.IsFormed = 1;
			}
			else if (i + 1 >= L)
			{
				var priceChanges = 0;
				var directionChanges = 0;
				var prevDirection = 0;

				var start = i - L + 1;
				var prevPrice = ExtractPrice(flatCandles[offset + start], priceType);

				for (var j = start + 1; j <= i; j++)
				{
					var currentPrice = ExtractPrice(flatCandles[offset + j], priceType);
					var diff = currentPrice - prevPrice;
					var direction = diff > 0f ? 1 : diff < 0f ? -1 : 0;

					if (direction != 0)
						priceChanges++;

					if (direction != prevDirection && prevDirection != 0)
						directionChanges++;

					prevDirection = direction;
					prevPrice = currentPrice;
				}

				result.Value = priceChanges > 0 ? 100f * directionChanges / priceChanges : 0f;
				result.IsFormed = 1;
			}

			flatResults[resIndex] = result;
		}
	}
}

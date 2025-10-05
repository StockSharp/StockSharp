namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Nick Rypock Trailing Reverse (NRTR) calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuNickRypockTrailingReverseParams"/> struct.
/// </remarks>
/// <param name="length">Indicator length.</param>
/// <param name="multiple">Multiplier value already scaled for internal NRTR calculation.</param>
/// <param name="priceType">Price type to extract from candles.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuNickRypockTrailingReverseParams(int length, float multiple, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Indicator length.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// Multiplier used in NRTR smoothing (already divided by 1000 as in indicator internals).
	/// </summary>
	public float Multiple = multiple;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is NickRypockTrailingReverse nrtr)
		{
			Unsafe.AsRef(in this).Length = nrtr.Length;
			Unsafe.AsRef(in this).Multiple = (float)(nrtr.Multiple / 1000m);
		}
	}
}

/// <summary>
/// GPU calculator for Nick Rypock Trailing Reverse (NRTR).
/// </summary>
public class GpuNickRypockTrailingReverseCalculator : GpuIndicatorCalculatorBase<NickRypockTrailingReverse, GpuNickRypockTrailingReverseParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuNickRypockTrailingReverseParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuNickRypockTrailingReverseCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuNickRypockTrailingReverseCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
				<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuNickRypockTrailingReverseParams>>(NickRypockTrailingReverseParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuNickRypockTrailingReverseParams[] parameters)
	{
		ArgumentNullException.ThrowIfNull(candlesSeries);
		ArgumentNullException.ThrowIfNull(parameters);

		if (candlesSeries.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(candlesSeries));

		if (parameters.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(parameters));

		var seriesCount = candlesSeries.Length;

		// Flatten input series
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
	/// ILGPU kernel: NRTR computation for multiple series and parameter sets.
	/// One thread handles one (parameter, series) pair sequentially.
	/// </summary>
	private static void NickRypockTrailingReverseParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuNickRypockTrailingReverseParams> parameters)
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

		var multiple = prm.Multiple;
		if (multiple <= 0f)
			multiple = 0.001f;

		var priceType = (Level1Fields)prm.PriceType;
		var flatLength = flatCandles.Length;

		var isInitialized = false;
		float k = 0f;
		float reverse = 0f;
		float highPrice = 0f;
		float lowPrice = 0f;
		var trend = 0;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var price = ExtractPrice(candle, priceType);

			if (!isInitialized)
			{
				k = price;
				highPrice = price;
				lowPrice = price;
				isInitialized = true;
			}

			k = (k + (price - k) / length) * multiple;

			var newTrend = 0;

			if (trend >= 0)
			{
				if (price > highPrice)
					highPrice = price;

				reverse = highPrice - k;

				if (price <= reverse)
				{
					newTrend = -1;
					lowPrice = price;
					reverse = lowPrice + k;
				}
				else
				{
					newTrend = 1;
				}
			}

			if (trend <= 0)
			{
				if (price < lowPrice)
					lowPrice = price;

				reverse = lowPrice + k;

				if (price >= reverse)
				{
					newTrend = 1;
					highPrice = price;
					reverse = highPrice - k;
				}
				else
				{
					newTrend = -1;
				}
			}

			if (newTrend != 0)
				trend = newTrend;

			var resIndex = paramIdx * flatLength + globalIdx;
			flatResults[resIndex] = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = reverse,
				IsFormed = (byte)((i + 1) >= length ? 1 : 0)
			};
		}
	}
}

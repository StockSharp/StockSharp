namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Kaufman Adaptive Moving Average (KAMA) calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuKamaParams"/> struct.
/// </remarks>
/// <param name="length">KAMA efficiency ratio window length.</param>
/// <param name="fastPeriod">Fast smoothing constant period.</param>
/// <param name="slowPeriod">Slow smoothing constant period.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuKamaParams(int length, int fastPeriod, int slowPeriod, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// KAMA efficiency ratio window length.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// Fast smoothing constant period.
	/// </summary>
	public int FastPeriod = fastPeriod;

	/// <summary>
	/// Slow smoothing constant period.
	/// </summary>
	public int SlowPeriod = slowPeriod;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is KaufmanAdaptiveMovingAverage kama)
		{
			Unsafe.AsRef(in this).Length = kama.Length;
			Unsafe.AsRef(in this).FastPeriod = kama.FastSCPeriod;
			Unsafe.AsRef(in this).SlowPeriod = kama.SlowSCPeriod;
		}
	}
}

/// <summary>
/// GPU calculator for Kaufman Adaptive Moving Average (KAMA).
/// </summary>
public class GpuKamaCalculator : GpuIndicatorCalculatorBase<KaufmanAdaptiveMovingAverage, GpuKamaParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuKamaParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuKamaCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuKamaCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuKamaParams>>(KamaParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuKamaParams[] parameters)
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
	/// ILGPU kernel: KAMA computation for multiple series and parameter sets.
	/// One thread processes one (parameter, series) pair sequentially.
	/// </summary>
	private static void KamaParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuKamaParams> parameters)
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

		var fastPeriod = prm.FastPeriod;
		if (fastPeriod <= 0)
			fastPeriod = 1;

		var slowPeriod = prm.SlowPeriod;
		if (slowPeriod <= 0)
			slowPeriod = 1;

		var priceType = (Level1Fields)prm.PriceType;
		var fastSC = 2f / (fastPeriod + 1f);
		var slowSC = 2f / (slowPeriod + 1f);

		var prevValue = 0f;
		var isInitialized = false;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var price = ExtractPrice(candle, priceType);

			var resIndex = paramIdx * flatCandles.Length + (offset + i);
			var result = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0
			};

			if (i >= length)
			{
				if (!isInitialized)
				{
					result.Value = price;
					result.IsFormed = 1;
					isInitialized = true;
					prevValue = price;
				}
				else
				{
					var pastPrice = ExtractPrice(flatCandles[offset + i - length], priceType);
					var direction = price - pastPrice;

					var volatility = 0f;
					var prev = pastPrice;
					for (var j = i - length + 1; j <= i; j++)
					{
						var current = ExtractPrice(flatCandles[offset + j], priceType);
						volatility += MathF.Abs(current - prev);
						prev = current;
					}

					if (volatility <= 0f)
						volatility = 0.00001f;

					var er = MathF.Abs(direction / volatility);
					var ssc = er * (fastSC - slowSC) + slowSC;
					var smooth = ssc * ssc;
					var curValue = (price - prevValue) * smooth + prevValue;

					result.Value = curValue;
					result.IsFormed = 1;
					prevValue = curValue;
				}
			}

			flatResults[resIndex] = result;
		}
	}
}

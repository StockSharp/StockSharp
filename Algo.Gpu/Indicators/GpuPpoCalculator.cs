namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Percentage Price Oscillator (PPO) calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuPpoParams"/> struct.
/// </remarks>
/// <param name="shortLength">Short EMA length.</param>
/// <param name="longLength">Long EMA length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuPpoParams(int shortLength, int longLength, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Short EMA period length.
	/// </summary>
	public int ShortLength = shortLength;

	/// <summary>
	/// Long EMA period length.
	/// </summary>
	public int LongLength = longLength;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is PercentagePriceOscillator ppo)
		{
			Unsafe.AsRef(in this).ShortLength = ppo.ShortPeriod;
			Unsafe.AsRef(in this).LongLength = ppo.LongPeriod;
		}
	}
}

/// <summary>
/// GPU calculator for Percentage Price Oscillator (PPO).
/// </summary>
public class GpuPpoCalculator : GpuIndicatorCalculatorBase<PercentagePriceOscillator, GpuPpoParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuPpoParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuPpoCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuPpoCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
		<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuPpoParams>>(PpoParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuPpoParams[] parameters)
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
	/// ILGPU kernel: PPO computation for multiple series and multiple parameter sets.
	/// One thread handles one (parameter, series) pair and iterates bars sequentially.
	/// </summary>
	private static void PpoParamsSeriesKernel(
	Index2D index,
	ArrayView<GpuCandle> flatCandles,
	ArrayView<GpuIndicatorResult> flatResults,
	ArrayView<int> offsets,
	ArrayView<int> lengths,
	ArrayView<GpuPpoParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var param = parameters[paramIdx];
		var shortLength = param.ShortLength;
		var longLength = param.LongLength;
		if (shortLength <= 0)
			shortLength = 1;
		if (longLength <= 0)
			longLength = 1;

		var priceType = (Level1Fields)param.PriceType;
		var shortMultiplier = 2f / (shortLength + 1f);
		var longMultiplier = 2f / (longLength + 1f);

		var shortSum = 0f;
		var longSum = 0f;
		var shortEma = 0f;
		var longEma = 0f;
		var shortCount = 0;
		var longCount = 0;
		var shortFormed = false;
		var longFormed = false;

		for (var i = 0; i < len; i++)
		{
			var idx = offset + i;
			var candle = flatCandles[idx];
			var price = ExtractPrice(candle, priceType);

			var resIndex = paramIdx * flatCandles.Length + idx;
			flatResults[resIndex] = new()
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0
			};

			if (!shortFormed)
			{
				shortSum += price;
				shortCount++;
				if (shortCount == shortLength)
				{
					shortEma = shortSum / shortLength;
					shortFormed = true;
				}
			}
			else
			{
				shortEma = (price - shortEma) * shortMultiplier + shortEma;
			}

			if (!longFormed)
			{
				longSum += price;
				longCount++;
				if (longCount == longLength)
				{
					longEma = longSum / longLength;
					longFormed = true;
				}
			}
			else
			{
				longEma = (price - longEma) * longMultiplier + longEma;
			}

			if (shortFormed && longFormed)
			{
				var denom = longEma;
				var value = denom == 0f ? 0f : ((shortEma - denom) / denom) * 100f;
				flatResults[resIndex] = new()
				{
					Time = candle.Time,
					Value = value,
					IsFormed = 1
				};
			}
		}
	}
}

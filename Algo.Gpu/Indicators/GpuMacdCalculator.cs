namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU MACD calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuMacdParams"/> struct.
/// </remarks>
/// <param name="longLength">Long EMA length.</param>
/// <param name="shortLength">Short EMA length.</param>
/// <param name="priceType">Price type to extract from candles.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuMacdParams(int longLength, int shortLength, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Long EMA length.
	/// </summary>
	public int LongLength = longLength;

	/// <summary>
	/// Short EMA length.
	/// </summary>
	public int ShortLength = shortLength;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is MovingAverageConvergenceDivergence macd)
		{
			Unsafe.AsRef(in this).LongLength = macd.LongMa.Length;
			Unsafe.AsRef(in this).ShortLength = macd.ShortMa.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Moving Average Convergence Divergence (MACD).
/// </summary>
public class GpuMacdCalculator : GpuIndicatorCalculatorBase<MovingAverageConvergenceDivergence, GpuMacdParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMacdParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuMacdCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuMacdCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMacdParams>>(MacdParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuMacdParams[] parameters)
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
	/// ILGPU kernel: MACD computation for multiple series and parameter sets.
	/// </summary>
	private static void MacdParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuMacdParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var shortLength = prm.ShortLength;
		var longLength = prm.LongLength;

		if (shortLength <= 0)
			shortLength = 1;
		if (longLength <= 0)
			longLength = 1;

		var priceType = (Level1Fields)prm.PriceType;

		var shortMultiplier = 2f / (shortLength + 1f);
		var longMultiplier = 2f / (longLength + 1f);

		float shortSum = 0f;
		float longSum = 0f;
		float shortEma = 0f;
		float longEma = 0f;

		var shortFormed = shortLength == 1;
		var longFormed = longLength == 1;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var price = ExtractPrice(candle, priceType);

			var resIndex = paramIdx * flatCandles.Length + globalIdx;
			flatResults[resIndex] = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0
			};

			if (!shortFormed)
			{
				shortSum += price;

				if (i == shortLength - 1)
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

				if (i == longLength - 1)
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
				flatResults[resIndex] = new GpuIndicatorResult
				{
					Time = candle.Time,
					Value = shortEma - longEma,
					IsFormed = 1
				};
			}
		}
	}
}

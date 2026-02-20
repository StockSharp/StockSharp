namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Schaff Trend Cycle calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuSchaffTrendCycleParams"/> struct.
/// </remarks>
/// <param name="length">EMA length applied to stochastic values.</param>
/// <param name="macdShortLength">MACD short EMA length.</param>
/// <param name="macdLongLength">MACD long EMA length.</param>
/// <param name="macdSignalLength">MACD signal EMA length.</param>
/// <param name="stochasticLength">Stochastic length.</param>
/// <param name="priceType">Price type to extract.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuSchaffTrendCycleParams(
        int length,
        int macdShortLength,
        int macdLongLength,
        int macdSignalLength,
        int stochasticLength,
        byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// EMA length used for final smoothing and buffer size.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// MACD short EMA length.
	/// </summary>
	public int MacdShortLength = macdShortLength;

	/// <summary>
	/// MACD long EMA length.
	/// </summary>
	public int MacdLongLength = macdLongLength;

	/// <summary>
	/// MACD signal EMA length.
	/// </summary>
	public int MacdSignalLength = macdSignalLength;

	/// <summary>
	/// Stochastic length.
	/// </summary>
	public int StochasticLength = stochasticLength;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is SchaffTrendCycle stc)
		{
			Unsafe.AsRef(in this).Length = stc.Length;
			Unsafe.AsRef(in this).MacdShortLength = stc.Macd.Macd.ShortMa.Length;
			Unsafe.AsRef(in this).MacdLongLength = stc.Macd.Macd.LongMa.Length;
			Unsafe.AsRef(in this).MacdSignalLength = stc.Macd.SignalMa.Length;
			Unsafe.AsRef(in this).StochasticLength = stc.StochasticK.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Schaff Trend Cycle (STC).
/// </summary>
public class GpuSchaffTrendCycleCalculator : GpuIndicatorCalculatorBase<SchaffTrendCycle, GpuSchaffTrendCycleParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<float>, ArrayView<int>, ArrayView<int>, ArrayView<GpuSchaffTrendCycleParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuSchaffTrendCycleCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuSchaffTrendCycleCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<float>, ArrayView<int>, ArrayView<int>, ArrayView<GpuSchaffTrendCycleParams>>(SchaffTrendCycleKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuSchaffTrendCycleParams[] parameters)
	{
		ArgumentNullException.ThrowIfNull(candlesSeries);
		ArgumentNullException.ThrowIfNull(parameters);

		if (candlesSeries.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(candlesSeries));

		if (parameters.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(parameters));

		var seriesCount = candlesSeries.Length;

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
		using var normHistoryBuffer = Accelerator.Allocate1D<float>(totalSize * parameters.Length);
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, normHistoryBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

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
	/// ILGPU kernel computing Schaff Trend Cycle for multiple series and parameter sets.
	/// </summary>
	private static void SchaffTrendCycleKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<float> normHistory,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuSchaffTrendCycleParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var totalSize = flatCandles.Length;
		var offset = offsets[seriesIdx];
		var baseResultIndex = paramIdx * totalSize;

		var prm = parameters[paramIdx];
		var priceType = (Level1Fields)prm.PriceType;

		var length = prm.Length <= 0 ? 1 : prm.Length;
		var macdShortLength = prm.MacdShortLength <= 0 ? 1 : prm.MacdShortLength;
		var macdLongLength = prm.MacdLongLength <= 0 ? 1 : prm.MacdLongLength;
		var macdSignalLength = prm.MacdSignalLength <= 0 ? 1 : prm.MacdSignalLength;
		var stochasticLength = prm.StochasticLength <= 0 ? 1 : prm.StochasticLength;

		var shortMultiplier = 2f / (macdShortLength + 1f);
		var longMultiplier = 2f / (macdLongLength + 1f);
		var signalMultiplier = 2f / (macdSignalLength + 1f);
		var emaMultiplier = 2f / (length + 1f);

		// CPU EMAs use Buffer.Sum / Length initialization (always divide by Length, not count)
		float shortSum = 0f;
		float longSum = 0f;
		float signalSum = 0f;

		float shortEma = 0f;
		float longEma = 0f;
		float signalEma = 0f;
		float emaValue = 0f;
		float emaSum = 0f;

		int shortCount = 0;
		int longCount = 0;
		int signalCount = 0;
		int stochValidCount = 0;
		int emaCount = 0;

		float prevStoch = 0f;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var resIndex = baseResultIndex + globalIdx;

			normHistory[resIndex] = float.NaN;
			flatResults[resIndex] = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0,
			};

			var price = ExtractPrice(candle, priceType);

			// Short EMA: produce values from bar 0 using CPU-matching initialization
			shortCount++;
			shortSum += price;
			if (shortCount <= macdShortLength)
				shortEma = shortSum / macdShortLength;
			else
				shortEma = (price - shortEma) * shortMultiplier + shortEma;

			// Long EMA: produce values from bar 0
			longCount++;
			longSum += price;
			if (longCount <= macdLongLength)
				longEma = longSum / macdLongLength;
			else
				longEma = (price - longEma) * longMultiplier + longEma;

			// MACD available from bar 0
			var macd = shortEma - longEma;

			// CPU: In Sequence mode, SignalMa only starts receiving MACD values
			// AFTER the inner MACD is formed (LongMa.IsFormed at bar macdLongLength-1).
			if (i >= macdLongLength - 1)
			{
				signalCount++;
				signalSum += macd;
				if (signalCount <= macdSignalLength)
					signalEma = signalSum / macdSignalLength;
				else
					signalEma = (macd - signalEma) * signalMultiplier + signalEma;
			}

			// CPU: MACD Signal formed when both MACD and SignalMa are formed.
			// SignalMa gets first value at bar macdLongLength-1, formed after macdSignalLength values.
			if (signalCount < macdSignalLength)
				continue;

			var macdHist = macd - signalEma;

			// Price buffer min/max over last `length` bars
			var priceMin = price;
			var priceMax = price;
			var start = i + 1 < length ? 0 : i + 1 - length;
			for (var j = start; j <= i; j++)
			{
				var pr = ExtractPrice(flatCandles[offset + j], priceType);
				if (pr < priceMin)
					priceMin = pr;
				if (pr > priceMax)
					priceMax = pr;
			}

			// First stochastic: normalize macdHist using price range
			var denom = priceMax - priceMin;
			float norm;
			if (denom == 0f)
			{
				norm = float.NaN;
			}
			else
			{
				norm = (macdHist - priceMin) / denom;
				normHistory[resIndex] = norm;
			}

			// Second stochastic via StochasticK(stochasticLength): min/max of norm values
			float stoch;
			if (float.IsNaN(norm))
			{
				stoch = prevStoch;
			}
			else
			{
				var needed = stochasticLength;
				var considered = 0;
				var hasNorm = false;
				float minNorm = 0f;
				float maxNorm = 0f;

				for (var j = i; j >= 0 && considered < needed; j--)
				{
					var idx = baseResultIndex + offset + j;
					var value = normHistory[idx];
					if (float.IsNaN(value))
						continue;

					if (!hasNorm)
					{
						minNorm = value;
						maxNorm = value;
						hasNorm = true;
					}
					else
					{
						if (value < minNorm)
							minNorm = value;
						if (value > maxNorm)
							maxNorm = value;
					}

					considered++;
				}

				if (!hasNorm)
				{
					stoch = prevStoch;
				}
				else
				{
					var stochDen = maxNorm - minNorm;
					stoch = stochDen == 0f ? 0f : 100f * (norm - minNorm) / stochDen;
					stochValidCount++;
				}
			}

			prevStoch = stoch;

			// CPU: if (!StochasticK.IsFormed) return null
			// StochasticK formed after stochasticLength values
			if (stochValidCount < stochasticLength)
				continue;

			// Final EMA smoothing (CPU EMA uses Buffer.Sum/Length initialization)
			emaCount++;
			if (emaCount <= length)
			{
				emaSum += stoch;
				emaValue = emaSum / length;
			}
			else
			{
				emaValue = (stoch - emaValue) * emaMultiplier + emaValue;
			}

			// CPU STC formed: Macd.IsFormed && StochasticK.IsFormed && base(EMA).CalcIsFormed
			// No one-bar delay: DecimalLengthIndicator creates DecimalIndicatorValue AFTER
			// OnProcessDecimal, so IsFormed captures post-processing state.
			var isFormed = (byte)(emaCount >= length ? 1 : 0);

			flatResults[resIndex] = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = emaValue,
				IsFormed = isFormed,
			};
		}
	}
}

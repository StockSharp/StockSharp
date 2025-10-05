namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Percentage Price Oscillator Histogram calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuPercentagePriceOscillatorHistogramParams"/> struct.
/// </remarks>
/// <param name="shortLength">Short EMA length.</param>
/// <param name="longLength">Long EMA length.</param>
/// <param name="signalLength">Signal EMA length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuPercentagePriceOscillatorHistogramParams(int shortLength, int longLength, int signalLength, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Short EMA length.
	/// </summary>
	public int ShortLength = shortLength;

	/// <summary>
	/// Long EMA length.
	/// </summary>
	public int LongLength = longLength;

	/// <summary>
	/// Signal EMA length.
	/// </summary>
	public int SignalLength = signalLength;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		var priceTypeValue = indicator.Source ?? Level1Fields.ClosePrice;

		if (indicator is PercentagePriceOscillatorHistogram histogram)
		{
			var ppo = histogram.Ppo;
			var signal = histogram.SignalMa;

			if (ppo.Source.HasValue)
				priceTypeValue = ppo.Source.Value;

			Unsafe.AsRef(in this).ShortLength = ppo.ShortPeriod;
			Unsafe.AsRef(in this).LongLength = ppo.LongPeriod;
			Unsafe.AsRef(in this).SignalLength = signal.Length;
		}
		else if (indicator is PercentagePriceOscillator ppo)
		{
			if (ppo.Source.HasValue)
				priceTypeValue = ppo.Source.Value;

			Unsafe.AsRef(in this).ShortLength = ppo.ShortPeriod;
			Unsafe.AsRef(in this).LongLength = ppo.LongPeriod;
		}

		Unsafe.AsRef(in this).PriceType = (byte)priceTypeValue;
	}
}

/// <summary>
/// GPU result for Percentage Price Oscillator Histogram calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuPercentagePriceOscillatorHistogramResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTimeOffset.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// PPO value.
	/// </summary>
	public float Ppo;

	/// <summary>
	/// Signal EMA value.
	/// </summary>
	public float Signal;

	/// <summary>
	/// Indicator formed flag.
	/// </summary>
	public byte IsFormed;

	/// <summary>
	/// PPO inner indicator formed flag.
	/// </summary>
	public byte PpoIsFormed;

	/// <summary>
	/// Signal EMA inner indicator formed flag.
	/// </summary>
	public byte SignalIsFormed;

	readonly long IGpuIndicatorResult.Time => Time;
	readonly byte IGpuIndicatorResult.IsFormed => IsFormed;

	/// <inheritdoc />
	public readonly IIndicatorValue ToValue(IIndicator indicator)
	{
		var time = this.GetTime();
		var isFormed = this.GetIsFormed();
		var histogram = (PercentagePriceOscillatorHistogram)indicator;

		var value = new PercentagePriceOscillatorHistogramValue(histogram, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
			IsEmpty = Ppo.IsNaN() && Signal.IsNaN(),
		};

		var ppoIndicator = histogram.Ppo;
		IIndicatorValue ppoValue = Ppo.IsNaN()
			? new DecimalIndicatorValue(ppoIndicator, time)
			: new DecimalIndicatorValue(ppoIndicator, (decimal)Ppo, time);
		ppoValue.IsFinal = true;
		ppoValue.IsFormed = PpoIsFormed != 0;
		value.Add(ppoIndicator, ppoValue);

		var signalIndicator = histogram.SignalMa;
		IIndicatorValue signalValue = Signal.IsNaN()
			? new DecimalIndicatorValue(signalIndicator, time)
			: new DecimalIndicatorValue(signalIndicator, (decimal)Signal, time);
		signalValue.IsFinal = true;
		signalValue.IsFormed = SignalIsFormed != 0;
		value.Add(signalIndicator, signalValue);

		return value;
	}
}

/// <summary>
/// GPU calculator for Percentage Price Oscillator Histogram indicator.
/// </summary>
public class GpuPercentagePriceOscillatorHistogramCalculator : GpuIndicatorCalculatorBase<PercentagePriceOscillatorHistogram, GpuPercentagePriceOscillatorHistogramParams, GpuPercentagePriceOscillatorHistogramResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuPercentagePriceOscillatorHistogramResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuPercentagePriceOscillatorHistogramParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuPercentagePriceOscillatorHistogramCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuPercentagePriceOscillatorHistogramCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuPercentagePriceOscillatorHistogramResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuPercentagePriceOscillatorHistogramParams>>(PpoHistogramParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuPercentagePriceOscillatorHistogramResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuPercentagePriceOscillatorHistogramParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuPercentagePriceOscillatorHistogramResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuPercentagePriceOscillatorHistogramResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuPercentagePriceOscillatorHistogramResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuPercentagePriceOscillatorHistogramResult[len];
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
	/// ILGPU kernel: PPO histogram computation for multiple series and parameter sets.
	/// One thread handles one (parameter, series) pair and iterates sequentially over bars.
	/// </summary>
	private static void PpoHistogramParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuPercentagePriceOscillatorHistogramResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuPercentagePriceOscillatorHistogramParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var priceType = (Level1Fields)prm.PriceType;

		var shortLen = prm.ShortLength;
		if (shortLen <= 0)
			shortLen = 1;

		var longLen = prm.LongLength;
		if (longLen <= 0)
			longLen = 1;

		var signalLen = prm.SignalLength;
		if (signalLen <= 0)
			signalLen = 1;

		var shortAlpha = 2f / (shortLen + 1f);
		var longAlpha = 2f / (longLen + 1f);
		var signalAlpha = 2f / (signalLen + 1f);

		float shortSum = 0f, longSum = 0f, signalSum = 0f;
		float shortEma = 0f, longEma = 0f, signalEma = 0f;
		int shortCount = 0, longCount = 0, signalCount = 0;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var price = ExtractPrice(candle, priceType);

			shortCount++;
			if (shortCount <= shortLen)
			{
				shortSum += price;
				if (shortCount >= shortLen)
					shortEma = shortSum / shortLen;
			}
			else
			{
				shortEma = shortEma + shortAlpha * (price - shortEma);
			}

			longCount++;
			if (longCount <= longLen)
			{
				longSum += price;
				if (longCount >= longLen)
					longEma = longSum / longLen;
			}
			else
			{
				longEma = longEma + longAlpha * (price - longEma);
			}

			var resIndex = paramIdx * flatCandles.Length + globalIdx;
			var result = new GpuPercentagePriceOscillatorHistogramResult
			{
				Time = candle.Time,
				Ppo = float.NaN,
				Signal = float.NaN,
				IsFormed = 0,
				PpoIsFormed = 0,
				SignalIsFormed = 0,
			};

			if (longCount >= longLen)
			{
				result.PpoIsFormed = 1;
				var denominator = longEma;
				var ppoValue = denominator != 0f ? ((shortEma - denominator) / denominator) * 100f : 0f;
				result.Ppo = ppoValue;

				signalCount++;
				signalSum += ppoValue;

				if (signalCount <= signalLen)
				{
					var avg = signalSum / signalLen;
					signalEma = avg;
					result.Signal = avg;
					if (signalCount >= signalLen)
					{
						result.SignalIsFormed = 1;
						result.IsFormed = 1;
					}
				}
				else
				{
					signalEma = signalEma + signalAlpha * (ppoValue - signalEma);
					result.Signal = signalEma;
					result.SignalIsFormed = 1;
					result.IsFormed = 1;
				}
			}

			flatResults[resIndex] = result;
		}
	}
}

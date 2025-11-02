namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Percentage Price Oscillator with signal calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuPercentagePriceOscillatorSignalParams"/> struct.
/// </remarks>
/// <param name="shortPeriod">Short period for fast EMA.</param>
/// <param name="longPeriod">Long period for slow EMA.</param>
/// <param name="signalLength">Signal EMA length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuPercentagePriceOscillatorSignalParams(int shortPeriod, int longPeriod, int signalLength, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Short period for fast EMA.
	/// </summary>
	public int ShortPeriod = shortPeriod;

	/// <summary>
	/// Long period for slow EMA.
	/// </summary>
	public int LongPeriod = longPeriod;

	/// <summary>
	/// Signal EMA period.
	/// </summary>
	public int SignalLength = signalLength;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is PercentagePriceOscillatorSignal ppoSignal)
		{
			var ppo = ppoSignal.Ppo;

			Unsafe.AsRef(in this).ShortPeriod = ppo.ShortPeriod;
			Unsafe.AsRef(in this).LongPeriod = ppo.LongPeriod;
			Unsafe.AsRef(in this).SignalLength = ppoSignal.SignalMa.Length;
		}
	}
}

/// <summary>
/// GPU result for Percentage Price Oscillator with signal calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuPercentagePriceOscillatorSignalResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
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
	/// Indicator formation flag (byte for GPU compatibility).
	/// </summary>
	public byte IsFormed;

	/// <summary>
	/// PPO formation flag (byte for GPU compatibility).
	/// </summary>
	public byte IsPpoFormed;

	/// <summary>
	/// Signal formation flag (byte for GPU compatibility).
	/// </summary>
	public byte IsSignalFormed;

	readonly long IGpuIndicatorResult.Time => Time;

	readonly byte IGpuIndicatorResult.IsFormed => IsFormed;

	/// <inheritdoc />
	public readonly IIndicatorValue ToValue(IIndicator indicator)
	{
		var time = this.GetTime();
		var overallFormed = this.GetIsFormed();
		var ppoIndicator = (PercentagePriceOscillatorSignal)indicator;

		var result = new PercentagePriceOscillatorSignalValue(ppoIndicator, time)
		{
			IsFinal = true,
			IsFormed = overallFormed,
		};

		IIndicatorValue ppoValue;

		if (IsPpoFormed == 0 || float.IsNaN(Ppo))
		{
			ppoValue = new DecimalIndicatorValue(ppoIndicator.Ppo, time)
			{
				IsFinal = true,
				IsFormed = false,
			};
		}
		else
		{
			ppoValue = new DecimalIndicatorValue(ppoIndicator.Ppo, (decimal)Ppo, time)
			{
				IsFinal = true,
				IsFormed = true,
			};
		}

		var signalValue = new DecimalIndicatorValue(ppoIndicator.SignalMa, (decimal)Signal, time)
		{
			IsFinal = true,
			IsFormed = IsSignalFormed != 0,
		};

		result.Add(ppoIndicator.Ppo, ppoValue);
		result.Add(ppoIndicator.SignalMa, signalValue);

		return result;
	}
}

/// <summary>
/// GPU calculator for Percentage Price Oscillator with signal line.
/// </summary>
public class GpuPercentagePriceOscillatorSignalCalculator : GpuIndicatorCalculatorBase<PercentagePriceOscillatorSignal, GpuPercentagePriceOscillatorSignalParams, GpuPercentagePriceOscillatorSignalResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuPercentagePriceOscillatorSignalResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuPercentagePriceOscillatorSignalParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuPercentagePriceOscillatorSignalCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuPercentagePriceOscillatorSignalCalculator(Context context, Accelerator accelerator)
	: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
		<Index2D, ArrayView<GpuCandle>, ArrayView<GpuPercentagePriceOscillatorSignalResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuPercentagePriceOscillatorSignalParams>>(PercentagePriceOscillatorSignalParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuPercentagePriceOscillatorSignalResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuPercentagePriceOscillatorSignalParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuPercentagePriceOscillatorSignalResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuPercentagePriceOscillatorSignalResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuPercentagePriceOscillatorSignalResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuPercentagePriceOscillatorSignalResult[len];
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
	/// ILGPU kernel: PPO with signal computation for multiple series and parameter sets.
	/// One thread handles one (parameter, series) pair and iterates bars sequentially.
	/// </summary>
	private static void PercentagePriceOscillatorSignalParamsSeriesKernel(
	Index2D index,
	ArrayView<GpuCandle> flatCandles,
	ArrayView<GpuPercentagePriceOscillatorSignalResult> flatResults,
	ArrayView<int> offsets,
	ArrayView<int> lengths,
	ArrayView<GpuPercentagePriceOscillatorSignalParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
		return;

		var prm = parameters[paramIdx];
		var shortLength = prm.ShortPeriod <= 0 ? 1 : prm.ShortPeriod;
		var longLength = prm.LongPeriod <= 0 ? 1 : prm.LongPeriod;
		var signalLength = prm.SignalLength <= 0 ? 1 : prm.SignalLength;
		var shortAlpha = 2f / (shortLength + 1f);
		var longAlpha = 2f / (longLength + 1f);
		var signalAlpha = 2f / (signalLength + 1f);
		var priceType = (Level1Fields)prm.PriceType;

		float shortSum = 0f;
		float longSum = 0f;
		float shortEma = 0f;
		float longEma = 0f;
		var shortInitialized = shortLength <= 1;
		var longInitialized = longLength <= 1;

		float signalSum = 0f;
		float signalEma = 0f;
		var signalCount = signalLength <= 1 ? signalLength : 0;

		if (shortInitialized)
		shortEma = ExtractPrice(flatCandles[offset], priceType);

		if (longInitialized)
		longEma = ExtractPrice(flatCandles[offset], priceType);

		if (signalLength <= 1)
		signalEma = 0f;

		for (var i = 0; i < len; i++)
		{
			var c = flatCandles[offset + i];
			var price = ExtractPrice(c, priceType);

			var resIndex = paramIdx * flatCandles.Length + (offset + i);
			var result = new GpuPercentagePriceOscillatorSignalResult
			{
				Time = c.Time,
				Ppo = float.NaN,
				Signal = 0f,
				IsFormed = 0,
				IsPpoFormed = 0,
				IsSignalFormed = 0,
			};

			if (!shortInitialized)
			{
				shortSum += price;
				if (i == shortLength - 1)
				{
					shortEma = shortSum / shortLength;
					shortInitialized = true;
				}
			}
			else
			{
				shortEma += (price - shortEma) * shortAlpha;
			}

			if (!longInitialized)
			{
				longSum += price;
				if (i == longLength - 1)
				{
					longEma = longSum / longLength;
					longInitialized = true;
				}
			}
			else
			{
				longEma += (price - longEma) * longAlpha;
			}

			var ppoFormed = shortInitialized && longInitialized;

			if (ppoFormed)
			{
				var denom = longEma;
				var ppo = denom == 0f ? 0f : (shortEma - denom) / denom * 100f;

				result.Ppo = ppo;
				result.IsPpoFormed = 1;

				if (signalLength <= 1)
				{
					signalEma = ppo;
					signalCount = signalLength;
					result.Signal = ppo;
					result.IsSignalFormed = 1;
					result.IsFormed = 1;
				}
				else if (signalCount < signalLength)
				{
					signalCount++;
					signalSum += ppo;
					var signalValue = signalSum / signalLength;
					result.Signal = signalValue;

					if (signalCount == signalLength)
					{
						signalEma = signalValue;
						result.IsSignalFormed = 1;
						result.IsFormed = 1;
					}
				}
				else
				{
					signalEma += (ppo - signalEma) * signalAlpha;
					result.Signal = signalEma;
					result.IsSignalFormed = 1;
					result.IsFormed = 1;
				}
			}

			flatResults[resIndex] = result;
		}
	}
}

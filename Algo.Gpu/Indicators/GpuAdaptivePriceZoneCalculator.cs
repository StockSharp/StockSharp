namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Adaptive Price Zone (APZ) calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuAdaptivePriceZoneParams"/> struct.
/// </remarks>
/// <param name="period">Indicator period.</param>
/// <param name="bandPercentage">Band percentage multiplier.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuAdaptivePriceZoneParams(int period, float bandPercentage, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Indicator period.
	/// </summary>
	public int Period = period;

	/// <summary>
	/// Band percentage multiplier.
	/// </summary>
	public float BandPercentage = bandPercentage;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is AdaptivePriceZone apz)
		{
			Unsafe.AsRef(in this).Period = apz.Period;
			Unsafe.AsRef(in this).BandPercentage = (float)apz.BandPercentage;
		}
	}
}

/// <summary>
/// Complex GPU result for Adaptive Price Zone (APZ) calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuAdaptivePriceZoneResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTimeOffset.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Moving average value.
	/// </summary>
	public float MovingAverage;

	/// <summary>
	/// Upper band value.
	/// </summary>
	public float UpperBand;

	/// <summary>
	/// Lower band value.
	/// </summary>
	public float LowerBand;

	/// <summary>
	/// Is indicator formed (byte to be GPU-friendly).
	/// </summary>
	public byte IsFormed;

	readonly long IGpuIndicatorResult.Time => Time;
	readonly byte IGpuIndicatorResult.IsFormed => IsFormed;

	/// <inheritdoc />
	public readonly IIndicatorValue ToValue(IIndicator indicator)
	{
		var time = this.GetTime();
		var isFormed = this.GetIsFormed();
		var apz = (AdaptivePriceZone)indicator;

		if (MovingAverage.IsNaN() || UpperBand.IsNaN() || LowerBand.IsNaN())
		{
			return new AdaptivePriceZoneValue(apz, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		var result = new AdaptivePriceZoneValue(apz, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		var maValue = new DecimalIndicatorValue(apz.MovingAverage, (decimal)MovingAverage, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		var upperValue = new DecimalIndicatorValue(apz.UpperBand, (decimal)UpperBand, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		var lowerValue = new DecimalIndicatorValue(apz.LowerBand, (decimal)LowerBand, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		result.Add(apz.MovingAverage, maValue);
		result.Add(apz.UpperBand, upperValue);
		result.Add(apz.LowerBand, lowerValue);

		return result;
	}
}

/// <summary>
/// GPU calculator for Adaptive Price Zone (APZ).
/// </summary>
public class GpuAdaptivePriceZoneCalculator : GpuIndicatorCalculatorBase<AdaptivePriceZone, GpuAdaptivePriceZoneParams, GpuAdaptivePriceZoneResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuAdaptivePriceZoneResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuAdaptivePriceZoneParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuAdaptivePriceZoneCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuAdaptivePriceZoneCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuAdaptivePriceZoneResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuAdaptivePriceZoneParams>>(AdaptivePriceZoneParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuAdaptivePriceZoneResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuAdaptivePriceZoneParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuAdaptivePriceZoneResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuAdaptivePriceZoneResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuAdaptivePriceZoneResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuAdaptivePriceZoneResult[len];
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
	/// ILGPU kernel: Adaptive Price Zone computation for multiple series and parameter sets.
	/// </summary>
	private static void AdaptivePriceZoneParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuAdaptivePriceZoneResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuAdaptivePriceZoneParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var period = prm.Period;
		if (period <= 0)
			period = 1;

		var alpha = 2f / (period + 1f);
		var priceType = (Level1Fields)prm.PriceType;
		var bandPercentage = prm.BandPercentage;

		float ema = 0f;
		var emaInitialized = false;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var price = ExtractPrice(candle, priceType);
			var resIndex = paramIdx * flatCandles.Length + globalIdx;

			var result = new GpuAdaptivePriceZoneResult
			{
				Time = candle.Time,
				MovingAverage = float.NaN,
				UpperBand = float.NaN,
				LowerBand = float.NaN,
				IsFormed = 0,
			};

			if (i >= period - 1)
			{
				float sum = 0f;
				for (var j = 0; j < period; j++)
					sum += ExtractPrice(flatCandles[globalIdx - j], priceType);

				var sma = sum / period;

				float variance = 0f;
				for (var j = 0; j < period; j++)
				{
					var windowPrice = ExtractPrice(flatCandles[globalIdx - j], priceType);
					var diff = windowPrice - sma;
					variance += diff * diff;
				}

				var stdDev = MathF.Sqrt(variance / period);

				if (!emaInitialized)
				{
					ema = sma;
					emaInitialized = true;
				}
				else
				{
					ema = ema + alpha * (price - ema);
				}

				var upper = ema + bandPercentage * stdDev;
				var lower = ema - bandPercentage * stdDev;

				result.MovingAverage = ema;
				result.UpperBand = upper;
				result.LowerBand = lower;
				result.IsFormed = 1;
			}

			flatResults[resIndex] = result;
		}
	}
}

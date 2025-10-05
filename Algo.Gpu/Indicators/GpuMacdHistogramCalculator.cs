namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU MACD histogram calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuMacdHistogramParams"/> struct.
/// </remarks>
/// <param name="longLength">Long EMA length.</param>
/// <param name="shortLength">Short EMA length.</param>
/// <param name="signalLength">Signal EMA length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuMacdHistogramParams(int longLength, int shortLength, int signalLength, byte priceType) : IGpuIndicatorParams
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
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is MovingAverageConvergenceDivergenceHistogram histogram)
		{
			var macd = histogram.Macd;
			Unsafe.AsRef(in this).LongLength = macd.LongMa.Length;
			Unsafe.AsRef(in this).ShortLength = macd.ShortMa.Length;
			Unsafe.AsRef(in this).SignalLength = histogram.SignalMa.Length;
		}
	}
}

/// <summary>
/// Complex GPU result for MACD histogram calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuMacdHistogramResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTimeOffset.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// MACD value.
	/// </summary>
	public float Macd;

	/// <summary>
	/// Signal value.
	/// </summary>
	public float Signal;

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

		var macdHistogram = (MovingAverageConvergenceDivergenceHistogram)indicator;

		if (Macd.IsNaN() || Signal.IsNaN())
		{
			return new MovingAverageConvergenceDivergenceHistogramValue(macdHistogram, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		var value = new MovingAverageConvergenceDivergenceHistogramValue(macdHistogram, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		value.Add(macdHistogram.Macd, new DecimalIndicatorValue(macdHistogram.Macd, (decimal)Macd, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		value.Add(macdHistogram.SignalMa, new DecimalIndicatorValue(macdHistogram.SignalMa, (decimal)Signal, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		return value;
	}
}

/// <summary>
/// GPU calculator for MACD histogram indicator.
/// </summary>
public class GpuMacdHistogramCalculator : GpuIndicatorCalculatorBase<MovingAverageConvergenceDivergenceHistogram, GpuMacdHistogramParams, GpuMacdHistogramResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuMacdHistogramResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMacdHistogramParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuMacdHistogramCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuMacdHistogramCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuMacdHistogramResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMacdHistogramParams>>(MacdHistogramParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuMacdHistogramResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuMacdHistogramParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuMacdHistogramResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuMacdHistogramResult[seriesCount][][];

		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuMacdHistogramResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuMacdHistogramResult[len];
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
	/// ILGPU kernel: MACD histogram computation for multiple series and parameter sets.
	/// </summary>
	private static void MacdHistogramParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuMacdHistogramResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuMacdHistogramParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];

		if (len <= 0)
			return;

		var param = parameters[paramIdx];
		var shortLength = param.ShortLength <= 0 ? 1 : param.ShortLength;
		var longLength = param.LongLength <= 0 ? 1 : param.LongLength;
		var signalLength = param.SignalLength <= 0 ? 1 : param.SignalLength;

		var shortMultiplier = 2f / (shortLength + 1);
		var longMultiplier = 2f / (longLength + 1);
		var signalMultiplier = 2f / (signalLength + 1);

		float shortEma = 0f;
		float longEma = 0f;
		float signalEma = 0f;

		float shortSum = 0f;
		float longSum = 0f;
		float signalSum = 0f;

		var shortReady = shortLength <= 1;
		var longReady = longLength <= 1;
		var signalReady = signalLength <= 1;
		var macdCount = 0;

		for (var i = 0; i < len; i++)
		{
			var idx = offset + i;
			var candle = flatCandles[idx];
			var price = ExtractPrice(candle, (Level1Fields)param.PriceType);

			var resultIndex = paramIdx * flatCandles.Length + idx;

			var result = new GpuMacdHistogramResult
			{
				Time = candle.Time,
				Macd = float.NaN,
				Signal = float.NaN,
				IsFormed = 0,
			};

			if (!shortReady)
			{
				shortSum += price;
				if (i == shortLength - 1)
				{
					shortEma = shortSum / shortLength;
					shortReady = true;
				}
			}
			else
			{
				shortEma += (price - shortEma) * shortMultiplier;
			}

			if (!longReady)
			{
				longSum += price;
				if (i == longLength - 1)
				{
					longEma = longSum / longLength;
					longReady = true;
				}
			}
			else
			{
				longEma += (price - longEma) * longMultiplier;
			}

			var macdFormed = shortReady && longReady;
			float macd = 0f;

			if (macdFormed)
			{
				macd = shortEma - longEma;
				result.Macd = macd;

				if (!signalReady)
				{
					signalSum += macd;
					macdCount++;

					if (signalLength <= 1 || macdCount >= signalLength)
					{
						signalEma = signalLength <= 1 ? macd : signalSum / signalLength;
						signalReady = true;
					}
				}
				else
				{
					signalEma += (macd - signalEma) * signalMultiplier;
				}
			}

			if (signalReady)
			{
				result.Signal = signalEma;
				result.IsFormed = (byte)((signalReady && macdFormed) ? 1 : 0);
			}

			flatResults[resultIndex] = result;
		}
	}
}

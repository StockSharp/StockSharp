namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU MACD with signal calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuMacdSignalParams"/> struct.
/// </remarks>
/// <param name="longLength">Length for the long EMA.</param>
/// <param name="shortLength">Length for the short EMA.</param>
/// <param name="signalLength">Length for the signal EMA.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuMacdSignalParams(int longLength, int shortLength, int signalLength, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Length for the long EMA.
	/// </summary>
	public int LongLength = longLength;

	/// <summary>
	/// Length for the short EMA.
	/// </summary>
	public int ShortLength = shortLength;

	/// <summary>
	/// Length for the signal EMA.
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

		if (indicator is MovingAverageConvergenceDivergenceSignal macdSignal)
		{
			var macd = macdSignal.Macd;
			Unsafe.AsRef(in this).LongLength = macd.LongMa.Length;
			Unsafe.AsRef(in this).ShortLength = macd.ShortMa.Length;
			Unsafe.AsRef(in this).SignalLength = macdSignal.SignalMa.Length;
		}
	}
}

/// <summary>
/// GPU result for MACD with signal calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuMacdSignalResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// MACD value.
	/// </summary>
	public float Macd;

	/// <summary>
	/// Signal line value.
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

		var macdSignal = (MovingAverageConvergenceDivergenceSignal)indicator;

		if (Macd.IsNaN() || Signal.IsNaN())
		{
			return new MovingAverageConvergenceDivergenceSignalValue(macdSignal, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		var value = new MovingAverageConvergenceDivergenceSignalValue(macdSignal, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		var macdIndicator = macdSignal.Macd;
		value.Add(macdIndicator, new DecimalIndicatorValue(macdIndicator, (decimal)Macd, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		var signalIndicator = macdSignal.SignalMa;
		value.Add(signalIndicator, new DecimalIndicatorValue(signalIndicator, (decimal)Signal, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		return value;
	}
}

/// <summary>
/// GPU calculator for MACD with signal.
/// </summary>
public class GpuMacdSignalCalculator : GpuIndicatorCalculatorBase<MovingAverageConvergenceDivergenceSignal, GpuMacdSignalParams, GpuMacdSignalResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuMacdSignalResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMacdSignalParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuMacdSignalCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuMacdSignalCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuMacdSignalResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMacdSignalParams>>(MacdSignalParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuMacdSignalResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuMacdSignalParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuMacdSignalResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		// Re-split [series][param][bar]
		var result = new GpuMacdSignalResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuMacdSignalResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuMacdSignalResult[len];
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
	/// ILGPU kernel: MACD with signal computation for multiple series and parameter sets.
	/// One thread handles one (parameter, series) pair and iterates bars sequentially.
	/// </summary>
	private static void MacdSignalParamsSeriesKernel(
	Index2D index,
	ArrayView<GpuCandle> flatCandles,
	ArrayView<GpuMacdSignalResult> flatResults,
	ArrayView<int> offsets,
	ArrayView<int> lengths,
	ArrayView<GpuMacdSignalParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var shortLength = prm.ShortLength <= 0 ? 1 : prm.ShortLength;
		var longLength = prm.LongLength <= 0 ? 1 : prm.LongLength;
		var signalLength = prm.SignalLength <= 0 ? 1 : prm.SignalLength;
		var priceType = (Level1Fields)prm.PriceType;

		var shortMultiplier = 2f / (shortLength + 1f);
		var longMultiplier = 2f / (longLength + 1f);
		var signalMultiplier = 2f / (signalLength + 1f);

		float shortSum = 0f, longSum = 0f, signalSum = 0f;
		float shortEma = 0f, longEma = 0f, signalEma = 0f;
		var macdCount = 0;

		for (var i = 0; i < len; i++)
		{
			var c = flatCandles[offset + i];
			var price = ExtractPrice(c, priceType);

			var resIndex = paramIdx * flatCandles.Length + (offset + i);
			var result = new GpuMacdSignalResult
			{
				Time = c.Time,
				Macd = float.NaN,
				Signal = float.NaN,
				IsFormed = 0,
			};

			if (i < shortLength)
			{
				shortSum += price;
				if (i == shortLength - 1)
					shortEma = shortSum / shortLength;
			}
			else
			{
				shortEma = (price - shortEma) * shortMultiplier + shortEma;
			}

			if (i < longLength)
			{
				longSum += price;
				if (i == longLength - 1)
					longEma = longSum / longLength;
			}
			else
			{
				longEma = (price - longEma) * longMultiplier + longEma;
			}

			var shortReady = i >= shortLength - 1;
			var longReady = i >= longLength - 1;

			if (shortReady && longReady)
			{
				var macd = shortEma - longEma;
				result.Macd = macd;

				if (macdCount < signalLength)
				{
					signalSum += macd;
					macdCount++;

					if (macdCount == signalLength)
					{
						signalEma = signalSum / signalLength;
						result.Signal = signalEma;
						result.IsFormed = 1;
					}
				}
				else
				{
					signalEma = (macd - signalEma) * signalMultiplier + signalEma;
					result.Signal = signalEma;
					result.IsFormed = 1;
				}

				if (result.IsFormed == 0)
					result.Signal = float.NaN;
			}

			flatResults[resIndex] = result;
		}
	}
}

namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Klinger Volume Oscillator calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuKlingerVolumeOscillatorParams"/> struct.
/// </remarks>
/// <param name="shortLength">Short EMA length.</param>
/// <param name="longLength">Long EMA length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuKlingerVolumeOscillatorParams(int shortLength, int longLength) : IGpuIndicatorParams
{
	/// <summary>
	/// Length for the short EMA.
	/// </summary>
	public int ShortLength = shortLength;

	/// <summary>
	/// Length for the long EMA.
	/// </summary>
	public int LongLength = longLength;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is KlingerVolumeOscillator kvo)
		{
			Unsafe.AsRef(in this).ShortLength = kvo.ShortPeriod;
			Unsafe.AsRef(in this).LongLength = kvo.LongPeriod;
		}
	}
}

/// <summary>
/// GPU result for Klinger Volume Oscillator calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuKlingerVolumeOscillatorResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Short EMA value.
	/// </summary>
	public float Short;

	/// <summary>
	/// Long EMA value.
	/// </summary>
	public float Long;

	/// <summary>
	/// Oscillator (short - long) value.
	/// </summary>
	public float Oscillator;

	/// <summary>
	/// Indicator formed flag (byte to be GPU-friendly).
	/// </summary>
	public byte IsFormed;

	readonly long IGpuIndicatorResult.Time => Time;
	readonly byte IGpuIndicatorResult.IsFormed => IsFormed;

	/// <inheritdoc />
	public readonly IIndicatorValue ToValue(IIndicator indicator)
	{
		var time = this.GetTime();
		var isFormed = this.GetIsFormed();

		var kvo = (KlingerVolumeOscillator)indicator;

		if (Short.IsNaN() || Long.IsNaN() || Oscillator.IsNaN())
		{
			return new KlingerVolumeOscillatorValue(kvo, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		var result = new KlingerVolumeOscillatorValue(kvo, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		var shortValue = new DecimalIndicatorValue(kvo.ShortEma, (decimal)Short, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		var longValue = new DecimalIndicatorValue(kvo.LongEma, (decimal)Long, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		var oscValue = new DecimalIndicatorValue(kvo, (decimal)Oscillator, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		result.Add(kvo.ShortEma, shortValue);
		result.Add(kvo.LongEma, longValue);
		result.Add(kvo, oscValue);

		return result;
	}
}

/// <summary>
/// GPU calculator for <see cref="KlingerVolumeOscillator"/> indicator.
/// </summary>
public class GpuKlingerVolumeOscillatorCalculator : GpuIndicatorCalculatorBase<KlingerVolumeOscillator, GpuKlingerVolumeOscillatorParams, GpuKlingerVolumeOscillatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuKlingerVolumeOscillatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuKlingerVolumeOscillatorParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuKlingerVolumeOscillatorCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuKlingerVolumeOscillatorCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuKlingerVolumeOscillatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuKlingerVolumeOscillatorParams>>(KlingerParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuKlingerVolumeOscillatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuKlingerVolumeOscillatorParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuKlingerVolumeOscillatorResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_paramsSeriesKernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuKlingerVolumeOscillatorResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuKlingerVolumeOscillatorResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuKlingerVolumeOscillatorResult[len];
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
	/// ILGPU kernel: Klinger Volume Oscillator computation per parameter/series pair.
	/// </summary>
	private static void KlingerParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuKlingerVolumeOscillatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuKlingerVolumeOscillatorParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var shortLen = prm.ShortLength;
		var longLen = prm.LongLength;
		if (shortLen <= 0)
			shortLen = 1;
		if (longLen <= 0)
			longLen = 1;

		var shortMultiplier = 2f / (shortLen + 1f);
		var longMultiplier = 2f / (longLen + 1f);

		var shortSum = 0f;
		var longSum = 0f;
		var shortCount = 0;
		var longCount = 0;
		var shortPrev = 0f;
		var longPrev = 0f;
		var prevHlc = 0f;

		byte prevFormed = 0;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var hlc = (candle.High + candle.Low + candle.Close) / 3f;
			var direction = hlc > prevHlc ? 1f : -1f;
			var sv = candle.Volume * direction;

			var shortValue = float.NaN;
			if (shortCount < shortLen)
			{
				shortSum += sv;
				shortCount++;
				shortValue = shortSum / shortLen;
				if (shortCount == shortLen)
					shortPrev = shortValue;
			}
			else
			{
				shortPrev = shortPrev + (sv - shortPrev) * shortMultiplier;
				shortValue = shortPrev;
			}

			var longValue = float.NaN;
			if (longCount < longLen)
			{
				longSum += sv;
				longCount++;
				longValue = longSum / longLen;
				if (longCount == longLen)
					longPrev = longValue;
			}
			else
			{
				longPrev = longPrev + (sv - longPrev) * longMultiplier;
				longValue = longPrev;
			}

			var resIndex = paramIdx * flatCandles.Length + (offset + i);
			var curFormed = (byte)((shortCount >= shortLen && longCount >= longLen) ? 1 : 0);
			var oscValue = curFormed == 1 ? shortValue - longValue : float.NaN;

			flatResults[resIndex] = new GpuKlingerVolumeOscillatorResult
			{
				Time = candle.Time,
				Short = shortValue,
				Long = longValue,
				Oscillator = oscValue,
				IsFormed = prevFormed,
			};

			prevFormed = curFormed;
			prevHlc = hlc;
		}
	}
}

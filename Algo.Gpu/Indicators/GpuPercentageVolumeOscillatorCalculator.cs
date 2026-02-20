namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Percentage Volume Oscillator (PVO) calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuPercentageVolumeOscillatorParams"/> struct.
/// </remarks>
/// <param name="shortLength">Short EMA length.</param>
/// <param name="longLength">Long EMA length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuPercentageVolumeOscillatorParams(int shortLength, int longLength) : IGpuIndicatorParams
{
	/// <summary>
	/// Short EMA period length.
	/// </summary>
	public int ShortLength = shortLength;

	/// <summary>
	/// Long EMA period length.
	/// </summary>
	public int LongLength = longLength;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is PercentageVolumeOscillator pvo)
		{
			Unsafe.AsRef(in this).ShortLength = pvo.ShortPeriod;
			Unsafe.AsRef(in this).LongLength = pvo.LongPeriod;
		}
	}
}

/// <summary>
/// Complex GPU result for Percentage Volume Oscillator calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuPercentageVolumeOscillatorResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Short EMA value.
	/// </summary>
	public float ShortEma;

	/// <summary>
	/// Long EMA value.
	/// </summary>
	public float LongEma;

	/// <summary>
	/// Percentage Volume Oscillator value.
	/// </summary>
	public float Pvo;

	/// <summary>
	/// Flag indicating the short EMA is formed (byte for GPU friendliness).
	/// </summary>
	public byte ShortIsFormed;

	/// <summary>
	/// Flag indicating the long EMA is formed (byte for GPU friendliness).
	/// </summary>
	public byte LongIsFormed;

	/// <summary>
	/// Is indicator formed (byte for GPU friendliness).
	/// </summary>
	public byte IsFormed;

	readonly long IGpuIndicatorResult.Time => Time;
	readonly byte IGpuIndicatorResult.IsFormed => IsFormed;

	/// <inheritdoc />
	public readonly IIndicatorValue ToValue(IIndicator indicator)
	{
		var time = this.GetTime();
		var isFormed = this.GetIsFormed();

		var pvoIndicator = (PercentageVolumeOscillator)indicator;

		var result = new PercentageVolumeOscillatorValue(pvoIndicator, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		var shortValue = float.IsNaN(ShortEma)
			? new DecimalIndicatorValue(pvoIndicator.ShortEma, time)
			{
				IsFinal = true,
				IsFormed = ShortIsFormed != 0,
				IsEmpty = true,
			}
			: new DecimalIndicatorValue(pvoIndicator.ShortEma, (decimal)ShortEma, time)
			{
				IsFinal = true,
				IsFormed = ShortIsFormed != 0,
			};

		result.Add(pvoIndicator.ShortEma, shortValue);

		var longValue = float.IsNaN(LongEma)
			? new DecimalIndicatorValue(pvoIndicator.LongEma, time)
			{
				IsFinal = true,
				IsFormed = LongIsFormed != 0,
				IsEmpty = true,
			}
			: new DecimalIndicatorValue(pvoIndicator.LongEma, (decimal)LongEma, time)
			{
				IsFinal = true,
				IsFormed = LongIsFormed != 0,
			};

		result.Add(pvoIndicator.LongEma, longValue);

		if (!float.IsNaN(Pvo))
		{
			var pvoValue = new DecimalIndicatorValue(pvoIndicator, (decimal)Pvo, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
			};

			result.Add(pvoIndicator, pvoValue);
		}

		return result;
	}
}

/// <summary>
/// GPU calculator for Percentage Volume Oscillator.
/// </summary>
public class GpuPercentageVolumeOscillatorCalculator : GpuIndicatorCalculatorBase<PercentageVolumeOscillator, GpuPercentageVolumeOscillatorParams, GpuPercentageVolumeOscillatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuPercentageVolumeOscillatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuPercentageVolumeOscillatorParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuPercentageVolumeOscillatorCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuPercentageVolumeOscillatorCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuPercentageVolumeOscillatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuPercentageVolumeOscillatorParams>>(PvoParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuPercentageVolumeOscillatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuPercentageVolumeOscillatorParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuPercentageVolumeOscillatorResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuPercentageVolumeOscillatorResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuPercentageVolumeOscillatorResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuPercentageVolumeOscillatorResult[len];
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
	/// ILGPU kernel computing PVO for multiple parameter sets and series.
	/// </summary>
	private static void PvoParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuPercentageVolumeOscillatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuPercentageVolumeOscillatorParams> parameters)
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

		float shortSum = 0f;
		float longSum = 0f;
		float prevShort = 0f;
		float prevLong = 0f;

		byte prevFormed = 0;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var volume = candle.Volume;

			var shortValue = 0f;
			byte shortIsFormed = 0;

			if (shortLen <= 1)
			{
				shortValue = volume;
				prevShort = shortValue;
				shortIsFormed = 1;
			}
			else if (i < shortLen)
			{
				shortSum += volume;
				shortValue = shortSum / shortLen;
				if (i == shortLen - 1)
				{
					prevShort = shortValue;
					shortIsFormed = 1;
				}
			}
			else
			{
				shortValue = ((volume - prevShort) * shortMultiplier) + prevShort;
				prevShort = shortValue;
				shortIsFormed = 1;
			}

			var longValue = 0f;
			byte longIsFormed = 0;

			if (longLen <= 1)
			{
				longValue = volume;
				prevLong = longValue;
				longIsFormed = 1;
			}
			else if (i < longLen)
			{
				longSum += volume;
				longValue = longSum / longLen;
				if (i == longLen - 1)
				{
					prevLong = longValue;
					longIsFormed = 1;
				}
			}
			else
			{
				longValue = ((volume - prevLong) * longMultiplier) + prevLong;
				prevLong = longValue;
				longIsFormed = 1;
			}

			var pvo = float.NaN;
			if (longIsFormed != 0)
			{
				pvo = longValue == 0f ? 0f : ((shortValue - longValue) / longValue) * 100f;
			}

			byte curFormed = (byte)((shortIsFormed != 0 && longIsFormed != 0) ? 1 : 0);

			var resIndex = paramIdx * flatCandles.Length + (offset + i);
			flatResults[resIndex] = new()
			{
				Time = candle.Time,
				ShortEma = shortValue,
				LongEma = longValue,
				Pvo = pvo,
				ShortIsFormed = shortIsFormed,
				LongIsFormed = longIsFormed,
				IsFormed = prevFormed,
			};
			prevFormed = curFormed;
		}
	}
}

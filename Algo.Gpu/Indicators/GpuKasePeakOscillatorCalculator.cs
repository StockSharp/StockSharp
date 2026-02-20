namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Kase Peak Oscillator calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuKasePeakOscillatorParams"/> struct.
/// </remarks>
/// <param name="shortPeriod">Short oscillator period.</param>
/// <param name="longPeriod">Long oscillator period.</param>
/// <param name="atrLength">ATR length used inside the oscillator.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuKasePeakOscillatorParams(int shortPeriod, int longPeriod, int atrLength = 10) : IGpuIndicatorParams
{
	/// <summary>
	/// Short oscillator period.
	/// </summary>
	public int ShortPeriod = shortPeriod;

	/// <summary>
	/// Long oscillator period.
	/// </summary>
	public int LongPeriod = longPeriod;

	/// <summary>
	/// ATR length used for volatility smoothing.
	/// </summary>
	public int AtrLength = atrLength;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is not KasePeakOscillator kpo)
			return;

		Unsafe.AsRef(in this).ShortPeriod = kpo.ShortPeriod;
		Unsafe.AsRef(in this).LongPeriod = kpo.LongPeriod;
		Unsafe.AsRef(in this).AtrLength = 10;
	}
}

/// <summary>
/// GPU result for Kase Peak Oscillator calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuKasePeakOscillatorResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Short-term oscillator value.
	/// </summary>
	public float ShortTerm;

	/// <summary>
	/// Long-term oscillator value.
	/// </summary>
	public float LongTerm;

	/// <summary>
	/// Short-term oscillator formed flag.
	/// </summary>
	public byte ShortIsFormed;

	/// <summary>
	/// Long-term oscillator formed flag.
	/// </summary>
	public byte LongIsFormed;

	/// <summary>
	/// Indicator formed flag.
	/// </summary>
	public byte IsFormed;

	readonly long IGpuIndicatorResult.Time => Time;
	readonly byte IGpuIndicatorResult.IsFormed => IsFormed;

	/// <inheritdoc />
	public readonly IIndicatorValue ToValue(IIndicator indicator)
	{
		var time = this.GetTime();
		var formed = this.GetIsFormed();
		var kpo = (KasePeakOscillator)indicator;

		var value = new KasePeakOscillatorValue(kpo, time)
		{
			IsFinal = true,
			IsFormed = formed,
			IsEmpty = ShortTerm.IsNaN() && LongTerm.IsNaN(),
		};

		value.Add(kpo.ShortTerm, CreatePartValue(kpo.ShortTerm, time, ShortTerm, ShortIsFormed));
		value.Add(kpo.LongTerm, CreatePartValue(kpo.LongTerm, time, LongTerm, LongIsFormed));

		return value;
	}

	private static IIndicatorValue CreatePartValue(KasePeakOscillatorPart part, DateTime time, float data, byte formed)
	{
		if (data.IsNaN())
		{
			return new DecimalIndicatorValue(part, time)
			{
				IsFinal = true,
				IsFormed = formed != 0,
			};
		}

		return new DecimalIndicatorValue(part, (decimal)data, time)
		{
			IsFinal = true,
			IsFormed = formed != 0,
		};
	}
}

/// <summary>
/// GPU calculator for <see cref="KasePeakOscillator"/>.
/// </summary>
public class GpuKasePeakOscillatorCalculator : GpuIndicatorCalculatorBase<KasePeakOscillator, GpuKasePeakOscillatorParams, GpuKasePeakOscillatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuKasePeakOscillatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuKasePeakOscillatorParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuKasePeakOscillatorCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuKasePeakOscillatorCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuKasePeakOscillatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuKasePeakOscillatorParams>>(KasePeakOscillatorKernel);
	}

	/// <inheritdoc />
	public override GpuKasePeakOscillatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuKasePeakOscillatorParams[] parameters)
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
			if (len <= 0)
				continue;

			Array.Copy(candlesSeries[s], 0, flatCandles, offset, len);
			offset += len;
		}

		using var inputBuffer = Accelerator.Allocate1D(flatCandles);
		using var offsetsBuffer = Accelerator.Allocate1D(seriesOffsets);
		using var lengthsBuffer = Accelerator.Allocate1D(seriesLengths);
		using var paramsBuffer = Accelerator.Allocate1D(parameters);
		using var outputBuffer = Accelerator.Allocate1D<GpuKasePeakOscillatorResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuKasePeakOscillatorResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuKasePeakOscillatorResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuKasePeakOscillatorResult[len];
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

	private static void KasePeakOscillatorKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuKasePeakOscillatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuKasePeakOscillatorParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var shortPeriod = prm.ShortPeriod <= 0 ? 1 : prm.ShortPeriod;
		var longPeriod = prm.LongPeriod <= 0 ? 1 : prm.LongPeriod;
		var atrLength = prm.AtrLength <= 0 ? 1 : prm.AtrLength;

		float prevCloseTrend = 0f;
		var hasPrevTrend = false;

		float prevClose = 0f;
		var hasPrev = false;

		float atr = 0f;
		var atrCount = 0;

		float peakOldest = 0f, peakLatest = 0f;
		var peakCount = 0;

		float valleyOldest = 0f, valleyLatest = 0f;
		var valleyCount = 0;

		var shortCount = 0;
		var longCount = 0;

		var totalCandles = flatCandles.Length;

		byte prevFormed = 0;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];

			var high = candle.High;
			var low = candle.Low;
			var close = candle.Close;

			float tr;
			if (!hasPrev)
			{
				tr = high - low;
				prevClose = close;
				hasPrev = true;
			}
			else
			{
				var tr1 = high - low;
				var tr2 = MathF.Abs(high - prevClose);
				var tr3 = MathF.Abs(low - prevClose);
				tr = MathF.Max(tr1, MathF.Max(tr2, tr3));
				prevClose = close;
			}

			atrCount++;
			var buffCount = atrCount < atrLength ? atrCount : atrLength;
			if (atrCount == 1)
			{
				atr = tr;
			}
			else
			{
				atr = ((atr * (buffCount - 1)) + tr) / buffCount;
			}

			var resIndex = paramIdx * totalCandles + globalIdx;
			var result = new GpuKasePeakOscillatorResult
			{
				Time = candle.Time,
				ShortTerm = float.NaN,
				LongTerm = float.NaN,
				ShortIsFormed = 0,
				LongIsFormed = 0,
				IsFormed = 0,
			};

			if (atrCount >= atrLength)
			{
				var peak = high;
				var valley = low;

				if (hasPrevTrend)
				{
					if (close > prevCloseTrend)
					{
						peak = MathF.Max(high, prevCloseTrend + atr);
						valley = MathF.Max(low, prevCloseTrend - 0.5f * atr);
					}
					else if (close < prevCloseTrend)
					{
						peak = MathF.Min(high, prevCloseTrend + 0.5f * atr);
						valley = MathF.Min(low, prevCloseTrend - atr);
					}
				}

				Push(ref peakOldest, ref peakLatest, ref peakCount, peak);
				Push(ref valleyOldest, ref valleyLatest, ref valleyCount, valley);

				prevCloseTrend = close;
				hasPrevTrend = true;

				var minValue = GetMin(valleyOldest, valleyLatest, valleyCount);
				var maxValue = GetMax(peakOldest, peakLatest, peakCount);

				var den1 = maxValue - minValue;
				var den2 = peakCount > 0 && valleyCount > 0 ? peakOldest - valleyOldest : 0f;

				var shortOsc = den1 == 0f ? 0f : 100f * (close - minValue) / den1;
				var longOsc = den2 == 0f ? 0f : 100f * (close - valleyOldest) / den2;

				if (shortCount < int.MaxValue)
					shortCount++;

				if (longCount < int.MaxValue)
					longCount++;

				var shortFormed = (byte)(shortCount >= shortPeriod ? 1 : 0);
				var longFormed = (byte)(longCount >= longPeriod ? 1 : 0);
				var curFormed = (byte)((shortFormed != 0 && longFormed != 0) ? 1 : 0);

				result.ShortTerm = shortOsc;
				result.LongTerm = longOsc;
				result.ShortIsFormed = shortFormed;
				result.LongIsFormed = longFormed;
				result.IsFormed = prevFormed;

				prevFormed = curFormed;
			}

			flatResults[resIndex] = result;
		}
	}

	private static void Push(ref float oldest, ref float latest, ref int count, float value)
	{
		if (count == 0)
		{
			oldest = value;
			latest = value;
			count = 1;
		}
		else if (count == 1)
		{
			latest = value;
			count = 2;
		}
		else
		{
			oldest = latest;
			latest = value;
		}
	}

	private static float GetMin(float oldest, float latest, int count)
	{
		if (count <= 0)
			return float.NaN;

		if (count == 1)
			return latest;

		return MathF.Min(oldest, latest);
	}

	private static float GetMax(float oldest, float latest, int count)
	{
		if (count <= 0)
			return float.NaN;

		if (count == 1)
			return latest;

		return MathF.Max(oldest, latest);
	}
}

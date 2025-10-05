namespace StockSharp.Algo.Gpu.Indicators;

using System.Reflection;

/// <summary>
/// Parameter set for GPU Know Sure Thing (KST) calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuKnowSureThingParams : IGpuIndicatorParams
{
	private const int DefaultRoc1Length = 10;
	private const int DefaultRoc2Length = 15;
	private const int DefaultRoc3Length = 20;
	private const int DefaultRoc4Length = 30;
	private const int DefaultSma1Length = 10;
	private const int DefaultSma2Length = 10;
	private const int DefaultSma3Length = 10;
	private const int DefaultSma4Length = 15;
	private const int DefaultSignalLength = 9;

	/// <summary>
	/// Rate of Change length #1.
	/// </summary>
	public int Roc1Length;

	/// <summary>
	/// Rate of Change length #2.
	/// </summary>
	public int Roc2Length;

	/// <summary>
	/// Rate of Change length #3.
	/// </summary>
	public int Roc3Length;

	/// <summary>
	/// Rate of Change length #4.
	/// </summary>
	public int Roc4Length;

	/// <summary>
	/// Simple Moving Average length applied to ROC #1.
	/// </summary>
	public int Sma1Length;

	/// <summary>
	/// Simple Moving Average length applied to ROC #2.
	/// </summary>
	public int Sma2Length;

	/// <summary>
	/// Simple Moving Average length applied to ROC #3.
	/// </summary>
	public int Sma3Length;

	/// <summary>
	/// Simple Moving Average length applied to ROC #4.
	/// </summary>
	public int Sma4Length;

	/// <summary>
	/// Signal line Simple Moving Average length.
	/// </summary>
	public int SignalLength;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		ArgumentNullException.ThrowIfNull(indicator);

		ref var self = ref Unsafe.AsRef(in this);

		self.PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is KnowSureThing kst)
		{
			self.Roc1Length = GetLength(kst, "_roc1", self.Roc1Length, DefaultRoc1Length);
			self.Roc2Length = GetLength(kst, "_roc2", self.Roc2Length, DefaultRoc2Length);
			self.Roc3Length = GetLength(kst, "_roc3", self.Roc3Length, DefaultRoc3Length);
			self.Roc4Length = GetLength(kst, "_roc4", self.Roc4Length, DefaultRoc4Length);
			self.Sma1Length = GetLength(kst, "_sma1", self.Sma1Length, DefaultSma1Length);
			self.Sma2Length = GetLength(kst, "_sma2", self.Sma2Length, DefaultSma2Length);
			self.Sma3Length = GetLength(kst, "_sma3", self.Sma3Length, DefaultSma3Length);
			self.Sma4Length = GetLength(kst, "_sma4", self.Sma4Length, DefaultSma4Length);
			self.SignalLength = kst.Signal?.Length ?? DefaultSignalLength;
		}

		self.Roc1Length = EnsurePositive(self.Roc1Length, DefaultRoc1Length);
		self.Roc2Length = EnsurePositive(self.Roc2Length, DefaultRoc2Length);
		self.Roc3Length = EnsurePositive(self.Roc3Length, DefaultRoc3Length);
		self.Roc4Length = EnsurePositive(self.Roc4Length, DefaultRoc4Length);
		self.Sma1Length = EnsurePositive(self.Sma1Length, DefaultSma1Length);
		self.Sma2Length = EnsurePositive(self.Sma2Length, DefaultSma2Length);
		self.Sma3Length = EnsurePositive(self.Sma3Length, DefaultSma3Length);
		self.Sma4Length = EnsurePositive(self.Sma4Length, DefaultSma4Length);
		self.SignalLength = EnsurePositive(self.SignalLength, DefaultSignalLength);
	}

	private static int EnsurePositive(int value, int fallback)
	=> value > 0 ? value : fallback;

	private static int GetLength(KnowSureThing indicator, string fieldName, int currentValue, int fallback)
	{
		var field = typeof(KnowSureThing).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
		if (field?.GetValue(indicator) is LengthIndicator<decimal> lengthIndicator)
		{
			return EnsurePositive(lengthIndicator.Length, fallback);
		}

		return EnsurePositive(currentValue, fallback);
	}
}

/// <summary>
/// Complex GPU result for Know Sure Thing (KST) calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuKnowSureThingResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTimeOffset.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Calculated Know Sure Thing (KST) line value.
	/// </summary>
	public float Kst;

	/// <summary>
	/// Calculated signal line value.
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
		ArgumentNullException.ThrowIfNull(indicator);

		var time = this.GetTime();
		var isFormed = this.GetIsFormed();

		var kstIndicator = (KnowSureThing)indicator;

		if (float.IsNaN(Kst) || float.IsNaN(Signal))
		{
			return new KnowSureThingValue(kstIndicator, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		var value = new KnowSureThingValue(kstIndicator, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		value.SetInnerDecimal(kstIndicator.KstLine, time, (decimal)Kst, true);
		value.SetInnerDecimal(kstIndicator.Signal, time, (decimal)Signal, true);

		return value;
	}
}

/// <summary>
/// GPU calculator for Know Sure Thing (KST).
/// </summary>
public class GpuKnowSureThingCalculator : GpuIndicatorCalculatorBase<KnowSureThing, GpuKnowSureThingParams, GpuKnowSureThingResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuKnowSureThingResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuKnowSureThingParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuKnowSureThingCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuKnowSureThingCalculator(Context context, Accelerator accelerator)
	: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuKnowSureThingResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuKnowSureThingParams>>(KstParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuKnowSureThingResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuKnowSureThingParams[] parameters)
	{
		ArgumentNullException.ThrowIfNull(candlesSeries);
		ArgumentNullException.ThrowIfNull(parameters);

		if (candlesSeries.Length == 0)
		{
			throw new ArgumentOutOfRangeException(nameof(candlesSeries));
		}

		if (parameters.Length == 0)
		{
			throw new ArgumentOutOfRangeException(nameof(parameters));
		}

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
		using var outputBuffer = Accelerator.Allocate1D<GpuKnowSureThingResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuKnowSureThingResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuKnowSureThingResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuKnowSureThingResult[len];
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
	/// ILGPU kernel: KST computation for multiple series and parameter sets.
	/// </summary>
	private static void KstParamsSeriesKernel(
	Index2D index,
	ArrayView<GpuCandle> flatCandles,
	ArrayView<GpuKnowSureThingResult> flatResults,
	ArrayView<int> offsets,
	ArrayView<int> lengths,
	ArrayView<GpuKnowSureThingParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
		{
			return;
		}

		var baseIndex = paramIdx * flatCandles.Length;

		var prm = parameters[paramIdx];
		var priceType = (Level1Fields)prm.PriceType;

		var roc1Len = Math.Max(prm.Roc1Length, 1);
		var roc2Len = Math.Max(prm.Roc2Length, 1);
		var roc3Len = Math.Max(prm.Roc3Length, 1);
		var roc4Len = Math.Max(prm.Roc4Length, 1);

		var sma1Len = Math.Max(prm.Sma1Length, 1);
		var sma2Len = Math.Max(prm.Sma2Length, 1);
		var sma3Len = Math.Max(prm.Sma3Length, 1);
		var sma4Len = Math.Max(prm.Sma4Length, 1);

		var signalLen = Math.Max(prm.SignalLength, 1);

		var kstStartIndex = roc4Len + sma4Len - 1;
		var formedIndex = kstStartIndex + signalLen - 1;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var resIndex = baseIndex + offset + i;

			var result = new GpuKnowSureThingResult
			{
				Time = candle.Time,
				Kst = float.NaN,
				Signal = float.NaN,
				IsFormed = 0,
			};

			if (i < kstStartIndex)
			{
				flatResults[resIndex] = result;
				continue;
			}

			var sma1 = ComputeSmaOfRoc(flatCandles, offset, i, roc1Len, sma1Len, priceType);
			var sma2 = ComputeSmaOfRoc(flatCandles, offset, i, roc2Len, sma2Len, priceType);
			var sma3 = ComputeSmaOfRoc(flatCandles, offset, i, roc3Len, sma3Len, priceType);
			var sma4 = ComputeSmaOfRoc(flatCandles, offset, i, roc4Len, sma4Len, priceType);

			if (float.IsNaN(sma1) || float.IsNaN(sma2) || float.IsNaN(sma3) || float.IsNaN(sma4))
			{
				flatResults[resIndex] = result;
				continue;
			}

			var kst = sma1 + (2f * sma2) + (3f * sma3) + (4f * sma4);
			result.Kst = kst;

			var signal = ComputeSignal(flatResults, baseIndex, offset, i, signalLen, kstStartIndex, kst);
			result.Signal = signal;

			if (!float.IsNaN(signal) && i >= formedIndex)
			{
				result.IsFormed = 1;
			}

			flatResults[resIndex] = result;
		}
	}

	private static float ComputeSignal(
	ArrayView<GpuKnowSureThingResult> flatResults,
	int baseIndex,
	int offset,
	int currentIndex,
	int signalLength,
	int kstStartIndex,
	float currentKst)
	{
		var sum = 0f;
		var hasValue = false;

		for (var j = 0; j < signalLength; j++)
		{
			var idx = currentIndex - j;
			if (idx < kstStartIndex)
			{
				continue;
			}

			var value = idx == currentIndex ? currentKst : flatResults[baseIndex + offset + idx].Kst;
			if (float.IsNaN(value))
			{
				return float.NaN;
			}

			sum += value;
			hasValue = true;
		}

		return hasValue ? sum / signalLength : float.NaN;
	}

	private static float ComputeSmaOfRoc(
	ArrayView<GpuCandle> flatCandles,
	int offset,
	int index,
	int rocLength,
	int smaLength,
	Level1Fields priceType)
	{
		var sum = 0f;
		var hasValue = false;

		for (var j = 0; j < smaLength; j++)
		{
			var rocIndex = index - j;
			if (rocIndex < 0)
			{
				continue;
			}

			var roc = ComputeRoc(flatCandles, offset, rocIndex, rocLength, priceType);
			if (float.IsNaN(roc))
			{
				return float.NaN;
			}

			sum += roc;
			hasValue = true;
		}

		return hasValue ? sum / smaLength : float.NaN;
	}

	private static float ComputeRoc(
	ArrayView<GpuCandle> flatCandles,
	int offset,
	int index,
	int rocLength,
	Level1Fields priceType)
	{
		var current = ExtractPrice(flatCandles[offset + index], priceType);
		var prevIndex = index >= rocLength ? index - rocLength : 0;
		var previous = ExtractPrice(flatCandles[offset + prevIndex], priceType);

		if (previous == 0f)
		{
			return float.NaN;
		}

		return ((current - previous) / previous) * 100f;
	}
}

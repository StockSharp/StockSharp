namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Composite Momentum calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuCompositeMomentumParams"/> struct.
/// </remarks>
/// <param name="shortRocLength">Length for the short ROC component.</param>
/// <param name="longRocLength">Length for the long ROC component.</param>
/// <param name="rsiLength">Length for RSI calculation.</param>
/// <param name="emaFastLength">Length for fast EMA.</param>
/// <param name="emaSlowLength">Length for slow EMA.</param>
/// <param name="smaLength">Length for smoothing SMA.</param>
/// <param name="priceType">Price type for value extraction.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuCompositeMomentumParams(int shortRocLength, int longRocLength, int rsiLength, int emaFastLength, int emaSlowLength, int smaLength, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Length for the short ROC component.
	/// </summary>
	public int ShortRocLength = shortRocLength;

	/// <summary>
	/// Length for the long ROC component.
	/// </summary>
	public int LongRocLength = longRocLength;

	/// <summary>
	/// Length for RSI calculation.
	/// </summary>
	public int RsiLength = rsiLength;

	/// <summary>
	/// Length for fast EMA.
	/// </summary>
	public int EmaFastLength = emaFastLength;

	/// <summary>
	/// Length for slow EMA.
	/// </summary>
	public int EmaSlowLength = emaSlowLength;

	/// <summary>
	/// Length for smoothing SMA.
	/// </summary>
	public int SmaLength = smaLength;

	/// <summary>
	/// Price type for value extraction.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is CompositeMomentum composite)
		{
			Unsafe.AsRef(in this).ShortRocLength = GetLength(composite, "_roc1", ShortRocLength);
			Unsafe.AsRef(in this).LongRocLength = GetLength(composite, "_roc2", LongRocLength);
			Unsafe.AsRef(in this).RsiLength = GetLength(composite, "_rsi", RsiLength);
			Unsafe.AsRef(in this).EmaFastLength = GetLength(composite, "_emaFast", EmaFastLength);
			Unsafe.AsRef(in this).EmaSlowLength = GetLength(composite, "_emaSlow", EmaSlowLength);
			Unsafe.AsRef(in this).SmaLength = composite.Sma.Length;
		}
	}

	private static int GetLength(CompositeMomentum composite, string fieldName, int fallback)
	{
		var field = typeof(CompositeMomentum).GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		if (field?.GetValue(composite) is LengthIndicator<decimal> indicator)
			return indicator.Length;

		return fallback;
	}
}

/// <summary>
/// GPU result for Composite Momentum calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuCompositeMomentumResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTimeOffset.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Composite momentum line value.
	/// </summary>
	public float CompositeLine;

	/// <summary>
	/// Smoothed composite momentum (SMA).
	/// </summary>
	public float Sma;

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
		var isFormed = this.GetIsFormed();
		var cm = (CompositeMomentum)indicator;

		var value = new CompositeMomentumValue(cm, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		if (CompositeLine.IsNaN() || Sma.IsNaN())
		{
			value.IsEmpty = true;
			return value;
		}

		var composite = new DecimalIndicatorValue(cm.CompositeLine, (decimal)CompositeLine, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		value.Add(cm.CompositeLine, composite);

		var sma = new DecimalIndicatorValue(cm.Sma, (decimal)Sma, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		value.Add(cm.Sma, sma);

		return value;
	}
}

/// <summary>
/// GPU calculator for <see cref="CompositeMomentum"/> indicator.
/// </summary>
public class GpuCompositeMomentumCalculator : GpuIndicatorCalculatorBase<CompositeMomentum, GpuCompositeMomentumParams, GpuCompositeMomentumResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuCompositeMomentumResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuCompositeMomentumParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuCompositeMomentumCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuCompositeMomentumCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuCompositeMomentumResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuCompositeMomentumParams>>(CompositeMomentumParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuCompositeMomentumResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuCompositeMomentumParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuCompositeMomentumResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuCompositeMomentumResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuCompositeMomentumResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuCompositeMomentumResult[len];
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

	private static void CompositeMomentumParamsSeriesKernel(Index2D index, ArrayView<GpuCandle> flatCandles, ArrayView<GpuCompositeMomentumResult> flatResults, ArrayView<int> offsets, ArrayView<int> lengths, ArrayView<GpuCompositeMomentumParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var priceType = (Level1Fields)prm.PriceType;

		var shortLen = prm.ShortRocLength <= 0 ? 1 : prm.ShortRocLength;
		var longLen = prm.LongRocLength <= 0 ? 1 : prm.LongRocLength;
		var rsiLen = prm.RsiLength <= 1 ? 1 : prm.RsiLength;
		var emaFastLen = prm.EmaFastLength <= 0 ? 1 : prm.EmaFastLength;
		var emaSlowLen = prm.EmaSlowLength <= 0 ? 1 : prm.EmaSlowLength;
		var smaLen = prm.SmaLength <= 0 ? 1 : prm.SmaLength;

		var baseIndex = paramIdx * flatCandles.Length;

		float prevPrice = 0f;
		var hasPrevPrice = false;

		float gainSum = 0f, lossSum = 0f;
		float avgGain = 0f, avgLoss = 0f;
		var rsiSamples = 0;
		var rsiReady = false;

		float emaFastPrev = 0f, emaFastSum = 0f;
		var emaFastCount = 0;

		float emaSlowPrev = 0f, emaSlowSum = 0f;
		var emaSlowCount = 0;

		float smaSum = 0f;
		var smaCount = 0;

		for (var i = 0; i < len; i++)
		{
			var candleIndex = offset + i;
			var candle = flatCandles[candleIndex];
			var price = ExtractPrice(candle, priceType);

			float shortRoc = float.NaN;
			if (i >= shortLen)
			{
				var prevShort = ExtractPrice(flatCandles[candleIndex - shortLen], priceType);
				if (prevShort != 0f)
					shortRoc = (price - prevShort) / prevShort * 100f;
			}

			float longRoc = float.NaN;
			if (i >= longLen)
			{
				var prevLong = ExtractPrice(flatCandles[candleIndex - longLen], priceType);
				if (prevLong != 0f)
					longRoc = (price - prevLong) / prevLong * 100f;
			}

			float rsiValue = float.NaN;
			if (hasPrevPrice)
			{
				var delta = price - prevPrice;
				var gain = delta > 0f ? delta : 0f;
				var loss = delta < 0f ? -delta : 0f;

				if (!rsiReady)
				{
					gainSum += gain;
					lossSum += loss;
					rsiSamples++;

					if (rsiSamples >= rsiLen)
					{
						avgGain = gainSum / rsiLen;
						avgLoss = lossSum / rsiLen;
						rsiReady = true;
					}
				}
				else
				{
					avgGain = (avgGain * (rsiLen - 1) + gain) / rsiLen;
					avgLoss = (avgLoss * (rsiLen - 1) + loss) / rsiLen;
				}

				var curGain = rsiReady ? avgGain : gainSum / rsiLen;
				var curLoss = rsiReady ? avgLoss : lossSum / rsiLen;

				if (curLoss == 0f)
					rsiValue = 100f;
				else
				{
					var rs = curGain / curLoss;
					rsiValue = rs == 1f ? 0f : 100f - 100f / (1f + rs);
				}
			}

			prevPrice = price;
			hasPrevPrice = true;

			float emaFastValue;
			if (emaFastLen <= 1)
			{
				emaFastValue = price;
				emaFastPrev = price;
				emaFastCount = emaFastLen;
			}
			else if (emaFastCount < emaFastLen)
			{
				emaFastSum += price;
				emaFastCount++;
				emaFastValue = emaFastSum / emaFastLen;
				if (emaFastCount == emaFastLen)
					emaFastPrev = emaFastValue;
			}
			else
			{
				var multiplier = 2f / (emaFastLen + 1f);
				emaFastPrev = (price - emaFastPrev) * multiplier + emaFastPrev;
				emaFastValue = emaFastPrev;
			}

			float emaSlowValue;
			if (emaSlowLen <= 1)
			{
				emaSlowValue = price;
				emaSlowPrev = price;
				emaSlowCount = emaSlowLen;
			}
			else if (emaSlowCount < emaSlowLen)
			{
				emaSlowSum += price;
				emaSlowCount++;
				emaSlowValue = emaSlowSum / emaSlowLen;
				if (emaSlowCount == emaSlowLen)
					emaSlowPrev = emaSlowValue;
			}
			else
			{
				var multiplier = 2f / (emaSlowLen + 1f);
				emaSlowPrev = (price - emaSlowPrev) * multiplier + emaSlowPrev;
				emaSlowValue = emaSlowPrev;
			}

			var roc1Ready = i >= shortLen;
			var roc2Ready = i >= longLen;
			var rsiFormed = rsiReady && rsiSamples >= rsiLen;
			var emaFastFormed = emaFastLen <= 1 ? i >= 0 : emaFastCount >= emaFastLen;
			var emaSlowFormed = emaSlowLen <= 1 ? i >= 0 : emaSlowCount >= emaSlowLen;

			var compositeLine = float.NaN;
			var smaValue = float.NaN;
			byte formed = 0;

			if (roc1Ready && roc2Ready && rsiFormed && emaFastFormed && emaSlowFormed && !shortRoc.IsNaN() && !longRoc.IsNaN() && !rsiValue.IsNaN())
			{
				var normalizedShort = shortRoc / 100f;
				var normalizedLong = longRoc / 100f;
				var normalizedRsi = (rsiValue - 50f) / 50f;
				var macdLine = 0f;

				if (MathF.Abs(emaSlowValue) > float.Epsilon)
					macdLine = (emaFastValue - emaSlowValue) / emaSlowValue;

				compositeLine = (normalizedShort + normalizedLong + normalizedRsi + macdLine) / 4f * 100f;

				if (!compositeLine.IsNaN())
				{
					smaSum += compositeLine;
					smaCount++;

					if (smaCount > smaLen)
					{
						var prevIdx = baseIndex + candleIndex - smaLen;
						var prevResult = flatResults[prevIdx];
						if (!prevResult.CompositeLine.IsNaN())
							smaSum -= prevResult.CompositeLine;
						else
							smaSum -= 0f;
						smaCount = smaLen;
					}

					smaValue = smaSum / smaLen;

					if (smaCount >= smaLen)
						formed = 1;
				}
			}

			flatResults[baseIndex + candleIndex] = new GpuCompositeMomentumResult
			{
				Time = candle.Time,
				CompositeLine = compositeLine,
				Sma = smaValue,
				IsFormed = formed,
			};
		}
	}
}

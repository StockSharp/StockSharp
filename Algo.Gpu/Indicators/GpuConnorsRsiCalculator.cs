namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Connors RSI calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuConnorsRsiParams"/> struct.
/// </remarks>
/// <param name="rsiLength">RSI length.</param>
/// <param name="streakRsiLength">Streak RSI length.</param>
/// <param name="rocRsiLength">ROC RSI length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuConnorsRsiParams(int rsiLength, int streakRsiLength, int rocRsiLength, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength = rsiLength;

	/// <summary>
	/// Up/down RSI length.
	/// </summary>
	public int StreakRsiLength = streakRsiLength;

	/// <summary>
	/// ROC RSI length.
	/// </summary>
	public int RocRsiLength = rocRsiLength;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is ConnorsRSI connors)
		{
			Unsafe.AsRef(in this).RsiLength = connors.RSIPeriod;
			Unsafe.AsRef(in this).StreakRsiLength = connors.StreakRSIPeriod;
			Unsafe.AsRef(in this).RocRsiLength = connors.ROCRSIPeriod;
		}
	}
}

/// <summary>
/// GPU result for Connors RSI calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuConnorsRsiResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// RSI component.
	/// </summary>
	public float Rsi;

	/// <summary>
	/// Up/down RSI component.
	/// </summary>
	public float UpDownRsi;

	/// <summary>
	/// ROC RSI component.
	/// </summary>
	public float RocRsi;

	/// <summary>
	/// Composite Connors RSI value.
	/// </summary>
	public float Crsi;

	/// <summary>
	/// Is indicator formed (GPU-friendly byte flag).
	/// </summary>
	public byte IsFormed;

	readonly long IGpuIndicatorResult.Time => Time;
	readonly byte IGpuIndicatorResult.IsFormed => IsFormed;

	/// <inheritdoc />
	public readonly IIndicatorValue ToValue(IIndicator indicator)
	{
		var time = this.GetTime();
		var isFormed = this.GetIsFormed();
		var connors = (ConnorsRSI)indicator;

		if (Rsi.IsNaN() || UpDownRsi.IsNaN() || RocRsi.IsNaN() || Crsi.IsNaN())
		{
			return new ConnorsRSIValue(connors, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		var result = new ConnorsRSIValue(connors, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		result.Add(connors.Rsi, new DecimalIndicatorValue(connors.Rsi, (decimal)Rsi, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		result.Add(connors.UpDownRsi, new DecimalIndicatorValue(connors.UpDownRsi, (decimal)UpDownRsi, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		result.Add(connors.RocRsi, new DecimalIndicatorValue(connors.RocRsi, (decimal)RocRsi, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		result.Add(connors.CrsiLine, new DecimalIndicatorValue(connors.CrsiLine, (decimal)Crsi, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		return result;
	}
}

/// <summary>
/// GPU calculator for Connors RSI indicator.
/// </summary>
public class GpuConnorsRsiCalculator : GpuIndicatorCalculatorBase<ConnorsRSI, GpuConnorsRsiParams, GpuConnorsRsiResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuConnorsRsiResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuConnorsRsiParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuConnorsRsiCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuConnorsRsiCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuConnorsRsiResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuConnorsRsiParams>>(ConnorsRsiKernel);
	}

	/// <inheritdoc />
	public override GpuConnorsRsiResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuConnorsRsiParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuConnorsRsiResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuConnorsRsiResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuConnorsRsiResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuConnorsRsiResult[len];
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

	private static void ConnorsRsiKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuConnorsRsiResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuConnorsRsiParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var param = parameters[paramIdx];
		var priceType = (Level1Fields)param.PriceType;

		var rsiLen = param.RsiLength > 0 ? param.RsiLength : 1;
		var streakLen = param.StreakRsiLength > 0 ? param.StreakRsiLength : 1;
		var rocRsiLen = param.RocRsiLength > 0 ? param.RocRsiLength : 1;
		var rocLength = rocRsiLen;

		var totalCandles = flatCandles.Length;

		var hasPrevPrice = false;
		var prevPrice = 0f;
		var rsiAvgGain = 0f;
		var rsiAvgLoss = 0f;
		var rsiWarmup = 0;
		var rsiFormed = false;

		var hasPrevStreakBase = false;
		var prevStreakBase = 1f;
		var prevPriceForStreak = 0f;

		var hasPrevStreakValue = false;
		var prevStreakValue = 0f;
		var streakAvgGain = 0f;
		var streakAvgLoss = 0f;
		var streakWarmup = 0;
		var streakFormed = false;

		var hasPrevRocValue = false;
		var prevRocValue = 0f;
		var rocAvgGain = 0f;
		var rocAvgLoss = 0f;
		var rocWarmup = 0;
		var rocFormed = false;

		// CPU uses one-bar delay: the output value captures IsFormed BEFORE it is set.
		// So we track whether the indicator was formed on the previous bar.
		var wasFormed = false;

		for (var i = 0; i < len; i++)
		{
			var candleIndex = offset + i;
			var candle = flatCandles[candleIndex];
			var price = ExtractPrice(candle, priceType);

			var result = new GpuConnorsRsiResult
			{
				Time = candle.Time,
				Rsi = float.NaN,
				UpDownRsi = float.NaN,
				RocRsi = float.NaN,
				Crsi = float.NaN,
				IsFormed = 0,
			};

			var streak = 1f;
			if (hasPrevStreakBase)
			{
				if (price > prevPriceForStreak)
					streak = prevStreakBase > 0f ? prevStreakBase + 1f : 1f;
				else if (price < prevPriceForStreak)
					streak = prevStreakBase < 0f ? prevStreakBase - 1f : -1f;
				else
					streak = 0f;
			}

			var rsiValue = UpdateRsi(price, rsiLen, ref hasPrevPrice, ref prevPrice, ref rsiAvgGain, ref rsiAvgLoss, ref rsiWarmup, ref rsiFormed);

			var updownValue = UpdateRsi(streak, streakLen, ref hasPrevStreakValue, ref prevStreakValue, ref streakAvgGain, ref streakAvgLoss, ref streakWarmup, ref streakFormed);

			// CPU Momentum produces values from bar 0 using oldest available price.
			// Buffer[0] = price at bar max(0, i - rocLength).
			var rocValue = float.NaN;
			{
				var pastIdx = i >= rocLength ? (candleIndex - rocLength) : offset;
				var pastPrice = ExtractPrice(flatCandles[pastIdx], priceType);
				if (pastPrice != 0f)
					rocValue = (price - pastPrice) / pastPrice * 100f;
			}

			// RocRsi is always fed from bar 0 (matching CPU behavior).
			var rocRsiValue = float.NaN;
			if (!float.IsNaN(rocValue))
			{
				rocRsiValue = UpdateRsi(rocValue, rocRsiLen, ref hasPrevRocValue, ref prevRocValue, ref rocAvgGain, ref rocAvgLoss, ref rocWarmup, ref rocFormed);
			}

			if (!float.IsNaN(rsiValue))
				result.Rsi = rsiValue;

			if (!float.IsNaN(updownValue))
				result.UpDownRsi = updownValue;

			if (!float.IsNaN(rocRsiValue))
				result.RocRsi = rocRsiValue;

			// CPU IsFormed uses one-bar delay: the output value captures the
			// indicator's IsFormed from BEFORE the current bar's processing sets it.
			// Track with wasFormed to replicate this behavior.
			if (wasFormed)
				result.IsFormed = 1;

			// CPU condition: !rocValue.IsEmpty && Rsi.IsFormed && UpDownRsi.IsFormed && RocRsi.IsFormed && _roc.IsFormed
			// _roc.IsFormed = Momentum.Buffer.Count > Length, true when i >= rocLength.
			if (!wasFormed && rsiFormed && streakFormed && rocFormed && i >= rocLength && !float.IsNaN(rocValue))
				wasFormed = true;

			if (!float.IsNaN(result.Rsi) && !float.IsNaN(result.UpDownRsi) && !float.IsNaN(result.RocRsi))
			{
				result.Crsi = (result.Rsi + result.UpDownRsi + result.RocRsi) / 3f;
			}

			flatResults[paramIdx * totalCandles + candleIndex] = result;

			hasPrevStreakBase = true;
			prevStreakBase = streak;
			prevPriceForStreak = price;
		}
	}

	private static float UpdateRsi(
		float value,
		int length,
		ref bool hasPrevValue,
		ref float prevValue,
		ref float avgGain,
		ref float avgLoss,
		ref int warmupCount,
		ref bool formed)
	{
		if (length <= 0)
			length = 1;

		if (!hasPrevValue)
		{
			prevValue = value;
			hasPrevValue = true;
			return float.NaN;
		}

		var delta = value - prevValue;
		var gain = delta > 0f ? delta : 0f;
		var loss = delta < 0f ? -delta : 0f;

		if (!formed)
		{
			avgGain += gain;
			avgLoss += loss;
			warmupCount++;

			if (warmupCount >= length)
			{
				avgGain /= length;
				avgLoss /= length;
				formed = true;
			}
		}
		else
		{
			avgGain = (avgGain * (length - 1) + gain) / length;
			avgLoss = (avgLoss * (length - 1) + loss) / length;
		}

		prevValue = value;

		if (!formed)
			return float.NaN;

		if (avgLoss == 0f)
			return 100f;

		var rs = avgGain / avgLoss;

		if (rs == 1f)
			return 0f;

		return 100f - (100f / (1f + rs));
	}
}

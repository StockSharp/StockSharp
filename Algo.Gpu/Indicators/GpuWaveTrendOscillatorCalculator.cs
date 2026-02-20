namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Wave Trend Oscillator calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuWaveTrendOscillatorParams"/> struct.
/// </remarks>
/// <param name="esaPeriod">ESA period length.</param>
/// <param name="dPeriod">Period length for the EMA of price deviation.</param>
/// <param name="averagePeriod">Simple moving average period for WT2 line.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuWaveTrendOscillatorParams(int esaPeriod, int dPeriod, int averagePeriod) : IGpuIndicatorParams
{
	/// <summary>
	/// ESA period.
	/// </summary>
	public int EsaPeriod = esaPeriod;

	/// <summary>
	/// EMA period for deviation (D).
	/// </summary>
	public int DPeriod = dPeriod;

	/// <summary>
	/// SMA period for WT2 line.
	/// </summary>
	public int AveragePeriod = averagePeriod;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is WaveTrendOscillator wto)
		{
			Unsafe.AsRef(in this).EsaPeriod = wto.EsaPeriod;
			Unsafe.AsRef(in this).DPeriod = wto.DPeriod;
			Unsafe.AsRef(in this).AveragePeriod = wto.AveragePeriod;
		}
	}
}

/// <summary>
/// Complex GPU result for Wave Trend Oscillator calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuWaveTrendOscillatorResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// WT1 line value.
	/// </summary>
	public float Wt1;

	/// <summary>
	/// WT2 line value.
	/// </summary>
	public float Wt2;

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
		var wto = (WaveTrendOscillator)indicator;

		var value = new WaveTrendOscillatorValue(wto, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		if (Wt1.IsNaN() || Wt2.IsNaN())
		{
			value.IsEmpty = true;
			return value;
		}

		var wt1Value = new DecimalIndicatorValue(wto.Wt1, (decimal)Wt1, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		var wt2Value = new DecimalIndicatorValue(wto.Wt2, (decimal)Wt2, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		value.Add(wto.Wt1, wt1Value);
		value.Add(wto.Wt2, wt2Value);

		return value;
	}
}

/// <summary>
/// GPU calculator for Wave Trend Oscillator (WTO).
/// </summary>
public class GpuWaveTrendOscillatorCalculator : GpuIndicatorCalculatorBase<WaveTrendOscillator, GpuWaveTrendOscillatorParams, GpuWaveTrendOscillatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuWaveTrendOscillatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuWaveTrendOscillatorParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuWaveTrendOscillatorCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuWaveTrendOscillatorCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuWaveTrendOscillatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuWaveTrendOscillatorParams>>(WaveTrendKernel);
	}

	/// <inheritdoc />
	public override GpuWaveTrendOscillatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuWaveTrendOscillatorParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuWaveTrendOscillatorResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuWaveTrendOscillatorResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuWaveTrendOscillatorResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuWaveTrendOscillatorResult[len];
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
	/// ILGPU kernel performing Wave Trend Oscillator computation for multiple series and parameter sets.
	/// </summary>
	private static void WaveTrendKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuWaveTrendOscillatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuWaveTrendOscillatorParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var param = parameters[paramIdx];
		var esaPeriod = param.EsaPeriod;
		if (esaPeriod <= 0)
			esaPeriod = 1;

		var dPeriod = param.DPeriod;
		if (dPeriod <= 0)
			dPeriod = 1;

		var avgPeriod = param.AveragePeriod;
		if (avgPeriod <= 0)
			avgPeriod = 1;

		var esaMultiplier = 2f / (esaPeriod + 1f);
		var dMultiplier = 2f / (dPeriod + 1f);

		var baseResIndex = paramIdx * flatCandles.Length;

		float esaSum = 0f;
		float esaPrev = 0f;
		var esaFormed = false;

		float dSum = 0f;
		float dPrev = 0f;
		var dCount = 0;
		var dFormed = false;

		float wt2Sum = 0f;
		var validWtCount = 0;

		byte prevFormed = 0;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var resIndex = baseResIndex + globalIdx;

			var result = new GpuWaveTrendOscillatorResult
			{
				Time = candle.Time,
				Wt1 = float.NaN,
				Wt2 = float.NaN,
				IsFormed = 0,
			};

			var capo = (candle.High + candle.Low + candle.Close) / 3f;

			byte curFormed = 0;

			if (!esaFormed)
			{
				esaSum += capo;
				if (i == esaPeriod - 1)
				{
					esaPrev = esaSum / esaPeriod;
					esaFormed = true;
				}
			}
			else
			{
				esaPrev = (capo - esaPrev) * esaMultiplier + esaPrev;
			}

			if (esaFormed)
			{
				var absDiff = MathF.Abs(capo - esaPrev);

				if (!dFormed)
				{
					dSum += absDiff;
					dCount++;
					if (dCount == dPeriod)
					{
						dPrev = dSum / dPeriod;
						dFormed = true;
					}
				}
				else
				{
					dPrev = (absDiff - dPrev) * dMultiplier + dPrev;
				}

				if (dFormed)
				{
					var denom = 0.015f * dPrev;
					var diff = capo - esaPrev;
					var wt1 = denom != 0f ? diff / denom : 0f;
					result.Wt1 = wt1;

					wt2Sum += wt1;
					validWtCount++;

					if (validWtCount > avgPeriod)
					{
						var oldGlobalIdx = globalIdx - avgPeriod;
						var oldResIdx = baseResIndex + oldGlobalIdx;
						var oldWt1 = flatResults[oldResIdx].Wt1;
						wt2Sum -= oldWt1;
						validWtCount--;
					}

					result.Wt2 = wt2Sum / avgPeriod;
					curFormed = 1;
				}
			}

			result.IsFormed = prevFormed;
			flatResults[resIndex] = result;
			prevFormed = curFormed;
		}
	}
}

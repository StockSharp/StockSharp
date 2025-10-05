namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Chande Kroll Stop calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuChandeKrollStopParams"/> struct.
/// </remarks>
/// <param name="multiplier">Stop multiplier.</param>
/// <param name="period">Period for highest/lowest calculation.</param>
/// <param name="stopPeriod">Period for smoothing stop levels.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuChandeKrollStopParams(float multiplier, int period, int stopPeriod) : IGpuIndicatorParams
{
	/// <summary>
	/// Stop multiplier.
	/// </summary>
	public float Multiplier = multiplier;

	/// <summary>
	/// Period for highest/lowest calculation.
	/// </summary>
	public int Period = period;

	/// <summary>
	/// Period for smoothing stop levels.
	/// </summary>
	public int StopPeriod = stopPeriod;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is ChandeKrollStop cks)
		{
			Unsafe.AsRef(in this).Multiplier = (float)cks.Multiplier;
			Unsafe.AsRef(in this).Period = cks.Period;
			Unsafe.AsRef(in this).StopPeriod = cks.StopPeriod;
		}
	}
}

/// <summary>
/// GPU result for Chande Kroll Stop calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuChandeKrollStopResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTimeOffset.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Smoothed highest stop level.
	/// </summary>
	public float HighestStop;

	/// <summary>
	/// Smoothed lowest stop level.
	/// </summary>
	public float LowestStop;

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

		var cks = (ChandeKrollStop)indicator;

		if (HighestStop.IsNaN() || LowestStop.IsNaN())
		{
			return new ChandeKrollStopValue(cks, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		var value = new ChandeKrollStopValue(cks, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		value.Add(cks.Highest, new DecimalIndicatorValue(cks.Highest, (decimal)HighestStop, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		value.Add(cks.Lowest, new DecimalIndicatorValue(cks.Lowest, (decimal)LowestStop, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		return value;
	}
}

/// <summary>
/// GPU calculator for Chande Kroll Stop.
/// </summary>
public class GpuChandeKrollStopCalculator : GpuIndicatorCalculatorBase<ChandeKrollStop, GpuChandeKrollStopParams, GpuChandeKrollStopResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuChandeKrollStopResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuChandeKrollStopParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuChandeKrollStopCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuChandeKrollStopCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuChandeKrollStopResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuChandeKrollStopParams>>(ChandeKrollStopKernel);
	}

	/// <inheritdoc />
	public override GpuChandeKrollStopResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuChandeKrollStopParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuChandeKrollStopResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuChandeKrollStopResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuChandeKrollStopResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuChandeKrollStopResult[len];
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
	/// ILGPU kernel to compute Chande Kroll Stop for multiple series and parameter sets.
	/// </summary>
	private static void ChandeKrollStopKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuChandeKrollStopResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuChandeKrollStopParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var param = parameters[paramIdx];
		var period = param.Period;
		var stopPeriod = param.StopPeriod;
		var multiplier = param.Multiplier;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var resIndex = paramIdx * flatCandles.Length + globalIdx;

			var result = new GpuChandeKrollStopResult
			{
				Time = candle.Time,
				HighestStop = float.NaN,
				LowestStop = float.NaN,
				IsFormed = 0
			};

			if (period > 0 && stopPeriod > 0 && i >= period + stopPeriod - 2)
			{
				float sumHighStops = 0f;
				float sumLowStops = 0f;

				for (var j = 0; j < stopPeriod; j++)
				{
					var barIdx = i - j;
					var barGlobalIdx = offset + barIdx;

					float highest = float.MinValue;
					float lowest = float.MaxValue;

					var window = period;
					var available = barIdx + 1;
					if (window > available)
						window = available;

					for (var k = 0; k < window; k++)
					{
						var c = flatCandles[barGlobalIdx - k];
						var high = c.High;
						var low = c.Low;

						if (high > highest)
							highest = high;

						if (low < lowest)
							lowest = low;
					}

					var diff = highest - lowest;
					sumHighStops += highest - diff * multiplier;
					sumLowStops += lowest + diff * multiplier;
				}

				result.HighestStop = sumHighStops / stopPeriod;
				result.LowestStop = sumLowStops / stopPeriod;
				result.IsFormed = 1;
			}

			flatResults[resIndex] = result;
		}
	}
}

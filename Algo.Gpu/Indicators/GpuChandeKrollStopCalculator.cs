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
	/// Time in <see cref="DateTime.Ticks"/>.
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

		// Running SMA state for stop levels
		float smaHighSum = 0f;
		float smaLowSum = 0f;
		var smaCount = 0;

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

			if (period > 0 && i >= period - 1)
			{
				result.IsFormed = 1;

				// Compute highest/lowest over the current window of 'period' bars
				float highest = float.MinValue;
				float lowest = float.MaxValue;

				var window = period;
				var available = i + 1;
				if (window > available)
					window = available;

				for (var k = 0; k < window; k++)
				{
					var c = flatCandles[offset + i - k];
					var high = c.High;
					var low = c.Low;

					if (high > highest)
						highest = high;
					if (low < lowest)
						lowest = low;
				}

				var diff = highest - lowest;
				var stopHigh = highest - diff * multiplier;
				var stopLow = lowest + diff * multiplier;

				// Running SMA: CPU SMA pushes value and returns Sum / Length
				// When buffer is full (count >= stopPeriod), oldest value must be dropped.
				// We look back to find the oldest value to subtract.
				if (smaCount < stopPeriod)
				{
					smaHighSum += stopHigh;
					smaLowSum += stopLow;
					smaCount++;
				}
				else
				{
					// Subtract the oldest stop value (from stopPeriod bars ago in the stop sequence)
					// The oldest bar in the SMA window is at i - stopPeriod (in terms of bars with stops)
					// Since stops are computed every bar from period-1 onwards, the oldest bar is i - stopPeriod
					var oldBarIdx = i - stopPeriod;
					var oldGlobalIdx = offset + oldBarIdx;

					float oldHighest = float.MinValue;
					float oldLowest = float.MaxValue;

					var oldWindow = period;
					var oldAvailable = oldBarIdx + 1;
					if (oldWindow > oldAvailable)
						oldWindow = oldAvailable;

					for (var k = 0; k < oldWindow; k++)
					{
						var c = flatCandles[oldGlobalIdx - k];
						var high = c.High;
						var low = c.Low;

						if (high > oldHighest)
							oldHighest = high;
						if (low < oldLowest)
							oldLowest = low;
					}

					var oldDiff = oldHighest - oldLowest;
					var oldStopHigh = oldHighest - oldDiff * multiplier;
					var oldStopLow = oldLowest + oldDiff * multiplier;

					smaHighSum = smaHighSum - oldStopHigh + stopHigh;
					smaLowSum = smaLowSum - oldStopLow + stopLow;
				}

				// CPU SMA always divides by Length (stopPeriod), even when buffer not full
				result.HighestStop = smaHighSum / stopPeriod;
				result.LowestStop = smaLowSum / stopPeriod;
			}

			flatResults[resIndex] = result;
		}
	}
}

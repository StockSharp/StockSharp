namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Fibonacci Retracement calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuFibonacciRetracementParams"/> struct.
/// </remarks>
/// <param name="length">Lookback length for highest/lowest search.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuFibonacciRetracementParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// Lookback period length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is FibonacciRetracement retracement)
		{
			Unsafe.AsRef(in this).Length = retracement.Length;
		}
	}
}

/// <summary>
/// GPU result for Fibonacci Retracement calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuFibonacciRetracementResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTimeOffset.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Highest high within the lookback window.
	/// </summary>
	public float HighestHigh;

	/// <summary>
	/// Lowest low within the lookback window.
	/// </summary>
	public float LowestLow;

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

		var fib = (FibonacciRetracement)indicator;
		var value = new FibonacciRetracementValue(fib, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		if (HighestHigh.IsNaN() || LowestLow.IsNaN())
		{
			value.IsEmpty = true;
			return value;
		}

		var highest = (decimal)HighestHigh;
		var lowest = (decimal)LowestLow;

		foreach (var level in fib.Levels)
		{
			var levelPrice = lowest + (highest - lowest) * level.Level;
			var levelValue = new DecimalIndicatorValue(level, levelPrice, time)
			{
				IsFinal = true,
				IsFormed = true,
			};

			value.Add(level, levelValue);
		}

		return value;
	}
}

/// <summary>
/// GPU calculator for <see cref="FibonacciRetracement"/>.
/// </summary>
public class GpuFibonacciRetracementCalculator : GpuIndicatorCalculatorBase<FibonacciRetracement, GpuFibonacciRetracementParams, GpuFibonacciRetracementResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuFibonacciRetracementResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuFibonacciRetracementParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuFibonacciRetracementCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuFibonacciRetracementCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index3D, ArrayView<GpuCandle>, ArrayView<GpuFibonacciRetracementResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuFibonacciRetracementParams>>(FibonacciRetracementParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuFibonacciRetracementResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuFibonacciRetracementParams[] parameters)
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
		var maxLen = 0;
		var offset = 0;
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			if (len > 0)
			{
				Array.Copy(candlesSeries[s], 0, flatCandles, offset, len);
				offset += len;
				if (len > maxLen)
					maxLen = len;
			}
		}

		using var inputBuffer = Accelerator.Allocate1D(flatCandles);
		using var offsetsBuffer = Accelerator.Allocate1D(seriesOffsets);
		using var lengthsBuffer = Accelerator.Allocate1D(seriesLengths);
		using var paramsBuffer = Accelerator.Allocate1D(parameters);
		using var outputBuffer = Accelerator.Allocate1D<GpuFibonacciRetracementResult>(totalSize * parameters.Length);

		var extent = new Index3D(parameters.Length, seriesCount, maxLen);
		_paramsSeriesKernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuFibonacciRetracementResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuFibonacciRetracementResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuFibonacciRetracementResult[len];
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
	/// ILGPU kernel: Fibonacci Retracement computation for multiple series and parameter sets.
	/// </summary>
	private static void FibonacciRetracementParamsSeriesKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuFibonacciRetracementResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuFibonacciRetracementParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;
		var candleIdx = index.Z;

		var len = lengths[seriesIdx];
		if (candleIdx >= len)
			return;

		var offset = offsets[seriesIdx];
		var globalIdx = offset + candleIdx;

		var candle = flatCandles[globalIdx];
		var resIndex = paramIdx * flatCandles.Length + globalIdx;

		var lookback = parameters[paramIdx].Length;
		if (lookback <= 0)
			lookback = 1;

		var start = candleIdx - lookback + 1;
		if (start < 0)
			start = 0;

		var firstCandle = flatCandles[offset + start];
		var highest = firstCandle.High;
		var lowest = firstCandle.Low;

		for (var j = start + 1; j <= candleIdx; j++)
		{
			var c = flatCandles[offset + j];
			if (c.High > highest)
				highest = c.High;
			if (c.Low < lowest)
				lowest = c.Low;
		}

		var formed = candleIdx >= lookback - 1 ? (byte)1 : (byte)0;

		flatResults[resIndex] = new GpuFibonacciRetracementResult
		{
			Time = candle.Time,
			HighestHigh = highest,
			LowestLow = lowest,
			IsFormed = formed,
		};
	}
}

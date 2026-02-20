namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Aroon Oscillator calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuAroonOscillatorParams"/> struct.
/// </remarks>
/// <param name="length">Aroon Oscillator period length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuAroonOscillatorParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// Period length for Aroon Oscillator.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is AroonOscillator aroonOscillator)
		{
			Unsafe.AsRef(in this).Length = aroonOscillator.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Aroon Oscillator indicator.
/// </summary>
public class GpuAroonOscillatorCalculator : GpuIndicatorCalculatorBase<AroonOscillator, GpuAroonOscillatorParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuAroonOscillatorParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuAroonOscillatorCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuAroonOscillatorCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuAroonOscillatorParams>>(AroonOscillatorParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuAroonOscillatorParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuIndicatorResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuIndicatorResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuIndicatorResult[len];
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
	/// ILGPU kernel: Aroon Oscillator using incremental state machine matching CPU AroonUp/AroonDown.
	/// </summary>
	private static void AroonOscillatorParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuAroonOscillatorParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var L = parameters[paramIdx].Length;
		if (L <= 0)
			L = 1;

		var maxHigh = float.MinValue;
		var maxAge = 0;
		var minLow = float.MaxValue;
		var minAge = 0;
		var bufCount = 0;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var resIndex = paramIdx * flatCandles.Length + globalIdx;
			var high = candle.High;
			var low = candle.Low;

			// AroonUp: track max high (matches CPU >= tie-breaking)
			if (high >= maxHigh)
			{
				maxHigh = high;
				maxAge = 0;
			}
			else
			{
				maxAge++;
			}

			// AroonDown: track min low (matches CPU <= tie-breaking)
			if (low <= minLow)
			{
				minLow = low;
				minAge = 0;
			}
			else
			{
				minAge++;
			}

			// Buffer overflow handling (rescan when oldest value leaves window)
			if (bufCount == L)
			{
				// AroonUp rescan
				var removedHigh = flatCandles[globalIdx - L].High;
				if (removedHigh == maxHigh)
				{
					maxHigh = high;
					maxAge = 0;
					for (var j = 1; j < L; j++)
					{
						var bHigh = flatCandles[globalIdx - L + j].High;
						if (bHigh > maxHigh)
						{
							maxHigh = bHigh;
							maxAge = j;
						}
					}
				}

				// AroonDown rescan
				var removedLow = flatCandles[globalIdx - L].Low;
				if (removedLow == minLow)
				{
					minLow = low;
					minAge = 0;
					for (var j = 1; j < L; j++)
					{
						var bLow = flatCandles[globalIdx - L + j].Low;
						if (bLow < minLow)
						{
							minLow = bLow;
							minAge = j;
						}
					}
				}
			}
			else
			{
				bufCount++;
			}

			if (bufCount >= L)
			{
				var up = 100f * (L - maxAge) / L;
				var down = 100f * (L - minAge) / L;
				flatResults[resIndex] = new() { Time = candle.Time, Value = up - down, IsFormed = 1 };
			}
			else
			{
				flatResults[resIndex] = new() { Time = candle.Time, Value = float.NaN, IsFormed = 0 };
			}
		}
	}
}

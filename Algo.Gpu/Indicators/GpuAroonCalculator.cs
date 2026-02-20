namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Aroon calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuAroonParams"/> struct.
/// </remarks>
/// <param name="length">Indicator period length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuAroonParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// Aroon period length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is Aroon aroon)
		{
			Unsafe.AsRef(in this).Length = aroon.Length;
		}
	}
}

/// <summary>
/// GPU result for Aroon indicator.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuAroonResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Aroon Up value.
	/// </summary>
	public float Up;

	/// <summary>
	/// Aroon Down value.
	/// </summary>
	public float Down;

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
		var aroon = (Aroon)indicator;

		if (Up.IsNaN() || Down.IsNaN())
		{
			return new AroonValue(aroon, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		var value = new AroonValue(aroon, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		value.Add(aroon.Up, new DecimalIndicatorValue(aroon.Up, (decimal)Up, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		value.Add(aroon.Down, new DecimalIndicatorValue(aroon.Down, (decimal)Down, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		return value;
	}
}

/// <summary>
/// GPU calculator for Aroon indicator.
/// </summary>
public class GpuAroonCalculator : GpuIndicatorCalculatorBase<Aroon, GpuAroonParams, GpuAroonResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuAroonResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuAroonParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuAroonCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuAroonCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuAroonResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuAroonParams>>(AroonParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuAroonResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuAroonParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuAroonResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuAroonResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuAroonResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuAroonResult[len];
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
	/// ILGPU kernel computing Aroon for multiple series and parameter sets.
	/// Replicates the CPU AroonUp/AroonDown incremental state machine exactly.
	/// </summary>
	private static void AroonParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuAroonResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuAroonParams> parameters)
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
		// Complex indicator delay: value.IsFormed is captured BEFORE inner processing
		byte prevFormed = 0;

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

			if (prevFormed != 0)
			{
				flatResults[resIndex] = new GpuAroonResult
				{
					Time = candle.Time,
					Up = 100f * (L - maxAge) / L,
					Down = 100f * (L - minAge) / L,
					IsFormed = 1,
				};
			}
			else
			{
				flatResults[resIndex] = new GpuAroonResult
				{
					Time = candle.Time,
					Up = float.NaN,
					Down = float.NaN,
					IsFormed = 0,
				};
			}

			if (bufCount >= L)
				prevFormed = 1;
		}
	}
}

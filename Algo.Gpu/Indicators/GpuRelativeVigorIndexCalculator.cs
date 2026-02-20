namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Relative Vigor Index (RVI) calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuRelativeVigorIndexParams"/> struct.
/// </remarks>
/// <param name="averageLength">Length of the weighted average part.</param>
/// <param name="signalLength">Length of the weighted signal part.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuRelativeVigorIndexParams(int averageLength, int signalLength) : IGpuIndicatorParams
{
	/// <summary>
	/// Length of the weighted average part.
	/// </summary>
	public int AverageLength = averageLength;

	/// <summary>
	/// Length of the weighted signal part.
	/// </summary>
	public int SignalLength = signalLength;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is RelativeVigorIndex rvi)
		{
			Unsafe.AsRef(in this).AverageLength = rvi.Average.Length;
			Unsafe.AsRef(in this).SignalLength = rvi.Signal.Length;
		}
	}
}

/// <summary>
/// GPU result for Relative Vigor Index (RVI) calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuRelativeVigorIndexResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Weighted average value of the RVI.
	/// </summary>
	public float Average;

	/// <summary>
	/// Weighted signal value of the RVI.
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
		var time = this.GetTime();
		var isFormed = this.GetIsFormed();

		var rvi = (RelativeVigorIndex)indicator;

		if (Average.IsNaN() || Signal.IsNaN())
		{
			return new RelativeVigorIndexValue(rvi, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		var value = new RelativeVigorIndexValue(rvi, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		value.Add(rvi.Average, new DecimalIndicatorValue(rvi.Average, (decimal)Average, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		value.Add(rvi.Signal, new DecimalIndicatorValue(rvi.Signal, (decimal)Signal, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		return value;
	}
}

/// <summary>
/// GPU calculator for Relative Vigor Index (RVI).
/// </summary>
public class GpuRelativeVigorIndexCalculator : GpuIndicatorCalculatorBase<RelativeVigorIndex, GpuRelativeVigorIndexParams, GpuRelativeVigorIndexResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuRelativeVigorIndexResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuRelativeVigorIndexParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuRelativeVigorIndexCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuRelativeVigorIndexCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuRelativeVigorIndexResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuRelativeVigorIndexParams>>(RelativeVigorIndexParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuRelativeVigorIndexResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuRelativeVigorIndexParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuRelativeVigorIndexResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuRelativeVigorIndexResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuRelativeVigorIndexResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuRelativeVigorIndexResult[len];
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

	/// <summary>
	/// ILGPU kernel: RVI computation for multiple series and multiple parameter sets.
	/// </summary>
	private static void RelativeVigorIndexParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuRelativeVigorIndexResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuRelativeVigorIndexParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var offset = offsets[seriesIdx];
		var param = parameters[paramIdx];

		var avgLength = param.AverageLength;
		if (avgLength < 4)
			avgLength = 4;

		var signalLength = param.SignalLength;
		if (signalLength < 4)
			signalLength = 4;

		var avgValidCount = 0;

		byte prevFormed = 0;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];

			var resIndex = paramIdx * flatCandles.Length + globalIdx;

			var averageValue = float.NaN;
			var signalValue = float.NaN;
			byte curFormed = 0;

			// CPU RelativeVigorIndexAverage has a candle buffer of capacity avgLength.
			// It becomes formed when _buffer.Count >= avgLength (i.e., at bar avgLength-1).
			// When formed, it uses _buffer[0..3]: the 4 OLDEST candles in the circular buffer.
			// _buffer[0] = candle at (i - avgLength + 1), etc.
			if (i >= avgLength - 1)
			{
				var j = offset + i - avgLength + 1; // oldest candle in buffer
				var c0 = flatCandles[j];
				var c1 = flatCandles[j + 1];
				var c2 = flatCandles[j + 2];
				var c3 = flatCandles[j + 3];

				var valueUp = ((c0.Close - c0.Open) + 2f * (c1.Close - c1.Open) + 2f * (c2.Close - c2.Open) + (c3.Close - c3.Open)) / 6f;
				var valueDn = ((c0.High - c0.Low) + 2f * (c1.High - c1.Low) + 2f * (c2.High - c2.Low) + (c3.High - c3.Low)) / 6f;

				averageValue = valueDn == 0f ? valueUp : valueUp / valueDn;

				// Store average in result for signal lookback
				flatResults[resIndex] = new GpuRelativeVigorIndexResult
				{
					Time = candle.Time,
					Average = averageValue,
					Signal = float.NaN,
					IsFormed = 0,
				};

				avgValidCount++;

				// CPU RelativeVigorIndexSignal has Buffer of capacity signalLength.
				// Formed when Buffer.Count >= signalLength. Uses Buffer[0..3] (oldest 4 values).
				if (avgValidCount >= signalLength)
				{
					// Look back to find the 4 oldest Average values in the signal window
					var signalStart = i - signalLength + 1; // oldest bar in signal window (relative)
					// That bar's Average is at avgLength-1 offset from series start
					var idx0 = paramIdx * flatCandles.Length + (offset + signalStart);
					var idx1 = paramIdx * flatCandles.Length + (offset + signalStart + 1);
					var idx2 = paramIdx * flatCandles.Length + (offset + signalStart + 2);
					var idx3 = paramIdx * flatCandles.Length + (offset + signalStart + 3);

					var v0 = flatResults[idx0].Average;
					var v1 = flatResults[idx1].Average;
					var v2 = flatResults[idx2].Average;
					var v3 = flatResults[idx3].Average;

					signalValue = (v0 + 2f * v1 + 2f * v2 + v3) / 6f;
					curFormed = 1;
				}
			}

			flatResults[resIndex] = new GpuRelativeVigorIndexResult
			{
				Time = candle.Time,
				Average = averageValue,
				Signal = curFormed == 1 ? signalValue : float.NaN,
				IsFormed = prevFormed,
			};
			prevFormed = curFormed;
		}
	}
}

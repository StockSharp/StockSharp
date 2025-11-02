namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Trough (ZigZag lows) calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuTroughParams"/> struct.
/// </remarks>
/// <param name="deviation">Deviation threshold.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuTroughParams(float deviation) : IGpuIndicatorParams
{
	/// <summary>
	/// Percentage deviation threshold used to detect direction change.
	/// </summary>
	public float Deviation = deviation;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is ZigZag zigzag)
		{
			Unsafe.AsRef(in this).Deviation = (float)zigzag.Deviation;
		}
	}
}

/// <summary>
/// GPU result for Trough (ZigZag lows) calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuTroughResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Calculated extremum value.
	/// </summary>
	public float Value;

	/// <summary>
	/// Shift (number of bars back to the extremum).
	/// </summary>
	public int Shift;

	/// <summary>
	/// Is extremum detected on up trend (byte to remain GPU friendly).
	/// </summary>
	public byte IsUp;

	/// <summary>
	/// Is indicator formed (byte to remain GPU friendly).
	/// </summary>
	public byte IsFormed;

	readonly long IGpuIndicatorResult.Time => Time;

	readonly byte IGpuIndicatorResult.IsFormed => IsFormed;

	/// <inheritdoc />
	public readonly IIndicatorValue ToValue(IIndicator indicator)
	{
		var time = this.GetTime();
		var isFormed = this.GetIsFormed();

		var zigZag = (ZigZag)indicator;

		if (Value.IsNaN() || IsUp != 0)
		{
			return new ZigZagIndicatorValue(zigZag, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
			};
		}

		return new ZigZagIndicatorValue(zigZag, (decimal)Value, Shift, time, false)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};
	}
}

/// <summary>
/// GPU calculator for <see cref="Trough"/> indicator.
/// </summary>
public class GpuTroughCalculator : GpuIndicatorCalculatorBase<Trough, GpuTroughParams, GpuTroughResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuTroughResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuTroughParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuTroughCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuTroughCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuTroughResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuTroughParams>>(TroughParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuTroughResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuTroughParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuTroughResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuTroughResult[seriesCount][][];

		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuTroughResult[parameters.Length][];

			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuTroughResult[len];

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
	/// ILGPU kernel that evaluates Trough (ZigZag lows) for multiple series and parameter sets.
	/// </summary>
	private static void TroughParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuTroughResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuTroughParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];

		if (len <= 0)
			return;

		var deviation = parameters[paramIdx].Deviation;
		if (deviation <= 0f)
			deviation = 0.000001f;

		var resBase = paramIdx * flatCandles.Length + offset;

		var firstCandle = flatCandles[offset];
		flatResults[resBase] = new GpuTroughResult
		{
			Time = firstCandle.Time,
			Value = float.NaN,
			Shift = 0,
			IsUp = 0,
			IsFormed = 0
		};

		var lastExtremum = 0f;
		var hasExtremum = false;
		var isUpTrend = false;
		var trendInitialized = false;
		var shift = 0;
		var prevPrice = firstCandle.Low;

		for (var i = 1; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var price = candle.Low;
			var resIndex = resBase + i;

			if (!hasExtremum)
			{
				lastExtremum = price;
				hasExtremum = true;
			}

			if (!trendInitialized)
			{
				isUpTrend = price >= prevPrice;
				trendInitialized = true;
			}

			var currentTrend = isUpTrend;
			var threshold = lastExtremum * deviation;
			var changeTrend = false;

			if (currentTrend)
			{
				if (lastExtremum < price)
				{
					lastExtremum = price;
				}
				else if (price <= (lastExtremum - threshold))
				{
					changeTrend = true;
				}
			}
			else
			{
				if (lastExtremum > price)
				{
					lastExtremum = price;
				}
				else if (price >= (lastExtremum + threshold))
				{
					changeTrend = true;
				}
			}

			if (changeTrend)
			{
				flatResults[resIndex] = new GpuTroughResult
				{
					Time = candle.Time,
					Value = lastExtremum,
					Shift = shift,
					IsUp = (byte)(currentTrend ? 1 : 0),
					IsFormed = 1
				};

				isUpTrend = !currentTrend;
				lastExtremum = price;
				shift = 1;
			}
			else
			{
				flatResults[resIndex] = new GpuTroughResult
				{
					Time = candle.Time,
					Value = float.NaN,
					Shift = 0,
					IsUp = (byte)(currentTrend ? 1 : 0),
					IsFormed = 1
				};

				isUpTrend = currentTrend;
				shift++;
			}

			prevPrice = price;
		}
	}
}

namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Pivot Points calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuPivotPointsParams"/> struct.
/// Pivot Points do not require any additional parameters.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct GpuPivotPointsParams : IGpuIndicatorParams
{
	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
	}
}

/// <summary>
/// Complex GPU result for Pivot Points calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuPivotPointsResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTimeOffset.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Pivot Point value.
	/// </summary>
	public float PivotPoint;

	/// <summary>
	/// Resistance level R1.
	/// </summary>
	public float R1;

	/// <summary>
	/// Resistance level R2.
	/// </summary>
	public float R2;

	/// <summary>
	/// Support level S1.
	/// </summary>
	public float S1;

	/// <summary>
	/// Support level S2.
	/// </summary>
	public float S2;

	/// <summary>
	/// Is indicator formed (byte to be GPU-friendly).
	/// </summary>
	public byte IsFormed;

	readonly long IGpuIndicatorResult.Time => Time;
	readonly byte IGpuIndicatorResult.IsFormed => IsFormed;

	/// <inheritdoc />
	public readonly IIndicatorValue ToValue(IIndicator indicator)
	{
		var pivotPoints = (PivotPoints)indicator;
		var time = this.GetTime();
		var isFormed = this.GetIsFormed();

		if (PivotPoint.IsNaN() || R1.IsNaN() || R2.IsNaN() || S1.IsNaN() || S2.IsNaN())
		{
			return new PivotPointsValue(pivotPoints, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		var value = new PivotPointsValue(pivotPoints, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		value.Add(pivotPoints.PivotPoint, new DecimalIndicatorValue(pivotPoints.PivotPoint, (decimal)PivotPoint, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		value.Add(pivotPoints.R1, new DecimalIndicatorValue(pivotPoints.R1, (decimal)R1, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		value.Add(pivotPoints.R2, new DecimalIndicatorValue(pivotPoints.R2, (decimal)R2, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		value.Add(pivotPoints.S1, new DecimalIndicatorValue(pivotPoints.S1, (decimal)S1, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		value.Add(pivotPoints.S2, new DecimalIndicatorValue(pivotPoints.S2, (decimal)S2, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		return value;
	}
}

/// <summary>
/// GPU calculator for Pivot Points indicator.
/// </summary>
public class GpuPivotPointsCalculator : GpuIndicatorCalculatorBase<PivotPoints, GpuPivotPointsParams, GpuPivotPointsResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuPivotPointsResult>, ArrayView<int>, ArrayView<int>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuPivotPointsCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuPivotPointsCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index3D, ArrayView<GpuCandle>, ArrayView<GpuPivotPointsResult>, ArrayView<int>, ArrayView<int>>(PivotPointsKernel);
	}

	/// <inheritdoc />
	public override GpuPivotPointsResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuPivotPointsParams[] parameters)
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
		var maxLen = 0;

		for (var s = 0; s < seriesCount; s++)
		{
			seriesOffsets[s] = totalSize;
			var len = candlesSeries[s]?.Length ?? 0;
			seriesLengths[s] = len;
			totalSize += len;
			if (len > maxLen)
				maxLen = len;
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
		using var outputBuffer = Accelerator.Allocate1D<GpuPivotPointsResult>(totalSize * parameters.Length);

		var extent = new Index3D(parameters.Length, seriesCount, maxLen);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuPivotPointsResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuPivotPointsResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuPivotPointsResult[len];
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
	/// ILGPU kernel: Pivot Points computation for multiple series and parameter slots.
	/// </summary>
	private static void PivotPointsKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuPivotPointsResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths)
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

		var high = candle.High;
		var low = candle.Low;
		var close = candle.Close;
		var range = high - low;
		var pivot = (high + low + close) / 3f;

		flatResults[resIndex] = new GpuPivotPointsResult
		{
			Time = candle.Time,
			PivotPoint = pivot,
			R1 = 2f * pivot - low,
			R2 = pivot + range,
			S1 = 2f * pivot - high,
			S2 = pivot - range,
			IsFormed = 1,
		};
	}
}

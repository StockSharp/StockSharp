namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU ZigZag calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuZigZagParams"/> struct.
/// </remarks>
/// <param name="deviation">Deviation threshold in relative units.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuZigZagParams(float deviation, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Deviation threshold in relative units.
	/// </summary>
	public float Deviation = deviation;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is ZigZag zigZag)
		{
			Unsafe.AsRef(in this).Deviation = (float)zigZag.Deviation;
		}
	}
}

/// <summary>
/// GPU result for ZigZag calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuZigZagResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTimeOffset.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Calculated ZigZag value.
	/// </summary>
	public float Value;

	/// <summary>
	/// Number of bars since the last extremum.
	/// </summary>
	public int Shift;

	/// <summary>
	/// Direction flag of the last extremum.
	/// </summary>
	public byte IsUp;

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
		var zigZag = (ZigZag)indicator;

		if (Value.IsNaN())
		{
			return new ZigZagIndicatorValue(zigZag, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		return new ZigZagIndicatorValue(zigZag, (decimal)Value, Shift, time, IsUp != 0)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};
	}
}

/// <summary>
/// GPU calculator for ZigZag indicator.
/// </summary>
public class GpuZigZagCalculator : GpuIndicatorCalculatorBase<ZigZag, GpuZigZagParams, GpuZigZagResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuZigZagResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuZigZagParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuZigZagCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuZigZagCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuZigZagResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuZigZagParams>>(ZigZagParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuZigZagResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuZigZagParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuZigZagResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuZigZagResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuZigZagResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuZigZagResult[len];
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
	/// ILGPU kernel: ZigZag computation for multiple series and multiple parameter sets.
	/// </summary>
	private static void ZigZagParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuZigZagResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuZigZagParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var deviation = MathF.Abs(prm.Deviation);
		var priceType = (Level1Fields)prm.PriceType;
		var totalCandles = flatCandles.Length;

		var prevPrice = ExtractPrice(flatCandles[offset], priceType);
		var lastExtremum = 0f;
		var hasLastExtremum = false;
		var isUpTrend = false;
		var hasTrend = false;
		var shift = 0;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var price = ExtractPrice(candle, priceType);
			var globalIdx = offset + i;
			var resIndex = paramIdx * totalCandles + globalIdx;
			var formed = i >= 1;

			if (!formed)
			{
				flatResults[resIndex] = new GpuZigZagResult
				{
					Time = candle.Time,
					Value = float.NaN,
					Shift = 0,
					IsUp = 0,
					IsFormed = 0,
				};
				prevPrice = price;
				continue;
			}

			if (!hasLastExtremum)
			{
				lastExtremum = price;
				hasLastExtremum = true;
			}

			if (!hasTrend)
			{
				isUpTrend = price >= prevPrice;
				hasTrend = true;
			}

			var threshold = MathF.Abs(lastExtremum * deviation);
			var changeTrend = false;

			if (isUpTrend)
			{
				if (lastExtremum < price)
				{
					lastExtremum = price;
				}
				else if (price <= lastExtremum - threshold)
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
				else if (price >= lastExtremum + threshold)
				{
					changeTrend = true;
				}
			}

			var result = new GpuZigZagResult
			{
				Time = candle.Time,
				Value = float.NaN,
				Shift = shift,
				IsUp = (byte)(isUpTrend ? 1 : 0),
				IsFormed = 1,
			};

			if (changeTrend)
			{
				result.Value = lastExtremum;
				flatResults[resIndex] = result;

				isUpTrend = !isUpTrend;
				lastExtremum = price;
				shift = 1;
			}
			else
			{
				flatResults[resIndex] = result;
				shift++;
			}

			prevPrice = price;
		}
	}
}

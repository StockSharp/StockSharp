namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Peak calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuPeakParams"/> struct.
/// </remarks>
/// <param name="deviation">Percentage change threshold expressed as a fraction (0-1).</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuPeakParams(float deviation) : IGpuIndicatorParams
{
	/// <summary>
	/// Percentage change threshold expressed as a fraction (0-1).
	/// </summary>
	public float Deviation = deviation;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is Peak peak)
		{
			Unsafe.AsRef(in this).Deviation = (float)peak.Deviation;
		}
	}
}

/// <summary>
/// GPU result for Peak calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuPeakResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Calculated peak value.
	/// </summary>
	public float Value;

	/// <summary>
	/// Shift (number of bars since the previous extremum).
	/// </summary>
	public int Shift;

	/// <summary>
	/// Indicator direction flag (1 - up trend, 0 - down trend).
	/// </summary>
	public byte IsUp;

	/// <summary>
	/// Flag indicating presence of a valid value (1 - has value, 0 - empty).
	/// </summary>
	public byte HasValue;

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

		if (HasValue == 0 || Value.IsNaN())
		{
			return new ZigZagIndicatorValue(indicator, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
			};
		}

		var zigZagValue = new ZigZagIndicatorValue(indicator, (decimal)Value, Shift, time, IsUp != 0)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		return zigZagValue;
	}
}

/// <summary>
/// GPU calculator for <see cref="Peak"/> indicator.
/// </summary>
public class GpuPeakCalculator : GpuIndicatorCalculatorBase<Peak, GpuPeakParams, GpuPeakResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuPeakResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuPeakParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuPeakCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuPeakCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuPeakResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuPeakParams>>(PeakParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuPeakResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuPeakParams[] parameters)
	{
		ArgumentNullException.ThrowIfNull(candlesSeries);
		ArgumentNullException.ThrowIfNull(parameters);

		if (candlesSeries.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(candlesSeries));

		if (parameters.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(parameters));

		var seriesCount = candlesSeries.Length;

		// Flatten input
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
			if (len <= 0)
				continue;

			var series = candlesSeries[s]!;
			Array.Copy(series, 0, flatCandles, offset, len);
			offset += len;
		}

		using var inputBuffer = Accelerator.Allocate1D(flatCandles);
		using var offsetsBuffer = Accelerator.Allocate1D(seriesOffsets);
		using var lengthsBuffer = Accelerator.Allocate1D(seriesLengths);
		using var paramsBuffer = Accelerator.Allocate1D(parameters);
		using var outputBuffer = Accelerator.Allocate1D<GpuPeakResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		// Re-split [series][param][bar]
		var result = new GpuPeakResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuPeakResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				if (len <= 0)
				{
					result[s][p] = Array.Empty<GpuPeakResult>();
					continue;
				}

				var arr = new GpuPeakResult[len];
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
	/// ILGPU kernel: Peak computation for multiple series and multiple parameter sets.
	/// One thread handles one (parameter, series) pair and iterates bars sequentially using candle High price.
	/// </summary>
	private static void PeakParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuPeakResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuPeakParams> parameters)
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

		var prevPrice = 0f;
		var hasPrev = false;
		var lastExtremum = 0f;
		var hasLastExtremum = false;
		var isUpTrend = false;
		var hasTrend = false;
		var shift = 0;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var price = candle.High;
			var resIndex = paramIdx * flatCandles.Length + (offset + i);
			var isFormed = (byte)(i >= 1 ? 1 : 0);

			byte hasValue = 0;
			var value = float.NaN;
			var shiftValue = 0;
			byte isUpValue = 0;

			if (!hasPrev)
			{
				prevPrice = price;
				hasPrev = true;

				flatResults[resIndex] = new GpuPeakResult
				{
					Time = candle.Time,
					Value = value,
					Shift = shiftValue,
					IsUp = isUpValue,
					HasValue = hasValue,
					IsFormed = isFormed,
				};

				continue;
			}

			var lastExt = hasLastExtremum ? lastExtremum : price;
			var upTrend = hasTrend ? isUpTrend : price >= prevPrice;
			var threshold = lastExt * deviation;
			var changeTrend = false;

			if (upTrend)
			{
				if (lastExt < price)
				{
					lastExt = price;
				}
				else if (price <= (lastExt - threshold))
				{
					changeTrend = true;
				}
			}
			else
			{
				if (lastExt > price)
				{
					lastExt = price;
				}
				else if (price >= (lastExt + threshold))
				{
					changeTrend = true;
				}
			}

			if (changeTrend)
			{
				if (upTrend)
				{
					hasValue = 1;
					value = lastExt;
					shiftValue = shift;
					isUpValue = 1;
				}

				lastExtremum = price;
				hasLastExtremum = true;
				isUpTrend = !upTrend;
				hasTrend = true;
				shift = 1;
			}
			else
			{
				lastExtremum = lastExt;
				hasLastExtremum = true;
				isUpTrend = upTrend;
				hasTrend = true;
				if (shift < int.MaxValue)
					shift++;
			}

			prevPrice = price;

			flatResults[resIndex] = new GpuPeakResult
			{
				Time = candle.Time,
				Value = value,
				Shift = shiftValue,
				IsUp = isUpValue,
				HasValue = hasValue,
				IsFormed = isFormed,
			};
		}
	}
}

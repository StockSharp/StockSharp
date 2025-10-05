namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Median calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuMedianParams"/> struct.
/// </remarks>
/// <param name="length">Median window length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuMedianParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Median window length.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is Median median)
		{
			Unsafe.AsRef(in this).Length = median.Length;
		}
	}
}

/// <summary>
/// GPU calculator for the Median indicator.
/// </summary>
public class GpuMedianCalculator : GpuIndicatorCalculatorBase<Median, GpuMedianParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMedianParams>, ArrayView<float>, ArrayView<float>, int, int> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuMedianCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuMedianCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMedianParams>, ArrayView<float>, ArrayView<float>, int, int>(MedianParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuMedianParams[] parameters)
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

		var maxWindow = 1;
		for (var p = 0; p < parameters.Length; p++)
		{
			var L = parameters[p].Length;
			if (L <= 0)
				L = 1;
			if (L > maxWindow)
				maxWindow = L;
		}

		using var inputBuffer = Accelerator.Allocate1D(flatCandles);
		using var offsetsBuffer = Accelerator.Allocate1D(seriesOffsets);
		using var lengthsBuffer = Accelerator.Allocate1D(seriesLengths);
		using var paramsBuffer = Accelerator.Allocate1D(parameters);
		using var valuesBuffer = Accelerator.Allocate1D<float>(seriesCount * parameters.Length * maxWindow);
		using var sortedBuffer = Accelerator.Allocate1D<float>(seriesCount * parameters.Length * maxWindow);
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_paramsSeriesKernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View, valuesBuffer.View, sortedBuffer.View, maxWindow, seriesCount);
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
	/// ILGPU kernel: Median computation for multiple series and multiple parameter sets.
	/// Each thread handles one (parameter, series) pair.
	/// </summary>
	private static void MedianParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuMedianParams> parameters,
		ArrayView<float> windowValues,
		ArrayView<float> windowSorted,
		int maxWindow,
		int seriesCount)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var offset = offsets[seriesIdx];
		var prm = parameters[paramIdx];
		var length = prm.Length;
		if (length <= 0)
			length = 1;

		var priceType = (Level1Fields)prm.PriceType;
		var comboIndex = paramIdx * seriesCount + seriesIdx;
		var windowOffset = comboIndex * maxWindow;
		var values = windowValues.SubView(windowOffset, maxWindow);
		var sorted = windowSorted.SubView(windowOffset, maxWindow);

		var count = 0;
		var head = 0;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var resIndex = paramIdx * flatCandles.Length + globalIdx;
			var price = ExtractPrice(candle, priceType);

			if (count < length)
			{
				values[count] = price;

				var insertPos = 0;
				for (; insertPos < count; insertPos++)
				{
					if (price < sorted[insertPos])
						break;
				}

				for (var k = count; k > insertPos; k--)
				{
					sorted[k] = sorted[k - 1];
				}

				sorted[insertPos] = price;
				count++;
				if (count == length)
					head = 0;
			}
			else
			{
				var oldVal = values[head];
				values[head] = price;
				head++;
				if (head >= length)
					head = 0;

				var removeIdx = 0;
				for (; removeIdx < length; removeIdx++)
				{
					if (sorted[removeIdx] == oldVal)
						break;
				}

				if (removeIdx == length)
				{
					var minDiff = MathF.Abs(sorted[0] - oldVal);
					removeIdx = 0;
					for (var k = 1; k < length; k++)
					{
						var diff = MathF.Abs(sorted[k] - oldVal);
						if (diff < minDiff)
						{
							minDiff = diff;
							removeIdx = k;
						}
					}
				}

				for (var k = removeIdx; k < length - 1; k++)
				{
					sorted[k] = sorted[k + 1];
				}

				var insertPos = 0;
				var sortedCount = length - 1;
				for (; insertPos < sortedCount; insertPos++)
				{
					if (price < sorted[insertPos])
						break;
				}

				for (var k = sortedCount; k > insertPos; k--)
				{
					sorted[k] = sorted[k - 1];
				}

				sorted[insertPos] = price;
			}

			var result = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0
			};

			if (count >= length)
			{
				var mid = length >> 1;
				var median = (length & 1) == 1
					? sorted[mid]
					: (sorted[mid - 1] + sorted[mid]) * 0.5f;

				result.Value = median;
				result.IsFormed = 1;
			}

			flatResults[resIndex] = result;
		}
	}
}

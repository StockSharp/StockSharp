namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU SMMA calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuSmmaParams"/> struct.
/// </remarks>
/// <param name="length">SMMA length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuSmmaParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// SMMA window length.
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

		if (indicator is SmoothedMovingAverage smma)
		{
			Unsafe.AsRef(in this).Length = smma.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Smoothed Moving Average (SMMA).
/// </summary>
public class GpuSmmaCalculator : GpuIndicatorCalculatorBase<SmoothedMovingAverage, GpuSmmaParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuSmmaParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuSmmaCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuSmmaCalculator(Context context, Accelerator accelerator)
	: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
		<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuSmmaParams>>(SmmaParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuSmmaParams[] parameters)
	{
		ArgumentNullException.ThrowIfNull(candlesSeries);
		ArgumentNullException.ThrowIfNull(parameters);

		if (candlesSeries.Length == 0)
		{
			throw new ArgumentOutOfRangeException(nameof(candlesSeries));
		}

		if (parameters.Length == 0)
		{
			throw new ArgumentOutOfRangeException(nameof(parameters));
		}

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
		_paramsSeriesKernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
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
	/// ILGPU kernel: SMMA computation for multiple series and parameter sets. Results are stored as [param][globalIdx].
	/// </summary>
	private static void SmmaParamsSeriesKernel(
	Index2D index,
	ArrayView<GpuCandle> flatCandles,
	ArrayView<GpuIndicatorResult> flatResults,
	ArrayView<int> offsets,
	ArrayView<int> lengths,
	ArrayView<GpuSmmaParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;
		var seriesLen = lengths[seriesIdx];

		if (seriesLen <= 0)
		{
			return;
		}

		var offset = offsets[seriesIdx];
		var totalCandles = flatCandles.Length;
		var prm = parameters[paramIdx];
		var length = prm.Length < 1 ? 1 : prm.Length;
		var priceType = (Level1Fields)prm.PriceType;
		var sum = 0f;
		var prev = 0f;
		var hasPrev = false;

		for (var i = 0; i < seriesLen; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var resIndex = paramIdx * totalCandles + globalIdx;
			var price = ExtractPrice(candle, priceType);

			if (i < length)
			{
				sum += price;
				var value = sum / length;
				byte formed = 0;

				if (i >= length - 1)
				{
					prev = value;
					hasPrev = true;
					formed = 1;
				}

				flatResults[resIndex] = new()
				{
					Time = candle.Time,
					Value = value,
					IsFormed = formed,
				};
			}
			else
			{
				if (!hasPrev)
				{
					prev = sum / length;
					hasPrev = true;
				}

				prev = prev + (price - prev) / length;

				flatResults[resIndex] = new()
				{
					Time = candle.Time,
					Value = prev,
					IsFormed = 1,
				};
			}
		}
	}
}

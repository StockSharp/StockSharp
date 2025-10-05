namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU DEMA calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuDemaParams"/> struct.
/// </remarks>
/// <param name="length">DEMA length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuDemaParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// DEMA length.
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

		if (indicator is DoubleExponentialMovingAverage dema)
		{
			Unsafe.AsRef(in this).Length = dema.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Double Exponential Moving Average (DEMA).
/// </summary>
public class GpuDemaCalculator : GpuIndicatorCalculatorBase<DoubleExponentialMovingAverage, GpuDemaParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuDemaParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuDemaCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuDemaCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuDemaParams>>(DemaParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuDemaParams[] parameters)
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
					var resIdx = p * flatCandles.Length + globalIdx;
					arr[i] = flatResults[resIdx];
				}
				result[s][p] = arr;
			}
		}

		return result;
	}

	/// <summary>
	/// ILGPU kernel: DEMA computation for multiple series and multiple parameter sets.
	/// Each thread processes entire (parameter, series) pair sequentially.
	/// </summary>
	private static void DemaParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuDemaParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var length = prm.Length;
		if (length <= 1)
			length = 1;

		var alpha = 2f / (length + 1f);
		var priceType = (Level1Fields)prm.PriceType;

		float sumPrice = 0f;
		float ema1 = 0f;
		var ema1Formed = false;

		float sumEma1 = 0f;
		float ema2 = 0f;
		var ema2Formed = false;
		var ema1Count = 0;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var price = ExtractPrice(candle, priceType);
			var resIndex = paramIdx * flatCandles.Length + (offset + i);

			var result = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0
			};

			if (!ema1Formed)
			{
				sumPrice += price;
				if (i == length - 1)
				{
					ema1 = sumPrice / length;
					ema1Formed = true;
				}
			}
			else
			{
				ema1 += alpha * (price - ema1);
			}

			if (ema1Formed)
			{
				var ema1Value = ema1;

				if (!ema2Formed)
				{
					sumEma1 += ema1Value;
					ema1Count++;

					if (ema1Count == length)
					{
						ema2 = sumEma1 / length;
						ema2Formed = true;
					}
				}
				else
				{
					ema2 += alpha * (ema1Value - ema2);
				}

				if (ema2Formed)
				{
					result.Value = 2f * ema1Value - ema2;
					result.IsFormed = 1;
				}
			}

			flatResults[resIndex] = result;
		}
	}
}

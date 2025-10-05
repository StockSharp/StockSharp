namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Triple Exponential Moving Average (TEMA) calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuTemaParams"/> struct.
/// </remarks>
/// <param name="length">TEMA length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuTemaParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// TEMA window length.
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

		if (indicator is TripleExponentialMovingAverage tema)
		{
			Unsafe.AsRef(in this).Length = tema.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Triple Exponential Moving Average (TEMA).
/// </summary>
public class GpuTemaCalculator : GpuIndicatorCalculatorBase<TripleExponentialMovingAverage, GpuTemaParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuTemaParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuTemaCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuTemaCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
				<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuTemaParams>>(TemaParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuTemaParams[] parameters)
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

		// Re-split [series][param][bar]
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
	/// ILGPU kernel: TEMA computation for multiple series and parameter sets.
	/// One thread handles one (parameter, series) pair and iterates bars sequentially.
	/// </summary>
	private static void TemaParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuTemaParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var length = prm.Length;
		if (length <= 0)
			length = 1;

		var priceType = (Level1Fields)prm.PriceType;
		var alpha = 2f / (length + 1f);
		var oneMinusAlpha = 1f - alpha;

		float ema1 = 0f, ema2 = 0f, ema3 = 0f;
		float sum1 = 0f, sum2 = 0f, sum3 = 0f;
		var count1 = 0;
		var count2 = 0;
		var count3 = 0;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var price = ExtractPrice(candle, priceType);

			var resIndex = paramIdx * flatCandles.Length + (offset + i);
			var result = new GpuIndicatorResult { Time = candle.Time, Value = float.NaN, IsFormed = 0 };

			count1++;
			if (count1 <= length)
				sum1 += price;

			var ema1Ready = false;
			if (length <= 1)
			{
				ema1 = price;
				ema1Ready = true;
			}
			else if (count1 == length)
			{
				ema1 = sum1 / length;
				ema1Ready = true;
			}
			else if (count1 > length)
			{
				ema1 = alpha * price + oneMinusAlpha * ema1;
				ema1Ready = true;
			}

			if (ema1Ready)
			{
				count2++;
				if (count2 <= length)
					sum2 += ema1;

				var ema2Ready = false;
				if (length <= 1)
				{
					ema2 = ema1;
					ema2Ready = true;
				}
				else if (count2 == length)
				{
					ema2 = sum2 / length;
					ema2Ready = true;
				}
				else if (count2 > length)
				{
					ema2 = alpha * ema1 + oneMinusAlpha * ema2;
					ema2Ready = true;
				}

				if (ema2Ready)
				{
					count3++;
					if (count3 <= length)
						sum3 += ema2;

					var ema3Ready = false;
					if (length <= 1)
					{
						ema3 = ema2;
						ema3Ready = true;
					}
					else if (count3 == length)
					{
						ema3 = sum3 / length;
						ema3Ready = true;
					}
					else if (count3 > length)
					{
						ema3 = alpha * ema2 + oneMinusAlpha * ema3;
						ema3Ready = true;
					}

					if (ema3Ready)
					{
						result.Value = 3f * ema1 - 3f * ema2 + ema3;
						result.IsFormed = 1;
					}
				}
			}

			flatResults[resIndex] = result;
		}
	}
}

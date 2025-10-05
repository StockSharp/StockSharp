namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU McGinley Dynamic calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuMcGinleyDynamicParams"/> struct.
/// </remarks>
/// <param name="length">Indicator length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuMcGinleyDynamicParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Indicator period length.
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

		if (indicator is McGinleyDynamic mcGinley)
		{
			Unsafe.AsRef(in this).Length = mcGinley.Length;
		}
	}
}

/// <summary>
/// GPU calculator for McGinley Dynamic indicator.
/// </summary>
public class GpuMcGinleyDynamicCalculator : GpuIndicatorCalculatorBase<McGinleyDynamic, GpuMcGinleyDynamicParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMcGinleyDynamicParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuMcGinleyDynamicCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuMcGinleyDynamicCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMcGinleyDynamicParams>>(McGinleyParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuMcGinleyDynamicParams[] parameters)
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
	/// ILGPU kernel for McGinley Dynamic computation. One thread handles a parameter and series pair sequentially.
	/// </summary>
	private static void McGinleyParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuMcGinleyDynamicParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;
		var len = lengths[seriesIdx];

		if (len <= 0)
			return;

		var offset = offsets[seriesIdx];
		var prm = parameters[paramIdx];
		var L = prm.Length;

		if (L <= 0)
			L = 1;

		var priceType = (Level1Fields)prm.PriceType;
		var sum = 0f;
		var prevMd = 0f;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var price = ExtractPrice(candle, priceType);
			var resIndex = paramIdx * flatCandles.Length + globalIdx;

			var result = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0
			};

			if (i < L - 1)
			{
				sum += price;
				flatResults[resIndex] = result;
				continue;
			}

			if (i == L - 1)
			{
				sum += price;
				prevMd = sum / L;
				flatResults[resIndex] = new GpuIndicatorResult
				{
					Time = candle.Time,
					Value = prevMd,
					IsFormed = 1
				};
				continue;
			}

			var denominator = 0.6f * L;

			if (prevMd != 0f)
			{
				var ratio = price / prevMd;
				denominator *= MathF.Pow(ratio, 4f);
			}
			else
			{
				denominator = 0f;
			}

			float md;

			if (denominator == 0f || float.IsInfinity(denominator) || float.IsNaN(denominator))
			{
				md = price;
			}
			else
			{
				md = prevMd + (price - prevMd) / denominator;
			}

			prevMd = md;
			flatResults[resIndex] = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = md,
				IsFormed = 1
			};
		}
	}
}

namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU correlation calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuCorrelationParams"/> struct.
/// </remarks>
/// <param name="length">Correlation window length.</param>
/// <param name="sourcePriceType">Price type for the first data sequence.</param>
/// <param name="otherPriceType">Price type for the second data sequence.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuCorrelationParams(int length, byte sourcePriceType, byte otherPriceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Window length for correlation calculation.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// Price type to extract for the first series.
	/// </summary>
	public byte SourcePriceType = sourcePriceType;

	/// <summary>
	/// Price type to extract for the second series.
	/// </summary>
	public byte OtherPriceType = otherPriceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		// CPU Correlation receives PairIndicatorValue<decimal> where
		// CandleIndicatorValue.GetValue<(decimal,decimal)>() returns (LowPrice, HighPrice).
		Unsafe.AsRef(in this).SourcePriceType = (byte)Level1Fields.LowPrice;
		Unsafe.AsRef(in this).OtherPriceType = (byte)Level1Fields.HighPrice;

		if (indicator is Correlation correlation)
		{
			Unsafe.AsRef(in this).Length = correlation.Length;
		}
	}
}

/// <summary>
/// GPU calculator for the <see cref="Correlation"/> indicator.
/// </summary>
public class GpuCorrelationCalculator : GpuIndicatorCalculatorBase<Correlation, GpuCorrelationParams, GpuIndicatorResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuCorrelationParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuCorrelationCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuCorrelationCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuCorrelationParams>>(CorrelationParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuCorrelationParams[] parameters)
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
		var maxLen = 0;
		var offset = 0;

		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			if (len > 0)
			{
				Array.Copy(candlesSeries[s], 0, flatCandles, offset, len);
				offset += len;
				if (len > maxLen)
					maxLen = len;
			}
		}

		using var inputBuffer = Accelerator.Allocate1D(flatCandles);
		using var offsetsBuffer = Accelerator.Allocate1D(seriesOffsets);
		using var lengthsBuffer = Accelerator.Allocate1D(seriesLengths);
		using var paramsBuffer = Accelerator.Allocate1D(parameters);
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var extent = new Index3D(parameters.Length, seriesCount, maxLen);
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
					var resIdx = p * totalSize + globalIdx;
					arr[i] = flatResults[resIdx];
				}
				result[s][p] = arr;
			}
		}

		return result;
	}

	/// <summary>
	/// ILGPU kernel that calculates correlation for multiple parameter sets and series.
	/// </summary>
	private static void CorrelationParamsSeriesKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuCorrelationParams> parameters)
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
		flatResults[resIndex] = new() { Time = candle.Time, Value = float.NaN, IsFormed = 0 };

		var prm = parameters[paramIdx];
		var window = prm.Length;
		if (window <= 0 || candleIdx < window - 1)
			return;

		var sourceField = (Level1Fields)prm.SourcePriceType;
		var otherField = (Level1Fields)prm.OtherPriceType;

		var sumX = 0f;
		var sumY = 0f;
		for (var j = 0; j < window; j++)
		{
			var c = flatCandles[globalIdx - j];
			var x = ExtractPrice(c, sourceField);
			var y = ExtractPrice(c, otherField);
			sumX += x;
			sumY += y;
		}

		var meanX = sumX / window;
		var meanY = sumY / window;

		var covariance = 0f;
		var varianceX = 0f;
		var varianceY = 0f;
		for (var j = 0; j < window; j++)
		{
			var c = flatCandles[globalIdx - j];
			var x = ExtractPrice(c, sourceField);
			var y = ExtractPrice(c, otherField);
			var dx = x - meanX;
			var dy = y - meanY;
			covariance += dx * dy;
			varianceX += dx * dx;
			varianceY += dy * dy;
		}

		covariance /= window;
		varianceX /= window;
		varianceY /= window;

		var stdProduct = MathF.Sqrt(varianceX) * MathF.Sqrt(varianceY);
		var correlation = 0f;
		if (stdProduct != 0f && !float.IsNaN(stdProduct))
			correlation = covariance / stdProduct;

		flatResults[resIndex] = new() { Time = candle.Time, Value = correlation, IsFormed = 1 };
	}
}

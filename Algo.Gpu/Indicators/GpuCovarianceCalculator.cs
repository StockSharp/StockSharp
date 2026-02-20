namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Covariance calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuCovarianceParams"/> struct.
/// </remarks>
/// <param name="length">Covariance window length.</param>
/// <param name="firstPriceType">Price type for the first value.</param>
/// <param name="secondPriceType">Price type for the second value.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuCovarianceParams(int length, byte firstPriceType, byte secondPriceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Covariance period length.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// Price type to extract for the first series.
	/// </summary>
	public byte FirstPriceType = firstPriceType;

	/// <summary>
	/// Price type to extract for the second series.
	/// </summary>
	public byte SecondPriceType = secondPriceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		// CPU Covariance receives PairIndicatorValue<decimal> where
		// CandleIndicatorValue.GetValue<(decimal,decimal)>() returns (LowPrice, HighPrice).
		Unsafe.AsRef(in this).FirstPriceType = (byte)Level1Fields.LowPrice;
		Unsafe.AsRef(in this).SecondPriceType = (byte)Level1Fields.HighPrice;

		if (indicator is Covariance covariance)
		{
			Unsafe.AsRef(in this).Length = covariance.Length;
		}
	}
}

/// <summary>
/// GPU calculator for the Covariance indicator.
/// </summary>
public class GpuCovarianceCalculator : GpuIndicatorCalculatorBase<Covariance, GpuCovarianceParams, GpuIndicatorResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuCovarianceParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuCovarianceCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuCovarianceCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuCovarianceParams>>(CovarianceParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuCovarianceParams[] parameters)
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
		_paramsSeriesKernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
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
					var resIdx = p * totalSize + globalIdx;
					arr[i] = flatResults[resIdx];
				}
				result[s][p] = arr;
			}
		}

		return result;
	}

	/// <summary>
	/// ILGPU kernel: Covariance computation for multiple series and parameter sets.
	/// </summary>
	private static void CovarianceParamsSeriesKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuCovarianceParams> parameters)
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
		var L = prm.Length;
		if (L <= 0)
			L = 1;

		if (candleIdx < L - 1)
			return;

		var firstType = (Level1Fields)prm.FirstPriceType;
		var secondType = (Level1Fields)prm.SecondPriceType;

		float sumX = 0f;
		float sumY = 0f;
		for (var j = 0; j < L; j++)
		{
			var c = flatCandles[globalIdx - j];
			sumX += ExtractPrice(c, firstType);
			sumY += ExtractPrice(c, secondType);
		}

		var invL = 1f / L;
		var avgX = sumX * invL;
		var avgY = sumY * invL;

		float covariance = 0f;
		for (var j = 0; j < L; j++)
		{
			var c = flatCandles[globalIdx - j];
			var x = ExtractPrice(c, firstType);
			var y = ExtractPrice(c, secondType);
			covariance += (x - avgX) * (y - avgY);
		}

		flatResults[resIndex] = new() { Time = candle.Time, Value = covariance * invL, IsFormed = 1 };
	}
}

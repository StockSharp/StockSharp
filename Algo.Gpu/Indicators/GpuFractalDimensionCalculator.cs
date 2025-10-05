namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Fractal Dimension calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuFractalDimensionParams"/> struct.
/// </remarks>
/// <param name="length">Indicator length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuFractalDimensionParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Indicator window length.
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

		if (indicator is FractalDimension fractalDimension)
		{
			Unsafe.AsRef(in this).Length = fractalDimension.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Fractal Dimension indicator.
/// </summary>
public class GpuFractalDimensionCalculator : GpuIndicatorCalculatorBase<FractalDimension, GpuFractalDimensionParams, GpuIndicatorResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuFractalDimensionParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuFractalDimensionCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuFractalDimensionCalculator(Context context, Accelerator accelerator)
	: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
		<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuFractalDimensionParams>>(FractalDimensionParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuFractalDimensionParams[] parameters)
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
	/// ILGPU kernel: Fractal Dimension computation for multiple series and multiple parameter sets. Results are stored as [param][globalIdx].
	/// </summary>
	private static void FractalDimensionParamsSeriesKernel(
	Index3D index,
	ArrayView<GpuCandle> flatCandles,
	ArrayView<GpuIndicatorResult> flatResults,
	ArrayView<int> offsets,
	ArrayView<int> lengths,
	ArrayView<GpuFractalDimensionParams> parameters)
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

		var prm = parameters[paramIdx];
		var L = prm.Length;
		if (L < 1)
		{
			flatResults[resIndex] = new() { Time = candle.Time, Value = float.NaN, IsFormed = 0 };
			return;
		}

		var count = XMath.Min(L, candleIdx + 1);
		var isFormed = (byte)((candleIdx + 1) >= L ? 1 : 0);

		if (count < 2)
		{
			flatResults[resIndex] = new() { Time = candle.Time, Value = 1.5f, IsFormed = isFormed };
			return;
		}

		var priceType = (Level1Fields)prm.PriceType;
		var start = globalIdx - (count - 1);
		var prevPrice = ExtractPrice(flatCandles[start], priceType);
		var minPrice = prevPrice;
		var maxPrice = prevPrice;
		var pathLength = 0f;

		for (var i = 1; i < count; i++)
		{
			var currentPrice = ExtractPrice(flatCandles[start + i], priceType);
			pathLength += XMath.Abs(currentPrice - prevPrice);
			prevPrice = currentPrice;
			minPrice = XMath.Min(minPrice, currentPrice);
			maxPrice = XMath.Max(maxPrice, currentPrice);
		}

		var range = maxPrice - minPrice;
		float fractalDimension;

		if (pathLength <= 0f || range <= 0f)
		{
			fractalDimension = 1.5f;
		}
		else
		{
			var denominatorBase = 2f * (L - 1);
			if (denominatorBase <= 0f)
			{
				fractalDimension = 1.5f;
			}
			else
			{
				var logDenominator = XMath.Log(denominatorBase);
				if (logDenominator == 0f)
				{
					fractalDimension = 1.5f;
				}
				else
				{
					var logPathLength = XMath.Log(pathLength);
					var logRange = XMath.Log(range);
					fractalDimension = 1f + (logPathLength - logRange) / logDenominator;
				}
			}
		}

		fractalDimension = XMath.Max(1f, XMath.Min(2f, fractalDimension));

		flatResults[resIndex] = new() { Time = candle.Time, Value = fractalDimension, IsFormed = isFormed };
	}
}

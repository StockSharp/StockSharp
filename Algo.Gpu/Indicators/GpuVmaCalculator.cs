namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Variable Moving Average (VMA) calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuVmaParams"/> struct.
/// </remarks>
/// <param name="length">Indicator length.</param>
/// <param name="priceType">Price type.</param>
/// <param name="volatilityIndex">Volatility index factor.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuVmaParams(int length, byte priceType, float volatilityIndex) : IGpuIndicatorParams
{
	/// <summary>
	/// Indicator length.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <summary>
	/// Volatility index factor controlling smoothing.
	/// </summary>
	public float VolatilityIndex = volatilityIndex;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is VariableMovingAverage vma)
		{
			Unsafe.AsRef(in this).Length = vma.Length;
			Unsafe.AsRef(in this).VolatilityIndex = (float)vma.VolatilityIndex;
		}
	}
}

/// <summary>
/// GPU calculator for Variable Moving Average (VMA).
/// </summary>
public class GpuVmaCalculator : GpuIndicatorCalculatorBase<VariableMovingAverage, GpuVmaParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuVmaParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuVmaCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuVmaCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuVmaParams>>(VmaParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuVmaParams[] parameters)
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
	/// ILGPU kernel: Variable Moving Average computation for multiple series and parameter sets.
	/// One thread handles one (parameter, series) pair and iterates bars sequentially.
	/// </summary>
	private static void VmaParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuVmaParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var L = prm.Length;
		if (L <= 0)
			L = 1;

		var priceType = (Level1Fields)prm.PriceType;
		var totalCandles = flatCandles.Length;

		float prevVma = 0f;
		var initialized = false;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var price = ExtractPrice(candle, priceType);
			var resIndex = paramIdx * totalCandles + globalIdx;

			var result = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = price,
				IsFormed = 0,
			};

			if (i + 1 < L)
			{
				prevVma = price;
				flatResults[resIndex] = result;
				continue;
			}

			float sum = 0f;
			float sumSquares = 0f;
			for (var j = 0; j < L; j++)
			{
				var c = flatCandles[globalIdx - j];
				var p = ExtractPrice(c, priceType);
				sum += p;
				sumSquares += p * p;
			}

			var avg = sum / L;
			var variance = MathF.Max(0f, sumSquares / L - avg * avg);
			var stdDev = MathF.Sqrt(variance);

			var vi = avg != 0f ? MathF.Abs(stdDev / avg) : 0f;
			var smoothingDenom = L * (1f + prm.VolatilityIndex * vi) + 1f;
			var smoothingConstant = smoothingDenom != 0f ? 2f / smoothingDenom : 0f;

			if (!initialized)
			{
				prevVma = avg;
				result.Value = avg;
				result.IsFormed = 1;
				initialized = true;
			}
			else
			{
				var curValue = (price - prevVma) * smoothingConstant + prevVma;
				prevVma = curValue;
				result.Value = curValue;
				result.IsFormed = 1;
			}

			flatResults[resIndex] = result;
		}
	}
}

namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU ALMA calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuAlmaParams"/> struct.
/// </remarks>
/// <param name="length">ALMA window length.</param>
/// <param name="priceType">Price type.</param>
/// <param name="offset">Offset factor.</param>
/// <param name="sigma">Sigma value.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuAlmaParams(int length, byte priceType, float offset, int sigma) : IGpuIndicatorParams
{
	/// <summary>
	/// ALMA window length.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <summary>
	/// Offset factor controlling the moving average bias.
	/// </summary>
	public float Offset = offset;

	/// <summary>
	/// Sigma value controlling the gaussian kernel width.
	/// </summary>
	public int Sigma = sigma;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is ArnaudLegouxMovingAverage alma)
		{
			Unsafe.AsRef(in this).Length = alma.Length;
			Unsafe.AsRef(in this).Offset = (float)alma.Offset;
			Unsafe.AsRef(in this).Sigma = alma.Sigma;
		}
	}
}

/// <summary>
/// GPU calculator for Arnaud Legoux Moving Average (ALMA).
/// </summary>
public class GpuArnaudLegouxMovingAverageCalculator : GpuIndicatorCalculatorBase<ArnaudLegouxMovingAverage, GpuAlmaParams, GpuIndicatorResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuAlmaParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuArnaudLegouxMovingAverageCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuArnaudLegouxMovingAverageCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuAlmaParams>>(AlmaParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuAlmaParams[] parameters)
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
	/// ILGPU kernel: ALMA computation for multiple series and parameter sets.
	/// </summary>
	private static void AlmaParamsSeriesKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuAlmaParams> parameters)
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
		var length = prm.Length;
		if (length <= 0)
			return;

		if (prm.Sigma <= 0)
			return;

		if (candleIdx < length - 1)
			return;

		var m = prm.Offset * (length - 1);
		var s = length / (float)prm.Sigma;
		if (s == 0f)
			return;

		var weightSum = 0f;
		var sum = 0f;
		var priceType = (Level1Fields)prm.PriceType;

		for (var i = 0; i < length; i++)
		{
			var idx = globalIdx - i;
			var x = ((float)i - m) / s;
			var weight = MathF.Exp(-0.5f * x * x);
			var price = ExtractPrice(flatCandles[idx], priceType);
			weightSum += weight;
			sum += weight * price;
		}

		if (weightSum <= 0f)
			return;

		flatResults[resIndex] = new() { Time = candle.Time, Value = sum / weightSum, IsFormed = 1 };
	}
}

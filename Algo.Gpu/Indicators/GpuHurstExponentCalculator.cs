namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Hurst Exponent calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuHurstExponentParams"/> struct.
/// </remarks>
/// <param name="length">Window length for the rescaled range calculation.</param>
/// <param name="priceType">Price type to extract from candles.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuHurstExponentParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Window length.
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

		if (indicator is HurstExponent hurst)
		{
			Unsafe.AsRef(in this).Length = hurst.Length;
		}
	}
}

/// <summary>
/// GPU calculator for the Hurst Exponent indicator.
/// </summary>
public class GpuHurstExponentCalculator : GpuIndicatorCalculatorBase<HurstExponent, GpuHurstExponentParams, GpuIndicatorResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuHurstExponentParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuHurstExponentCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuHurstExponentCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuHurstExponentParams>>(HurstParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuHurstExponentParams[] parameters)
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
	/// ILGPU kernel computing Hurst Exponent for multiple series and parameter sets.
	/// </summary>
	private static void HurstParamsSeriesKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuHurstExponentParams> parameters)
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
		if (L < 2)
			L = 2;

		if (candleIdx < L - 1)
			return;

		var priceType = (Level1Fields)prm.PriceType;
		var start = globalIdx - (L - 1);

		var sum = 0f;
		for (var j = 0; j < L; j++)
			sum += ExtractPrice(flatCandles[start + j], priceType);

		var mean = sum / L;

		var cumulative = 0f;
		var maxCum = float.MinValue;
		var minCum = float.MaxValue;
		double sumSqr = 0d;

		for (var j = 0; j < L; j++)
		{
			var value = ExtractPrice(flatCandles[start + j], priceType);
			var diff = value - mean;
			cumulative += diff;
			if (cumulative > maxCum)
				maxCum = cumulative;
			if (cumulative < minCum)
				minCum = cumulative;
			sumSqr += diff * diff;
		}

		var range = maxCum - minCum;
		var std = (float)Math.Sqrt(sumSqr / L);

		if (std <= 0f || range <= 0f)
			return;

		var rs = range / std;
		if (rs <= 0f)
			return;

		var lengthLog = MathF.Log((float)L);
		if (lengthLog == 0f)
			return;

		var hurst = MathF.Log(rs) / lengthLog;

		if (float.IsNaN(hurst) || float.IsInfinity(hurst))
			return;

		flatResults[resIndex] = new() { Time = candle.Time, Value = hurst, IsFormed = 1 };
	}
}

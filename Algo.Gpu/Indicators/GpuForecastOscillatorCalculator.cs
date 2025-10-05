namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Forecast Oscillator calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuForecastOscillatorParams"/> struct.
/// </remarks>
/// <param name="length">Forecast oscillator length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuForecastOscillatorParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Forecast oscillator window length.
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

		if (indicator is ForecastOscillator fosc)
		{
			Unsafe.AsRef(in this).Length = fosc.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Forecast Oscillator (FOSC).
/// </summary>
public class GpuForecastOscillatorCalculator : GpuIndicatorCalculatorBase<ForecastOscillator, GpuForecastOscillatorParams, GpuIndicatorResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuForecastOscillatorParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuForecastOscillatorCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuForecastOscillatorCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
		<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuForecastOscillatorParams>>(ForecastOscillatorParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuForecastOscillatorParams[] parameters)
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
	/// ILGPU kernel: Forecast Oscillator computation for multiple series and parameter sets. Results are stored as [param][globalIdx].
	/// </summary>
	private static void ForecastOscillatorParamsSeriesKernel(
	Index3D index,
	ArrayView<GpuCandle> flatCandles,
	ArrayView<GpuIndicatorResult> flatResults,
	ArrayView<int> offsets,
	ArrayView<int> lengths,
	ArrayView<GpuForecastOscillatorParams> parameters)
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
			return;

		if (candleIdx + 1 < L)
			return;

		var priceType = (Level1Fields)prm.PriceType;
		var currentPrice = ExtractPrice(candle, priceType);
		if (currentPrice == 0f)
			return;

		var sumX = 0f;
		var sumY = 0f;
		var sumXy = 0f;
		var sumX2 = 0f;

		var startIdx = globalIdx - L + 1;
		for (var j = 0; j < L; j++)
		{
			var price = ExtractPrice(flatCandles[startIdx + j], priceType);
			var x = (float)j;
			sumX += x;
			sumY += price;
			sumXy += x * price;
			sumX2 += x * x;
		}

		var lengthf = (float)L;
		var divisor = lengthf * sumX2 - sumX * sumX;
		var slope = divisor == 0f ? 0f : (lengthf * sumXy - sumX * sumY) / divisor;
		var intercept = (sumY - slope * sumX) / lengthf;
		var forecast = slope * (lengthf - 1f) + intercept;
		var value = ((currentPrice - forecast) / currentPrice) * 100f;

		flatResults[resIndex] = new() { Time = candle.Time, Value = value, IsFormed = 1 };
	}
}

namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Standard Error calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuStandardErrorParams"/> struct.
/// </remarks>
/// <param name="length">Window length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuStandardErrorParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Window length to evaluate the regression error.
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

		if (indicator is StandardError standardError)
		{
			Unsafe.AsRef(in this).Length = standardError.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Standard Error indicator.
/// </summary>
public class GpuStandardErrorCalculator : GpuIndicatorCalculatorBase<StandardError, GpuStandardErrorParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuStandardErrorParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuStandardErrorCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuStandardErrorCalculator(Context context, Accelerator accelerator)
	: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuStandardErrorParams>>(StandardErrorParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuStandardErrorParams[] parameters)
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
	/// ILGPU kernel: Standard Error computation for multiple series and parameter sets.
	/// Each thread processes one (parameter, series) pair and iterates sequentially over bars.
	/// </summary>
	private static void StandardErrorParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuStandardErrorParams> parameters)
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

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var resIndex = paramIdx * flatCandles.Length + globalIdx;

			flatResults[resIndex] = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0
			};

			if (i < L - 1)
				continue;

			var sumX = 0f;
			var sumY = 0f;
			var sumXY = 0f;
			var sumX2 = 0f;

			var start = i - (L - 1);
			for (var j = 0; j < L; j++)
			{
				var x = (float)j;
				var y = ExtractPrice(flatCandles[offset + start + j], priceType);
				sumX += x;
				sumY += y;
				sumXY += x * y;
				sumX2 += x * x;
			}

			var divisor = L * sumX2 - sumX * sumX;
			var slope = 0f;
			if (MathF.Abs(divisor) > 1e-20f)
				slope = (L * sumXY - sumX * sumY) / divisor;

			var intercept = (sumY - slope * sumX) / L;

			var sumErr2 = 0f;
			for (var j = 0; j < L; j++)
			{
				var y = ExtractPrice(flatCandles[offset + start + j], priceType);
				var yEst = slope * j + intercept;
				var diff = y - yEst;
				sumErr2 += diff * diff;
			}

			var value = 0f;
			if (L == 2)
				value = 0f;
			else if (L > 2)
				value = MathF.Sqrt(sumErr2 / (L - 2));

			flatResults[resIndex] = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = value,
				IsFormed = 1
			};
		}
	}
}

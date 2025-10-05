namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Hull Moving Average calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuHullMovingAverageParams"/> struct.
/// </remarks>
/// <param name="length">Base period length for Hull Moving Average.</param>
/// <param name="sqrtPeriod">Custom period for the final smoothing (0 uses square root of <paramref name="length"/>).</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuHullMovingAverageParams(int length, int sqrtPeriod, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Base length used for slow/fast weighted moving averages.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// Period for result smoothing. If zero the square root of <see cref="Length"/> is used.
	/// </summary>
	public int SqrtPeriod = sqrtPeriod;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is HullMovingAverage hma)
		{
			Unsafe.AsRef(in this).Length = hma.Length;
			Unsafe.AsRef(in this).SqrtPeriod = hma.SqrtPeriod;
		}
	}
}

/// <summary>
/// GPU calculator for Hull Moving Average (HMA).
/// </summary>
public class GpuHullMovingAverageCalculator : GpuIndicatorCalculatorBase<HullMovingAverage, GpuHullMovingAverageParams, GpuIndicatorResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<float>, ArrayView<int>, ArrayView<int>, ArrayView<GpuHullMovingAverageParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuHullMovingAverageCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuHullMovingAverageCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<float>, ArrayView<int>, ArrayView<int>, ArrayView<GpuHullMovingAverageParams>>(HmaParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuHullMovingAverageParams[] parameters)
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
		using var diffBuffer = Accelerator.Allocate1D<float>(totalSize * parameters.Length);
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var extent = new Index3D(parameters.Length, seriesCount, maxLen);
		_paramsSeriesKernel(extent, inputBuffer.View, outputBuffer.View, diffBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
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
	/// ILGPU kernel: HMA computation for multiple series and parameter sets. Results stored as [param][globalIdx].
	/// </summary>
	private static void HmaParamsSeriesKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<float> diffBuffer,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuHullMovingAverageParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;
		var candleIdx = index.Z;

		var len = lengths[seriesIdx];

		if (candleIdx >= len)
			return;

		var offset = offsets[seriesIdx];
		var globalIdx = offset + candleIdx;
		var resIndexBase = paramIdx * flatCandles.Length;
		var resIndex = resIndexBase + globalIdx;
		var candle = flatCandles[globalIdx];

		flatResults[resIndex] = new() { Time = candle.Time, Value = float.NaN, IsFormed = 0 };
		diffBuffer[resIndex] = float.NaN;

		var prm = parameters[paramIdx];
		var length = prm.Length;

		if (length <= 0)
			length = 1;

		var fastLength = length / 2;

		if (fastLength <= 0)
			fastLength = 1;

		var sqrtLength = prm.SqrtPeriod;

		if (sqrtLength <= 0)
		{
			sqrtLength = (int)MathF.Sqrt(length);

			if (sqrtLength <= 0)
				sqrtLength = 1;
		}

		var priceType = (Level1Fields)prm.PriceType;

		if (candleIdx < length - 1)
			return;

		var slow = CalculateWeightedAverage(flatCandles, globalIdx, length, priceType);

		if (candleIdx < fastLength - 1)
			return;

		var fast = CalculateWeightedAverage(flatCandles, globalIdx, fastLength, priceType);
		var diff = 2f * fast - slow;

		diffBuffer[resIndex] = diff;

		var diffStartIdx = globalIdx - (sqrtLength - 1);
		var earliestDiffIdx = offset + length - 1;

		if (diffStartIdx < earliestDiffIdx)
			return;

		var weightSum = sqrtLength * (sqrtLength + 1) * 0.5f;
		var diffSum = 0f;

		for (var j = 0; j < sqrtLength; j++)
		{
			var diffIdx = resIndexBase + diffStartIdx + j;
			var diffVal = diffBuffer[diffIdx];

			if (float.IsNaN(diffVal))
				return;

			var weight = j + 1;
			diffSum += diffVal * weight;
		}

		flatResults[resIndex] = new() { Time = candle.Time, Value = diffSum / weightSum, IsFormed = 1 };
	}

	private static float CalculateWeightedAverage(ArrayView<GpuCandle> candles, int endIndex, int length, Level1Fields priceType)
	{
		var start = endIndex - length + 1;
		var weightSum = length * (length + 1) * 0.5f;
		var sum = 0f;

		for (var j = 0; j < length; j++)
		{
			var idx = start + j;
			var price = ExtractPrice(candles[idx], priceType);
			var weight = j + 1;
			sum += price * weight;
		}

		return sum / weightSum;
	}
}

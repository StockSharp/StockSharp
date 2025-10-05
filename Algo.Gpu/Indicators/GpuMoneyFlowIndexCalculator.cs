namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Money Flow Index calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuMoneyFlowIndexParams"/> struct.
/// </remarks>
/// <param name="length">Money Flow Index length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuMoneyFlowIndexParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// Period length for Money Flow Index.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is MoneyFlowIndex mfi)
		{
			Unsafe.AsRef(in this).Length = mfi.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Money Flow Index (MFI).
/// </summary>
public class GpuMoneyFlowIndexCalculator : GpuIndicatorCalculatorBase<MoneyFlowIndex, GpuMoneyFlowIndexParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMoneyFlowIndexParams>, ArrayView<float>, ArrayView<float>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuMoneyFlowIndexCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuMoneyFlowIndexCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMoneyFlowIndexParams>, ArrayView<float>, ArrayView<float>>(MoneyFlowIndexParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuMoneyFlowIndexParams[] parameters)
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
		using var positiveBuffer = Accelerator.Allocate1D<float>(totalSize * parameters.Length);
		using var negativeBuffer = Accelerator.Allocate1D<float>(totalSize * parameters.Length);
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View, positiveBuffer.View, negativeBuffer.View);
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
	/// ILGPU kernel: MFI computation for multiple series and parameter sets.
	/// One thread handles a combination of parameter set and series.
	/// </summary>
	private static void MoneyFlowIndexParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuMoneyFlowIndexParams> parameters,
		ArrayView<float> positiveFlows,
		ArrayView<float> negativeFlows)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var L = parameters[paramIdx].Length;
		if (L <= 0)
			L = 1;

		float prevTypical = 0f;
		float positiveSum = 0f;
		float negativeSum = 0f;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var typicalPrice = (candle.High + candle.Low + candle.Close) / 3f;
			var moneyFlow = typicalPrice * candle.Volume;

			var positiveFlow = typicalPrice > prevTypical ? moneyFlow : 0f;
			var negativeFlow = typicalPrice < prevTypical ? moneyFlow : 0f;

			var globalIdx = offset + i;
			var bufferIdx = paramIdx * flatCandles.Length + globalIdx;

			positiveSum += positiveFlow;
			negativeSum += negativeFlow;

			positiveFlows[bufferIdx] = positiveFlow;
			negativeFlows[bufferIdx] = negativeFlow;

			if (i >= L)
			{
				var oldIdx = paramIdx * flatCandles.Length + (globalIdx - L);
				positiveSum -= positiveFlows[oldIdx];
				negativeSum -= negativeFlows[oldIdx];
			}

			var result = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0,
			};

			if (i >= L - 1)
			{
				float value;

				if (negativeSum == 0f)
				{
					value = 100f;
				}
				else
				{
					var ratio = positiveSum / negativeSum;
					if (ratio == 1f)
					{
						value = 0f;
					}
					else
					{
						value = 100f - 100f / (1f + ratio);
					}
				}

				result.Value = value;
				result.IsFormed = 1;
			}

			flatResults[bufferIdx] = result;
			prevTypical = typicalPrice;
		}
	}
}

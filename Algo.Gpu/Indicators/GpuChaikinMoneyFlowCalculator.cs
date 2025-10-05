namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Chaikin Money Flow calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuChaikinMoneyFlowParams"/> struct.
/// </remarks>
/// <param name="length">Chaikin Money Flow length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuChaikinMoneyFlowParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// CMF window length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is ChaikinMoneyFlow cmf)
		{
			Unsafe.AsRef(in this).Length = cmf.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Chaikin Money Flow (CMF).
/// </summary>
public class GpuChaikinMoneyFlowCalculator : GpuIndicatorCalculatorBase<ChaikinMoneyFlow, GpuChaikinMoneyFlowParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuChaikinMoneyFlowParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuChaikinMoneyFlowCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuChaikinMoneyFlowCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuChaikinMoneyFlowParams>>(ChaikinMoneyFlowKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuChaikinMoneyFlowParams[] parameters)
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
	/// ILGPU kernel: Chaikin Money Flow computation for multiple series and multiple parameter sets.
	/// </summary>
	private static void ChaikinMoneyFlowKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuChaikinMoneyFlowParams> parameters)
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

		float moneyFlowVolumeSum = 0f;
		float volumeSum = 0f;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var high = candle.High;
			var low = candle.Low;
			var close = candle.Close;
			var volume = candle.Volume;
			var hl = high - low;
			var moneyFlowMultiplier = hl != 0f ? (((close - low) - (high - close)) / hl) : 0f;
			var moneyFlowVolume = moneyFlowMultiplier * volume;

			var globalIdx = offset + i;
			var resIndex = paramIdx * flatCandles.Length + globalIdx;
			flatResults[resIndex] = new() { Time = candle.Time, Value = float.NaN, IsFormed = 0 };

			moneyFlowVolumeSum += moneyFlowVolume;
			volumeSum += volume;

			if (i >= L)
			{
				var oldCandle = flatCandles[offset + i - L];
				var oldHigh = oldCandle.High;
				var oldLow = oldCandle.Low;
				var oldClose = oldCandle.Close;
				var oldVolume = oldCandle.Volume;
				var oldHl = oldHigh - oldLow;
				var oldMultiplier = oldHl != 0f ? (((oldClose - oldLow) - (oldHigh - oldClose)) / oldHl) : 0f;
				var oldMoneyFlowVolume = oldMultiplier * oldVolume;

				moneyFlowVolumeSum -= oldMoneyFlowVolume;
				volumeSum -= oldMoneyFlowVolume;
			}

			if (i >= L - 1)
			{
				var value = volumeSum != 0f ? moneyFlowVolumeSum / volumeSum : 0f;
				flatResults[resIndex] = new() { Time = candle.Time, Value = value, IsFormed = 1 };
			}
		}
	}
}

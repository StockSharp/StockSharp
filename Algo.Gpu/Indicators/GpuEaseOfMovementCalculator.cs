namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Ease of Movement (EMV) calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuEaseOfMovementParams"/> struct.
/// </remarks>
/// <param name="length">Smoothing length for EMV moving average.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuEaseOfMovementParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// Moving average window length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is EaseOfMovement emv)
		{
			Unsafe.AsRef(in this).Length = emv.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Ease of Movement (EMV).
/// </summary>
public class GpuEaseOfMovementCalculator : GpuIndicatorCalculatorBase<EaseOfMovement, GpuEaseOfMovementParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<float>, ArrayView<int>, ArrayView<int>, ArrayView<GpuEaseOfMovementParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuEaseOfMovementCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuEaseOfMovementCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
				<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<float>, ArrayView<int>, ArrayView<int>, ArrayView<GpuEaseOfMovementParams>>(EaseOfMovementParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuEaseOfMovementParams[] parameters)
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
		using var rawEmvBuffer = Accelerator.Allocate1D<float>(totalSize * parameters.Length);
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, rawEmvBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
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
	/// ILGPU kernel: EMV computation for multiple series and parameter sets. One thread processes a (parameter, series) pair sequentially.
	/// </summary>
	private static void EaseOfMovementParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<float> rawEmv,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuEaseOfMovementParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var totalBars = flatCandles.Length;
		var L = parameters[paramIdx].Length;
		if (L <= 0)
			L = 1;

		var prevHigh = 0f;
		var prevLow = 0f;
		var hasPrev = false;

		for (var i = 0; i < len; i++)
		{
			var candleIdx = offset + i;
			var candle = flatCandles[candleIdx];
			var resIndex = paramIdx * totalBars + candleIdx;

			rawEmv[resIndex] = float.NaN;
			flatResults[resIndex] = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0,
			};

			if (!hasPrev)
			{
				hasPrev = true;
				prevHigh = candle.High;
				prevLow = candle.Low;
				continue;
			}

			var range = candle.High - candle.Low;
			if (range == 0f)
			{
				prevHigh = candle.High;
				prevLow = candle.Low;
				continue;
			}

			var midpointMove = ((candle.High + candle.Low) * 0.5f) - ((prevHigh + prevLow) * 0.5f);
			var boxRatio = candle.Volume / range;
			var emv = midpointMove / boxRatio;

			rawEmv[resIndex] = emv;

			var sum = 0f;
			var count = 0;
			var backIdx = candleIdx;
			while (backIdx >= offset && count < L)
			{
				var rawIdx = paramIdx * totalBars + backIdx;
				var val = rawEmv[rawIdx];
				if (!float.IsNaN(val))
				{
					sum += val;
					count++;
				}
				backIdx--;
			}

			if (count == L)
			{
				flatResults[resIndex] = new GpuIndicatorResult
				{
					Time = candle.Time,
					Value = sum / L,
					IsFormed = 1,
				};
			}

			prevHigh = candle.High;
			prevLow = candle.Low;
		}
	}
}

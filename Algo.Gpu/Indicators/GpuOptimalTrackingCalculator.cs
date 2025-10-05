namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Optimal Tracking calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuOptimalTrackingParams"/> struct.
/// </remarks>
/// <param name="length">Indicator length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuOptimalTrackingParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// Indicator length (number of candles required to form the value).
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is OptimalTracking optimalTracking)
		{
			Unsafe.AsRef(in this).Length = optimalTracking.Length;
		}
	}
}

/// <summary>
/// GPU calculator for <see cref="OptimalTracking"/> indicator.
/// </summary>
public class GpuOptimalTrackingCalculator : GpuIndicatorCalculatorBase<OptimalTracking, GpuOptimalTrackingParams, GpuIndicatorResult>
{
	private static readonly float SmoothConstant1 = (float)Math.Exp(-0.25);
	private static readonly float SmoothConstant = 1f - SmoothConstant1;

	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuOptimalTrackingParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuOptimalTrackingCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuOptimalTrackingCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuOptimalTrackingParams>>(OptimalTrackingParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuOptimalTrackingParams[] parameters)
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
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
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
					var resIdx = p * flatCandles.Length + globalIdx;
					arr[i] = flatResults[resIdx];
				}
				result[s][p] = arr;
			}
		}

		return result;
	}

	/// <summary>
	/// ILGPU kernel: Optimal Tracking computation for multiple series and parameter sets.
	/// Each thread handles one pair of parameter set and candle series.
	/// </summary>
	private static void OptimalTrackingParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuOptimalTrackingParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var required = prm.Length;
		if (required < 2)
			required = 2;

		var lambda = 0f;
		var value1Old = 0f;
		var value2Old = 0f;
		var resultOld = 0f;
		var prevAverage = 0f;

		var formedThreshold = required - 1;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var high = candle.High;
			var low = candle.Low;
			var average = (high + low) * 0.5f;
			var halfRange = (high - low) * 0.5f;

			float result;
			byte isFormed;

			if (i < formedThreshold)
			{
				value2Old = halfRange;
				resultOld = average;
				result = resultOld;
				isFormed = 0;
			}
			else
			{
				var avgDiff = average - prevAverage;
				var smoothDiff = SmoothConstant * avgDiff + SmoothConstant1 * value1Old;
				value1Old = smoothDiff;

				var smoothRng = SmoothConstant * halfRange + SmoothConstant1 * value2Old;
				value2Old = smoothRng;

				if (smoothRng != 0f)
					lambda = MathF.Abs(smoothDiff / smoothRng);

				var lambdaSq = lambda * lambda;
				var sqrtTerm = MathF.Sqrt(lambdaSq * lambdaSq + 16f * lambdaSq);
				var alpha = (-lambdaSq + sqrtTerm) * 0.125f;

				var oneMinusAlpha = 1f - alpha;
				result = alpha * average + oneMinusAlpha * resultOld;
				resultOld = result;
				isFormed = 1;
			}

			var resIdx = paramIdx * flatCandles.Length + globalIdx;
			flatResults[resIdx] = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = result,
				IsFormed = isFormed,
			};

			prevAverage = average;
		}
	}
}

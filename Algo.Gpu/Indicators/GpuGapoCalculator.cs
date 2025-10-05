namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Gopalakrishnan Range Index (GAPO) calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuGapoParams"/> struct.
/// </remarks>
/// <param name="length">Indicator length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuGapoParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// GAPO window length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is GopalakrishnanRangeIndex gapo)
		{
			Unsafe.AsRef(in this).Length = gapo.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Gopalakrishnan Range Index (GAPO).
/// </summary>
public class GpuGapoCalculator : GpuIndicatorCalculatorBase<GopalakrishnanRangeIndex, GpuGapoParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuGapoParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuGapoCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuGapoCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuGapoParams>>(GapoParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuGapoParams[] parameters)
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
	/// ILGPU kernel: GAPO computation for multiple series and parameter sets.
	/// </summary>
	private static void GapoParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuGapoParams> parameters)
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

		var logLength = MathF.Log(L);

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var resIndex = paramIdx * flatCandles.Length + globalIdx;

			var result = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0
			};

			if (i >= L - 1)
			{
				var highestHigh = float.MinValue;
				var lowestLow = float.MaxValue;

				for (var j = 0; j < L; j++)
				{
					var c = flatCandles[globalIdx - j];
					if (c.High > highestHigh)
						highestHigh = c.High;
					if (c.Low < lowestLow)
						lowestLow = c.Low;
				}

				var currentRange = candle.High - candle.Low;
				var gapo = 0f;

				if (currentRange > 0f && logLength != 0f)
				{
					var ratio = (highestHigh - lowestLow) / currentRange;
					if (ratio > 0f)
					{
						gapo = MathF.Log(ratio) / logLength;
					}
				}

				result.Value = gapo;
				result.IsFormed = 1;
			}

			flatResults[resIndex] = result;
		}
	}
}

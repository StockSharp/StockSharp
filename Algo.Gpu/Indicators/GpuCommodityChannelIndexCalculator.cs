namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Commodity Channel Index (CCI) calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuCciParams"/> struct.
/// </remarks>
/// <param name="length">CCI length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuCciParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// CCI window length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is CommodityChannelIndex cci)
		{
			Unsafe.AsRef(in this).Length = cci.Length;
		}
	}
}

/// <summary>
/// GPU calculator for <see cref="CommodityChannelIndex"/>.
/// </summary>
public class GpuCommodityChannelIndexCalculator : GpuIndicatorCalculatorBase<CommodityChannelIndex, GpuCciParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuCciParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuCommodityChannelIndexCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuCommodityChannelIndexCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuCciParams>>(CciParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuCciParams[] parameters)
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
	/// ILGPU kernel: CCI computation for multiple series and parameter sets. Results are stored as [param][globalIdx].
	/// </summary>
	private static void CciParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuCciParams> parameters)
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

			var sumTp = 0f;
			for (var j = 0; j < L; j++)
			{
				var c = flatCandles[globalIdx - j];
				sumTp += (c.High + c.Low + c.Close) / 3f;
			}

			var sma = sumTp / L;

			var devSum = 0f;
			for (var j = 0; j < L; j++)
			{
				var c = flatCandles[globalIdx - j];
				var tp = (c.High + c.Low + c.Close) / 3f;
				devSum += MathF.Abs(tp - sma);
			}

			var md = devSum / L;
			if (md == 0f)
				continue;

			var currentTp = (candle.High + candle.Low + candle.Close) / 3f;
			var cci = (currentTp - sma) / (0.015f * md);

			flatResults[resIndex] = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = cci,
				IsFormed = 1
			};
		}
	}
}

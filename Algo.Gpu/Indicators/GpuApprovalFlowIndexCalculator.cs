namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Approval Flow Index calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuApprovalFlowIndexParams"/> struct.
/// </remarks>
/// <param name="length">Indicator length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuApprovalFlowIndexParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// AFI window length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is ApprovalFlowIndex afi)
		{
			Unsafe.AsRef(in this).Length = afi.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Approval Flow Index (AFI).
/// </summary>
public class GpuApprovalFlowIndexCalculator : GpuIndicatorCalculatorBase<ApprovalFlowIndex, GpuApprovalFlowIndexParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuApprovalFlowIndexParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuApprovalFlowIndexCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuApprovalFlowIndexCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuApprovalFlowIndexParams>>(ApprovalFlowIndexParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuApprovalFlowIndexParams[] parameters)
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
					var resIdx = p * flatCandles.Length + globalIdx;
					arr[i] = flatResults[resIdx];
				}
				result[s][p] = arr;
			}
		}

		return result;
	}

	/// <summary>
	/// ILGPU kernel: Approval Flow Index computation for multiple series and parameter sets.
	/// Each thread processes a single (parameter, series) pair sequentially.
	/// </summary>
	private static void ApprovalFlowIndexParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuApprovalFlowIndexParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var param = parameters[paramIdx];
		var length = param.Length;
		if (length <= 0)
			length = 1;

		var totalUpVolume = 0f;
		var totalDownVolume = 0f;
		var count = 0;
		var formed = false;
		var hasPrevClose = false;
		var prevClose = 0f;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var resIndex = paramIdx * flatCandles.Length + globalIdx;

			var result = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0,
			};

			var close = candle.Close;
			if (!hasPrevClose)
			{
				hasPrevClose = true;
				prevClose = close;
				flatResults[resIndex] = result;
				continue;
			}

			if (close > prevClose)
			{
				totalUpVolume += candle.Volume;
			}
			else if (close < prevClose)
			{
				totalDownVolume += candle.Volume;
			}

			count++;
			if (!formed && count >= length)
				formed = true;

			if (formed)
			{
				result.IsFormed = 1;
				var totalVolume = totalUpVolume + totalDownVolume;
				if (totalVolume != 0f)
				{
					result.Value = 100f * (totalUpVolume - totalDownVolume) / totalVolume;
				}
			}

			prevClose = close;
			flatResults[resIndex] = result;
		}
	}
}

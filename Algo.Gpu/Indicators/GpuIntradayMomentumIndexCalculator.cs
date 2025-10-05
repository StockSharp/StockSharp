namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Intraday Momentum Index calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuIntradayMomentumIndexParams"/> struct.
/// </remarks>
/// <param name="length">Intraday Momentum Index length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuIntradayMomentumIndexParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// IMI period length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is IntradayMomentumIndex imi)
		{
			Unsafe.AsRef(in this).Length = imi.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Intraday Momentum Index (IMI).
/// </summary>
public class GpuIntradayMomentumIndexCalculator : GpuIndicatorCalculatorBase<IntradayMomentumIndex, GpuIntradayMomentumIndexParams, GpuIndicatorResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuIntradayMomentumIndexParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuIntradayMomentumIndexCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuIntradayMomentumIndexCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuIntradayMomentumIndexParams>>(IntradayMomentumIndexParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuIntradayMomentumIndexParams[] parameters)
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
	/// ILGPU kernel: IMI computation for multiple series and parameter sets. Results are stored as [param][globalIdx].
	/// </summary>
	private static void IntradayMomentumIndexParamsSeriesKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuIntradayMomentumIndexParams> parameters)
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

		var prm = parameters[paramIdx];
		var window = prm.Length;
		if (window <= 0)
		{
			flatResults[resIndex] = new() { Time = candle.Time, Value = float.NaN, IsFormed = 0 };
			return;
		}

		var start = candleIdx - window + 1;
		if (start < 0)
			start = 0;

		var sumUp = 0f;
		var sumDown = 0f;

		for (var idx = start; idx <= candleIdx; idx++)
		{
			var bar = flatCandles[offset + idx];
			var diff = bar.Close - bar.Open;
			if (diff > 0f)
			{
				sumUp += diff;
			}
			else if (diff < 0f)
			{
				sumDown += -diff;
			}
		}

		var isFormed = (byte)((candleIdx + 1) >= window ? 1 : 0);
		var value = float.NaN;

		if (isFormed == 1)
		{
			var denom = sumUp + sumDown;
			value = denom > 0f ? 100f * (sumUp / denom) : 0f;
		}

		flatResults[resIndex] = new() { Time = candle.Time, Value = value, IsFormed = isFormed };
	}
}

namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Balance Volume calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuBalanceVolumeParams : IGpuIndicatorParams
{
	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
	}
}

/// <summary>
/// GPU calculator for Balance Volume indicator.
/// </summary>
public class GpuBalanceVolumeCalculator : GpuIndicatorCalculatorBase<BalanceVolume, GpuBalanceVolumeParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuBalanceVolumeParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuBalanceVolumeCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuBalanceVolumeCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuBalanceVolumeParams>>(BalanceVolumeParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuBalanceVolumeParams[] parameters)
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
	/// ILGPU kernel: Balance Volume computation for multiple series and parameter sets.
	/// One thread handles one (parameter, series) pair and iterates bars sequentially.
	/// </summary>
	private static void BalanceVolumeParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuBalanceVolumeParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		if (paramIdx >= parameters.Length)
			return;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var totalCandles = flatCandles.Length;
		var prevClose = 0f;
		var cumulative = 0f;
		var hasPrev = false;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var close = candle.Close;
			var resIndex = paramIdx * totalCandles + (offset + i);

			var result = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0,
			};

			if (!hasPrev)
			{
				prevClose = close;
				hasPrev = true;
			}
			else
			{
				var balanceVolume = 0f;
				if (close > prevClose)
					balanceVolume = candle.Volume;
				else if (close < prevClose)
					balanceVolume = -candle.Volume;

				cumulative += balanceVolume;
				result.Value = cumulative;
				result.IsFormed = 1;
				prevClose = close;
			}

			flatResults[resIndex] = result;
		}
	}
}

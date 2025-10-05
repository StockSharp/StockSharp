namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Average True Range calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuAtrParams"/> struct.
/// </remarks>
/// <param name="length">ATR length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuAtrParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// ATR period length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is AverageTrueRange atr)
		{
			Unsafe.AsRef(in this).Length = atr.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Average True Range (ATR).
/// </summary>
public class GpuAtrCalculator : GpuIndicatorCalculatorBase<AverageTrueRange, GpuAtrParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuAtrParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuAtrCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuAtrCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuAtrParams>>(AtrParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuAtrParams[] parameters)
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
	/// ILGPU kernel: ATR computation for multiple series and parameter sets.
	/// Each thread processes one (parameter, series) pair sequentially across bars.
	/// </summary>
	private static void AtrParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuAtrParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var length = parameters[paramIdx].Length;
		if (length <= 0)
			length = 1;

		var prevClose = flatCandles[offset].Close;
		var prevAtr = 0f;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var high = candle.High;
			var low = candle.Low;
			var close = candle.Close;

			var tr = high - low;
			if (i > 0)
			{
				var diffHighClose = MathF.Abs(high - prevClose);
				var diffLowClose = MathF.Abs(low - prevClose);
				if (diffHighClose > tr)
					tr = diffHighClose;
				if (diffLowClose > tr)
					tr = diffLowClose;
			}

			var atr = tr;
			if (i == 0)
			{
				atr = tr;
			}
			else if (i < length)
			{
				atr = (prevAtr * i + tr) / (i + 1);
			}
			else
			{
				atr = (prevAtr * (length - 1) + tr) / length;
			}

			prevAtr = atr;

			var resIndex = paramIdx * flatCandles.Length + (offset + i);
			flatResults[resIndex] = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = atr,
				IsFormed = (byte)(i >= length - 1 ? 1 : 0),
			};

			prevClose = close;
		}
	}
}

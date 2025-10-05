namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU On Balance Volume Mean calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuObvmParams"/> struct.
/// </remarks>
/// <param name="length">OBVM length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuObvmParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// OBVM window length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is OnBalanceVolumeMean obvm)
		{
			Unsafe.AsRef(in this).Length = obvm.Length;
		}
	}
}

/// <summary>
/// GPU calculator for On Balance Volume Mean.
/// </summary>
public class GpuObvmCalculator : GpuIndicatorCalculatorBase<OnBalanceVolumeMean, GpuObvmParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<float>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuObvmParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuObvmCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuObvmCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<float>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuObvmParams>>(ObvmParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuObvmParams[] parameters)
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

		var obvValues = new float[totalSize];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			if (len <= 0)
				continue;

			var start = seriesOffsets[s];
			var obv = 0f;
			var prevClose = 0f;

			for (var i = 0; i < len; i++)
			{
				var candle = flatCandles[start + i];
				var close = candle.Close;

				if (i > 0)
				{
					if (close > prevClose)
						obv += candle.Volume;
					else if (close < prevClose)
						obv -= candle.Volume;
				}

				prevClose = close;
				obvValues[start + i] = obv;
			}
		}

		using var inputBuffer = Accelerator.Allocate1D(flatCandles);
		using var offsetsBuffer = Accelerator.Allocate1D(seriesOffsets);
		using var lengthsBuffer = Accelerator.Allocate1D(seriesLengths);
		using var paramsBuffer = Accelerator.Allocate1D(parameters);
		using var obvBuffer = Accelerator.Allocate1D(obvValues);
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, obvBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
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
	/// ILGPU kernel: OBVM computation for multiple series and parameter sets.
	/// </summary>
	private static void ObvmParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<float> obvValues,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuObvmParams> parameters)
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

		var totalCandles = flatCandles.Length;
		var sum = 0f;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var obv = obvValues[globalIdx];
			sum += obv;

			if (i >= L)
				sum -= obvValues[globalIdx - L];

			var resIndex = paramIdx * totalCandles + globalIdx;
			var candle = flatCandles[globalIdx];
			var result = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0
			};

			if (i >= L - 1)
			{
				result.Value = sum / L;
				result.IsFormed = 1;
			}

			flatResults[resIndex] = result;
		}
	}
}

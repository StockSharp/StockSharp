namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Vertical-Horizontal Filter (VHF) calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuVerticalHorizontalFilterParams"/> struct.
/// </remarks>
/// <param name="length">VHF length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuVerticalHorizontalFilterParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// VHF period length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is VerticalHorizontalFilter vhf)
		{
			Unsafe.AsRef(in this).Length = vhf.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Vertical-Horizontal Filter (VHF).
/// </summary>
public class GpuVerticalHorizontalFilterCalculator : GpuIndicatorCalculatorBase<VerticalHorizontalFilter, GpuVerticalHorizontalFilterParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuVerticalHorizontalFilterParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuVerticalHorizontalFilterCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuVerticalHorizontalFilterCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuVerticalHorizontalFilterParams>>(VerticalHorizontalFilterParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuVerticalHorizontalFilterParams[] parameters)
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
	/// ILGPU kernel: VHF computation for multiple series and parameter sets. Each thread handles one (parameter, series) pair.
	/// </summary>
	private static void VerticalHorizontalFilterParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuVerticalHorizontalFilterParams> parameters)
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

		var candleCount = flatCandles.Length;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var resIndex = paramIdx * candleCount + globalIdx;

			var result = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0
			};

			var windowStart = i - length + 1;
			if (windowStart < 0)
				windowStart = 0;

			var minLow = float.MaxValue;
			var maxHigh = float.MinValue;
			for (var j = windowStart; j <= i; j++)
			{
				var windowCandle = flatCandles[offset + j];
				if (windowCandle.Low < minLow)
					minLow = windowCandle.Low;
				if (windowCandle.High > maxHigh)
					maxHigh = windowCandle.High;
			}

			var diffStart = i - length + 1;
			if (diffStart < 1)
				diffStart = 1;

			float diffSum = 0f;
			for (var j = diffStart; j <= i; j++)
			{
				var current = flatCandles[offset + j];
				var previous = flatCandles[offset + j - 1];
				diffSum += MathF.Abs(current.Close - previous.Close);
			}

			var formed = i >= length;
			if (formed)
			{
				result.IsFormed = 1;
				if (diffSum > 0f)
				{
					result.Value = (maxHigh - minLow) / diffSum;
				}
			}

			flatResults[resIndex] = result;
		}
	}
}

namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for Elder's Force Index GPU calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuElderForceIndexParams"/> struct.
/// </remarks>
/// <param name="length">EMA smoothing length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuElderForceIndexParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// EMA smoothing length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is ElderForceIndex efi)
		{
			Unsafe.AsRef(in this).Length = efi.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Elder's Force Index.
/// </summary>
public class GpuElderForceIndexCalculator : GpuIndicatorCalculatorBase<ElderForceIndex, GpuElderForceIndexParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuElderForceIndexParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuElderForceIndexCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuElderForceIndexCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuElderForceIndexParams>>(ElderForceIndexKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuElderForceIndexParams[] parameters)
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
	/// ILGPU kernel: Elder's Force Index computation for multiple series and parameter sets.
	/// </summary>
	private static void ElderForceIndexKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuElderForceIndexParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var offset = offsets[seriesIdx];
		var length = parameters[paramIdx].Length;
		if (length <= 0)
			length = 1;

		var multiplier = 2f / (length + 1f);
		float prevClose = 0f;
		float sum = 0f;
		float ema = 0f;
		var formed = false;
		var forceCount = 0;

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

			if (i == 0)
			{
				prevClose = candle.Close;
				flatResults[resIndex] = result;
				continue;
			}

			var force = candle.Volume * (candle.Close - prevClose);
			prevClose = candle.Close;

			forceCount++;

			if (!formed)
			{
				sum += force;
				if (forceCount < length)
				{
					flatResults[resIndex] = result;
					continue;
				}

				ema = sum / length;
				result.Value = ema;
				result.IsFormed = 1;
				flatResults[resIndex] = result;
				formed = true;
				continue;
			}

			ema = (force - ema) * multiplier + ema;
			result.Value = ema;
			result.IsFormed = 1;
			flatResults[resIndex] = result;
		}
	}
}

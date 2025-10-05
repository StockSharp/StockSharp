namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Jurik Moving Average (JMA) calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuJmaParams"/> struct.
/// </remarks>
/// <param name="length">JMA length.</param>
/// <param name="phase">JMA phase.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuJmaParams(int length, int phase, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// JMA window length.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// JMA phase value (-100..100).
	/// </summary>
	public int Phase = phase;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is JurikMovingAverage jma)
		{
			Unsafe.AsRef(in this).Length = jma.Length;
			Unsafe.AsRef(in this).Phase = jma.Phase;
		}
	}
}

/// <summary>
/// GPU calculator for Jurik Moving Average (JMA).
/// </summary>
public class GpuJmaCalculator : GpuIndicatorCalculatorBase<JurikMovingAverage, GpuJmaParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuJmaParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuJmaCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuJmaCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuJmaParams>>(JmaParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuJmaParams[] parameters)
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
	/// ILGPU kernel: JMA computation for multiple series and parameter sets. One thread handles one (parameter, series) pair.
	/// </summary>
	private static void JmaParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuJmaParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var length = prm.Length;
		if (length <= 0)
			length = 1;

		var phase = prm.Phase;
		if (phase < -100)
			phase = -100;
		else if (phase > 100)
			phase = 100;

		var priceType = (Level1Fields)prm.PriceType;

		var lenMinusOne = length - 1f;
		var beta = lenMinusOne <= 0f
			? 0f
			: (0.45f * lenMinusOne) / (0.45f * lenMinusOne + 2f);
		var phaseRatio = (phase + 100f) / 200f;

		var formedIndex = length - 1;
		if (formedIndex < 0)
			formedIndex = 0;

		var hasPrev = false;
		var prevMa1 = 0f;
		var prevMa2 = 0f;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var price = ExtractPrice(candle, priceType);
			GpuIndicatorResult result;

			if (!hasPrev)
			{
				prevMa1 = price;
				prevMa2 = price;
				hasPrev = true;
				result = new GpuIndicatorResult
				{
					Time = candle.Time,
					Value = price,
					IsFormed = (byte)(i >= formedIndex ? 1 : 0)
				};
			}
			else
			{
				var ma1 = prevMa1 + beta * (price - prevMa1);
				var ma2 = prevMa2 + beta * (ma1 - prevMa2);
				var jma = ma2 + phaseRatio * (ma2 - prevMa2);

				prevMa1 = ma1;
				prevMa2 = ma2;

				result = new GpuIndicatorResult
				{
					Time = candle.Time,
					Value = jma,
					IsFormed = (byte)(i >= formedIndex ? 1 : 0)
				};
			}

			var resIndex = paramIdx * flatCandles.Length + (offset + i);
			flatResults[resIndex] = result;
		}
	}
}

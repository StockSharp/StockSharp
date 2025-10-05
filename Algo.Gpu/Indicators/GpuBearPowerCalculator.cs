namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Bear Power calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuBearPowerParams"/> struct.
/// </remarks>
/// <param name="length">EMA length.</param>
/// <param name="priceType">Price type for EMA source.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuBearPowerParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// EMA window length.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// Price type to extract from candles for EMA.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is BearPower bearPower)
		{
			Unsafe.AsRef(in this).Length = bearPower.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Bear Power indicator.
/// </summary>
public class GpuBearPowerCalculator : GpuIndicatorCalculatorBase<BearPower, GpuBearPowerParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuBearPowerParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuBearPowerCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuBearPowerCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
				<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuBearPowerParams>>(BearPowerParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuBearPowerParams[] parameters)
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
					var resIdx = p * totalSize + globalIdx;
					arr[i] = flatResults[resIdx];
				}
				result[s][p] = arr;
			}
		}

		return result;
	}

	/// <summary>
	/// ILGPU kernel: Bear Power computation for multiple series and parameter sets.
	/// </summary>
	private static void BearPowerParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuBearPowerParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var len = lengths[seriesIdx];
		if (len == 0)
			return;

		var offset = offsets[seriesIdx];
		var totalSize = flatCandles.Length;
		var prm = parameters[paramIdx];

		var period = prm.Length <= 0 ? 1 : prm.Length;
		var multiplier = 2f / (period + 1f);
		var priceField = (Level1Fields)prm.PriceType;

		var sum = 0f;
		var ema = 0f;
		var formed = false;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var resIdx = paramIdx * totalSize + globalIdx;

			var result = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0,
			};

			var price = ExtractPrice(candle, priceField);

			if (!formed)
			{
				sum += price;
				if (i + 1 < period)
				{
					flatResults[resIdx] = result;
					continue;
				}

				ema = sum / period;
				formed = true;
			}
			else
			{
				ema = (price - ema) * multiplier + ema;
			}

			result.Value = candle.Low - ema;
			result.IsFormed = 1;
			flatResults[resIdx] = result;
		}
	}
}

namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Bull Power calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuBullPowerParams"/> struct.
/// </remarks>
/// <param name="length">Exponential moving average length.</param>
/// <param name="priceType">Price type used for EMA source.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuBullPowerParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// EMA period length.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// Price type to extract for EMA calculation.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is BullPower bullPower)
		{
			Unsafe.AsRef(in this).Length = bullPower.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Bull Power indicator.
/// </summary>
public class GpuBullPowerCalculator : GpuIndicatorCalculatorBase<BullPower, GpuBullPowerParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuBullPowerParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuBullPowerCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuBullPowerCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuBullPowerParams>>(BullPowerParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuBullPowerParams[] parameters)
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
			var series = candlesSeries[s];

			if (len <= 0 || series is null)
				continue;

			Array.Copy(series, 0, flatCandles, offset, len);
			offset += len;
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
					var resIdx = p * totalSize + globalIdx;
					arr[i] = flatResults[resIdx];
				}
				result[s][p] = arr;
			}
		}

		return result;
	}

	/// <summary>
	/// ILGPU kernel: Bull Power computation for multiple series and parameter sets.
	/// Each thread processes one (parameter, series) pair sequentially.
	/// </summary>
	private static void BullPowerParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuBullPowerParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var L = prm.Length;
		if (L <= 0)
			L = 1;

		var priceType = (Level1Fields)prm.PriceType;
		var multiplier = 2f / (L + 1f);
		var totalCandles = flatCandles.Length;

		float sum = 0f;
		float prevEma = 0f;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var price = ExtractPrice(candle, priceType);

			var resultIndex = paramIdx * totalCandles + globalIdx;
			var result = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0
			};

			if (i < L)
				sum += price;

			if (i < L - 1)
			{
				flatResults[resultIndex] = result;
				continue;
			}

			if (i == L - 1)
			{
				prevEma = sum / L;
			}
			else
			{
				prevEma = ((price - prevEma) * multiplier) + prevEma;
			}

			result.Value = candle.High - prevEma;
			result.IsFormed = 1;
			flatResults[resultIndex] = result;
		}
	}
}

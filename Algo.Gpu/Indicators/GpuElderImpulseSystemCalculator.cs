namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Elder Impulse System calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuElderImpulseSystemParams"/> struct.
/// </remarks>
/// <param name="emaLength">EMA length.</param>
/// <param name="macdLongLength">MACD long EMA length.</param>
/// <param name="macdShortLength">MACD short EMA length.</param>
/// <param name="priceType">Price type to extract from candles.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuElderImpulseSystemParams(int emaLength, int macdLongLength, int macdShortLength, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// EMA window length.
	/// </summary>
	public int EmaLength = emaLength;

	/// <summary>
	/// MACD long EMA length.
	/// </summary>
	public int MacdLongLength = macdLongLength;

	/// <summary>
	/// MACD short EMA length.
	/// </summary>
	public int MacdShortLength = macdShortLength;

	/// <summary>
	/// Price type to extract from candles. Defaults to Close.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is ElderImpulseSystem eis)
		{
			Unsafe.AsRef(in this).EmaLength = eis.Ema.Length;
			Unsafe.AsRef(in this).MacdLongLength = eis.Macd.LongMa.Length;
			Unsafe.AsRef(in this).MacdShortLength = eis.Macd.ShortMa.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Elder Impulse System indicator.
/// </summary>
public class GpuElderImpulseSystemCalculator : GpuIndicatorCalculatorBase<ElderImpulseSystem, GpuElderImpulseSystemParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuElderImpulseSystemParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuElderImpulseSystemCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuElderImpulseSystemCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuElderImpulseSystemParams>>(ElderImpulseParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuElderImpulseSystemParams[] parameters)
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
					var resIdx = p * totalSize + globalIdx;
					arr[i] = flatResults[resIdx];
				}
				result[s][p] = arr;
			}
		}

		return result;
	}

	/// <summary>
	/// ILGPU kernel computing Elder Impulse System for a parameter set and series.
	/// </summary>
	private static void ElderImpulseParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuElderImpulseSystemParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var offset = offsets[seriesIdx];
		var candlesCount = flatCandles.Length;
		var prm = parameters[paramIdx];

		var emaLength = prm.EmaLength;
		if (emaLength <= 0)
			emaLength = 1;
		var macdLongLength = prm.MacdLongLength;
		if (macdLongLength <= 0)
			macdLongLength = 1;
		var macdShortLength = prm.MacdShortLength;
		if (macdShortLength <= 0)
			macdShortLength = 1;

		var emaMultiplier = 2f / (emaLength + 1f);
		var macdLongMultiplier = 2f / (macdLongLength + 1f);
		var macdShortMultiplier = 2f / (macdShortLength + 1f);

		var priceType = (Level1Fields)prm.PriceType;

		float ema = 0f;
		float emaSum = 0f;
		var emaFormed = false;

		float macdShort = 0f;
		float macdShortSum = 0f;
		var macdShortFormed = false;

		float macdLong = 0f;
		float macdLongSum = 0f;
		var macdLongFormed = false;

		for (var i = 0; i < len; i++)
		{
			var globalIndex = offset + i;
			var candle = flatCandles[globalIndex];

			var prevEma = ema;
			var prevMacd = macdShort - macdLong;

			var price = ExtractPrice(candle, priceType);

			if (!emaFormed)
			{
				emaSum += price;
				ema = emaSum / emaLength;
				if (i >= emaLength - 1)
					emaFormed = true;
			}
			else
			{
				ema = (price - ema) * emaMultiplier + ema;
			}

			if (!macdShortFormed)
			{
				macdShortSum += price;
				macdShort = macdShortSum / macdShortLength;
				if (i >= macdShortLength - 1)
					macdShortFormed = true;
			}
			else
			{
				macdShort = (price - macdShort) * macdShortMultiplier + macdShort;
			}

			if (!macdLongFormed)
			{
				macdLongSum += price;
				macdLong = macdLongSum / macdLongLength;
				if (i >= macdLongLength - 1)
					macdLongFormed = true;
			}
			else
			{
				macdLong = (price - macdLong) * macdLongMultiplier + macdLong;
			}

			var resIndex = paramIdx * candlesCount + globalIndex;
			flatResults[resIndex] = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0
			};

			if (emaFormed && macdShortFormed && macdLongFormed)
			{
				var macdValue = macdShort - macdLong;
				var impulse = 0f;

				if (ema > prevEma && macdValue > prevMacd)
					impulse = 1f;
				else if (ema < prevEma && macdValue < prevMacd)
					impulse = -1f;

				flatResults[resIndex] = new GpuIndicatorResult
				{
					Time = candle.Time,
					Value = impulse,
					IsFormed = 1
				};
			}
		}
	}
}

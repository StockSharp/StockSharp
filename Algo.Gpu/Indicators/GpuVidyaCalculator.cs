namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU VIDYA calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuVidyaParams"/> struct.
/// </remarks>
/// <param name="length">VIDYA length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuVidyaParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// VIDYA period length.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is Vidya vidya)
		{
			Unsafe.AsRef(in this).Length = vidya.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Variable Index Dynamic Average (VIDYA).
/// </summary>
public class GpuVidyaCalculator : GpuIndicatorCalculatorBase<Vidya, GpuVidyaParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuVidyaParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuVidyaCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuVidyaCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuVidyaParams>>(VidyaParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuVidyaParams[] parameters)
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
	/// ILGPU kernel: VIDYA computation for multiple series and parameter sets.
	/// One thread handles one (parameter, series) pair and iterates bars sequentially.
	/// </summary>
	private static void VidyaParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuVidyaParams> parameters)
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
		var prevVidya = 0f;

		// CPU VIDYA has 3 phases:
		// Phase 1 (bars 0 to L-1): CMO not available (needs L+1 bars: 1 init + L deltas) → NaN
		// Phase 2 (bars L to 2L-2): Buffer filling, output running SMA (bufferSum/L)
		// Phase 3 (bars 2L-1+): VIDYA formula with CMO
		var bufferSum = 0f;
		var bufferCount = 0;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var price = ExtractPrice(candle, priceType);
			var resIndex = paramIdx * flatCandles.Length + (offset + i);

			flatResults[resIndex] = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0,
			};

			// Phase 1: CMO not available yet (needs bar 0 as init + L deltas)
			if (i < L)
				continue;

			// Phase 2: Buffer filling (matching CPU's Buffer.PushBack + Sum/Length)
			if (bufferCount < L)
			{
				bufferCount++;
				bufferSum += price;
				prevVidya = bufferSum / L;

				flatResults[resIndex] = new GpuIndicatorResult
				{
					Time = candle.Time,
					Value = prevVidya,
					IsFormed = (byte)(bufferCount >= L ? 1 : 0),
				};
				continue;
			}

			// Phase 3: VIDYA formula with CMO
			var start = i - L + 1;
			var upSum = 0f;
			var downSum = 0f;
			for (var j = start; j <= i; j++)
			{
				var curr = ExtractPrice(flatCandles[offset + j], priceType);
				var prev = ExtractPrice(flatCandles[offset + j - 1], priceType);
				var delta = curr - prev;

				if (delta > 0f)
					upSum += delta;
				else if (delta < 0f)
					downSum += -delta;
			}

			var denom = upSum + downSum;
			var cmo = denom > 0f ? 100f * (upSum - downSum) / denom : 0f;
			var weight = MathF.Abs(cmo) / 100f;
			var curValue = (price - prevVidya) * multiplier * weight + prevVidya;
			prevVidya = curValue;

			flatResults[resIndex] = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = curValue,
				IsFormed = 1,
			};
		}
	}
}

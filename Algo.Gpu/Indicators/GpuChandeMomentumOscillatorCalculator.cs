namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Chande Momentum Oscillator calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuChandeMomentumOscillatorParams"/> struct.
/// </remarks>
/// <param name="length">CMO length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuChandeMomentumOscillatorParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// CMO window length.
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

		if (indicator is ChandeMomentumOscillator cmo)
		{
			Unsafe.AsRef(in this).Length = cmo.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Chande Momentum Oscillator (CMO).
/// </summary>
public class GpuChandeMomentumOscillatorCalculator : GpuIndicatorCalculatorBase<ChandeMomentumOscillator, GpuChandeMomentumOscillatorParams, GpuIndicatorResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuChandeMomentumOscillatorParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuChandeMomentumOscillatorCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuChandeMomentumOscillatorCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuChandeMomentumOscillatorParams>>(ChandeMomentumOscillatorParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuChandeMomentumOscillatorParams[] parameters)
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
		var maxLen = 0;
		var offset = 0;
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			if (len > 0)
			{
				Array.Copy(candlesSeries[s], 0, flatCandles, offset, len);
				offset += len;
				if (len > maxLen)
					maxLen = len;
			}
		}

		using var inputBuffer = Accelerator.Allocate1D(flatCandles);
		using var offsetsBuffer = Accelerator.Allocate1D(seriesOffsets);
		using var lengthsBuffer = Accelerator.Allocate1D(seriesLengths);
		using var paramsBuffer = Accelerator.Allocate1D(parameters);
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var extent = new Index3D(parameters.Length, seriesCount, maxLen);
		_paramsSeriesKernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
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
	/// ILGPU kernel: CMO computation for multiple series and parameter sets. Results are stored as [param][globalIdx].
	/// </summary>
	private static void ChandeMomentumOscillatorParamsSeriesKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuChandeMomentumOscillatorParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;
		var candleIdx = index.Z;

		var len = lengths[seriesIdx];
		if (candleIdx >= len)
			return;

		var offset = offsets[seriesIdx];
		var globalIdx = offset + candleIdx;

		var candle = flatCandles[globalIdx];
		var resIndex = paramIdx * flatCandles.Length + globalIdx;
		flatResults[resIndex] = new() { Time = candle.Time, Value = float.NaN, IsFormed = 0 };

		if (candleIdx == 0)
			return;

		var prm = parameters[paramIdx];
		var length = prm.Length;
		if (length <= 0)
			return;

		if (candleIdx < length)
			return;

		var priceType = (Level1Fields)prm.PriceType;

		var upSum = 0f;
		var downSum = 0f;
		for (var j = 0; j < length; j++)
		{
			var curIdx = globalIdx - j;
			var prevIdx = curIdx - 1;
			if (prevIdx < offset)
				break;

			var currentPrice = ExtractPrice(flatCandles[curIdx], priceType);
			var prevPrice = ExtractPrice(flatCandles[prevIdx], priceType);
			var delta = currentPrice - prevPrice;
			if (delta > 0f)
			{
				upSum += delta;
			}
			else if (delta < 0f)
			{
				downSum += -delta;
			}
		}

		var denom = upSum + downSum;
		var value = denom == 0f ? 0f : 100f * (upSum - downSum) / denom;

		flatResults[resIndex] = new() { Time = candle.Time, Value = value, IsFormed = 1 };
	}
}

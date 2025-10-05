namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Elliot Wave Oscillator calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuElliotWaveOscillatorParams"/> struct.
/// </remarks>
/// <param name="shortPeriod">Short SMA length.</param>
/// <param name="longPeriod">Long SMA length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuElliotWaveOscillatorParams(int shortPeriod, int longPeriod, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Short SMA window length.
	/// </summary>
	public int ShortPeriod = shortPeriod;

	/// <summary>
	/// Long SMA window length.
	/// </summary>
	public int LongPeriod = longPeriod;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is ElliotWaveOscillator ewo)
		{
			Unsafe.AsRef(in this).ShortPeriod = ewo.ShortPeriod;
			Unsafe.AsRef(in this).LongPeriod = ewo.LongPeriod;
		}
	}
}

/// <summary>
/// GPU calculator for Elliot Wave Oscillator (EWO).
/// </summary>
public class GpuElliotWaveOscillatorCalculator : GpuIndicatorCalculatorBase<ElliotWaveOscillator, GpuElliotWaveOscillatorParams, GpuIndicatorResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuElliotWaveOscillatorParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuElliotWaveOscillatorCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuElliotWaveOscillatorCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuElliotWaveOscillatorParams>>(EwoParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuElliotWaveOscillatorParams[] parameters)
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
	/// ILGPU kernel: Elliot Wave Oscillator computation for multiple series and parameter sets.
	/// </summary>
	private static void EwoParamsSeriesKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuElliotWaveOscillatorParams> parameters)
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

		var prm = parameters[paramIdx];
		var shortPeriod = prm.ShortPeriod;
		var longPeriod = prm.LongPeriod;
		if (shortPeriod <= 0 || longPeriod <= 0)
			return;

		var maxPeriod = shortPeriod > longPeriod ? shortPeriod : longPeriod;
		if (candleIdx < maxPeriod - 1)
			return;

		var priceType = (Level1Fields)prm.PriceType;
		var shortSum = 0f;
		for (var j = 0; j < shortPeriod; j++)
			shortSum += ExtractPrice(flatCandles[globalIdx - j], priceType);

		var longSum = 0f;
		for (var j = 0; j < longPeriod; j++)
			longSum += ExtractPrice(flatCandles[globalIdx - j], priceType);

		var shortAvg = shortSum / shortPeriod;
		var longAvg = longSum / longPeriod;
		flatResults[resIndex] = new() { Time = candle.Time, Value = shortAvg - longAvg, IsFormed = 1 };
	}
}

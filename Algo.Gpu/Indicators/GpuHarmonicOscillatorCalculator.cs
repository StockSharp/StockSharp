namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Harmonic Oscillator calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuHarmonicOscillatorParams"/> struct.
/// </remarks>
/// <param name="length">Harmonic Oscillator length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuHarmonicOscillatorParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Harmonic Oscillator window length.
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

		if (indicator is HarmonicOscillator ho)
		{
			Unsafe.AsRef(in this).Length = ho.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Harmonic Oscillator indicator.
/// </summary>
public class GpuHarmonicOscillatorCalculator : GpuIndicatorCalculatorBase<HarmonicOscillator, GpuHarmonicOscillatorParams, GpuIndicatorResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuHarmonicOscillatorParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuHarmonicOscillatorCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuHarmonicOscillatorCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuHarmonicOscillatorParams>>(HarmonicOscillatorParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuHarmonicOscillatorParams[] parameters)
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
	/// ILGPU kernel: Harmonic Oscillator computation for multiple series and multiple parameter sets. Results are stored as [param][globalIdx].
	/// </summary>
	private static void HarmonicOscillatorParamsSeriesKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuHarmonicOscillatorParams> parameters)
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
		var window = prm.Length;
		if (window <= 0)
			window = 1;

		if (candleIdx < window - 1)
			return;

		var priceType = (Level1Fields)prm.PriceType;
		var sum = 0f;
		for (var j = 0; j < window; j++)
		{
			var price = ExtractPrice(flatCandles[globalIdx - j], priceType);
			var angle = 2f * MathF.PI * j / window;
			sum += price * MathF.Sin(angle);
		}

		flatResults[resIndex] = new() { Time = candle.Time, Value = sum / window, IsFormed = 1 };
	}
}

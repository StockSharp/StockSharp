namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Awesome Oscillator calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuAwesomeOscillatorParams"/> struct.
/// </remarks>
/// <param name="longLength">Long SMA length.</param>
/// <param name="shortLength">Short SMA length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuAwesomeOscillatorParams(int longLength, int shortLength) : IGpuIndicatorParams
{
	/// <summary>
	/// Long SMA period length.
	/// </summary>
	public int LongLength = longLength;

	/// <summary>
	/// Short SMA period length.
	/// </summary>
	public int ShortLength = shortLength;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is AwesomeOscillator ao)
		{
			Unsafe.AsRef(in this).LongLength = ao.LongMa.Length;
			Unsafe.AsRef(in this).ShortLength = ao.ShortMa.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Awesome Oscillator (AO).
/// </summary>
public class GpuAwesomeOscillatorCalculator : GpuIndicatorCalculatorBase<AwesomeOscillator, GpuAwesomeOscillatorParams, GpuIndicatorResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuAwesomeOscillatorParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuAwesomeOscillatorCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuAwesomeOscillatorCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuAwesomeOscillatorParams>>(AwesomeOscillatorKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuAwesomeOscillatorParams[] parameters)
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
	/// ILGPU kernel: Awesome Oscillator computation for multiple series and parameter sets. Results are stored as [param][globalIdx].
	/// </summary>
	private static void AwesomeOscillatorKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuAwesomeOscillatorParams> parameters)
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
		var longLength = prm.LongLength;
		var shortLength = prm.ShortLength;

		if (longLength <= 0 || shortLength <= 0)
			return;

		var requiredLength = longLength;
		if (shortLength > requiredLength)
			requiredLength = shortLength;

		if (candleIdx < requiredLength - 1)
			return;

		var longSum = 0f;
		for (var j = 0; j < longLength; j++)
		{
			var c = flatCandles[globalIdx - j];
			longSum += (c.High + c.Low) * 0.5f;
		}

		var shortSum = 0f;
		for (var j = 0; j < shortLength; j++)
		{
			var c = flatCandles[globalIdx - j];
			shortSum += (c.High + c.Low) * 0.5f;
		}

		flatResults[resIndex] = new()
		{
			Time = candle.Time,
			Value = shortSum / shortLength - longSum / longLength,
			IsFormed = 1,
		};
	}
}

namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Acceleration calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuAccelerationParams"/> struct.
/// </remarks>
/// <param name="shortLength">Awesome Oscillator short period length.</param>
/// <param name="longLength">Awesome Oscillator long period length.</param>
/// <param name="smoothingLength">Smoothing length applied to the Acceleration oscillator.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuAccelerationParams(int shortLength, int longLength, int smoothingLength) : IGpuIndicatorParams
{
	/// <summary>
	/// Awesome Oscillator short period length.
	/// </summary>
	public int ShortLength = shortLength;

	/// <summary>
	/// Awesome Oscillator long period length.
	/// </summary>
	public int LongLength = longLength;

	/// <summary>
	/// Smoothing length for the Acceleration oscillator.
	/// </summary>
	public int SmoothingLength = smoothingLength;

	/// <inheritdoc />
	/// <param name="indicator">Acceleration indicator instance.</param>
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is Acceleration acceleration)
		{
			Unsafe.AsRef(in this).ShortLength = acceleration.Ao.ShortMa.Length;
			Unsafe.AsRef(in this).LongLength = acceleration.Ao.LongMa.Length;
			Unsafe.AsRef(in this).SmoothingLength = acceleration.Sma.Length;
		}
	}
}

/// <summary>
/// GPU calculator for the Acceleration indicator.
/// </summary>
public class GpuAccelerationCalculator : GpuIndicatorCalculatorBase<Acceleration, GpuAccelerationParams, GpuIndicatorResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuAccelerationParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuAccelerationCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuAccelerationCalculator(Context context, Accelerator accelerator)
	: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
		<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuAccelerationParams>>(AccelerationParamsSeriesKernel);
	}

	/// <summary>
	/// Executes GPU Acceleration calculation for the provided candle series and parameter combinations.
	/// </summary>
	/// <param name="candlesSeries">Per-series candle arrays.</param>
	/// <param name="parameters">Acceleration parameter combinations.</param>
	/// <returns>GPU indicator results grouped by series and parameter index.</returns>
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuAccelerationParams[] parameters)
	{
		ArgumentNullException.ThrowIfNull(candlesSeries);
		ArgumentNullException.ThrowIfNull(parameters);

		if (candlesSeries.Length == 0)
		{
			throw new ArgumentOutOfRangeException(nameof(candlesSeries));
		}

		if (parameters.Length == 0)
		{
			throw new ArgumentOutOfRangeException(nameof(parameters));
		}

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
				{
					maxLen = len;
				}
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
	/// Kernel executing Acceleration calculations per parameter/series combination.
	/// </summary>
	/// <param name="index">3D index (parameter, series, candle).</param>
	/// <param name="flatCandles">Flattened input candle array.</param>
	/// <param name="flatResults">Flattened GPU result array.</param>
	/// <param name="offsets">Offsets for each series inside the flattened candle array.</param>
	/// <param name="lengths">Lengths of each series.</param>
	/// <param name="parameters">Acceleration parameters per kernel batch.</param>
	private static void AccelerationParamsSeriesKernel(
	Index3D index,
	ArrayView<GpuCandle> flatCandles,
	ArrayView<GpuIndicatorResult> flatResults,
	ArrayView<int> offsets,
	ArrayView<int> lengths,
	ArrayView<GpuAccelerationParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;
		var candleIdx = index.Z;

		var len = lengths[seriesIdx];

		if (candleIdx >= len)
		{
			return;
		}

		var offset = offsets[seriesIdx];
		var globalIdx = offset + candleIdx;
		var prm = parameters[paramIdx];

		var shortLen = prm.ShortLength;
		if (shortLen < 1)
		{
			shortLen = 1;
		}

		var longLen = prm.LongLength;
		if (longLen < 1)
		{
			longLen = 1;
		}

		var smoothingLen = prm.SmoothingLength;
		if (smoothingLen < 1)
		{
			smoothingLen = 1;
		}

		var candle = flatCandles[globalIdx];
		var resIndex = paramIdx * flatCandles.Length + globalIdx;

		var aoValue = ComputeAwesome(flatCandles, offset, globalIdx, candleIdx, shortLen, longLen);
		var value = aoValue;
		byte isFormed = 0;

		if (candleIdx >= longLen - 1)
		{
			var maxAoCount = candleIdx - (longLen - 1) + 1;

			if (maxAoCount > smoothingLen)
			{
				maxAoCount = smoothingLen;
			}

			var aoSum = 0f;

			for (var k = 0; k < maxAoCount; k++)
			{
				var prevLocalIdx = candleIdx - k;
				var prevGlobalIdx = globalIdx - k;
				aoSum += ComputeAwesome(flatCandles, offset, prevGlobalIdx, prevLocalIdx, shortLen, longLen);
			}

			var aoSma = aoSum / smoothingLen;
			value = aoValue - aoSma;

			if (candleIdx >= longLen + smoothingLen - 2)
			{
				isFormed = 1;
			}
		}

		flatResults[resIndex] = new()
		{
			Time = candle.Time,
			Value = value,
			IsFormed = isFormed,
		};
	}

	/// <summary>
	/// Computes Awesome Oscillator value for the specified index.
	/// </summary>
	/// <param name="flatCandles">Flattened candle array.</param>
	/// <param name="seriesOffset">Offset of the current series.</param>
	/// <param name="globalIdx">Global candle index.</param>
	/// <param name="localIdx">Index inside the current series.</param>
	/// <param name="shortLen">Short moving average length.</param>
	/// <param name="longLen">Long moving average length.</param>
	private static float ComputeAwesome(
	ArrayView<GpuCandle> flatCandles,
	int seriesOffset,
	int globalIdx,
	int localIdx,
	int shortLen,
	int longLen)
	{
		var shortCount = shortLen;

		if (shortCount > localIdx + 1)
		{
			shortCount = localIdx + 1;
		}

		var shortSum = 0f;

		for (var i = 0; i < shortCount; i++)
		{
			var idx = globalIdx - i;

			if (idx < seriesOffset)
			{
				break;
			}

			shortSum += GetMedian(flatCandles[idx]);
		}

		var longCount = longLen;

		if (longCount > localIdx + 1)
		{
			longCount = localIdx + 1;
		}

		var longSum = 0f;

		for (var i = 0; i < longCount; i++)
		{
			var idx = globalIdx - i;

			if (idx < seriesOffset)
			{
				break;
			}

			longSum += GetMedian(flatCandles[idx]);
		}

		return shortSum / shortLen - longSum / longLen;
	}

	/// <summary>
	/// Gets the median price for the specified candle.
	/// </summary>
	/// <param name="candle">GPU candle structure.</param>
	private static float GetMedian(GpuCandle candle)
	=> (candle.High + candle.Low) * 0.5f;
}

namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU McClellan Oscillator calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuMcClellanOscillatorParams"/> struct.
/// </remarks>
/// <param name="fastLength">Fast EMA length.</param>
/// <param name="slowLength">Slow EMA length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuMcClellanOscillatorParams(int fastLength, int slowLength, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastLength = fastLength > 0 ? fastLength : 19;

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowLength = slowLength > 0 ? slowLength : 39;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is McClellanOscillator mc)
		{
			Unsafe.AsRef(in this).FastLength = mc.Ema19.Length;
			Unsafe.AsRef(in this).SlowLength = mc.Ema39.Length;
		}
	}
}

/// <summary>
/// GPU calculator for McClellan Oscillator.
/// </summary>
public class GpuMcClellanOscillatorCalculator : GpuIndicatorCalculatorBase<McClellanOscillator, GpuMcClellanOscillatorParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMcClellanOscillatorParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuMcClellanOscillatorCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuMcClellanOscillatorCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMcClellanOscillatorParams>>(McClellanParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuMcClellanOscillatorParams[] parameters)
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
					var resIdx = p * flatCandles.Length + globalIdx;
					arr[i] = flatResults[resIdx];
				}
				result[s][p] = arr;
			}
		}

		return result;
	}

	/// <summary>
	/// ILGPU kernel: McClellan Oscillator computation for multiple series and parameter sets.
	/// </summary>
	private static void McClellanParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuMcClellanOscillatorParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];

		var fastLength = prm.FastLength;
		if (fastLength <= 0)
			fastLength = 1;

		var slowLength = prm.SlowLength;
		if (slowLength <= 0)
			slowLength = 1;

		var fastMultiplier = 2f / (fastLength + 1f);
		var slowMultiplier = 2f / (slowLength + 1f);
		var priceType = (Level1Fields)prm.PriceType;

		var fastCount = 0;
		var slowCount = 0;
		var fastSum = 0f;
		var slowSum = 0f;
		var fastEma = 0f;
		var slowEma = 0f;
		var fastReady = false;
		var slowReady = false;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var price = ExtractPrice(candle, priceType);

			var resIndex = paramIdx * flatCandles.Length + globalIdx;
			flatResults[resIndex] = new() { Time = candle.Time, Value = float.NaN, IsFormed = 0 };

			if (!fastReady)
			{
				fastSum += price;
				fastCount++;
				if (fastCount == fastLength)
				{
					fastEma = fastSum / fastLength;
					fastReady = true;
				}
			}
			else
			{
				fastEma += fastMultiplier * (price - fastEma);
			}

			if (!slowReady)
			{
				slowSum += price;
				slowCount++;
				if (slowCount == slowLength)
				{
					slowEma = slowSum / slowLength;
					slowReady = true;
				}
			}
			else
			{
				slowEma += slowMultiplier * (price - slowEma);
			}

			if (fastReady && slowReady)
			{
				flatResults[resIndex] = new()
				{
					Time = candle.Time,
					Value = fastEma - slowEma,
					IsFormed = 1
				};
			}
		}
	}
}

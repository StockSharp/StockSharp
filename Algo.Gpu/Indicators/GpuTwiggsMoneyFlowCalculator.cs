namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Twiggs Money Flow calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuTwiggsMoneyFlowParams"/> struct.
/// </remarks>
/// <param name="length">Indicator length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuTwiggsMoneyFlowParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// Indicator period length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is TwiggsMoneyFlow tmf)
		{
			Unsafe.AsRef(in this).Length = tmf.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Twiggs Money Flow indicator.
/// </summary>
public class GpuTwiggsMoneyFlowCalculator : GpuIndicatorCalculatorBase<TwiggsMoneyFlow, GpuTwiggsMoneyFlowParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuTwiggsMoneyFlowParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuTwiggsMoneyFlowCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuTwiggsMoneyFlowCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuTwiggsMoneyFlowParams>>(TwiggsMoneyFlowKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuTwiggsMoneyFlowParams[] parameters)
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
	/// ILGPU kernel computing Twiggs Money Flow for multiple series and parameter sets.
	/// </summary>
	private static void TwiggsMoneyFlowKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuTwiggsMoneyFlowParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var length = parameters[paramIdx].Length;
		if (length <= 0)
			length = 1;

		var multiplier = 2f / (length + 1);
		var sumAd = 0f;
		var sumVol = 0f;
		var advEma = 0f;
		var volEma = 0f;
		var prevAd = 0f;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var high = candle.High;
			var low = candle.Low;
			var close = candle.Close;
			var volume = candle.Volume;
			var range = high - low;
			var typical = (high + low + close) / 3f;

			var ad = range != 0f
				? volume * (2f * typical - high - low) / range
				: prevAd;

			prevAd = ad;

			var globalIdx = offset + i;
			var resIndex = paramIdx * flatCandles.Length + globalIdx;
			flatResults[resIndex] = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0
			};

			if (i < length)
			{
				sumAd += ad;
				sumVol += volume;

				if (i == length - 1)
				{
					advEma = sumAd / length;
					volEma = sumVol / length;
				}
			}
			else
			{
				advEma = (ad - advEma) * multiplier + advEma;
				volEma = (volume - volEma) * multiplier + volEma;
			}

			if (i >= length - 1)
			{
				var value = 0f;

				if (volEma != 0f)
					value = advEma / volEma;

				flatResults[resIndex] = new GpuIndicatorResult
				{
					Time = candle.Time,
					Value = value == 0f ? float.NaN : value,
					IsFormed = 1
				};
			}
		}
	}
}

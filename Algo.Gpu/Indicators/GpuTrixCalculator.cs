namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU TRIX calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuTrixParams"/> struct.
/// </remarks>
/// <param name="length">EMA length.</param>
/// <param name="rocLength">Rate of Change length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuTrixParams(int length, int rocLength, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// EMA window length.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// Rate of Change period length.
	/// </summary>
	public int RocLength = rocLength;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is Trix trix)
		{
			Unsafe.AsRef(in this).Length = trix.Length;
			Unsafe.AsRef(in this).RocLength = trix.RocLength;
		}
	}
}

/// <summary>
/// GPU calculator for Trix (Triple Exponential Average Oscillator).
/// </summary>
public class GpuTrixCalculator : GpuIndicatorCalculatorBase<Trix, GpuTrixParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<float>, ArrayView<int>, ArrayView<int>, ArrayView<GpuTrixParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuTrixCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuTrixCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<float>, ArrayView<int>, ArrayView<int>, ArrayView<GpuTrixParams>>(TrixParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuTrixParams[] parameters)
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
		using var ema3Buffer = Accelerator.Allocate1D<float>(totalSize * parameters.Length);
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, ema3Buffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
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
	/// ILGPU kernel: TRIX computation for multiple series and multiple parameter sets.
	/// One thread handles one (parameter, series) pair and iterates bars sequentially.
	/// </summary>
	private static void TrixParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<float> ema3History,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuTrixParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var length = prm.Length;
		if (length <= 0)
			length = 1;

		var rocLen = prm.RocLength;
		if (rocLen <= 0)
			rocLen = 1;

		var multiplier = 2f / (length + 1);
		var priceType = (Level1Fields)prm.PriceType;

		var baseIndex = paramIdx * flatCandles.Length + offset;

		float sum1 = 0f, sum2 = 0f, sum3 = 0f;
		int count2 = 0, count3 = 0;
		var ema1Formed = false;
		var ema2Formed = false;
		var ema3Formed = false;
		float ema1Prev = 0f, ema2Prev = 0f, ema3Prev = 0f;

		for (int i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var price = ExtractPrice(candle, priceType);

			var resIndex = baseIndex + i;
			flatResults[resIndex] = new() { Time = candle.Time, Value = float.NaN, IsFormed = 0 };
			ema3History[resIndex] = float.NaN;

			sum1 += price;

			if (!ema1Formed)
			{
				if (i == length - 1)
				{
					ema1Prev = sum1 / length;
					ema1Formed = true;
				}
				else
				{
					continue;
				}
			}
			else
			{
				ema1Prev = ema1Prev + multiplier * (price - ema1Prev);
			}

			if (!ema2Formed)
			{
				sum2 += ema1Prev;
				count2++;
				if (count2 < length)
					continue;

				ema2Prev = sum2 / length;
				ema2Formed = true;
			}
			else
			{
				ema2Prev = ema2Prev + multiplier * (ema1Prev - ema2Prev);
			}

			if (!ema3Formed)
			{
				sum3 += ema2Prev;
				count3++;
				if (count3 < length)
					continue;

				ema3Prev = sum3 / length;
				ema3Formed = true;
			}
			else
			{
				ema3Prev = ema3Prev + multiplier * (ema2Prev - ema3Prev);
			}

			ema3History[resIndex] = ema3Prev;

			if (i < rocLen)
				continue;

			var prevIndex = resIndex - rocLen;
			var prevEma3 = ema3History[prevIndex];
			if (float.IsNaN(prevEma3) || prevEma3 == 0f)
				continue;

			var roc = ((ema3Prev - prevEma3) / prevEma3) * 100f;
			flatResults[resIndex] = new() { Time = candle.Time, Value = 10f * roc, IsFormed = 1 };
		}
	}
}

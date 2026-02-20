namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Guppy Multiple Moving Average calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuGmmaParams"/> struct.
/// </remarks>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuGmmaParams(byte priceType) : IGpuIndicatorParams
{
	private static readonly int[] _defaultLengths = new[] { 3, 5, 8, 10, 12, 15, 30, 35, 40, 45, 50, 60 };

	/// <summary>
	/// Number of moving averages calculated by GMMA.
	/// </summary>
	public const int AveragesCount = GpuGmmaResult.AveragesCount;

	/// <summary>
	/// Length of the first exponential moving average.
	/// </summary>
	public int Length0 = 3;

	/// <summary>
	/// Length of the second exponential moving average.
	/// </summary>
	public int Length1 = 5;

	/// <summary>
	/// Length of the third exponential moving average.
	/// </summary>
	public int Length2 = 8;

	/// <summary>
	/// Length of the fourth exponential moving average.
	/// </summary>
	public int Length3 = 10;

	/// <summary>
	/// Length of the fifth exponential moving average.
	/// </summary>
	public int Length4 = 12;

	/// <summary>
	/// Length of the sixth exponential moving average.
	/// </summary>
	public int Length5 = 15;

	/// <summary>
	/// Length of the seventh exponential moving average.
	/// </summary>
	public int Length6 = 30;

	/// <summary>
	/// Length of the eighth exponential moving average.
	/// </summary>
	public int Length7 = 35;

	/// <summary>
	/// Length of the ninth exponential moving average.
	/// </summary>
	public int Length8 = 40;

	/// <summary>
	/// Length of the tenth exponential moving average.
	/// </summary>
	public int Length9 = 45;

	/// <summary>
	/// Length of the eleventh exponential moving average.
	/// </summary>
	public int Length10 = 50;

	/// <summary>
	/// Length of the twelfth exponential moving average.
	/// </summary>
	public int Length11 = 60;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <summary>
	/// Get GMMA default lengths.
	/// </summary>
	public static ReadOnlySpan<int> DefaultLengths => _defaultLengths;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		var dest = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in this).Length0, AveragesCount);
		var defaults = DefaultLengths;
		defaults.CopyTo(dest);

		if (indicator is not GuppyMultipleMovingAverage gmma)
			return;

		var index = 0;
		foreach (var ema in gmma.InnerIndicators.OfType<ExponentialMovingAverage>())
		{
			if (index >= dest.Length)
				break;

			dest[index++] = ema.Length;
		}

		for (; index < dest.Length; index++)
			dest[index] = defaults[index];
	}

	/// <summary>
	/// Get configured length by index.
	/// </summary>
	/// <param name="index">Moving average index.</param>
	/// <returns>Configured length.</returns>
	public readonly int GetLength(int index)
	{
		return index switch
		{
			0 => Length0,
			1 => Length1,
			2 => Length2,
			3 => Length3,
			4 => Length4,
			5 => Length5,
			6 => Length6,
			7 => Length7,
			8 => Length8,
			9 => Length9,
			10 => Length10,
			_ => Length11,
		};
	}
}

/// <summary>
/// GPU result for Guppy Multiple Moving Average calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuGmmaResult : IGpuIndicatorResult
{
	/// <summary>
	/// Number of averages produced by GMMA.
	/// </summary>
	public const int AveragesCount = 12;

	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// First moving average value.
	/// </summary>
	public float Average0;

	/// <summary>
	/// Second moving average value.
	/// </summary>
	public float Average1;

	/// <summary>
	/// Third moving average value.
	/// </summary>
	public float Average2;

	/// <summary>
	/// Fourth moving average value.
	/// </summary>
	public float Average3;

	/// <summary>
	/// Fifth moving average value.
	/// </summary>
	public float Average4;

	/// <summary>
	/// Sixth moving average value.
	/// </summary>
	public float Average5;

	/// <summary>
	/// Seventh moving average value.
	/// </summary>
	public float Average6;

	/// <summary>
	/// Eighth moving average value.
	/// </summary>
	public float Average7;

	/// <summary>
	/// Ninth moving average value.
	/// </summary>
	public float Average8;

	/// <summary>
	/// Tenth moving average value.
	/// </summary>
	public float Average9;

	/// <summary>
	/// Eleventh moving average value.
	/// </summary>
	public float Average10;

	/// <summary>
	/// Twelfth moving average value.
	/// </summary>
	public float Average11;

	/// <summary>
	/// Is indicator formed (int for GPU alignment safety).
	/// </summary>
	public int IsFormed;

	readonly long IGpuIndicatorResult.Time => Time;
	readonly byte IGpuIndicatorResult.IsFormed => (byte)IsFormed;

	/// <summary>
	/// Get average value by index.
	/// </summary>
	/// <param name="index">Average index.</param>
	/// <returns>Average value.</returns>
	public readonly float GetAverage(int index)
	{
		return index switch
		{
			0 => Average0,
			1 => Average1,
			2 => Average2,
			3 => Average3,
			4 => Average4,
			5 => Average5,
			6 => Average6,
			7 => Average7,
			8 => Average8,
			9 => Average9,
			10 => Average10,
			_ => Average11,
		};
	}

	/// <summary>
	/// Set average value by index.
	/// </summary>
	/// <param name="index">Average index.</param>
	/// <param name="value">Average value.</param>
	public void SetAverage(int index, float value)
	{
		switch (index)
		{
			case 0:
				Average0 = value;
				break;
			case 1:
				Average1 = value;
				break;
			case 2:
				Average2 = value;
				break;
			case 3:
				Average3 = value;
				break;
			case 4:
				Average4 = value;
				break;
			case 5:
				Average5 = value;
				break;
			case 6:
				Average6 = value;
				break;
			case 7:
				Average7 = value;
				break;
			case 8:
				Average8 = value;
				break;
			case 9:
				Average9 = value;
				break;
			case 10:
				Average10 = value;
				break;
			default:
				Average11 = value;
				break;
		}
	}

	/// <inheritdoc />
	public readonly IIndicatorValue ToValue(IIndicator indicator)
	{
		var time = this.GetTime();
		var isFormed = this.GetIsFormed();
		var gmma = (GuppyMultipleMovingAverage)indicator;

		var value = new GuppyMultipleMovingAverageValue(gmma, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		var hasValue = false;
		var innerCount = Math.Min(gmma.InnerIndicators.Count, AveragesCount);
		for (var i = 0; i < innerCount; i++)
		{
			var avg = GetAverage(i);
			var inner = gmma.InnerIndicators[i];

			if (float.IsNaN(avg))
			{
				value.Add(inner, new DecimalIndicatorValue(inner, time)
				{
					IsFinal = true,
					IsFormed = false,
				});
			}
			else
			{
				value.Add(inner, new DecimalIndicatorValue(inner, (decimal)avg, time)
				{
					IsFinal = true,
					IsFormed = true,
				});
				hasValue = true;
			}
		}

		value.Add(gmma, new DecimalIndicatorValue(gmma, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
			IsEmpty = !hasValue,
		});

		value.IsEmpty = !hasValue;
		return value;
	}
}

/// <summary>
/// GPU calculator for Guppy Multiple Moving Average (GMMA).
/// </summary>
public class GpuGmmaCalculator : GpuIndicatorCalculatorBase<GuppyMultipleMovingAverage, GpuGmmaParams, GpuGmmaResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuGmmaResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuGmmaParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuGmmaCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuGmmaCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuGmmaResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuGmmaParams>>(GmmaParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuGmmaResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuGmmaParams[] parameters)
	{
		ArgumentNullException.ThrowIfNull(candlesSeries);
		ArgumentNullException.ThrowIfNull(parameters);

		if (candlesSeries.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(candlesSeries));

		if (parameters.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(parameters));

		var seriesCount = candlesSeries.Length;
		var seriesOffsets = new int[seriesCount];
		var seriesLengths = new int[seriesCount];
		var totalSize = 0;

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
			if (len <= 0)
				continue;

			Array.Copy(candlesSeries[s], 0, flatCandles, offset, len);
			offset += len;
		}

		using var inputBuffer = Accelerator.Allocate1D(flatCandles);
		using var offsetsBuffer = Accelerator.Allocate1D(seriesOffsets);
		using var lengthsBuffer = Accelerator.Allocate1D(seriesLengths);
		using var paramsBuffer = Accelerator.Allocate1D(parameters);
		using var outputBuffer = Accelerator.Allocate1D<GpuGmmaResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();
		var result = new GpuGmmaResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuGmmaResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				// Compute maxLen from parameters for CPU-side IsFormed
				var prm = parameters[p];
				var maxLen = prm.Length0;
				if (prm.Length1 > maxLen) maxLen = prm.Length1;
				if (prm.Length2 > maxLen) maxLen = prm.Length2;
				if (prm.Length3 > maxLen) maxLen = prm.Length3;
				if (prm.Length4 > maxLen) maxLen = prm.Length4;
				if (prm.Length5 > maxLen) maxLen = prm.Length5;
				if (prm.Length6 > maxLen) maxLen = prm.Length6;
				if (prm.Length7 > maxLen) maxLen = prm.Length7;
				if (prm.Length8 > maxLen) maxLen = prm.Length8;
				if (prm.Length9 > maxLen) maxLen = prm.Length9;
				if (prm.Length10 > maxLen) maxLen = prm.Length10;
				if (prm.Length11 > maxLen) maxLen = prm.Length11;

				var arr = new GpuGmmaResult[len];
				for (var i = 0; i < len; i++)
				{
					var globalIdx = seriesOffsets[s] + i;
					var resIdx = p * totalSize + globalIdx;
					arr[i] = flatResults[resIdx];
					// CPU-side IsFormed: one-bar delayed formed (BaseComplexIndicator pattern)
					arr[i].IsFormed = i >= maxLen ? 1 : 0;
				}
				result[s][p] = arr;
			}
		}

		return result;
	}

	/// <summary>
	/// ILGPU kernel for GMMA computation. One thread handles one parameter set for a single series.
	/// </summary>
	private static void GmmaParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuGmmaResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuGmmaParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var priceType = (Level1Fields)prm.PriceType;

		var len0 = prm.Length0 > 0 ? prm.Length0 : 1;
		var len1 = prm.Length1 > 0 ? prm.Length1 : 1;
		var len2 = prm.Length2 > 0 ? prm.Length2 : 1;
		var len3 = prm.Length3 > 0 ? prm.Length3 : 1;
		var len4 = prm.Length4 > 0 ? prm.Length4 : 1;
		var len5 = prm.Length5 > 0 ? prm.Length5 : 1;
		var len6 = prm.Length6 > 0 ? prm.Length6 : 1;
		var len7 = prm.Length7 > 0 ? prm.Length7 : 1;
		var len8 = prm.Length8 > 0 ? prm.Length8 : 1;
		var len9 = prm.Length9 > 0 ? prm.Length9 : 1;
		var len10 = prm.Length10 > 0 ? prm.Length10 : 1;
		var len11 = prm.Length11 > 0 ? prm.Length11 : 1;

		var maxLen = len0;
		if (len1 > maxLen)
			maxLen = len1;
		if (len2 > maxLen)
			maxLen = len2;
		if (len3 > maxLen)
			maxLen = len3;
		if (len4 > maxLen)
			maxLen = len4;
		if (len5 > maxLen)
			maxLen = len5;
		if (len6 > maxLen)
			maxLen = len6;
		if (len7 > maxLen)
			maxLen = len7;
		if (len8 > maxLen)
			maxLen = len8;
		if (len9 > maxLen)
			maxLen = len9;
		if (len10 > maxLen)
			maxLen = len10;
		if (len11 > maxLen)
			maxLen = len11;

		var sum0 = 0f;
		var sum1 = 0f;
		var sum2 = 0f;
		var sum3 = 0f;
		var sum4 = 0f;
		var sum5 = 0f;
		var sum6 = 0f;
		var sum7 = 0f;
		var sum8 = 0f;
		var sum9 = 0f;
		var sum10 = 0f;
		var sum11 = 0f;

		var ema0 = 0f;
		var ema1 = 0f;
		var ema2 = 0f;
		var ema3 = 0f;
		var ema4 = 0f;
		var ema5 = 0f;
		var ema6 = 0f;
		var ema7 = 0f;
		var ema8 = 0f;
		var ema9 = 0f;
		var ema10 = 0f;
		var ema11 = 0f;

		var mul0 = 2f / (len0 + 1f);
		var mul1 = 2f / (len1 + 1f);
		var mul2 = 2f / (len2 + 1f);
		var mul3 = 2f / (len3 + 1f);
		var mul4 = 2f / (len4 + 1f);
		var mul5 = 2f / (len5 + 1f);
		var mul6 = 2f / (len6 + 1f);
		var mul7 = 2f / (len7 + 1f);
		var mul8 = 2f / (len8 + 1f);
		var mul9 = 2f / (len9 + 1f);
		var mul10 = 2f / (len10 + 1f);
		var mul11 = 2f / (len11 + 1f);

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var price = ExtractPrice(candle, priceType);
			var resIndex = paramIdx * flatCandles.Length + globalIdx;

			if (i < len0)
				sum0 += price;
			if (i == len0 - 1)
				ema0 = sum0 / len0;
			else if (i >= len0)
				ema0 = (price - ema0) * mul0 + ema0;

			if (i < len1)
				sum1 += price;
			if (i == len1 - 1)
				ema1 = sum1 / len1;
			else if (i >= len1)
				ema1 = (price - ema1) * mul1 + ema1;

			if (i < len2)
				sum2 += price;
			if (i == len2 - 1)
				ema2 = sum2 / len2;
			else if (i >= len2)
				ema2 = (price - ema2) * mul2 + ema2;

			if (i < len3)
				sum3 += price;
			if (i == len3 - 1)
				ema3 = sum3 / len3;
			else if (i >= len3)
				ema3 = (price - ema3) * mul3 + ema3;

			if (i < len4)
				sum4 += price;
			if (i == len4 - 1)
				ema4 = sum4 / len4;
			else if (i >= len4)
				ema4 = (price - ema4) * mul4 + ema4;

			if (i < len5)
				sum5 += price;
			if (i == len5 - 1)
				ema5 = sum5 / len5;
			else if (i >= len5)
				ema5 = (price - ema5) * mul5 + ema5;

			if (i < len6)
				sum6 += price;
			if (i == len6 - 1)
				ema6 = sum6 / len6;
			else if (i >= len6)
				ema6 = (price - ema6) * mul6 + ema6;

			if (i < len7)
				sum7 += price;
			if (i == len7 - 1)
				ema7 = sum7 / len7;
			else if (i >= len7)
				ema7 = (price - ema7) * mul7 + ema7;

			if (i < len8)
				sum8 += price;
			if (i == len8 - 1)
				ema8 = sum8 / len8;
			else if (i >= len8)
				ema8 = (price - ema8) * mul8 + ema8;

			if (i < len9)
				sum9 += price;
			if (i == len9 - 1)
				ema9 = sum9 / len9;
			else if (i >= len9)
				ema9 = (price - ema9) * mul9 + ema9;

			if (i < len10)
				sum10 += price;
			if (i == len10 - 1)
				ema10 = sum10 / len10;
			else if (i >= len10)
				ema10 = (price - ema10) * mul10 + ema10;

			if (i < len11)
				sum11 += price;
			if (i == len11 - 1)
				ema11 = sum11 / len11;
			else if (i >= len11)
				ema11 = (price - ema11) * mul11 + ema11;

			var result = default(GpuGmmaResult);
			result.Time = candle.Time;
			result.Average0 = i >= len0 - 1 ? ema0 : float.NaN;
			result.Average1 = i >= len1 - 1 ? ema1 : float.NaN;
			result.Average2 = i >= len2 - 1 ? ema2 : float.NaN;
			result.Average3 = i >= len3 - 1 ? ema3 : float.NaN;
			result.Average4 = i >= len4 - 1 ? ema4 : float.NaN;
			result.Average5 = i >= len5 - 1 ? ema5 : float.NaN;
			result.Average6 = i >= len6 - 1 ? ema6 : float.NaN;
			result.Average7 = i >= len7 - 1 ? ema7 : float.NaN;
			result.Average8 = i >= len8 - 1 ? ema8 : float.NaN;
			result.Average9 = i >= len9 - 1 ? ema9 : float.NaN;
			result.Average10 = i >= len10 - 1 ? ema10 : float.NaN;
			result.Average11 = i >= len11 - 1 ? ema11 : float.NaN;

			flatResults[resIndex] = result;
		}
	}
}

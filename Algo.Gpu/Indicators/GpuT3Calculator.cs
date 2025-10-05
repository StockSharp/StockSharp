namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU T3 Moving Average calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuT3Params"/> struct.
/// </remarks>
/// <param name="length">Indicator length.</param>
/// <param name="volumeFactor">T3 volume factor.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuT3Params(int length, float volumeFactor, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Period length.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// T3 volume factor (0..1).
	/// </summary>
	public float VolumeFactor = volumeFactor;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is T3MovingAverage t3)
		{
			Unsafe.AsRef(in this).Length = t3.Length;
			Unsafe.AsRef(in this).VolumeFactor = (float)t3.VolumeFactor;
		}
	}
}

/// <summary>
/// GPU calculator for T3 Moving Average indicator.
/// </summary>
public class GpuT3Calculator : GpuIndicatorCalculatorBase<T3MovingAverage, GpuT3Params, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuT3Params>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuT3Calculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuT3Calculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuT3Params>>(T3ParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuT3Params[] parameters)
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
	/// ILGPU kernel: sequential T3 Moving Average computation per (parameter, series) pair.
	/// </summary>
	private static void T3ParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuT3Params> parameters)
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

		var vf = prm.VolumeFactor;
		if (vf <= 0f)
			vf = 0.000001f;
		else if (vf >= 1f)
			vf = 0.999999f;

		var alpha = 2f / (L + 1f);
		var oneMinusAlpha = 1f - alpha;

		var vf2 = vf * vf;
		var vf3 = vf2 * vf;
		var c1 = -vf3;
		var c2 = 3f * vf2 + 3f * vf3;
		var c3 = -6f * vf2 - 3f * vf - 3f * vf3;
		var c4 = 1f + 3f * vf + vf3 + 3f * vf2;

		float sum1 = 0f, sum2 = 0f, sum3 = 0f, sum4 = 0f, sum5 = 0f, sum6 = 0f;
		int count1 = 0, count2 = 0, count3 = 0, count4 = 0, count5 = 0, count6 = 0;
		float e1 = 0f, e2 = 0f, e3 = 0f, e4 = 0f, e5 = 0f, e6 = 0f;
		int formed1 = 0, formed2 = 0, formed3 = 0, formed4 = 0, formed5 = 0, formed6 = 0;
		var warmUp = 10;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var price = ExtractPrice(candle, (Level1Fields)prm.PriceType);

			count1++;
			sum1 += price;
			if (count1 < L)
			{
				e1 = sum1 / L;
			}
			else if (count1 == L)
			{
				e1 = sum1 / L;
				formed1 = 1;
			}
			else
			{
				e1 = alpha * price + oneMinusAlpha * e1;
				formed1 = 1;
			}

			count2++;
			sum2 += e1;
			if (count2 < L)
			{
				e2 = sum2 / L;
			}
			else if (count2 == L)
			{
				e2 = sum2 / L;
				formed2 = 1;
			}
			else
			{
				e2 = alpha * e1 + oneMinusAlpha * e2;
				formed2 = 1;
			}

			count3++;
			sum3 += e2;
			if (count3 < L)
			{
				e3 = sum3 / L;
			}
			else if (count3 == L)
			{
				e3 = sum3 / L;
				formed3 = 1;
			}
			else
			{
				e3 = alpha * e2 + oneMinusAlpha * e3;
				formed3 = 1;
			}

			count4++;
			sum4 += e3;
			if (count4 < L)
			{
				e4 = sum4 / L;
			}
			else if (count4 == L)
			{
				e4 = sum4 / L;
				formed4 = 1;
			}
			else
			{
				e4 = alpha * e3 + oneMinusAlpha * e4;
				formed4 = 1;
			}

			count5++;
			sum5 += e4;
			if (count5 < L)
			{
				e5 = sum5 / L;
			}
			else if (count5 == L)
			{
				e5 = sum5 / L;
				formed5 = 1;
			}
			else
			{
				e5 = alpha * e4 + oneMinusAlpha * e5;
				formed5 = 1;
			}

			count6++;
			sum6 += e5;
			if (count6 < L)
			{
				e6 = sum6 / L;
			}
			else if (count6 == L)
			{
				e6 = sum6 / L;
				formed6 = 1;
			}
			else
			{
				e6 = alpha * e5 + oneMinusAlpha * e6;
				formed6 = 1;
			}

			var resIndex = paramIdx * flatCandles.Length + globalIdx;
			flatResults[resIndex] = new()
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0
			};

			if (formed1 == 1 && formed2 == 1 && formed3 == 1 && formed4 == 1 && formed5 == 1 && formed6 == 1)
			{
				if (warmUp > 0)
				{
					warmUp--;
				}
				else
				{
					var value = c1 * e6 + c2 * e5 + c3 * e4 + c4 * e3;
					flatResults[resIndex] = new()
					{
						Time = candle.Time,
						Value = value,
						IsFormed = 1
					};
				}
			}
		}
	}
}

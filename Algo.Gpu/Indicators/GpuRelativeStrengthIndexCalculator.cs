namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Relative Strength Index (RSI) calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuRsiParams"/> struct.
/// </remarks>
/// <param name="length">RSI length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuRsiParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// RSI period length.
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

		if (indicator is RelativeStrengthIndex rsi)
		{
			Unsafe.AsRef(in this).Length = rsi.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Relative Strength Index (RSI).
/// </summary>
public class GpuRelativeStrengthIndexCalculator : GpuIndicatorCalculatorBase<RelativeStrengthIndex, GpuRsiParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuRsiParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuRelativeStrengthIndexCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuRelativeStrengthIndexCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuRsiParams>>(RsiParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuRsiParams[] parameters)
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
	/// ILGPU kernel: RSI computation for multiple series and parameter sets. One thread processes one (parameter, series) pair.
	/// </summary>
	private static void RsiParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuRsiParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
		{
			return;
		}

		var prm = parameters[paramIdx];
		var L = prm.Length;
		if (L <= 0)
		{
			L = 1;
		}

		var priceType = (Level1Fields)prm.PriceType;

		var firstCandle = flatCandles[offset];
		var prevPrice = ExtractPrice(firstCandle, priceType);

		var resIndexFirst = paramIdx * flatCandles.Length + offset;
		flatResults[resIndexFirst] = new GpuIndicatorResult { Time = firstCandle.Time, Value = float.NaN, IsFormed = 0 };

		float gainSum = 0f, lossSum = 0f;
		float avgGain = 0f, avgLoss = 0f;

		for (var i = 1; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var price = ExtractPrice(candle, priceType);

			var delta = price - prevPrice;
			var gain = delta > 0f ? delta : 0f;
			var loss = delta < 0f ? -delta : 0f;

			if (i <= L)
			{
				gainSum += gain;
				lossSum += loss;
				if (i == L)
				{
					avgGain = gainSum / L;
					avgLoss = lossSum / L;
				}
			}
			else
			{
				avgGain = ((avgGain * (L - 1)) + gain) / L;
				avgLoss = ((avgLoss * (L - 1)) + loss) / L;
			}

			var resIndex = paramIdx * flatCandles.Length + globalIdx;
			var result = new GpuIndicatorResult { Time = candle.Time, Value = float.NaN, IsFormed = 0 };

			if (i >= L)
			{
				float value;
				if (avgLoss == 0f)
				{
					value = 100f;
				}
				else
				{
					var ratio = avgGain / avgLoss;
					if (ratio == 1f)
					{
						value = 0f;
					}
					else
					{
						value = 100f - 100f / (1f + ratio);
					}
				}

				result.Value = value;
				result.IsFormed = 1;
			}

			flatResults[resIndex] = result;
			prevPrice = price;
		}
	}
}

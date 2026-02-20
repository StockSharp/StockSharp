namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Dynamic Zones RSI calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuDynamicZonesRsiParams"/> struct.
/// </remarks>
/// <param name="length">RSI length.</param>
/// <param name="priceType">Price type for RSI source.</param>
/// <param name="oversoldLevel">Oversold level in percent.</param>
/// <param name="overboughtLevel">Overbought level in percent.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuDynamicZonesRsiParams(int length, byte priceType, float oversoldLevel, float overboughtLevel) : IGpuIndicatorParams
{
	/// <summary>
	/// RSI period length.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <summary>
	/// Oversold level percentage.
	/// </summary>
	public float OversoldLevel = oversoldLevel;

	/// <summary>
	/// Overbought level percentage.
	/// </summary>
	public float OverboughtLevel = overboughtLevel;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is DynamicZonesRSI dzrsi)
		{
			Unsafe.AsRef(in this).Length = dzrsi.Length;
			Unsafe.AsRef(in this).OversoldLevel = (float)dzrsi.OversoldLevel;
			Unsafe.AsRef(in this).OverboughtLevel = (float)dzrsi.OverboughtLevel;
		}
	}
}

/// <summary>
/// GPU calculator for Dynamic Zones RSI.
/// </summary>
public class GpuDynamicZonesRsiCalculator : GpuIndicatorCalculatorBase<DynamicZonesRSI, GpuDynamicZonesRsiParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<float>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuDynamicZonesRsiParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuDynamicZonesRsiCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuDynamicZonesRsiCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<float>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuDynamicZonesRsiParams>>(DynamicZonesRsiParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuDynamicZonesRsiParams[] parameters)
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
		using var rsiBuffer = Accelerator.Allocate1D<float>(totalSize * parameters.Length);
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, rsiBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
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
	/// ILGPU kernel: Dynamic Zones RSI computation for multiple series and parameter sets.
	/// One thread processes one (parameter, series) pair and iterates through all bars sequentially.
	/// </summary>
	private static void DynamicZonesRsiParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<float> rsiBuffer,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuDynamicZonesRsiParams> parameters)
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

		var priceType = (Level1Fields)prm.PriceType;

		float sumGain = 0f, sumLoss = 0f;
		float avgGain = 0f, avgLoss = 0f;
		float prevValue = ExtractPrice(flatCandles[offset], priceType);
		float prevRsi = 50f;

		byte prevFormed = 0;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var value = ExtractPrice(candle, priceType);

			var resIndex = paramIdx * flatCandles.Length + (offset + i);
			flatResults[resIndex] = new() { Time = candle.Time, Value = float.NaN, IsFormed = prevFormed };
			rsiBuffer[resIndex] = float.NaN;

			byte curFormed = (byte)(i >= 2 * L - 2 ? 1 : 0);

			if (i == 0)
			{
				prevFormed = curFormed;
				prevValue = value;
				continue;
			}

			var delta = value - prevValue;
			var gain = delta > 0f ? delta : 0f;
			var loss = delta < 0f ? -delta : 0f;

			if (i <= L)
			{
				sumGain += gain;
				sumLoss += loss;
			}

			if (i == L)
			{
				avgGain = sumGain / L;
				avgLoss = sumLoss / L;
			}
			else if (i > L)
			{
				avgGain = ((avgGain * (L - 1)) + gain) / L;
				avgLoss = ((avgLoss * (L - 1)) + loss) / L;
			}

			if (i >= L)
			{
				// CPU formula: rsi = 100 * avgGain / (avgGain + avgLoss)
				// When sum == 0 (no movement): return prevResult ?? 50
				var rsiSum = avgGain + avgLoss;
				float rsi;
				if (rsiSum == 0f)
					rsi = prevRsi;
				else
					rsi = 100f * avgGain / rsiSum;

				prevRsi = rsi;

				rsiBuffer[resIndex] = rsi;

				var start = i - L + 1;
				if (start < 0)
					start = 0;

				var min = rsi;
				var max = rsi;
				for (var j = start; j <= i; j++)
				{
					var idx = paramIdx * flatCandles.Length + (offset + j);
					var val = rsiBuffer[idx];
					if (float.IsNaN(val))
						continue;
					if (val < min)
						min = val;
					if (val > max)
						max = val;
				}

				var dynamicOversold = min + (max - min) * (prm.OversoldLevel / 100f);
				var dynamicOverbought = min + (max - min) * (prm.OverboughtLevel / 100f);

				float dynamicRsi;
				if (rsi <= dynamicOversold)
				{
					dynamicRsi = 0f;
				}
				else if (rsi >= dynamicOverbought)
				{
					dynamicRsi = 100f;
				}
				else
				{
					dynamicRsi = (rsi - dynamicOversold) / (dynamicOverbought - dynamicOversold) * 100f;
				}

				flatResults[resIndex] = new() { Time = candle.Time, Value = dynamicRsi, IsFormed = prevFormed };
			}

			prevFormed = curFormed;
			prevValue = value;
		}
	}
}

namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Constance Brown Composite Index calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuConstanceBrownCompositeIndexParams"/> struct.
/// </remarks>
/// <param name="rsiLength">RSI length.</param>
/// <param name="stochasticKLength">Stochastic %K length.</param>
/// <param name="stochasticDLength">Stochastic %D length.</param>
/// <param name="priceType">Price type used for RSI part.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuConstanceBrownCompositeIndexParams(int rsiLength, int stochasticKLength, int stochasticDLength, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// RSI window length.
	/// </summary>
	public int RsiLength = rsiLength;

	/// <summary>
	/// Stochastic %K length.
	/// </summary>
	public int StochasticKLength = stochasticKLength;

	/// <summary>
	/// Stochastic %D length.
	/// </summary>
	public int StochasticDLength = stochasticDLength;

	/// <summary>
	/// Price type to extract for RSI part.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is ConstanceBrownCompositeIndex cbci)
		{
			Unsafe.AsRef(in this).RsiLength = cbci.Length;
			Unsafe.AsRef(in this).StochasticKLength = cbci.StochasticKPeriod;
			Unsafe.AsRef(in this).StochasticDLength = cbci.StochasticDPeriod;
		}
	}
}

/// <summary>
/// GPU result for Constance Brown Composite Index calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuConstanceBrownCompositeIndexResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTimeOffset.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// RSI value.
	/// </summary>
	public float Rsi;

	/// <summary>
	/// Stochastic %K value.
	/// </summary>
	public float StochK;

	/// <summary>
	/// Stochastic %D value.
	/// </summary>
	public float StochD;

	/// <summary>
	/// Composite index line value.
	/// </summary>
	public float Composite;

	/// <summary>
	/// Is indicator formed (byte to be GPU-friendly).
	/// </summary>
	public byte IsFormed;

	readonly long IGpuIndicatorResult.Time => Time;
	readonly byte IGpuIndicatorResult.IsFormed => IsFormed;

	/// <inheritdoc />
	public readonly IIndicatorValue ToValue(IIndicator indicator)
	{
		var time = this.GetTime();
		var isFormed = this.GetIsFormed();
		var cbci = (ConstanceBrownCompositeIndex)indicator;

		if (Rsi.IsNaN() || StochK.IsNaN() || StochD.IsNaN() || Composite.IsNaN())
		{
			return new ConstanceBrownCompositeIndexValue(cbci, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		var result = new ConstanceBrownCompositeIndexValue(cbci, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		result.Add(cbci.Rsi, new DecimalIndicatorValue(cbci.Rsi, (decimal)Rsi, time)
		{
			IsFinal = true,
			IsFormed = true,
		});

		var stochInd = cbci.Stoch;
		var stochValue = new StochasticOscillatorValue(stochInd, time)
		{
			IsFinal = true,
			IsFormed = true,
		};

		stochValue.Add(stochInd.K, new DecimalIndicatorValue(stochInd.K, (decimal)StochK, time)
		{
			IsFinal = true,
			IsFormed = true,
		});

		stochValue.Add(stochInd.D, new DecimalIndicatorValue(stochInd.D, (decimal)StochD, time)
		{
			IsFinal = true,
			IsFormed = true,
		});

		result.Add(stochInd, stochValue);

		var compositeLine = cbci.CompositeIndexLine;
		result.Add(compositeLine, new DecimalIndicatorValue(compositeLine, (decimal)Composite, time)
		{
			IsFinal = true,
			IsFormed = true,
		});

		return result;
	}
}

/// <summary>
/// GPU calculator for Constance Brown Composite Index.
/// </summary>
public class GpuConstanceBrownCompositeIndexCalculator : GpuIndicatorCalculatorBase<ConstanceBrownCompositeIndex, GpuConstanceBrownCompositeIndexParams, GpuConstanceBrownCompositeIndexResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuConstanceBrownCompositeIndexResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuConstanceBrownCompositeIndexParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuConstanceBrownCompositeIndexCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuConstanceBrownCompositeIndexCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuConstanceBrownCompositeIndexResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuConstanceBrownCompositeIndexParams>>(ConstanceBrownCompositeIndexKernel);
	}

	/// <inheritdoc />
	public override GpuConstanceBrownCompositeIndexResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuConstanceBrownCompositeIndexParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuConstanceBrownCompositeIndexResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuConstanceBrownCompositeIndexResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuConstanceBrownCompositeIndexResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuConstanceBrownCompositeIndexResult[len];
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

	private static void ConstanceBrownCompositeIndexKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuConstanceBrownCompositeIndexResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuConstanceBrownCompositeIndexParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var rsiLength = prm.RsiLength <= 0 ? 1 : prm.RsiLength;
		var kLength = prm.StochasticKLength <= 0 ? 1 : prm.StochasticKLength;
		var dLength = prm.StochasticDLength <= 0 ? 1 : prm.StochasticDLength;
		var priceType = (Level1Fields)prm.PriceType;

		var firstCandle = flatCandles[offset];
		var prevPrice = ExtractPrice(firstCandle, priceType);

		var gainSum = 0f;
		var lossSum = 0f;
		var avgGain = 0f;
		var avgLoss = 0f;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var resIndex = paramIdx * flatCandles.Length + (offset + i);

			var result = new GpuConstanceBrownCompositeIndexResult
			{
				Time = candle.Time,
				Rsi = float.NaN,
				StochK = float.NaN,
				StochD = float.NaN,
				Composite = float.NaN,
				IsFormed = 0,
			};

			var price = ExtractPrice(candle, priceType);
			float rsiValue = float.NaN;
			if (i > 0)
			{
				var delta = price - prevPrice;
				var gain = delta > 0f ? delta : 0f;
				var loss = delta < 0f ? -delta : 0f;

				if (i <= rsiLength)
				{
					gainSum += gain;
					lossSum += loss;

					if (i == rsiLength)
					{
						avgGain = gainSum / rsiLength;
						avgLoss = lossSum / rsiLength;
					}
				}
				else
				{
					avgGain = (avgGain * (rsiLength - 1) + gain) / rsiLength;
					avgLoss = (avgLoss * (rsiLength - 1) + loss) / rsiLength;
				}

				if (i >= rsiLength)
				{
					if (avgLoss == 0f)
					{
						rsiValue = 100f;
					}
					else
					{
						var rs = avgGain / avgLoss;
						rsiValue = rs == 1f ? 0f : 100f - 100f / (1f + rs);
					}
				}
			}

			var stochK = float.NaN;
			if (i >= kLength - 1)
			{
				stochK = ComputeStochK(flatCandles, offset, i, kLength);
			}

			var stochD = float.NaN;
			if (!stochK.IsNaN() && i >= kLength + dLength - 2)
			{
				var sumK = 0f;
				for (var j = i - dLength + 1; j <= i; j++)
				{
					sumK += ComputeStochK(flatCandles, offset, j, kLength);
				}
				stochD = sumK / dLength;
			}

			var composite = float.NaN;
			if (!rsiValue.IsNaN() && !stochK.IsNaN() && !stochD.IsNaN())
			{
				composite = (rsiValue + stochK + stochD) / 3f;
				result.IsFormed = 1;
			}

			result.Rsi = rsiValue;
			result.StochK = stochK;
			result.StochD = stochD;
			result.Composite = composite;
			flatResults[resIndex] = result;

			prevPrice = price;
		}
	}

	private static float ComputeStochK(ArrayView<GpuCandle> flatCandles, int offset, int index, int kLength)
	{
		var start = index - kLength + 1;
		if (start < 0)
			start = 0;

		var highestHigh = float.MinValue;
		var lowestLow = float.MaxValue;

		for (var i = start; i <= index; i++)
		{
			var c = flatCandles[offset + i];
			if (c.High > highestHigh)
				highestHigh = c.High;
			if (c.Low < lowestLow)
				lowestLow = c.Low;
		}

		var diff = highestHigh - lowestLow;
		if (diff == 0f)
			return 0f;

		var close = flatCandles[offset + index].Close;
		return 100f * (close - lowestLow) / diff;
	}
}

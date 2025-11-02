namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Constance Brown Composite Index calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuConstanceBrownCompositeIndexParams"/> struct.
/// </remarks>
/// <param name="rsiLength">RSI length.</param>
/// <param name="rocLength">ROC length over RSI.</param>
/// <param name="shortRsiLength">Short RSI length.</param>
/// <param name="momentumLength">Momentum SMA length over short RSI.</param>
/// <param name="fastSmaLength">Fast SMA length over composite.</param>
/// <param name="slowSmaLength">Slow SMA length over composite.</param>
/// <param name="priceType">Price type used for RSI part.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuConstanceBrownCompositeIndexParams(
	int rsiLength,
	int rocLength,
	int shortRsiLength,
	int momentumLength,
	int fastSmaLength,
	int slowSmaLength,
	byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength = rsiLength;

	/// <summary>
	/// ROC length over RSI.
	/// </summary>
	public int RocLength = rocLength;

	/// <summary>
	/// Short RSI length.
	/// </summary>
	public int ShortRsiLength = shortRsiLength;

	/// <summary>
	/// Momentum SMA length over short RSI.
	/// </summary>
	public int MomentumLength = momentumLength;

	/// <summary>
	/// Fast SMA length over composite.
	/// </summary>
	public int FastSmaLength = fastSmaLength;

	/// <summary>
	/// Slow SMA length over composite.
	/// </summary>
	public int SlowSmaLength = slowSmaLength;

	/// <summary>
	/// Price type to extract for RSI part.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is ConstanceBrownCompositeIndex cbci)
		{
			Unsafe.AsRef(in this).RsiLength = cbci.RsiLength;
			Unsafe.AsRef(in this).RocLength = cbci.RocLength;
			Unsafe.AsRef(in this).ShortRsiLength = cbci.ShortRsiLength;
			Unsafe.AsRef(in this).MomentumLength = cbci.MomentumLength;
			Unsafe.AsRef(in this).FastSmaLength = cbci.FastSmaLength;
			Unsafe.AsRef(in this).SlowSmaLength = cbci.SlowSmaLength;
		}

		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);
	}
}

/// <summary>
/// GPU result for Constance Brown Composite Index calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuConstanceBrownCompositeIndexResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Composite index line value.
	/// </summary>
	public float Composite;

	/// <summary>
	/// Fast SMA value.
	/// </summary>
	public float Fast;

	/// <summary>
	/// Slow SMA value.
	/// </summary>
	public float Slow;

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

		if (Composite.IsNaN() || Fast.IsNaN() || Slow.IsNaN())
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

		var ci = cbci.CompositeIndexLine;
		result.Add(ci, new DecimalIndicatorValue(ci, (decimal)Composite, time) { IsFinal = true, IsFormed = true });

		result.Add(cbci.FastSma, new DecimalIndicatorValue(cbci.FastSma, (decimal)Fast, time) { IsFinal = true, IsFormed = true });
		result.Add(cbci.SlowSma, new DecimalIndicatorValue(cbci.SlowSma, (decimal)Slow, time) { IsFinal = true, IsFormed = true });

		return result;
	}
}

/// <summary>
/// GPU calculator for Constance Brown Composite Index.
/// </summary>
public class GpuConstanceBrownCompositeIndexCalculator : GpuIndicatorCalculatorBase<ConstanceBrownCompositeIndex, GpuConstanceBrownCompositeIndexParams, GpuConstanceBrownCompositeIndexResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuConstanceBrownCompositeIndexResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuConstanceBrownCompositeIndexParams>, ArrayView<float>, int, ArrayView<float>, int, ArrayView<float>, int, ArrayView<float>, int> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuConstanceBrownCompositeIndexCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuConstanceBrownCompositeIndexCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuConstanceBrownCompositeIndexResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuConstanceBrownCompositeIndexParams>, ArrayView<float>, int, ArrayView<float>, int, ArrayView<float>, int, ArrayView<float>, int>(ConstanceBrownCompositeIndexKernel);
	}

	/// <inheritdoc />
	public override GpuConstanceBrownCompositeIndexResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuConstanceBrownCompositeIndexParams[] parameters)
	{
		ArgumentNullException.ThrowIfNull(candlesSeries);
		ArgumentNullException.ThrowIfNull(parameters);
		if (candlesSeries.Length == 0) throw new ArgumentOutOfRangeException(nameof(candlesSeries));
		if (parameters.Length == 0) throw new ArgumentOutOfRangeException(nameof(parameters));

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

		int maxRoc = 1, maxMom = 1, maxFast = 1, maxSlow = 1;
		for (var i = 0; i < parameters.Length; i++)
		{
			maxRoc = Math.Max(maxRoc, Math.Max(1, parameters[i].RocLength));
			maxMom = Math.Max(maxMom, Math.Max(1, parameters[i].MomentumLength));
			maxFast = Math.Max(maxFast, Math.Max(1, parameters[i].FastSmaLength));
			maxSlow = Math.Max(maxSlow, Math.Max(1, parameters[i].SlowSmaLength));
		}

		using var inputBuffer = Accelerator.Allocate1D(flatCandles);
		using var offsetsBuffer = Accelerator.Allocate1D(seriesOffsets);
		using var lengthsBuffer = Accelerator.Allocate1D(seriesLengths);
		using var paramsBuffer = Accelerator.Allocate1D(parameters);
		using var rocWindowBuffer = Accelerator.Allocate1D<float>(parameters.Length * seriesCount * maxRoc);
		using var momWindowBuffer = Accelerator.Allocate1D<float>(parameters.Length * seriesCount * maxMom);
		using var fastWindowBuffer = Accelerator.Allocate1D<float>(parameters.Length * seriesCount * maxFast);
		using var slowWindowBuffer = Accelerator.Allocate1D<float>(parameters.Length * seriesCount * maxSlow);
		using var outputBuffer = Accelerator.Allocate1D<GpuConstanceBrownCompositeIndexResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);

		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View,
			rocWindowBuffer.View, maxRoc,
			momWindowBuffer.View, maxMom,
			fastWindowBuffer.View, maxFast,
			slowWindowBuffer.View, maxSlow);
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
		ArrayView<GpuConstanceBrownCompositeIndexParams> parameters,
		ArrayView<float> rocWindowStorage, int maxRoc,
		ArrayView<float> momWindowStorage, int maxMom,
		ArrayView<float> fastWindowStorage, int maxFast,
		ArrayView<float> slowWindowStorage, int maxSlow)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var total = flatCandles.Length;
		var resBase = paramIdx * total;
		var prm = parameters[paramIdx];

		int rsiLen = prm.RsiLength <= 0 ? 1 : prm.RsiLength;
		int rocLen = prm.RocLength <= 0 ? 1 : prm.RocLength;
		int shortLen = prm.ShortRsiLength <= 0 ? 1 : prm.ShortRsiLength;
		int momLen = prm.MomentumLength <= 0 ? 1 : prm.MomentumLength;
		int fastLen = prm.FastSmaLength <= 0 ? 1 : prm.FastSmaLength;
		int slowLen = prm.SlowSmaLength <= 0 ? 1 : prm.SlowSmaLength;
		var priceType = (Level1Fields)prm.PriceType;

		// RSI state (Wilder)
		float prevPrice = ExtractPrice(flatCandles[offset], priceType);
		float gainSum = 0f, lossSum = 0f;
		float avgGain = 0f, avgLoss = 0f;

		// Short RSI state
		float prevPriceShort = prevPrice;
		float gainSumS = 0f, lossSumS = 0f;
		float avgGainS = 0f, avgLossS = 0f;

		// ROC window over RSI
		var rocBase = (paramIdx * lengths.Length + seriesIdx) * maxRoc;
		var rocWindow = rocWindowStorage.SubView(rocBase, maxRoc);
		int rocCount = 0, rocHead = 0;

		// Momentum SMA window over Short RSI
		var momBase = (paramIdx * lengths.Length + seriesIdx) * maxMom;
		var momWindow = momWindowStorage.SubView(momBase, maxMom);
		int momCount = 0, momHead = 0;
		float momSum = 0f;

		// Fast/Slow SMA over Composite
		var fastBase = (paramIdx * lengths.Length + seriesIdx) * maxFast;
		var fastWindow = fastWindowStorage.SubView(fastBase, maxFast);
		int fastCount = 0, fastHead = 0;
		float fastSum = 0f;

		var slowBase = (paramIdx * lengths.Length + seriesIdx) * maxSlow;
		var slowWindow = slowWindowStorage.SubView(slowBase, maxSlow);
		int slowCount = 0, slowHead = 0;
		float slowSum = 0f;

		for (int i = 0; i < len; i++)
		{
			var c = flatCandles[offset + i];
			var resIndex = resBase + (offset + i);

			var outVal = new GpuConstanceBrownCompositeIndexResult
			{
				Time = c.Time,
				Composite = float.NaN,
				Fast = float.NaN,
				Slow = float.NaN,
				IsFormed = 0,
			};

			var price = ExtractPrice(c, priceType);

			// RSI
			float rsi = float.NaN;
			if (i > 0)
			{
				var delta = price - prevPrice;
				var gain = delta > 0f ? delta : 0f;
				var loss = delta < 0f ? -delta : 0f;

				if (i <= rsiLen)
				{
					gainSum += gain;
					lossSum += loss;
					if (i == rsiLen)
					{
						avgGain = gainSum / rsiLen;
						avgLoss = lossSum / rsiLen;
					}
				}
				else
				{
					avgGain = (avgGain * (rsiLen - 1) + gain) / rsiLen;
					avgLoss = (avgLoss * (rsiLen - 1) + loss) / rsiLen;
				}

				if (i >= rsiLen)
					rsi = avgLoss == 0f ? 100f : (100f - 100f / (1f + (avgGain / avgLoss)));
			}

			// Short RSI
			float shortRsi = float.NaN;
			if (i > 0)
			{
				var deltaS = price - prevPriceShort;
				var gainS = deltaS > 0f ? deltaS : 0f;
				var lossS = deltaS < 0f ? -deltaS : 0f;

				if (i <= shortLen)
				{
					gainSumS += gainS;
					lossSumS += lossS;
					if (i == shortLen)
					{
						avgGainS = gainSumS / shortLen;
						avgLossS = lossSumS / shortLen;
					}
				}
				else
				{
					avgGainS = (avgGainS * (shortLen - 1) + gainS) / shortLen;
					avgLossS = (avgLossS * (shortLen - 1) + lossS) / shortLen;
				}

				if (i >= shortLen)
					shortRsi = avgLossS == 0f ? 100f : (100f - 100f / (1f + (avgGainS / avgLossS)));
			}

			// ROC over RSI
			float rsiRoc = float.NaN;
			if (!float.IsNaN(rsi))
			{
				if (rocCount < rocLen)
				{
					if (rocCount == rocLen - 1)
					{
						// can compute on next iteration when buffer is full
					}
					rocWindow[rocCount] = rsi;
					rocCount++;
				}
				else
				{
					var old = rocWindow[rocHead];
					rsiRoc = rsi - old;
					rocWindow[rocHead] = rsi;
					rocHead++;
					if (rocHead == rocLen) rocHead = 0;
				}
			}

			// Momentum SMA over short RSI
			float rsiMom = float.NaN;
			if (!float.IsNaN(shortRsi))
			{
				if (momCount < momLen)
				{
					momWindow[momCount] = shortRsi;
					momSum += shortRsi;
					momCount++;
				}
				else
				{
					var oldM = momWindow[momHead];
					momSum = momSum - oldM + shortRsi;
					momWindow[momHead] = shortRsi;
					momHead++;
					if (momHead == momLen) momHead = 0;
				}

				if (momCount >= momLen)
					rsiMom = momSum / momLen;
			}

			// Composite
			float composite = float.NaN;
			if (!float.IsNaN(rsiRoc) && !float.IsNaN(rsiMom))
				composite = rsiRoc + rsiMom;

			// Fast SMA over composite
			float fast = float.NaN;
			if (!float.IsNaN(composite))
			{
				if (fastCount < fastLen)
				{
					fastWindow[fastCount] = composite;
					fastSum += composite;
					fastCount++;
				}
				else
				{
					var oldF = fastWindow[fastHead];
					fastSum = fastSum - oldF + composite;
					fastWindow[fastHead] = composite;
					fastHead++;
					if (fastHead == fastLen) fastHead = 0;
				}

				if (fastCount >= fastLen)
					fast = fastSum / fastLen;
			}

			// Slow SMA over composite
			float slow = float.NaN;
			if (!float.IsNaN(composite))
			{
				if (slowCount < slowLen)
				{
					slowWindow[slowCount] = composite;
					slowSum += composite;
					slowCount++;
				}
				else
				{
					var oldS = slowWindow[slowHead];
					slowSum = slowSum - oldS + composite;
					slowWindow[slowHead] = composite;
					slowHead++;
					if (slowHead == slowLen) slowHead = 0;
				}

				if (slowCount >= slowLen)
					slow = slowSum / slowLen;
			}

			outVal.Composite = composite;
			outVal.Fast = fast;
			outVal.Slow = slow;
			outVal.IsFormed = (!float.IsNaN(fast) && !float.IsNaN(slow)) ? (byte)1 : (byte)0;
			flatResults[resIndex] = outVal;

			prevPrice = price;
			prevPriceShort = price;
		}
	}
}

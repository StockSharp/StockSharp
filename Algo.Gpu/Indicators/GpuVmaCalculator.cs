namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Variable Moving Average (VMA) calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuVmaParams"/> struct.
/// </remarks>
/// <param name="length">Indicator length.</param>
/// <param name="priceType">Price type.</param>
/// <param name="volatilityIndex">Volatility index factor.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuVmaParams(int length, byte priceType, float volatilityIndex) : IGpuIndicatorParams
{
	/// <summary>
	/// Indicator length.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <summary>
	/// Volatility index factor controlling smoothing.
	/// </summary>
	public float VolatilityIndex = volatilityIndex;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is VariableMovingAverage vma)
		{
			Unsafe.AsRef(in this).Length = vma.Length;
			Unsafe.AsRef(in this).VolatilityIndex = (float)vma.VolatilityIndex;
		}
	}
}

/// <summary>
/// GPU calculator for Variable Moving Average (VMA).
/// </summary>
public class GpuVmaCalculator : GpuIndicatorCalculatorBase<VariableMovingAverage, GpuVmaParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuVmaParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuVmaCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuVmaCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuVmaParams>>(VmaParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuVmaParams[] parameters)
	{
		ArgumentNullException.ThrowIfNull(candlesSeries);
		ArgumentNullException.ThrowIfNull(parameters);

		if (candlesSeries.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(candlesSeries));
		if (parameters.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(parameters));

		var seriesCount = candlesSeries.Length;

		// Flatten input
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

		// Re-split [series][param][bar]
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
	/// ILGPU kernel: Variable Moving Average computation for multiple series and parameter sets.
	/// One thread handles one (parameter, series) pair and iterates bars sequentially.
	/// Matches CPU algorithm: StdDev starts from bar 1, VMA buffer tracks pushed prices.
	/// </summary>
	private static void VmaParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuVmaParams> parameters)
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
		var totalCandles = flatCandles.Length;

		float prevVma = 0f;

		// VMA buffer tracking (sum and count for avgPrice computation)
		float bufSum = 0f;
		int bufCount = 0;
		int pushCount = 0; // total number of pushes to buffer

		// StdDev processing count (starts from bar 1)
		int stdDevCount = 0;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var price = ExtractPrice(candle, priceType);
			var resIndex = paramIdx * totalCandles + globalIdx;

			if (i == 0)
			{
				// Bar 0: initialization (matches CPU !_isInitialized path)
				prevVma = price;
				bufSum = price;
				bufCount = 1;
				pushCount = 1;

				flatResults[resIndex] = new GpuIndicatorResult
				{
					Time = candle.Time,
					Value = price,
					IsFormed = 0,
				};
				continue;
			}

			// Bars 1+: process StdDev (count values for SMA/StdDev formation)
			stdDevCount++;

			if (stdDevCount < L)
			{
				// StdDev not yet formed, return previous VMA value
				flatResults[resIndex] = new GpuIndicatorResult
				{
					Time = candle.Time,
					Value = prevVma,
					IsFormed = 0,
				};
				continue;
			}

			// StdDev is formed (stdDevCount >= L)
			// Compute StdDev over the last L values that StdDev has seen (bars i-L+1 to i)
			// StdDev processes bars 1..i, so last L values are bars i-L+1..i
			float smaSum = 0f;
			for (var j = 0; j < L; j++)
			{
				var p = ExtractPrice(flatCandles[offset + i - j], priceType);
				smaSum += p;
			}
			var sma = smaSum / L;

			float sqSum = 0f;
			for (var j = 0; j < L; j++)
			{
				var p = ExtractPrice(flatCandles[offset + i - j], priceType);
				var diff = p - sma;
				sqSum += diff * diff;
			}
			var stdDev = MathF.Sqrt(sqSum / L);

			// avgPrice from VMA's buffer (not from StdDev's window)
			var avgPrice = bufSum / bufCount;

			// Volatility index
			var vi = avgPrice != 0f ? MathF.Abs(stdDev / avgPrice) : 0f;

			// Smoothing constant
			var smoothingConstant = 2f / (L * (1f + prm.VolatilityIndex * vi) + 1f);

			// VMA EMA-like step
			var curValue = (price - prevVma) * smoothingConstant + prevVma;

			// Push price to VMA buffer (circular with capacity L)
			pushCount++;
			if (bufCount < L)
			{
				bufSum += price;
				bufCount++;
			}
			else
			{
				// Evict oldest value from buffer
				// pushCount-1 is current push index (0-based)
				// Evicted push index = pushCount - 1 - L
				var evictedPushIdx = pushCount - 1 - L;
				float evictedPrice;
				if (evictedPushIdx == 0)
				{
					// First push was bar 0
					evictedPrice = ExtractPrice(flatCandles[offset], priceType);
				}
				else
				{
					// Push k (k>=1) was at bar L + k - 1
					var evictedBar = L + evictedPushIdx - 1;
					evictedPrice = ExtractPrice(flatCandles[offset + evictedBar], priceType);
				}
				bufSum = bufSum - evictedPrice + price;
			}

			prevVma = curValue;

			// IsFormed: VMA is formed when StdDev is formed.
			// StdDev is formed when its SMA has Length values.
			// StdDev starts receiving from bar 1, so at bar L (stdDevCount == L), StdDev is formed.
			byte isFormed = (byte)(stdDevCount >= L ? 1 : 0);

			flatResults[resIndex] = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = curValue,
				IsFormed = isFormed,
			};
		}
	}
}

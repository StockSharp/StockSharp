namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Psychological Line calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuPsychologicalLineParams"/> struct.
/// </remarks>
/// <param name="length">Indicator length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuPsychologicalLineParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Indicator window length.
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

		if (indicator is PsychologicalLine psychologicalLine)
		{
			Unsafe.AsRef(in this).Length = psychologicalLine.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Psychological Line indicator.
/// </summary>
public class GpuPsychologicalLineCalculator : GpuIndicatorCalculatorBase<PsychologicalLine, GpuPsychologicalLineParams, GpuIndicatorResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuPsychologicalLineParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuPsychologicalLineCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuPsychologicalLineCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuPsychologicalLineParams>>(PsychologicalLineParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuPsychologicalLineParams[] parameters)
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
		var maxLen = 0;
		var offset = 0;
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			if (len > 0)
			{
				Array.Copy(candlesSeries[s], 0, flatCandles, offset, len);
				offset += len;
				if (len > maxLen)
					maxLen = len;
			}
		}

		using var inputBuffer = Accelerator.Allocate1D(flatCandles);
		using var offsetsBuffer = Accelerator.Allocate1D(seriesOffsets);
		using var lengthsBuffer = Accelerator.Allocate1D(seriesLengths);
		using var paramsBuffer = Accelerator.Allocate1D(parameters);
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var extent = new Index3D(parameters.Length, seriesCount, maxLen);
		_paramsSeriesKernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
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
					var resIdx = p * totalSize + globalIdx;
					arr[i] = flatResults[resIdx];
				}
				result[s][p] = arr;
			}
		}

		return result;
	}

	/// <summary>
	/// ILGPU kernel: Psychological Line computation for multiple series and parameter sets. Results are stored as [param][globalIdx].
	/// Uses the same incremental logic as the CPU indicator to match its behavior exactly.
	/// </summary>
	private static void PsychologicalLineParamsSeriesKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuPsychologicalLineParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;
		var candleIdx = index.Z;

		var len = lengths[seriesIdx];
		if (candleIdx >= len)
			return;

		var offset = offsets[seriesIdx];
		var globalIdx = offset + candleIdx;

		var candle = flatCandles[globalIdx];
		var resIndex = paramIdx * flatCandles.Length + globalIdx;
		var result = new GpuIndicatorResult { Time = candle.Time, Value = float.NaN, IsFormed = 0 };

		var prm = parameters[paramIdx];
		var L = prm.Length;

		if (L <= 0)
		{
			flatResults[resIndex] = result;
			return;
		}

		// Not enough bars to be formed (need L bars in buffer = L bars total)
		if (candleIdx < L - 1)
		{
			flatResults[resIndex] = result;
			return;
		}

		// Replicate CPU incremental logic by simulating the buffer state.
		// The CPU tracks _upCount incrementally:
		// - When buffer full and pushing new price: if Buffer[0] < Buffer[^1], _upCount--
		// - If price > Buffer[^1], _upCount++
		// We simulate this by iterating from the start of the window.
		var priceType = (Level1Fields)prm.PriceType;
		var upCount = 0;

		// Simulate the CPU's incremental processing for bars in the window.
		// The window at candleIdx contains bars [candleIdx-L+1 .. candleIdx].
		// We need to replay the CPU's incremental logic for the last L bars.
		// The CPU buffer fills up over the first L bars, then slides.

		// We need to simulate how the CPU processes bars from the beginning up to candleIdx.
		// Start from bar max(0, candleIdx - L + 1) and process L bars to build the state.
		// Actually, we need to replay from the very first bar to get the correct _upCount
		// due to the CPU's specific removal logic (Buffer[0] < Buffer[^1]).

		// The CPU's removal checks Buffer[0] (oldest) < Buffer[^1] (newest before push).
		// We need to track a circular buffer of L prices.

		// Since ILGPU doesn't support arrays, simulate using lookback into flatCandles.
		// Process bars from the first bar up to candleIdx, maintaining upCount.

		// Replay the CPU's incremental logic from the start of the series.
		// Track buffer via indices into flatCandles.
		var bufferCount = 0;
		var bufferFirstIdx = offset; // global index of oldest in buffer
		var bufferLastIdx = offset;  // global index of newest in buffer

		for (var i = offset; i <= globalIdx; i++)
		{
			var price = ExtractPrice(flatCandles[i], priceType);

			if (bufferCount == L)
			{
				// Buffer is full, about to push out oldest.
				// CPU checks: if (Buffer[0] < Buffer[^1]) _upCount--
				var oldestPrice = ExtractPrice(flatCandles[bufferFirstIdx], priceType);
				var newestPrice = ExtractPrice(flatCandles[bufferLastIdx], priceType);
				if (oldestPrice < newestPrice)
					upCount--;
				bufferFirstIdx++;
			}

			// CPU checks: if (Buffer.Count > 0 && price > Buffer[^1]) _upCount++
			if (bufferCount > 0)
			{
				var lastPrice = ExtractPrice(flatCandles[bufferLastIdx], priceType);
				if (price > lastPrice)
					upCount++;
			}

			// Buffer.PushBack(price)
			bufferLastIdx = i;
			if (bufferCount < L)
				bufferCount++;
		}

		result.Value = (float)upCount / L;
		result.IsFormed = 1;

		flatResults[resIndex] = result;
	}
}

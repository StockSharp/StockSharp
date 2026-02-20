namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Momentum of Moving Average calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuMomentumOfMovingAverageParams"/> struct.
/// </remarks>
/// <param name="length">Moving average length.</param>
/// <param name="momentumPeriod">Momentum period.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuMomentumOfMovingAverageParams(int length, int momentumPeriod, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Moving average period length.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// Momentum lookback period.
	/// </summary>
	public int MomentumPeriod = momentumPeriod;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is MomentumOfMovingAverage moma)
		{
			Unsafe.AsRef(in this).Length = moma.Length;
			Unsafe.AsRef(in this).MomentumPeriod = moma.MomentumPeriod;
		}
	}
}

/// <summary>
/// GPU calculator for Momentum of Moving Average (MOMA).
/// </summary>
public class GpuMomentumOfMovingAverageCalculator : GpuIndicatorCalculatorBase<MomentumOfMovingAverage, GpuMomentumOfMovingAverageParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<float>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMomentumOfMovingAverageParams>, ArrayView<int>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuMomentumOfMovingAverageCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuMomentumOfMovingAverageCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<float>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMomentumOfMovingAverageParams>, ArrayView<int>>(MomentumParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuMomentumOfMovingAverageParams[] parameters)
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

		// Compute max length for circular buffer allocation
		var maxLength = 1;
		for (var p = 0; p < parameters.Length; p++)
		{
			if (parameters[p].Length > maxLength)
				maxLength = parameters[p].Length;
		}

		using var inputBuffer = Accelerator.Allocate1D(flatCandles);
		using var offsetsBuffer = Accelerator.Allocate1D(seriesOffsets);
		using var lengthsBuffer = Accelerator.Allocate1D(seriesLengths);
		using var paramsBuffer = Accelerator.Allocate1D(parameters);
		// Circular buffer: one buffer of size maxLength per (param, series) combination
		using var circBuf = Accelerator.Allocate1D<float>(maxLength * parameters.Length * seriesCount);
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var maxLengthArr = new int[] { maxLength };
		using var maxLenBuffer = Accelerator.Allocate1D(maxLengthArr);

		var extent = new Index2D(parameters.Length, seriesCount);
		_paramsSeriesKernel(extent, inputBuffer.View, outputBuffer.View, circBuf.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View, maxLenBuffer.View);
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
	/// ILGPU kernel: Momentum of Moving Average computation for multiple series and multiple parameter sets.
	/// Sequential loop over candles to simulate the CPU circular buffer behavior exactly.
	/// Results are stored as [param][globalIdx].
	/// </summary>
	private static void MomentumParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<float> circBuf,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuMomentumOfMovingAverageParams> parameters,
		ArrayView<int> maxLenArr)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var offset = offsets[seriesIdx];
		var resIndexBase = paramIdx * flatCandles.Length;

		var prm = parameters[paramIdx];
		var maLength = prm.Length;

		if (maLength <= 0)
			maLength = 1;

		var priceType = (Level1Fields)prm.PriceType;

		// Circular buffer in global memory
		var maxLen = maxLenArr[0];
		var numSeries = lengths.Length;
		var bufBase = (paramIdx * numSeries + seriesIdx) * maxLen;
		var bufCount = 0;
		var bufHead = 0; // index of oldest element in circular buffer
		var bufSum = 0f;

		for (var candleIdx = 0; candleIdx < len; candleIdx++)
		{
			var globalIdx = offset + candleIdx;
			var candle = flatCandles[globalIdx];
			var resIndex = resIndexBase + globalIdx;

			flatResults[resIndex] = new()
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0,
			};

			var price = ExtractPrice(candle, priceType);

			// SMA pushes raw price to circular buffer (same as CPU base.OnProcessDecimal)
			if (bufCount >= maLength)
			{
				bufSum -= circBuf[bufBase + bufHead];
				bufHead = (bufHead + 1) % maLength;
			}
			else
			{
				bufCount++;
			}

			var tail = (bufHead + bufCount - 1) % maLength;
			circBuf[bufBase + tail] = price;
			bufSum += price;

			// SMA value = Buffer.Sum / Length
			var ma = bufSum / maLength;

			// IsFormed = Buffer.Count >= Length
			if (bufCount < maLength)
				continue;

			// MOMA: push MA value to the same buffer, then compute momentum from Buffer[0]
			// (CPU does Buffer.PushBack(ma) then reads Buffer[0])
			bufSum -= circBuf[bufBase + bufHead];
			bufHead = (bufHead + 1) % maLength;

			tail = (bufHead + bufCount - 1) % maLength;
			circBuf[bufBase + tail] = ma;
			bufSum += ma;

			var firstBuffer = circBuf[bufBase + bufHead];

			if (firstBuffer == 0f)
			{
				flatResults[resIndex] = new()
				{
					Time = candle.Time,
					Value = float.NaN,
					IsFormed = 1,
				};
				continue;
			}

			var momentum = (ma - firstBuffer) / firstBuffer * 100f;

			flatResults[resIndex] = new()
			{
				Time = candle.Time,
				Value = momentum,
				IsFormed = 1,
			};
		}
	}
}

namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Moving Average Ribbon calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuMovingAverageRibbonParams"/> struct.
/// </remarks>
/// <param name="shortPeriod">Shortest moving average length.</param>
/// <param name="longPeriod">Longest moving average length.</param>
/// <param name="ribbonCount">Number of moving averages inside the ribbon.</param>
/// <param name="priceType">Price type to use for averages.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuMovingAverageRibbonParams(int shortPeriod, int longPeriod, int ribbonCount, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Shortest moving average length.
	/// </summary>
	public int ShortPeriod = shortPeriod;

	/// <summary>
	/// Longest moving average length.
	/// </summary>
	public int LongPeriod = longPeriod;

	/// <summary>
	/// Number of moving averages inside the ribbon.
	/// </summary>
	public int RibbonCount = ribbonCount;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is MovingAverageRibbon ribbon)
		{
			Unsafe.AsRef(in this).ShortPeriod = ribbon.ShortPeriod;
			Unsafe.AsRef(in this).LongPeriod = ribbon.LongPeriod;
			Unsafe.AsRef(in this).RibbonCount = ribbon.RibbonCount;
		}
	}
}

/// <summary>
/// GPU result for Moving Average Ribbon.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct GpuMovingAverageRibbonResult : IGpuIndicatorResult
{
	/// <summary>
	/// Maximum number of averages stored per result.
	/// </summary>
	public const int MaxRibbonCount = 32;

	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Ribbon average values.
	/// </summary>
	[CLSCompliant(false)]
	public fixed float Averages[MaxRibbonCount];

	/// <summary>
	/// Actual number of averages stored for the bar.
	/// </summary>
	public int RibbonCount;

	/// <summary>
	/// Is indicator formed (byte to be GPU-friendly).
	/// </summary>
	public byte IsFormed;

	readonly long IGpuIndicatorResult.Time => Time;

	readonly byte IGpuIndicatorResult.IsFormed => IsFormed;

	/// <summary>
	/// Store average value at the specified index.
	/// </summary>
	/// <param name="index">Average index.</param>
	/// <param name="value">Average value.</param>
	public void SetAverage(int index, float value)
	{
		if ((uint)index >= MaxRibbonCount)
		{
			return;
		}

		fixed (float* ptr = Averages)
		{
			ptr[index] = value;
		}
	}

	/// <summary>
	/// Get stored average value at the specified index.
	/// </summary>
	/// <param name="index">Average index.</param>
	/// <returns>Average value.</returns>
	public readonly float GetAverage(int index)
	{
		fixed (float* ptr = Averages)
		{
			return ptr[index];
		}
	}

	/// <inheritdoc />
	public readonly IIndicatorValue ToValue(IIndicator indicator)
	{
		var ribbon = (MovingAverageRibbon)indicator;
		var time = this.GetTime();
		var isFormed = this.GetIsFormed();

		var value = new MovingAverageRibbonValue(ribbon, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		var innerIndicators = ribbon.InnerIndicators;
		var available = Math.Min(RibbonCount, MaxRibbonCount);
		var hasValue = false;

		for (var i = 0; i < innerIndicators.Count; i++)
		{
			var inner = innerIndicators[i];
			var avg = i < available ? GetAverage(i) : float.NaN;

			if (!isFormed || float.IsNaN(avg))
			{
				var empty = new DecimalIndicatorValue(inner, time)
				{
					IsFinal = true,
					IsFormed = isFormed,
					IsEmpty = true,
				};
				value.Add(inner, empty);
				continue;
			}

			var decimalValue = new DecimalIndicatorValue(inner, (decimal)avg, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
			};
			value.Add(inner, decimalValue);
			hasValue = true;
		}

		value.IsEmpty = !hasValue;
		return value;
	}
}

/// <summary>
/// GPU calculator for Moving Average Ribbon indicator.
/// </summary>
public class GpuMovingAverageRibbonCalculator : GpuIndicatorCalculatorBase<MovingAverageRibbon, GpuMovingAverageRibbonParams, GpuMovingAverageRibbonResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuMovingAverageRibbonResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMovingAverageRibbonParams>, ArrayView<float>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuMovingAverageRibbonCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuMovingAverageRibbonCalculator(Context context, Accelerator accelerator)
	: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuMovingAverageRibbonResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMovingAverageRibbonParams>, ArrayView<float>>(MovingAverageRibbonParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuMovingAverageRibbonResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuMovingAverageRibbonParams[] parameters)
	{
		ArgumentNullException.ThrowIfNull(candlesSeries);
		ArgumentNullException.ThrowIfNull(parameters);

		if (candlesSeries.Length == 0)
		{
			throw new ArgumentOutOfRangeException(nameof(candlesSeries));
		}

		if (parameters.Length == 0)
		{
			throw new ArgumentOutOfRangeException(nameof(parameters));
		}

		foreach (var param in parameters)
		{
			if (param.RibbonCount < 2)
			{
				throw new ArgumentOutOfRangeException(nameof(parameters));
			}

			if (param.RibbonCount > GpuMovingAverageRibbonResult.MaxRibbonCount)
			{
				throw new ArgumentOutOfRangeException(nameof(parameters));
			}

			if (param.ShortPeriod < 1 || param.LongPeriod < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(parameters));
			}
		}

		var seriesCount = candlesSeries.Length;

		var totalSize = 0;
		var seriesOffsets = new int[seriesCount];
		var seriesLengths = new int[seriesCount];
		var maxLen = 0;

		for (var s = 0; s < seriesCount; s++)
		{
			seriesOffsets[s] = totalSize;
			var len = candlesSeries[s]?.Length ?? 0;
			seriesLengths[s] = len;
			totalSize += len;

			if (len > maxLen)
			{
				maxLen = len;
			}
		}

		var flatCandles = new GpuCandle[totalSize];
		var offset = 0;

		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			if (len <= 0)
			{
				continue;
			}

			Array.Copy(candlesSeries[s], 0, flatCandles, offset, len);
			offset += len;
		}

		using var inputBuffer = Accelerator.Allocate1D(flatCandles);
		using var offsetsBuffer = Accelerator.Allocate1D(seriesOffsets);
		using var lengthsBuffer = Accelerator.Allocate1D(seriesLengths);
		using var paramsBuffer = Accelerator.Allocate1D(parameters);
		using var outputBuffer = Accelerator.Allocate1D<GpuMovingAverageRibbonResult>(totalSize * parameters.Length);

		// Scratch buffer for intermediate SMA values per bar (used for sequence chaining)
		// Size: totalSize * parameters.Length (one float per bar per param set)
		using var scratchBuffer = Accelerator.Allocate1D<float>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View, scratchBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuMovingAverageRibbonResult[seriesCount][][];

		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuMovingAverageRibbonResult[parameters.Length][];

			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuMovingAverageRibbonResult[len];

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
	/// ILGPU kernel: Moving Average Ribbon computation for multiple series and parameter sets.
	/// Uses sequential bar processing to match CPU Sequence mode (each SMA output feeds the next SMA).
	/// </summary>
	private static void MovingAverageRibbonParamsSeriesKernel(
	Index2D index,
	ArrayView<GpuCandle> flatCandles,
	ArrayView<GpuMovingAverageRibbonResult> flatResults,
	ArrayView<int> offsets,
	ArrayView<int> lengths,
	ArrayView<GpuMovingAverageRibbonParams> parameters,
	ArrayView<float> scratch)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var offset = offsets[seriesIdx];
		var prm = parameters[paramIdx];
		var count = prm.RibbonCount;
		var priceType = (Level1Fields)prm.PriceType;
		var step = count > 1 ? (prm.LongPeriod - prm.ShortPeriod) / (count - 1) : 0;

		var scratchOffset = paramIdx * flatCandles.Length + offset;

		// Initialize scratch to NaN
		for (var i = 0; i < len; i++)
			scratch[scratchOffset + i] = float.NaN;

		// Process each ribbon SMA in sequence (matching CPU Sequence mode).
		// SMA[0] reads from candle prices. SMA[i] reads from SMA[i-1] output stored in scratch.
		for (var ribbonIdx = 0; ribbonIdx < count && ribbonIdx < GpuMovingAverageRibbonResult.MaxRibbonCount; ribbonIdx++)
		{
			var smaLength = prm.ShortPeriod + ribbonIdx * step;
			if (smaLength < 1)
				smaLength = 1;

			// Track how many valid inputs received for this SMA
			var validCount = 0;

			for (var i = 0; i < len; i++)
			{
				var globalIdx = offset + i;
				var resIndex = paramIdx * flatCandles.Length + globalIdx;

				// Initialize result on first ribbon pass
				if (ribbonIdx == 0)
				{
					var candle = flatCandles[globalIdx];
					var result = new GpuMovingAverageRibbonResult
					{
						Time = candle.Time,
						RibbonCount = count,
						IsFormed = 0,
					};

					for (var k = 0; k < GpuMovingAverageRibbonResult.MaxRibbonCount; k++)
						result.SetAverage(k, float.NaN);

					flatResults[resIndex] = result;
				}

				// Get input value: candle price for first SMA, previous SMA output for subsequent
				if (ribbonIdx == 0)
				{
					// Always have candle data
					validCount++;
				}
				else
				{
					var inputValue = scratch[scratchOffset + i];
					// If previous SMA hadn't produced a value, skip this bar
					if (float.IsNaN(inputValue))
						continue;
					validCount++;
				}

				if (validCount >= smaLength)
				{
					// Compute SMA over last smaLength input values
					var sum = 0f;
					if (ribbonIdx == 0)
					{
						// Read directly from candle data
						for (var j = 0; j < smaLength; j++)
							sum += ExtractPrice(flatCandles[globalIdx - j], priceType);
					}
					else
					{
						// Read from scratch (previous SMA outputs)
						// Find last smaLength valid entries going backwards
						var found = 0;
						for (var j = i; j >= 0 && found < smaLength; j--)
						{
							var val = scratch[scratchOffset + j];
							if (!float.IsNaN(val))
							{
								sum += val;
								found++;
							}
						}
					}

					var smaValue = sum / smaLength;

					// Store in result
					var res = flatResults[resIndex];
					res.SetAverage(ribbonIdx, smaValue);
					flatResults[resIndex] = res;
				}
			}

			// After processing this ribbon, copy outputs from flatResults to scratch
			// for the next ribbon to use as input. NaN for bars where this SMA wasn't formed.
			for (var i = 0; i < len; i++)
			{
				var resIndex = paramIdx * flatCandles.Length + (offset + i);
				var res = flatResults[resIndex];
				scratch[scratchOffset + i] = res.GetAverage(ribbonIdx);
			}
		}

		// Set IsFormed: CPU uses prevFormed pattern for complex indicators
		byte prevFormed = 0;
		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var resIndex = paramIdx * flatCandles.Length + globalIdx;

			var res = flatResults[resIndex];

			// Check if all ribbon SMAs have values for this bar
			byte allFormed = 1;
			for (var r = 0; r < count && r < GpuMovingAverageRibbonResult.MaxRibbonCount; r++)
			{
				if (float.IsNaN(res.GetAverage(r)))
				{
					allFormed = 0;
					break;
				}
			}

			res.IsFormed = prevFormed;
			flatResults[resIndex] = res;
			prevFormed = allFormed;
		}
	}
}

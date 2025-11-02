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
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuMovingAverageRibbonResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMovingAverageRibbonParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuMovingAverageRibbonCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuMovingAverageRibbonCalculator(Context context, Accelerator accelerator)
	: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index3D, ArrayView<GpuCandle>, ArrayView<GpuMovingAverageRibbonResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMovingAverageRibbonParams>>(MovingAverageRibbonParamsSeriesKernel);
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

		var extent = new Index3D(parameters.Length, seriesCount, maxLen);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
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
	/// </summary>
	private static void MovingAverageRibbonParamsSeriesKernel(
	Index3D index,
	ArrayView<GpuCandle> flatCandles,
	ArrayView<GpuMovingAverageRibbonResult> flatResults,
	ArrayView<int> offsets,
	ArrayView<int> lengths,
	ArrayView<GpuMovingAverageRibbonParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;
		var candleIdx = index.Z;

		var len = lengths[seriesIdx];
		if (candleIdx >= len)
		{
			return;
		}

		var offset = offsets[seriesIdx];
		var globalIdx = offset + candleIdx;
		var resIndex = paramIdx * flatCandles.Length + globalIdx;

		var candle = flatCandles[globalIdx];
		var prm = parameters[paramIdx];
		var result = new GpuMovingAverageRibbonResult
		{
			Time = candle.Time,
			RibbonCount = prm.RibbonCount,
			IsFormed = 0,
		};

		for (var i = 0; i < GpuMovingAverageRibbonResult.MaxRibbonCount; i++)
		{
			result.SetAverage(i, float.NaN);
		}

		var count = prm.RibbonCount;
		var priceType = (Level1Fields)prm.PriceType;
		var allFormed = true;
		var step = count > 1 ? (prm.LongPeriod - prm.ShortPeriod) / (count - 1) : 0;

		for (var ribbonIdx = 0; ribbonIdx < count && ribbonIdx < GpuMovingAverageRibbonResult.MaxRibbonCount; ribbonIdx++)
		{
			var length = prm.ShortPeriod + ribbonIdx * step;
			if (length < 1)
			{
				length = 1;
			}

			if (candleIdx + 1 < length)
			{
				allFormed = false;
				continue;
			}

			var sum = 0f;
			for (var j = 0; j < length; j++)
			{
				sum += ExtractPrice(flatCandles[globalIdx - j], priceType);
			}

			result.SetAverage(ribbonIdx, sum / length);
		}

		result.IsFormed = (byte)(allFormed ? 1 : 0);
		flatResults[resIndex] = result;
	}
}

namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU SuperTrend calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuSuperTrendParams"/> struct.
/// </remarks>
/// <param name="length">ATR period length.</param>
/// <param name="multiplier">ATR multiplier.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuSuperTrendParams(int length, float multiplier) : IGpuIndicatorParams
{
	/// <summary>
	/// ATR window length.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// ATR multiplier for SuperTrend bands.
	/// </summary>
	public float Multiplier = multiplier;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is SuperTrend st)
		{
			Unsafe.AsRef(in this).Length = st.Length;
			Unsafe.AsRef(in this).Multiplier = (float)st.Multiplier;
		}
	}
}

/// <summary>
/// GPU result for SuperTrend calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuSuperTrendResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// SuperTrend line value.
	/// </summary>
	public float SuperTrend;

	/// <summary>
	/// Trend direction flag (1 - uptrend, 0 - downtrend).
	/// </summary>
	public byte IsUpTrend;

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

		if (SuperTrend.IsNaN())
		{
			return new SuperTrendIndicatorValue(indicator, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		return new SuperTrendIndicatorValue(indicator, (decimal)SuperTrend, IsUpTrend != 0, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};
	}
}

/// <summary>
/// GPU calculator for SuperTrend indicator.
/// </summary>
public class GpuSuperTrendCalculator : GpuIndicatorCalculatorBase<SuperTrend, GpuSuperTrendParams, GpuSuperTrendResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuSuperTrendResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuSuperTrendParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuSuperTrendCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuSuperTrendCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuSuperTrendResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuSuperTrendParams>>(SuperTrendParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuSuperTrendResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuSuperTrendParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuSuperTrendResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuSuperTrendResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuSuperTrendResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuSuperTrendResult[len];
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
	/// ILGPU kernel that calculates SuperTrend for each (parameter, series) pair sequentially.
	/// </summary>
	private static void SuperTrendParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuSuperTrendResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuSuperTrendParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var param = parameters[paramIdx];
		var length = param.Length;
		if (length <= 0)
			length = 1;

		var multiplier = param.Multiplier;
		if (multiplier <= 0f)
			multiplier = 1f;

		var prevClose = flatCandles[offset].Close;
		var trSum = 0f;
		var atr = 0f;
		var atrInitialized = false;

		var prevUpperBand = 0f;
		var prevLowerBand = 0f;
		var prevTrend = 1;
		var hasPrevUpper = false;
		var hasPrevLower = false;
		var hasPrevSupertrend = false;
		var totalSize = flatCandles.Length;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var high = candle.High;
			var low = candle.Low;
			var close = candle.Close;

			var tr1 = high - low;
			var tr2 = MathF.Abs(high - prevClose);
			var tr3 = MathF.Abs(low - prevClose);
			var tr = MathF.Max(tr1, MathF.Max(tr2, tr3));

			var resIndex = paramIdx * totalSize + globalIdx;
			flatResults[resIndex] = new GpuSuperTrendResult
			{
				Time = candle.Time,
				SuperTrend = float.NaN,
				IsUpTrend = 0,
				IsFormed = 0,
			};

			if (!atrInitialized)
			{
				trSum += tr;
				if (i == length - 1)
				{
					atr = trSum / length;
					atrInitialized = true;
				}
			}
			else
			{
				atr = ((atr * (length - 1)) + tr) / length;
			}

			if (!atrInitialized)
			{
				prevClose = close;
				continue;
			}

			var hl2 = (high + low) / 2f;
			var basicUpperBand = hl2 + multiplier * atr;
			var basicLowerBand = hl2 - multiplier * atr;

			var finalUpperBand = !hasPrevUpper || basicUpperBand < prevUpperBand || prevClose > prevUpperBand
				? basicUpperBand
				: prevUpperBand;

			var finalLowerBand = !hasPrevLower || basicLowerBand > prevLowerBand || prevClose < prevLowerBand
				? basicLowerBand
				: prevLowerBand;

			float supertrend;
			int trend;

			if (!hasPrevSupertrend)
			{
				supertrend = close >= hl2 ? finalLowerBand : finalUpperBand;
				trend = close >= hl2 ? 1 : -1;
			}
			else if (prevTrend == 1)
			{
				supertrend = close <= finalLowerBand ? finalUpperBand : finalLowerBand;
				trend = close <= finalLowerBand ? -1 : 1;
			}
			else
			{
				supertrend = close >= finalUpperBand ? finalLowerBand : finalUpperBand;
				trend = close >= finalUpperBand ? 1 : -1;
			}

			flatResults[resIndex] = new GpuSuperTrendResult
			{
				Time = candle.Time,
				SuperTrend = supertrend,
				IsUpTrend = (byte)(trend == 1 ? 1 : 0),
				IsFormed = 1,
			};

			prevTrend = trend;
			prevUpperBand = finalUpperBand;
			prevLowerBand = finalLowerBand;
			hasPrevUpper = true;
			hasPrevLower = true;
			hasPrevSupertrend = true;
			prevClose = close;
		}
	}
}

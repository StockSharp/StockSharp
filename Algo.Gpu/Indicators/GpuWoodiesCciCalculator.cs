namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Woodies CCI calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuWoodiesCciParams"/> struct.
/// </remarks>
/// <param name="cciLength">CCI length.</param>
/// <param name="smaLength">SMA length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuWoodiesCciParams(int cciLength, int smaLength) : IGpuIndicatorParams
{
	/// <summary>
	/// CCI period length.
	/// </summary>
	public int CciLength = cciLength;

	/// <summary>
	/// SMA period length applied to CCI.
	/// </summary>
	public int SmaLength = smaLength;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is WoodiesCCI woodies)
		{
			Unsafe.AsRef(in this).CciLength = woodies.Length;
			Unsafe.AsRef(in this).SmaLength = woodies.SMALength;
		}
	}
}

/// <summary>
/// GPU result for Woodies CCI calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuWoodiesCciResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Computed CCI value.
	/// </summary>
	public float Cci;

	/// <summary>
	/// SMA value applied to CCI.
	/// </summary>
	public float Sma;

	/// <summary>
	/// Indicates whether CCI is formed.
	/// </summary>
	public byte CciIsFormed;

	/// <summary>
	/// Indicates whether SMA is formed.
	/// </summary>
	public byte SmaIsFormed;

	/// <summary>
	/// Indicator formed flag (byte for GPU alignment).
	/// </summary>
	public byte IsFormed;

	readonly long IGpuIndicatorResult.Time => Time;
	readonly byte IGpuIndicatorResult.IsFormed => IsFormed;

	/// <inheritdoc />
	public readonly IIndicatorValue ToValue(IIndicator indicator)
	{
		var time = this.GetTime();
		var isFormed = this.GetIsFormed();
		var woodies = (WoodiesCCI)indicator;

		var value = new WoodiesCCIValue(woodies, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
			IsEmpty = float.IsNaN(Cci) && float.IsNaN(Sma),
		};

		var cciIndicator = woodies.Cci;
		IIndicatorValue cciValue;

		if (!float.IsNaN(Cci))
		{
			cciValue = new DecimalIndicatorValue(cciIndicator, (decimal)Cci, time)
			{
				IsFinal = true,
				IsFormed = CciIsFormed != 0,
			};
		}
		else
		{
			cciValue = new DecimalIndicatorValue(cciIndicator, time)
			{
				IsFinal = true,
				IsFormed = false,
				IsEmpty = true,
			};
		}

		value.Add(cciIndicator, cciValue);

		var smaIndicator = woodies.Sma;
		IIndicatorValue smaValue;

		if (!float.IsNaN(Sma))
		{
			smaValue = new DecimalIndicatorValue(smaIndicator, (decimal)Sma, time)
			{
				IsFinal = true,
				IsFormed = SmaIsFormed != 0,
			};
		}
		else
		{
			smaValue = new DecimalIndicatorValue(smaIndicator, time)
			{
				IsFinal = true,
				IsFormed = false,
				IsEmpty = true,
			};
		}

		value.Add(smaIndicator, smaValue);

		return value;
	}
}

/// <summary>
/// GPU calculator for Woodies CCI indicator.
/// </summary>
public class GpuWoodiesCciCalculator : GpuIndicatorCalculatorBase<WoodiesCCI, GpuWoodiesCciParams, GpuWoodiesCciResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuWoodiesCciResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuWoodiesCciParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuWoodiesCciCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuWoodiesCciCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index3D, ArrayView<GpuCandle>, ArrayView<GpuWoodiesCciResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuWoodiesCciParams>>(WoodiesCciParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuWoodiesCciResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuWoodiesCciParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuWoodiesCciResult>(totalSize * parameters.Length);

		var extent = new Index3D(parameters.Length, seriesCount, maxLen);
		_paramsSeriesKernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		// Re-split [series][param][bar]
		var result = new GpuWoodiesCciResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuWoodiesCciResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuWoodiesCciResult[len];
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
	/// ILGPU kernel: computes Woodies CCI for multiple series and parameter sets.
	/// </summary>
	private static void WoodiesCciParamsSeriesKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuWoodiesCciResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuWoodiesCciParams> parameters)
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

		var prm = parameters[paramIdx];
		var cciLength = prm.CciLength;
		var smaLength = prm.SmaLength;

		var cciValue = ComputeCci(flatCandles, globalIdx, cciLength, out var cciIsFormed);
		var smaValue = ComputeSmaOfCci(flatCandles, globalIdx, cciLength, smaLength, cciValue, cciIsFormed, out var smaIsFormed);

		flatResults[resIndex] = new()
		{
			Time = candle.Time,
			Cci = cciValue,
			Sma = smaValue,
			CciIsFormed = cciIsFormed,
			SmaIsFormed = smaIsFormed,
			IsFormed = smaIsFormed,
		};
	}

	private static float ComputeCci(ArrayView<GpuCandle> candles, int globalIdx, int cciLength, out byte isFormed)
	{
		isFormed = 0;

		if (cciLength <= 0 || globalIdx < 0)
			return float.NaN;

		if (globalIdx < cciLength - 1)
			return float.NaN;

		var sum = 0f;
		for (var i = 0; i < cciLength; i++)
			sum += GetTypicalPrice(candles[globalIdx - i]);

		var mean = sum / cciLength;
		var deviation = 0f;
		for (var i = 0; i < cciLength; i++)
		{
			var tp = GetTypicalPrice(candles[globalIdx - i]);
			deviation += MathF.Abs(tp - mean);
		}

		deviation /= cciLength;
		if (deviation == 0f)
			return float.NaN;

		isFormed = 1;
		var currentTp = GetTypicalPrice(candles[globalIdx]);
		return (currentTp - mean) / (0.015f * deviation);
	}

	private static float ComputeSmaOfCci(
		ArrayView<GpuCandle> candles,
		int globalIdx,
		int cciLength,
		int smaLength,
		float currentCci,
		byte currentCciIsFormed,
		out byte isFormed)
	{
		isFormed = 0;

		if (smaLength <= 1)
		{
			if (currentCciIsFormed == 0)
				return float.NaN;

			isFormed = currentCciIsFormed;
			return currentCci;
		}

		if (currentCciIsFormed == 0)
			return float.NaN;

		var minIndex = cciLength - 1 + (smaLength - 1);
		if (globalIdx < minIndex)
			return float.NaN;

		var sum = currentCci;
		for (var i = 1; i < smaLength; i++)
		{
			var idx = globalIdx - i;
			var cciValue = ComputeCci(candles, idx, cciLength, out var cciIsFormed);
			if (cciIsFormed == 0 || float.IsNaN(cciValue))
				return float.NaN;

			sum += cciValue;
		}

		isFormed = 1;
		return sum / smaLength;
	}

	private static float GetTypicalPrice(GpuCandle candle)
	=> (candle.High + candle.Low + candle.Close) / 3f;
}

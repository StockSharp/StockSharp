namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Bollinger Bands calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuBollingerBandsParams"/> struct.
/// </remarks>
/// <param name="length">Bollinger Bands period length.</param>
/// <param name="width">Channel width multiplier.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuBollingerBandsParams(int length, float width, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Period length.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// Channel width multiplier.
	/// </summary>
	public float Width = width;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is BollingerBands bands)
		{
			Unsafe.AsRef(in this).Length = bands.Length;
			Unsafe.AsRef(in this).Width = (float)bands.Width;
		}
	}
}

/// <summary>
/// Complex GPU result for Bollinger Bands calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuBollingerBandsResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTimeOffset.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Middle band value.
	/// </summary>
	public float Middle;

	/// <summary>
	/// Upper band value.
	/// </summary>
	public float Upper;

	/// <summary>
	/// Lower band value.
	/// </summary>
	public float Lower;

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

		var bands = (BollingerBands)indicator;

		if (Middle.IsNaN() || Upper.IsNaN() || Lower.IsNaN())
		{
			return new BollingerBandsValue(bands, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		var value = new BollingerBandsValue(bands, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		value.Add(bands.MovingAverage, new DecimalIndicatorValue(bands.MovingAverage, (decimal)Middle, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		value.Add(bands.UpBand, new DecimalIndicatorValue(bands.UpBand, (decimal)Upper, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		value.Add(bands.LowBand, new DecimalIndicatorValue(bands.LowBand, (decimal)Lower, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		return value;
	}
}

/// <summary>
/// GPU calculator for Bollinger Bands indicator.
/// </summary>
public class GpuBollingerBandsCalculator : GpuIndicatorCalculatorBase<BollingerBands, GpuBollingerBandsParams, GpuBollingerBandsResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuBollingerBandsResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuBollingerBandsParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuBollingerBandsCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuBollingerBandsCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index3D, ArrayView<GpuCandle>, ArrayView<GpuBollingerBandsResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuBollingerBandsParams>>(BollingerParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuBollingerBandsResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuBollingerBandsParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuBollingerBandsResult>(totalSize * parameters.Length);

		var extent = new Index3D(parameters.Length, seriesCount, maxLen);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		// Re-split [series][param][bar]
		var result = new GpuBollingerBandsResult[seriesCount][][];

		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuBollingerBandsResult[parameters.Length][];

			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuBollingerBandsResult[len];

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
	/// ILGPU kernel: Bollinger Bands computation for multiple series and multiple parameter sets.
	/// </summary>
	private static void BollingerParamsSeriesKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuBollingerBandsResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuBollingerBandsParams> parameters)
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

		flatResults[resIndex] = new GpuBollingerBandsResult
		{
			Time = candle.Time,
			Middle = float.NaN,
			Upper = float.NaN,
			Lower = float.NaN,
			IsFormed = 0,
		};

		var prm = parameters[paramIdx];
		var L = prm.Length;

		if (L <= 0 || candleIdx < L - 1)
			return;

		var priceType = (Level1Fields)prm.PriceType;

		var sum = 0f;
		var sumSq = 0f;

		for (var j = 0; j < L; j++)
		{
			var price = ExtractPrice(flatCandles[globalIdx - j], priceType);
			sum += price;
			sumSq += price * price;
		}

		var lengthF = (float)L;
		var mean = sum / lengthF;
		var variance = MathF.Max((sumSq / lengthF) - (mean * mean), 0f);
		var stdDev = MathF.Sqrt(variance);
		var width = prm.Width;

		flatResults[resIndex] = new GpuBollingerBandsResult
		{
			Time = candle.Time,
			Middle = mean,
			Upper = mean + (width * stdDev),
			Lower = mean - (width * stdDev),
			IsFormed = 1,
		};
	}
}

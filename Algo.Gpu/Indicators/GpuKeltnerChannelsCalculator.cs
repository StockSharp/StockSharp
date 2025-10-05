namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Keltner Channels calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuKeltnerChannelsParams"/> struct.
/// </remarks>
/// <param name="length">Keltner Channels period length.</param>
/// <param name="multiplier">ATR multiplier.</param>
/// <param name="priceType">Price type for EMA calculation.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuKeltnerChannelsParams(int length, float multiplier, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Keltner Channels period length.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// ATR multiplier value.
	/// </summary>
	public float Multiplier = multiplier;

	/// <summary>
	/// Price type to use for EMA calculation.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is KeltnerChannels channels)
		{
			Unsafe.AsRef(in this).Length = channels.Length;
			Unsafe.AsRef(in this).Multiplier = (float)channels.Multiplier;
		}
	}
}

/// <summary>
/// GPU result for Keltner Channels calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuKeltnerChannelsResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTimeOffset.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Middle line value.
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

		var channels = (KeltnerChannels)indicator;

		if (Middle.IsNaN() || Upper.IsNaN() || Lower.IsNaN())
		{
			return new KeltnerChannelsValue(channels, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		var middleValue = new DecimalIndicatorValue(channels.Middle, (decimal)Middle, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		var upperValue = new DecimalIndicatorValue(channels.Upper, (decimal)Upper, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		var lowerValue = new DecimalIndicatorValue(channels.Lower, (decimal)Lower, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		var result = new KeltnerChannelsValue(channels, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		result.Add(channels.Middle, middleValue);
		result.Add(channels.Upper, upperValue);
		result.Add(channels.Lower, lowerValue);

		return result;
	}
}

/// <summary>
/// GPU calculator for Keltner Channels indicator.
/// </summary>
public class GpuKeltnerChannelsCalculator : GpuIndicatorCalculatorBase<KeltnerChannels, GpuKeltnerChannelsParams, GpuKeltnerChannelsResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuKeltnerChannelsResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuKeltnerChannelsParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuKeltnerChannelsCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuKeltnerChannelsCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuKeltnerChannelsResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuKeltnerChannelsParams>>(KeltnerChannelsParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuKeltnerChannelsResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuKeltnerChannelsParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuKeltnerChannelsResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuKeltnerChannelsResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuKeltnerChannelsResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuKeltnerChannelsResult[len];
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
	/// ILGPU kernel: Keltner Channels computation for multiple series and parameter sets.
	/// </summary>
	private static void KeltnerChannelsParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuKeltnerChannelsResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuKeltnerChannelsParams> parameters)
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

		var priceType = (Level1Fields)param.PriceType;
		var multiplier = param.Multiplier;
		var emaMultiplier = 2f / (length + 1f);

		float emaSum = 0f;
		float ema = 0f;

		float trSum = 0f;
		float atr = 0f;

		var firstCandle = flatCandles[offset];
		var prevClose = firstCandle.Close;

		for (var i = 0; i < len; i++)
		{
			var c = flatCandles[offset + i];
			var resIndex = paramIdx * flatCandles.Length + (offset + i);
			var result = new GpuKeltnerChannelsResult
			{
				Time = c.Time,
				Middle = float.NaN,
				Upper = float.NaN,
				Lower = float.NaN,
				IsFormed = 0,
			};

			var price = ExtractPrice(c, priceType);

			if (i < length)
			{
				emaSum += price;
				if (i == length - 1)
					ema = emaSum / length;
			}
			else
			{
				ema = ema + emaMultiplier * (price - ema);
			}

			float tr;
			if (i == 0)
			{
				tr = c.High - c.Low;
			}
			else
			{
				var tr1 = c.High - c.Low;
				var tr2 = MathF.Abs(c.High - prevClose);
				var tr3 = MathF.Abs(c.Low - prevClose);
				tr = MathF.Max(tr1, MathF.Max(tr2, tr3));
			}

			if (i < length)
			{
				trSum += tr;
				if (i == length - 1)
					atr = trSum / length;
			}
			else
			{
				atr = ((atr * (length - 1)) + tr) / length;
			}

			if (i >= length - 1)
			{
				var offsetAtr = multiplier * atr;
				result.Middle = ema;
				result.Upper = ema + offsetAtr;
				result.Lower = ema - offsetAtr;
				result.IsFormed = 1;
			}

			flatResults[resIndex] = result;

			prevClose = c.Close;
		}
	}
}

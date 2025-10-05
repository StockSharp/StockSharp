namespace StockSharp.Algo.Gpu.Indicators;

using System.Runtime.InteropServices;

/// <summary>
/// Parameter set for GPU Price Channels calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuPriceChannelsParams"/> struct.
/// </remarks>
/// <param name="length">Price Channels period length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuPriceChannelsParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// Price Channels period length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is PriceChannels priceChannels)
		{
			Unsafe.AsRef(in this).Length = priceChannels.Length;
		}
	}
}

/// <summary>
/// GPU result for Price Channels calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuPriceChannelsResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTimeOffset.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Upper channel value.
	/// </summary>
	public float Upper;

	/// <summary>
	/// Lower channel value.
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

		var priceChannels = (PriceChannels)indicator;
		var value = new PriceChannelsValue(priceChannels, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		IIndicatorValue upperValue;
		if (Upper.IsNaN())
		{
			upperValue = new DecimalIndicatorValue(priceChannels.UpperChannel, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
			value.IsEmpty = true;
		}
		else
		{
			upperValue = new DecimalIndicatorValue(priceChannels.UpperChannel, (decimal)Upper, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
			};
		}

		IIndicatorValue lowerValue;
		if (Lower.IsNaN())
		{
			lowerValue = new DecimalIndicatorValue(priceChannels.LowerChannel, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
			value.IsEmpty = true;
		}
		else
		{
			lowerValue = new DecimalIndicatorValue(priceChannels.LowerChannel, (decimal)Lower, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
			};
		}

		value.Add(priceChannels.UpperChannel, upperValue);
		value.Add(priceChannels.LowerChannel, lowerValue);

		return value;
	}
}

/// <summary>
/// GPU calculator for Price Channels indicator.
/// </summary>
public class GpuPriceChannelsCalculator : GpuIndicatorCalculatorBase<PriceChannels, GpuPriceChannelsParams, GpuPriceChannelsResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuPriceChannelsResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuPriceChannelsParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuPriceChannelsCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuPriceChannelsCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index3D, ArrayView<GpuCandle>, ArrayView<GpuPriceChannelsResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuPriceChannelsParams>>(PriceChannelsKernel);
	}

	/// <inheritdoc />
	public override GpuPriceChannelsResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuPriceChannelsParams[] parameters)
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
				{
					maxLen = len;
				}
			}
		}

		using var inputBuffer = Accelerator.Allocate1D(flatCandles);
		using var offsetsBuffer = Accelerator.Allocate1D(seriesOffsets);
		using var lengthsBuffer = Accelerator.Allocate1D(seriesLengths);
		using var paramsBuffer = Accelerator.Allocate1D(parameters);
		using var outputBuffer = Accelerator.Allocate1D<GpuPriceChannelsResult>(totalSize * parameters.Length);

		var extent = new Index3D(parameters.Length, seriesCount, maxLen);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuPriceChannelsResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuPriceChannelsResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuPriceChannelsResult[len];
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
	/// ILGPU kernel that calculates Price Channels for multiple series and parameter sets simultaneously.
	/// </summary>
	private static void PriceChannelsKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuPriceChannelsResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuPriceChannelsParams> parameters)
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

		var candle = flatCandles[globalIdx];
		var resIdx = paramIdx * flatCandles.Length + globalIdx;
		flatResults[resIdx] = new()
		{
			Time = candle.Time,
			Upper = float.NaN,
			Lower = float.NaN,
			IsFormed = 0,
		};

		var prm = parameters[paramIdx];
		var length = prm.Length;
		if (length <= 0)
		{
			return;
		}

		if (candleIdx < length - 1)
		{
			return;
		}

		var highest = float.MinValue;
		var lowest = float.MaxValue;
		for (var j = 0; j < length; j++)
		{
			var sourceCandle = flatCandles[globalIdx - j];
			if (sourceCandle.High > highest)
			{
				highest = sourceCandle.High;
			}
			if (sourceCandle.Low < lowest)
			{
				lowest = sourceCandle.Low;
			}
		}

		flatResults[resIdx] = new()
		{
			Time = candle.Time,
			Upper = highest,
			Lower = lowest,
			IsFormed = 1,
		};
	}
}

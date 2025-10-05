﻿namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Donchian Channels calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuDonchianChannelsParams"/> struct.
/// </remarks>
/// <param name="length">Donchian channel length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuDonchianChannelsParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// Donchian channel window length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is DonchianChannels donchian)
		{
			Unsafe.AsRef(in this).Length = donchian.Length;
		}
	}
}

/// <summary>
/// Complex GPU result for Donchian Channels calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuDonchianChannelsResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTimeOffset.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Upper band value.
	/// </summary>
	public float Upper;

	/// <summary>
	/// Lower band value.
	/// </summary>
	public float Lower;

	/// <summary>
	/// Middle band value.
	/// </summary>
	public float Middle;

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

		var donchian = (DonchianChannels)indicator;

		if (Upper.IsNaN() || Lower.IsNaN() || Middle.IsNaN())
		{
			return new DonchianChannelsValue(donchian, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		var upperValue = new DecimalIndicatorValue(donchian.UpperBand, (decimal)Upper, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		var lowerValue = new DecimalIndicatorValue(donchian.LowerBand, (decimal)Lower, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		var middleValue = new DecimalIndicatorValue(donchian.Middle, (decimal)Middle, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		var value = new DonchianChannelsValue(donchian, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		value.Add(donchian.UpperBand, upperValue);
		value.Add(donchian.LowerBand, lowerValue);
		value.Add(donchian.Middle, middleValue);

		return value;
	}
}

/// <summary>
/// GPU calculator for Donchian Channels indicator.
/// </summary>
public class GpuDonchianChannelsCalculator : GpuIndicatorCalculatorBase<DonchianChannels, GpuDonchianChannelsParams, GpuDonchianChannelsResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuDonchianChannelsResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuDonchianChannelsParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuDonchianChannelsCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuDonchianChannelsCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index3D, ArrayView<GpuCandle>, ArrayView<GpuDonchianChannelsResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuDonchianChannelsParams>>(DonchianParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuDonchianChannelsResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuDonchianChannelsParams[] parameters)
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
				{
					maxLen = len;
				}
			}
		}

		using var inputBuffer = Accelerator.Allocate1D(flatCandles);
		using var offsetsBuffer = Accelerator.Allocate1D(seriesOffsets);
		using var lengthsBuffer = Accelerator.Allocate1D(seriesLengths);
		using var paramsBuffer = Accelerator.Allocate1D(parameters);
		using var outputBuffer = Accelerator.Allocate1D<GpuDonchianChannelsResult>(totalSize * parameters.Length);

		var extent = new Index3D(parameters.Length, seriesCount, maxLen);
		_paramsSeriesKernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuDonchianChannelsResult[seriesCount][][];

		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuDonchianChannelsResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuDonchianChannelsResult[len];
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
	/// ILGPU kernel: Donchian Channels computation for multiple series and multiple parameter sets. Results are stored as [parameter][global candle index].
	/// </summary>
	private static void DonchianParamsSeriesKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuDonchianChannelsResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuDonchianChannelsParams> parameters)
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

		var resultIndex = paramIdx * flatCandles.Length + globalIdx;

		var candle = flatCandles[globalIdx];

		var result = new GpuDonchianChannelsResult
		{
			Time = candle.Time,
			Upper = float.NaN,
			Lower = float.NaN,
			Middle = float.NaN,
			IsFormed = 0,
		};

		var length = parameters[paramIdx].Length;
		if (length <= 0)
		{
			length = 1;
		}

		if (candleIdx >= length - 1)
		{
			var highest = float.MinValue;
			var lowest = float.MaxValue;

			for (var j = 0; j < length; j++)
			{
				var windowCandle = flatCandles[globalIdx - j];
				if (windowCandle.High > highest)
				{
					highest = windowCandle.High;
				}
				if (windowCandle.Low < lowest)
				{
					lowest = windowCandle.Low;
				}
			}

			result.Upper = highest;
			result.Lower = lowest;
			result.Middle = (highest + lowest) / 2f;
			result.IsFormed = 1;
		}

		flatResults[resultIndex] = result;
	}
}



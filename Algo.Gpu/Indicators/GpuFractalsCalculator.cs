namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Fractals calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuFractalsParams"/> struct.
/// </remarks>
/// <param name="length">Fractals window length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuFractalsParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// Fractals window length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is Fractals fractals)
		{
			Unsafe.AsRef(in this).Length = fractals.Length;
		}
	}
}

/// <summary>
/// GPU result for Fractals calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuFractalsResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTimeOffset.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Up fractal value.
	/// </summary>
	public float Up;

	/// <summary>
	/// Down fractal value.
	/// </summary>
	public float Down;

	/// <summary>
	/// Shift applied to up fractal value.
	/// </summary>
	public int UpShift;

	/// <summary>
	/// Shift applied to down fractal value.
	/// </summary>
	public int DownShift;

	/// <summary>
	/// Is indicator formed (byte for GPU friendliness).
	/// </summary>
	public byte IsFormed;

	readonly long IGpuIndicatorResult.Time => Time;
	readonly byte IGpuIndicatorResult.IsFormed => IsFormed;

	/// <inheritdoc />
	public readonly IIndicatorValue ToValue(IIndicator indicator)
	{
		var fractals = (Fractals)indicator;
		var time = this.GetTime();
		var isFormed = this.GetIsFormed();

		var value = new FractalsValue(fractals, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		IIndicatorValue upValue;

		if (Up.IsNaN())
		{
			upValue = new FractalPartIndicatorValue(fractals.Up, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
			};
		}
		else
		{
			upValue = new FractalPartIndicatorValue(fractals.Up, (decimal)Up, UpShift, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
			};
		}

		value.Add(fractals.Up, upValue);

		IIndicatorValue downValue;

		if (Down.IsNaN())
		{
			downValue = new FractalPartIndicatorValue(fractals.Down, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
			};
		}
		else
		{
			downValue = new FractalPartIndicatorValue(fractals.Down, (decimal)Down, DownShift, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
			};
		}

		value.Add(fractals.Down, downValue);

		return value;
	}
}

/// <summary>
/// GPU calculator for Fractals indicator.
/// </summary>
public class GpuFractalsCalculator : GpuIndicatorCalculatorBase<Fractals, GpuFractalsParams, GpuFractalsResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuFractalsResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuFractalsParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuFractalsCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuFractalsCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuFractalsResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuFractalsParams>>(FractalsParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuFractalsResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuFractalsParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuFractalsResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuFractalsResult[seriesCount][][];

		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuFractalsResult[parameters.Length][];

			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuFractalsResult[len];

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
	/// ILGPU kernel performing Fractals calculation for multiple series and parameter sets.
	/// </summary>
	private static void FractalsParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuFractalsResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuFractalsParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];

		if (len <= 0)
			return;

		var window = parameters[paramIdx].Length;
		var validWindow = window >= 3 && (window & 1) != 0;
		var half = validWindow ? window >> 1 : 0;

		var counterUp = 0;
		var counterDown = 0;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var resIndex = paramIdx * flatCandles.Length + globalIdx;

			var result = new GpuFractalsResult
			{
				Time = candle.Time,
				Up = float.NaN,
				Down = float.NaN,
				UpShift = half,
				DownShift = half,
				IsFormed = (byte)(validWindow && (i + 1) >= window ? 1 : 0),
			};

			if (!validWindow)
			{
				flatResults[resIndex] = result;
				continue;
			}

			counterUp++;
			counterDown++;

			if (counterUp >= window)
			{
				var start = globalIdx - window + 1;
				var isUp = true;

				for (var j = 0; j < half; j++)
				{
					if (!(flatCandles[start + j].High < flatCandles[start + j + 1].High))
					{
						isUp = false;
						break;
					}
				}

				if (isUp)
				{
					for (var j = half; j < window - 1; j++)
					{
						if (!(flatCandles[start + j].High > flatCandles[start + j + 1].High))
						{
							isUp = false;
							break;
						}
					}
				}

				if (isUp)
				{
					result.Up = flatCandles[start + half].High;
					counterUp = 0;
				}
			}

			if (counterDown >= window)
			{
				var start = globalIdx - window + 1;
				var isDown = true;

				for (var j = 0; j < half; j++)
				{
					if (!(flatCandles[start + j].Low > flatCandles[start + j + 1].Low))
					{
						isDown = false;
						break;
					}
				}

				if (isDown)
				{
					for (var j = half; j < window - 1; j++)
					{
						if (!(flatCandles[start + j].Low < flatCandles[start + j + 1].Low))
						{
							isDown = false;
							break;
						}
					}
				}

				if (isDown)
				{
					result.Down = flatCandles[start + half].Low;
					counterDown = 0;
				}
			}

			flatResults[resIndex] = result;
		}
	}
}

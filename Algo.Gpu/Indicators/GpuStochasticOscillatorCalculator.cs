namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Stochastic Oscillator calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuStochasticOscillatorParams"/> struct.
/// </remarks>
/// <param name="kLength">%K length.</param>
/// <param name="dLength">%D length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuStochasticOscillatorParams(int kLength, int dLength) : IGpuIndicatorParams
{
	/// <summary>
	/// %K period length.
	/// </summary>
	public int KLength = kLength;

	/// <summary>
	/// %D smoothing length.
	/// </summary>
	public int DLength = dLength;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is StochasticOscillator stochastic)
		{
			Unsafe.AsRef(in this).KLength = stochastic.K.Length;
			Unsafe.AsRef(in this).DLength = stochastic.D.Length;
		}
	}
}

/// <summary>
/// Complex GPU result for Stochastic Oscillator calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuStochasticOscillatorResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Calculated %K value.
	/// </summary>
	public float K;

	/// <summary>
	/// Calculated %D value.
	/// </summary>
	public float D;

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

		var stochastic = (StochasticOscillator)indicator;

		var kFormed = !K.IsNaN();
		var dFormed = !D.IsNaN();

		if (!kFormed && !dFormed)
		{
			return new StochasticOscillatorValue(stochastic, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		var value = new StochasticOscillatorValue(stochastic, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
			IsEmpty = false,
		};

		if (kFormed)
		{
			value.Add(stochastic.K, new DecimalIndicatorValue(stochastic.K, (decimal)K, time)
			{
				IsFinal = true,
				IsFormed = true,
			});
		}
		else
		{
			value.Add(stochastic.K, new DecimalIndicatorValue(stochastic.K, time)
			{
				IsFinal = true,
				IsFormed = false,
			});
		}

		if (dFormed)
		{
			value.Add(stochastic.D, new DecimalIndicatorValue(stochastic.D, (decimal)D, time)
			{
				IsFinal = true,
				IsFormed = true,
			});
		}
		else
		{
			value.Add(stochastic.D, new DecimalIndicatorValue(stochastic.D, time)
			{
				IsFinal = true,
				IsFormed = false,
			});
		}

		return value;
	}
}

/// <summary>
/// GPU calculator for Stochastic Oscillator indicator.
/// </summary>
public class GpuStochasticOscillatorCalculator : GpuIndicatorCalculatorBase<StochasticOscillator, GpuStochasticOscillatorParams, GpuStochasticOscillatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuStochasticOscillatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuStochasticOscillatorParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuStochasticOscillatorCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuStochasticOscillatorCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuStochasticOscillatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuStochasticOscillatorParams>>(StochasticOscillatorKernel);
	}

	/// <inheritdoc />
	public override GpuStochasticOscillatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuStochasticOscillatorParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuStochasticOscillatorResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuStochasticOscillatorResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuStochasticOscillatorResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuStochasticOscillatorResult[len];
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
	/// ILGPU kernel performing Stochastic Oscillator calculation per (parameter, series) pair.
	/// </summary>
	private static void StochasticOscillatorKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuStochasticOscillatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuStochasticOscillatorParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var offset = offsets[seriesIdx];
		var total = flatCandles.Length;
		var prm = parameters[paramIdx];

		var kLength = prm.KLength <= 0 ? 1 : prm.KLength;
		var dLength = prm.DLength <= 0 ? 1 : prm.DLength;

		var dStartIndex = kLength + dLength - 2;
		var resBase = paramIdx * total;
		var kWindow = kLength - 1;

		float dSum = 0f;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];

			var result = new GpuStochasticOscillatorResult
			{
				Time = candle.Time,
				K = float.NaN,
				D = float.NaN,
				IsFormed = 0,
			};

			if (i >= kWindow)
			{
				var windowStart = globalIdx - kWindow;
				var highest = float.MinValue;
				var lowest = float.MaxValue;

				for (var j = 0; j < kLength; j++)
				{
					var c = flatCandles[windowStart + j];

					if (c.High > highest)
						highest = c.High;

					if (c.Low < lowest)
						lowest = c.Low;
				}

				var diff = highest - lowest;
				var kValue = diff == 0f ? 0f : 100f * ((candle.Close - lowest) / diff);
				result.K = kValue;

				dSum += kValue;

				if (i >= dStartIndex)
				{
					if (i > dStartIndex)
					{
						var removeIdx = resBase + (globalIdx - dLength);
						dSum -= flatResults[removeIdx].K;
					}

					result.D = dSum / dLength;
					result.IsFormed = 1;
				}
			}

			flatResults[resBase + globalIdx] = result;
		}
	}
}

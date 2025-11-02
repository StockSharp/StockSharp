namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Sine Wave calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuSineWaveParams"/> struct.
/// </remarks>
/// <param name="length">Sine Wave length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuSineWaveParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// Sine Wave period length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is SineWave sineWave)
		{
			Unsafe.AsRef(in this).Length = sineWave.Length;
		}
	}
}

/// <summary>
/// GPU result structure for Sine Wave indicator.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuSineWaveResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Main line value.
	/// </summary>
	public float Main;

	/// <summary>
	/// Lead line value.
	/// </summary>
	public float Lead;

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

		var sineWave = (SineWave)indicator;

		if (Main.IsNaN() || Lead.IsNaN())
		{
			return new SineWaveValue(sineWave, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		var result = new SineWaveValue(sineWave, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		result.Add(sineWave.Main, new DecimalIndicatorValue(sineWave.Main, (decimal)Main, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		result.Add(sineWave.Lead, new DecimalIndicatorValue(sineWave.Lead, (decimal)Lead, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		return result;
	}
}

/// <summary>
/// GPU calculator for Sine Wave indicator.
/// </summary>
public class GpuSineWaveCalculator : GpuIndicatorCalculatorBase<SineWave, GpuSineWaveParams, GpuSineWaveResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuSineWaveResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuSineWaveParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuSineWaveCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuSineWaveCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index3D, ArrayView<GpuCandle>, ArrayView<GpuSineWaveResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuSineWaveParams>>(SineWaveParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuSineWaveResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuSineWaveParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuSineWaveResult>(totalSize * parameters.Length);

		var extent = new Index3D(parameters.Length, seriesCount, maxLen);
		_paramsSeriesKernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		// Re-split [series][param][bar]
		var result = new GpuSineWaveResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuSineWaveResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuSineWaveResult[len];
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
	/// ILGPU kernel: Sine Wave computation for multiple series and multiple parameter sets. Results are stored as [param][globalIdx].
	/// </summary>
	private static void SineWaveParamsSeriesKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuSineWaveResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuSineWaveParams> parameters)
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
		var period = prm.Length <= 0 ? 1 : prm.Length;

		var angleMain = 2.0 * XMath.PI * candleIdx / period;
		var angleLead = 2.0 * XMath.PI * (candleIdx + 0.5) / period;

		flatResults[resIndex] = new GpuSineWaveResult
		{
			Time = candle.Time,
			Main = (float)XMath.Sin(angleMain),
			Lead = (float)XMath.Sin(angleLead),
			IsFormed = (byte)((candleIdx + 1) >= period ? 1 : 0),
		};
	}
}

namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Vortex indicator calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuVortexParams"/> struct.
/// </remarks>
/// <param name="length">Vortex window length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuVortexParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// Vortex window length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is VortexIndicator vortex)
		{
			Unsafe.AsRef(in this).Length = vortex.Length;
		}
	}
}

/// <summary>
/// Complex GPU result for Vortex indicator calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuVortexResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// +VI value.
	/// </summary>
	public float PlusVi;

	/// <summary>
	/// -VI value.
	/// </summary>
	public float MinusVi;

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

		var vortex = (VortexIndicator)indicator;

		if (PlusVi.IsNaN() || MinusVi.IsNaN())
		{
			return new VortexIndicatorValue(vortex, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		var value = new VortexIndicatorValue(vortex, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		value.Add(vortex.PlusVi, new DecimalIndicatorValue(vortex.PlusVi, (decimal)PlusVi, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		value.Add(vortex.MinusVi, new DecimalIndicatorValue(vortex.MinusVi, (decimal)MinusVi, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		return value;
	}
}

/// <summary>
/// GPU calculator for Vortex indicator.
/// </summary>
public class GpuVortexCalculator : GpuIndicatorCalculatorBase<VortexIndicator, GpuVortexParams, GpuVortexResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuVortexResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuVortexParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuVortexCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuVortexCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
				<Index2D, ArrayView<GpuCandle>, ArrayView<GpuVortexResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuVortexParams>>(VortexParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuVortexResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuVortexParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuVortexResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuVortexResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuVortexResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuVortexResult[len];
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
	/// ILGPU kernel: Vortex computation for multiple series and multiple parameter sets.
	/// One thread handles one (parameter, series) pair and iterates bars sequentially.
	/// </summary>
	private static void VortexParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuVortexResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuVortexParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var L = parameters[paramIdx].Length;
		if (L <= 0)
			L = 1;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var resIndex = paramIdx * flatCandles.Length + (offset + i);

			flatResults[resIndex] = new GpuVortexResult
			{
				Time = candle.Time,
				PlusVi = float.NaN,
				MinusVi = float.NaN,
				IsFormed = 0,
			};

			if (i == 0)
				continue;

			var sumTr = 0f;
			var sumVmPlus = 0f;
			var sumVmMinus = 0f;
			var count = 0;

			for (var j = 0; j < L; j++)
			{
				var idx = i - j;
				if (idx <= 0)
					break;

				var current = flatCandles[offset + idx];
				var previous = flatCandles[offset + idx - 1];

				var tr1 = current.High - current.Low;
				var tr2 = MathF.Abs(current.High - previous.Close);
				var tr3 = MathF.Abs(current.Low - previous.Close);
				var tr = MathF.Max(tr1, MathF.Max(tr2, tr3));

				sumTr += tr;
				sumVmPlus += MathF.Abs(current.High - previous.Low);
				sumVmMinus += MathF.Abs(current.Low - previous.High);
				count++;
			}

			if (count >= L)
			{
				var plusVi = sumTr > 0f ? sumVmPlus / sumTr : 0f;
				var minusVi = sumTr > 0f ? sumVmMinus / sumTr : 0f;

				flatResults[resIndex] = new GpuVortexResult
				{
					Time = candle.Time,
					PlusVi = plusVi,
					MinusVi = minusVi,
					IsFormed = 1,
				};
			}
		}
	}
}

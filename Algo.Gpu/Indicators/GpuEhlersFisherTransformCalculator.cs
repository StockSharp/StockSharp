namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Ehlers Fisher Transform calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuEhlersFisherTransformParams"/> struct.
/// </remarks>
/// <param name="length">Lookback length for high/low range.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuEhlersFisherTransformParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// Ehlers Fisher Transform lookback length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is EhlersFisherTransform eft)
		{
			Unsafe.AsRef(in this).Length = eft.Length;
		}
	}
}

/// <summary>
/// GPU result for Ehlers Fisher Transform calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuEhlersFisherTransformResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Main line value.
	/// </summary>
	public float MainLine;

	/// <summary>
	/// Trigger line value.
	/// </summary>
	public float TriggerLine;

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
		var eft = (EhlersFisherTransform)indicator;

		if (MainLine.IsNaN() || TriggerLine.IsNaN())
		{
			return new EhlersFisherTransformValue(eft, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		var result = new EhlersFisherTransformValue(eft, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		result.Add(eft.MainLine, new DecimalIndicatorValue(eft.MainLine, (decimal)MainLine, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		result.Add(eft.TriggerLine, new DecimalIndicatorValue(eft.TriggerLine, (decimal)TriggerLine, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		return result;
	}
}

/// <summary>
/// GPU calculator for Ehlers Fisher Transform.
/// </summary>
public class GpuEhlersFisherTransformCalculator : GpuIndicatorCalculatorBase<EhlersFisherTransform, GpuEhlersFisherTransformParams, GpuEhlersFisherTransformResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuEhlersFisherTransformResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuEhlersFisherTransformParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuEhlersFisherTransformCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuEhlersFisherTransformCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuEhlersFisherTransformResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuEhlersFisherTransformParams>>(EhlersFisherTransformParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuEhlersFisherTransformResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuEhlersFisherTransformParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuEhlersFisherTransformResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();
		var result = new GpuEhlersFisherTransformResult[seriesCount][][];

		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuEhlersFisherTransformResult[parameters.Length][];

			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuEhlersFisherTransformResult[len];

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
	/// ILGPU kernel: computes Ehlers Fisher Transform for each series/parameter pair.
	/// </summary>
	private static void EhlersFisherTransformParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuEhlersFisherTransformResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuEhlersFisherTransformParams> parameters)
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

		var prevValue = 0f;
		var currValue = 0f;
		var totalCandles = flatCandles.Length;

		for (var i = 0; i < len; i++)
		{
			var candleIndex = offset + i;
			var candle = flatCandles[candleIndex];
			var resIndex = paramIdx * totalCandles + candleIndex;

			var result = new GpuEhlersFisherTransformResult
			{
				Time = candle.Time,
				MainLine = float.NaN,
				TriggerLine = float.NaN,
				IsFormed = 0
			};

			if (i >= L - 1)
			{
				var start = i - (L - 1);
				if (start < 0)
					start = 0;

				var maxHigh = float.MinValue;
				var minLow = float.MaxValue;

				for (var j = start; j <= i; j++)
				{
					var c = flatCandles[offset + j];
					if (c.High > maxHigh)
						maxHigh = c.High;
					if (c.Low < minLow)
						minLow = c.Low;
				}

				var diff = maxHigh - minLow;
				if (diff <= 0f)
					diff = 1e-6f;

				var median = 0.5f * (candle.High + candle.Low);
				var normalized = ((median - minLow) / diff) - 0.5f;
				var value = 0.5f * normalized;
				value = 0.66f * value + 0.67f * prevValue;

				if (value > 0.999f)
					value = 0.999f;
				else if (value < -0.999f)
					value = -0.999f;

				var fisher = 0.5f * MathF.Log((1f + value) / (1f - value));

				result.MainLine = fisher;
				result.TriggerLine = currValue;
				result.IsFormed = 1;

				prevValue = value;
				currValue = fisher;
			}

			flatResults[resIndex] = result;
		}
	}
}

namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Envelope calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuEnvelopeParams"/> struct.
/// </remarks>
/// <param name="length">Moving average length used for the middle line.</param>
/// <param name="shift">Shift coefficient expressed as fraction (e.g. 0.01 = 1%).</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuEnvelopeParams(int length, float shift, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Window length for the middle moving average.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// Shift coefficient (0..1) applied to the middle line.
	/// </summary>
	public float Shift = shift;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is Envelope envelope)
		{
			Unsafe.AsRef(in this).Length = envelope.Length;
			Unsafe.AsRef(in this).Shift = (float)envelope.Shift;
		}
	}
}

/// <summary>
/// Complex GPU result for Envelope calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuEnvelopeResult : IGpuIndicatorResult
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
	/// Upper line value.
	/// </summary>
	public float Upper;

	/// <summary>
	/// Lower line value.
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

		var envelope = (Envelope)indicator;

		if (Middle.IsNaN() || Upper.IsNaN() || Lower.IsNaN())
		{
			return new EnvelopeValue(envelope, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		var value = new EnvelopeValue(envelope, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		value.SetInnerDecimal(envelope.Middle, time, (decimal)Middle, true);
		value.SetInnerDecimal(envelope.Upper, time, (decimal)Upper, true);
		value.SetInnerDecimal(envelope.Lower, time, (decimal)Lower, true);

		return value;
	}
}

/// <summary>
/// GPU calculator for Envelope indicator.
/// </summary>
public class GpuEnvelopeCalculator : GpuIndicatorCalculatorBase<Envelope, GpuEnvelopeParams, GpuEnvelopeResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuEnvelopeResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuEnvelopeParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuEnvelopeCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuEnvelopeCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index3D, ArrayView<GpuCandle>, ArrayView<GpuEnvelopeResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuEnvelopeParams>>(EnvelopeParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuEnvelopeResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuEnvelopeParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuEnvelopeResult>(totalSize * parameters.Length);

		var extent = new Index3D(parameters.Length, seriesCount, maxLen);
		_paramsSeriesKernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		// Re-split [series][param][bar]
		var result = new GpuEnvelopeResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuEnvelopeResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuEnvelopeResult[len];
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
	/// ILGPU kernel: Envelope computation for multiple series and parameter sets. Results stored as [param][globalIdx].
	/// </summary>
	private static void EnvelopeParamsSeriesKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuEnvelopeResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuEnvelopeParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;
		var candleIdx = index.Z;

		var len = lengths[seriesIdx];
		if (candleIdx >= len)
			return;

		var offset = offsets[seriesIdx];
		var globalIdx = offset + candleIdx;

		var resIndex = paramIdx * flatCandles.Length + globalIdx;
		var candle = flatCandles[globalIdx];

		flatResults[resIndex] = new GpuEnvelopeResult
		{
			Time = candle.Time,
			Middle = float.NaN,
			Upper = float.NaN,
			Lower = float.NaN,
			IsFormed = 0
		};

		var prm = parameters[paramIdx];
		var length = prm.Length <= 0 ? 1 : prm.Length;

		if (candleIdx < length - 1)
			return;

		var priceType = (Level1Fields)prm.PriceType;
		var sum = 0f;
		for (var j = 0; j < length; j++)
			sum += ExtractPrice(flatCandles[globalIdx - j], priceType);

		var middle = sum / length;
		var shift = prm.Shift;
		var upper = middle * (1f + shift);
		var lower = middle * (1f - shift);

		flatResults[resIndex] = new GpuEnvelopeResult
		{
			Time = candle.Time,
			Middle = middle,
			Upper = upper,
			Lower = lower,
			IsFormed = 1
		};
	}
}

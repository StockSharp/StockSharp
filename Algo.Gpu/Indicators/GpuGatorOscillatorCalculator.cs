namespace StockSharp.Algo.Gpu.Indicators;

using System.Reflection;

/// <summary>
/// Parameter set for GPU Gator Oscillator calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuGatorOscillatorParams"/> struct.
/// </remarks>
/// <param name="jawLength">Jaw SMMA length.</param>
/// <param name="jawShift">Jaw forward shift.</param>
/// <param name="teethLength">Teeth SMMA length.</param>
/// <param name="teethShift">Teeth forward shift.</param>
/// <param name="lipsLength">Lips SMMA length.</param>
/// <param name="lipsShift">Lips forward shift.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuGatorOscillatorParams(int jawLength, int jawShift, int teethLength, int teethShift, int lipsLength, int lipsShift) : IGpuIndicatorParams
{
	private static readonly FieldInfo _line1Field = typeof(GatorHistogram).GetField("_line1", BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly FieldInfo _line2Field = typeof(GatorHistogram).GetField("_line2", BindingFlags.Instance | BindingFlags.NonPublic);

	/// <summary>
	/// Jaw SMMA period length.
	/// </summary>
	public int JawLength = jawLength;

	/// <summary>
	/// Jaw shift to the future.
	/// </summary>
	public int JawShift = jawShift;

	/// <summary>
	/// Teeth SMMA period length.
	/// </summary>
	public int TeethLength = teethLength;

	/// <summary>
	/// Teeth shift to the future.
	/// </summary>
	public int TeethShift = teethShift;

	/// <summary>
	/// Lips SMMA period length.
	/// </summary>
	public int LipsLength = lipsLength;

	/// <summary>
	/// Lips shift to the future.
	/// </summary>
	public int LipsShift = lipsShift;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is not GatorOscillator gator)
			return;

		ref var self = ref Unsafe.AsRef(in this);

		var histogram1 = gator.Histogram1;
		var histogram2 = gator.Histogram2;

		if (_line1Field?.GetValue(histogram1) is AlligatorLine jaw)
		{
			self.JawLength = jaw.Length;
			self.JawShift = jaw.Shift;
		}

		if (_line2Field?.GetValue(histogram1) is AlligatorLine lips)
		{
			self.LipsLength = lips.Length;
			self.LipsShift = lips.Shift;
		}

		if (_line2Field?.GetValue(histogram2) is AlligatorLine teeth)
		{
			self.TeethLength = teeth.Length;
			self.TeethShift = teeth.Shift;
		}
	}
}

/// <summary>
/// GPU result for Gator Oscillator calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuGatorOscillatorResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTimeOffset.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Upper histogram value (Jaw vs Lips).
	/// </summary>
	public float UpperHistogram;

	/// <summary>
	/// Lower histogram value (Lips vs Teeth).
	/// </summary>
	public float LowerHistogram;

	/// <summary>
	/// Indicator formed flag stored as byte.
	/// </summary>
	public byte IsFormed;

	readonly long IGpuIndicatorResult.Time => Time;
	readonly byte IGpuIndicatorResult.IsFormed => IsFormed;

	/// <inheritdoc />
	public readonly IIndicatorValue ToValue(IIndicator indicator)
	{
		var time = this.GetTime();
		var isFormed = this.GetIsFormed();
		var gator = (GatorOscillator)indicator;

		var value = new GatorOscillatorValue(gator, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		value.Add(gator.Histogram1, CreateHistogramValue(gator.Histogram1, UpperHistogram, time, isFormed));
		value.Add(gator.Histogram2, CreateHistogramValue(gator.Histogram2, LowerHistogram, time, isFormed));

		return value;
	}

	private static IIndicatorValue CreateHistogramValue(GatorHistogram histogram, float data, DateTimeOffset time, bool isFormed)
	{
		if (float.IsNaN(data))
		{
			return new DecimalIndicatorValue(histogram, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		return new DecimalIndicatorValue(histogram, (decimal)data, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};
	}
}

/// <summary>
/// GPU calculator for <see cref="GatorOscillator"/> indicator.
/// </summary>
public class GpuGatorOscillatorCalculator : GpuIndicatorCalculatorBase<GatorOscillator, GpuGatorOscillatorParams, GpuGatorOscillatorResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuGatorOscillatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuGatorOscillatorParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuGatorOscillatorCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuGatorOscillatorCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index3D, ArrayView<GpuCandle>, ArrayView<GpuGatorOscillatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuGatorOscillatorParams>>(CalculateKernel);
	}

	/// <inheritdoc />
	public override GpuGatorOscillatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuGatorOscillatorParams[] parameters)
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
		var maxLen = 0;

		for (var s = 0; s < seriesCount; s++)
		{
			seriesOffsets[s] = totalSize;
			var len = candlesSeries[s]?.Length ?? 0;
			seriesLengths[s] = len;
			totalSize += len;
			if (len > maxLen)
				maxLen = len;
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
		using var outputBuffer = Accelerator.Allocate1D<GpuGatorOscillatorResult>(totalSize * parameters.Length);

		var extent = new Index3D(parameters.Length, seriesCount, maxLen);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuGatorOscillatorResult[seriesCount][][];

		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuGatorOscillatorResult[parameters.Length][];

			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuGatorOscillatorResult[len];

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

	private static void CalculateKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuGatorOscillatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuGatorOscillatorParams> parameters)
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

		var upper = float.NaN;
		var lower = float.NaN;
		byte formed = 0;

		var jawReady = TryCalculateLine(flatCandles, offset, candleIdx, len, prm.JawLength, prm.JawShift, out var jawValue);
		var lipsReady = TryCalculateLine(flatCandles, offset, candleIdx, len, prm.LipsLength, prm.LipsShift, out var lipsValue);
		var teethReady = TryCalculateLine(flatCandles, offset, candleIdx, len, prm.TeethLength, prm.TeethShift, out var teethValue);

		if (jawReady && lipsReady)
			upper = XMath.Abs(jawValue - lipsValue);

		if (lipsReady && teethReady)
			lower = -XMath.Abs(lipsValue - teethValue);

		if (jawReady && lipsReady && teethReady)
			formed = 1;

		flatResults[resIndex] = new GpuGatorOscillatorResult
		{
			Time = candle.Time,
			UpperHistogram = upper,
			LowerHistogram = lower,
			IsFormed = formed,
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryCalculateLine(
		ArrayView<GpuCandle> flatCandles,
		int seriesOffset,
		int candleIdx,
		int seriesLength,
		int length,
		int shift,
		out float value)
	{
		if (length <= 0)
		{
			value = float.NaN;
			return false;
		}

		var idx = candleIdx - shift;

		if (idx < 0)
		{
			value = float.NaN;
			return false;
		}

		if (idx >= seriesLength)
		{
			value = float.NaN;
			return false;
		}

		var smma = 0f;
		var sum = 0f;

		for (var i = 0; i <= idx; i++)
		{
			var price = (flatCandles[seriesOffset + i].High + flatCandles[seriesOffset + i].Low) * 0.5f;

			if (i < length)
			{
				sum += price;
				smma = sum / length;
			}
			else
			{
				smma = (smma * (length - 1) + price) / length;
			}
		}

		value = smma;

		return idx >= length - 1;
	}
}

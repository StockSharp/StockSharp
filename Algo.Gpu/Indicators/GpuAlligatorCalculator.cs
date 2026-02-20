namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Alligator calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuAlligatorParams"/> struct.
/// </remarks>
/// <param name="jawLength">Jaw length.</param>
/// <param name="jawShift">Jaw shift.</param>
/// <param name="teethLength">Teeth length.</param>
/// <param name="teethShift">Teeth shift.</param>
/// <param name="lipsLength">Lips length.</param>
/// <param name="lipsShift">Lips shift.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuAlligatorParams(int jawLength, int jawShift, int teethLength, int teethShift, int lipsLength, int lipsShift) : IGpuIndicatorParams
{
	/// <summary>
	/// Jaw line length.
	/// </summary>
	public int JawLength = jawLength;

	/// <summary>
	/// Jaw line shift.
	/// </summary>
	public int JawShift = jawShift;

	/// <summary>
	/// Teeth line length.
	/// </summary>
	public int TeethLength = teethLength;

	/// <summary>
	/// Teeth line shift.
	/// </summary>
	public int TeethShift = teethShift;

	/// <summary>
	/// Lips line length.
	/// </summary>
	public int LipsLength = lipsLength;

	/// <summary>
	/// Lips line shift.
	/// </summary>
	public int LipsShift = lipsShift;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is not Alligator alligator)
			return;

		Unsafe.AsRef(in this).JawLength = alligator.Jaw.Length;
		Unsafe.AsRef(in this).TeethLength = alligator.Teeth.Length;
		Unsafe.AsRef(in this).LipsLength = alligator.Lips.Length;

		Unsafe.AsRef(in this).JawShift = alligator.Jaw.Shift;
		Unsafe.AsRef(in this).TeethShift = alligator.Teeth.Shift;
		Unsafe.AsRef(in this).LipsShift = alligator.Lips.Shift;
	}
}

/// <summary>
/// GPU result for Alligator indicator.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuAlligatorResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Jaw value.
	/// </summary>
	public float Jaw;

	/// <summary>
	/// Teeth value.
	/// </summary>
	public float Teeth;

	/// <summary>
	/// Lips value.
	/// </summary>
	public float Lips;

	/// <summary>
	/// Is indicator formed (byte to be GPU-friendly).
	/// </summary>
	public byte IsFormed;

	readonly long IGpuIndicatorResult.Time => Time;
	readonly byte IGpuIndicatorResult.IsFormed => IsFormed;

	/// <inheritdoc />
	public readonly IIndicatorValue ToValue(IIndicator indicator)
	{
		var alligator = (Alligator)indicator;
		var time = this.GetTime();
		var isFormed = this.GetIsFormed();

		var value = new AlligatorValue(alligator, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
			IsEmpty = Jaw.IsNaN() && Teeth.IsNaN() && Lips.IsNaN(),
		};

		value.Add(alligator.Jaw, CreateInnerValue(alligator.Jaw, Jaw, time, isFormed));
		value.Add(alligator.Teeth, CreateInnerValue(alligator.Teeth, Teeth, time, isFormed));
		value.Add(alligator.Lips, CreateInnerValue(alligator.Lips, Lips, time, isFormed));

		return value;
	}

	private static IIndicatorValue CreateInnerValue(AlligatorLine line, float rawValue, DateTime time, bool parentIsFormed)
	{
		if (rawValue.IsNaN())
		{
			return new DecimalIndicatorValue(line, time)
			{
				IsFinal = true,
				IsFormed = false,
			};
		}

		return new DecimalIndicatorValue(line, (decimal)rawValue, time)
		{
			IsFinal = true,
			IsFormed = parentIsFormed,
		};
	}
}

/// <summary>
/// GPU calculator for the Alligator indicator.
/// </summary>
public class GpuAlligatorCalculator : GpuIndicatorCalculatorBase<Alligator, GpuAlligatorParams, GpuAlligatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuAlligatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuAlligatorParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuAlligatorCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuAlligatorCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuAlligatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuAlligatorParams>>(AlligatorKernel);
	}

	/// <inheritdoc />
	public override GpuAlligatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuAlligatorParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuAlligatorResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuAlligatorResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuAlligatorResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuAlligatorResult[len];
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
	/// ILGPU kernel computing Alligator lines for multiple series and parameter sets.
	/// </summary>
	private static void AlligatorKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuAlligatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuAlligatorParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];

		var jawLength = XMath.Max(prm.JawLength, 1);
		var teethLength = XMath.Max(prm.TeethLength, 1);
		var lipsLength = XMath.Max(prm.LipsLength, 1);

		var jawShift = XMath.Max(prm.JawShift, 0);
		var teethShift = XMath.Max(prm.TeethShift, 0);
		var lipsShift = XMath.Max(prm.LipsShift, 0);

		var total = flatCandles.Length;
		var baseIndex = paramIdx * total + offset;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			flatResults[baseIndex + i] = new GpuAlligatorResult
			{
				Time = candle.Time,
				Jaw = float.NaN,
				Teeth = float.NaN,
				Lips = float.NaN,
				IsFormed = 0,
			};
		}

		float jawSum = 0f, teethSum = 0f, lipsSum = 0f;
		float jawSmma = 0f, teethSmma = 0f, lipsSmma = 0f;
		bool jawHasSmma = false, teethHasSmma = false, lipsHasSmma = false;
		byte prevFormed = 0;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var median = (candle.High + candle.Low) * 0.5f;

			jawSum += median;
			if (i == jawLength - 1)
			{
				jawSmma = jawSum / jawLength;
				jawHasSmma = true;
			}
			else if (i >= jawLength)
			{
				jawSmma = ((jawSmma * (jawLength - 1)) + median) / jawLength;
			}

			if (jawHasSmma)
			{
				var target = i + jawShift;
				if (target < len)
				{
					var idx = baseIndex + target;
					var res = flatResults[idx];
					res.Jaw = jawSmma;
					flatResults[idx] = res;
				}
			}

			teethSum += median;
			if (i == teethLength - 1)
			{
				teethSmma = teethSum / teethLength;
				teethHasSmma = true;
			}
			else if (i >= teethLength)
			{
				teethSmma = ((teethSmma * (teethLength - 1)) + median) / teethLength;
			}

			if (teethHasSmma)
			{
				var target = i + teethShift;
				if (target < len)
				{
					var idx = baseIndex + target;
					var res = flatResults[idx];
					res.Teeth = teethSmma;
					flatResults[idx] = res;
				}
			}

			lipsSum += median;
			if (i == lipsLength - 1)
			{
				lipsSmma = lipsSum / lipsLength;
				lipsHasSmma = true;
			}
			else if (i >= lipsLength)
			{
				lipsSmma = ((lipsSmma * (lipsLength - 1)) + median) / lipsLength;
			}

			if (lipsHasSmma)
			{
				var target = i + lipsShift;
				if (target < len)
				{
					var idx = baseIndex + target;
					var res = flatResults[idx];
					res.Lips = lipsSmma;
					flatResults[idx] = res;
				}
			}

			var currentIdx = baseIndex + i;
			var current = flatResults[currentIdx];
			current.IsFormed = prevFormed;
			flatResults[currentIdx] = current;
			prevFormed = (byte)(i >= jawLength - 1 + jawShift ? 1 : 0);
		}
	}
}

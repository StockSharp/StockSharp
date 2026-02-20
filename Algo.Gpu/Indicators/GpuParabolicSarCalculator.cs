namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Parabolic SAR calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuParabolicSarParams"/> struct.
/// </remarks>
/// <param name="acceleration">Initial acceleration factor.</param>
/// <param name="accelerationStep">Acceleration factor step.</param>
/// <param name="accelerationMax">Maximum acceleration factor.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuParabolicSarParams(float acceleration, float accelerationStep, float accelerationMax) : IGpuIndicatorParams
{
	/// <summary>
	/// Initial acceleration factor.
	/// </summary>
	public float Acceleration = acceleration;

	/// <summary>
	/// Acceleration factor step.
	/// </summary>
	public float AccelerationStep = accelerationStep;

	/// <summary>
	/// Maximum acceleration factor.
	/// </summary>
	public float AccelerationMax = accelerationMax;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is ParabolicSar sar)
		{
			Unsafe.AsRef(in this).Acceleration = (float)sar.Acceleration;
			Unsafe.AsRef(in this).AccelerationStep = (float)sar.AccelerationStep;
			Unsafe.AsRef(in this).AccelerationMax = (float)sar.AccelerationMax;
		}
	}
}

/// <summary>
/// GPU calculator for Parabolic SAR.
/// </summary>
public class GpuParabolicSarCalculator : GpuIndicatorCalculatorBase<ParabolicSar, GpuParabolicSarParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuParabolicSarParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuParabolicSarCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuParabolicSarCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuParabolicSarParams>>(ParabolicSarParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuParabolicSarParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuIndicatorResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuIndicatorResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuIndicatorResult[len];
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

	private static void ParabolicSarParamsSeriesKernel(
	Index2D index,
	ArrayView<GpuCandle> flatCandles,
	ArrayView<GpuIndicatorResult> flatResults,
	ArrayView<int> offsets,
	ArrayView<int> lengths,
	ArrayView<GpuParabolicSarParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
		return;

		var prm = parameters[paramIdx];
		var acceleration = prm.Acceleration;
		var accelerationStep = prm.AccelerationStep;
		var accelerationMax = prm.AccelerationMax;

		var prevValue = 0f;
		var longPosition = false;
		var xp = 0f;
		var af = 0f;
		var prevBar = 0;
		var afIncreased = false;
		var reverseBar = 0;
		var reverseValue = 0f;
		var prevSar = 0f;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var resIndex = paramIdx * flatCandles.Length + globalIdx;

			var result = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 1
			};

			var count = i + 1;
			if (count < 2)
			{
				flatResults[resIndex] = result;
				continue;
			}

			float value;

			if (count == 2)
			{
				// CPU adds first candle twice, so at bar 1 the CPU list is [c0, c0, c1].
				// Emulate by using the previous candle for both prev1 and prev2.
				var prev1 = flatCandles[globalIdx - 1];
				var prev2 = prev1;

				longPosition = candle.High > prev1.High;
				var maxHigh = MathF.Max(candle.High, MathF.Max(prev1.High, prev2.High));
				var minLow = MathF.Min(candle.Low, MathF.Min(prev1.Low, prev2.Low));
				xp = longPosition ? maxHigh : minLow;
				af = acceleration;
				value = xp + (longPosition ? -1f : 1f) * (maxHigh - minLow) * af;

				prevValue = value;
				result.Value = value;
				result.IsFormed = 1;
				flatResults[resIndex] = result;
				continue;
			}

			if (afIncreased && prevBar != count)
				afIncreased = false;

			value = prevValue;

			if (reverseBar != count)
			{
				var todaySar = prevValue + af * (xp - prevValue);
				todaySar = TodaySar(flatCandles, offset, i, ref longPosition, ref reverseBar, ref reverseValue, ref af, ref xp, ref prevSar, prevBar, acceleration, todaySar);

				for (var x = 1; x <= 2; x++)
				{
					var t = flatCandles[globalIdx - x];

					if (longPosition)
					{
						if (todaySar > t.Low)
						todaySar = t.Low;
					}
					else
					{
						if (todaySar < t.High)
						todaySar = t.High;
					}
				}

				var prevCandle = flatCandles[globalIdx - 1];
				var needReverse = (longPosition && (candle.Low < todaySar || prevCandle.Low < todaySar))
				|| (!longPosition && (candle.High > todaySar || prevCandle.High > todaySar));

				if (needReverse)
				{
					value = Reverse(ref longPosition, ref reverseBar, ref reverseValue, ref af, ref xp, ref prevSar, prevBar, count, acceleration, candle.High, candle.Low);
					prevValue = value;
					result.Value = value;
					result.IsFormed = 1;
					flatResults[resIndex] = result;
					continue;
				}

				if (longPosition)
				{
					if (prevBar != count || candle.Low < prevSar)
					{
						value = todaySar;
						prevSar = todaySar;
					}
					else
					{
						value = prevSar;
					}

					if (candle.High > xp)
					{
						xp = candle.High;
						IncreaseAcceleration(ref af, ref afIncreased, accelerationMax, accelerationStep);
					}
				}
				else
				{
					if (prevBar != count || candle.High > prevSar)
					{
						value = todaySar;
						prevSar = todaySar;
					}
					else
					{
						value = prevSar;
					}

					if (candle.Low < xp)
					{
						xp = candle.Low;
						IncreaseAcceleration(ref af, ref afIncreased, accelerationMax, accelerationStep);
					}
				}
			}
			else
			{
				if (longPosition && candle.High > xp)
					xp = candle.High;
				else if (!longPosition && candle.Low < xp)
					xp = candle.Low;

				value = prevSar;

				var baseSar = longPosition ? MathF.Min(reverseValue, candle.Low) : MathF.Max(reverseValue, candle.High);
				_ = TodaySar(flatCandles, offset, i, ref longPosition, ref reverseBar, ref reverseValue, ref af, ref xp, ref prevSar, prevBar, acceleration, baseSar);
			}

			prevBar = count;
			prevValue = value;

			result.Value = value;
			result.IsFormed = 1;
			flatResults[resIndex] = result;
		}
	}

	private static float Reverse(
	ref bool longPosition,
	ref int reverseBar,
	ref float reverseValue,
	ref float af,
	ref float xp,
	ref float prevSar,
	int prevBar,
	int count,
	float acceleration,
	float currentHigh,
	float currentLow)
	{
		var todaySar = xp;

		if ((longPosition && prevSar > currentLow)
		|| (!longPosition && prevSar < currentHigh)
		|| prevBar != count)
		{
			longPosition = !longPosition;
			reverseBar = count;
			reverseValue = xp;
			af = acceleration;
			xp = longPosition ? currentHigh : currentLow;
			prevSar = todaySar;
		}
		else
		{
			todaySar = prevSar;
		}

		return todaySar;
	}

	private static float TodaySar(
	ArrayView<GpuCandle> candles,
	int offset,
	int index,
	ref bool longPosition,
	ref int reverseBar,
	ref float reverseValue,
	ref float af,
	ref float xp,
	ref float prevSar,
	int prevBar,
	float acceleration,
	float todaySar)
	{
		var current = candles[offset + index];
		var previous = candles[offset + index - 1];

		if (longPosition)
		{
			var lowestSar = MathF.Min(MathF.Min(todaySar, current.Low), previous.Low);
			if (current.Low > lowestSar)
			return lowestSar;

			return Reverse(ref longPosition, ref reverseBar, ref reverseValue, ref af, ref xp, ref prevSar, prevBar, index + 1, acceleration, current.High, current.Low);
		}

		var highestSar = MathF.Max(MathF.Max(todaySar, current.High), previous.High);
		if (current.High < highestSar)
		return highestSar;

		return Reverse(ref longPosition, ref reverseBar, ref reverseValue, ref af, ref xp, ref prevSar, prevBar, index + 1, acceleration, current.High, current.Low);
	}

	private static void IncreaseAcceleration(ref float af, ref bool afIncreased, float accelerationMax, float accelerationStep)
	{
		if (afIncreased)
		return;

		af = MathF.Min(accelerationMax, af + accelerationStep);
		afIncreased = true;
	}
}

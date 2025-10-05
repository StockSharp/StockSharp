namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU ADX calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuAdxParams"/> struct.
/// </remarks>
/// <param name="length">ADX length (used for Wilder smoothing).</param>
/// <param name="priceType">Price type for smoothing.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuAdxParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// ADX period length.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// Price type to extract for smoothing part (MA over DX). Defaults to Close.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is AverageDirectionalIndex adx)
		{
			Unsafe.AsRef(in this).Length = adx.Length;
		}
	}
}

/// <summary>
/// Complex GPU result for ADX calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuAdxResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTimeOffset.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// ADX value.
	/// </summary>
	public float Adx;

	/// <summary>
	/// +DI value.
	/// </summary>
	public float PlusDi;

	/// <summary>
	/// -DI value.
	/// </summary>
	public float MinusDi;

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

		var adxInd = (AverageDirectionalIndex)indicator;

		if (Adx.IsNaN() || PlusDi.IsNaN() || MinusDi.IsNaN())
		{
			return new AverageDirectionalIndexValue(adxInd, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		var adxVal = new AverageDirectionalIndexValue(adxInd, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		adxVal.Add(adxInd.MovingAverage, new DecimalIndicatorValue(adxInd.MovingAverage, (decimal)Adx, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		var dxInd = adxInd.Dx;

		var dxVal = new DirectionalIndexValue(dxInd, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		dxVal.Add(dxInd.Plus, new DecimalIndicatorValue(dxInd.Plus, (decimal)PlusDi, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		dxVal.Add(dxInd.Minus, new DecimalIndicatorValue(dxInd.Minus, (decimal)MinusDi, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		adxVal.Add(dxInd, dxVal);

		return adxVal;
	}
}

/// <summary>
/// GPU calculator for Average Directional Index (ADX).
/// </summary>
public class GpuAdxCalculator : GpuIndicatorCalculatorBase<AverageDirectionalIndex, GpuAdxParams, GpuAdxResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuAdxResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuAdxParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuAdxCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuAdxCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuAdxResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuAdxParams>>(AdxParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuAdxResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuAdxParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuAdxResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		// Re-split [series][param][bar]
		var result = new GpuAdxResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuAdxResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuAdxResult[len];
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
	/// ILGPU kernel: ADX computation for multiple series and multiple parameter sets.
	/// One thread handles one (parameter, series) pair and iterates bars sequentially.
	/// </summary>
	private static void AdxParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuAdxResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuAdxParams> parameters)
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

		// Wilder smoothing accumulators
		float trSum = 0f, dmPlusSum = 0f, dmMinusSum = 0f;
		float trSmooth = 0f, dmPlusSmooth = 0f, dmMinusSmooth = 0f;
		float prevClose = flatCandles[offset].Close;
		float prevHigh = flatCandles[offset].High;
		float prevLow = flatCandles[offset].Low;

		// ADX accumulators
		float dxSum = 0f;
		float adxPrev = 0f;

		for (int i = 0; i < len; i++)
		{
			var c = flatCandles[offset + i];
			var high = c.High;
			var low = c.Low;
			var close = c.Close;

			float upMove = high - prevHigh;
			float downMove = prevLow - low;
			float plusDM = (upMove > downMove && upMove > 0f) ? upMove : 0f;
			float minusDM = (downMove > upMove && downMove > 0f) ? downMove : 0f;

			float tr1 = high - low;
			float tr2 = MathF.Abs(high - prevClose);
			float tr3 = MathF.Abs(low - prevClose);
			float tr = MathF.Max(tr1, MathF.Max(tr2, tr3));

			// Default result: not formed
			var resIndex = paramIdx * flatCandles.Length + (offset + i);
			flatResults[resIndex] = new GpuAdxResult
			{
				Time = c.Time,
				Adx = float.NaN,
				PlusDi = float.NaN,
				MinusDi = float.NaN,
				IsFormed = 0
			};

			if (i < L)
			{
				trSum += tr;
				dmPlusSum += plusDM;
				dmMinusSum += minusDM;
			}

			if (i == L - 1)
			{
				trSmooth = trSum;
				dmPlusSmooth = dmPlusSum;
				dmMinusSmooth = dmMinusSum;
			}
			else if (i >= L)
			{
				trSmooth = trSmooth - (trSmooth / L) + tr;
				dmPlusSmooth = dmPlusSmooth - (dmPlusSmooth / L) + plusDM;
				dmMinusSmooth = dmMinusSmooth - (dmMinusSmooth / L) + minusDM;
			}

			if (i >= L - 1)
			{
				var plusDi = trSmooth > 0f ? 100f * (dmPlusSmooth / trSmooth) : 0f;
				var minusDi = trSmooth > 0f ? 100f * (dmMinusSmooth / trSmooth) : 0f;
				var diSum = plusDi + minusDi;
				var diDiff = MathF.Abs(plusDi - minusDi);
				var dx = diSum > 0f ? (100f * diDiff / diSum) : 0f;

				if (i < (2 * L - 1))
				{
					dxSum += dx;
					if (i == (2 * L - 2))
					{
						adxPrev = dxSum / L;
						flatResults[resIndex] = new GpuAdxResult { Time = c.Time, Adx = adxPrev, PlusDi = plusDi, MinusDi = minusDi, IsFormed = 1 };
					}
				}
				else
				{
					adxPrev = (adxPrev * (L - 1) + dx) / L;
					flatResults[resIndex] = new GpuAdxResult { Time = c.Time, Adx = adxPrev, PlusDi = plusDi, MinusDi = minusDi, IsFormed = 1 };
				}
			}

			prevClose = close;
			prevHigh = high;
			prevLow = low;
		}
	}
}
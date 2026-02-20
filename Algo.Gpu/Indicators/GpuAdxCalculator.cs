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
	/// Time in <see cref="DateTime.Ticks"/>.
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
	/// Matches CPU algorithm: DI via WilderMA, then ADX = WilderMA(DX) only after DX is formed.
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

		// WilderMA state for ATR (starts from bar 0)
		float atrValue = 0f;
		int atrCount = 0;

		// WilderMA state for +DM and -DM (starts from bar 1)
		float dmPlusMa = 0f;
		float dmMinusMa = 0f;
		int dmCount = 0;

		// WilderMA state for ADX (only starts when DX is formed, matching CPU sequence mode)
		float adxValue = 0f;
		int adxCount = 0;

		float prevClose = flatCandles[offset].Close;
		float prevHigh = flatCandles[offset].High;
		float prevLow = flatCandles[offset].Low;

		// DX formed state: DX is formed when DiPart.IsFormed = true for both Plus and Minus.
		// DiPart.IsFormed becomes true on bar L+1 (0-indexed): when ATR formed (bar>=L) AND DM formed (bar-1>=L).
		// DX.IsFormed is sticky once true.
		byte dxFormed = 0;

		byte prevFormed = 0;

		for (int i = 0; i < len; i++)
		{
			var c = flatCandles[offset + i];
			var high = c.High;
			var low = c.Low;
			var close = c.Close;

			// Compute TrueRange
			float tr;
			if (i == 0)
			{
				tr = high - low;
			}
			else
			{
				var tr1 = high - low;
				var tr2 = MathF.Abs(high - prevClose);
				var tr3 = MathF.Abs(low - prevClose);
				tr = MathF.Max(tr1, MathF.Max(tr2, tr3));
			}

			// WilderMA for ATR
			atrCount++;
			var atrN = atrCount < L ? atrCount : L;
			atrValue = (atrValue * (atrN - 1) + tr) / atrN;

			var resIndex = paramIdx * flatCandles.Length + (offset + i);

			if (i == 0)
			{
				// Bar 0: no previous candle for DM computation
				flatResults[resIndex] = new GpuAdxResult
				{
					Time = c.Time,
					Adx = float.NaN,
					PlusDi = float.NaN,
					MinusDi = float.NaN,
					IsFormed = prevFormed
				};
			}
			else
			{
				// Compute DM+ and DM-
				float upMove = high - prevHigh;
				float downMove = prevLow - low;
				float plusDM = (upMove > downMove && upMove > 0f) ? upMove : 0f;
				float minusDM = (downMove > upMove && downMove > 0f) ? downMove : 0f;

				// WilderMA for DM+ and DM-
				dmCount++;
				var dmN = dmCount < L ? dmCount : L;
				dmPlusMa = (dmPlusMa * (dmN - 1) + plusDM) / dmN;
				dmMinusMa = (dmMinusMa * (dmN - 1) + minusDM) / dmN;

				// Compute DI values
				var plusDi = atrValue > 0f ? 100f * dmPlusMa / atrValue : 0f;
				var minusDi = atrValue > 0f ? 100f * dmMinusMa / atrValue : 0f;

				var diSum = plusDi + minusDi;
				var diDiff = MathF.Abs(plusDi - minusDi);
				var dx = diSum > 0f ? (100f * diDiff / diSum) : 0f;

				// Check if DX becomes formed after processing this bar.
				// DiPart.IsFormed at bar k: ATR formed after bar k-1 (atrCount-1 >= L) AND DM formed after bar k-1 (dmCount-1 >= L)
				// Since atrCount = i+1 at this point, and dmCount = i at this point (for i>=1):
				// ATR formed after bar k-1: (i+1)-1 >= L, i.e., i >= L
				// DM formed after bar k-1: i-1 >= L, i.e., i >= L+1
				// Combined: i >= L+1
				if (dxFormed == 0 && atrCount >= L + 1 && dmCount >= L + 1)
					dxFormed = 1;

				// Only feed DX to ADX WilderMA when DX is formed (matching CPU sequence mode)
				if (dxFormed == 1)
				{
					adxCount++;
					var adxN = adxCount < L ? adxCount : L;
					adxValue = (adxValue * (adxN - 1) + dx) / adxN;

					flatResults[resIndex] = new GpuAdxResult
					{
						Time = c.Time,
						Adx = adxValue,
						PlusDi = plusDi,
						MinusDi = minusDi,
						IsFormed = prevFormed
					};
				}
				else
				{
					flatResults[resIndex] = new GpuAdxResult
					{
						Time = c.Time,
						Adx = float.NaN,
						PlusDi = plusDi,
						MinusDi = minusDi,
						IsFormed = prevFormed
					};
				}
			}

			// ADX overall formed: DX must be formed AND ADX MA must be formed
			// This matches CPU: CalcIsFormed = InnerIndicators.All(i => i.IsFormed)
			byte curFormed = (byte)(dxFormed == 1 && adxCount >= L ? 1 : 0);

			prevFormed = curFormed;
			prevClose = close;
			prevHigh = high;
			prevLow = low;
		}
	}
}
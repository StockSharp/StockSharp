namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Directional Index calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuDirectionalIndexParams"/> struct.
/// </remarks>
/// <param name="length">Directional Index length (used for Wilder smoothing).</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuDirectionalIndexParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// Directional Index period length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is DirectionalIndex directionalIndex)
		{
			Unsafe.AsRef(in this).Length = directionalIndex.Length;
		}
	}
}

/// <summary>
/// GPU result for Directional Index calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuDirectionalIndexResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Directional Index (DX) value.
	/// </summary>
	public float Dx;

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

		var directionalIndex = (DirectionalIndex)indicator;

		if (Dx.IsNaN() || PlusDi.IsNaN() || MinusDi.IsNaN())
		{
			return new DirectionalIndexValue(directionalIndex, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		var value = new DirectionalIndexValue(directionalIndex, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		value.Add(directionalIndex.Plus, new DecimalIndicatorValue(directionalIndex.Plus, (decimal)PlusDi, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		value.Add(directionalIndex.Minus, new DecimalIndicatorValue(directionalIndex.Minus, (decimal)MinusDi, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		value.Add(directionalIndex, new DecimalIndicatorValue(directionalIndex, (decimal)Dx, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		return value;
	}
}

/// <summary>
/// GPU calculator for Directional Index (DX, +DI, -DI).
/// </summary>
public class GpuDirectionalIndexCalculator : GpuIndicatorCalculatorBase<DirectionalIndex, GpuDirectionalIndexParams, GpuDirectionalIndexResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuDirectionalIndexResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuDirectionalIndexParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuDirectionalIndexCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuDirectionalIndexCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuDirectionalIndexResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuDirectionalIndexParams>>(DirectionalIndexParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuDirectionalIndexResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuDirectionalIndexParams[] parameters)
	{
		ArgumentNullException.ThrowIfNull(candlesSeries);
		ArgumentNullException.ThrowIfNull(parameters);

		if (candlesSeries.Length == 0)
		{
			throw new ArgumentOutOfRangeException(nameof(candlesSeries));
		}

		if (parameters.Length == 0)
		{
			throw new ArgumentOutOfRangeException(nameof(parameters));
		}

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
		using var outputBuffer = Accelerator.Allocate1D<GpuDirectionalIndexResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		// Re-split [series][param][bar]
		var result = new GpuDirectionalIndexResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuDirectionalIndexResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuDirectionalIndexResult[len];
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
	/// ILGPU kernel: Directional Index computation for multiple series and parameter sets.
	/// One thread handles one (parameter, series) pair and iterates bars sequentially.
	/// Matches CPU algorithm: ATR = WilderMA(TrueRange), DI = 100 * WilderMA(DM) / ATR.
	/// </summary>
	private static void DirectionalIndexParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuDirectionalIndexResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuDirectionalIndexParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
		{
			return;
		}

		var length = parameters[paramIdx].Length;
		if (length <= 0)
		{
			length = 1;
		}

		// WilderMA state for ATR (starts from bar 0)
		float atrValue = 0f;
		int atrCount = 0;

		// WilderMA state for +DM and -DM (starts from bar 1, when prevCandle is available)
		float dmPlusMa = 0f;
		float dmMinusMa = 0f;
		int dmCount = 0;

		float prevClose = flatCandles[offset].Close;
		float prevHigh = flatCandles[offset].High;
		float prevLow = flatCandles[offset].Low;

		// Track whether ATR and DM MAs were formed BEFORE current bar processing (1-bar delay)
		byte atrWasFormed = 0;
		byte dmWasFormed = 0;
		byte prevFormed = 0;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var high = candle.High;
			var low = candle.Low;
			var close = candle.Close;

			// Check formed state from PREVIOUS bar (before processing current bar)
			byte curFormed = (byte)(atrWasFormed == 1 && dmWasFormed == 1 ? 1 : 0);

			// Compute TrueRange (matches CPU TrueRange)
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

			// WilderMA for ATR: (prevValue * (count-1) + newValue) / count
			atrCount++;
			var atrN = atrCount < length ? atrCount : length;
			atrValue = (atrValue * (atrN - 1) + tr) / atrN;

			var resIndex = paramIdx * flatCandles.Length + (offset + i);

			if (i == 0)
			{
				// Bar 0: no previous candle, DI not computed yet
				flatResults[resIndex] = new()
				{
					Time = candle.Time,
					Dx = float.NaN,
					PlusDi = float.NaN,
					MinusDi = float.NaN,
					IsFormed = prevFormed,
				};
			}
			else
			{
				// Compute DM+ and DM- (matches CPU DiPlus/DiMinus)
				var upMove = high - prevHigh;
				var downMove = prevLow - low;
				var plusDm = (upMove > downMove && upMove > 0f) ? upMove : 0f;
				var minusDm = (downMove > upMove && downMove > 0f) ? downMove : 0f;

				// WilderMA for DM+ and DM-
				dmCount++;
				var dmN = dmCount < length ? dmCount : length;
				dmPlusMa = (dmPlusMa * (dmN - 1) + plusDm) / dmN;
				dmMinusMa = (dmMinusMa * (dmN - 1) + minusDm) / dmN;

				// Compute DI values
				var plusDi = atrValue > 0f ? 100f * dmPlusMa / atrValue : 0f;
				var minusDi = atrValue > 0f ? 100f * dmMinusMa / atrValue : 0f;

				var diSum = plusDi + minusDi;
				var diDiff = MathF.Abs(plusDi - minusDi);
				var dx = diSum > 0f ? 100f * diDiff / diSum : 0f;

				flatResults[resIndex] = new()
				{
					Time = candle.Time,
					Dx = dx,
					PlusDi = plusDi,
					MinusDi = minusDi,
					IsFormed = prevFormed,
				};
			}

			// Update formed tracking AFTER processing (for next bar's prevFormed)
			atrWasFormed = (byte)(atrCount >= length ? 1 : 0);
			dmWasFormed = (byte)(dmCount >= length ? 1 : 0);

			prevFormed = curFormed;
			prevClose = close;
			prevHigh = high;
			prevLow = low;
		}
	}
}

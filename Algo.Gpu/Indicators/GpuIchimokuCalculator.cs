namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Ichimoku calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuIchimokuParams"/> struct.
/// </remarks>
/// <param name="tenkanLength">Tenkan-sen length.</param>
/// <param name="kijunLength">Kijun-sen length.</param>
/// <param name="senkouBLength">Senkou Span B length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuIchimokuParams(int tenkanLength, int kijunLength, int senkouBLength) : IGpuIndicatorParams
{
	/// <summary>
	/// Tenkan-sen period length.
	/// </summary>
	public int TenkanLength = tenkanLength;

	/// <summary>
	/// Kijun-sen period length.
	/// </summary>
	public int KijunLength = kijunLength;

	/// <summary>
	/// Senkou Span B period length.
	/// </summary>
	public int SenkouBLength = senkouBLength;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is Ichimoku ichimoku)
		{
			Unsafe.AsRef(in this).TenkanLength = ichimoku.Tenkan.Length;
			Unsafe.AsRef(in this).KijunLength = ichimoku.Kijun.Length;
			Unsafe.AsRef(in this).SenkouBLength = ichimoku.SenkouB.Length;
		}
	}
}

/// <summary>
/// GPU result for Ichimoku calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuIchimokuResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Tenkan-sen value.
	/// </summary>
	public float Tenkan;

	/// <summary>
	/// Kijun-sen value.
	/// </summary>
	public float Kijun;

	/// <summary>
	/// Senkou Span A value (shifted).
	/// </summary>
	public float SenkouA;

	/// <summary>
	/// Senkou Span B value (shifted).
	/// </summary>
	public float SenkouB;

	/// <summary>
	/// Chinkou Span value.
	/// </summary>
	public float Chinkou;

	/// <summary>
	/// Tenkan formation flag.
	/// </summary>
	public byte TenkanIsFormed;

	/// <summary>
	/// Kijun formation flag.
	/// </summary>
	public byte KijunIsFormed;

	/// <summary>
	/// Senkou Span A formation flag.
	/// </summary>
	public byte SenkouAIsFormed;

	/// <summary>
	/// Senkou Span B formation flag.
	/// </summary>
	public byte SenkouBIsFormed;

	/// <summary>
	/// Chinkou formation flag.
	/// </summary>
	public byte ChinkouIsFormed;

	/// <summary>
	/// Overall indicator formation flag.
	/// </summary>
	public byte IsFormed;

	readonly long IGpuIndicatorResult.Time => Time;
	readonly byte IGpuIndicatorResult.IsFormed => IsFormed;

	/// <inheritdoc />
	public readonly IIndicatorValue ToValue(IIndicator indicator)
	{
		var time = this.GetTime();
		var isFormed = this.GetIsFormed();

		var ichimoku = (Ichimoku)indicator;

		var value = new IchimokuValue(ichimoku, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
			IsEmpty = Tenkan.IsNaN() && Kijun.IsNaN() && SenkouA.IsNaN() && SenkouB.IsNaN() && Chinkou.IsNaN(),
		};

		value.Add(ichimoku.Tenkan, CreateDecimalValue(ichimoku.Tenkan, time, Tenkan, TenkanIsFormed));
		value.Add(ichimoku.Kijun, CreateDecimalValue(ichimoku.Kijun, time, Kijun, KijunIsFormed));
		value.Add(ichimoku.SenkouA, CreateDecimalValue(ichimoku.SenkouA, time, SenkouA, SenkouAIsFormed));
		value.Add(ichimoku.SenkouB, CreateDecimalValue(ichimoku.SenkouB, time, SenkouB, SenkouBIsFormed));
		value.Add(ichimoku.Chinkou, CreateDecimalValue(ichimoku.Chinkou, time, Chinkou, ChinkouIsFormed));

		return value;
	}

	private static DecimalIndicatorValue CreateDecimalValue(IIndicator indicator, DateTime time, float data, byte formed)
	{
		if (data.IsNaN())
		{
			return new DecimalIndicatorValue(indicator, time)
			{
				IsFinal = true,
				IsFormed = formed != 0,
			};
		}

		return new DecimalIndicatorValue(indicator, (decimal)data, time)
		{
			IsFinal = true,
			IsFormed = formed != 0,
		};
	}
}

/// <summary>
/// GPU calculator for Ichimoku indicator.
/// </summary>
public class GpuIchimokuCalculator : GpuIndicatorCalculatorBase<Ichimoku, GpuIchimokuParams, GpuIchimokuResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIchimokuResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuIchimokuParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuIchimokuCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuIchimokuCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIchimokuResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuIchimokuParams>>(IchimokuKernel);
	}

	/// <inheritdoc />
	public override GpuIchimokuResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuIchimokuParams[] parameters)
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
			if (len <= 0)
				continue;

			Array.Copy(candlesSeries[s], 0, flatCandles, offset, len);
			offset += len;
		}

		using var inputBuffer = Accelerator.Allocate1D(flatCandles);
		using var offsetsBuffer = Accelerator.Allocate1D(seriesOffsets);
		using var lengthsBuffer = Accelerator.Allocate1D(seriesLengths);
		using var paramsBuffer = Accelerator.Allocate1D(parameters);
		using var outputBuffer = Accelerator.Allocate1D<GpuIchimokuResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuIchimokuResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			var offsetBase = seriesOffsets[s];
			result[s] = new GpuIchimokuResult[parameters.Length][];

			for (var p = 0; p < parameters.Length; p++)
			{
				var prm = parameters[p];
				var tenkanLength = Math.Max(prm.TenkanLength, 1);
				var kijunLength = Math.Max(prm.KijunLength, 1);
				var senkouBLength = Math.Max(prm.SenkouBLength, 1);

				var senkouAQueue = new Queue<float>(kijunLength);
				var senkouBQueue = new Queue<float>(kijunLength);

				var arr = new GpuIchimokuResult[len];

				for (var i = 0; i < len; i++)
				{
					var globalIdx = offsetBase + i;
					var resIdx = p * totalSize + globalIdx;
					var res = flatResults[resIdx];

					var rawSenkouA = res.SenkouA;
					var rawSenkouB = res.SenkouB;

					float senkouAValue = float.NaN;
					if (senkouAQueue.Count > 0)
					{
						if (senkouAQueue.Count >= kijunLength)
							senkouAValue = senkouAQueue.Peek();
						else if (!rawSenkouA.IsNaN() && senkouAQueue.Count == (kijunLength - 1))
							senkouAValue = senkouAQueue.Peek();
					}

					if (!rawSenkouA.IsNaN())
					{
						senkouAQueue.Enqueue(rawSenkouA);
						if (senkouAQueue.Count > kijunLength)
							senkouAQueue.Dequeue();
					}

					var kijunFormed = i >= (kijunLength - 1);

					float senkouBValue = float.NaN;
					if (senkouBQueue.Count > 0)
					{
						if (senkouBQueue.Count >= kijunLength)
							senkouBValue = senkouBQueue.Peek();
						else if (kijunFormed && !rawSenkouB.IsNaN() && senkouBQueue.Count == (kijunLength - 1))
							senkouBValue = senkouBQueue.Peek();
					}

					if (kijunFormed && !rawSenkouB.IsNaN())
					{
						senkouBQueue.Enqueue(rawSenkouB);
						if (senkouBQueue.Count > kijunLength)
							senkouBQueue.Dequeue();
					}

					var tenkanFormed = i >= (tenkanLength - 1);
					var senkouAFormed = senkouAQueue.Count >= kijunLength;
					var senkouBRawFormed = i >= (senkouBLength - 1);
					var senkouBFormed = senkouBRawFormed && senkouBQueue.Count >= kijunLength;
					var chinkouFormed = i >= (kijunLength - 1);

					res.SenkouA = senkouAValue;
					res.SenkouB = senkouBValue;
					res.TenkanIsFormed = (byte)(tenkanFormed ? 1 : 0);
					res.KijunIsFormed = (byte)(kijunFormed ? 1 : 0);
					res.SenkouAIsFormed = (byte)(senkouAFormed ? 1 : 0);
					res.SenkouBIsFormed = (byte)(senkouBFormed ? 1 : 0);
					res.ChinkouIsFormed = (byte)(chinkouFormed ? 1 : 0);
					res.IsFormed = (byte)((tenkanFormed && kijunFormed && senkouAFormed && senkouBFormed && chinkouFormed) ? 1 : 0);

					arr[i] = res;
				}

				result[s][p] = arr;
			}
		}

		return result;
	}

	/// <summary>
	/// ILGPU kernel calculating raw Ichimoku components per series/parameter set.
	/// </summary>
	private static void IchimokuKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIchimokuResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuIchimokuParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var tenkanLength = prm.TenkanLength > 0 ? prm.TenkanLength : 1;
		var kijunLength = prm.KijunLength > 0 ? prm.KijunLength : 1;
		var senkouBLength = prm.SenkouBLength > 0 ? prm.SenkouBLength : 1;

		var totalCandles = flatCandles.Length;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var resIndex = paramIdx * totalCandles + (offset + i);

			var tenkan = float.NaN;
			if (i >= (tenkanLength - 1))
			{
				var maxHigh = float.NegativeInfinity;
				var minLow = float.PositiveInfinity;
				var start = i - tenkanLength + 1;
				for (var j = start; j <= i; j++)
				{
					var c = flatCandles[offset + j];
					if (c.High > maxHigh)
						maxHigh = c.High;
					if (c.Low < minLow)
						minLow = c.Low;
				}
				tenkan = (maxHigh + minLow) / 2f;
			}

			var kijun = float.NaN;
			if (i >= (kijunLength - 1))
			{
				var maxHigh = float.NegativeInfinity;
				var minLow = float.PositiveInfinity;
				var start = i - kijunLength + 1;
				for (var j = start; j <= i; j++)
				{
					var c = flatCandles[offset + j];
					if (c.High > maxHigh)
						maxHigh = c.High;
					if (c.Low < minLow)
						minLow = c.Low;
				}
				kijun = (maxHigh + minLow) / 2f;
			}

			var senkouA = (!tenkan.IsNaN() && !kijun.IsNaN()) ? (tenkan + kijun) / 2f : float.NaN;

			var senkouB = float.NaN;
			if (i >= (senkouBLength - 1))
			{
				var maxHigh = float.NegativeInfinity;
				var minLow = float.PositiveInfinity;
				var start = i - senkouBLength + 1;
				for (var j = start; j <= i; j++)
				{
					var c = flatCandles[offset + j];
					if (c.High > maxHigh)
						maxHigh = c.High;
					if (c.Low < minLow)
						minLow = c.Low;
				}
				senkouB = (maxHigh + minLow) / 2f;
			}

			flatResults[resIndex] = new GpuIchimokuResult
			{
				Time = candle.Time,
				Tenkan = tenkan,
				Kijun = kijun,
				SenkouA = senkouA,
				SenkouB = senkouB,
				Chinkou = candle.Close,
				TenkanIsFormed = 0,
				KijunIsFormed = 0,
				SenkouAIsFormed = 0,
				SenkouBIsFormed = 0,
				ChinkouIsFormed = 0,
				IsFormed = 0,
			};
		}
	}
}

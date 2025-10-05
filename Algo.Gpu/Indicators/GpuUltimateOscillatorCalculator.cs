namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Ultimate Oscillator calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuUltimateOscillatorParams : IGpuIndicatorParams
{
	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		// Ultimate oscillator has no configurable GPU parameters.
	}
}

/// <summary>
/// GPU calculator for Ultimate Oscillator indicator.
/// </summary>
public class GpuUltimateOscillatorCalculator : GpuIndicatorCalculatorBase<UltimateOscillator, GpuUltimateOscillatorParams, GpuIndicatorResult>
{
	private const int Period7 = 7;
	private const int Period14 = 14;
	private const int Period28 = 28;
	private const float Weight1 = 1f;
	private const float Weight2 = 2f;
	private const float Weight4 = 4f;
	private const float WeightSum = Weight1 + Weight2 + Weight4;
	private const float Hundred = 100f;

	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuUltimateOscillatorParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuUltimateOscillatorCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuUltimateOscillatorCalculator(Context context, Accelerator accelerator)
	: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
		<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuUltimateOscillatorParams>>(UltimateOscillatorParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuUltimateOscillatorParams[] parameters)
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

	private struct RingBuffer28
	{
		public float V00;
		public float V01;
		public float V02;
		public float V03;
		public float V04;
		public float V05;
		public float V06;
		public float V07;
		public float V08;
		public float V09;
		public float V10;
		public float V11;
		public float V12;
		public float V13;
		public float V14;
		public float V15;
		public float V16;
		public float V17;
		public float V18;
		public float V19;
		public float V20;
		public float V21;
		public float V22;
		public float V23;
		public float V24;
		public float V25;
		public float V26;
		public float V27;

		public readonly float Get(int index)
		=> index switch
		{
			0 => V00,
			1 => V01,
			2 => V02,
			3 => V03,
			4 => V04,
			5 => V05,
			6 => V06,
			7 => V07,
			8 => V08,
			9 => V09,
			10 => V10,
			11 => V11,
			12 => V12,
			13 => V13,
			14 => V14,
			15 => V15,
			16 => V16,
			17 => V17,
			18 => V18,
			19 => V19,
			20 => V20,
			21 => V21,
			22 => V22,
			23 => V23,
			24 => V24,
			25 => V25,
			26 => V26,
			27 => V27,
			_ => 0f,
		};

		public void Set(int index, float value)
		{
			switch (index)
			{
				case 0:
					V00 = value;
					break;
				case 1:
					V01 = value;
					break;
				case 2:
					V02 = value;
					break;
				case 3:
					V03 = value;
					break;
				case 4:
					V04 = value;
					break;
				case 5:
					V05 = value;
					break;
				case 6:
					V06 = value;
					break;
				case 7:
					V07 = value;
					break;
				case 8:
					V08 = value;
					break;
				case 9:
					V09 = value;
					break;
				case 10:
					V10 = value;
					break;
				case 11:
					V11 = value;
					break;
				case 12:
					V12 = value;
					break;
				case 13:
					V13 = value;
					break;
				case 14:
					V14 = value;
					break;
				case 15:
					V15 = value;
					break;
				case 16:
					V16 = value;
					break;
				case 17:
					V17 = value;
					break;
				case 18:
					V18 = value;
					break;
				case 19:
					V19 = value;
					break;
				case 20:
					V20 = value;
					break;
				case 21:
					V21 = value;
					break;
				case 22:
					V22 = value;
					break;
				case 23:
					V23 = value;
					break;
				case 24:
					V24 = value;
					break;
				case 25:
					V25 = value;
					break;
				case 26:
					V26 = value;
					break;
				case 27:
					V27 = value;
					break;
			}
		}
	}

	/// <summary>
	/// ILGPU kernel: Ultimate Oscillator computation for multiple series and parameter sets.
	/// One thread processes entire series for a single parameter set.
	/// </summary>
	private static void UltimateOscillatorParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuUltimateOscillatorParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var bpBuffer = new RingBuffer28();
		var trBuffer = new RingBuffer28();

		float sumBp7 = 0f, sumBp14 = 0f, sumBp28 = 0f;
		float sumTr7 = 0f, sumTr14 = 0f, sumTr28 = 0f;

		var hasPrev = false;
		var prevClose = 0f;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var resIndex = paramIdx * flatCandles.Length + globalIdx;

			flatResults[resIndex] = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0,
			};

			if (!hasPrev)
			{
				prevClose = candle.Close;
				hasPrev = true;
				continue;
			}

			var low = candle.Low;
			var high = candle.High;
			var close = candle.Close;

			var min = prevClose < low ? prevClose : low;
			var max = prevClose > high ? prevClose : high;

			var bp = close - min;
			var tr = max - min;

			var oldIndex7 = i >= Period7 ? (i - Period7) % Period28 : 0;
			var oldIndex14 = i >= Period14 ? (i - Period14) % Period28 : 0;
			var oldIndex28 = i >= Period28 ? (i - Period28) % Period28 : 0;

			var oldBp7 = i >= Period7 ? bpBuffer.Get(oldIndex7) : 0f;
			var oldBp14 = i >= Period14 ? bpBuffer.Get(oldIndex14) : 0f;
			var oldBp28 = i >= Period28 ? bpBuffer.Get(oldIndex28) : 0f;

			var oldTr7 = i >= Period7 ? trBuffer.Get(oldIndex7) : 0f;
			var oldTr14 = i >= Period14 ? trBuffer.Get(oldIndex14) : 0f;
			var oldTr28 = i >= Period28 ? trBuffer.Get(oldIndex28) : 0f;

			var bufferIndex = i % Period28;
			bpBuffer.Set(bufferIndex, bp);
			trBuffer.Set(bufferIndex, tr);

			sumBp7 += bp;
			sumBp14 += bp;
			sumBp28 += bp;

			sumTr7 += tr;
			sumTr14 += tr;
			sumTr28 += tr;

			if (i >= Period7)
			{
				sumBp7 -= oldBp7;
				sumTr7 -= oldTr7;
			}

			if (i >= Period14)
			{
				sumBp14 -= oldBp14;
				sumTr14 -= oldTr14;
			}

			if (i >= Period28)
			{
				sumBp28 -= oldBp28;
				sumTr28 -= oldTr28;
			}

			prevClose = close;

			if (i >= Period28 && sumTr7 != 0f && sumTr14 != 0f && sumTr28 != 0f)
			{
				var average7 = sumBp7 / sumTr7;
				var average14 = sumBp14 / sumTr14;
				var average28 = sumBp28 / sumTr28;
				var value = Hundred * ((Weight4 * average7 + Weight2 * average14 + Weight1 * average28) / WeightSum);

				flatResults[resIndex] = new GpuIndicatorResult
				{
					Time = candle.Time,
					Value = value,
					IsFormed = 1,
				};
			}
		}
	}
}

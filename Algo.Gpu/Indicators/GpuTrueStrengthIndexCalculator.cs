namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU True Strength Index calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuTrueStrengthIndexParams"/> struct.
/// </remarks>
/// <param name="firstLength">First smoothing length.</param>
/// <param name="secondLength">Second smoothing length.</param>
/// <param name="signalLength">Signal length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuTrueStrengthIndexParams(int firstLength, int secondLength, int signalLength, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// First smoothing period.
	/// </summary>
	public int FirstLength = firstLength;

	/// <summary>
	/// Second smoothing period.
	/// </summary>
	public int SecondLength = secondLength;

	/// <summary>
	/// Signal smoothing period.
	/// </summary>
	public int SignalLength = signalLength;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is TrueStrengthIndex tsi)
		{
			Unsafe.AsRef(in this).FirstLength = tsi.FirstLength;
			Unsafe.AsRef(in this).SecondLength = tsi.SecondLength;
			Unsafe.AsRef(in this).SignalLength = tsi.SignalLength;
		}
	}
}

/// <summary>
/// Complex GPU result for True Strength Index calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuTrueStrengthIndexResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTime.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// TSI line value.
	/// </summary>
	public float Tsi;

	/// <summary>
	/// Signal line value.
	/// </summary>
	public float Signal;

	/// <summary>
	/// Whether the TSI line is formed.
	/// </summary>
	public byte TsiIsFormed;

	/// <summary>
	/// Whether the signal line is formed.
	/// </summary>
	public byte SignalIsFormed;

	/// <summary>
	/// Overall indicator form state (byte to be GPU-friendly).
	/// </summary>
	public byte IsFormed;

	readonly long IGpuIndicatorResult.Time => Time;
	readonly byte IGpuIndicatorResult.IsFormed => IsFormed;

	/// <inheritdoc />
	public readonly IIndicatorValue ToValue(IIndicator indicator)
	{
		var time = this.GetTime();
		var isFormed = this.GetIsFormed();
		var tsiIndicator = (TrueStrengthIndex)indicator;
		var tsiFormed = TsiIsFormed.To<bool>();
		var signalFormed = SignalIsFormed.To<bool>();
		var isEmpty = Tsi.IsNaN() && Signal.IsNaN();

		var result = new TrueStrengthIndexValue(tsiIndicator, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
			IsEmpty = isEmpty,
		};

		IIndicatorValue tsiValue;

		if (Tsi.IsNaN())
		{
			tsiValue = new DecimalIndicatorValue(tsiIndicator.Tsi, time)
			{
				IsFinal = true,
				IsFormed = tsiFormed,
				IsEmpty = true,
			};
		}
		else
		{
			tsiValue = new DecimalIndicatorValue(tsiIndicator.Tsi, (decimal)Tsi, time)
			{
				IsFinal = true,
				IsFormed = tsiFormed,
			};
		}

		result.Add(tsiIndicator.Tsi, tsiValue);

		if (!Signal.IsNaN())
		{
			var signalValue = new DecimalIndicatorValue(tsiIndicator.Signal, (decimal)Signal, time)
			{
				IsFinal = true,
				IsFormed = signalFormed,
			};

			result.Add(tsiIndicator.Signal, signalValue);
		}

		return result;
	}
}

/// <summary>
/// GPU calculator for True Strength Index (TSI).
/// </summary>
public class GpuTrueStrengthIndexCalculator : GpuIndicatorCalculatorBase<TrueStrengthIndex, GpuTrueStrengthIndexParams, GpuTrueStrengthIndexResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuTrueStrengthIndexResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuTrueStrengthIndexParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuTrueStrengthIndexCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuTrueStrengthIndexCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuTrueStrengthIndexResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuTrueStrengthIndexParams>>(TsiParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuTrueStrengthIndexResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuTrueStrengthIndexParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuTrueStrengthIndexResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuTrueStrengthIndexResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuTrueStrengthIndexResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuTrueStrengthIndexResult[len];
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
	/// ILGPU kernel: TSI computation for multiple series and parameter sets.
	/// </summary>
	private static void TsiParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuTrueStrengthIndexResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuTrueStrengthIndexParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var firstLength = prm.FirstLength < 1 ? 1 : prm.FirstLength;
		var secondLength = prm.SecondLength < 1 ? 1 : prm.SecondLength;
		var signalLength = prm.SignalLength < 1 ? 1 : prm.SignalLength;
		var priceType = (Level1Fields)prm.PriceType;

		var resBase = paramIdx * flatCandles.Length;

		var prevPrice = ExtractPrice(flatCandles[offset], priceType);

		var firstMomentumSum = 0f;
		var firstAbsSum = 0f;
		var secondMomentumSum = 0f;
		var secondAbsSum = 0f;
		var signalSum = 0f;

		var firstMomentumEma = 0f;
		var firstAbsEma = 0f;
		var doubleMomentum = 0f;
		var doubleAbs = 0f;
		var signalEma = 0f;

		var firstCount = 0;
		var secondCount = 0;
		var signalCount = 0;

		var alphaFirst = 2f / (firstLength + 1f);
		var alphaSecond = 2f / (secondLength + 1f);
		var alphaSignal = 2f / (signalLength + 1f);

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var resultIndex = resBase + globalIdx;

			var output = new GpuTrueStrengthIndexResult
			{
				Time = candle.Time,
				Tsi = float.NaN,
				Signal = float.NaN,
				TsiIsFormed = 0,
				SignalIsFormed = 0,
				IsFormed = 0,
			};

			if (i == 0)
			{
				flatResults[resultIndex] = output;
				continue;
			}

			var price = ExtractPrice(candle, priceType);
			var momentum = price - prevPrice;
			var absMomentum = MathF.Abs(momentum);

			if (firstCount < firstLength)
			{
				firstCount++;
				firstMomentumSum += momentum;
				firstAbsSum += absMomentum;
				firstMomentumEma = firstMomentumSum / firstLength;
				firstAbsEma = firstAbsSum / firstLength;
			}
			else
			{
				firstMomentumEma = (momentum - firstMomentumEma) * alphaFirst + firstMomentumEma;
				firstAbsEma = (absMomentum - firstAbsEma) * alphaFirst + firstAbsEma;
			}

			if (secondCount < secondLength)
			{
				secondCount++;
				secondMomentumSum += firstMomentumEma;
				secondAbsSum += firstAbsEma;
				doubleMomentum = secondMomentumSum / secondLength;
				doubleAbs = secondAbsSum / secondLength;
			}
			else
			{
				doubleMomentum = (firstMomentumEma - doubleMomentum) * alphaSecond + doubleMomentum;
				doubleAbs = (firstAbsEma - doubleAbs) * alphaSecond + doubleAbs;
			}

			var tsi = doubleAbs != 0f ? 100f * (doubleMomentum / doubleAbs) : 0f;
			var tsiFormed = secondCount >= secondLength;

			output.Tsi = tsi;
			output.TsiIsFormed = (byte)(tsiFormed ? 1 : 0);

			if (tsiFormed)
			{
				if (signalCount < signalLength)
				{
					signalCount++;
					signalSum += tsi;
					signalEma = signalSum / signalLength;
				}
				else
				{
					signalEma = (tsi - signalEma) * alphaSignal + signalEma;
				}

				output.Signal = signalEma;
				var signalFormed = signalCount >= signalLength;
				output.SignalIsFormed = (byte)(signalFormed ? 1 : 0);
				output.IsFormed = (byte)(signalFormed ? 1 : 0);
			}

			flatResults[resultIndex] = output;
			prevPrice = price;
		}
	}
}

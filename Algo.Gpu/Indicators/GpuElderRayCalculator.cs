namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Elder Ray calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuElderRayParams"/> struct.
/// </remarks>
/// <param name="length">EMA length.</param>
/// <param name="priceType">Price type for EMA calculation.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuElderRayParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// EMA period length.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// Price type to extract for EMA calculation.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is ElderRay elderRay)
		{
			Unsafe.AsRef(in this).Length = elderRay.Length;
		}
	}
}

/// <summary>
/// GPU result for Elder Ray calculation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuElderRayResult : IGpuIndicatorResult
{
	/// <summary>
	/// Time in <see cref="DateTimeOffset.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Bull Power value.
	/// </summary>
	public float BullPower;

	/// <summary>
	/// Bear Power value.
	/// </summary>
	public float BearPower;

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
		var elderRay = (ElderRay)indicator;

		if (BullPower.IsNaN() || BearPower.IsNaN())
		{
			return new ElderRayValue(elderRay, time)
			{
				IsFinal = true,
				IsFormed = isFormed,
				IsEmpty = true,
			};
		}

		var value = new ElderRayValue(elderRay, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		var bullIndicator = elderRay.BullPower;
		value.Add(bullIndicator, new DecimalIndicatorValue(bullIndicator, (decimal)BullPower, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		var bearIndicator = elderRay.BearPower;
		value.Add(bearIndicator, new DecimalIndicatorValue(bearIndicator, (decimal)BearPower, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		});

		return value;
	}
}

/// <summary>
/// GPU calculator for Elder Ray indicator.
/// </summary>
public class GpuElderRayCalculator : GpuIndicatorCalculatorBase<ElderRay, GpuElderRayParams, GpuElderRayResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuElderRayResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuElderRayParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuElderRayCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuElderRayCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuElderRayResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuElderRayParams>>(ElderRayParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuElderRayResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuElderRayParams[] parameters)
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
		using var outputBuffer = Accelerator.Allocate1D<GpuElderRayResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuElderRayResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuElderRayResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuElderRayResult[len];
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
	/// ILGPU kernel: Elder Ray computation for multiple series and parameter sets.
	/// </summary>
	private static void ElderRayParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuElderRayResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuElderRayParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var L = prm.Length;
		if (L <= 0)
			L = 1;

		var priceType = (Level1Fields)prm.PriceType;
		var multiplier = 2f / (L + 1f);
		var sum = 0f;
		var formed = false;
		var emaPrev = 0f;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var price = ExtractPrice(candle, priceType);
			var resIndex = paramIdx * flatCandles.Length + globalIdx;

			var result = new GpuElderRayResult
			{
				Time = candle.Time,
				BullPower = float.NaN,
				BearPower = float.NaN,
				IsFormed = 0
			};

			if (!formed)
			{
				sum += price;
				if (i == L - 1)
				{
					emaPrev = sum / L;
					formed = true;
				}
			}
			else
			{
				emaPrev = (price - emaPrev) * multiplier + emaPrev;
			}

			if (formed)
			{
				result.BullPower = candle.High - emaPrev;
				result.BearPower = candle.Low - emaPrev;
				result.IsFormed = 1;
			}

			flatResults[resIndex] = result;
		}
	}
}

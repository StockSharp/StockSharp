namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Demand Index calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuDemandIndexParams"/> struct.
/// </remarks>
/// <param name="length">Demand Index moving average length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuDemandIndexParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// Demand Index moving average window length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is DemandIndex demandIndex)
		{
			Unsafe.AsRef(in this).Length = demandIndex.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Demand Index indicator.
/// </summary>
public class GpuDemandIndexCalculator : GpuIndicatorCalculatorBase<DemandIndex, GpuDemandIndexParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuDemandIndexParams>, ArrayView<float>, int> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuDemandIndexCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuDemandIndexCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuDemandIndexParams>, ArrayView<float>, int>(DemandIndexParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuDemandIndexParams[] parameters)
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

		var maxParamLength = 0;
		for (var i = 0; i < parameters.Length; i++)
		{
			var len = parameters[i].Length;
			if (len > maxParamLength)
				maxParamLength = len;
		}

		if (maxParamLength <= 0)
			maxParamLength = 1;

		using var inputBuffer = Accelerator.Allocate1D(flatCandles);
		using var offsetsBuffer = Accelerator.Allocate1D(seriesOffsets);
		using var lengthsBuffer = Accelerator.Allocate1D(seriesLengths);
		using var paramsBuffer = Accelerator.Allocate1D(parameters);
		using var windowBuffer = Accelerator.Allocate1D<float>(parameters.Length * seriesCount * maxParamLength);
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View, windowBuffer.View, maxParamLength);
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

	/// <summary>
	/// ILGPU kernel: Demand Index computation for multiple series and parameter sets.
	/// One thread processes a (parameter, series) pair sequentially across bars.
	/// </summary>
	private static void DemandIndexParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuDemandIndexParams> parameters,
		ArrayView<float> windowStorage,
		int maxParamLength)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var offset = offsets[seriesIdx];
		var totalCandles = flatCandles.Length;
		var resBase = paramIdx * totalCandles;

		var prm = parameters[paramIdx];
		var L = prm.Length;
		if (L <= 0)
			L = 1;

		var windowBase = (paramIdx * lengths.Length + seriesIdx) * maxParamLength;
		var window = windowStorage.SubView(windowBase, maxParamLength);

		float sum = 0f;
		int count = 0;
		int head = 0;
		float prevClose = 0f;
		float prevVolume = 0f;
		float prevResult = 0f;
		byte prevIsFormed = 0;
		var hasPrevResult = false;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var resIndex = resBase + globalIdx;

			flatResults[resIndex] = new()
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0,
			};

			if (i == 0)
			{
				prevClose = candle.Close;
				prevVolume = candle.Volume;
				continue;
			}

			if (prevClose == 0f || prevVolume == 0f)
			{
				prevClose = candle.Close;
				prevVolume = candle.Volume;
				continue;
			}

			var deltaP = candle.Close - prevClose;
			var deltaV = candle.Volume - prevVolume;

			if (deltaP == 0f || deltaV == 0f)
			{
				var value = hasPrevResult ? prevResult : 0f;
				var formed = hasPrevResult ? prevIsFormed : (byte)0;
				flatResults[resIndex] = new()
				{
					Time = candle.Time,
					Value = value,
					IsFormed = formed,
				};

				prevClose = candle.Close;
				prevVolume = candle.Volume;
				continue;
			}

			var absDeltaP = MathF.Abs(deltaP);
			var absDeltaV = MathF.Abs(deltaV);
			var logDeltaP = MathF.Log(absDeltaP);
			var logDeltaV = MathF.Log(absDeltaV);
			var a = logDeltaP * logDeltaV;
			var b = logDeltaP - logDeltaV;
			var demandIndex = 0f;

			if (b != 0f)
				demandIndex = a / b;

			var sign = deltaP > 0f ? 1f : (deltaP < 0f ? -1f : 0f);
			demandIndex *= sign;

			if (count < L)
			{
				window[count] = demandIndex;
				sum += demandIndex;
				count++;
			}
			else
			{
				var old = window[head];
				sum = sum - old + demandIndex;
				window[head] = demandIndex;
				head++;
				if (head == L)
					head = 0;
			}

			var avg = count > 0 ? sum / L : float.NaN;
			var formedNow = (byte)(count >= L ? 1 : 0);

			flatResults[resIndex] = new()
			{
				Time = candle.Time,
				Value = avg,
				IsFormed = formedNow,
			};

			prevClose = candle.Close;
			prevVolume = candle.Volume;
			prevResult = avg;
			prevIsFormed = formedNow;
			hasPrevResult = true;
		}
	}
}

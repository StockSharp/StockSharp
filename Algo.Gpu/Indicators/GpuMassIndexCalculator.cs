namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Mass Index calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuMassIndexParams"/> struct.
/// </remarks>
/// <param name="length">Summation length.</param>
/// <param name="emaLength">EMA length used for single and double smoothing.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuMassIndexParams(int length, int emaLength) : IGpuIndicatorParams
{
	/// <summary>
	/// Summation length for ratio accumulation.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// EMA length for both single and double smoothing.
	/// </summary>
	public int EmaLength = emaLength;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is MassIndex massIndex)
		{
			Unsafe.AsRef(in this).Length = massIndex.Length;
			Unsafe.AsRef(in this).EmaLength = massIndex.EmaLength;
		}
	}
}

/// <summary>
/// GPU calculator for Mass Index indicator.
/// </summary>
public class GpuMassIndexCalculator : GpuIndicatorCalculatorBase<MassIndex, GpuMassIndexParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMassIndexParams>, ArrayView<float>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuMassIndexCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuMassIndexCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuMassIndexParams>, ArrayView<float>>(MassIndexParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuMassIndexParams[] parameters)
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
		using var ratioBuffer = Accelerator.Allocate1D<float>(totalSize * parameters.Length);
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View, ratioBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		// Re-split [series][param][bar]
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
	/// ILGPU kernel: Mass Index computation for multiple series and multiple parameter sets.
	/// One thread handles one (parameter, series) pair and iterates sequentially over bars.
	/// </summary>
	private static void MassIndexParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuMassIndexParams> parameters,
		ArrayView<float> ratiosScratch)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var sumLength = prm.Length;
		var emaLength = prm.EmaLength;

		if (sumLength <= 0 || emaLength <= 0)
		{
			for (var i = 0; i < len; i++)
			{
				var globalIdx = offset + i;
				var resIndex = paramIdx * flatCandles.Length + globalIdx;
				var candle = flatCandles[globalIdx];
				ratiosScratch[resIndex] = 0f;
				flatResults[resIndex] = new GpuIndicatorResult
				{
					Time = candle.Time,
					Value = float.NaN,
					IsFormed = 0
				};
			}
			return;
		}

		var smoothing = 2f / (emaLength + 1f);
		var singleInitSum = 0f;
		var singleInitCount = 0;
		var singleReady = false;
		var prevSingle = 0f;

		var doubleInitSum = 0f;
		var doubleInitCount = 0;
		var doubleReady = false;
		var prevDouble = 0f;

		var validRatios = 0;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var resIndex = paramIdx * flatCandles.Length + globalIdx;

			var result = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0
			};

			var range = MathF.Abs(candle.High - candle.Low);

			singleInitSum += range;
			singleInitCount++;

			float singleValue;
			if (!singleReady)
			{
				if (singleInitCount >= emaLength)
				{
					singleValue = singleInitSum / emaLength;
					prevSingle = singleValue;
					singleReady = true;
				}
				else
				{
					// CPU EMA returns Buffer.Sum / Length during initialization,
					// always dividing by Length (not by current count).
					singleValue = singleInitSum / emaLength;
				}
			}
			else
			{
				singleValue = (range - prevSingle) * smoothing + prevSingle;
				prevSingle = singleValue;
			}

			doubleInitSum += singleValue;
			doubleInitCount++;

			var hasDoubleValue = false;
			float doubleValue = 0f;

			if (!doubleReady)
			{
				if (doubleInitCount >= emaLength)
				{
					doubleValue = doubleInitSum / emaLength;
					prevDouble = doubleValue;
					doubleReady = true;
					hasDoubleValue = true;
				}
			}
			else
			{
				doubleValue = (singleValue - prevDouble) * smoothing + prevDouble;
				prevDouble = doubleValue;
				hasDoubleValue = true;
			}

			if (hasDoubleValue && doubleValue != 0f && !float.IsNaN(doubleValue))
			{
				var ratio = singleValue / doubleValue;
				ratiosScratch[resIndex] = ratio;
				validRatios++;

				if (validRatios >= sumLength)
				{
					var sum = 0f;
					for (var j = 0; j < sumLength; j++)
						sum += ratiosScratch[resIndex - j];

					result.Value = sum;
					result.IsFormed = 1;
				}
			}
			else
			{
				ratiosScratch[resIndex] = 0f;
			}

			flatResults[resIndex] = result;
		}
	}
}

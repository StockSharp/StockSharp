namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Adaptive Laguerre Filter calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuAdaptiveLaguerreFilterParams"/> struct.
/// </remarks>
/// <param name="gamma">Gamma coefficient.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuAdaptiveLaguerreFilterParams(float gamma, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Gamma smoothing coefficient.
	/// </summary>
	public float Gamma = gamma;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is AdaptiveLaguerreFilter filter)
		{
			Unsafe.AsRef(in this).Gamma = (float)filter.Gamma;
		}
	}
}

/// <summary>
/// GPU calculator for Adaptive Laguerre Filter indicator.
/// </summary>
public class GpuAdaptiveLaguerreFilterCalculator : GpuIndicatorCalculatorBase<AdaptiveLaguerreFilter, GpuAdaptiveLaguerreFilterParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuAdaptiveLaguerreFilterParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuAdaptiveLaguerreFilterCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuAdaptiveLaguerreFilterCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuAdaptiveLaguerreFilterParams>>(AdaptiveLaguerreParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuAdaptiveLaguerreFilterParams[] parameters)
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

	/// <summary>
	/// ILGPU kernel: Adaptive Laguerre Filter computation for multiple series and parameter sets.
	/// </summary>
	private static void AdaptiveLaguerreParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuAdaptiveLaguerreFilterParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var gamma = prm.Gamma;
		if (gamma <= 0f)
			gamma = 0.000001f;
		else if (gamma >= 1f)
			gamma = 0.999999f;

		var gamma1 = 1f - gamma;
		var priceType = (Level1Fields)prm.PriceType;

		var totalCandles = flatCandles.Length;
		float l0 = 0f, l1 = 0f, l2 = 0f, l3 = 0f;
		byte formed = 0;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var price = ExtractPrice(candle, priceType);

			l0 = gamma1 * price + gamma * l0;
			l1 = -gamma * l0 + l0 + gamma * l1;
			l2 = -gamma * l1 + l1 + gamma * l2;
			l3 = -gamma * l2 + l2 + gamma * l3;

			var filteredValue = (l0 + 2f * l1 + 2f * l2 + l3) / 6f;

			if (formed == 0 && filteredValue >= price)
				formed = 1;

			flatResults[paramIdx * totalCandles + globalIdx] = new()
			{
				Time = candle.Time,
				Value = filteredValue,
				IsFormed = formed
			};
		}
	}
}

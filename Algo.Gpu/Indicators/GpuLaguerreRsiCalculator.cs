namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Laguerre RSI calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuLaguerreRsiParams"/> struct.
/// </remarks>
/// <param name="gamma">Laguerre smoothing coefficient.</param>
/// <param name="priceType">Price type to extract from candles.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuLaguerreRsiParams(float gamma, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Gamma coefficient in range (0, 1).
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
		
		if (indicator is LaguerreRSI lrsi)
		{
			Unsafe.AsRef(in this).Gamma = (float)lrsi.Gamma;
		}
	}
}

/// <summary>
/// GPU calculator for Laguerre RSI indicator.
/// </summary>
public class GpuLaguerreRsiCalculator : GpuIndicatorCalculatorBase<LaguerreRSI, GpuLaguerreRsiParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuLaguerreRsiParams>> _kernel;
	
	/// <summary>
	/// Initializes a new instance of the <see cref="GpuLaguerreRsiCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuLaguerreRsiCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
		<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuLaguerreRsiParams>>(LaguerreRsiParamsSeriesKernel);
	}
	
	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuLaguerreRsiParams[] parameters)
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
					var resIdx = p * totalSize + globalIdx;
					arr[i] = flatResults[resIdx];
				}
				result[s][p] = arr;
			}
		}
		
		return result;
	}
	
	/// <summary>
	/// ILGPU kernel: Laguerre RSI computation for multiple series and parameter sets.
	/// One thread handles one (parameter, series) pair and iterates bars sequentially.
	/// </summary>
	private static void LaguerreRsiParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuLaguerreRsiParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;
		
		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
		{
			return;
		}
		
		var param = parameters[paramIdx];
		var gamma = MathF.Max(0.000001f, MathF.Min(0.999999f, param.Gamma));
		var gamma1 = 1f - gamma;
		var priceType = (Level1Fields)param.PriceType;
		
		var l0 = 0f;
		var l1 = 0f;
		var l2 = 0f;
		var l3 = 0f;
		var prevCu = 0f;
		var prevCd = 0f;
		
		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var price = ExtractPrice(candle, priceType);
			
			var l0New = gamma1 * price + gamma * l0;
			var l1New = -gamma * l0New + l0 + gamma * l1;
			var l2New = -gamma * l1New + l1 + gamma * l2;
			var l3New = -gamma * l2New + l2 + gamma * l3;
			
			var cu = 0f;
			var cd = 0f;
			
			if (l0New >= l1New)
			{
				cu = l0New - l1New;
			}
			else
			{
				cd = l1New - l0New;
			}
			
			if (l1New >= l2New)
			{
				cu += l1New - l2New;
			}
			else
			{
				cd += l2New - l1New;
			}
			
			if (l2New >= l3New)
			{
				cu += l2New - l3New;
			}
			else
			{
				cd += l3New - l2New;
			}
			
			var smoothCu = gamma1 * cu + gamma * prevCu;
			var smoothCd = gamma1 * cd + gamma * prevCd;
			var sum = smoothCu + smoothCd;
			var lrsi = sum != 0f ? (smoothCu / sum) * 100f : 50f;
			
			var resIndex = paramIdx * flatCandles.Length + globalIdx;
			flatResults[resIndex] = new()
			{
				Time = candle.Time,
				Value = lrsi,
				IsFormed = 1
			};
			
			l0 = l0New;
			l1 = l1New;
			l2 = l2New;
			l3 = l3New;
			prevCu = smoothCu;
			prevCd = smoothCd;
		}
	}
}

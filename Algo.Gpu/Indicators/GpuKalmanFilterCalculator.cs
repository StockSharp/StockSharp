namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Kalman Filter calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuKalmanParams"/> struct.
/// </remarks>
/// <param name="length">Kalman filter length.</param>
/// <param name="priceType">Price type.</param>
/// <param name="processNoise">Process noise coefficient.</param>
/// <param name="measurementNoise">Measurement noise coefficient.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuKalmanParams(int length, byte priceType, float processNoise, float measurementNoise) : IGpuIndicatorParams
{
/// <summary>
/// Kalman filter length.
/// </summary>
public int Length = length;

/// <summary>
/// Price type to extract from candles.
/// </summary>
public byte PriceType = priceType;

/// <summary>
/// Process noise coefficient.
/// </summary>
public float ProcessNoise = processNoise;

/// <summary>
/// Measurement noise coefficient.
/// </summary>
public float MeasurementNoise = measurementNoise;

/// <inheritdoc />
public readonly void FromIndicator(IIndicator indicator)
{
Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

if (indicator is KalmanFilter kalman)
{
Unsafe.AsRef(in this).Length = kalman.Length;
Unsafe.AsRef(in this).ProcessNoise = (float)kalman.ProcessNoise;
Unsafe.AsRef(in this).MeasurementNoise = (float)kalman.MeasurementNoise;
}
}
}

/// <summary>
/// GPU calculator for the Kalman Filter indicator.
/// </summary>
public class GpuKalmanFilterCalculator : GpuIndicatorCalculatorBase<KalmanFilter, GpuKalmanParams, GpuIndicatorResult>
{
private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuKalmanParams>> _paramsSeriesKernel;

/// <summary>
/// Initializes a new instance of the <see cref="GpuKalmanFilterCalculator"/> class.
/// </summary>
/// <param name="context">ILGPU context.</param>
/// <param name="accelerator">ILGPU accelerator.</param>
public GpuKalmanFilterCalculator(Context context, Accelerator accelerator)
: base(context, accelerator)
{
_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuKalmanParams>>(KalmanParamsSeriesKernel);
}

/// <inheritdoc />
public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuKalmanParams[] parameters)
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
_paramsSeriesKernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
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
/// ILGPU kernel: Kalman Filter computation for multiple series and parameter sets.
/// </summary>
private static void KalmanParamsSeriesKernel(
Index2D index,
ArrayView<GpuCandle> flatCandles,
ArrayView<GpuIndicatorResult> flatResults,
ArrayView<int> offsets,
ArrayView<int> lengths,
ArrayView<GpuKalmanParams> parameters)
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

var processNoise = prm.ProcessNoise;
if (processNoise <= 0f)
processNoise = 1e-6f;

var measurementNoise = prm.MeasurementNoise;
if (measurementNoise <= 0f)
measurementNoise = 1e-6f;

var hasEstimate = false;
var lastEstimate = 0f;
var errorCovariance = 1f;

for (var i = 0; i < len; i++)
{
var globalIdx = offset + i;
var candle = flatCandles[globalIdx];
var value = ExtractPrice(candle, (Level1Fields)prm.PriceType);

float estimate;

if (!hasEstimate)
{
hasEstimate = true;
estimate = value;
lastEstimate = value;
errorCovariance = 1f;
}
else
{
var priorErrorCovariance = errorCovariance + processNoise;
var kalmanGain = priorErrorCovariance / (priorErrorCovariance + measurementNoise);
estimate = lastEstimate + kalmanGain * (value - lastEstimate);
lastEstimate = estimate;
errorCovariance = (1f - kalmanGain) * priorErrorCovariance;
}

var resIndex = paramIdx * flatCandles.Length + globalIdx;
flatResults[resIndex] = new GpuIndicatorResult
{
Time = candle.Time,
Value = estimate,
IsFormed = (byte)(i >= L - 1 ? 1 : 0)
};
}
}
}

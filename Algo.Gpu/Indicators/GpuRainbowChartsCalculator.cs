namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Rainbow Charts calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuRainbowChartsParams"/> struct.
/// </remarks>
/// <param name="lines">Total number of Rainbow Chart lines including the base price line.</param>
/// <param name="priceType">Price type used to extract values from candles.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuRainbowChartsParams(int lines, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Total number of Rainbow Chart lines including the base price line.
	/// </summary>
	public int Lines = lines;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is RainbowCharts rainbow)
		{
			Unsafe.AsRef(in this).Lines = rainbow.Lines;
		}
	}
}

/// <summary>
/// GPU calculation result for Rainbow Charts.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuRainbowChartsResult : IGpuIndicatorResult
{
	/// <summary>
	/// Maximum number of SMA averages stored in the result.
	/// </summary>
	public const int MaxLineCount = 32;

	/// <summary>
	/// Time in <see cref="DateTimeOffset.Ticks"/>.
	/// </summary>
	public long Time;

	/// <summary>
	/// Number of moving average values stored in this result.
	/// </summary>
	public byte LineCount;

	/// <summary>
	/// Is indicator formed (byte to be GPU-friendly).
	/// </summary>
	public byte IsFormed;

	/// <summary>
	/// Bit mask describing which averages are formed (bit index corresponds to average index).
	/// </summary>
	public ulong FormedMask;

	/// <summary>
	/// First average value.
	/// </summary>
	public float Average0;

	/// <summary>
	/// Second average value.
	/// </summary>
	public float Average1;

	/// <summary>
	/// Third average value.
	/// </summary>
	public float Average2;

	/// <summary>
	/// Fourth average value.
	/// </summary>
	public float Average3;

	/// <summary>
	/// Fifth average value.
	/// </summary>
	public float Average4;

	/// <summary>
	/// Sixth average value.
	/// </summary>
	public float Average5;

	/// <summary>
	/// Seventh average value.
	/// </summary>
	public float Average6;

	/// <summary>
	/// Eighth average value.
	/// </summary>
	public float Average7;

	/// <summary>
	/// Ninth average value.
	/// </summary>
	public float Average8;

	/// <summary>
	/// Tenth average value.
	/// </summary>
	public float Average9;

	/// <summary>
	/// Eleventh average value.
	/// </summary>
	public float Average10;

	/// <summary>
	/// Twelfth average value.
	/// </summary>
	public float Average11;

	/// <summary>
	/// Thirteenth average value.
	/// </summary>
	public float Average12;

	/// <summary>
	/// Fourteenth average value.
	/// </summary>
	public float Average13;

	/// <summary>
	/// Fifteenth average value.
	/// </summary>
	public float Average14;

	/// <summary>
	/// Sixteenth average value.
	/// </summary>
	public float Average15;

	/// <summary>
	/// Seventeenth average value.
	/// </summary>
	public float Average16;

	/// <summary>
	/// Eighteenth average value.
	/// </summary>
	public float Average17;

	/// <summary>
	/// Nineteenth average value.
	/// </summary>
	public float Average18;

	/// <summary>
	/// Twentieth average value.
	/// </summary>
	public float Average19;

	/// <summary>
	/// Twenty first average value.
	/// </summary>
	public float Average20;

	/// <summary>
	/// Twenty second average value.
	/// </summary>
	public float Average21;

	/// <summary>
	/// Twenty third average value.
	/// </summary>
	public float Average22;

	/// <summary>
	/// Twenty fourth average value.
	/// </summary>
	public float Average23;

	/// <summary>
	/// Twenty fifth average value.
	/// </summary>
	public float Average24;

	/// <summary>
	/// Twenty sixth average value.
	/// </summary>
	public float Average25;

	/// <summary>
	/// Twenty seventh average value.
	/// </summary>
	public float Average26;

	/// <summary>
	/// Twenty eighth average value.
	/// </summary>
	public float Average27;

	/// <summary>
	/// Twenty ninth average value.
	/// </summary>
	public float Average28;

	/// <summary>
	/// Thirtieth average value.
	/// </summary>
	public float Average29;

	/// <summary>
	/// Thirty first average value.
	/// </summary>
	public float Average30;

	/// <summary>
	/// Thirty second average value.
	/// </summary>
	public float Average31;

	readonly long IGpuIndicatorResult.Time => Time;
	readonly byte IGpuIndicatorResult.IsFormed => IsFormed;

	/// <summary>
	/// Set average value by index.
	/// </summary>
	/// <param name="index">Average index.</param>
	/// <param name="value">Average value.</param>
	internal void SetAverage(int index, float value)
	{
		ref var start = ref Average0;
		Unsafe.Add(ref start, index) = value;
	}

	/// <summary>
	/// Get average value by index.
	/// </summary>
	/// <param name="index">Average index.</param>
	/// <returns>Average value.</returns>
	internal readonly float GetAverage(int index)
	=> Unsafe.Add(ref Unsafe.AsRef(in Average0), index);

	/// <inheritdoc />
	public readonly IIndicatorValue ToValue(IIndicator indicator)
	{
		var rainbow = (RainbowCharts)indicator;
		var time = this.GetTime();
		var isFormed = this.GetIsFormed();

		var value = new RainbowChartsValue(rainbow, time)
		{
			IsFinal = true,
			IsFormed = isFormed,
		};

		var innerCount = rainbow.InnerIndicators.Count;
		var available = Math.Min(LineCount, (byte)innerCount);
		var formedMask = FormedMask;
		var hasValue = false;
		var index = 0;

		for (; index < available; index++)
		{
			var inner = rainbow.InnerIndicators[index];
			var avg = GetAverage(index);
			var lineFormed = (formedMask & (1UL << index)) != 0;

			if (avg.IsNaN())
			{
				value.Add(inner, new DecimalIndicatorValue(inner, time)
				{
					IsFinal = true,
					IsFormed = lineFormed,
					IsEmpty = true,
				});
			}
			else
			{
				value.Add(inner, new DecimalIndicatorValue(inner, (decimal)avg, time)
				{
					IsFinal = true,
					IsFormed = lineFormed,
				});
				hasValue = true;
			}
		}

		for (; index < innerCount; index++)
		{
			var inner = rainbow.InnerIndicators[index];
			value.Add(inner, new DecimalIndicatorValue(inner, time)
			{
				IsFinal = true,
				IsFormed = false,
				IsEmpty = true,
			});
		}

		value.IsEmpty = !hasValue;

		return value;
	}
}

/// <summary>
/// GPU calculator for Rainbow Charts indicator.
/// </summary>
public class GpuRainbowChartsCalculator : GpuIndicatorCalculatorBase<RainbowCharts, GpuRainbowChartsParams, GpuRainbowChartsResult>
{
	private readonly GpuSmaCalculator _smaCalculator;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuRainbowChartsCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuRainbowChartsCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_smaCalculator = new(context, accelerator);
	}

	/// <inheritdoc />
	public override GpuRainbowChartsResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuRainbowChartsParams[] parameters)
	{
		ArgumentNullException.ThrowIfNull(candlesSeries);
		ArgumentNullException.ThrowIfNull(parameters);

		if (candlesSeries.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(candlesSeries));

		if (parameters.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(parameters));

		var seriesCount = candlesSeries.Length;
		var seriesLengths = new int[seriesCount];

		for (var s = 0; s < seriesCount; s++)
		{
			seriesLengths[s] = candlesSeries[s]?.Length ?? 0;
		}

		var paramOffsets = new int[parameters.Length];
		var lineCounts = new int[parameters.Length];
		var totalSmaParams = 0;

		for (var p = 0; p < parameters.Length; p++)
		{
			var lines = parameters[p].Lines;

			if (lines < 1)
				throw new ArgumentOutOfRangeException(nameof(parameters));

			var lineCount = lines - 1;

			if (lineCount > GpuRainbowChartsResult.MaxLineCount)
				throw new ArgumentOutOfRangeException(nameof(parameters));

			paramOffsets[p] = totalSmaParams;
			lineCounts[p] = lineCount;
			totalSmaParams += lineCount;
		}

		GpuIndicatorResult[][][] smaResults = null;

		if (totalSmaParams > 0)
		{
			var smaParams = new GpuSmaParams[totalSmaParams];
			var idx = 0;

			for (var p = 0; p < parameters.Length; p++)
			{
				var lineCount = lineCounts[p];
				var priceType = parameters[p].PriceType;

				for (var l = 0; l < lineCount; l++)
				{
					smaParams[idx++] = new GpuSmaParams((l + 1) * 2, priceType);
				}
			}

			smaResults = _smaCalculator.Calculate(candlesSeries, smaParams);
		}

		var result = new GpuRainbowChartsResult[seriesCount][][];

		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			var seriesRes = new GpuRainbowChartsResult[parameters.Length][];
			result[s] = seriesRes;

			var seriesCandles = candlesSeries[s];
			var smaSeries = smaResults?[s];

			for (var p = 0; p < parameters.Length; p++)
			{
				var lineCount = lineCounts[p];
				var arr = new GpuRainbowChartsResult[len];
				seriesRes[p] = arr;

				if (len == 0)
					continue;

				if (lineCount == 0)
				{
					for (var bar = 0; bar < len; bar++)
					{
						ref var rcRes = ref arr[bar];
						rcRes.Time = seriesCandles[bar].Time;
						rcRes.LineCount = 0;
						rcRes.FormedMask = 0;
						rcRes.IsFormed = 1;
					}

					continue;
				}

				var offset = paramOffsets[p];

				for (var bar = 0; bar < len; bar++)
				{
					ref var rcRes = ref arr[bar];
					rcRes.LineCount = (byte)lineCount;

					ulong formedMask = 0;
					var allFormed = true;
					var time = 0L;

					for (var line = 0; line < lineCount; line++)
					{
						var smaRes = smaSeries[offset + line][bar];

						if (line == 0)
							time = smaRes.Time;

						var lineFormed = smaRes.IsFormed != 0 && !smaRes.Value.IsNaN();

						if (lineFormed)
							formedMask |= 1UL << line;
						else
							allFormed = false;

						rcRes.SetAverage(line, smaRes.Value);
					}

					rcRes.Time = time;
					rcRes.FormedMask = formedMask;
					rcRes.IsFormed = (byte)(allFormed ? 1 : 0);
				}
			}
		}

		return result;
	}
}

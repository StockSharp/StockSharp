namespace StockSharp.Algo.Analytics;

/// <summary>
/// The analytic script, shows chart drawing possibilities.
/// </summary>
public class ChartDrawScript : IAnalyticsScript
{
	async Task IAnalyticsScript.Run(ILogReceiver logs, IAnalyticsPanel panel, SecurityId[] securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, DataType dataType, CancellationToken cancellationToken)
	{
		if (securities.Length == 0)
		{
			logs.LogWarning("No instruments.");
			return;
		}

		var lineChart = panel.CreateChart<DateTime, decimal>();
		var histogramChart = panel.CreateChart<DateTime, decimal>();

		var idx = 0;
		foreach (var security in securities)
		{
			// stop calculation if user cancel script execution
			if (cancellationToken.IsCancellationRequested)
				break;

			logs.LogInfo("Processing {0} of {1}: {2}...", ++idx, securities.Length, security);

			var candlesSeries = new Dictionary<DateTime, decimal>();
			var volsSeries = new Dictionary<DateTime, decimal>();

			// get candle storage
			var candleStorage = storage.GetCandleMessageStorage(security, dataType, drive, format);
			var prevDate = default(DateOnly);

			await foreach (var candle in candleStorage.LoadAsync(from, to).WithCancellation(cancellationToken))
			{
				var currDate = DateOnly.FromDateTime(candle.OpenTime.Date);
				if (currDate != prevDate)
				{
					prevDate = currDate;
					logs.LogInfo("  {0}...", currDate);
				}

				// fill series
				candlesSeries[candle.OpenTime] = candle.ClosePrice;
				volsSeries[candle.OpenTime] = candle.TotalVolume;
			}

			// draw series on chart as line and histogram
			lineChart.Append($"{security} (close)", candlesSeries.Keys, candlesSeries.Values, DrawStyles.DashedLine);
			histogramChart.Append($"{security} (vol)", volsSeries.Keys, volsSeries.Values, DrawStyles.Histogram);
		}
	}
}
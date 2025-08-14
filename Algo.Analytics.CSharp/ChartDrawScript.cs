namespace StockSharp.Algo.Analytics;

/// <summary>
/// The analytic script, shows chart drawing possibilities.
/// </summary>
public class ChartDrawScript : IAnalyticsScript
{
	Task IAnalyticsScript.Run(ILogReceiver logs, IAnalyticsPanel panel, SecurityId[] securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, TimeSpan timeFrame, CancellationToken cancellationToken)
	{
		if (securities.Length == 0)
		{
			logs.LogWarning("No instruments.");
			return Task.CompletedTask;
		}

		var lineChart = panel.CreateChart<DateTimeOffset, decimal>();
		var histogramChart = panel.CreateChart<DateTimeOffset, decimal>();

		foreach (var security in securities)
		{
			// stop calculation if user cancel script execution
			if (cancellationToken.IsCancellationRequested)
				break;

			var candlesSeries = new Dictionary<DateTimeOffset, decimal>();
			var volsSeries = new Dictionary<DateTimeOffset, decimal>();

			// get candle storage
			var candleStorage = storage.GetTimeFrameCandleMessageStorage(security, timeFrame, drive, format);

			foreach (var candle in candleStorage.Load(from, to))
			{
				// fill series
				candlesSeries[candle.OpenTime] = candle.ClosePrice;
				volsSeries[candle.OpenTime] = candle.TotalVolume;
			}

			// draw series on chart as line and histogram
			lineChart.Append($"{security} (close)", candlesSeries.Keys, candlesSeries.Values, DrawStyles.DashedLine);
			histogramChart.Append($"{security} (vol)", volsSeries.Keys, volsSeries.Values, DrawStyles.Histogram);
		}

		return Task.CompletedTask;
	}
}
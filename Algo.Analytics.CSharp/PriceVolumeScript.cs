namespace StockSharp.Algo.Analytics;

/// <summary>
/// The analytic script, calculating distribution of the volume by price levels.
/// </summary>
public class PriceVolumeScript : IAnalyticsScript
{
	Task IAnalyticsScript.Run(ILogReceiver logs, IAnalyticsPanel panel, SecurityId[] securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, TimeSpan timeFrame, CancellationToken cancellationToken)
	{
		if (securities.Length == 0)
		{
			logs.LogWarning("No instruments.");
			return Task.CompletedTask;
		}

		// script can process only 1 instrument
		var security = securities.First();

		// get candle storage
		var candleStorage = storage.GetTimeFrameCandleMessageStorage(security, timeFrame, drive, format);

		// get available dates for the specified period
		var dates = candleStorage.GetDates(from, to).ToArray();

		if (dates.Length == 0)
		{
			logs.LogWarning("no data");
			return Task.CompletedTask;
		}

		// grouping candles by middle price
		var rows = candleStorage.Load(from, to)
			.GroupBy(c => c.LowPrice + c.GetLength() / 2)
			.ToDictionary(g => g.Key, g => g.Sum(c => c.TotalVolume));

		// draw on chart
		panel.CreateChart<decimal, decimal>()
			.Append(security.ToStringId(), rows.Keys, rows.Values, DrawStyles.Histogram);

		return Task.CompletedTask;
	}
}
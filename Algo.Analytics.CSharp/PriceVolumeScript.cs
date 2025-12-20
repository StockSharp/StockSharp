namespace StockSharp.Algo.Analytics;

/// <summary>
/// The analytic script, calculating distribution of the volume by price levels.
/// </summary>
public class PriceVolumeScript : IAnalyticsScript
{
	async Task IAnalyticsScript.Run(ILogReceiver logs, IAnalyticsPanel panel, SecurityId[] securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, DataType dataType, CancellationToken cancellationToken)
	{
		if (securities.Length == 0)
		{
			logs.LogWarning("No instruments.");
			return;
		}

		// script can process only 1 instrument
		var security = securities.First();

		// get candle storage
		var candleStorage = storage.GetCandleMessageStorage(security, dataType, drive, format);

		// get available dates for the specified period
		var dates = (await candleStorage.GetDatesAsync(from, to, cancellationToken)).ToArray();

		if (dates.Length == 0)
		{
			logs.LogWarning("no data");
			return;
		}

		// grouping candles by middle price
		var rows = (await candleStorage.LoadAsync(from, to)
			.ToArrayAsync(cancellationToken))
			.GroupBy(c => c.LowPrice + c.GetLength() / 2)
			.ToDictionary(g => g.Key, g => g.Sum(c => c.TotalVolume));

		// draw on chart
		panel.CreateChart<decimal, decimal>()
			.Append(security.ToStringId(), rows.Keys, rows.Values, DrawStyles.Histogram);
	}
}
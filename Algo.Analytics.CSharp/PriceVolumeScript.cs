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

		logs.LogInfo("Processing {0}...", security);

		// get candle storage
		var candleStorage = storage.GetCandleMessageStorage(security, dataType, drive, format);

		// get available dates for the specified period
		var dates = await candleStorage.GetDatesAsync(from, to).ToArrayAsync(cancellationToken);

		if (dates.Length == 0)
		{
			logs.LogWarning("no data");
			return;
		}

		// grouping candles by middle price
		var rows = new Dictionary<decimal, decimal>();
		var prevDate = default(DateOnly);

		await foreach (var candle in candleStorage.LoadAsync(from, to).WithCancellation(cancellationToken))
		{
			var currDate = DateOnly.FromDateTime(candle.OpenTime.Date);
			if (currDate != prevDate)
			{
				prevDate = currDate;
				logs.LogInfo("  {0}...", currDate);
			}

			var midPrice = candle.LowPrice + candle.GetLength() / 2;

			if (rows.TryGetValue(midPrice, out var volume))
				rows[midPrice] = volume + candle.TotalVolume;
			else
				rows[midPrice] = candle.TotalVolume;
		}

		// draw on chart
		panel.CreateChart<decimal, decimal>()
			.Append(security.ToStringId(), rows.Keys, rows.Values, DrawStyles.Histogram);
	}
}
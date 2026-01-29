namespace StockSharp.Algo.Analytics;

/// <summary>
/// The analytic script, calculating distribution of the biggest volume by hours.
/// </summary>
public class TimeVolumeScript : IAnalyticsScript
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

		// grouping candles by opening time (time part only) with 1 hour truncating
		var rows = new Dictionary<TimeSpan, decimal>();
		var prevDate = default(DateOnly);

		await foreach (var candle in candleStorage.LoadAsync(from, to).WithCancellation(cancellationToken))
		{
			var currDate = DateOnly.FromDateTime(candle.OpenTime.Date);
			if (currDate != prevDate)
			{
				prevDate = currDate;
				logs.LogInfo("  {0}...", currDate);
			}

			var hour = candle.OpenTime.TimeOfDay.Truncate(TimeSpan.FromHours(1));

			if (rows.TryGetValue(hour, out var volume))
				rows[hour] = volume + candle.TotalVolume;
			else
				rows[hour] = candle.TotalVolume;
		}

		// put our calculations into grid
		var grid = panel.CreateGrid("Time", "Volume");

		foreach (var row in rows)
			grid.SetRow(row.Key, row.Value);

		// sorting by volume column (descending)
		grid.SetSort("Volume", false);
	}
}
namespace StockSharp.Algo.Analytics
{
	/// <summary>
	/// The analytic script, calculating distribution of the biggest volume by hours.
	/// </summary>
	public class TimeVolumeScript : IAnalyticsScript
	{
		Task IAnalyticsScript.Run(ILogReceiver logs, IAnalyticsPanel panel, IEnumerable<Security> securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, TimeSpan timeFrame, CancellationToken cancellationToken)
		{
			// script can process only 1 instrument
			var security = securities.First();

			// get candle storage
			var candleStorage = storage.GetCandleStorage(typeof(TimeFrameCandle), security, timeFrame, format: format);

			// get available dates for the specified period
			var dates = candleStorage.GetDates(from, to).ToArray();

			if (dates.Length == 0)
			{
				logs.AddWarningLog("no data");
				return Task.CompletedTask;
			}

			// grouping candles by opening time (time part only) with 1 hour truncating
			var rows = candleStorage.Load(from, to)
				.GroupBy(c => c.OpenTime.TimeOfDay.Truncate(TimeSpan.FromHours(1)))
				.ToDictionary(g => g.Key, g => g.Sum(c => c.TotalVolume));

			// put our calculations into grid
			var grid = panel.CreateGrid("Price", "Volume");

			foreach (var row in rows)
				grid.SetRow(row.Key, row.Value);

			// sorting by volume column (descending)
			grid.SetSort("Volume", false);

			return Task.CompletedTask;
		}
	}
}
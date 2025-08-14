namespace StockSharp.Algo.Analytics;

/// <summary>
/// The analytic script, calculating distribution of the biggest volume by hours
/// and shows its in 3D chart.
/// </summary>
public class Chart3DScript : IAnalyticsScript
{
	Task IAnalyticsScript.Run(ILogReceiver logs, IAnalyticsPanel panel, SecurityId[] securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, TimeSpan timeFrame, CancellationToken cancellationToken)
	{
		if (securities.Length == 0)
		{
			logs.LogWarning("No instruments.");
			return Task.CompletedTask;
		}

		var x = new List<string>();
		var y = new List<string>();

		// fill Y labels
		for (var h = 0; h < 24; h++)
			y.Add(h.ToString());

		var z = new double[securities.Length, y.Count];

		for (var i = 0; i < securities.Length; i++)
		{
			// stop calculation if user cancel script execution
			if (cancellationToken.IsCancellationRequested)
				break;

			var security = securities[i];

			// fill X labels
			x.Add(security.ToStringId());

			// get candle storage
			var candleStorage = storage.GetTimeFrameCandleMessageStorage(security, timeFrame, drive, format);

			// get available dates for the specified period
			var dates = candleStorage.GetDates(from, to).ToArray();

			if (dates.Length == 0)
			{
				logs.LogWarning("no data");
				return Task.CompletedTask;
			}

			// grouping candles by opening time (time part only) with 1 hour truncating
			var byHours = candleStorage.Load(from, to)
				.GroupBy(c => c.OpenTime.TimeOfDay.Truncate(TimeSpan.FromHours(1)))
				.ToDictionary(g => g.Key.Hours, g => g.Sum(c => c.TotalVolume));

			// fill Z values
			foreach (var pair in byHours)
				z[i, pair.Key] = (double)pair.Value;
		}

		panel.Draw3D(x, y, z, "Instruments", "Hours", "Volume");

		return Task.CompletedTask;
	}
}
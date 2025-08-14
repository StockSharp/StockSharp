namespace StockSharp.Algo.Analytics;

/// <summary>
/// The analytic script, normalize securities close prices and shows on same chart.
/// </summary>
public class NormalizePriceScript : IAnalyticsScript
{
	Task IAnalyticsScript.Run(ILogReceiver logs, IAnalyticsPanel panel, SecurityId[] securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, TimeSpan timeFrame, CancellationToken cancellationToken)
	{
		if (securities.Length == 0)
		{
			logs.LogWarning("No instruments.");
			return Task.CompletedTask;
		}

		var chart = panel.CreateChart<DateTimeOffset, decimal>();

		foreach (var security in securities)
		{
			// stop calculation if user cancel script execution
			if (cancellationToken.IsCancellationRequested)
				break;

			var series = new Dictionary<DateTimeOffset, decimal>();

			// get candle storage
			var candleStorage = storage.GetTimeFrameCandleMessageStorage(security, timeFrame, drive, format);

			decimal? firstClose = null;

			foreach (var candle in candleStorage.Load(from, to))
			{
				firstClose ??= candle.ClosePrice;

				// normalize close prices by dividing on first close
				series[candle.OpenTime] = candle.ClosePrice / firstClose.Value;
			}

			// draw series on chart
			chart.Append(security.ToStringId(), series.Keys, series.Values);
		}

		return Task.CompletedTask;
	}
}
namespace StockSharp.Algo.Analytics;

/// <summary>
/// The analytic script, normalize securities close prices and shows on same chart.
/// </summary>
public class NormalizePriceScript : IAnalyticsScript
{
	async Task IAnalyticsScript.Run(ILogReceiver logs, IAnalyticsPanel panel, SecurityId[] securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, DataType dataType, CancellationToken cancellationToken)
	{
		if (securities.Length == 0)
		{
			logs.LogWarning("No instruments.");
			return;
		}

		var chart = panel.CreateChart<DateTime, decimal>();

		var idx = 0;
		foreach (var security in securities)
		{
			// stop calculation if user cancel script execution
			if (cancellationToken.IsCancellationRequested)
				break;

			logs.LogInfo("Processing {0} of {1}: {2}...", ++idx, securities.Length, security);

			var series = new Dictionary<DateTime, decimal>();

			// get candle storage
			var candleStorage = storage.GetCandleMessageStorage(security, dataType, drive, format);

			decimal? firstClose = null;
			var prevDate = default(DateOnly);

			await foreach (var candle in candleStorage.LoadAsync(from, to).WithCancellation(cancellationToken))
			{
				var currDate = DateOnly.FromDateTime(candle.OpenTime.Date);
				if (currDate != prevDate)
				{
					prevDate = currDate;
					logs.LogInfo("  {0}...", currDate);
				}

				firstClose ??= candle.ClosePrice;

				// normalize close prices by dividing on first close
				series[candle.OpenTime] = candle.ClosePrice / firstClose.Value;
			}

			// draw series on chart
			chart.Append(security.ToStringId(), series.Keys, series.Values);
		}
	}
}
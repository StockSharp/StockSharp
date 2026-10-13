namespace StockSharp.Algo.Analytics;

/// <summary>
/// The analytic script, normalize securities close prices and shows on same chart.
/// </summary>
public class NormalizePriceScript : IAnalyticsScript
{
	async Task IAnalyticsScript.Run(ILogReceiver logs, IAnalyticsPanel panel, SecurityId[] securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, DataType dataType, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (securities.Length == 0)
		{
			logs.LogWarning("No instruments.");
			return;
		}

		var chart = panel.CreateChart<DateTime, decimal>();

		var idx = 0;
		foreach (var security in securities)
		{
			cancellationToken.ThrowIfCancellationRequested();

			logs.LogInfo("Processing {0} of {1}: {2}...", ++idx, securities.Length, security);
			cancellationToken.ThrowIfCancellationRequested();

			var series = new Dictionary<DateTime, decimal>();

			// get candle storage
			var candleStorage = storage.GetCandleMessageStorage(security, dataType, drive, format);

			decimal? firstClose = null;
			var prevDate = default(DateOnly);

			await foreach (var candle in candleStorage.LoadAsync(from, to).WithCancellation(cancellationToken))
			{
				cancellationToken.ThrowIfCancellationRequested();

				var currDate = DateOnly.FromDateTime(candle.OpenTime.Date);
				if (currDate != prevDate)
				{
					prevDate = currDate;
					logs.LogInfo("  {0}...", currDate);
					cancellationToken.ThrowIfCancellationRequested();
				}

				firstClose ??= candle.ClosePrice;

				// normalize close prices by dividing on first close
				if (firstClose.Value != 0)
					series[candle.OpenTime] = candle.ClosePrice / firstClose.Value;
			}

			// draw series on chart
			chart.Append(security.ToStringId(), series.Keys, series.Values);
		}
	}
}

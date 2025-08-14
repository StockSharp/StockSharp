namespace StockSharp.Algo.Analytics;

/// <summary>
/// The analytic script, shows biggest candle (by volume and by length) for specified securities.
/// </summary>
public class BiggestCandleScript : IAnalyticsScript
{
	Task IAnalyticsScript.Run(ILogReceiver logs, IAnalyticsPanel panel, SecurityId[] securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, TimeSpan timeFrame, CancellationToken cancellationToken)
	{
		if (securities.Length == 0)
		{
			logs.LogWarning("No instruments.");
			return Task.CompletedTask;
		}

		var priceChart = panel.CreateChart<DateTimeOffset, decimal, decimal>();
		var volChart = panel.CreateChart<DateTimeOffset, decimal, decimal>();

		var bigPriceCandles = new List<CandleMessage>();
		var bigVolCandles = new List<CandleMessage>();

		foreach (var security in securities)
		{
			// stop calculation if user cancel script execution
			if (cancellationToken.IsCancellationRequested)
				break;

			// get candle storage
			var candleStorage = storage.GetTimeFrameCandleMessageStorage(security, timeFrame, drive, format);

			var allCandles = candleStorage.Load(from, to).ToArray();

			// first orders by volume desc will be our biggest candle
			var bigPriceCandle = allCandles.OrderByDescending(c => c.GetLength()).FirstOrDefault();
			var bigVolCandle = allCandles.OrderByDescending(c => c.TotalVolume).FirstOrDefault();

			if (bigPriceCandle != null)
				bigPriceCandles.Add(bigPriceCandle);

			if (bigVolCandle != null)
				bigVolCandles.Add(bigVolCandle);
		}

		// draw series on chart
		priceChart.Append("prices", bigPriceCandles.Select(c => c.OpenTime), bigPriceCandles.Select(c => c.GetMiddlePrice(null)), bigPriceCandles.Select(c => c.GetLength()));
		volChart.Append("prices", bigVolCandles.Select(c => c.OpenTime), bigPriceCandles.Select(c => c.GetMiddlePrice(null)), bigVolCandles.Select(c => c.TotalVolume));

		return Task.CompletedTask;
	}
}
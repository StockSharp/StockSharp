namespace StockSharp.Algo.Analytics;

/// <summary>
/// The analytic script, shows biggest candle (by volume and by length) for specified securities.
/// </summary>
public class BiggestCandleScript : IAnalyticsScript
{
	async Task IAnalyticsScript.Run(ILogReceiver logs, IAnalyticsPanel panel, SecurityId[] securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, DataType dataType, CancellationToken cancellationToken)
	{
		if (securities.Length == 0)
		{
			logs.LogWarning("No instruments.");
			return;
		}

		var priceChart = panel.CreateChart<DateTime, decimal, decimal>();
		var volChart = panel.CreateChart<DateTime, decimal, decimal>();

		var bigPriceCandles = new List<CandleMessage>();
		var bigVolCandles = new List<CandleMessage>();

		var idx = 0;
		foreach (var security in securities)
		{
			// stop calculation if user cancel script execution
			if (cancellationToken.IsCancellationRequested)
				break;

			logs.LogInfo("Processing {0} of {1}: {2}...", ++idx, securities.Length, security);

			// get candle storage
			var candleStorage = storage.GetCandleMessageStorage(security, dataType, drive, format);

			// find biggest candles by iterating through stream
			CandleMessage bigPriceCandle = null;
			CandleMessage bigVolCandle = null;
			decimal maxLength = 0;
			decimal maxVolume = 0;
			var prevDate = default(DateOnly);

			await foreach (var candle in candleStorage.LoadAsync(from, to).WithCancellation(cancellationToken))
			{
				var currDate = DateOnly.FromDateTime(candle.OpenTime.Date);
				if (currDate != prevDate)
				{
					prevDate = currDate;
					logs.LogInfo("  {0}...", currDate);
				}

				var length = candle.GetLength();
				if (bigPriceCandle == null || length > maxLength)
				{
					maxLength = length;
					bigPriceCandle = candle;
				}

				if (bigVolCandle == null || candle.TotalVolume > maxVolume)
				{
					maxVolume = candle.TotalVolume;
					bigVolCandle = candle;
				}
			}

			if (bigPriceCandle != null)
				bigPriceCandles.Add(bigPriceCandle);

			if (bigVolCandle != null)
				bigVolCandles.Add(bigVolCandle);
		}

		// draw series on chart
		priceChart.Append("prices", bigPriceCandles.Select(c => c.OpenTime), bigPriceCandles.Select(c => c.GetMiddlePrice(null)), bigPriceCandles.Select(c => c.GetLength()));
		volChart.Append("prices", bigVolCandles.Select(c => c.OpenTime), bigPriceCandles.Select(c => c.GetMiddlePrice(null)), bigVolCandles.Select(c => c.TotalVolume));
	}
}
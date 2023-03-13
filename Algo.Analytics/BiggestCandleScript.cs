namespace StockSharp.Algo.Analytics
{
	/// <summary>
	/// The analytic script, shows biggest candle (by volume and by length) for specified securities.
	/// </summary>
	public class BiggestCandleScript : IAnalyticsScript
	{
		Task IAnalyticsScript.Run(ILogReceiver logs, IAnalyticsPanel panel, Security[] securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, TimeSpan timeFrame, CancellationToken cancellationToken)
		{
			var priceChart = panel.CreateChart<DateTimeOffset, decimal, decimal>();
			var volChart = panel.CreateChart<DateTimeOffset, decimal, decimal>();

			var bigPriceCandles = new List<CandleMessage>();
			var bigVolCandles = new List<CandleMessage>();

			foreach (var security in securities)
			{
				// get candle storage
				var candleStorage = storage.GetCandleStorage(typeof(TimeFrameCandle), security, timeFrame, format: format);

				var allCandles = candleStorage.Load(from, to).ToArray();

				// first orders by volume desc will be our biggest candle
				var bigPriceCandle = allCandles.OrderByDescending(c => c.GetLength()).FirstOrDefault();
				var bigVolCandle = allCandles.OrderByDescending(c => c.TotalVolume).FirstOrDefault();

				if (bigPriceCandle is not null)
					bigPriceCandles.Add(bigPriceCandle);

				if (bigVolCandle is not null)
					bigVolCandles.Add(bigVolCandle);
			}

			// draw series on chart
			priceChart.Append("prices", bigPriceCandles.Select(c => c.OpenTime), bigPriceCandles.Select(c => c.GetMiddlePrice()), bigPriceCandles.Select(c => c.GetLength()), ChartIndicatorDrawStyles.Bubble);
			priceChart.Append("prices", bigVolCandles.Select(c => c.OpenTime), bigPriceCandles.Select(c => c.GetMiddlePrice()), bigVolCandles.Select(c => c.TotalVolume), ChartIndicatorDrawStyles.Bubble);

			return Task.CompletedTask;
		}
	}
}
namespace StockSharp.Algo.Analytics;

/// <summary>
/// The analytic script, using indicator ROC.
/// </summary>
public class IndicatorScript : IAnalyticsScript
{
	async Task IAnalyticsScript.Run(ILogReceiver logs, IAnalyticsPanel panel, SecurityId[] securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, DataType dataType, CancellationToken cancellationToken)
	{
		if (securities.Length == 0)
		{
			logs.LogWarning("No instruments.");
			return;
		}

		// creating 2 panes for candles and indicator series
		var candleChart = panel.CreateChart<DateTime, decimal>();
		var indicatorChart = panel.CreateChart<DateTime, decimal>();

		var idx = 0;
		foreach (var security in securities)
		{
			// stop calculation if user cancel script execution
			if (cancellationToken.IsCancellationRequested)
				break;

			logs.LogInfo("Processing {0} of {1}: {2}...", ++idx, securities.Length, security);

			var candlesSeries = new Dictionary<DateTime, decimal>();
			var indicatorSeries = new Dictionary<DateTime, decimal>();

			// creating ROC
			var roc = new RateOfChange();

			// get candle storage
			var candleStorage = storage.GetCandleMessageStorage(security, dataType, drive, format);
			var prevDate = default(DateOnly);

			await foreach (var candle in candleStorage.LoadAsync(from, to).WithCancellation(cancellationToken))
			{
				var currDate = DateOnly.FromDateTime(candle.OpenTime.Date);
				if (currDate != prevDate)
				{
					prevDate = currDate;
					logs.LogInfo("  {0}...", currDate);
				}

				// fill series
				candlesSeries[candle.OpenTime] = candle.ClosePrice;
				indicatorSeries[candle.OpenTime] = roc.Process(candle).ToDecimal();
			}

			// draw series on chart
			candleChart.Append($"{security} (close)", candlesSeries.Keys, candlesSeries.Values);
			indicatorChart.Append($"{security} (ROC)", indicatorSeries.Keys, indicatorSeries.Values);
		}
	}
}
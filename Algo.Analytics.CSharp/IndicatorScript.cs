namespace StockSharp.Algo.Analytics;

/// <summary>
/// The analytic script, using indicator ROC.
/// </summary>
public class IndicatorScript : IAnalyticsScript
{
	Task IAnalyticsScript.Run(ILogReceiver logs, IAnalyticsPanel panel, SecurityId[] securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, TimeSpan timeFrame, CancellationToken cancellationToken)
	{
		if (securities.Length == 0)
		{
			logs.LogWarning("No instruments.");
			return Task.CompletedTask;
		}

		// creating 2 panes for candles and indicator series
		var candleChart = panel.CreateChart<DateTimeOffset, decimal>();
		var indicatorChart = panel.CreateChart<DateTimeOffset, decimal>();

		foreach (var security in securities)
		{
			// stop calculation if user cancel script execution
			if (cancellationToken.IsCancellationRequested)
				break;

			var candlesSeries = new Dictionary<DateTimeOffset, decimal>();
			var indicatorSeries = new Dictionary<DateTimeOffset, decimal>();

			// creating ROC
			var roc = new RateOfChange();

			// get candle storage
			var candleStorage = storage.GetTimeFrameCandleMessageStorage(security, timeFrame, drive, format);

			foreach (var candle in candleStorage.Load(from, to))
			{
				// fill series
				candlesSeries[candle.OpenTime] = candle.ClosePrice;
				indicatorSeries[candle.OpenTime] = roc.Process(candle).ToDecimal();
			}

			// draw series on chart
			candleChart.Append($"{security} (close)", candlesSeries.Keys, candlesSeries.Values);
			indicatorChart.Append($"{security} (ROC)", indicatorSeries.Keys, indicatorSeries.Values);
		}

		return Task.CompletedTask;
	}
}
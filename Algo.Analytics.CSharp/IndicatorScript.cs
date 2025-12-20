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

		foreach (var security in securities)
		{
			// stop calculation if user cancel script execution
			if (cancellationToken.IsCancellationRequested)
				break;

			var candlesSeries = new Dictionary<DateTime, decimal>();
			var indicatorSeries = new Dictionary<DateTime, decimal>();

			// creating ROC
			var roc = new RateOfChange();

			// get candle storage
			var candleStorage = storage.GetCandleMessageStorage(security, dataType, drive, format);

			await foreach (var candle in candleStorage.LoadAsync(from, to).WithCancellation(cancellationToken))
			{
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
namespace StockSharp.Algo.Analytics;

using MathNet.Numerics.Statistics;

/// <summary>
/// The analytic script, calculating Pearson correlation by specified securities.
/// </summary>
public class PearsonCorrelationScript : IAnalyticsScript
{
	Task IAnalyticsScript.Run(ILogReceiver logs, IAnalyticsPanel panel, SecurityId[] securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, TimeSpan timeFrame, CancellationToken cancellationToken)
	{
		if (securities.Length == 0)
		{
			logs.LogWarning("No instruments.");
			return Task.CompletedTask;
		}

		var closes = new List<double[]>();

		foreach (var security in securities)
		{
			// stop calculation if user cancel script execution
			if (cancellationToken.IsCancellationRequested)
				break;

			// get candle storage
			var candleStorage = storage.GetTimeFrameCandleMessageStorage(security, timeFrame, drive, format);

			// get closing prices
			var prices = candleStorage.Load(from, to).Select(c => (double)c.ClosePrice).ToArray();

			if (prices.Length == 0)
			{
				logs.LogWarning("No data for {0}", security);
				return Task.CompletedTask;
			}

			closes.Add(prices);
		}

		// all array must be same length, so truncate longer
		var min = closes.Select(arr => arr.Length).Min();

		for (var i = 0; i < closes.Count; i++)
		{
			var arr = closes[i];

			if (arr.Length > min)
				closes[i] = arr.Take(min).ToArray();
		}

		// calculating correlation
		var matrix = Correlation.PearsonMatrix(closes);

		// displaying result into heatmap
		var ids = securities.Select(s => s.ToStringId());
		panel.DrawHeatmap(ids, ids, matrix.ToArray());

		return Task.CompletedTask;
	}
}
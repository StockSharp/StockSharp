namespace StockSharp.Algo.Analytics;

using MathNet.Numerics.Statistics;

/// <summary>
/// The analytic script, calculating Pearson correlation by specified securities.
/// </summary>
public class PearsonCorrelationScript : IAnalyticsScript
{
	async Task IAnalyticsScript.Run(ILogReceiver logs, IAnalyticsPanel panel, SecurityId[] securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, DataType dataType, CancellationToken cancellationToken)
	{
		if (securities.Length == 0)
		{
			logs.LogWarning("No instruments.");
			return;
		}

		var closes = new List<double[]>();

		foreach (var security in securities)
		{
			// stop calculation if user cancel script execution
			if (cancellationToken.IsCancellationRequested)
				break;

			// get candle storage
			var candleStorage = storage.GetCandleMessageStorage(security, dataType, drive, format);

			// get closing prices
			var prices = await candleStorage.LoadAsync(from, to).Select(c => (double)c.ClosePrice).ToArrayAsync(cancellationToken);

			if (prices.Length == 0)
			{
				logs.LogWarning("No data for {0}", security);
				return;
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
	}
}
namespace StockSharp.Algo.Analytics;

using MathNet.Numerics.Statistics;

/// <summary>
/// The analytic script, calculating Pearson correlation by specified securities.
/// </summary>
public class PearsonCorrelationScript : IAnalyticsScript
{
	async Task IAnalyticsScript.Run(ILogReceiver logs, IAnalyticsPanel panel, SecurityId[] securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, DataType dataType, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (securities.Length == 0)
		{
			logs.LogWarning("No instruments.");
			return;
		}

		var closes = new List<Dictionary<DateTime, double>>();

		var idx = 0;
		foreach (var security in securities)
		{
			cancellationToken.ThrowIfCancellationRequested();

			logs.LogInfo("Processing {0} of {1}: {2}...", ++idx, securities.Length, security);
			cancellationToken.ThrowIfCancellationRequested();

			// get candle storage
			var candleStorage = storage.GetCandleMessageStorage(security, dataType, drive, format);

			// get closing prices
			var pricesByTime = new Dictionary<DateTime, double>();
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

				pricesByTime[candle.OpenTime] = (double)candle.ClosePrice;
			}

			if (pricesByTime.Count == 0)
			{
				logs.LogWarning("No data for {0}", security);
				return;
			}

			closes.Add(pricesByTime);
		}

		// A correlation pairs observations of the same market moment. Truncating each series to
		// the same length instead pairs unrelated candles whenever either instrument has a gap.
		var commonTimes = closes[0].Keys
			.Where(time => closes.Skip(1).All(series => series.ContainsKey(time)))
			.OrderBy(time => time)
			.ToArray();

		if (commonTimes.Length == 0)
		{
			logs.LogWarning("The instruments have no candles at the same time.");
			return;
		}

		var pairedCloses = closes
			.Select(series => commonTimes.Select(time => series[time]).ToArray())
			.ToArray();

		// calculating correlation
		var matrix = Correlation.PearsonMatrix(pairedCloses);

		// displaying result into heatmap
		var ids = securities.Select(s => s.ToStringId());
		panel.DrawHeatmap(ids, ids, matrix.ToArray());
	}
}

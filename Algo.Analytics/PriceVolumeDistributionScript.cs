namespace StockSharp.Algo.Analytics
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Collections;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;

	/// <summary>
	/// The analytic script, calculating distribution of the volume by price levels.
	/// </summary>
	public class PriceVolumeDistributionScript : BaseLogReceiver, IAnalyticsScript
	{
		Task IAnalyticsScript.Run(IAnalyticsPanel panel, IEnumerable<Security> securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, TimeSpan timeFrame, CancellationToken cancellationToken)
		{
			// script can process only 1 instrument
			var security = securities.First();

			// get candle storage
			var candleStorage = storage.GetCandleStorage(typeof(TimeFrameCandle), security, timeFrame, format: format);

			// get available dates for the specified period
			var dates = candleStorage.GetDates(from, to).ToArray();

			if (dates.Length == 0)
			{
				this.AddWarningLog("no data");
				return Task.CompletedTask;
			}

			var rows = new Dictionary<decimal, decimal>();

			foreach (var loadDate in dates)
			{
				// check if stopped
				cancellationToken.ThrowIfCancellationRequested();

				// load candles
				var candles = candleStorage.Load(loadDate);

				// grouping candles by candle's middle price
				var groupedCandles = candles.GroupBy(c => c.LowPrice + c.GetLength() / 2);

				foreach (var group in groupedCandles.OrderBy(g => g.Key))
				{
					// check if stopped
					cancellationToken.ThrowIfCancellationRequested();

					var price = group.Key;

					// calc total volume for the specified time frame
					var sumVol = group.Sum(c => c.TotalVolume);

					if (!rows.TryGetValue(price, out var volume))
						volume = sumVol;
					else
						volume += sumVol;

					rows[price] = volume;
				}
			}

			// put our calculations into grid
			var grid = panel.CreateGrid("Price", "Volume");

			// sorting by volume column (descending)
			grid.SetSort("Volume", false);

			foreach (var row in rows)
				grid.SetRow(row.Key, row.Value);

			return Task.CompletedTask;
		}
	}
}
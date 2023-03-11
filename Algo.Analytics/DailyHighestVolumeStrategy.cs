namespace StockSharp.Algo.Analytics
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.Logging;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// The analytic strategy, calculating distribution of the biggest volume by hours.
	/// </summary>
	public class DailyHighestVolumeStrategy : BaseLogReceiver, IAnalyticsScript
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

			var rows = new Dictionary<TimeSpan, decimal>();

			foreach (var loadDate in dates)
			{
				// check if stopped
				cancellationToken.ThrowIfCancellationRequested();

				// load candles
				var candles = candleStorage.Load(loadDate);

				// grouping candles by open time
				var groupedCandles = candles.GroupBy(c => c.OpenTime.TimeOfDay.Truncate(TimeSpan.FromHours(1)));

				foreach (var group in groupedCandles.OrderBy(g => g.Key))
				{
					// check if stopped
					cancellationToken.ThrowIfCancellationRequested();

					var time = group.Key;

					// calc total volume for the specified time frame
					var sumVol = group.Sum(c => c.TotalVolume);

					if (!rows.TryGetValue(time, out var volume))
						volume = sumVol;
					else
						volume += sumVol;
				}
			}

			// draw on chart
			var chart = panel.CreateHistogramChart();

			foreach (var row in rows)
			{
				chart.Append(DateTime.Today + row.Key, row.Value, default);
			}

			return Task.CompletedTask;
		}
	}
}
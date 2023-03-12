namespace StockSharp.Algo.Analytics;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Logging;

/// <summary>
/// Interface describes analytics script.
/// </summary>
public interface IAnalyticsScript
{
	/// <summary>
	/// Run analytics script.
	/// </summary>
	/// <param name="logs"><see cref="ILogReceiver"/></param>
	/// <param name="panel"><see cref="IAnalyticsPanel"/></param>
	/// <param name="securities">Securities.</param>
	/// <param name="from">Begin date.</param>
	/// <param name="to">End date.</param>
	/// <param name="storage"><see cref="IStorageRegistry"/></param>
	/// <param name="drive"><see cref="IMarketDataDrive"/></param>
	/// <param name="format"><see cref="StorageFormats"/></param>
	/// <param name="timeFrame">Time-frame.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	Task Run(ILogReceiver logs, IAnalyticsPanel panel, IEnumerable<Security> securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, TimeSpan timeFrame, CancellationToken cancellationToken);
}
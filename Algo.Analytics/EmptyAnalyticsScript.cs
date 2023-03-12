namespace StockSharp.Algo.Analytics
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;

	/// <summary>
	/// The empty analytic strategy.
	/// </summary>
	public class EmptyAnalyticsScript : BaseLogReceiver, IAnalyticsScript
	{
		Task IAnalyticsScript.Run(IAnalyticsPanel panel, IEnumerable<Security> securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, TimeSpan timeFrame, CancellationToken cancellationToken)
		{
			// !! add logic here

			return Task.CompletedTask;
		}
	}
}
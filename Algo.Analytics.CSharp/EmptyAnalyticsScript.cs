namespace StockSharp.Algo.Analytics;

/// <summary>
/// The empty analytic strategy.
/// </summary>
public class EmptyAnalyticsScript : IAnalyticsScript
{
	Task IAnalyticsScript.Run(ILogReceiver logs, IAnalyticsPanel panel, SecurityId[] securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, TimeSpan timeFrame, CancellationToken cancellationToken)
	{
		if (securities.Length == 0)
		{
			logs.LogWarning("No instruments.");
			return Task.CompletedTask;
		}

		// !! add logic here

		return Task.CompletedTask;
	}
}
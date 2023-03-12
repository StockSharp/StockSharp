namespace StockSharp.Algo.Analytics
{
	/// <summary>
	/// The empty analytic strategy.
	/// </summary>
	public class EmptyAnalyticsScript : IAnalyticsScript
	{
		Task IAnalyticsScript.Run(ILogReceiver logs, IAnalyticsPanel panel, IEnumerable<Security> securities, DateTime from, DateTime to, IStorageRegistry storage, IMarketDataDrive drive, StorageFormats format, TimeSpan timeFrame, CancellationToken cancellationToken)
		{
			// !! add logic here

			return Task.CompletedTask;
		}
	}
}
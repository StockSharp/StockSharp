namespace StockSharp.Algo
{
	using StockSharp.BusinessEntities;

	/// <summary>
	/// The market data by the instrument provider interface.
	/// </summary>
	public interface IMarketDataProviderEx : IMarketDataProvider, ISubscriptionProvider
	{
		/// <summary>
		/// To start getting filtered quotes (order book) by the instrument. Quotes values are available through the event <see cref="IMarketDataProvider.FilteredMarketDepthChanged"/>.
		/// </summary>
		/// <param name="security">The instrument by which quotes getting should be started.</param>
		/// <returns>Subscription.</returns>
		Subscription SubscribeFilteredMarketDepth(Security security);
	}
}
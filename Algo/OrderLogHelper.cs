namespace StockSharp.Algo;

/// <summary>
/// Building order book by the orders log.
/// </summary>
public static class OrderLogHelper
{
	/// <summary>
	/// To check, does the order log contain the order registration.
	/// </summary>
	/// <param name="item">Order log item.</param>
	/// <returns><see langword="true" />, if the order log contains the order registration, otherwise, <see langword="false" />.</returns>
	[Obsolete("Use messages only.")]
	public static bool IsRegistered(this OrderLogItem item)
	{
		return item.ToMessage().IsOrderLogRegistered();
	}

	/// <summary>
	/// To check, does the order log contain the cancelled order.
	/// </summary>
	/// <param name="item">Order log item.</param>
	/// <returns><see langword="true" />, if the order log contain the cancelled order, otherwise, <see langword="false" />.</returns>
	[Obsolete("Use messages only.")]
	public static bool IsCanceled(this OrderLogItem item)
	{
		return item.ToMessage().IsOrderLogCanceled();
	}

	/// <summary>
	/// To check, does the order log contain the order matching.
	/// </summary>
	/// <param name="item">Order log item.</param>
	/// <returns><see langword="true" />, if the order log contains order matching, otherwise, <see langword="false" />.</returns>
	[Obsolete("Use messages only.")]
	public static bool IsMatched(this OrderLogItem item)
	{
		return item.ToMessage().IsOrderLogMatched();
	}

	/// <summary>
	/// To get the reason for cancelling order in orders log.
	/// </summary>
	/// <param name="item">Order log item.</param>
	/// <returns>The reason for order cancelling in order log.</returns>
	[Obsolete("Use messages only.")]
	public static OrderLogCancelReasons GetCancelReason(this OrderLogItem item)
	{
		return item.ToMessage().GetOrderLogCancelReason();
	}

	/// <summary>
	/// Build market depths from order log.
	/// </summary>
	/// <param name="items">Orders log lines.</param>
	/// <param name="builder">Order log to market depth builder.</param>
	/// <param name="interval">The interval of the order book generation. The default is <see cref="TimeSpan.Zero"/>, which means order books generation at each new item of orders log.</param>
	/// <param name="maxDepth">The maximal depth of order book. The default is <see cref="Int32.MaxValue"/>, which means endless depth.</param>
	/// <returns>Market depths.</returns>
	[Obsolete("Use messages only.")]
	public static IEnumerable<MarketDepth> ToOrderBooks(this IEnumerable<OrderLogItem> items, IOrderLogMarketDepthBuilder builder, TimeSpan interval = default, int maxDepth = int.MaxValue)
	{
		var first = items.FirstOrDefault();

		if (first == null)
			return [];

		return items.ToMessages<OrderLogItem, ExecutionMessage>()
			.ToOrderBooks(builder, interval)
			.BuildIfNeed()
			.ToEntities<QuoteChangeMessage, MarketDepth>(first.Order.Security);
	}

	/// <summary>
	/// To build tick trades from the orders log.
	/// </summary>
	/// <param name="items">Orders log lines.</param>
	/// <returns>Tick trades.</returns>
	[Obsolete("Use messages only.")]
	public static IEnumerable<Trade> ToTrades(this IEnumerable<OrderLogItem> items)
	{
		var first = items.FirstOrDefault();

		if (first == null)
			return [];

		var ticks = items
			.Select(i => i.ToMessage())
			.ToTicks();

		return ticks.Select(m => m.ToTrade(first.Order.Security));
	}
}

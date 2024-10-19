namespace StockSharp.BusinessEntities;

/// <summary>
/// Order log item.
/// </summary>
[Serializable]
[System.Runtime.Serialization.DataContract]
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OrderLogOfKey,
	Description = LocalizedStrings.OrderLogDescKey)]
[Obsolete("Use IOrderLogMessage.")]
public class OrderLogItem : MyTrade, IOrderLogMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="OrderLogItem"/>.
	/// </summary>
	public OrderLogItem()
	{
	}

	IOrderMessage IOrderLogMessage.Order => Order;
	ITickTradeMessage IOrderLogMessage.Trade => Trade;

	/// <inheritdoc />
	public override string ToString()
	{
		var result = LocalizedStrings.OLFromOrder.Put(Trade == null ? (Order.State == OrderStates.Done ? LocalizedStrings.Cancellation : LocalizedStrings.Registration) : LocalizedStrings.Matching, Order);

		if (Trade != null)
			result += " " + LocalizedStrings.OLFromTrade.Put(Trade);

		return result;
	}
}
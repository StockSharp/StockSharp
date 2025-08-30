namespace StockSharp.Algo.Commissions;

/// <summary>
/// Order commission.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OrderKey,
	Description = LocalizedStrings.OrderCommissionKey,
	GroupName = LocalizedStrings.OrdersKey)]
public class CommissionOrderRule : CommissionRule
{
	/// <inheritdoc />
	public override decimal? Process(ExecutionMessage message)
	{
		if (!message.HasOrderInfo())
			return null;

		var price = message.HasTradeInfo() ? message.TradePrice : message.OrderPrice;
		var volume = message.HasTradeInfo() ? message.TradeVolume : message.OrderVolume;

		return GetValue(price, volume);
	}
}

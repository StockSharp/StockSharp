namespace StockSharp.Algo.Commissions;

/// <summary>
/// Order volume commission.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OrderVolume2Key,
	Description = LocalizedStrings.OrderVolCommissionKey,
	GroupName = LocalizedStrings.OrdersKey)]
public class CommissionOrderVolumeRule : CommissionRule
{
	/// <inheritdoc />
	public override decimal? Process(ExecutionMessage message)
	{
		if (!message.HasOrderInfo())
			return null;

		var price = message.HasTradeInfo() ? message.TradePrice : message.OrderPrice;
		var volume = message.HasTradeInfo() ? message.TradeVolume : message.OrderVolume;

		if (Value.Type == UnitTypes.Percent)
		{
			// percent of turnover
			return price * volume * (Value.Value / 100m);
		}

		// absolute per volume
		return volume * Value.Value;
	}
}
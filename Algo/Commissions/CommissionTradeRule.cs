namespace StockSharp.Algo.Commissions;

/// <summary>
/// Trade commission.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TradeKey,
	Description = LocalizedStrings.TradeCommissionKey,
	GroupName = LocalizedStrings.TradesKey)]
public class CommissionTradeRule : CommissionRule
{
	/// <inheritdoc />
	public override decimal? Process(ExecutionMessage message)
	{
		if (message.HasTradeInfo())
			return GetValue(message.TradePrice, message.TradeVolume);

		return null;
	}
}

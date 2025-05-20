namespace StockSharp.Algo.Commissions;

/// <summary>
/// Trade price commission.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TradePriceKey,
	Description = LocalizedStrings.TradePriceCommissionKey,
	GroupName = LocalizedStrings.TradesKey)]
public class CommissionTradePriceRule : CommissionRule
{
	/// <inheritdoc />
	public override decimal? Process(ExecutionMessage message)
	{
		if (message.HasTradeInfo())
			return (decimal)(message.TradePrice * message.TradeVolume * Value);

		return null;
	}
}

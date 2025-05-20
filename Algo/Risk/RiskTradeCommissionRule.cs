namespace StockSharp.Algo.Risk;

/// <summary>
/// Risk-rule, tracking total commission for own trades.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TradeCommissionKey,
	Description = LocalizedStrings.RiskTradeCommissionKey,
	GroupName = LocalizedStrings.PnLKey)]
public class RiskTradeCommissionRule : RiskTransactionCommissionRule
{
	/// <inheritdoc />
	protected override bool IsMatch(ExecutionMessage execMsg)
		=> execMsg.HasTradeInfo();
}

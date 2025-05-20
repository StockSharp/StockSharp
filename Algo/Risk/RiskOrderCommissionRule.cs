namespace StockSharp.Algo.Risk;

/// <summary>
/// Risk-rule, tracking total commission for order registrations.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OrderCommissionKey,
	Description = LocalizedStrings.RiskOrderCommissionKey,
	GroupName = LocalizedStrings.OrdersKey)]
public class RiskOrderCommissionRule : RiskTransactionCommissionRule
{
	/// <inheritdoc />
	protected override bool IsMatch(ExecutionMessage execMsg)
		=> execMsg.HasOrderInfo();
}

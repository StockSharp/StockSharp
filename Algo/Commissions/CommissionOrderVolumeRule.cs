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
		if (message.HasOrderInfo())
			return (decimal)(message.OrderVolume * Value);

		return null;
	}
}

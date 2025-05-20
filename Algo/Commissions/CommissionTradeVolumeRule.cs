namespace StockSharp.Algo.Commissions;

/// <summary>
/// Trade volume commission.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TradeVolumeKey,
	Description = LocalizedStrings.TradeVolCommissionKey,
	GroupName = LocalizedStrings.TradesKey)]
public class CommissionTradeVolumeRule : CommissionRule
{
	/// <inheritdoc />
	public override decimal? Process(ExecutionMessage message)
	{
		if (message.HasTradeInfo())
			return (decimal)(message.TradeVolume * Value);

		return null;
	}
}

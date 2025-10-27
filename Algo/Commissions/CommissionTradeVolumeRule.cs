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
		if (!message.HasTradeInfo())
			return null;

		var price = message.GetTradePrice();
		var volume = message.GetTradeVolume();

		if (Value.Type == UnitTypes.Percent)
		{
			// percent of turnover
			return price * volume * (Value.Value / 100m);
		}

		// absolute per volume
		return volume * Value.Value;
	}
}
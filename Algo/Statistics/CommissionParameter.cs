namespace StockSharp.Algo.Statistics;

/// <summary>
/// Total commission.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CommissionKey,
	Description = LocalizedStrings.TotalCommissionDescKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 10
)]
public class CommissionParameter : BasePnLStatisticParameter<decimal>
{
	/// <summary>
	/// Initialize <see cref="CommissionParameter"/>.
	/// </summary>
	public CommissionParameter()
		: base(StatisticParameterTypes.Commission)
	{
	}

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		if (commission is decimal c)
			Value += c;
	}
}

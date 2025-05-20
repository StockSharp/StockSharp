namespace StockSharp.Algo.Statistics;

/// <summary>
/// The maximal profit value for the entire period.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MaxProfitKey,
	Description = LocalizedStrings.MaxProfitWholePeriodKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 2
)]
public class MaxProfitParameter : BasePnLStatisticParameter<decimal>
{
	/// <summary>
	/// Initialize <see cref="MaxProfitParameter"/>.
	/// </summary>
	public MaxProfitParameter()
		: base(StatisticParameterTypes.MaxProfit)
	{
	}

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		Value = Math.Max(Value, pnl);
	}
}

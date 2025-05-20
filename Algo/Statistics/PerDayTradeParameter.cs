namespace StockSharp.Algo.Statistics;

/// <summary>
/// Average trades count per one day.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PerDayTradesKey,
	Description = LocalizedStrings.PerDayTradesDescKey,
	GroupName = LocalizedStrings.TradesKey,
	Order = 108)]
public class PerDayTradeParameter : PerPeriodBaseTradeParameter
{
	/// <summary>
	/// Initialize <see cref="PerDayTradeParameter"/>.
	/// </summary>
	public PerDayTradeParameter()
		: base(StatisticParameterTypes.PerDayTrades)
	{
	}

	/// <inheritdoc/>
	protected override DateTime Align(DateTime date) => date.Date;
}

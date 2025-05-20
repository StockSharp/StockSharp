namespace StockSharp.Algo.Statistics;

/// <summary>
/// Average trades count per one month.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PerMonthTradesKey,
	Description = LocalizedStrings.PerMonthTradesDescKey,
	GroupName = LocalizedStrings.TradesKey,
	Order = 107)]	
public class PerMonthTradeParameter : PerPeriodBaseTradeParameter
{
	/// <summary>
	/// Initialize <see cref="PerMonthTradeParameter"/>.
	/// </summary>
	public PerMonthTradeParameter()
		: base(StatisticParameterTypes.PerMonthTrades)
	{
	}

	/// <inheritdoc/>
	protected override DateTime Align(DateTime date) => new(date.Year, date.Month, 1);
}

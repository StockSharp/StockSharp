namespace StockSharp.Algo.Statistics;

using StockSharp.Algo.PnL;

/// <summary>
/// Number of trades won (whose profit is greater than 0).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ProfitTradesKey,
	Description = LocalizedStrings.ProfitTradesDescKey,
	GroupName = LocalizedStrings.TradesKey,
	Order = 100
)]
public class WinningTradesParameter : BaseStatisticParameter<int>, ITradeStatisticParameter
{
	/// <summary>
	/// Initialize <see cref="WinningTradesParameter"/>.
	/// </summary>
	public WinningTradesParameter()
		: base(StatisticParameterTypes.WinningTrades)
	{
	}

	/// <inheritdoc />
	public void Add(PnLInfo info)
	{
		if (info == null)
			throw new ArgumentNullException(nameof(info));

		if (info.ClosedVolume > 0 && info.PnL > 0)
			Value++;
	}
}

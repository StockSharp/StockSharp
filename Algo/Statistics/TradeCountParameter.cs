namespace StockSharp.Algo.Statistics;

using StockSharp.Algo.PnL;

/// <summary>
/// Total number of trades.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TotalTradesKey,
	Description = LocalizedStrings.TotalTradesDescKey,
	GroupName = LocalizedStrings.TradesKey,
	Order = 102
)]
public class TradeCountParameter : BaseStatisticParameter<int>, ITradeStatisticParameter
{
	/// <summary>
	/// Initialize <see cref="TradeCountParameter"/>.
	/// </summary>
	public TradeCountParameter()
		: base(StatisticParameterTypes.TradeCount)
	{
	}

	/// <inheritdoc />
	public void Add(PnLInfo info)
	{
		ArgumentNullException.ThrowIfNull(info);

		if (info.ClosedVolume > 0)
			Value++;
	}
}

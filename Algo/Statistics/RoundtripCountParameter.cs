namespace StockSharp.Algo.Statistics;

using StockSharp.Algo.PnL;

/// <summary>
/// Total number of closing trades.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ClosingTradesKey,
	Description = LocalizedStrings.ClosingTradesDescKey,
	GroupName = LocalizedStrings.TradesKey,
	Order = 103
)]
public class RoundtripCountParameter : BaseStatisticParameter<int>, ITradeStatisticParameter
{
	/// <summary>
	/// Initialize <see cref="RoundtripCountParameter"/>.
	/// </summary>
	public RoundtripCountParameter()
		: base(StatisticParameterTypes.RoundtripCount)
	{
	}

	/// <inheritdoc />
	public void Add(PnLInfo info)
	{
		if (info.ClosedVolume > 0)
			Value++;
	}
}

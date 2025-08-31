namespace StockSharp.Algo.Statistics;

using StockSharp.Algo.PnL;

/// <summary>
/// Number of trades lost with negative profit.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.LossTradesKey,
	Description = LocalizedStrings.LossTradesDescKey,
	GroupName = LocalizedStrings.TradesKey,
	Order = 101
)]
public class LossingTradesParameter : BaseStatisticParameter<int>, ITradeStatisticParameter
{
	/// <summary>
	/// Initialize <see cref="LossingTradesParameter"/>.
	/// </summary>
	public LossingTradesParameter()
		: base(StatisticParameterTypes.LossingTrades)
	{
	}

	/// <inheritdoc />
	public void Add(PnLInfo info)
	{
		if (info == null)
			throw new ArgumentNullException(nameof(info));

		if (info.ClosedVolume > 0 && info.PnL < 0)
			Value++;
	}
}
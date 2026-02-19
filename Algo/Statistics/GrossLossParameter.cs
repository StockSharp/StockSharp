namespace StockSharp.Algo.Statistics;

using StockSharp.Algo.PnL;

/// <summary>
/// Total currency amount of all completed losing trades.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.GrossLossKey,
	Description = LocalizedStrings.GrossLossDescKey,
	GroupName = LocalizedStrings.TradesKey,
	Order = 111
)]
public class GrossLossParameter : BaseStatisticParameter<decimal>, ITradeStatisticParameter
{
	/// <summary>
	/// Initialize <see cref="GrossLossParameter"/>.
	/// </summary>
	public GrossLossParameter()
		: base(StatisticParameterTypes.GrossLoss)
	{
	}

	/// <inheritdoc />
	public void Add(PnLInfo info)
	{
		if (info == null)
			throw new ArgumentNullException(nameof(info));

		if (info.ClosedVolume == 0)
			return;

		if (info.PnL < 0)
			Value += info.PnL;
	}
}

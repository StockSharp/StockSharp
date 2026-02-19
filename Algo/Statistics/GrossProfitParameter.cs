namespace StockSharp.Algo.Statistics;

using StockSharp.Algo.PnL;

/// <summary>
/// Total currency amount of all completed winning trades.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.GrossProfitKey,
	Description = LocalizedStrings.GrossProfitDescKey,
	GroupName = LocalizedStrings.TradesKey,
	Order = 110
)]
public class GrossProfitParameter : BaseStatisticParameter<decimal>, ITradeStatisticParameter
{
	/// <summary>
	/// Initialize <see cref="GrossProfitParameter"/>.
	/// </summary>
	public GrossProfitParameter()
		: base(StatisticParameterTypes.GrossProfit)
	{
	}

	/// <inheritdoc />
	public void Add(PnLInfo info)
	{
		if (info == null)
			throw new ArgumentNullException(nameof(info));

		if (info.ClosedVolume == 0)
			return;

		if (info.PnL > 0)
			Value += info.PnL;
	}
}

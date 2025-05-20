namespace StockSharp.Algo.Statistics;

/// <summary>
/// Net profit for whole time period.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.NetProfitKey,
	Description = LocalizedStrings.NetProfitWholeTimeKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 0
)]
public class NetProfitParameter : BasePnLStatisticParameter<decimal>
{
	/// <summary>
	/// Initialize <see cref="NetProfitParameter"/>.
	/// </summary>
	public NetProfitParameter()
		: base(StatisticParameterTypes.NetProfit)
	{
	}

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		Value = pnl;
	}
}

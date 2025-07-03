namespace StockSharp.Algo.Statistics;

/// <summary>
/// Sterling ratio (annualized net profit / average drawdown).
/// </summary>
/// <remarks>
/// Initialize <see cref="SterlingRatioParameter"/>.
/// </remarks>
/// <param name="profit"><see cref="NetProfitParameter"/></param>
/// <param name="avgDrawdown"><see cref="AverageDrawdownParameter"/></param>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.SterlingRatioKey,
	Description = LocalizedStrings.SterlingRatioDescKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 14
)]
public class SterlingRatioParameter(NetProfitParameter profit, AverageDrawdownParameter avgDrawdown) : BasePnLStatisticParameter<decimal>(StatisticParameterTypes.SterlingRatio)
{
	private readonly NetProfitParameter _profit = profit ?? throw new ArgumentNullException(nameof(profit));
	private readonly AverageDrawdownParameter _avgDrawdown = avgDrawdown ?? throw new ArgumentNullException(nameof(avgDrawdown));

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		var annualizedProfit = _profit.Value;
		var avgDrawdown = _avgDrawdown.Value;

		Value = avgDrawdown != 0 ? annualizedProfit / avgDrawdown : 0;
	}
}

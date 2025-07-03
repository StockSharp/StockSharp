namespace StockSharp.Algo.Statistics;

/// <summary>
/// Calmar ratio (annualized net profit / max drawdown).
/// </summary>
/// <remarks>
/// Initialize <see cref="CalmarRatioParameter"/>.
/// </remarks>
/// <param name="profit"><see cref="NetProfitParameter"/></param>
/// <param name="maxDrawdown"><see cref="MaxDrawdownParameter"/></param>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CalmarRatioKey,
	Description = LocalizedStrings.CalmarRatioDescKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 13
)]
public class CalmarRatioParameter(NetProfitParameter profit, MaxDrawdownParameter maxDrawdown) : BasePnLStatisticParameter<decimal>(StatisticParameterTypes.CalmarRatio)
{
	private readonly NetProfitParameter _profit = profit ?? throw new ArgumentNullException(nameof(profit));
	private readonly MaxDrawdownParameter _maxDrawdown = maxDrawdown ?? throw new ArgumentNullException(nameof(maxDrawdown));

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		var annualizedProfit = _profit.Value;
		var maxDrawdown = _maxDrawdown.Value;

		Value = maxDrawdown != 0 ? annualizedProfit / maxDrawdown : 0;
	}
}

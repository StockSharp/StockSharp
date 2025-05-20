namespace StockSharp.Algo.Statistics;

/// <summary>
/// Recovery factor (net profit / maximum drawdown).
/// </summary>
/// <remarks>
/// Initialize <see cref="RecoveryFactorParameter"/>.
/// </remarks>
/// <param name="maxDrawdown"><see cref="MaxDrawdownParameter"/></param>
/// <param name="netProfit"><see cref="NetProfitParameter"/></param>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.RecoveryFactorKey,
	Description = LocalizedStrings.RecoveryFactorDescKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 9
)]
public class RecoveryFactorParameter(MaxDrawdownParameter maxDrawdown, NetProfitParameter netProfit) : BasePnLStatisticParameter<decimal>(StatisticParameterTypes.RecoveryFactor)
{
	private readonly MaxDrawdownParameter _maxDrawdown = maxDrawdown ?? throw new ArgumentNullException(nameof(maxDrawdown));
	private readonly NetProfitParameter _netProfit = netProfit ?? throw new ArgumentNullException(nameof(netProfit));

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		if (_maxDrawdown.Value != 0)
			Value = _netProfit.Value / _maxDrawdown.Value;
	}
}

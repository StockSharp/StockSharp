namespace StockSharp.Algo.Statistics;

/// <summary>
/// Maximum absolute drawdown during the whole period in percent.
/// </summary>
/// <remarks>
/// Initialize <see cref="MaxDrawdownPercentParameter"/>.
/// </remarks>
/// <param name="underlying"><see cref="MaxDrawdownParameter"/></param>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MaxDrawdownPercentKey,
	Description = LocalizedStrings.MaxDrawdownPercentKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 5
)]
public class MaxDrawdownPercentParameter(MaxDrawdownParameter underlying) : BasePnLStatisticParameter<decimal>(StatisticParameterTypes.MaxDrawdownPercent)
{
	private readonly MaxDrawdownParameter _underlying = underlying ?? throw new ArgumentNullException(nameof(underlying));

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		var maxEquity = _underlying.MaxEquity;

		Value = maxEquity != 0 ? (_underlying.Value * 100m / maxEquity) : 0;
	}
}

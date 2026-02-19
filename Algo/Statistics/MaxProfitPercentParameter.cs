namespace StockSharp.Algo.Statistics;

/// <summary>
/// Maximum profit value for the period, expressed as a percentage.
/// </summary>
/// <remarks>
/// Initialize <see cref="MaxProfitPercentParameter"/>.
/// </remarks>
/// <param name="underlying"><see cref="MaxProfitParameter"/></param>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MaxProfitPercentKey,
	Description = LocalizedStrings.MaxProfitPercentDescKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 3
)]
public class MaxProfitPercentParameter(MaxProfitParameter underlying) : BasePnLStatisticParameter<decimal>(StatisticParameterTypes.MaxProfitPercent), IBeginValueStatisticParameter
{
	private readonly MaxProfitParameter _underlying = underlying ?? throw new ArgumentNullException(nameof(underlying));

	/// <inheritdoc />
	public decimal BeginValue { get; set; }

	/// <inheritdoc />
	public override void Add(DateTime marketTime, decimal pnl, decimal? commission)
	{
		if (BeginValue == 0)
			return;

		Value = _underlying.Value * 100m / BeginValue;
	}
}

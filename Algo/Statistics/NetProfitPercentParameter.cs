namespace StockSharp.Algo.Statistics;

/// <summary>
/// Net profit for whole time period in percent.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.NetProfitPercentKey,
	Description = LocalizedStrings.NetProfitPercentDescKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 1
)]
public class NetProfitPercentParameter : BasePnLStatisticParameter<decimal>
{
	private decimal _beginValue;

	/// <summary>
	/// Initialize <see cref="NetProfitPercentParameter"/>.
	/// </summary>
	public NetProfitPercentParameter()
		: base(StatisticParameterTypes.NetProfitPercent)
	{
	}

	/// <inheritdoc />
	public override void Init(decimal beginValue)
	{
		base.Init(beginValue);

		_beginValue = beginValue;
	}

	/// <inheritdoc />
	public override void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
	{
		if (_beginValue == 0)
			return;

		Value = pnl * 100m / _beginValue;
	}
}
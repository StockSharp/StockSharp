namespace StockSharp.Algo.Statistics;

/// <summary>
/// Sharpe ratio (annualized return - risk-free rate / annualized standard deviation).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.SharpeRatioKey,
	Description = LocalizedStrings.SharpeRatioDescKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 11
)]
public class SharpeRatioParameter : RiskAdjustedRatioParameter
{
	private decimal _sumSq; // Sum of squared returns

	/// <summary>
	/// Initialize a new instance of the <see cref="SharpeRatioParameter"/> class.
	/// </summary>
	public SharpeRatioParameter()
		: base(StatisticParameterTypes.SharpeRatio)
	{
	}

	/// <inheritdoc />
	protected override void AddRiskSample(decimal ret)
	{
		_sumSq += ret * ret;
	}

	/// <inheritdoc />
	protected override decimal GetRisk(int count, decimal sumReturn)
	{
		if (count < 2)
			return 0;

		var avg = sumReturn / count;
		return (decimal)Math.Sqrt((double)((_sumSq - avg * avg * count) / (count - 1)));
	}

	/// <inheritdoc />
	protected override bool HasEnoughRiskSamples(int count)
		=> count >= 2;

	/// <inheritdoc />
	public override void Reset()
	{
		_sumSq = 0;
		base.Reset();
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue("SumSq", _sumSq);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		_sumSq = storage.GetValue<decimal>("SumSq");
	}
}

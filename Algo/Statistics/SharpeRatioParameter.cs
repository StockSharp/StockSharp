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
	protected override void AddRiskSample(decimal ret, long count)
	{
		_sumSq += ret * ret * count;
	}

	/// <inheritdoc />
	protected override decimal GetRisk(long count, decimal sumReturn)
	{
		if (count < 2)
			return 0;

		var avg = sumReturn / count;
		var variance = (_sumSq - avg * avg * count) / (count - 1);
		return variance > 0 ? (decimal)Math.Sqrt((double)variance) : 0;
	}

	/// <inheritdoc />
	protected override bool HasEnoughRiskSamples(long count)
		=> count >= 2;

	/// <inheritdoc />
	public override void Reset()
	{
		_sumSq = 0;
		base.Reset();
	}

	/// <inheritdoc />
	public override async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		await base.SaveAsync(storage, cancellationToken);

		storage.SetValue("SumSq", _sumSq);
	}

	/// <inheritdoc />
	public override async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		await base.LoadAsync(storage, cancellationToken);

		_sumSq = storage.GetValue<decimal>("SumSq");
	}
}

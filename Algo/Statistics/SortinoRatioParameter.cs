namespace StockSharp.Algo.Statistics;

/// <summary>
/// Sortino ratio (annualized return - risk-free rate / annualized downside deviation).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.SortinoRatioKey,
	Description = LocalizedStrings.SortinoRatioDescKey,
	GroupName = LocalizedStrings.PnLKey,
	Order = 12
)]
public class SortinoRatioParameter : RiskAdjustedRatioParameter
{
	private decimal _downsideSumSq;

	/// <summary>
	/// Initialize a new instance of the <see cref="SortinoRatioParameter"/> class.
	/// </summary>
	public SortinoRatioParameter()
		: base(StatisticParameterTypes.SortinoRatio)
	{
	}

	/// <inheritdoc />
	protected override void AddRiskSample(decimal ret, long count)
	{
		if (ret >= 0)
			return;

		_downsideSumSq += ret * ret * count;
	}

	/// <inheritdoc />
	protected override decimal GetRisk(long count, decimal sumReturn)
	{
		return count > 0
			? (decimal)Math.Sqrt((double)(_downsideSumSq / count))
			: 0;
	}

	/// <inheritdoc />
	protected override bool HasEnoughRiskSamples(long count)
		=> count >= 2;

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_downsideSumSq = 0;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.Set("DownsideSumSq", _downsideSumSq);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		_downsideSumSq = storage.GetValue<decimal>("DownsideSumSq");
	}
}

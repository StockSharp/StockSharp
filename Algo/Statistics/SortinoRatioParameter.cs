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
	private int _downsideCount;

	/// <summary>
	/// Initialize a new instance of the <see cref="SortinoRatioParameter"/> class.
	/// </summary>
	public SortinoRatioParameter()
		: base(StatisticParameterTypes.SortinoRatio)
	{
	}

	/// <inheritdoc />
	protected override void AddRiskSample(decimal ret)
	{
		if (ret >= 0)
			return;

		_downsideSumSq += ret * ret;
		_downsideCount++;
	}

	/// <inheritdoc />
	protected override decimal GetRisk(int count, decimal sumReturn)
	{
		return _downsideCount > 0
			? (decimal)Math.Sqrt((double)(_downsideSumSq / _downsideCount))
			: 0;
	}

	/// <inheritdoc />
	protected override bool HasEnoughRiskSamples(int count)
		=> _downsideCount > 0;

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_downsideSumSq = 0;
		_downsideCount = 0;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.Set("DownsideSumSq", _downsideSumSq);
		storage.Set("DownsideCount", _downsideCount);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		_downsideSumSq = storage.GetValue<decimal>("DownsideSumSq");
		_downsideCount = storage.GetValue<int>("DownsideCount");
	}
}

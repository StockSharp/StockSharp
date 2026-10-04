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
public class NetProfitPercentParameter : BasePnLStatisticParameter<decimal>, IBeginValueStatisticParameter
{
	/// <summary>
	/// Initialize <see cref="NetProfitPercentParameter"/>.
	/// </summary>
	public NetProfitPercentParameter()
		: base(StatisticParameterTypes.NetProfitPercent)
	{
	}

	/// <inheritdoc />
	public decimal BeginValue { get; set; }

	/// <inheritdoc />
	public override void Add(DateTime marketTime, decimal pnl, decimal? commission)
	{
		if (BeginValue == 0)
			return;

		Value = pnl * 100m / BeginValue;
	}

	/// <inheritdoc />
	public override async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		await base.LoadAsync(storage, cancellationToken);
		BeginValue = storage.GetValue(nameof(BeginValue), BeginValue);
	}

	/// <inheritdoc />
	public override async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		await base.SaveAsync(storage, cancellationToken);
		storage.SetValue(nameof(BeginValue), BeginValue);
	}
}

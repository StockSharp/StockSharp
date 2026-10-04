namespace StockSharp.Algo.Statistics;

using StockSharp.Algo.PnL;

/// <summary>
/// Average trade profit.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.AverageProfitKey,
	Description = LocalizedStrings.AverageTradeProfitKey,
	GroupName = LocalizedStrings.TradesKey,
	Order = 104
)]
public class AverageTradeProfitParameter : BaseStatisticParameter<decimal>, ITradeStatisticParameter
{
	/// <summary>
	/// Initialize <see cref="AverageTradeProfitParameter"/>.
	/// </summary>
	public AverageTradeProfitParameter()
		: base(StatisticParameterTypes.AverageTradeProfit)
	{
	}

	private decimal _sum;
	private int _count;

	/// <inheritdoc />
	public override void Reset()
	{
		_sum = 0;
		_count = 0;
		base.Reset();
	}

	/// <inheritdoc />
	public void Add(PnLInfo info)
	{
		if (info == null)
			throw new ArgumentNullException(nameof(info));

		if (info.ClosedVolume == 0)
			return;

		_sum += info.PnL;
		_count++;

		Value = _count > 0 ? _sum / _count : 0;
	}

	/// <inheritdoc />
	public override async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		storage.Set("Sum", _sum);
		storage.Set("Count", _count);

		await base.SaveAsync(storage, cancellationToken);
	}

	/// <inheritdoc />
	public override async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		_sum = storage.GetValue<decimal>("Sum");
		_count = storage.GetValue<int>("Count");

		await base.LoadAsync(storage, cancellationToken);
	}
}

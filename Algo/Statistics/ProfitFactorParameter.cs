namespace StockSharp.Algo.Statistics;

using StockSharp.Algo.PnL;

/// <summary>
/// The ratio of the average profit of winning trades to the average loss of losing trades.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ProfitFactorKey,
	Description = LocalizedStrings.ProfitFactorDescKey,
	GroupName = LocalizedStrings.TradesKey,
	Order = 109)]
public class ProfitFactorParameter : BaseStatisticParameter<decimal>, ITradeStatisticParameter
{
	private decimal _grossProfit;
	private decimal _grossLoss;

	/// <summary>
	/// Initialize <see cref="ProfitFactorParameter"/>.
	/// </summary>
	public ProfitFactorParameter()
		: base(StatisticParameterTypes.ProfitFactor)
	{
	}

	/// <inheritdoc/>
	public void Add(PnLInfo info)
	{
		ArgumentNullException.ThrowIfNull(info);

		if (info.ClosedVolume == 0)
			return;

		if (info.PnL > 0)
			_grossProfit += info.PnL;
		else if (info.PnL < 0)
			_grossLoss -= info.PnL;

		// When no losses, profit factor is effectively infinite (use MaxValue as proxy)
		Value = _grossLoss > 0 ? _grossProfit / _grossLoss : (_grossProfit > 0 ? decimal.MaxValue : 0);
	}

	/// <inheritdoc/>
	public override void Reset()
	{
		_grossProfit = 0;
		_grossLoss = 0;

		base.Reset();
	}

	/// <inheritdoc/>
	public override async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		storage.Set("GrossProfit", _grossProfit);
		storage.Set("GrossLoss", _grossLoss);

		await base.SaveAsync(storage, cancellationToken);
	}

	/// <inheritdoc/>
	public override async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		_grossProfit = storage.GetValue<decimal>("GrossProfit");
		_grossLoss = storage.GetValue<decimal>("GrossLoss");

		await base.LoadAsync(storage, cancellationToken);
	}
}

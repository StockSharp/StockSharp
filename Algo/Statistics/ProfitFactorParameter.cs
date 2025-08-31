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

		Value = _grossLoss > 0 ? _grossProfit / _grossLoss : 0;
	}

	/// <inheritdoc/>
	public override void Reset()
	{
		_grossProfit = 0;
		_grossLoss = 0;

		base.Reset();
	}

	/// <inheritdoc/>
	public override void Save(SettingsStorage storage)
	{
		storage.Set("GrossProfit", _grossProfit);
		storage.Set("GrossLoss", _grossLoss);

		base.Save(storage);
	}

	/// <inheritdoc/>
	public override void Load(SettingsStorage storage)
	{
		_grossProfit = storage.GetValue<decimal>("GrossProfit");
		_grossLoss = storage.GetValue<decimal>("GrossLoss");

		base.Load(storage);
	}
}

namespace StockSharp.Algo.Statistics;

using StockSharp.Algo.PnL;

/// <summary>
/// Average losing trade.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.AverageLossKey,
	Description = LocalizedStrings.AverageLossTradeKey,
	GroupName = LocalizedStrings.TradesKey,
	Order = 106
)]
public class AverageLossTradeParameter : BaseStatisticParameter<decimal>, ITradeStatisticParameter
{
	/// <summary>
	/// Initialize <see cref="AverageLossTradeParameter"/>.
	/// </summary>
	public AverageLossTradeParameter()
		: base(StatisticParameterTypes.AverageLossTrades)
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

		if (info.PnL < 0)
		{
			_sum += info.PnL;
			_count++;
		}

		Value = _count > 0 ? _sum / _count : 0;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		storage.Set("Sum", _sum);
		storage.Set("Count", _count);

		base.Save(storage);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		_sum = storage.GetValue<decimal>("Sum");
		_count = storage.GetValue<int>("Count");

		base.Load(storage);
	}
}
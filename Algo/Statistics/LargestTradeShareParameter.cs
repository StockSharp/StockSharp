namespace StockSharp.Algo.Statistics;

using StockSharp.Algo.PnL;

/// <summary>
/// The share of the net result made by the single most profitable closing trade, in percent.
/// </summary>
/// <remarks>
/// Zero while the net result is not a profit: a share of a loss is not a share of anything, and dividing
/// by a negative total would make the trade that lost least look like the one that carried the result.
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.LargestTradeShareKey,
	Description = LocalizedStrings.LargestTradeShareDescKey,
	GroupName = LocalizedStrings.TradesKey,
	Order = 112
)]
public class LargestTradeShareParameter : BaseStatisticParameter<decimal>, ITradeStatisticParameter
{
	private decimal _net;
	private decimal? _best;

	/// <summary>
	/// Initializes a new instance of the <see cref="LargestTradeShareParameter"/>.
	/// </summary>
	public LargestTradeShareParameter()
		: base(StatisticParameterTypes.LargestTradeShare)
	{
	}

	/// <inheritdoc />
	public void Add(PnLInfo info)
	{
		if (info is null)
			throw new ArgumentNullException(nameof(info));

		if (info.ClosedVolume == 0)
			return;

		_net += info.PnL;

		if (_best is not decimal best || info.PnL > best)
			_best = info.PnL;

		Value = _net <= 0 || _best is not decimal top ? 0m : top / _net * 100m;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_net = default;
		_best = default;

		base.Reset();
	}

	/// <inheritdoc />
	public override async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		storage
			.Set("Net", _net)
			.Set("Best", _best);

		await base.SaveAsync(storage, cancellationToken);
	}

	/// <inheritdoc />
	public override async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		_net = storage.GetValue<decimal>("Net");
		_best = storage.GetValue<decimal?>("Best");

		await base.LoadAsync(storage, cancellationToken);
	}
}

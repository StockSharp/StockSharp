namespace StockSharp.Algo.Statistics;

using StockSharp.Algo.PnL;

/// <summary>
/// The average profit of winning trades minus the average loss of losing trades.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ExpectancyKey,
	Description = LocalizedStrings.ExpectancyDescKey,
	GroupName = LocalizedStrings.TradesKey,
	Order = 110)]
public class ExpectancyParameter : BaseStatisticParameter<decimal>, ITradeStatisticParameter
{
	private int _winCount;
	private int _lossCount;
	private decimal _winSum;
	private decimal _lossSum;

	/// <summary>
	/// Initialize <see cref="ExpectancyParameter"/>.
	/// </summary>
	public ExpectancyParameter()
		: base(StatisticParameterTypes.Expectancy)
	{
	}

	/// <inheritdoc/>
	public void Add(PnLInfo info)
	{
		if (info.PnL > 0)
		{
			_winCount++;
			_winSum += info.PnL;
		}
		else if (info.PnL < 0)
		{
			_lossCount++;
			_lossSum += info.PnL;
		}

		var total = _winCount + _lossCount;

		if (total == 0)
		{
			Value = 0;
			return;
		}

		var probWin = (decimal)_winCount / total;
		var probLoss = (decimal)_lossCount / total;

		var avgWin = _winCount > 0 ? _winSum / _winCount : 0;
		var avgLoss = _lossCount > 0 ? _lossSum / _lossCount : 0;

		Value = probWin * avgWin + probLoss * avgLoss;
	}

	/// <inheritdoc/>
	public override void Reset()
	{
		_winCount = 0;
		_lossCount = 0;
		_winSum = 0;
		_lossSum = 0;

		base.Reset();
	}

	/// <inheritdoc/>
	public override async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		storage.Set("WinCount", _winCount);
		storage.Set("LossCount", _lossCount);
		storage.Set("WinSum", _winSum);
		storage.Set("LossSum", _lossSum);

		await base.SaveAsync(storage, cancellationToken);
	}

	/// <inheritdoc/>
	public override async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		_winCount = storage.GetValue<int>("WinCount");
		_lossCount = storage.GetValue<int>("LossCount");
		_winSum = storage.GetValue<decimal>("WinSum");
		_lossSum = storage.GetValue<decimal>("LossSum");

		await base.LoadAsync(storage, cancellationToken);
	}
}
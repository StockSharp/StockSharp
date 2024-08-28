namespace StockSharp.Algo.Indicators;

/// <summary>
/// Balance Volume indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.BVKey,
	Description = LocalizedStrings.BalanceVolumeKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/indicators/balance_volume.html")]
public class BalanceVolume : BaseIndicator
{
	private decimal _prevClose;
	private decimal _cumulativeBalanceVolume;

	/// <summary>
	/// Initializes a new instance of the <see cref="BalanceVolume"/>.
	/// </summary>
	public BalanceVolume()
	{
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Volume;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var (_, _, _, close, volume) = input.GetOhlcv();

		if (_prevClose == 0)
		{
			_prevClose = close;
			return new DecimalIndicatorValue(this, input.Time);
		}

		decimal cumulativeBalanceVolume;

		var balanceVolume = close > _prevClose ? volume : (close < _prevClose ? -volume : 0);

		if (input.IsFinal)
		{
			_cumulativeBalanceVolume += balanceVolume;
			_prevClose = close;
			cumulativeBalanceVolume = _cumulativeBalanceVolume;
			IsFormed = true;
		}
		else
			cumulativeBalanceVolume = _cumulativeBalanceVolume + balanceVolume;

		return new DecimalIndicatorValue(this, cumulativeBalanceVolume, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_prevClose = 0;
		_cumulativeBalanceVolume = 0;
		base.Reset();
	}
}

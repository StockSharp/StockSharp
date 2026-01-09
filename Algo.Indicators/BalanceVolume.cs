namespace StockSharp.Algo.Indicators;

/// <summary>
/// Balance Volume indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.BVKey,
	Description = LocalizedStrings.BalanceVolumeKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/balance_volume.html")]
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
	public override int NumValuesToInitialize => 2;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		if (_prevClose == 0)
		{
			if (input.IsFinal)
				_prevClose = candle.ClosePrice;

			return new DecimalIndicatorValue(this, input.Time);
		}

		decimal cumulativeBalanceVolume;

		var balanceVolume = candle.ClosePrice > _prevClose ? candle.TotalVolume : (candle.ClosePrice < _prevClose ? -candle.TotalVolume : 0);

		if (input.IsFinal)
		{
			_cumulativeBalanceVolume += balanceVolume;
			_prevClose = candle.ClosePrice;
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
		_prevClose = default;
		_cumulativeBalanceVolume = default;

		base.Reset();
	}
}

namespace StockSharp.Algo.Indicators;

/// <summary>
/// Williams Accumulation/Distribution indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.WADKey,
	Description = LocalizedStrings.WilliamsAccumulationDistributionKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/williams_accumulation_distribution.html")]
public class WilliamsAccumulationDistribution : BaseIndicator
{
	private decimal _prevClose;
	private decimal _ad;

	/// <summary>
	/// Initializes a new instance of the <see cref="WilliamsAccumulationDistribution"/>.
	/// </summary>
	public WilliamsAccumulationDistribution()
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

		decimal todayAD;

		if (candle.ClosePrice > _prevClose)
			todayAD = candle.ClosePrice - Math.Min(candle.LowPrice, _prevClose);
		else if (candle.ClosePrice < _prevClose)
			todayAD = candle.ClosePrice - Math.Max(candle.HighPrice, _prevClose);
		else
			todayAD = 0;

		var currentAD = _ad;

		if (input.IsFinal)
		{
			_ad += todayAD;
			_prevClose = candle.ClosePrice;
			IsFormed = true;
			currentAD = _ad;
		}
		else
		{
			currentAD += todayAD;
		}

		return new DecimalIndicatorValue(this, currentAD, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_prevClose = 0;
		_ad = 0;
		base.Reset();
	}
}
namespace StockSharp.Algo.Indicators;

using StockSharp.Algo.Candles;

/// <summary>
/// Twiggs Money Flow.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TMFKey,
	Description = LocalizedStrings.TwiggsMoneyFlowKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/twiggs_money_flow.html")]
public class TwiggsMoneyFlow : LengthIndicator<decimal>
{
	private readonly ExponentialMovingAverage _adv = new();
	private readonly ExponentialMovingAverage _vol = new();
	private decimal _prevAd;

	/// <summary>
	/// Initializes a new instance of the <see cref="TwiggsMoneyFlow"/>.
	/// </summary>
	public TwiggsMoneyFlow()
	{
		Length = 21;
	}

	/// <inheritdoc />
	public override int NumValuesToInitialize => _adv.NumValuesToInitialize.Max(_vol.NumValuesToInitialize);

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _adv.IsFormed && _vol.IsFormed;

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();
		var cl = candle.GetLength();

		var typicalPrice = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3;

		decimal ad;

		if (cl != 0)
			ad = candle.TotalVolume * (2 * typicalPrice - candle.HighPrice - candle.LowPrice) / cl;
		else
			ad = _prevAd;

		var advValue = _adv.Process(input, ad);
		var volValue = _vol.Process(input, candle.TotalVolume);

		decimal tmf = 0;

		if (IsFormed)
		{
			var advDecimal = advValue.ToDecimal();
			var volDecimal = volValue.ToDecimal();

			if (volDecimal != 0)
				tmf = advDecimal / volDecimal;
		}

		if (input.IsFinal)
			_prevAd = ad;

		return tmf == 0
			? null
			: tmf;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_adv.Length = Length;
		_vol.Length = Length;
		_adv.Reset();
		_vol.Reset();
		_prevAd = 0;

		base.Reset();
	}
}
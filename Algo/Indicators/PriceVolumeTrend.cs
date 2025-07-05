namespace StockSharp.Algo.Indicators;

/// <summary>
/// Price Volume Trend (PVT).
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/pvt.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PVTKey,
	Description = LocalizedStrings.PriceVolumeTrendKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/pvt.html")]
public class PriceVolumeTrend : BaseIndicator
{
	private decimal _pvt;
	private decimal _prevClose;

	/// <summary>
	/// Initializes a new instance of the <see cref="PriceVolumeTrend"/>.
	/// </summary>
	public PriceVolumeTrend()
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

		decimal pvt;

		// PVT = Previous PVT + Volume * [(Close - Previous Close) / Previous Close]
		var priceChange = (candle.ClosePrice - _prevClose) / _prevClose;
		var volumeContribution = candle.TotalVolume * priceChange;

		if (input.IsFinal)
		{
			_pvt += volumeContribution;
			_prevClose = candle.ClosePrice;
			IsFormed = true;
			pvt = _pvt;
		}
		else
		{
			pvt = _pvt + volumeContribution;
		}

		return new DecimalIndicatorValue(this, pvt, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_pvt = 0;
		_prevClose = 0;
		base.Reset();
	}
}
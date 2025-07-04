namespace StockSharp.Algo.Indicators;

/// <summary>
/// Positive Volume Index (PVI) indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PVIKey,
	Description = LocalizedStrings.PositiveVolumeIndexKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/indicators/positive_volume_index.html")]
public class PositiveVolumeIndex : BaseIndicator
{
	private decimal _prevClose;
	private decimal _prevVolume;
	private decimal _pvi;

	/// <summary>
	/// Initializes a new instance of the <see cref="PositiveVolumeIndex"/>.
	/// </summary>
	public PositiveVolumeIndex()
	{
		Reset();
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var pvi = _pvi;

		if (_prevClose != 0 && _prevVolume != 0 && candle.TotalVolume > 0)
		{
			if (candle.TotalVolume > _prevVolume)
			{
				var priceChangePercent = (candle.ClosePrice - _prevClose) / _prevClose;

				pvi += pvi * priceChangePercent;
			}
		}

		if (input.IsFinal)
		{
			_prevClose = candle.ClosePrice;
			_prevVolume = candle.TotalVolume;
			_pvi = pvi;

			IsFormed = true;
		}

		return new DecimalIndicatorValue(this, pvi, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_prevClose = 0;
		_prevVolume = 0;
		_pvi = 1000;

		base.Reset();
	}
}
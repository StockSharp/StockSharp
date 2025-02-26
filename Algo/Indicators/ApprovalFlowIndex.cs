namespace StockSharp.Algo.Indicators;

/// <summary>
/// Approval Flow Index indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.AFIKey,
	Description = LocalizedStrings.ApprovalFlowIndexKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/approval_flow_index.html")]
public class ApprovalFlowIndex : LengthIndicator<decimal>
{
	private decimal _totalUpVolume;
	private decimal _totalDownVolume;
	private int _count;
	private decimal _prevClose;

	/// <summary>
	/// Initializes a new instance of the <see cref="ApprovalFlowIndex"/>.
	/// </summary>
	public ApprovalFlowIndex()
	{
		Length = 14;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		if (_prevClose == 0)
		{
			_prevClose = candle.ClosePrice;
			return null;
		}

		if (_count < Length)
			_count++;

		var upVolume = candle.ClosePrice > _prevClose ? candle.TotalVolume : 0;
		var downVolume = candle.ClosePrice < _prevClose ? candle.TotalVolume : 0;

		_totalUpVolume += upVolume;
		_totalDownVolume += downVolume;

		if (_count == Length)
		{
			IsFormed = true;

			var totalVolume = _totalUpVolume + _totalDownVolume;

			if (totalVolume != 0)
			{
				var afi = 100m * (_totalUpVolume - _totalDownVolume) / totalVolume;
				return afi;
			}
		}

		_prevClose = candle.ClosePrice;
		return null;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_totalUpVolume = 0;
		_totalDownVolume = 0;
		_count = 0;
		_prevClose = 0;
		base.Reset();
	}
}

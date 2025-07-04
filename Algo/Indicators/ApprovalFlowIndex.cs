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
	public override int NumValuesToInitialize => base.NumValuesToInitialize + 1;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		if (input.IsFinal)
		{
			if (_prevClose == 0)
			{
				_prevClose = candle.ClosePrice;
				return null;
			}

			if (!IsFormed)
			{
				_count++;

				if (_count == Length)
					IsFormed = true;
			}
		}

		var upVolume = candle.ClosePrice > _prevClose ? candle.TotalVolume : 0;
		var downVolume = candle.ClosePrice < _prevClose ? candle.TotalVolume : 0;

		var totalUpVolume = _totalUpVolume + upVolume;
		var totalDownVolume = _totalDownVolume + downVolume;

		if (input.IsFinal)
		{
			_totalUpVolume = totalUpVolume;
			_totalDownVolume = totalDownVolume;
		}

		if (IsFormed)
		{
			var totalVolume = totalUpVolume + totalDownVolume;

			if (totalVolume != 0)
			{
				var afi = 100m * (totalUpVolume - totalDownVolume) / totalVolume;
				return afi;
			}
		}

		if (input.IsFinal)
			_prevClose = candle.ClosePrice;

		return null;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_totalUpVolume = default;
		_totalDownVolume = default;
		_count = default;
		_prevClose = default;

		base.Reset();
	}
}

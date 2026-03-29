namespace StockSharp.Algo.Indicators;

/// <summary>
/// Intraday Momentum Index (IMI) indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.IMIKey,
	Description = LocalizedStrings.IntradayMomentumIndexKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/intraday_momentum_index.html")]
public class IntradayMomentumIndex : LengthIndicator<(decimal o, decimal c), CircularBufferEx<(decimal, decimal)>>
{
	private decimal _upSum;
	private decimal _downSum;

	/// <summary>
	/// Initializes a new instance of the <see cref="IntradayMomentumIndex"/>.
	/// </summary>
	public IntradayMomentumIndex()
		: base(new(14))
	{
		Length = 14;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var upMove = (candle.ClosePrice - candle.OpenPrice).Max(0);
		var downMove = (candle.OpenPrice - candle.ClosePrice).Max(0);

		decimal sumUp, sumDown;

		if (input.IsFinal)
		{
			_upSum += upMove;
			_downSum += downMove;

			if (Buffer.Count == Length)
			{
				var (o, c) = Buffer.First();

				_upSum -= (c - o).Max(0);
				_downSum -= (o - c).Max(0);
			}

			Buffer.PushBack((candle.OpenPrice, candle.ClosePrice));

			sumUp = _upSum;
			sumDown = _downSum;
		}
		else
		{
			sumUp = _upSum;
			sumDown = _downSum;

			if (Buffer.Count == Length)
			{
				var (o, c) = Buffer.First();

				sumUp -= (c - o).Max(0);
				sumDown -= (o - c).Max(0);
			}

			sumUp += upMove;
			sumDown += downMove;
		}

		if (IsFormed)
		{
			var den = sumUp + sumDown;
			var imi = den != 0 ? 100m * (sumUp / den) : 0;
			return imi;
		}

		return null;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_upSum = default;
		_downSum = default;

		base.Reset();
	}
}
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
public class IntradayMomentumIndex : LengthIndicator<(decimal o, decimal c)>
{
	private decimal _upSum;
	private decimal _downSum;

	/// <summary>
	/// Initializes a new instance of the <see cref="IntradayMomentumIndex"/>.
	/// </summary>
	public IntradayMomentumIndex()
	{
		Length = 14;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var upMove = Math.Max(candle.ClosePrice - candle.OpenPrice, 0);
		var downMove = Math.Max(candle.OpenPrice - candle.ClosePrice, 0);

		decimal sumUp, sumDown;

		if (input.IsFinal)
		{
			_upSum += upMove;
			_downSum += downMove;

			if (Buffer.Count == Length)
			{
				var (o, c) = Buffer.First();

				_upSum -= Math.Max(c - o, 0);
				_downSum -= Math.Max(o - c, 0);
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

				sumUp -= Math.Max(c - o, 0);
				sumDown -= Math.Max(o - c, 0);
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
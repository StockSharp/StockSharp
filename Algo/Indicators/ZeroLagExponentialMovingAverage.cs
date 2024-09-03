namespace StockSharp.Algo.Indicators;

/// <summary>
/// Zero Lag Exponential Moving Average (ZLEMA).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ZLEMAKey,
	Description = LocalizedStrings.ZeroLagExponentialMovingAverageKey)]
[Doc("topics/indicators/zero_lag_exponential_moving_average.html")]
public class ZeroLagExponentialMovingAverage : LengthIndicator<decimal>
{
	private decimal _prevZlema;
	private decimal _ema;
	private int _lag;
	private bool _isFormedEx;

	/// <summary>
	/// Initializes a new instance of the <see cref="ZeroLagExponentialMovingAverage"/>.
	/// </summary>
	public ZeroLagExponentialMovingAverage()
	{
		Buffer.MinComparer = Comparer<decimal>.Default;
		Length = 14;
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var price = input.ToDecimal();

		if (input.IsFinal)
		{
			Buffer.AddEx(price);
		}

		if (IsFormed)
		{
			var lagPrice = input.IsFinal ? Buffer[_lag] : (Buffer.Count > _lag ? Buffer[_lag] : price);
			var zlema = _ema * (2 * price - lagPrice) + (1 - _ema) * _prevZlema;

			if (input.IsFinal)
			{
				_prevZlema = zlema;
			}

			if (!_isFormedEx)
				_isFormedEx = zlema >= Buffer.Min.Value;

			if (_isFormedEx)
				return new DecimalIndicatorValue(this, zlema, input.Time);
		}

		return new DecimalIndicatorValue(this, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_prevZlema = 0;
		_ema = 2.0m / (Length + 1);
		_lag = (Length - 1) / 2;
		_isFormedEx = false;

		base.Reset();
	}
}
namespace StockSharp.Algo.Indicators;

/// <summary>
/// Zero Lag Exponential Moving Average (ZLEMA).
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ZLEMAKey,
	Description = LocalizedStrings.ZeroLagExponentialMovingAverageKey)]
[Doc("topics/api/indicators/list_of_indicators/zero_lag_exponential_moving_average.html")]
public class ZeroLagExponentialMovingAverage : LengthIndicator<decimal>
{
	private decimal _prevZlema;
	private decimal _ema;
	private int _lag;

	/// <summary>
	/// Initializes a new instance of the <see cref="ZeroLagExponentialMovingAverage"/>.
	/// </summary>
	public ZeroLagExponentialMovingAverage()
	{
		Buffer.MinComparer = Comparer<decimal>.Default;
		Length = 14;
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var price = input.ToDecimal();

		IList<decimal> buffer;

		if (input.IsFinal)
		{
			Buffer.PushBack(price);
			buffer = Buffer;
		}
		else
		{
			buffer = [.. Buffer.Skip(1), price];
		}

		if (IsFormed)
		{
			var lagPrice = input.IsFinal ? buffer[_lag] : (buffer.Count > _lag ? buffer[_lag] : price);
			var zlema = _ema * (2 * price - lagPrice) + (1 - _ema) * _prevZlema;

			if (input.IsFinal)
			{
				_prevZlema = zlema;
			}

			return zlema;
		}

		return null;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_prevZlema = 0;
		_ema = 2.0m / (Length + 1);
		_lag = (Length - 1) / 2;

		base.Reset();
	}
}
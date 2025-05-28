namespace StockSharp.Algo.Indicators;

/// <summary>
/// Center of Gravity Oscillator indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CGOKey,
	Description = LocalizedStrings.CenterOfGravityOscillatorKey)]
[Doc("topics/api/indicators/list_of_indicators/center_of_gravity_oscillator.html")]
public class CenterOfGravityOscillator : LengthIndicator<decimal>
{
	private decimal _sumPrice;
	private decimal _part;

	/// <summary>
	/// Initializes a new instance of the <see cref="CenterOfGravityOscillator"/>.
	/// </summary>
	public CenterOfGravityOscillator()
	{
		Length = 10;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_sumPrice = default;

		_part = (Length + 1m) / 2m;
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var price = input.ToDecimal();

		decimal sumPrice;
		decimal sumWeightedPrice = 0m;

		if (input.IsFinal)
		{
			if (Buffer.Count >= Length)
			{
				var oldPrice = Buffer.Front();

				_sumPrice -= oldPrice;
			}

			Buffer.PushBack(price);

			_sumPrice += price;

			sumPrice = _sumPrice;

			var i = 1;

			foreach (var p in Buffer)
			{
				sumWeightedPrice += p * i;
				i++;
			}
		}
		else
		{
			if (Buffer.Count == 0)
				return null;

			sumPrice = _sumPrice - Buffer.Front() + price;

			var i = 1;

			foreach (var p in Buffer.Skip(1))
			{
				sumWeightedPrice += p * i;
				i++;
			}

			sumWeightedPrice += i * price;
		}

		if (IsFormed)
		{
			var cgo = (sumWeightedPrice / sumPrice) - _part;
			return cgo;
		}

		return null;
	}
}

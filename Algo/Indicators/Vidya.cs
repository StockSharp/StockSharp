namespace StockSharp.Algo.Indicators;

/// <summary>
/// The dynamic average of variable index  (Variable Index Dynamic Average).
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/vidya.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.VidyaKey,
	Description = LocalizedStrings.VariableIndexDynamicAverageKey)]
[Doc("topics/api/indicators/list_of_indicators/vidya.html")]
public class Vidya : LengthIndicator<decimal>
{
	private decimal _multiplier = 1;
	private decimal _prevFinalValue;

	private readonly ChandeMomentumOscillator _cmo;

	/// <summary>
	/// To create the indicator <see cref="Vidya"/>.
	/// </summary>
	public Vidya()
	{
		_cmo = new ChandeMomentumOscillator();
		Length = 15;
		Buffer.Operator = new DecimalOperator();
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_cmo.Length = Length;
		_multiplier = 2m / (Length + 1);
		_prevFinalValue = 0;

		base.Reset();
	}

	/// <inheritdoc />
	public override int NumValuesToInitialize
		=> _cmo.NumValuesToInitialize + base.NumValuesToInitialize - 1;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var newValue = input.ToDecimal();

		// calc СMO
		var cmoValue = _cmo.Process(input);

		if (cmoValue.IsEmpty)
			return null;

		// calc Vidya
		if (!IsFormed)
		{
			if (!input.IsFinal)
				return (Buffer.SumNoFirst + newValue) / Length;

			Buffer.PushBack(newValue);

			_prevFinalValue = Buffer.Sum / Length;

			return _prevFinalValue;
		}

		var curValue = (newValue - _prevFinalValue) * _multiplier * Math.Abs(cmoValue.ToDecimal() / 100m) + _prevFinalValue;
			
		if (input.IsFinal)
			_prevFinalValue = curValue;

		return curValue;
	}
}
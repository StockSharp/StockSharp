namespace StockSharp.Algo.Indicators;

/// <summary>
/// Chande Momentum Oscillator.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/cmo.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CMOKey,
	Description = LocalizedStrings.ChandeMomentumOscillatorKey)]
[Doc("topics/api/indicators/list_of_indicators/cmo.html")]
public class ChandeMomentumOscillator : LengthIndicator<decimal>
{
	private readonly Sum _cmoUp = new();
	private readonly Sum _cmoDn = new();
	private bool _isInitialized;
	private decimal _last;

	/// <summary>
	/// Initializes a new instance of the <see cref="ChandeMomentumOscillator"/>.
	/// </summary>
	public ChandeMomentumOscillator()
	{
		Length = 15;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	public override void Reset()
	{
		_cmoDn.Length = _cmoUp.Length = Length;
		_isInitialized = false;
		_last = 0;

		base.Reset();
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _cmoUp.IsFormed;

	/// <inheritdoc />
	public override int NumValuesToInitialize => base.NumValuesToInitialize + 1;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var newValue = input.ToDecimal();

		if (!_isInitialized)
		{
			if (input.IsFinal)
			{
				_last = newValue;
				_isInitialized = true;
			}

			return null;
		}

		var delta = newValue - _last;

		var upValue = _cmoUp.Process(input, delta > 0 ? delta : 0m).ToDecimal();
		var downValue = _cmoDn.Process(input, delta > 0 ? 0m : -delta).ToDecimal();

		if (input.IsFinal)
			_last = newValue;

		var value = (upValue + downValue) == 0 ? 0 : 100m * (upValue - downValue) / (upValue + downValue);

		return IsFormed
			? value
			: null;
	}
}
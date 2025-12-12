namespace StockSharp.Algo.Indicators;

/// <summary>
/// Aroon Oscillator.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/aroon_oscillator.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.AroonOscillatorKey,
	Description = LocalizedStrings.AroonOscillatorDescKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/aroon_oscillator.html")]
public class AroonOscillator : DecimalLengthIndicator
{
	private readonly Aroon _aroon;

	/// <summary>
	/// Initializes a new instance of the <see cref="AroonOscillator"/>.
	/// </summary>
	public AroonOscillator()
	{
		_aroon = new();
		Length = 14;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	public override int NumValuesToInitialize => _aroon.NumValuesToInitialize;

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _aroon.IsFormed;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var aroonValue = (AroonValue)_aroon.Process(input);

		if (aroonValue.Up is not decimal up || aroonValue.Down is not decimal down)
			return null;

		return up - down;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_aroon.Length = Length;
		base.Reset();
	}
}
namespace StockSharp.Algo.Indicators;

/// <summary>
/// Momentum.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/momentum.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MomentumKey,
	Description = LocalizedStrings.MomentumKey)]
[Doc("topics/api/indicators/list_of_indicators/momentum.html")]
public class Momentum : LengthIndicator<decimal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Momentum"/>.
	/// </summary>
	public Momentum()
	{
		Length = 5;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override bool CalcIsFormed() => Buffer.Count > Length;

	/// <inheritdoc />
	public override int NumValuesToInitialize => base.NumValuesToInitialize + 1;

	/// <inheritdoc />
	protected override int GetCapacity() => Length + 1;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var newValue = input.ToDecimal();

		if (input.IsFinal)
		{
			Buffer.PushBack(newValue);
		}

		if (Buffer.Count == 0)
			return null;

		return newValue - Buffer[0];
	}
}
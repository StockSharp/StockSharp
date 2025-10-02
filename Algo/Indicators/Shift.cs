namespace StockSharp.Algo.Indicators;

/// <summary>
/// Shift indicator. Does nothing, only needed for value counting.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/shift.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ShiftKey,
	Description = LocalizedStrings.ShiftDescKey)]
[Doc("topics/api/indicators/list_of_indicators/shift.html")]
public class Shift : LengthIndicator<decimal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Shift"/>.
	/// </summary>
	public Shift()
	{
		Length = 1;
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var value = input.ToDecimal(Source);

		if (input.IsFinal)
			Buffer.PushBack(value);

		return IsFormed
			? new DecimalIndicatorValue(this, Buffer[input.IsFinal ? 0 : Math.Min(1, Buffer.Count - 1)], input.Time)
			: new DecimalIndicatorValue(this, input.Time);
	}
}
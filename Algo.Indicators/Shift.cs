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
public class Shift : DecimalLengthIndicator
{
	private int _left;

	/// <summary>
	/// Initializes a new instance of the <see cref="Shift"/>.
	/// </summary>
	public Shift()
	{
		Length = 1;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_left = Length;
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _left <= 0;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		// The value being processed is one of the Length values counted, so it is served as soon as it
		// completes the count: a length of one passes the very first value through.
		if (input.IsFinal && _left > 0)
			_left--;

		if (IsFormed)
			return new DecimalIndicatorValue(this, input.ToDecimal(Source), input.Time);

		return new DecimalIndicatorValue(this, input.Time);
	}
}
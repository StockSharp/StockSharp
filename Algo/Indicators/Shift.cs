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
		try
		{
			if (IsFormed)
				return new DecimalIndicatorValue(this, input.ToDecimal(Source), input.Time);

			return new DecimalIndicatorValue(this, input.Time);
		}
		finally
		{
			if (input.IsFinal)
				_left--;
		}
	}
}
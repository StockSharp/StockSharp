namespace StockSharp.Algo.Indicators;

/// <summary>
/// Simple moving average.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/sma.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.SMAKey,
	Description = LocalizedStrings.SimpleMovingAverageKey)]
[Doc("topics/api/indicators/list_of_indicators/sma.html")]
public class SimpleMovingAverage : LengthIndicator<decimal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SimpleMovingAverage"/>.
	/// </summary>
	public SimpleMovingAverage()
	{
		Length = 32;
		Buffer.Operator = new DecimalOperator();
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var newValue = input.ToDecimal();

		if (input.IsFinal)
		{
			Buffer.PushBack(newValue);
			return Buffer.Sum / Length;
		}

		return (Buffer.SumNoFirst + newValue) / Length;
	}
}
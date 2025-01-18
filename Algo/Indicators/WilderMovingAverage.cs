namespace StockSharp.Algo.Indicators;

/// <summary>
/// Welles Wilder Moving Average.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/wilder_ma.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.WilderMAKey,
	Description = LocalizedStrings.WilderMovingAverageKey)]
[Doc("topics/api/indicators/list_of_indicators/wilder_ma.html")]
public class WilderMovingAverage : LengthIndicator<decimal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="WilderMovingAverage"/>.
	/// </summary>
	public WilderMovingAverage()
	{
		Length = 32;
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var newValue = input.ToDecimal();

		if (input.IsFinal)
			Buffer.PushBack(newValue);

		var buffCount = input.IsFinal ? Buffer.Count : ((Buffer.Count - 1).Max(0) + 1);

		return (this.GetCurrentValue() * (buffCount - 1) + newValue) / buffCount;
	}
}
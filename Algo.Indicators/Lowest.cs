namespace StockSharp.Algo.Indicators;

/// <summary>
/// Minimum value for a period.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/lowest.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.LowestKey,
	Description = LocalizedStrings.MinValuePeriodKey)]
[Doc("topics/api/indicators/list_of_indicators/lowest.html")]
public class Lowest : DecimalLengthIndicator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Lowest"/>.
	/// </summary>
	public Lowest()
	{
		Length = 5;

#if !NET7_0_OR_GREATER
		Buffer.Operator = new DecimalOperator();
#endif
		Buffer.Stats = CircularBufferStats.Min;
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var low = input.ToCandle().LowPrice;

		if (input.IsFinal)
		{
			Buffer.PushBack(low);
			return Buffer.Min.Value;
		}
		else
			return low.Min(Buffer.Count == 0 ? low : Buffer.Min.Value);
	}
}
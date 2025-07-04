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
public class Lowest : LengthIndicator<decimal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Lowest"/>.
	/// </summary>
	public Lowest()
	{
		Length = 5;
		Buffer.MinComparer = Comparer<decimal>.Default;
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
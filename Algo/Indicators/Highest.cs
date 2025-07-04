namespace StockSharp.Algo.Indicators;

/// <summary>
/// Maximum value for a period.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/highest.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.HighestKey,
	Description = LocalizedStrings.MaxValueForPeriodKey)]
[Doc("topics/api/indicators/list_of_indicators/highest.html")]
public class Highest : LengthIndicator<decimal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Highest"/>.
	/// </summary>
	public Highest()
	{
		Length = 5;
		Buffer.MaxComparer = Comparer<decimal>.Default;
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var high = input.ToCandle().HighPrice;

		if (input.IsFinal)
		{
			Buffer.PushBack(high);
			return Buffer.Max.Value;
		}
		else
			return high.Max(Buffer.Count == 0 ? high : Buffer.Max.Value);
	}
}
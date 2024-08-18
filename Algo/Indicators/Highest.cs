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
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var (_, high, _, _) = input.GetOhlc();

		var lastValue = Buffer.Count == 0 ? high : this.GetCurrentValue();

		if (high > lastValue)
			lastValue = high;

		if (input.IsFinal)
		{
			Buffer.AddEx(high);
			lastValue = Buffer.Max.Value;
		}

		return new DecimalIndicatorValue(this, lastValue);
	}
}
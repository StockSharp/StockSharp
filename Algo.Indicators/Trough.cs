namespace StockSharp.Algo.Indicators;

/// <summary>
/// Trough.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/trough.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TroughKey,
	Description = LocalizedStrings.TroughDescKey)]
[Doc("topics/api/indicators/list_of_indicators/trough.html")]
public sealed class Trough : ZigZag
{
	/// <summary>
	/// To create the indicator <see cref="Trough"/>.
	/// </summary>
	public Trough()
	{
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();
		if (candle is null)
			return new ZigZagIndicatorValue(this, input.Time);

		var value = CalcZigZag(input, candle.LowPrice);

		if (!value.IsEmpty)
		{
			var typed = value;

			if (typed.IsUp)
				return new ZigZagIndicatorValue(this, value.Time);
		}

		return value;
	}
}
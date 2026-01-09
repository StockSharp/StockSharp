namespace StockSharp.Algo.Indicators;

/// <summary>
/// Peak.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/peak.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PeakKey,
	Description = LocalizedStrings.PeakKey)]
[Doc("topics/api/indicators/list_of_indicators/peak.html")]
public sealed class Peak : ZigZag
{
	/// <summary>
	/// To create the indicator <see cref="Peak"/>.
	/// </summary>
	public Peak()
	{
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();
		if (candle is null)
			return new ZigZagIndicatorValue(this, input.Time);

		var value = CalcZigZag(input, candle.HighPrice);

		if (!value.IsEmpty)
		{
			var typed = value;

			if (!typed.IsUp)
				return new ZigZagIndicatorValue(this, value.Time);
		}

		return value;
	}
}
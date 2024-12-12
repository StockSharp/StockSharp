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
	protected override decimal GetPrice(IIndicatorValue input)
		=> input.ToCandle().HighPrice;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var value = base.OnProcess(input);

		if (IsFormed && !value.IsEmpty)
		{
			if (CurrentValue < value.ToDecimal())
			{
				return value;
			}

			var lastValue = this.GetCurrentValue<ShiftedIndicatorValue>();

			if (input.IsFinal)
				IsFormed = !lastValue.IsEmpty;

			return IsFormed ? new ShiftedIndicatorValue(this, lastValue.Value, lastValue.Shift + 1, input.Time) : lastValue;
		}

		IsFormed = false;

		return value;
	}
}
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
public sealed class Trough : ZigZagEquis
{
	/// <summary>
	/// To create the indicator <see cref="Trough"/>.
	/// </summary>
	public Trough()
	{
		PriceField = Level1Fields.LowPrice;
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var value = base.OnProcess(input);

		if (IsFormed && !value.IsEmpty)
		{
			if (CurrentValue > value.GetValue<decimal>())
			{
				return value;
			}
			else
			{
				var lastValue = this.GetCurrentValue<ShiftedIndicatorValue>();

				if (input.IsFinal)
					IsFormed = !lastValue.IsEmpty;

				return IsFormed ? new ShiftedIndicatorValue(this, lastValue.Value, lastValue.Shift + 1) : lastValue;
			}
		}

		IsFormed = false;

		return value;
	}
}
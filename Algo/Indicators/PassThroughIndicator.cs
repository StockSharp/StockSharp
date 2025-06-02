namespace StockSharp.Algo.Indicators;

/// <summary>
/// Pass through indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.StubKey,
	Description = LocalizedStrings.StubIndicatorKey)]
[Doc("topics/api/indicators/list_of_indicators/pass_through.html")]
public class PassThroughIndicator : BaseIndicator
{
	/// <inheritdoc/>
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input.IsFinal && !IsFormed)
			IsFormed = true;

		return new DecimalIndicatorValue(this, input.ToDecimal(), input.Time);
	}
}

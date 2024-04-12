namespace StockSharp.Algo.Indicators;

using System.ComponentModel.DataAnnotations;

using Ecng.ComponentModel;

using StockSharp.Localization;

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
		IsFormed |= !input.IsEmpty;

		return input.IsEmpty
			? new DecimalIndicatorValue(this)
			: new DecimalIndicatorValue(this, input.GetValue<decimal>());
	}
}

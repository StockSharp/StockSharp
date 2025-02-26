namespace StockSharp.Algo.Indicators;

/// <summary>
/// Lunar Phase indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.LPKey,
	Description = LocalizedStrings.LunarPhaseKey)]
[Doc("topics/api/indicators/list_of_indicators/lunar_phase.html")]
public class LunarPhase : BaseIndicator
{
	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input.IsFinal)
			IsFormed = true;

		var time = input.Time;
		var phase = (int)time.DateTime.GetLunarPhase();
		return new DecimalIndicatorValue(this, phase, time);
	}
}



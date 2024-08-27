namespace StockSharp.Algo.Indicators;

/// <summary>
/// Lunar Phase indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.LPKey,
	Description = LocalizedStrings.LunarPhaseKey)]
[Doc("topics/indicators/lunar_phase.html")]
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
		var phase = CalculateLunarPhase(time.DateTime);
		return new DecimalIndicatorValue(this, phase, time);
	}

	private static int CalculateLunarPhase(DateTime date)
	{
		// Convert the date to Julian Date
		var julianDate = ToJulianDate(date);

		// Calculate days since the last known new moon (Jan 6, 2000)
		var daysSinceNew = julianDate - 2451549.5;

		// Calculate the number of lunar cycles since the reference date
		var newMoons = daysSinceNew / 29.53; // 29.53 is the length of a lunar cycle in days

		// Get the current position in the lunar cycle (0 to 1)
		var phase = newMoons - Math.Floor(newMoons);

		// Convert the phase (0 to 1) to one of 8 moon phases (0 to 7)
		var phaseIndex = (int)Math.Floor(phase * 8);

		return phaseIndex;
	}

	private static double ToJulianDate(DateTime date)
	{
		return date.ToOADate() + 2415018.5;
	}
}



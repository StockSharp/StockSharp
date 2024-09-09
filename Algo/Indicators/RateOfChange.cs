﻿namespace StockSharp.Algo.Indicators;

/// <summary>
/// Rate of change.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/roc.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ROCKey,
	Description = LocalizedStrings.RateOfChangeKey)]
[Doc("topics/api/indicators/list_of_indicators/roc.html")]
public class RateOfChange : Momentum
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RateOfChange"/>.
	/// </summary>
	public RateOfChange()
	{
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var result = base.OnProcess(input);

		if (Buffer.Count > 0 && Buffer[0] != 0)
			return new DecimalIndicatorValue(this, result.ToDecimal() / Buffer[0] * 100, input.Time);
		
		return new DecimalIndicatorValue(this, input.Time);
	}
}
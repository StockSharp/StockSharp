﻿namespace StockSharp.Algo.Indicators;

/// <summary>
/// Sum of N last values.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/sum_n.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.SumKey,
	Description = LocalizedStrings.SumNLastValuesKey)]
[Doc("topics/api/indicators/list_of_indicators/sum_n.html")]
public class Sum : LengthIndicator<decimal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Sum"/>.
	/// </summary>
	public Sum()
	{
		Length = 15;
		Buffer.Operator = new DecimalOperator();
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Volume;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var newValue = input.ToDecimal();

		if (input.IsFinal)
			Buffer.PushBack(newValue);

		if (input.IsFinal)
		{
			return new DecimalIndicatorValue(this, Buffer.Sum, input.Time);
		}
		else
		{
			return new DecimalIndicatorValue(this, (Buffer.SumNoFirst + newValue), input.Time);
		}
	}
}
﻿namespace StockSharp.Algo.Indicators;

/// <summary>
/// Kaufman Efficiency Ratio indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.KERKey,
	Description = LocalizedStrings.KaufmanEfficiencyRatioKey)]
[Doc("topics/indicators/kaufman_efficiency_ratio.html")]
public class KaufmanEfficiencyRatio : LengthIndicator<decimal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="KaufmanEfficiencyRatio"/>.
	/// </summary>
	public KaufmanEfficiencyRatio()
	{
		Length = 10;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var price = input.ToDecimal();

		if (input.IsFinal)
		{
			Buffer.PushBack(price);
		}

		if (IsFormed)
		{
			var change = Math.Abs(price - Buffer[0]);
			var volatility = Buffer.Skip(Math.Max(0, Buffer.Count - Length)).Zip(Buffer.Skip(Math.Max(0, Buffer.Count - Length + 1)), (a, b) => Math.Abs(a - b)).Sum();

			if (!input.IsFinal)
			{
				volatility += Math.Abs(price - Buffer[^1]);
			}

			var result = volatility != 0 ? change / volatility : 0;
			return new DecimalIndicatorValue(this, result, input.Time);
		}

		return new DecimalIndicatorValue(this, input.Time);
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" {Length}";
}
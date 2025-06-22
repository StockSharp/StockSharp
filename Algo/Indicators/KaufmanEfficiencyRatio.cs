namespace StockSharp.Algo.Indicators;

/// <summary>
/// Kaufman Efficiency Ratio indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.KERKey,
	Description = LocalizedStrings.KaufmanEfficiencyRatioKey)]
[Doc("topics/api/indicators/list_of_indicators/kaufman_efficiency_ratio.html")]
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
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var price = input.ToDecimal();

		if (input.IsFinal)
		{
			Buffer.PushBack(price);
		}

		if (IsFormed)
		{
			var change = (price - Buffer[0]).Abs();
			var volatility = Buffer.Skip(0.Max(Buffer.Count - Length)).Zip(Buffer.Skip(0.Max(Buffer.Count - Length + 1)), (a, b) => (a - b).Abs()).Sum();

			if (!input.IsFinal)
			{
				volatility += Math.Abs(price - Buffer[^1]);
			}

			var result = volatility != 0 ? change / volatility : 0;
			return result;
		}

		return null;
	}
}
namespace StockSharp.Algo.Indicators;

/// <summary>
/// Momentum Pinball indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MPKey,
	Description = LocalizedStrings.MomentumPinballKey)]
[Doc("topics/indicators/momentum_pinball.html")]
public class MomentumPinball : LengthIndicator<decimal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MomentumPinball"/>.
	/// </summary>
	public MomentumPinball()
	{
		Length = 14;

		Buffer.Operator = new DecimalOperator();
		Buffer.MaxComparer = Comparer<decimal>.Default;
		Buffer.MinComparer = Comparer<decimal>.Default;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

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
			var momentum = price - Buffer[0];
			var range = Buffer.Max.Value - Buffer.Min.Value;
			var result = range != 0 ? (momentum / range) * 100 : 0;
			return new DecimalIndicatorValue(this, result, input.Time);
		}

		return new DecimalIndicatorValue(this, input.Time);
	}
}
namespace StockSharp.Algo.Indicators;

using Ecng.ComponentModel;

/// <summary>
/// Weighted moving average.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/weighted_ma.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.WMAKey,
	Description = LocalizedStrings.WeightedMovingAverageKey)]
[Doc("topics/api/indicators/list_of_indicators/weighted_ma.html")]
public class WeightedMovingAverage : LengthIndicator<decimal>
{
	private decimal _denominator = 1;

	/// <summary>
	/// Initializes a new instance of the <see cref="WeightedMovingAverage"/>.
	/// </summary>
	public WeightedMovingAverage()
	{
		Length = 32;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_denominator = 0;

		for (var i = 1; i <= Length; i++)
			_denominator += i;
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var newValue = input.GetValue<decimal>();

		if (input.IsFinal)
		{
			Buffer.AddEx(newValue);
		}

		var buff = input.IsFinal ? Buffer : Buffer.Skip(1).Append(newValue);

		var w = 1;
		return new DecimalIndicatorValue(this, buff.Sum(v => w++ * v) / _denominator);
	}
}
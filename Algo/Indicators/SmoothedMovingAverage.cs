namespace StockSharp.Algo.Indicators;

/// <summary>
/// Smoothed Moving Average.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/smoothed_ma.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.SMMAKey,
	Description = LocalizedStrings.SmoothedMovingAverageKey)]
[Doc("topics/api/indicators/list_of_indicators/smoothed_ma.html")]
public class SmoothedMovingAverage : LengthIndicator<decimal>
{
	private decimal _prevFinalValue;

	/// <summary>
	/// Initializes a new instance of the <see cref="SmoothedMovingAverage"/>.
	/// </summary>
	public SmoothedMovingAverage()
	{
		Length = 32;
		Buffer.Operator = new DecimalOperator();
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_prevFinalValue = 0;
		base.Reset();
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var newValue = input.ToDecimal();

		if (!IsFormed)
		{
			if (input.IsFinal)
			{
				Buffer.PushBack(newValue);

				_prevFinalValue = Buffer.Sum / Length;

				return _prevFinalValue;
			}

			return (Buffer.SumNoFirst + newValue) / Length;
		}

		var curValue = (_prevFinalValue * (Length - 1) + newValue) / Length;

		if (input.IsFinal)
			_prevFinalValue = curValue;

		return curValue;
	}
}
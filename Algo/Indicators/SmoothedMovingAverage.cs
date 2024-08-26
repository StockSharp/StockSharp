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
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var newValue = input.GetValue<decimal>();

		if (!IsFormed)
		{
			if (input.IsFinal)
			{
				Buffer.AddEx(newValue);

				_prevFinalValue = Buffer.Sum / Length;

				return new DecimalIndicatorValue(this, _prevFinalValue, input.Time);
			}

			return new DecimalIndicatorValue(this, (Buffer.SumNoFirst + newValue) / Length, input.Time);
		}

		var curValue = (_prevFinalValue * (Length - 1) + newValue) / Length;

		if (input.IsFinal)
			_prevFinalValue = curValue;

		return new DecimalIndicatorValue(this, curValue, input.Time);
	}
}
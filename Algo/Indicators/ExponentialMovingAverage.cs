namespace StockSharp.Algo.Indicators;

/// <summary>
/// Exponential Moving Average.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/ema.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.EMAKey,
	Description = LocalizedStrings.ExponentialMovingAverageKey)]
[Doc("topics/api/indicators/list_of_indicators/ema.html")]
public class ExponentialMovingAverage : LengthIndicator<decimal>
{
	private decimal _prevFinalValue;
	private decimal _multiplier = 1;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExponentialMovingAverage"/>.
	/// </summary>
	public ExponentialMovingAverage()
	{
		Length = 32;
		Buffer.Operator = new DecimalOperator();
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_multiplier = 2m / (Length + 1);
		_prevFinalValue = 0;
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var newValue = input.ToDecimal();

		// буфер нужен только для формирования начального значение - SMA
		if (!IsFormed)
		{
			// пока sma не сформирована, возвращаем или "недоделанную" sma из финальных значенией
			// или "недоделанную" sma c пропущенным первым значением из буфера + промежуточное значение
			if (input.IsFinal)
			{
				Buffer.PushBack(newValue);

				_prevFinalValue = Buffer.Sum / Length;

				return _prevFinalValue;
			}
			else
			{
				return (Buffer.SumNoFirst + newValue) / Length;
			}
		}
		else
		{
			// если sma сформирована 
			// если IsFinal = true рассчитываем ema и сохраняем для последующих расчетов с промежуточными значениями
			var curValue = (newValue - _prevFinalValue) * _multiplier + _prevFinalValue;

			if (input.IsFinal)
				_prevFinalValue = curValue;

			return curValue;
		}
	}
}
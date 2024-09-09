namespace StockSharp.Algo.Indicators;

/// <summary>
/// Linear regression R-squared.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/IndicatorLinearRegRSquared.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.RSquaredKey,
	Description = LocalizedStrings.LinearRegRSquaredKey)]
[Doc("topics/IndicatorLinearRegRSquared.html")]
public class LinearRegRSquared : LengthIndicator<decimal>
{
	// Коэффициент при независимой переменной, угол наклона прямой.
	private decimal _slope;

	/// <summary>
	/// Initializes a new instance of the <see cref="LinearRegRSquared"/>.
	/// </summary>
	public LinearRegRSquared()
	{
		Length = 10;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();
		_slope = 0;
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var newValue = input.ToDecimal();

		if (input.IsFinal)
		{
			Buffer.PushBack(newValue);
		}

		var buff = input.IsFinal ? Buffer : (IList<decimal>)Buffer.Skip(1).Append(newValue).ToArray();

		// если значений хватает, считаем регрессию
		if (IsFormed)
		{
			//x - независимая переменная, номер значения в буфере
			//y - зависимая переменная - значения из буфера
			var sumX = 0m; //сумма x
			var sumY = 0m; //сумма y
			var sumXy = 0m; //сумма x*y
			var sumX2 = 0m; //сумма x^2

			for (var i = 0; i < Length; i++)
			{
				sumX += i;
				sumY += buff[i];
				sumXy += i * buff[i];
				sumX2 += i * i;
			}

			//коэффициент при независимой переменной
			var divisor = Length * sumX2 - sumX * sumX;
			if (divisor == 0) _slope = 0;
			else _slope = (Length * sumXy - sumX * sumY) / divisor;

			//свободный член
			var b = (sumY - _slope * sumX) / Length;

			//сумма квадратов отклонений от среднего и сумма квадратов ошибок
			var av = buff.Average();// среднее значение
			var sumYAv2 = 0m; //сумма квадратов отклонений от среднего
			var sumErr2 = 0m; //сумма квадратов ошибок

			for (var i = 0; i < Length; i++)
			{
				var y = buff[i];// значение
				var yEst = _slope * i + b;// оценка по регрессии
				sumYAv2 += (y - av) * (y - av);
				sumErr2 += (y - yEst) * (y - yEst);
			}

			//R-квадрат регресии
			if (sumYAv2 == 0) 
				return new DecimalIndicatorValue(this, 0, input.Time);

			return new DecimalIndicatorValue(this, (1 - sumErr2 / sumYAv2), input.Time);
		}

		return new DecimalIndicatorValue(this, input.Time);
	}
}
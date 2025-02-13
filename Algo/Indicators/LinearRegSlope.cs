namespace StockSharp.Algo.Indicators;

/// <summary>
/// Linear regression gradient.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/lrs.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.LRSKey,
	Description = LocalizedStrings.LinearRegSlopeKey)]
[Doc("topics/api/indicators/list_of_indicators/lrs.html")]
public class LinearRegSlope : LengthIndicator<decimal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="LinearRegSlope"/>.
	/// </summary>
	public LinearRegSlope()
	{
		Length = 11;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var newValue = input.ToDecimal();

		if (input.IsFinal)
		{
			Buffer.PushBack(newValue);
		}

		var buff = input.IsFinal ? Buffer : (IList<decimal>)[.. Buffer.Skip(1), newValue];

		//x - независимая переменная, номер значения в буфере
		//y - зависимая переменная - значения из буфера
		var sumX = 0m; //сумма x
		var sumY = 0m; //сумма y
		var sumXy = 0m; //сумма x*y
		var sumX2 = 0m; //сумма x^2

		for (var i = 0; i < buff.Count; i++)
		{
			sumX += i;
			sumY += buff[i];
			sumXy += i * buff[i];
			sumX2 += i * i;
		}

		//коэффициент при независимой переменной
		var divisor = Length * sumX2 - sumX * sumX;
		if (divisor == 0) 
			return null;

		return (Length * sumXy - sumX * sumY) / divisor;
	}
}
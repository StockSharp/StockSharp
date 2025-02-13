namespace StockSharp.Algo.Indicators;

/// <summary>
/// Linear regression - Value returns the last point prediction.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/lrc.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.LRCKey,
	Description = LocalizedStrings.LinearRegressionKey)]
[Doc("topics/api/indicators/list_of_indicators/lrc.html")]
public class LinearReg : LengthIndicator<decimal>
{
	// Коэффициент при независимой переменной, угол наклона прямой.
	private decimal _slope;

	/// <summary>
	/// Initializes a new instance of the <see cref="LinearReg"/>.
	/// </summary>
	public LinearReg()
	{
		Length = 11;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_slope = 0;
		base.Reset();
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var newValue = input.ToDecimal();

		if (input.IsFinal)
		{
			Buffer.PushBack(newValue);

			if (Buffer.Count > Length)
				Buffer.PopFront();
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
		if (divisor == 0) _slope = 0;
		else _slope = (Length * sumXy - sumX * sumY) / divisor;

		//свободный член
		var b = (sumY - _slope * sumX) / Length;

		//прогноз последнего значения (его номер Length - 1)
		return _slope * (Length - 1) + b;
	}
}
namespace StockSharp.Algo.Indicators
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Localization;

	/// <summary>
	/// R-квадрат в линейной регрессии.
	/// </summary>
	[DisplayName("RSquared")]
	[DescriptionLoc(LocalizedStrings.Str746Key)]
	public class RSquared : LengthIndicator<decimal>
	{
		// Коэффициент при независимой переменной, угол наклона прямой.
		private decimal _slope;

		/// <summary>
		/// Создать <see cref="RSquared"/>.
		/// </summary>
		public RSquared()
			: base(typeof(decimal))
		{
			Length = 10;
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			base.Reset();
			_slope = 0;
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var newValue = input.GetValue<decimal>();

			if (input.IsFinal)
			{
				Buffer.Add(newValue);

				if (Buffer.Count > Length)
					Buffer.RemoveAt(0);
			}

			var buff = Buffer;
			if (!input.IsFinal)
			{
				buff = new List<decimal>();
				buff.AddRange(Buffer.Skip(1));
				buff.Add(newValue);
			}

			// если значений хватает, считаем регрессию
			if (IsFormed)
			{
				//x - независимая переменная, номер значения в буфере
				//y - зависимая переменная - значения из буфера
				decimal sumX = 0m; //сумма x
				decimal sumY = 0m; //сумма y
				decimal sumXy = 0m; //сумма x*y
				decimal sumX2 = 0m; //сумма x^2

				for (int i = 0; i < Length; i++)
				{
					sumX += i;
					sumY += buff.ElementAt(i);
					sumXy += i * buff.ElementAt(i);
					sumX2 += i * i;
				}

				//коэффициент при независимой переменной
				var divisor = (Length * sumX2 - sumX * sumX);
				if (divisor == 0) _slope = 0;
				else _slope = (Length * sumXy - sumX * sumY) / divisor;

				//свободный член
				decimal b = (sumY - _slope * sumX) / Length;

				//сумма квадратов отклонений от среднего и сумма квадратов ошибок
				decimal av = buff.Average();// срднее значение
				decimal sumYAv2 = 0m; //сумма квадратов отклонений от среднего
				decimal sumErr2 = 0m; //сумма квадратов ошибок

				for (int i = 0; i < Length; i++)
				{
					decimal y = buff.ElementAt(i);// значение
					decimal yEst = _slope * i + b;// оценка по регрессии
					sumYAv2 += (y - av) * (y - av);
					sumErr2 += (y - yEst) * (y - yEst);
				}

				//R-квадрат регресии
				if (sumYAv2 == 0) 
					return new DecimalIndicatorValue(this, 0);

				return new DecimalIndicatorValue(this, (1 - sumErr2 / sumYAv2));
			}

			return new DecimalIndicatorValue(this);
		}
	}
}
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Localization;

	/// <summary>
	/// Стандартная ошибка в линейной регрессии.
	/// </summary>
	[DisplayName("StdErr")]
	[DescriptionLoc(LocalizedStrings.Str750Key)]
	public class StandardError : LengthIndicator<decimal>
	{
		// Коэффициент при независимой переменной, угол наклона прямой.
		private decimal _slope;

		/// <summary>
		/// Создать <see cref="StandardError"/>.
		/// </summary>
		public StandardError()
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
				var sumX = 0m; //сумма x
				var sumY = 0m; //сумма y
				var sumXy = 0m; //сумма x*y
				var sumX2 = 0m; //сумма x^2

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
				var b = (sumY - _slope * sumX) / Length;

				//счиаем сумму квадратов ошибок
				var sumErr2 = 0m; //сумма квадратов ошибок

				for (int i = 0; i < Length; i++)
				{
					var y = buff.ElementAt(i); // значение
					var yEst = _slope * i + b; // оценка по регрессии
					sumErr2 += (y - yEst) * (y - yEst);
				}

				//Стандартная ошибка
				if (Length == 2)
				{
					return new DecimalIndicatorValue(this, 0); //если всего 2 точки, то прямая проходит через них и стандартная ошибка равна нулю.
				}
				else
				{
					return new DecimalIndicatorValue(this, (decimal)Math.Sqrt((double)(sumErr2 / (Length - 2))));
				}
			}

			return new DecimalIndicatorValue(this);
		}
	}
}
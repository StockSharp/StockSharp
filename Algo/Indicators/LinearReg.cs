namespace StockSharp.Algo.Indicators
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Localization;

	/// <summary>
	/// Линейная регрессия - Value возвращает прогноз последней точки.
	/// </summary>
	[DisplayName("LinearReg")]
	[DescriptionLoc(LocalizedStrings.Str734Key)]
	public class LinearReg : LengthIndicator<decimal>
	{
		// Коэффициент при независимой переменной, угол наклона прямой.
		private decimal _slope;

		/// <summary>
		/// Создать <see cref="LinearReg"/>.
		/// </summary>
		public LinearReg()
		{
			Length = 11;
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			_slope = 0;
			base.Reset();
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

			//x - независимая переменная, номер значения в буфере
			//y - зависимая переменная - значения из буфера
			var sumX = 0m; //сумма x
			var sumY = 0m; //сумма y
			var sumXy = 0m; //сумма x*y
			var sumX2 = 0m; //сумма x^2

			for (var i = 0; i < buff.Count; i++)
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

			//прогноз последнего значения (его номер Length - 1)
			return new DecimalIndicatorValue(this, _slope * (Length - 1) + b);
		}
	}
}
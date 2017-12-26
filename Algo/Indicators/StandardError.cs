#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: StandardError.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Localization;

	/// <summary>
	/// Standard error in linear regression.
	/// </summary>
	[DisplayName("StdErr")]
	[DescriptionLoc(LocalizedStrings.Str750Key)]
	public class StandardError : LengthIndicator<decimal>
	{
		// Коэффициент при независимой переменной, угол наклона прямой.
		private decimal _slope;

		/// <summary>
		/// Initializes a new instance of the <see cref="StandardError"/>.
		/// </summary>
		public StandardError()
		{
			Length = 10;
		}

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			base.Reset();
			_slope = 0;
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
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

				for (var i = 0; i < Length; i++)
				{
					sumX += i;
					sumY += buff.ElementAt(i);
					sumXy += i * buff.ElementAt(i);
					sumX2 += i * i;
				}

				//коэффициент при независимой переменной
				var divisor = Length * sumX2 - sumX * sumX;
				if (divisor == 0) _slope = 0;
				else _slope = (Length * sumXy - sumX * sumY) / divisor;

				//свободный член
				var b = (sumY - _slope * sumX) / Length;

				//счиаем сумму квадратов ошибок
				var sumErr2 = 0m; //сумма квадратов ошибок

				for (var i = 0; i < Length; i++)
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
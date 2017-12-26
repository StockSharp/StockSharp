#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: LinearRegSlope.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Localization;

	/// <summary>
	/// Linear regression gradient.
	/// </summary>
	[DisplayName("LinearRegSlope")]
	[DescriptionLoc(LocalizedStrings.Str742Key)]
	public class LinearRegSlope : LengthIndicator<decimal>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LinearRegSlope"/>.
		/// </summary>
		public LinearRegSlope()
		{
			Length = 11;
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
			var divisor = Length * sumX2 - sumX * sumX;
			if (divisor == 0) 
				return new DecimalIndicatorValue(this);

			return new DecimalIndicatorValue(this, (Length * sumXy - sumX * sumY) / divisor);
		}
	}
}
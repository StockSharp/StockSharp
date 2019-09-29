#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleUnit.SampleUnitPublic
File: Program.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleUnit
{
	using System;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	class Program
	{
		static void Main()
		{
			// test instrument with pips = 1 cent and points = 10 usd
			var security = new Security
			{
				Id = "AAPL@NASDAQ",
				StepPrice = 10,
				PriceStep = 0.01m,
			};

			var absolute = new Unit(30);
			var percent = 30.0.Percents();
			var pips = 30.0.Pips(security);
			var point = 30.0.Points(security);

			Console.WriteLine("absolute = " + absolute);
			Console.WriteLine("percent = " + percent);
			Console.WriteLine("pips = " + pips);
			Console.WriteLine("point = " + point);
			Console.WriteLine();

			// test values as a $90
			const decimal testValue = 90m;
			// or using this notation
			// var testValue = (decimal)new Unit { Value = 90 };

			Console.WriteLine("testValue = " + testValue);
			Console.WriteLine();

			// addition of all values
			Console.WriteLine("testValue + absolute = " + (testValue + absolute));
			Console.WriteLine("testValue + percent = " + (testValue + percent));
			Console.WriteLine("testValue + pips = " + (testValue + pips));
			Console.WriteLine("testValue + point = " + (testValue + point));
			Console.WriteLine();

			// multiplication of all values
			Console.WriteLine("testValue * absolute = " + (testValue * absolute));
			Console.WriteLine("testValue * percent = " + (testValue * percent));
			Console.WriteLine("testValue * pips = " + (testValue * pips));
			Console.WriteLine("testValue * point = " + (testValue * point));
			Console.WriteLine();

			// subtraction of values
			Console.WriteLine("testValue - absolute = " + (testValue - absolute));
			Console.WriteLine("testValue - percent = " + (testValue - percent));
			Console.WriteLine("testValue - pips = " + (testValue - pips));
			Console.WriteLine("testValue - point = " + (testValue - point));
			Console.WriteLine();

			// division of all values
			Console.WriteLine("testValue / absolute = " + (testValue / absolute));
			Console.WriteLine("testValue / percent = " + (testValue / percent));
			Console.WriteLine("testValue / pips = " + (testValue / pips));
			Console.WriteLine("testValue / point = " + (testValue / point));
			Console.WriteLine();

			// addition of pips and points
			var resultPipsPoint = pips + point;
			// and casting to decimal
			var resultPipsPointDecimal = (decimal)resultPipsPoint;

			Console.WriteLine("pips + point = " + resultPipsPoint);
			Console.WriteLine("(decimal)(pips + point) = " + resultPipsPointDecimal);
			Console.WriteLine();

			// addition of pips and percents
			var resultPipsPercents = pips + percent;
			// and casting to decimal
			var resultPipsPercentsDecimal = (decimal)resultPipsPercents;

			Console.WriteLine("pips + percent = " + resultPipsPercents);
			Console.WriteLine("(decimal)(pips + percent) = " + resultPipsPercentsDecimal);
			Console.WriteLine();
		}
	}
}
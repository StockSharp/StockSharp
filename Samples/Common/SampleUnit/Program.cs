namespace SampleUnit
{
	using System;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	class Program
	{
		static void Main()
		{
			// тестовый инструмент и шагом цены в 1 копейку и стоимостью в 10 рублей
			// (в реальном приложении информацию необходимо получать через IConnector.NewSecurities)
			var security = new Security
			{
				Id = "LKOH@TQNE",
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

			// тестовые значение в 90 рублей
			const decimal testValue = 90m;
			// или можно создать через приведение
			// var testValue = (decimal)new Unit { Value = 90 };

			Console.WriteLine("testValue = " + testValue);
			Console.WriteLine();

			// сложение всех величин
			Console.WriteLine("testValue + absolute = " + (testValue + absolute));
			Console.WriteLine("testValue + percent = " + (testValue + percent));
			Console.WriteLine("testValue + pips = " + (testValue + pips));
			Console.WriteLine("testValue + point = " + (testValue + point));
			Console.WriteLine();

			// умножение всех величин
			Console.WriteLine("testValue * absolute = " + (testValue * absolute));
			Console.WriteLine("testValue * percent = " + (testValue * percent));
			Console.WriteLine("testValue * pips = " + (testValue * pips));
			Console.WriteLine("testValue * point = " + (testValue * point));
			Console.WriteLine();

			// вычитание всех величин
			Console.WriteLine("testValue - absolute = " + (testValue - absolute));
			Console.WriteLine("testValue - percent = " + (testValue - percent));
			Console.WriteLine("testValue - pips = " + (testValue - pips));
			Console.WriteLine("testValue - point = " + (testValue - point));
			Console.WriteLine();

			// деление всех величин
			Console.WriteLine("testValue / absolute = " + (testValue / absolute));
			Console.WriteLine("testValue / percent = " + (testValue / percent));
			Console.WriteLine("testValue / pips = " + (testValue / pips));
			Console.WriteLine("testValue / point = " + (testValue / point));
			Console.WriteLine();

			// сложение пипсов и пунктов
			var resultPipsPoint = pips + point;
			// и приведением из в decimal
			var resultPipsPointDecimal = (decimal)resultPipsPoint;

			Console.WriteLine("pips + point = " + resultPipsPoint);
			Console.WriteLine("(decimal)(pips + point) = " + resultPipsPointDecimal);
			Console.WriteLine();

			// сложение пипсов и процентов
			var resultPipsPercents = pips + percent;
			// и приведением из в decimal
			var resultPipsPercentsDecimal = (decimal)resultPipsPercents;

			Console.WriteLine("pips + percent = " + resultPipsPercents);
			Console.WriteLine("(decimal)(pips + percent) = " + resultPipsPercentsDecimal);
			Console.WriteLine();
		}
	}
}
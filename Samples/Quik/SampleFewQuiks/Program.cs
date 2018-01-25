#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleFewQuiks.SampleFewQuiksPublic
File: Program.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleFewQuiks
{
	using System;
	using System.Net;
	using System.Threading;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Quik;
	using StockSharp.Localization;

	class Program
	{
		private static volatile Portfolio _portfolio1;
		private static volatile Portfolio _portfolio2;

		private static volatile Security _lkoh;
		private static volatile Security _ri;

		static void Main()
		{
			try
			{
				Console.Write(LocalizedStrings.Str2992);
				var account1 = Console.ReadLine();

				Console.Write(LocalizedStrings.Str2993);
				var account2 = Console.ReadLine();

				using (var quikTrader1 = new QuikTrader { LuaFixServerAddress = "127.0.0.1:5001".To<EndPoint>() })
				using (var quikTrader2 = new QuikTrader { LuaFixServerAddress = "127.0.0.1:5002".To<EndPoint>() })
				{
					// подписываемся на событие ошибок обработки данных и разрыва соединения
					//
					quikTrader1.Error += OnError;
					quikTrader2.Error += OnError;

					quikTrader1.ConnectionError += OnError;
					quikTrader2.ConnectionError += OnError;

				
					var portfoliosWait = new ManualResetEvent(false);

					void NewPortfolio(Portfolio portfolio)
					{
						if (_portfolio1 == null && portfolio.Name == account1)
							_portfolio1 = portfolio;

						if (_portfolio2 == null && portfolio.Name == account2)
							_portfolio2 = portfolio;

						// если оба инструмента появились
						if (_portfolio1 != null && _portfolio2 != null)
							portfoliosWait.Set();
					}

					// подписываемся на события новых портфелей
					quikTrader1.NewPortfolio += NewPortfolio;
					quikTrader2.NewPortfolio += NewPortfolio;


					var securitiesWait = new ManualResetEvent(false);

					// подписываемся на события новых инструментов
					quikTrader1.NewSecurity += security =>
					{
						if (_lkoh == null && security.Code == "LKOH")
							_lkoh = security;

						// если оба инструмента появились
						if (_lkoh != null && _ri != null)
							securitiesWait.Set();
					};
					quikTrader2.NewSecurity += security =>
					{
						if (_ri == null && security.Code == "RIZ7")
							_ri = security;

						// если оба инструмента появились
						if (_lkoh != null && _ri != null)
							securitiesWait.Set();
					};


					// запускаем экспорты в Quik-ах, когда получим событие об успешном соединении
					//
					quikTrader1.Connected += () =>
					{
						Console.WriteLine(LocalizedStrings.Str2994Params.Put(quikTrader1.LuaFixServerAddress));
					};
					quikTrader2.Connected += () =>
					{
						Console.WriteLine(LocalizedStrings.Str2994Params.Put(quikTrader2.LuaFixServerAddress));
					};

					// производим подключение каждого из QuikTrader-а
					//
					quikTrader1.Connect();
					quikTrader2.Connect();

					Console.WriteLine(LocalizedStrings.Str2995);
					portfoliosWait.WaitOne();
					securitiesWait.WaitOne();

					Console.WriteLine(LocalizedStrings.Str2996);
					if (_lkoh.BestBid == null || _ri.BestBid == null)
						throw new Exception(LocalizedStrings.Str2990);

					quikTrader1.RegisterOrder(new Order
					{
						Portfolio = _portfolio1,
						Volume = 1,
						Security = _lkoh,
						Price = _lkoh.BestBid.Price
					});
					Console.WriteLine(LocalizedStrings.Str2997);

					quikTrader2.RegisterOrder(new Order
					{
						Portfolio = _portfolio2,
						Volume = 1,
						Security = _ri,
						Price = _ri.BestBid.Price
					});
					Console.WriteLine(LocalizedStrings.Str2998);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		private static void OnError(Exception error)
		{
			Console.WriteLine(error);
		}
	}
}
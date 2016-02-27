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
	using System.Collections.Generic;
	using System.Linq;
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
		private static volatile Security _riz0;

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

					Action<IEnumerable<Portfolio>> newPortfolios = portfolios =>
					{
						if (_portfolio1 == null)
							_portfolio1 = portfolios.FirstOrDefault(p => p.Name == account1);

						if (_portfolio2 == null)
							_portfolio2 = portfolios.FirstOrDefault(p => p.Name == account2);

						// если оба инструмента появились
						if (_portfolio1 != null && _portfolio2 != null)
							portfoliosWait.Set();
					};

					// подписываемся на события новых портфелей
					quikTrader1.NewPortfolios += newPortfolios;
					quikTrader2.NewPortfolios += newPortfolios;


					var securitiesWait = new ManualResetEvent(false);

					// подписываемся на события новых инструментов
					quikTrader1.NewSecurities += securities =>
					{
						if (_lkoh == null)
							_lkoh = securities.FirstOrDefault(s => s.Code == "LKOH");

						// если оба инструмента появились
						if (_lkoh != null && _riz0 != null)
							securitiesWait.Set();
					};
					quikTrader2.NewSecurities += securities =>
					{
						if (_riz0 == null)
							_riz0 = securities.FirstOrDefault(s => s.Code == "RIZ0");

						// если оба инструмента появились
						if (_lkoh != null && _riz0 != null)
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
					if (_lkoh.BestBid == null || _riz0.BestBid == null)
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
						Security = _riz0,
						Price = _riz0.BestBid.Price
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
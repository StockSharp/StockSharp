#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleSmartConsole.SampleSmartConsolePublic
File: Program.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleSmartConsole
{
	using System;
	using System.Threading;

	using Ecng.Common;
	using Ecng.Localization;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.SmartCom;
	using StockSharp.Algo;
	using StockSharp.Localization;

	class Program
	{
		private static Security _lkoh;
		private static Portfolio _portfolio;

		static void Main()
		{
			try
			{
				// для теста выбираем бумагу Лукойл
				const string secCode = "LKOH";

				Console.Write(LocalizedStrings.EnterLogin);
				var login = Console.ReadLine();

				Console.Write(LocalizedStrings.EnterPassword);
				var password = Console.ReadLine();

				Console.Write("Enter account number through which an order will be placed:".Translate());
				var account = Console.ReadLine();

				using (var waitHandle = new AutoResetEvent(false))
				{
					// создаем подключение к Smart-у
					using (var trader = new SmartTrader { Login = login, Password = password, Address = SmartComAddresses.Demo })
					{
						// подписываемся на событие успешного подключения
						// все действия необходимо производить только после подключения
						trader.Connected += () =>
						{
							Console.WriteLine(LocalizedStrings.Str2169);

							// извещаем об успешном соединени
							waitHandle.Set();
						};

						Console.WriteLine(LocalizedStrings.Str2170);

						trader.Connect();

						// дожидаемся события об успешном соединении
						waitHandle.WaitOne();

						// подписываемся на все портфели-счета
						trader.NewPortfolio += portfolio =>
						{
							if (_portfolio != null)
								return;

							// находим нужный портфель и присваиваем его переменной _portfolio

							if (portfolio.Name != account)
								return;

							_portfolio = portfolio;

							Console.WriteLine(LocalizedStrings.Str2171Params, account);

							if (_lkoh != null)
								waitHandle.Set();
						};

						// подписываемся на событие появление инструментов
						trader.NewSecurity += security =>
						{
							if (_lkoh == null)
							{
								if (security.Code != secCode || security.Type != SecurityTypes.Stock)
									return;

								// находим Лукойл и присваиваем ее переменной lkoh
								_lkoh = security;

								if (_lkoh != null)
								{
									Console.WriteLine(LocalizedStrings.Str2987);

									if (_portfolio != null)
										waitHandle.Set();
								}
							}
						};

						// подписываемся на событие появления моих новых сделок
						trader.NewMyTrade += myTrade =>
						{
							var trade = myTrade.Trade;
							Console.WriteLine(LocalizedStrings.Str2173Params, trade.Id, trade.Price, trade.Security.Code, trade.Volume, trade.Time);
						};

						Console.WriteLine(LocalizedStrings.Str2989Params.Put(account));

						// дожидаемся появления портфеля и инструмента
						waitHandle.WaitOne();

						trader.SecurityChanged += security =>
						{
							// если инструмент хоть раз изменился (по нему пришли актуальные данные)
							if (security == _lkoh && _lkoh.BestBid != null && _lkoh.BestAsk != null)
								waitHandle.Set();
						};

						Console.WriteLine("Waiting for Lukoil security data to update...".Translate());

						// запускаем обновление по инструменту
						trader.RegisterSecurity(_lkoh);
						waitHandle.WaitOne();

						// 0.1% от изменения цены
						const decimal delta = 0.001m;

						// запоминаем первоначальное значение середины спреда
						var firstMid = _lkoh.BestPair.SpreadPrice / 2;
						if (_lkoh.BestBid == null || firstMid == null)
							throw new Exception(LocalizedStrings.Str2990);

						Console.WriteLine(LocalizedStrings.Str2991Params, _lkoh.BestBid.Price + firstMid);

						while (true)
						{
							var mid = _lkoh.BestPair.SpreadPrice / 2;

							// если спред вышел за пределы нашего диапазона
							if (mid != null &&
								((firstMid + firstMid * delta) <= mid ||
								(firstMid - firstMid * delta) >= mid)
								)
							{
								var order = new Order
								{
									Portfolio = _portfolio,
									Price = _lkoh.ShrinkPrice(_lkoh.BestBid.Price + mid.Value),
									Security = _lkoh,
									Volume = 1,
									Direction = Sides.Buy,
								};
								trader.RegisterOrder(order);
								Console.WriteLine(LocalizedStrings.Str1157Params, order.Id);
								break;
							}
							else
								Console.WriteLine(LocalizedStrings.Str2176Params, _lkoh.BestBid.Price + mid);

							// ждем 1 секунду
							Thread.Sleep(1000);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}
	}
}
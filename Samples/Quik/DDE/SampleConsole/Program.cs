#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleConsole.SampleConsolePublic
File: Program.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleConsole
{
	using System;
	using System.Threading;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Quik;
	using StockSharp.Algo;
	using StockSharp.Localization;

	class Program
	{
		private static Security _lkoh;
		private static Portfolio _portfolio;
		private static MarketDepth _depth;

		static void Main()
		{
			try
			{
				// для теста выбираем бумагу Лукойл
				const string secCode = "LKOH";

				var quikPath = QuikTerminal.GetDefaultPath();

				if (quikPath.IsEmpty())
				{
					Console.WriteLine(LocalizedStrings.Str2984);
					return;
				}

				Console.WriteLine(LocalizedStrings.Str2985.Put(quikPath));

				Console.Write(LocalizedStrings.Str2986);
				var account = Console.ReadLine();

				using (var waitHandle = new AutoResetEvent(false))
				{
					// создаем подключение к Quik-у
					using (var trader = new QuikTrader(quikPath) { IsDde = true })
					{
						// необходимо раскомментировать, если идет работа с РТС Стандарт
						//trader.FormatTransaction += builder => builder.RemoveInstruction(Transaction.TimeInForce);

						// подписываемся на событие успешного подключения
						// все действия необходимо производить только после подключения
						trader.Connected += () =>
						{
							Console.WriteLine(LocalizedStrings.Str2169);

							// извещаем об успешном соединени
							waitHandle.Set();
						};

						Console.WriteLine(LocalizedStrings.Str2170);

						trader.DdeTables = new[] { trader.SecuritiesTable, trader.MyTradesTable, trader.EquityPositionsTable,
						                   trader.EquityPortfoliosTable, trader.OrdersTable };

						trader.Connect();

						// дожидаемся события об успешном соединении
						waitHandle.WaitOne();

						trader.NewPortfolio += portfolio =>
						{
							if (_portfolio == null && portfolio.Name == account)
							{
								// находим нужный портфель и присваиваем его переменной _portfolio
								_portfolio = portfolio;

								Console.WriteLine(LocalizedStrings.Str2171Params, account);

								// если инструмент и стакан уже появились,
								// то извещаем об этом основной поток для выставления заявки
								if (_lkoh != null && _depth != null)
									waitHandle.Set();
							}
						};

						// подписываемся на событие появление инструментов
						trader.NewSecurity += security =>
						{
							if (_lkoh == null)
							{
								if (!security.Code.CompareIgnoreCase(secCode))
									return;

								// находим Лукойл и присваиваем ее переменной lkoh
								_lkoh = security;

								if (_lkoh != null)
								{
									Console.WriteLine(LocalizedStrings.Str2987);

									// запускаем экспорт стакана
									trader.RegisterMarketDepth(_lkoh);

									if (_portfolio != null && _depth != null)
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

						// подписываемся на событие обновления стакана
						trader.MarketDepthChanged += depth =>
						{
							if (_depth == null && _lkoh != null && depth.Security == _lkoh)
							{
								_depth = depth;

								Console.WriteLine(LocalizedStrings.Str2988);

								// если портфель и инструмент уже появился, то извещаем об этом основной поток для выставления заявки
								if (_portfolio != null && _lkoh != null)
									waitHandle.Set();
							}
						};

						Console.WriteLine(LocalizedStrings.Str2989Params.Put(account));

						// дожидаемся появления портфеля и инструмента
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

						// останавливаем подключение
						trader.Disconnect();
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
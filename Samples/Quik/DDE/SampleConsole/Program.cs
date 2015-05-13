namespace SampleConsole
{
	using System;
	using System.Linq;
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

				Console.WriteLine(LocalizedStrings.Str2985 + quikPath);

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

						trader.NewPortfolios += portfolios =>
						{
							if (_portfolio == null)
							{
								// находим нужный портфель и присваиваем его переменной _portfolio
								_portfolio = portfolios.FirstOrDefault(p => p.Name == account);

								if (_portfolio != null)
								{
									Console.WriteLine(LocalizedStrings.Str2171Params, account);

									// если инструмент и стакан уже появились,
									// то извещаем об этом основной поток для выставления заявки
									if (_lkoh != null && _depth != null)
										waitHandle.Set();
								}
							}
						};

						// подписываемся на событие появление инструментов
						trader.NewSecurities += securities =>
						{
							if (_lkoh == null)
							{
								// находим Лукойл и присваиваем ее переменной lkoh
								_lkoh = securities.FirstOrDefault(sec => sec.Code == secCode);

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
						trader.NewMyTrades += myTrades =>
						{
							foreach (var myTrade in myTrades)
							{
								var trade = myTrade.Trade;
								Console.WriteLine(LocalizedStrings.Str2173Params, trade.Id, trade.Price, trade.Security.Code, trade.Volume, trade.Time);
							}
						};

						// подписываемся на событие обновления стакана
						trader.MarketDepthsChanged += depths =>
						{
							if (_depth == null && _lkoh != null)
							{
								_depth = depths.FirstOrDefault(d => d.Security == _lkoh);

								if (_depth != null)
								{
									Console.WriteLine(LocalizedStrings.Str2988);

									// если портфель и инструмент уже появился, то извещаем об этом основной поток для выставления заявки
									if (_portfolio != null && _lkoh != null)
										waitHandle.Set();
								}
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
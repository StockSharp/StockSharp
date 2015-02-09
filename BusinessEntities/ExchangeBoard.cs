namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Reflection;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;
	using Ecng.Configuration;
	using Ecng.Reflection;

	using MoreLinq;

	using StockSharp.Messages;

	using StockSharp.Localization;

	/// <summary>
	/// Информация о биржевой площадке.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[Ignore(FieldName = "IsDisposed")]
	public class ExchangeBoard : Equatable<ExchangeBoard>, IExtendableEntity, IPersistable, INotifyPropertyChanged
	{
		private class InMemoryExchangeInfoProvider : IExchangeInfoProvider
		{
			private readonly CachedSynchronizedDictionary<string, ExchangeBoard> _boards = new CachedSynchronizedDictionary<string, ExchangeBoard>(StringComparer.InvariantCultureIgnoreCase);
			private readonly CachedSynchronizedDictionary<string, Exchange> _exchanges = new CachedSynchronizedDictionary<string, Exchange>(StringComparer.InvariantCultureIgnoreCase);

			IEnumerable<ExchangeBoard> IExchangeInfoProvider.Boards
			{
				get { return _boards.CachedValues; }
			}

			IEnumerable<Exchange> IExchangeInfoProvider.Exchanges
			{
				get { return _exchanges.CachedValues; }
			}

			ExchangeBoard IExchangeInfoProvider.GetExchangeBoard(string code)
			{
				return _boards.TryGetValue(code);
			}

			Exchange IExchangeInfoProvider.GetExchange(string code)
			{
				return _exchanges.TryGetValue(code);
			}

			void IExchangeInfoProvider.Save(ExchangeBoard board)
			{
				if (board == null)
					throw new ArgumentNullException("board");

				lock (_boards.SyncRoot)
				{
					if (!_boards.TryAdd(board.Code, board))
						return;
				}

				BoardAdded.SafeInvoke(board);
			}

			void IExchangeInfoProvider.Save(Exchange exchange)
			{
				if (exchange == null)
					throw new ArgumentNullException("exchange");

				lock (_exchanges.SyncRoot)
				{
					if (!_exchanges.TryAdd(exchange.Name, exchange))
						return;
				}

				ExchangeAdded.SafeInvoke(exchange);
			}

			public event Action<ExchangeBoard> BoardAdded;

			public event Action<Exchange> ExchangeAdded;

			public InMemoryExchangeInfoProvider()
			{
				EnumerateExchanges().ForEach(b => _exchanges[b.Name] = b);
				EnumerateExchangeBoards().ForEach(b => _boards[b.Code] = b);
			}
		}

		private static readonly SyncObject _syncObject = new SyncObject();
		private static IExchangeInfoProvider _exchangeInfoProvider;

		private static IExchangeInfoProvider ExchangeInfoProvider
		{
			get
			{
				if (_exchangeInfoProvider != null)
					return _exchangeInfoProvider;

				lock (_syncObject)
				{
					if (_exchangeInfoProvider == null)
					{
						_exchangeInfoProvider = ConfigManager.TryGetService<IExchangeInfoProvider>();

						if (_exchangeInfoProvider != null)
							return _exchangeInfoProvider;

						ConfigManager.RegisterService(_exchangeInfoProvider = new InMemoryExchangeInfoProvider());	
					}

					return _exchangeInfoProvider;
				}
			}
		}

		static ExchangeBoard()
		{
			// NOTE
			// описание ММВБ площадок взято из документа http://fs.rts.micex.ru/files/707

			Associated = new ExchangeBoard
			{
				Code = "ALL",
				Exchange = Exchange.Test,
			};

			Test = new ExchangeBoard
			{
				Code = "TEST",
				Exchange = Exchange.Test,
			};

			Finam = new ExchangeBoard
			{
				Code = "FINAM",
				Exchange = Exchange.Test,
			};

			Mfd = new ExchangeBoard
			{
				Code = "MFD",
				Exchange = Exchange.Test,
			};

			// http://stocksharp.com/forum/yaf_postst667_Rabochiie-dni-dlia-birzh--2011-ghod.aspx
			// http://stocksharp.com/forum/yaf_postst1689_Exchange-WorkingTime-2012.aspx
			var russianSpecialWorkingDays = new[]
			{
				// http://www.rts.ru/a742
				new DateTime(2001, 3, 11),
				new DateTime(2001, 4, 28),
				new DateTime(2001, 6, 9),
				new DateTime(2001, 12, 29),

				// http://www.rts.ru/a3414
				new DateTime(2002, 4, 27),
				new DateTime(2002, 5, 18),
				new DateTime(2002, 11, 10),

				// http://www.rts.ru/a5194
				new DateTime(2003, 1, 4),
				new DateTime(2003, 1, 5),
				new DateTime(2003, 6, 21),

				// http://www.rts.ru/a6598
				// дат нет

				// http://www.rts.ru/a7751
				new DateTime(2005, 3, 5),
				new DateTime(2005, 5, 14),

				// http://www.rts.ru/a743
				new DateTime(2006, 2, 26),
				new DateTime(2006, 5, 6),

				// http://www.rts.ru/a13059
				new DateTime(2007, 4, 28),
				new DateTime(2007, 6, 9),

				// http://www.rts.ru/a15065
				new DateTime(2008, 5, 4),
				new DateTime(2008, 6, 7),
				new DateTime(2008, 11, 1),

				// http://www.rts.ru/a17902
				new DateTime(2009, 1, 11),

				// http://www.rts.ru/a19524
				new DateTime(2010, 2, 27),
				new DateTime(2010, 11, 13),

				// http://www.rts.ru/s355
				new DateTime(2011, 3, 5),
				
				// http://rts.micex.ru/a254
				new DateTime(2012, 3, 11),
				new DateTime(2012, 4, 28),
				new DateTime(2012, 5, 5),
				new DateTime(2012, 5, 12),
				new DateTime(2012, 6, 9),
				new DateTime(2012, 12, 29)
			};

			var russianSpecialHolidays = new[]
			{
				// http://www.rts.ru/a742
				new DateTime(2001, 1, 1),
				new DateTime(2001, 1, 2),
				new DateTime(2001, 1, 8),
				new DateTime(2001, 3, 8),
				new DateTime(2001, 3, 9),
				new DateTime(2001, 4, 30),
				new DateTime(2001, 5, 1),
				new DateTime(2001, 5, 2),
				new DateTime(2001, 5, 9),
				new DateTime(2001, 6, 11),
				new DateTime(2001, 6, 12),
				new DateTime(2001, 11, 7),
				new DateTime(2001, 12, 12),
				new DateTime(2001, 12, 31),

				// http://www.rts.ru/a3414
				new DateTime(2002, 1, 1),
				new DateTime(2002, 1, 2),
				new DateTime(2002, 1, 7),
				new DateTime(2002, 2, 25),
				new DateTime(2002, 3, 8),
				new DateTime(2002, 3, 9),
				new DateTime(2002, 5, 1),
				new DateTime(2002, 5, 2),
				new DateTime(2002, 5, 3),
				new DateTime(2002, 5, 9),
				new DateTime(2002, 5, 10),
				new DateTime(2002, 6, 12),
				new DateTime(2002, 11, 7),
				new DateTime(2002, 11, 8),
				new DateTime(2002, 12, 12),
				new DateTime(2002, 12, 13),

				// http://www.rts.ru/a5194
				new DateTime(2003, 1, 1),
				new DateTime(2003, 1, 2),
				new DateTime(2003, 1, 3),
				new DateTime(2003, 1, 6),
				new DateTime(2003, 1, 7),
				new DateTime(2003, 2, 24),
				new DateTime(2003, 3, 10),
				new DateTime(2003, 5, 1),
				new DateTime(2003, 5, 2),
				new DateTime(2003, 5, 9),
				new DateTime(2003, 6, 12),
				new DateTime(2003, 6, 13),
				new DateTime(2003, 11, 7),
				new DateTime(2003, 12, 12),

				// http://www.rts.ru/a6598
				new DateTime(2004, 1, 1),
				new DateTime(2004, 1, 2),
				new DateTime(2004, 1, 7),
				new DateTime(2004, 2, 23),
				new DateTime(2004, 3, 8),
				new DateTime(2004, 5, 3),
				new DateTime(2004, 5, 4),
				new DateTime(2004, 5, 10),
				new DateTime(2004, 6, 14),
				new DateTime(2004, 11, 8),
				new DateTime(2004, 12, 13),

				// http://www.rts.ru/a7751
				new DateTime(2005, 1, 3),
				new DateTime(2005, 1, 4),
				new DateTime(2005, 1, 5),
				new DateTime(2005, 1, 6),
				new DateTime(2005, 1, 7),
				new DateTime(2005, 1, 10),
				new DateTime(2005, 2, 23),
				new DateTime(2005, 3, 7),
				new DateTime(2005, 3, 8),
				new DateTime(2005, 5, 2),
				new DateTime(2005, 5, 9),
				new DateTime(2005, 5, 10),
				new DateTime(2005, 6, 13),
				new DateTime(2005, 11, 4),

				// http://www.rts.ru/a743
				new DateTime(2006, 1, 2),
				new DateTime(2006, 1, 3),
				new DateTime(2006, 1, 4),
				new DateTime(2006, 1, 5),
				new DateTime(2006, 1, 6),
				new DateTime(2006, 1, 9),
				new DateTime(2006, 2, 23),
				new DateTime(2006, 2, 24),
				new DateTime(2006, 3, 8),
				new DateTime(2006, 5, 1),
				new DateTime(2006, 5, 8),
				new DateTime(2006, 5, 9),
				new DateTime(2006, 6, 12),
				new DateTime(2006, 11, 6),

				// http://www.rts.ru/a13059
				new DateTime(2007, 1, 1),
				new DateTime(2007, 1, 2),
				new DateTime(2007, 1, 3),
				new DateTime(2007, 1, 4),
				new DateTime(2007, 1, 5),
				new DateTime(2007, 1, 8),
				new DateTime(2007, 2, 23),
				new DateTime(2007, 3, 8),
				new DateTime(2007, 4, 30),
				new DateTime(2007, 5, 1),
				new DateTime(2007, 5, 9),
				new DateTime(2007, 6, 11),
				new DateTime(2007, 6, 12),
				new DateTime(2007, 11, 5),
				new DateTime(2007, 12, 31),

				// http://www.rts.ru/a15065
				new DateTime(2008, 1, 1),
				new DateTime(2008, 1, 2),
				new DateTime(2008, 1, 3),
				new DateTime(2008, 1, 4),
				new DateTime(2008, 1, 7),
				new DateTime(2008, 1, 8),
				new DateTime(2008, 2, 25),
				new DateTime(2008, 3, 10),
				new DateTime(2008, 5, 1),
				new DateTime(2008, 5, 2),
				new DateTime(2008, 6, 12),
				new DateTime(2008, 6, 13),
				new DateTime(2008, 11, 3),
				new DateTime(2008, 11, 4),

				// http://www.rts.ru/a17902
				new DateTime(2009, 1, 1),
				new DateTime(2009, 1, 2),
				new DateTime(2009, 1, 5),
				new DateTime(2009, 1, 6),
				new DateTime(2009, 1, 7),
				new DateTime(2009, 1, 8),
				new DateTime(2009, 1, 9),
				new DateTime(2009, 2, 23),
				new DateTime(2009, 3, 9),
				new DateTime(2009, 5, 1),
				new DateTime(2009, 5, 11),
				new DateTime(2009, 6, 12),
				new DateTime(2009, 11, 4),

				// http://www.rts.ru/a19524
				new DateTime(2010, 1, 1),
				new DateTime(2010, 1, 4),
				new DateTime(2010, 1, 5),
				new DateTime(2010, 1, 6),
				new DateTime(2010, 1, 7),
				new DateTime(2010, 1, 8),
				new DateTime(2010, 2, 22),
				new DateTime(2010, 2, 23),
				new DateTime(2010, 3, 8),
				new DateTime(2010, 5, 3),
				new DateTime(2010, 5, 10),
				new DateTime(2010, 6, 14),
				new DateTime(2010, 11, 4),
				new DateTime(2010, 11, 5),

				// http://www.rts.ru/s355
				new DateTime(2011, 1, 3),
				new DateTime(2011, 1, 4),
				new DateTime(2011, 1, 5),
				new DateTime(2011, 1, 6),
				new DateTime(2011, 1, 7),
				new DateTime(2011, 1, 10),
				new DateTime(2011, 2, 23),
				new DateTime(2011, 3, 7),
				new DateTime(2011, 3, 8),
				new DateTime(2011, 5, 2),
				new DateTime(2011, 5, 9),
				new DateTime(2011, 6, 13),
				new DateTime(2011, 11, 4),

				// http://rts.micex.ru/a254
				new DateTime(2012, 1, 2),
				new DateTime(2012, 2, 23),
				new DateTime(2012, 3, 8),
				new DateTime(2012, 3, 9),
				new DateTime(2012, 4, 30),
				new DateTime(2012, 5, 1),
				new DateTime(2012, 5, 9),
				new DateTime(2012, 6, 11),
				new DateTime(2012, 6, 12),
				new DateTime(2012, 11, 5),
				new DateTime(2012, 12, 31),

				// http://rts.micex.ru/s690
				new DateTime(2013, 1, 1),
				new DateTime(2013, 1, 2),
				new DateTime(2013, 1, 3),
				new DateTime(2013, 1, 4),
				new DateTime(2013, 1, 7),
				new DateTime(2013, 3, 8),
				new DateTime(2013, 5, 1),
				new DateTime(2013, 5, 9),
				new DateTime(2013, 5, 10),
				new DateTime(2013, 6, 12)
			};

			//russianSpecialHolidays =
			//	russianSpecialHolidays
			//		.Concat(GetDefaultRussianHolidays(new DateTime(2001, 01, 01), new DateTime(2010, 01, 01)))
			//		.ToArray();

			Forts = new ExchangeBoard
			{
				Code = "FORTS",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("10:00:00".To<TimeSpan>(), "14:00:00".To<TimeSpan>()),
								new Range<TimeSpan>("14:03:00".To<TimeSpan>(), "18:45:00".To<TimeSpan>()),
								new Range<TimeSpan>("19:00:00".To<TimeSpan>(), "23:50:00".To<TimeSpan>())
							},
						}
					},
					SpecialWorkingDays = ArrayHelper.Clone(russianSpecialWorkingDays),
					SpecialHolidays = ArrayHelper.Clone(russianSpecialHolidays),
				},
				ExpiryTime = new TimeSpan(18, 45, 00),
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			var micexWorkingTime = new WorkingTime
			{
				Periods = new[]
				{
					new WorkingTimePeriod
					{
						Till = DateTime.MaxValue,
						Times = new[]
						{
							new Range<TimeSpan>("10:00:00".To<TimeSpan>(), "18:45:00".To<TimeSpan>())
						},
					}
				},
				SpecialWorkingDays = ArrayHelper.Clone(russianSpecialWorkingDays),
				SpecialHolidays = ArrayHelper.Clone(russianSpecialHolidays),
			};

			Micex = new ExchangeBoard
			{
				Code = "MICEX",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexAuct = new ExchangeBoard
			{
				Code = "AUCT",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexAubb = new ExchangeBoard
			{
				Code = "AUBB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexCasf = new ExchangeBoard
			{
				Code = "CASF",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexEqbr = new ExchangeBoard
			{
				Code = "EQBR",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexEqbs = new ExchangeBoard
			{
				Code = "EQBS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexEqdp = new ExchangeBoard
			{
				Code = "EQDP",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexEqeu = new ExchangeBoard
			{
				Code = "EQEU",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexEqus = new ExchangeBoard
			{
				Code = "EQUS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexEqnb = new ExchangeBoard
			{
				Code = "EQNB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexEqne = new ExchangeBoard
			{
				Code = "EQNE",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexEqnl = new ExchangeBoard
			{
				Code = "EQNL",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexEqno = new ExchangeBoard
			{
				Code = "EQNO",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexEqob = new ExchangeBoard
			{
				Code = "EQOB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexEqos = new ExchangeBoard
			{
				Code = "EQOS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexEqov = new ExchangeBoard
			{
				Code = "EQOV",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexEqlv = new ExchangeBoard
			{
				Code = "EQLV",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexEqdb = new ExchangeBoard
			{
				Code = "EQDB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexEqde = new ExchangeBoard
			{
				Code = "EQDE",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexEqli = new ExchangeBoard
			{
				Code = "EQLI",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexEqqi = new ExchangeBoard
			{
				Code = "EQQI",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexSmal = new ExchangeBoard
			{
				Code = "SMAL",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexSpob = new ExchangeBoard
			{
				Code = "SPOB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexTqbr = new ExchangeBoard
			{
				Code = "TQBR",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexTqde = new ExchangeBoard
			{
				Code = "TQDE",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexTqbs = new ExchangeBoard
			{
				Code = "TQBS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexTqeu = new ExchangeBoard
			{
				Code = "TQEU",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexTqus = new ExchangeBoard
			{
				Code = "TQUS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexTqnb = new ExchangeBoard
			{
				Code = "TQNB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexTqne = new ExchangeBoard
			{
				Code = "TQNE",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexTqnl = new ExchangeBoard
			{
				Code = "TQNL",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexTqno = new ExchangeBoard
			{
				Code = "TQNO",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexTqob = new ExchangeBoard
			{
				Code = "TQOB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexTqos = new ExchangeBoard
			{
				Code = "TQOS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexTqov = new ExchangeBoard
			{
				Code = "TQOV",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexTqlv = new ExchangeBoard
			{
				Code = "TQLV",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexTqli = new ExchangeBoard
			{
				Code = "TQLI",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexTqqi = new ExchangeBoard
			{
				Code = "TQQI",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexEqrp = new ExchangeBoard
			{
				Code = "EQRP",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPsrp = new ExchangeBoard
			{
				Code = "PSRP",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexRfnd = new ExchangeBoard
			{
				Code = "RFND",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexTadm = new ExchangeBoard
			{
				Code = "TADM",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexNadm = new ExchangeBoard
			{
				Code = "NADM",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			//MicexTran = new ExchangeBoard
			//{
			//	Code = "TRAN",
			//	WorkingTime = micexWorkingTime.Clone(),
			//	IsSupportMarketOrders = true,
			//	Exchange = Exchange.Moex,
			//};

			MicexPsau = new ExchangeBoard
			{
				Code = "PSAU",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPaus = new ExchangeBoard
			{
				Code = "PAUS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPsbb = new ExchangeBoard
			{
				Code = "PSBB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPseq = new ExchangeBoard
			{
				Code = "PSEQ",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPses = new ExchangeBoard
			{
				Code = "PSES",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPseu = new ExchangeBoard
			{
				Code = "PSEU",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPsdb = new ExchangeBoard
			{
				Code = "PSDB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPsde = new ExchangeBoard
			{
				Code = "PSDE",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPsus = new ExchangeBoard
			{
				Code = "PSUS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPsnb = new ExchangeBoard
			{
				Code = "PSNB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPsne = new ExchangeBoard
			{
				Code = "PSNE",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPsnl = new ExchangeBoard
			{
				Code = "PSNL",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPsno = new ExchangeBoard
			{
				Code = "PSNO",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPsob = new ExchangeBoard
			{
				Code = "PSOB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPsos = new ExchangeBoard
			{
				Code = "PSOS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPsov = new ExchangeBoard
			{
				Code = "PSOV",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPslv = new ExchangeBoard
			{
				Code = "PSLV",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPsli = new ExchangeBoard
			{
				Code = "PSLI",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPsqi = new ExchangeBoard
			{
				Code = "PSQI",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexRpeu = new ExchangeBoard
			{
				Code = "RPEU",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexRpma = new ExchangeBoard
			{
				Code = "RPMA",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexRpmo = new ExchangeBoard
			{
				Code = "RPMO",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexRpua = new ExchangeBoard
			{
				Code = "RPUA",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexRpuo = new ExchangeBoard
			{
				Code = "RPUO",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexRpuq = new ExchangeBoard
			{
				Code = "RPUQ",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexFbcb = new ExchangeBoard
			{
				Code = "FBCB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexFbfx = new ExchangeBoard
			{
				Code = "FBFX",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexIrk2 = new ExchangeBoard
			{
				Code = "IRK2",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexRpqi = new ExchangeBoard
			{
				Code = "RPQI",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPteq = new ExchangeBoard
			{
				Code = "PTEQ",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPtes = new ExchangeBoard
			{
				Code = "PTES",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPteu = new ExchangeBoard
			{
				Code = "PTEU",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPtus = new ExchangeBoard
			{
				Code = "PTUS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPtnb = new ExchangeBoard
			{
				Code = "PTNB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPtne = new ExchangeBoard
			{
				Code = "PTNE",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPtnl = new ExchangeBoard
			{
				Code = "PTNL",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPtno = new ExchangeBoard
			{
				Code = "PTNO",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPtob = new ExchangeBoard
			{
				Code = "PTOB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPtos = new ExchangeBoard
			{
				Code = "PTOS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPtov = new ExchangeBoard
			{
				Code = "PTOV",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPtlv = new ExchangeBoard
			{
				Code = "PTLV",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPtli = new ExchangeBoard
			{
				Code = "PTLI",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexPtqi = new ExchangeBoard
			{
				Code = "PTQI",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexScvc = new ExchangeBoard
			{
				Code = "SCVC",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexRpng = new ExchangeBoard
			{
				Code = "RPNG",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexRpfg = new ExchangeBoard
			{
				Code = "RPFG",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexCbcr = new ExchangeBoard
			{
				Code = "CBCR",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexCred = new ExchangeBoard
			{
				Code = "CRED",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexDepz = new ExchangeBoard
			{
				Code = "DEPZ",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexDpvb = new ExchangeBoard
			{
				Code = "DPVB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexDpfk = new ExchangeBoard
			{
				Code = "DPFK",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexDpfo = new ExchangeBoard
			{
				Code = "DPFO",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexDppf = new ExchangeBoard
			{
				Code = "DPPF",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexCets = new ExchangeBoard
			{
				Code = "CETS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexAets = new ExchangeBoard
			{
				Code = "AETS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexCngd = new ExchangeBoard
			{
				Code = "CNGD",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexTran = new ExchangeBoard
			{
				Code = "TRAN",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexJunior = new ExchangeBoard
			{
				Code = "QJSIM",
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			Ux = new ExchangeBoard
			{
				Code = "UX",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("10:30:00".To<TimeSpan>(), "13:00:00".To<TimeSpan>()),
								new Range<TimeSpan>("13:03:00".To<TimeSpan>(), "17:30:00".To<TimeSpan>())
							},
						}
					},
				},
				ExpiryTime = new TimeSpan(18, 45, 00),
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Ux
			};

			UxStock = new ExchangeBoard
			{
				Code = "GTS",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
					Times = new[]
					{
						new Range<TimeSpan>("10:30:00".To<TimeSpan>(), "17:30:00".To<TimeSpan>())
					},
						}
					},
				},
				Exchange = Exchange.Ux
			};

			Amex = new ExchangeBoard
			{
				Code = "AMEX",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
					Times = new[]
					{
						new Range<TimeSpan>("09:30:00".To<TimeSpan>(), "16:00:00".To<TimeSpan>())
					},
						}
					},
				},
				IsSupportMarketOrders = true,
				Exchange = Exchange.Amex
			};

			Cme = new ExchangeBoard
			{
				Code = "CME",
				Exchange = Exchange.Cme,
			};

			Cbot = new ExchangeBoard
			{
				Code = "CBOT",
				Exchange = Exchange.Cbot,
			};

			Cce = new ExchangeBoard
			{
				Code = "CCE",
				Exchange = Exchange.Cce,
			};

			Nyse = new ExchangeBoard
			{
				Code = "NYSE",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("09:30:00".To<TimeSpan>(), "16:00:00".To<TimeSpan>())
							},
						}
					},
				},
				IsSupportMarketOrders = true,
				Exchange = Exchange.Nyse
			};

			Nymex = new ExchangeBoard
			{
				Code = "NYMEX",
				Exchange = Exchange.Nymex,
			};

			Nasdaq = new ExchangeBoard
			{
				Code = "NASDAQ",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("09:30:00".To<TimeSpan>(), "16:00:00".To<TimeSpan>())
							},
						}
					},
				},
				IsSupportMarketOrders = true,
				Exchange = Exchange.Nasdaq,
			};

			Nqlx = new ExchangeBoard
			{
				Code = "NQLX",
				Exchange = Exchange.Nqlx,
			};

			Tsx = new ExchangeBoard
			{
				Code = "TSX",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("09:30:00".To<TimeSpan>(), "16:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Tsx,
			};

			Lse = new ExchangeBoard
			{
				Code = "LSE",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("08:00:00".To<TimeSpan>(), "16:30:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Lse,
			};

			Tse = new ExchangeBoard
			{
				Code = "TSE",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("09:00:00".To<TimeSpan>(), "11:30:00".To<TimeSpan>()),
								new Range<TimeSpan>("12:30:00".To<TimeSpan>(), "15:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Tse,
			};

			Hkex = new ExchangeBoard
			{
				Code = "HKEX",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("09:20:00".To<TimeSpan>(), "12:00:00".To<TimeSpan>()),
								new Range<TimeSpan>("13:00:00".To<TimeSpan>(), "16:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Hkex,
			};

			Hkfe = new ExchangeBoard
			{
				Code = "HKFE",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("09:15:00".To<TimeSpan>(), "12:00:00".To<TimeSpan>()),
								new Range<TimeSpan>("13:00:00".To<TimeSpan>(), "16:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Hkfe,
			};

			Sse = new ExchangeBoard
			{
				Code = "SSE",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("09:30:00".To<TimeSpan>(), "11:30:00".To<TimeSpan>()),
								new Range<TimeSpan>("13:00:00".To<TimeSpan>(), "15:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Sse,
			};

			Szse = new ExchangeBoard
			{
				Code = "SZSE",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("09:30:00".To<TimeSpan>(), "11:30:00".To<TimeSpan>()),
								new Range<TimeSpan>("13:00:00".To<TimeSpan>(), "15:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Szse
			};

			Tsec = new ExchangeBoard
			{
				Code = "TSEC",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("09:00:00".To<TimeSpan>(), "13:30:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Tsec,
			};

			Sgx = new ExchangeBoard
			{
				Code = "SGX",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("09:00:00".To<TimeSpan>(), "17:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Sgx,
			};

			Pse = new ExchangeBoard
			{
				Code = "PSE",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("09:30:00".To<TimeSpan>(), "12:00:00".To<TimeSpan>()),
								new Range<TimeSpan>("13:30:00".To<TimeSpan>(), "15:30:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Pse,
			};

			Klse = new ExchangeBoard
			{
				Code = "KLSE",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("09:00:00".To<TimeSpan>(), "12:30:00".To<TimeSpan>()),
								new Range<TimeSpan>("14:00:00".To<TimeSpan>(), "17:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Klse,
			};

			Idx = new ExchangeBoard
			{
				Code = "IDX",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("09:30:00".To<TimeSpan>(), "16:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Idx,
			};

			Set = new ExchangeBoard
			{
				Code = "SET",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("10:00:00".To<TimeSpan>(), "12:30:00".To<TimeSpan>()),
								new Range<TimeSpan>("14:30:00".To<TimeSpan>(), "16:30:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Set,
			};

			Bse = new ExchangeBoard
			{
				Code = "BSE",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("09:15:00".To<TimeSpan>(), "15:30:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Bse,
			};

			Nse = new ExchangeBoard
			{
				Code = "NSE",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("09:15:00".To<TimeSpan>(), "15:30:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Nse,
			};

			Cse = new ExchangeBoard
			{
				Code = "CSE",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("09:30:00".To<TimeSpan>(), "14:30:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Cse,
			};

			Krx = new ExchangeBoard
			{
				Code = "KRX",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("09:00:00".To<TimeSpan>(), "15:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Krx,
			};

			Asx = new ExchangeBoard
			{
				Code = "ASX",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("09:50:00".To<TimeSpan>(), "16:12:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Asx,
			};

			Nzx = new ExchangeBoard
			{
				Code = "NZX",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("10:00:00".To<TimeSpan>(), "17:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Nzx,
			};

			Tase = new ExchangeBoard
			{
				Code = "TASE",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("09:00:00".To<TimeSpan>(), "16:25:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Tase,
			};

			Fwb = new ExchangeBoard
			{
				Code = "FWB",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("08:00:00".To<TimeSpan>(), "22:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Fwb,
			};

			Mse = new ExchangeBoard
			{
				Code = "MSE",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("9:00:00".To<TimeSpan>(), "17:30:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Mse,
			};

			Swx = new ExchangeBoard
			{
				Code = "SWX",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("9:00:00".To<TimeSpan>(), "17:30:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Swx,
			};

			Jse = new ExchangeBoard
			{
				Code = "JSE",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("9:00:00".To<TimeSpan>(), "17:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Jse,
			};

			Lmax = new ExchangeBoard
			{
				Code = "LMAX",
				WorkingTime = new WorkingTime
				{
					Periods = new[]
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new[]
							{
								new Range<TimeSpan>("9:00:00".To<TimeSpan>(), "17:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Lmax,
			};

			DukasCopy = new ExchangeBoard
			{
				Code = "DUKAS",
				Exchange = Exchange.DukasCopy,
			};

			GainCapital = new ExchangeBoard
			{
				Code = "GAIN",
				Exchange = Exchange.GainCapital,
			};

			MBTrading = new ExchangeBoard
			{
				Code = "MBT",
				Exchange = Exchange.MBTrading,
			};

			TrueFX = new ExchangeBoard
			{
				Code = "TRUEFX",
				Exchange = Exchange.TrueFX,
			};

			Integral = new ExchangeBoard
			{
				Code = "INTGRL",
				Exchange = Exchange.Integral,
			};

			Cfh = new ExchangeBoard
			{
				Code = "CFH",
				Exchange = Exchange.Cfh,
			};

			Ond = new ExchangeBoard
			{
				Code = "OND",
				Exchange = Exchange.Ond,
			};

			Smart = new ExchangeBoard
			{
				Code = "SMART",
				Exchange = Exchange.Nasdaq,
			};

			Btce = new ExchangeBoard
			{
				Code = Exchange.Btce.Name,
				Exchange = Exchange.Btce,
			};

			BitStamp = new ExchangeBoard
			{
				Code = Exchange.BitStamp.Name,
				Exchange = Exchange.BitStamp,
			};

			BtcChina = new ExchangeBoard
			{
				Code = Exchange.BtcChina.Name,
				Exchange = Exchange.BtcChina,
			};

			Icbit = new ExchangeBoard
			{
				Code = Exchange.Icbit.Name,
				Exchange = Exchange.Icbit,
			};
		}

		//private static IEnumerable<DateTime> GetDefaultRussianHolidays(DateTime startYear, DateTime endYear)
		//{
		//	if (startYear >= endYear)
		//		throw new ArgumentOutOfRangeException("endYear");

		//	var holidays = new List<DateTime>();

		//	for (var year = startYear.Year; year <= endYear.Year; year++)
		//	{
		//		for (var i = 1; i <= 10; i++)
		//			holidays.Add(new DateTime(year, 1, i));

		//		holidays.Add(new DateTime(year, 2, 23));
		//		holidays.Add(new DateTime(year, 3, 8));
		//		holidays.Add(new DateTime(year, 5, 1));
		//		holidays.Add(new DateTime(year, 5, 2));
		//		holidays.Add(new DateTime(year, 5, 9));
		//		holidays.Add(new DateTime(year, 6, 12));
		//		holidays.Add(new DateTime(year, 11, 4));
		//	}

		//	return holidays;
		//}

		/// <summary>
		/// Информация об объединенной площадке, не имеющей ограничений в расписание работы.
		/// </summary>
		public static ExchangeBoard Associated { get; private set; }

		/// <summary>
		/// Информация о тестовой площадке, не имеющая ограничения в расписание работы.
		/// </summary>
		public static ExchangeBoard Test { get; private set; }

		/// <summary>
		/// Информация о площадке FORTS биржи <see cref="BusinessEntities.Exchange.Moex"/>
		/// </summary>
		public static ExchangeBoard Forts { get; private set; }

		/// <summary>
		/// Информация о индексах биржи <see cref="BusinessEntities.Exchange.Moex"/>
		/// </summary>
		public static ExchangeBoard Micex { get; private set; }

		/// <summary>
		/// Информация о площадке AUCT биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexAuct { get; private set; }

		/// <summary>
		/// Информация о площадке AUBB биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexAubb { get; private set; }

		/// <summary>
		/// Информация о площадке CASF биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexCasf { get; private set; }

		/// <summary>
		/// Информация о площадке EQBR биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexEqbr { get; private set; }

		/// <summary>
		/// Информация о площадке EQBS биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexEqbs { get; private set; }

		/// <summary>
		/// Информация о площадке EQDP биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexEqdp { get; private set; }

		/// <summary>
		/// Информация о площадке EQEU биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexEqeu { get; private set; }

		/// <summary>
		/// Информация о площадке EQUS биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexEqus { get; private set; }

		/// <summary>
		/// Информация о площадке EQNB биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexEqnb { get; private set; }

		/// <summary>
		/// Информация о площадке EQNE биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexEqne { get; private set; }

		/// <summary>
		/// Информация о площадке EQNL биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexEqnl { get; private set; }

		/// <summary>
		/// Информация о площадке EQNO биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexEqno { get; private set; }

		/// <summary>
		/// Информация о площадке EQOB биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexEqob { get; private set; }

		/// <summary>
		/// Информация о площадке EQOS биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexEqos { get; private set; }

		/// <summary>
		/// Информация о площадке EQOV биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexEqov { get; private set; }

		/// <summary>
		/// Информация о площадке EQLV биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexEqlv { get; private set; }

		/// <summary>
		/// Информация о площадке EQDB биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexEqdb { get; private set; }

		/// <summary>
		/// Информация о площадке EQDE биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexEqde { get; private set; }

		/// <summary>
		/// Информация о площадке EQLI биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexEqli { get; private set; }

		/// <summary>
		/// Информация о площадке EQQI биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexEqqi { get; private set; }

		/// <summary>
		/// Информация о площадке SMAL биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexSmal { get; private set; }

		/// <summary>
		/// Информация о площадке SPOB биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexSpob { get; private set; }

		/// <summary>
		/// Информация о площадке TQBR биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexTqbr { get; private set; }

		/// <summary>
		/// Информация о площадке TQDE биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexTqde { get; private set; }

		/// <summary>
		/// Информация о площадке TQBS биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexTqbs { get; private set; }

		/// <summary>
		/// Информация о площадке TQEU биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexTqeu { get; private set; }

		/// <summary>
		/// Информация о площадке TQUS биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexTqus { get; private set; }

		/// <summary>
		/// Информация о площадке TQNB биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexTqnb { get; private set; }

		/// <summary>
		/// Информация о площадке TQNE биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexTqne { get; private set; }

		/// <summary>
		/// Информация о площадке TQNL биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexTqnl { get; private set; }

		/// <summary>
		/// Информация о площадке TQNO биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexTqno { get; private set; }

		/// <summary>
		/// Информация о площадке TQOB биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexTqob { get; private set; }

		/// <summary>
		/// Информация о площадке TQOS биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexTqos { get; private set; }

		/// <summary>
		/// Информация о площадке TQOV биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexTqov { get; private set; }

		/// <summary>
		/// Информация о площадке TQLV биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexTqlv { get; private set; }

		/// <summary>
		/// Информация о площадке TQLI биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexTqli { get; private set; }

		/// <summary>
		/// Информация о площадке TQQI биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexTqqi { get; private set; }

		/// <summary>
		/// Информация о площадке EQRP биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexEqrp { get; private set; }

		/// <summary>
		/// Информация о площадке PSRP биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPsrp { get; private set; }

		/// <summary>
		/// Информация о площадке RFND биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexRfnd { get; private set; }

		/// <summary>
		/// Информация о площадке TADM биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexTadm { get; private set; }

		/// <summary>
		/// Информация о площадке NADM биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexNadm { get; private set; }

		///// <summary>
		///// Информация о площадке TRAN биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		///// </summary>
		//public static ExchangeBoard MicexTran { get; private set; }

		/// <summary>
		/// Информация о площадке PSAU биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPsau { get; private set; }

		/// <summary>
		/// Информация о площадке PAUS биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPaus { get; private set; }

		/// <summary>
		/// Информация о площадке PSBB биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPsbb { get; private set; }

		/// <summary>
		/// Информация о площадке PSEQ биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPseq { get; private set; }

		/// <summary>
		/// Информация о площадке PSES биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPses { get; private set; }

		/// <summary>
		/// Информация о площадке PSEU биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPseu { get; private set; }

		/// <summary>
		/// Информация о площадке PSDB биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPsdb { get; private set; }

		/// <summary>
		/// Информация о площадке PSDE биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPsde { get; private set; }

		/// <summary>
		/// Информация о площадке PSUS биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPsus { get; private set; }

		/// <summary>
		/// Информация о площадке PSNB биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPsnb { get; private set; }

		/// <summary>
		/// Информация о площадке PSNE биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPsne { get; private set; }

		/// <summary>
		/// Информация о площадке PSNL биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPsnl { get; private set; }

		/// <summary>
		/// Информация о площадке PSNO биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPsno { get; private set; }

		/// <summary>
		/// Информация о площадке PSOB биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPsob { get; private set; }

		/// <summary>
		/// Информация о площадке PSOS биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPsos { get; private set; }

		/// <summary>
		/// Информация о площадке PSOV биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPsov { get; private set; }

		/// <summary>
		/// Информация о площадке PSLV биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPslv { get; private set; }

		/// <summary>
		/// Информация о площадке PSLI биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPsli { get; private set; }

		/// <summary>
		/// Информация о площадке PSQI биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPsqi { get; private set; }

		/// <summary>
		/// Информация о площадке RPEU биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexRpeu { get; private set; }

		/// <summary>
		/// Информация о площадке RPMA биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexRpma { get; private set; }

		/// <summary>
		/// Информация о площадке RPMO биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexRpmo { get; private set; }

		/// <summary>
		/// Информация о площадке RPUA биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexRpua { get; private set; }

		/// <summary>
		/// Информация о площадке RPUO биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexRpuo { get; private set; }

		/// <summary>
		/// Информация о площадке RPUQ биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexRpuq { get; private set; }

		/// <summary>
		/// Информация о площадке FBCB биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexFbcb { get; private set; }

		/// <summary>
		/// Информация о площадке FBFX биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexFbfx { get; private set; }

		/// <summary>
		/// Информация о площадке IRK2 биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexIrk2 { get; private set; }

		/// <summary>
		/// Информация о площадке RPQI биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexRpqi { get; private set; }

		/// <summary>
		/// Информация о площадке PTEQ биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPteq { get; private set; }

		/// <summary>
		/// Информация о площадке PTES биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPtes { get; private set; }

		/// <summary>
		/// Информация о площадке PTEU биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPteu { get; private set; }

		/// <summary>
		/// Информация о площадке PTUS биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPtus { get; private set; }

		/// <summary>
		/// Информация о площадке PTNB биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPtnb { get; private set; }

		/// <summary>
		/// Информация о площадке PTNE биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPtne { get; private set; }

		/// <summary>
		/// Информация о площадке PTNL биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPtnl { get; private set; }

		/// <summary>
		/// Информация о площадке PTNO биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPtno { get; private set; }

		/// <summary>
		/// Информация о площадке PTOB биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPtob { get; private set; }

		/// <summary>
		/// Информация о площадке PTOS биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPtos { get; private set; }

		/// <summary>
		/// Информация о площадке PTOV биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPtov { get; private set; }

		/// <summary>
		/// Информация о площадке PTLV биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPtlv { get; private set; }

		/// <summary>
		/// Информация о площадке PTLI биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPtli { get; private set; }

		/// <summary>
		/// Информация о площадке PTQI биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexPtqi { get; private set; }

		/// <summary>
		/// Информация о площадке SCVC биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexScvc { get; private set; }

		/// <summary>
		/// Информация о площадке RPNG биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexRpng { get; private set; }

		/// <summary>
		/// Информация о площадке RPFG биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexRpfg { get; private set; }

		/// <summary>
		/// Информация о площадке CDCR биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexCbcr { get; private set; }

		/// <summary>
		/// Информация о площадке CRED биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexCred { get; private set; }

		/// <summary>
		/// Информация о площадке DEPZ биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexDepz { get; private set; }

		/// <summary>
		/// Информация о площадке DPVB биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexDpvb { get; private set; }

		/// <summary>
		/// Информация о площадке DPFK биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexDpfk { get; private set; }

		/// <summary>
		/// Информация о площадке DPFO биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexDpfo { get; private set; }

		/// <summary>
		/// Информация о площадке DPPF биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexDppf { get; private set; }

		/// <summary>
		/// Информация о площадке CETS биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexCets { get; private set; }

		/// <summary>
		/// Информация о площадке AETS биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexAets { get; private set; }

		/// <summary>
		/// Информация о площадке CNGD биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexCngd { get; private set; }

		/// <summary>
		/// Информация о площадке TRAN биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexTran { get; private set; }

		/// <summary>
		/// Информация о площадке QJSIM биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		/// </summary>
		public static ExchangeBoard MicexJunior { get; private set; }

		/// <summary>
		/// Информация о площадке срочного рынка биржи <see cref="BusinessEntities.Exchange.Ux"/>.
		/// </summary>
		public static ExchangeBoard Ux { get; private set; }

		/// <summary>
		/// Информация о площадке фондового рынка биржи <see cref="BusinessEntities.Exchange.Ux"/>.
		/// </summary>
		public static ExchangeBoard UxStock { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Cme"/>.
		/// </summary>
		public static ExchangeBoard Cme { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Cce"/>.
		/// </summary>
		public static ExchangeBoard Cce { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Cbot"/>.
		/// </summary>
		public static ExchangeBoard Cbot { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Nymex"/>.
		/// </summary>
		public static ExchangeBoard Nymex { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Amex"/>.
		/// </summary>
		public static ExchangeBoard Amex { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Nyse"/>.
		/// </summary>
		public static ExchangeBoard Nyse { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Nasdaq"/>.
		/// </summary>
		public static ExchangeBoard Nasdaq { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Nqlx"/>.
		/// </summary>
		public static ExchangeBoard Nqlx { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Lse"/>.
		/// </summary>
		public static ExchangeBoard Lse { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Tse"/>.
		/// </summary>
		public static ExchangeBoard Tse { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Hkex"/>.
		/// </summary>
		public static ExchangeBoard Hkex { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Hkfe"/>.
		/// </summary>
		public static ExchangeBoard Hkfe { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Sse"/>.
		/// </summary>
		public static ExchangeBoard Sse { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Szse"/>.
		/// </summary>
		public static ExchangeBoard Szse { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Tsx"/>.
		/// </summary>
		public static ExchangeBoard Tsx { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Fwb"/>.
		/// </summary>
		public static ExchangeBoard Fwb { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Asx"/>.
		/// </summary>
		public static ExchangeBoard Asx { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Nzx"/>.
		/// </summary>
		public static ExchangeBoard Nzx { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Bse"/>.
		/// </summary>
		public static ExchangeBoard Bse { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Nse"/>.
		/// </summary>
		public static ExchangeBoard Nse { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Swx"/>.
		/// </summary>
		public static ExchangeBoard Swx { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Krx"/>.
		/// </summary>
		public static ExchangeBoard Krx { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Mse"/>.
		/// </summary>
		public static ExchangeBoard Mse { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Jse"/>.
		/// </summary>
		public static ExchangeBoard Jse { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Sgx"/>.
		/// </summary>
		public static ExchangeBoard Sgx { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Tsec"/>.
		/// </summary>
		public static ExchangeBoard Tsec { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Pse"/>.
		/// </summary>
		public static ExchangeBoard Pse { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Klse"/>.
		/// </summary>
		public static ExchangeBoard Klse { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Idx"/>.
		/// </summary>
		public static ExchangeBoard Idx { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Set"/>.
		/// </summary>
		public static ExchangeBoard Set { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Cse"/>.
		/// </summary>
		public static ExchangeBoard Cse { get; private set; }

		/// <summary>
		/// Информация о площадке биржи <see cref="BusinessEntities.Exchange.Tase"/>.
		/// </summary>
		public static ExchangeBoard Tase { get; private set; }

		/// <summary>
		/// Информация о площадке брокера <see cref="BusinessEntities.Exchange.Lmax"/>.
		/// </summary>
		public static ExchangeBoard Lmax { get; private set; }

		/// <summary>
		/// Информация о площадке брокера <see cref="BusinessEntities.Exchange.DukasCopy"/>.
		/// </summary>
		public static ExchangeBoard DukasCopy { get; private set; }

		/// <summary>
		/// Информация о площадке брокера <see cref="BusinessEntities.Exchange.GainCapital"/>.
		/// </summary>
		public static ExchangeBoard GainCapital { get; private set; }

		/// <summary>
		/// Информация о площадке брокера <see cref="BusinessEntities.Exchange.MBTrading"/>.
		/// </summary>
		public static ExchangeBoard MBTrading { get; private set; }

		/// <summary>
		/// Информация о площадке брокера <see cref="BusinessEntities.Exchange.TrueFX"/>.
		/// </summary>
		public static ExchangeBoard TrueFX { get; private set; }

		/// <summary>
		/// Информация о площадке <see cref="BusinessEntities.Exchange.Integral"/>.
		/// </summary>
		public static ExchangeBoard Integral { get; private set; }

		/// <summary>
		/// Информация о площадке <see cref="BusinessEntities.Exchange.Cfh"/>.
		/// </summary>
		public static ExchangeBoard Cfh { get; private set; }

		/// <summary>
		/// Информация о площадке <see cref="BusinessEntities.Exchange.Ond"/>.
		/// </summary>
		public static ExchangeBoard Ond { get; private set; }

		/// <summary>
		/// Информация о площадке <see cref="BusinessEntities.Exchange.Nasdaq"/>.
		/// </summary>
		public static ExchangeBoard Smart { get; private set; }

		/// <summary>
		/// Информация о площадке <see cref="BusinessEntities.Exchange.Btce"/>.
		/// </summary>
		public static ExchangeBoard Btce { get; private set; }

		/// <summary>
		/// Информация о площадке <see cref="BusinessEntities.Exchange.BitStamp"/>.
		/// </summary>
		public static ExchangeBoard BitStamp { get; private set; }

		/// <summary>
		/// Информация о площадке <see cref="BusinessEntities.Exchange.BtcChina"/>.
		/// </summary>
		public static ExchangeBoard BtcChina { get; private set; }

		/// <summary>
		/// Информация о площадке <see cref="BusinessEntities.Exchange.Icbit"/>.
		/// </summary>
		public static ExchangeBoard Icbit { get; private set; }

		/// <summary>
		/// Информация о виртуальной площадке Finam.
		/// </summary>
		public static ExchangeBoard Finam { get; private set; }

		/// <summary>
		/// Информация о виртуальной площадке Mfd.
		/// </summary>
		public static ExchangeBoard Mfd { get; private set; }

		private const BindingFlags _publicStatic = BindingFlags.Public | BindingFlags.Static;

		/// <summary>
		/// Получить список бирж.
		/// </summary>
		/// <returns>Биржи.</returns>
		public static IEnumerable<Exchange> EnumerateExchanges()
		{
			return typeof(Exchange).GetMembers<PropertyInfo>(_publicStatic, typeof(Exchange))
				.Select(prop => (Exchange)prop.GetValue(null, null));
		}

		/// <summary>
		/// Получить список площадок.
		/// </summary>
		/// <returns>Площадки.</returns>
		public static IEnumerable<ExchangeBoard> EnumerateExchangeBoards()
		{
			return typeof(ExchangeBoard).GetMembers<PropertyInfo>(_publicStatic, typeof(ExchangeBoard))
				.Select(prop => (ExchangeBoard)prop.GetValue(null, null));
		}

		/// <summary>
		/// Создать <see cref="ExchangeBoard"/>.
		/// </summary>
		public ExchangeBoard()
		{
			ExtensionInfo = new Dictionary<object, object>();
		}

		private string _code = string.Empty;

		/// <summary>
		/// Код площадки.
		/// </summary>
		[DataMember]
		[Identity]
		[DisplayNameLoc(LocalizedStrings.CodeKey)]
		[DescriptionLoc(LocalizedStrings.BoardCodeKey)]
		[MainCategory]
		public string Code
		{
			get { return _code; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (Code == value)
					return;

				_code = value;
				Notify("Code");
			}
		}

		private TimeSpan _expiryTime;

		/// <summary>
		/// Время экспирации инструментов.
		/// </summary>
		[TimeSpan]
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ExpiryDateKey)]
		[DescriptionLoc(LocalizedStrings.Str64Key)]
		[MainCategory]
		public TimeSpan ExpiryTime
		{
			get { return _expiryTime; }
			set
			{
				if (ExpiryTime == value)
					return;

				_expiryTime = value;
				Notify("ExpiryTime");
			}
		}

		/// <summary>
		/// Биржа, на которой расположена площадка.
		/// </summary>
		[RelationSingle(IdentityType = typeof(string))]
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.StockExchangeKey)]
		[DescriptionLoc(LocalizedStrings.Str479Key)]
		[MainCategory]
		public Exchange Exchange { get; set; }

		private bool _isSupportAtomicReRegister;

		/// <summary>
		/// Поддерживается ли перерегистрация заявок через <see cref="OrderReplaceMessage"/> в виде одной транзакции.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ReregisteringKey)]
		[DescriptionLoc(LocalizedStrings.Str60Key)]
		[MainCategory]
		public bool IsSupportAtomicReRegister
		{
			get { return _isSupportAtomicReRegister; }
			set
			{
				_isSupportAtomicReRegister = value;
				Notify("IsSupportAtomicReRegister");
			}
		}

		private bool _isSupportMarketOrders;

		/// <summary>
		/// Поддерживается ли рыночный тип заявок <see cref="OrderTypes.Market"/>.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.MarketOrdersKey)]
		[DescriptionLoc(LocalizedStrings.MarketOrdersSupportedKey)]
		[MainCategory]
		public bool IsSupportMarketOrders
		{
			get { return _isSupportMarketOrders; }
			set
			{
				_isSupportMarketOrders = value;
				Notify("IsSupportMarketOrders");
			}
		}

		private WorkingTime _workingTime = new WorkingTime();

		/// <summary>
		/// Время работы площадки.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.WorkingTimeKey)]
		[DescriptionLoc(LocalizedStrings.WorkingHoursKey)]
		[MainCategory]
		[InnerSchema]
		public WorkingTime WorkingTime
		{
			get { return _workingTime; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (WorkingTime == value)
					return;

				_workingTime = value;
				Notify("WorkingTime");
			}
		}

		///// <summary>
		///// Все площадки.
		///// </summary>
		//public static ExchangeBoard[] AllBoards
		//{
		//	get { return ExchangeInfoProvider.Boards; }
		//}

		/// <summary>
		/// Получить площадку по ее коду.
		/// </summary>
		/// <param name="code">Код площадки.</param>
		/// <returns>Найденная площадка. Если площадки с переданным названием не существует, то будет возвращено null.</returns>
		public static ExchangeBoard GetBoard(string code)
		{
			return code.CompareIgnoreCase("RTS") ? Forts : ExchangeInfoProvider.GetExchangeBoard(code);
		}

		/// <summary>
		/// Получить площадку по ее коду. Если площадки с переданным кодом не существует, то она будет создана.
		/// </summary>
		/// <param name="code">Код площадки.</param>
		/// <param name="createBoard">Обработчик, создающий площадку, если она не найдена. Если значение равно null, то площадка создается с инициализацией по-умолчанию.</param>
		/// <returns>Биржевая площадка.</returns>
		public static ExchangeBoard GetOrCreateBoard(string code, Func<string, ExchangeBoard> createBoard = null)
		{
			if (code.IsEmpty())
				throw new ArgumentNullException("code");

			if (code.CompareIgnoreCase("RTS"))
				return Forts;

			var board = ExchangeInfoProvider.GetExchangeBoard(code);

			if (board != null)
				return board;

			if (createBoard == null)
			{
				var exchange = ExchangeInfoProvider.GetExchange(code);

				if (exchange == null)
				{
					exchange = new Exchange { Name = code };
					ExchangeInfoProvider.Save(exchange);
				}

				board = new ExchangeBoard
				{
					Code = code,
					Exchange = exchange
				};
			}
			else
			{
				board = createBoard(code);

				if (ExchangeInfoProvider.GetExchange(board.Exchange.Name) == null)
					ExchangeInfoProvider.Save(board.Exchange);
			}

			SaveBoard(board);

			return board;
		}

		/// <summary>
		/// Сохранить площадку.
		/// </summary>
		/// <param name="board">Площадка.</param>
		public static void SaveBoard(ExchangeBoard board)
		{
			if (board == null)
				throw new ArgumentNullException("board");

			ExchangeInfoProvider.Save(board);
		}

		[field: NonSerialized]
		private IDictionary<object, object> _extensionInfo;

		/// <summary>
		/// Расширенная информация по бирже.
		/// </summary>
		/// <remarks>
		/// Необходима в случае хранения в программе дополнительной информации, ассоциированной с биржей.
		/// </remarks>
		[XmlIgnore]
		[Browsable(false)]
		[DataMember]
		public IDictionary<object, object> ExtensionInfo
		{
			get { return _extensionInfo; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_extensionInfo = value;
				Notify("ExtensionInfo");
			}
		}

		[OnDeserialized]
		private void AfterDeserialization(StreamingContext ctx)
		{
			if (ExtensionInfo == null)
				ExtensionInfo = new Dictionary<object, object>();
		}

		[field: NonSerialized]
		private PropertyChangedEventHandler _propertyChanged;

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add { _propertyChanged += value; }
			remove { _propertyChanged -= value; }
		}

		private void Notify(string info)
		{
			_propertyChanged.SafeInvoke(this, info);
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return "{0} ({1})".Put(Code, Exchange);
		}

		/// <summary>
		/// Сравнить два объекта <see cref="ExchangeBoard" /> на эквивалентность.
		/// </summary>
		/// <param name="other">Объект для сравнения.</param>
		/// <returns><see langword="true"/>, если другой объект равен текущему, иначе, <see langword="false"/>.</returns>
		protected override bool OnEquals(ExchangeBoard other)
		{
			return Code == other.Code && Exchange == other.Exchange;
		}

		private int _hashCode;

		/// <summary>
		/// Рассчитать хеш-код объекта <see cref="ExchangeBoard"/>.
		/// </summary>
		/// <returns>Хеш-код.</returns>
		public override int GetHashCode()
		{
			if (_hashCode == 0)
				_hashCode = Code.GetHashCode() ^ (Exchange == null ? 0 : Exchange.GetHashCode());

			return _hashCode;
		}

		/// <summary>
		/// Создать копию объекта <see cref="ExchangeBoard" />.
		/// </summary>
		/// <returns>Копия.</returns>
		public override ExchangeBoard Clone()
		{
			return new ExchangeBoard
			{
				Exchange = Exchange,
				Code = Code,
				IsSupportAtomicReRegister = IsSupportAtomicReRegister,
				IsSupportMarketOrders = IsSupportMarketOrders,
				ExpiryTime = ExpiryTime,
				WorkingTime = WorkingTime.Clone(),
			};
		}

		/// <summary>
		/// Является ли площадка ММВБ площадкой.
		/// </summary>
		public bool IsMicex
		{
			get { return Exchange == Exchange.Moex && this != Forts; }
		}

		/// <summary>
		/// Является ли площадка площадкой фондового рынка биржи UX.
		/// </summary>
		public bool IsUxStock
		{
			get { return Exchange == Exchange.Ux && this != Ux; }
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Load(SettingsStorage storage)
		{
			Exchange = storage.GetValue<SettingsStorage>("Exchange").Load<Exchange>();
			Code = storage.GetValue<string>("Code");
			IsSupportMarketOrders = storage.GetValue<bool>("IsSupportMarketOrders");
			IsSupportAtomicReRegister = storage.GetValue<bool>("IsSupportAtomicReRegister");
			ExpiryTime = storage.GetValue<TimeSpan>("ExpiryTime");
			WorkingTime = storage.GetValue<SettingsStorage>("WorkingTime").Load<WorkingTime>();
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("Exchange", Exchange.Save());
			storage.SetValue("Code", Code);
			storage.SetValue("IsSupportMarketOrders", IsSupportMarketOrders);
			storage.SetValue("IsSupportAtomicReRegister", IsSupportAtomicReRegister);
			storage.SetValue("ExpiryTime", ExpiryTime);
			storage.SetValue("WorkingTime", WorkingTime.Save());
		}
	}
}
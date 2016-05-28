#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: ExchangeBoard_Instances.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Messages;

	partial class ExchangeBoard
	{
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

			var moscowTime = TimeZoneInfo.FromSerializedString("Russian Standard Time;180;(UTC+03:00) Moscow, St. Petersburg, Volgograd (RTZ 2);Russia TZ 2 Standard Time;Russia TZ 2 Daylight Time;[01:01:0001;12:31:2010;60;[0;02:00:00;3;5;0;];[0;03:00:00;10;5;0;];][01:01:2011;12:31:2011;60;[0;02:00:00;3;5;0;];[0;00:00:00;1;1;6;];][01:01:2014;12:31:2014;60;[0;00:00:00;1;1;3;];[0;02:00:00;10;5;0;];];");

			//russianSpecialHolidays =
			//	russianSpecialHolidays
			//		.Concat(GetDefaultRussianHolidays(new DateTime(2001, 01, 01), new DateTime(2010, 01, 01)))
			//		.ToArray();

			Forts = new ExchangeBoard
			{
				Code = "FORTS",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("10:00:00".To<TimeSpan>(), "14:00:00".To<TimeSpan>()),
								new Range<TimeSpan>("14:03:00".To<TimeSpan>(), "18:45:00".To<TimeSpan>()),
								new Range<TimeSpan>("19:00:00".To<TimeSpan>(), "23:50:00".To<TimeSpan>())
							},
						}
					},
					SpecialWorkingDays = new List<DateTime>(russianSpecialWorkingDays),
					SpecialHolidays = new List<DateTime>(russianSpecialHolidays),
				},
				ExpiryTime = new TimeSpan(18, 45, 00),
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			var micexWorkingTime = new WorkingTime
			{
				Periods = new List<WorkingTimePeriod>
				{
					new WorkingTimePeriod
					{
						Till = DateTime.MaxValue,
						Times = new List<Range<TimeSpan>>
						{
							new Range<TimeSpan>("10:00:00".To<TimeSpan>(), "18:45:00".To<TimeSpan>())
						},
					}
				},
				SpecialWorkingDays = new List<DateTime>(russianSpecialWorkingDays),
				SpecialHolidays = new List<DateTime>(russianSpecialHolidays),
			};

			Micex = new ExchangeBoard
			{
				Code = "MICEX",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexAuct = new ExchangeBoard
			{
				Code = "AUCT",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexAubb = new ExchangeBoard
			{
				Code = "AUBB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexCasf = new ExchangeBoard
			{
				Code = "CASF",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqbr = new ExchangeBoard
			{
				Code = "EQBR",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqbs = new ExchangeBoard
			{
				Code = "EQBS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqdp = new ExchangeBoard
			{
				Code = "EQDP",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqeu = new ExchangeBoard
			{
				Code = "EQEU",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqus = new ExchangeBoard
			{
				Code = "EQUS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqnb = new ExchangeBoard
			{
				Code = "EQNB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqne = new ExchangeBoard
			{
				Code = "EQNE",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqnl = new ExchangeBoard
			{
				Code = "EQNL",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqno = new ExchangeBoard
			{
				Code = "EQNO",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqob = new ExchangeBoard
			{
				Code = "EQOB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqos = new ExchangeBoard
			{
				Code = "EQOS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqov = new ExchangeBoard
			{
				Code = "EQOV",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqlv = new ExchangeBoard
			{
				Code = "EQLV",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqdb = new ExchangeBoard
			{
				Code = "EQDB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqde = new ExchangeBoard
			{
				Code = "EQDE",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqli = new ExchangeBoard
			{
				Code = "EQLI",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqqi = new ExchangeBoard
			{
				Code = "EQQI",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexSmal = new ExchangeBoard
			{
				Code = "SMAL",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexSpob = new ExchangeBoard
			{
				Code = "SPOB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqbr = new ExchangeBoard
			{
				Code = "TQBR",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqde = new ExchangeBoard
			{
				Code = "TQDE",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqbs = new ExchangeBoard
			{
				Code = "TQBS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqeu = new ExchangeBoard
			{
				Code = "TQEU",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqus = new ExchangeBoard
			{
				Code = "TQUS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqnb = new ExchangeBoard
			{
				Code = "TQNB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqne = new ExchangeBoard
			{
				Code = "TQNE",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqnl = new ExchangeBoard
			{
				Code = "TQNL",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqno = new ExchangeBoard
			{
				Code = "TQNO",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqob = new ExchangeBoard
			{
				Code = "TQOB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqos = new ExchangeBoard
			{
				Code = "TQOS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqov = new ExchangeBoard
			{
				Code = "TQOV",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqlv = new ExchangeBoard
			{
				Code = "TQLV",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqli = new ExchangeBoard
			{
				Code = "TQLI",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqqi = new ExchangeBoard
			{
				Code = "TQQI",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqrp = new ExchangeBoard
			{
				Code = "EQRP",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsrp = new ExchangeBoard
			{
				Code = "PSRP",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexRfnd = new ExchangeBoard
			{
				Code = "RFND",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTadm = new ExchangeBoard
			{
				Code = "TADM",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexNadm = new ExchangeBoard
			{
				Code = "NADM",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			//MicexTran = new ExchangeBoard
			//{
			//	Code = "TRAN",
			//	WorkingTime = micexWorkingTime.Clone(),
			//	IsSupportMarketOrders = true,
			//	Exchange = Exchange.Moex,
			//	TimeZone = moscowTime,
			//};

			MicexPsau = new ExchangeBoard
			{
				Code = "PSAU",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPaus = new ExchangeBoard
			{
				Code = "PAUS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsbb = new ExchangeBoard
			{
				Code = "PSBB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPseq = new ExchangeBoard
			{
				Code = "PSEQ",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPses = new ExchangeBoard
			{
				Code = "PSES",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPseu = new ExchangeBoard
			{
				Code = "PSEU",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsdb = new ExchangeBoard
			{
				Code = "PSDB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsde = new ExchangeBoard
			{
				Code = "PSDE",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsus = new ExchangeBoard
			{
				Code = "PSUS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsnb = new ExchangeBoard
			{
				Code = "PSNB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsne = new ExchangeBoard
			{
				Code = "PSNE",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsnl = new ExchangeBoard
			{
				Code = "PSNL",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsno = new ExchangeBoard
			{
				Code = "PSNO",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsob = new ExchangeBoard
			{
				Code = "PSOB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsos = new ExchangeBoard
			{
				Code = "PSOS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsov = new ExchangeBoard
			{
				Code = "PSOV",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPslv = new ExchangeBoard
			{
				Code = "PSLV",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsli = new ExchangeBoard
			{
				Code = "PSLI",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsqi = new ExchangeBoard
			{
				Code = "PSQI",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexRpeu = new ExchangeBoard
			{
				Code = "RPEU",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexRpma = new ExchangeBoard
			{
				Code = "RPMA",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexRpmo = new ExchangeBoard
			{
				Code = "RPMO",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexRpua = new ExchangeBoard
			{
				Code = "RPUA",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexRpuo = new ExchangeBoard
			{
				Code = "RPUO",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexRpuq = new ExchangeBoard
			{
				Code = "RPUQ",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexFbcb = new ExchangeBoard
			{
				Code = "FBCB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexFbfx = new ExchangeBoard
			{
				Code = "FBFX",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexIrk2 = new ExchangeBoard
			{
				Code = "IRK2",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexRpqi = new ExchangeBoard
			{
				Code = "RPQI",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPteq = new ExchangeBoard
			{
				Code = "PTEQ",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtes = new ExchangeBoard
			{
				Code = "PTES",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPteu = new ExchangeBoard
			{
				Code = "PTEU",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtus = new ExchangeBoard
			{
				Code = "PTUS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtnb = new ExchangeBoard
			{
				Code = "PTNB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtne = new ExchangeBoard
			{
				Code = "PTNE",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtnl = new ExchangeBoard
			{
				Code = "PTNL",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtno = new ExchangeBoard
			{
				Code = "PTNO",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtob = new ExchangeBoard
			{
				Code = "PTOB",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtos = new ExchangeBoard
			{
				Code = "PTOS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtov = new ExchangeBoard
			{
				Code = "PTOV",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtlv = new ExchangeBoard
			{
				Code = "PTLV",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtli = new ExchangeBoard
			{
				Code = "PTLI",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtqi = new ExchangeBoard
			{
				Code = "PTQI",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexScvc = new ExchangeBoard
			{
				Code = "SCVC",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexRpng = new ExchangeBoard
			{
				Code = "RPNG",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexRpfg = new ExchangeBoard
			{
				Code = "RPFG",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexCbcr = new ExchangeBoard
			{
				Code = "CBCR",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexCred = new ExchangeBoard
			{
				Code = "CRED",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
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
				TimeZone = moscowTime,
			};

			MicexDpfk = new ExchangeBoard
			{
				Code = "DPFK",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexDpfo = new ExchangeBoard
			{
				Code = "DPFO",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexDppf = new ExchangeBoard
			{
				Code = "DPPF",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexCets = new ExchangeBoard
			{
				Code = "CETS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexAets = new ExchangeBoard
			{
				Code = "AETS",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexCngd = new ExchangeBoard
			{
				Code = "CNGD",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTran = new ExchangeBoard
			{
				Code = "TRAN",
				WorkingTime = micexWorkingTime.Clone(),
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexJunior = new ExchangeBoard
			{
				Code = "QJSIM",
				IsSupportMarketOrders = true,
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			Spb = new ExchangeBoard
			{
				Code = "SPB",
				IsSupportMarketOrders = false,
				IsSupportAtomicReRegister = false,
				Exchange = Exchange.Spb,
				TimeZone = moscowTime,
			};

			Ux = new ExchangeBoard
			{
				Code = "UX",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("10:30:00".To<TimeSpan>(), "13:00:00".To<TimeSpan>()),
								new Range<TimeSpan>("13:03:00".To<TimeSpan>(), "17:30:00".To<TimeSpan>())
							},
						}
					},
				},
				ExpiryTime = new TimeSpan(18, 45, 00),
				IsSupportAtomicReRegister = true,
				Exchange = Exchange.Ux,
				TimeZone = TimeZoneInfo.FromSerializedString("FLE Standard Time;120;(GMT+02:00) Helsinki, Kyiv, Riga, Sofia, Tallinn, Vilnius;FLE Standard Time;FLE Daylight Time;[01:01:0001;12:31:9999;60;[0;03:00:00;3;5;0;];[0;04:00:00;10;5;0;];];"),
			};

			UxStock = new ExchangeBoard
			{
				Code = "GTS",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("10:30:00".To<TimeSpan>(), "17:30:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Ux,
				TimeZone = TimeZoneInfo.FromSerializedString("FLE Standard Time;120;(GMT+02:00) Helsinki, Kyiv, Riga, Sofia, Tallinn, Vilnius;FLE Standard Time;FLE Daylight Time;[01:01:0001;12:31:9999;60;[0;03:00:00;3;5;0;];[0;04:00:00;10;5;0;];];"),
			};

			var newYorkTime = TimeZoneInfo.FromSerializedString("Eastern Standard Time;-300;(UTC-05:00) Eastern Time (US & Canada);Eastern Standard Time;Eastern Daylight Time;[01:01:0001;12:31:2006;60;[0;02:00:00;4;1;0;];[0;02:00:00;10;5;0;];][01:01:2007;12:31:9999;60;[0;02:00:00;3;2;0;];[0;02:00:00;11;1;0;];];");
			var chicagoTime = TimeZoneInfo.FromSerializedString("Central Standard Time;-360;(UTC-06:00) Central Time (US & Canada);Central Standard Time;Central Daylight Time;[01:01:0001;12:31:2006;60;[0;02:00:00;4;1;0;];[0;02:00:00;10;5;0;];][01:01:2007;12:31:9999;60;[0;02:00:00;3;2;0;];[0;02:00:00;11;1;0;];];");

			Amex = new ExchangeBoard
			{
				Code = "AMEX",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("09:30:00".To<TimeSpan>(), "16:00:00".To<TimeSpan>())
							},
						}
					},
				},
				IsSupportMarketOrders = true,
				TimeZone = newYorkTime,
				Exchange = Exchange.Amex
			};

			Cme = new ExchangeBoard
			{
				Code = "CME",
				TimeZone = chicagoTime,
				Exchange = Exchange.Cme,
			};

			Cbot = new ExchangeBoard
			{
				Code = "CBOT",
				TimeZone = chicagoTime,
				Exchange = Exchange.Cbot,
			};

			Cce = new ExchangeBoard
			{
				Code = "CCE",
				TimeZone = chicagoTime,
				Exchange = Exchange.Cce,
			};

			Nyse = new ExchangeBoard
			{
				Code = "NYSE",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("09:30:00".To<TimeSpan>(), "16:00:00".To<TimeSpan>())
							},
						}
					},
				},
				IsSupportMarketOrders = true,
				TimeZone = newYorkTime,
				Exchange = Exchange.Nyse
			};

			Nymex = new ExchangeBoard
			{
				Code = "NYMEX",
				TimeZone = newYorkTime,
				Exchange = Exchange.Nymex,
			};

			Nasdaq = new ExchangeBoard
			{
				Code = "NASDAQ",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("09:30:00".To<TimeSpan>(), "16:00:00".To<TimeSpan>())
							},
						}
					},
				},
				IsSupportMarketOrders = true,
				Exchange = Exchange.Nasdaq,
				TimeZone = newYorkTime,
			};

			Nqlx = new ExchangeBoard
			{
				Code = "NQLX",
				Exchange = Exchange.Nqlx,
				TimeZone = newYorkTime,
			};

			Tsx = new ExchangeBoard
			{
				Code = "TSX",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("09:30:00".To<TimeSpan>(), "16:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Tsx,
				TimeZone = newYorkTime,
			};

			Lse = new ExchangeBoard
			{
				Code = "LSE",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("08:00:00".To<TimeSpan>(), "16:30:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Lse,
				TimeZone = TimeZoneInfo.FromSerializedString("GMT Standard Time;0;(UTC) Dublin, Edinburgh, Lisbon, London;GMT Standard Time;GMT Daylight Time;[01:01:0001;12:31:9999;60;[0;01:00:00;3;5;0;];[0;02:00:00;10;5;0;];];"),
			};

			Tse = new ExchangeBoard
			{
				Code = "TSE",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("09:00:00".To<TimeSpan>(), "11:30:00".To<TimeSpan>()),
								new Range<TimeSpan>("12:30:00".To<TimeSpan>(), "15:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Tse,
				TimeZone = TimeZoneInfo.FromSerializedString("Tokyo Standard Time;540;(UTC+09:00) Osaka, Sapporo, Tokyo;Tokyo Standard Time;Tokyo Daylight Time;;"),
			};

			var chinaTime = TimeZoneInfo.FromSerializedString("China Standard Time;480;(UTC+08:00) Beijing, Chongqing, Hong Kong, Urumqi;China Standard Time;China Daylight Time;;");

			Hkex = new ExchangeBoard
			{
				Code = "HKEX",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("09:20:00".To<TimeSpan>(), "12:00:00".To<TimeSpan>()),
								new Range<TimeSpan>("13:00:00".To<TimeSpan>(), "16:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Hkex,
				TimeZone = chinaTime,
			};

			Hkfe = new ExchangeBoard
			{
				Code = "HKFE",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("09:15:00".To<TimeSpan>(), "12:00:00".To<TimeSpan>()),
								new Range<TimeSpan>("13:00:00".To<TimeSpan>(), "16:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Hkfe,
				TimeZone = chinaTime,
			};

			Sse = new ExchangeBoard
			{
				Code = "SSE",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("09:30:00".To<TimeSpan>(), "11:30:00".To<TimeSpan>()),
								new Range<TimeSpan>("13:00:00".To<TimeSpan>(), "15:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Sse,
				TimeZone = chinaTime,
			};

			Szse = new ExchangeBoard
			{
				Code = "SZSE",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("09:30:00".To<TimeSpan>(), "11:30:00".To<TimeSpan>()),
								new Range<TimeSpan>("13:00:00".To<TimeSpan>(), "15:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Szse,
				TimeZone = chinaTime,
			};

			Tsec = new ExchangeBoard
			{
				Code = "TSEC",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("09:00:00".To<TimeSpan>(), "13:30:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Tsec,
				TimeZone = chinaTime,
			};

			var singaporeTime = TimeZoneInfo.FromSerializedString("Singapore Standard Time;480;(UTC+08:00) Kuala Lumpur, Singapore;Malay Peninsula Standard Time;Malay Peninsula Daylight Time;;");

			Sgx = new ExchangeBoard
			{
				Code = "SGX",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("09:00:00".To<TimeSpan>(), "17:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Sgx,
				TimeZone = singaporeTime,
			};

			Pse = new ExchangeBoard
			{
				Code = "PSE",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("09:30:00".To<TimeSpan>(), "12:00:00".To<TimeSpan>()),
								new Range<TimeSpan>("13:30:00".To<TimeSpan>(), "15:30:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Pse,
				TimeZone = singaporeTime,
			};

			Klse = new ExchangeBoard
			{
				Code = "KLSE",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("09:00:00".To<TimeSpan>(), "12:30:00".To<TimeSpan>()),
								new Range<TimeSpan>("14:00:00".To<TimeSpan>(), "17:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Klse,
				TimeZone = singaporeTime,
			};

			var bangkokTime = TimeZoneInfo.FromSerializedString("SE Asia Standard Time;420;(UTC+07:00) Bangkok, Hanoi, Jakarta;SE Asia Standard Time;SE Asia Daylight Time;;");

			Idx = new ExchangeBoard
			{
				Code = "IDX",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("09:30:00".To<TimeSpan>(), "16:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Idx,
				TimeZone = bangkokTime,
			};

			Set = new ExchangeBoard
			{
				Code = "SET",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("10:00:00".To<TimeSpan>(), "12:30:00".To<TimeSpan>()),
								new Range<TimeSpan>("14:30:00".To<TimeSpan>(), "16:30:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Set,
				TimeZone = bangkokTime,
			};

			var indiaTime = TimeZoneInfo.FromSerializedString("India Standard Time;330;(UTC+05:30) Chennai, Kolkata, Mumbai, New Delhi;India Standard Time;India Daylight Time;;");

			Bse = new ExchangeBoard
			{
				Code = "BSE",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("09:15:00".To<TimeSpan>(), "15:30:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Bse,
				TimeZone = indiaTime,
			};

			Nse = new ExchangeBoard
			{
				Code = "NSE",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("09:15:00".To<TimeSpan>(), "15:30:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Nse,
				TimeZone = indiaTime,
			};

			Cse = new ExchangeBoard
			{
				Code = "CSE",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("09:30:00".To<TimeSpan>(), "14:30:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Cse,
				TimeZone = TimeZoneInfo.FromSerializedString("Sri Lanka Standard Time;330;(UTC+05:30) Sri Jayawardenepura;Sri Lanka Standard Time;Sri Lanka Daylight Time;;"),
			};

			Krx = new ExchangeBoard
			{
				Code = "KRX",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("09:00:00".To<TimeSpan>(), "15:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Krx,
				TimeZone = TimeZoneInfo.FromSerializedString("Korea Standard Time;540;(UTC+09:00) Seoul;Korea Standard Time;Korea Daylight Time;;"),
			};

			Asx = new ExchangeBoard
			{
				Code = "ASX",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("09:50:00".To<TimeSpan>(), "16:12:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Asx,
				TimeZone = TimeZoneInfo.FromSerializedString("AUS Eastern Standard Time;600;(UTC+10:00) Canberra, Melbourne, Sydney;AUS Eastern Standard Time;AUS Eastern Daylight Time;[01:01:0001;12:31:2007;60;[0;02:00:00;10;5;0;];[0;03:00:00;3;5;0;];][01:01:2008;12:31:9999;60;[0;02:00:00;10;1;0;];[0;03:00:00;4;1;0;];];"),
			};

			Nzx = new ExchangeBoard
			{
				Code = "NZX",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("10:00:00".To<TimeSpan>(), "17:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Nzx,
				TimeZone = TimeZoneInfo.FromSerializedString("New Zealand Standard Time;720;(UTC+12:00) Auckland, Wellington;New Zealand Standard Time;New Zealand Daylight Time;[01:01:0001;12:31:2006;60;[0;02:00:00;10;1;0;];[0;03:00:00;3;3;0;];][01:01:2007;12:31:2007;60;[0;02:00:00;9;5;0;];[0;03:00:00;3;3;0;];][01:01:2008;12:31:9999;60;[0;02:00:00;9;5;0;];[0;03:00:00;4;1;0;];];"),
			};

			Tase = new ExchangeBoard
			{
				Code = "TASE",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("09:00:00".To<TimeSpan>(), "16:25:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Tase,
				TimeZone = TimeZoneInfo.FromSerializedString("Israel Standard Time;120;(UTC+02:00) Jerusalem;Jerusalem Standard Time;Jerusalem Daylight Time;[01:01:2005;12:31:2005;60;[0;02:00:00;4;1;5;];[0;02:00:00;10;2;0;];][01:01:2006;12:31:2006;60;[0;02:00:00;3;5;5;];[0;02:00:00;10;1;0;];][01:01:2007;12:31:2007;60;[0;02:00:00;3;5;5;];[0;02:00:00;9;3;0;];][01:01:2008;12:31:2008;60;[0;02:00:00;3;5;5;];[0;02:00:00;10;1;0;];][01:01:2009;12:31:2009;60;[0;02:00:00;3;5;5;];[0;02:00:00;9;5;0;];][01:01:2010;12:31:2010;60;[0;02:00:00;3;5;5;];[0;02:00:00;9;2;0;];][01:01:2011;12:31:2011;60;[0;02:00:00;4;1;5;];[0;02:00:00;10;1;0;];][01:01:2012;12:31:2012;60;[0;02:00:00;3;5;5;];[0;02:00:00;9;4;0;];][01:01:2013;12:31:2013;60;[0;02:00:00;3;5;5;];[0;02:00:00;9;2;0;];][01:01:2014;12:31:2014;60;[0;02:00:00;3;5;5;];[0;02:00:00;9;4;0;];][01:01:2015;12:31:2015;60;[0;02:00:00;3;5;5;];[0;02:00:00;9;3;0;];][01:01:2016;12:31:2016;60;[0;02:00:00;4;1;5;];[0;02:00:00;10;2;0;];][01:01:2017;12:31:2017;60;[0;02:00:00;3;5;5;];[0;02:00:00;9;4;0;];][01:01:2018;12:31:2018;60;[0;02:00:00;3;5;5;];[0;02:00:00;9;3;0;];][01:01:2019;12:31:2019;60;[0;02:00:00;3;5;5;];[0;02:00:00;10;1;0;];][01:01:2020;12:31:2020;60;[0;02:00:00;3;5;5;];[0;02:00:00;9;4;0;];][01:01:2021;12:31:2021;60;[0;02:00:00;3;5;5;];[0;02:00:00;9;2;0;];][01:01:2022;12:31:2022;60;[0;02:00:00;4;1;5;];[0;02:00:00;10;1;0;];];"),
			};

			Fwb = new ExchangeBoard
			{
				Code = "FWB",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("08:00:00".To<TimeSpan>(), "22:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Fwb,
				TimeZone = TimeZoneInfo.FromSerializedString("W. Europe Standard Time;60;(UTC+01:00) Amsterdam, Berlin, Bern, Rome, Stockholm, Vienna;W. Europe Standard Time;W. Europe Daylight Time;[01:01:0001;12:31:9999;60;[0;02:00:00;3;5;0;];[0;03:00:00;10;5;0;];];"),
			};

			Mse = new ExchangeBoard
			{
				Code = "MSE",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("9:00:00".To<TimeSpan>(), "17:30:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Mse,
				TimeZone = TimeZoneInfo.FromSerializedString("Romance Standard Time;60;(UTC+01:00) Brussels, Copenhagen, Madrid, Paris;Romance Standard Time;Romance Daylight Time;[01:01:0001;12:31:9999;60;[0;02:00:00;3;5;0;];[0;03:00:00;10;5;0;];];"),
			};

			Swx = new ExchangeBoard
			{
				Code = "SWX",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("9:00:00".To<TimeSpan>(), "17:30:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Swx,
				TimeZone = GetTimeZone("Central European Standard Time", TimeSpan.FromHours(1)),
			};

			Jse = new ExchangeBoard
			{
				Code = "JSE",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("9:00:00".To<TimeSpan>(), "17:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Jse,
				TimeZone = GetTimeZone("South Africa Standard Time", TimeSpan.FromHours(2)),
			};

			Lmax = new ExchangeBoard
			{
				Code = "LMAX",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
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

		private static TimeZoneInfo GetTimeZone(string id, TimeSpan offset)
		{
			try
			{
				return TimeZoneInfo.FindSystemTimeZoneById(id);
			}
			catch (TimeZoneNotFoundException)
			{
				return TimeZoneInfo.GetSystemTimeZones().First(z => z.BaseUtcOffset == offset);
			}
		}

		/// <summary>
		/// ALL board with no schedule limits.
		/// </summary>
		public static ExchangeBoard Associated { get; private set; }

		/// <summary>
		/// Test board with no schedule limits.
		/// </summary>
		public static ExchangeBoard Test { get; private set; }

		/// <summary>
		/// Information about FORTS board of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard Forts { get; }

		/// <summary>
		/// Information about indecies of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard Micex { get; private set; }

		/// <summary>
		/// Information about AUCT of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexAuct { get; private set; }

		/// <summary>
		/// Information about AUBB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexAubb { get; private set; }

		/// <summary>
		/// Information about CASF of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexCasf { get; private set; }

		/// <summary>
		/// Information about EQBR of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqbr { get; private set; }

		/// <summary>
		/// Information about EQBS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqbs { get; private set; }

		/// <summary>
		/// Information about EQDP of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqdp { get; private set; }

		/// <summary>
		/// Information about EQEU of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqeu { get; private set; }

		/// <summary>
		/// Information about EQUS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqus { get; private set; }

		/// <summary>
		/// Information about EQNB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqnb { get; private set; }

		/// <summary>
		/// Information about EQNE of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqne { get; private set; }

		/// <summary>
		/// Information about EQNL of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqnl { get; private set; }

		/// <summary>
		/// Information about EQNO of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqno { get; private set; }

		/// <summary>
		/// Information about EQOB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqob { get; private set; }

		/// <summary>
		/// Information about EQOS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqos { get; private set; }

		/// <summary>
		/// Information about EQOV of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqov { get; private set; }

		/// <summary>
		/// Information about EQLV of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqlv { get; private set; }

		/// <summary>
		/// Information about EQDB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqdb { get; private set; }

		/// <summary>
		/// Information about EQDE of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqde { get; private set; }

		/// <summary>
		/// Information about EQLI of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqli { get; private set; }

		/// <summary>
		/// Information about EQQI of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqqi { get; private set; }

		/// <summary>
		/// Information about SMAL of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexSmal { get; private set; }

		/// <summary>
		/// Information about SPOB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexSpob { get; private set; }

		/// <summary>
		/// Information about TQBR of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqbr { get; private set; }

		/// <summary>
		/// Information about TQDE of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqde { get; private set; }

		/// <summary>
		/// Information about TQBS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqbs { get; private set; }

		/// <summary>
		/// Information about TQEU of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqeu { get; private set; }

		/// <summary>
		/// Information about TQUS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqus { get; private set; }

		/// <summary>
		/// Information about TQNB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqnb { get; private set; }

		/// <summary>
		/// Information about TQNE of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqne { get; private set; }

		/// <summary>
		/// Information about TQNL of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqnl { get; private set; }

		/// <summary>
		/// Information about TQNO of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqno { get; private set; }

		/// <summary>
		/// Information about TQOB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqob { get; private set; }

		/// <summary>
		/// Information about TQOS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqos { get; private set; }

		/// <summary>
		/// Information about TQOV of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqov { get; private set; }

		/// <summary>
		/// Information about TQLV of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqlv { get; private set; }

		/// <summary>
		/// Information about TQLI of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqli { get; private set; }

		/// <summary>
		/// Information about TQQI of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqqi { get; private set; }

		/// <summary>
		/// Information about EQRP of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqrp { get; private set; }

		/// <summary>
		/// Information about PSRP of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsrp { get; private set; }

		/// <summary>
		/// Information about RFND of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexRfnd { get; private set; }

		/// <summary>
		/// Information about TADM of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTadm { get; private set; }

		/// <summary>
		/// Information about NADM of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexNadm { get; private set; }

		///// <summary>
		///// Информация о площадке TRAN биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		///// </summary>
		//public static ExchangeBoard MicexTran { get; private set; }

		/// <summary>
		/// Information about PSAU of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsau { get; private set; }

		/// <summary>
		/// Information about PAUS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPaus { get; private set; }

		/// <summary>
		/// Information about PSBB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsbb { get; private set; }

		/// <summary>
		/// Information about PSEQ of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPseq { get; private set; }

		/// <summary>
		/// Information about PSES of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPses { get; private set; }

		/// <summary>
		/// Information about PSEU of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPseu { get; private set; }

		/// <summary>
		/// Information about PSDB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsdb { get; private set; }

		/// <summary>
		/// Information about PSDE of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsde { get; private set; }

		/// <summary>
		/// Information about PSUS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsus { get; private set; }

		/// <summary>
		/// Information about PSNB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsnb { get; private set; }

		/// <summary>
		/// Information about PSNE of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsne { get; private set; }

		/// <summary>
		/// Information about PSNL of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsnl { get; private set; }

		/// <summary>
		/// Information about PSNO of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsno { get; private set; }

		/// <summary>
		/// Information about PSOB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsob { get; private set; }

		/// <summary>
		/// Information about PSOS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsos { get; private set; }

		/// <summary>
		/// Information about PSOV of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsov { get; private set; }

		/// <summary>
		/// Information about PSLV of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPslv { get; private set; }

		/// <summary>
		/// Information about PSLI of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsli { get; private set; }

		/// <summary>
		/// Information about PSQI of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsqi { get; private set; }

		/// <summary>
		/// Information about RPEU of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexRpeu { get; private set; }

		/// <summary>
		/// Information about RPMA of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexRpma { get; private set; }

		/// <summary>
		/// Information about RPMO of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexRpmo { get; private set; }

		/// <summary>
		/// Information about RPUA of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexRpua { get; private set; }

		/// <summary>
		/// Information about RPUO of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexRpuo { get; private set; }

		/// <summary>
		/// Information about RPUQ of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexRpuq { get; private set; }

		/// <summary>
		/// Information about FBCB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexFbcb { get; private set; }

		/// <summary>
		/// Information about FBFX of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexFbfx { get; private set; }

		/// <summary>
		/// Information about IRK2 of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexIrk2 { get; private set; }

		/// <summary>
		/// Information about RPQI of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexRpqi { get; private set; }

		/// <summary>
		/// Information about PTEQ of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPteq { get; private set; }

		/// <summary>
		/// Information about PTES of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtes { get; private set; }

		/// <summary>
		/// Information about PTEU of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPteu { get; private set; }

		/// <summary>
		/// Information about PTUS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtus { get; private set; }

		/// <summary>
		/// Information about PTNB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtnb { get; private set; }

		/// <summary>
		/// Information about PTNE of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtne { get; private set; }

		/// <summary>
		/// Information about PTNL of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtnl { get; private set; }

		/// <summary>
		/// Information about PTNO of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtno { get; private set; }

		/// <summary>
		/// Information about PTOB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtob { get; private set; }

		/// <summary>
		/// Information about PTOS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtos { get; private set; }

		/// <summary>
		/// Information about PTOV of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtov { get; private set; }

		/// <summary>
		/// Information about PTLV of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtlv { get; private set; }

		/// <summary>
		/// Information about PTLI of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtli { get; private set; }

		/// <summary>
		/// Information about PTQI of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtqi { get; private set; }

		/// <summary>
		/// Information about SCVC of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexScvc { get; private set; }

		/// <summary>
		/// Information about RPNG of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexRpng { get; private set; }

		/// <summary>
		/// Information about RPFG of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexRpfg { get; private set; }

		/// <summary>
		/// Information about CDCR of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexCbcr { get; private set; }

		/// <summary>
		/// Information about CRED of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexCred { get; private set; }

		/// <summary>
		/// Information about DEPZ of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexDepz { get; private set; }

		/// <summary>
		/// Information about DPVB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexDpvb { get; private set; }

		/// <summary>
		/// Information about DPFK of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexDpfk { get; private set; }

		/// <summary>
		/// Information about DPFO of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexDpfo { get; private set; }

		/// <summary>
		/// Information about DPPF of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexDppf { get; private set; }

		/// <summary>
		/// Information about CETS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexCets { get; private set; }

		/// <summary>
		/// Information about AETS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexAets { get; private set; }

		/// <summary>
		/// Information about CNGD of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexCngd { get; private set; }

		/// <summary>
		/// Information about TRAN of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTran { get; private set; }

		/// <summary>
		/// Information about QJSIM of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexJunior { get; private set; }

		/// <summary>
		/// Information about SPB of <see cref="BusinessEntities.Exchange.Spb"/> exchange.
		/// </summary>
		public static ExchangeBoard Spb { get; private set; }

		/// <summary>
		/// Information about derivatives market of <see cref="BusinessEntities.Exchange.Ux"/> exchange.
		/// </summary>
		public static ExchangeBoard Ux { get; }

		/// <summary>
		/// Information about stock market of <see cref="BusinessEntities.Exchange.Ux"/> exchange.
		/// </summary>
		public static ExchangeBoard UxStock { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Cme"/> exchange.
		/// </summary>
		public static ExchangeBoard Cme { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Cce"/> exchange.
		/// </summary>
		public static ExchangeBoard Cce { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Cbot"/> exchange.
		/// </summary>
		public static ExchangeBoard Cbot { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Nymex"/> exchange.
		/// </summary>
		public static ExchangeBoard Nymex { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Amex"/> exchange.
		/// </summary>
		public static ExchangeBoard Amex { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Nyse"/> exchange.
		/// </summary>
		public static ExchangeBoard Nyse { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Nasdaq"/> exchange.
		/// </summary>
		public static ExchangeBoard Nasdaq { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Nqlx"/> exchange.
		/// </summary>
		public static ExchangeBoard Nqlx { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Lse"/> exchange.
		/// </summary>
		public static ExchangeBoard Lse { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Tse"/> exchange.
		/// </summary>
		public static ExchangeBoard Tse { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Hkex"/> exchange.
		/// </summary>
		public static ExchangeBoard Hkex { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Hkfe"/> exchange.
		/// </summary>
		public static ExchangeBoard Hkfe { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Sse"/> exchange.
		/// </summary>
		public static ExchangeBoard Sse { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Szse"/> exchange.
		/// </summary>
		public static ExchangeBoard Szse { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Tsx"/> exchange.
		/// </summary>
		public static ExchangeBoard Tsx { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Fwb"/> exchange.
		/// </summary>
		public static ExchangeBoard Fwb { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Asx"/> exchange.
		/// </summary>
		public static ExchangeBoard Asx { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Nzx"/> exchange.
		/// </summary>
		public static ExchangeBoard Nzx { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Bse"/> exchange.
		/// </summary>
		public static ExchangeBoard Bse { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Nse"/> exchange.
		/// </summary>
		public static ExchangeBoard Nse { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Swx"/> exchange.
		/// </summary>
		public static ExchangeBoard Swx { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Krx"/> exchange.
		/// </summary>
		public static ExchangeBoard Krx { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Mse"/> exchange.
		/// </summary>
		public static ExchangeBoard Mse { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Jse"/> exchange.
		/// </summary>
		public static ExchangeBoard Jse { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Sgx"/> exchange.
		/// </summary>
		public static ExchangeBoard Sgx { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Tsec"/> exchange.
		/// </summary>
		public static ExchangeBoard Tsec { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Pse"/> exchange.
		/// </summary>
		public static ExchangeBoard Pse { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Klse"/> exchange.
		/// </summary>
		public static ExchangeBoard Klse { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Idx"/> exchange.
		/// </summary>
		public static ExchangeBoard Idx { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Set"/> exchange.
		/// </summary>
		public static ExchangeBoard Set { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Cse"/> exchange.
		/// </summary>
		public static ExchangeBoard Cse { get; private set; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Tase"/> exchange.
		/// </summary>
		public static ExchangeBoard Tase { get; private set; }

		/// <summary>
		/// Information about brokerage board <see cref="BusinessEntities.Exchange.Lmax"/>.
		/// </summary>
		public static ExchangeBoard Lmax { get; private set; }

		/// <summary>
		/// Information about brokerage board <see cref="BusinessEntities.Exchange.DukasCopy"/>.
		/// </summary>
		public static ExchangeBoard DukasCopy { get; private set; }

		/// <summary>
		/// Information about brokerage board <see cref="BusinessEntities.Exchange.GainCapital"/>.
		/// </summary>
		public static ExchangeBoard GainCapital { get; private set; }

		/// <summary>
		/// Information about brokerage board <see cref="BusinessEntities.Exchange.MBTrading"/>.
		/// </summary>
		public static ExchangeBoard MBTrading { get; private set; }

		/// <summary>
		/// Information about brokerage board <see cref="BusinessEntities.Exchange.TrueFX"/>.
		/// </summary>
		public static ExchangeBoard TrueFX { get; private set; }

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Integral"/>.
		/// </summary>
		public static ExchangeBoard Integral { get; private set; }

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Cfh"/>.
		/// </summary>
		public static ExchangeBoard Cfh { get; private set; }

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Ond"/>.
		/// </summary>
		public static ExchangeBoard Ond { get; private set; }

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Nasdaq"/>.
		/// </summary>
		public static ExchangeBoard Smart { get; private set; }

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Btce"/>.
		/// </summary>
		public static ExchangeBoard Btce { get; private set; }

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.BitStamp"/>.
		/// </summary>
		public static ExchangeBoard BitStamp { get; private set; }

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.BtcChina"/>.
		/// </summary>
		public static ExchangeBoard BtcChina { get; private set; }

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Icbit"/>.
		/// </summary>
		public static ExchangeBoard Icbit { get; private set; }

		/// <summary>
		/// Information about virtual board Finam.
		/// </summary>
		public static ExchangeBoard Finam { get; private set; }

		/// <summary>
		/// Information about virtual board Mfd.
		/// </summary>
		public static ExchangeBoard Mfd { get; private set; }

	}
}
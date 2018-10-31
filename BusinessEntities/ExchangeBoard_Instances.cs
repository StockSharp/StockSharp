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

			Associated = new ExchangeBoard
			{
				Code = MessageAdapter.DefaultAssociatedBoardCode,
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
				
				// http://moex.com/a254
				new DateTime(2012, 3, 11),
				new DateTime(2012, 4, 28),
				new DateTime(2012, 5, 5),
				new DateTime(2012, 5, 12),
				new DateTime(2012, 6, 9),
				new DateTime(2012, 12, 29),

				// http://moex.com/a3367
				new DateTime(2016, 02, 20)
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

				// http://moex.com/a254
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

				// http://moex.com/a1343
				new DateTime(2013, 1, 1),
				new DateTime(2013, 1, 2),
				new DateTime(2013, 1, 3),
				new DateTime(2013, 1, 4),
				new DateTime(2013, 1, 7),
				new DateTime(2013, 3, 8),
				new DateTime(2013, 5, 1),
				new DateTime(2013, 5, 9),
				new DateTime(2013, 6, 12),
				new DateTime(2013, 11, 4),
				new DateTime(2013, 12, 31),

				// http://moex.com/a2973
				new DateTime(2014, 1, 1),
				new DateTime(2014, 1, 2),
				new DateTime(2014, 1, 3),
				new DateTime(2014, 1, 7),
				new DateTime(2014, 3, 10),
				new DateTime(2014, 5, 1),
				new DateTime(2014, 5, 9),
				new DateTime(2014, 6, 12),
				new DateTime(2014, 11, 4),
				new DateTime(2014, 12, 31),
				
				// http://moex.com/a2793
				new DateTime(2015, 1, 1),
				new DateTime(2015, 1, 2),
				new DateTime(2015, 1, 7),
				new DateTime(2015, 2, 23),
				new DateTime(2015, 3, 9),
				new DateTime(2015, 5, 1),
				new DateTime(2015, 5, 4),
				new DateTime(2015, 5, 11),
				new DateTime(2015, 6, 12),
				new DateTime(2015, 11, 4),
				new DateTime(2015, 12, 31),

				// http://moex.com/a3367
				new DateTime(2016, 1, 1),
				new DateTime(2016, 1, 7),
				new DateTime(2016, 1, 8),
				new DateTime(2016, 2, 23),
				new DateTime(2016, 3, 8),
				new DateTime(2016, 5, 2),
				new DateTime(2016, 5, 3),
				new DateTime(2016, 5, 9),
				new DateTime(2016, 6, 13),
				new DateTime(2016, 11, 4),
			};

			//var moscowTime = TimeZoneInfo.FromSerializedString("Russian Standard Time;180;(UTC+03:00) Moscow, St. Petersburg, Volgograd (RTZ 2);Russia TZ 2 Standard Time;Russia TZ 2 Daylight Time;[01:01:0001;12:31:2010;60;[0;02:00:00;3;5;0;];[0;03:00:00;10;5;0;];][01:01:2011;12:31:2011;60;[0;02:00:00;3;5;0;];[0;00:00:00;1;1;6;];][01:01:2014;12:31:2014;60;[0;00:00:00;1;1;3;];[0;02:00:00;10;5;0;];];");
			var moscowTime = TimeHelper.Moscow;

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
					SpecialWorkingDays = russianSpecialWorkingDays,
					SpecialHolidays = russianSpecialHolidays,
				},
				ExpiryTime = new TimeSpan(18, 45, 00),
				//IsSupportAtomicReRegister = true,
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
				SpecialWorkingDays = russianSpecialWorkingDays,
				SpecialHolidays = russianSpecialHolidays,
			};

			Micex = new ExchangeBoard
			{
				Code = "MICEX",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexAuct = new ExchangeBoard
			{
				Code = "AUCT",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexAubb = new ExchangeBoard
			{
				Code = "AUBB",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexCasf = new ExchangeBoard
			{
				Code = "CASF",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqbr = new ExchangeBoard
			{
				Code = "EQBR",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqbs = new ExchangeBoard
			{
				Code = "EQBS",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqdp = new ExchangeBoard
			{
				Code = "EQDP",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqeu = new ExchangeBoard
			{
				Code = "EQEU",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqus = new ExchangeBoard
			{
				Code = "EQUS",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqnb = new ExchangeBoard
			{
				Code = "EQNB",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqne = new ExchangeBoard
			{
				Code = "EQNE",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqnl = new ExchangeBoard
			{
				Code = "EQNL",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqno = new ExchangeBoard
			{
				Code = "EQNO",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqob = new ExchangeBoard
			{
				Code = "EQOB",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqos = new ExchangeBoard
			{
				Code = "EQOS",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqov = new ExchangeBoard
			{
				Code = "EQOV",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqlv = new ExchangeBoard
			{
				Code = "EQLV",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqdb = new ExchangeBoard
			{
				Code = "EQDB",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqde = new ExchangeBoard
			{
				Code = "EQDE",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqli = new ExchangeBoard
			{
				Code = "EQLI",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqqi = new ExchangeBoard
			{
				Code = "EQQI",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexSmal = new ExchangeBoard
			{
				Code = "SMAL",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexSpob = new ExchangeBoard
			{
				Code = "SPOB",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqbr = new ExchangeBoard
			{
				Code = "TQBR",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqde = new ExchangeBoard
			{
				Code = "TQDE",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqbs = new ExchangeBoard
			{
				Code = "TQBS",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqeu = new ExchangeBoard
			{
				Code = "TQEU",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqus = new ExchangeBoard
			{
				Code = "TQUS",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqnb = new ExchangeBoard
			{
				Code = "TQNB",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqne = new ExchangeBoard
			{
				Code = "TQNE",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqnl = new ExchangeBoard
			{
				Code = "TQNL",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqno = new ExchangeBoard
			{
				Code = "TQNO",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqob = new ExchangeBoard
			{
				Code = "TQOB",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqos = new ExchangeBoard
			{
				Code = "TQOS",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqov = new ExchangeBoard
			{
				Code = "TQOV",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqlv = new ExchangeBoard
			{
				Code = "TQLV",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqli = new ExchangeBoard
			{
				Code = "TQLI",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTqqi = new ExchangeBoard
			{
				Code = "TQQI",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexEqrp = new ExchangeBoard
			{
				Code = "EQRP",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsrp = new ExchangeBoard
			{
				Code = "PSRP",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexRfnd = new ExchangeBoard
			{
				Code = "RFND",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTadm = new ExchangeBoard
			{
				Code = "TADM",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexNadm = new ExchangeBoard
			{
				Code = "NADM",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			//MicexTran = new ExchangeBoard
			//{
			//	Code = "TRAN",
			//	WorkingTime = micexWorkingTime.Clone(),
			//	//IsSupportMarketOrders = true,
			//	Exchange = Exchange.Moex,
			//	TimeZone = moscowTime,
			//};

			MicexPsau = new ExchangeBoard
			{
				Code = "PSAU",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPaus = new ExchangeBoard
			{
				Code = "PAUS",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsbb = new ExchangeBoard
			{
				Code = "PSBB",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPseq = new ExchangeBoard
			{
				Code = "PSEQ",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPses = new ExchangeBoard
			{
				Code = "PSES",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPseu = new ExchangeBoard
			{
				Code = "PSEU",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsdb = new ExchangeBoard
			{
				Code = "PSDB",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsde = new ExchangeBoard
			{
				Code = "PSDE",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsus = new ExchangeBoard
			{
				Code = "PSUS",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsnb = new ExchangeBoard
			{
				Code = "PSNB",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsne = new ExchangeBoard
			{
				Code = "PSNE",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsnl = new ExchangeBoard
			{
				Code = "PSNL",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsno = new ExchangeBoard
			{
				Code = "PSNO",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsob = new ExchangeBoard
			{
				Code = "PSOB",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsos = new ExchangeBoard
			{
				Code = "PSOS",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsov = new ExchangeBoard
			{
				Code = "PSOV",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPslv = new ExchangeBoard
			{
				Code = "PSLV",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsli = new ExchangeBoard
			{
				Code = "PSLI",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPsqi = new ExchangeBoard
			{
				Code = "PSQI",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexRpeu = new ExchangeBoard
			{
				Code = "RPEU",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexRpma = new ExchangeBoard
			{
				Code = "RPMA",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexRpmo = new ExchangeBoard
			{
				Code = "RPMO",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexRpua = new ExchangeBoard
			{
				Code = "RPUA",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexRpuo = new ExchangeBoard
			{
				Code = "RPUO",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexRpuq = new ExchangeBoard
			{
				Code = "RPUQ",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexFbcb = new ExchangeBoard
			{
				Code = "FBCB",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexFbfx = new ExchangeBoard
			{
				Code = "FBFX",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexIrk2 = new ExchangeBoard
			{
				Code = "IRK2",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexRpqi = new ExchangeBoard
			{
				Code = "RPQI",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPteq = new ExchangeBoard
			{
				Code = "PTEQ",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtes = new ExchangeBoard
			{
				Code = "PTES",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPteu = new ExchangeBoard
			{
				Code = "PTEU",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtus = new ExchangeBoard
			{
				Code = "PTUS",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtnb = new ExchangeBoard
			{
				Code = "PTNB",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtne = new ExchangeBoard
			{
				Code = "PTNE",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtnl = new ExchangeBoard
			{
				Code = "PTNL",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtno = new ExchangeBoard
			{
				Code = "PTNO",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtob = new ExchangeBoard
			{
				Code = "PTOB",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtos = new ExchangeBoard
			{
				Code = "PTOS",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtov = new ExchangeBoard
			{
				Code = "PTOV",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtlv = new ExchangeBoard
			{
				Code = "PTLV",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtli = new ExchangeBoard
			{
				Code = "PTLI",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexPtqi = new ExchangeBoard
			{
				Code = "PTQI",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexScvc = new ExchangeBoard
			{
				Code = "SCVC",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexRpng = new ExchangeBoard
			{
				Code = "RPNG",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexRpfg = new ExchangeBoard
			{
				Code = "RPFG",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexCbcr = new ExchangeBoard
			{
				Code = "CBCR",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexCred = new ExchangeBoard
			{
				Code = "CRED",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexDepz = new ExchangeBoard
			{
				Code = "DEPZ",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
			};

			MicexDpvb = new ExchangeBoard
			{
				Code = "DPVB",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexDpfk = new ExchangeBoard
			{
				Code = "DPFK",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexDpfo = new ExchangeBoard
			{
				Code = "DPFO",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexDppf = new ExchangeBoard
			{
				Code = "DPPF",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexCets = new ExchangeBoard
			{
				Code = "CETS",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexAets = new ExchangeBoard
			{
				Code = "AETS",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexCngd = new ExchangeBoard
			{
				Code = "CNGD",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexTran = new ExchangeBoard
			{
				Code = "TRAN",
				WorkingTime = micexWorkingTime.Clone(),
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			MicexJunior = new ExchangeBoard
			{
				Code = "QJSIM",
				//IsSupportMarketOrders = true,
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Moex,
				TimeZone = moscowTime,
			};

			Spb = new ExchangeBoard
			{
				Code = "SPB",
				//IsSupportMarketOrders = false,
				//IsSupportAtomicReRegister = false,
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
				//IsSupportAtomicReRegister = true,
				Exchange = Exchange.Ux,
				TimeZone = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time"),
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
				TimeZone = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time"),
			};

			var newYorkTime = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
			var chicagoTime = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");

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
				//IsSupportMarketOrders = true,
				TimeZone = newYorkTime,
				Exchange = Exchange.Amex
			};

			Cme = new ExchangeBoard
			{
				Code = "CME",
				TimeZone = chicagoTime,
				Exchange = Exchange.Cme,
			};

			CmeMini = new ExchangeBoard
			{
				Code = "CMEMINI",
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
				//IsSupportMarketOrders = true,
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
				//IsSupportMarketOrders = true,
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
				TimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"),
			};

			Lme = new ExchangeBoard
			{
				Code = "LME",
				WorkingTime = new WorkingTime
				{
					Periods = new List<WorkingTimePeriod>
					{
						new WorkingTimePeriod
						{
							Till = DateTime.MaxValue,
							Times = new List<Range<TimeSpan>>
							{
								new Range<TimeSpan>("09:00:00".To<TimeSpan>(), "18:00:00".To<TimeSpan>())
							},
						}
					},
				},
				Exchange = Exchange.Lme,
				TimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"),
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
				TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time"),
			};

			var chinaTime = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");

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

			var singaporeTime = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");

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

			var bangkokTime = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

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

			var indiaTime = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

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
				TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time"),
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
				TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time"),
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
				TimeZone = TimeZoneInfo.FindSystemTimeZoneById("AUS Eastern Standard Time"),
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
				TimeZone = TimeZoneInfo.FindSystemTimeZoneById("New Zealand Standard Time"),
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
				TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time"),
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
				TimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time"),
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
				TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"),
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
		public static ExchangeBoard Associated { get; }

		/// <summary>
		/// Test board with no schedule limits.
		/// </summary>
		public static ExchangeBoard Test { get; }

		/// <summary>
		/// Information about FORTS board of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard Forts { get; }

		/// <summary>
		/// Information about indecies of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard Micex { get; }

		/// <summary>
		/// Information about AUCT of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexAuct { get; }

		/// <summary>
		/// Information about AUBB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexAubb { get; }

		/// <summary>
		/// Information about CASF of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexCasf { get; }

		/// <summary>
		/// Information about EQBR of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqbr { get; }

		/// <summary>
		/// Information about EQBS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqbs { get; }

		/// <summary>
		/// Information about EQDP of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqdp { get; }

		/// <summary>
		/// Information about EQEU of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqeu { get; }

		/// <summary>
		/// Information about EQUS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqus { get; }

		/// <summary>
		/// Information about EQNB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqnb { get; }

		/// <summary>
		/// Information about EQNE of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqne { get; }

		/// <summary>
		/// Information about EQNL of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqnl { get; }

		/// <summary>
		/// Information about EQNO of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqno { get; }

		/// <summary>
		/// Information about EQOB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqob { get; }

		/// <summary>
		/// Information about EQOS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqos { get; }

		/// <summary>
		/// Information about EQOV of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqov { get; }

		/// <summary>
		/// Information about EQLV of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqlv { get; }

		/// <summary>
		/// Information about EQDB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqdb { get; }

		/// <summary>
		/// Information about EQDE of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqde { get; }

		/// <summary>
		/// Information about EQLI of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqli { get; }

		/// <summary>
		/// Information about EQQI of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqqi { get; }

		/// <summary>
		/// Information about SMAL of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexSmal { get; }

		/// <summary>
		/// Information about SPOB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexSpob { get; }

		/// <summary>
		/// Information about TQBR of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqbr { get; }

		/// <summary>
		/// Information about TQDE of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqde { get; }

		/// <summary>
		/// Information about TQBS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqbs { get; }

		/// <summary>
		/// Information about TQEU of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqeu { get; }

		/// <summary>
		/// Information about TQUS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqus { get; }

		/// <summary>
		/// Information about TQNB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqnb { get; }

		/// <summary>
		/// Information about TQNE of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqne { get; }

		/// <summary>
		/// Information about TQNL of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqnl { get; }

		/// <summary>
		/// Information about TQNO of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqno { get; }

		/// <summary>
		/// Information about TQOB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqob { get; }

		/// <summary>
		/// Information about TQOS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqos { get; }

		/// <summary>
		/// Information about TQOV of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqov { get; }

		/// <summary>
		/// Information about TQLV of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqlv { get; }

		/// <summary>
		/// Information about TQLI of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqli { get; }

		/// <summary>
		/// Information about TQQI of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTqqi { get; }

		/// <summary>
		/// Information about EQRP of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexEqrp { get; }

		/// <summary>
		/// Information about PSRP of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsrp { get; }

		/// <summary>
		/// Information about RFND of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexRfnd { get; }

		/// <summary>
		/// Information about TADM of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTadm { get; }

		/// <summary>
		/// Information about NADM of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexNadm { get; }

		///// <summary>
		///// Информация о площадке TRAN биржи <see cref="BusinessEntities.Exchange.Moex"/>.
		///// </summary>
		//public static ExchangeBoard MicexTran { get; }

		/// <summary>
		/// Information about PSAU of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsau { get; }

		/// <summary>
		/// Information about PAUS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPaus { get; }

		/// <summary>
		/// Information about PSBB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsbb { get; }

		/// <summary>
		/// Information about PSEQ of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPseq { get; }

		/// <summary>
		/// Information about PSES of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPses { get; }

		/// <summary>
		/// Information about PSEU of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPseu { get; }

		/// <summary>
		/// Information about PSDB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsdb { get; }

		/// <summary>
		/// Information about PSDE of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsde { get; }

		/// <summary>
		/// Information about PSUS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsus { get; }

		/// <summary>
		/// Information about PSNB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsnb { get; }

		/// <summary>
		/// Information about PSNE of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsne { get; }

		/// <summary>
		/// Information about PSNL of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsnl { get; }

		/// <summary>
		/// Information about PSNO of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsno { get; }

		/// <summary>
		/// Information about PSOB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsob { get; }

		/// <summary>
		/// Information about PSOS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsos { get; }

		/// <summary>
		/// Information about PSOV of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsov { get; }

		/// <summary>
		/// Information about PSLV of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPslv { get; }

		/// <summary>
		/// Information about PSLI of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsli { get; }

		/// <summary>
		/// Information about PSQI of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPsqi { get; }

		/// <summary>
		/// Information about RPEU of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexRpeu { get; }

		/// <summary>
		/// Information about RPMA of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexRpma { get; }

		/// <summary>
		/// Information about RPMO of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexRpmo { get; }

		/// <summary>
		/// Information about RPUA of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexRpua { get; }

		/// <summary>
		/// Information about RPUO of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexRpuo { get; }

		/// <summary>
		/// Information about RPUQ of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexRpuq { get; }

		/// <summary>
		/// Information about FBCB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexFbcb { get; }

		/// <summary>
		/// Information about FBFX of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexFbfx { get; }

		/// <summary>
		/// Information about IRK2 of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexIrk2 { get; }

		/// <summary>
		/// Information about RPQI of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexRpqi { get; }

		/// <summary>
		/// Information about PTEQ of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPteq { get; }

		/// <summary>
		/// Information about PTES of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtes { get; }

		/// <summary>
		/// Information about PTEU of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPteu { get; }

		/// <summary>
		/// Information about PTUS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtus { get; }

		/// <summary>
		/// Information about PTNB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtnb { get; }

		/// <summary>
		/// Information about PTNE of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtne { get; }

		/// <summary>
		/// Information about PTNL of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtnl { get; }

		/// <summary>
		/// Information about PTNO of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtno { get; }

		/// <summary>
		/// Information about PTOB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtob { get; }

		/// <summary>
		/// Information about PTOS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtos { get; }

		/// <summary>
		/// Information about PTOV of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtov { get; }

		/// <summary>
		/// Information about PTLV of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtlv { get; }

		/// <summary>
		/// Information about PTLI of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtli { get; }

		/// <summary>
		/// Information about PTQI of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexPtqi { get; }

		/// <summary>
		/// Information about SCVC of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexScvc { get; }

		/// <summary>
		/// Information about RPNG of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexRpng { get; }

		/// <summary>
		/// Information about RPFG of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexRpfg { get; }

		/// <summary>
		/// Information about CDCR of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexCbcr { get; }

		/// <summary>
		/// Information about CRED of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexCred { get; }

		/// <summary>
		/// Information about DEPZ of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexDepz { get; }

		/// <summary>
		/// Information about DPVB of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexDpvb { get; }

		/// <summary>
		/// Information about DPFK of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexDpfk { get; }

		/// <summary>
		/// Information about DPFO of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexDpfo { get; }

		/// <summary>
		/// Information about DPPF of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexDppf { get; }

		/// <summary>
		/// Information about CETS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexCets { get; }

		/// <summary>
		/// Information about AETS of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexAets { get; }

		/// <summary>
		/// Information about CNGD of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexCngd { get; }

		/// <summary>
		/// Information about TRAN of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexTran { get; }

		/// <summary>
		/// Information about QJSIM of <see cref="BusinessEntities.Exchange.Moex"/> exchange.
		/// </summary>
		public static ExchangeBoard MicexJunior { get; }

		/// <summary>
		/// Information about SPB of <see cref="BusinessEntities.Exchange.Spb"/> exchange.
		/// </summary>
		public static ExchangeBoard Spb { get; }

		/// <summary>
		/// Information about derivatives market of <see cref="BusinessEntities.Exchange.Ux"/> exchange.
		/// </summary>
		public static ExchangeBoard Ux { get; }

		/// <summary>
		/// Information about stock market of <see cref="BusinessEntities.Exchange.Ux"/> exchange.
		/// </summary>
		public static ExchangeBoard UxStock { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Cme"/> exchange.
		/// </summary>
		public static ExchangeBoard Cme { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Cme"/> exchange.
		/// </summary>
		public static ExchangeBoard CmeMini { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Cce"/> exchange.
		/// </summary>
		public static ExchangeBoard Cce { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Cbot"/> exchange.
		/// </summary>
		public static ExchangeBoard Cbot { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Nymex"/> exchange.
		/// </summary>
		public static ExchangeBoard Nymex { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Amex"/> exchange.
		/// </summary>
		public static ExchangeBoard Amex { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Nyse"/> exchange.
		/// </summary>
		public static ExchangeBoard Nyse { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Nasdaq"/> exchange.
		/// </summary>
		public static ExchangeBoard Nasdaq { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Nqlx"/> exchange.
		/// </summary>
		public static ExchangeBoard Nqlx { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Lse"/> exchange.
		/// </summary>
		public static ExchangeBoard Lse { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Lme"/> exchange.
		/// </summary>
		public static ExchangeBoard Lme { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Tse"/> exchange.
		/// </summary>
		public static ExchangeBoard Tse { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Hkex"/> exchange.
		/// </summary>
		public static ExchangeBoard Hkex { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Hkfe"/> exchange.
		/// </summary>
		public static ExchangeBoard Hkfe { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Sse"/> exchange.
		/// </summary>
		public static ExchangeBoard Sse { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Szse"/> exchange.
		/// </summary>
		public static ExchangeBoard Szse { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Tsx"/> exchange.
		/// </summary>
		public static ExchangeBoard Tsx { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Fwb"/> exchange.
		/// </summary>
		public static ExchangeBoard Fwb { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Asx"/> exchange.
		/// </summary>
		public static ExchangeBoard Asx { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Nzx"/> exchange.
		/// </summary>
		public static ExchangeBoard Nzx { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Bse"/> exchange.
		/// </summary>
		public static ExchangeBoard Bse { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Nse"/> exchange.
		/// </summary>
		public static ExchangeBoard Nse { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Swx"/> exchange.
		/// </summary>
		public static ExchangeBoard Swx { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Krx"/> exchange.
		/// </summary>
		public static ExchangeBoard Krx { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Mse"/> exchange.
		/// </summary>
		public static ExchangeBoard Mse { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Jse"/> exchange.
		/// </summary>
		public static ExchangeBoard Jse { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Sgx"/> exchange.
		/// </summary>
		public static ExchangeBoard Sgx { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Tsec"/> exchange.
		/// </summary>
		public static ExchangeBoard Tsec { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Pse"/> exchange.
		/// </summary>
		public static ExchangeBoard Pse { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Klse"/> exchange.
		/// </summary>
		public static ExchangeBoard Klse { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Idx"/> exchange.
		/// </summary>
		public static ExchangeBoard Idx { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Set"/> exchange.
		/// </summary>
		public static ExchangeBoard Set { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Cse"/> exchange.
		/// </summary>
		public static ExchangeBoard Cse { get; }

		/// <summary>
		/// Information about board of <see cref="BusinessEntities.Exchange.Tase"/> exchange.
		/// </summary>
		public static ExchangeBoard Tase { get; }

		/// <summary>
		/// Information about brokerage board <see cref="BusinessEntities.Exchange.Lmax"/>.
		/// </summary>
		public static ExchangeBoard Lmax { get; }

		/// <summary>
		/// Information about brokerage board <see cref="BusinessEntities.Exchange.DukasCopy"/>.
		/// </summary>
		public static ExchangeBoard DukasCopy { get; }

		/// <summary>
		/// Information about brokerage board <see cref="BusinessEntities.Exchange.GainCapital"/>.
		/// </summary>
		public static ExchangeBoard GainCapital { get; }

		/// <summary>
		/// Information about brokerage board <see cref="BusinessEntities.Exchange.MBTrading"/>.
		/// </summary>
		public static ExchangeBoard MBTrading { get; }

		/// <summary>
		/// Information about brokerage board <see cref="BusinessEntities.Exchange.TrueFX"/>.
		/// </summary>
		public static ExchangeBoard TrueFX { get; }

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Integral"/>.
		/// </summary>
		public static ExchangeBoard Integral { get; }

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Cfh"/>.
		/// </summary>
		public static ExchangeBoard Cfh { get; }

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Ond"/>.
		/// </summary>
		public static ExchangeBoard Ond { get; }

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Nasdaq"/>.
		/// </summary>
		public static ExchangeBoard Smart { get; }

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Btce"/>.
		/// </summary>
		public static ExchangeBoard Btce { get; }

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.BitStamp"/>.
		/// </summary>
		public static ExchangeBoard BitStamp { get; }

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.BtcChina"/>.
		/// </summary>
		public static ExchangeBoard BtcChina { get; }

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Icbit"/>.
		/// </summary>
		public static ExchangeBoard Icbit { get; }

		/// <summary>
		/// Information about virtual board Finam.
		/// </summary>
		public static ExchangeBoard Finam { get; }

		/// <summary>
		/// Information about virtual board Mfd.
		/// </summary>
		public static ExchangeBoard Mfd { get; }

		/// <summary>
		/// Information about board Arca.
		/// </summary>
		public static ExchangeBoard Arca { get; } = new ExchangeBoard
		{
			Code = "ARCA",
			Exchange = Exchange.Nyse,
		};

		/// <summary>
		/// Information about board BATS.
		/// </summary>
		public static ExchangeBoard Bats { get; } = new ExchangeBoard
		{
			Code = "BATS",
			Exchange = Exchange.Cbot,
		};

		/// <summary>
		/// Information about board BATS.
		/// </summary>
		public static ExchangeBoard Currenex { get; } = new ExchangeBoard
		{
			Code = Exchange.Currenex.Name,
			Exchange = Exchange.Currenex,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Fxcm"/>.
		/// </summary>
		public static ExchangeBoard Fxcm { get; } = new ExchangeBoard
		{
			Code = Exchange.Fxcm.Name,
			Exchange = Exchange.Fxcm,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Poloniex"/>.
		/// </summary>
		public static ExchangeBoard Poloniex { get; } = new ExchangeBoard
		{
			Code = Exchange.Poloniex.Name,
			Exchange = Exchange.Poloniex,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Kraken"/>.
		/// </summary>
		public static ExchangeBoard Kraken { get; } = new ExchangeBoard
		{
			Code = Exchange.Kraken.Name,
			Exchange = Exchange.Kraken,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Bittrex"/>.
		/// </summary>
		public static ExchangeBoard Bittrex { get; } = new ExchangeBoard
		{
			Code = Exchange.Bittrex.Name,
			Exchange = Exchange.Bittrex,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Bitfinex"/>.
		/// </summary>
		public static ExchangeBoard Bitfinex { get; } = new ExchangeBoard
		{
			Code = Exchange.Bitfinex.Name,
			Exchange = Exchange.Bitfinex,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Coinbase"/>.
		/// </summary>
		public static ExchangeBoard Coinbase { get; } = new ExchangeBoard
		{
			Code = Exchange.Coinbase.Name,
			Exchange = Exchange.Coinbase,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Gdax"/>.
		/// </summary>
		public static ExchangeBoard Gdax { get; } = new ExchangeBoard
		{
			Code = Exchange.Gdax.Name,
			Exchange = Exchange.Gdax,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Bithumb"/>.
		/// </summary>
		public static ExchangeBoard Bithumb { get; } = new ExchangeBoard
		{
			Code = Exchange.Bithumb.Name,
			Exchange = Exchange.Bithumb,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.HitBtc"/>.
		/// </summary>
		public static ExchangeBoard HitBtc { get; } = new ExchangeBoard
		{
			Code = Exchange.HitBtc.Name,
			Exchange = Exchange.HitBtc,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.OkCoin"/>.
		/// </summary>
		public static ExchangeBoard OkCoin { get; } = new ExchangeBoard
		{
			Code = Exchange.OkCoin.Name,
			Exchange = Exchange.OkCoin,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Coincheck"/>.
		/// </summary>
		public static ExchangeBoard Coincheck { get; } = new ExchangeBoard
		{
			Code = Exchange.Coincheck.Name,
			Exchange = Exchange.Coincheck,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Binance"/>.
		/// </summary>
		public static ExchangeBoard Binance { get; } = new ExchangeBoard
		{
			Code = Exchange.Binance.Name,
			Exchange = Exchange.Binance,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Bitexbook"/>.
		/// </summary>
		public static ExchangeBoard Bitexbook { get; } = new ExchangeBoard
		{
			Code = Exchange.Bitexbook.Name,
			Exchange = Exchange.Bitexbook,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Bitmex"/>.
		/// </summary>
		public static ExchangeBoard Bitmex { get; } = new ExchangeBoard
		{
			Code = Exchange.Bitmex.Name,
			Exchange = Exchange.Bitmex,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Cex"/>.
		/// </summary>
		public static ExchangeBoard Cex { get; } = new ExchangeBoard
		{
			Code = Exchange.Cex.Name,
			Exchange = Exchange.Cex,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Cryptopia"/>.
		/// </summary>
		public static ExchangeBoard Cryptopia { get; } = new ExchangeBoard
		{
			Code = Exchange.Cryptopia.Name,
			Exchange = Exchange.Cryptopia,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Okex"/>.
		/// </summary>
		public static ExchangeBoard Okex { get; } = new ExchangeBoard
		{
			Code = Exchange.Okex.Name,
			Exchange = Exchange.Okex,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Yobit"/>.
		/// </summary>
		public static ExchangeBoard Yobit { get; } = new ExchangeBoard
		{
			Code = Exchange.Yobit.Name,
			Exchange = Exchange.Yobit,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.CoinExchange"/>.
		/// </summary>
		public static ExchangeBoard CoinExchange { get; } = new ExchangeBoard
		{
			Code = Exchange.CoinExchange.Name,
			Exchange = Exchange.CoinExchange,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.LiveCoin"/>.
		/// </summary>
		public static ExchangeBoard LiveCoin { get; } = new ExchangeBoard
		{
			Code = Exchange.LiveCoin.Name,
			Exchange = Exchange.LiveCoin,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Exmo"/>.
		/// </summary>
		public static ExchangeBoard Exmo { get; } = new ExchangeBoard
		{
			Code = Exchange.Exmo.Name,
			Exchange = Exchange.Exmo,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Deribit"/>.
		/// </summary>
		public static ExchangeBoard Deribit { get; } = new ExchangeBoard
		{
			Code = Exchange.Deribit.Name,
			Exchange = Exchange.Deribit,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Kucoin"/>.
		/// </summary>
		public static ExchangeBoard Kucoin { get; } = new ExchangeBoard
		{
			Code = Exchange.Kucoin.Name,
			Exchange = Exchange.Kucoin,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Liqui"/>.
		/// </summary>
		public static ExchangeBoard Liqui { get; } = new ExchangeBoard
		{
			Code = Exchange.Liqui.Name,
			Exchange = Exchange.Liqui,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Huobi"/>.
		/// </summary>
		public static ExchangeBoard Huobi { get; } = new ExchangeBoard
		{
			Code = Exchange.Huobi.Name,
			Exchange = Exchange.Huobi,
		};

		/// <summary>
		/// Information about Globex board of <see cref="BusinessEntities.Exchange.Cme"/> exchange.
		/// </summary>
		public static ExchangeBoard Globex { get; } = new ExchangeBoard
		{
			Code = "Globex",
			Exchange = Exchange.Cme,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.IEX"/>.
		/// </summary>
		public static ExchangeBoard IEX { get; } = new ExchangeBoard
		{
			Code = Exchange.IEX.Name,
			Exchange = Exchange.IEX,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Bitbank"/>.
		/// </summary>
		public static ExchangeBoard Bitbank { get; } = new ExchangeBoard
		{
			Code = Exchange.Bitbank.Name,
			Exchange = Exchange.Bitbank,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Zaif"/>.
		/// </summary>
		public static ExchangeBoard Zaif { get; } = new ExchangeBoard
		{
			Code = Exchange.Zaif.Name,
			Exchange = Exchange.Zaif,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Quoinex"/>.
		/// </summary>
		public static ExchangeBoard Quoinex { get; } = new ExchangeBoard
		{
			Code = Exchange.Quoinex.Name,
			Exchange = Exchange.Quoinex,
		};

		/// <summary>
		/// Information about board <see cref="BusinessEntities.Exchange.Wiki"/>.
		/// </summary>
		public static ExchangeBoard Wiki { get; } = new ExchangeBoard
		{
			Code = Exchange.Wiki.Name,
			Exchange = Exchange.Wiki,
		};
	}
}
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
	using Ecng.Serialization;

	using StockSharp.Messages;

	partial class ExchangeBoard
	{
		static ExchangeBoard()
		{
			// NOTE

			Associated = new ExchangeBoard
			{
				Code = SecurityId.AssociatedBoardCode,
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
					IsEnabled = true,
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
				IsEnabled = true,
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
					IsEnabled = true,
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
				TimeZone = TimeHelper.Fle,
			};

			UxStock = new ExchangeBoard
			{
				Code = "GTS",
				WorkingTime = new WorkingTime
				{
					IsEnabled = true,
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
				TimeZone = TimeHelper.Fle,
			};

			Amex = new ExchangeBoard
			{
				Code = "AMEX",
				WorkingTime = new WorkingTime
				{
					IsEnabled = true,
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
				TimeZone = TimeHelper.Est,
				Exchange = Exchange.Amex
			};

			Cme = new ExchangeBoard
			{
				Code = "CME",
				TimeZone = TimeHelper.Cst,
				Exchange = Exchange.Cme,
			};

			CmeMini = new ExchangeBoard
			{
				Code = "CMEMINI",
				TimeZone = TimeHelper.Cst,
				Exchange = Exchange.Cme,
			};

			Cbot = new ExchangeBoard
			{
				Code = "CBOT",
				TimeZone = TimeHelper.Cst,
				Exchange = Exchange.Cbot,
			};

			Cce = new ExchangeBoard
			{
				Code = "CCE",
				TimeZone = TimeHelper.Cst,
				Exchange = Exchange.Cce,
			};

			Nyse = new ExchangeBoard
			{
				Code = "NYSE",
				WorkingTime = new WorkingTime
				{
					IsEnabled = true,
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
				TimeZone = TimeHelper.Est,
				Exchange = Exchange.Nyse
			};

			Nymex = new ExchangeBoard
			{
				Code = "NYMEX",
				TimeZone = TimeHelper.Est,
				Exchange = Exchange.Nymex,
			};

			Nasdaq = new ExchangeBoard
			{
				Code = "NASDAQ",
				WorkingTime = new WorkingTime
				{
					IsEnabled = true,
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
				TimeZone = TimeHelper.Est,
			};

			Nqlx = new ExchangeBoard
			{
				Code = "NQLX",
				Exchange = Exchange.Nqlx,
				TimeZone = TimeHelper.Est,
			};

			Tsx = new ExchangeBoard
			{
				Code = "TSX",
				WorkingTime = new WorkingTime
				{
					IsEnabled = true,
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
				TimeZone = TimeHelper.Est,
			};

			Lse = new ExchangeBoard
			{
				Code = "LSE",
				WorkingTime = new WorkingTime
				{
					IsEnabled = true,
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
				TimeZone = TimeHelper.Gmt,
			};

			Lme = new ExchangeBoard
			{
				Code = "LME",
				WorkingTime = new WorkingTime
				{
					IsEnabled = true,
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
				TimeZone = TimeHelper.Gmt,
			};

			Tse = new ExchangeBoard
			{
				Code = "TSE",
				WorkingTime = new WorkingTime
				{
					IsEnabled = true,
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
				TimeZone = TimeHelper.Tokyo,
			};

			Hkex = new ExchangeBoard
			{
				Code = "HKEX",
				WorkingTime = new WorkingTime
				{
					IsEnabled = true,
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
				TimeZone = TimeHelper.China,
			};

			Hkfe = new ExchangeBoard
			{
				Code = "HKFE",
				WorkingTime = new WorkingTime
				{
					IsEnabled = true,
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
				TimeZone = TimeHelper.China,
			};

			Sse = new ExchangeBoard
			{
				Code = "SSE",
				WorkingTime = new WorkingTime
				{
					IsEnabled = true,
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
				TimeZone = TimeHelper.China,
			};

			Szse = new ExchangeBoard
			{
				Code = "SZSE",
				WorkingTime = new WorkingTime
				{
					IsEnabled = true,
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
				TimeZone = TimeHelper.China,
			};

			Tsec = new ExchangeBoard
			{
				Code = "TSEC",
				WorkingTime = new WorkingTime
				{
					IsEnabled = true,
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
				TimeZone = TimeHelper.China,
			};

			var singaporeTime = "Singapore Standard Time".To<TimeZoneInfo>();

			Sgx = new ExchangeBoard
			{
				Code = "SGX",
				WorkingTime = new WorkingTime
				{
					IsEnabled = true,
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
					IsEnabled = true,
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
					IsEnabled = true,
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

			var bangkokTime = "SE Asia Standard Time".To<TimeZoneInfo>();

			Idx = new ExchangeBoard
			{
				Code = "IDX",
				WorkingTime = new WorkingTime
				{
					IsEnabled = true,
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
					IsEnabled = true,
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

			var indiaTime = "India Standard Time".To<TimeZoneInfo>();

			Bse = new ExchangeBoard
			{
				Code = "BSE",
				WorkingTime = new WorkingTime
				{
					IsEnabled = true,
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
					IsEnabled = true,
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
					IsEnabled = true,
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
				TimeZone = "Sri Lanka Standard Time".To<TimeZoneInfo>(),
			};

			Krx = new ExchangeBoard
			{
				Code = "KRX",
				WorkingTime = new WorkingTime
				{
					IsEnabled = true,
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
				TimeZone = TimeHelper.Korea,
			};

			Asx = new ExchangeBoard
			{
				Code = "ASX",
				WorkingTime = new WorkingTime
				{
					IsEnabled = true,
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
				TimeZone = "AUS Eastern Standard Time".To<TimeZoneInfo>(),
			};

			Nzx = new ExchangeBoard
			{
				Code = "NZX",
				WorkingTime = new WorkingTime
				{
					IsEnabled = true,
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
				TimeZone = "New Zealand Standard Time".To<TimeZoneInfo>(),
			};

			Tase = new ExchangeBoard
			{
				Code = "TASE",
				WorkingTime = new WorkingTime
				{
					IsEnabled = true,
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
				TimeZone = "Israel Standard Time".To<TimeZoneInfo>(),
			};

			Fwb = new ExchangeBoard
			{
				Code = "FWB",
				WorkingTime = new WorkingTime
				{
					IsEnabled = true,
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
				TimeZone = "W. Europe Standard Time".To<TimeZoneInfo>(),
			};

			Mse = new ExchangeBoard
			{
				Code = "MSE",
				WorkingTime = new WorkingTime
				{
					IsEnabled = true,
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
				TimeZone = "Romance Standard Time".To<TimeZoneInfo>(),
			};

			Swx = new ExchangeBoard
			{
				Code = "SWX",
				WorkingTime = new WorkingTime
				{
					IsEnabled = true,
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
					IsEnabled = true,
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
					IsEnabled = true,
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
				return id.To<TimeZoneInfo>();
			}
			catch (TimeZoneNotFoundException)
			{
				return TimeZoneInfo.GetSystemTimeZones().First(z => z.BaseUtcOffset == offset);
			}
		}

		/// <summary>
		/// Information about board <see cref="Associated"/>.
		/// </summary>
		public static ExchangeBoard Associated { get; }

		/// <summary>
		/// Information about board <see cref="Test"/>.
		/// </summary>
		public static ExchangeBoard Test { get; }

		/// <summary>
		/// Information about board <see cref="Forts"/>.
		/// </summary>
		public static ExchangeBoard Forts { get; }

		/// <summary>
		/// Information about board <see cref="Micex"/>.
		/// </summary>
		public static ExchangeBoard Micex { get; }

		/// <summary>
		/// Information about board <see cref="MicexAuct"/>.
		/// </summary>
		public static ExchangeBoard MicexAuct { get; }

		/// <summary>
		/// Information about board <see cref="MicexAubb"/>.
		/// </summary>
		public static ExchangeBoard MicexAubb { get; }

		/// <summary>
		/// Information about board <see cref="MicexCasf"/>.
		/// </summary>
		public static ExchangeBoard MicexCasf { get; }

		/// <summary>
		/// Information about board <see cref="MicexEqbr"/>.
		/// </summary>
		public static ExchangeBoard MicexEqbr { get; }

		/// <summary>
		/// Information about board <see cref="MicexEqbs"/>.
		/// </summary>
		public static ExchangeBoard MicexEqbs { get; }

		/// <summary>
		/// Information about board <see cref="MicexEqdp"/>.
		/// </summary>
		public static ExchangeBoard MicexEqdp { get; }

		/// <summary>
		/// Information about board <see cref="MicexEqeu"/>.
		/// </summary>
		public static ExchangeBoard MicexEqeu { get; }

		/// <summary>
		/// Information about board <see cref="MicexEqus"/>.
		/// </summary>
		public static ExchangeBoard MicexEqus { get; }

		/// <summary>
		/// Information about board <see cref="MicexEqnb"/>.
		/// </summary>
		public static ExchangeBoard MicexEqnb { get; }

		/// <summary>
		/// Information about board <see cref="MicexEqne"/>.
		/// </summary>
		public static ExchangeBoard MicexEqne { get; }

		/// <summary>
		/// Information about board <see cref="MicexEqnl"/>.
		/// </summary>
		public static ExchangeBoard MicexEqnl { get; }

		/// <summary>
		/// Information about board <see cref="MicexEqno"/>.
		/// </summary>
		public static ExchangeBoard MicexEqno { get; }

		/// <summary>
		/// Information about board <see cref="MicexEqob"/>.
		/// </summary>
		public static ExchangeBoard MicexEqob { get; }

		/// <summary>
		/// Information about board <see cref="MicexEqos"/>.
		/// </summary>
		public static ExchangeBoard MicexEqos { get; }

		/// <summary>
		/// Information about board <see cref="MicexEqov"/>.
		/// </summary>
		public static ExchangeBoard MicexEqov { get; }

		/// <summary>
		/// Information about board <see cref="MicexEqlv"/>.
		/// </summary>
		public static ExchangeBoard MicexEqlv { get; }

		/// <summary>
		/// Information about board <see cref="MicexEqdb"/>.
		/// </summary>
		public static ExchangeBoard MicexEqdb { get; }

		/// <summary>
		/// Information about board <see cref="MicexEqde"/>.
		/// </summary>
		public static ExchangeBoard MicexEqde { get; }

		/// <summary>
		/// Information about board <see cref="MicexEqli"/>.
		/// </summary>
		public static ExchangeBoard MicexEqli { get; }

		/// <summary>
		/// Information about board <see cref="MicexEqqi"/>.
		/// </summary>
		public static ExchangeBoard MicexEqqi { get; }

		/// <summary>
		/// Information about board <see cref="MicexSmal"/>.
		/// </summary>
		public static ExchangeBoard MicexSmal { get; }

		/// <summary>
		/// Information about board <see cref="MicexSpob"/>.
		/// </summary>
		public static ExchangeBoard MicexSpob { get; }

		/// <summary>
		/// Information about board <see cref="MicexTqbr"/>.
		/// </summary>
		public static ExchangeBoard MicexTqbr { get; }

		/// <summary>
		/// Information about board <see cref="MicexTqde"/>.
		/// </summary>
		public static ExchangeBoard MicexTqde { get; }

		/// <summary>
		/// Information about board <see cref="MicexTqbs"/>.
		/// </summary>
		public static ExchangeBoard MicexTqbs { get; }

		/// <summary>
		/// Information about board <see cref="MicexTqeu"/>.
		/// </summary>
		public static ExchangeBoard MicexTqeu { get; }

		/// <summary>
		/// Information about board <see cref="MicexTqus"/>.
		/// </summary>
		public static ExchangeBoard MicexTqus { get; }

		/// <summary>
		/// Information about board <see cref="MicexTqnb"/>.
		/// </summary>
		public static ExchangeBoard MicexTqnb { get; }

		/// <summary>
		/// Information about board <see cref="MicexTqne"/>.
		/// </summary>
		public static ExchangeBoard MicexTqne { get; }

		/// <summary>
		/// Information about board <see cref="MicexTqnl"/>.
		/// </summary>
		public static ExchangeBoard MicexTqnl { get; }

		/// <summary>
		/// Information about board <see cref="MicexTqno"/>.
		/// </summary>
		public static ExchangeBoard MicexTqno { get; }

		/// <summary>
		/// Information about board <see cref="MicexTqob"/>.
		/// </summary>
		public static ExchangeBoard MicexTqob { get; }

		/// <summary>
		/// Information about board <see cref="MicexTqos"/>.
		/// </summary>
		public static ExchangeBoard MicexTqos { get; }

		/// <summary>
		/// Information about board <see cref="MicexTqov"/>.
		/// </summary>
		public static ExchangeBoard MicexTqov { get; }

		/// <summary>
		/// Information about board <see cref="MicexTqlv"/>.
		/// </summary>
		public static ExchangeBoard MicexTqlv { get; }

		/// <summary>
		/// Information about board <see cref="MicexTqli"/>.
		/// </summary>
		public static ExchangeBoard MicexTqli { get; }

		/// <summary>
		/// Information about board <see cref="MicexTqqi"/>.
		/// </summary>
		public static ExchangeBoard MicexTqqi { get; }

		/// <summary>
		/// Information about board <see cref="MicexEqrp"/>.
		/// </summary>
		public static ExchangeBoard MicexEqrp { get; }

		/// <summary>
		/// Information about board <see cref="MicexPsrp"/>.
		/// </summary>
		public static ExchangeBoard MicexPsrp { get; }

		/// <summary>
		/// Information about board <see cref="MicexRfnd"/>.
		/// </summary>
		public static ExchangeBoard MicexRfnd { get; }

		/// <summary>
		/// Information about board <see cref="MicexTadm"/>.
		/// </summary>
		public static ExchangeBoard MicexTadm { get; }

		/// <summary>
		/// Information about board <see cref="MicexNadm"/>.
		/// </summary>
		public static ExchangeBoard MicexNadm { get; }

		/// <summary>
		/// Information about board <see cref="MicexPsau"/>.
		/// </summary>
		public static ExchangeBoard MicexPsau { get; }

		/// <summary>
		/// Information about board <see cref="MicexPaus"/>.
		/// </summary>
		public static ExchangeBoard MicexPaus { get; }

		/// <summary>
		/// Information about board <see cref="MicexPsbb"/>.
		/// </summary>
		public static ExchangeBoard MicexPsbb { get; }

		/// <summary>
		/// Information about board <see cref="MicexPseq"/>.
		/// </summary>
		public static ExchangeBoard MicexPseq { get; }

		/// <summary>
		/// Information about board <see cref="MicexPses"/>.
		/// </summary>
		public static ExchangeBoard MicexPses { get; }

		/// <summary>
		/// Information about board <see cref="MicexPseu"/>.
		/// </summary>
		public static ExchangeBoard MicexPseu { get; }

		/// <summary>
		/// Information about board <see cref="MicexPsdb"/>.
		/// </summary>
		public static ExchangeBoard MicexPsdb { get; }

		/// <summary>
		/// Information about board <see cref="MicexPsde"/>.
		/// </summary>
		public static ExchangeBoard MicexPsde { get; }

		/// <summary>
		/// Information about board <see cref="MicexPsus"/>.
		/// </summary>
		public static ExchangeBoard MicexPsus { get; }

		/// <summary>
		/// Information about board <see cref="MicexPsnb"/>.
		/// </summary>
		public static ExchangeBoard MicexPsnb { get; }

		/// <summary>
		/// Information about board <see cref="MicexPsne"/>.
		/// </summary>
		public static ExchangeBoard MicexPsne { get; }

		/// <summary>
		/// Information about board <see cref="MicexPsnl"/>.
		/// </summary>
		public static ExchangeBoard MicexPsnl { get; }

		/// <summary>
		/// Information about board <see cref="MicexPsno"/>.
		/// </summary>
		public static ExchangeBoard MicexPsno { get; }

		/// <summary>
		/// Information about board <see cref="MicexPsob"/>.
		/// </summary>
		public static ExchangeBoard MicexPsob { get; }

		/// <summary>
		/// Information about board <see cref="MicexPsos"/>.
		/// </summary>
		public static ExchangeBoard MicexPsos { get; }

		/// <summary>
		/// Information about board <see cref="MicexPsov"/>.
		/// </summary>
		public static ExchangeBoard MicexPsov { get; }

		/// <summary>
		/// Information about board <see cref="MicexPslv"/>.
		/// </summary>
		public static ExchangeBoard MicexPslv { get; }

		/// <summary>
		/// Information about board <see cref="MicexPsli"/>.
		/// </summary>
		public static ExchangeBoard MicexPsli { get; }

		/// <summary>
		/// Information about board <see cref="MicexPsqi"/>.
		/// </summary>
		public static ExchangeBoard MicexPsqi { get; }

		/// <summary>
		/// Information about board <see cref="MicexRpeu"/>.
		/// </summary>
		public static ExchangeBoard MicexRpeu { get; }

		/// <summary>
		/// Information about board <see cref="MicexRpma"/>.
		/// </summary>
		public static ExchangeBoard MicexRpma { get; }

		/// <summary>
		/// Information about board <see cref="MicexRpmo"/>.
		/// </summary>
		public static ExchangeBoard MicexRpmo { get; }

		/// <summary>
		/// Information about board <see cref="MicexRpua"/>.
		/// </summary>
		public static ExchangeBoard MicexRpua { get; }

		/// <summary>
		/// Information about board <see cref="MicexRpuo"/>.
		/// </summary>
		public static ExchangeBoard MicexRpuo { get; }

		/// <summary>
		/// Information about board <see cref="MicexRpuq"/>.
		/// </summary>
		public static ExchangeBoard MicexRpuq { get; }

		/// <summary>
		/// Information about board <see cref="MicexFbcb"/>.
		/// </summary>
		public static ExchangeBoard MicexFbcb { get; }

		/// <summary>
		/// Information about board <see cref="MicexFbfx"/>.
		/// </summary>
		public static ExchangeBoard MicexFbfx { get; }

		/// <summary>
		/// Information about board <see cref="MicexIrk2"/>.
		/// </summary>
		public static ExchangeBoard MicexIrk2 { get; }

		/// <summary>
		/// Information about board <see cref="MicexRpqi"/>.
		/// </summary>
		public static ExchangeBoard MicexRpqi { get; }

		/// <summary>
		/// Information about board <see cref="MicexPteq"/>.
		/// </summary>
		public static ExchangeBoard MicexPteq { get; }

		/// <summary>
		/// Information about board <see cref="MicexPtes"/>.
		/// </summary>
		public static ExchangeBoard MicexPtes { get; }

		/// <summary>
		/// Information about board <see cref="MicexPteu"/>.
		/// </summary>
		public static ExchangeBoard MicexPteu { get; }

		/// <summary>
		/// Information about board <see cref="MicexPtus"/>.
		/// </summary>
		public static ExchangeBoard MicexPtus { get; }

		/// <summary>
		/// Information about board <see cref="MicexPtnb"/>.
		/// </summary>
		public static ExchangeBoard MicexPtnb { get; }

		/// <summary>
		/// Information about board <see cref="MicexPtne"/>.
		/// </summary>
		public static ExchangeBoard MicexPtne { get; }

		/// <summary>
		/// Information about board <see cref="MicexPtnl"/>.
		/// </summary>
		public static ExchangeBoard MicexPtnl { get; }

		/// <summary>
		/// Information about board <see cref="MicexPtno"/>.
		/// </summary>
		public static ExchangeBoard MicexPtno { get; }

		/// <summary>
		/// Information about board <see cref="MicexPtob"/>.
		/// </summary>
		public static ExchangeBoard MicexPtob { get; }

		/// <summary>
		/// Information about board <see cref="MicexPtos"/>.
		/// </summary>
		public static ExchangeBoard MicexPtos { get; }

		/// <summary>
		/// Information about board <see cref="MicexPtov"/>.
		/// </summary>
		public static ExchangeBoard MicexPtov { get; }

		/// <summary>
		/// Information about board <see cref="MicexPtlv"/>.
		/// </summary>
		public static ExchangeBoard MicexPtlv { get; }

		/// <summary>
		/// Information about board <see cref="MicexPtli"/>.
		/// </summary>
		public static ExchangeBoard MicexPtli { get; }

		/// <summary>
		/// Information about board <see cref="MicexPtqi"/>.
		/// </summary>
		public static ExchangeBoard MicexPtqi { get; }

		/// <summary>
		/// Information about board <see cref="MicexScvc"/>.
		/// </summary>
		public static ExchangeBoard MicexScvc { get; }

		/// <summary>
		/// Information about board <see cref="MicexRpng"/>.
		/// </summary>
		public static ExchangeBoard MicexRpng { get; }

		/// <summary>
		/// Information about board <see cref="MicexRpfg"/>.
		/// </summary>
		public static ExchangeBoard MicexRpfg { get; }

		/// <summary>
		/// Information about board <see cref="MicexCbcr"/>.
		/// </summary>
		public static ExchangeBoard MicexCbcr { get; }

		/// <summary>
		/// Information about board <see cref="MicexCred"/>.
		/// </summary>
		public static ExchangeBoard MicexCred { get; }

		/// <summary>
		/// Information about board <see cref="MicexDepz"/>.
		/// </summary>
		public static ExchangeBoard MicexDepz { get; }

		/// <summary>
		/// Information about board <see cref="MicexDpvb"/>.
		/// </summary>
		public static ExchangeBoard MicexDpvb { get; }

		/// <summary>
		/// Information about board <see cref="MicexDpfk"/>.
		/// </summary>
		public static ExchangeBoard MicexDpfk { get; }

		/// <summary>
		/// Information about board <see cref="MicexDpfo"/>.
		/// </summary>
		public static ExchangeBoard MicexDpfo { get; }

		/// <summary>
		/// Information about board <see cref="MicexDppf"/>.
		/// </summary>
		public static ExchangeBoard MicexDppf { get; }

		/// <summary>
		/// Information about board <see cref="MicexCets"/>.
		/// </summary>
		public static ExchangeBoard MicexCets { get; }

		/// <summary>
		/// Information about board <see cref="MicexAets"/>.
		/// </summary>
		public static ExchangeBoard MicexAets { get; }

		/// <summary>
		/// Information about board <see cref="MicexCngd"/>.
		/// </summary>
		public static ExchangeBoard MicexCngd { get; }

		/// <summary>
		/// Information about board <see cref="MicexTran"/>.
		/// </summary>
		public static ExchangeBoard MicexTran { get; }

		/// <summary>
		/// Information about board <see cref="MicexJunior"/>.
		/// </summary>
		public static ExchangeBoard MicexJunior { get; }

		/// <summary>
		/// Information about board <see cref="Spb"/>.
		/// </summary>
		public static ExchangeBoard Spb { get; }

		/// <summary>
		/// Information about board <see cref="Ux"/>.
		/// </summary>
		public static ExchangeBoard Ux { get; }

		/// <summary>
		/// Information about board <see cref="UxStock"/>.
		/// </summary>
		public static ExchangeBoard UxStock { get; }

		/// <summary>
		/// Information about board <see cref="Cme"/>.
		/// </summary>
		public static ExchangeBoard Cme { get; }

		/// <summary>
		/// Information about board <see cref="Cme"/>.
		/// </summary>
		public static ExchangeBoard CmeMini { get; }

		/// <summary>
		/// Information about board <see cref="Cce"/>.
		/// </summary>
		public static ExchangeBoard Cce { get; }

		/// <summary>
		/// Information about board <see cref="Cbot"/>.
		/// </summary>
		public static ExchangeBoard Cbot { get; }

		/// <summary>
		/// Information about board <see cref="Nymex"/>.
		/// </summary>
		public static ExchangeBoard Nymex { get; }

		/// <summary>
		/// Information about board <see cref="Amex"/>.
		/// </summary>
		public static ExchangeBoard Amex { get; }

		/// <summary>
		/// Information about board <see cref="Nyse"/>.
		/// </summary>
		public static ExchangeBoard Nyse { get; }

		/// <summary>
		/// Information about board <see cref="Nasdaq"/>.
		/// </summary>
		public static ExchangeBoard Nasdaq { get; }

		/// <summary>
		/// Information about board <see cref="Nqlx"/>.
		/// </summary>
		public static ExchangeBoard Nqlx { get; }

		/// <summary>
		/// Information about board <see cref="Lse"/>.
		/// </summary>
		public static ExchangeBoard Lse { get; }

		/// <summary>
		/// Information about board <see cref="Lme"/>.
		/// </summary>
		public static ExchangeBoard Lme { get; }

		/// <summary>
		/// Information about board <see cref="Tse"/>.
		/// </summary>
		public static ExchangeBoard Tse { get; }

		/// <summary>
		/// Information about board <see cref="Hkex"/>.
		/// </summary>
		public static ExchangeBoard Hkex { get; }

		/// <summary>
		/// Information about board <see cref="Hkfe"/>.
		/// </summary>
		public static ExchangeBoard Hkfe { get; }

		/// <summary>
		/// Information about board <see cref="Sse"/>.
		/// </summary>
		public static ExchangeBoard Sse { get; }

		/// <summary>
		/// Information about board <see cref="Szse"/>.
		/// </summary>
		public static ExchangeBoard Szse { get; }

		/// <summary>
		/// Information about board <see cref="Tsx"/>.
		/// </summary>
		public static ExchangeBoard Tsx { get; }

		/// <summary>
		/// Information about board <see cref="Fwb"/>.
		/// </summary>
		public static ExchangeBoard Fwb { get; }

		/// <summary>
		/// Information about board <see cref="Asx"/>.
		/// </summary>
		public static ExchangeBoard Asx { get; }

		/// <summary>
		/// Information about board <see cref="Nzx"/>.
		/// </summary>
		public static ExchangeBoard Nzx { get; }

		/// <summary>
		/// Information about board <see cref="Bse"/>.
		/// </summary>
		public static ExchangeBoard Bse { get; }

		/// <summary>
		/// Information about board <see cref="Nse"/>.
		/// </summary>
		public static ExchangeBoard Nse { get; }

		/// <summary>
		/// Information about board <see cref="Swx"/>.
		/// </summary>
		public static ExchangeBoard Swx { get; }

		/// <summary>
		/// Information about board <see cref="Krx"/>.
		/// </summary>
		public static ExchangeBoard Krx { get; }

		/// <summary>
		/// Information about board <see cref="Mse"/>.
		/// </summary>
		public static ExchangeBoard Mse { get; }

		/// <summary>
		/// Information about board <see cref="Jse"/>.
		/// </summary>
		public static ExchangeBoard Jse { get; }

		/// <summary>
		/// Information about board <see cref="Sgx"/>.
		/// </summary>
		public static ExchangeBoard Sgx { get; }

		/// <summary>
		/// Information about board <see cref="Tsec"/>.
		/// </summary>
		public static ExchangeBoard Tsec { get; }

		/// <summary>
		/// Information about board <see cref="Pse"/>.
		/// </summary>
		public static ExchangeBoard Pse { get; }

		/// <summary>
		/// Information about board <see cref="Klse"/>.
		/// </summary>
		public static ExchangeBoard Klse { get; }

		/// <summary>
		/// Information about board <see cref="Idx"/>.
		/// </summary>
		public static ExchangeBoard Idx { get; }

		/// <summary>
		/// Information about board <see cref="Set"/>.
		/// </summary>
		public static ExchangeBoard Set { get; }

		/// <summary>
		/// Information about board <see cref="Cse"/>.
		/// </summary>
		public static ExchangeBoard Cse { get; }

		/// <summary>
		/// Information about board <see cref="Tase"/>.
		/// </summary>
		public static ExchangeBoard Tase { get; }

		/// <summary>
		/// Information about board <see cref="Lmax"/>.
		/// </summary>
		public static ExchangeBoard Lmax { get; }

		/// <summary>
		/// Information about board <see cref="DukasCopy"/>.
		/// </summary>
		public static ExchangeBoard DukasCopy { get; }

		/// <summary>
		/// Information about board <see cref="GainCapital"/>.
		/// </summary>
		public static ExchangeBoard GainCapital { get; }

		/// <summary>
		/// Information about board <see cref="MBTrading"/>.
		/// </summary>
		public static ExchangeBoard MBTrading { get; }

		/// <summary>
		/// Information about board <see cref="TrueFX"/>.
		/// </summary>
		public static ExchangeBoard TrueFX { get; }

		/// <summary>
		/// Information about board <see cref="Integral"/>.
		/// </summary>
		public static ExchangeBoard Integral { get; }

		/// <summary>
		/// Information about board <see cref="Cfh"/>.
		/// </summary>
		public static ExchangeBoard Cfh { get; }

		/// <summary>
		/// Information about board <see cref="Ond"/>.
		/// </summary>
		public static ExchangeBoard Ond { get; }

		/// <summary>
		/// Information about board <see cref="Nasdaq"/>.
		/// </summary>
		public static ExchangeBoard Smart { get; }

		/// <summary>
		/// Information about board <see cref="Btce"/>.
		/// </summary>
		public static ExchangeBoard Btce { get; }

		/// <summary>
		/// Information about board <see cref="BitStamp"/>.
		/// </summary>
		public static ExchangeBoard BitStamp { get; }

		/// <summary>
		/// Information about board <see cref="BtcChina"/>.
		/// </summary>
		public static ExchangeBoard BtcChina { get; }

		/// <summary>
		/// Information about board <see cref="Icbit"/>.
		/// </summary>
		public static ExchangeBoard Icbit { get; }

		/// <summary>
		/// Information about board <see cref="Finam"/>.
		/// </summary>
		public static ExchangeBoard Finam { get; }

		/// <summary>
		/// Information about board <see cref="Mfd"/>.
		/// </summary>
		public static ExchangeBoard Mfd { get; }

		/// <summary>
		/// Information about board <see cref="Arca"/>.
		/// </summary>
		public static ExchangeBoard Arca { get; } = new ExchangeBoard
		{
			Code = "ARCA",
			Exchange = Exchange.Nyse,
		};

		/// <summary>
		/// Information about board <see cref="Bats"/>.
		/// </summary>
		public static ExchangeBoard Bats { get; } = new ExchangeBoard
		{
			Code = "BATS",
			Exchange = Exchange.Cbot,
		};

		/// <summary>
		/// Information about board <see cref="Currenex"/>.
		/// </summary>
		public static ExchangeBoard Currenex { get; } = new ExchangeBoard
		{
			Code = Exchange.Currenex.Name,
			Exchange = Exchange.Currenex,
		};

		/// <summary>
		/// Information about board <see cref="Fxcm"/>.
		/// </summary>
		public static ExchangeBoard Fxcm { get; } = new ExchangeBoard
		{
			Code = Exchange.Fxcm.Name,
			Exchange = Exchange.Fxcm,
		};

		/// <summary>
		/// Information about board <see cref="Poloniex"/>.
		/// </summary>
		public static ExchangeBoard Poloniex { get; } = new ExchangeBoard
		{
			Code = Exchange.Poloniex.Name,
			Exchange = Exchange.Poloniex,
		};

		/// <summary>
		/// Information about board <see cref="Kraken"/>.
		/// </summary>
		public static ExchangeBoard Kraken { get; } = new ExchangeBoard
		{
			Code = Exchange.Kraken.Name,
			Exchange = Exchange.Kraken,
		};

		/// <summary>
		/// Information about board <see cref="Bittrex"/>.
		/// </summary>
		public static ExchangeBoard Bittrex { get; } = new ExchangeBoard
		{
			Code = Exchange.Bittrex.Name,
			Exchange = Exchange.Bittrex,
		};

		/// <summary>
		/// Information about board <see cref="Bitfinex"/>.
		/// </summary>
		public static ExchangeBoard Bitfinex { get; } = new ExchangeBoard
		{
			Code = Exchange.Bitfinex.Name,
			Exchange = Exchange.Bitfinex,
		};

		/// <summary>
		/// Information about board <see cref="Coinbase"/>.
		/// </summary>
		public static ExchangeBoard Coinbase { get; } = new ExchangeBoard
		{
			Code = Exchange.Coinbase.Name,
			Exchange = Exchange.Coinbase,
		};

		/// <summary>
		/// Information about board <see cref="Gdax"/>.
		/// </summary>
		public static ExchangeBoard Gdax { get; } = new ExchangeBoard
		{
			Code = Exchange.Gdax.Name,
			Exchange = Exchange.Gdax,
		};

		/// <summary>
		/// Information about board <see cref="Bithumb"/>.
		/// </summary>
		public static ExchangeBoard Bithumb { get; } = new ExchangeBoard
		{
			Code = Exchange.Bithumb.Name,
			Exchange = Exchange.Bithumb,
		};

		/// <summary>
		/// Information about board <see cref="HitBtc"/>.
		/// </summary>
		public static ExchangeBoard HitBtc { get; } = new ExchangeBoard
		{
			Code = Exchange.HitBtc.Name,
			Exchange = Exchange.HitBtc,
		};

		/// <summary>
		/// Information about board <see cref="OkCoin"/>.
		/// </summary>
		public static ExchangeBoard OkCoin { get; } = new ExchangeBoard
		{
			Code = Exchange.OkCoin.Name,
			Exchange = Exchange.OkCoin,
		};

		/// <summary>
		/// Information about board <see cref="Coincheck"/>.
		/// </summary>
		public static ExchangeBoard Coincheck { get; } = new ExchangeBoard
		{
			Code = Exchange.Coincheck.Name,
			Exchange = Exchange.Coincheck,
		};

		/// <summary>
		/// Information about board <see cref="Binance"/>.
		/// </summary>
		public static ExchangeBoard Binance { get; } = new ExchangeBoard
		{
			Code = Exchange.Binance.Name,
			Exchange = Exchange.Binance,
		};

		/// <summary>
		/// Information about board <see cref="BinanceCoin"/>.
		/// </summary>
		public static ExchangeBoard BinanceCoin { get; } = new ExchangeBoard
		{
			Code = Exchange.Binance.Name + "CN",
			Exchange = Exchange.Binance,
		};

		/// <summary>
		/// Information about board <see cref="Bitexbook"/>.
		/// </summary>
		public static ExchangeBoard Bitexbook { get; } = new ExchangeBoard
		{
			Code = Exchange.Bitexbook.Name,
			Exchange = Exchange.Bitexbook,
		};

		/// <summary>
		/// Information about board <see cref="Bitmex"/>.
		/// </summary>
		public static ExchangeBoard Bitmex { get; } = new ExchangeBoard
		{
			Code = Exchange.Bitmex.Name,
			Exchange = Exchange.Bitmex,
		};

		/// <summary>
		/// Information about board <see cref="Cex"/>.
		/// </summary>
		public static ExchangeBoard Cex { get; } = new ExchangeBoard
		{
			Code = Exchange.Cex.Name,
			Exchange = Exchange.Cex,
		};

		/// <summary>
		/// Information about board <see cref="Cryptopia"/>.
		/// </summary>
		public static ExchangeBoard Cryptopia { get; } = new ExchangeBoard
		{
			Code = Exchange.Cryptopia.Name,
			Exchange = Exchange.Cryptopia,
		};

		/// <summary>
		/// Information about board <see cref="Okex"/>.
		/// </summary>
		public static ExchangeBoard Okex { get; } = new ExchangeBoard
		{
			Code = Exchange.Okex.Name,
			Exchange = Exchange.Okex,
		};

		/// <summary>
		/// Information about board <see cref="Yobit"/>.
		/// </summary>
		public static ExchangeBoard Yobit { get; } = new ExchangeBoard
		{
			Code = Exchange.Yobit.Name,
			Exchange = Exchange.Yobit,
		};

		/// <summary>
		/// Information about board <see cref="CoinExchange"/>.
		/// </summary>
		public static ExchangeBoard CoinExchange { get; } = new ExchangeBoard
		{
			Code = Exchange.CoinExchange.Name,
			Exchange = Exchange.CoinExchange,
		};

		/// <summary>
		/// Information about board <see cref="LiveCoin"/>.
		/// </summary>
		public static ExchangeBoard LiveCoin { get; } = new ExchangeBoard
		{
			Code = Exchange.LiveCoin.Name,
			Exchange = Exchange.LiveCoin,
		};

		/// <summary>
		/// Information about board <see cref="Exmo"/>.
		/// </summary>
		public static ExchangeBoard Exmo { get; } = new ExchangeBoard
		{
			Code = Exchange.Exmo.Name,
			Exchange = Exchange.Exmo,
		};

		/// <summary>
		/// Information about board <see cref="Deribit"/>.
		/// </summary>
		public static ExchangeBoard Deribit { get; } = new ExchangeBoard
		{
			Code = Exchange.Deribit.Name,
			Exchange = Exchange.Deribit,
		};

		/// <summary>
		/// Information about board <see cref="Kucoin"/>.
		/// </summary>
		public static ExchangeBoard Kucoin { get; } = new ExchangeBoard
		{
			Code = Exchange.Kucoin.Name,
			Exchange = Exchange.Kucoin,
		};

		/// <summary>
		/// Information about board <see cref="Liqui"/>.
		/// </summary>
		public static ExchangeBoard Liqui { get; } = new ExchangeBoard
		{
			Code = Exchange.Liqui.Name,
			Exchange = Exchange.Liqui,
		};

		/// <summary>
		/// Information about board <see cref="Huobi"/>.
		/// </summary>
		public static ExchangeBoard Huobi { get; } = new ExchangeBoard
		{
			Code = Exchange.Huobi.Name,
			Exchange = Exchange.Huobi,
		};

		/// <summary>
		/// Information about board <see cref="Globex"/>.
		/// </summary>
		public static ExchangeBoard Globex { get; } = new ExchangeBoard
		{
			Code = "Globex",
			Exchange = Exchange.Cme,
		};

		/// <summary>
		/// Information about board <see cref="IEX"/>.
		/// </summary>
		public static ExchangeBoard IEX { get; } = new ExchangeBoard
		{
			Code = Exchange.IEX.Name,
			Exchange = Exchange.IEX,
		};

		/// <summary>
		/// Information about board <see cref="AlphaVantage"/>.
		/// </summary>
		public static ExchangeBoard AlphaVantage { get; } = new ExchangeBoard
		{
			Code = Exchange.AlphaVantage.Name,
			Exchange = Exchange.AlphaVantage,
		};

		/// <summary>
		/// Information about board <see cref="Bitbank"/>.
		/// </summary>
		public static ExchangeBoard Bitbank { get; } = new ExchangeBoard
		{
			Code = Exchange.Bitbank.Name,
			Exchange = Exchange.Bitbank,
		};

		/// <summary>
		/// Information about board <see cref="Zaif"/>.
		/// </summary>
		public static ExchangeBoard Zaif { get; } = new ExchangeBoard
		{
			Code = Exchange.Zaif.Name,
			Exchange = Exchange.Zaif,
		};

		/// <summary>
		/// Information about board <see cref="Quoinex"/>.
		/// </summary>
		public static ExchangeBoard Quoinex { get; } = new ExchangeBoard
		{
			Code = Exchange.Quoinex.Name,
			Exchange = Exchange.Quoinex,
		};

		/// <summary>
		/// Information about board <see cref="Wiki"/>.
		/// </summary>
		public static ExchangeBoard Wiki { get; } = new ExchangeBoard
		{
			Code = Exchange.Wiki.Name,
			Exchange = Exchange.Wiki,
		};

		/// <summary>
		/// Information about board <see cref="Idax"/>.
		/// </summary>
		public static ExchangeBoard Idax { get; } = new ExchangeBoard
		{
			Code = Exchange.Idax.Name,
			Exchange = Exchange.Idax,
		};

		/// <summary>
		/// Information about board <see cref="Digifinex"/>.
		/// </summary>
		public static ExchangeBoard Digifinex { get; } = new ExchangeBoard
		{
			Code = Exchange.Digifinex.Name,
			Exchange = Exchange.Digifinex,
		};

		/// <summary>
		/// Information about board <see cref="TradeOgre"/>.
		/// </summary>
		public static ExchangeBoard TradeOgre { get; } = new ExchangeBoard
		{
			Code = Exchange.TradeOgre.Name,
			Exchange = Exchange.TradeOgre,
		};

		/// <summary>
		/// Information about board <see cref="CoinCap"/>.
		/// </summary>
		public static ExchangeBoard CoinCap { get; } = new ExchangeBoard
		{
			Code = Exchange.CoinCap.Name,
			Exchange = Exchange.CoinCap,
		};

		/// <summary>
		/// Information about board <see cref="Coinigy"/>.
		/// </summary>
		public static ExchangeBoard Coinigy { get; } = new ExchangeBoard
		{
			Code = Exchange.Coinigy.Name,
			Exchange = Exchange.Coinigy,
		};

		/// <summary>
		/// Information about board <see cref="LBank"/>.
		/// </summary>
		public static ExchangeBoard LBank { get; } = new ExchangeBoard
		{
			Code = Exchange.LBank.Name,
			Exchange = Exchange.LBank,
		};

		/// <summary>
		/// Information about board <see cref="BitMax"/>.
		/// </summary>
		public static ExchangeBoard BitMax { get; } = new ExchangeBoard
		{
			Code = Exchange.BitMax.Name,
			Exchange = Exchange.BitMax,
		};

		/// <summary>
		/// Information about board <see cref="BW"/>.
		/// </summary>
		public static ExchangeBoard BW { get; } = new ExchangeBoard
		{
			Code = Exchange.BW.Name,
			Exchange = Exchange.BW,
		};

		/// <summary>
		/// Information about board <see cref="Bibox"/>.
		/// </summary>
		public static ExchangeBoard Bibox { get; } = new ExchangeBoard
		{
			Code = Exchange.Bibox.Name,
			Exchange = Exchange.Bibox,
		};

		/// <summary>
		/// Information about board <see cref="CoinBene"/>.
		/// </summary>
		public static ExchangeBoard CoinBene { get; } = new ExchangeBoard
		{
			Code = Exchange.CoinBene.Name,
			Exchange = Exchange.CoinBene,
		};

		/// <summary>
		/// Information about board <see cref="BitZ"/>.
		/// </summary>
		public static ExchangeBoard BitZ { get; } = new ExchangeBoard
		{
			Code = Exchange.BitZ.Name,
			Exchange = Exchange.BitZ,
		};

		/// <summary>
		/// Information about board <see cref="ZB"/>.
		/// </summary>
		public static ExchangeBoard ZB { get; } = new ExchangeBoard
		{
			Code = Exchange.ZB.Name,
			Exchange = Exchange.ZB,
		};

		/// <summary>
		/// Information about board <see cref="Tradier"/>.
		/// </summary>
		public static ExchangeBoard Tradier { get; } = new ExchangeBoard
		{
			Code = Exchange.Tradier.Name,
			Exchange = Exchange.Tradier,
		};

		/// <summary>
		/// Information about board <see cref="SwSq"/>.
		/// </summary>
		public static ExchangeBoard SwSq { get; } = new ExchangeBoard
		{
			Code = Exchange.SwSq.Name,
			Exchange = Exchange.SwSq,
		};

		/// <summary>
		/// Information about board <see cref="StockSharp"/>.
		/// </summary>
		public static ExchangeBoard StockSharp { get; } = new ExchangeBoard
		{
			Code = Exchange.StockSharp.Name,
			Exchange = Exchange.StockSharp,
		};

		/// <summary>
		/// Information about board <see cref="Upbit"/>.
		/// </summary>
		public static ExchangeBoard Upbit { get; } = new ExchangeBoard
		{
			Code = Exchange.Upbit.Name,
			Exchange = Exchange.Upbit,
		};
		
		/// <summary>
		/// Information about board <see cref="CoinEx"/>.
		/// </summary>
		public static ExchangeBoard CoinEx { get; } = new ExchangeBoard
		{
			Code = Exchange.CoinEx.Name,
			Exchange = Exchange.CoinEx,
		};

		/// <summary>
		/// Information about board <see cref="FatBtc"/>.
		/// </summary>
		public static ExchangeBoard FatBtc { get; } = new ExchangeBoard
		{
			Code = Exchange.FatBtc.Name,
			Exchange = Exchange.FatBtc,
		};
		
		/// <summary>
		/// Information about board <see cref="Latoken"/>.
		/// </summary>
		public static ExchangeBoard Latoken { get; } = new ExchangeBoard
		{
			Code = Exchange.Latoken.Name,
			Exchange = Exchange.Latoken,
		};

		/// <summary>
		/// Information about board <see cref="Gopax"/>.
		/// </summary>
		public static ExchangeBoard Gopax { get; } = new ExchangeBoard
		{
			Code = Exchange.Gopax.Name,
			Exchange = Exchange.Gopax,
		};

		/// <summary>
		/// Information about board <see cref="CoinHub"/>.
		/// </summary>
		public static ExchangeBoard CoinHub { get; } = new ExchangeBoard
		{
			Code = Exchange.CoinHub.Name,
			Exchange = Exchange.CoinHub,
		};

		/// <summary>
		/// Information about board <see cref="Hotbit"/>.
		/// </summary>
		public static ExchangeBoard Hotbit { get; } = new ExchangeBoard
		{
			Code = Exchange.Hotbit.Name,
			Exchange = Exchange.Hotbit,
		};

		/// <summary>
		/// Information about board <see cref="Bitalong"/>.
		/// </summary>
		public static ExchangeBoard Bitalong { get; } = new ExchangeBoard
		{
			Code = Exchange.Bitalong.Name,
			Exchange = Exchange.Bitalong,
		};

		/// <summary>
		/// Information about board <see cref="PrizmBit"/>.
		/// </summary>
		public static ExchangeBoard PrizmBit { get; } = new ExchangeBoard
		{
			Code = Exchange.PrizmBit.Name,
			Exchange = Exchange.PrizmBit,
		};

		/// <summary>
		/// Information about board <see cref="DigitexFutures"/>.
		/// </summary>
		public static ExchangeBoard DigitexFutures { get; } = new ExchangeBoard
		{
			Code = Exchange.DigitexFutures.Name,
			Exchange = Exchange.DigitexFutures,
		};

		/// <summary>
		/// Information about board <see cref="Bovespa"/>.
		/// </summary>
		public static ExchangeBoard Bovespa { get; } = new ExchangeBoard
		{
			Code = Exchange.Bovespa.Name,
			Exchange = Exchange.Bovespa,
		};

		/// <summary>
		/// Information about board <see cref="IQFeed"/>.
		/// </summary>
		public static ExchangeBoard IQFeed { get; } = new ExchangeBoard
		{
			Code = Exchange.IQFeed.Name,
			Exchange = Exchange.IQFeed,
		};

		/// <summary>
		/// Information about board <see cref="IBKR"/>.
		/// </summary>
		public static ExchangeBoard IBKR { get; } = new ExchangeBoard
		{
			Code = Exchange.IBKR.Name,
			Exchange = Exchange.IBKR,
		};

		/// <summary>
		/// Information about board <see cref="STSH"/>.
		/// </summary>
		public static ExchangeBoard STSH { get; } = new ExchangeBoard
		{
			Code = Exchange.STSH.Name,
			Exchange = Exchange.STSH,
		};

		/// <summary>
		/// Information about board <see cref="STRLG"/>.
		/// </summary>
		public static ExchangeBoard STRLG { get; } = new ExchangeBoard
		{
			Code = Exchange.STRLG.Name,
			Exchange = Exchange.STRLG,
		};

		/// <summary>
		/// Information about board <see cref="QNDL"/>.
		/// </summary>
		public static ExchangeBoard QNDL { get; } = new ExchangeBoard
		{
			Code = Exchange.QNDL.Name,
			Exchange = Exchange.QNDL,
		};
	}
}
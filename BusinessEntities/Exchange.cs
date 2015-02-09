namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// Информация о бирже.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[Ignore(FieldName = "IsDisposed")]
	[KnownType(typeof(TimeZoneInfo))]
	[KnownType(typeof(TimeZoneInfo.AdjustmentRule))]
	[KnownType(typeof(TimeZoneInfo.AdjustmentRule[]))]
	[KnownType(typeof(TimeZoneInfo.TransitionTime))]
	[KnownType(typeof(DayOfWeek))]
	public class Exchange : Equatable<Exchange>, IExtendableEntity, IPersistable, INotifyPropertyChanged
	{
		static Exchange()
		{
			Test = new Exchange
			{
				Name = "TEST",
				RusName = "Тестовая биржа",
				EngName = "Test Exchange",
				TimeZoneInfo = TimeZoneInfo.Utc,
			};

			Moex = new Exchange
			{
				Name = "MOEX",
				RusName = "Московская биржа",
				EngName = "Moscow Exchange",
				TimeZoneInfo = TimeZoneInfo.FromSerializedString("Russian Standard Time;180;(UTC+03:00) Moscow, St. Petersburg, Volgograd (RTZ 2);Russia TZ 2 Standard Time;Russia TZ 2 Daylight Time;[01:01:0001;12:31:2010;60;[0;02:00:00;3;5;0;];[0;03:00:00;10;5;0;];][01:01:2011;12:31:2011;60;[0;02:00:00;3;5;0;];[0;00:00:00;1;1;6;];][01:01:2014;12:31:2014;60;[0;00:00:00;1;1;3;];[0;02:00:00;10;5;0;];];"),
				CountryCode = CountryCodes.RU,
			};

			Ux = new Exchange
			{
				Name = "UX",
				RusName = "Украинская биржа",
				EngName = "Ukrain Exchange",
				TimeZoneInfo = TimeZoneInfo.FromSerializedString("FLE Standard Time;120;(GMT+02:00) Helsinki, Kyiv, Riga, Sofia, Tallinn, Vilnius;FLE Standard Time;FLE Daylight Time;[01:01:0001;12:31:9999;60;[0;03:00:00;3;5;0;];[0;04:00:00;10;5;0;];];"),
				CountryCode = CountryCodes.UA,
			};

			var newYorkTime = TimeZoneInfo.FromSerializedString("Eastern Standard Time;-300;(UTC-05:00) Eastern Time (US & Canada);Eastern Standard Time;Eastern Daylight Time;[01:01:0001;12:31:2006;60;[0;02:00:00;4;1;0;];[0;02:00:00;10;5;0;];][01:01:2007;12:31:9999;60;[0;02:00:00;3;2;0;];[0;02:00:00;11;1;0;];];");
			var chicagoTime = TimeZoneInfo.FromSerializedString("Central Standard Time;-360;(UTC-06:00) Central Time (US & Canada);Central Standard Time;Central Daylight Time;[01:01:0001;12:31:2006;60;[0;02:00:00;4;1;0;];[0;02:00:00;10;5;0;];][01:01:2007;12:31:9999;60;[0;02:00:00;3;2;0;];[0;02:00:00;11;1;0;];];");

			Amex = new Exchange
			{
				Name = "AMEX",
				RusName = "Американская фондовая биржа",
				EngName = "American Stock Exchange",
				TimeZoneInfo = newYorkTime,
				CountryCode = CountryCodes.US,
			};

			Cme = new Exchange
			{
				Name = "CME",
				RusName = "Чикагская товарная биржа",
				EngName = "Chicago Mercantile Exchange",
				TimeZoneInfo = chicagoTime,
				CountryCode = CountryCodes.US,
			};

			Cce = new Exchange
			{
				Name = "CCE",
				RusName = "Чикагская климатическая биржа",
				EngName = "Chicago Climate Exchange",
				TimeZoneInfo = chicagoTime,
				CountryCode = CountryCodes.US,
			};

			Cbot = new Exchange
			{
				Name = "CBOT",
				RusName = "Чикагская торговая палата",
				EngName = "Chicago Board of Trade",
				TimeZoneInfo = chicagoTime,
				CountryCode = CountryCodes.US,
			};

			Nymex = new Exchange
			{
				Name = "NYMEX",
				RusName = "Нью-Йоркская товарная биржа",
				EngName = "New York Mercantile Exchange",
				TimeZoneInfo = newYorkTime,
				CountryCode = CountryCodes.US,
			};

			Nyse = new Exchange
			{
				Name = "NYSE",
				RusName = "Нью-Йоркская фондовая биржа",
				EngName = "New York Stock Exchange",
				TimeZoneInfo = newYorkTime,
				CountryCode = CountryCodes.US,
			};

			Nasdaq = new Exchange
			{
				Name = "NASDAQ",
				RusName = "Насдак",
				EngName = "NASDAQ",
				TimeZoneInfo = newYorkTime,
				CountryCode = CountryCodes.US,
			};

			Nqlx = new Exchange
			{
				Name = "NQLX",
				RusName = "Насдак LM",
				EngName = "Nasdaq-Liffe Markets",
				TimeZoneInfo = newYorkTime,
				CountryCode = CountryCodes.US,
			};

			Tsx = new Exchange
			{
				Name = "TSX",
				RusName = "Фондовая биржа Торонто",
				EngName = "Toronto Stock Exchange",
				TimeZoneInfo = newYorkTime,
				CountryCode = CountryCodes.CA,
			};

			Lse = new Exchange
			{
				Name = "LSE",
				RusName = "Лондонская фондовая биржа",
				EngName = "London Stock Exchange",
				TimeZoneInfo = TimeZoneInfo.FromSerializedString("GMT Standard Time;0;(UTC) Dublin, Edinburgh, Lisbon, London;GMT Standard Time;GMT Daylight Time;[01:01:0001;12:31:9999;60;[0;01:00:00;3;5;0;];[0;02:00:00;10;5;0;];];"),
				CountryCode = CountryCodes.GB,
			};

			Tse = new Exchange
			{
				Name = "TSE",
				RusName = "Токийская фондовая биржа",
				EngName = "Tokio Stock Exchange",
				TimeZoneInfo = TimeZoneInfo.FromSerializedString("Tokyo Standard Time;540;(UTC+09:00) Osaka, Sapporo, Tokyo;Tokyo Standard Time;Tokyo Daylight Time;;"),
				CountryCode = CountryCodes.JP,
			};

			var chinaTime = TimeZoneInfo.FromSerializedString("China Standard Time;480;(UTC+08:00) Beijing, Chongqing, Hong Kong, Urumqi;China Standard Time;China Daylight Time;;");

			Hkex = new Exchange
			{
				Name = "HKEX",
				RusName = "Гонконгская фондовая биржа",
				EngName = "Hong Kong Stock Exchange",
				TimeZoneInfo = chinaTime,
				CountryCode = CountryCodes.HK,
			};

			Hkfe = new Exchange
			{
				Name = "HKFE",
				RusName = "Гонконгская фьючерсная биржа",
				EngName = "Hong Kong Futures Exchange",
				TimeZoneInfo = chinaTime,
				CountryCode = CountryCodes.HK,
			};

			Sse = new Exchange
			{
				Name = "SSE",
				RusName = "Шанхаская фондовая биржа",
				EngName = "Shanghai Stock Exchange",
				TimeZoneInfo = chinaTime,
				CountryCode = CountryCodes.CN,
			};

			Szse = new Exchange
			{
				Name = "SZSE",
				RusName = "Шэньчжэньская фондовая биржа",
				EngName = "Shenzhen Stock Exchange",
				TimeZoneInfo = chinaTime,
				CountryCode = CountryCodes.CN,
			};

			Tsec = new Exchange
			{
				Name = "TSEC",
				RusName = "Тайваньская фондовая биржа",
				EngName = "Taiwan Stock Exchange",
				TimeZoneInfo = chinaTime,
				CountryCode = CountryCodes.TW,
			};

			var singaporeTime = TimeZoneInfo.FromSerializedString("Singapore Standard Time;480;(UTC+08:00) Kuala Lumpur, Singapore;Malay Peninsula Standard Time;Malay Peninsula Daylight Time;;");

			Sgx = new Exchange
			{
				Name = "SGX",
				RusName = "Сингапурская биржа",
				EngName = "Singapore Exchange",
				TimeZoneInfo = singaporeTime,
				CountryCode = CountryCodes.SG,
			};

			Pse = new Exchange
			{
				Name = "PSE",
				RusName = "Филиппинская фондовая биржа",
				EngName = "Philippine Stock Exchange",
				TimeZoneInfo = singaporeTime,
				CountryCode = CountryCodes.PH,
			};

			Klse = new Exchange
			{
				Name = "MYX",
				RusName = "Малайзийская биржа",
				EngName = "Bursa Malaysia",
				TimeZoneInfo = singaporeTime,
				CountryCode = CountryCodes.MY,
			};

			var bangkokTime = TimeZoneInfo.FromSerializedString("SE Asia Standard Time;420;(UTC+07:00) Bangkok, Hanoi, Jakarta;SE Asia Standard Time;SE Asia Daylight Time;;");

			Idx = new Exchange
			{
				Name = "IDX",
				RusName = "Индонезийская фондовая биржа",
				EngName = "Indonesia Stock Exchange",
				TimeZoneInfo = bangkokTime,
				CountryCode = CountryCodes.ID,
			};

			Set = new Exchange
			{
				Name = "SET",
				RusName = "Фондовая биржа Таиланда",
				EngName = "Stock Exchange of Thailand",
				TimeZoneInfo = bangkokTime,
				CountryCode = CountryCodes.TH,
			};

			var indiaTime = TimeZoneInfo.FromSerializedString("India Standard Time;330;(UTC+05:30) Chennai, Kolkata, Mumbai, New Delhi;India Standard Time;India Daylight Time;;");

			Bse = new Exchange
			{
				Name = "BSE",
				RusName = "Бомбейская фондовая биржа",
				EngName = "Bombay Stock Exchange",
				TimeZoneInfo = indiaTime,
				CountryCode = CountryCodes.IN,
			};

			Nse = new Exchange
			{
				Name = "NSE",
				RusName = "Национальная фондовая биржа Индии",
				EngName = "National Stock Exchange of India",
				TimeZoneInfo = indiaTime,
				CountryCode = CountryCodes.IN,
			};

			Cse = new Exchange
			{
				Name = "CSE",
				RusName = "Колумбийская фондовая биржа",
				EngName = "Colombo Stock Exchange",
				TimeZoneInfo = TimeZoneInfo.FromSerializedString("Sri Lanka Standard Time;330;(UTC+05:30) Sri Jayawardenepura;Sri Lanka Standard Time;Sri Lanka Daylight Time;;"),
				CountryCode = CountryCodes.CO,
			};

			Krx = new Exchange
			{
				Name = "KRX",
				RusName = "Корейская биржа",
				EngName = "Korea Exchange",
				TimeZoneInfo = TimeZoneInfo.FromSerializedString("Korea Standard Time;540;(UTC+09:00) Seoul;Korea Standard Time;Korea Daylight Time;;"),
				CountryCode = CountryCodes.KR,
			};

			Asx = new Exchange
			{
				Name = "ASX",
				RusName = "Австралийская фондовая биржа",
				EngName = "Australian Securities Exchange",
				TimeZoneInfo = TimeZoneInfo.FromSerializedString("AUS Eastern Standard Time;600;(UTC+10:00) Canberra, Melbourne, Sydney;AUS Eastern Standard Time;AUS Eastern Daylight Time;[01:01:0001;12:31:2007;60;[0;02:00:00;10;5;0;];[0;03:00:00;3;5;0;];][01:01:2008;12:31:9999;60;[0;02:00:00;10;1;0;];[0;03:00:00;4;1;0;];];"),
				CountryCode = CountryCodes.AU,
			};

			Nzx = new Exchange
			{
				Name = "NZSX",
				RusName = "Новозеландская биржа",
				EngName = "New Zealand Exchange",
				TimeZoneInfo = TimeZoneInfo.FromSerializedString("New Zealand Standard Time;720;(UTC+12:00) Auckland, Wellington;New Zealand Standard Time;New Zealand Daylight Time;[01:01:0001;12:31:2006;60;[0;02:00:00;10;1;0;];[0;03:00:00;3;3;0;];][01:01:2007;12:31:2007;60;[0;02:00:00;9;5;0;];[0;03:00:00;3;3;0;];][01:01:2008;12:31:9999;60;[0;02:00:00;9;5;0;];[0;03:00:00;4;1;0;];];"),
				CountryCode = CountryCodes.NZ,
			};

			Tase = new Exchange
			{
				Name = "TASE",
				RusName = "Тель-Авивская фондовая биржа",
				EngName = "Tel Aviv Stock Exchange",
				TimeZoneInfo = TimeZoneInfo.FromSerializedString("Israel Standard Time;120;(UTC+02:00) Jerusalem;Jerusalem Standard Time;Jerusalem Daylight Time;[01:01:2005;12:31:2005;60;[0;02:00:00;4;1;5;];[0;02:00:00;10;2;0;];][01:01:2006;12:31:2006;60;[0;02:00:00;3;5;5;];[0;02:00:00;10;1;0;];][01:01:2007;12:31:2007;60;[0;02:00:00;3;5;5;];[0;02:00:00;9;3;0;];][01:01:2008;12:31:2008;60;[0;02:00:00;3;5;5;];[0;02:00:00;10;1;0;];][01:01:2009;12:31:2009;60;[0;02:00:00;3;5;5;];[0;02:00:00;9;5;0;];][01:01:2010;12:31:2010;60;[0;02:00:00;3;5;5;];[0;02:00:00;9;2;0;];][01:01:2011;12:31:2011;60;[0;02:00:00;4;1;5;];[0;02:00:00;10;1;0;];][01:01:2012;12:31:2012;60;[0;02:00:00;3;5;5;];[0;02:00:00;9;4;0;];][01:01:2013;12:31:2013;60;[0;02:00:00;3;5;5;];[0;02:00:00;9;2;0;];][01:01:2014;12:31:2014;60;[0;02:00:00;3;5;5;];[0;02:00:00;9;4;0;];][01:01:2015;12:31:2015;60;[0;02:00:00;3;5;5;];[0;02:00:00;9;3;0;];][01:01:2016;12:31:2016;60;[0;02:00:00;4;1;5;];[0;02:00:00;10;2;0;];][01:01:2017;12:31:2017;60;[0;02:00:00;3;5;5;];[0;02:00:00;9;4;0;];][01:01:2018;12:31:2018;60;[0;02:00:00;3;5;5;];[0;02:00:00;9;3;0;];][01:01:2019;12:31:2019;60;[0;02:00:00;3;5;5;];[0;02:00:00;10;1;0;];][01:01:2020;12:31:2020;60;[0;02:00:00;3;5;5;];[0;02:00:00;9;4;0;];][01:01:2021;12:31:2021;60;[0;02:00:00;3;5;5;];[0;02:00:00;9;2;0;];][01:01:2022;12:31:2022;60;[0;02:00:00;4;1;5;];[0;02:00:00;10;1;0;];];"),
				CountryCode = CountryCodes.IL,
			};

			Fwb = new Exchange
			{
				Name = "FWB",
				RusName = "Франкфуртская фондовая биржа",
				EngName = "Frankfurt Stock Exchange",
				TimeZoneInfo = TimeZoneInfo.FromSerializedString("W. Europe Standard Time;60;(UTC+01:00) Amsterdam, Berlin, Bern, Rome, Stockholm, Vienna;W. Europe Standard Time;W. Europe Daylight Time;[01:01:0001;12:31:9999;60;[0;02:00:00;3;5;0;];[0;03:00:00;10;5;0;];];"),
				CountryCode = CountryCodes.DE,
			};

			Mse = new Exchange
			{
				Name = "MSE",
				RusName = "Мадридская фондовая биржа",
				EngName = "Madrid Stock Exchange",
				TimeZoneInfo = TimeZoneInfo.FromSerializedString("Romance Standard Time;60;(UTC+01:00) Brussels, Copenhagen, Madrid, Paris;Romance Standard Time;Romance Daylight Time;[01:01:0001;12:31:9999;60;[0;02:00:00;3;5;0;];[0;03:00:00;10;5;0;];];"),
				CountryCode = CountryCodes.ES,
			};

			Swx = new Exchange
			{
				Name = "SWX",
				RusName = "Швейцарская биржа",
				EngName = "Swiss Exchange",
				TimeZoneInfo = GetTimeZone("Central European Standard Time", TimeSpan.FromHours(1)),
				CountryCode = CountryCodes.CH,
			};

			Jse = new Exchange
			{
				Name = "JSE",
				RusName = "Йоханнесбургская фондовая биржа",
				EngName = "Johannesburg Stock Exchange",
				TimeZoneInfo = GetTimeZone("South Africa Standard Time", TimeSpan.FromHours(2)),
				CountryCode = CountryCodes.ZA,
			};

			Lmax = new Exchange
			{
				Name = "LMAX",
				RusName = "Форекс брокер LMAX",
				EngName = "LMAX",
				CountryCode = CountryCodes.GB,
			};

			DukasCopy = new Exchange
			{
				Name = "DUKAS",
				RusName = "Форекс брокер DukasCopy",
				EngName = "DukasCopy",
				CountryCode = CountryCodes.CH,
			};

			GainCapital = new Exchange
			{
				Name = "GAIN",
				RusName = "Форекс брокер GAIN Capital",
				EngName = "GAIN Capital",
				CountryCode = CountryCodes.US,
			};
			
			MBTrading = new Exchange
			{
				Name = "MBT",
				RusName = "Форекс брокер MB Trading",
				EngName = "MB Trading",
				CountryCode = CountryCodes.US,
			};

			TrueFX = new Exchange
			{
				Name = "TRUEFX",
				RusName = "Форекс брокер TrueFX",
				EngName = "TrueFX",
				CountryCode = CountryCodes.US,
			};

			Cfh = new Exchange
			{
				Name = "CFH",
				RusName = "CFH",
				EngName = "CFH",
				CountryCode = CountryCodes.GB,
			};

			Ond = new Exchange
			{
				Name = "OANDA",
				RusName = "Форекс брокер OANDA",
				EngName = "OANDA",
				CountryCode = CountryCodes.US,
			};

			Integral = new Exchange
			{
				Name = "INTGRL",
				RusName = "Integral",
				EngName = "Integral",
				CountryCode = CountryCodes.US,
			};

			Btce = new Exchange
			{
				Name = "BTCE",
				RusName = "BTCE",
				EngName = "BTCE",
				CountryCode = CountryCodes.RU,
			};

			BitStamp = new Exchange
			{
				Name = "BITSTAMP",
				RusName = "BitStamp",
				EngName = "BitStamp",
				CountryCode = CountryCodes.GB,
			};

			BtcChina = new Exchange
			{
				Name = "BTCCHINA",
				RusName = "BTCChina",
				EngName = "BTCChina",
				CountryCode = CountryCodes.CN,
			};

			Icbit = new Exchange
			{
				Name = "ICBIT",
				RusName = "iCBIT",
				EngName = "iCBIT",
				CountryCode = CountryCodes.RU,
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
		/// Создать <see cref="Exchange"/>.
		/// </summary>
		public Exchange()
		{
			ExtensionInfo = new Dictionary<object, object>();
			RusName = EngName = string.Empty;
		}

		private string _name;

		/// <summary>
		/// Кодовое название биржи.
		/// </summary>
		[DataMember]
		[Identity]
		public string Name
		{
			get { return _name; }
			set
			{
				if (Name == value)
					return;

				_name = value;
				Notify("Name");
			}
		}

		private string _rusName;

		/// <summary>
		/// Русскоязычное название биржи.
		/// </summary>
		[DataMember]
		public string RusName
		{
			get { return _rusName; }
			set
			{
				if (RusName == value)
					return;

				_rusName = value;
				Notify("RusName");
			}
		}

		private string _engName;

		/// <summary>
		/// Англоязычное название биржи.
		/// </summary>
		[DataMember]
		public string EngName
		{
			get { return _engName; }
			set
			{
				if (EngName == value)
					return;

				_engName = value;
				Notify("EngName");
			}
		}

		private CountryCodes? _countryCode;

		/// <summary>
		/// ISO код страны.
		/// </summary>
		[DataMember]
		[Nullable]
		public CountryCodes? CountryCode
		{
			get { return _countryCode; }
			set
			{
				if (CountryCode == value)
					return;

				_countryCode = value;
				Notify("CountryCode");
			}
		}

		[field: NonSerialized]
		private TimeZoneInfo _timeZoneInfo = TimeZoneInfo.Utc;

		/// <summary>
		/// Информация о временной зоне, где находится биржа.
		/// </summary>
		[TimeZoneInfo]
		[XmlIgnore]
		[DataMember]
		public TimeZoneInfo TimeZoneInfo
		{
			get { return _timeZoneInfo; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (TimeZoneInfo == value)
					return;

				_timeZoneInfo = value;
				Notify("TimeZoneInfo");
			}
		}

		/// <summary>
		/// Информация о тестовой бирже, не имеющая ограничения в расписание работы.
		/// </summary>
		public static Exchange Test { get; private set; }

		/// <summary>
		/// Информация о бирже MOEX (Moscow Exchange).
		/// </summary>
		public static Exchange Moex { get; private set; }

		/// <summary>
		/// Информация об Украiнська Бiржа.
		/// </summary>
		public static Exchange Ux { get; private set; }

		/// <summary>
		/// Информация о бирже AMEX (American Stock Exchange).
		/// </summary>
		public static Exchange Amex { get; private set; }

		/// <summary>
		/// Информация о бирже CME (Chicago Mercantile Exchange).
		/// </summary>
		public static Exchange Cme { get; private set; }

		/// <summary>
		/// Информация о бирже CBOT (Chicago Board of Trade).
		/// </summary>
		public static Exchange Cbot { get; private set; }

		/// <summary>
		/// Информация о бирже CCE (Chicago Climate Exchange).
		/// </summary>
		public static Exchange Cce { get; private set; }

		/// <summary>
		/// Информация о бирже NYMEX (New York Mercantile Exchange).
		/// </summary>
		public static Exchange Nymex { get; private set; }

		/// <summary>
		/// Информация о бирже NYSE (New York Stock Exchange).
		/// </summary>
		public static Exchange Nyse { get; private set; }

		/// <summary>
		/// Информация о бирже NASDAQ.
		/// </summary>
		public static Exchange Nasdaq { get; private set; }

		/// <summary>
		/// Информация о бирже NQLX.
		/// </summary>
		public static Exchange Nqlx { get; private set; }

		/// <summary>
		/// Информация о бирже LSE (London Stock Exchange).
		/// </summary>
		public static Exchange Lse { get; private set; }

		/// <summary>
		/// Информация о бирже TSE (Tokio Stock Exchange).
		/// </summary>
		public static Exchange Tse { get; private set; }

		/// <summary>
		/// Информация о бирже HKEX (Hong Kong Stock Exchange).
		/// </summary>
		public static Exchange Hkex { get; private set; }

		/// <summary>
		/// Информация о бирже HKFE (Hong Kong Futures Exchange).
		/// </summary>
		public static Exchange Hkfe { get; private set; }

		/// <summary>
		/// Информация о бирже Sse (Shanghai Stock Exchange).
		/// </summary>
		public static Exchange Sse { get; private set; }

		/// <summary>
		/// Информация о бирже SZSE (Shenzhen Stock Exchange).
		/// </summary>
		public static Exchange Szse { get; private set; }

		/// <summary>
		/// Информация о бирже TSX (Toronto Stock Exchange).
		/// </summary>
		public static Exchange Tsx { get; private set; }

		/// <summary>
		/// Информация о бирже FWB (Frankfurt Stock Exchange).
		/// </summary>
		public static Exchange Fwb { get; private set; }

		/// <summary>
		/// Информация о бирже ASX (Australian Securities Exchange).
		/// </summary>
		public static Exchange Asx { get; private set; }

		/// <summary>
		/// Информация о бирже NZX (New Zealand Exchange).
		/// </summary>
		public static Exchange Nzx { get; private set; }

		/// <summary>
		/// Информация о бирже BSE (Bombay Stock Exchange).
		/// </summary>
		public static Exchange Bse { get; private set; }

		/// <summary>
		/// Информация о бирже NSE (National Stock Exchange of India).
		/// </summary>
		public static Exchange Nse { get; private set; }

		/// <summary>
		/// Информация о бирже SWX (Swiss Exchange).
		/// </summary>
		public static Exchange Swx { get; private set; }

		/// <summary>
		/// Информация о бирже KRX (Korea Exchange).
		/// </summary>
		public static Exchange Krx { get; private set; }

		/// <summary>
		/// Информация о бирже MSE (Madrid Stock Exchange).
		/// </summary>
		public static Exchange Mse { get; private set; }

		/// <summary>
		/// Информация о бирже JSE (Johannesburg Stock Exchange).
		/// </summary>
		public static Exchange Jse { get; private set; }

		/// <summary>
		/// Информация о бирже SGX (Singapore Exchange).
		/// </summary>
		public static Exchange Sgx { get; private set; }

		/// <summary>
		/// Информация о бирже TSEC (Taiwan Stock Exchange).
		/// </summary>
		public static Exchange Tsec { get; private set; }

		/// <summary>
		/// Информация о бирже PSE (Philippine Stock Exchange).
		/// </summary>
		public static Exchange Pse { get; private set; }

		/// <summary>
		/// Информация о бирже KLSE (Bursa Malaysia).
		/// </summary>
		public static Exchange Klse { get; private set; }

		/// <summary>
		/// Информация о бирже IDX (Indonesia Stock Exchange).
		/// </summary>
		public static Exchange Idx { get; private set; }

		/// <summary>
		/// Информация о бирже SET (Stock Exchange of Thailand).
		/// </summary>
		public static Exchange Set { get; private set; }

		/// <summary>
		/// Информация о бирже CSE (Colombo Stock Exchange).
		/// </summary>
		public static Exchange Cse { get; private set; }

		/// <summary>
		/// Информация о бирже TASE (Tel Aviv Stock Exchange).
		/// </summary>
		public static Exchange Tase { get; private set; }

		/// <summary>
		/// Информация о брокере LMAX (LMAX Exchange).
		/// </summary>
		public static Exchange Lmax { get; private set; }

		/// <summary>
		/// Информация о брокере DukasCopy.
		/// </summary>
		public static Exchange DukasCopy { get; private set; }

		/// <summary>
		/// Информация о брокере GAIN Capital.
		/// </summary>
		public static Exchange GainCapital { get; private set; }

		/// <summary>
		/// Информация о брокере MB Trading.
		/// </summary>
		public static Exchange MBTrading { get; private set; }

		/// <summary>
		/// Информация о брокере TrueFX
		/// </summary>
		public static Exchange TrueFX { get; private set; }

		/// <summary>
		/// Информация о CFH.
		/// </summary>
		public static Exchange Cfh { get; private set; }

		/// <summary>
		/// Информация о OANDA.
		/// </summary>
		public static Exchange Ond { get; private set; }

		/// <summary>
		/// Информация о Integral.
		/// </summary>
		public static Exchange Integral { get; private set; }

		/// <summary>
		/// Информация о бирже BTCE.
		/// </summary>
		public static Exchange Btce { get; private set; }

		/// <summary>
		/// Информация о бирже BitStamp.
		/// </summary>
		public static Exchange BitStamp { get; private set; }

		/// <summary>
		/// Информация о бирже BtcChina.
		/// </summary>
		public static Exchange BtcChina { get; private set; }

		/// <summary>
		/// Информация о бирже Icbit.
		/// </summary>
		public static Exchange Icbit { get; private set; }
		
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
			return Name;
		}

		/// <summary>
		/// Сравнить два объекта <see cref="Exchange" /> на эквивалентность.
		/// </summary>
		/// <param name="other">Объект для сравнения.</param>
		/// <returns><see langword="true"/>, если другой объект равен текущему, иначе, <see langword="false"/>.</returns>
		protected override bool OnEquals(Exchange other)
		{
			return Name == other.Name;
		}

		/// <summary>
		/// Рассчитать хеш-код объекта <see cref="Exchange"/>.
		/// </summary>
		/// <returns>Хеш-код.</returns>
		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

		/// <summary>
		/// Создать копию объекта <see cref="Exchange" />.
		/// </summary>
		/// <returns>Копия.</returns>
		public override Exchange Clone()
		{
			return new Exchange
			{
				Name = Name,
				RusName = RusName,
				EngName = EngName,
				TimeZoneInfo = TimeZoneInfo,
				CountryCode = CountryCode,
			};
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Load(SettingsStorage storage)
		{
			Name = storage.GetValue<string>("Name");
			RusName = storage.GetValue<string>("RusName");
			EngName = storage.GetValue<string>("EngName");
			CountryCode = storage.GetValue<CountryCodes?>("CountryCode");
			TimeZoneInfo = storage.GetValue<TimeZoneInfo>("TimeZoneInfo");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("Name", Name);
			storage.SetValue("RusName", RusName);
			storage.SetValue("EngName", EngName);
			storage.SetValue("CountryCode", CountryCode.To<string>());
			storage.SetValue("TimeZoneInfo", TimeZoneInfo);
		}
	}
}
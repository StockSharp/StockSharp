#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: Exchange_Instances.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BusinessEntities
{
	using Ecng.Common;

	partial class Exchange
	{
		static Exchange()
		{
			Test = new Exchange
			{
				Name = "TEST",
				RusName = "Тестовая биржа",
				EngName = "Test Exchange",
			};

			Moex = new Exchange
			{
				Name = "MOEX",
				RusName = "Московская биржа",
				EngName = "Moscow Exchange",
				CountryCode = CountryCodes.RU,
			};

			Spb = new Exchange
			{
				Name = "SPB",
				RusName = "Санкт-Петербургская биржа",
				EngName = "Saint-Petersburg Exchange",
				CountryCode = CountryCodes.RU,
			};

			Ux = new Exchange
			{
				Name = "UX",
				RusName = "Украинская биржа",
				EngName = "Ukrain Exchange",
				CountryCode = CountryCodes.UA,
			};

			Amex = new Exchange
			{
				Name = "AMEX",
				RusName = "Американская фондовая биржа",
				EngName = "American Stock Exchange",
				CountryCode = CountryCodes.US,
			};

			Cme = new Exchange
			{
				Name = "CME",
				RusName = "Чикагская товарная биржа",
				EngName = "Chicago Mercantile Exchange",
				CountryCode = CountryCodes.US,
			};

			Cce = new Exchange
			{
				Name = "CCE",
				RusName = "Чикагская климатическая биржа",
				EngName = "Chicago Climate Exchange",
				CountryCode = CountryCodes.US,
			};

			Cbot = new Exchange
			{
				Name = "CBOT",
				RusName = "Чикагская торговая палата",
				EngName = "Chicago Board of Trade",
				CountryCode = CountryCodes.US,
			};

			Nymex = new Exchange
			{
				Name = "NYMEX",
				RusName = "Нью-Йоркская товарная биржа",
				EngName = "New York Mercantile Exchange",
				CountryCode = CountryCodes.US,
			};

			Nyse = new Exchange
			{
				Name = "NYSE",
				RusName = "Нью-Йоркская фондовая биржа",
				EngName = "New York Stock Exchange",
				CountryCode = CountryCodes.US,
			};

			Nasdaq = new Exchange
			{
				Name = "NASDAQ",
				RusName = "Насдак",
				EngName = "NASDAQ",
				CountryCode = CountryCodes.US,
			};

			Nqlx = new Exchange
			{
				Name = "NQLX",
				RusName = "Насдак LM",
				EngName = "Nasdaq-Liffe Markets",
				CountryCode = CountryCodes.US,
			};

			Tsx = new Exchange
			{
				Name = "TSX",
				RusName = "Фондовая биржа Торонто",
				EngName = "Toronto Stock Exchange",
				CountryCode = CountryCodes.CA,
			};

			Lse = new Exchange
			{
				Name = "LSE",
				RusName = "Лондонская фондовая биржа",
				EngName = "London Stock Exchange",
				CountryCode = CountryCodes.GB,
			};

			Lme = new Exchange
			{
				Name = "LME",
				RusName = "Лондонская биржа металлов",
				EngName = "London Metal Exchange",
				CountryCode = CountryCodes.GB,
			};

			Tse = new Exchange
			{
				Name = "TSE",
				RusName = "Токийская фондовая биржа",
				EngName = "Tokyo Stock Exchange",
				CountryCode = CountryCodes.JP,
			};

			Hkex = new Exchange
			{
				Name = "HKEX",
				RusName = "Гонконгская фондовая биржа",
				EngName = "Hong Kong Stock Exchange",
				CountryCode = CountryCodes.HK,
			};

			Hkfe = new Exchange
			{
				Name = "HKFE",
				RusName = "Гонконгская фьючерсная биржа",
				EngName = "Hong Kong Futures Exchange",
				CountryCode = CountryCodes.HK,
			};

			Sse = new Exchange
			{
				Name = "SSE",
				RusName = "Шанхаская фондовая биржа",
				EngName = "Shanghai Stock Exchange",
				CountryCode = CountryCodes.CN,
			};

			Szse = new Exchange
			{
				Name = "SZSE",
				RusName = "Шэньчжэньская фондовая биржа",
				EngName = "Shenzhen Stock Exchange",
				CountryCode = CountryCodes.CN,
			};

			Tsec = new Exchange
			{
				Name = "TSEC",
				RusName = "Тайваньская фондовая биржа",
				EngName = "Taiwan Stock Exchange",
				CountryCode = CountryCodes.TW,
			};

			Sgx = new Exchange
			{
				Name = "SGX",
				RusName = "Сингапурская биржа",
				EngName = "Singapore Exchange",
				CountryCode = CountryCodes.SG,
			};

			Pse = new Exchange
			{
				Name = "PSE",
				RusName = "Филиппинская фондовая биржа",
				EngName = "Philippine Stock Exchange",
				CountryCode = CountryCodes.PH,
			};

			Klse = new Exchange
			{
				Name = "MYX",
				RusName = "Малайзийская биржа",
				EngName = "Bursa Malaysia",
				CountryCode = CountryCodes.MY,
			};

			Idx = new Exchange
			{
				Name = "IDX",
				RusName = "Индонезийская фондовая биржа",
				EngName = "Indonesia Stock Exchange",
				CountryCode = CountryCodes.ID,
			};

			Set = new Exchange
			{
				Name = "SET",
				RusName = "Фондовая биржа Таиланда",
				EngName = "Stock Exchange of Thailand",
				CountryCode = CountryCodes.TH,
			};

			Bse = new Exchange
			{
				Name = "BSE",
				RusName = "Бомбейская фондовая биржа",
				EngName = "Bombay Stock Exchange",
				CountryCode = CountryCodes.IN,
			};

			Nse = new Exchange
			{
				Name = "NSE",
				RusName = "Национальная фондовая биржа Индии",
				EngName = "National Stock Exchange of India",
				CountryCode = CountryCodes.IN,
			};

			Cse = new Exchange
			{
				Name = "CSE",
				RusName = "Колумбийская фондовая биржа",
				EngName = "Colombo Stock Exchange",
				CountryCode = CountryCodes.CO,
			};

			Krx = new Exchange
			{
				Name = "KRX",
				RusName = "Корейская биржа",
				EngName = "Korea Exchange",
				CountryCode = CountryCodes.KR,
			};

			Asx = new Exchange
			{
				Name = "ASX",
				RusName = "Австралийская фондовая биржа",
				EngName = "Australian Securities Exchange",
				CountryCode = CountryCodes.AU,
			};

			Nzx = new Exchange
			{
				Name = "NZSX",
				RusName = "Новозеландская биржа",
				EngName = "New Zealand Exchange",
				CountryCode = CountryCodes.NZ,
			};

			Tase = new Exchange
			{
				Name = "TASE",
				RusName = "Тель-Авивская фондовая биржа",
				EngName = "Tel Aviv Stock Exchange",
				CountryCode = CountryCodes.IL,
			};

			Fwb = new Exchange
			{
				Name = "FWB",
				RusName = "Франкфуртская фондовая биржа",
				EngName = "Frankfurt Stock Exchange",
				CountryCode = CountryCodes.DE,
			};

			Mse = new Exchange
			{
				Name = "MSE",
				RusName = "Мадридская фондовая биржа",
				EngName = "Madrid Stock Exchange",
				CountryCode = CountryCodes.ES,
			};

			Swx = new Exchange
			{
				Name = "SWX",
				RusName = "Швейцарская биржа",
				EngName = "Swiss Exchange",
				CountryCode = CountryCodes.CH,
			};

			Jse = new Exchange
			{
				Name = "JSE",
				RusName = "Йоханнесбургская фондовая биржа",
				EngName = "Johannesburg Stock Exchange",
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

		/// <summary>
		/// Information about the test exchange, which has no limitations in work schedule.
		/// </summary>
		public static Exchange Test { get; }

		/// <summary>
		/// Information about MOEX (Moscow Exchange).
		/// </summary>
		public static Exchange Moex { get; }

		/// <summary>
		/// Saint-Petersburg Exchange (SPB).
		/// </summary>
		public static Exchange Spb { get; }

		/// <summary>
		/// Information about UX.
		/// </summary>
		public static Exchange Ux { get; }

		/// <summary>
		/// Information about AMEX (American Stock Exchange).
		/// </summary>
		public static Exchange Amex { get; }

		/// <summary>
		/// Information about CME (Chicago Mercantile Exchange).
		/// </summary>
		public static Exchange Cme { get; }

		/// <summary>
		/// Information about CBOT (Chicago Board of Trade).
		/// </summary>
		public static Exchange Cbot { get; }

		/// <summary>
		/// Information about CCE (Chicago Climate Exchange).
		/// </summary>
		public static Exchange Cce { get; }

		/// <summary>
		/// Information about NYMEX (New York Mercantile Exchange).
		/// </summary>
		public static Exchange Nymex { get; }

		/// <summary>
		/// Information about NYSE (New York Stock Exchange).
		/// </summary>
		public static Exchange Nyse { get; }

		/// <summary>
		/// Information about NASDAQ.
		/// </summary>
		public static Exchange Nasdaq { get; }

		/// <summary>
		/// Information about NQLX.
		/// </summary>
		public static Exchange Nqlx { get; }

		/// <summary>
		/// Information about LSE (London Stock Exchange).
		/// </summary>
		public static Exchange Lse { get; }

		/// <summary>
		/// Information about LME (London Metal Exchange).
		/// </summary>
		public static Exchange Lme { get; }

		/// <summary>
		/// Information about TSE (Tokyo Stock Exchange).
		/// </summary>
		public static Exchange Tse { get; }

		/// <summary>
		/// Information about HKEX (Hong Kong Stock Exchange).
		/// </summary>
		public static Exchange Hkex { get; }

		/// <summary>
		/// Information about HKFE (Hong Kong Futures Exchange).
		/// </summary>
		public static Exchange Hkfe { get; }

		/// <summary>
		/// Information about Sse (Shanghai Stock Exchange).
		/// </summary>
		public static Exchange Sse { get; }

		/// <summary>
		/// Information about SZSE (Shenzhen Stock Exchange).
		/// </summary>
		public static Exchange Szse { get; }

		/// <summary>
		/// Information about TSX (Toronto Stock Exchange).
		/// </summary>
		public static Exchange Tsx { get; }

		/// <summary>
		/// Information about FWB (Frankfurt Stock Exchange).
		/// </summary>
		public static Exchange Fwb { get; }

		/// <summary>
		/// Information about ASX (Australian Securities Exchange).
		/// </summary>
		public static Exchange Asx { get; }

		/// <summary>
		/// Information about NZX (New Zealand Exchange).
		/// </summary>
		public static Exchange Nzx { get; }

		/// <summary>
		/// Information about BSE (Bombay Stock Exchange).
		/// </summary>
		public static Exchange Bse { get; }

		/// <summary>
		/// Information about NSE (National Stock Exchange of India).
		/// </summary>
		public static Exchange Nse { get; }

		/// <summary>
		/// Information about SWX (Swiss Exchange).
		/// </summary>
		public static Exchange Swx { get; }

		/// <summary>
		/// Information about KRX (Korea Exchange).
		/// </summary>
		public static Exchange Krx { get; }

		/// <summary>
		/// Information about MSE (Madrid Stock Exchange).
		/// </summary>
		public static Exchange Mse { get; }

		/// <summary>
		/// Information about JSE (Johannesburg Stock Exchange).
		/// </summary>
		public static Exchange Jse { get; }

		/// <summary>
		/// Information about SGX (Singapore Exchange).
		/// </summary>
		public static Exchange Sgx { get; }

		/// <summary>
		/// Information about TSEC (Taiwan Stock Exchange).
		/// </summary>
		public static Exchange Tsec { get; }

		/// <summary>
		/// Information about PSE (Philippine Stock Exchange).
		/// </summary>
		public static Exchange Pse { get; }

		/// <summary>
		/// Information about KLSE (Bursa Malaysia).
		/// </summary>
		public static Exchange Klse { get; }

		/// <summary>
		/// Information about IDX (Indonesia Stock Exchange).
		/// </summary>
		public static Exchange Idx { get; }

		/// <summary>
		/// Information about SET (Stock Exchange of Thailand).
		/// </summary>
		public static Exchange Set { get; }

		/// <summary>
		/// Information about CSE (Colombo Stock Exchange).
		/// </summary>
		public static Exchange Cse { get; }

		/// <summary>
		/// Information about TASE (Tel Aviv Stock Exchange).
		/// </summary>
		public static Exchange Tase { get; }

		/// <summary>
		/// Information about LMAX (LMAX Exchange).
		/// </summary>
		public static Exchange Lmax { get; }

		/// <summary>
		/// Information about DukasCopy.
		/// </summary>
		public static Exchange DukasCopy { get; }

		/// <summary>
		/// Information about GAIN Capital.
		/// </summary>
		public static Exchange GainCapital { get; }

		/// <summary>
		/// Information about MB Trading.
		/// </summary>
		public static Exchange MBTrading { get; }

		/// <summary>
		/// Information about TrueFX.
		/// </summary>
		public static Exchange TrueFX { get; }

		/// <summary>
		/// Information about CFH.
		/// </summary>
		public static Exchange Cfh { get; }

		/// <summary>
		/// Information about OANDA.
		/// </summary>
		public static Exchange Ond { get; }

		/// <summary>
		/// Information about Integral.
		/// </summary>
		public static Exchange Integral { get; }

		/// <summary>
		/// Information about BTCE.
		/// </summary>
		public static Exchange Btce { get; }

		/// <summary>
		/// Information about BitStamp.
		/// </summary>
		public static Exchange BitStamp { get; }

		/// <summary>
		/// Information about BtcChina.
		/// </summary>
		public static Exchange BtcChina { get; }

		/// <summary>
		/// Information about Icbit.
		/// </summary>
		public static Exchange Icbit { get; }

		/// <summary>
		/// Information about Currenex.
		/// </summary>
		public static Exchange Currenex { get; } = new Exchange
		{
			Name = "CURRENEX",
			EngName = "Currenex",
			RusName = "Currenex",
			CountryCode = CountryCodes.US,
		};

		/// <summary>
		/// Information about FXCM.
		/// </summary>
		public static Exchange Fxcm { get; } = new Exchange
		{
			Name = "FXCM",
			EngName = "FXCM",
			RusName = "FXCM",
			CountryCode = CountryCodes.US,
		};

		/// <summary>
		/// Information about Poloniex.
		/// </summary>
		public static Exchange Poloniex { get; } = new Exchange
		{
			Name = "PLNX",
			EngName = "Poloniex",
			RusName = "Poloniex",
		};

		/// <summary>
		/// Information about Kraken.
		/// </summary>
		public static Exchange Kraken { get; } = new Exchange
		{
			Name = "KRKN",
			EngName = "Kraken",
			RusName = "Kraken",
		};

		/// <summary>
		/// Information about Bittrex.
		/// </summary>
		public static Exchange Bittrex { get; } = new Exchange
		{
			Name = "BTRX",
			EngName = "Bittrex",
			RusName = "Bittrex",
		};

		/// <summary>
		/// Information about Bitfinex.
		/// </summary>
		public static Exchange Bitfinex { get; } = new Exchange
		{
			Name = "BTFX",
			EngName = "Bitfinex",
			RusName = "Bitfinex",
		};

		/// <summary>
		/// Information about Coinbase.
		/// </summary>
		public static Exchange Coinbase { get; } = new Exchange
		{
			Name = "CNBS",
			EngName = "Coinbase",
			RusName = "Coinbase",
		};

		/// <summary>
		/// Information about GDAX.
		/// </summary>
		public static Exchange Gdax { get; } = new Exchange
		{
			Name = "GDAX",
			EngName = "GDAX",
			RusName = "GDAX",
		};

		/// <summary>
		/// Information about Bithumb.
		/// </summary>
		public static Exchange Bithumb { get; } = new Exchange
		{
			Name = "BTHB",
			EngName = "Bithumb",
			RusName = "Bithumb",
		};

		/// <summary>
		/// Information about HitBTC.
		/// </summary>
		public static Exchange HitBtc { get; } = new Exchange
		{
			Name = "HTBTC",
			EngName = "HitBTC",
			RusName = "HitBTC",
		};

		/// <summary>
		/// Information about OKCoin.
		/// </summary>
		public static Exchange OkCoin { get; } = new Exchange
		{
			Name = "OKCN",
			EngName = "OKCoin",
			RusName = "OKCoin",
		};

		/// <summary>
		/// Information about Coincheck.
		/// </summary>
		public static Exchange Coincheck { get; } = new Exchange
		{
			Name = "CNCK",
			EngName = "Coincheck",
			RusName = "Coincheck",
		};

		/// <summary>
		/// Information about Binance.
		/// </summary>
		public static Exchange Binance { get; } = new Exchange
		{
			Name = "BNB",
			EngName = "Binance",
			RusName = "Binance",
		};

		/// <summary>
		/// Information about Bitexbook.
		/// </summary>
		public static Exchange Bitexbook { get; } = new Exchange
		{
			Name = "BTXB",
			EngName = "Bitexbook",
			RusName = "Bitexbook",
		};

		/// <summary>
		/// Information about BitMEX.
		/// </summary>
		public static Exchange Bitmex { get; } = new Exchange
		{
			Name = "BMEX",
			EngName = "BitMEX",
			RusName = "BitMEX",
		};

		/// <summary>
		/// Information about CEX.IO.
		/// </summary>
		public static Exchange Cex { get; } = new Exchange
		{
			Name = "CEXIO",
			EngName = "CEX.IO",
			RusName = "CEX.IO",
		};

		/// <summary>
		/// Information about Cryptopia.
		/// </summary>
		public static Exchange Cryptopia { get; } = new Exchange
		{
			Name = "CRTP",
			EngName = "Cryptopia",
			RusName = "Cryptopia",
		};

		/// <summary>
		/// Information about OKEx.
		/// </summary>
		public static Exchange Okex { get; } = new Exchange
		{
			Name = "OKEX",
			EngName = "OKEx",
			RusName = "OKEx",
		};

		/// <summary>
		/// Information about YoBit.
		/// </summary>
		public static Exchange Yobit { get; } = new Exchange
		{
			Name = "YBIT",
			EngName = "YoBit",
			RusName = "YoBit",
		};

		/// <summary>
		/// Information about CoinExchange.
		/// </summary>
		public static Exchange CoinExchange { get; } = new Exchange
		{
			Name = "CNEX",
			EngName = "CoinExchange",
			RusName = "CoinExchange",
		};

		/// <summary>
		/// Information about Livecoin.
		/// </summary>
		public static Exchange LiveCoin { get; } = new Exchange
		{
			Name = "LVCN",
			EngName = "Livecoin",
			RusName = "Livecoin",
		};

		/// <summary>
		/// Information about Exmo.
		/// </summary>
		public static Exchange Exmo { get; } = new Exchange
		{
			Name = "EXMO",
			EngName = "Exmo",
			RusName = "Exmo",
		};

		/// <summary>
		/// Information about Deribit.
		/// </summary>
		public static Exchange Deribit { get; } = new Exchange
		{
			Name = "DRBT",
			EngName = "Deribit",
			RusName = "Deribit",
		};

		/// <summary>
		/// Information about Kucoin.
		/// </summary>
		public static Exchange Kucoin { get; } = new Exchange
		{
			Name = "KUCN",
			EngName = "Kucoin",
			RusName = "Kucoin",
		};

		/// <summary>
		/// Information about Liqui.
		/// </summary>
		public static Exchange Liqui { get; } = new Exchange
		{
			Name = "LIQI",
			EngName = "Liqui",
			RusName = "Liqui",
		};

		/// <summary>
		/// Information about Huobi.
		/// </summary>
		public static Exchange Huobi { get; } = new Exchange
		{
			Name = "HUBI",
			EngName = "Huobi",
			RusName = "Huobi",
		};

		/// <summary>
		/// Information about IEX.
		/// </summary>
		public static Exchange IEX { get; } = new Exchange
		{
			Name = "IEX",
			EngName = "IEX",
			RusName = "IEX",
			CountryCode = CountryCodes.US,
		};

		/// <summary>
		/// Information about Bitbank.
		/// </summary>
		public static Exchange Bitbank { get; } = new Exchange
		{
			Name = "BTBN",
			EngName = "Bitbank",
			RusName = "Bitbank",
		};

		/// <summary>
		/// Information about Zaif.
		/// </summary>
		public static Exchange Zaif { get; } = new Exchange
		{
			Name = "ZAIF",
			EngName = "Zaif",
			RusName = "Zaif",
		};

		/// <summary>
		/// Information about QUOINEX.
		/// </summary>
		public static Exchange Quoinex { get; } = new Exchange
		{
			Name = "QINX",
			EngName = "QUOINEX",
			RusName = "QUOINEX",
		};

		/// <summary>
		/// Information about WIKI.
		/// </summary>
		public static Exchange Wiki { get; } = new Exchange
		{
			Name = "WIKI",
			EngName = "WIKI",
			RusName = "WIKI",
		};
	}
}
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

	using StockSharp.Localization;

	partial class Exchange
	{
		static Exchange()
		{
			Test = new Exchange
			{
				Name = "TEST",
				FullNameLoc = LocalizedStrings.TestExchangeKey,
			};

			Moex = new Exchange
			{
				Name = "MOEX",
				FullNameLoc = LocalizedStrings.MoscowExchangeKey,
				CountryCode = CountryCodes.RU,
			};

			Spb = new Exchange
			{
				Name = "SPB",
				FullNameLoc = LocalizedStrings.SaintPetersburgExchangeKey,
				CountryCode = CountryCodes.RU,
			};

			Ux = new Exchange
			{
				Name = "UX",
				FullNameLoc = LocalizedStrings.UkrainExchangeKey,
				CountryCode = CountryCodes.UA,
			};

			Amex = new Exchange
			{
				Name = "AMEX",
				FullNameLoc = LocalizedStrings.AmericanStockExchangeKey,
				CountryCode = CountryCodes.US,
			};

			Cme = new Exchange
			{
				Name = "CME",
				FullNameLoc = LocalizedStrings.ChicagoMercantileExchangeKey,
				CountryCode = CountryCodes.US,
			};

			Cce = new Exchange
			{
				Name = "CCE",
				FullNameLoc = LocalizedStrings.ChicagoClimateExchangeKey,
				CountryCode = CountryCodes.US,
			};

			Cbot = new Exchange
			{
				Name = "CBOT",
				FullNameLoc = LocalizedStrings.ChicagoBoardofTradeKey,
				CountryCode = CountryCodes.US,
			};

			Nymex = new Exchange
			{
				Name = "NYMEX",
				FullNameLoc = LocalizedStrings.NewYorkMercantileExchangeKey,
				CountryCode = CountryCodes.US,
			};

			Nyse = new Exchange
			{
				Name = "NYSE",
				FullNameLoc = LocalizedStrings.NewYorkStockExchangeKey,
				CountryCode = CountryCodes.US,
			};

			Nasdaq = new Exchange
			{
				Name = "NASDAQ",
				FullNameLoc = LocalizedStrings.NASDAQKey,
				CountryCode = CountryCodes.US,
			};

			Nqlx = new Exchange
			{
				Name = "NQLX",
				FullNameLoc = LocalizedStrings.NasdaqLiffeMarketsKey,
				CountryCode = CountryCodes.US,
			};

			Tsx = new Exchange
			{
				Name = "TSX",
				FullNameLoc = LocalizedStrings.TorontoStockExchangeKey,
				CountryCode = CountryCodes.CA,
			};

			Lse = new Exchange
			{
				Name = "LSE",
				FullNameLoc = LocalizedStrings.LondonStockExchangeKey,
				CountryCode = CountryCodes.GB,
			};

			Lme = new Exchange
			{
				Name = "LME",
				FullNameLoc = LocalizedStrings.LondonMetalExchangeKey,
				CountryCode = CountryCodes.GB,
			};

			Tse = new Exchange
			{
				Name = "TSE",
				FullNameLoc = LocalizedStrings.TokyoStockExchangeKey,
				CountryCode = CountryCodes.JP,
			};

			Hkex = new Exchange
			{
				Name = "HKEX",
				FullNameLoc = LocalizedStrings.HongKongStockExchangeKey,
				CountryCode = CountryCodes.HK,
			};

			Hkfe = new Exchange
			{
				Name = "HKFE",
				FullNameLoc = LocalizedStrings.HongKongFuturesExchangeKey,
				CountryCode = CountryCodes.HK,
			};

			Sse = new Exchange
			{
				Name = "SSE",
				FullNameLoc = LocalizedStrings.ShanghaiStockExchangeKey,
				CountryCode = CountryCodes.CN,
			};

			Szse = new Exchange
			{
				Name = "SZSE",
				FullNameLoc = LocalizedStrings.ShenzhenStockExchangeKey,
				CountryCode = CountryCodes.CN,
			};

			Tsec = new Exchange
			{
				Name = "TSEC",
				FullNameLoc = LocalizedStrings.TaiwanStockExchangeKey,
				CountryCode = CountryCodes.TW,
			};

			Sgx = new Exchange
			{
				Name = "SGX",
				FullNameLoc = LocalizedStrings.SingaporeExchangeKey,
				CountryCode = CountryCodes.SG,
			};

			Pse = new Exchange
			{
				Name = "PSE",
				FullNameLoc = LocalizedStrings.PhilippineStockExchangeKey,
				CountryCode = CountryCodes.PH,
			};

			Klse = new Exchange
			{
				Name = "MYX",
				FullNameLoc = LocalizedStrings.BursaMalaysiaKey,
				CountryCode = CountryCodes.MY,
			};

			Idx = new Exchange
			{
				Name = "IDX",
				FullNameLoc = LocalizedStrings.IndonesiaStockExchangeKey,
				CountryCode = CountryCodes.ID,
			};

			Set = new Exchange
			{
				Name = "SET",
				FullNameLoc = LocalizedStrings.StockExchangeofThailandKey,
				CountryCode = CountryCodes.TH,
			};

			Bse = new Exchange
			{
				Name = "BSE",
				FullNameLoc = LocalizedStrings.BombayStockExchangeKey,
				CountryCode = CountryCodes.IN,
			};

			Nse = new Exchange
			{
				Name = "NSE",
				FullNameLoc = LocalizedStrings.NationalStockExchangeofIndiaKey,
				CountryCode = CountryCodes.IN,
			};

			Cse = new Exchange
			{
				Name = "CSE",
				FullNameLoc = LocalizedStrings.ColomboStockExchangeKey,
				CountryCode = CountryCodes.CO,
			};

			Krx = new Exchange
			{
				Name = "KRX",
				FullNameLoc = LocalizedStrings.KoreaExchangeKey,
				CountryCode = CountryCodes.KR,
			};

			Asx = new Exchange
			{
				Name = "ASX",
				FullNameLoc = LocalizedStrings.AustralianSecuritiesExchangeKey,
				CountryCode = CountryCodes.AU,
			};

			Nzx = new Exchange
			{
				Name = "NZSX",
				FullNameLoc = LocalizedStrings.NewZealandExchangeKey,
				CountryCode = CountryCodes.NZ,
			};

			Tase = new Exchange
			{
				Name = "TASE",
				FullNameLoc = LocalizedStrings.TelAvivStockExchangeKey,
				CountryCode = CountryCodes.IL,
			};

			Fwb = new Exchange
			{
				Name = "FWB",
				FullNameLoc = LocalizedStrings.FrankfurtStockExchangeKey,
				CountryCode = CountryCodes.DE,
			};

			Mse = new Exchange
			{
				Name = "MSE",
				FullNameLoc = LocalizedStrings.MadridStockExchangeKey,
				CountryCode = CountryCodes.ES,
			};

			Swx = new Exchange
			{
				Name = "SWX",
				FullNameLoc = LocalizedStrings.SwissExchangeKey,
				CountryCode = CountryCodes.CH,
			};

			Jse = new Exchange
			{
				Name = "JSE",
				FullNameLoc = LocalizedStrings.JohannesburgStockExchangeKey,
				CountryCode = CountryCodes.ZA,
			};

			Lmax = new Exchange
			{
				Name = "LMAX",
				FullNameLoc = LocalizedStrings.LmaxKey,
				CountryCode = CountryCodes.GB,
			};

			DukasCopy = new Exchange
			{
				Name = "DUKAS",
				FullNameLoc = LocalizedStrings.DukasCopyKey,
				CountryCode = CountryCodes.CH,
			};

			GainCapital = new Exchange
			{
				Name = "GAIN",
				FullNameLoc = LocalizedStrings.GainCapitalKey,
				CountryCode = CountryCodes.US,
			};

			MBTrading = new Exchange
			{
				Name = "MBT",
				FullNameLoc = LocalizedStrings.MBTradingKey,
				CountryCode = CountryCodes.US,
			};

			TrueFX = new Exchange
			{
				Name = "TRUEFX",
				FullNameLoc = LocalizedStrings.TrueFXKey,
				CountryCode = CountryCodes.US,
			};

			Cfh = new Exchange
			{
				Name = "CFH",
				FullNameLoc = LocalizedStrings.CFHKey,
				CountryCode = CountryCodes.GB,
			};

			Ond = new Exchange
			{
				Name = "OANDA",
				FullNameLoc = LocalizedStrings.OandaKey,
				CountryCode = CountryCodes.US,
			};

			Integral = new Exchange
			{
				Name = "INTGRL",
				FullNameLoc = LocalizedStrings.IntegralKey,
				CountryCode = CountryCodes.US,
			};

			Btce = new Exchange
			{
				Name = "BTCE",
				FullNameLoc = LocalizedStrings.BtceKey,
				CountryCode = CountryCodes.RU,
			};

			BitStamp = new Exchange
			{
				Name = "BITSTAMP",
				FullNameLoc = LocalizedStrings.BitStampKey,
				CountryCode = CountryCodes.GB,
			};

			BtcChina = new Exchange
			{
				Name = "BTCCHINA",
				FullNameLoc = LocalizedStrings.BtcChinaKey,
				CountryCode = CountryCodes.CN,
			};

			Icbit = new Exchange
			{
				Name = "ICBIT",
				FullNameLoc = LocalizedStrings.IcBitKey,
				CountryCode = CountryCodes.RU,
			};
		}

		/// <summary>
		/// Information about <see cref="Test"/>.
		/// </summary>
		public static Exchange Test { get; }

		/// <summary>
		/// Information about <see cref="Moex"/>.
		/// </summary>
		public static Exchange Moex { get; }

		/// <summary>
		/// Information about <see cref="Spb"/>.
		/// </summary>
		public static Exchange Spb { get; }

		/// <summary>
		/// Information about <see cref="Ux"/>.
		/// </summary>
		public static Exchange Ux { get; }

		/// <summary>
		/// Information about <see cref="Amex"/>.
		/// </summary>
		public static Exchange Amex { get; }

		/// <summary>
		/// Information about <see cref="Cme"/>.
		/// </summary>
		public static Exchange Cme { get; }

		/// <summary>
		/// Information about <see cref="Cbot"/>.
		/// </summary>
		public static Exchange Cbot { get; }

		/// <summary>
		/// Information about <see cref="Cce"/>.
		/// </summary>
		public static Exchange Cce { get; }

		/// <summary>
		/// Information about <see cref="Nymex"/>.
		/// </summary>
		public static Exchange Nymex { get; }

		/// <summary>
		/// Information about <see cref="Nyse"/>.
		/// </summary>
		public static Exchange Nyse { get; }

		/// <summary>
		/// Information about <see cref="Nasdaq"/>.
		/// </summary>
		public static Exchange Nasdaq { get; }

		/// <summary>
		/// Information about <see cref="Nqlx"/>.
		/// </summary>
		public static Exchange Nqlx { get; }

		/// <summary>
		/// Information about <see cref="Lse"/>.
		/// </summary>
		public static Exchange Lse { get; }

		/// <summary>
		/// Information about <see cref="Lme"/>.
		/// </summary>
		public static Exchange Lme { get; }

		/// <summary>
		/// Information about <see cref="Tse"/>.
		/// </summary>
		public static Exchange Tse { get; }

		/// <summary>
		/// Information about <see cref="Hkex"/>.
		/// </summary>
		public static Exchange Hkex { get; }

		/// <summary>
		/// Information about <see cref="Hkfe"/>.
		/// </summary>
		public static Exchange Hkfe { get; }

		/// <summary>
		/// Information about <see cref="Sse"/>.
		/// </summary>
		public static Exchange Sse { get; }

		/// <summary>
		/// Information about <see cref="Szse"/>.
		/// </summary>
		public static Exchange Szse { get; }

		/// <summary>
		/// Information about <see cref="Tsx"/>.
		/// </summary>
		public static Exchange Tsx { get; }

		/// <summary>
		/// Information about <see cref="Fwb"/>.
		/// </summary>
		public static Exchange Fwb { get; }

		/// <summary>
		/// Information about <see cref="Asx"/>.
		/// </summary>
		public static Exchange Asx { get; }

		/// <summary>
		/// Information about <see cref="Nzx"/>.
		/// </summary>
		public static Exchange Nzx { get; }

		/// <summary>
		/// Information about <see cref="Bse"/>.
		/// </summary>
		public static Exchange Bse { get; }

		/// <summary>
		/// Information about <see cref="Nse"/>.
		/// </summary>
		public static Exchange Nse { get; }

		/// <summary>
		/// Information about <see cref="Swx"/>.
		/// </summary>
		public static Exchange Swx { get; }

		/// <summary>
		/// Information about <see cref="Krx"/>.
		/// </summary>
		public static Exchange Krx { get; }

		/// <summary>
		/// Information about <see cref="Mse"/>.
		/// </summary>
		public static Exchange Mse { get; }

		/// <summary>
		/// Information about <see cref="Jse"/>.
		/// </summary>
		public static Exchange Jse { get; }

		/// <summary>
		/// Information about <see cref="Sgx"/>.
		/// </summary>
		public static Exchange Sgx { get; }

		/// <summary>
		/// Information about <see cref="Tsec"/>.
		/// </summary>
		public static Exchange Tsec { get; }

		/// <summary>
		/// Information about <see cref="Pse"/>.
		/// </summary>
		public static Exchange Pse { get; }

		/// <summary>
		/// Information about <see cref="Klse"/>.
		/// </summary>
		public static Exchange Klse { get; }

		/// <summary>
		/// Information about <see cref="Idx"/>.
		/// </summary>
		public static Exchange Idx { get; }

		/// <summary>
		/// Information about <see cref="Set"/>.
		/// </summary>
		public static Exchange Set { get; }

		/// <summary>
		/// Information about <see cref="Cse"/>.
		/// </summary>
		public static Exchange Cse { get; }

		/// <summary>
		/// Information about <see cref="Tase"/>.
		/// </summary>
		public static Exchange Tase { get; }

		/// <summary>
		/// Information about <see cref="Lmax"/>.
		/// </summary>
		public static Exchange Lmax { get; }

		/// <summary>
		/// Information about <see cref="DukasCopy"/>.
		/// </summary>
		public static Exchange DukasCopy { get; }

		/// <summary>
		/// Information about <see cref="GainCapital"/>.
		/// </summary>
		public static Exchange GainCapital { get; }

		/// <summary>
		/// Information about <see cref="MBTrading"/>.
		/// </summary>
		public static Exchange MBTrading { get; }

		/// <summary>
		/// Information about <see cref="TrueFX"/>.
		/// </summary>
		public static Exchange TrueFX { get; }

		/// <summary>
		/// Information about <see cref="Cfh"/>.
		/// </summary>
		public static Exchange Cfh { get; }

		/// <summary>
		/// Information about <see cref="Ond"/>.
		/// </summary>
		public static Exchange Ond { get; }

		/// <summary>
		/// Information about <see cref="Integral"/>.
		/// </summary>
		public static Exchange Integral { get; }

		/// <summary>
		/// Information about <see cref="Btce"/>.
		/// </summary>
		public static Exchange Btce { get; }

		/// <summary>
		/// Information about <see cref="BitStamp"/>.
		/// </summary>
		public static Exchange BitStamp { get; }

		/// <summary>
		/// Information about <see cref="BtcChina"/>.
		/// </summary>
		public static Exchange BtcChina { get; }

		/// <summary>
		/// Information about <see cref="Icbit"/>.
		/// </summary>
		public static Exchange Icbit { get; }

		/// <summary>
		/// Information about <see cref="Currenex"/>.
		/// </summary>
		public static Exchange Currenex { get; } = new Exchange
		{
			Name = "CURRENEX",
			FullNameLoc = LocalizedStrings.CurrenexKey,
			CountryCode = CountryCodes.US,
		};

		/// <summary>
		/// Information about <see cref="Fxcm"/>.
		/// </summary>
		public static Exchange Fxcm { get; } = new Exchange
		{
			Name = "FXCM",
			FullNameLoc = LocalizedStrings.FxcmKey,
			CountryCode = CountryCodes.US,
		};

		/// <summary>
		/// Information about <see cref="Poloniex"/>.
		/// </summary>
		public static Exchange Poloniex { get; } = new Exchange
		{
			Name = "PLNX",
			FullNameLoc = LocalizedStrings.PoloniexKey,
		};

		/// <summary>
		/// Information about <see cref="Kraken"/>.
		/// </summary>
		public static Exchange Kraken { get; } = new Exchange
		{
			Name = "KRKN",
			FullNameLoc = LocalizedStrings.KrakenKey,
		};

		/// <summary>
		/// Information about <see cref="Bittrex"/>.
		/// </summary>
		public static Exchange Bittrex { get; } = new Exchange
		{
			Name = "BTRX",
			FullNameLoc = LocalizedStrings.BittrexKey,
		};

		/// <summary>
		/// Information about <see cref="Bitfinex"/>.
		/// </summary>
		public static Exchange Bitfinex { get; } = new Exchange
		{
			Name = "BTFX",
			FullNameLoc = LocalizedStrings.BitfinexKey,
		};

		/// <summary>
		/// Information about <see cref="Coinbase"/>.
		/// </summary>
		public static Exchange Coinbase { get; } = new Exchange
		{
			Name = "CNBS",
			FullNameLoc = LocalizedStrings.CoinbaseKey,
		};

		/// <summary>
		/// Information about <see cref="Gdax"/>.
		/// </summary>
		public static Exchange Gdax { get; } = new Exchange
		{
			Name = "GDAX",
			FullNameLoc = LocalizedStrings.GdaxKey,
		};

		/// <summary>
		/// Information about <see cref="Bithumb"/>.
		/// </summary>
		public static Exchange Bithumb { get; } = new Exchange
		{
			Name = "BTHB",
			FullNameLoc = LocalizedStrings.BithumbKey,
		};

		/// <summary>
		/// Information about <see cref="HitBtc"/>.
		/// </summary>
		public static Exchange HitBtc { get; } = new Exchange
		{
			Name = "HTBTC",
			FullNameLoc = LocalizedStrings.HitBtcKey,
		};

		/// <summary>
		/// Information about <see cref="OkCoin"/>.
		/// </summary>
		public static Exchange OkCoin { get; } = new Exchange
		{
			Name = "OKCN",
			FullNameLoc = LocalizedStrings.OkcoinKey,
		};

		/// <summary>
		/// Information about <see cref="Coincheck"/>.
		/// </summary>
		public static Exchange Coincheck { get; } = new Exchange
		{
			Name = "CNCK",
			FullNameLoc = LocalizedStrings.CoincheckKey,
		};

		/// <summary>
		/// Information about <see cref="Binance"/>.
		/// </summary>
		public static Exchange Binance { get; } = new Exchange
		{
			Name = "BNB",
			FullNameLoc = LocalizedStrings.BinanceKey,
		};

		/// <summary>
		/// Information about <see cref="Bitexbook"/>.
		/// </summary>
		public static Exchange Bitexbook { get; } = new Exchange
		{
			Name = "BTXB",
			FullNameLoc = LocalizedStrings.BitexbookKey,
		};

		/// <summary>
		/// Information about <see cref="Bitmex"/>.
		/// </summary>
		public static Exchange Bitmex { get; } = new Exchange
		{
			Name = "BMEX",
			FullNameLoc = LocalizedStrings.BitmexKey,
		};

		/// <summary>
		/// Information about <see cref="Cex"/>.
		/// </summary>
		public static Exchange Cex { get; } = new Exchange
		{
			Name = "CEXIO",
			FullNameLoc = LocalizedStrings.CexKey,
		};

		/// <summary>
		/// Information about <see cref="Cryptopia"/>.
		/// </summary>
		public static Exchange Cryptopia { get; } = new Exchange
		{
			Name = "CRTP",
			FullNameLoc = LocalizedStrings.CryptopiaKey,
		};

		/// <summary>
		/// Information about <see cref="Okex"/>.
		/// </summary>
		public static Exchange Okex { get; } = new Exchange
		{
			Name = "OKEX",
			FullNameLoc = LocalizedStrings.OkexKey,
		};

		/// <summary>
		/// Information about <see cref="Yobit"/>.
		/// </summary>
		public static Exchange Yobit { get; } = new Exchange
		{
			Name = "YBIT",
			FullNameLoc = LocalizedStrings.YobitKey,
		};

		/// <summary>
		/// Information about <see cref="CoinExchange"/>.
		/// </summary>
		public static Exchange CoinExchange { get; } = new Exchange
		{
			Name = "CNEX",
			FullNameLoc = LocalizedStrings.CoinExchangeKey,
		};

		/// <summary>
		/// Information about <see cref="LiveCoin"/>.
		/// </summary>
		public static Exchange LiveCoin { get; } = new Exchange
		{
			Name = "LVCN",
			FullNameLoc = LocalizedStrings.LiveCoinKey,
		};

		/// <summary>
		/// Information about <see cref="Exmo"/>.
		/// </summary>
		public static Exchange Exmo { get; } = new Exchange
		{
			Name = "EXMO",
			FullNameLoc = LocalizedStrings.ExmoKey,
		};

		/// <summary>
		/// Information about <see cref="Deribit"/>.
		/// </summary>
		public static Exchange Deribit { get; } = new Exchange
		{
			Name = "DRBT",
			FullNameLoc = LocalizedStrings.DeribitKey,
		};

		/// <summary>
		/// Information about <see cref="Kucoin"/>.
		/// </summary>
		public static Exchange Kucoin { get; } = new Exchange
		{
			Name = "KUCN",
			FullNameLoc = LocalizedStrings.KucoinKey,
		};

		/// <summary>
		/// Information about <see cref="Liqui"/>.
		/// </summary>
		public static Exchange Liqui { get; } = new Exchange
		{
			Name = "LIQI",
			FullNameLoc = LocalizedStrings.LiquiKey,
		};

		/// <summary>
		/// Information about <see cref="Huobi"/>.
		/// </summary>
		public static Exchange Huobi { get; } = new Exchange
		{
			Name = "HUBI",
			FullNameLoc = LocalizedStrings.HuobiKey,
		};

		/// <summary>
		/// Information about <see cref="IEX"/>.
		/// </summary>
		public static Exchange IEX { get; } = new Exchange
		{
			Name = "IEX",
			FullNameLoc = LocalizedStrings.IEXKey,
			CountryCode = CountryCodes.US,
		};

		/// <summary>
		/// Information about <see cref="AlphaVantage"/>.
		/// </summary>
		public static Exchange AlphaVantage { get; } = new Exchange
		{
			Name = "ALVG",
			FullNameLoc = LocalizedStrings.AlphaVantageKey,
			CountryCode = CountryCodes.US,
		};

		/// <summary>
		/// Information about <see cref="Bitbank"/>.
		/// </summary>
		public static Exchange Bitbank { get; } = new Exchange
		{
			Name = "BTBN",
			FullNameLoc = LocalizedStrings.BitbankKey,
		};

		/// <summary>
		/// Information about <see cref="Zaif"/>.
		/// </summary>
		public static Exchange Zaif { get; } = new Exchange
		{
			Name = "ZAIF",
			FullNameLoc = LocalizedStrings.ZaifKey,
		};

		/// <summary>
		/// Information about <see cref="Quoinex"/>.
		/// </summary>
		public static Exchange Quoinex { get; } = new Exchange
		{
			Name = "QINX",
			FullNameLoc = LocalizedStrings.QuoinexKey,
		};

		/// <summary>
		/// Information about <see cref="Wiki"/>.
		/// </summary>
		public static Exchange Wiki { get; } = new Exchange
		{
			Name = "WIKI",
			FullNameLoc = LocalizedStrings.WIKIKey,
		};

		/// <summary>
		/// Information about <see cref="Idax"/>.
		/// </summary>
		public static Exchange Idax { get; } = new Exchange
		{
			Name = "IDAX",
			FullNameLoc = LocalizedStrings.IdaxKey,
		};

		/// <summary>
		/// Information about <see cref="Digifinex"/>.
		/// </summary>
		public static Exchange Digifinex { get; } = new Exchange
		{
			Name = "DGFX",
			FullNameLoc = LocalizedStrings.DigifinexKey,
		};

		/// <summary>
		/// Information about <see cref="TradeOgre"/>.
		/// </summary>
		public static Exchange TradeOgre { get; } = new Exchange
		{
			Name = "TOGR",
			FullNameLoc = LocalizedStrings.TradeOgreKey,
		};

		/// <summary>
		/// Information about <see cref="CoinCap"/>.
		/// </summary>
		public static Exchange CoinCap { get; } = new Exchange
		{
			Name = "CNCP",
			FullNameLoc = LocalizedStrings.CoinCapKey,
		};

		/// <summary>
		/// Information about <see cref="Coinigy"/>.
		/// </summary>
		public static Exchange Coinigy { get; } = new Exchange
		{
			Name = "CNGY",
			FullNameLoc = LocalizedStrings.CoinigyKey,
		};

		/// <summary>
		/// Information about <see cref="LBank"/>.
		/// </summary>
		public static Exchange LBank { get; } = new Exchange
		{
			Name = "LBNK",
			FullNameLoc = LocalizedStrings.LBankKey,
		};

		/// <summary>
		/// Information about <see cref="BitMax"/>.
		/// </summary>
		public static Exchange BitMax { get; } = new Exchange
		{
			Name = "BMAX",
			FullNameLoc = LocalizedStrings.BitMaxKey,
		};

		/// <summary>
		/// Information about <see cref="BW"/>.
		/// </summary>
		public static Exchange BW { get; } = new Exchange
		{
			Name = "BW",
			FullNameLoc = LocalizedStrings.BWKey,
		};

		/// <summary>
		/// Information about <see cref="Bibox"/>.
		/// </summary>
		public static Exchange Bibox { get; } = new Exchange
		{
			Name = "BBOX",
			FullNameLoc = LocalizedStrings.BiboxKey,
		};

		/// <summary>
		/// Information about <see cref="CoinBene"/>.
		/// </summary>
		public static Exchange CoinBene { get; } = new Exchange
		{
			Name = "CNBN",
			FullNameLoc = LocalizedStrings.CoinBeneKey,
		};

		/// <summary>
		/// Information about <see cref="BitZ"/>.
		/// </summary>
		public static Exchange BitZ { get; } = new Exchange
		{
			Name = "BITZ",
			FullNameLoc = LocalizedStrings.BitZKey,
		};

		/// <summary>
		/// Information about <see cref="ZB"/>.
		/// </summary>
		public static Exchange ZB { get; } = new Exchange
		{
			Name = "ZB",
			FullNameLoc = LocalizedStrings.ZBKey,
		};

		/// <summary>
		/// Information about <see cref="Tradier"/>.
		/// </summary>
		public static Exchange Tradier { get; } = new Exchange
		{
			Name = "TRDR",
			FullNameLoc = LocalizedStrings.TradierKey,
		};

		/// <summary>
		/// Information about <see cref="SwSq"/>.
		/// </summary>
		public static Exchange SwSq { get; } = new Exchange
		{
			Name = "SWSQ",
			FullNameLoc = LocalizedStrings.SwissQuoteKey,
		};

		/// <summary>
		/// Information about <see cref="StockSharp"/>.
		/// </summary>
		public static Exchange StockSharp { get; } = new Exchange
		{
			Name = "STSH",
			FullNameLoc = LocalizedStrings.StockSharpKey,
		};

		/// <summary>
		/// Information about <see cref="Upbit"/>.
		/// </summary>
		public static Exchange Upbit { get; } = new Exchange
		{
			Name = "UPBT",
			FullNameLoc = LocalizedStrings.UpbitKey,
		};

		/// <summary>
		/// Information about <see cref="CoinEx"/>.
		/// </summary>
		public static Exchange CoinEx { get; } = new Exchange
		{
			Name = "CIEX",
			FullNameLoc = LocalizedStrings.CoinExKey,
		};

		/// <summary>
		/// Information about <see cref="FatBtc"/>.
		/// </summary>
		public static Exchange FatBtc { get; } = new Exchange
		{
			Name = "FTBT",
			FullNameLoc = LocalizedStrings.FatBtcKey,
		};

		/// <summary>
		/// Information about <see cref="Latoken"/>.
		/// </summary>
		public static Exchange Latoken { get; } = new Exchange
		{
			Name = "LTKN",
			FullNameLoc = LocalizedStrings.LatokenKey,
		};

		/// <summary>
		/// Information about <see cref="Gopax"/>.
		/// </summary>
		public static Exchange Gopax { get; } = new Exchange
		{
			Name = "GPAX",
			FullNameLoc = LocalizedStrings.GopaxKey,
		};

		/// <summary>
		/// Information about <see cref="CoinHub"/>.
		/// </summary>
		public static Exchange CoinHub { get; } = new Exchange
		{
			Name = "CNHB",
			FullNameLoc = LocalizedStrings.CoinHubKey,
		};

		/// <summary>
		/// Information about <see cref="Hotbit"/>.
		/// </summary>
		public static Exchange Hotbit { get; } = new Exchange
		{
			Name = "HTBT",
			FullNameLoc = LocalizedStrings.HotbitKey,
		};

		/// <summary>
		/// Information about <see cref="Bitalong"/>.
		/// </summary>
		public static Exchange Bitalong { get; } = new Exchange
		{
			Name = "BTLG",
			FullNameLoc = LocalizedStrings.BitalongKey,
		};

		/// <summary>
		/// Information about <see cref="PrizmBit"/>.
		/// </summary>
		public static Exchange PrizmBit { get; } = new Exchange
		{
			Name = "PRZM",
			FullNameLoc = LocalizedStrings.PrizmBitKey,
		};

		/// <summary>
		/// Information about <see cref="DigitexFutures"/>.
		/// </summary>
		public static Exchange DigitexFutures { get; } = new Exchange
		{
			Name = "DGFT",
			FullNameLoc = LocalizedStrings.DigitexFuturesKey,
		};

		/// <summary>
		/// Information about <see cref="Bovespa"/>.
		/// </summary>
		public static Exchange Bovespa { get; } = new Exchange
		{
			Name = "B3",
			FullNameLoc = LocalizedStrings.BrasilBolsaKey,
		};

		/// <summary>
		/// Information about <see cref="IQFeed"/>.
		/// </summary>
		public static Exchange IQFeed { get; } = new Exchange
		{
			Name = "IQFD",
			FullNameLoc = LocalizedStrings.IQFeedKey,
		};

		/// <summary>
		/// Information about <see cref="IBKR"/>.
		/// </summary>
		public static Exchange IBKR { get; } = new Exchange
		{
			Name = "IBKR",
			FullNameLoc = LocalizedStrings.InteractiveBrokersKey,
		};

		/// <summary>
		/// Information about <see cref="STSH"/>.
		/// </summary>
		public static Exchange STSH { get; } = new Exchange
		{
			Name = "STSH",
			FullNameLoc = LocalizedStrings.StockSharpKey,
		};

		/// <summary>
		/// Information about <see cref="STRLG"/>.
		/// </summary>
		public static Exchange STRLG { get; } = new Exchange
		{
			Name = "STRLG",
			FullNameLoc = LocalizedStrings.SterlingKey,
		};

		/// <summary>
		/// Information about <see cref="QNDL"/>.
		/// </summary>
		public static Exchange QNDL { get; } = new Exchange
		{
			Name = "QNDL",
			FullNameLoc = LocalizedStrings.QuandlKey,
		};

		/// <summary>
		/// Information about <see cref="QTFD"/>.
		/// </summary>
		public static Exchange QTFD { get; } = new Exchange
		{
			Name = "QTFD",
			FullNameLoc = LocalizedStrings.QuantFeed,
		};
	}
}
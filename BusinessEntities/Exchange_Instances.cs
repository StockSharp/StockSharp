namespace StockSharp.BusinessEntities;

partial class Exchange
{
	/// <summary>
	/// Information about <see cref="Test"/>.
	/// </summary>
	public static Exchange Test { get; } = new()
	{
		Name = "TEST",
		FullNameLoc = LocalizedStrings.TestExchangeKey,
	};

	/// <summary>
	/// Information about <see cref="Moex"/>.
	/// </summary>
	public static Exchange Moex { get; } = new()
	{
		Name = "MOEX",
		FullNameLoc = LocalizedStrings.MoscowExchangeKey,
		CountryCode = CountryCodes.RU,
	};

	/// <summary>
	/// Information about <see cref="Spb"/>.
	/// </summary>
	public static Exchange Spb { get; } = new()
	{
		Name = "SPB",
		FullNameLoc = LocalizedStrings.SaintPetersburgExchangeKey,
		CountryCode = CountryCodes.RU,
	};

	/// <summary>
	/// Information about <see cref="Ux"/>.
	/// </summary>
	public static Exchange Ux { get; } = new()
	{
		Name = "UX",
		FullNameLoc = LocalizedStrings.UkrainExchangeKey,
		CountryCode = CountryCodes.UA,
	};

	/// <summary>
	/// Information about <see cref="Amex"/>.
	/// </summary>
	public static Exchange Amex { get; } = new()
	{
		Name = "AMEX",
		FullNameLoc = LocalizedStrings.AmericanStockExchangeKey,
		CountryCode = CountryCodes.US,
	};

	/// <summary>
	/// Information about <see cref="Cme"/>.
	/// </summary>
	public static Exchange Cme { get; } = new()
	{
		Name = "CME",
		FullNameLoc = LocalizedStrings.ChicagoMercantileExchangeKey,
		CountryCode = CountryCodes.US,
	};

	/// <summary>
	/// Information about <see cref="Cbot"/>.
	/// </summary>
	public static Exchange Cbot { get; } = new()
	{
		Name = "CBOT",
		FullNameLoc = LocalizedStrings.ChicagoBoardofTradeKey,
		CountryCode = CountryCodes.US,
	};

	/// <summary>
	/// Information about <see cref="Cce"/>.
	/// </summary>
	public static Exchange Cce { get; } = new()
	{
		Name = "CCE",
		FullNameLoc = LocalizedStrings.ChicagoClimateExchangeKey,
		CountryCode = CountryCodes.US,
	};

	/// <summary>
	/// Information about <see cref="Nymex"/>.
	/// </summary>
	public static Exchange Nymex { get; } = new()
	{
		Name = "NYMEX",
		FullNameLoc = LocalizedStrings.NewYorkMercantileExchangeKey,
		CountryCode = CountryCodes.US,
	};

	/// <summary>
	/// Information about <see cref="Nyse"/>.
	/// </summary>
	public static Exchange Nyse { get; } = new()
	{
		Name = "NYSE",
		FullNameLoc = LocalizedStrings.NewYorkStockExchangeKey,
		CountryCode = CountryCodes.US,
	};

	/// <summary>
	/// Information about <see cref="Nasdaq"/>.
	/// </summary>
	public static Exchange Nasdaq { get; } = new()
	{
		Name = "NASDAQ",
		FullNameLoc = LocalizedStrings.NASDAQKey,
		CountryCode = CountryCodes.US,
	};

	/// <summary>
	/// Information about <see cref="Nqlx"/>.
	/// </summary>
	public static Exchange Nqlx { get; } = new()
	{
		Name = "NQLX",
		FullNameLoc = LocalizedStrings.NasdaqLiffeMarketsKey,
		CountryCode = CountryCodes.US,
	};

	/// <summary>
	/// Information about <see cref="Lse"/>.
	/// </summary>
	public static Exchange Lse { get; } = new()
	{
		Name = "LSE",
		FullNameLoc = LocalizedStrings.LondonStockExchangeKey,
		CountryCode = CountryCodes.GB,
	};

	/// <summary>
	/// Information about <see cref="Lme"/>.
	/// </summary>
	public static Exchange Lme { get; } = new()
	{
		Name = "LME",
		FullNameLoc = LocalizedStrings.LondonMetalExchangeKey,
		CountryCode = CountryCodes.GB,
	};

	/// <summary>
	/// Information about <see cref="Tse"/>.
	/// </summary>
	public static Exchange Tse { get; } = new()
	{
		Name = "TSE",
		FullNameLoc = LocalizedStrings.TokyoStockExchangeKey,
		CountryCode = CountryCodes.JP,
	};

	/// <summary>
	/// Information about <see cref="Hkex"/>.
	/// </summary>
	public static Exchange Hkex { get; } = new()
	{
		Name = "HKEX",
		FullNameLoc = LocalizedStrings.HongKongStockExchangeKey,
		CountryCode = CountryCodes.HK,
	};

	/// <summary>
	/// Information about <see cref="Hkfe"/>.
	/// </summary>
	public static Exchange Hkfe { get; } = new()
	{
		Name = "HKFE",
		FullNameLoc = LocalizedStrings.HongKongFuturesExchangeKey,
		CountryCode = CountryCodes.HK,
	};

	/// <summary>
	/// Information about <see cref="Sse"/>.
	/// </summary>
	public static Exchange Sse { get; } = new()
	{
		Name = "SSE",
		FullNameLoc = LocalizedStrings.ShanghaiStockExchangeKey,
		CountryCode = CountryCodes.CN,
	};

	/// <summary>
	/// Information about <see cref="Szse"/>.
	/// </summary>
	public static Exchange Szse { get; } = new()
	{
		Name = "SZSE",
		FullNameLoc = LocalizedStrings.ShenzhenStockExchangeKey,
		CountryCode = CountryCodes.CN,
	};

	/// <summary>
	/// Information about <see cref="Tsx"/>.
	/// </summary>
	public static Exchange Tsx { get; } = new()
	{
		Name = "TSX",
		FullNameLoc = LocalizedStrings.TorontoStockExchangeKey,
		CountryCode = CountryCodes.CA,
	};

	/// <summary>
	/// Information about <see cref="Fwb"/>.
	/// </summary>
	public static Exchange Fwb { get; } = new()
	{
		Name = "FWB",
		FullNameLoc = LocalizedStrings.FrankfurtStockExchangeKey,
		CountryCode = CountryCodes.DE,
	};

	/// <summary>
	/// Information about <see cref="Asx"/>.
	/// </summary>
	public static Exchange Asx { get; } = new()
	{
		Name = "ASX",
		FullNameLoc = LocalizedStrings.AustralianSecuritiesExchangeKey,
		CountryCode = CountryCodes.AU,
	};

	/// <summary>
	/// Information about <see cref="Nzx"/>.
	/// </summary>
	public static Exchange Nzx { get; } = new()
	{
		Name = "NZSX",
		FullNameLoc = LocalizedStrings.NewZealandExchangeKey,
		CountryCode = CountryCodes.NZ,
	};

	/// <summary>
	/// Information about <see cref="Bse"/>.
	/// </summary>
	public static Exchange Bse { get; } = new()
	{
		Name = "BSE",
		FullNameLoc = LocalizedStrings.BombayStockExchangeKey,
		CountryCode = CountryCodes.IN,
	};

	/// <summary>
	/// Information about <see cref="Nse"/>.
	/// </summary>
	public static Exchange Nse { get; } = new()
	{
		Name = "NSE",
		FullNameLoc = LocalizedStrings.NationalStockExchangeofIndiaKey,
		CountryCode = CountryCodes.IN,
	};

	/// <summary>
	/// Information about <see cref="Swx"/>.
	/// </summary>
	public static Exchange Swx { get; } = new()
	{
		Name = "SWX",
		FullNameLoc = LocalizedStrings.SwissExchangeKey,
		CountryCode = CountryCodes.CH,
	};

	/// <summary>
	/// Information about <see cref="Krx"/>.
	/// </summary>
	public static Exchange Krx { get; } = new()
	{
		Name = "KRX",
		FullNameLoc = LocalizedStrings.KoreaExchangeKey,
		CountryCode = CountryCodes.KR,
	};

	/// <summary>
	/// Information about <see cref="Mse"/>.
	/// </summary>
	public static Exchange Mse { get; } = new()
	{
		Name = "MSE",
		FullNameLoc = LocalizedStrings.MadridStockExchangeKey,
		CountryCode = CountryCodes.ES,
	};

	/// <summary>
	/// Information about <see cref="Jse"/>.
	/// </summary>
	public static Exchange Jse { get; } = new()
	{
		Name = "JSE",
		FullNameLoc = LocalizedStrings.JohannesburgStockExchangeKey,
		CountryCode = CountryCodes.ZA,
	};

	/// <summary>
	/// Information about <see cref="Sgx"/>.
	/// </summary>
	public static Exchange Sgx { get; } = new()
	{
		Name = "SGX",
		FullNameLoc = LocalizedStrings.SingaporeExchangeKey,
		CountryCode = CountryCodes.SG,
	};

	/// <summary>
	/// Information about <see cref="Tsec"/>.
	/// </summary>
	public static Exchange Tsec { get; } = new()
	{
		Name = "TSEC",
		FullNameLoc = LocalizedStrings.TaiwanStockExchangeKey,
		CountryCode = CountryCodes.TW,
	};

	/// <summary>
	/// Information about <see cref="Pse"/>.
	/// </summary>
	public static Exchange Pse { get; } = new()
	{
		Name = "PSE",
		FullNameLoc = LocalizedStrings.PhilippineStockExchangeKey,
		CountryCode = CountryCodes.PH,
	};

	/// <summary>
	/// Information about <see cref="Klse"/>.
	/// </summary>
	public static Exchange Klse { get; } = new()
	{
		Name = "MYX",
		FullNameLoc = LocalizedStrings.BursaMalaysiaKey,
		CountryCode = CountryCodes.MY,
	};

	/// <summary>
	/// Information about <see cref="Idx"/>.
	/// </summary>
	public static Exchange Idx { get; } = new()
	{
		Name = "IDX",
		FullNameLoc = LocalizedStrings.IndonesiaStockExchangeKey,
		CountryCode = CountryCodes.ID,
	};

	/// <summary>
	/// Information about <see cref="Set"/>.
	/// </summary>
	public static Exchange Set { get; } = new()
	{
		Name = "SET",
		FullNameLoc = LocalizedStrings.StockExchangeofThailandKey,
		CountryCode = CountryCodes.TH,
	};

	/// <summary>
	/// Information about <see cref="Cse"/>.
	/// </summary>
	public static Exchange Cse { get; } = new()
	{
		Name = "CSE",
		FullNameLoc = LocalizedStrings.ColomboStockExchangeKey,
		CountryCode = CountryCodes.CO,
	};

	/// <summary>
	/// Information about <see cref="Tase"/>.
	/// </summary>
	public static Exchange Tase { get; } = new()
	{
		Name = "TASE",
		FullNameLoc = LocalizedStrings.TelAvivStockExchangeKey,
		CountryCode = CountryCodes.IL,
	};

	/// <summary>
	/// Information about <see cref="Lmax"/>.
	/// </summary>
	public static Exchange Lmax { get; } = new()
	{
		Name = "LMAX",
		FullNameLoc = LocalizedStrings.LmaxKey,
		CountryCode = CountryCodes.GB,
	};

	/// <summary>
	/// Information about <see cref="DukasCopy"/>.
	/// </summary>
	public static Exchange DukasCopy { get; } = new()
	{
		Name = "DUKAS",
		FullNameLoc = LocalizedStrings.DukasCopyKey,
		CountryCode = CountryCodes.CH,
	};

	/// <summary>
	/// Information about <see cref="GainCapital"/>.
	/// </summary>
	public static Exchange GainCapital { get; } = new()
	{
		Name = "GAIN",
		FullNameLoc = LocalizedStrings.GainCapitalKey,
		CountryCode = CountryCodes.US,
	};

	/// <summary>
	/// Information about <see cref="MBTrading"/>.
	/// </summary>
	public static Exchange MBTrading { get; } = new()
	{
		Name = "MBT",
		FullNameLoc = LocalizedStrings.MBTradingKey,
		CountryCode = CountryCodes.US,
	};

	/// <summary>
	/// Information about <see cref="TrueFX"/>.
	/// </summary>
	public static Exchange TrueFX { get; } = new()
	{
		Name = "TRUEFX",
		FullNameLoc = LocalizedStrings.TrueFXKey,
		CountryCode = CountryCodes.US,
	};

	/// <summary>
	/// Information about <see cref="Cfh"/>.
	/// </summary>
	public static Exchange Cfh { get; } = new()
	{
		Name = "CFH",
		FullNameLoc = LocalizedStrings.CFHKey,
		CountryCode = CountryCodes.GB,
	};

	/// <summary>
	/// Information about <see cref="Ond"/>.
	/// </summary>
	public static Exchange Ond { get; } = new()
	{
		Name = "OANDA",
		FullNameLoc = LocalizedStrings.OandaKey,
		CountryCode = CountryCodes.US,
	};

	/// <summary>
	/// Information about <see cref="Integral"/>.
	/// </summary>
	public static Exchange Integral { get; } = new()
	{
		Name = "INTGRL",
		FullNameLoc = LocalizedStrings.IntegralKey,
		CountryCode = CountryCodes.US,
	};

	/// <summary>
	/// Information about <see cref="Btce"/>.
	/// </summary>
	public static Exchange Btce { get; } = new()
	{
		Name = "BTCE",
		FullNameLoc = LocalizedStrings.BtceKey,
		CountryCode = CountryCodes.RU,
	};

	/// <summary>
	/// Information about <see cref="BitStamp"/>.
	/// </summary>
	public static Exchange BitStamp { get; } = new()
	{
		Name = "BITSTAMP",
		FullNameLoc = LocalizedStrings.BitStampKey,
		CountryCode = CountryCodes.GB,
	};

	/// <summary>
	/// Information about <see cref="BtcChina"/>.
	/// </summary>
	public static Exchange BtcChina { get; } = new()
	{
		Name = "BTCCHINA",
		FullNameLoc = LocalizedStrings.BtcChinaKey,
		CountryCode = CountryCodes.CN,
	};

	/// <summary>
	/// Information about <see cref="Icbit"/>.
	/// </summary>
	public static Exchange Icbit { get; } = new()
	{
		Name = "ICBIT",
		FullNameLoc = LocalizedStrings.IcBitKey,
		CountryCode = CountryCodes.RU,
	};

	/// <summary>
	/// Information about <see cref="Currenex"/>.
	/// </summary>
	public static Exchange Currenex { get; } = new()
	{
		Name = "CURRENEX",
		FullNameLoc = LocalizedStrings.CurrenexKey,
		CountryCode = CountryCodes.US,
	};

	/// <summary>
	/// Information about <see cref="Fxcm"/>.
	/// </summary>
	public static Exchange Fxcm { get; } = new()
	{
		Name = "FXCM",
		FullNameLoc = LocalizedStrings.FxcmKey,
		CountryCode = CountryCodes.US,
	};

	/// <summary>
	/// Information about <see cref="Poloniex"/>.
	/// </summary>
	public static Exchange Poloniex { get; } = new()
	{
		Name = "PLNX",
		FullNameLoc = LocalizedStrings.PoloniexKey,
	};

	/// <summary>
	/// Information about <see cref="Kraken"/>.
	/// </summary>
	public static Exchange Kraken { get; } = new()
	{
		Name = "KRKN",
		FullNameLoc = LocalizedStrings.KrakenKey,
	};

	/// <summary>
	/// Information about <see cref="Bittrex"/>.
	/// </summary>
	public static Exchange Bittrex { get; } = new()
	{
		Name = "BTRX",
		FullNameLoc = LocalizedStrings.BittrexKey,
	};

	/// <summary>
	/// Information about <see cref="Bitfinex"/>.
	/// </summary>
	public static Exchange Bitfinex { get; } = new()
	{
		Name = "BTFX",
		FullNameLoc = LocalizedStrings.BitfinexKey,
	};

	/// <summary>
	/// Information about <see cref="Coinbase"/>.
	/// </summary>
	public static Exchange Coinbase { get; } = new()
	{
		Name = "CNBS",
		FullNameLoc = LocalizedStrings.CoinbaseKey,
	};

	/// <summary>
	/// Information about <see cref="Gdax"/>.
	/// </summary>
	public static Exchange Gdax { get; } = new()
	{
		Name = "GDAX",
		FullNameLoc = LocalizedStrings.GdaxKey,
	};

	/// <summary>
	/// Information about <see cref="Bithumb"/>.
	/// </summary>
	public static Exchange Bithumb { get; } = new()
	{
		Name = "BTHB",
		FullNameLoc = LocalizedStrings.BithumbKey,
	};

	/// <summary>
	/// Information about <see cref="HitBtc"/>.
	/// </summary>
	public static Exchange HitBtc { get; } = new()
	{
		Name = "HTBTC",
		FullNameLoc = LocalizedStrings.HitBtcKey,
	};

	/// <summary>
	/// Information about <see cref="OkCoin"/>.
	/// </summary>
	public static Exchange OkCoin { get; } = new()
	{
		Name = "OKCN",
		FullNameLoc = LocalizedStrings.OkcoinKey,
	};

	/// <summary>
	/// Information about <see cref="Coincheck"/>.
	/// </summary>
	public static Exchange Coincheck { get; } = new()
	{
		Name = "CNCK",
		FullNameLoc = LocalizedStrings.CoincheckKey,
	};

	/// <summary>
	/// Information about <see cref="Binance"/>.
	/// </summary>
	public static Exchange Binance { get; } = new()
	{
		Name = "BNB",
		FullNameLoc = LocalizedStrings.BinanceKey,
	};

	/// <summary>
	/// Information about <see cref="Bitexbook"/>.
	/// </summary>
	public static Exchange Bitexbook { get; } = new()
	{
		Name = "BTXB",
		FullNameLoc = LocalizedStrings.BitexbookKey,
	};

	/// <summary>
	/// Information about <see cref="Bitmex"/>.
	/// </summary>
	public static Exchange Bitmex { get; } = new()
	{
		Name = "BMEX",
		FullNameLoc = LocalizedStrings.BitmexKey,
	};

	/// <summary>
	/// Information about <see cref="Cex"/>.
	/// </summary>
	public static Exchange Cex { get; } = new()
	{
		Name = "CEXIO",
		FullNameLoc = LocalizedStrings.CexKey,
	};

	/// <summary>
	/// Information about <see cref="Cryptopia"/>.
	/// </summary>
	public static Exchange Cryptopia { get; } = new()
	{
		Name = "CRTP",
		FullNameLoc = LocalizedStrings.CryptopiaKey,
	};

	/// <summary>
	/// Information about <see cref="Okex"/>.
	/// </summary>
	public static Exchange Okex { get; } = new()
	{
		Name = "OKEX",
		FullNameLoc = LocalizedStrings.OkexKey,
	};

	/// <summary>
	/// Information about <see cref="Bitmart"/>.
	/// </summary>
	public static Exchange Bitmart { get; } = new()
	{
		Name = "BIMA",
		FullNameLoc = LocalizedStrings.BitmartKey,
	};

	/// <summary>
	/// Information about <see cref="Yobit"/>.
	/// </summary>
	public static Exchange Yobit { get; } = new()
	{
		Name = "YBIT",
		FullNameLoc = LocalizedStrings.YobitKey,
	};

	/// <summary>
	/// Information about <see cref="CoinExchange"/>.
	/// </summary>
	public static Exchange CoinExchange { get; } = new()
	{
		Name = "CNEX",
		FullNameLoc = LocalizedStrings.CoinExchangeKey,
	};

	/// <summary>
	/// Information about <see cref="LiveCoin"/>.
	/// </summary>
	public static Exchange LiveCoin { get; } = new()
	{
		Name = "LVCN",
		FullNameLoc = LocalizedStrings.LiveCoinKey,
	};

	/// <summary>
	/// Information about <see cref="Exmo"/>.
	/// </summary>
	public static Exchange Exmo { get; } = new()
	{
		Name = "EXMO",
		FullNameLoc = LocalizedStrings.ExmoKey,
	};

	/// <summary>
	/// Information about <see cref="Deribit"/>.
	/// </summary>
	public static Exchange Deribit { get; } = new()
	{
		Name = "DRBT",
		FullNameLoc = LocalizedStrings.DeribitKey,
	};

	/// <summary>
	/// Information about <see cref="Kucoin"/>.
	/// </summary>
	public static Exchange Kucoin { get; } = new()
	{
		Name = "KUCN",
		FullNameLoc = LocalizedStrings.KucoinKey,
	};

	/// <summary>
	/// Information about <see cref="Liqui"/>.
	/// </summary>
	public static Exchange Liqui { get; } = new()
	{
		Name = "LIQI",
		FullNameLoc = LocalizedStrings.LiquiKey,
	};

	/// <summary>
	/// Information about <see cref="Huobi"/>.
	/// </summary>
	public static Exchange Huobi { get; } = new()
	{
		Name = "HUBI",
		FullNameLoc = LocalizedStrings.HuobiKey,
	};

	/// <summary>
	/// Information about <see cref="IEX"/>.
	/// </summary>
	public static Exchange IEX { get; } = new()
	{
		Name = "IEX",
		FullNameLoc = LocalizedStrings.IEXKey,
		CountryCode = CountryCodes.US,
	};

	/// <summary>
	/// Information about <see cref="AlphaVantage"/>.
	/// </summary>
	public static Exchange AlphaVantage { get; } = new()
	{
		Name = "ALVG",
		FullNameLoc = LocalizedStrings.AlphaVantageKey,
		CountryCode = CountryCodes.US,
	};

	/// <summary>
	/// Information about <see cref="Bitbank"/>.
	/// </summary>
	public static Exchange Bitbank { get; } = new()
	{
		Name = "BTBN",
		FullNameLoc = LocalizedStrings.BitbankKey,
	};

	/// <summary>
	/// Information about <see cref="Zaif"/>.
	/// </summary>
	public static Exchange Zaif { get; } = new()
	{
		Name = "ZAIF",
		FullNameLoc = LocalizedStrings.ZaifKey,
	};

	/// <summary>
	/// Information about <see cref="Quoinex"/>.
	/// </summary>
	public static Exchange Quoinex { get; } = new()
	{
		Name = "QINX",
		FullNameLoc = LocalizedStrings.QuoinexKey,
	};

	/// <summary>
	/// Information about <see cref="Wiki"/>.
	/// </summary>
	public static Exchange Wiki { get; } = new()
	{
		Name = "WIKI",
		FullNameLoc = LocalizedStrings.WIKIKey,
	};

	/// <summary>
	/// Information about <see cref="Idax"/>.
	/// </summary>
	public static Exchange Idax { get; } = new()
	{
		Name = "IDAX",
		FullNameLoc = LocalizedStrings.IdaxKey,
	};

	/// <summary>
	/// Information about <see cref="Digifinex"/>.
	/// </summary>
	public static Exchange Digifinex { get; } = new()
	{
		Name = "DGFX",
		FullNameLoc = LocalizedStrings.DigifinexKey,
	};

	/// <summary>
	/// Information about <see cref="TradeOgre"/>.
	/// </summary>
	public static Exchange TradeOgre { get; } = new()
	{
		Name = "TOGR",
		FullNameLoc = LocalizedStrings.TradeOgreKey,
	};

	/// <summary>
	/// Information about <see cref="CoinCap"/>.
	/// </summary>
	public static Exchange CoinCap { get; } = new()
	{
		Name = "CNCP",
		FullNameLoc = LocalizedStrings.CoinCapKey,
	};

	/// <summary>
	/// Information about <see cref="Coinigy"/>.
	/// </summary>
	public static Exchange Coinigy { get; } = new()
	{
		Name = "CNGY",
		FullNameLoc = LocalizedStrings.CoinigyKey,
	};

	/// <summary>
	/// Information about <see cref="LBank"/>.
	/// </summary>
	public static Exchange LBank { get; } = new()
	{
		Name = "LBNK",
		FullNameLoc = LocalizedStrings.LBankKey,
	};

	/// <summary>
	/// Information about <see cref="BitMax"/>.
	/// </summary>
	public static Exchange BitMax { get; } = new()
	{
		Name = "BMAX",
		FullNameLoc = LocalizedStrings.BitMaxKey,
	};

	/// <summary>
	/// Information about <see cref="BW"/>.
	/// </summary>
	public static Exchange BW { get; } = new()
	{
		Name = "BW",
		FullNameLoc = LocalizedStrings.BWKey,
	};

	/// <summary>
	/// Information about <see cref="Bibox"/>.
	/// </summary>
	public static Exchange Bibox { get; } = new()
	{
		Name = "BBOX",
		FullNameLoc = LocalizedStrings.BiboxKey,
	};

	/// <summary>
	/// Information about <see cref="CoinBene"/>.
	/// </summary>
	public static Exchange CoinBene { get; } = new()
	{
		Name = "CNBN",
		FullNameLoc = LocalizedStrings.CoinBeneKey,
	};

	/// <summary>
	/// Information about <see cref="BitZ"/>.
	/// </summary>
	public static Exchange BitZ { get; } = new()
	{
		Name = "BITZ",
		FullNameLoc = LocalizedStrings.BitZKey,
	};

	/// <summary>
	/// Information about <see cref="ZB"/>.
	/// </summary>
	public static Exchange ZB { get; } = new()
	{
		Name = "ZB",
		FullNameLoc = LocalizedStrings.ZBKey,
	};

	/// <summary>
	/// Information about <see cref="Tradier"/>.
	/// </summary>
	public static Exchange Tradier { get; } = new()
	{
		Name = "TRDR",
		FullNameLoc = LocalizedStrings.TradierKey,
	};

	/// <summary>
	/// Information about <see cref="SwSq"/>.
	/// </summary>
	public static Exchange SwSq { get; } = new()
	{
		Name = "SWSQ",
		FullNameLoc = LocalizedStrings.SwissQuoteKey,
	};

	/// <summary>
	/// Information about <see cref="StockSharp"/>.
	/// </summary>
	public static Exchange StockSharp { get; } = new()
	{
		Name = "STSH",
		FullNameLoc = LocalizedStrings.StockSharpKey,
	};

	/// <summary>
	/// Information about <see cref="Upbit"/>.
	/// </summary>
	public static Exchange Upbit { get; } = new()
	{
		Name = "UPBT",
		FullNameLoc = LocalizedStrings.UpbitKey,
	};

	/// <summary>
	/// Information about <see cref="CoinEx"/>.
	/// </summary>
	public static Exchange CoinEx { get; } = new()
	{
		Name = "CIEX",
		FullNameLoc = LocalizedStrings.CoinExKey,
	};

	/// <summary>
	/// Information about <see cref="FatBtc"/>.
	/// </summary>
	public static Exchange FatBtc { get; } = new()
	{
		Name = "FTBT",
		FullNameLoc = LocalizedStrings.FatBtcKey,
	};

	/// <summary>
	/// Information about <see cref="Latoken"/>.
	/// </summary>
	public static Exchange Latoken { get; } = new()
	{
		Name = "LTKN",
		FullNameLoc = LocalizedStrings.LatokenKey,
	};

	/// <summary>
	/// Information about <see cref="Gopax"/>.
	/// </summary>
	public static Exchange Gopax { get; } = new()
	{
		Name = "GPAX",
		FullNameLoc = LocalizedStrings.GopaxKey,
	};

	/// <summary>
	/// Information about <see cref="CoinHub"/>.
	/// </summary>
	public static Exchange CoinHub { get; } = new()
	{
		Name = "CNHB",
		FullNameLoc = LocalizedStrings.CoinHubKey,
	};

	/// <summary>
	/// Information about <see cref="Hotbit"/>.
	/// </summary>
	public static Exchange Hotbit { get; } = new()
	{
		Name = "HTBT",
		FullNameLoc = LocalizedStrings.HotbitKey,
	};

	/// <summary>
	/// Information about <see cref="Bitalong"/>.
	/// </summary>
	public static Exchange Bitalong { get; } = new()
	{
		Name = "BTLG",
		FullNameLoc = LocalizedStrings.BitalongKey,
	};

	/// <summary>
	/// Information about <see cref="PrizmBit"/>.
	/// </summary>
	public static Exchange PrizmBit { get; } = new()
	{
		Name = "PRZM",
		FullNameLoc = LocalizedStrings.PrizmBitKey,
	};

	/// <summary>
	/// Information about <see cref="DigitexFutures"/>.
	/// </summary>
	public static Exchange DigitexFutures { get; } = new()
	{
		Name = "DGFT",
		FullNameLoc = LocalizedStrings.DigitexFuturesKey,
	};

	/// <summary>
	/// Information about <see cref="Bovespa"/>.
	/// </summary>
	public static Exchange Bovespa { get; } = new()
	{
		Name = "B3",
		FullNameLoc = LocalizedStrings.BrasilBolsaKey,
	};

	/// <summary>
	/// Information about <see cref="Bvmt"/>.
	/// </summary>
	public static Exchange Bvmt { get; } = new()
	{
		Name = "BVMT",
		FullNameLoc = LocalizedStrings.TunisBvmtKey,
	};

	/// <summary>
	/// Information about <see cref="IQFeed"/>.
	/// </summary>
	public static Exchange IQFeed { get; } = new()
	{
		Name = "IQFD",
		FullNameLoc = LocalizedStrings.IQFeedKey,
	};

	/// <summary>
	/// Information about <see cref="IBKR"/>.
	/// </summary>
	public static Exchange IBKR { get; } = new()
	{
		Name = "IBKR",
		FullNameLoc = LocalizedStrings.InteractiveBrokersKey,
	};

	/// <summary>
	/// Information about <see cref="STRLG"/>.
	/// </summary>
	public static Exchange STRLG { get; } = new()
	{
		Name = "STRLG",
		FullNameLoc = LocalizedStrings.SterlingKey,
	};

	/// <summary>
	/// Information about <see cref="QNDL"/>.
	/// </summary>
	public static Exchange QNDL { get; } = new()
	{
		Name = "QNDL",
		FullNameLoc = LocalizedStrings.QuandlKey,
	};

	/// <summary>
	/// Information about <see cref="QTFD"/>.
	/// </summary>
	public static Exchange QTFD { get; } = new()
	{
		Name = "QTFD",
		FullNameLoc = LocalizedStrings.QuantFeed,
	};

	/// <summary>
	/// Information about <see cref="FTX"/>.
	/// </summary>
	public static Exchange FTX { get; } = new()
	{
		Name = "FTX",
		FullNameLoc = LocalizedStrings.FTX,
		CountryCode = CountryCodes.BS,
	};

	/// <summary>
	/// Information about board <see cref="YHF"/>.
	/// </summary>
	public static Exchange YHF { get; } = new()
	{
		Name = "YHF",
		FullNameLoc = LocalizedStrings.Yahoo,
	};

	/// <summary>
	/// Information about board <see cref="EUREX"/>.
	/// </summary>
	public static Exchange EUREX { get; } = new()
	{
		Name = nameof(EUREX),
	};
}
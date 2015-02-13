namespace StockSharp.InteractiveBrokers.Native
{
	/// <summary>
	/// Incoming Tick Types
	/// </summary>
	enum FieldTypes
	{
		/// <summary>
		/// Bid Size
		/// </summary>
		BidVolume = 0,

		/// <summary>
		/// Bid Price
		/// </summary>
		BidPrice = 1,

		/// <summary>
		/// Ask Price
		/// </summary>
		AskPrice = 2,

		/// <summary>
		/// Ask Size
		/// </summary>
		AskVolume = 3,

		/// <summary>
		/// Last Price
		/// </summary>
		LastPrice = 4,

		/// <summary>
		/// Last Size
		/// </summary>
		LastVolume = 5,

		/// <summary>
		/// High Price
		/// </summary>
		HighPrice = 6,

		/// <summary>
		/// Low Price
		/// </summary>
		LowPrice = 7,

		/// <summary>
		/// Volume
		/// </summary>
		Volume = 8,

		/// <summary>
		/// Close Price
		/// </summary>
		ClosePrice = 9,

		/// <summary>
		/// Bid Option
		/// </summary>
		BidOption = 10,

		/// <summary>
		/// Ask Option
		/// </summary>
		AskOption = 11,

		/// <summary>
		/// Last Option
		/// </summary>
		LastOption = 12,

		/// <summary>
		/// Model Option
		/// </summary>
		ModelOption = 13,

		/// <summary>
		/// Open Price
		/// </summary>
		OpenPrice = 14,

		/// <summary>
		/// Low Price over last 13 weeks
		/// </summary>
		Low13Week = 15,

		/// <summary>
		/// High Price over last 13 weeks
		/// </summary>
		High13Week = 16,

		/// <summary>
		/// Low Price over last 26 weeks
		/// </summary>
		Low26Week = 17,

		/// <summary>
		/// High Price over last 26 weeks
		/// </summary>
		High26Week = 18,

		/// <summary>
		/// Low Price over last 52 weeks
		/// </summary>
		Low52Week = 19,

		/// <summary>
		/// High Price over last 52 weeks
		/// </summary>
		High52Week = 20,

		/// <summary>
		/// Average Volume
		/// </summary>
		AverageVolume = 21,

		/// <summary>
		/// Open Interest
		/// </summary>
		OpenInterest = 22,

		/// <summary>
		/// Option Historical Volatility
		/// </summary>
		OptionHistoricalVolatility = 23,

		/// <summary>
		/// Option Implied Volatility
		/// </summary>
		OptionImpliedVolatility = 24,

		/// <summary>
		/// Option Bid Exchange
		/// </summary>
		OptionBidExchange = 25,

		/// <summary>
		/// Option Ask Exchange
		/// </summary>
		OptionAskExchange = 26,

		/// <summary>
		/// Option Call Open Interest
		/// </summary>
		OptionCallOpenInterest = 27,

		/// <summary>
		/// Option Put Open Interest
		/// </summary>
		OptionPutOpenInterest = 28,

		/// <summary>
		/// Option Call Volume
		/// </summary>
		OptionCallVolume = 29,

		/// <summary>
		/// Option Put Volume
		/// </summary>
		OptionPutVolume = 30,

		/// <summary>
		/// Index Future Premium
		/// </summary>
		IndexFuturePremium = 31,

		/// <summary>
		/// Bid Exchange
		/// </summary>
		BidExchange = 32,

		/// <summary>
		/// Ask Exchange
		/// </summary>
		AskExchange = 33,

		/// <summary>
		/// Auction Volume
		/// </summary>
		AuctionVolume = 34,

		/// <summary>
		/// Auction Price
		/// </summary>
		AuctionPrice = 35,

		/// <summary>
		/// Auction Imbalance
		/// </summary>
		AuctionImbalance = 36,

		/// <summary>
		/// Mark Price
		/// </summary>
		MarkPrice = 37,

		/// <summary>
		/// Bid EFP Computation
		/// </summary>
		BidEfpComputation = 38,

		/// <summary>
		/// Ask EFP Computation
		/// </summary>
		AskEfpComputation = 39,

		/// <summary>
		/// Last EFP Computation
		/// </summary>
		LastEfpComputation = 40,

		/// <summary>
		/// Open EFP Computation
		/// </summary>
		OpenEfpComputation = 41,

		/// <summary>
		/// High EFP Computation
		/// </summary>
		HighEfpComputation = 42,

		/// <summary>
		/// Low EFP Computation
		/// </summary>
		LowEfpComputation = 43,

		/// <summary>
		/// Close EFP Computation
		/// </summary>
		CloseEfpComputation = 44,

		/// <summary>
		/// Last Time Stamp
		/// </summary>
		LastTimestamp = 45,

		/// <summary>
		/// Shortable
		/// </summary>
		Shortable = 46,

		/// <summary>
		/// Fundamental Ratios
		/// </summary>
		FundamentalRatios = 47,

		/// <summary>
		/// Real Time Volume
		/// </summary>
		RealTimeVolume = 48,

		/// <summary>
		/// When trading is halted for a contract, TWS receives a special tick: haltedLast=1. When trading is resumed, TWS receives haltedLast=0. A new tick type, HALTED, tick ID = 49, is now available in regular market data via the API to indicate this halted state.
		/// Possible values for this new tick type are:
		/// 0 = Not halted 
		/// 1 = Halted. 
		///  </summary>
		Halted = 49,

		/// <summary>
		/// Bond Yield for Bid Price
		/// </summary>
		BidYield = 50,

		/// <summary>
		/// Bond Yield for Ask Price
		/// </summary>
		AskYield = 51,

		/// <summary>
		/// Bond Yield for Last Price
		/// </summary>
		LastYield = 52,

		/// <summary>
		/// returns calculated implied volatility as a result of an calculateImpliedVolatility( ) request.
		/// </summary>
		CustOptionComputation = 53,

		/// <summary>
		/// Trades
		/// </summary>
		TradeCount = 54,

		/// <summary>
		/// Trades per Minute
		/// </summary>
		TradeRate = 55,

		/// <summary>
		/// Volume per Minute
		/// </summary>
		VolumeRate = 56,

		/// <summary>
		/// Last Regular Trading Hours Trade
		/// </summary>
		LastRthTrade = 57,

		/// <summary>
		/// Real Time Historical Volatility
		/// </summary>
		RealTimeHistoricalVolatility = 58,
	}
}
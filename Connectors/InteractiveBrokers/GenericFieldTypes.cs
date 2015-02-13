namespace StockSharp.InteractiveBrokers
{
	/// <summary>
	/// Поля маркет-данных.
	/// </summary>
	public enum GenericFieldTypes
	{
		/// <summary>
		/// Option Volume
		/// For stocks only.
		/// Returns TickType.OptionCallVolume and TickType.OptionPutVolume 
		/// </summary>
		OptionVolume = 100,

		/// <summary>
		/// Option Open Interest
		/// For stocks only.
		/// Returns TickType.OptionCallOpenInterest and TickType.OptionPutOpenInterest
		/// </summary>
		OptionOpenInterest = 101,

		/// <summary>
		/// Historical Volatility
		/// For stocks only.
		/// Returns TickType.OptionHistoricalVol
		/// </summary>
		HistoricalVolatility = 104,

		/// <summary>
		/// Average Opt Volume
		/// </summary>
		AverageOptVolume = 105,

		/// <summary>
		/// Option Implied Volatility
		/// For stocks only.
		/// Returns TickType.OptionImpliedVol
		/// </summary>
		OptionImpliedVolatility = 106,

		/// <summary>
		/// Close Implied Volatility
		/// </summary>
		CloseImpliedVolatility = 107,

		/// <summary>
		/// Bond analytic data
		/// </summary>
		BondAnalyticData = 125,

		/// <summary>
		/// Index Future Premium
		/// Returns TickType.IndexFuturePremium
		/// </summary>
		IndexFuturePremium = 162,

		/// <summary>
		/// Miscellaneous Stats
		/// Returns TickType.Low13Week, TickType.High13Week, TickType.Low26Week, TickType.High26Week, TickType.Low52Week, TickType.High52Week and TickType.AverageVolume
		/// </summary>
		MiscellaneousStats = 165,

		/// <summary>
		/// CScreen
		/// </summary>
		CScreen = 166,

		/// <summary>
		/// Mark Price
		/// Used in TWS P/L Computations
		/// Returns TickType.MarkPrice
		/// </summary>
		MarkPrice = 221,

		/// <summary>
		/// Auction Price
		/// Auction values (volume, price and imbalance)
		/// Returns TickType.AuctionVolume, TickType.AuctionPrice, TickType.AuctionImbalance
		/// </summary>
		AuctionPrice = 225,

		/// <summary>
		/// Real Time Volume Tick Type
		/// </summary>
		RealTimeVolume = 233,

		/// <summary>
		/// Shortable Ticks
		/// </summary>
		Shortable = 236,

		/// <summary>
		/// Inventory Type
		/// </summary>
		Inventory = 256,

		/// <summary>
		/// Fundamental Ratios Tick Type
		/// </summary>
		FundamentalRatios = 258,

		/// <summary>
		/// Close Implied Volatility
		/// </summary>
		CloseImpliedVolatility2 = 291,

		/// <summary>
		/// Trade Count
		/// </summary>
		TradeCount = 293,

		/// <summary>
		/// Trade Rate
		/// </summary>
		TradeRate = 294,

		/// <summary>
		/// Volume Rate
		/// </summary>
		VolumeRate = 295,

		/// <summary>
		/// Last RTH Trade
		/// </summary>
		LastRthTrade = 318,

		/// <summary>
		/// Participation Monitor
		/// </summary>
		ParticipationMonitor = 370,

		/// <summary>
		/// Ctt Tick Tag
		/// </summary>
		CttTickTag = 377,

		/// <summary>
		/// IB Rate
		/// </summary>
		IBRate = 381,

		/// <summary>
		/// Rfq Tick Resp Tag
		/// </summary>
		RfqTickRespTag = 384,

		/// <summary>
		/// DMM
		/// </summary>
		Dmm = 387,

		/// <summary>
		/// Issuer Fundamentals
		/// </summary>
		IssuerFundamentals = 388,

		/// <summary>
		/// IB Warrant Imp Vol Compete Tick
		/// </summary>
		IBWarrantImpVolCompeteTick = 391,

		/// <summary>
		/// Futures Margins
		/// </summary>
		FuturesMargins = 407,

		/// <summary>
		/// Realtime Historical Volatility
		/// </summary>
		RealTimeHistoricalVolatility = 411,

		/// <summary>
		/// Monetary Close Price
		/// </summary>
		MonetaryClosePrice = 428,

		/// <summary>
		/// RTCLOSE
		/// </summary>
		RealTimeClose = 438,

		/// <summary>
		/// MonitorTickTag
		/// </summary>
		MonitorTickTag = 439,
	}
}
namespace StockSharp.Messages;

/// <summary>
/// The interfaces describes candle.
/// </summary>
public interface ICandleMessage :
	ISecurityIdMessage, ISeqNumMessage, IGeneratedMessage,
	ILocalTimeMessage, IServerTimeMessage
{
	/// <summary>
	/// Opening price.
	/// </summary>
	decimal OpenPrice { get; set; }

	/// <summary>
	/// Highest price.
	/// </summary>
	decimal HighPrice { get; set; }

	/// <summary>
	/// Lowest price.
	/// </summary>
	decimal LowPrice { get; set; }

	/// <summary>
	/// Closing price.
	/// </summary>
	decimal ClosePrice { get; set; }

	/// <summary>
	/// Open time.
	/// </summary>
	DateTimeOffset OpenTime { get; set; }

	/// <summary>
	/// Close time.
	/// </summary>
	DateTimeOffset CloseTime { get; set; }

	/// <summary>
	/// High time.
	/// </summary>
	DateTimeOffset HighTime { get; set; }

	/// <summary>
	/// Low time.
	/// </summary>
	DateTimeOffset LowTime { get; set; }

	/// <summary>
	/// State.
	/// </summary>
	CandleStates State { get; set; }

	/// <summary>
	/// Price levels.
	/// </summary>
	IEnumerable<CandlePriceLevel> PriceLevels { get; set; }

	/// <summary>
	/// <see cref="DataType"/>.
	/// </summary>
	DataType DataType { get; set; }

	/// <summary>
	/// Total price size.
	/// </summary>
	decimal TotalPrice { get; set; }

	/// <summary>
	/// Volume at open.
	/// </summary>
	decimal? OpenVolume { get; set; }

	/// <summary>
	/// Volume at close.
	/// </summary>
	decimal? CloseVolume { get; set; }

	/// <summary>
	/// Volume at high.
	/// </summary>
	decimal? HighVolume { get; set; }

	/// <summary>
	/// Volume at low.
	/// </summary>
	decimal? LowVolume { get; set; }

	/// <summary>
	/// Total volume.
	/// </summary>
	decimal TotalVolume { get; set; }

	/// <summary>
	/// Relative volume.
	/// </summary>
	decimal? RelativeVolume { get; set; }

	/// <summary>
	/// Buy volume.
	/// </summary>
	decimal? BuyVolume { get; set; }

	/// <summary>
	/// Sell volume.
	/// </summary>
	decimal? SellVolume { get; set; }

	/// <summary>
	/// Number of ticks.
	/// </summary>
	int? TotalTicks { get; set; }

	/// <summary>
	/// Number of up trending ticks.
	/// </summary>
	int? UpTicks { get; set; }

	/// <summary>
	/// Number of down trending ticks.
	/// </summary>
	int? DownTicks { get; set; }

	/// <summary>
	/// Open interest.
	/// </summary>
	decimal? OpenInterest { get; set; }

	/// <summary>
	/// Type of argument.
	/// </summary>
	Type ArgType { get; }
}

/// <summary>
/// The interfaces describes candle.
/// </summary>
/// <typeparam name="TArg">Type of <see cref="TypedArg"/>.</typeparam>
public interface ICandleMessage<TArg> : ICandleMessage
{
	/// <summary>
	/// Arg.
	/// </summary>
	TArg TypedArg { get; set; }
}

/// <summary>
/// Time-frame candle.
/// </summary>
public interface ITimeFrameCandleMessage : ICandleMessage<TimeSpan>
{
}

/// <summary>
/// Tick candle.
/// </summary>
public interface ITickCandleMessage : ICandleMessage<int>
{
}

/// <summary>
/// Volume candle.
/// </summary>
public interface IVolumeCandleMessage : ICandleMessage<decimal>
{
}

/// <summary>
/// Range candle.
/// </summary>
public interface IRangeCandleMessage : ICandleMessage<Unit>
{
}

/// <summary>
/// X0 candle.
/// </summary>
public interface IPnFCandleMessage : ICandleMessage<PnFArg>
{
}

/// <summary>
/// Renko candle.
/// </summary>
public interface IRenkoCandleMessage : ICandleMessage<Unit>
{
}

/// <summary>
/// Heikin-Ashi candle.
/// </summary>
public interface IHeikinAshiCandleMessage : ITimeFrameCandleMessage
{
}
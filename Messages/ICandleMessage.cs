namespace StockSharp.Messages;

using System;
using System.Collections.Generic;

/// <summary>
/// The interfaces describes candle.
/// </summary>
public interface ICandleMessage
{
	/// <summary>
	/// Security ID.
	/// </summary>
	SecurityId SecurityId { get; }

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
	/// Candle arg.
	/// </summary>
	object Arg { get; set; }

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
	/// Sequence number.
	/// </summary>
	/// <remarks>Zero means no information.</remarks>
	long SeqNum { get; set; }

	/// <summary>
	/// Determines the message is generated from the specified <see cref="DataType"/>.
	/// </summary>
	DataType BuildFrom { get; set; }
}
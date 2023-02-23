namespace StockSharp.Messages;

using System;

/// <summary>
/// The interfaces describes candle.
/// </summary>
public interface ICandleMessage
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
	/// Total volume.
	/// </summary>
	decimal TotalVolume { get; set; }

	/// <summary>
	/// Open time.
	/// </summary>
	DateTimeOffset OpenTime { get; set; }
}
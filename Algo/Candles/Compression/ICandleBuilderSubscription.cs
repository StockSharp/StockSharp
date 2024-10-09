﻿namespace StockSharp.Algo.Candles.Compression;

/// <summary>
/// Interface described candles subscription.
/// </summary>
public interface ICandleBuilderSubscription
{
	/// <summary>
	/// Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).
	/// </summary>
	MarketDataMessage Message { get; }

	/// <summary>
	/// Volume profile.
	/// </summary>
	VolumeProfileBuilder VolumeProfile { get; set; }

	/// <summary>
	/// The current candle.
	/// </summary>
	CandleMessage CurrentCandle { get; set; }
}

/// <summary>
/// Default implementation of <see cref="ICandleBuilderSubscription"/>.
/// </summary>
public class CandleBuilderSubscription : ICandleBuilderSubscription
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CandleBuilderSubscription"/>.
	/// </summary>
	/// <param name="message">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
	public CandleBuilderSubscription(MarketDataMessage message)
	{
		_message = message ?? throw new System.ArgumentNullException(nameof(message));
	}

	private readonly MarketDataMessage _message;
	MarketDataMessage ICandleBuilderSubscription.Message => _message;

	VolumeProfileBuilder ICandleBuilderSubscription.VolumeProfile { get; set; }
	CandleMessage ICandleBuilderSubscription.CurrentCandle { get; set; }
}
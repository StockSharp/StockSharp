namespace StockSharp.Messages
{
	using System;

	/// <summary>
	/// The types of messages.
	/// </summary>
	public enum MessageTypes
	{
		/// <summary>
		/// Security info.
		/// </summary>
		Security,

		/// <summary>
		/// Level1 market-data changes.
		/// </summary>
		Level1Change,

		/// <summary>
		/// Register new order.
		/// </summary>
		OrderRegister,

		/// <summary>
		/// Modify order.
		/// </summary>
		OrderReplace,

		/// <summary>
		/// Pair order move.
		/// </summary>
		OrderPairReplace,

		/// <summary>
		/// Cancel order.
		/// </summary>
		OrderCancel,

		/// <summary>
		/// Order group cancel.
		/// </summary>
		OrderGroupCancel,

		/// <summary>
		/// Time change.
		/// </summary>
		Time,

		/// <summary>
		/// News.
		/// </summary>
		News,

		/// <summary>
		/// Order error (registration or cancel).
		/// </summary>
		OrderError,

		/// <summary>
		/// Portfolio.
		/// </summary>
		Portfolio,

		/// <summary>
		/// Position.
		/// </summary>
		Position,

		/// <summary>
		/// Candle (time-frame).
		/// </summary>
		CandleTimeFrame,

		/// <summary>
		/// Quotes change.
		/// </summary>
		QuoteChange,

		/// <summary>
		/// Order execution.
		/// </summary>
		Execution,

		/// <summary>
		/// Position change.
		/// </summary>
		PositionChange,

		/// <summary>
		/// Portfolio change.
		/// </summary>
		PortfolioChange,

		/// <summary>
		/// Subscribe/unsubscribe market-data.
		/// </summary>
		MarketData,

		/// <summary>
		/// Association <see cref="SecurityId"/> with <see cref="SecurityId.Native"/>.
		/// </summary>
		[Obsolete]
		NativeSecurityId,

		/// <summary>
		/// Connection string.
		/// </summary>
		Connect,

		/// <summary>
		/// Disconnect.
		/// </summary>
		Disconnect,

		/// <summary>
		/// Securities search.
		/// </summary>
		SecurityLookup,

		/// <summary>
		/// Portfolio lookup.
		/// </summary>
		PortfolioLookup,

		/// <summary>
		/// Security lookup result.
		/// </summary>
		SecurityLookupResult,

		/// <summary>
		/// Error.
		/// </summary>
		Error,

		/// <summary>
		/// Session.
		/// </summary>
		Session,

		/// <summary>
		/// Order state request.
		/// </summary>
		OrderStatus,

		/// <summary>
		/// Electronic board info.
		/// </summary>
		Board,

		/// <summary>
		/// Portfolio lookup result.
		/// </summary>
		PortfolioLookupResult,

		/// <summary>
		/// Password change.
		/// </summary>
		ChangePassword,

		/// <summary>
		/// Clear message queueu.
		/// </summary>
		ClearQueue,

		/// <summary>
		/// Candle (tick).
		/// </summary>
		CandleTick,

		/// <summary>
		/// Candle (volume).
		/// </summary>
		CandleVolume,

		/// <summary>
		/// Candle (range).
		/// </summary>
		CandleRange,

		/// <summary>
		/// Candle (X&amp;0).
		/// </summary>
		CandlePnF,

		/// <summary>
		/// Candle (renko).
		/// </summary>
		CandleRenko,

		/// <summary>
		/// Reset state.
		/// </summary>
		Reset
	}
}
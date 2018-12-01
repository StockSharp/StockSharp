#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: MessageTypes.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
		[Obsolete]
		OrderError,

		/// <summary>
		/// Portfolio.
		/// </summary>
		Portfolio,

		/// <summary>
		/// Position.
		/// </summary>
		[Obsolete]
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
		BoardState,

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
		/// Clear message queue.
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
		Reset,

		/// <summary>
		/// Market data request finished.
		/// </summary>
		MarketDataFinished,

		/// <summary>
		/// Remove object request (security, portfolio etc.).
		/// </summary>
		Remove,

		/// <summary>
		/// User info.
		/// </summary>
		UserInfo,

		/// <summary>
		/// Users search.
		/// </summary>
		UserLookup,

		/// <summary>
		/// Users search result.
		/// </summary>
		UserLookupResult,

		/// <summary>
		/// Board subscription request.
		/// </summary>
		BoardRequest,

		/// <summary>
		/// Boards search.
		/// </summary>
		BoardLookup,

		/// <summary>
		/// Boards search result.
		/// </summary>
		BoardLookupResult,

		/// <summary>
		/// User subscription request.
		/// </summary>
		UserRequest,

		/// <summary>
		/// Time-frames search.
		/// </summary>
		TimeFrameLookup,

		/// <summary>
		/// Time-frames search result.
		/// </summary>
		TimeFrameLookupResult,
	}
}
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

	using Ecng.ComponentModel;

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
		/// Connect.
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
		[Obsolete]
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

		/// <summary>
		/// <see cref="SecurityMappingRequestMessage"/>.
		/// </summary>
		SecurityMappingRequest,

		/// <summary>
		/// <see cref="SecurityMappingResultMessage"/>.
		/// </summary>
		SecurityMappingResult,

		/// <summary>
		/// <see cref="SecurityLegsRequestMessage"/>.
		/// </summary>
		SecurityLegsRequest,

		/// <summary>
		/// <see cref="SecurityLegsResultMessage"/>.
		/// </summary>
		SecurityLegsResult,

		/// <summary>
		/// <see cref="AdapterListRequestMessage"/>.
		/// </summary>
		AdapterListRequest,

		/// <summary>
		/// <see cref="AdapterListFinishedMessage"/>.
		/// </summary>
		AdapterListFinished,

		/// <summary>
		/// <see cref="AdapterCommandMessage"/>.
		/// </summary>
		AdapterCommand,

		/// <summary>
		/// <see cref="AdapterResponseMessage"/>.
		/// </summary>
		AdapterResponse,

		/// <summary>
		/// <see cref="SubscriptionListRequestMessage"/>.
		/// </summary>
		SubscriptionListRequest,

		/// <summary>
		/// <see cref="SubscriptionListFinishedMessage"/>.
		/// </summary>
		SubscriptionListFinished,

		/// <summary>
		/// <see cref="SecurityRouteListRequestMessage"/>.
		/// </summary>
		SecurityRouteListRequest,

		/// <summary>
		/// <see cref="SecurityRouteMessage"/>.
		/// </summary>
		SecurityRoute,

		/// <summary>
		/// <see cref="SecurityRouteListFinishedMessage"/>.
		/// </summary>
		SecurityRouteListFinished,

		/// <summary>
		/// <see cref="PortfolioRouteListRequestMessage"/>.
		/// </summary>
		PortfolioRouteListRequest,

		/// <summary>
		/// <see cref="PortfolioRouteMessage"/>.
		/// </summary>
		PortfolioRoute,

		/// <summary>
		/// <see cref="PortfolioRouteListFinishedMessage"/>.
		/// </summary>
		PortfolioRouteListFinished,

		/// <summary>
		/// <see cref="SecurityMappingMessage"/>.
		/// </summary>
		SecurityMapping
	}

	/// <summary>
	/// Extended info for <see cref="MessageTypes"/>.
	/// </summary>
	public class MessageTypeInfo
	{
		/// <summary>
		/// Message type.
		/// </summary>
		public MessageTypes Type { get; }

		/// <summary>
		/// <see cref="Type"/> is market-data type.
		/// </summary>
		public bool? IsMarketData { get; }

		/// <summary>
		/// Display name.
		/// </summary>
		public string DisplayName { get; }

		/// <summary>
		/// Description.
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MessageTypeInfo"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		/// <param name="isMarketData"><see cref="Type"/> is market-data type.</param>
		public MessageTypeInfo(MessageTypes type, bool? isMarketData)
			: this(type, isMarketData, type.GetDisplayName(), null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MessageTypeInfo"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		/// <param name="isMarketData"><see cref="Type"/> is market-data type.</param>
		/// <param name="displayName">Display name.</param>
		/// <param name="description">Description.</param>
		public MessageTypeInfo(MessageTypes type, bool? isMarketData, string displayName, string description)
		{
			Type = type;
			IsMarketData = isMarketData;
			DisplayName = displayName;
			Description = description;
		}

		/// <inheritdoc />
		public override string ToString() => DisplayName;
	}
}
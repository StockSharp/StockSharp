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
		/// <see cref="SecurityMessage"/>.
		/// </summary>
		Security,

		/// <summary>
		/// <see cref="Level1ChangeMessage"/>.
		/// </summary>
		Level1Change,

		/// <summary>
		/// <see cref="OrderRegisterMessage"/>.
		/// </summary>
		OrderRegister,

		/// <summary>
		/// <see cref="OrderReplaceMessage"/>.
		/// </summary>
		OrderReplace,

		/// <summary>
		/// <see cref="OrderPairReplaceMessage"/>.
		/// </summary>
		OrderPairReplace,

		/// <summary>
		/// <see cref="OrderCancelMessage"/>.
		/// </summary>
		OrderCancel,

		/// <summary>
		/// <see cref="OrderGroupCancelMessage"/>.
		/// </summary>
		OrderGroupCancel,

		/// <summary>
		/// <see cref="TimeMessage"/>.
		/// </summary>
		Time,

		/// <summary>
		/// <see cref="NewsMessage"/>.
		/// </summary>
		News,

		/// <summary>
		/// Order error (registration or cancel).
		/// </summary>
		[Obsolete]
		OrderError,

		/// <summary>
		/// <see cref="PortfolioMessage"/>.
		/// </summary>
		Portfolio,

		/// <summary>
		/// Position.
		/// </summary>
		[Obsolete]
		Position,

		/// <summary>
		/// <see cref="TimeFrameCandleMessage"/>.
		/// </summary>
		CandleTimeFrame,

		/// <summary>
		/// <see cref="QuoteChangeMessage"/>.
		/// </summary>
		QuoteChange,

		/// <summary>
		/// <see cref="ExecutionMessage"/>.
		/// </summary>
		Execution,

		/// <summary>
		/// <see cref="PositionChangeMessage"/>.
		/// </summary>
		PositionChange,

		/// <summary>
		/// <see cref="PortfolioChangeMessage"/>.
		/// </summary>
		PortfolioChange,

		/// <summary>
		/// <see cref="MarketDataMessage"/>.
		/// </summary>
		MarketData,

		/// <summary>
		/// Association <see cref="SecurityId"/> with <see cref="SecurityId.Native"/>.
		/// </summary>
		[Obsolete]
		NativeSecurityId,

		/// <summary>
		/// <see cref="ConnectMessage"/>.
		/// </summary>
		Connect,

		/// <summary>
		/// <see cref="DisconnectMessage"/>.
		/// </summary>
		Disconnect,

		/// <summary>
		/// <see cref="SecurityLookupMessage"/>.
		/// </summary>
		SecurityLookup,

		/// <summary>
		/// <see cref="PortfolioLookupMessage"/>.
		/// </summary>
		PortfolioLookup,

		/// <summary>
		/// <see cref="SecurityLookupResultMessage"/>.
		/// </summary>
		SecurityLookupResult,

		/// <summary>
		/// <see cref="ErrorMessage"/>.
		/// </summary>
		Error,

		/// <summary>
		/// <see cref="BoardStateMessage"/>.
		/// </summary>
		BoardState,

		/// <summary>
		/// <see cref="OrderStatusMessage"/>.
		/// </summary>
		OrderStatus,

		/// <summary>
		/// <see cref="BoardMessage"/>.
		/// </summary>
		Board,

		/// <summary>
		/// <see cref="PortfolioLookupResultMessage"/>.
		/// </summary>
		PortfolioLookupResult,

		/// <summary>
		/// <see cref="ChangePasswordMessage"/>.
		/// </summary>
		ChangePassword,

		/// <summary>
		/// Clear message queue.
		/// </summary>
		[Obsolete]
		ClearQueue,

		/// <summary>
		/// <see cref="TickCandleMessage"/>.
		/// </summary>
		CandleTick,

		/// <summary>
		/// <see cref="VolumeCandleMessage"/>.
		/// </summary>
		CandleVolume,

		/// <summary>
		/// <see cref="RangeCandleMessage"/>.
		/// </summary>
		CandleRange,

		/// <summary>
		/// <see cref="PnFCandleMessage"/>.
		/// </summary>
		CandlePnF,

		/// <summary>
		/// <see cref="RenkoCandleMessage"/>.
		/// </summary>
		CandleRenko,

		/// <summary>
		/// <see cref="ResetMessage"/>.
		/// </summary>
		Reset,

		/// <summary>
		/// <see cref="MarketDataFinishedMessage"/>.
		/// </summary>
		MarketDataFinished,

		/// <summary>
		/// <see cref="RemoveMessage"/>.
		/// </summary>
		Remove,

		/// <summary>
		/// <see cref="UserInfoMessage"/>.
		/// </summary>
		UserInfo,

		/// <summary>
		/// <see cref="UserLookupMessage"/>.
		/// </summary>
		UserLookup,

		/// <summary>
		/// <see cref="UserLookupResultMessage"/>.
		/// </summary>
		UserLookupResult,

		/// <summary>
		/// Board subscription request.
		/// </summary>
		[Obsolete]
		BoardRequest,

		/// <summary>
		/// <see cref="BoardLookupMessage"/>.
		/// </summary>
		BoardLookup,

		/// <summary>
		/// <see cref="BoardLookupResultMessage"/>.
		/// </summary>
		BoardLookupResult,

		/// <summary>
		/// <see cref="UserRequestMessage"/>.
		/// </summary>
		UserRequest,

		/// <summary>
		/// <see cref="TimeFrameLookupMessage"/>.
		/// </summary>
		TimeFrameLookup,

		/// <summary>
		/// <see cref="TimeFrameLookupResultMessage"/>.
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
		SecurityMapping,
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
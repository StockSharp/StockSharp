namespace StockSharp.Messages;

/// <summary>
/// The types of messages.
/// </summary>
public enum MessageTypes
{
	/// <summary>
	/// <see cref="SecurityMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SecurityKey)]
	Security,

	/// <summary>
	/// <see cref="Level1ChangeMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.Level1Key)]
	Level1Change,

	/// <summary>
	/// <see cref="OrderRegisterMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OrderRegisteringKey)]
	OrderRegister,

	/// <summary>
	/// <see cref="OrderReplaceMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ReregisteringKey)]
	OrderReplace,

	/// <summary>
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OrderPairReplaceKey)]
	OrderPairReplace,

	/// <summary>
	/// <see cref="OrderCancelMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OrderCancellingKey)]
	OrderCancel,

	/// <summary>
	/// <see cref="OrderGroupCancelMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OrderGroupCancelKey)]
	OrderGroupCancel,

	/// <summary>
	/// <see cref="TimeMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TimeKey)]
	Time,

	/// <summary>
	/// <see cref="NewsMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.NewsKey)]
	News,

	/// <summary>
	/// Order error (registration or cancel).
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OrderFailKey)]
	OrderError,

	/// <summary>
	/// <see cref="PortfolioMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PortfolioKey)]
	Portfolio,

	/// <summary>
	/// Position.
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PositionKey)]
	Position,

	/// <summary>
	/// <see cref="TimeFrameCandleMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TimeFrameCandleKey)]
	CandleTimeFrame,

	/// <summary>
	/// <see cref="QuoteChangeMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MarketDepthKey)]
	QuoteChange,

	/// <summary>
	/// <see cref="ExecutionMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ExecutionKey)]
	Execution,

	/// <summary>
	/// <see cref="PositionChangeMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PositionChangeKey)]
	PositionChange,

	/// <summary>
	/// Obsolete.
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PortfolioChangeKey)]
	PortfolioChange,

	/// <summary>
	/// <see cref="MarketDataMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MarketDataKey)]
	MarketData,

	/// <summary>
	/// Association <see cref="SecurityId"/> with <see cref="SecurityId.Native"/>.
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.NativeIdKey)]
	NativeSecurityId,

	/// <summary>
	/// <see cref="ConnectMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ConnectKey)]
	Connect,

	/// <summary>
	/// <see cref="DisconnectMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DisconnectKey)]
	Disconnect,

	/// <summary>
	/// <see cref="SecurityLookupMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SecurityLookupKey)]
	SecurityLookup,

	/// <summary>
	/// <see cref="PortfolioLookupMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PortfolioLookupKey)]
	PortfolioLookup,

	/// <summary>
	/// Obsolete.
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SecurityLookupResultKey)]
	SecurityLookupResult,

	/// <summary>
	/// <see cref="ErrorMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ErrorKey)]
	Error,

	/// <summary>
	/// <see cref="BoardStateMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SessionStateKey)]
	BoardState,

	/// <summary>
	/// <see cref="OrderStatusMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OrderStateKey)]
	OrderStatus,

	/// <summary>
	/// <see cref="BoardMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BoardKey)]
	Board,

	/// <summary>
	/// Obsolete.
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PortfolioLookupResultKey)]
	PortfolioLookupResult,

	/// <summary>
	/// <see cref="ChangePasswordMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ChangePasswordKey)]
	ChangePassword,

	/// <summary>
	/// Clear message queue.
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ClearQueueKey)]
	ClearQueue,

	/// <summary>
	/// <see cref="TickCandleMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TickCandleKey)]
	CandleTick,

	/// <summary>
	/// <see cref="VolumeCandleMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.VolumeCandleKey)]
	CandleVolume,

	/// <summary>
	/// <see cref="RangeCandleMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.RangeCandleKey)]
	CandleRange,

	/// <summary>
	/// <see cref="PnFCandleMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PnFCandleKey)]
	CandlePnF,

	/// <summary>
	/// <see cref="RenkoCandleMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.RenkoCandleKey)]
	CandleRenko,

	/// <summary>
	/// <see cref="ResetMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ResetKey)]
	Reset,

	/// <summary>
	/// <see cref="SubscriptionFinishedMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SubscriptionCompletedKey)]
	SubscriptionFinished,

	/// <summary>
	/// <see cref="RemoveMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.RemoveKey)]
	Remove,

	/// <summary>
	/// <see cref="UserInfoMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.UserInfoKey)]
	UserInfo,

	/// <summary>
	/// <see cref="UserLookupMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.UserLookupKey)]
	UserLookup,

	/// <summary>
	/// Obsolete.
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.UserLookupResultKey)]
	UserLookupResult,

	/// <summary>
	/// Board subscription request.
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BoardRequestKey)]
	BoardRequest,

	/// <summary>
	/// <see cref="BoardLookupMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BoardLookupKey)]
	BoardLookup,

	/// <summary>
	/// Obsolete.
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BoardLookupResultKey)]
	BoardLookupResult,

	/// <summary>
	/// <see cref="UserRequestMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.UserRequestKey)]
	UserRequest,

	/// <summary>
	/// <see cref="DataTypeLookupMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DataTypeLookupKey)]
	DataTypeLookup,

	/// <summary>
	/// <see cref="DataTypeInfoMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DataTypeInfoKey)]
	DataTypeInfo,

	/// <summary>
	/// 
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SecurityMappingRequestKey)]
	SecurityMappingRequest,

	/// <summary>
	/// 
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SecurityMappingInfoKey)]
	SecurityMappingInfo,

	/// <summary>
	/// <see cref="SecurityLegsRequestMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SecurityLegsRequestKey)]
	SecurityLegsRequest,

	/// <summary>
	/// <see cref="SecurityLegsInfoMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SecurityLegsInfoKey)]
	SecurityLegsInfo,

	/// <summary>
	/// 
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AdapterListRequestKey)]
	AdapterListRequest,

	/// <summary>
	/// Obsolete.
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AdapterListFinishedKey)]
	AdapterListFinished,

	/// <summary>
	/// <see cref="CommandMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CommandKey)]
	Command,

	/// <summary>
	/// 
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AdapterResponseKey)]
	AdapterResponse,

	/// <summary>
	/// <see cref="SubscriptionListRequestMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SubscriptionListRequestKey)]
	SubscriptionListRequest,

	/// <summary>
	/// Obsolete.
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SubscriptionListFinishedKey)]
	SubscriptionListFinished,

	/// <summary>
	/// 
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SecurityRouteListRequestKey)]
	SecurityRouteListRequest,

	/// <summary>
	/// 
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SecurityRouteKey)]
	SecurityRoute,

	/// <summary>
	/// Obsolete.
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SecurityRouteListFinishedKey)]
	SecurityRouteListFinished,

	/// <summary>
	/// 
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PortfolioRouteListRequestKey)]
	PortfolioRouteListRequest,

	/// <summary>
	///
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PortfolioRouteKey)]
	PortfolioRoute,

	/// <summary>
	/// Obsolete.
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PortfolioRouteListFinishedKey)]
	PortfolioRouteListFinished,

	/// <summary>
	/// <see cref="SecurityMappingMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SecurityMappingKey)]
	SecurityMapping,

	/// <summary>
	/// <see cref="SubscriptionOnlineMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SubscriptionIsOnlineKey)]
	SubscriptionOnline,

	/// <summary>
	/// <see cref="SubscriptionResponseMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SubscriptionResponseKey)]
	SubscriptionResponse,

	/// <summary>
	/// <see cref="HeikinAshiCandleMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.HeikinAshiKey)]
	CandleHeikinAshi,

	/// <summary>
	/// <see cref="ProcessSuspendedMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ProcessSuspendedKey)]
	ProcessSuspended,

	/// <summary>
	/// <see cref="EmulationStateMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.EmulationStateKey)]
	EmulationState,

	/// <summary>
	/// 
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ChangeTimeIntervalKey)]
	ChangeTimeInterval,

	/// <summary>
	/// 
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StrategyInfoKey)]
	StrategyInfo,

	/// <summary>
	/// 
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StrategyTypeKey)]
	StrategyType,

	/// <summary>
	/// 
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StrategyStateKey)]
	StrategyState,

	/// <summary>
	/// <see cref="RemoteFileMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.RemoteFileKey)]
	RemoteFile,

	/// <summary>
	/// <see cref="RemoteFileCommandMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.RemoteFileCommandKey)]
	RemoteFileCommand,

	/// <summary>
	/// 
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StrategySubscriptionInfoKey)]
	StrategySubscriptionInfo,

	/// <summary>
	/// 
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StrategyBacktestResultKey)]
	StrategyBacktestResult,

	/// <summary>
	/// 
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AvailableDataRequestKey)]
	AvailableDataRequest,

	/// <summary>
	/// 
	/// </summary>
	[Obsolete]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AvailableDataInfoKey)]
	AvailableDataInfo,

	/// <summary>
	/// <see cref="ConnectionLostMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ConnectionLostKey)]
	ConnectionLost,

	/// <summary>
	/// <see cref="ConnectionRestoredMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ConnectionRestoredKey)]
	ConnectionRestored,

	/// <summary>
	/// <see cref="IndicatorMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.IndicatorKey)]
	Indicator,

	/// <summary>
	/// <see cref="IndicatorInfoMessage"/>.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.IndicatorInfoKey)]
	IndicatorInfo,
}

/// <summary>
/// Extended info for <see cref="MessageTypes"/>.
/// </summary>
public class MessageTypeInfo : Equatable<MessageTypeInfo>
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

	/// <summary>
	/// Compare <see cref="MessageTypeInfo"/> on the equivalence.
	/// </summary>
	/// <param name="other">Another value with which to compare.</param>
	/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
	protected override bool OnEquals(MessageTypeInfo other) => other.Type == Type;

	/// <inheritdoc cref="object.GetHashCode" />
	public override int GetHashCode() => Type.GetHashCode();

	/// <summary>
	/// Create a copy of <see cref="MessageTypeInfo"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override MessageTypeInfo Clone() => new(Type, IsMarketData, DisplayName, Description);
}
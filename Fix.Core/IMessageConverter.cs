namespace StockSharp.Fix;

/// <summary>
/// Bidirectional conversion between FIX record structs and StockSharp messages.
/// </summary>
public interface IMessageConverter
{
	#region Transaction requests

	/// <summary>
	/// Convert FIX NewOrderSingle to StockSharp OrderRegisterMessage.
	/// </summary>
	OrderRegisterMessage ToMessage(FixNewOrderSingle fix);

	/// <summary>
	/// Convert StockSharp OrderRegisterMessage to FIX NewOrderSingle.
	/// </summary>
	FixNewOrderSingle ToFixNewOrderSingle(OrderRegisterMessage message);

	/// <summary>
	/// Convert FIX OrderCancelRequest to StockSharp OrderCancelMessage.
	/// </summary>
	OrderCancelMessage ToMessage(FixOrderCancelRequest fix);

	/// <summary>
	/// Convert StockSharp OrderCancelMessage to FIX OrderCancelRequest.
	/// </summary>
	FixOrderCancelRequest ToFixOrderCancelRequest(OrderCancelMessage message);

	/// <summary>
	/// Convert FIX OrderCancelReplaceRequest to StockSharp OrderReplaceMessage.
	/// </summary>
	OrderReplaceMessage ToMessage(FixOrderCancelReplaceRequest fix);

	/// <summary>
	/// Convert StockSharp OrderReplaceMessage to FIX OrderCancelReplaceRequest.
	/// </summary>
	FixOrderCancelReplaceRequest ToFixOrderCancelReplaceRequest(OrderReplaceMessage message);

	/// <summary>
	/// Convert FIX OrderMassCancelRequest to StockSharp OrderGroupCancelMessage.
	/// </summary>
	OrderGroupCancelMessage ToMessage(FixOrderMassCancelRequest fix);

	/// <summary>
	/// Convert StockSharp OrderGroupCancelMessage to FIX OrderMassCancelRequest.
	/// </summary>
	FixOrderMassCancelRequest ToFixOrderMassCancelRequest(OrderGroupCancelMessage message);

	/// <summary>
	/// Convert FIX OrderStatusRequest to StockSharp OrderStatusMessage.
	/// </summary>
	OrderStatusMessage ToMessage(FixOrderStatusRequest fix);

	/// <summary>
	/// Convert StockSharp OrderStatusMessage to FIX OrderStatusRequest.
	/// </summary>
	FixOrderStatusRequest ToFixOrderStatusRequest(OrderStatusMessage message);

	/// <summary>
	/// Convert FIX OrderMassStatusRequest to StockSharp OrderStatusMessage (mass request).
	/// </summary>
	OrderStatusMessage ToMessage(FixOrderMassStatusRequest fix);

	/// <summary>
	/// Convert StockSharp OrderStatusMessage (mass request) to FIX OrderMassStatusRequest.
	/// </summary>
	FixOrderMassStatusRequest ToFixOrderMassStatusRequest(OrderStatusMessage message);

	#endregion

	#region Session messages

	/// <summary>
	/// Convert FIX TestRequest to StockSharp TimeMessage.
	/// </summary>
	TimeMessage ToMessage(FixTestRequest fix);

	/// <summary>
	/// Convert StockSharp TimeMessage to FIX TestRequest.
	/// </summary>
	FixTestRequest ToFixTestRequest(TimeMessage message);

	/// <summary>
	/// Convert FIX SequenceReset data to sequence numbers.
	/// </summary>
	(bool GapFill, long NewSeqNo) ToSequenceReset(FixSequenceReset fix);

	/// <summary>
	/// Convert sequence reset parameters to FIX SequenceReset.
	/// </summary>
	FixSequenceReset ToFixSequenceReset(bool gapFill, long newSeqNo);

	/// <summary>
	/// Convert FIX ResendRequest data to sequence range.
	/// </summary>
	(long BeginSeqNo, long EndSeqNo) ToResendRange(FixResendRequest fix);

	/// <summary>
	/// Convert sequence range to FIX ResendRequest.
	/// </summary>
	FixResendRequest ToFixResendRequest(long beginSeqNo, long endSeqNo);

	/// <summary>
	/// Convert FIX TradingSessionStatusRequest to StockSharp BoardLookupMessage.
	/// </summary>
	BoardLookupMessage ToMessage(FixTradingSessionStatusRequest fix);

	/// <summary>
	/// Convert StockSharp BoardLookupMessage to FIX TradingSessionStatusRequest.
	/// </summary>
	FixTradingSessionStatusRequest ToFixTradingSessionStatusRequest(BoardLookupMessage message);

	#endregion

	#region Security requests

	/// <summary>
	/// Convert FIX SecurityListRequest to StockSharp SecurityLookupMessage.
	/// </summary>
	SecurityLookupMessage ToMessage(FixSecurityListRequest fix);

	/// <summary>
	/// Convert StockSharp SecurityLookupMessage to FIX SecurityListRequest.
	/// </summary>
	FixSecurityListRequest ToFixSecurityListRequest(SecurityLookupMessage message);

	/// <summary>
	/// Convert FIX SecurityStatusRequest to StockSharp SecurityMessage.
	/// </summary>
	SecurityMessage ToMessage(FixSecurityStatusRequest fix);

	/// <summary>
	/// Convert StockSharp SecurityMessage to FIX SecurityStatusRequest.
	/// </summary>
	FixSecurityStatusRequest ToFixSecurityStatusRequest(SecurityMessage message);

	/// <summary>
	/// Convert FIX SecurityLegsRequest to StockSharp SecurityLegsRequestMessage.
	/// </summary>
	SecurityLegsRequestMessage ToMessage(FixSecurityLegsRequest fix);

	/// <summary>
	/// Convert StockSharp SecurityLegsRequestMessage to FIX SecurityLegsRequest.
	/// </summary>
	FixSecurityLegsRequest ToFixSecurityLegsRequest(SecurityLegsRequestMessage message);

	/// <summary>
	/// Convert FIX SecurityMapping to StockSharp SecurityMappingMessage.
	/// </summary>
	SecurityMappingMessage ToMessage(FixSecurityMapping fix);

	/// <summary>
	/// Convert StockSharp SecurityMappingMessage to FIX SecurityMapping.
	/// </summary>
	FixSecurityMapping ToFixSecurityMapping(SecurityMappingMessage message);

	#endregion

	#region Position requests

	/// <summary>
	/// Convert FIX RequestForPositions to StockSharp PortfolioLookupMessage.
	/// </summary>
	PortfolioLookupMessage ToMessage(FixRequestForPositions fix);

	/// <summary>
	/// Convert StockSharp PortfolioLookupMessage to FIX RequestForPositions.
	/// </summary>
	FixRequestForPositions ToFixRequestForPositions(PortfolioLookupMessage message);

	#endregion

	#region User requests

	/// <summary>
	/// Convert FIX UserRequest to StockSharp UserRequestMessage.
	/// </summary>
	UserRequestMessage ToMessage(FixUserRequest fix);

	/// <summary>
	/// Convert StockSharp UserRequestMessage to FIX UserRequest.
	/// </summary>
	FixUserRequest ToFixUserRequest(UserRequestMessage message);

	/// <summary>
	/// Convert FIX UserRequestEx to StockSharp UserLookupMessage.
	/// </summary>
	UserLookupMessage ToMessage(FixUserRequestEx fix);

	/// <summary>
	/// Convert StockSharp UserLookupMessage to FIX UserRequestEx.
	/// </summary>
	FixUserRequestEx ToFixUserRequestEx(UserLookupMessage message);

	#endregion

	#region Other requests

	/// <summary>
	/// Convert FIX BoardLookup to StockSharp BoardLookupMessage.
	/// </summary>
	BoardLookupMessage ToMessage(FixBoardLookup fix);

	/// <summary>
	/// Convert StockSharp BoardLookupMessage to FIX BoardLookup.
	/// </summary>
	FixBoardLookup ToFixBoardLookup(BoardLookupMessage message);

	/// <summary>
	/// Convert FIX BoardUpdate to StockSharp BoardMessage.
	/// </summary>
	BoardMessage ToMessage(FixBoardUpdate fix);

	/// <summary>
	/// Convert StockSharp BoardMessage to FIX BoardUpdate.
	/// </summary>
	FixBoardUpdate ToFixBoardUpdate(BoardMessage message);

	/// <summary>
	/// Convert FIX Remove to StockSharp RemoveMessage.
	/// </summary>
	RemoveMessage ToMessage(FixRemove fix);

	/// <summary>
	/// Convert StockSharp RemoveMessage to FIX Remove.
	/// </summary>
	FixRemove ToFixRemove(RemoveMessage message);

	/// <summary>
	/// Convert FIX HistoryStart to history parameters.
	/// </summary>
	(string RequestId, DateTime? From) ToHistoryStart(FixHistoryStart fix);

	/// <summary>
	/// Convert history parameters to FIX HistoryStart.
	/// </summary>
	FixHistoryStart ToFixHistoryStart(string requestId, DateTime? from);

	/// <summary>
	/// Convert FIX DataTypeLookup to StockSharp DataTypeLookupMessage.
	/// </summary>
	DataTypeLookupMessage ToMessage(FixDataTypeLookup fix);

	/// <summary>
	/// Convert StockSharp DataTypeLookupMessage to FIX DataTypeLookup.
	/// </summary>
	FixDataTypeLookup ToFixDataTypeLookup(DataTypeLookupMessage message);

	#endregion

	#region Responses and reports

	/// <summary>
	/// Convert StockSharp ExecutionMessage to FIX ExecutionReport.
	/// </summary>
	FixExecutionReport ToFixExecutionReport(ExecutionMessage message, string ordStatusReqId, string massStatusReqId, string origClOrdId, string clOrdId);

	/// <summary>
	/// Convert FIX ExecutionReport to StockSharp ExecutionMessage.
	/// </summary>
	ExecutionMessage ToMessage(FixExecutionReport fix);

	/// <summary>
	/// Convert StockSharp PositionChangeMessage to FIX PositionReport.
	/// </summary>
	FixPositionReport ToFixPositionReport(PositionChangeMessage message, string posReqId);

	/// <summary>
	/// Convert position parameters to FIX PositionReport.
	/// </summary>
	FixPositionReport ToFixPositionReport(string posReqId, string account, string symbol, string securityExchange,
		SecurityTypes? securityType, TPlusLimits? limitType, string clientCode, CurrencyTypes? currency,
		string strategyId, Sides? side, DataType buildFrom, string depoName, string description,
		ICollection<KeyValuePair<PositionChangeTypes, object>> changes, DateTime? transactTime = null);

	/// <summary>
	/// Convert FIX PositionReport to StockSharp PositionChangeMessage.
	/// </summary>
	PositionChangeMessage ToMessage(FixPositionReport fix);

	/// <summary>
	/// Convert StockSharp SecurityMessage collection to FIX SecurityListResponse.
	/// </summary>
	FixSecurityListResponse ToFixSecurityListResponse(ICollection<SecurityMessage> securities, string securityReqId, string securityResponseId, bool lastFragment);

	/// <summary>
	/// Convert FIX SecurityListResponse to StockSharp SecurityMessage collection with metadata.
	/// </summary>
	(ICollection<SecurityMessage> Securities, string SecurityReqId, string SecurityResponseId, bool LastFragment) ToMessage(FixSecurityListResponse fix);

	/// <summary>
	/// Convert error to FIX SecurityListResponseError.
	/// </summary>
	FixSecurityListResponseError ToFixSecurityListResponseError(string securityReqId, string securityResponseId, Exception error);

	/// <summary>
	/// Convert StockSharp NewsMessage to FIX News.
	/// </summary>
	FixNews ToFixNews(NewsMessage message, string mdResponseId);

	/// <summary>
	/// Convert FIX News to StockSharp NewsMessage.
	/// </summary>
	NewsMessage ToMessage(FixNews fix);

	/// <summary>
	/// Convert StockSharp UserInfoMessage to FIX UserInfo.
	/// </summary>
	FixUserInfo ToFixUserInfo(UserInfoMessage message, string userRequestId);

	/// <summary>
	/// Convert FIX UserInfo to StockSharp UserInfoMessage.
	/// </summary>
	UserInfoMessage ToMessage(FixUserInfo fix);

	/// <summary>
	/// Convert user response parameters to FIX UserResponse.
	/// </summary>
	FixUserResponse ToFixUserResponse(string userRequestId, string userName, UserStatus status, string text);

	/// <summary>
	/// Convert FIX UserResponse to user response parameters.
	/// </summary>
	(string UserRequestId, string UserName, UserStatus Status, string Text) ToMessage(FixUserResponse fix);

	/// <summary>
	/// Convert StockSharp SubscriptionResponseMessage to FIX SubscriptionResponse.
	/// </summary>
	FixSubscriptionResponse ToFixSubscriptionResponse(SubscriptionResponseMessage message, string mdReqId, string mdResponseId);

	/// <summary>
	/// Convert error to FIX SubscriptionResponse.
	/// </summary>
	FixSubscriptionResponse ToFixSubscriptionResponse(string mdReqId, string mdResponseId, string error);

	/// <summary>
	/// Convert FIX SubscriptionResponse to StockSharp SubscriptionResponseMessage.
	/// </summary>
	SubscriptionResponseMessage ToMessage(FixSubscriptionResponse fix);

	/// <summary>
	/// Convert StockSharp SubscriptionFinishedMessage to FIX SubscriptionFinished.
	/// </summary>
	FixSubscriptionFinished ToFixSubscriptionFinished(SubscriptionFinishedMessage message, string mdReqId);

	/// <summary>
	/// Convert FIX SubscriptionFinished to StockSharp SubscriptionFinishedMessage.
	/// </summary>
	SubscriptionFinishedMessage ToMessage(FixSubscriptionFinished fix);

	/// <summary>
	/// Convert StockSharp SubscriptionOnlineMessage to FIX SubscriptionOnline.
	/// </summary>
	FixSubscriptionOnline ToFixSubscriptionOnline(string mdReqId);

	/// <summary>
	/// Convert FIX SubscriptionOnline to StockSharp SubscriptionOnlineMessage.
	/// </summary>
	SubscriptionOnlineMessage ToMessage(FixSubscriptionOnline fix);

	/// <summary>
	/// Convert order cancel reject parameters to FIX OrderCancelReject.
	/// </summary>
	FixOrderCancelReject ToFixOrderCancelReject(string clOrdId, string orderId, string errorText, DateTime transactTime, char cxlRejResponseTo);

	/// <summary>
	/// Convert FIX OrderCancelReject to order cancel reject parameters.
	/// </summary>
	(string ClOrdId, string OrderId, string ErrorText, DateTime TransactTime) ToMessage(FixOrderCancelReject fix);

	/// <summary>
	/// Convert order mass cancel report parameters to FIX OrderMassCancelReport.
	/// </summary>
	FixOrderMassCancelReport ToFixOrderMassCancelReport(string clOrdId, char massCancelRequestType, string errorMessage);

	/// <summary>
	/// Convert FIX OrderMassCancelReport to order mass cancel report parameters.
	/// </summary>
	(string ClOrdId, char MassCancelRequestType, string ErrorMessage) ToMessage(FixOrderMassCancelReport fix);

	/// <summary>
	/// Convert market data entries to FIX MarketDataSnapshotFullRefresh.
	/// </summary>
	FixMarketDataSnapshotFullRefresh ToFixMarketDataSnapshotFullRefresh(SecurityId securityId, string mdReqId, DataType buildFrom, ICollection<MDEntry> entries);

	/// <summary>
	/// Convert FIX MarketDataSnapshotFullRefresh to market data parameters.
	/// </summary>
	(SecurityId SecurityId, string MdReqId, DataType BuildFrom, ICollection<MDEntry> Entries) ToMessage(FixMarketDataSnapshotFullRefresh fix);

	/// <summary>
	/// Convert market data entries to FIX MarketDataIncrementalRefresh.
	/// </summary>
	FixMarketDataIncrementalRefresh ToFixMarketDataIncrementalRefresh(string symbol, string securityExchange, string mdReqId, DataType buildFrom, ICollection<MDEntry> entries);

	/// <summary>
	/// Convert FIX MarketDataIncrementalRefresh to market data parameters.
	/// </summary>
	(string Symbol, string SecurityExchange, string MdReqId, DataType BuildFrom, ICollection<MDEntry> Entries) ToMessage(FixMarketDataIncrementalRefresh fix);

	/// <summary>
	/// Convert error to FIX MarketDataRequestReject.
	/// </summary>
	FixMarketDataRequestReject ToFixMarketDataRequestReject(string mdReqId, Exception error);

	/// <summary>
	/// Convert StockSharp BoardStateMessage to FIX TradingSessionStatus.
	/// </summary>
	FixTradingSessionStatus ToFixTradingSessionStatus(string requestId, BoardStateMessage message);

	/// <summary>
	/// Convert StockSharp BoardMessage to FIX Board.
	/// </summary>
	FixBoard ToFixBoard(string requestId, BoardMessage message);

	/// <summary>
	/// Convert StockSharp DataTypeInfoMessage to FIX DataTypeInfo.
	/// </summary>
	FixDataTypeInfo ToFixDataTypeInfo(string requestId, DataTypeInfoMessage message);

	/// <summary>
	/// Convert logon response parameters to FIX LogonResponse.
	/// </summary>
	FixLogonResponse ToFixLogonResponse(string sessionId, string licenseFeatureId, DateTime componentTimestamp);

	/// <summary>
	/// Convert reject parameters to FIX Reject.
	/// </summary>
	FixReject ToFixReject(string error, long? refSeqNum, string refMsgType, FixTags? refTagId, int? sessionRejectReason, string requestId);

	/// <summary>
	/// Convert heartbeat parameters to FIX Heartbeat.
	/// </summary>
	FixHeartbeat ToFixHeartbeat(string testReqId);

	/// <summary>
	/// Convert security legs to FIX SecurityLegsInfo.
	/// </summary>
	FixSecurityLegsInfo ToFixSecurityLegsInfo(string requestId, IDictionary<SecurityId, IEnumerable<SecurityId>> legs);

	/// <summary>
	/// Convert StockSharp MarketDataMessage to FIX Subscription.
	/// </summary>
	FixSubscription ToFixSubscription(string requestId, string responseId, MarketDataMessage message);

	/// <summary>
	/// Convert StockSharp RemoteFileMessage to FIX RemoteFile.
	/// </summary>
	FixRemoteFile ToFixRemoteFile(string requestId, RemoteFileMessage message);

	#endregion

	#region Market data requests

	/// <summary>
	/// Convert FIX MarketDataRequest to StockSharp MarketDataMessage collection.
	/// One FIX request may generate multiple S# messages (per security/datatype combination).
	/// </summary>
	IEnumerable<MarketDataMessage> ToMessages(FixMarketDataRequest fix);

	/// <summary>
	/// Convert StockSharp MarketDataMessage to FIX MarketDataRequest.
	/// </summary>
	FixMarketDataRequest ToFixMarketDataRequest(MarketDataMessage message, FixId mdReqId, FixId mdResponseId);

	#endregion
}

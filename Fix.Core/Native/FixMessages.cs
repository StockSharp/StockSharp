namespace StockSharp.Fix.Native;

/// <summary>
/// FIX messages codes.
/// </summary>
public static class FixMessages
{
	private static readonly Dictionary<string, string> _messagesByType = [];

	static FixMessages()
	{
		foreach (var type in new[] { typeof(FixMessages), typeof(FixExtendedMessages) })
		{
			foreach (var field in type.GetFields())
			{
				if (!field.IsLiteral)
					continue;

				var code = (string)field.GetValue(null);

				if (!_messagesByType.TryAdd2(code, field.Name))
					throw new InvalidOperationException($"Duplicate '{code}'.");
			}
		}
	}

	/// <summary>
	/// To get the message name by its type.
	/// </summary>
	/// <param name="msgType">Message type.</param>
	/// <returns>The message name.</returns>
	public static string GetName(string msgType)
		=> _messagesByType.TryGetValue(msgType) ?? msgType;

	/// <summary>
	/// The response to ping.
	/// </summary>
	public const string Heartbeat = "0";

	/// <summary>
	/// Ping request.
	/// </summary>
	public const string TestRequest = "1";

	/// <summary>
	/// Get counter request.
	/// </summary>
	public const string ResendRequest = "2";

	/// <summary>
	/// The response with the error description.
	/// </summary>
	public const string Reject = "3";

	/// <summary>
	/// The request for counter reset.
	/// </summary>
	public const string SequenceReset = "4";

	/// <summary>
	/// Exit.
	/// </summary>
	public const string Logout = "5";

	/// <summary>
	/// Information about order or trade.
	/// </summary>
	public const string ExecutionReport = "8";

	/// <summary>
	/// The response with error to <see cref="OrderCancelRequest"/>.
	/// </summary>
	public const string OrderCancelReject = "9";

	/// <summary>
	/// Input.
	/// </summary>
	public const string Logon = "A";

	/// <summary>
	/// The request for the getting the orders status.
	/// </summary>
	public const string OrderMassStatusRequest = "AF";

	/// <summary>
	/// The response with error to <see cref="QuoteRequest"/>.
	/// </summary>
	public const string QuoteRequestReject = "AG";

	/// <summary>
	/// The request the following purposes in markets that employ tradeable or restricted tradeable quotes.
	/// </summary>
	public const string QuoteStatusRequest = "a";

	/// <summary>
	/// The response with error to <see cref="QuoteStatusRequest"/>.
	/// </summary>
	public const string QuoteStatusReport = "AI";

	/// <summary>
	/// The request for the getting positions.
	/// </summary>
	public const string RequestForPositions = "AN";

	/// <summary>
	/// The response to <see cref="RequestForPositions"/>.
	/// </summary>
	public const string RequestForPositionsAck = "AO";

	/// <summary>
	/// The position information.
	/// </summary>
	public const string PositionReport = "AP";

	/// <summary>
	/// Information about news.
	/// </summary>
	public const string News = "B";

	/// <summary>
	/// The request for user change.
	/// </summary>
	public const string UserRequest = "BE";

	/// <summary>
	/// The response to <see cref="UserRequest"/>.
	/// </summary>
	public const string UserResponse = "BF";

	/// <summary>
	/// The request for instruments getting.
	/// </summary>
	public const string SecurityDefinitionRequest = "c";

	/// <summary>
	/// Security info.
	/// </summary>
	public const string SecurityDefinition = "d";

	/// <summary>
	/// Order registration.
	/// </summary>
	public const string NewOrderSingle = "D";

	/// <summary>
	/// The request for order cancel.
	/// </summary>
	public const string OrderCancelRequest = "F";

	/// <summary>
	/// The request for order change.
	/// </summary>
	public const string OrderCancelReplaceRequest = "G";

	/// <summary>
	/// The response to a request with the error description.
	/// </summary>
	public const string BusinessMessageReject = "j";

	/// <summary>
	/// The request for the getting the order status.
	/// </summary>
	public const string OrderStatusRequest = "H";

	/// <summary>
	/// The request for the getting the session status.
	/// </summary>
	public const string TradingSessionStatusRequest = "g";

	/// <summary>
	/// The response with the session state.
	/// </summary>
	public const string TradingSessionStatus = "h";

	/// <summary>
	/// The request for the orders cancel.
	/// </summary>
	public const string OrderMassCancelRequest = "q";

	/// <summary>
	/// The request for the getting quotes.
	/// </summary>
	public const string QuoteRequest = "R";

	/// <summary>
	/// Information about quotes.
	/// </summary>
	public const string Quote = "S";

	/// <summary>
	/// Quotes for multiple securities.
	/// </summary>
	public const string MassQuote = "i";

	/// <summary>
	/// The response with error to <see cref="MarketDataRequest"/>.
	/// </summary>
	public const string MarketDataRequestReject = "Y";

	/// <summary>
	/// Information about securities.
	/// </summary>
	public const string SecurityList = "y";

	/// <summary>
	/// The request for the getting market data.
	/// </summary>
	public const string MarketDataRequest = "V";

	/// <summary>
	/// Information (incremental) about market data.
	/// </summary>
	public const string MarketDataIncrementalRefresh = "X";

	/// <summary>
	/// The request for instruments getting.
	/// </summary>
	public const string SecurityListRequest = "x";

	/// <summary>
	/// Information about market data.
	/// </summary>
	public const string MarketDataSnapshotFullRefresh = "W";

	/// <summary>
	/// The request for the quotes cancel.
	/// </summary>
	public const string QuoteCancel = "Z";

	/// <summary>
	/// The response to <see cref="OrderMassCancelRequest"/>.
	/// </summary>
	public const string OrderMassCancelReport = "r";

	/// <summary>
	/// Request the status of a security.
	/// </summary>
	public const string SecurityStatusRequest = "e";

	/// <summary>
	/// Report changes in status to a security.
	/// </summary>
	public const string SecurityStatus = "f";

	/// <summary>
	/// This message is used to request a retransmission of a set of one or more messages generated by the application.
	/// </summary>
	public const string ApplicationMessageRequest = "BW";

	/// <summary>
	/// This message is used to acknowledge an <see cref="ApplicationMessageRequest"/> providing a status on the request (i.e. whether successful or not).
	/// </summary>
	public const string ApplicationMessageRequestAck = "BX";

	/// <summary>
	/// Report, initiated by <see cref="ApplicationMessageRequest"/>.
	/// </summary>
	public const string ApplicationMessageReport = "BY";

	/// <summary>
	/// Administrative custom message.
	/// </summary>
	public const string XmlNonFix = "n";
}

/// <summary>
/// FIX extended messages codes.
/// </summary>
public static class FixExtendedMessages
{
	/// <summary>
	/// Message type for <see cref="SubscriptionResponseMessage"/>.
	/// </summary>
	public const string SubscriptionResponse = "MO";

	/// <summary>
	/// Message type for <see cref="SubscriptionFinishedMessage"/>.
	/// </summary>
	public const string SubscriptionFinished = "MF";

	/// <summary>
	/// Message type for <see cref="SubscriptionOnlineMessage"/>.
	/// </summary>
	public const string SubscriptionOnline = "SO";

	/// <summary>
	/// The request for instruments uploading.
	/// </summary>
	public const string SecurityListUpload = "xu";

	/// <summary>
	/// The request for instruments deleting.
	/// </summary>
	public const string SecurityListDelete = "xd";

	/// <summary>
	/// The response of board lookup.
	/// </summary>
	public const string Board = "bb";

	/// <summary>
	/// The request for board update.
	/// </summary>
	public const string BoardUpdate = "bu";

	/// <summary>
	/// The request for start historical data.
	/// </summary>
	public const string HistoryStart = "hs";

	/// <summary>
	/// The request for stop historical data.
	/// </summary>
	public const string HistoryEnd = "he";

	/// <summary>
	/// The request for board lookup.
	/// </summary>
	public const string BoardLookup = "bl";

	/// <summary>
	/// Message type for <see cref="UserRequestMessage"/>.
	/// </summary>
	public const string UserRequest = "ur";

	/// <summary>
	/// Message type for <see cref="UserInfoMessage"/>.
	/// </summary>
	public const string UserInfo = "ui";

	/// <summary>
	/// Message type for <see cref="DataTypeLookupMessage"/>.
	/// </summary>
	public const string DataTypeLookup = "TR";

	/// <summary>
	/// Message type for <see cref="DataTypeInfoMessage"/>.
	/// </summary>
	public const string DataTypeInfo = "TS";

	/// <summary>
	/// Message type for <see cref="SecurityLegsRequestMessage"/>.
	/// </summary>
	public const string SecurityLegsRequest = "LQ";

	/// <summary>
	/// Message type for <see cref="SecurityLegsInfoMessage"/>.
	/// </summary>
	public const string SecurityLegsInfo = "LR";

	/// <summary>
	/// Message type for <see cref="CommandMessage"/>.
	/// </summary>
	public const string Command = "DC";

	/// <summary>
	/// Message type for <see cref="SubscriptionListRequestMessage"/>.
	/// </summary>
	public const string SubscriptionListRequest = "CR";

	/// <summary>
	/// Message type for <see cref="MarketDataMessage"/>.
	/// </summary>
	public const string Subscription = "CC";

	/// <summary>
	/// Message type for <see cref="SecurityMappingMessage"/>.
	/// </summary>
	public const string SecurityMapping = "SM";

	/// <summary>
	/// Message type for <see cref="RemoveMessage"/>.
	/// </summary>
	public const string Remove = "bd";

	/// <summary>
	/// Message type for <see cref="RemoteFileCommandMessage"/>.
	/// </summary>
	public const string RemoteFileCommand = "RC";

	/// <summary>
	/// Message type for <see cref="RemoteFileMessage"/>.
	/// </summary>
	public const string RemoteFile = "RF";

	/// <summary>
	/// </summary>
	[Obsolete]		
	public const string AvailableDataRequest = "ADR";

	/// <summary>
	/// </summary>
	[Obsolete]
	public const string AvailableDataInfo = "ADI";
}
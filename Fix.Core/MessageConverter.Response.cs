namespace StockSharp.Fix;

/// <summary>
/// Response and report converters: ExecutionReport, PositionReport, SecurityListResponse, etc.
/// </summary>
partial class MessageConverter
{
	/// <inheritdoc />
	public virtual FixExecutionReport ToFixExecutionReport(ExecutionMessage message, string ordStatusReqId, string massStatusReqId, string origClOrdId, string clOrdId)
	{
		return new FixExecutionReport(
			OrdStatusReqId: ordStatusReqId,
			MassStatusReqId: massStatusReqId,
			OrigClOrdId: origClOrdId,
			ClOrdId: clOrdId,
			ExecMsg: message);
	}

	/// <inheritdoc />
	public virtual ExecutionMessage ToMessage(FixExecutionReport fix)
	{
		return fix.ExecMsg;
	}

	/// <inheritdoc />
	public virtual FixPositionReport ToFixPositionReport(PositionChangeMessage message, string posReqId)
	{
		return new FixPositionReport(
			PosReqId: posReqId,
			Account: message.PortfolioName,
			Symbol: message.SecurityId.SecurityCode,
			SecurityExchange: message.SecurityId.BoardCode,
			SecurityType: null,
			LimitType: message.LimitType,
			ClientCode: message.ClientCode,
			Currency: null,
			StrategyId: message.StrategyId,
			Side: message.Side,
			BuildFrom: message.BuildFrom,
			DepoName: message.DepoName,
			Description: message.Description,
			Changes: message.Changes.Select(kvp => new KeyValuePair<PositionChangeTypes, object>(kvp.Key, kvp.Value)).ToList(),
			TransactTime: message.ServerTime != default ? message.ServerTime : null);
	}

	/// <inheritdoc />
	public virtual PositionChangeMessage ToMessage(FixPositionReport fix)
	{
		var msg = new PositionChangeMessage
		{
			OriginalTransactionId = fix.PosReqId.ToLong(),
			PortfolioName = fix.Account,
			LimitType = fix.LimitType,
			ClientCode = fix.ClientCode,
			StrategyId = fix.StrategyId,
			Side = fix.Side,
			BuildFrom = fix.BuildFrom,
			DepoName = fix.DepoName,
			Description = fix.Description,
			SecurityId = new SecurityId
			{
				SecurityCode = fix.Symbol,
				BoardCode = fix.SecurityExchange,
			},
		};

		if (fix.TransactTime.HasValue)
			msg.ServerTime = fix.TransactTime.Value;

		if (fix.Changes != null)
		{
			foreach (var change in fix.Changes)
				msg.Changes.Add(change.Key, change.Value);
		}

		return msg;
	}

	/// <inheritdoc />
	public virtual FixSecurityListResponse ToFixSecurityListResponse(ICollection<SecurityMessage> securities, string securityReqId, string securityResponseId, bool lastFragment)
	{
		return new FixSecurityListResponse(
			SecurityReqId: securityReqId,
			SecurityResponseId: securityResponseId,
			Securities: securities,
			LastFragment: lastFragment);
	}

	/// <inheritdoc />
	public virtual (ICollection<SecurityMessage> Securities, string SecurityReqId, string SecurityResponseId, bool LastFragment) ToMessage(FixSecurityListResponse fix)
	{
		return (fix.Securities, fix.SecurityReqId, fix.SecurityResponseId, fix.LastFragment);
	}

	/// <inheritdoc />
	public virtual FixNews ToFixNews(NewsMessage message, string mdResponseId)
	{
		return new FixNews(
			MdResponseId: mdResponseId,
			NewsMsg: message);
	}

	/// <inheritdoc />
	public virtual NewsMessage ToMessage(FixNews fix)
	{
		var msg = fix.NewsMsg;

		if (!fix.MdResponseId.IsEmpty())
			msg.OriginalTransactionId = fix.MdResponseId.To<long>();

		return msg;
	}

	/// <inheritdoc />
	public virtual FixUserInfo ToFixUserInfo(UserInfoMessage message, string userRequestId)
	{
		return new FixUserInfo(
			UserRequestId: userRequestId,
			Message: message);
	}

	/// <inheritdoc />
	public virtual UserInfoMessage ToMessage(FixUserInfo fix)
	{
		var msg = fix.Message;

		if (!fix.UserRequestId.IsEmpty())
			msg.TransactionId = fix.UserRequestId.ToLong();

		return msg;
	}

	/// <inheritdoc />
	public virtual FixUserResponse ToFixUserResponse(string userRequestId, string userName, UserStatus status, string text)
	{
		return new FixUserResponse(
			UserRequestId: userRequestId,
			UserName: userName,
			Status: status,
			Text: text);
	}

	/// <inheritdoc />
	public virtual (string UserRequestId, string UserName, UserStatus Status, string Text) ToMessage(FixUserResponse fix)
	{
		return (fix.UserRequestId, fix.UserName, fix.Status, fix.Text);
	}

	/// <inheritdoc />
	public virtual FixSubscriptionResponse ToFixSubscriptionResponse(SubscriptionResponseMessage message, string mdReqId, string mdResponseId)
	{
		return new FixSubscriptionResponse(
			MdReqId: mdReqId,
			MdResponseId: mdResponseId,
			Error: message.Error?.Message);
	}

	/// <inheritdoc />
	public virtual SubscriptionResponseMessage ToMessage(FixSubscriptionResponse fix)
	{
		var msg = new SubscriptionResponseMessage
		{
			OriginalTransactionId = fix.MdReqId.ToLong(),
		};

		if (!fix.Error.IsEmpty())
			msg.Error = new InvalidOperationException(fix.Error);

		return msg;
	}

	/// <inheritdoc />
	public virtual FixSubscriptionFinished ToFixSubscriptionFinished(SubscriptionFinishedMessage message, string mdReqId)
	{
		return new FixSubscriptionFinished(
			MdReqId: mdReqId,
			FinishMsg: message);
	}

	/// <inheritdoc />
	public virtual SubscriptionFinishedMessage ToMessage(FixSubscriptionFinished fix)
	{
		var msg = fix.FinishMsg;

		if (!fix.MdReqId.IsEmpty())
			msg.OriginalTransactionId = fix.MdReqId.ToLong();

		return msg;
	}

	/// <inheritdoc />
	public virtual FixSubscriptionOnline ToFixSubscriptionOnline(string mdReqId)
	{
		return new FixSubscriptionOnline(
			MdReqId: mdReqId);
	}

	/// <inheritdoc />
	public virtual SubscriptionOnlineMessage ToMessage(FixSubscriptionOnline fix)
	{
		return new SubscriptionOnlineMessage
		{
			OriginalTransactionId = fix.MdReqId.ToLong(),
		};
	}

	/// <inheritdoc />
	public virtual FixOrderCancelReject ToFixOrderCancelReject(string clOrdId, string orderId, string errorText, DateTime transactTime, char cxlRejResponseTo)
	{
		return new FixOrderCancelReject(
			ClOrdId: clOrdId,
			OrderId: orderId,
			ErrorText: errorText,
			TransactTime: transactTime,
			CxlRejResponseTo: cxlRejResponseTo);
	}

	/// <inheritdoc />
	public virtual (string ClOrdId, string OrderId, string ErrorText, DateTime TransactTime) ToMessage(FixOrderCancelReject fix)
	{
		return (fix.ClOrdId, fix.OrderId, fix.ErrorText, fix.TransactTime);
	}

	/// <inheritdoc />
	public virtual FixOrderMassCancelReport ToFixOrderMassCancelReport(string clOrdId, char massCancelRequestType, string errorMessage)
	{
		return new FixOrderMassCancelReport(
			ClOrdId: clOrdId,
			MassCancelRequestType: massCancelRequestType,
			ErrorMessage: errorMessage);
	}

	/// <inheritdoc />
	public virtual (string ClOrdId, char MassCancelRequestType, string ErrorMessage) ToMessage(FixOrderMassCancelReport fix)
	{
		return (fix.ClOrdId, fix.MassCancelRequestType, fix.ErrorMessage);
	}

	/// <inheritdoc />
	public virtual FixMarketDataSnapshotFullRefresh ToFixMarketDataSnapshotFullRefresh(SecurityId securityId, string mdReqId, DataType buildFrom, ICollection<MDEntry> entries)
	{
		return new FixMarketDataSnapshotFullRefresh(
			SecurityId: securityId,
			MdReqId: mdReqId,
			BuildFrom: buildFrom,
			Entries: entries);
	}

	/// <inheritdoc />
	public virtual (SecurityId SecurityId, string MdReqId, DataType BuildFrom, ICollection<MDEntry> Entries) ToMessage(FixMarketDataSnapshotFullRefresh fix)
	{
		return (fix.SecurityId, fix.MdReqId, fix.BuildFrom, fix.Entries);
	}

	/// <inheritdoc />
	public virtual FixMarketDataIncrementalRefresh ToFixMarketDataIncrementalRefresh(string symbol, string securityExchange, string mdReqId, DataType buildFrom, ICollection<MDEntry> entries)
	{
		return new FixMarketDataIncrementalRefresh(
			Symbol: symbol,
			SecurityExchange: securityExchange,
			MdReqId: mdReqId,
			BuildFrom: buildFrom,
			Entries: entries);
	}

	/// <inheritdoc />
	public virtual (string Symbol, string SecurityExchange, string MdReqId, DataType BuildFrom, ICollection<MDEntry> Entries) ToMessage(FixMarketDataIncrementalRefresh fix)
	{
		return (fix.Symbol, fix.SecurityExchange, fix.MdReqId, fix.BuildFrom, fix.Entries);
	}

	/// <inheritdoc />
	public virtual FixPositionReport ToFixPositionReport(string posReqId, string account, string symbol, string securityExchange,
		SecurityTypes? securityType, TPlusLimits? limitType, string clientCode, CurrencyTypes? currency,
		string strategyId, Sides? side, DataType buildFrom, string depoName, string description,
		ICollection<KeyValuePair<PositionChangeTypes, object>> changes, DateTime? transactTime = null)
	{
		return new FixPositionReport(
			PosReqId: posReqId,
			Account: account,
			Symbol: symbol,
			SecurityExchange: securityExchange,
			SecurityType: securityType,
			LimitType: limitType,
			ClientCode: clientCode,
			Currency: currency,
			StrategyId: strategyId,
			Side: side,
			BuildFrom: buildFrom,
			DepoName: depoName,
			Description: description,
			Changes: changes,
			TransactTime: transactTime);
	}

	/// <inheritdoc />
	public virtual FixSecurityListResponseError ToFixSecurityListResponseError(string securityReqId, string securityResponseId, Exception error)
	{
		return new FixSecurityListResponseError(
			SecurityReqId: securityReqId,
			SecurityResponseId: securityResponseId,
			Error: error);
	}

	/// <inheritdoc />
	public virtual FixSubscriptionResponse ToFixSubscriptionResponse(string mdReqId, string mdResponseId, string error)
	{
		return new FixSubscriptionResponse(
			MdReqId: mdReqId,
			MdResponseId: mdResponseId,
			Error: error);
	}

	/// <inheritdoc />
	public virtual FixMarketDataRequestReject ToFixMarketDataRequestReject(string mdReqId, Exception error)
	{
		return new FixMarketDataRequestReject(
			MdReqId: mdReqId,
			Error: error);
	}

	/// <inheritdoc />
	public virtual FixTradingSessionStatus ToFixTradingSessionStatus(string requestId, BoardStateMessage message)
	{
		return new FixTradingSessionStatus(
			TradSesReqId: requestId,
			StateMsg: message);
	}

	/// <inheritdoc />
	public virtual FixBoard ToFixBoard(string requestId, BoardMessage message)
	{
		return new FixBoard(
			TradSesReqId: requestId,
			BoardMsg: message);
	}

	/// <inheritdoc />
	public virtual FixDataTypeInfo ToFixDataTypeInfo(string requestId, DataTypeInfoMessage message)
	{
		return new FixDataTypeInfo(
			MdReqId: requestId,
			Message: message);
	}

	/// <inheritdoc />
	public virtual FixLogonResponse ToFixLogonResponse(string sessionId, string licenseFeatureId, DateTime componentTimestamp)
	{
		return new FixLogonResponse(
			SessionId: sessionId,
			LicenseFeatureId: licenseFeatureId,
			ComponentTimestamp: componentTimestamp);
	}

	/// <inheritdoc />
	public virtual FixReject ToFixReject(string error, long? refSeqNum, string refMsgType, FixTags? refTagId, int? sessionRejectReason, string requestId)
	{
		return new FixReject(
			Error: error,
			RefSeqNum: refSeqNum,
			RefMsgType: refMsgType,
			RefTagId: refTagId,
			SessionRejectReason: sessionRejectReason,
			RequestId: requestId);
	}

	/// <inheritdoc />
	public virtual FixHeartbeat ToFixHeartbeat(string testReqId)
	{
		return new FixHeartbeat(
			TestReqId: testReqId);
	}

	/// <inheritdoc />
	public virtual FixSecurityLegsInfo ToFixSecurityLegsInfo(string requestId, IDictionary<SecurityId, IEnumerable<SecurityId>> legs)
	{
		return new FixSecurityLegsInfo(
			MdReqId: requestId,
			Legs: legs);
	}

	/// <inheritdoc />
	public virtual FixSubscription ToFixSubscription(string requestId, string responseId, MarketDataMessage message)
	{
		return new FixSubscription(
			MdReqId: requestId,
			MdResponseId: responseId,
			MdMsg: message);
	}

	/// <inheritdoc />
	public virtual FixRemoteFile ToFixRemoteFile(string requestId, RemoteFileMessage message)
	{
		return new FixRemoteFile(
			MdResponseId: requestId,
			File: message);
	}
}

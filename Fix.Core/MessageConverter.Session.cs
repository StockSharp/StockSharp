namespace StockSharp.Fix;

/// <summary>
/// Session message converters: TestRequest, SequenceReset, ResendRequest, TradingSessionStatus.
/// </summary>
partial class MessageConverter
{
	/// <inheritdoc />
	public virtual TimeMessage ToMessage(FixTestRequest fix)
	{
		return new TimeMessage
		{
			TransactionId = fix.TestReqId.To<long?>() ?? 0,
		};
	}

	/// <inheritdoc />
	public virtual FixTestRequest ToFixTestRequest(TimeMessage message)
	{
		return new FixTestRequest(
			TestReqId: message.TransactionId.To<string>());
	}

	/// <inheritdoc />
	public virtual (bool GapFill, long NewSeqNo) ToSequenceReset(FixSequenceReset fix)
	{
		return (fix.GapFill ?? false, fix.NewSeqNo ?? 0);
	}

	/// <inheritdoc />
	public virtual FixSequenceReset ToFixSequenceReset(bool gapFill, long newSeqNo)
	{
		return new FixSequenceReset(
			GapFill: gapFill,
			NewSeqNo: newSeqNo);
	}

	/// <inheritdoc />
	public virtual (long BeginSeqNo, long EndSeqNo) ToResendRange(FixResendRequest fix)
	{
		return (fix.BeginSeqNo ?? 0, fix.EndSeqNo ?? 0);
	}

	/// <inheritdoc />
	public virtual FixResendRequest ToFixResendRequest(long beginSeqNo, long endSeqNo)
	{
		return new FixResendRequest(
			BeginSeqNo: beginSeqNo,
			EndSeqNo: endSeqNo);
	}

	/// <inheritdoc />
	public virtual BoardLookupMessage ToMessage(FixTradingSessionStatusRequest fix)
	{
		var msg = new BoardLookupMessage
		{
			TransactionId = fix.TradSesReqId.ToLong(),
			IsSubscribe = GetIsSubscribe(fix.SubscriptionRequestType),
		};

		if (!fix.SessionId.IsEmpty())
			msg.Like = fix.SessionId;

		return msg;
	}

	/// <inheritdoc />
	public virtual FixTradingSessionStatusRequest ToFixTradingSessionStatusRequest(BoardLookupMessage message)
	{
		return new FixTradingSessionStatusRequest(
			SessionId: message.Like,
			TradSesReqId: FixId.FromLong(message.TransactionId),
			ResponseId: message.OriginalTransactionId != 0 ? FixId.FromLong(message.OriginalTransactionId) : default,
			SubscriptionRequestType: ToFixSubscriptionType(message.IsSubscribe));
	}
}

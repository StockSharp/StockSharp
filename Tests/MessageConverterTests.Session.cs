namespace StockSharp.Tests;

/// <summary>
/// Session message converter tests.
/// </summary>
partial class MessageConverterTests
{
	#region TimeMessage (TestRequest)

	[TestMethod]
	public void TimeMessage_RoundTrip()
	{
		var original = new TimeMessage
		{
			TransactionId = 12400,
		};

		var fix = Converter.ToFixTestRequest(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	#endregion

	#region SequenceReset

	[TestMethod]
	public void SequenceReset_RoundTrip()
	{
		var gapFill = true;
		var newSeqNo = 100L;

		var fix = Converter.ToFixSequenceReset(gapFill, newSeqNo);
		var (resultGapFill, resultNewSeqNo) = Converter.ToSequenceReset(fix);

		AreEqual(gapFill, resultGapFill);
		AreEqual(newSeqNo, resultNewSeqNo);
	}

	[TestMethod]
	public void SequenceReset_NoGapFill_RoundTrip()
	{
		var gapFill = false;
		var newSeqNo = 50L;

		var fix = Converter.ToFixSequenceReset(gapFill, newSeqNo);
		var (resultGapFill, resultNewSeqNo) = Converter.ToSequenceReset(fix);

		AreEqual(gapFill, resultGapFill);
		AreEqual(newSeqNo, resultNewSeqNo);
	}

	#endregion

	#region ResendRequest

	[TestMethod]
	public void ResendRequest_RoundTrip()
	{
		var beginSeqNo = 10L;
		var endSeqNo = 50L;

		var fix = Converter.ToFixResendRequest(beginSeqNo, endSeqNo);
		var (resultBegin, resultEnd) = Converter.ToResendRange(fix);

		AreEqual(beginSeqNo, resultBegin);
		AreEqual(endSeqNo, resultEnd);
	}

	#endregion

	#region BoardLookupMessage (TradingSessionStatusRequest)

	[TestMethod]
	public void BoardLookupMessage_TradingSession_RoundTrip()
	{
		var original = new BoardLookupMessage
		{
			TransactionId = 12410,
			IsSubscribe = true,
			Like = "NYSE",
		};

		var fix = Converter.ToFixTradingSessionStatusRequest(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void BoardLookupMessage_Unsubscribe_RoundTrip()
	{
		var original = new BoardLookupMessage
		{
			TransactionId = 12411,
			IsSubscribe = false,
		};

		var fix = Converter.ToFixTradingSessionStatusRequest(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	#endregion
}

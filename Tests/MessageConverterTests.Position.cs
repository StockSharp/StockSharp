namespace StockSharp.Tests;

/// <summary>
/// Position message converter tests.
/// </summary>
partial class MessageConverterTests
{
	#region PortfolioLookupMessage

	[TestMethod]
	public void PortfolioLookupMessage_RoundTrip()
	{
		var original = new PortfolioLookupMessage
		{
			TransactionId = 12600,
			PortfolioName = "TestPortfolio",
			IsSubscribe = true,
		};

		var fix = Converter.ToFixRequestForPositions(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void PortfolioLookupMessage_Unsubscribe_RoundTrip()
	{
		var original = new PortfolioLookupMessage
		{
			TransactionId = 12601,
			IsSubscribe = false,
		};

		var fix = Converter.ToFixRequestForPositions(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	// FIX SubscriptionRequestType (263) — 0=Snapshot, 1=SnapshotPlusUpdates,
	// 2=DisablePrevious. Only an explicit 2 is an unsubscribe; a snapshot (0) and an absent
	// type are subscribes. Treating just '1' as subscribe routed a snapshot into unsubscribe.
	[TestMethod]
	public void PortfolioLookup_SubscriptionType_SnapshotAndAbsentAreSubscribe()
	{
		var baseFix = Converter.ToFixRequestForPositions(new PortfolioLookupMessage { TransactionId = 12602, IsSubscribe = true });

		Converter.ToMessage(baseFix with { SubscriptionRequestType = '0' }).IsSubscribe.AssertTrue();
		Converter.ToMessage(baseFix with { SubscriptionRequestType = '1' }).IsSubscribe.AssertTrue();
		Converter.ToMessage(baseFix with { SubscriptionRequestType = null }).IsSubscribe.AssertTrue();
		Converter.ToMessage(baseFix with { SubscriptionRequestType = '2' }).IsSubscribe.AssertFalse();
	}

	[TestMethod]
	public void PortfolioLookupMessage_WithOriginalTransactionId_RoundTrip()
	{
		var original = new PortfolioLookupMessage
		{
			TransactionId = 12602,
			OriginalTransactionId = 12600,
			PortfolioName = "MarginAccount",
			IsSubscribe = true,
		};

		var fix = Converter.ToFixRequestForPositions(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void PortfolioLookupMessage_WithIncrementalAndUserId_RoundTrip()
	{
		var original = new PortfolioLookupMessage
		{
			TransactionId = 12603,
			PortfolioName = "TestPortfolio",
			IsSubscribe = true,
			IsIncremental = true,
			UserId = "user456",
		};

		var fix = Converter.ToFixRequestForPositions(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void PortfolioLookupMessage_WithSecurityIds_RoundTrip()
	{
		var original = new PortfolioLookupMessage
		{
			TransactionId = 12604,
			PortfolioName = "TestPortfolio",
			IsSubscribe = true,
			SecurityIds =
			[
				CreateTestSecurityId("AAPL", "NASDAQ"),
				CreateTestSecurityId("TSLA", "NASDAQ"),
			],
		};

		var fix = Converter.ToFixRequestForPositions(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void PortfolioLookupMessage_WithClientCode_RoundTrip()
	{
		// ClientCode identifies the client a lookup is for and must survive the
		// RequestForPositions round-trip.
		var original = new PortfolioLookupMessage
		{
			TransactionId = 12605,
			PortfolioName = "TestPortfolio",
			ClientCode = "test1",
			IsSubscribe = true,
		};

		var fix = Converter.ToFixRequestForPositions(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void PortfolioLookupMessage_WithClientCode_NoPortfolio_RoundTrip()
	{
		// PortfolioName is optional on the lookup, so ClientCode must encode
		// independently of it.
		var original = new PortfolioLookupMessage
		{
			TransactionId = 12606,
			ClientCode = "test1",
			IsSubscribe = true,
		};

		var fix = Converter.ToFixRequestForPositions(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	#endregion
}

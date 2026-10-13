namespace StockSharp.Tests;

/// <summary>
/// Other message converter tests.
/// </summary>
partial class MessageConverterTests
{
	#region BoardLookupMessage (FixBoardLookup)

	[TestMethod]
	public void BoardLookupMessage_FixBoardLookup_RoundTrip()
	{
		var original = new BoardLookupMessage
		{
			Like = "NYSE*",
		};

		var fix = Converter.ToFixBoardLookup(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	#endregion

	#region BoardMessage

	[TestMethod]
	public void BoardMessage_RoundTrip()
	{
		var original = new BoardMessage
		{
			Code = "NYSE",
			ExchangeCode = "USA",
		};

		var fix = Converter.ToFixBoardUpdate(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	#endregion

	#region RemoveMessage

	[TestMethod]
	public void RemoveMessage_Security_RoundTrip()
	{
		var original = new RemoveMessage
		{
			TransactionId = 12800,
			RemoveType = RemoveTypes.Security,
			RemoveId = "AAPL@NYSE",
		};

		var fix = Converter.ToFixRemove(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void RemoveMessage_Portfolio_RoundTrip()
	{
		var original = new RemoveMessage
		{
			TransactionId = 12801,
			RemoveType = RemoveTypes.Portfolio,
			RemoveId = "TestPortfolio",
		};

		var fix = Converter.ToFixRemove(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void RemoveMessage_Board_RoundTrip()
	{
		var original = new RemoveMessage
		{
			TransactionId = 12802,
			RemoveType = RemoveTypes.Board,
			RemoveId = "NYSE",
		};

		var fix = Converter.ToFixRemove(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	#endregion

	#region HistoryStart

	[TestMethod]
	public void HistoryStart_RoundTrip()
	{
		var from = new DateTime(2024, 1, 1, 0, 0, 0).UtcKind();

		var fix = Converter.ToFixHistoryStart(null, from);
		var (resultId, resultFrom) = Converter.ToHistoryStart(fix);

		IsNull(resultId);
		AreEqual(from, resultFrom);
	}

	[TestMethod]
	public void HistoryStart_NoDate_RoundTrip()
	{
		var fix = Converter.ToFixHistoryStart(null, null);
		var (resultId, resultFrom) = Converter.ToHistoryStart(fix);

		IsNull(resultId);
		IsNull(resultFrom);
	}

	#endregion

	#region DataTypeLookupMessage

	[TestMethod]
	public void DataTypeLookupMessage_RoundTrip()
	{
		var original = new DataTypeLookupMessage
		{
			TransactionId = 12810,
			SecurityId = CreateTestSecurityId(),
			IncludeDates = true,
		};

		var fix = Converter.ToFixDataTypeLookup(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void DataTypeLookupMessage_WithFormat_RoundTrip()
	{
		var original = new DataTypeLookupMessage
		{
			TransactionId = 12811,
			SecurityId = CreateTestSecurityId("TSLA", "NASDAQ"),
			Format = 1,
			IncludeDates = false,
		};

		var fix = Converter.ToFixDataTypeLookup(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	#endregion
}

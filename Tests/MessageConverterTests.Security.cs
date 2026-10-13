namespace StockSharp.Tests;

/// <summary>
/// Security message converter tests.
/// </summary>
partial class MessageConverterTests
{
	#region SecurityLookupMessage

	[TestMethod]
	public void SecurityLookupMessage_RoundTrip()
	{
		var original = new SecurityLookupMessage
		{
			TransactionId = 12500,
			SecurityId = CreateTestSecurityId(),
			Name = "Apple Inc.",
			SecurityType = SecurityTypes.Stock,
		};

		var fix = Converter.ToFixSecurityListRequest(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void SecurityLookupMessage_Option_RoundTrip()
	{
		var original = new SecurityLookupMessage
		{
			TransactionId = 12501,
			SecurityId = CreateTestSecurityId("AAPL-CALL-150"),
			Strike = 150.00m,
			OptionType = OptionTypes.Call,
			SecurityType = SecurityTypes.Option,
			ExpiryDate = CreateTestDateTime(),
		};

		var fix = Converter.ToFixSecurityListRequest(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void SecurityLookupMessage_WithSkipCount_RoundTrip()
	{
		var original = new SecurityLookupMessage
		{
			TransactionId = 12502,
			Skip = 100,
			Count = 50,
			OnlySecurityId = true,
		};

		var fix = Converter.ToFixSecurityListRequest(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	// the per-type filter and the archive policy must survive the round-trip.
	// Encoding SecurityTypes=null / DisableArchive=false widened "these types, no archive"
	// into a heavier, differently-scoped result.
	[TestMethod]
	public void SecurityLookupMessage_SecurityTypesAndDisableArchive_RoundTrip()
	{
		var original = new SecurityLookupMessage
		{
			TransactionId = 12503,
			SecurityTypes = [SecurityTypes.Stock, SecurityTypes.Future],
			DisableArchive = true,
		};

		var fix = Converter.ToFixSecurityListRequest(original);
		fix.DisableArchive.AssertTrue();

		var result = Converter.ToMessage(fix);

		result.DisableArchive.AssertTrue();
		result.SecurityTypes.AssertNotNull();
		result.SecurityTypes.Length.AssertEqual(2);
		result.SecurityTypes.Contains(SecurityTypes.Stock).AssertTrue();
		result.SecurityTypes.Contains(SecurityTypes.Future).AssertTrue();
	}

	#endregion

	#region SecurityMessage (SecurityStatusRequest)

	[TestMethod]
	public void SecurityMessage_RoundTrip()
	{
		var original = new SecurityMessage
		{
			OriginalTransactionId = 12510,
			SecurityId = CreateTestSecurityId(),
			SecurityType = SecurityTypes.Stock,
		};

		var fix = Converter.ToFixSecurityStatusRequest(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	#endregion

	#region SecurityLegsRequestMessage

	[TestMethod]
	public void SecurityLegsRequestMessage_RoundTrip()
	{
		var original = new SecurityLegsRequestMessage
		{
			TransactionId = 12520,
			Like = "SPREAD-AAPL",
		};

		var fix = Converter.ToFixSecurityLegsRequest(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	#endregion

	#region SecurityMappingMessage

	[TestMethod]
	public void SecurityMappingMessage_RoundTrip()
	{
		var original = new SecurityMappingMessage
		{
			TransactionId = 12530,
			Mapping = new SecurityIdMapping
			{
				StockSharpId = CreateTestSecurityId("AAPL", "NYSE"),
				AdapterId = new SecurityId { SecurityCode = "AAPL.US", BoardCode = "XNYS" },
			},
		};

		var fix = Converter.ToFixSecurityMapping(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void SecurityMappingMessage_StorageNameAndDelete_RoundTrip()
	{
		// StorageName (IdSource) and the delete intent (Action) must survive the round-trip;
		// dropping them lost the mapping source and turned a delete into an add on decode.
		var original = new SecurityMappingMessage
		{
			TransactionId = 12531,
			StorageName = "MyStorage",
			IsDelete = true,
			Mapping = new SecurityIdMapping
			{
				StockSharpId = CreateTestSecurityId("AAPL", "NYSE"),
				AdapterId = new SecurityId { SecurityCode = "AAPL.US", BoardCode = "XNYS" },
			},
		};

		var fix = Converter.ToFixSecurityMapping(original);
		var result = Converter.ToMessage(fix);

		result.StorageName.AssertEqual("MyStorage");
		result.IsDelete.AssertTrue();
	}

	[TestMethod]
	public void SecurityMappingMessage_AddNotMisreadAsDelete_RoundTrip()
	{
		var original = new SecurityMappingMessage
		{
			TransactionId = 12532,
			StorageName = "MyStorage",
			IsDelete = false,
			Mapping = new SecurityIdMapping
			{
				StockSharpId = CreateTestSecurityId("AAPL", "NYSE"),
				AdapterId = new SecurityId { SecurityCode = "AAPL.US", BoardCode = "XNYS" },
			},
		};

		var result = Converter.ToMessage(Converter.ToFixSecurityMapping(original));

		result.IsDelete.AssertFalse();
	}

	#endregion
}

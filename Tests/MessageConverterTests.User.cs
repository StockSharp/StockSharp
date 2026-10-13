namespace StockSharp.Tests;

/// <summary>
/// User message converter tests.
/// </summary>
partial class MessageConverterTests
{
	#region UserRequestMessage

	[TestMethod]
	public void UserRequestMessage_RoundTrip()
	{
		var original = new UserRequestMessage
		{
			TransactionId = 12700,
			Login = "testuser",
			Id = 12345,
		};

		var fix = Converter.ToFixUserRequest(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void UserRequestMessage_WithoutId_RoundTrip()
	{
		var original = new UserRequestMessage
		{
			TransactionId = 12701,
			Login = "admin",
		};

		var fix = Converter.ToFixUserRequest(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	#endregion

	#region UserLookupMessage

	[TestMethod]
	public void UserLookupMessage_RoundTrip()
	{
		var original = new UserLookupMessage
		{
			TransactionId = 12710,
			IsSubscribe = true,
			Like = "user*",
		};

		var fix = Converter.ToFixUserRequestEx(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void UserLookupMessage_Unsubscribe_RoundTrip()
	{
		var original = new UserLookupMessage
		{
			TransactionId = 12711,
			IsSubscribe = false,
		};

		var fix = Converter.ToFixUserRequestEx(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	// a lookup by primary key must keep the key on both encode and decode;
	// dropping it turned a targeted user request into an unfiltered/wrong lookup.
	[TestMethod]
	public void UserLookupMessage_ById_RoundTrip()
	{
		var original = new UserLookupMessage
		{
			TransactionId = 12712,
			IsSubscribe = true,
			UserId = 4242,
		};

		var fix = Converter.ToFixUserRequestEx(original);
		fix.Id.AssertEqual(4242L);

		var result = Converter.ToMessage(fix);
		result.UserId.AssertEqual(4242L);
	}

	#endregion
}

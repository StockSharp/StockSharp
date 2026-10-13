namespace StockSharp.Tests;

/// <summary>
/// Tests for the payload comparison inside Helper.CheckEqual. Many tests lean on it to compare
/// <see cref="Level1ChangeMessage"/>, <see cref="PositionChangeMessage"/>, <see cref="QuoteChange"/> and
/// <see cref="SettingsStorage"/>, so the comparison itself has to fail on a difference: an extra key, a
/// missing key, a changed value and a changed part of a quote each fail, in the serializer mode as well
/// as outside it. A comparison that passes over a field is a round-trip test that proves nothing about
/// it, which is worse than having no test there at all - the report says the field is covered.
/// </summary>
[TestClass]
public class CheckEqualChangesTests : BaseTestClass
{
	private static readonly SecurityId _secId = new() { SecurityCode = "TEST", BoardCode = "TBRD" };
	private static readonly DateTime _time = new(2024, 5, 17, 10, 30, 0, DateTimeKind.Utc);

	private static Level1ChangeMessage CreateLevel1()
		=> new Level1ChangeMessage
		{
			SecurityId = _secId,
			ServerTime = _time,
			LocalTime = _time,
		}
		.Add(Level1Fields.BestBidPrice, 10.5m)
		.Add(Level1Fields.BestAskPrice, 11.5m)
		.Add(Level1Fields.LastTradeTime, _time);

	private static PositionChangeMessage CreatePosition()
		=> new PositionChangeMessage
		{
			SecurityId = _secId,
			PortfolioName = "TestPortfolio",
			ServerTime = _time,
			LocalTime = _time,
		}
		.Add(PositionChangeTypes.CurrentValue, 100m)
		.Add(PositionChangeTypes.Currency, CurrencyTypes.USD)
		.Add(PositionChangeTypes.ExpirationDate, _time);

	#region Level1ChangeMessage

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void Level1_Same_Passes(bool isSerializer)
	{
		var expected = CreateLevel1();

		Helper.CheckEqual(expected, expected.TypedClone(), isSerializer: isSerializer);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void Level1_ExtraKey_Fails(bool isSerializer)
	{
		var expected = CreateLevel1();
		var actual = expected.TypedClone().Add(Level1Fields.OpenInterest, 42m);

		Throws<AssertFailedException>(() => Helper.CheckEqual(expected, actual, isSerializer: isSerializer));
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void Level1_MissingKey_Fails(bool isSerializer)
	{
		var expected = CreateLevel1();
		var actual = expected.TypedClone();

		actual.Changes.Remove(Level1Fields.BestAskPrice);

		Throws<AssertFailedException>(() => Helper.CheckEqual(expected, actual, isSerializer: isSerializer));
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void Level1_ChangedValue_Fails(bool isSerializer)
	{
		var expected = CreateLevel1();
		var actual = expected.TypedClone();

		actual.Changes[Level1Fields.BestBidPrice] = 10.6m;

		Throws<AssertFailedException>(() => Helper.CheckEqual(expected, actual, isSerializer: isSerializer));
	}

	#endregion

	#region PositionChangeMessage

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void Position_Same_Passes(bool isSerializer)
	{
		var expected = CreatePosition();

		Helper.CheckEqual(expected, expected.TypedClone(), isSerializer: isSerializer);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void Position_ExtraKey_Fails(bool isSerializer)
	{
		var expected = CreatePosition();
		var actual = expected.TypedClone().Add(PositionChangeTypes.BlockedValue, 5m);

		Throws<AssertFailedException>(() => Helper.CheckEqual(expected, actual, isSerializer: isSerializer));
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void Position_MissingKey_Fails(bool isSerializer)
	{
		var expected = CreatePosition();
		var actual = expected.TypedClone();

		actual.Changes.Remove(PositionChangeTypes.Currency);

		Throws<AssertFailedException>(() => Helper.CheckEqual(expected, actual, isSerializer: isSerializer));
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void Position_ChangedValue_Fails(bool isSerializer)
	{
		var expected = CreatePosition();
		var actual = expected.TypedClone();

		actual.Changes[PositionChangeTypes.CurrentValue] = 101m;

		Throws<AssertFailedException>(() => Helper.CheckEqual(expected, actual, isSerializer: isSerializer));
	}

	#endregion

	#region The one difference the contract allows

	// Truncating the expected time to the millisecond is the only normalisation the comparison
	// permits, and it is asked for by name: without isMls the same sub-millisecond difference fails.
	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void Level1_SubMillisecondTime_OnlyIsMlsAllowsIt(bool isSerializer)
	{
		var expected = CreateLevel1();

		expected.Changes[Level1Fields.LastTradeTime] = _time.AddTicks(1234);

		var actual = expected.TypedClone();

		actual.Changes[Level1Fields.LastTradeTime] = _time;

		Helper.CheckEqual(expected, actual, isMls: true, isSerializer: isSerializer);
		Throws<AssertFailedException>(() => Helper.CheckEqual(expected, actual, isMls: false, isSerializer: isSerializer));
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void Position_SubMillisecondTime_OnlyIsMlsAllowsIt(bool isSerializer)
	{
		var expected = CreatePosition();

		expected.Changes[PositionChangeTypes.ExpirationDate] = _time.AddTicks(1234);

		var actual = expected.TypedClone();

		actual.Changes[PositionChangeTypes.ExpirationDate] = _time;

		Helper.CheckEqual(expected, actual, isMls: true, isSerializer: isSerializer);
		Throws<AssertFailedException>(() => Helper.CheckEqual(expected, actual, isMls: false, isSerializer: isSerializer));
	}

	#endregion

	#region QuoteChange

	private static QuoteChangeMessage CreateBook(QuoteChange bid, QuoteChange ask)
		=> new()
		{
			SecurityId = _secId,
			ServerTime = _time,
			LocalTime = _time,
			Bids = [bid],
			Asks = [ask],
		};

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void QuoteChange_Same_Passes(bool isSerializer)
	{
		var expected = CreateBook(new(10m, 5m), new(11m, 5m));

		Helper.CheckEqual(expected, expected.TypedClone(), isSerializer: isSerializer);
	}

	// A grouped level says which orders it was built from, and two levels built from different orders are
	// different levels however the totals come out. Setting InnerQuotes recomputes Volume and OrdersCount
	// from the parts, so both sides here land on price 10 and volume 5 and the only thing telling them
	// apart is the split - which is exactly what a grouping test asks this comparison to look at.
	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void QuoteChange_ChangedInnerQuote_Fails(bool isSerializer)
	{
		var expected = CreateBook(new(10m, 0m) { InnerQuotes = [new(10m, 2m), new(10m, 3m)] }, new(11m, 5m));
		var actual = CreateBook(new(10m, 0m) { InnerQuotes = [new(10m, 1m), new(10m, 4m)] }, new(11m, 5m));

		Throws<AssertFailedException>(() => Helper.CheckEqual(expected, actual, isSerializer: isSerializer));
	}

	#endregion

	#region SettingsStorage

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void SettingsStorage_Same_Passes(bool isSerializer)
	{
		var expected = new SettingsStorage().Set("Length", 10);
		var actual = new SettingsStorage().Set("Length", 10);

		Helper.CheckEqual(expected, actual, isSerializer: isSerializer);
	}

	// The value under a key is the payload, not the number of keys. A serializer that brings back the right
	// key set with the wrong number in it has broken the round-trip just as surely as one that loses the
	// key, so counting entries is no answer in either mode.
	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void SettingsStorage_ChangedValue_Fails(bool isSerializer)
	{
		var expected = new SettingsStorage().Set("Length", 10);
		var actual = new SettingsStorage().Set("Length", 11);

		Throws<AssertFailedException>(() => Helper.CheckEqual(expected, actual, isSerializer: isSerializer));
	}

	#endregion
}

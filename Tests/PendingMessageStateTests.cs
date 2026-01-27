namespace StockSharp.Tests;

using StockSharp.Algo.Basket;

[TestClass]
public class PendingMessageStateTests : BaseTestClass
{
	private static readonly SecurityId _secId = new() { SecurityCode = "AAPL", BoardCode = "NYSE" };

	private static MarketDataMessage CreateMarketData(long transactionId) => new()
	{
		TransactionId = transactionId,
		IsSubscribe = true,
		DataType2 = DataType.Ticks,
		SecurityId = _secId,
	};

	[TestMethod]
	public void Count_Initially_Zero()
	{
		var state = new PendingMessageState();

		state.Count.AssertEqual(0);
	}

	[TestMethod]
	public void Add_IncreasesCount()
	{
		var state = new PendingMessageState();

		state.Add(new ResetMessage());

		state.Count.AssertEqual(1);

		state.Add(new ResetMessage());

		state.Count.AssertEqual(2);
	}

	[TestMethod]
	public void GetAndClear_ReturnsAndClears()
	{
		var state = new PendingMessageState();
		var msg1 = new ResetMessage();
		var msg2 = new ResetMessage();

		state.Add(msg1);
		state.Add(msg2);

		var result = state.GetAndClear();

		result.Length.AssertEqual(2);
		result[0].AssertEqual(msg1);
		result[1].AssertEqual(msg2);

		state.Count.AssertEqual(0);
	}

	[TestMethod]
	public void TryRemoveMarketData_Existing_ReturnsMessage()
	{
		var state = new PendingMessageState();
		var md = CreateMarketData(42);

		state.Add(md);

		var result = state.TryRemoveMarketData(42);

		result.AssertNotNull();
		result.AssertEqual(md);
		state.Count.AssertEqual(0);
	}

	[TestMethod]
	public void TryRemoveMarketData_NonExistent_ReturnsNull()
	{
		var state = new PendingMessageState();

		state.Add(new ResetMessage());

		var result = state.TryRemoveMarketData(999);

		result.AssertNull();
		state.Count.AssertEqual(1);
	}

	[TestMethod]
	public void Clear_RemovesAll()
	{
		var state = new PendingMessageState();

		state.Add(new ResetMessage());
		state.Add(CreateMarketData(1));
		state.Add(CreateMarketData(2));

		state.Clear();

		state.Count.AssertEqual(0);
	}
}

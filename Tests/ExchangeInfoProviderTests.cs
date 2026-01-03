namespace StockSharp.Tests;

[TestClass]
public class ExchangeInfoProviderTests : BaseTestClass
{
	[TestMethod]
	public void InMemoryExchangeInfoProvider_Constructor_LoadsDefaultBoards()
	{
		var provider = new InMemoryExchangeInfoProvider();

		provider.Boards.Any().AssertTrue();
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_Constructor_LoadsDefaultExchanges()
	{
		var provider = new InMemoryExchangeInfoProvider();

		provider.Exchanges.Any().AssertTrue();
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_TryGetExchangeBoard_ReturnsBoardWhenExists()
	{
		var provider = new InMemoryExchangeInfoProvider();

		var board = provider.TryGetExchangeBoard(ExchangeBoard.Nasdaq.Code);

		board.AssertNotNull();
		board.Code.AssertEqual(ExchangeBoard.Nasdaq.Code);
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_TryGetExchangeBoard_ReturnsNullWhenNotExists()
	{
		var provider = new InMemoryExchangeInfoProvider();

		var board = provider.TryGetExchangeBoard("UNKNOWN_BOARD");

		board.AssertNull();
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_TryGetExchangeBoard_ThrowsOnEmptyCode()
	{
		var provider = new InMemoryExchangeInfoProvider();

		ThrowsExactly<ArgumentNullException>(() => provider.TryGetExchangeBoard(string.Empty));
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_TryGetExchange_ReturnsExchangeWhenExists()
	{
		var provider = new InMemoryExchangeInfoProvider();

		var exchange = provider.TryGetExchange(Exchange.Nasdaq.Name);

		exchange.AssertNotNull();
		exchange.Name.AssertEqual(Exchange.Nasdaq.Name);
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_TryGetExchange_ReturnsNullWhenNotExists()
	{
		var provider = new InMemoryExchangeInfoProvider();

		var exchange = provider.TryGetExchange("UNKNOWN_EXCHANGE");

		exchange.AssertNull();
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_TryGetExchange_ThrowsOnEmptyCode()
	{
		var provider = new InMemoryExchangeInfoProvider();

		ThrowsExactly<ArgumentNullException>(() => provider.TryGetExchange(string.Empty));
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_TryGetExchange_HandlesRtsToForts()
	{
		var provider = new InMemoryExchangeInfoProvider();

		// Add FORTS exchange first since it may not be in default list
		var fortsExchange = new Exchange { Name = "FORTS" };
		provider.Save(fortsExchange);

		var exchange = provider.TryGetExchange("RTS");

		exchange.AssertNotNull();
		exchange.Name.AssertEqual("FORTS");
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_SaveBoard_AddsNewBoard()
	{
		var provider = new InMemoryExchangeInfoProvider();
		var board = new ExchangeBoard
		{
			Code = "TESTBOARD" + Guid.NewGuid().ToString("N")[..8],
			Exchange = Exchange.Test
		};

		provider.Save(board);

		var result = provider.TryGetExchangeBoard(board.Code);
		result.AssertNotNull();
		result.Code.AssertEqual(board.Code);
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_SaveBoard_TriggersBoardAddedEvent()
	{
		var provider = new InMemoryExchangeInfoProvider();
		var board = new ExchangeBoard
		{
			Code = "TESTBOARD" + Guid.NewGuid().ToString("N")[..8],
			Exchange = Exchange.Test
		};
		ExchangeBoard addedBoard = null;

		provider.BoardAdded += b => addedBoard = b;
		provider.Save(board);

		addedBoard.AssertNotNull();
		addedBoard.Code.AssertEqual(board.Code);
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_SaveBoard_ThrowsOnNull()
	{
		var provider = new InMemoryExchangeInfoProvider();

		ThrowsExactly<ArgumentNullException>(() => provider.Save((ExchangeBoard)null));
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_SaveExchange_AddsNewExchange()
	{
		var provider = new InMemoryExchangeInfoProvider();
		var exchange = new Exchange
		{
			Name = "TESTEXCH" + Guid.NewGuid().ToString("N")[..8]
		};

		provider.Save(exchange);

		var result = provider.TryGetExchange(exchange.Name);
		result.AssertNotNull();
		result.Name.AssertEqual(exchange.Name);
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_SaveExchange_TriggersExchangeAddedEvent()
	{
		var provider = new InMemoryExchangeInfoProvider();
		var exchange = new Exchange
		{
			Name = "TESTEXCH" + Guid.NewGuid().ToString("N")[..8]
		};
		Exchange addedExchange = null;

		provider.ExchangeAdded += e => addedExchange = e;
		provider.Save(exchange);

		addedExchange.AssertNotNull();
		addedExchange.Name.AssertEqual(exchange.Name);
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_SaveExchange_ThrowsOnNull()
	{
		var provider = new InMemoryExchangeInfoProvider();

		ThrowsExactly<ArgumentNullException>(() => provider.Save((Exchange)null));
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_DeleteBoard_RemovesBoard()
	{
		var provider = new InMemoryExchangeInfoProvider();
		var board = new ExchangeBoard
		{
			Code = "TESTBOARD" + Guid.NewGuid().ToString("N")[..8],
			Exchange = Exchange.Test
		};
		provider.Save(board);

		provider.Delete(board);

		var result = provider.TryGetExchangeBoard(board.Code);
		result.AssertNull();
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_DeleteBoard_TriggersBoardRemovedEvent()
	{
		var provider = new InMemoryExchangeInfoProvider();
		var board = new ExchangeBoard
		{
			Code = "TESTBOARD" + Guid.NewGuid().ToString("N")[..8],
			Exchange = Exchange.Test
		};
		provider.Save(board);
		ExchangeBoard removedBoard = null;

		provider.BoardRemoved += b => removedBoard = b;
		provider.Delete(board);

		removedBoard.AssertNotNull();
		removedBoard.Code.AssertEqual(board.Code);
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_DeleteBoard_ThrowsOnNull()
	{
		var provider = new InMemoryExchangeInfoProvider();

		ThrowsExactly<ArgumentNullException>(() => provider.Delete((ExchangeBoard)null));
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_DeleteExchange_RemovesExchange()
	{
		var provider = new InMemoryExchangeInfoProvider();
		var exchange = new Exchange
		{
			Name = "TESTEXCH" + Guid.NewGuid().ToString("N")[..8]
		};
		provider.Save(exchange);

		provider.Delete(exchange);

		var result = provider.TryGetExchange(exchange.Name);
		result.AssertNull();
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_DeleteExchange_TriggersExchangeRemovedEvent()
	{
		var provider = new InMemoryExchangeInfoProvider();
		var exchange = new Exchange
		{
			Name = "TESTEXCH" + Guid.NewGuid().ToString("N")[..8]
		};
		provider.Save(exchange);
		Exchange removedExchange = null;

		provider.ExchangeRemoved += e => removedExchange = e;
		provider.Delete(exchange);

		removedExchange.AssertNotNull();
		removedExchange.Name.AssertEqual(exchange.Name);
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_DeleteExchange_ThrowsOnNull()
	{
		var provider = new InMemoryExchangeInfoProvider();

		ThrowsExactly<ArgumentNullException>(() => provider.Delete((Exchange)null));
	}

	[TestMethod]
	public async Task InMemoryExchangeInfoProvider_InitAsync_CompletesSuccessfully()
	{
		var provider = new InMemoryExchangeInfoProvider();

		await provider.InitAsync(CancellationToken);

		// InitAsync for InMemoryExchangeInfoProvider does nothing but should complete without error
	}

	[TestMethod]
	public void IBoardMessageProvider_Lookup_ReturnsAllBoards()
	{
		IBoardMessageProvider provider = new InMemoryExchangeInfoProvider();
		var criteria = new BoardLookupMessage();

		var results = provider.Lookup(criteria).ToArray();

		results.Any().AssertTrue();
	}

	[TestMethod]
	public void IBoardMessageProvider_Lookup_ReturnsBoardMessages()
	{
		IBoardMessageProvider provider = new InMemoryExchangeInfoProvider();
		var criteria = new BoardLookupMessage();

		var results = provider.Lookup(criteria).ToArray();

		foreach (var result in results)
		{
			result.Code.IsEmpty().AssertFalse();
		}
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_SaveBoard_DoesNotTriggerEventForSameReference()
	{
		var provider = new InMemoryExchangeInfoProvider();
		var board = new ExchangeBoard
		{
			Code = "TESTBOARD" + Guid.NewGuid().ToString("N")[..8],
			Exchange = Exchange.Test
		};
		provider.Save(board);
		var eventCount = 0;

		provider.BoardAdded += _ => eventCount++;
		provider.Save(board); // Save same reference again

		eventCount.AssertEqual(0);
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_SaveExchange_DoesNotTriggerEventForSameReference()
	{
		var provider = new InMemoryExchangeInfoProvider();
		var exchange = new Exchange
		{
			Name = "TESTEXCH" + Guid.NewGuid().ToString("N")[..8]
		};
		provider.Save(exchange);
		var eventCount = 0;

		provider.ExchangeAdded += _ => eventCount++;
		provider.Save(exchange); // Save same reference again

		eventCount.AssertEqual(0);
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_TryGetExchangeBoard_IsCaseInsensitive()
	{
		var provider = new InMemoryExchangeInfoProvider();

		var board1 = provider.TryGetExchangeBoard("NASDAQ");
		var board2 = provider.TryGetExchangeBoard("nasdaq");
		var board3 = provider.TryGetExchangeBoard("NasDaq");

		board1.AssertNotNull();
		board2.AssertNotNull();
		board3.AssertNotNull();
		board1.Code.AssertEqual(board2.Code);
		board2.Code.AssertEqual(board3.Code);
	}

	[TestMethod]
	public void InMemoryExchangeInfoProvider_TryGetExchange_IsCaseInsensitive()
	{
		var provider = new InMemoryExchangeInfoProvider();

		var exchange1 = provider.TryGetExchange("NASDAQ");
		var exchange2 = provider.TryGetExchange("nasdaq");
		var exchange3 = provider.TryGetExchange("NasDaq");

		exchange1.AssertNotNull();
		exchange2.AssertNotNull();
		exchange3.AssertNotNull();
		exchange1.Name.AssertEqual(exchange2.Name);
		exchange2.Name.AssertEqual(exchange3.Name);
	}
}

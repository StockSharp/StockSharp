namespace StockSharp.Tests;

using StockSharp.Algo.Slippage;

[TestClass]
public class SlippageTests
{
	private static readonly SecurityId _secId = Helper.CreateSecurityId();

	[TestMethod]
	public void Level1AndOrderRegisterBuy()
	{
		var mgr = new SlippageManager();

		mgr.ProcessMessage(new Level1ChangeMessage
		{
			SecurityId = _secId
		}
		.Add(Level1Fields.BestBidPrice, 100m)
		.Add(Level1Fields.BestAskPrice, 102m));

		// Simulate Buy order registration
		var regMsg = new OrderRegisterMessage
		{
			SecurityId = _secId,
			Side = Sides.Buy,
			TransactionId = 1
		};
		mgr.ProcessMessage(regMsg);

		// Order executed with price 2 points worse (higher)
		var slip = mgr.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _secId,
			OriginalTransactionId = regMsg.TransactionId,
			TradePrice = 104m,
			Side = Sides.Buy
		});

		slip.AssertEqual(2m); // 104 - 102 (ask at registration)
		mgr.Slippage.AssertEqual(2m); // Check total slippage
	}

	[TestMethod]
	public void Level1AndOrderRegisterSell()
	{
		var mgr = new SlippageManager();

		mgr.ProcessMessage(new Level1ChangeMessage
		{
			SecurityId = _secId
		}
		.Add(Level1Fields.BestBidPrice, 101m)
		.Add(Level1Fields.BestAskPrice, 103m));

		var regMsg = new OrderRegisterMessage
		{
			SecurityId = _secId,
			Side = Sides.Sell,
			TransactionId = 1
		};
		mgr.ProcessMessage(regMsg);

		var slip = mgr.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _secId,
			OriginalTransactionId = regMsg.TransactionId,
			TradePrice = 100m,
			Side = Sides.Sell
		});

		slip.AssertEqual(1m); // 101 - 100 (bid at registration)
		mgr.Slippage.AssertEqual(1m); // Check total slippage
	}

	[TestMethod]
	public void QuoteChangeRegisterAndExecution()
	{
		var mgr = new SlippageManager();

		mgr.ProcessMessage(new QuoteChangeMessage
		{
			SecurityId = _secId,
			Bids = [new(50m, 1)],
			Asks = [new(55m, 1)]
		});

		var regMsg = new OrderRegisterMessage
		{
			SecurityId = _secId,
			Side = Sides.Buy,
			TransactionId = 77
		};
		mgr.ProcessMessage(regMsg);

		var slip = mgr.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _secId,
			OriginalTransactionId = regMsg.TransactionId,
			TradePrice = 60m,
			Side = Sides.Buy
		});

		slip.AssertEqual(5m); // 60 - 55
		mgr.Slippage.AssertEqual(5m); // Check total slippage
	}

	[TestMethod]
	public void SlippageNegativeAndOption()
	{
		var mgr = new SlippageManager();

		mgr.ProcessMessage(new Level1ChangeMessage
		{
			SecurityId = _secId
		}
		.Add(Level1Fields.BestBidPrice, 100m)
		.Add(Level1Fields.BestAskPrice, 101m));

		var regMsg = new OrderRegisterMessage
		{
			SecurityId = _secId,
			Side = Sides.Buy,
			TransactionId = 123
		};
		mgr.ProcessMessage(regMsg);

		// Execution BETTER than ask — negative slippage
		var slip = mgr.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _secId,
			OriginalTransactionId = regMsg.TransactionId,
			TradePrice = 100m,
			Side = Sides.Buy
		});

		slip.AssertEqual(-1m); // 100 - 101
		mgr.Slippage.AssertEqual(-1m); // Check total slippage

		// Disable negative slippage
		mgr.CalculateNegative = false;

		var slip2 = mgr.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _secId,
			OriginalTransactionId = 123,
			TradePrice = 100m,
			Side = Sides.Buy
		});

		slip2.AssertEqual(0m); // cannot be < 0
		mgr.Slippage.AssertEqual(-1m); // Total slippage remains unchanged
	}

	[TestMethod]
	public void NoBestPrices()
	{
		var mgr = new SlippageManager();

		// Without Level1/QuoteChange: OrderRegister doesn't add plannedPrice
		var regMsg = new OrderRegisterMessage
		{
			SecurityId = _secId,
			Side = Sides.Buy,
			TransactionId = 404
		};
		mgr.ProcessMessage(regMsg);

		// Execution should return null (no plannedPrice)
		var slip = mgr.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _secId,
			OriginalTransactionId = regMsg.TransactionId,
			TradePrice = 88,
			Side = Sides.Buy
		});

		slip.AssertNull();
		mgr.Slippage.AssertEqual(0m); // Total slippage remains 0
	}

	[TestMethod]
	public void NoPlannedPrice()
	{
		var mgr = new SlippageManager();

		mgr.ProcessMessage(new Level1ChangeMessage
		{
			SecurityId = _secId
		}
		.Add(Level1Fields.BestBidPrice, 90m)
		.Add(Level1Fields.BestAskPrice, 92m));

		// Without order registration, direct Execution — no plannedPrice
		var slip = mgr.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _secId,
			OriginalTransactionId = 999,
			TradePrice = 91m,
			Side = Sides.Buy
		});

		slip.AssertNull();
		mgr.Slippage.AssertEqual(0m); // Total slippage remains 0
	}

	[TestMethod]
	public void ResetWorks()
	{
		var mgr = new SlippageManager();

		mgr.ProcessMessage(new Level1ChangeMessage
		{
			SecurityId = _secId
		}
		.Add(Level1Fields.BestBidPrice, 1m)
		.Add(Level1Fields.BestAskPrice, 2m));

		var regMsg = new OrderRegisterMessage
		{
			SecurityId = _secId,
			Side = Sides.Buy,
			TransactionId = 12
		};
		mgr.ProcessMessage(regMsg);

		var slip = mgr.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _secId,
			OriginalTransactionId = regMsg.TransactionId,
			TradePrice = 3m,
			Side = Sides.Buy
		});
		slip.AssertEqual(1);
		mgr.Slippage.AssertEqual(1m); // Check total slippage before reset

		// Reset
		mgr.ProcessMessage(new ResetMessage());
		mgr.Slippage.AssertEqual(0m); // Check slippage is reset

		// Data should be cleared, next Execution won't find plannedPrice
		var slip2 = mgr.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _secId,
			OriginalTransactionId = 12,
			TradePrice = 3m,
			Side = Sides.Buy
		});
		slip2.AssertNull();
		mgr.Slippage.AssertEqual(0m); // Total slippage remains 0
	}

	[TestMethod]
	public void SaveLoadSettings()
	{
		var mgr = new SlippageManager
		{
			CalculateNegative = false
		};

		var storage = new SettingsStorage();
		mgr.Save(storage);

		var mgr2 = new SlippageManager
		{
			CalculateNegative = true
		};

		mgr2.Load(storage);

		mgr2.CalculateNegative.AssertFalse();
	}

	[TestMethod]
	public void MultipleExecutionsSlippageSum()
	{
		var mgr = new SlippageManager();

		mgr.ProcessMessage(new Level1ChangeMessage
		{
			SecurityId = _secId
		}
		.Add(Level1Fields.BestBidPrice, 100m)
		.Add(Level1Fields.BestAskPrice, 102m));

		// First order
		var regMsg1 = new OrderRegisterMessage
		{
			SecurityId = _secId,
			Side = Sides.Buy,
			TransactionId = 1
		};
		mgr.ProcessMessage(regMsg1);

		var slip1 = mgr.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _secId,
			OriginalTransactionId = regMsg1.TransactionId,
			TradePrice = 104m,
			Side = Sides.Buy
		});

		slip1.AssertEqual(2m); // 104 - 102
		mgr.Slippage.AssertEqual(2m); // Total slippage: 2

		// Second order
		var regMsg2 = new OrderRegisterMessage
		{
			SecurityId = _secId,
			Side = Sides.Sell,
			TransactionId = 2
		};
		mgr.ProcessMessage(regMsg2);

		var slip2 = mgr.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _secId,
			OriginalTransactionId = regMsg2.TransactionId,
			TradePrice = 99m,
			Side = Sides.Sell
		});

		slip2.AssertEqual(1m); // 100 - 99
		mgr.Slippage.AssertEqual(3m); // Total slippage: 2 + 1 = 3

		// Third order with negative slippage
		var regMsg3 = new OrderRegisterMessage
		{
			SecurityId = _secId,
			Side = Sides.Buy,
			TransactionId = 3
		};
		mgr.ProcessMessage(regMsg3);

		var slip3 = mgr.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _secId,
			OriginalTransactionId = regMsg3.TransactionId,
			TradePrice = 101m,
			Side = Sides.Buy
		});

		slip3.AssertEqual(-1m); // 101 - 102
		mgr.Slippage.AssertEqual(2m); // Total slippage: 3 + (-1) = 2
	}
}
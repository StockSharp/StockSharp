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
			TradeVolume = 3m,
			Side = Sides.Buy
		});

		slip.AssertEqual(6m); // (104 - 102) * 3
		mgr.Slippage.AssertEqual(6m); // Check total slippage
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
			TradeVolume = 2m,
			Side = Sides.Sell
		});

		slip.AssertEqual(2m); // (101 - 100) * 2
		mgr.Slippage.AssertEqual(2m); // Check total slippage
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
			TradeVolume = 4m,
			Side = Sides.Buy
		});

		slip.AssertEqual(20m); // (60 - 55) * 4
		mgr.Slippage.AssertEqual(20m); // Check total slippage
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
			TradeVolume = 2m,
			Side = Sides.Buy
		});

		slip.AssertEqual(-2m); // (100 - 101) * 2
		mgr.Slippage.AssertEqual(-2m); // Check total slippage

		// Disable negative slippage
		mgr.CalculateNegative = false;

		var slip2 = mgr.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _secId,
			OriginalTransactionId = 123,
			TradePrice = 100m,
			TradeVolume = 2m,
			Side = Sides.Buy
		});

		slip2.AssertEqual(0m); // cannot be < 0
		mgr.Slippage.AssertEqual(-2m); // Total slippage remains unchanged
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
			TradeVolume = 5m,
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
			TradeVolume = 10m,
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
			TradeVolume = 1m,
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
			TradeVolume = 1m,
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
			TradeVolume = 2m,
			Side = Sides.Buy
		});

		slip1.AssertEqual(4m); // (104 - 102) * 2
		mgr.Slippage.AssertEqual(4m); // Total slippage: 4

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
			TradeVolume = 1m,
			Side = Sides.Sell
		});

		slip2.AssertEqual(1m); // (100 - 99) * 1
		mgr.Slippage.AssertEqual(5m); // Total slippage: 4 + 1 = 5

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
			TradeVolume = 3m,
			Side = Sides.Buy
		});

		slip3.AssertEqual(-3m); // (101 - 102) * 3
		mgr.Slippage.AssertEqual(2m); // Total slippage: 4 + 1 - 3 = 2
	}

	[TestMethod]
	public void MissingTradePrice()
	{
		var mgr = new SlippageManager();

		mgr.ProcessMessage(new Level1ChangeMessage
		{
			SecurityId = _secId
		}
		.Add(Level1Fields.BestBidPrice, 100m)
		.Add(Level1Fields.BestAskPrice, 102m));

		var regMsg = new OrderRegisterMessage
		{
			SecurityId = _secId,
			Side = Sides.Buy,
			TransactionId = 500
		};
		mgr.ProcessMessage(regMsg);

		// No TradePrice, only TradeVolume provided
		var result = mgr.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _secId,
			OriginalTransactionId = regMsg.TransactionId,
			TradeVolume = 5m,
			Side = Sides.Buy
		});

		result.AssertNull();
		mgr.Slippage.AssertEqual(0m);
	}

	[TestMethod]
	public void PlannedPriceRemoved()
	{
		var mgr = new SlippageManager();

		mgr.ProcessMessage(new Level1ChangeMessage
		{
			SecurityId = _secId
		}
		.Add(Level1Fields.BestBidPrice, 100m)
		.Add(Level1Fields.BestAskPrice, 102m));

		var regMsg = new OrderRegisterMessage
		{
			SecurityId = _secId,
			Side = Sides.Buy,
			TransactionId = 600
		};
		mgr.ProcessMessage(regMsg);

		var first = mgr.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _secId,
			OriginalTransactionId = regMsg.TransactionId,
			TradePrice = 103m,
			TradeVolume = 1m,
			Side = Sides.Buy
		});
		first.AssertEqual(1m);
		mgr.Slippage.AssertEqual(1m);

		// Second partial execution should accumulate slippage (planned price still present)
		var second = mgr.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _secId,
			OriginalTransactionId = regMsg.TransactionId,
			TradePrice = 105m,
			TradeVolume = 1m,
			Side = Sides.Buy
		});
		second.AssertEqual(3m); // (105 - 102) * 1
		mgr.Slippage.AssertEqual(4m);

		// Now mark order as completed -> planned price should be removed
		mgr.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _secId,
			OriginalTransactionId = regMsg.TransactionId,
			HasOrderInfo = true,
			OrderState = OrderStates.Done,
			Side = Sides.Buy
		});

		// Further executions should not use (or find) planned price
		var afterDone = mgr.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _secId,
			OriginalTransactionId = regMsg.TransactionId,
			TradePrice = 106m,
			TradeVolume = 1m,
			Side = Sides.Buy
		});
		afterDone.AssertNull();
		mgr.Slippage.AssertEqual(4m);
	}
}
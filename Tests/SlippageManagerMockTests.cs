namespace StockSharp.Tests;

using StockSharp.Algo.Slippage;

[TestClass]
public class SlippageManagerMockTests : BaseTestClass
{
	private static readonly SecurityId _secId = Helper.CreateSecurityId();

	[TestMethod]
	public void Reset_ClearsState()
	{
		var mockState = new Mock<ISlippageManagerState>();
		var mgr = new SlippageManager(mockState.Object);

		mgr.Reset();

		mockState.Verify(s => s.Clear(), Times.Once);
	}

	[TestMethod]
	public void ResetMessage_ClearsState()
	{
		var mockState = new Mock<ISlippageManagerState>();
		var mgr = new SlippageManager(mockState.Object);

		mgr.ProcessMessage(new ResetMessage());

		mockState.Verify(s => s.Clear(), Times.Once);
	}

	[TestMethod]
	public void Level1_UpdatesBestPrices()
	{
		var mockState = new Mock<ISlippageManagerState>();
		var mgr = new SlippageManager(mockState.Object);
		var t = DateTime.UtcNow;

		var l1 = new Level1ChangeMessage { SecurityId = _secId, ServerTime = t };
		l1.Add(Level1Fields.BestBidPrice, 100m);
		l1.Add(Level1Fields.BestAskPrice, 101m);

		mgr.ProcessMessage(l1);

		mockState.Verify(s => s.UpdateBestPrices(_secId, 100m, 101m, t), Times.Once);
	}

	[TestMethod]
	public void Level1_NoPrices_DoesNotUpdate()
	{
		var mockState = new Mock<ISlippageManagerState>();
		var mgr = new SlippageManager(mockState.Object);

		var l1 = new Level1ChangeMessage { SecurityId = _secId, ServerTime = DateTime.UtcNow };
		l1.Add(Level1Fields.LastTradePrice, 100m);

		mgr.ProcessMessage(l1);

		mockState.Verify(s => s.UpdateBestPrices(It.IsAny<SecurityId>(), It.IsAny<decimal?>(), It.IsAny<decimal?>(), It.IsAny<DateTime>()), Times.Never);
	}

	[TestMethod]
	public void QuoteChange_UpdatesBestPrices()
	{
		var mockState = new Mock<ISlippageManagerState>();
		var mgr = new SlippageManager(mockState.Object);
		var t = DateTime.UtcNow;

		var quotes = new QuoteChangeMessage
		{
			SecurityId = _secId,
			ServerTime = t,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [new QuoteChange(101m, 10)],
		};

		mgr.ProcessMessage(quotes);

		mockState.Verify(s => s.UpdateBestPrices(_secId, 100m, 101m, t), Times.Once);
	}

	[TestMethod]
	public void QuoteChange_WithState_SkipsUpdate()
	{
		var mockState = new Mock<ISlippageManagerState>();
		var mgr = new SlippageManager(mockState.Object);

		var quotes = new QuoteChangeMessage
		{
			SecurityId = _secId,
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [new QuoteChange(101m, 10)],
			State = QuoteChangeStates.SnapshotComplete,
		};

		mgr.ProcessMessage(quotes);

		mockState.Verify(s => s.UpdateBestPrices(It.IsAny<SecurityId>(), It.IsAny<decimal?>(), It.IsAny<decimal?>(), It.IsAny<DateTime>()), Times.Never);
	}

	[TestMethod]
	public void OrderRegister_AddsPlannedPrice()
	{
		var mockState = new Mock<ISlippageManagerState>();
		mockState.Setup(s => s.TryGetBestPrice(_secId, Sides.Buy, out It.Ref<decimal>.IsAny))
			.Returns(new TryGetBestPriceDelegate((SecurityId secId, Sides side, out decimal price) =>
			{
				price = 101m; // ask price for buy
				return true;
			}));

		var mgr = new SlippageManager(mockState.Object);

		mgr.ProcessMessage(new OrderRegisterMessage
		{
			TransactionId = 1,
			SecurityId = _secId,
			Side = Sides.Buy,
		});

		mockState.Verify(s => s.AddPlannedPrice(1, Sides.Buy, 101m), Times.Once);
	}

	private delegate bool TryGetBestPriceDelegate(SecurityId secId, Sides side, out decimal price);

	[TestMethod]
	public void OrderRegister_NoBestPrice_DoesNotAddPlanned()
	{
		var mockState = new Mock<ISlippageManagerState>();
		mockState.Setup(s => s.TryGetBestPrice(It.IsAny<SecurityId>(), It.IsAny<Sides>(), out It.Ref<decimal>.IsAny))
			.Returns(false);

		var mgr = new SlippageManager(mockState.Object);

		mgr.ProcessMessage(new OrderRegisterMessage
		{
			TransactionId = 1,
			SecurityId = _secId,
			Side = Sides.Buy,
		});

		mockState.Verify(s => s.AddPlannedPrice(It.IsAny<long>(), It.IsAny<Sides>(), It.IsAny<decimal>()), Times.Never);
	}

	[TestMethod]
	public void Execution_WithTrade_CalculatesSlippage()
	{
		var mockState = new Mock<ISlippageManagerState>();

		var side = Sides.Buy;
		var price = 100m;
		mockState.Setup(s => s.TryGetPlannedPrice(1, out side, out price)).Returns(true);

		var mgr = new SlippageManager(mockState.Object);

		var result = mgr.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 1,
			TradePrice = 101m,
			TradeVolume = 10m,
			ServerTime = DateTime.UtcNow,
		});

		// slippage = (101 - 100) * 10 = 10 for buy
		result.AssertNotNull();
		result.Value.AssertEqual(10m);
		mockState.Verify(s => s.AddSlippage(10m), Times.Once);
	}

	[TestMethod]
	public void Execution_SellSide_CalculatesSlippageCorrectly()
	{
		var mockState = new Mock<ISlippageManagerState>();

		var side = Sides.Sell;
		var price = 100m;
		mockState.Setup(s => s.TryGetPlannedPrice(1, out side, out price)).Returns(true);

		var mgr = new SlippageManager(mockState.Object);

		var result = mgr.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 1,
			TradePrice = 99m,
			TradeVolume = 5m,
			ServerTime = DateTime.UtcNow,
		});

		// slippage = (100 - 99) * 5 = 5 for sell
		result.AssertNotNull();
		result.Value.AssertEqual(5m);
		mockState.Verify(s => s.AddSlippage(5m), Times.Once);
	}

	[TestMethod]
	public void Execution_NegativeSlippage_WhenCalculateNegativeDisabled_ReturnsZero()
	{
		var mockState = new Mock<ISlippageManagerState>();

		var side = Sides.Buy;
		var price = 100m;
		mockState.Setup(s => s.TryGetPlannedPrice(1, out side, out price)).Returns(true);

		var mgr = new SlippageManager(mockState.Object) { CalculateNegative = false };

		var result = mgr.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 1,
			TradePrice = 99m, // better price than planned for buy
			TradeVolume = 10m,
			ServerTime = DateTime.UtcNow,
		});

		// slippage = (99 - 100) * 10 = -10 → clamped to 0
		result.AssertNotNull();
		result.Value.AssertEqual(0m);
		mockState.Verify(s => s.AddSlippage(0m), Times.Once);
	}

	[TestMethod]
	public void Execution_OrderDone_RemovesPlannedPrice()
	{
		var mockState = new Mock<ISlippageManagerState>();

		var side = Sides.Buy;
		var price = 100m;
		mockState.Setup(s => s.TryGetPlannedPrice(1, out side, out price)).Returns(true);

		var mgr = new SlippageManager(mockState.Object);

		mgr.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 1,
			TradePrice = 101m,
			TradeVolume = 10m,
			HasOrderInfo = true,
			OrderState = OrderStates.Done,
			ServerTime = DateTime.UtcNow,
		});

		mockState.Verify(s => s.RemovePlannedPrice(1), Times.Once);
	}

	[TestMethod]
	public void Execution_OrderFinalState_NoTrade_RemovesPlannedPrice()
	{
		var mockState = new Mock<ISlippageManagerState>();
		var mgr = new SlippageManager(mockState.Object);

		mgr.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OriginalTransactionId = 1,
			OrderState = OrderStates.Done,
			ServerTime = DateTime.UtcNow,
		});

		mockState.Verify(s => s.RemovePlannedPrice(1), Times.Once);
	}

	[TestMethod]
	public void Slippage_ReadsFromState()
	{
		var mockState = new Mock<ISlippageManagerState>();
		mockState.Setup(s => s.Slippage).Returns(42.5m);

		var mgr = new SlippageManager(mockState.Object);

		mgr.Slippage.AssertEqual(42.5m);
	}
}

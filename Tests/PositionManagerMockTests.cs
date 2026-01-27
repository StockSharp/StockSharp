namespace StockSharp.Tests;

using StockSharp.Algo.Positions;

[TestClass]
public class PositionManagerMockTests : BaseTestClass
{
	private static readonly SecurityId _secId = Helper.CreateSecurityId();
	private const string Portfolio = "pf";

	private static Mock<IPositionManagerState> CreateStateMock()
	{
		return new Mock<IPositionManagerState>(MockBehavior.Loose);
	}

	[TestMethod]
	public void Reset_ClearsState()
	{
		var stateMock = CreateStateMock();
		var manager = new PositionManager(true, stateMock.Object);

		manager.ProcessMessage(new ResetMessage());

		stateMock.Verify(s => s.Clear(), Times.Once());
	}

	[TestMethod]
	public void OrderRegister_AddsOrder()
	{
		var stateMock = CreateStateMock();
		stateMock
			.Setup(s => s.AddOrGetOrder(1, _secId, Portfolio, Sides.Buy, 10m, 10m))
			.Returns(10m);

		var manager = new PositionManager(true, stateMock.Object);

		var regMsg = new OrderRegisterMessage
		{
			TransactionId = 1,
			SecurityId = _secId,
			PortfolioName = Portfolio,
			Side = Sides.Buy,
			Volume = 10,
		};

		manager.ProcessMessage(regMsg);

		stateMock.Verify(s => s.AddOrGetOrder(1, _secId, Portfolio, Sides.Buy, 10m, 10m), Times.Once());
	}

	[TestMethod]
	public void Execution_ByOrders_UpdatesPosition()
	{
		var stateMock = CreateStateMock();
		stateMock
			.Setup(s => s.AddOrGetOrder(1, _secId, Portfolio, Sides.Buy, 10m, 10m))
			.Returns(10m);
		stateMock
			.Setup(s => s.AddOrGetOrder(1, _secId, Portfolio, Sides.Buy, 10m, 7m))
			.Returns(10m);
		stateMock
			.Setup(s => s.UpdatePosition(_secId, Portfolio, It.IsAny<decimal>()))
			.Returns(3m);

		var manager = new PositionManager(true, stateMock.Object);

		// Register order first
		manager.ProcessMessage(new OrderRegisterMessage
		{
			TransactionId = 1,
			SecurityId = _secId,
			PortfolioName = Portfolio,
			Side = Sides.Buy,
			Volume = 10,
		});

		// Execution with balance change
		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			TransactionId = 1,
			SecurityId = _secId,
			PortfolioName = Portfolio,
			Side = Sides.Buy,
			OrderVolume = 10,
			Balance = 7,
			ServerTime = DateTime.UtcNow,
		};

		var result = manager.ProcessMessage(execMsg);

		stateMock.Verify(s => s.UpdateOrderBalance(1, 7m), Times.Once());
		stateMock.Verify(s => s.UpdatePosition(_secId, Portfolio, It.IsAny<decimal>()), Times.Once());
		result.AssertNotNull();
	}

	[TestMethod]
	public void Execution_ByTrades_UpdatesPosition()
	{
		var stateMock = CreateStateMock();
		stateMock
			.Setup(s => s.AddOrGetOrder(1, _secId, Portfolio, Sides.Buy, 10m, 10m))
			.Returns(10m);

		var secId = default(SecurityId);
		var pfName = default(string);
		var side = default(Sides);
		var balance = default(decimal);

		stateMock
			.Setup(s => s.TryGetOrder(1, out secId, out pfName, out side, out balance))
			.Returns(false);

		stateMock
			.Setup(s => s.UpdatePosition(It.IsAny<SecurityId>(), It.IsAny<string>(), It.IsAny<decimal>()))
			.Returns(5m);

		var manager = new PositionManager(false, stateMock.Object);

		// Register order first
		manager.ProcessMessage(new OrderRegisterMessage
		{
			TransactionId = 1,
			SecurityId = _secId,
			PortfolioName = Portfolio,
			Side = Sides.Buy,
			Volume = 10,
		});

		// Execution with trade info (HasTradeInfo is computed from TradeVolume/TradePrice)
		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 1,
			SecurityId = _secId,
			PortfolioName = Portfolio,
			Side = Sides.Buy,
			TradeVolume = 5,
			TradePrice = 100,
			ServerTime = DateTime.UtcNow,
		};

		var result = manager.ProcessMessage(execMsg);

		stateMock.Verify(s => s.UpdatePosition(It.IsAny<SecurityId>(), It.IsAny<string>(), It.IsAny<decimal>()), Times.Once());
		result.AssertNotNull();
	}

	[TestMethod]
	public void Execution_FinalState_RemovesOrder()
	{
		var stateMock = CreateStateMock();
		stateMock
			.Setup(s => s.AddOrGetOrder(1, _secId, Portfolio, Sides.Buy, 10m, 10m))
			.Returns(10m);
		stateMock
			.Setup(s => s.AddOrGetOrder(1, _secId, Portfolio, Sides.Buy, 10m, 0m))
			.Returns(10m);
		stateMock
			.Setup(s => s.UpdatePosition(_secId, Portfolio, It.IsAny<decimal>()))
			.Returns(10m);

		var manager = new PositionManager(true, stateMock.Object);

		// Register order first
		manager.ProcessMessage(new OrderRegisterMessage
		{
			TransactionId = 1,
			SecurityId = _secId,
			PortfolioName = Portfolio,
			Side = Sides.Buy,
			Volume = 10,
		});

		// Execution with final state
		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			TransactionId = 1,
			SecurityId = _secId,
			PortfolioName = Portfolio,
			Side = Sides.Buy,
			OrderVolume = 10,
			Balance = 0,
			OrderState = OrderStates.Done,
			ServerTime = DateTime.UtcNow,
		};

		manager.ProcessMessage(execMsg);

		stateMock.Verify(s => s.RemoveOrder(1), Times.Once());
	}
}

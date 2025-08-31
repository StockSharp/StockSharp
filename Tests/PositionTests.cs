namespace StockSharp.Tests;

using StockSharp.Algo.Positions;

[TestClass]
public class PositionTests
{
	[TestMethod]
	public void UpdateByOrders()
	{
		var secId = Helper.CreateSecurityId();
		var manager = new PositionManager(true);

		manager.ProcessMessage(new OrderRegisterMessage
		{
			TransactionId = 1,
			SecurityId = secId,
			PortfolioName = "pf",
			Side = Sides.Buy,
			Volume = 10,
		});

		var change = manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			TransactionId = 1,
			SecurityId = secId,
			PortfolioName = "pf",
			Side = Sides.Buy,
			OrderVolume = 10,
			Balance = 7,
			ServerTime = DateTimeOffset.UtcNow,
		});

		change.AssertNotNull();
		change.Changes[PositionChangeTypes.CurrentValue].To<decimal>().AssertEqual(3m);
	}

	[TestMethod]
	public void UpdateByTrades()
	{
		var secId = Helper.CreateSecurityId();
		var manager = new PositionManager(false);

		manager.ProcessMessage(new OrderRegisterMessage
		{
			TransactionId = 1,
			SecurityId = secId,
			PortfolioName = "pf",
			Side = Sides.Buy,
			Volume = 10,
		});

		var change = manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 1,
			SecurityId = secId,
			PortfolioName = "pf",
			Side = Sides.Buy,
			TradeVolume = 2,
			ServerTime = DateTimeOffset.UtcNow,
		});

		change.AssertNotNull();
		change.Changes[PositionChangeTypes.CurrentValue].To<decimal>().AssertEqual(2m);
	}

	[TestMethod]
	public void ResetClearsState()
	{
		var secId = Helper.CreateSecurityId();
		var manager = new PositionManager(true);

		var regMsg = new OrderRegisterMessage
		{
			TransactionId = 1,
			SecurityId = secId,
			PortfolioName = "pf",
			Side = Sides.Buy,
			Volume = 10,
		};
		manager.ProcessMessage(regMsg);

		manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OriginalTransactionId = regMsg.TransactionId,
			SecurityId = secId,
			PortfolioName = "pf",
			Side = Sides.Buy,
			OrderVolume = 10,
			Balance = 7,
			ServerTime = DateTimeOffset.UtcNow,
		});

		manager.ProcessMessage(new ResetMessage());

		var change = manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OriginalTransactionId = regMsg.TransactionId,
			SecurityId = secId,
			PortfolioName = "pf",
			Side = Sides.Buy,
			OrderVolume = 10,
			Balance = 5,
			ServerTime = DateTimeOffset.UtcNow,
		});

		change.AssertNull();
	}

	[TestMethod]
	public void ApplyPositionChange()
	{
		var secId = Helper.CreateSecurityId();
		var manager = new PositionManager(true);

		manager.ProcessMessage(new OrderRegisterMessage
		{
			TransactionId = 1,
			SecurityId = secId,
			PortfolioName = "pf",
			Side = Sides.Buy,
			Volume = 10,
		});

		var change = manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			TransactionId = 1,
			SecurityId = secId,
			PortfolioName = "pf",
			Side = Sides.Buy,
			OrderVolume = 10,
			Balance = 7,
			ServerTime = DateTimeOffset.UtcNow,
		});

		change.AssertNotNull();
		((decimal)change.Changes[PositionChangeTypes.CurrentValue]).AssertEqual(3m);
	}

	[TestMethod]
	public void IgnoreFurtherOrderUpdates()
	{
		var secId = Helper.CreateSecurityId();
		var manager = new PositionManager(true);

		var reg = new OrderRegisterMessage
		{
			TransactionId = 1001,
			SecurityId = secId,
			PortfolioName = "pf",
			Side = Sides.Buy,
			Volume = 10,
		};
		manager.ProcessMessage(reg);

		// full fill -> position increases by 10
		var first = manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			TransactionId = reg.TransactionId,
			SecurityId = secId,
			PortfolioName = reg.PortfolioName,
			Side = reg.Side,
			OrderVolume = reg.Volume,
			Balance = 0,
			OrderState = OrderStates.Done,
			ServerTime = DateTimeOffset.UtcNow,
		});
		first.AssertNotNull();
		first.Changes[PositionChangeTypes.CurrentValue].To<decimal>().AssertEqual(10m);

		// any later "update" for that order must be ignored
		var afterComplete = manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OriginalTransactionId = reg.TransactionId,
			Balance = 1, // bogus balance after completion
			ServerTime = DateTimeOffset.UtcNow,
		});

		afterComplete.AssertNull();
	}

	private class TestPositionManager : IPositionManager
	{
		public PositionChangeMessage ProcessMessage(Message message)
			=> null;

		void IPersistable.Load(SettingsStorage storage) { }
		void IPersistable.Save(SettingsStorage storage) { }
	}

	private class TestInnerAdapter(bool? emulate) : PassThroughMessageAdapter(new IncrementalIdGenerator())
	{
		public override bool? IsPositionsEmulationRequired
			=> emulate;
	}

	[TestMethod]
	public void ChangeHasSubscriptionId()
	{
		var manager = new TestPositionManager();
		var inner = new TestInnerAdapter(true);
		var adapter = new PositionMessageAdapter(inner, manager);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		var lookup = new PortfolioLookupMessage
		{
			IsSubscribe = true,
			TransactionId = 1
		};

		adapter.SendInMessage(lookup);
		output.Count.AssertEqual(1);
		output[0].AssertOfType<SubscriptionOnlineMessage>();
		output.Clear();

		var secId = new SecurityId { SecurityCode = "S", BoardCode = "X" };

		adapter.SendInMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			PortfolioName = "pf",
			Side = Sides.Buy,
			TradeVolume = 1m,
			SecurityId = secId,
			ServerTime = DateTimeOffset.UtcNow
		});

		output.Count.AssertEqual(1);
		output[0].AssertOfType<ExecutionMessage>();
	}
}

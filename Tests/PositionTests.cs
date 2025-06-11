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

		manager.ProcessMessage(new OrderRegisterMessage
		{
			TransactionId = 1,
			SecurityId = secId,
			PortfolioName = "pf",
			Side = Sides.Buy,
			Volume = 10,
		});

		manager.ProcessMessage(new ExecutionMessage
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

		manager.ProcessMessage(new ResetMessage());

		var change = manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			TransactionId = 1,
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

		manager.ProcessMessage(new PositionChangeMessage
		{
			SecurityId = secId,
			PortfolioName = "pf",
			ServerTime = DateTimeOffset.UtcNow
		}.Add(PositionChangeTypes.CurrentValue, 5m));

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
		change.Changes[PositionChangeTypes.CurrentValue].To<decimal>().AssertEqual(8m);
	}

	private sealed class TestPositionManager : IPositionManager
	{
		public Func<Message, PositionChangeMessage> Processor { get; set; }

		public PositionChangeMessage ProcessMessage(Message message)
			=> Processor?.Invoke(message);
	}

	private sealed class TestInnerAdapter(bool? emulate) : PassThroughMessageAdapter(new IncrementalIdGenerator())
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
		((PortfolioLookupMessage)output[0]).IsSubscribe.AssertFalse();
		output.Clear();

		var change = new PositionChangeMessage
		{
			PortfolioName = "pf",
			SecurityId = new SecurityId { SecurityCode = "S", BoardCode = "X" },
			ServerTime = DateTimeOffset.UtcNow
		}.Add(PositionChangeTypes.CurrentValue, 1m);

		manager.Processor = _ => change;

		adapter.SendInMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			PortfolioName = "pf",
			Side = Sides.Buy,
			TradeVolume = 1m,
			SecurityId = change.SecurityId,
			ServerTime = DateTimeOffset.UtcNow
		});

		output.Count.AssertEqual(2);
		(output[0] is ExecutionMessage).AssertTrue();
		var pos = (PositionChangeMessage)output[1];
		pos.GetSubscriptionIds().Length.AssertEqual(1);
		pos.GetSubscriptionIds()[0].AssertEqual(1L);
	}
}

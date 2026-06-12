namespace StockSharp.Tests;

using StockSharp.Algo.Positions;

[TestClass]
public class PositionTests : BaseTestClass
{
	[TestMethod]
	public void UpdateByOrders()
	{
		var secId = Helper.CreateSecurityId();
		var manager = new PositionManager(true, new PositionManagerState());

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
			ServerTime = DateTime.UtcNow,
		});

		change.AssertNotNull();
		change.Changes[PositionChangeTypes.CurrentValue].To<decimal>().AssertEqual(3m);
	}

	[TestMethod]
	public void UpdateByTrades()
	{
		var secId = Helper.CreateSecurityId();
		var manager = new PositionManager(false, new PositionManagerState());

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
			ServerTime = DateTime.UtcNow,
		});

		change.AssertNotNull();
		change.Changes[PositionChangeTypes.CurrentValue].To<decimal>().AssertEqual(2m);
	}

	[TestMethod]
	public void ResetClearsState()
	{
		var secId = Helper.CreateSecurityId();
		var manager = new PositionManager(true, new PositionManagerState());

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
			ServerTime = DateTime.UtcNow,
		});

		manager.ProcessMessage(new ResetMessage());

		// Reset must clear the orders dictionary: an update for the (now unknown)
		// order is ignored, so no position change is produced.
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
			ServerTime = DateTime.UtcNow,
		});

		change.AssertNull();

		// Reset must also clear the positions dictionary: re-registering the same
		// order and filling it must compute the position from zero (10 - 7 = 3),
		// not accumulate onto the pre-reset value (which would yield 6).
		manager.ProcessMessage(regMsg);

		var afterReset = manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			TransactionId = regMsg.TransactionId,
			SecurityId = secId,
			PortfolioName = "pf",
			Side = Sides.Buy,
			OrderVolume = 10,
			Balance = 7,
			ServerTime = DateTime.UtcNow,
		});

		afterReset.AssertNotNull();
		afterReset.Changes[PositionChangeTypes.CurrentValue].To<decimal>().AssertEqual(3m);
	}

	[TestMethod]
	public void IncomingPositionChangeIsIgnored()
	{
		var secId = Helper.CreateSecurityId();
		var manager = new PositionManager(true, new PositionManagerState());

		// Establish a known position (10 - 7 = 3) via the normal order/fill path.
		manager.ProcessMessage(new OrderRegisterMessage
		{
			TransactionId = 1,
			SecurityId = secId,
			PortfolioName = "pf",
			Side = Sides.Buy,
			Volume = 10,
		});

		var filled = manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			TransactionId = 1,
			SecurityId = secId,
			PortfolioName = "pf",
			Side = Sides.Buy,
			OrderVolume = 10,
			Balance = 7,
			ServerTime = DateTime.UtcNow,
		});

		filled.AssertNotNull();
		filled.Changes[PositionChangeTypes.CurrentValue].To<decimal>().AssertEqual(3m);

		// PositionManager.ProcessMessage has no branch for MessageTypes.PositionChange:
		// an externally supplied position change must be ignored (no echo, no state
		// mutation). The switch only handles Reset/OrderRegister/OrderReplace/Execution.
		var external = new PositionChangeMessage
		{
			SecurityId = secId,
			PortfolioName = "pf",
			ServerTime = DateTime.UtcNow,
		}.Add(PositionChangeTypes.CurrentValue, 999m);

		var result = manager.ProcessMessage(external);

		// The manager must not produce its own change message for an incoming one.
		result.AssertNull();

		// And the externally supplied value must not have leaked into the manager's
		// state: a subsequent fill of a fresh order still computes from the prior
		// internal position (3), giving 3 + (10 - 9) = 4, not anything derived from 999.
		manager.ProcessMessage(new OrderRegisterMessage
		{
			TransactionId = 2,
			SecurityId = secId,
			PortfolioName = "pf",
			Side = Sides.Buy,
			Volume = 10,
		});

		var nextFill = manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			TransactionId = 2,
			SecurityId = secId,
			PortfolioName = "pf",
			Side = Sides.Buy,
			OrderVolume = 10,
			Balance = 9,
			ServerTime = DateTime.UtcNow,
		});

		nextFill.AssertNotNull();
		nextFill.Changes[PositionChangeTypes.CurrentValue].To<decimal>().AssertEqual(4m);
	}

	[TestMethod]
	public void IgnoreFurtherOrderUpdates()
	{
		var secId = Helper.CreateSecurityId();
		var manager = new PositionManager(true, new PositionManagerState());

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
			ServerTime = DateTime.UtcNow,
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
			ServerTime = DateTime.UtcNow,
		});

		afterComplete.AssertNull();
	}

	private class TestInnerAdapter() : PassThroughMessageAdapter(new IncrementalIdGenerator())
	{
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task ChangeHasSubscriptionId()
	{
		// Use a real manager so OnInnerAdapterNewOutMessageAsync actually produces a
		// non-null PositionChangeMessage; only then is the SetSubscriptionIds branch
		// (PositionMessageAdapter.cs:108-112) exercised. A mock that always returns
		// null would leave that contract completely unverified.
		var manager = new PositionManager(false, new PositionManagerState());
		var inner = new TestInnerAdapter();
		var adapter = new PositionMessageAdapter(inner, manager);

		const long subscriptionId = 1;

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var lookup = new PortfolioLookupMessage
		{
			IsSubscribe = true,
			TransactionId = subscriptionId
		};

		var token = CancellationToken;

		await adapter.SendInMessageAsync(lookup, token);
		output.Count.AssertEqual(1);
		output[0].AssertOfType<SubscriptionOnlineMessage>();
		output.Clear();

		var secId = new SecurityId { SecurityCode = "S", BoardCode = "X" };

		// A trade execution bubbling up from the inner adapter must yield a
		// PositionChangeMessage emitted ahead of the original execution, and that
		// change must carry the active portfolio-lookup subscription id.
		await adapter.SendInMessageAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			PortfolioName = "pf",
			Side = Sides.Buy,
			TradeVolume = 1m,
			SecurityId = secId,
			ServerTime = DateTime.UtcNow
		}, token);

		output.Count.AssertEqual(2);

		output[0].AssertOfType<PositionChangeMessage>();
		var change = (PositionChangeMessage)output[0];
		// The actual contract under test: the emitted change carries the subscription id.
		change.GetSubscriptionIds().AssertContains(subscriptionId);

		output[1].AssertOfType<ExecutionMessage>();
	}
}

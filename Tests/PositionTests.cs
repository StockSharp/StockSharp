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

	/// <summary>
	/// A fill can arrive for an order this manager never saw - one placed before the session, a drop
	/// copy, a reconnect - and such a fill need not name a portfolio of its own. The manager sits in
	/// the middle of the message pipeline, so a message it cannot attribute has to be dealt with
	/// rather than thrown out of: a failure here stops the pipeline for everything behind it, and the
	/// positions that were being tracked correctly stop being tracked at all.
	/// </summary>
	[TestMethod]
	public void AFillForAnUnknownOrderThatNamesNoPortfolioLeavesTheManagerWorking()
	{
		var secId = Helper.CreateSecurityId();
		var manager = new PositionManager(false, new PositionManagerState());

		// Nothing registered this order here, and the fill states no portfolio.
		manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 99,
			SecurityId = secId,
			Side = Sides.Buy,
			TradeId = 1,
			TradePrice = 100,
			TradeVolume = 3,
			ServerTime = DateTime.UtcNow,
		});

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

		IsNotNull(change, "the fill that could not be attributed must not cost the manager the ones that can be");
		AreEqual(2m, change.Changes[PositionChangeTypes.CurrentValue].To<decimal>(), "two bought is a position of two");
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

	/// <summary>
	/// A newer sender can add a position field before this version of the entity has a home for it.
	/// The understood changes in the same message still have to be applied so protocol evolution does
	/// not make the whole position update unusable.
	/// </summary>
	[TestMethod]
	public void ApplyChanges_UnknownChangeType_DoesNotBlockKnownChanges()
	{
		var position = new Position
		{
			Portfolio = new Portfolio { Name = "pf" },
			Security = Helper.CreateSecurity(),
		};

		var message = new PositionChangeMessage
		{
			SecurityId = position.Security.ToSecurityId(),
			PortfolioName = position.Portfolio.Name,
			ServerTime = DateTime.UtcNow,
		}
		.Add(PositionChangeTypes.CurrentValue, 7m)
		.Add((PositionChangeTypes)int.MaxValue, 5m);

		position.ApplyChanges(message);

		AreEqual(7m, position.CurrentValue, "a newer protocol field must not prevent understood fields from being applied");
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
		output.Count.AssertEqual(2);
		output[0].AssertOfType<SubscriptionResponseMessage>();
		output[1].AssertOfType<SubscriptionOnlineMessage>();
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

	/// <summary>
	/// A subscription is acknowledged before it is answered. The caller learns that its portfolio
	/// lookup was accepted from the response and from nothing else, so an adapter that answers the
	/// lookup itself and skips straight to "you are online" leaves the caller holding a subscription
	/// it was never told it had - and every position that arrives under it belongs to a subscription
	/// the caller does not know exists.
	/// </summary>
	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task APortfolioLookupIsAcknowledgedBeforeItIsDeclaredOnline()
	{
		var manager = new PositionManager(false, new PositionManagerState());
		var inner = new TestInnerAdapter();
		var adapter = new PositionMessageAdapter(inner, manager);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		const long subscriptionId = 42;

		await adapter.SendInMessageAsync(new PortfolioLookupMessage
		{
			IsSubscribe = true,
			TransactionId = subscriptionId,
		}, CancellationToken);

		HasCount(2, output, "the request is acknowledged, then the subscription is declared online");

		output[0].AssertOfType<SubscriptionResponseMessage>();

		var response = (SubscriptionResponseMessage)output[0];
		AreEqual(subscriptionId, response.OriginalTransactionId, "the acknowledgement has to name the request it answers");
		IsNull(response.Error, "the adapter answers this lookup itself, so there is nothing to refuse");

		output[1].AssertOfType<SubscriptionOnlineMessage>();
	}

	#region Connector position processing

	/// <summary>
	/// A connector whose channels pass messages straight through, so one send is fully processed by
	/// the time it returns.
	/// </summary>
	private sealed class InlineConnector : Connector
	{
		public InlineConnector()
			: base(new InMemorySecurityStorage(), new InMemoryPositionStorage(), new InMemoryExchangeInfoProvider(), initChannels: false)
		{
			InMessageChannel = new PassThroughMessageChannel();
			OutMessageChannel = new PassThroughMessageChannel();
		}
	}

	/// <summary>
	/// A venue that reports a position in lots is reporting how many lots are held, and one lot is
	/// however many units the instrument says a lot is. Converting with the volume step instead -
	/// the smallest amount that may be traded, a different number entirely - hands the trader a
	/// position that is wrong by whatever ratio the two happen to stand in, and every P&amp;L, risk
	/// figure and closing order computed from it is wrong by the same factor.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task APositionReportedInLotsIsConvertedWithTheInstrumentsLotSize()
	{
		var connector = new InlineConnector();
		var secId = Helper.CreateSecurityId();

		// One lot is a hundred units, and the smallest tradable amount is a single unit.
		await connector.SendOutMessageAsync(new SecurityMessage
		{
			SecurityId = secId,
			Multiplier = 100m,
			VolumeStep = 1m,
		}, CancellationToken);

		await connector.SendOutMessageAsync(new PositionChangeMessage
		{
			SecurityId = secId,
			PortfolioName = "pf",
			ServerTime = DateTime.UtcNow,
		}.Add(PositionChangeTypes.CurrentValueInLots, 5m), CancellationToken);

		var position = connector.Positions.FirstOrDefault(p => p.Security?.ToSecurityId() == secId);

		IsNotNull(position, "the venue reported a position, so the trader has to have one");
		AreEqual(500m, position.CurrentValue, "five lots of a hundred units each is five hundred units");
	}

	/// <summary>
	/// The position update belongs to whoever sent it, and the connector is one of its readers, not
	/// its owner. Rewriting the update in place - replacing the lot figure the venue stated with a
	/// unit figure the connector computed - means everything reading the same update afterwards, a
	/// storage writing it down included, keeps a number the venue never sent, with no way left to
	/// tell what it actually said.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task TheConnectorLeavesThePositionUpdateItWasGivenAsItWasSent()
	{
		var connector = new InlineConnector();
		var secId = Helper.CreateSecurityId();

		await connector.SendOutMessageAsync(new SecurityMessage
		{
			SecurityId = secId,
			Multiplier = 100m,
			VolumeStep = 1m,
		}, CancellationToken);

		var message = new PositionChangeMessage
		{
			SecurityId = secId,
			PortfolioName = "pf",
			ServerTime = DateTime.UtcNow,
		}.Add(PositionChangeTypes.CurrentValueInLots, 5m);

		await connector.SendOutMessageAsync(message, CancellationToken);

		IsTrue(message.Changes.ContainsKey(PositionChangeTypes.CurrentValueInLots),
			"the venue stated a figure in lots, and that is what its message still has to say");
		IsFalse(message.Changes.ContainsKey(PositionChangeTypes.CurrentValue),
			"the connector's own conversion belongs to the position it keeps, not to the sender's message");
	}

	/// <summary>
	/// A position a venue reports against a strategy is still a position the account holds, and the
	/// trader has to see it. Dropping it without a word leaves the account looking flat while real
	/// exposure stands behind it - nothing in the portfolio, nothing in the logs, and risk measured
	/// against a book that is missing the very positions a running strategy put on.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task APositionReportedForAStrategyStillReachesTheTrader()
	{
		var connector = new InlineConnector();
		var secId = Helper.CreateSecurityId();

		const string strategyId = "MyStrategy";

		await connector.SendOutMessageAsync(new PositionChangeMessage
		{
			SecurityId = secId,
			PortfolioName = "pf",
			StrategyId = strategyId,
			ServerTime = DateTime.UtcNow,
		}.Add(PositionChangeTypes.CurrentValue, 7m), CancellationToken);

		var position = connector.Positions.FirstOrDefault(p => p.StrategyId == strategyId);

		IsNotNull(position, "a position the venue reported for a strategy is exposure the account really has");
		AreEqual(7m, position.CurrentValue, "and it is worth what the venue said it is worth");
	}

	#endregion
}

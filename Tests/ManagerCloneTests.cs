namespace StockSharp.Tests;

using StockSharp.Algo.Commissions;
using StockSharp.Algo.Latency;
using StockSharp.Algo.PnL;
using StockSharp.Algo.Slippage;
using StockSharp.Algo.Positions;

[TestClass]
public class ManagerCloneTests : BaseTestClass
{
	private sealed class TestReceiver : TestLogReceiver
	{
	}

	private static readonly SecurityId _secId = Helper.CreateSecurityId();

	#region Tracking state subclasses (for type preservation verification)

	private class TrackingLatencyManagerState : LatencyManagerState
	{
		public static int InstanceCount;
		public TrackingLatencyManagerState() => Interlocked.Increment(ref InstanceCount);
	}

	private class TrackingSlippageManagerState : SlippageManagerState
	{
		public static int InstanceCount;
		public TrackingSlippageManagerState() => Interlocked.Increment(ref InstanceCount);
	}

	private class TrackingPositionManagerState : PositionManagerState
	{
		public static int InstanceCount;
		public TrackingPositionManagerState() => Interlocked.Increment(ref InstanceCount);
	}

	private class TrackingOrderBookIncrementManagerState : OrderBookIncrementManagerState
	{
		public static int InstanceCount;
		public TrackingOrderBookIncrementManagerState() => Interlocked.Increment(ref InstanceCount);
	}

	private class TrackingOrderBookTruncateManagerState : OrderBookTruncateManagerState
	{
		public static int InstanceCount;
		public TrackingOrderBookTruncateManagerState() => Interlocked.Increment(ref InstanceCount);
	}

	private class TrackingLevel1DepthBuilderManagerState : Level1DepthBuilderManagerState
	{
		public static int InstanceCount;
		public TrackingLevel1DepthBuilderManagerState() => Interlocked.Increment(ref InstanceCount);
	}

	#endregion

	#region LatencyManager

	[TestMethod]
	public void LatencyManager_Clone_CreatesNewInstance()
	{
		var manager = new LatencyManager(new LatencyManagerState());

		var clone = manager.Clone();

		clone.AssertNotNull();
		clone.AssertNotSame(manager);
	}

	[TestMethod]
	public void LatencyManager_Clone_StateIsIndependent()
	{
		var manager = new LatencyManager(new LatencyManagerState());
		var t0 = DateTime.UtcNow;

		manager.ProcessMessage(new OrderRegisterMessage { TransactionId = 1, LocalTime = t0 });

		var clone = manager.Clone();

		// clone state should be empty — registration for transId=1 should not exist
		var latency = clone.ProcessMessage(new ExecutionMessage
		{
			OriginalTransactionId = 1,
			LocalTime = t0 + TimeSpan.FromMilliseconds(50),
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
		});

		latency.AssertNull("Clone should have independent empty state");
	}

	[TestMethod]
	public void LatencyManager_Clone_StateIsFunctional()
	{
		var manager = new LatencyManager(new LatencyManagerState());
		var clone = manager.Clone();

		var t0 = DateTime.UtcNow;
		var t1 = t0 + TimeSpan.FromMilliseconds(100);

		clone.ProcessMessage(new OrderRegisterMessage { TransactionId = 10, LocalTime = t0 });

		var latency = clone.ProcessMessage(new ExecutionMessage
		{
			OriginalTransactionId = 10,
			LocalTime = t1,
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
		});

		latency.AssertNotNull("Clone state should track registrations");
		latency.Value.AssertEqual(TimeSpan.FromMilliseconds(100));
	}

	#endregion

	#region SlippageManager

	[TestMethod]
	public void SlippageManager_Clone_CreatesNewInstance()
	{
		var manager = new SlippageManager(new SlippageManagerState());

		var clone = manager.Clone();

		clone.AssertNotNull();
		clone.AssertNotSame(manager);
	}

	[TestMethod]
	public void SlippageManager_Clone_PreservesSettings()
	{
		var manager = new SlippageManager(new SlippageManagerState());
		manager.CalculateNegative = false;

		var clone = (SlippageManager)manager.Clone();

		clone.CalculateNegative.AssertEqual(false);
	}

	[TestMethod]
	public void SlippageManager_Clone_StateIsIndependent()
	{
		var manager = new SlippageManager(new SlippageManagerState());

		var l1 = new Level1ChangeMessage
		{
			SecurityId = _secId,
			ServerTime = DateTime.UtcNow,
		};
		l1.TryAdd(Level1Fields.BestBidPrice, 100m);
		l1.TryAdd(Level1Fields.BestAskPrice, 101m);
		manager.ProcessMessage(l1);
		manager.ProcessMessage(new OrderRegisterMessage
		{
			TransactionId = 1,
			SecurityId = _secId,
			Side = Sides.Buy,
			Price = 101m,
			Volume = 1m,
		});
		manager.ProcessMessage(new ExecutionMessage
		{
			OriginalTransactionId = 1,
			SecurityId = _secId,
			DataTypeEx = DataType.Transactions,
			TradePrice = 102m,
			TradeVolume = 1m,
		});
		manager.Slippage.AssertEqual(1m);

		var clone = manager.Clone();

		clone.Slippage.AssertEqual(0);
	}

	[TestMethod]
	public void SlippageManager_Clone_StateIsFunctional()
	{
		var manager = new SlippageManager(new SlippageManagerState());
		var clone = manager.Clone();

		// Feed Level1 to set best prices in clone state
		var l1 = new Level1ChangeMessage
		{
			SecurityId = _secId,
			ServerTime = DateTime.UtcNow,
		};
		l1.TryAdd(Level1Fields.BestBidPrice, 100m);
		l1.TryAdd(Level1Fields.BestAskPrice, 101m);
		clone.ProcessMessage(l1);

		// Register order at ask price (buy)
		clone.ProcessMessage(new OrderRegisterMessage
		{
			TransactionId = 1,
			SecurityId = _secId,
			Side = Sides.Buy,
			Price = 101m,
			Volume = 1,
			LocalTime = DateTime.UtcNow,
		});

		// Execute at same price — zero slippage
		var slippage = clone.ProcessMessage(new ExecutionMessage
		{
			OriginalTransactionId = 1,
			SecurityId = _secId,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			TradePrice = 101m,
			TradeVolume = 1,
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		});

		// state should have processed the messages without crash
		slippage.AssertNotNull("Clone state should calculate slippage");
	}

	#endregion

	#region PositionManager

	[TestMethod]
	public void PositionManager_Clone_CreatesNewInstance()
	{
		var manager = new PositionManager(true, new PositionManagerState());

		var clone = manager.Clone();

		clone.AssertNotNull();
		clone.AssertNotSame(manager);
	}

	[TestMethod]
	public void PositionManager_Clone_PreservesByOrders()
	{
		var manager = new PositionManager(true, new PositionManagerState());

		var clone = (PositionManager)manager.Clone();

		clone.ByOrders.AssertTrue();
	}

	[TestMethod]
	public void PositionManager_Clone_StateIsIndependent()
	{
		var manager = new PositionManager(false, new PositionManagerState());

		// Add a trade to original
		manager.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = false,
			SecurityId = _secId,
			PortfolioName = "test",
			TradePrice = 100,
			TradeVolume = 5,
			Side = Sides.Buy,
			ServerTime = DateTime.UtcNow,
		});

		var clone = manager.Clone();

		// Clone should start from zero position
		var result = clone.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = false,
			SecurityId = _secId,
			PortfolioName = "test",
			TradePrice = 100,
			TradeVolume = 3,
			Side = Sides.Buy,
			ServerTime = DateTime.UtcNow,
		});

		// Position should be 3 (not 5+3=8)
		result.AssertNotNull();
		var pos = result.Changes[PositionChangeTypes.CurrentValue];
		AreEqual(3m, pos);
	}

	[TestMethod]
	public void PositionManager_Clone_StateIsFunctional()
	{
		var manager = new PositionManager(false, new PositionManagerState());
		var clone = manager.Clone();

		var result = clone.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = false,
			SecurityId = _secId,
			PortfolioName = "test",
			TradePrice = 100,
			TradeVolume = 10,
			Side = Sides.Buy,
			ServerTime = DateTime.UtcNow,
		});

		result.AssertNotNull("Clone state should track positions");
		var pos = result.Changes[PositionChangeTypes.CurrentValue];
		AreEqual(10m, pos);
	}

	#endregion

	#region OrderBookIncrementManager

	[TestMethod]
	public void OrderBookIncrementManager_Clone_CreatesNewInstance()
	{
		var manager = new OrderBookIncrementManager(new TestReceiver(), new OrderBookIncrementManagerState());

		var clone = manager.Clone();

		clone.AssertNotNull();
		clone.AssertNotSame(manager);
	}

	[TestMethod]
	public void OrderBookIncrementManager_Clone_StateIsFunctional()
	{
		var manager = new OrderBookIncrementManager(new TestReceiver(), new OrderBookIncrementManagerState());
		var clone = manager.Clone();

		// Subscribe on clone
		var mdMsg = new MarketDataMessage
		{
			TransactionId = 100,
			SecurityId = _secId,
			DataType2 = DataType.MarketDepth,
			IsSubscribe = true,
		};

		var (toInner, toOut) = clone.ProcessInMessage(mdMsg);
		toInner.Length.AssertEqual(1);
		toOut.Length.AssertEqual(0);

		var snapshot = new QuoteChangeMessage
		{
			SecurityId = _secId,
			ServerTime = DateTime.UtcNow,
			State = QuoteChangeStates.SnapshotComplete,
			Bids = [new QuoteChange(100m, 10m)],
			Asks = [new QuoteChange(101m, 20m)],
		};
		snapshot.SetSubscriptionIds([100]);

		var (forward, extraOut) = clone.ProcessOutMessage(snapshot);
		forward.AssertNull();
		extraOut.Length.AssertEqual(1);
		extraOut[0].To<QuoteChangeMessage>().Bids.Single().Price.AssertEqual(100m);
	}

	[TestMethod]
	public void OrderBookIncrementManager_Clone_StateIsIndependent()
	{
		var manager = new OrderBookIncrementManager(new TestReceiver(), new OrderBookIncrementManagerState());

		// Subscribe on original
		manager.ProcessInMessage(new MarketDataMessage
		{
			TransactionId = 1,
			SecurityId = _secId,
			DataType2 = DataType.MarketDepth,
			IsSubscribe = true,
		});

		var clone = manager.Clone();

		// Clone state should not have subscription id=1
		// Reset on clone should not affect original's state
		clone.ProcessInMessage(new ResetMessage());

		var snapshot = new QuoteChangeMessage
		{
			SecurityId = _secId,
			ServerTime = DateTime.UtcNow,
			State = QuoteChangeStates.SnapshotComplete,
			Bids = [new QuoteChange(100m, 10m)],
			Asks = [new QuoteChange(101m, 20m)],
		};
		snapshot.SetSubscriptionIds([1]);

		var (forward, extraOut) = manager.ProcessOutMessage(snapshot);
		forward.AssertNull("Original state should be unaffected by clone reset");
		extraOut.Length.AssertEqual(1);
	}

	#endregion

	#region OrderBookTruncateManager

	[TestMethod]
	public void OrderBookTruncateManager_Clone_CreatesNewInstance()
	{
		var manager = new OrderBookTruncateManager(new TestReceiver(), _ => 10, new OrderBookTruncateManagerState());

		var clone = manager.Clone();

		clone.AssertNotNull();
		clone.AssertNotSame(manager);
	}

	[TestMethod]
	public void OrderBookTruncateManager_Clone_StateIsFunctional()
	{
		var manager = new OrderBookTruncateManager(new TestReceiver(), _ => 10, new OrderBookTruncateManagerState());
		var clone = manager.Clone();

		var mdMsg = new MarketDataMessage
		{
			TransactionId = 100,
			SecurityId = _secId,
			DataType2 = DataType.MarketDepth,
			IsSubscribe = true,
			MaxDepth = 1,
		};

		var (toInner, toOut) = clone.ProcessInMessage(mdMsg);
		toInner.To<MarketDataMessage>().MaxDepth.AssertEqual(10);
		toOut.Length.AssertEqual(0);

		var depth = new QuoteChangeMessage
		{
			SecurityId = _secId,
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(100m, 1m), new QuoteChange(99m, 1m)],
			Asks = [new QuoteChange(101m, 1m), new QuoteChange(102m, 1m)],
		};
		depth.SetSubscriptionIds([100]);

		var (forward, extraOut) = clone.ProcessOutMessage(depth);
		forward.AssertNull();
		extraOut.Length.AssertEqual(1);
		extraOut[0].To<QuoteChangeMessage>().Bids.Length.AssertEqual(1);
		extraOut[0].To<QuoteChangeMessage>().Asks.Length.AssertEqual(1);
	}

	#endregion

	#region Level1DepthBuilderManager

	[TestMethod]
	public void Level1DepthBuilderManager_Clone_CreatesNewInstance()
	{
		var manager = new Level1DepthBuilderManager(new TestReceiver(), new Level1DepthBuilderManagerState());

		var clone = manager.Clone();

		clone.AssertNotNull();
		clone.AssertNotSame(manager);
	}

	[TestMethod]
	public void Level1DepthBuilderManager_Clone_StateIsFunctional()
	{
		var manager = new Level1DepthBuilderManager(new TestReceiver(), new Level1DepthBuilderManagerState());
		var clone = manager.Clone();

		var mdMsg = new MarketDataMessage
		{
			TransactionId = 100,
			SecurityId = _secId,
			DataType2 = DataType.MarketDepth,
			IsSubscribe = true,
		};

		var (toInner, _) = clone.ProcessInMessage(mdMsg);

		toInner.Length.AssertEqual(1);
		toInner[0].To<MarketDataMessage>().DataType2.AssertEqual(DataType.Level1);
	}

	#endregion

	#region CommissionManager & PnLManager (no injectable state)

	[TestMethod]
	public void CommissionManager_Clone_CreatesNewInstance()
	{
		var manager = new CommissionManager();

		var clone = manager.Clone();

		clone.AssertNotNull();
		clone.AssertNotSame(manager);
	}

	[TestMethod]
	public void PnLManager_Clone_CreatesNewInstance()
	{
		var manager = new PnLManager();

		var clone = manager.Clone();

		clone.AssertNotNull();
		clone.AssertNotSame(manager);
	}

	#endregion

	#region Clone preserves custom state type

	[TestMethod]
	public void LatencyManager_Clone_PreservesStateType()
	{
		TrackingLatencyManagerState.InstanceCount = 0;

		var manager = new LatencyManager(new TrackingLatencyManagerState());
		AreEqual(1, TrackingLatencyManagerState.InstanceCount);

		var clone = manager.Clone();
		AreEqual(2, TrackingLatencyManagerState.InstanceCount, "Clone should create TrackingLatencyManagerState, not default LatencyManagerState");

		// verify clone state is functional
		var t0 = DateTime.UtcNow;
		clone.ProcessMessage(new OrderRegisterMessage { TransactionId = 1, LocalTime = t0 });
		var latency = clone.ProcessMessage(new ExecutionMessage
		{
			OriginalTransactionId = 1,
			LocalTime = t0 + TimeSpan.FromMilliseconds(25),
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
		});
		latency.AssertNotNull();
	}

	[TestMethod]
	public void SlippageManager_Clone_PreservesStateType()
	{
		TrackingSlippageManagerState.InstanceCount = 0;

		var manager = new SlippageManager(new TrackingSlippageManagerState());
		AreEqual(1, TrackingSlippageManagerState.InstanceCount);

		var clone = manager.Clone();
		AreEqual(2, TrackingSlippageManagerState.InstanceCount, "Clone should create TrackingSlippageManagerState, not default SlippageManagerState");

		// verify clone state is functional — Reset calls _state.Clear()
		clone.Reset();
		clone.Slippage.AssertEqual(0);
	}

	[TestMethod]
	public void PositionManager_Clone_PreservesStateType()
	{
		TrackingPositionManagerState.InstanceCount = 0;

		var manager = new PositionManager(false, new TrackingPositionManagerState());
		AreEqual(1, TrackingPositionManagerState.InstanceCount);

		var clone = manager.Clone();
		AreEqual(2, TrackingPositionManagerState.InstanceCount, "Clone should create TrackingPositionManagerState, not default PositionManagerState");

		// verify clone state is functional
		var result = clone.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = false,
			SecurityId = _secId,
			PortfolioName = "test",
			TradePrice = 100,
			TradeVolume = 5,
			Side = Sides.Buy,
			ServerTime = DateTime.UtcNow,
		});
		result.AssertNotNull();
	}

	[TestMethod]
	public void OrderBookIncrementManager_Clone_PreservesStateType()
	{
		TrackingOrderBookIncrementManagerState.InstanceCount = 0;

		var manager = new OrderBookIncrementManager(new TestReceiver(), new TrackingOrderBookIncrementManagerState());
		AreEqual(1, TrackingOrderBookIncrementManagerState.InstanceCount);

		var clone = manager.Clone();
		AreEqual(2, TrackingOrderBookIncrementManagerState.InstanceCount, "Clone should create TrackingOrderBookIncrementManagerState, not default");

		// verify clone state is functional
		clone.ProcessInMessage(new MarketDataMessage
		{
			TransactionId = 1,
			SecurityId = _secId,
			DataType2 = DataType.MarketDepth,
			IsSubscribe = true,
		});
	}

	[TestMethod]
	public void OrderBookTruncateManager_Clone_PreservesStateType()
	{
		TrackingOrderBookTruncateManagerState.InstanceCount = 0;

		var manager = new OrderBookTruncateManager(new TestReceiver(), _ => null, new TrackingOrderBookTruncateManagerState());
		AreEqual(1, TrackingOrderBookTruncateManagerState.InstanceCount);

		var clone = manager.Clone();
		AreEqual(2, TrackingOrderBookTruncateManagerState.InstanceCount, "Clone should create TrackingOrderBookTruncateManagerState, not default");

		// verify clone state is functional
		clone.ProcessInMessage(new MarketDataMessage
		{
			TransactionId = 1,
			SecurityId = _secId,
			DataType2 = DataType.MarketDepth,
			IsSubscribe = true,
			MaxDepth = 10,
		});
	}

	[TestMethod]
	public void Level1DepthBuilderManager_Clone_PreservesStateType()
	{
		TrackingLevel1DepthBuilderManagerState.InstanceCount = 0;

		var manager = new Level1DepthBuilderManager(new TestReceiver(), new TrackingLevel1DepthBuilderManagerState());
		AreEqual(1, TrackingLevel1DepthBuilderManagerState.InstanceCount);

		var clone = manager.Clone();
		AreEqual(2, TrackingLevel1DepthBuilderManagerState.InstanceCount, "Clone should create TrackingLevel1DepthBuilderManagerState, not default");

		// verify clone state is functional
		clone.ProcessInMessage(new MarketDataMessage
		{
			TransactionId = 1,
			SecurityId = _secId,
			DataType2 = DataType.MarketDepth,
			IsSubscribe = true,
		});
	}

	#endregion

}

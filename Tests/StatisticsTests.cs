namespace StockSharp.Tests;

using StockSharp.Algo.PnL;
using StockSharp.Algo.Statistics;

[TestClass]
public class StatisticsTests
{
	[TestMethod]
	public void NetProfit()
	{
		// Arrange
		var parameter = new NetProfitParameter();
		var marketTime = DateTimeOffset.UtcNow;
		var pnl = 1000m;

		// Act
		parameter.Add(marketTime, pnl, null);

		// Assert
		parameter.Value.AssertEqual(pnl);
	}

	[TestMethod]
	public void NetProfitPercent()
	{
		// Arrange
		var parameter = new NetProfitPercentParameter();
		var beginValue = 500m;
		parameter.BeginValue = beginValue;

		var marketTime = DateTimeOffset.UtcNow;
		var pnl = 600m;
		var expectedPercent = (pnl * 100m) / beginValue; // 120%

		// Act
		parameter.Add(marketTime, pnl, null);

		// Assert
		parameter.Value.AssertEqual(expectedPercent);
	}

	[TestMethod]
	public void NetProfitPercentZero()
	{
		// Arrange
		var parameter = new NetProfitPercentParameter();
		parameter.BeginValue = 0m;

		var marketTime = DateTimeOffset.UtcNow;
		var pnl = 600m;

		// Act
		parameter.Add(marketTime, pnl, null);

		// Assert
		parameter.Value.AssertEqual(0m);
	}

	[TestMethod]
	public void MaxProfit()
	{
		// Arrange
		var parameter = new MaxProfitParameter();
		var marketTime = DateTimeOffset.UtcNow;

		// Act & Assert
		parameter.Add(marketTime, 100m, null);
		parameter.Value.AssertEqual(100m);

		parameter.Add(marketTime, 500m, null);
		parameter.Value.AssertEqual(500m);

		parameter.Add(marketTime, 200m, null);
		parameter.Value.AssertEqual(500m); // Value should remain the highest
	}

	[TestMethod]
	public void MaxProfitDate()
	{
		// Arrange
		var maxProfitParam = new MaxProfitParameter();
		var parameter = new MaxProfitDateParameter(maxProfitParam);

		var time1 = new DateTimeOffset(2023, 1, 1, 12, 0, 0, TimeSpan.Zero);
		var time2 = new DateTimeOffset(2023, 1, 2, 12, 0, 0, TimeSpan.Zero);
		var time3 = new DateTimeOffset(2023, 1, 3, 12, 0, 0, TimeSpan.Zero);

		// Act & Assert
		// First update sets the max profit and the date
		maxProfitParam.Add(time1, 100m, null);
		parameter.Add(time1, 100m, null);
		parameter.Value.AssertEqual(time1);

		// Larger profit updates both max profit and date
		maxProfitParam.Add(time2, 200m, null);
		parameter.Add(time2, 200m, null);
		parameter.Value.AssertEqual(time2);

		// Lower profit doesn't change the value
		maxProfitParam.Add(time3, 150m, null);
		parameter.Add(time3, 150m, null);
		parameter.Value.AssertEqual(time2); // Still time2
	}

	[TestMethod]
	public void MaxDrawdown()
	{
		// Arrange
		var parameter = new MaxDrawdownParameter();
		var marketTime = DateTimeOffset.UtcNow;

		// Act & Assert
		// Start with profit
		parameter.Add(marketTime, 1000m, null);
		parameter.Value.AssertEqual(0m); // No drawdown yet

		// Drop to 800 creates 200 drawdown
		parameter.Add(marketTime, 800m, null);
		parameter.Value.AssertEqual(200m);

		// Further drop increases drawdown
		parameter.Add(marketTime, 600m, null);
		parameter.Value.AssertEqual(400m);

		// Recovery doesn't change max drawdown
		parameter.Add(marketTime, 900m, null);
		parameter.Value.AssertEqual(400m);

		// New high doesn't change max drawdown
		parameter.Add(marketTime, 1200m, null);
		parameter.Value.AssertEqual(400m);

		// New deeper drawdown updates max drawdown
		parameter.Add(marketTime, 700m, null);
		parameter.Value.AssertEqual(500m);
	}

	[TestMethod]
	public void MaxDrawdownPercent()
	{
		// Arrange
		var maxDrawdownParam = new MaxDrawdownParameter();
		var parameter = new MaxDrawdownPercentParameter(maxDrawdownParam);
		var marketTime = DateTimeOffset.UtcNow;

		// Act & Assert
		// Create a 200 drawdown (20%)
		maxDrawdownParam.Add(marketTime, 1000m, null);
		maxDrawdownParam.Add(marketTime, 800m, null);
		parameter.Add(marketTime, 800m, null);
		parameter.Value.AssertEqual(20m); // 20% drawdown

		// Create a 400 drawdown (40%)
		maxDrawdownParam.Add(marketTime, 600m, null);
		parameter.Add(marketTime, 600m, null);
		parameter.Value.AssertEqual(40m); // 40% drawdown
	}

	[TestMethod]
	public void RecoveryFactor()
	{
		// Arrange
		var maxDrawdownParam = new MaxDrawdownParameter();
		var netProfitParam = new NetProfitParameter();
		var parameter = new RecoveryFactorParameter(maxDrawdownParam, netProfitParam);

		var marketTime = DateTimeOffset.UtcNow;

		// Act
		// Create a drawdown and final profit
		maxDrawdownParam.Add(marketTime, 1000m, null);
		maxDrawdownParam.Add(marketTime, 600m, null); // 400 drawdown
		netProfitParam.Add(marketTime, 800m, null); // 800 final profit
		parameter.Add(marketTime, 800m, null);

		// Assert
		// Recovery factor = Net profit / Max drawdown = 800 / 400 = 2
		parameter.Value.AssertEqual(2m);
	}

	[TestMethod]
	public void Commission()
	{
		// Arrange
		var parameter = new CommissionParameter();
		var marketTime = DateTimeOffset.UtcNow;

		// Act
		parameter.Add(marketTime, 1000m, 25m);

		// Assert
		parameter.Value.AssertEqual(25m);

		// Act - update to new commission
		parameter.Add(marketTime, 1200m, 30m);

		// Assert - should track latest commission
		parameter.Value.AssertEqual(55m);
	}

	[TestMethod]
	public void MaxRelativeDrawdown()
	{
		// Arrange
		var parameter = new MaxRelativeDrawdownParameter();
		var marketTime = DateTimeOffset.UtcNow;

		// Act & Assert
		// Start with profit
		parameter.Add(marketTime, 1000m, null);
		parameter.Value.AssertEqual(0m); // No drawdown yet

		// 20% drawdown (200/1000)
		parameter.Add(marketTime, 800m, null);
		parameter.Value.AssertEqual(0.2m);

		// 40% drawdown (400/1000)
		parameter.Add(marketTime, 600m, null);
		parameter.Value.AssertEqual(0.4m);

		// Partial recovery doesn't change max relative drawdown
		parameter.Add(marketTime, 700m, null);
		parameter.Value.AssertEqual(0.4m);

		// New high resets peak equity
		parameter.Add(marketTime, 1200m, null);
		parameter.Value.AssertEqual(0.4m);

		// 50% drawdown from new high (600/1200)
		parameter.Add(marketTime, 600m, null);
		parameter.Value.AssertEqual(0.5m);
	}

	[TestMethod]
	public void Return()
	{
		// Arrange
		var parameter = new ReturnParameter();
		var marketTime = DateTimeOffset.UtcNow;

		// Act & Assert
		// Start with value
		parameter.Add(marketTime, 1000m, null);
		parameter.Value.AssertEqual(0m); // No return yet

		// Drop creates minimum
		parameter.Add(marketTime, 500m, null);
		parameter.Value.AssertEqual(0m); // Still no return calculated

		// Rise from minimum creates return
		parameter.Add(marketTime, 750m, null);
		parameter.Value.AssertEqual(0.5m); // (750-500)/500 = 0.5

		// Further rise increases return
		parameter.Add(marketTime, 1000m, null);
		parameter.Value.AssertEqual(1m); // (1000-500)/500 = 1

		// New low changes the base for calculation
		parameter.Add(marketTime, 400m, null);
		parameter.Value.AssertEqual(1m); // Keep max return seen so far

		// New high from new low creates larger return
		parameter.Add(marketTime, 1200m, null);
		parameter.Value.AssertEqual(2m); // (1200-400)/400 = 2
	}

	[TestMethod]
	public void Return_WithNegativePnL()
	{
		// Arrange
		var parameter = new ReturnParameter();
		var marketTime = DateTimeOffset.UtcNow;

		// Act & Assert
		// Start with loss
		parameter.Add(marketTime, -1000m, null);
		parameter.Value.AssertEqual(0m); // No return yet

		// Deeper loss creates new minimum
		parameter.Add(marketTime, -1500m, null);
		parameter.Value.AssertEqual(0m); // Still no return calculated

		// Recovery from minimum should show positive return
		parameter.Add(marketTime, -1000m, null);
		// Expected: (-1000 - (-1500)) / abs(-1500) = 500 / 1500 = 0.333...
		parameter.Value.Round(3).AssertEqual(0.333m);

		// Full recovery to break-even
		parameter.Add(marketTime, 0m, null);
		// Expected: (0 - (-1500)) / abs(-1500) = 1500 / 1500 = 1.0
		parameter.Value.AssertEqual(1.0m);

		// Profit after recovery
		parameter.Add(marketTime, 500m, null);
		// Expected: (500 - (-1500)) / abs(-1500) = 2000 / 1500 = 1.333...
		parameter.Value.Round(3).AssertEqual(1.333m);
	}

	[TestMethod]
	public void MaxLatencyRegistration()
	{
		// Arrange
		var parameter = new MaxLatencyRegistrationParameter();
		var order = new Order
		{
			LatencyRegistration = TimeSpan.FromMilliseconds(100)
		};

		// Act
		parameter.New(order);

		// Assert
		parameter.Value.AssertEqual(TimeSpan.FromMilliseconds(100));

		// Act - order with higher latency
		var order2 = new Order
		{
			LatencyRegistration = TimeSpan.FromMilliseconds(200)
		};
		parameter.New(order2);

		// Assert - should update to higher value
		parameter.Value.AssertEqual(TimeSpan.FromMilliseconds(200));

		// Act - order with lower latency
		var order3 = new Order
		{
			LatencyRegistration = TimeSpan.FromMilliseconds(50)
		};
		parameter.New(order3);

		// Assert - should remain at highest value
		parameter.Value.AssertEqual(TimeSpan.FromMilliseconds(200));
	}

	[TestMethod]
	public void MaxLatencyRegistrationNullLatency()
	{
		// Arrange
		var parameter = new MaxLatencyRegistrationParameter();
		var order = new Order
		{
			LatencyRegistration = TimeSpan.FromMilliseconds(100)
		};

		parameter.New(order);
		parameter.Value.AssertEqual(TimeSpan.FromMilliseconds(100));

		// Act - order with null latency
		var order2 = new Order
		{
			LatencyRegistration = null
		};
		parameter.New(order2);

		// Assert - value should not change
		parameter.Value.AssertEqual(TimeSpan.FromMilliseconds(100));
	}

	[TestMethod]
	public void MinLatencyRegistration()
	{
		// Arrange
		var parameter = new MinLatencyRegistrationParameter();
		var order = new Order
		{
			LatencyRegistration = TimeSpan.FromMilliseconds(100)
		};

		// Act
		parameter.New(order);

		// Assert
		parameter.Value.AssertEqual(TimeSpan.FromMilliseconds(100));

		// Act - order with lower latency
		var order2 = new Order
		{
			LatencyRegistration = TimeSpan.FromMilliseconds(50)
		};
		parameter.New(order2);

		// Assert - should update to lower value
		parameter.Value.AssertEqual(TimeSpan.FromMilliseconds(50));

		// Act - order with higher latency
		var order3 = new Order
		{
			LatencyRegistration = TimeSpan.FromMilliseconds(150)
		};
		parameter.New(order3);

		// Assert - should remain at lowest value
		parameter.Value.AssertEqual(TimeSpan.FromMilliseconds(50));
	}

	[TestMethod]
	public void MaxLatencyCancellation()
	{
		// Arrange
		var parameter = new MaxLatencyCancellationParameter();
		var order = new Order
		{
			LatencyCancellation = TimeSpan.FromMilliseconds(100)
		};

		// Act
		parameter.Changed(order);

		// Assert
		parameter.Value.AssertEqual(TimeSpan.FromMilliseconds(100));

		// Act - order with higher latency
		var order2 = new Order
		{
			LatencyCancellation = TimeSpan.FromMilliseconds(200)
		};
		parameter.Changed(order2);

		// Assert - should update to higher value
		parameter.Value.AssertEqual(TimeSpan.FromMilliseconds(200));
	}

	[TestMethod]
	public void MinLatencyCancellation()
	{
		// Arrange
		var parameter = new MinLatencyCancellationParameter();
		var order = new Order
		{
			LatencyCancellation = TimeSpan.FromMilliseconds(100)
		};

		// Act
		parameter.Changed(order);

		// Assert
		parameter.Value.AssertEqual(TimeSpan.FromMilliseconds(100));

		// Act - order with lower latency
		var order2 = new Order
		{
			LatencyCancellation = TimeSpan.FromMilliseconds(50)
		};
		parameter.Changed(order2);

		// Assert - should update to lower value
		parameter.Value.AssertEqual(TimeSpan.FromMilliseconds(50));
	}

	[TestMethod]
	public void OrderCount()
	{
		// Arrange
		var parameter = new OrderCountParameter();

		// Act & Assert
		parameter.Value.AssertEqual(0);

		parameter.New(new Order());
		parameter.Value.AssertEqual(1);

		parameter.New(new Order());
		parameter.Value.AssertEqual(2);

		parameter.New(new Order());
		parameter.Value.AssertEqual(3);
	}

	[TestMethod]
	public void OrderErrorCount()
	{
		// Arrange
		var parameter = new OrderRegisterErrorCountParameter();

		// Act & Assert
		parameter.Value.AssertEqual(0);

		parameter.RegisterFailed(new OrderFail { Error = new Exception("Test error") });
		parameter.Value.AssertEqual(1);

		parameter.RegisterFailed(new OrderFail { Error = new Exception("Another error") });
		parameter.Value.AssertEqual(2);
	}

	[TestMethod]
	public void OrderCancelErrorCount()
	{
		// Arrange
		var parameter = new OrderCancelErrorCountParameter();

		// Act & Assert
		parameter.Value.AssertEqual(0);

		parameter.CancelFailed(new OrderFail { Error = new Exception("Cancel error") });
		parameter.Value.AssertEqual(1);

		parameter.CancelFailed(new OrderFail { Error = new Exception("Another cancel error") });
		parameter.Value.AssertEqual(2);
	}

	[TestMethod]
	public void OrderInsufficientFundError()
	{
		// Arrange
		var parameter = new OrderInsufficientFundErrorCountParameter();

		// Act & Assert
		parameter.Value.AssertEqual(0);

		// Order fail with InsufficientFundException
		parameter.RegisterFailed(new OrderFail { Error = new InsufficientFundException("Not enough funds") });
		parameter.Value.AssertEqual(1);

		// Another OrderFail with InsufficientFundException
		parameter.RegisterFailed(new OrderFail { Error = new InsufficientFundException("Insufficient funds") });
		parameter.Value.AssertEqual(2);

		// OrderFail with different exception type should not increment
		parameter.RegisterFailed(new OrderFail { Error = new Exception("Generic error") });
		parameter.Value.AssertEqual(2); // Still 2
	}

	[TestMethod]
	public void BaseOrderStatistic()
	{
		// Arrange
		var parameter = new OrderCountParameter();
		var order = new Order();
		var fail = new OrderFail { Error = new Exception("Test error") };

		// Act & Assert - these methods have empty implementations but shouldn't throw
		parameter.Changed(order); // Empty implementation
		parameter.RegisterFailed(fail); // Empty implementation in base class
		parameter.CancelFailed(fail); // Empty implementation in base class
	}

	[TestMethod]
	public void Order()
	{
		// Arrange
		var orderCount = new OrderCountParameter();
		var orderErrorCount = new OrderRegisterErrorCountParameter();
		var cancelErrorCount = new OrderCancelErrorCountParameter();
		var maxLatency = new MaxLatencyRegistrationParameter();

		// Add some values
		orderCount.New(new Order());
		orderErrorCount.RegisterFailed(new OrderFail { Error = new Exception() });
		cancelErrorCount.CancelFailed(new OrderFail { Error = new Exception() });
		maxLatency.New(new Order { LatencyRegistration = TimeSpan.FromMilliseconds(100) });

		// Act
		orderCount.Reset();
		orderErrorCount.Reset();
		cancelErrorCount.Reset();
		maxLatency.Reset();

		// Assert
		orderCount.Value.AssertEqual(0);
		orderErrorCount.Value.AssertEqual(0);
		cancelErrorCount.Value.AssertEqual(0);
		maxLatency.Value.AssertEqual(default);
	}

	[TestMethod]
	public void MaxLongPosition()
	{
		// Arrange
		var parameter = new MaxLongPositionParameter();
		var marketTime = DateTimeOffset.UtcNow;

		// Act & Assert
		// Initial zero position
		parameter.Add(marketTime, 0);
		parameter.Value.AssertEqual(0);

		// Long position - should update
		parameter.Add(marketTime, 100);
		parameter.Value.AssertEqual(100);

		// Larger long position - should update
		parameter.Add(marketTime, 200);
		parameter.Value.AssertEqual(200);

		// Smaller long position - should not update
		parameter.Add(marketTime, 150);
		parameter.Value.AssertEqual(200);

		// Short position - should not update
		parameter.Add(marketTime, -50);
		parameter.Value.AssertEqual(200);
	}

	[TestMethod]
	public void MaxLongPositionIgnoresNegative()
	{
		// Arrange
		var parameter = new MaxLongPositionParameter();
		var marketTime = DateTimeOffset.UtcNow;

		// Act & Assert
		// Initial short position - should not update from default 0
		parameter.Add(marketTime, -100);
		parameter.Value.AssertEqual(0);

		// Another short position - should not update
		parameter.Add(marketTime, -200);
		parameter.Value.AssertEqual(0);

		// Long position - should update
		parameter.Add(marketTime, 50);
		parameter.Value.AssertEqual(50);
	}

	[TestMethod]
	public void MaxShortPosition()
	{
		// Arrange
		var parameter = new MaxShortPositionParameter();
		var marketTime = DateTimeOffset.UtcNow;

		// Act & Assert
		// Initial zero position
		parameter.Add(marketTime, 0);
		parameter.Value.AssertEqual(0);

		// Short position - should update (absolute value of position is tracked)
		parameter.Add(marketTime, -100);
		parameter.Value.AssertEqual(-100);

		// Larger short position - should update
		parameter.Add(marketTime, -200);
		parameter.Value.AssertEqual(-200);

		// Smaller short position - should not update
		parameter.Add(marketTime, -150);
		parameter.Value.AssertEqual(-200);

		// Long position - should not update
		parameter.Add(marketTime, 50);
		parameter.Value.AssertEqual(-200);
	}

	[TestMethod]
	public void MaxShortPositionIgnoresPositive()
	{
		// Arrange
		var parameter = new MaxShortPositionParameter();
		var marketTime = DateTimeOffset.UtcNow;

		// Act & Assert
		// Initial long position - should not update from default 0
		parameter.Add(marketTime, 100);
		parameter.Value.AssertEqual(0);

		// Another long position - should not update
		parameter.Add(marketTime, 200);
		parameter.Value.AssertEqual(0);

		// Short position - should update
		parameter.Add(marketTime, -50);
		parameter.Value.AssertEqual(-50);
	}

	[TestMethod]
	public void Position()
	{
		// Arrange
		var maxLong = new MaxLongPositionParameter();
		var maxShort = new MaxShortPositionParameter();
		var marketTime = DateTimeOffset.UtcNow;

		// Add some values
		maxLong.Add(marketTime, 100);
		maxShort.Add(marketTime, -200);

		// Act
		maxLong.Reset();
		maxShort.Reset();

		// Assert
		maxLong.Value.AssertEqual(0);
		maxShort.Value.AssertEqual(0);
	}

	[TestMethod]
	public void PositionHandlesBoth()
	{
		// Arrange
		var maxLong = new MaxLongPositionParameter();
		var maxShort = new MaxShortPositionParameter();
		var marketTime = DateTimeOffset.UtcNow;

		// Act - add mixed positions
		maxLong.Add(marketTime, 50);
		maxLong.Add(marketTime, -30);
		maxLong.Add(marketTime, 100);
		maxLong.Add(marketTime, -50);

		maxShort.Add(marketTime, 30);
		maxShort.Add(marketTime, -70);
		maxShort.Add(marketTime, 80);
		maxShort.Add(marketTime, -120);

		// Assert
		maxLong.Value.AssertEqual(100);  // Maximum long position
		maxShort.Value.AssertEqual(-120); // Maximum short position
	}

	[TestMethod]
	public void PositionHandlesZeroValue()
	{
		// Arrange
		var maxLong = new MaxLongPositionParameter();
		var maxShort = new MaxShortPositionParameter();
		var marketTime = DateTimeOffset.UtcNow;

		// Act & Assert - starting with zero
		maxLong.Add(marketTime, 0);
		maxShort.Add(marketTime, 0);
		maxLong.Value.AssertEqual(0);
		maxShort.Value.AssertEqual(0);

		// Add some non-zero positions
		maxLong.Add(marketTime, 50);
		maxShort.Add(marketTime, -70);
		maxLong.Value.AssertEqual(50);
		maxShort.Value.AssertEqual(-70);

		// Back to zero - shouldn't change values
		maxLong.Add(marketTime, 0);
		maxShort.Add(marketTime, 0);
		maxLong.Value.AssertEqual(50);
		maxShort.Value.AssertEqual(-70);
	}

	[TestMethod]
	public void WinningTrades()
	{
		// Arrange
		var parameter = new WinningTradesParameter();
		var serverTime = DateTimeOffset.UtcNow;

		// Act & Assert
		// Profitable trade
		parameter.Add(new(serverTime, 10, 100));
		parameter.Value.AssertEqual(1);

		// Another profitable trade
		parameter.Add(new(serverTime, 5, 50));
		parameter.Value.AssertEqual(2);

		// Losing trade - should not increment
		parameter.Add(new(serverTime, 15, -30));
		parameter.Value.AssertEqual(2);

		// Break-even trade - should not increment
		parameter.Add(new(serverTime, 8, 0));
		parameter.Value.AssertEqual(2);

		// No closed volume - should not increment even if PnL > 0
		parameter.Add(new(serverTime, 0, 200));
		parameter.Value.AssertEqual(2);
	}

	[TestMethod]
	public void LossingTrades()
	{
		// Arrange
		var parameter = new LossingTradesParameter();
		var serverTime = DateTimeOffset.UtcNow;

		// Act & Assert
		// Losing trade with closed volume
		parameter.Add(new(serverTime, 10, -50));
		parameter.Value.AssertEqual(1);

		// Another losing trade with closed volume
		parameter.Add(new(serverTime, 5, -30));
		parameter.Value.AssertEqual(2);

		// Break-even trade with closed volume - should NOT increment (neutral)
		parameter.Add(new(serverTime, 15, 0));
		parameter.Value.AssertEqual(2);

		// Profitable trade - should not increment
		parameter.Add(new(serverTime, 20, 100));
		parameter.Value.AssertEqual(2);

		// Losing trade with no closed volume - should not increment
		parameter.Add(new(serverTime, 0, -40));
		parameter.Value.AssertEqual(2);
	}

	[TestMethod]
	public void TradeCount()
	{
		// Arrange
		var parameter = new TradeCountParameter();
		var serverTime = DateTimeOffset.UtcNow;

		// Act & Assert
		// Profitable trade
		parameter.Add(new(serverTime, 10, 100));
		parameter.Value.AssertEqual(1);

		// Losing trade
		parameter.Add(new(serverTime, 5, -50));
		parameter.Value.AssertEqual(2);

		// Break-even trade
		parameter.Add(new(serverTime, 8, 0));
		parameter.Value.AssertEqual(3);

		// No closed volume - should NOT increment
		parameter.Add(new(serverTime, 0, 999));
		parameter.Value.AssertEqual(3);
	}

	[TestMethod]
	public void RoundtripCount()
	{
		// Arrange
		var parameter = new RoundtripCountParameter();
		var serverTime = DateTimeOffset.UtcNow;

		// Act & Assert
		// Trade with closed volume
		parameter.Add(new(serverTime, 10, 100));
		parameter.Value.AssertEqual(1);

		// Another trade with closed volume
		parameter.Add(new(serverTime, 5, -20));
		parameter.Value.AssertEqual(2);

		// Trade with no closed volume - should not increment
		parameter.Add(new(serverTime, 0, 50));
		parameter.Value.AssertEqual(2);
	}

	[TestMethod]
	public void AverageTradeProfit()
	{
		// Arrange
		var parameter = new AverageTradeProfitParameter();
		var serverTime = DateTimeOffset.UtcNow;

		// Act & Assert
		// First trade
		parameter.Add(new(serverTime, 10, 100));
		parameter.Value.AssertEqual(100m);

		// Second trade - average should be (100 + 50) / 2 = 75
		parameter.Add(new(serverTime, 5, 50));
		parameter.Value.AssertEqual(75m);

		// Third trade - average should be (100 + 50 - 30) / 3 = 40
		parameter.Add(new(serverTime, 15, -30));
		parameter.Value.AssertEqual(40m);

		// Trade with no closed volume - should not affect average
		parameter.Add(new(serverTime, 0, 200));
		parameter.Value.AssertEqual(40m);
	}

	[TestMethod]
	public void AverageWinTrade()
	{
		// Arrange
		var parameter = new AverageWinTradeParameter();
		var serverTime = DateTimeOffset.UtcNow;

		// Act & Assert
		// First winning trade
		parameter.Add(new(serverTime, 10, 100));
		parameter.Value.AssertEqual(100m);

		// Second winning trade - average should be (100 + 80) / 2 = 90
		parameter.Add(new(serverTime, 5, 80));
		parameter.Value.AssertEqual(90m);

		// Losing trade - should not affect winning average
		parameter.Add(new(serverTime, 15, -50));
		parameter.Value.AssertEqual(90m);

		// Break-even trade - should not affect winning average
		parameter.Add(new(serverTime, 20, 0));
		parameter.Value.AssertEqual(90m);

		// Third winning trade - average should be (100 + 80 + 60) / 3 = 80
		parameter.Add(new(serverTime, 8, 60));
		parameter.Value.AssertEqual(80m);

		// Trade with no closed volume - should not affect average
		parameter.Add(new(serverTime, 0, 200));
		parameter.Value.AssertEqual(80m);
	}

	[TestMethod]
	public void AverageLossTrade()
	{
		// Arrange
		var parameter = new AverageLossTradeParameter();
		var serverTime = DateTimeOffset.UtcNow;

		// Act & Assert
		// First losing trade
		parameter.Add(new(serverTime, 10, -50));
		parameter.Value.AssertEqual(-50m);

		// Second losing trade - average should be (-50 + -30) / 2 = -40
		parameter.Add(new(serverTime, 5, -30));
		parameter.Value.AssertEqual(-40m);

		// Break-even trade - should be ignored (neutral)
		parameter.Add(new(serverTime, 15, 0));
		parameter.Value.AssertEqual(-40m);

		// Winning trade - should not affect losing average
		parameter.Add(new(serverTime, 20, 100));
		parameter.Value.AssertEqual(-40m);

		// Third losing trade - average should be (-50 + -30 + -20) / 3 = -33.333...
		parameter.Add(new(serverTime, 8, -20));
		parameter.Value.Round(3).AssertEqual(-33.333m);

		// Trade with no closed volume - should not affect average
		parameter.Add(new(serverTime, 0, -100));
		parameter.Value.Round(3).AssertEqual(-33.333m);
	}

	[TestMethod]
	public void PerMonthTrade()
	{
		// Arrange
		var parameter = new PerMonthTradeParameter();
		var baseTime = new DateTime(2023, 1, 15);

		// Act & Assert
		// First trade in January
		parameter.Add(new(baseTime, 10, 100));
		parameter.Value.AssertEqual(1m);

		// Second trade in same month
		parameter.Add(new(baseTime.AddDays(10), 5, 50));
		parameter.Value.AssertEqual(2m);

		// First trade in February - average becomes (2 + 1) / 2 = 1.5
		parameter.Add(new(baseTime.AddMonths(1), 8, -30));
		parameter.Value.AssertEqual(1.5m);

		// Second trade in February - average becomes (2 + 2) / 2 = 2
		parameter.Add(new(baseTime.AddMonths(1).AddDays(5), 12, 75));
		parameter.Value.AssertEqual(2m);

		// First trade in March - average becomes (2 + 2 + 1) / 3 = 1.67
		parameter.Add(new(baseTime.AddMonths(2), 6, 25));
		parameter.Value.AssertInRange(1.66m, 1.67m);
	}

	[TestMethod]
	public void PerDayTrade()
	{
		// Arrange
		var parameter = new PerDayTradeParameter();
		var baseTime = new DateTime(2023, 1, 15, 10, 0, 0);

		// Act & Assert
		// First trade on day 1
		parameter.Add(new(baseTime, 10, 100));
		parameter.Value.AssertEqual(1m);

		// Second trade on same day
		parameter.Add(new(baseTime.AddHours(2), 5, 50));
		parameter.Value.AssertEqual(2m);

		// First trade on day 2 - average becomes (2 + 1) / 2 = 1.5
		parameter.Add(new(baseTime.AddDays(1), 8, -30));
		parameter.Value.AssertEqual(1.5m);

		// Second trade on day 2 - average becomes (2 + 2) / 2 = 2
		parameter.Add(new(baseTime.AddDays(1).AddHours(3), 12, 75));
		parameter.Value.AssertEqual(2m);

		// First trade on day 3 - average becomes (2 + 2 + 1) / 3 = 1.67
		parameter.Add(new(baseTime.AddDays(2), 6, 25));
		parameter.Value.AssertInRange(1.66m, 1.67m);
	}

	[TestMethod]
	public void TradeStatistic()
	{
		// Arrange
		var winningTrades = new WinningTradesParameter();
		var lossingTrades = new LossingTradesParameter();
		var averageTradeProfit = new AverageTradeProfitParameter();
		var averageWinTrade = new AverageWinTradeParameter();
		var averageLossTrade = new AverageLossTradeParameter();

		// Act & Assert - null argument should throw
		Assert.ThrowsExactly<ArgumentNullException>(() => winningTrades.Add(null));
		Assert.ThrowsExactly<ArgumentNullException>(() => lossingTrades.Add(null));
		Assert.ThrowsExactly<ArgumentNullException>(() => averageTradeProfit.Add(null));
		Assert.ThrowsExactly<ArgumentNullException>(() => averageWinTrade.Add(null));
		Assert.ThrowsExactly<ArgumentNullException>(() => averageLossTrade.Add(null));
	}

	[TestMethod]
	public void PnLInfo()
	{
		// Arrange
		var serverTime = DateTimeOffset.UtcNow;

		// Act & Assert - negative closed volume should throw
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = new PnLInfo(serverTime, -1, 100));

		// Valid closed volume should not throw
		var pnlInfo = new PnLInfo(serverTime, 0, 100);
		pnlInfo.ClosedVolume.AssertEqual(0);
		pnlInfo.PnL.AssertEqual(100);
		pnlInfo.ServerTime.AssertEqual(serverTime);
	}

	[TestMethod]
	public void TradeStatisticPnL()
	{
		// Arrange
		var winningTrades = new WinningTradesParameter();
		var lossingTrades = new LossingTradesParameter();
		var tradeCount = new TradeCountParameter();
		var roundtripCount = new RoundtripCountParameter();
		var averageTradeProfit = new AverageTradeProfitParameter();
		var averageWinTrade = new AverageWinTradeParameter();
		var averageLossTrade = new AverageLossTradeParameter();
		var perMonthTrade = new PerMonthTradeParameter();
		var perDayTrade = new PerDayTradeParameter();

		var serverTime = DateTimeOffset.UtcNow;

		// Add some values
		winningTrades.Add(new(serverTime, 10, 100));
		lossingTrades.Add(new(serverTime, 10, -50));
		tradeCount.Add(new(serverTime, 10, 100));
		roundtripCount.Add(new(serverTime, 10, 50));
		averageTradeProfit.Add(new(serverTime, 10, 100));
		averageWinTrade.Add(new(serverTime, 10, 100));
		averageLossTrade.Add(new(serverTime, 10, -50));
		perMonthTrade.Add(new(serverTime, 10, 30));
		perDayTrade.Add(new(serverTime, 10, 40));

		// Act
		winningTrades.Reset();
		lossingTrades.Reset();
		tradeCount.Reset();
		roundtripCount.Reset();
		averageTradeProfit.Reset();
		averageWinTrade.Reset();
		averageLossTrade.Reset();
		perMonthTrade.Reset();
		perDayTrade.Reset();

		// Assert
		winningTrades.Value.AssertEqual(0);
		lossingTrades.Value.AssertEqual(0);
		tradeCount.Value.AssertEqual(0);
		roundtripCount.Value.AssertEqual(0);
		averageTradeProfit.Value.AssertEqual(0m);
		averageWinTrade.Value.AssertEqual(0m);
		averageLossTrade.Value.AssertEqual(0m);
		perMonthTrade.Value.AssertEqual(0m);
		perDayTrade.Value.AssertEqual(0m);
	}

	[TestMethod]
	public void SaveAndLoad()
	{
		// Arrange
		var parameter = new PerMonthTradeParameter();
		var baseTime = new DateTime(2023, 1, 15);

		// Add some trades
		parameter.Add(new(baseTime, 10, 100));
		parameter.Add(new(baseTime.AddDays(10), 5, 50));
		parameter.Add(new(baseTime.AddMonths(1), 8, -30));

		// Act - save state
		var storage = parameter.Save();

		// Create new parameter and load state
		var newParameter = new PerMonthTradeParameter();
		newParameter.Load(storage);

		// Assert - state should be preserved
		newParameter.Value.AssertEqual(parameter.Value);

		// Add new trade and verify behavior is consistent
		var nextTradeTime = baseTime.AddMonths(1).AddDays(5);
		parameter.Add(new(nextTradeTime, 12, 75));
		newParameter.Add(new(nextTradeTime, 12, 75));

		parameter.Value.AssertEqual(newParameter.Value);
	}

	[TestMethod]
	public void SharpeRatioPositive()
	{
		var parameter = new SharpeRatioParameter();
		var t = DateTimeOffset.UtcNow;

		// PnL: 0.0 → 0.1 → 0.3 → 0.5, returns: +0.1, +0.2, +0.2 (mean > 0)
		parameter.Add(t, 0.0m, null);
		parameter.Add(t, 0.1m, null);
		parameter.Add(t, 0.3m, null);
		parameter.Add(t, 0.5m, null);

		(parameter.Value > 0).AssertTrue();
	}

	[TestMethod]
	public void SharpeRatioNegative()
	{
		var parameter = new SharpeRatioParameter();
		var t = DateTimeOffset.UtcNow;

		// PnL: 0.0 → -0.1 → -0.2 → -0.5, returns: -0.1, -0.1, -0.3 (mean < 0)
		parameter.Add(t, 0.0m, null);
		parameter.Add(t, -0.1m, null);
		parameter.Add(t, -0.2m, null);
		parameter.Add(t, -0.5m, null);

		(parameter.Value < 0).AssertTrue();
	}

	[TestMethod]
	public void SharpeRatioZero()
	{
		var parameter = new SharpeRatioParameter();
		var t = DateTimeOffset.UtcNow;

		// PnL: 0.0 → 0.0 → 0.0 → 0.0, returns: 0, 0, 0 (mean = 0)
		parameter.Add(t, 0.0m, null);
		parameter.Add(t, 0.0m, null);
		parameter.Add(t, 0.0m, null);
		parameter.Add(t, 0.0m, null);

		parameter.Value.AssertEqual(0);
	}

	[TestMethod]
	public void SortinoRatioPositive()
	{
		var parameter = new SortinoRatioParameter();
		var t = DateTimeOffset.UtcNow;

		// PnL: 0.0 → 0.1 → 0.2 → 0.4, returns: +0.1, +0.1, +0.2 (downside deviation = 0)
		parameter.Add(t, 0.0m, null);
		parameter.Add(t, 0.1m, null);
		parameter.Add(t, 0.2m, null);
		parameter.Add(t, 0.4m, null);

		// Если нет отрицательных возвратов, downside deviation = 0, коэффициент не определён (Value = 0)
		parameter.Value.AssertEqual(0);
	}

	[TestMethod]
	public void SortinoRatioMixed()
	{
		var parameter = new SortinoRatioParameter();
		var t = DateTimeOffset.UtcNow;

		// PnL: 0.0 → 0.1 → 0.3 → 0.2, returns: +0.1, +0.2, -0.1 (mean > 0, есть downside)
		parameter.Add(t, 0.0m, null);
		parameter.Add(t, 0.1m, null);
		parameter.Add(t, 0.3m, null);
		parameter.Add(t, 0.2m, null);

		(parameter.Value > 0).AssertTrue();
	}

	[TestMethod]
	public void SortinoRatioNegative()
	{
		var parameter = new SortinoRatioParameter();
		var t = DateTimeOffset.UtcNow;

		// PnL: 0.0 → -0.1 → -0.3 → -0.5, returns: -0.1, -0.2, -0.2 (mean < 0, downside deviation > 0)
		parameter.Add(t, 0.0m, null);
		parameter.Add(t, -0.1m, null);
		parameter.Add(t, -0.3m, null);
		parameter.Add(t, -0.5m, null);

		(parameter.Value < 0).AssertTrue();
	}

	[TestMethod]
	public void SortinoRatioZero()
	{
		var parameter = new SortinoRatioParameter();
		var t = DateTimeOffset.UtcNow;

		// PnL: 0.0 → 0.1 → 0.3 → 0.6, returns: +0.1, +0.2, +0.3 (все возвраты положительные)
		parameter.Add(t, 0.0m, null);
		parameter.Add(t, 0.1m, null);
		parameter.Add(t, 0.3m, null);
		parameter.Add(t, 0.6m, null);

		// Downside deviation = 0, коэффициент = 0 (по текущей реализации)
		parameter.Value.AssertEqual(0);
	}

	[TestMethod]
	public void AverageDrawdown()
	{
		// Arrange
		var parameter = new AverageDrawdownParameter();
		var marketTime = DateTimeOffset.UtcNow;

		// Act - simulate drawdowns: 0, 100, 50, 0, 200
		parameter.Add(marketTime, 1000m, null); // peak
		parameter.Add(marketTime, 900m, null);  // drawdown 100
		parameter.Add(marketTime, 950m, null);  // drawdown 50
		parameter.Add(marketTime, 1000m, null); // recovery
		parameter.Add(marketTime, 800m, null);  // drawdown 200

		// Assert - average drawdown should be > 0
		(parameter.Value > 0).AssertTrue();
	}

	[TestMethod]
	public void AverageDrawdown_ComplexCases()
	{
		var parameter = new AverageDrawdownParameter();
		var t = DateTimeOffset.UtcNow;

		// Case 1: Standard drawdown and recovery
		parameter.Add(t, 1000m, null); // New peak
		parameter.Add(t, 900m, null);  // Start of drawdown (down 100)
		parameter.Add(t, 950m, null);  // Partial recovery
		parameter.Add(t, 1000m, null); // Full recovery to previous peak (drawdown fixed)

		// Expect: Only one drawdown of 100
		parameter.Value.AreEqual(100m);

		parameter.Reset();

		// Case 2: Minimum inside drawdown is lower than last value before recovery
		parameter.Add(t, 1000m, null); // Peak
		parameter.Add(t, 800m, null);  // Deepest value in drawdown
		parameter.Add(t, 900m, null);  // Recovery, but not to peak
		parameter.Add(t, 950m, null);  // More recovery
		parameter.Add(t, 1000m, null); // Full recovery (drawdown should be from 1000 to 800)

		// If your logic only captures the last value, result will be 1000-950=50,
		// but the canonical logic expects 200 (1000-800).
		parameter.Value.AreEqual(200m);

		parameter.Reset();

		// Case 3: Two separate drawdowns
		parameter.Add(t, 1000m, null); // Peak
		parameter.Add(t, 900m, null);  // First drawdown
		parameter.Add(t, 1000m, null); // Recovery
		parameter.Add(t, 800m, null);  // Second drawdown
		parameter.Add(t, 1000m, null); // Recovery

		// Expect: Average of 100 (first) and 200 (second) = 150
		parameter.Value.AreEqual(150m);

		parameter.Reset();

		// Case 4: Drawdown not yet recovered (unfinished)
		parameter.Add(t, 1000m, null); // Peak
		parameter.Add(t, 900m, null);  // Start drawdown
		parameter.Add(t, 800m, null);  // Gets deeper
		parameter.Add(t, 850m, null);  // Partial recovery, but not to peak

		// Expect: Current unfinished drawdown is from 1000 to 800 (200)
		parameter.Value.AreEqual(200m);

		parameter.Reset();

		// Case 5: Multiple peaks, no drawdown
		parameter.Add(t, 1000m, null);
		parameter.Add(t, 1000m, null);
		parameter.Add(t, 1000m, null);

		// Expect: No drawdown, average is zero
		parameter.Value.AreEqual(0m);

		parameter.Reset();

		// Case 6: Sharp drop, then immediate new peak
		parameter.Add(t, 1000m, null);
		parameter.Add(t, 700m, null);   // Drop
		parameter.Add(t, 1200m, null);  // New peak right after

		// Expect: Drawdown from 1000 to 700 = 300
		parameter.Value.AreEqual(300m);
	}

	[TestMethod]
	public void Expectancy()
	{
		// Arrange
		var parameter = new ExpectancyParameter();
		var marketTime = DateTimeOffset.UtcNow;

		// Act - add some trades: win 100, loss -50, win 50, loss -30
		parameter.Add(new(marketTime, 1, 100m));
		parameter.Add(new(marketTime, 1, -50m));
		parameter.Add(new(marketTime, 1, 50m));
		parameter.Add(new(marketTime, 1, -30m));

		// Assert - expectancy should be > 0
		(parameter.Value > 0).AssertTrue();
	}

	[TestMethod]
	public void ProfitFactor()
	{
		// Arrange
		var parameter = new ProfitFactorParameter();
		var marketTime = DateTimeOffset.UtcNow;

		// Act - add some trades: win 100, loss -50, win 50, loss -25
		parameter.Add(new(marketTime, 1, 100m));
		parameter.Add(new(marketTime, 1, -50m));
		parameter.Add(new(marketTime, 1, 50m));
		parameter.Add(new(marketTime, 1, -25m));

		// Assert - profit factor should be > 1
		Assert.IsTrue(parameter.Value > 1);
	}

	[TestMethod]
	public void ProfitFactor2()
	{
		// Arrange
		var parameter = new ProfitFactorParameter();
		var marketTime = DateTimeOffset.UtcNow;

		// Act - add some trades: win 100, loss -50, win 50, loss -25
		parameter.Add(new(marketTime, 1, 100m));
		parameter.Add(new(marketTime, 1, -50m));
		parameter.Add(new(marketTime, 1, 0m));
		parameter.Add(new(marketTime, 1, 200m));
		parameter.Add(new(marketTime, 1, -100m));
		parameter.Add(new(marketTime, 1, 50m));

		// total win = 100 + 200 + 50 = 350
		// total loss = 50 + 100 = 150
		// The ratio should be win/loss
		((Math.Abs(parameter.Value - (350m / 150m)) < 1e-10m)).AssertTrue();
	}

	[TestMethod]
	public void CalmarRatio()
	{
		// Arrange
		var profit = new NetProfitParameter();
		var maxDrawdown = new MaxDrawdownParameter();
		var parameter = new CalmarRatioParameter(profit, maxDrawdown);

		var t = DateTimeOffset.UtcNow;

		// Act - add some profits and drawdowns
		decimal[] pnls = [1000m, 800m, 1200m];

		foreach (var pnl in pnls)
		{
			profit.Add(t, pnl, null);
			maxDrawdown.Add(t, pnl, null);
			parameter.Add(t, pnl, null);
		}

		// Assert - Calmar ratio should be > 0
		(parameter.Value > 0).AssertTrue();
	}

	[TestMethod]
	public void SterlingRatio()
	{
		// Arrange
		var profit = new NetProfitParameter();
		var avgDrawdown = new AverageDrawdownParameter();
		var parameter = new SterlingRatioParameter(profit, avgDrawdown);

		var t = DateTimeOffset.UtcNow;
		decimal[] pnls = [1000m, 900m, 1100m, 950m];

		foreach (var pnl in pnls)
		{
			profit.Add(t, pnl, null);
			avgDrawdown.Add(t, pnl, null);
			parameter.Add(t, pnl, null);
		}

		(parameter.Value > 0).AssertTrue();
	}

	[TestMethod]
	public void AverageDrawdown_MultipleDrawdownsAndUnfinished()
	{
		var parameter = new AverageDrawdownParameter();
		var t = DateTimeOffset.UtcNow;

		// Peak → deep drawdown → partial recovery → new peak (fix first drawdown)
		parameter.Add(t, 1000m, null);
		parameter.Add(t, 800m, null);   // drawdown to 800
		parameter.Add(t, 900m, null);   // partial recovery
		parameter.Add(t, 1100m, null);  // new peak, drawdown fixed (1100-800=300)

		// Start new drawdown but do not recover fully yet
		parameter.Add(t, 900m, null);   // start new drawdown
		parameter.Add(t, 950m, null);   // partial recovery, still in drawdown

		parameter.Value.AssertEqual(200);
	}

	[TestMethod]
	public void MaxDrawdown_HandlesMultipleMinimas()
	{
		var parameter = new MaxDrawdownParameter();
		var t = DateTimeOffset.UtcNow;

		// Simulate two drawdowns, second is deeper
		parameter.Add(t, 1000m, null); // first peak
		parameter.Add(t, 900m, null);  // drawdown to 900
		parameter.Add(t, 1000m, null); // recovery
		parameter.Add(t, 800m, null);  // second drawdown
		parameter.Add(t, 1000m, null); // recovery

		// Expect: Maximum of 100 (first) and 200 (second) = 200
		parameter.Value.AssertEqual(200m);
	}

	[TestMethod]
	public void ProfitFactor_NegativeAndZeroTrades()
	{
		var parameter = new ProfitFactorParameter();
		var t = DateTimeOffset.UtcNow;

		// 3 wins, 2 losses, 1 zero
		parameter.Add(new(t, 1, 100m));   // win
		parameter.Add(new(t, 1, -50m));   // loss
		parameter.Add(new(t, 1, 0m));     // zero (should be ignored)
		parameter.Add(new(t, 1, 200m));   // win
		parameter.Add(new(t, 1, -100m));  // loss
		parameter.Add(new(t, 1, 50m));    // win

		// total win = 100 + 200 + 50 = 350
		// total loss = 50 + 100 = 150
		// The ratio should be win/loss
		parameter.Value.AssertEqual(350m / 150m);

		// No closed volume samples must be ignored
		var before = parameter.Value;
		parameter.Add(new(t, 0, 500m));   // should be ignored
		parameter.Add(new(t, 0, -500m));  // should be ignored
		parameter.Value.AssertEqual(before);
	}

	[TestMethod]
	public void RecoveryFactor_MultipleDrawdownsAndProfits()
	{
		var maxDrawdown = new MaxDrawdownParameter();
		var netProfit = new NetProfitParameter();
		var parameter = new RecoveryFactorParameter(maxDrawdown, netProfit);
		var t = DateTimeOffset.UtcNow;

		// Two drawdowns, two recoveries
		maxDrawdown.Add(t, 1000m, null);
		netProfit.Add(t, 1000m, null);
		parameter.Add(t, 1000m, null);

		maxDrawdown.Add(t, 700m, null);   // first drawdown
		netProfit.Add(t, 700m, null);
		parameter.Add(t, 700m, null);

		maxDrawdown.Add(t, 1100m, null);  // recovery and new peak
		netProfit.Add(t, 1100m, null);
		parameter.Add(t, 1100m, null);

		maxDrawdown.Add(t, 800m, null);   // second drawdown
		netProfit.Add(t, 800m, null);
		parameter.Add(t, 800m, null);

		maxDrawdown.Add(t, 1200m, null);  // recovery and new peak
		netProfit.Add(t, 1200m, null);
		parameter.Add(t, 1200m, null);

		parameter.Value.AssertEqual(4);
	}

	[TestMethod]
	public void MaxDrawdown_InitialNegative()
	{
		// Arrange
		var parameter = new MaxDrawdownParameter();
		var t = DateTimeOffset.UtcNow;

		// Act: first value is negative
		parameter.Add(t, -1000m, null);
		// Expect drawdown counted from zero baseline
		parameter.Value.AssertEqual(1000m);

		// Deeper negative should increase drawdown from zero
		parameter.Add(t, -1500m, null);
		parameter.Value.AssertEqual(1500m);
	}

	[TestMethod]
	public void MaxRelativeDrawdown_InitialNegative()
	{
		// Arrange
		var parameter = new MaxRelativeDrawdownParameter();
		var t = DateTimeOffset.UtcNow;

		// Act: first negative point
		parameter.Add(t, -1000m, null);
		// With zero baseline this is undefined, but after second point we can check behavior
		parameter.Value.AssertEqual(0m);

		// Second, deeper negative
		parameter.Add(t, -1500m, null);
		// Expect relative drawdown to be positive (500/1000 = 0.5)
		parameter.Value.AssertEqual(0.5m);
	}

	[TestMethod]
	public void AverageDrawdown_InitialNegative()
	{
		// Arrange
		var parameter = new AverageDrawdownParameter();
		var t = DateTimeOffset.UtcNow;

		// Act: first value is negative
		parameter.Add(t, -1000m, null);
		// Expect unfinished drawdown from zero baseline to be reflected
		(parameter.Value > 0).AssertTrue();

		// Deeper negative increases unfinished drawdown
		parameter.Add(t, -1200m, null);
		(parameter.Value >= 1200m).AssertTrue();
	}

	[TestMethod]
	public void SharpeRatio_ScaledPnL()
	{
		// Arrange
		var s1 = new SharpeRatioParameter { RiskFreeRate = 0.05m }; // 5% RF
		var s2 = new SharpeRatioParameter { RiskFreeRate = 0.05m };
		var t = DateTimeOffset.UtcNow;

		// Baseline equity series (currency units)
		decimal[] a = [1000m, 1100m, 1050m, 1200m]; // returns: +100, -50, +150
		// Scaled by factor 10
		decimal[] b = [10000m, 11000m, 10500m, 12000m]; // returns: +1000, -500, +1500

		foreach (var v in a)
			s1.Add(t, v, null);

		foreach (var v in b)
			s2.Add(t, v, null);

		Math.Abs(s1.Value - s2.Value).AssertEqual(0m);
	}

	[TestMethod]
	public void SortinoRatio_ScaledPnL()
	{
		// Arrange
		var r1 = new SortinoRatioParameter { RiskFreeRate = 0.05m };
		var r2 = new SortinoRatioParameter { RiskFreeRate = 0.05m };
		var t = DateTimeOffset.UtcNow;

		// Baseline equity series (currency units) with negative period to ensure downside samples
		decimal[] a = [1000m, 900m, 950m, 1100m]; // returns: -100, +50, +150 (has downside)
		// Scaled by factor 10
		decimal[] b = [10000m, 9000m, 9500m, 11000m]; // returns: -1000, +500, +1500

		foreach (var v in a)
			r1.Add(t, v, null);

		foreach (var v in b)
			r2.Add(t, v, null);

		Math.Abs(r1.Value - r2.Value).AssertEqual(0m);
	}
}
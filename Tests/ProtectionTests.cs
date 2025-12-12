namespace StockSharp.Tests;

using StockSharp.Algo.Strategies.Protective;

[TestClass]
public class ProtectionTests : BaseTestClass
{
	[TestMethod]
	public void BasicPositionTracking()
	{
		// Arrange
		IProtectiveBehaviourFactory factory = new LocalProtectiveBehaviourFactory(0.1m, 2);
		var behaviour = factory.Create(
			new Unit(1m, UnitTypes.Percent),  // Take profit 1%
			new Unit(1m, UnitTypes.Percent),  // Stop loss 1%
			false,                            // No trailing stop
			TimeSpan.Zero,                    // No take timeout
			TimeSpan.Zero,                    // No stop timeout
			false);                           // No market orders

		// Act & Assert - Initial state
		behaviour.Position.AssertEqual(0);

		// Open long position at price 100
		var updateResult = behaviour.Update(100m, 10m, DateTime.UtcNow);
		updateResult.AssertNull(); // No immediate activation expected
		behaviour.Position.AssertEqual(10m);

		// Test price movement without activation
		var activationResult = behaviour.TryActivate(100.5m, DateTime.UtcNow);
		activationResult.AssertNull(); // No activation yet

		// Test take profit activation
		activationResult = behaviour.TryActivate(101.1m, DateTime.UtcNow);
		activationResult.AssertNotNull();

		var (isTake, side, price, volume, condition) = activationResult.Value;
		isTake.AssertTrue(); // Should be take profit
		side.AssertEqual(Sides.Sell); // Should sell to close long
		price.AssertEqual(101.1m); // Activation price should match
		volume.AssertEqual(10m); // Full position volume
		condition.AssertNull(); // LocalProtectiveBehaviour should return null condition

		// Emulate execution of protective order (position reset)
		behaviour.Update(price, -volume, DateTime.UtcNow);
		behaviour.Position.AssertEqual(0);

		// After activation, position should be reset
		var newActivation = behaviour.TryActivate(102m, DateTime.UtcNow);
		newActivation.AssertNull(); // No activation should happen
	}

	[TestMethod]
	public void StopLossActivation()
	{
		// Arrange
		IProtectiveBehaviourFactory factory = new LocalProtectiveBehaviourFactory(0.1m, 2);
		var behaviour = factory.Create(
			new Unit(1m, UnitTypes.Percent),  // Take profit 1%
			new Unit(1m, UnitTypes.Percent),  // Stop loss 1%
			false,                            // No trailing stop
			TimeSpan.Zero,                    // No take timeout
			TimeSpan.Zero,                    // No stop timeout
			false);                           // No market orders

		// Open long position at price 100
		behaviour.Update(100m, 10m, DateTime.UtcNow);

		// Test stop loss activation
		var activationResult = behaviour.TryActivate(98.9m, DateTime.UtcNow);
		activationResult.AssertNotNull();

		var (isTake, side, price, volume, condition) = activationResult.Value;
		isTake.AssertFalse(); // Should be stop loss
		side.AssertEqual(Sides.Sell); // Should sell to close long
		price.AssertEqual(98.9m); // Activation price should match
		volume.AssertEqual(10m); // Full position volume
		condition.AssertNull();

		// Emulate execution of protective order (position reset)
		behaviour.Update(price, -volume, DateTime.UtcNow);
		behaviour.Position.AssertEqual(0);
	}

	[TestMethod]
	public void ShortPositionProtection()
	{
		// Arrange
		IProtectiveBehaviourFactory factory = new LocalProtectiveBehaviourFactory(0.1m, 2);
		var behaviour = factory.Create(
			new Unit(1m, UnitTypes.Percent),  // Take profit 1%
			new Unit(1m, UnitTypes.Percent),  // Stop loss 1%
			false,                            // No trailing stop 
			TimeSpan.Zero,                    // No take timeout
			TimeSpan.Zero,                    // No stop timeout
			false);                           // No market orders

		// Open short position at price 100
		behaviour.Update(100m, -10m, DateTime.UtcNow);
		behaviour.Position.AssertEqual(-10m);

		// Test take profit activation (price going down for short)
		var activationResult = behaviour.TryActivate(98.9m, DateTime.UtcNow);
		activationResult.AssertNotNull();

		var (isTake, side, price, volume, condition) = activationResult.Value;
		isTake.AssertTrue(); // Should be take profit
		side.AssertEqual(Sides.Buy); // Should buy to close short
		price.AssertEqual(98.9m); // Activation price should match
		volume.AssertEqual(10m); // Full position volume
		condition.AssertNull();

		// Emulate execution of protective order (position reset)
		behaviour.Update(price, volume, DateTime.UtcNow);
		behaviour.Position.AssertEqual(0);

		// Test new position after activation
		behaviour.Update(95m, -5m, DateTime.UtcNow);

		// Test stop loss activation (price going up for short)
		activationResult = behaviour.TryActivate(96m, DateTime.UtcNow);
		activationResult.AssertNotNull();
		(isTake, side, price, volume, condition) = activationResult.Value;
		isTake.AssertFalse(); // Should be stop loss
		side.AssertEqual(Sides.Buy); // Should buy to close short
		price.AssertEqual(96m); // Activation price should match
		volume.AssertEqual(5m); // Full position volume for this test
		condition.AssertNull();

		// Emulate execution of protective order (position reset)
		behaviour.Update(price, volume, DateTime.UtcNow);
		behaviour.Position.AssertEqual(0);
	}

	[TestMethod]
	public void TrailingStop()
	{
		// Arrange
		IProtectiveBehaviourFactory factory = new LocalProtectiveBehaviourFactory(0.1m, 2);
		var behaviour = factory.Create(
			new Unit(2m, UnitTypes.Percent),  // Take profit 2%
			new Unit(1m, UnitTypes.Percent),  // Stop loss 1%
			true,                             // Trailing stop
			TimeSpan.Zero,                    // No take timeout
			TimeSpan.Zero,                    // No stop timeout
			false);                           // No market orders

		// Open long position at price 100
		behaviour.Update(100m, 10m, DateTime.UtcNow);

		// Price moves up, no activation
		var activationResult = behaviour.TryActivate(101m, DateTime.UtcNow);
		activationResult.AssertNull();

		// Price moves up to take profit, should activate take profit
		activationResult = behaviour.TryActivate(102m, DateTime.UtcNow);
		activationResult.AssertNotNull();
		{
			var (isTake, side, price, volume, condition) = activationResult.Value;
			isTake.AssertTrue(); // Take profit
			side.AssertEqual(Sides.Sell);
			price.AssertEqual(102m);
			volume.AssertEqual(10m);
			condition.AssertNull();
		}
		// Emulate execution of protective order (position reset)
		behaviour.Update(102m, -10m, DateTime.UtcNow);
		behaviour.Position.AssertEqual(0);

		// Open new position at price 100
		behaviour.Update(100m, 10m, DateTime.UtcNow);

		// Price moves up more, no activation
		activationResult = behaviour.TryActivate(102m, DateTime.UtcNow);
		activationResult.AssertNotNull(); // take profit снова
		{
			var (isTake, side, price, volume, condition) = activationResult.Value;
			isTake.AssertTrue();
			side.AssertEqual(Sides.Sell);
			price.AssertEqual(102m);
			volume.AssertEqual(10m);
			condition.AssertNull();
		}
		behaviour.Update(102m, -10m, DateTime.UtcNow);
		behaviour.Position.AssertEqual(0);

		// Test trailing stop activation (take profit is not active)
		var behaviourTrailingOnly = factory.Create(
			new Unit(1000m, UnitTypes.Percent), // Take profit too high to activate
			new Unit(1m, UnitTypes.Percent),    // Stop loss 1%
			true,                              // Trailing stop
			TimeSpan.Zero, TimeSpan.Zero, false);

		behaviourTrailingOnly.Update(100m, 10m, DateTime.UtcNow);

		// Price moves up, trailing stop подтягивается, но не срабатывает
		activationResult = behaviourTrailingOnly.TryActivate(101m, DateTime.UtcNow);
		activationResult.AssertNull();
		activationResult = behaviourTrailingOnly.TryActivate(102m, DateTime.UtcNow);
		activationResult.AssertNull();
		activationResult = behaviourTrailingOnly.TryActivate(101.5m, DateTime.UtcNow);
		activationResult.AssertNull();

		// Price falls to trailing stop (1% below 102 = 100.98)
		activationResult = behaviourTrailingOnly.TryActivate(100.98m, DateTime.UtcNow);
		activationResult.AssertNotNull();
		{
			var (isTake, side, price, volume, condition) = activationResult.Value;
			isTake.AssertFalse(); // Stop loss
			side.AssertEqual(Sides.Sell);
			price.AssertEqual(100.98m);
			volume.AssertEqual(10m);
			condition.AssertNull();
		}
		behaviourTrailingOnly.Update(100.98m, -10m, DateTime.UtcNow);
		behaviourTrailingOnly.Position.AssertEqual(0);
	}

	[TestMethod]
	public void PositionModifications()
	{
		// Arrange
		IProtectiveBehaviourFactory factory = new LocalProtectiveBehaviourFactory(0.01m, 2);
		var behaviour = factory.Create(
			new Unit(1m, UnitTypes.Percent),  // Take profit 1%
			new Unit(1m, UnitTypes.Percent),  // Stop loss 1%
			false,                            // No trailing stop
			TimeSpan.Zero,                    // No take timeout
			TimeSpan.Zero,                    // No stop timeout
			false);                           // No market orders

		// Open long position at price 100
		behaviour.Update(100m, 5m, DateTime.UtcNow);
		behaviour.Position.AssertEqual(5m);

		// Add to position at higher price
		behaviour.Update(102m, 5m, DateTime.UtcNow);
		behaviour.Position.AssertEqual(10m);

		// Test take profit based on weighted average price (101)
		var activationResult = behaviour.TryActivate(102.01m, DateTime.UtcNow);
		activationResult.AssertNotNull();

		var (isTake, side, price, volume, condition) = activationResult.Value;
		isTake.AssertTrue(); // Should be take profit
		side.AssertEqual(Sides.Sell);
		price.AssertEqual(102.01m);
		volume.AssertEqual(10m);
		condition.AssertNull();

		// Emulate execution of protective order (position reset)
		behaviour.Update(price, -volume, DateTime.UtcNow);
		behaviour.Position.AssertEqual(0);

		// Open new position and reduce it
		behaviour.Update(100m, 10m, DateTime.UtcNow);
		behaviour.Position.AssertEqual(10m);

		// Reduce position
		behaviour.Update(100m, -3m, DateTime.UtcNow);
		behaviour.Position.AssertEqual(7m);

		// Close position
		behaviour.Update(100m, -7m, DateTime.UtcNow);
		behaviour.Position.AssertEqual(0);

		// After position is closed, no activation should happen
		activationResult = behaviour.TryActivate(110m, DateTime.UtcNow);
		activationResult.AssertNull();
	}

	[TestMethod]
	public void TimeoutActivation()
	{
		// Arrange
		var takeTimeout = TimeSpan.FromSeconds(30);
		var stopTimeout = TimeSpan.FromSeconds(60);

		IProtectiveBehaviourFactory factory = new LocalProtectiveBehaviourFactory(0.1m, 2);
		var behaviour = factory.Create(
			new Unit(5m, UnitTypes.Percent),  // Take profit 5%
			new Unit(3m, UnitTypes.Percent),  // Stop loss 3%
			false,                            // No trailing stop
			takeTimeout,                      // Take profit timeout
			stopTimeout,                      // Stop loss timeout
			true);                            // Use market orders

		// Open long position at price 100
		var startTime = DateTime.UtcNow;
		behaviour.Update(100m, 10m, startTime);

		// No activation before timeout
		startTime = startTime.AddSeconds(10);
		var activationResult = behaviour.TryActivate(100m, startTime);
		activationResult.AssertNull();

		// Take profit timeout activation
		startTime = startTime.AddSeconds(35);
		activationResult = behaviour.TryActivate(100m, startTime);
		activationResult.AssertNotNull();

		var (isTake, side, price, volume, condition) = activationResult.Value;
		// Should be take profit due to take timeout occurring first
		isTake.AssertTrue();
		side.AssertEqual(Sides.Sell);
		price.AssertEqual(0m);
		volume.AssertEqual(10m);
		condition.AssertNull(); // No special condition

		// Emulate execution of protective order (position reset)
		behaviour.Update(100m, -volume, startTime);
		behaviour.Position.AssertEqual(0);

		behaviour = factory.Create(
			new Unit(5m, UnitTypes.Percent),  // Take profit 5%
			new Unit(3m, UnitTypes.Percent),  // Stop loss 3%
			false,                            // No trailing stop
			default,                          // No take profit timeout
			stopTimeout,                      // Stop loss timeout
			true);                            // Use market orders
											  // Setup new test for stop loss timeout
		startTime = startTime.AddSeconds(5);
		behaviour.Update(100m, 10m, startTime);

		// Take profit timeout deactivated for test
		startTime = startTime.AddSeconds(65);
		activationResult = behaviour.TryActivate(100m, startTime);
		activationResult.AssertNotNull();
		(isTake, side, price, volume, condition) = activationResult.Value;
		isTake.AssertFalse(); // Should be stop loss due to timeout
		side.AssertEqual(Sides.Sell);
		price.AssertEqual(0);
		volume.AssertEqual(10m);
		condition.AssertNull(); // No special condition

		// Emulate execution of protective order (position reset)
		behaviour.Update(100m, -volume, startTime);
		behaviour.Position.AssertEqual(0);
	}

	[TestMethod]
	public void PositionFlipping()
	{
		// Arrange
		IProtectiveBehaviourFactory factory = new LocalProtectiveBehaviourFactory(0.1m, 2);
		var behaviour = factory.Create(
			new Unit(1m, UnitTypes.Percent),  // Take profit 1%
			new Unit(1m, UnitTypes.Percent),  // Stop loss 1%
			false,                            // No trailing stop
			TimeSpan.Zero,                    // No take timeout
			TimeSpan.Zero,                    // No stop timeout
			false);                           // No market orders

		// Open long position
		behaviour.Update(100m, 10m, DateTime.UtcNow);

		// Flip to short position
		behaviour.Update(102m, -20m, DateTime.UtcNow);
		behaviour.Position.AssertEqual(-10m);

		// Check take profit for new short position (based on new entry price 102)
		var activationResult = behaviour.TryActivate(100.98m, DateTime.UtcNow);
		activationResult.AssertNotNull();

		var (isTake, side, price, volume, condition) = activationResult.Value;
		isTake.AssertTrue(); // Should be take profit
		side.AssertEqual(Sides.Buy); // Should buy to close short
		price.AssertEqual(100.98m);
		volume.AssertEqual(10m);
		condition.AssertNull(); // No special condition

		// Emulate execution of protective order (position reset)
		behaviour.Update(price, volume, DateTime.UtcNow);
		behaviour.Position.AssertEqual(0);
	}

	[TestMethod]
	public void MultiplePositions()
	{
		// Arrange
		var controller = new ProtectiveController();
		IProtectiveBehaviourFactory factory = new LocalProtectiveBehaviourFactory(0.1m, 2);

		// Create two different position controllers
		var securityId1 = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var securityId2 = new SecurityId { SecurityCode = "MSFT", BoardCode = "NASDAQ" };
		var portfolio1 = "Portfolio1";
		var portfolio2 = "Portfolio2";

		var posController1 = controller.GetController(
			securityId1, portfolio1, factory,
			new Unit(1m, UnitTypes.Percent),  // Take profit 1%
			new Unit(1m, UnitTypes.Percent),  // Stop loss 1%
			false, TimeSpan.Zero, TimeSpan.Zero, false);

		var posController2 = controller.GetController(
			securityId1, portfolio2, factory,
			new Unit(2m, UnitTypes.Percent),  // Take profit 2%
			new Unit(2m, UnitTypes.Percent),  // Stop loss 2%
			true, TimeSpan.Zero, TimeSpan.Zero, false);

		var posController3 = controller.GetController(
			securityId2, portfolio1, factory,
			new Unit(1.5m, UnitTypes.Percent),  // Take profit 1.5%
			new Unit(1.5m, UnitTypes.Percent),  // Stop loss 1.5%
			false, TimeSpan.Zero, TimeSpan.Zero, false);

		// Create positions
		posController1.Update(100m, 10m, DateTime.UtcNow);
		posController2.Update(100m, -10m, DateTime.UtcNow);
		posController3.Update(50m, 20m, DateTime.UtcNow);

		// Test position values
		posController1.Position.AssertEqual(10m);
		posController2.Position.AssertEqual(-10m);
		posController3.Position.AssertEqual(20m);

		// Test activations for securityId1
		var activations = controller.TryActivate(securityId1, 101m, DateTime.UtcNow).ToArray();
		activations.Length.AssertEqual(1); // Only posController1 should activate
		{
			var (isTake, side, price, volume, condition) = activations[0];
			isTake.AssertTrue(); // Take profit
			side.AssertEqual(Sides.Sell);
			price.AssertEqual(101m);
			volume.AssertEqual(10m);
			condition.AssertNull();
		}

		// Emulate execution of take-profit for posController1
		posController1.Update(101m, -10m, DateTime.UtcNow);
		posController1.Position.AssertEqual(0m);

		// Now at price 98, take profit should activate for posController2
		activations = [.. controller.TryActivate(securityId1, 98m, DateTime.UtcNow)];
		activations.Length.AssertEqual(1); // posController2 should activate
		{
			var (isTake, side, price, volume, condition) = activations[0];
			isTake.AssertTrue(); // Take profit for short
			side.AssertEqual(Sides.Buy);
			price.AssertEqual(98m);
			volume.AssertEqual(10m);
			condition.AssertNull();
		}
		// Emulate execution of take-profit for posController2
		posController2.Update(98m, 10m, DateTime.UtcNow);
		posController2.Position.AssertEqual(0m);

		// Test activations for securityId2
		activations = [.. controller.TryActivate(securityId2, 50.8m, DateTime.UtcNow)];
		activations.Length.AssertEqual(1); // posController3 should activate
		{
			var (isTake, side, price, volume, condition) = activations[0];
			isTake.AssertTrue(); // Take profit
			side.AssertEqual(Sides.Sell);
			price.AssertEqual(50.8m);
			volume.AssertEqual(20m);
			condition.AssertNull();
		}

		// Test Clear method
		controller.Clear();
		activations = [.. controller.TryActivate(securityId1, 95m, DateTime.UtcNow)];
		activations.Length.AssertEqual(0); // No controllers after clear
	}

	[TestMethod]
	public void UnitsPercent()
	{
		// Arrange
		IProtectiveBehaviourFactory factory = new LocalProtectiveBehaviourFactory(0.01m, 2);
		var behaviour = factory.Create(
			new Unit(1m, UnitTypes.Percent),     // Take profit +1%
			new Unit(0.5m, UnitTypes.Percent),   // Stop loss -0.5%
			false,                            // No trailing stop
			TimeSpan.Zero,                    // No take timeout
			TimeSpan.Zero,                    // No stop timeout
			false);                           // No market orders

		// Initial position
		behaviour.Update(100m, 10m, DateTime.UtcNow);

		// Stop: threshold at 99.5 (0.5% below 100). Should not activate at 99.6
		var activationResult = behaviour.TryActivate(99.6m, DateTime.UtcNow);
		activationResult.AssertNull();

		// Should activate stop at 99.5
		var activationResult2 = behaviour.TryActivate(99.5m, DateTime.UtcNow);
		activationResult2.AssertNotNull();

		var (isTake, side, price, volume, condition) = activationResult2.Value;
		isTake.AssertFalse(); // Should be stop loss
		side.AssertEqual(Sides.Sell); // Should sell to close long
		price.AssertEqual(99.5m);
		volume.AssertEqual(10m);
		condition.AssertNull(); // No special condition

		// Emulate execution of protective order (position reset)
		behaviour.Update(price, -volume, DateTime.UtcNow);
		behaviour.Position.AssertEqual(0);

		// New position
		behaviour.Update(100m, 10m, DateTime.UtcNow);

		// Take: threshold at 101.0 (1% above 100)
		activationResult = behaviour.TryActivate(101.0m, DateTime.UtcNow);
		activationResult.AssertNotNull();
		(isTake, side, price, volume, condition) = activationResult.Value;
		isTake.AssertTrue(); // Should be take profit
		side.AssertEqual(Sides.Sell); // Should sell to close long
		price.AssertEqual(101.0m);
		volume.AssertEqual(10m);
		condition.AssertNull(); // No special condition

		// Emulate execution of protective order (position reset)
		behaviour.Update(price, -volume, DateTime.UtcNow);
		behaviour.Position.AssertEqual(0);
	}

	[TestMethod]
	public void UnitsAbsolute()
	{
		// Arrange
		IProtectiveBehaviourFactory factory = new LocalProtectiveBehaviourFactory(0.01m, 2);
		var behaviour = factory.Create(
			new Unit(101m, UnitTypes.Absolute),  // Take profit at absolute price 101
			new Unit(80m, UnitTypes.Absolute),   // Stop loss at absolute price 80
			false,                             // No trailing stop
			TimeSpan.Zero,                     // No take timeout
			TimeSpan.Zero,                     // No stop timeout
			false);                            // No market orders

		// Initial position
		behaviour.Update(100m, 10m, DateTime.UtcNow);

		// Should not activate at 100.5 (take at 101 absolute)
		var activationResult = behaviour.TryActivate(100.5m, DateTime.UtcNow);
		activationResult.AssertNull();

		// Should activate take at 101 (absolute price)
		activationResult = behaviour.TryActivate(101m, DateTime.UtcNow);
		activationResult.AssertNotNull();
		{
			var (isTake, side, price, volume, condition) = activationResult.Value;
			isTake.AssertTrue();
			side.AssertEqual(Sides.Sell);
			price.AssertEqual(101m);
			volume.AssertEqual(10m);
			condition.AssertNull();
		}
		behaviour.Update(101m, -10m, DateTime.UtcNow);
		behaviour.Position.AssertEqual(0);

		// New position
		behaviour.Update(100m, 10m, DateTime.UtcNow);

		// Should not activate at 85 (stop at 80 absolute)
		activationResult = behaviour.TryActivate(85m, DateTime.UtcNow);
		activationResult.AssertNull();

		// Should activate stop at 80 (absolute price)
		activationResult = behaviour.TryActivate(80m, DateTime.UtcNow);
		activationResult.AssertNotNull();
		{
			var (isTake, side, price, volume, condition) = activationResult.Value;
			isTake.AssertFalse();
			side.AssertEqual(Sides.Sell);
			price.AssertEqual(80m);
			volume.AssertEqual(10m);
			condition.AssertNull();
		}
		behaviour.Update(80m, -10m, DateTime.UtcNow);
		behaviour.Position.AssertEqual(0);
	}

	[TestMethod]
	public void MultipleTrades()
	{
		// Arrange
		IProtectiveBehaviourFactory factory = new LocalProtectiveBehaviourFactory(0.01m, 2);
		var behaviour = factory.Create(
			new Unit(1m, UnitTypes.Percent),  // Take profit 1%
			new Unit(1m, UnitTypes.Percent),  // Stop loss 1%
			false,                            // No trailing stop
			TimeSpan.Zero,                    // No take timeout
			TimeSpan.Zero,                    // No stop timeout
			false);                           // No market orders

		// Build position gradually
		behaviour.Update(100m, 5m, DateTime.UtcNow);
		behaviour.Update(101m, 3m, DateTime.UtcNow);
		behaviour.Update(102m, 2m, DateTime.UtcNow);

		behaviour.Position.AssertEqual(10m);

		// Average price should be (100*5 + 101*3 + 102*2)/10 = 100.7

		// Test take profit at 1% above average
		var activationResult = behaviour.TryActivate(101.71m, DateTime.UtcNow);
		activationResult.AssertNotNull();

		var (isTake, side, price, volume, condition) = activationResult.Value;
		isTake.AssertTrue();
		side.AssertEqual(Sides.Sell);
		price.AssertEqual(101.71m);
		volume.AssertEqual(10m);
		condition.AssertNull(); // No special condition

		// Emulate execution of protective order (position reset)
		behaviour.Update(price, -volume, DateTime.UtcNow);
		behaviour.Position.AssertEqual(0);

		// New set of trades with partial reductions
		behaviour.Update(100m, 10m, DateTime.UtcNow);
		behaviour.Update(98m, 5m, DateTime.UtcNow);

		// Total 15 units

		// Reduce partially, should be FIFO
		behaviour.Update(100m, -6m, DateTime.UtcNow);  // Reduce 6 from first trade
		behaviour.Update(100m, -4m, DateTime.UtcNow);  // Reduce 4 from first trade + 0 from second

		// Left with 5 units from second trade at 98
		behaviour.Position.AssertEqual(5m);

		// Test take profit at 1% above 98
		activationResult = behaviour.TryActivate(98.98m, DateTime.UtcNow);
		activationResult.AssertNotNull();
		(isTake, side, price, volume, condition) = activationResult.Value;
		isTake.AssertTrue();
		side.AssertEqual(Sides.Sell);
		price.AssertEqual(98.98m);
		volume.AssertEqual(5m);
		condition.AssertNull(); // No special condition

		// Emulate execution of protective order (position reset)
		behaviour.Update(price, -volume, DateTime.UtcNow);
		behaviour.Position.AssertEqual(0);
	}

	[TestMethod]
	public void MarketOrderFlag()
	{
		// Arrange - two behaviours, one with market orders and one without
		IProtectiveBehaviourFactory factory = new LocalProtectiveBehaviourFactory(0.1m, 2);

		var behaviourLimit = factory.Create(
			new Unit(1m, UnitTypes.Percent),
			new Unit(1m, UnitTypes.Percent),
			false, TimeSpan.Zero, TimeSpan.Zero,
			false);  // Limit orders

		var behaviourMarket = factory.Create(
			new Unit(1m, UnitTypes.Percent),
			new Unit(1m, UnitTypes.Percent),
			false, TimeSpan.Zero, TimeSpan.Zero,
			true);   // Market orders

		// Setup positions
		behaviourLimit.Update(100m, 10m, DateTime.UtcNow);
		behaviourMarket.Update(100m, 10m, DateTime.UtcNow);

		// Test activations
		var activationLimit = behaviourLimit.TryActivate(101m, DateTime.UtcNow);
		var activationMarket = behaviourMarket.TryActivate(101m, DateTime.UtcNow);

		activationLimit.AssertNotNull();
		activationMarket.AssertNotNull();

		// Check the price - for limit orders, it should be equal to trigger price
		// For market orders, price might be 0 or special flag might be set
		var orderTypeLimit = activationLimit.Value.condition == null ? "Limit" : "Special";
		var orderTypeMarket = activationMarket.Value.condition == null ? "Market" : "Special";

		orderTypeLimit.AssertEqual("Limit");
		orderTypeMarket.AssertEqual("Market");
	}

	[TestMethod]
	public void BasicTests()
	{
		// Test for ProtectiveProcessor class which is used internally by the behaviour
		var logReceiver = new LogReceiver();

		// Test long position take profit
		var processorTakeLong = new ProtectiveProcessor(
			Sides.Buy, 100m, true, false,
			new Unit(1m, UnitTypes.Percent), false,
			new Unit(), TimeSpan.Zero,
			DateTime.UtcNow, logReceiver);

		var activationPrice = processorTakeLong.GetActivationPrice(101m, DateTime.UtcNow);
		activationPrice.AssertEqual(101m);

		// Test long position stop loss
		var processorStopLong = new ProtectiveProcessor(
			Sides.Buy, 100m, false, false,
			new Unit(1m, UnitTypes.Percent), false,
			new Unit(), TimeSpan.Zero,
			DateTime.UtcNow, logReceiver);

		activationPrice = processorStopLong.GetActivationPrice(99m, DateTime.UtcNow);
		activationPrice.AssertEqual(99m);

		// Test trailing stop
		var processorTrailing = new ProtectiveProcessor(
			Sides.Buy, 100m, false, true,
			new Unit(1m, UnitTypes.Percent), false,
			new Unit(), TimeSpan.Zero,
			DateTime.UtcNow, logReceiver);

		// Price moves in favorable direction, no activation
		activationPrice = processorTrailing.GetActivationPrice(102m, DateTime.UtcNow);
		activationPrice.AssertNull();

		// Price moves in unfavorable direction but not enough to trigger
		activationPrice = processorTrailing.GetActivationPrice(101.5m, DateTime.UtcNow);
		activationPrice.AssertNull();

		// Price moves enough to trigger
		activationPrice = processorTrailing.GetActivationPrice(98.9m, DateTime.UtcNow);
		activationPrice.AssertEqual(98.9m);
	}

	[TestMethod]
	public void ExtremeValues()
	{
		// Test with extreme price levels and position sizes
		IProtectiveBehaviourFactory factory = new LocalProtectiveBehaviourFactory(0.01m, 2);
		var behaviour = factory.Create(
			new Unit(50m, UnitTypes.Percent),  // Extreme take profit 50%
			new Unit(50m, UnitTypes.Percent),  // Extreme stop loss 50%
			false, TimeSpan.Zero, TimeSpan.Zero, false);

		// Large position at high price
		behaviour.Update(1000m, 1000m, DateTime.UtcNow);

		// Test take profit
		var activationResult = behaviour.TryActivate(1500m, DateTime.UtcNow);
		activationResult.AssertNotNull();

		var (isTake, side, price, volume, condition) = activationResult.Value;
		isTake.AssertTrue();
		side.AssertEqual(Sides.Sell);
		price.AssertEqual(1500m);
		volume.AssertEqual(1000m);
		condition.AssertNull();

		// Emulate execution of protective order (position reset)
		behaviour.Update(price, -volume, DateTime.UtcNow);
		behaviour.Position.AssertEqual(0);

		// Very small position and price
		behaviour.Update(0.001m, 0.001m, DateTime.UtcNow);

		// Test stop loss
		var activationResult2 = behaviour.TryActivate(0.0005m, DateTime.UtcNow);
		activationResult2.AssertNotNull();
		(isTake, side, price, volume, condition) = activationResult2.Value;
		isTake.AssertFalse();
		side.AssertEqual(Sides.Sell); // Should sell to close long
		price.AssertEqual(0.0005m);
		volume.AssertEqual(0.001m);
		condition.AssertNull();

		// Emulate execution of protective order (position reset)
		behaviour.Update(price, -volume, DateTime.UtcNow);
		behaviour.Position.AssertEqual(0);

		// Test position with zero price (should throw)
		ThrowsExactly<ArgumentOutOfRangeException>(() => behaviour.Update(0m, 10m, DateTime.UtcNow));
	}

	[TestMethod]
	public void TrailingStop_LongPosition()
	{
		IProtectiveBehaviourFactory factory = new LocalProtectiveBehaviourFactory(0.01m, 2);
		var behaviour = factory.Create(
			new Unit(100m, UnitTypes.Percent), // Take-profit is not relevant for this test
			new Unit(1m, UnitTypes.Percent),   // Trailing stop 1%
			true,                             // Trailing enabled
			TimeSpan.Zero, TimeSpan.Zero, false);

		behaviour.Update(100m, 10m, DateTime.UtcNow);
		// Trailing stop for long: the stop level moves up as price rises, but triggers only when price falls to the trailing level.
		behaviour.TryActivate(101m, DateTime.UtcNow).AssertNull();
		behaviour.TryActivate(102m, DateTime.UtcNow).AssertNull();
		behaviour.TryActivate(101.5m, DateTime.UtcNow).AssertNull();
		
		// Price falls to the trailing stop level (1% below 102 = 100.98), should trigger stop
		var activation = behaviour.TryActivate(100.98m, DateTime.UtcNow);
		activation.AssertNotNull();

		var (isTake, side, price, volume, condition) = activation.Value;
		isTake.AssertFalse();
		side.AssertEqual(Sides.Sell);
		price.AssertEqual(100.98m);
		volume.AssertEqual(10m);
		condition.AssertNull();
	}

	[TestMethod]
	public void TrailingStop_ShortPosition()
	{
		IProtectiveBehaviourFactory factory = new LocalProtectiveBehaviourFactory(0.01m, 2);
		var behaviour = factory.Create(
			new Unit(100m, UnitTypes.Percent), // Take-profit is not relevant for this test
			new Unit(1m, UnitTypes.Percent),   // Trailing stop 1%
			true,                             // Trailing enabled
			TimeSpan.Zero, TimeSpan.Zero, false);

		behaviour.Update(100m, -10m, DateTime.UtcNow);
		
		// Trailing stop for short: the stop level moves down as price falls, but triggers only when price rises to the trailing level.
		behaviour.TryActivate(99m, DateTime.UtcNow).AssertNull();
		behaviour.TryActivate(98m, DateTime.UtcNow).AssertNull();
		behaviour.TryActivate(98.5m, DateTime.UtcNow).AssertNull();
		
		// Price rises to the trailing stop level (1% above 98 = 98.98), should trigger stop
		var activation = behaviour.TryActivate(98.98m, DateTime.UtcNow);
		activation.AssertNotNull();
		
		var (isTake, side, price, volume, condition) = activation.Value;
		isTake.AssertFalse();
		side.AssertEqual(Sides.Buy);
		price.AssertEqual(98.98m);
		volume.AssertEqual(10m);
		condition.AssertNull();
	}

	[TestMethod]
	public void TrailingStop_LimitUnitType()
	{
		// Test for ProtectiveProcessor class which is used internally by the behaviour
		var logReceiver = new LogReceiver();

		IProtectiveBehaviourFactory factory = new LocalProtectiveBehaviourFactory(0.1m, 2);

		// Trailing stop with UnitTypes.Absolute for take profit
		factory.Create(
			new Unit(1m, UnitTypes.Absolute),
			new Unit(1m, UnitTypes.Percent),
			true, // Trailing stop enabled
			TimeSpan.Zero, TimeSpan.Zero, false);

		// Trailing stop with UnitTypes.Absolute for stop loss
		ThrowsExactly<ArgumentException>(() =>
			factory.Create(
				new Unit(1m, UnitTypes.Percent),
				new Unit(1m, UnitTypes.Absolute),
				true, // Trailing stop enabled
				TimeSpan.Zero, TimeSpan.Zero, false));

		// Regular stop with UnitTypes.Absolute for take profit
		var p = new ProtectiveProcessor(
			Sides.Buy, 100m, false, false, new(10, UnitTypes.Absolute),
			true, new Unit(), TimeSpan.Zero, DateTime.UtcNow, logReceiver);

		p.AssertNotNull();

		// Trailing stop with UnitTypes.Absolute for stop loss
		ThrowsExactly<ArgumentException>(() =>
			new ProtectiveProcessor(
				Sides.Buy, 100m, false, true, new(10, UnitTypes.Absolute),
				true, new Unit(), TimeSpan.Zero, DateTime.UtcNow, logReceiver));
	}
}

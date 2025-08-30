namespace StockSharp.Tests;

using StockSharp.Algo.Commissions;

[TestClass]
public class CommissionTests
{
	private static DateTimeOffset Inc(ref DateTimeOffset time)
	{
		time = time.AddHours(1);
		return time;
	}

	private static ExecutionMessage CreateOrderMessage(decimal price, decimal volume, DateTimeOffset time)
	{
		return new()
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderPrice = price,
			OrderVolume = volume,
			ServerTime = time
		};
	}

	private static ExecutionMessage CreateTradeMessage(decimal price, decimal volume, DateTimeOffset time)
	{
		return new()
		{
			DataTypeEx = DataType.Transactions,
			TradePrice = price,
			TradeVolume = volume,
			ServerTime = time
		};
	}

	[TestMethod]
	public void PerOrderRule()
	{
		var now = DateTimeOffset.UtcNow;

		// Arrange
		var rule = new CommissionOrderRule
		{
			Value = 10m
		};

		// Act & Assert
		var orderMsg = CreateOrderMessage(100m, 10m, Inc(ref now));
		var result = rule.Process(orderMsg);
		result.AssertEqual(10m);

		// Test null when not order info
		var tradeMsg = CreateTradeMessage(100m, 10m, Inc(ref now));
		result = rule.Process(tradeMsg);
		result.AssertNull();

		// Test percent-based commission
		rule.Value = new Unit { Value = 5m, Type = UnitTypes.Percent };
		result = rule.Process(orderMsg);
		result.AssertEqual(50m); // 5% of 100 * 10 = 50
	}

	[TestMethod]
	public void PerTradeRule()
	{
		var now = DateTimeOffset.UtcNow;

		// Arrange
		var rule = new CommissionTradeRule
		{
			Value = 15m
		};

		// Act & Assert
		var tradeMsg = CreateTradeMessage(200m, 5m, Inc(ref now));
		var result = rule.Process(tradeMsg);
		result.AssertEqual(15m);

		// Test null when not trade info
		var orderMsg = CreateOrderMessage(200m, 5m, Inc(ref now));
		result = rule.Process(orderMsg);
		result.AssertNull();

		// Test percent-based commission
		rule.Value = new Unit { Value = 2.5m, Type = UnitTypes.Percent };
		result = rule.Process(tradeMsg);
		result.AssertEqual(25m); // 2.5% of 200 * 5 = 25
	}

	[TestMethod]
	public void PerOrderVolumeRule()
	{
		var now = DateTimeOffset.UtcNow;

		// Arrange
		var rule = new CommissionOrderVolumeRule
		{
			Value = 0.5m
		};

		// Act & Assert
		var orderMsg = CreateOrderMessage(150m, 20m, Inc(ref now));
		var result = rule.Process(orderMsg);
		result.AssertEqual(10m); // 0.5 * 20 = 10

		// Test null when not order info
		var tradeMsg = CreateTradeMessage(150m, 20m, Inc(ref now));
		result = rule.Process(tradeMsg);
		result.AssertNull();
	}

	[TestMethod]
	public void PerTradeVolumeRule()
	{
		var now = DateTimeOffset.UtcNow;

		// Arrange
		var rule = new CommissionTradeVolumeRule
		{
			Value = 0.25m
		};

		// Act & Assert
		var tradeMsg = CreateTradeMessage(300m, 40m, Inc(ref now));
		var result = rule.Process(tradeMsg);
		result.AssertEqual(10m); // 0.25 * 40 = 10

		// Test null when not trade info
		var orderMsg = CreateOrderMessage(300m, 40m, Inc(ref now));
		result = rule.Process(orderMsg);
		result.AssertNull();
	}

	[TestMethod]
	public void PerOrderCountRule()
	{
		var now = DateTimeOffset.UtcNow;

		// Arrange
		var rule = new CommissionOrderCountRule
		{
			Value = 25m,
			Count = 3
		};

		// Act & Assert
		var orderMsg = CreateOrderMessage(100m, 1m, Inc(ref now));

		// First 2 orders should return null
		var result = rule.Process(orderMsg);
		result.AssertNull();

		result = rule.Process(orderMsg);
		result.AssertNull();

		// 3rd order should apply commission
		result = rule.Process(orderMsg);
		result.AssertEqual(25m);

		// 4th order should be null again
		result = rule.Process(orderMsg);
		result.AssertNull();

		// Test reset functionality
		rule.Reset();

		result = rule.Process(orderMsg);
		result.AssertNull();

		// Test null when not order info
		var tradeMsg = CreateTradeMessage(100m, 1m, Inc(ref now));
		result = rule.Process(tradeMsg);
		result.AssertNull();
	}

	[TestMethod]
	public void PerTradeCountRule()
	{
		var now = DateTimeOffset.UtcNow;

		// Arrange
		var rule = new CommissionTradeCountRule
		{
			Value = 30m,
			Count = 2
		};

		// Act & Assert
		var tradeMsg = CreateTradeMessage(200m, 1m, Inc(ref now));

		// First order should return null
		var result = rule.Process(tradeMsg);
		result.AssertNull();

		// 2nd order should apply commission
		result = rule.Process(tradeMsg);
		result.AssertEqual(30m);

		// 3rd order should be null again
		result = rule.Process(tradeMsg);
		result.AssertNull();

		// Test reset functionality
		rule.Reset();

		result = rule.Process(tradeMsg);
		result.AssertNull();

		// Test null when not trade info
		var orderMsg = CreateOrderMessage(200m, 1m, Inc(ref now));
		result = rule.Process(orderMsg);
		result.AssertNull();
	}

	[TestMethod]
	public void PerTradePriceRule()
	{
		var now = DateTimeOffset.UtcNow;

		// Arrange
		var rule = new CommissionTradePriceRule
		{
			Value = 0.01m
		};

		// Act & Assert
		var tradeMsg = CreateTradeMessage(100m, 5m, Inc(ref now));
		var result = rule.Process(tradeMsg);
		result.AssertEqual(5m); // 100 * 5 * 0.01 = 5

		// Test null when not trade info
		var orderMsg = CreateOrderMessage(100m, 5m, Inc(ref now));
		result = rule.Process(orderMsg);
		result.AssertNull();
	}

	[TestMethod]
	public void SecurityIdRule()
	{
		var now = DateTimeOffset.UtcNow;

		// Arrange
		var securityId = new SecurityId { SecurityCode = "AAPL", BoardCode = BoardCodes.Nasdaq };
		var security = new Security { Id = securityId.ToStringId() };

		var rule = new CommissionSecurityIdRule
		{
			Value = 10m,
			Security = security
		};

		// Act & Assert
		var tradeMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = securityId,
			TradePrice = 150m,
			TradeVolume = 2,
			ServerTime = Inc(ref now)
		};

		var result = rule.Process(tradeMsg);
		result.AssertEqual(10m);

		// Test null for different security ID
		var differentSecurityMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = new() { SecurityCode = "MSFT", BoardCode = BoardCodes.Nasdaq },
			TradePrice = 150m,
			ServerTime = Inc(ref now)
		};

		result = rule.Process(differentSecurityMsg);
		result.AssertNull();

		// Test percent-based commission
		rule.Value = new Unit { Value = 1m, Type = UnitTypes.Percent };
		result = rule.Process(tradeMsg);
		result.AssertEqual(3m); // 1% of (150 * 2) = 3

		// Test null when not trade info
		var orderMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			SecurityId = securityId,
			OrderPrice = 150m,
			ServerTime = Inc(ref now)
		};

		result = rule.Process(orderMsg);
		result.AssertNull();
	}

	[TestMethod]
	public void BoardCodeRule()
	{
		var now = DateTimeOffset.UtcNow;

		// Arrange
		var board = new ExchangeBoard { Code = BoardCodes.Nasdaq };

		var rule = new CommissionBoardCodeRule
		{
			Value = 15m,
			Board = board
		};

		// Act & Assert
		var tradeMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = new() { BoardCode = BoardCodes.Nasdaq },
			TradePrice = 200m,
			TradeVolume = 2,
			ServerTime = Inc(ref now),
		};

		var result = rule.Process(tradeMsg);
		result.AssertEqual(15m);

		// Test null for different board
		var differentBoardMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = new() { BoardCode = "NYSE" },
			TradePrice = 200m,
			ServerTime = Inc(ref now)
		};

		result = rule.Process(differentBoardMsg);
		result.AssertNull();

		// Test percent-based commission
		rule.Value = new() { Value = 2m, Type = UnitTypes.Percent };
		result = rule.Process(tradeMsg);
		result.AssertEqual(8m); // 2% of (200 * 2) = 8

		// Test null when not trade info
		var orderMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			SecurityId = new() { BoardCode = BoardCodes.Nasdaq },
			OrderPrice = 200m,
			ServerTime = Inc(ref now)
		};

		result = rule.Process(orderMsg);
		result.AssertNull();
	}

	[TestMethod]
	public void TurnOverRule()
	{
		var now = DateTimeOffset.UtcNow;

		// Arrange
		var rule = new CommissionTurnOverRule
		{
			Value = 50m,
			TurnOver = 1000m
		};

		// Act & Assert
		var tradeMsg1 = CreateTradeMessage(100m, 5m, Inc(ref now)); // 500
		var result = rule.Process(tradeMsg1);
		result.AssertNull(); // Turnover not reached yet

		var tradeMsg2 = CreateTradeMessage(200m, 3m, Inc(ref now)); // 600 (total 1100)
		result = rule.Process(tradeMsg2);
		result.AssertEqual(50m); // Turnover reached

		// Test reset functionality
		rule.Reset();

		var tradeMsg3 = CreateTradeMessage(100m, 5m, Inc(ref now)); // 500
		result = rule.Process(tradeMsg3);
		result.AssertNull(); // Turnover not reached after reset

		// Test null when not trade info
		var orderMsg = CreateOrderMessage(100m, 5m, Inc(ref now));
		result = rule.Process(orderMsg);
		result.AssertNull();
	}

	[TestMethod]
	public void SecurityTypeRule()
	{
		var now = DateTimeOffset.UtcNow;

		// Arrange
		var rule = new CommissionSecurityTypeRule
		{
			Value = 20m,
			SecurityType = SecurityTypes.Stock
		};

		var tradeMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			TradePrice = 150m,
			TradeVolume = 2,
			ServerTime = Inc(ref now)
		};

		var result = rule.Process(tradeMsg);
		result.AssertNull();
	}

	[TestMethod]
	public void SecurityTypeRuleSecProvider()
	{
		var now = DateTimeOffset.UtcNow;

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = BoardCodes.Nasdaq };
		var appl = new Security { Id = secId.ToStringId(), Type = SecurityTypes.Stock };

		var provider = (CollectionSecurityProvider)ServicesRegistry.SecurityProvider;
		provider.Add(appl);

		// Arrange
		var rule = new CommissionSecurityTypeRule
		{
			Value = 20m,
			SecurityType = appl.Type.Value
		};

		var tradeMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = secId,
			TradePrice = 150m,
			TradeVolume = 2,
			ServerTime = Inc(ref now)
		};

		var result = rule.Process(tradeMsg);
		result.AssertEqual(20);
	}

	public static CommissionManager CreateManager()
	{
		var manager = new CommissionManager();

		var orderRule = new CommissionOrderRule
		{
			Value = new Unit { Value = 10m, Type = UnitTypes.Absolute }
		};
		var tradeRule = new CommissionTradeRule
		{
			Value = new Unit { Value = 15m, Type = UnitTypes.Absolute }
		};

		manager.Rules.Add(orderRule);
		manager.Rules.Add(tradeRule);

		return manager;
	}

	[TestMethod]
	public void ManagerOrderMessage()
	{
		var now = DateTimeOffset.UtcNow;

		var manager = CreateManager();

		// Arrange
		var orderMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderPrice = 100m,
			OrderVolume = 5m,
			ServerTime = Inc(ref now)
		};

		// Act
		var commission = manager.Process(orderMsg);

		// Assert
		commission.AssertEqual(10m);
		manager.Commission.AssertEqual(10m);
	}

	[TestMethod]
	public void ManagerTradeMessage()
	{
		var now = DateTimeOffset.UtcNow;

		var manager = CreateManager();

		// Arrange
		var tradeMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			TradePrice = 200m,
			TradeVolume = 3m,
			ServerTime = Inc(ref now)
		};

		// Act
		var commission = manager.Process(tradeMsg);

		// Assert
		commission.AssertEqual(15m);
		manager.Commission.AssertEqual(15m);
	}

	[TestMethod]
	public void ManagerOrderAndTradeMessages()
	{
		var now = DateTimeOffset.UtcNow;

		var manager = CreateManager();

		// Arrange
		var orderMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderPrice = 100m,
			OrderVolume = 5m,
			ServerTime = Inc(ref now)
		};

		var tradeMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			TradePrice = 200m,
			TradeVolume = 3m,
			ServerTime = Inc(ref now)
		};

		// Act
		manager.Process(orderMsg);
		manager.Process(tradeMsg);

		// Assert
		manager.Commission.AssertEqual(25m); // 10m + 15m
	}

	[TestMethod]
	public void ManagerResetMessage()
	{
		var now = DateTimeOffset.UtcNow;

		var manager = CreateManager();

		// Arrange
		var orderMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderPrice = 100m,
			OrderVolume = 5m,
			ServerTime = Inc(ref now)
		};

		manager.Process(orderMsg);
		manager.Commission.AssertEqual(10m);

		var resetMsg = new ResetMessage();

		// Act
		manager.Process(resetMsg);

		// Assert
		manager.Commission.AssertEqual(0m);
	}

	[TestMethod]
	public void ManagerEmptyRules()
	{
		var now = DateTimeOffset.UtcNow;

		var manager = CreateManager();

		// Arrange
		manager.Rules.Clear();
		var orderMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderPrice = 100m,
			OrderVolume = 5m,
			ServerTime = Inc(ref now)
		};

		// Act
		var commission = manager.Process(orderMsg);

		// Assert
		commission.AssertNull();
		manager.Commission.AssertEqual(0m);
	}

	[TestMethod]
	public void ManagerNonExecutionMessage()
	{
		var manager = CreateManager();

		// Arrange
		var quoteMsg = new QuoteChangeMessage
		{
		};

		// Act
		var commission = manager.Process(quoteMsg);

		// Assert
		commission.AssertNull();
		manager.Commission.AssertEqual(0m);
	}

	[TestMethod]
	public void ManagerSaveLoad()
	{
		var manager = CreateManager();

		// Arrange
		var storage = new SettingsStorage();

		// Act
		manager.Save(storage);

		var newManager = new CommissionManager();
		newManager.Load(storage);

		// Assert
		newManager.Rules.Count.AssertEqual(2);

		var savedOrderRule = newManager.Rules.OfType<CommissionOrderRule>().FirstOrDefault();
		savedOrderRule.AssertNotNull();
		savedOrderRule.Value.Value.AssertEqual(10m);
		savedOrderRule.Value.Type.AssertEqual(UnitTypes.Absolute);

		var savedTradeRule = newManager.Rules.OfType<CommissionTradeRule>().FirstOrDefault();
		savedTradeRule.AssertNotNull();
		savedTradeRule.Value.Value.AssertEqual(15m);
		savedTradeRule.Value.Type.AssertEqual(UnitTypes.Absolute);
	}

	[TestMethod]
	public void ManagerReset()
	{
		var now = DateTimeOffset.UtcNow;

		var manager = CreateManager();

		// Arrange
		var countRule = new CommissionOrderCountRule
		{
			Value = new Unit { Value = 5m, Type = UnitTypes.Absolute },
			Count = 2
		};
		manager.Rules.Add(countRule);

		var orderMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderPrice = 100m,
			OrderVolume = 5m,
			ServerTime = Inc(ref now)
		};

		// Process first order to increase the counter in countRule
		manager.Process(orderMsg);

		// Act
		manager.Reset();

		// Assert
		manager.Commission.AssertEqual(0m);

		// Process one more order, it should not trigger countRule commission yet
		// since the counter should have been reset
		manager.Process(orderMsg);
		var commission = manager.Process(orderMsg);

		// This should be just the orderRule commission (10m) + countRule (5m) that triggered on second order after reset
		commission.AssertEqual(15m);
	}

	[TestMethod]
	public void ProviderDefaultRules()
	{
		ICommissionRuleProvider provider = new InMemoryCommissionRuleProvider();

		// Assert
		var rules = provider.Rules.ToList();
		rules.Count.AssertGreater(0);

		// Verify that common rule types are included
		rules.Contains(typeof(CommissionOrderRule)).AssertTrue();
		rules.Contains(typeof(CommissionTradeRule)).AssertTrue();
		rules.Contains(typeof(CommissionOrderVolumeRule)).AssertTrue();
		rules.Contains(typeof(CommissionTradeVolumeRule)).AssertTrue();
		rules.Contains(typeof(CommissionOrderCountRule)).AssertTrue();
		rules.Contains(typeof(CommissionTradeCountRule)).AssertTrue();
		rules.Contains(typeof(CommissionTradePriceRule)).AssertTrue();
		rules.Contains(typeof(CommissionSecurityIdRule)).AssertTrue();
		rules.Contains(typeof(CommissionSecurityTypeRule)).AssertTrue();
		rules.Contains(typeof(CommissionBoardCodeRule)).AssertTrue();
		rules.Contains(typeof(CommissionTurnOverRule)).AssertTrue();
	}

	[TestMethod]
	public void ProviderAddRule()
	{
		ICommissionRuleProvider provider = new InMemoryCommissionRuleProvider();

		// Arrange
		var customRuleType = typeof(CustomCommissionRule);

		// Act
		provider.AddRule(customRuleType);

		// Assert
		var rules = provider.Rules.ToList();
		rules.Contains(customRuleType).AssertTrue();
	}

	[TestMethod]
	public void ProviderRemoveExistingRule()
	{
		ICommissionRuleProvider provider = new InMemoryCommissionRuleProvider();

		// Arrange
		var ruleType = typeof(CommissionOrderRule);
		provider.Rules.Contains(ruleType).AssertTrue();

		// Act
		provider.RemoveRule(ruleType);

		// Assert
		provider.Rules.Contains(ruleType).AssertFalse();
	}

	[TestMethod]
	public void ProviderRemoveNonExistingRule()
	{
		ICommissionRuleProvider provider = new InMemoryCommissionRuleProvider();

		// Arrange
		var nonExistingRuleType = typeof(CustomCommissionRule);
		provider.Rules.Contains(nonExistingRuleType).AssertFalse();

		// Act
		provider.RemoveRule(nonExistingRuleType);

		// Assert - should not throw exception
		provider.Rules.Contains(nonExistingRuleType).AssertFalse();
	}

	[TestMethod]
	public void RuleSerialization()
	{
		var secProvider = (CollectionSecurityProvider)ServicesRegistry.SecurityProvider;
		var boards = ServicesRegistry.ExchangeInfoProvider.Boards.ToArray();
		ICommissionRuleProvider provider = new InMemoryCommissionRuleProvider();

		// Arrange
		var rules = provider.Rules.ToArray();

		foreach (var type in rules)
		{
			var rule = type.CreateInstance<ICommissionRule>();

			var props = type.GetModifiableProps();

			foreach (var prop in props)
			{
				var propType = prop.PropertyType;
				propType = propType.GetUnderlyingType() ?? prop.PropertyType;

				object value;

				if (propType == typeof(Unit))
					value = new Unit { Value = RandomGen.GetInt(), Type = UnitTypes.Percent };
				else if (propType.IsNumeric())
					value = RandomGen.GetInt().To(propType);
				else if (propType.IsEnum)
					value = RandomGen.GetEnum(propType);
				else if (propType == typeof(SecurityId))
					value = Helper.CreateSecurityId();
				else if (propType == typeof(Security))
				{
					var sec = new Security { Id = Helper.CreateSecurityId().ToStringId() };
					secProvider.Add(sec);
					value = sec;
				}
				else if (propType == typeof(ExchangeBoard))
					value = RandomGen.GetElement(boards);
				else if (propType == typeof(string))
					value = RandomGen.GetString(3, 7);
				else
					throw new InvalidOperationException(propType.FullName);

				prop.SetValue(rule, value);
			}

			// Save
			var storage = rule.Save();

			// Create new instance of the same type
			var restored = type.CreateInstance<ICommissionRule>();
			restored.Load(storage);

			// Compare all public settable properties
			foreach (var prop in props)
			{
				var origValue = prop.GetValue(rule);
				var restoredValue = prop.GetValue(restored);

				origValue.AssertEqual(restoredValue);
			}
		}
	}

	[TestMethod]
	public void PerOrderCountRulePartialFill()
	{
		var now = DateTimeOffset.UtcNow;
		var rule = new CommissionOrderCountRule
		{
			Value = 10m,
			Count = 1
		};

		var orderId = 123L;

		// Order registration (order info only)
		rule.Process(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderId = orderId,
			OrderPrice = 100m,
			OrderVolume = 10m,
			ServerTime = Inc(ref now)
		}).AssertEqual(10m);

		// First partial fill (own trade message with order info present)
		rule.Process(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderId = orderId,
			TradePrice = 100m,
			TradeVolume = 3m,
			ServerTime = Inc(ref now)
		}).AssertNull();

		// Second partial fill for the same order
		rule.Process(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderId = orderId,
			TradePrice = 101m,
			TradeVolume = 7m,
			ServerTime = Inc(ref now)
		}).AssertNull();
	}

	[TestMethod]
	public void PerOrderTradeTurnover()
	{
		var now = DateTimeOffset.UtcNow;
		var rule = new CommissionOrderRule
		{
			Value = new Unit { Value = 1m, Type = UnitTypes.Percent }
		};

		var msg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderType = OrderTypes.Market,
			OrderPrice = 0m,
			OrderVolume = 4m,
			// actual execution info
			TradePrice = 120m,
			TradeVolume = 4m,
			ServerTime = Inc(ref now)
		};

		rule.Process(msg).AssertEqual(4.8m);
	}

	// A custom rule class for testing
	private class CustomCommissionRule : CommissionRule
	{
		public override decimal? Process(ExecutionMessage message)
		{
			return 1.0m; // Simple implementation for testing
		}
	}
}

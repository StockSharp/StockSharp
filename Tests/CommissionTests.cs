namespace StockSharp.Tests;

using System.Collections;

using StockSharp.Algo.Commissions;

[TestClass]
public class CommissionTests : BaseTestClass
{
	private static DateTime Inc(ref DateTime time)
	{
		time = time.AddHours(1);
		return time;
	}

	private static ExecutionMessage CreateOrderMessage(decimal price, decimal volume, DateTime time)
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

	private static ExecutionMessage CreateTradeMessage(decimal price, decimal volume, DateTime time)
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
	public async Task PerOrderRule()
	{
		var now = DateTime.UtcNow;

		// Arrange
		var rule = new CommissionOrderRule
		{
			Value = 10m
		};

		// Act & Assert
		var orderMsg = CreateOrderMessage(100m, 10m, Inc(ref now));
		var result = await rule.ProcessAsync(orderMsg, CancellationToken);
		result.AssertEqual(10m);

		// Test null when not order info
		var tradeMsg = CreateTradeMessage(100m, 10m, Inc(ref now));
		result = await rule.ProcessAsync(tradeMsg, CancellationToken);
		result.AssertNull();

		// Test percent-based commission
		rule.Value = new Unit { Value = 5m, Type = UnitTypes.Percent };
		result = await rule.ProcessAsync(orderMsg, CancellationToken);
		result.AssertEqual(50m); // 5% of 100 * 10 = 50
	}

	[TestMethod]
	public async Task PerTradeRule()
	{
		var now = DateTime.UtcNow;

		// Arrange
		var rule = new CommissionTradeRule
		{
			Value = 15m
		};

		// Act & Assert
		var tradeMsg = CreateTradeMessage(200m, 5m, Inc(ref now));
		var result = await rule.ProcessAsync(tradeMsg, CancellationToken);
		result.AssertEqual(15m);

		// Test null when not trade info
		var orderMsg = CreateOrderMessage(200m, 5m, Inc(ref now));
		result = await rule.ProcessAsync(orderMsg, CancellationToken);
		result.AssertNull();

		// Test percent-based commission
		rule.Value = new Unit { Value = 2.5m, Type = UnitTypes.Percent };
		result = await rule.ProcessAsync(tradeMsg, CancellationToken);
		result.AssertEqual(25m); // 2.5% of 200 * 5 = 25
	}

	[TestMethod]
	public async Task PerOrderVolumeRule()
	{
		var now = DateTime.UtcNow;

		// Arrange
		var rule = new CommissionOrderVolumeRule
		{
			Value = 0.5m
		};

		// Act & Assert
		var orderMsg = CreateOrderMessage(150m, 20m, Inc(ref now));
		var result = await rule.ProcessAsync(orderMsg, CancellationToken);
		result.AssertEqual(10m); // 0.5 * 20 = 10

		// Test null when not order info
		var tradeMsg = CreateTradeMessage(150m, 20m, Inc(ref now));
		result = await rule.ProcessAsync(tradeMsg, CancellationToken);
		result.AssertNull();
	}

	[TestMethod]
	public async Task PerTradeVolumeRule()
	{
		var now = DateTime.UtcNow;

		// Arrange
		var rule = new CommissionTradeVolumeRule
		{
			Value = 0.25m
		};

		// Act & Assert
		var tradeMsg = CreateTradeMessage(300m, 40m, Inc(ref now));
		var result = await rule.ProcessAsync(tradeMsg, CancellationToken);
		result.AssertEqual(10m); // 0.25 * 40 = 10

		// Test null when not trade info
		var orderMsg = CreateOrderMessage(300m, 40m, Inc(ref now));
		result = await rule.ProcessAsync(orderMsg, CancellationToken);
		result.AssertNull();
	}

	[TestMethod]
	public async Task PerOrderCountRule()
	{
		var now = DateTime.UtcNow;

		// Arrange
		var rule = new CommissionOrderCountRule
		{
			Value = 25m,
			Count = 3
		};

		// Act & Assert
		var orderMsg = CreateOrderMessage(100m, 1m, Inc(ref now));

		// First 2 orders should return null
		var result = await rule.ProcessAsync(orderMsg, CancellationToken);
		result.AssertNull();

		result = await rule.ProcessAsync(orderMsg, CancellationToken);
		result.AssertNull();

		// 3rd order should apply commission
		result = await rule.ProcessAsync(orderMsg, CancellationToken);
		result.AssertEqual(25m);

		// 4th order should be null again
		result = await rule.ProcessAsync(orderMsg, CancellationToken);
		result.AssertNull();

		// Test reset functionality
		rule.Reset();

		result = await rule.ProcessAsync(orderMsg, CancellationToken);
		result.AssertNull();

		// Test null when not order info
		var tradeMsg = CreateTradeMessage(100m, 1m, Inc(ref now));
		result = await rule.ProcessAsync(tradeMsg, CancellationToken);
		result.AssertNull();
	}

	[TestMethod]
	public async Task PerTradeCountRule()
	{
		var now = DateTime.UtcNow;

		// Arrange
		var rule = new CommissionTradeCountRule
		{
			Value = 30m,
			Count = 2
		};

		// Act & Assert
		var tradeMsg = CreateTradeMessage(200m, 1m, Inc(ref now));

		// First order should return null
		var result = await rule.ProcessAsync(tradeMsg, CancellationToken);
		result.AssertNull();

		// 2nd order should apply commission
		result = await rule.ProcessAsync(tradeMsg, CancellationToken);
		result.AssertEqual(30m);

		// 3rd order should be null again
		result = await rule.ProcessAsync(tradeMsg, CancellationToken);
		result.AssertNull();

		// Test reset functionality
		rule.Reset();

		result = await rule.ProcessAsync(tradeMsg, CancellationToken);
		result.AssertNull();

		// Test null when not trade info
		var orderMsg = CreateOrderMessage(200m, 1m, Inc(ref now));
		result = await rule.ProcessAsync(orderMsg, CancellationToken);
		result.AssertNull();
	}

	[TestMethod]
	public async Task PerTradePriceRule()
	{
		var now = DateTime.UtcNow;

		// Arrange
		var rule = new CommissionTradePriceRule
		{
			Value = 0.01m
		};

		// Act & Assert
		var tradeMsg = CreateTradeMessage(100m, 5m, Inc(ref now));
		var result = await rule.ProcessAsync(tradeMsg, CancellationToken);
		result.AssertEqual(5m); // 100 * 5 * 0.01 = 5

		// Test null when not trade info
		var orderMsg = CreateOrderMessage(100m, 5m, Inc(ref now));
		result = await rule.ProcessAsync(orderMsg, CancellationToken);
		result.AssertNull();
	}

	[TestMethod]
	public async Task SecurityIdRule_SurvivesSaveAndLoad()
	{
		var securityId = new SecurityId { SecurityCode = "AAPL", BoardCode = BoardCodes.Nasdaq };

		var storage = await new CommissionSecurityIdRule { Value = 10m, SecurityId = securityId }.SaveAsync(CancellationToken);
		var restored = await storage.LoadAsync<CommissionSecurityIdRule>(CancellationToken);

		restored.SecurityId.AssertEqual(securityId);

		(await restored.ProcessAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = securityId,
			TradePrice = 150m,
			TradeVolume = 2,
			ServerTime = DateTime.UtcNow,
		}, CancellationToken)).AssertEqual(10m);
	}

	[TestMethod]
	public async Task SecurityIdRule()
	{
		var now = DateTime.UtcNow;

		// Arrange
		var securityId = new SecurityId { SecurityCode = "AAPL", BoardCode = BoardCodes.Nasdaq };

		var rule = new CommissionSecurityIdRule
		{
			Value = 10m,
			SecurityId = securityId
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

		var result = await rule.ProcessAsync(tradeMsg, CancellationToken);
		result.AssertEqual(10m);

		// Test null for different security ID
		var differentSecurityMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = new() { SecurityCode = "MSFT", BoardCode = BoardCodes.Nasdaq },
			TradePrice = 150m,
			ServerTime = Inc(ref now)
		};

		result = await rule.ProcessAsync(differentSecurityMsg, CancellationToken);
		result.AssertNull();

		// Test percent-based commission
		rule.Value = new Unit { Value = 1m, Type = UnitTypes.Percent };
		result = await rule.ProcessAsync(tradeMsg, CancellationToken);
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

		result = await rule.ProcessAsync(orderMsg, CancellationToken);
		result.AssertNull();
	}

	[TestMethod]
	public async Task BoardCodeRule()
	{
		var now = DateTime.UtcNow;

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

		var result = await rule.ProcessAsync(tradeMsg, CancellationToken);
		result.AssertEqual(15m);

		// Test null for different board
		var differentBoardMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = new() { BoardCode = "NYSE" },
			TradePrice = 200m,
			ServerTime = Inc(ref now)
		};

		result = await rule.ProcessAsync(differentBoardMsg, CancellationToken);
		result.AssertNull();

		// Test percent-based commission
		rule.Value = new() { Value = 2m, Type = UnitTypes.Percent };
		result = await rule.ProcessAsync(tradeMsg, CancellationToken);
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

		result = await rule.ProcessAsync(orderMsg, CancellationToken);
		result.AssertNull();
	}

	[TestMethod]
	public async Task TurnOverRule()
	{
		var now = DateTime.UtcNow;

		// Arrange
		var rule = new CommissionTurnOverRule
		{
			Value = 50m,
			TurnOver = 1000m
		};

		// Act & Assert
		var tradeMsg1 = CreateTradeMessage(100m, 5m, Inc(ref now)); // 500
		var result = await rule.ProcessAsync(tradeMsg1, CancellationToken);
		result.AssertNull(); // Turnover not reached yet

		var tradeMsg2 = CreateTradeMessage(200m, 3m, Inc(ref now)); // 600 (total 1100)
		result = await rule.ProcessAsync(tradeMsg2, CancellationToken);
		result.AssertEqual(50m); // Turnover reached

		// Test reset functionality
		rule.Reset();

		var tradeMsg3 = CreateTradeMessage(100m, 5m, Inc(ref now)); // 500
		result = await rule.ProcessAsync(tradeMsg3, CancellationToken);
		result.AssertNull(); // Turnover not reached after reset

		// Test null when not trade info
		var orderMsg = CreateOrderMessage(100m, 5m, Inc(ref now));
		result = await rule.ProcessAsync(orderMsg, CancellationToken);
		result.AssertNull();
	}

	[TestMethod]
	public async Task SecurityTypeRule_UnknownSecurity_ReturnsNull()
	{
		var now = DateTime.UtcNow;

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

		var result = await rule.ProcessAsync(tradeMsg, CancellationToken);
		result.AssertNull();
	}

	[TestMethod]
	public async Task SecurityTypeRuleSecProvider()
	{
		var now = DateTime.UtcNow;

		// The rule reads the type off the security the process-wide registry answers with, so the
		// security has to be in there - under an id of its own, and only while this test runs.
		var secId = Helper.CreateSecurityId();
		var appl = new Security { Id = secId.ToStringId(), Type = SecurityTypes.Stock };

		var provider = (CollectionSecurityProvider)ServicesRegistry.SecurityProvider;
		provider.Add(appl);

		try
		{
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

			var result = await rule.ProcessAsync(tradeMsg, CancellationToken);
			result.AssertEqual(20);
		}
		finally
		{
			provider.Remove(appl);
		}

		(await provider.LookupByIdAsync(secId, CancellationToken)).AssertNull($"{secId} was left in the provider the whole assembly shares");
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
	public async Task ManagerOrderMessage()
	{
		var now = DateTime.UtcNow;

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
		var commission = await manager.ProcessAsync(orderMsg, CancellationToken);

		// Assert
		commission.AssertEqual(10m);
		manager.Commission.AssertEqual(10m);
	}

	[TestMethod]
	public async Task ManagerTradeMessage()
	{
		var now = DateTime.UtcNow;

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
		var commission = await manager.ProcessAsync(tradeMsg, CancellationToken);

		// Assert
		commission.AssertEqual(15m);
		manager.Commission.AssertEqual(15m);
	}

	[TestMethod]
	public async Task ManagerOrderAndTradeMessages()
	{
		var now = DateTime.UtcNow;

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
		await manager.ProcessAsync(orderMsg, CancellationToken);
		await manager.ProcessAsync(tradeMsg, CancellationToken);

		// Assert
		manager.Commission.AssertEqual(25m); // 10m + 15m
	}

	[TestMethod]
	public async Task ManagerResetMessage()
	{
		var now = DateTime.UtcNow;

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

		await manager.ProcessAsync(orderMsg, CancellationToken);
		manager.Commission.AssertEqual(10m);

		var resetMsg = new ResetMessage();

		// Act
		await manager.ProcessAsync(resetMsg, CancellationToken);

		// Assert
		manager.Commission.AssertEqual(0m);
	}

	[TestMethod]
	public async Task ManagerEmptyRules()
	{
		var now = DateTime.UtcNow;

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
		var commission = await manager.ProcessAsync(orderMsg, CancellationToken);

		// Assert
		commission.AssertNull();
		manager.Commission.AssertEqual(0m);
	}

	[TestMethod]
	public async Task ManagerNonExecutionMessage()
	{
		var manager = CreateManager();

		// Arrange
		var quoteMsg = new QuoteChangeMessage
		{
		};

		// Act
		var commission = await manager.ProcessAsync(quoteMsg, CancellationToken);

		// Assert
		commission.AssertNull();
		manager.Commission.AssertEqual(0m);
	}

	[TestMethod]
	public async Task ManagerSaveLoad()
	{
		var manager = CreateManager();

		// Arrange
		var storage = new SettingsStorage();

		// Act
		await manager.SaveAsync(storage, CancellationToken);

		var newManager = new CommissionManager();
		await newManager.LoadAsync(storage, CancellationToken);

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
	public async Task ManagerReset()
	{
		var now = DateTime.UtcNow;

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
		await manager.ProcessAsync(orderMsg, CancellationToken);

		// Act
		manager.Reset();

		// Assert
		manager.Commission.AssertEqual(0m);

		// Process one more order, it should not trigger countRule commission yet
		// since the counter should have been reset
		await manager.ProcessAsync(orderMsg, CancellationToken);
		var commission = await manager.ProcessAsync(orderMsg, CancellationToken);

		// This should be just the orderRule commission (10m) + countRule (5m) that triggered on second order after reset
		commission.AssertEqual(15m);
	}

	[TestMethod]
	public void ProviderDefaultRules()
	{
		ICommissionRuleProvider provider = new InMemoryCommissionRuleProvider();

		// Assert
		var rules = provider.All.ToList();
		rules.Count.AssertEqual(11);

		// Verify that common rule types are included
		rules.Count(t => t == typeof(CommissionOrderRule)).AssertEqual(1);
		rules.Count(t => t == typeof(CommissionTradeRule)).AssertEqual(1);
		rules.Count(t => t == typeof(CommissionOrderVolumeRule)).AssertEqual(1);
		rules.Count(t => t == typeof(CommissionTradeVolumeRule)).AssertEqual(1);
		rules.Count(t => t == typeof(CommissionOrderCountRule)).AssertEqual(1);
		rules.Count(t => t == typeof(CommissionTradeCountRule)).AssertEqual(1);
		rules.Count(t => t == typeof(CommissionTradePriceRule)).AssertEqual(1);
		rules.Count(t => t == typeof(CommissionSecurityIdRule)).AssertEqual(1);
		rules.Count(t => t == typeof(CommissionSecurityTypeRule)).AssertEqual(1);
		rules.Count(t => t == typeof(CommissionBoardCodeRule)).AssertEqual(1);
		rules.Count(t => t == typeof(CommissionTurnOverRule)).AssertEqual(1);
	}

	[TestMethod]
	public void ProviderAddRule()
	{
		ICommissionRuleProvider provider = new InMemoryCommissionRuleProvider();

		// Arrange
		var customRuleType = typeof(CustomCommissionRule);

		// Act
		provider.Add(customRuleType);

		// Assert
		var rules = provider.All.ToList();
		rules.Count(t => t == customRuleType).AssertEqual(1);
	}

	[TestMethod]
	public void ProviderRemoveExistingRule()
	{
		ICommissionRuleProvider provider = new InMemoryCommissionRuleProvider();

		// Arrange
		var ruleType = typeof(CommissionOrderRule);
		provider.All.Count(t => t == ruleType).AssertEqual(1);

		// Act
		provider.Remove(ruleType);

		// Assert
		provider.All.Count(t => t == ruleType).AssertEqual(0);
	}

	[TestMethod]
	public void ProviderRemoveNonExistingRule()
	{
		ICommissionRuleProvider provider = new InMemoryCommissionRuleProvider();

		// Arrange
		var nonExistingRuleType = typeof(CustomCommissionRule);
		provider.All.Count(t => t == nonExistingRuleType).AssertEqual(0);

		// Act
		provider.Remove(nonExistingRuleType);

		// Assert - should not throw exception
		provider.All.Count(t => t == nonExistingRuleType).AssertEqual(0);
	}

	[TestMethod]
	public async Task RuleSerialization()
	{
		// A rule holding a security saves its id and reads the security back through the process-wide
		// registry, so every security this test invents has to be in there while it runs.
		var secProvider = (CollectionSecurityProvider)ServicesRegistry.SecurityProvider;
		var added = new List<Security>();
		var boards = ServicesRegistry.ExchangeInfoProvider.Boards.ToArray();
		ICommissionRuleProvider provider = new InMemoryCommissionRuleProvider();

		// Arrange
		var rules = provider.All.ToArray();

		try
		{
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
						added.Add(sec);
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
				var storage = await rule.SaveAsync(CancellationToken);

				// Create new instance of the same type
				var restored = type.CreateInstance<ICommissionRule>();
				await restored.LoadAsync(storage, CancellationToken);

				// Compare all public settable properties
				foreach (var prop in props)
				{
					var origValue = prop.GetValue(rule);
					var restoredValue = prop.GetValue(restored);

					origValue.AssertEqual(restoredValue);
				}
			}
		}
		finally
		{
			foreach (var sec in added)
				secProvider.Remove(sec);
		}

		var left = 0;

		foreach (var sec in added)
		{
			if (await secProvider.LookupByIdAsync(sec.ToSecurityId(), CancellationToken) is not null)
				left++;
		}

		left.AssertEqual(0, "securities were left in the provider the whole assembly shares");
	}

	[TestMethod]
	public async Task PerOrderCountRulePartialFill()
	{
		var now = DateTime.UtcNow;
		var rule = new CommissionOrderCountRule
		{
			Value = 10m,
			Count = 1
		};

		var orderId = 123L;

		// Order registration (order info only)
		(await rule.ProcessAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderId = orderId,
			OrderPrice = 100m,
			OrderVolume = 10m,
			ServerTime = Inc(ref now)
		}, CancellationToken)).AssertEqual(10m);

		// First partial fill (own trade message with order info present)
		(await rule.ProcessAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderId = orderId,
			TradePrice = 100m,
			TradeVolume = 3m,
			ServerTime = Inc(ref now)
		}, CancellationToken)).AssertNull();

		// Second partial fill for the same order
		(await rule.ProcessAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderId = orderId,
			TradePrice = 101m,
			TradeVolume = 7m,
			ServerTime = Inc(ref now)
		}, CancellationToken)).AssertNull();
	}

	[TestMethod]
	public async Task PerOrderCountRuleChargesOncePerOrderAcrossSeparateMessages()
	{
		// The emulator reports one order as several messages - registration, then a standalone
		// trade, then a balance update, then the final state. One order is charged once.
		var now = DateTime.UtcNow;

		var rule = new CommissionOrderCountRule
		{
			Value = 10m,
			Count = 1
		};

		var transId = 1L;
		var orderId = 111L;

		// Registration acknowledged.
		(await rule.ProcessAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OriginalTransactionId = transId,
			OrderId = orderId,
			OrderPrice = 100m,
			OrderVolume = 10m,
			Balance = 10m,
			OrderState = OrderStates.Active,
			ServerTime = Inc(ref now)
		}, CancellationToken)).AssertEqual(10m);

		// Own trade delivered on its own, without order info.
		(await rule.ProcessAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = transId,
			OrderId = orderId,
			TradeId = 1001L,
			TradePrice = 100m,
			TradeVolume = 4m,
			ServerTime = Inc(ref now)
		}, CancellationToken)).AssertNull("a trade is not a new order");

		// Balance update for the very same order.
		(await rule.ProcessAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OriginalTransactionId = transId,
			OrderId = orderId,
			OrderPrice = 100m,
			OrderVolume = 10m,
			Balance = 6m,
			OrderState = OrderStates.Active,
			ServerTime = Inc(ref now)
		}, CancellationToken)).AssertNull("a balance update is not a new order");

		// Final state of the very same order.
		(await rule.ProcessAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OriginalTransactionId = transId,
			OrderId = orderId,
			OrderPrice = 100m,
			OrderVolume = 10m,
			Balance = 0m,
			OrderState = OrderStates.Done,
			ServerTime = Inc(ref now)
		}, CancellationToken)).AssertNull("a final state is not a new order");
	}

	[TestMethod]
	public async Task PerOrderCountRuleCountsOrdersNotNotifications()
	{
		// Two orders reported in interleaved messages: the charge lands on the second distinct
		// order, not on the second message about the first one.
		var now = DateTime.UtcNow;

		var rule = new CommissionOrderCountRule
		{
			Value = 10m,
			Count = 2
		};

		// First order registered.
		(await rule.ProcessAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OriginalTransactionId = 1L,
			OrderId = 111L,
			OrderPrice = 100m,
			OrderVolume = 10m,
			Balance = 10m,
			OrderState = OrderStates.Active,
			ServerTime = Inc(ref now)
		}, CancellationToken)).AssertNull();

		// Balance update for the first order - still one order seen.
		(await rule.ProcessAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OriginalTransactionId = 1L,
			OrderId = 111L,
			OrderPrice = 100m,
			OrderVolume = 10m,
			Balance = 4m,
			OrderState = OrderStates.Active,
			ServerTime = Inc(ref now)
		}, CancellationToken)).AssertNull("a second message about the first order is not a second order");

		// Second order registered - the second order completes the pair.
		(await rule.ProcessAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OriginalTransactionId = 2L,
			OrderId = 222L,
			OrderPrice = 200m,
			OrderVolume = 5m,
			Balance = 5m,
			OrderState = OrderStates.Active,
			ServerTime = Inc(ref now)
		}, CancellationToken)).AssertEqual(10m);

		// Trades and final states of both orders bring no further orders.
		(await rule.ProcessAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 2L,
			OrderId = 222L,
			TradeId = 1002L,
			TradePrice = 200m,
			TradeVolume = 5m,
			ServerTime = Inc(ref now)
		}, CancellationToken)).AssertNull();

		(await rule.ProcessAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OriginalTransactionId = 1L,
			OrderId = 111L,
			OrderPrice = 100m,
			OrderVolume = 10m,
			Balance = 0m,
			OrderState = OrderStates.Done,
			ServerTime = Inc(ref now)
		}, CancellationToken)).AssertNull();

		(await rule.ProcessAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OriginalTransactionId = 2L,
			OrderId = 222L,
			OrderPrice = 200m,
			OrderVolume = 5m,
			Balance = 0m,
			OrderState = OrderStates.Done,
			ServerTime = Inc(ref now)
		}, CancellationToken)).AssertNull();

		// A third distinct order starts the next pair.
		(await rule.ProcessAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OriginalTransactionId = 3L,
			OrderId = 333L,
			OrderPrice = 300m,
			OrderVolume = 1m,
			Balance = 1m,
			OrderState = OrderStates.Active,
			ServerTime = Inc(ref now)
		}, CancellationToken)).AssertNull();
	}

	[TestMethod]
	public async Task PerOrderTradeTurnover()
	{
		var now = DateTime.UtcNow;
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

		(await rule.ProcessAsync(msg, CancellationToken)).AssertEqual(4.8m);
	}

	[TestMethod]
	public async Task TurnOverRuleRepeatedTrigger()
	{
		var now = DateTime.UtcNow;

		// Arrange
		var rule = new CommissionTurnOverRule
		{
			Value = 50m,
			TurnOver = 1000m
		};

		// Act & Assert
		// First trade: 500 turnover (below threshold)
		var tradeMsg1 = CreateTradeMessage(100m, 5m, Inc(ref now));
		var result = await rule.ProcessAsync(tradeMsg1, CancellationToken);
		result.AssertNull(); // Turnover not reached yet

		// Second trade: +600 = 1100 total (above threshold)
		var tradeMsg2 = CreateTradeMessage(200m, 3m, Inc(ref now));
		result = await rule.ProcessAsync(tradeMsg2, CancellationToken);
		result.AssertEqual(50m); // Turnover reached, commission applied

		// After commission application the accumulated turnover is decreased by the threshold (1000),
		// so the remainder is100. Next small trade won't reach the threshold again and should return null.
		var tradeMsg3 = CreateTradeMessage(100m, 1m, Inc(ref now));
		result = await rule.ProcessAsync(tradeMsg3, CancellationToken);
		// Expected: null (need1000 more turnover)
		result.AssertNull();
	}

	[TestMethod]
	public async Task PerOrderVolumeRuleWithPercent()
	{
		var now = DateTime.UtcNow;

		// Arrange
		var rule = new CommissionOrderVolumeRule
		{
			Value = new Unit { Value = 5m, Type = UnitTypes.Percent }
		};

		// Act & Assert
		var orderMsg = CreateOrderMessage(100m, 20m, Inc(ref now));
		var result = await rule.ProcessAsync(orderMsg, CancellationToken);
		result.AssertEqual(100m);

		rule.Value = new Unit { Value = 10m, Type = UnitTypes.Percent };
		result = await rule.ProcessAsync(orderMsg, CancellationToken);
		result.AssertEqual(200m);

		// A different price distinguishes turnover percentage from volume multiplied by the raw percent.
		var orderMsg2 = CreateOrderMessage(50m, 10m, Inc(ref now));
		result = await rule.ProcessAsync(orderMsg2, CancellationToken);
		result.AssertEqual(50m);
	}

	/// <summary>
	/// A percent rule charges a share of the turnover, and a market order states no price, so until
	/// it fills there is no turnover to take a share of. The rule's contract has a way to say that -
	/// no value - and saying zero instead is a different statement altogether: it tells whoever asked
	/// that this order costs nothing, and that answer is carried on as a fact. A trader reading the
	/// order sees a commission of zero where the broker will take a real one, and a manager summing
	/// the rules turns "not known" into "known to be nothing" for every other rule in the set.
	/// </summary>
	[TestMethod]
	public async Task APercentRuleChargesNothingItCannotWorkOutOnAPricelessOrder()
	{
		var now = DateTime.UtcNow;

		var rule = new CommissionOrderVolumeRule
		{
			Value = new Unit { Value = 5m, Type = UnitTypes.Percent }
		};

		// A market order at registration: volume is known, price is not, and nothing has traded yet.
		var marketOrder = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderType = OrderTypes.Market,
			OrderPrice = 0m,
			OrderVolume = 20m,
			ServerTime = Inc(ref now)
		};

		(await rule.ProcessAsync(marketOrder, CancellationToken)).AssertNull("a share of a turnover nobody knows yet is not zero, it is unknown");

		// Once the order fills the turnover is known, and the same rule charges its share of it.
		var filled = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderType = OrderTypes.Market,
			OrderPrice = 0m,
			OrderVolume = 20m,
			TradePrice = 110m,
			TradeVolume = 20m,
			ServerTime = Inc(ref now)
		};

		(await rule.ProcessAsync(filled, CancellationToken)).AssertEqual(110m);
	}

	[TestMethod]
	public async Task PerTradeVolumeRuleWithPercent()
	{
		var now = DateTime.UtcNow;

		// Arrange
		var rule = new CommissionTradeVolumeRule
		{
			Value = new Unit { Value = 10m, Type = UnitTypes.Percent }
		};

		// Act & Assert
		var tradeMsg = CreateTradeMessage(50m, 10m, Inc(ref now));
		var result = await rule.ProcessAsync(tradeMsg, CancellationToken);

		// Expected (with GetValue): (50 * 10 * 10) / 100 = 50
		result.AssertEqual(50m);
	}

	[TestMethod]
	public async Task PerTradePriceRuleWithPercent()
	{
		var now = DateTime.UtcNow;

		// Arrange
		var rule = new CommissionTradePriceRule
		{
			Value = new Unit { Value = 10m, Type = UnitTypes.Percent }
		};

		// Act & Assert
		var tradeMsg = CreateTradeMessage(100m, 5m, Inc(ref now));
		var result = await rule.ProcessAsync(tradeMsg, CancellationToken);

		// Expected (with GetValue): (100 * 5 * 10) / 100 = 50
		result.AssertEqual(50m);
	}

	[TestMethod]
	public async Task PerOrderVolumeRuleWithNullValues()
	{
		var now = DateTime.UtcNow;

		// Test with absolute value commission
		var rule = new CommissionOrderVolumeRule
		{
			Value = 0.5m
		};

		// Test with null OrderVolume
		var orderMsgNullVolume = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderPrice = 100m,
			OrderVolume = null,
			ServerTime = Inc(ref now)
		};
		var result = await rule.ProcessAsync(orderMsgNullVolume, CancellationToken);
		result.AssertNull();

		// Test with percent-based commission and null volume
		rule.Value = new Unit { Value = 10m, Type = UnitTypes.Percent };
		result = await rule.ProcessAsync(orderMsgNullVolume, CancellationToken);
		result.AssertNull(); // percent commission needs volume for turnover calculation
	}

	[TestMethod]
	public async Task MinRaisesACharge()
	{
		// A tariff quoted as a percentage still charges a floor, so a trade too small to reach it
		// is not carried for free.
		var now = DateTime.UtcNow;

		var rule = new CommissionTradeVolumeRule
		{
			Value = new Unit { Value = 0.25m, Type = UnitTypes.Percent },
			Min = 1m,
		};

		// 100 * 1 * 0.25% = 0.25, under the floor.
		(await rule.ProcessAsync(CreateTradeMessage(100m, 1m, Inc(ref now)), CancellationToken)).AssertEqual(1m);

		// 100 * 100 * 0.25% = 25, over it.
		(await rule.ProcessAsync(CreateTradeMessage(100m, 100m, Inc(ref now)), CancellationToken)).AssertEqual(25m);
	}

	[TestMethod]
	public async Task MinDoesNotInventACharge()
	{
		// The floor bounds a charge; it does not create one where the rule found nothing to
		// charge for, and it leaves a rebate alone when no floor is set.
		var now = DateTime.UtcNow;

		var rule = new CommissionTradeVolumeRule
		{
			Value = new Unit { Value = 1m, Type = UnitTypes.Absolute },
			Min = 5m,
		};

		(await rule.ProcessAsync(CreateOrderMessage(100m, 1m, Inc(ref now)), CancellationToken)).AssertNull("a trade rule has nothing to say about an order");

		var rebate = new CommissionTradeVolumeRule
		{
			Value = new Unit { Value = -0.5m, Type = UnitTypes.Absolute },
		};

		(await rebate.ProcessAsync(CreateTradeMessage(100m, 2m, Inc(ref now)), CancellationToken)).AssertEqual(-1m);
	}

	[TestMethod]
	public async Task MinSurvivesSaveAndLoad()
	{
		var rule = new CommissionTradeVolumeRule { Value = new Unit { Value = 2m }, Min = 3m };

		var storage = new SettingsStorage();
		await rule.SaveAsync(storage, CancellationToken);

		var restored = new CommissionTradeVolumeRule();
		await restored.LoadAsync(storage, CancellationToken);

		restored.Min.AssertEqual(3m);
	}

	[TestMethod]
	public void ANegativeMinIsRefused()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new CommissionTradeVolumeRule { Min = -1m });
	}

	// A custom rule class for testing
	private class CustomCommissionRule : CommissionRule
	{
		protected override ValueTask<decimal?> OnProcessAsync(ExecutionMessage message, CancellationToken cancellationToken)
			=> new(1.0m);
	}

	/// <summary>
	/// The rule charges an order once however many times the venue reports it, and remembers which
	/// orders it has charged in order to do so. That memory is only ever cleared on reset, and a
	/// connection resets when it is torn down - so on a stand that runs for weeks it would hold every
	/// order the venue ever reported. What it holds has to stop growing, whatever the session does.
	/// </summary>
	[TestMethod]
	public async Task WhatTheCountRuleRemembersStopsGrowingHoweverLongTheSessionRuns()
	{
		var rule = new CommissionOrderCountRule { Value = 1m, Count = 1 };

		var now = DateTime.UtcNow;
		var next = 0;

		async Task Orders(int count)
		{
			for (var i = 0; i < count; i++)
			{
				var id = ++next;
				var msg = CreateOrderMessage(100m, 1m, Inc(ref now));

				// Every name a venue may know an order by, since each is remembered separately.
				msg.OriginalTransactionId = id;
				msg.OrderId = id;
				msg.OrderStringId = id.To<string>();

				await rule.ProcessAsync(msg, CancellationToken);
			}
		}

		static int Held(CommissionOrderCountRule rule)
			=> typeof(CommissionOrderCountRule)
				.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
				.Where(f => typeof(ICollection).IsAssignableFrom(f.FieldType))
				.Sum(f => ((ICollection)f.GetValue(rule)).Count);

		const int batch = 20_000;

		await Orders(batch);
		var afterOne = Held(rule);

		await Orders(batch);
		var afterTwo = Held(rule);

		afterTwo.AssertEqual(afterOne,
			$"the rule held {afterOne} entries after {batch} orders and {afterTwo} after {batch * 2}, so what it remembers grows with the session rather than being bounded");
	}
}

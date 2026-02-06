namespace StockSharp.Tests;

using System.ComponentModel;

using StockSharp.Algo.Risk;
using StockSharp.Algo.Testing;
using StockSharp.Algo.Testing.Emulation;

[TestClass]
public class RiskTests : BaseTestClass
{
	private static readonly string _pfName = Helper.CreatePortfolio().Name;

	[TestMethod]
	public void ManagerAddRemoveRules()
	{
		var manager = new RiskManager();

		var rule1 = new RiskPnLRule { PnL = new() { Value = 1000, Type = UnitTypes.Absolute } };
		var rule2 = new RiskPositionSizeRule { Position = 100 };

		manager.Rules.Add(rule1);
		manager.Rules.Add(rule2);

		manager.Rules.Count.AssertEqual(2);
		manager.Rules.Count(r => r == rule1).AssertEqual(1);
		manager.Rules.Count(r => r == rule2).AssertEqual(1);

		manager.Rules.Remove(rule1);
		manager.Rules.Count.AssertEqual(1);

		manager.Rules.Clear();
		manager.Rules.Count.AssertEqual(0);
	}

	[TestMethod]
	public void ManagerReset()
	{
		var manager = new RiskManager();
		var rule = new TestRiskRule();

		manager.Rules.Add(rule);
		rule.IsReset.AssertFalse();

		manager.Reset();
		rule.IsReset.AssertTrue();
	}

	[TestMethod]
	public void ManagerProcessRules()
	{
		var manager = new RiskManager();
		var rule1 = new TestRiskRule { ShouldActivate = true };
		var rule2 = new TestRiskRule { ShouldActivate = false };

		manager.Rules.Add(rule1);
		manager.Rules.Add(rule2);

		var message = new TimeMessage { LocalTime = DateTime.UtcNow };
		var activatedRules = manager.ProcessRules(message).ToArray();

		activatedRules.Length.AssertEqual(1);
		activatedRules[0].AssertEqual(rule1);

		rule1.LastMessage.AssertEqual(message);
		rule2.LastMessage.AssertEqual(message);
	}

	[TestMethod]
	public void ManagerProcessResetMessage()
	{
		var manager = new RiskManager();
		var rule = new TestRiskRule();

		manager.Rules.Add(rule);
		rule.IsReset.AssertFalse();

		var resetMessage = new ResetMessage();
		var activatedRules = manager.ProcessRules(resetMessage);

		activatedRules.Count().AssertEqual(0);
		rule.IsReset.AssertTrue();
	}

	[TestMethod]
	public void ManagerSaveLoad()
	{
		var manager = new RiskManager();

		var pnlRule = new RiskPnLRule
		{
			PnL = new() { Value = 1000, Type = UnitTypes.Absolute },
			Action = RiskActions.ClosePositions
		};

		var positionRule = new RiskPositionSizeRule
		{
			Position = 100,
			Action = RiskActions.StopTrading
		};

		manager.Rules.Add(pnlRule);
		manager.Rules.Add(positionRule);

		var storage = new SettingsStorage();
		manager.Save(storage);

		var newManager = new RiskManager();
		newManager.Load(storage);

		newManager.Rules.Count.AssertEqual(2);

		var savedPnlRule = newManager.Rules.OfType<RiskPnLRule>().FirstOrDefault();
		savedPnlRule.AssertNotNull();
		savedPnlRule.PnL.Value.AssertEqual(1000);
		savedPnlRule.PnL.Type.AssertEqual(UnitTypes.Absolute);
		savedPnlRule.Action.AssertEqual(RiskActions.ClosePositions);

		var savedPositionRule = newManager.Rules.OfType<RiskPositionSizeRule>().FirstOrDefault();
		savedPositionRule.AssertNotNull();
		savedPositionRule.Position.AssertEqual(100);
		savedPositionRule.Action.AssertEqual(RiskActions.StopTrading);
	}

	[TestMethod]
	public void PnLAbsolute()
	{
		var rule = new RiskPnLRule
		{
			PnL = new() { Value = -1000, Type = UnitTypes.Absolute },
			Action = RiskActions.ClosePositions
		};

		var positionMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, -500m);

		rule.ProcessMessage(positionMsg).AssertFalse();

		positionMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, -1500m);

		rule.ProcessMessage(positionMsg).AssertTrue();
	}

	[TestMethod]
	public void PnLLimit()
	{
		var rule = new RiskPnLRule
		{
			PnL = new() { Value = -1000, Type = UnitTypes.Absolute },
			Action = RiskActions.ClosePositions
		};

		var positionMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, -500m);

		rule.ProcessMessage(positionMsg).AssertFalse();

		positionMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, -1500m);

		rule.ProcessMessage(positionMsg).AssertTrue();
	}

	[TestMethod]
	public void PnLReset()
	{
		var rule = new RiskPnLRule
		{
			PnL = new() { Value = -1000, Type = UnitTypes.Absolute }
		};

		var positionMsg = new PositionChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, 0m);

		rule.ProcessMessage(positionMsg);

		rule.Reset();

		rule.ProcessMessage(positionMsg).AssertFalse();
	}

	[TestMethod]
	public void PnLZeroLimit()
	{
		// Test for PnL.Value == 0 with Limit type
		// Verifies that RiskPnLRule correctly handles zero threshold
		// Fixed in RiskPnLRule.cs:77-78 to explicitly return false when PnL == 0
		var rule = new RiskPnLRule
		{
			PnL = new() { Value = 0, Type = UnitTypes.Absolute },
			Action = RiskActions.ClosePositions
		};

		// Test 1: currValue = 0
		// Should NOT activate - zero threshold means rule is effectively disabled
		var positionMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, 0m);

		// Rule correctly does not activate for zero threshold
		rule.ProcessMessage(positionMsg).AssertFalse();

		// Test 2: currValue = -100
		// Should NOT activate - zero threshold means no risk limit is set
		positionMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, -100m);

		// Rule correctly does not activate
		rule.ProcessMessage(positionMsg).AssertFalse();

		// Test 3: currValue = 100 (positive value)
		// Should NOT activate - zero threshold means no limit
		positionMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, 100m);

		// Rule correctly does not activate
		rule.ProcessMessage(positionMsg).AssertFalse();
	}

	[TestMethod]
	public void PnLZeroAbsolute()
	{
		// Test for PnL.Value == 0 with Absolute type
		// Verifies that RiskPnLRule correctly handles zero threshold for absolute PnL changes
		// Fixed in RiskPnLRule.cs:85-86 to explicitly return false when PnL == 0
		var rule = new RiskPnLRule
		{
			PnL = new() { Value = 0, Type = UnitTypes.Absolute },
			Action = RiskActions.ClosePositions
		};

		// Initialize with starting value of 1000
		var positionMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, 1000m);

		// First message sets _initValue = 1000, should not activate
		rule.ProcessMessage(positionMsg).AssertFalse();

		// Test 1: currValue = 1000 (no change)
		// Should NOT activate - zero threshold means no risk limit
		positionMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, 1000m);

		// Rule correctly does not activate when threshold is zero
		rule.ProcessMessage(positionMsg).AssertFalse();

		// Test 2: currValue = 900 (loss of 100)
		// Should NOT activate - zero threshold means rule is effectively disabled
		positionMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, 900m);

		// Rule correctly does not activate
		rule.ProcessMessage(positionMsg).AssertFalse();

		// Test 3: currValue = 1100 (profit of 100)
		// Should NOT activate - zero threshold means no limit
		positionMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, 1100m);

		// Rule correctly does not activate
		rule.ProcessMessage(positionMsg).AssertFalse();
	}

	[TestMethod]
	public void PositionSize()
	{
		var rule = new RiskPositionSizeRule
		{
			Position = 100,
			Action = RiskActions.CancelOrders
		};

		var positionMsg = new PositionChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, 50m);

		rule.ProcessMessage(positionMsg).AssertFalse();

		positionMsg = new PositionChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, 150m);

		rule.ProcessMessage(positionMsg).AssertTrue();
	}

	[TestMethod]
	public void PositionSizeNegative()
	{
		var rule = new RiskPositionSizeRule
		{
			Position = -100,
			Action = RiskActions.CancelOrders
		};

		var positionMsg = new PositionChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, -50m);

		rule.ProcessMessage(positionMsg).AssertFalse();

		positionMsg = new PositionChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, -150m);

		rule.ProcessMessage(positionMsg).AssertTrue();
	}

	[TestMethod]
	public void PositionSizeZero()
	{
		var rule = new RiskPositionSizeRule
		{
			Position = 0,
			Action = RiskActions.CancelOrders
		};

		var positionMsg = new PositionChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, 100m);

		rule.ProcessMessage(positionMsg).AssertFalse();

		positionMsg = new PositionChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, -100m);

		rule.ProcessMessage(positionMsg).AssertFalse();

		// Missing current value should be ignored as well
		positionMsg = new PositionChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};

		rule.ProcessMessage(positionMsg).AssertFalse();

		positionMsg = new PositionChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, 0m);

		rule.ProcessMessage(positionMsg).AssertFalse();
	}

	[TestMethod]
	public void PositionTime()
	{
		var rule = new RiskPositionTimeRule
		{
			Time = TimeSpan.FromMinutes(5),
			Action = RiskActions.ClosePositions
		};

		var securityId = Helper.CreateSecurityId();
		var startTime = DateTime.UtcNow;

		var positionMsg = new PositionChangeMessage
		{
			SecurityId = securityId,
			PortfolioName = _pfName,
			LocalTime = startTime,
			ServerTime = startTime
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, 100m);

		rule.ProcessMessage(positionMsg).AssertFalse();

		var timeMsg = new TimeMessage
		{
			LocalTime = startTime.AddMinutes(3)
		};
		rule.ProcessMessage(timeMsg).AssertFalse();

		timeMsg = new TimeMessage
		{
			LocalTime = startTime.AddMinutes(6)
		};
		rule.ProcessMessage(timeMsg).AssertTrue();
	}

	[TestMethod]
	public void PositionTimeReset()
	{
		var rule = new RiskPositionTimeRule
		{
			Time = TimeSpan.FromMinutes(5)
		};

		var securityId = Helper.CreateSecurityId();

		var positionMsg = new PositionChangeMessage
		{
			SecurityId = securityId,
			PortfolioName = _pfName,
			LocalTime = DateTime.UtcNow,
			ServerTime = DateTime.UtcNow
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, 100m);

		rule.ProcessMessage(positionMsg);

		rule.Reset();

		var timeMsg = new TimeMessage
		{
			LocalTime = DateTime.UtcNow.AddMinutes(6)
		};
		rule.ProcessMessage(timeMsg).AssertFalse();
	}

	[TestMethod]
	public void Commission()
	{
		var rule = new RiskCommissionRule
		{
			Commission = 1000,
			Action = RiskActions.StopTrading
		};

		var positionMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.Commission, 500m);

		rule.ProcessMessage(positionMsg).AssertFalse();

		positionMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.Commission, 1500m);

		rule.ProcessMessage(positionMsg).AssertTrue();
	}

	[TestMethod]
	public void CommissionNegative()
	{
		// Test for negative commission limit (lower bound)
		// Verifies that RiskCommissionRule correctly handles negative thresholds
		// Fixed in RiskCommissionRule.cs:57-60 to handle both positive and negative limits
		var rule = new RiskCommissionRule
		{
			Commission = -1000, // Negative limit means we're tracking downside
			Action = RiskActions.StopTrading
		};

		// First test: commission is -500, which is ABOVE -1000 (less negative)
		// Should NOT trigger the rule (within acceptable range)
		var positionMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.Commission, -500m);

		rule.ProcessMessage(positionMsg).AssertFalse();

		// Second test: commission is -1500, which is BELOW -1000 (more negative)
		// SHOULD trigger the rule - commission exceeded the negative threshold
		// Correctly uses "currValue <= Commission" for negative limits
		positionMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.Commission, -1500m);

		// Rule activates correctly when commission goes below negative threshold
		rule.ProcessMessage(positionMsg).AssertTrue();
	}

	[TestMethod]
	public void Slippage()
	{
		var rule = new RiskSlippageRule
		{
			Slippage = 10,
			Action = RiskActions.CancelOrders
		};

		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Slippage = 5
		};

		rule.ProcessMessage(execMsg).AssertFalse();

		execMsg.Slippage = 15;
		rule.ProcessMessage(execMsg).AssertTrue();
	}

	[TestMethod]
	public void SlippageNegative()
	{
		var rule = new RiskSlippageRule
		{
			Slippage = -10,
			Action = RiskActions.CancelOrders
		};

		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Slippage = -5
		};

		rule.ProcessMessage(execMsg).AssertFalse();

		execMsg.Slippage = -15;
		rule.ProcessMessage(execMsg).AssertTrue();
	}

	[TestMethod]
	public void SlippageZero()
	{
		var rule = new RiskSlippageRule
		{
			Slippage = 0,
			Action = RiskActions.CancelOrders
		};

		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Slippage = 100
		};

		// Should not activate for positive slippage when limit is zero
		rule.ProcessMessage(execMsg).AssertFalse();

		execMsg.Slippage = -100;
		// Should not activate for negative slippage either
		rule.ProcessMessage(execMsg).AssertFalse();

		execMsg.Slippage = null;
		// Null slippage must be ignored
		rule.ProcessMessage(execMsg).AssertFalse();

		execMsg.Slippage = 0;
		rule.ProcessMessage(execMsg).AssertFalse();
	}

	[TestMethod]
	public void OrderPrice()
	{
		var rule = new RiskOrderPriceRule
		{
			Price = 100,
			Action = RiskActions.CancelOrders
		};

		var orderRegMsg = new OrderRegisterMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			Price = 90
		};

		rule.ProcessMessage(orderRegMsg).AssertFalse();

		orderRegMsg.Price = 110;
		rule.ProcessMessage(orderRegMsg).AssertTrue();

		var orderReplaceMsg = new OrderReplaceMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			Price = 80
		};

		rule.ProcessMessage(orderReplaceMsg).AssertFalse();

		orderReplaceMsg.Price = 120;
		rule.ProcessMessage(orderReplaceMsg).AssertTrue();
	}

	[TestMethod]
	public void OrderVolume()
	{
		var rule = new RiskOrderVolumeRule
		{
			Volume = 1000,
			Action = RiskActions.StopTrading
		};

		var orderRegMsg = new OrderRegisterMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			Volume = 500
		};

		rule.ProcessMessage(orderRegMsg).AssertFalse();

		orderRegMsg.Volume = 1500;
		rule.ProcessMessage(orderRegMsg).AssertTrue();

		var orderReplaceMsg = new OrderReplaceMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			Volume = 800
		};

		rule.ProcessMessage(orderReplaceMsg).AssertFalse();

		orderReplaceMsg.Volume = 1200;
		rule.ProcessMessage(orderReplaceMsg).AssertTrue();
	}

	[TestMethod]
	public void OrderVolumeInvalidVolume()
	{
		var rule = new RiskOrderVolumeRule();

		ThrowsExactly<ArgumentOutOfRangeException>(() => rule.Volume = -100);
	}

	[TestMethod]
	public void OrderFreq()
	{
		var rule = new RiskOrderFreqRule
		{
			Count = 3,
			Interval = TimeSpan.FromSeconds(10),
			Action = RiskActions.CancelOrders
		};

		var startTime = DateTime.UtcNow;

		for (int i = 0; i < 2; i++)
		{
			var orderMsg = new OrderRegisterMessage
			{
				SecurityId = Helper.CreateSecurityId(),
				LocalTime = startTime.AddSeconds(i)
			};

			rule.ProcessMessage(orderMsg).AssertFalse();
		}

		var thirdOrderMsg = new OrderRegisterMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			LocalTime = startTime.AddSeconds(5)
		};

		rule.ProcessMessage(thirdOrderMsg).AssertTrue();

		var fourthOrderMsg = new OrderRegisterMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			LocalTime = startTime.AddSeconds(6)
		};

		rule.ProcessMessage(fourthOrderMsg).AssertFalse();
	}

	[TestMethod]
	public void OrderFreqReset()
	{
		var rule = new RiskOrderFreqRule
		{
			Count = 2,
			Interval = TimeSpan.FromSeconds(10)
		};

		var orderMsg = new OrderRegisterMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			LocalTime = DateTime.UtcNow
		};

		rule.ProcessMessage(orderMsg);
		rule.Reset();

		rule.ProcessMessage(orderMsg).AssertFalse();
		rule.ProcessMessage(orderMsg).AssertTrue();
	}

	[TestMethod]
	public void OrderError()
	{
		var rule = new RiskOrderErrorRule
		{
			Count = 3,
			Action = RiskActions.StopTrading
		};

		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Error = new InvalidOperationException("Test error")
		};

		rule.ProcessMessage(execMsg).AssertFalse();
		rule.ProcessMessage(execMsg).AssertFalse();
		rule.ProcessMessage(execMsg).AssertTrue();

		var successMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			HasOrderInfo = true,
			OrderState = OrderStates.Active
		};

		rule.ProcessMessage(successMsg);

		rule.ProcessMessage(execMsg).AssertFalse();
	}

	[TestMethod]
	public void TradePrice()
	{
		var rule = new RiskTradePriceRule
		{
			Price = 100,
			Action = RiskActions.ClosePositions
		};

		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			TradePrice = 90,
			TradeVolume = 10
		};

		rule.ProcessMessage(execMsg).AssertFalse();

		execMsg.TradePrice = 110;
		rule.ProcessMessage(execMsg).AssertTrue();
	}

	[TestMethod]
	public void TradeVolume()
	{
		var rule = new RiskTradeVolumeRule
		{
			Volume = 1000,
			Action = RiskActions.CancelOrders
		};

		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			TradePrice = 100,
			TradeVolume = 500
		};

		rule.ProcessMessage(execMsg).AssertFalse();

		execMsg.TradeVolume = 1500;
		rule.ProcessMessage(execMsg).AssertTrue();
	}

	[TestMethod]
	public void TradeFreq()
	{
		var rule = new RiskTradeFreqRule
		{
			Count = 2,
			Interval = TimeSpan.FromSeconds(5),
			Action = RiskActions.StopTrading
		};

		var startTime = DateTime.UtcNow;

		var tradeMsg1 = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			LocalTime = startTime,
			TradePrice = 100,
			TradeVolume = 10
		};

		rule.ProcessMessage(tradeMsg1).AssertFalse();

		var tradeMsg2 = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			LocalTime = startTime.AddSeconds(2),
			TradePrice = 100,
			TradeVolume = 20
		};

		rule.ProcessMessage(tradeMsg2).AssertTrue();
	}

	[TestMethod]
	public void Error()
	{
		var rule = new RiskErrorRule
		{
			Count = 2,
			Action = RiskActions.ClosePositions
		};

		var errorMsg = new ErrorMessage
		{
			Error = new InvalidOperationException("Test error")
		};

		rule.ProcessMessage(errorMsg).AssertFalse();
		rule.ProcessMessage(errorMsg).AssertTrue();
	}

	[TestMethod]
	public void PropertyNotification()
	{
		var rule = new RiskPnLRule();
		var propertyChanged = false;
		string changedPropertyName = null;

		((INotifyPropertyChanged)rule).PropertyChanged += (s, e) =>
		{
			propertyChanged = true;
			changedPropertyName = e.PropertyName;
		};

		rule.Action = RiskActions.StopTrading;

		propertyChanged.AssertTrue();
		changedPropertyName.AssertEqual(nameof(RiskPnLRule.Action));
	}

	[TestMethod]
	public void UpdateTitle()
	{
		var rule = new RiskPnLRule
		{
			PnL = new() { Value = 1000, Type = UnitTypes.Absolute }
		};

		rule.Title.AssertNotNull();
		rule.Title.Contains("1000").AssertTrue();

		rule.PnL = new() { Value = 2000, Type = UnitTypes.Absolute };
		rule.Title.Contains("2000").AssertTrue();
	}

	[TestMethod]
	public void SaveLoad()
	{
		var originalRule = new RiskPnLRule
		{
			PnL = new() { Value = 1500, Type = UnitTypes.Percent },
			Action = RiskActions.StopTrading
		};

		var storage = originalRule.Save();

		var restoredRule = new RiskPnLRule();
		restoredRule.Load(storage);

		restoredRule.PnL.Value.AssertEqual(1500);
		restoredRule.PnL.Type.AssertEqual(UnitTypes.Percent);
		restoredRule.Action.AssertEqual(RiskActions.StopTrading);
	}

	[TestMethod]
	public void ProviderDefaultRules()
	{
		IRiskRuleProvider provider = new InMemoryRiskRuleProvider();

		var rules = provider.All.ToList();
		rules.Count.AssertEqual(15);

		rules.Count(t => t == typeof(RiskPnLRule)).AssertEqual(1);
		rules.Count(t => t == typeof(RiskPositionSizeRule)).AssertEqual(1);
		rules.Count(t => t == typeof(RiskPositionTimeRule)).AssertEqual(1);
		rules.Count(t => t == typeof(RiskCommissionRule)).AssertEqual(1);
		rules.Count(t => t == typeof(RiskSlippageRule)).AssertEqual(1);
		rules.Count(t => t == typeof(RiskOrderPriceRule)).AssertEqual(1);
		rules.Count(t => t == typeof(RiskOrderVolumeRule)).AssertEqual(1);
		rules.Count(t => t == typeof(RiskOrderFreqRule)).AssertEqual(1);
		rules.Count(t => t == typeof(RiskOrderErrorRule)).AssertEqual(1);
		rules.Count(t => t == typeof(RiskOrderCommissionRule)).AssertEqual(1);
		rules.Count(t => t == typeof(RiskTradePriceRule)).AssertEqual(1);
		rules.Count(t => t == typeof(RiskTradeVolumeRule)).AssertEqual(1);
		rules.Count(t => t == typeof(RiskTradeFreqRule)).AssertEqual(1);
		rules.Count(t => t == typeof(RiskTradeCommissionRule)).AssertEqual(1);
		rules.Count(t => t == typeof(RiskErrorRule)).AssertEqual(1);
	}

	[TestMethod]
	public void ProviderAddRemoveRule()
	{
		IRiskRuleProvider provider = new InMemoryRiskRuleProvider();

		var customRuleType = typeof(TestRiskRule);

		provider.Add(customRuleType);
		provider.All.Count(t => t == customRuleType).AssertEqual(1);

		provider.Remove(customRuleType);
		provider.All.Count(t => t == customRuleType).AssertEqual(0);
	}

	[TestMethod]
	public void Serialization()
	{
		IRiskRuleProvider provider = new InMemoryRiskRuleProvider();

		var excludeProps = new HashSet<string>
		{
			nameof(ILogSource.Id),
			nameof(ILogSource.Parent),
			nameof(ILogSource.IsRoot),
		};

		foreach (var ruleType in provider.All)
		{
			var rule = ruleType.CreateInstance<IRiskRule>();

			var props = ruleType
				.GetModifiableProps()
				.Where(p => !excludeProps.Contains(p.Name))
				.ToArray();

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
				else if (propType == typeof(TimeSpan))
					value = TimeSpan.FromSeconds(RandomGen.GetInt(1, 3600));
				else if (propType == typeof(string))
					value = RandomGen.GetString(3, 7);
				else
					throw new InvalidOperationException($"Unsupported property type: {propType.FullName}");

				prop.SetValue(rule, value);
			}

			var storage = rule.Save();

			var restored = ruleType.CreateInstance<IRiskRule>();
			restored.Load(storage);

			foreach (var prop in props)
			{
				var origValue = prop.GetValue(rule);
				var restoredValue = prop.GetValue(restored);

				origValue.AssertEqual(restoredValue);
			}
		}
	}

	[TestMethod]
	public void CommissionTrade()
	{
		var rule = new RiskTradeCommissionRule
		{
			Commission = 500,
			Action = RiskActions.StopTrading
		};

		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Commission = 200m,
			TradePrice = 100,
			TradeVolume = 10
		};

		rule.ProcessMessage(execMsg).AssertFalse();

		execMsg.Commission = 600m;
		rule.ProcessMessage(execMsg).AssertTrue();
	}

	[TestMethod]
	public void CommissionTradeNegative()
	{
		var rule = new RiskTradeCommissionRule
		{
			Commission = -500,
			Action = RiskActions.StopTrading
		};

		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Commission = -200m,
			TradePrice = 100,
			TradeVolume = 10
		};

		rule.ProcessMessage(execMsg).AssertFalse();

		execMsg.Commission = -600m;
		rule.ProcessMessage(execMsg).AssertTrue();
	}

	[TestMethod]
	public void CommissionOrder()
	{
		var rule = new RiskOrderCommissionRule
		{
			Commission = 100,
			Action = RiskActions.CancelOrders
		};

		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Commission = 50,
			HasOrderInfo = true
		};

		rule.ProcessMessage(execMsg).AssertFalse();

		execMsg.Commission = 150m;
		rule.ProcessMessage(execMsg).AssertTrue();

		execMsg.Commission = -180;
		rule.ProcessMessage(execMsg).AssertFalse();

		execMsg.Commission = 120m;
		rule.ProcessMessage(execMsg).AssertTrue();
	}

	[TestMethod]
	public void CommissionOrderNegative()
	{
		var rule = new RiskOrderCommissionRule
		{
			Commission = -100,
			Action = RiskActions.CancelOrders
		};

		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Commission = -50,
			HasOrderInfo = true
		};

		rule.ProcessMessage(execMsg).AssertFalse();

		execMsg.Commission = -150m;
		rule.ProcessMessage(execMsg).AssertTrue();

		execMsg.Commission = 180m;
		rule.ProcessMessage(execMsg).AssertFalse();

		execMsg.Commission = -120m;
		rule.ProcessMessage(execMsg).AssertTrue();
	}

	[TestMethod]
	public async Task AdapterClosePositionsMode()
	{
		var token = CancellationToken;
		// Test that RiskMessageAdapter sends OrderGroupCancelMessage with ClosePositions mode
		// when a risk rule with ClosePositions action is triggered

		// Use a custom adapter to intercept messages sent to inner adapter
		var testAdapter = new TestInnerAdapter();
		var riskManager = new RiskManager();
		var adapter = new RiskMessageAdapter(testAdapter, riskManager);

		var rule = new RiskPnLRule
		{
			PnL = new() { Value = -1000, Type = UnitTypes.Absolute },
			Action = RiskActions.ClosePositions
		};
		riskManager.Rules.Add(rule);

		await adapter.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		}.Add(PositionChangeTypes.CurrentValue, 0m), token);

		// Trigger the rule by sending a position change message with loss
		await adapter.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		}.Add(PositionChangeTypes.CurrentValue, -1500m), token);

		// Check that OrderGroupCancelMessage was sent to inner adapter with ClosePositions mode
		var cancelMsg = testAdapter.ReceivedMessages.OfType<OrderGroupCancelMessage>().FirstOrDefault();
		cancelMsg.AssertNotNull();
		cancelMsg.Mode.AssertEqual(OrderGroupCancelModes.ClosePositions);
	}

	[TestMethod]
	public async Task AdapterCancelOrdersMode()
	{
		// Test that RiskMessageAdapter sends OrderGroupCancelMessage with CancelOrders mode
		// when a risk rule with CancelOrders action is triggered
		var emu = new MarketEmulator(new CollectionSecurityProvider([new() { Id = "TEST@TEST" }]), new CollectionPortfolioProvider([Portfolio.CreateSimulator()]), new InMemoryExchangeInfoProvider(), new IncrementalIdGenerator());
		var emuAdapter = new MarketEmulatorAdapter(emu, new IncrementalIdGenerator());
		var riskManager = new RiskManager();
		var adapter = new RiskMessageAdapter(emuAdapter, riskManager);

		var messages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { messages.Add(m); return default; };

		var rule = new RiskPositionSizeRule
		{
			Position = 100,
			Action = RiskActions.CancelOrders
		};
		riskManager.Rules.Add(rule);

		// Trigger the rule by sending a position change message exceeding the limit
		var positionMsg = new PositionChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, 150m);

		await adapter.SendInMessageAsync(positionMsg, CancellationToken);

		// Check that OrderGroupCancelMessage was sent with CancelOrders mode (looped back)
		var cancelMsg = messages.OfType<OrderGroupCancelMessage>().FirstOrDefault();
		cancelMsg.AssertNotNull();
		// CancelOrders is the default mode, should be set
		(cancelMsg.Mode & OrderGroupCancelModes.CancelOrders).AssertEqual(OrderGroupCancelModes.CancelOrders);
	}

	[TestMethod]
	public async Task AdapterStopTradingBlocks()
	{
		var token = CancellationToken;
		// Test that RiskMessageAdapter blocks trading when StopTrading action is triggered
		var riskManager = new RiskManager();
		var adapter = new RiskMessageAdapter(new TestInnerAdapter(), riskManager);

		var messages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { messages.Add(m); return default; };

		var rule = new RiskCommissionRule
		{
			Commission = 1000,
			Action = RiskActions.StopTrading
		};
		riskManager.Rules.Add(rule);

		// Trigger the rule by sending a position change message with high commission
		var positionMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.Commission, 1500m);

		await adapter.SendInMessageAsync(positionMsg, token);

		// Now try to register an order - it should be rejected
		var orderMsg = new OrderRegisterMessage
		{
			TransactionId = 1,
			SecurityId = Helper.CreateSecurityId(),
			Side = Sides.Buy,
			Price = 100,
			Volume = 10,
			PortfolioName = _pfName
		};

		await adapter.SendInMessageAsync(orderMsg, token);

		// Check that the order was rejected with Failed state
		var execMsg = messages.OfType<ExecutionMessage>()
			.FirstOrDefault(x => x.OriginalTransactionId == 1 && x.OrderState == OrderStates.Failed);
		execMsg.AssertNotNull();
		execMsg.Error.AssertNotNull();
	}

	[TestMethod]
	public async Task AdapterTradingUnblocks()
	{
		var token = CancellationToken;
		// Test that RiskMessageAdapter unblocks trading when risk limits are no longer exceeded
		var testAdapter = new TestInnerAdapter();
		var riskManager = new RiskManager();
		var adapter = new RiskMessageAdapter(testAdapter, riskManager);

		var messages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { messages.Add(m); return default; };

		var rule = new RiskPnLRule
		{
			PnL = new() { Value = -1000, Type = UnitTypes.Absolute },
			Action = RiskActions.StopTrading
		};
		riskManager.Rules.Add(rule);

		await adapter.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		}.Add(PositionChangeTypes.CurrentValue, 0m), token);

		// Trigger the rule by sending a position change message with loss
		await adapter.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		}.Add(PositionChangeTypes.CurrentValue, -1500m), token);

		// Verify trading is blocked
		var orderMsg = new OrderRegisterMessage
		{
			TransactionId = 1,
			SecurityId = Helper.CreateSecurityId(),
			Side = Sides.Buy,
			Price = 100,
			Volume = 10,
			PortfolioName = _pfName
		};

		await adapter.SendInMessageAsync(orderMsg, token);

		var execMsg = messages.OfType<ExecutionMessage>()
			.FirstOrDefault(x => x.OriginalTransactionId == 1 && x.OrderState == OrderStates.Failed);
		execMsg.AssertNotNull();

		messages.Clear();
		testAdapter.ReceivedMessages.Clear();

		// Now send a position message that no longer exceeds the limit
		await adapter.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		}.Add(PositionChangeTypes.CurrentValue, -500m), token);

		// Try to register an order again - it should now be accepted (not rejected)
		var orderMsg2 = new OrderRegisterMessage
		{
			TransactionId = 2,
			SecurityId = Helper.CreateSecurityId(),
			Side = Sides.Buy,
			Price = 100,
			Volume = 10,
			PortfolioName = _pfName
		};

		await adapter.SendInMessageAsync(orderMsg2, token);

		// Check that the order was NOT rejected
		var failedMsg = messages.OfType<ExecutionMessage>()
			.FirstOrDefault(x => x.OriginalTransactionId == 2 && x.OrderState == OrderStates.Failed);
		failedMsg.AssertNull();

		// Verify the message was sent to inner adapter
		var sentOrder = testAdapter.ReceivedMessages.OfType<OrderRegisterMessage>()
			.FirstOrDefault(x => x.TransactionId == 2);
		sentOrder.AssertNotNull();
	}

	[TestMethod]
	public void TradePriceNullPrice()
	{
		var rule = new RiskTradePriceRule
		{
			Price = 1000,
			Action = RiskActions.CancelOrders
		};

		// Trade message with null TradeVolume should not throw and should return false
		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			TradePrice = null,
			TradeVolume = 10
		};

		// Should not throw and should return false since volume is null
		rule.ProcessMessage(execMsg).AssertFalse();
	}

	[TestMethod]
	public void TradeVolumeNullVolume()
	{
		var rule = new RiskTradeVolumeRule
		{
			Volume = 1000,
			Action = RiskActions.CancelOrders
		};

		// Trade message with null TradeVolume should not throw and should return false
		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			TradePrice = 100,
			TradeVolume = null
		};

		// Should not throw and should return false since volume is null
		rule.ProcessMessage(execMsg).AssertFalse();
	}

	private class TestRiskRule : RiskRule
	{
		public bool ShouldActivate { get; set; }
		public bool IsReset { get; private set; }
		public Message LastMessage { get; private set; }

		protected override string GetTitle() => "Test Rule";

		public override void Reset()
		{
			base.Reset();
			IsReset = true;
		}

		public override bool ProcessMessage(Message message)
		{
			LastMessage = message;
			return ShouldActivate;
		}
	}

	// Wrapper adapter that captures all incoming messages
	private class TestInnerAdapter : PassThroughMessageAdapter
	{
		public List<Message> ReceivedMessages { get; } = [];

		public TestInnerAdapter()
			: base(new IncrementalIdGenerator())
		{
		}

		public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken)
		{
			ReceivedMessages.Add(message);
			return base.SendInMessageAsync(message, cancellationToken);
		}

		public override IMessageAdapter Clone() => new TestInnerAdapter();
	}

	[TestMethod]
	public void CommissionZero()
	{
		var rule = new RiskCommissionRule
		{
			Commission = 0,
			Action = RiskActions.StopTrading
		};

		var positionMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.Commission, 1500m);

		rule.ProcessMessage(positionMsg).AssertFalse();

		positionMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.Commission, -1500m);

		rule.ProcessMessage(positionMsg).AssertFalse();

		positionMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTime.UtcNow,
			PortfolioName = _pfName
		};
		// No commission value
		rule.ProcessMessage(positionMsg).AssertFalse();
	}

	[TestMethod]
	public void CommissionTradeZero()
	{
		var rule = new RiskTradeCommissionRule
		{
			Commission = 0,
			Action = RiskActions.StopTrading
		};

		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Commission = 600m,
			TradePrice = 100,
			TradeVolume = 10
		};

		rule.ProcessMessage(execMsg).AssertFalse();

		execMsg.Commission = -600m;
		rule.ProcessMessage(execMsg).AssertFalse();

		execMsg.Commission = null;
		rule.ProcessMessage(execMsg).AssertFalse();
	}

	[TestMethod]
	public void CommissionOrderZero()
	{
		var rule = new RiskOrderCommissionRule
		{
			Commission = 0,
			Action = RiskActions.CancelOrders
		};

		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Commission = 150m,
			HasOrderInfo = true
		};

		rule.ProcessMessage(execMsg).AssertFalse();

		execMsg.Commission = -150m;
		rule.ProcessMessage(execMsg).AssertFalse();

		execMsg.Commission = null;
		rule.ProcessMessage(execMsg).AssertFalse();
	}
}
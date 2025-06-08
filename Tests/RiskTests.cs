namespace StockSharp.Tests;

using System.ComponentModel;

using StockSharp.Algo.Risk;

[TestClass]
public class RiskTests
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
		manager.Rules.Contains(rule1).AssertTrue();
		manager.Rules.Contains(rule2).AssertTrue();

		rule1.Parent.AssertEqual(manager);
		rule2.Parent.AssertEqual(manager);

		manager.Rules.Remove(rule1);
		manager.Rules.Count.AssertEqual(1);
		rule1.Parent.AssertNull();

		manager.Rules.Clear();
		manager.Rules.Count.AssertEqual(0);
		rule2.Parent.AssertNull();
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

		var message = new TimeMessage { LocalTime = DateTimeOffset.UtcNow };
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
			ServerTime = DateTimeOffset.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, -500m);

		rule.ProcessMessage(positionMsg).AssertFalse();

		positionMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTimeOffset.UtcNow,
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
			PnL = new() { Value = -1000, Type = UnitTypes.Limit },
			Action = RiskActions.ClosePositions
		};

		var positionMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTimeOffset.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, -500m);

		rule.ProcessMessage(positionMsg).AssertFalse();

		positionMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTimeOffset.UtcNow,
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
			ServerTime = DateTimeOffset.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, 0m);

		rule.ProcessMessage(positionMsg);

		rule.Reset();

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
			ServerTime = DateTimeOffset.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, 50m);

		rule.ProcessMessage(positionMsg).AssertFalse();

		positionMsg = new PositionChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTimeOffset.UtcNow,
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
			ServerTime = DateTimeOffset.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, -50m);

		rule.ProcessMessage(positionMsg).AssertFalse();

		positionMsg = new PositionChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTimeOffset.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, -150m);

		rule.ProcessMessage(positionMsg).AssertTrue();
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
		var startTime = DateTimeOffset.UtcNow;

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
			LocalTime = DateTimeOffset.UtcNow,
			ServerTime = DateTimeOffset.UtcNow
		};
		positionMsg.Add(PositionChangeTypes.CurrentValue, 100m);

		rule.ProcessMessage(positionMsg);

		rule.Reset();

		var timeMsg = new TimeMessage
		{
			LocalTime = DateTimeOffset.UtcNow.AddMinutes(6)
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
			ServerTime = DateTimeOffset.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.Commission, 500m);

		rule.ProcessMessage(positionMsg).AssertFalse();

		positionMsg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			ServerTime = DateTimeOffset.UtcNow,
			PortfolioName = _pfName
		};
		positionMsg.Add(PositionChangeTypes.Commission, 1500m);

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
			ServerTime = DateTimeOffset.UtcNow,
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
			ServerTime = DateTimeOffset.UtcNow,
			Slippage = -5
		};

		rule.ProcessMessage(execMsg).AssertFalse();

		execMsg.Slippage = -15;
		rule.ProcessMessage(execMsg).AssertTrue();
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

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => rule.Volume = -100);
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

		var startTime = DateTimeOffset.UtcNow;

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
			LocalTime = DateTimeOffset.UtcNow
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
			ServerTime = DateTimeOffset.UtcNow,
			Error = new InvalidOperationException("Test error")
		};

		rule.ProcessMessage(execMsg).AssertFalse();
		rule.ProcessMessage(execMsg).AssertFalse();
		rule.ProcessMessage(execMsg).AssertTrue();

		var successMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTimeOffset.UtcNow,
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
			ServerTime = DateTimeOffset.UtcNow,
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
			ServerTime = DateTimeOffset.UtcNow,
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

		var startTime = DateTimeOffset.UtcNow;

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

		var rules = provider.Rules.ToList();
		rules.Count.AssertGreater(0);

		rules.Contains(typeof(RiskPnLRule)).AssertTrue();
		rules.Contains(typeof(RiskPositionSizeRule)).AssertTrue();
		rules.Contains(typeof(RiskPositionTimeRule)).AssertTrue();
		rules.Contains(typeof(RiskCommissionRule)).AssertTrue();
		rules.Contains(typeof(RiskSlippageRule)).AssertTrue();
		rules.Contains(typeof(RiskOrderPriceRule)).AssertTrue();
		rules.Contains(typeof(RiskOrderVolumeRule)).AssertTrue();
		rules.Contains(typeof(RiskOrderFreqRule)).AssertTrue();
		rules.Contains(typeof(RiskOrderErrorRule)).AssertTrue();
		rules.Contains(typeof(RiskTradePriceRule)).AssertTrue();
		rules.Contains(typeof(RiskTradeVolumeRule)).AssertTrue();
		rules.Contains(typeof(RiskTradeFreqRule)).AssertTrue();
		rules.Contains(typeof(RiskErrorRule)).AssertTrue();
	}

	[TestMethod]
	public void ProviderAddRemoveRule()
	{
		IRiskRuleProvider provider = new InMemoryRiskRuleProvider();

		var customRuleType = typeof(TestRiskRule);

		provider.AddRule(customRuleType);
		provider.Rules.Contains(customRuleType).AssertTrue();

		provider.RemoveRule(customRuleType);
		provider.Rules.Contains(customRuleType).AssertFalse();
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

		foreach (var ruleType in provider.Rules)
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
			ServerTime = DateTimeOffset.UtcNow,
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
			ServerTime = DateTimeOffset.UtcNow,
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
			ServerTime = DateTimeOffset.UtcNow,
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
			ServerTime = DateTimeOffset.UtcNow,
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
}

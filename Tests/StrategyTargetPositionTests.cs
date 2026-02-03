namespace StockSharp.Tests;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;

[TestClass]
public class StrategyTargetPositionTests : BaseTestClass
{
	[TestMethod]
	public void SetTargetPosition_CreatesManager()
	{
		var strategy = new Strategy
		{
			Security = new Security { Id = "SBER@TQBR" },
			Portfolio = new Portfolio { Name = "test" },
		};

		IsNotNull(strategy.TargetPositionManager);
	}

	[TestMethod]
	public void GetTargetPosition_ReturnsNull_WhenNotSet()
	{
		var strategy = new Strategy
		{
			Security = new Security { Id = "SBER@TQBR" },
			Portfolio = new Portfolio { Name = "test" },
		};

		IsNull(strategy.GetTargetPosition());
	}

	[TestMethod]
	public void WhenTargetReached_RuleCreated()
	{
		var strategy = new Strategy
		{
			Security = new Security { Id = "SBER@TQBR" },
			Portfolio = new Portfolio { Name = "test" },
		};

		var rule = strategy.TargetPositionManager.WhenTargetReached();

		IsNotNull(rule);
		rule.Name.AreEqual("Target reached");
	}

	[TestMethod]
	public void WhenTargetReached_WithSecurity_RuleCreated()
	{
		var security = new Security { Id = "SBER@TQBR" };
		var strategy = new Strategy
		{
			Security = security,
			Portfolio = new Portfolio { Name = "test" },
		};

		var rule = strategy.TargetPositionManager.WhenTargetReached(security);

		IsNotNull(rule);
		rule.Name.AreEqual($"Target reached {security}");
	}
}

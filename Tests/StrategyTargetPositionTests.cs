namespace StockSharp.Tests;

using StockSharp.Algo;
using StockSharp.Algo.PositionManagement;
using StockSharp.Algo.Strategies;

[TestClass]
public class StrategyTargetPositionTests : BaseTestClass
{
	[TestMethod]
	public void SetTargetPosition_RoundTripsThroughStrategy()
	{
		var strategy = new Strategy
		{
			Security = new Security { Id = "SBER@TQBR" },
			Portfolio = new Portfolio { Name = "test" },
		};

		strategy.SetTargetPosition(25m);
		strategy.GetTargetPosition().AssertEqual(25m);
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

	// Fixed timestamp used when seeding positions by hand; its value plays no part in the rules.
	private static readonly DateTime _posTime = new(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

	// Started, but deliberately never driven online: canTrade stays false, so the target manager emits
	// no orders and the only thing left under test is the target-reached event. Started matters because
	// a rule whose container is not Started finishes after its first activation.
	private static async Task<Strategy> CreateStartedStrategyAsync(Security security, Portfolio portfolio, CancellationToken cancellationToken)
	{
		var connMock = new Mock<IConnector>();
		connMock.Setup(c => c.TransactionIdGenerator).Returns(new IncrementalIdGenerator());

		var strategy = new Strategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
		};

		await strategy.Engine.OnMessageAsync(new StrategyEngine.StrategyStateMessage(ProcessStates.Started), cancellationToken);

		strategy.ProcessState.AreEqual(ProcessStates.Started);
		IsFalse(strategy.IsOnline);

		return strategy;
	}

	// Applies the rule to the strategy and collects every activation argument in order.
	private static List<(Security sec, Portfolio pf)> Track(MarketRule<PositionTargetManager, (Security, Portfolio)> rule, Strategy strategy)
	{
		var hits = new List<(Security sec, Portfolio pf)>();
		rule.Do(arg => hits.Add(arg)).Apply(strategy);
		return hits;
	}

	[TestMethod]
	public async Task WhenTargetReached_FiresOnce_WhenTargetAlreadyMet()
	{
		var security = new Security { Id = "SBER@TQBR" };
		var portfolio = new Portfolio { Name = "test" };
		var strategy = await CreateStartedStrategyAsync(security, portfolio, CancellationToken);

		strategy.SetPositionValue(security, portfolio, 5m, _posTime);

		var hits = Track(strategy.TargetPositionManager.WhenTargetReached(), strategy);

		// delta = target(5) - position(5) = 0, so the target is reached the moment it is set, and the
		// activation must name the pair it was reached for.
		strategy.SetTargetPosition(5m);

		AreEqual(1, hits.Count, "An already met target must be reported exactly once");
		hits[0].sec.AreEqual(security);
		hits[0].pf.AreEqual(portfolio);
	}

	[TestMethod]
	public async Task WhenTargetReached_DoesNotFire_UntilTargetIsExactlyMet()
	{
		var security = new Security { Id = "SBER@TQBR" };
		var portfolio = new Portfolio { Name = "test" };
		var strategy = await CreateStartedStrategyAsync(security, portfolio, CancellationToken);

		var hits = Track(strategy.TargetPositionManager.WhenTargetReached(), strategy);

		// delta = 10 - 0 = 10: a target that is merely set is not a target reached.
		strategy.SetTargetPosition(10m);
		AreEqual(0, hits.Count, "Setting a target is not reaching it");

		// delta = 10 - 4 = 6: partial progress toward the target is still not attainment.
		strategy.SetPositionValue(security, portfolio, 4m, _posTime);
		strategy.SetTargetPosition(10m);
		AreEqual(0, hits.Count, "Partial progress toward the target must not activate the rule");

		// delta = 10 - 10 = 0: attained.
		strategy.SetPositionValue(security, portfolio, 10m, _posTime);
		strategy.SetTargetPosition(10m);
		AreEqual(1, hits.Count, "Exact attainment must activate the rule once");
	}

	[TestMethod]
	public async Task WhenTargetReached_DoesNotFire_WhenPositionOvershootsTarget()
	{
		var security = new Security { Id = "SBER@TQBR" };
		var portfolio = new Portfolio { Name = "test" };
		var strategy = await CreateStartedStrategyAsync(security, portfolio, CancellationToken);

		var hits = Track(strategy.TargetPositionManager.WhenTargetReached(), strategy);

		// delta = 10 - 12 = -2, distance 2 > tolerance 0: the target was passed, not reached, and a
		// correction of two lots the other way is still owed.
		strategy.SetPositionValue(security, portfolio, 12m, _posTime);
		strategy.SetTargetPosition(10m);
		AreEqual(0, hits.Count, "Overshooting the target is not reaching it");

		// delta = 10 - 10 = 0: only sitting on the target counts.
		strategy.SetPositionValue(security, portfolio, 10m, _posTime);
		strategy.SetTargetPosition(10m);
		AreEqual(1, hits.Count);
	}

	[TestMethod]
	public async Task WhenTargetReached_FiresAgain_OnEveryAttainment()
	{
		var security = new Security { Id = "SBER@TQBR" };
		var portfolio = new Portfolio { Name = "test" };
		var strategy = await CreateStartedStrategyAsync(security, portfolio, CancellationToken);

		strategy.SetPositionValue(security, portfolio, 5m, _posTime);

		var hits = Track(strategy.TargetPositionManager.WhenTargetReached(), strategy);

		// First attainment: delta = 5 - 5 = 0.
		strategy.SetTargetPosition(5m);
		AreEqual(1, hits.Count);

		// Target moved to 8 while the position is still 5: delta = 3, nothing reached.
		strategy.SetTargetPosition(8m);
		AreEqual(1, hits.Count, "Moving the target away must not activate the rule");

		// Second attainment of the same rule: delta = 8 - 8 = 0. The rule is not one-shot.
		strategy.SetPositionValue(security, portfolio, 8m, _posTime);
		strategy.SetTargetPosition(8m);
		AreEqual(2, hits.Count, "Each attainment is a separate activation");
	}

	[TestMethod]
	public async Task WhenTargetReached_CancelTarget_IsNotAnAttainment()
	{
		var security = new Security { Id = "SBER@TQBR" };
		var portfolio = new Portfolio { Name = "test" };
		var strategy = await CreateStartedStrategyAsync(security, portfolio, CancellationToken);

		strategy.SetPositionValue(security, portfolio, 5m, _posTime);

		var hits = Track(strategy.TargetPositionManager.WhenTargetReached(), strategy);

		strategy.SetTargetPosition(5m);
		AreEqual(1, hits.Count);

		// Dropping the target is not an attainment, however close the position sits to it.
		strategy.CancelTargetPosition();
		IsNull(strategy.GetTargetPosition());
		AreEqual(1, hits.Count, "Cancelling a target must not activate the rule");

		// Re-established at the position it already sits on: reached again.
		strategy.SetTargetPosition(5m);
		AreEqual(2, hits.Count);
	}

	[TestMethod]
	public async Task WhenTargetReached_WithSecurity_FiresOnlyForThatSecurity()
	{
		var sec1 = new Security { Id = "SBER@TQBR" };
		var sec2 = new Security { Id = "GAZP@TQBR" };
		var portfolio = new Portfolio { Name = "test" };
		var strategy = await CreateStartedStrategyAsync(sec1, portfolio, CancellationToken);

		var manager = strategy.TargetPositionManager;
		var filtered = Track(manager.WhenTargetReached(sec1), strategy);
		var any = Track(manager.WhenTargetReached(), strategy);

		// sec2 reaching its own target (delta = 3 - 3 = 0) is invisible to a rule bound to sec1, while
		// the unfiltered rule sees it: a null security filter means any.
		strategy.SetPositionValue(sec2, portfolio, 3m, _posTime);
		strategy.SetTargetPosition(sec2, portfolio, 3m);
		AreEqual(0, filtered.Count, "A security-bound rule must ignore another security");
		AreEqual(1, any.Count);
		any[0].sec.AreEqual(sec2);

		// sec1 reaching its target (delta = 7 - 7 = 0) activates both.
		strategy.SetPositionValue(sec1, portfolio, 7m, _posTime);
		strategy.SetTargetPosition(sec1, portfolio, 7m);
		AreEqual(1, filtered.Count);
		filtered[0].sec.AreEqual(sec1);
		AreEqual(2, any.Count);
	}

	[TestMethod]
	public async Task WhenTargetReached_WithPortfolio_FiresOnlyForThatPortfolio()
	{
		var security = new Security { Id = "SBER@TQBR" };
		var pf1 = new Portfolio { Name = "pf1" };
		var pf2 = new Portfolio { Name = "pf2" };
		var strategy = await CreateStartedStrategyAsync(security, pf1, CancellationToken);

		var manager = strategy.TargetPositionManager;
		var filtered = Track(manager.WhenTargetReached(portfolio: pf2), strategy);
		var any = Track(manager.WhenTargetReached(), strategy);

		// Same security under another portfolio: delta = 2 - 2 = 0 on pf1 must not reach a pf2 rule.
		strategy.SetPositionValue(security, pf1, 2m, _posTime);
		strategy.SetTargetPosition(security, pf1, 2m);
		AreEqual(0, filtered.Count, "A portfolio-bound rule must ignore another portfolio");
		AreEqual(1, any.Count);

		// delta = 4 - 4 = 0 on pf2.
		strategy.SetPositionValue(security, pf2, 4m, _posTime);
		strategy.SetTargetPosition(security, pf2, 4m);
		AreEqual(1, filtered.Count);
		filtered[0].pf.AreEqual(pf2);
		AreEqual(2, any.Count);
	}

	[TestMethod]
	public async Task WhenTargetReached_HonoursPositionTolerance()
	{
		var sec1 = new Security { Id = "SBER@TQBR" };
		var sec2 = new Security { Id = "GAZP@TQBR" };
		var portfolio = new Portfolio { Name = "test" };
		var strategy = await CreateStartedStrategyAsync(sec1, portfolio, CancellationToken);

		var manager = strategy.TargetPositionManager;
		manager.PositionTolerance = 0.5m;

		var hits = Track(manager.WhenTargetReached(), strategy);

		// |10.6 - 10| = 0.6 > 0.5: outside the tolerance, so not reached.
		strategy.SetPositionValue(sec2, portfolio, 10m, _posTime);
		strategy.SetTargetPosition(sec2, portfolio, 10.6m);
		AreEqual(0, hits.Count, "A gap wider than the tolerance is not a reached target");

		// |10.4 - 10| = 0.4 <= 0.5: inside the tolerance, so reached without an exact match.
		strategy.SetPositionValue(sec1, portfolio, 10m, _posTime);
		strategy.SetTargetPosition(sec1, portfolio, 10.4m);
		AreEqual(1, hits.Count, "A gap within the tolerance counts as reached");
		hits[0].sec.AreEqual(sec1);
	}

	[TestMethod]
	public async Task WhenTargetReached_Disposed_StopsFiring()
	{
		var security = new Security { Id = "SBER@TQBR" };
		var portfolio = new Portfolio { Name = "test" };
		var strategy = await CreateStartedStrategyAsync(security, portfolio, CancellationToken);

		strategy.SetPositionValue(security, portfolio, 5m, _posTime);

		var hits = new List<(Security sec, Portfolio pf)>();
		var rule = strategy.TargetPositionManager.WhenTargetReached();
		rule.Do(arg => hits.Add(arg)).Apply(strategy);

		strategy.SetTargetPosition(5m);
		AreEqual(1, hits.Count);

		// A disposed rule must have let go of the manager event: later attainments reach nobody.
		rule.Dispose();

		strategy.SetPositionValue(security, portfolio, 8m, _posTime);
		strategy.SetTargetPosition(8m);
		AreEqual(1, hits.Count, "A disposed rule must not be activated any more");
	}
}

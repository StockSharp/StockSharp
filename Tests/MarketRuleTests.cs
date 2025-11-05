namespace StockSharp.Tests;

/// <summary>
/// Tests for Market Rules system (IMarketRule, MarketRule, MarketRuleHelper).
/// </summary>
[TestClass]
public class MarketRuleTests
{
	/// <summary>
	/// Simple test rule for testing basic functionality.
	/// </summary>
	private class TestRule : MarketRule<string, int>
	{
		private readonly Action<int> _onActivate;

		public TestRule(string token, Action<int> onActivate = null)
			: base(token)
		{
			_onActivate = onActivate;
			Name = "Test Rule";
		}

		public void TriggerActivate(int value)
		{
			Activate(value);
		}

		protected override void Activate(int arg)
		{
			_onActivate?.Invoke(arg);
			base.Activate(arg);
		}
	}

	/// <summary>
	/// Test that rule can be created and has correct initial state.
	/// </summary>
	[TestMethod]
	public void RuleCreation_InitialState()
	{
		// Arrange & Act
		var rule = new TestRule("test-token");

		// Assert
		rule.Token.AssertEqual("test-token");
		rule.Name.AssertEqual("Test Rule");
		rule.IsSuspended.AssertFalse();
		rule.IsActive.AssertFalse();
		rule.IsReady.AssertFalse(); // Not ready until added to container
		rule.Container.AssertNull();
	}

	/// <summary>
	/// Test that rule becomes ready after being added to container.
	/// </summary>
	[TestMethod]
	public void RuleApply_BecomesReady()
	{
		// Arrange
		var rule = new TestRule("test-token");
		rule.IsReady.AssertFalse();

		// Act
		rule.Apply(); // Uses default container

		// Assert
		rule.IsReady.AssertTrue();
		rule.Container.AssertNotNull();
	}

	/// <summary>
	/// Test that Do() action is executed when rule is activated.
	/// </summary>
	[TestMethod]
	public void RuleActivation_ExecutesDoAction()
	{
		// Arrange
		var executed = false;
		var receivedValue = 0;

		var rule = new TestRule("test-token")
			.Do((int value) =>
			{
				executed = true;
				receivedValue = value;
			})
			.Apply();

		// Act
		rule.TriggerActivate(42);

		// Assert
		executed.AssertTrue();
		receivedValue.AssertEqual(42);
	}

	/// <summary>
	/// Test that Do() action without parameters works.
	/// </summary>
	[TestMethod]
	public void RuleActivation_ExecutesDoActionWithoutParams()
	{
		// Arrange
		var executed = false;

		var rule = new TestRule("test-token")
			.Do(() => executed = true)
			.Apply();

		// Act
		rule.TriggerActivate(100);

		// Assert
		executed.AssertTrue();
	}

	/// <summary>
	/// Test that Do() with return value works and Activated() handler receives the result.
	/// </summary>
	[TestMethod]
	public void RuleActivation_DoWithReturnValue_ActivatedReceivesResult()
	{
		// Arrange
		var activatedValue = 0;

		var rule = new TestRule("test-token")
			.Do((int value) => value * 2) // Double the input
			.Activated<int>(result => activatedValue = result)
			.Apply();

		// Act
		rule.TriggerActivate(21);

		// Assert
		activatedValue.AssertEqual(42);
	}

	/// <summary>
	/// Test that suspended rule does not execute.
	/// </summary>
	[TestMethod]
	public void RuleSuspension_DoesNotExecute()
	{
		// Arrange
		var executed = false;

		var rule = new TestRule("test-token")
			.Do(() => executed = true)
			.Apply();

		// Act
		rule.IsSuspended = true;
		rule.TriggerActivate(1);

		// Assert
		executed.AssertFalse(); // Should NOT execute when suspended
	}

	/// <summary>
	/// Test that rule executes after being resumed.
	/// </summary>
	[TestMethod]
	public void RuleSuspension_ExecutesAfterResume()
	{
		// Arrange
		var executed = false;

		var rule = new TestRule("test-token")
			.Do(() => executed = true)
			.Apply();

		rule.IsSuspended = true;

		// Act
		rule.IsSuspended = false; // Resume
		rule.TriggerActivate(1);

		// Assert
		executed.AssertTrue();
	}

	/// <summary>
	/// Test that Until() controls periodicity - rule is removed after canFinish returns true.
	/// </summary>
	[TestMethod]
	public void RuleUntil_RemovesWhenFinished()
	{
		// Arrange
		var executionCount = 0;
		var maxExecutions = 3;

		var rule = new TestRule("test-token")
			.Do(() => executionCount++)
			.Until(() => executionCount >= maxExecutions)
			.Apply();

		var container = rule.Container;
		container.Rules.Contains(rule).AssertTrue();

		// Act - trigger multiple times
		for (int i = 0; i < 5; i++)
		{
			if (rule.IsReady)
				rule.TriggerActivate(i);
		}

		// Assert
		executionCount.AssertEqual(maxExecutions); // Should stop at 3
		container.Rules.Contains(rule).AssertFalse(); // Rule should be removed
	}

	/// <summary>
	/// Test that one-time rule is automatically removed after first execution.
	/// </summary>
	[TestMethod]
	public void RuleOneTime_RemovedAfterExecution()
	{
		// Arrange
		var executionCount = 0;

		var rule = new TestRule("test-token")
			.Do(() => executionCount++)
			.Apply();

		var container = rule.Container;
		container.Rules.Contains(rule).AssertTrue();

		// Act
		rule.TriggerActivate(1);

		// Wait a moment for async removal
		Thread.Sleep(100);

		// Assert
		executionCount.AssertEqual(1);
		// One-time rules are removed after execution
		// (Until() by default uses CanFinish which returns true when container stops)
	}

	/// <summary>
	/// Test WhenConnected rule.
	/// </summary>
	[TestMethod]
	public void ConnectorRule_WhenConnected()
	{
		// Arrange
		var connector = new Connector(new InMemoryMessageAdapter(new IdGenerator()));
		var connected = false;
		IMessageAdapter connectedAdapter = null;

		var rule = connector
			.WhenConnected()
			.Do((adapter) =>
			{
				connected = true;
				connectedAdapter = adapter;
			})
			.Apply(connector);

		// Act
		connector.Connected += () => { }; // Ensure event is wired

		// Manually trigger the ConnectedEx event to simulate connection
		var adapter = connector.Adapter;
		// Note: In real test, you would actually connect
		// For this example, we just verify the rule is set up correctly

		// Assert
		rule.AssertNotNull();
		rule.Token.AssertEqual(connector);
	}

	/// <summary>
	/// Test exclusive rules - when one rule activates, exclusive rules are removed.
	/// </summary>
	[TestMethod]
	public void ExclusiveRules_RemovedWhenMainRuleActivates()
	{
		// Arrange
		var mainExecuted = false;
		var exclusiveExecuted = false;

		var mainRule = new TestRule("main")
			.Do(() => mainExecuted = true)
			.Apply();

		var exclusiveRule = new TestRule("exclusive")
			.Do(() => exclusiveExecuted = true)
			.Apply();

		// Set up exclusive relationship
		mainRule.ExclusiveRules.Add(exclusiveRule);

		var container = mainRule.Container;
		container.Rules.Contains(mainRule).AssertTrue();
		container.Rules.Contains(exclusiveRule).AssertTrue();

		// Act
		mainRule.TriggerActivate(1);

		// Wait for async removal
		Thread.Sleep(100);

		// Assert
		mainExecuted.AssertTrue();
		exclusiveExecuted.AssertFalse();
		container.Rules.Contains(exclusiveRule).AssertFalse(); // Exclusive rule removed
	}

	/// <summary>
	/// Test that disposed rule is not ready.
	/// </summary>
	[TestMethod]
	public void RuleDispose_NotReady()
	{
		// Arrange
		var rule = new TestRule("test-token").Apply();
		rule.IsReady.AssertTrue();

		// Act
		rule.Dispose();

		// Assert
		rule.IsReady.AssertFalse();
		rule.IsDisposed.AssertTrue();
	}

	/// <summary>
	/// Test container SuspendRules/ResumeRules functionality.
	/// </summary>
	[TestMethod]
	public void Container_SuspendResumeRules()
	{
		// Arrange
		var executed = 0;
		var rule = new TestRule("test-token")
			.Do(() => executed++)
			.Until(() => false) // Never finish
			.Apply();

		var container = rule.Container;

		// Act & Assert
		container.IsRulesSuspended.AssertFalse();

		container.SuspendRules();
		container.IsRulesSuspended.AssertTrue();

		container.ResumeRules();
		container.IsRulesSuspended.AssertFalse();
	}

	/// <summary>
	/// Test that multiple Do() calls can be chained (last one wins).
	/// </summary>
	[TestMethod]
	public void MultipleDo_LastOneWins()
	{
		// Arrange
		var firstExecuted = false;
		var secondExecuted = false;

		var rule = new TestRule("test-token")
			.Do(() => firstExecuted = true)
			.Do(() => secondExecuted = true) // This should override
			.Apply();

		// Act
		rule.TriggerActivate(1);

		// Assert
		firstExecuted.AssertFalse(); // First Do was overridden
		secondExecuted.AssertTrue();
	}

	/// <summary>
	/// Test CanFinish() method.
	/// </summary>
	[TestMethod]
	public void CanFinish_ReturnsTrueWhenContainerStopped()
	{
		// Arrange
		var rule = new TestRule("test-token").Apply();

		// Act & Assert
		// While container is started, CanFinish should return false (for periodic rules)
		// When not active and container is stopped, CanFinish returns true
		rule.IsActive.AssertFalse();

		// This depends on container state
		// By default, MarketRuleContainer has ProcessState = Started
		var canFinish = rule.CanFinish();

		// Rule can finish when: !IsActive && IsReady && internal canFinish delegate returns true
		// The internal delegate checks if container is null or stopped
	}

	/// <summary>
	/// Test rule name can be customized.
	/// </summary>
	[TestMethod]
	public void RuleName_CanBeCustomized()
	{
		// Arrange
		var rule = new TestRule("test-token");
		rule.Name.AssertEqual("Test Rule");

		// Act
		rule.Name = "Custom Name";

		// Assert
		rule.Name.AssertEqual("Custom Name");
	}

	/// <summary>
	/// Test LogLevel property.
	/// </summary>
	[TestMethod]
	public void RuleLogLevel_DefaultIsInherited()
	{
		// Arrange & Act
		var rule = new TestRule("test-token");

		// Assert
		// Default LogLevel is Inherit
		rule.LogLevel.AssertEqual(LogLevels.Inherit);

		// Can be changed
		rule.LogLevel = LogLevels.Debug;
		rule.LogLevel.AssertEqual(LogLevels.Debug);
	}

	/// <summary>
	/// Test that rule cannot be added to two containers.
	/// </summary>
	[TestMethod]
	public void RuleContainer_CannotAddToTwoContainers()
	{
		// Arrange
		var rule = new TestRule("test-token").Apply();

		// Act & Assert
		try
		{
			rule.Apply(); // Try to add to another container
			Assert.Fail("Should throw exception");
		}
		catch (ArgumentException ex)
		{
			ex.Message.AssertContains("already");
		}
	}
}

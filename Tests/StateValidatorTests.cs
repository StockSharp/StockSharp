namespace StockSharp.Tests;

[TestClass]
public class StateValidatorTests : BaseTestClass
{
	#region OrderStates Tests

	[TestMethod]
	public void OrderStates_ValidTransitions()
	{
		// None -> all other states
		StateValidator.IsValid(OrderStates.None, OrderStates.Pending).AssertTrue();
		StateValidator.IsValid(OrderStates.None, OrderStates.Active).AssertTrue();
		StateValidator.IsValid(OrderStates.None, OrderStates.Done).AssertTrue();
		StateValidator.IsValid(OrderStates.None, OrderStates.Failed).AssertTrue();

		// Pending -> Active or Failed
		StateValidator.IsValid(OrderStates.Pending, OrderStates.Active).AssertTrue();
		StateValidator.IsValid(OrderStates.Pending, OrderStates.Failed).AssertTrue();

		// Active -> Done
		StateValidator.IsValid(OrderStates.Active, OrderStates.Done).AssertTrue();

		// Same state is valid
		StateValidator.IsValid(OrderStates.None, OrderStates.None).AssertTrue();
		StateValidator.IsValid(OrderStates.Pending, OrderStates.Pending).AssertTrue();
		StateValidator.IsValid(OrderStates.Active, OrderStates.Active).AssertTrue();
		StateValidator.IsValid(OrderStates.Done, OrderStates.Done).AssertTrue();
		StateValidator.IsValid(OrderStates.Failed, OrderStates.Failed).AssertTrue();
	}

	[TestMethod]
	public void OrderStates_InvalidTransitions()
	{
		// Done is terminal - can't transition out
		StateValidator.IsValid(OrderStates.Done, OrderStates.None).AssertFalse();
		StateValidator.IsValid(OrderStates.Done, OrderStates.Pending).AssertFalse();
		StateValidator.IsValid(OrderStates.Done, OrderStates.Active).AssertFalse();
		StateValidator.IsValid(OrderStates.Done, OrderStates.Failed).AssertFalse();

		// Failed is terminal - can't transition out
		StateValidator.IsValid(OrderStates.Failed, OrderStates.None).AssertFalse();
		StateValidator.IsValid(OrderStates.Failed, OrderStates.Pending).AssertFalse();
		StateValidator.IsValid(OrderStates.Failed, OrderStates.Active).AssertFalse();
		StateValidator.IsValid(OrderStates.Failed, OrderStates.Done).AssertFalse();

		// Active can only go to Done
		StateValidator.IsValid(OrderStates.Active, OrderStates.None).AssertFalse();
		StateValidator.IsValid(OrderStates.Active, OrderStates.Pending).AssertFalse();
		StateValidator.IsValid(OrderStates.Active, OrderStates.Failed).AssertFalse();

		// Pending can only go to Active or Failed
		StateValidator.IsValid(OrderStates.Pending, OrderStates.None).AssertFalse();
		StateValidator.IsValid(OrderStates.Pending, OrderStates.Done).AssertFalse();
	}

	[TestMethod]
	public void OrderStates_IsTerminal()
	{
		StateValidator.IsTerminal(OrderStates.None).AssertFalse();
		StateValidator.IsTerminal(OrderStates.Pending).AssertFalse();
		StateValidator.IsTerminal(OrderStates.Active).AssertFalse();
		StateValidator.IsTerminal(OrderStates.Done).AssertTrue();
		StateValidator.IsTerminal(OrderStates.Failed).AssertTrue();
	}

	[TestMethod]
	public void OrderStates_IsActive()
	{
		StateValidator.IsActive(OrderStates.None).AssertFalse();
		StateValidator.IsActive(OrderStates.Pending).AssertTrue();
		StateValidator.IsActive(OrderStates.Active).AssertTrue();
		StateValidator.IsActive(OrderStates.Done).AssertFalse();
		StateValidator.IsActive(OrderStates.Failed).AssertFalse();
	}

	[TestMethod]
	public void OrderStates_Validate_NullFrom_AlwaysValid()
	{
		// Null from means initial state, always valid
		StateValidator.Validate(null, OrderStates.None, "test", null).AssertTrue();
		StateValidator.Validate(null, OrderStates.Pending, "test", null).AssertTrue();
		StateValidator.Validate(null, OrderStates.Active, "test", null).AssertTrue();
		StateValidator.Validate(null, OrderStates.Done, "test", null).AssertTrue();
		StateValidator.Validate(null, OrderStates.Failed, "test", null).AssertTrue();
	}

	[TestMethod]
	public void OrderStates_Validate_ThrowsOnInvalid()
	{
		ThrowsExactly<InvalidOperationException>(() =>
			StateValidator.Validate(OrderStates.Done, OrderStates.Active, "test", null, throwOnInvalid: true));

		ThrowsExactly<InvalidOperationException>(() =>
			StateValidator.Validate(OrderStates.Failed, OrderStates.Pending, "test", null, throwOnInvalid: true));
	}

	[TestMethod]
	public void OrderStates_Validate_NoThrowWhenDisabled()
	{
		// Should return false but not throw
		var result = StateValidator.Validate(OrderStates.Done, OrderStates.Active, "test", null, throwOnInvalid: false);
		result.AssertFalse();
	}

	#endregion

	#region ChannelStates Tests

	[TestMethod]
	public void ChannelStates_ValidTransitions()
	{
		// Stopped -> Starting
		StateValidator.IsValid(ChannelStates.Stopped, ChannelStates.Starting).AssertTrue();

		// Starting -> Stopped, Started
		StateValidator.IsValid(ChannelStates.Starting, ChannelStates.Stopped).AssertTrue();
		StateValidator.IsValid(ChannelStates.Starting, ChannelStates.Started).AssertTrue();

		// Started -> Stopping, Suspending
		StateValidator.IsValid(ChannelStates.Started, ChannelStates.Stopping).AssertTrue();
		StateValidator.IsValid(ChannelStates.Started, ChannelStates.Suspending).AssertTrue();

		// Suspending -> Suspended
		StateValidator.IsValid(ChannelStates.Suspending, ChannelStates.Suspended).AssertTrue();

		// Suspended -> Starting, Stopping
		StateValidator.IsValid(ChannelStates.Suspended, ChannelStates.Starting).AssertTrue();
		StateValidator.IsValid(ChannelStates.Suspended, ChannelStates.Stopping).AssertTrue();

		// Stopping -> Stopped
		StateValidator.IsValid(ChannelStates.Stopping, ChannelStates.Stopped).AssertTrue();

		// Same state is valid
		StateValidator.IsValid(ChannelStates.Stopped, ChannelStates.Stopped).AssertTrue();
		StateValidator.IsValid(ChannelStates.Started, ChannelStates.Started).AssertTrue();
	}

	[TestMethod]
	public void ChannelStates_InvalidTransitions()
	{
		// Stopped -> Started (must go through Starting)
		StateValidator.IsValid(ChannelStates.Stopped, ChannelStates.Started).AssertFalse();

		// Started -> Stopped (must go through Stopping)
		StateValidator.IsValid(ChannelStates.Started, ChannelStates.Stopped).AssertFalse();

		// Started -> Suspended (must go through Suspending)
		StateValidator.IsValid(ChannelStates.Started, ChannelStates.Suspended).AssertFalse();
	}

	[TestMethod]
	public void ChannelStates_IsStopped()
	{
		StateValidator.IsStopped(ChannelStates.Stopped).AssertTrue();
		StateValidator.IsStopped(ChannelStates.Stopping).AssertFalse();
		StateValidator.IsStopped(ChannelStates.Starting).AssertFalse();
		StateValidator.IsStopped(ChannelStates.Started).AssertFalse();
		StateValidator.IsStopped(ChannelStates.Suspending).AssertFalse();
		StateValidator.IsStopped(ChannelStates.Suspended).AssertFalse();
	}

	[TestMethod]
	public void ChannelStates_IsActive()
	{
		StateValidator.IsActive(ChannelStates.Stopped).AssertFalse();
		StateValidator.IsActive(ChannelStates.Stopping).AssertFalse();
		StateValidator.IsActive(ChannelStates.Starting).AssertFalse();
		StateValidator.IsActive(ChannelStates.Started).AssertTrue();
		StateValidator.IsActive(ChannelStates.Suspending).AssertFalse();
		StateValidator.IsActive(ChannelStates.Suspended).AssertFalse();
	}

	[TestMethod]
	public void ChannelStates_Validate_ThrowsOnInvalid()
	{
		ThrowsExactly<InvalidOperationException>(() =>
			StateValidator.Validate(ChannelStates.Stopped, ChannelStates.Started, "test", null, throwOnInvalid: true));
	}

	#endregion

	#region SubscriptionStates Tests

	[TestMethod]
	public void SubscriptionStates_ValidTransitions()
	{
		// Stopped -> Active, Online, Error, Finished
		StateValidator.IsValid(SubscriptionStates.Stopped, SubscriptionStates.Active).AssertTrue();
		StateValidator.IsValid(SubscriptionStates.Stopped, SubscriptionStates.Online).AssertTrue();
		StateValidator.IsValid(SubscriptionStates.Stopped, SubscriptionStates.Error).AssertTrue();
		StateValidator.IsValid(SubscriptionStates.Stopped, SubscriptionStates.Finished).AssertTrue();

		// Active -> Online, Stopped, Error, Finished
		StateValidator.IsValid(SubscriptionStates.Active, SubscriptionStates.Online).AssertTrue();
		StateValidator.IsValid(SubscriptionStates.Active, SubscriptionStates.Stopped).AssertTrue();
		StateValidator.IsValid(SubscriptionStates.Active, SubscriptionStates.Error).AssertTrue();
		StateValidator.IsValid(SubscriptionStates.Active, SubscriptionStates.Finished).AssertTrue();

		// Online -> Stopped, Error, Finished (not back to Active)
		StateValidator.IsValid(SubscriptionStates.Online, SubscriptionStates.Stopped).AssertTrue();
		StateValidator.IsValid(SubscriptionStates.Online, SubscriptionStates.Error).AssertTrue();
		StateValidator.IsValid(SubscriptionStates.Online, SubscriptionStates.Finished).AssertTrue();
	}

	[TestMethod]
	public void SubscriptionStates_InvalidTransitions()
	{
		// Online -> Active is invalid
		StateValidator.IsValid(SubscriptionStates.Online, SubscriptionStates.Active).AssertFalse();

		// Error is terminal
		StateValidator.IsValid(SubscriptionStates.Error, SubscriptionStates.Stopped).AssertFalse();
		StateValidator.IsValid(SubscriptionStates.Error, SubscriptionStates.Active).AssertFalse();
		StateValidator.IsValid(SubscriptionStates.Error, SubscriptionStates.Online).AssertFalse();
		StateValidator.IsValid(SubscriptionStates.Error, SubscriptionStates.Finished).AssertFalse();

		// Finished is terminal
		StateValidator.IsValid(SubscriptionStates.Finished, SubscriptionStates.Stopped).AssertFalse();
		StateValidator.IsValid(SubscriptionStates.Finished, SubscriptionStates.Active).AssertFalse();
		StateValidator.IsValid(SubscriptionStates.Finished, SubscriptionStates.Online).AssertFalse();
		StateValidator.IsValid(SubscriptionStates.Finished, SubscriptionStates.Error).AssertFalse();
	}

	[TestMethod]
	public void SubscriptionStates_IsTerminal()
	{
		StateValidator.IsTerminal(SubscriptionStates.Stopped).AssertFalse();
		StateValidator.IsTerminal(SubscriptionStates.Active).AssertFalse();
		StateValidator.IsTerminal(SubscriptionStates.Online).AssertFalse();
		StateValidator.IsTerminal(SubscriptionStates.Error).AssertTrue();
		StateValidator.IsTerminal(SubscriptionStates.Finished).AssertTrue();
	}

	[TestMethod]
	public void SubscriptionStates_IsActive()
	{
		StateValidator.IsActive(SubscriptionStates.Stopped).AssertFalse();
		StateValidator.IsActive(SubscriptionStates.Active).AssertTrue();
		StateValidator.IsActive(SubscriptionStates.Online).AssertTrue();
		StateValidator.IsActive(SubscriptionStates.Error).AssertFalse();
		StateValidator.IsActive(SubscriptionStates.Finished).AssertFalse();
	}

	#endregion

	#region SessionStates Tests

	[TestMethod]
	public void SessionStates_ValidTransitions()
	{
		// Assigned -> all other states
		StateValidator.IsValid(SessionStates.Assigned, SessionStates.Active).AssertTrue();
		StateValidator.IsValid(SessionStates.Assigned, SessionStates.Paused).AssertTrue();
		StateValidator.IsValid(SessionStates.Assigned, SessionStates.ForceStopped).AssertTrue();
		StateValidator.IsValid(SessionStates.Assigned, SessionStates.Ended).AssertTrue();

		// Active -> Paused, ForceStopped, Ended
		StateValidator.IsValid(SessionStates.Active, SessionStates.Paused).AssertTrue();
		StateValidator.IsValid(SessionStates.Active, SessionStates.ForceStopped).AssertTrue();
		StateValidator.IsValid(SessionStates.Active, SessionStates.Ended).AssertTrue();

		// Paused -> Active, ForceStopped, Ended
		StateValidator.IsValid(SessionStates.Paused, SessionStates.Active).AssertTrue();
		StateValidator.IsValid(SessionStates.Paused, SessionStates.ForceStopped).AssertTrue();
		StateValidator.IsValid(SessionStates.Paused, SessionStates.Ended).AssertTrue();
	}

	[TestMethod]
	public void SessionStates_InvalidTransitions()
	{
		// ForceStopped is terminal
		StateValidator.IsValid(SessionStates.ForceStopped, SessionStates.Assigned).AssertFalse();
		StateValidator.IsValid(SessionStates.ForceStopped, SessionStates.Active).AssertFalse();
		StateValidator.IsValid(SessionStates.ForceStopped, SessionStates.Paused).AssertFalse();
		StateValidator.IsValid(SessionStates.ForceStopped, SessionStates.Ended).AssertFalse();

		// Ended is terminal
		StateValidator.IsValid(SessionStates.Ended, SessionStates.Assigned).AssertFalse();
		StateValidator.IsValid(SessionStates.Ended, SessionStates.Active).AssertFalse();
		StateValidator.IsValid(SessionStates.Ended, SessionStates.Paused).AssertFalse();
		StateValidator.IsValid(SessionStates.Ended, SessionStates.ForceStopped).AssertFalse();
	}

	[TestMethod]
	public void SessionStates_IsTerminal()
	{
		StateValidator.IsTerminal(SessionStates.Assigned).AssertFalse();
		StateValidator.IsTerminal(SessionStates.Active).AssertFalse();
		StateValidator.IsTerminal(SessionStates.Paused).AssertFalse();
		StateValidator.IsTerminal(SessionStates.ForceStopped).AssertTrue();
		StateValidator.IsTerminal(SessionStates.Ended).AssertTrue();
	}

	[TestMethod]
	public void SessionStates_IsActive()
	{
		StateValidator.IsActive(SessionStates.Assigned).AssertFalse();
		StateValidator.IsActive(SessionStates.Active).AssertTrue();
		StateValidator.IsActive(SessionStates.Paused).AssertFalse();
		StateValidator.IsActive(SessionStates.ForceStopped).AssertFalse();
		StateValidator.IsActive(SessionStates.Ended).AssertFalse();
	}

	#endregion

	#region Extension Methods Compatibility Tests

	[TestMethod]
	public void ExtensionMethods_ValidateChannelState_Works()
	{
		// Test that existing extension method works with new StateValidator
		ChannelStates.Stopped.ValidateChannelState(ChannelStates.Starting).AssertTrue();
		ChannelStates.Stopped.ValidateChannelState(ChannelStates.Started).AssertFalse();
	}

	[TestMethod]
	public void ExtensionMethods_VerifyOrderState_Works()
	{
		// Test that existing extension method works with new StateValidator
		OrderStates? none = null;
		none.VerifyOrderState(OrderStates.Pending, 123, null).AssertTrue();

		OrderStates? pending = OrderStates.Pending;
		pending.VerifyOrderState(OrderStates.Active, 123, null).AssertTrue();
		pending.VerifyOrderState(OrderStates.Done, 123, null).AssertFalse();
	}

	[TestMethod]
	public void ExtensionMethods_IsFinal_Works()
	{
		// Test that existing extension method works with new StateValidator
		OrderStates.Done.IsFinal().AssertTrue();
		OrderStates.Failed.IsFinal().AssertTrue();
		OrderStates.Active.IsFinal().AssertFalse();
		OrderStates.Pending.IsFinal().AssertFalse();
		OrderStates.None.IsFinal().AssertFalse();
	}

	[TestMethod]
	public void ExtensionMethods_SubscriptionStates_IsActive_Works()
	{
		// Test that existing extension method works with new StateValidator
		SubscriptionStates.Active.IsActive().AssertTrue();
		SubscriptionStates.Online.IsActive().AssertTrue();
		SubscriptionStates.Stopped.IsActive().AssertFalse();
		SubscriptionStates.Error.IsActive().AssertFalse();
		SubscriptionStates.Finished.IsActive().AssertFalse();
	}

	#endregion
}

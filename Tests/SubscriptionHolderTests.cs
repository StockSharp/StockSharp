namespace StockSharp.Tests;

[TestClass]
public class SubscriptionHolderTests
{
	// Test subscription implementation
	private class TestSubscription : ISubscription<string>, ISecurityIdMessage, IDataTypeMessage
	{
		public long Id { get; set; }
		public SubscriptionStates State { get; set; }
		public bool Suspend { get; set; }
		public IEnumerable<MessageTypes> Responses { get; set; } = [];
		public string Session { get; set; }
		public SecurityId SecurityId { get; set; }
		public DataType DataType { get; set; }
	}

	private static SubscriptionHolder<TestSubscription, string, long> CreateHolder()
	{
		var logger = new LogManager();
		return new SubscriptionHolder<TestSubscription, string, long>(logger.Application);
	}

	private static TestSubscription CreateSubscription(long id, string session, SecurityId securityId, DataType dataType, SubscriptionStates state = SubscriptionStates.Active)
	{
		return new TestSubscription
		{
			Id = id,
			Session = session,
			SecurityId = securityId,
			DataType = dataType,
			State = state,
			Responses = [MessageTypes.CandleTimeFrame]
		};
	}

	#region Constructor Tests

	[TestMethod]
	public void Constructor_NullLogs_Throws()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => new SubscriptionHolder<TestSubscription, string, long>(null));
	}

	#endregion

	#region Add Tests

	[TestMethod]
	public void Add_NullSubscription_Throws()
	{
		var holder = CreateHolder();
		Assert.ThrowsExactly<ArgumentNullException>(() => holder.Add(null));
	}

	[TestMethod]
	public void Add_ValidSubscription_CanRetrieveById()
	{
		var holder = CreateHolder();
		var subscription = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks);

		holder.Add(subscription);

		var retrieved = holder.TryGetById(1);
		retrieved.AssertNotNull();
		retrieved.Id.AssertEqual(1);
	}

	[TestMethod]
	public void Add_MultipleSubscriptions_AllRetrievable()
	{
		var holder = CreateHolder();
		var sub1 = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks);
		var sub2 = CreateSubscription(2, "session1", new SecurityId { SecurityCode = "MSFT" }, DataType.Ticks);
		var sub3 = CreateSubscription(3, "session2", new SecurityId { SecurityCode = "GOOGL" }, DataType.Level1);

		holder.Add(sub1);
		holder.Add(sub2);
		holder.Add(sub3);

		holder.TryGetById(1).AssertNotNull();
		holder.TryGetById(2).AssertNotNull();
		holder.TryGetById(3).AssertNotNull();
	}

	[TestMethod]
	public void Add_SubscriptionWithAllSecurity_Stored()
	{
		var holder = CreateHolder();
		var subscription = CreateSubscription(1, "session1", default(SecurityId), DataType.Ticks);

		holder.Add(subscription);

		holder.TryGetById(1).AssertNotNull();
	}

	#endregion

	#region GetSubscriptions Tests

	[TestMethod]
	public void GetSubscriptions_NullSession_Throws()
	{
		var holder = CreateHolder();
		Assert.ThrowsExactly<ArgumentNullException>(() => holder.GetSubscriptions((string)null));
	}

	[TestMethod]
	public void GetSubscriptions_BySession_EmptyHolder_ReturnsEmpty()
	{
		var holder = CreateHolder();
		var subscriptions = holder.GetSubscriptions((string)"session1").ToArray();

		subscriptions.Length.AssertEqual(0);
	}

	[TestMethod]
	public void GetSubscriptions_FiltersBySession()
	{
		var holder = CreateHolder();
		var sub1 = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks);
		var sub2 = CreateSubscription(2, "session1", new SecurityId { SecurityCode = "MSFT" }, DataType.Ticks);
		var sub3 = CreateSubscription(3, "session2", new SecurityId { SecurityCode = "GOOGL" }, DataType.Level1);

		holder.Add(sub1);
		holder.Add(sub2);
		holder.Add(sub3);

		var session1Subs = holder.GetSubscriptions((string)"session1").ToArray();
		session1Subs.Length.AssertEqual(2);
		session1Subs.Any(s => s.Id == 1).AssertTrue();
		session1Subs.Any(s => s.Id == 2).AssertTrue();

		var session2Subs = holder.GetSubscriptions((string)"session2").ToArray();
		session2Subs.Length.AssertEqual(1);
		session2Subs[0].Id.AssertEqual(3);
	}

	#endregion

	#region TryGetById Tests

	[TestMethod]
	public void TryGetById_NotFound_ReturnsNull()
	{
		var holder = CreateHolder();
		var result = holder.TryGetById(999);

		result.AssertNull();
	}

	[TestMethod]
	public void TryGetById_Found_ReturnsSubscription()
	{
		var holder = CreateHolder();
		var subscription = CreateSubscription(100, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks);

		holder.Add(subscription);

		var result = holder.TryGetById(100);
		result.AssertNotNull();
		result.Id.AssertEqual(100);
		result.Session.AssertEqual("session1");
	}

	#endregion

	#region TryGetSubscription Tests

	[TestMethod]
	public void TryGetSubscription_NotFound_ReturnsNull()
	{
		var holder = CreateHolder();
		var result = holder.TryGetSubscription(999, SubscriptionStates.Active);

		result.AssertNull();
	}

	[TestMethod]
	public void TryGetSubscription_NullState_ReturnsSubscription()
	{
		var holder = CreateHolder();
		var subscription = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks, SubscriptionStates.Active);

		holder.Add(subscription);

		var result = holder.TryGetSubscription(1, null);
		result.AssertNotNull();
	}

	[TestMethod]
	public void TryGetSubscription_MatchingState_ReturnsSubscription()
	{
		var holder = CreateHolder();
		var subscription = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks, SubscriptionStates.Active);

		holder.Add(subscription);

		var result = holder.TryGetSubscription(1, SubscriptionStates.Active);
		result.AssertNotNull();
	}

	#endregion

	#region TryGetSubscriptionAndStop Tests

	[TestMethod]
	public void TryGetSubscriptionAndStop_NotFound_ReturnsNull()
	{
		var holder = CreateHolder();
		var result = holder.TryGetSubscriptionAndStop(999);

		result.AssertNull();
	}

	[TestMethod]
	public void TryGetSubscriptionAndStop_Found_ReturnsAndChangesState()
	{
		var holder = CreateHolder();
		var subscription = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks, SubscriptionStates.Active);

		holder.Add(subscription);

		var result = holder.TryGetSubscriptionAndStop(1);
		result.AssertNotNull();
		result.State.AssertEqual(SubscriptionStates.Stopped);
	}

	#endregion

	#region Remove Tests

	[TestMethod]
	public void Remove_BySubscription_RemovesFromHolder()
	{
		var holder = CreateHolder();
		var subscription = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks);

		holder.Add(subscription);
		holder.TryGetById(1).AssertNotNull();

		holder.Remove(subscription);

		holder.TryGetById(1).AssertNull();
	}

	[TestMethod]
	public void Remove_NullSubscription_DoesNotThrow()
	{
		var holder = CreateHolder();
		Assert.ThrowsExactly<ArgumentNullException>(() => holder.Remove((TestSubscription)null));
	}

	[TestMethod]
	public void Remove_BySession_RemovesAllForSession()
	{
		var holder = CreateHolder();
		var sub1 = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks);
		var sub2 = CreateSubscription(2, "session1", new SecurityId { SecurityCode = "MSFT" }, DataType.Ticks);
		var sub3 = CreateSubscription(3, "session2", new SecurityId { SecurityCode = "GOOGL" }, DataType.Level1);

		holder.Add(sub1);
		holder.Add(sub2);
		holder.Add(sub3);

		var removed = holder.Remove("session1").ToArray();

		removed.Length.AssertEqual(2);
		holder.TryGetById(1).AssertNull();
		holder.TryGetById(2).AssertNull();
		holder.TryGetById(3).AssertNotNull();
	}

	[TestMethod]
	public void Remove_BySession_NullSession_Throws()
	{
		var holder = CreateHolder();
		Assert.ThrowsExactly<ArgumentNullException>(() => holder.Remove((string)null));
	}

	[TestMethod]
	public void Remove_NonExistentSession_ReturnsEmpty()
	{
		var holder = CreateHolder();
		var removed = holder.Remove("nonexistent").ToArray();

		removed.Length.AssertEqual(0);
	}

	#endregion

	#region Clear Tests

	[TestMethod]
	public void Clear_RemovesAllSubscriptions()
	{
		var holder = CreateHolder();
		var sub1 = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks);
		var sub2 = CreateSubscription(2, "session2", new SecurityId { SecurityCode = "MSFT" }, DataType.Ticks);

		holder.Add(sub1);
		holder.Add(sub2);

		holder.Clear();

		holder.TryGetById(1).AssertNull();
		holder.TryGetById(2).AssertNull();
		holder.GetSubscriptions((string)"session1").Count().AssertEqual(0);
		holder.GetSubscriptions((string)"session2").Count().AssertEqual(0);
	}

	#endregion

	#region HasSubscriptions Tests

	[TestMethod]
	public void HasSubscriptions_NoSubscriptions_ReturnsFalse()
	{
		var holder = CreateHolder();
		var result = holder.HasSubscriptions(DataType.Ticks, new SecurityId { SecurityCode = "AAPL" });

		result.AssertFalse();
	}

	[TestMethod]
	public void HasSubscriptions_MatchingSubscription_ReturnsTrue()
	{
		var holder = CreateHolder();
		var securityId = new SecurityId { SecurityCode = "AAPL" };
		var subscription = CreateSubscription(1, "session1", securityId, DataType.Ticks);

		holder.Add(subscription);

		var result = holder.HasSubscriptions(DataType.Ticks, securityId);
		result.AssertTrue();
	}

	[TestMethod]
	public void HasSubscriptions_DifferentDataType_ReturnsFalse()
	{
		var holder = CreateHolder();
		var securityId = new SecurityId { SecurityCode = "AAPL" };
		var subscription = CreateSubscription(1, "session1", securityId, DataType.Ticks);

		holder.Add(subscription);

		var result = holder.HasSubscriptions(DataType.Level1, securityId);
		result.AssertFalse();
	}

	[TestMethod]
	public void HasSubscriptions_DifferentSecurityId_ReturnsFalse()
	{
		var holder = CreateHolder();
		var subscription = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks);

		holder.Add(subscription);

		var result = holder.HasSubscriptions(DataType.Ticks, new SecurityId { SecurityCode = "MSFT" });
		result.AssertFalse();
	}

	#endregion

	#region AddUnsubscribeRequest Tests

	[TestMethod]
	public void AddUnsubscribeRequest_StoresRequest()
	{
		var holder = CreateHolder();
		// Should not throw - just stores the unsubscribe request mapping
		holder.AddUnsubscribeRequest(100, 50);
	}

	#endregion

	#region SubscriptionChanged Event Tests

	[TestMethod]
	public void SubscriptionChanged_TriggeredOnStateChange()
	{
		var holder = CreateHolder();
		var subscription = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks, SubscriptionStates.Active);

		holder.Add(subscription);

		TestSubscription changedSub = null;
		holder.SubscriptionChanged += (sub) => changedSub = sub;

		holder.TryGetSubscriptionAndStop(1);

		changedSub.AssertNotNull();
		changedSub.Id.AssertEqual(1);
		changedSub.State.AssertEqual(SubscriptionStates.Stopped);
	}

	#endregion

	#region Multiple Sessions Tests

	[TestMethod]
	public void MultipleSessions_IsolatedCorrectly()
	{
		var holder = CreateHolder();
		var sub1 = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks);
		var sub2 = CreateSubscription(2, "session2", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks);

		holder.Add(sub1);
		holder.Add(sub2);

		var session1Subs = holder.GetSubscriptions((string)"session1").ToArray();
		var session2Subs = holder.GetSubscriptions((string)"session2").ToArray();

		session1Subs.Length.AssertEqual(1);
		session2Subs.Length.AssertEqual(1);

		holder.Remove("session1");

		holder.TryGetById(1).AssertNull();
		holder.TryGetById(2).AssertNotNull();
	}

	#endregion

	#region Edge Cases

	[TestMethod]
	public void Add_DuplicateId_SecondOverwritesFirst()
	{
		var holder = CreateHolder();
		var sub1 = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks);
		var sub2 = CreateSubscription(1, "session2", new SecurityId { SecurityCode = "MSFT" }, DataType.Level1);

		holder.Add(sub1);
		Assert.ThrowsExactly<ArgumentException>(() => holder.Add(sub2));
		
		var retrieved = holder.TryGetById(1);
		retrieved.AssertNotNull();
		// Should be the second subscription
		retrieved.Session.AssertEqual("session1");
		retrieved.SecurityId.SecurityCode.AssertEqual("AAPL");
	}

	[TestMethod]
	public void HasSubscriptions_AllSecurity_Checked()
	{
		var holder = CreateHolder();
		var subscription = CreateSubscription(1, "session1", default(SecurityId), DataType.Ticks);

		holder.Add(subscription);

		var result = holder.HasSubscriptions(DataType.Ticks, default(SecurityId));
		result.AssertTrue();
	}

	#endregion
}

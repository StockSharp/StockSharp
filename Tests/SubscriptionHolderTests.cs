namespace StockSharp.Tests;

[TestClass]
public class SubscriptionHolderTests : BaseTestClass
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

	private static SubscriptionHolder<TestSubscription, string> CreateHolder()
	{
		var logger = new LogManager();
		return new(logger.Application);
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
		ThrowsExactly<ArgumentNullException>(() => new SubscriptionHolder<TestSubscription, string>(null));
	}

	#endregion

	#region Add Tests

	[TestMethod]
	public void Add_NullSubscription_Throws()
	{
		using var holder = CreateHolder();
		ThrowsExactly<ArgumentNullException>(() => holder.Add(null));
	}

	[TestMethod]
	public void Add_ValidSubscription_CanRetrieveById()
	{
		using var holder = CreateHolder();
		var subscription = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks);

		holder.Add(subscription);

		holder.TryGetById(1, out var retrieved).AssertTrue();
		retrieved.AssertNotNull();
		retrieved.Id.AssertEqual(1);
	}

	[TestMethod]
	public void Add_MultipleSubscriptions_AllRetrievable()
	{
		using var holder = CreateHolder();
		var sub1 = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks);
		var sub2 = CreateSubscription(2, "session1", new SecurityId { SecurityCode = "MSFT" }, DataType.Ticks);
		var sub3 = CreateSubscription(3, "session2", new SecurityId { SecurityCode = "GOOGL" }, DataType.Level1);

		holder.Add(sub1);
		holder.Add(sub2);
		holder.Add(sub3);

		holder.TryGetById(1, out _).AssertTrue();
		holder.TryGetById(2, out _).AssertTrue();
		holder.TryGetById(3, out _).AssertTrue();
	}

	[TestMethod]
	public void Add_SubscriptionWithAllSecurity_Stored()
	{
		using var holder = CreateHolder();
		var subscription = CreateSubscription(1, "session1", default, DataType.Ticks);

		holder.Add(subscription);

		holder.TryGetById(1, out _).AssertTrue();
	}

	#endregion

	#region GetSubscriptions Tests

	[TestMethod]
	public void GetSubscriptions_NullSession_Throws()
	{
		using var holder = CreateHolder();
		ThrowsExactly<ArgumentNullException>(() => holder.GetSubscriptions((string)null));
	}

	[TestMethod]
	public void GetSubscriptions_BySession_EmptyHolder_ReturnsEmpty()
	{
		using var holder = CreateHolder();
		var subscriptions = holder.GetSubscriptions("session1").ToArray();

		subscriptions.Length.AssertEqual(0);
	}

	[TestMethod]
	public void GetSubscriptions_FiltersBySession()
	{
		using var holder = CreateHolder();
		var sub1 = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks);
		var sub2 = CreateSubscription(2, "session1", new SecurityId { SecurityCode = "MSFT" }, DataType.Ticks);
		var sub3 = CreateSubscription(3, "session2", new SecurityId { SecurityCode = "GOOGL" }, DataType.Level1);

		holder.Add(sub1);
		holder.Add(sub2);
		holder.Add(sub3);

		var session1Subs = holder.GetSubscriptions("session1").ToArray();
		session1Subs.Length.AssertEqual(2);
		session1Subs.Count(s => s.Id == 1).AssertEqual(1);
		session1Subs.Count(s => s.Id == 2).AssertEqual(1);

		var session2Subs = holder.GetSubscriptions("session2").ToArray();
		session2Subs.Length.AssertEqual(1);
		session2Subs[0].Id.AssertEqual(3);
	}

	#endregion

	#region TryGetById Tests

	[TestMethod]
	public void TryGetById_NotFound_ReturnsNull()
	{
		using var holder = CreateHolder();
		var result = holder.TryGetById(999, out var notFound);

		result.AssertFalse();
		notFound.AssertNull();
	}

	[TestMethod]
	public void TryGetById_Found_ReturnsSubscription()
	{
		using var holder = CreateHolder();
		var subscription = CreateSubscription(100, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks);

		holder.Add(subscription);

		holder.TryGetById(100, out var result).AssertTrue();
		result.AssertNotNull();
		result.Id.AssertEqual(100);
		result.Session.AssertEqual("session1");
	}

	#endregion

	#region TryGetSubscription Tests

	[TestMethod]
	public void TryGetSubscription_NotFound_ReturnsNull()
	{
		using var holder = CreateHolder();
		var ok = holder.TryGetSubscription(999, SubscriptionStates.Active, out var result);

		ok.AssertFalse();
		result.AssertNull();
	}

	[TestMethod]
	public void TryGetSubscription_NullState_ReturnsSubscription()
	{
		using var holder = CreateHolder();
		var subscription = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks, SubscriptionStates.Active);

		holder.Add(subscription);

		holder.TryGetSubscription(1, null, out var result).AssertTrue();
		result.AssertNotNull();
	}

	[TestMethod]
	public void TryGetSubscription_MatchingState_ReturnsSubscription()
	{
		using var holder = CreateHolder();
		var subscription = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks, SubscriptionStates.Active);

		holder.Add(subscription);

		holder.TryGetSubscription(1, SubscriptionStates.Active, out var result).AssertTrue();
		result.AssertNotNull();
	}

	#endregion

	#region TryGetSubscriptionAndStop Tests

	[TestMethod]
	public void TryGetSubscriptionAndStop_NotFound_ReturnsNull()
	{
		using var holder = CreateHolder();
		var ok = holder.TryGetSubscriptionAndStop(999, out var result);

		ok.AssertFalse();
		result.AssertNull();
	}

	[TestMethod]
	public void TryGetSubscriptionAndStop_Found_ReturnsAndChangesState()
	{
		using var holder = CreateHolder();
		var subscription = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks, SubscriptionStates.Active);

		holder.Add(subscription);

		holder.TryGetSubscriptionAndStop(1, out var result).AssertTrue();
		result.AssertNotNull();
		result.State.AssertEqual(SubscriptionStates.Stopped);
	}

	#endregion

	#region Remove Tests

	[TestMethod]
	public void Remove_BySubscription_RemovesFromHolder()
	{
		using var holder = CreateHolder();
		var subscription = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks);

		holder.Add(subscription);
		holder.TryGetById(1, out _).AssertTrue();

		holder.Remove(subscription);

		holder.TryGetById(1, out var shouldBeNull).AssertFalse();
		shouldBeNull.AssertNull();
	}

	[TestMethod]
	public void Remove_NullSubscription_Throws()
	{
		using var holder = CreateHolder();
		ThrowsExactly<ArgumentNullException>(() => holder.Remove((TestSubscription)null));
	}

	[TestMethod]
	public void Remove_BySession_RemovesAllForSession()
	{
		using var holder = CreateHolder();
		var sub1 = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks);
		var sub2 = CreateSubscription(2, "session1", new SecurityId { SecurityCode = "MSFT" }, DataType.Ticks);
		var sub3 = CreateSubscription(3, "session2", new SecurityId { SecurityCode = "GOOGL" }, DataType.Level1);

		holder.Add(sub1);
		holder.Add(sub2);
		holder.Add(sub3);

		var removed = holder.Remove("session1").ToArray();

		removed.Length.AssertEqual(2);
		holder.TryGetById(1, out var _c1).AssertFalse();
		_c1.AssertNull();
		holder.TryGetById(2, out var _c2).AssertFalse();
		_c2.AssertNull();
		holder.TryGetById(3, out _).AssertTrue();
	}

	[TestMethod]
	public void Remove_BySession_NullSession_Throws()
	{
		using var holder = CreateHolder();
		ThrowsExactly<ArgumentNullException>(() => holder.Remove((string)null));
	}

	[TestMethod]
	public void Remove_NonExistentSession_ReturnsEmpty()
	{
		using var holder = CreateHolder();
		var removed = holder.Remove("nonexistent").ToArray();

		removed.Length.AssertEqual(0);
	}

	#endregion

	#region Clear Tests

	[TestMethod]
	public void Clear_RemovesAllSubscriptions()
	{
		using var holder = CreateHolder();
		var sub1 = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks);
		var sub2 = CreateSubscription(2, "session2", new SecurityId { SecurityCode = "MSFT" }, DataType.Ticks);

		holder.Add(sub1);
		holder.Add(sub2);

		holder.Clear();

		holder.TryGetById(1, out var _a).AssertFalse();
		_a.AssertNull();
		holder.TryGetById(2, out var _b).AssertFalse();
		_b.AssertNull();
		holder.GetSubscriptions("session1").Count().AssertEqual(0);
		holder.GetSubscriptions("session2").Count().AssertEqual(0);
	}

	#endregion

	#region HasSubscriptions Tests

	[TestMethod]
	public void HasSubscriptions_NoSubscriptions_ReturnsFalse()
	{
		using var holder = CreateHolder();
		var result = holder.HasSubscriptions(DataType.Ticks, new SecurityId { SecurityCode = "AAPL" });

		result.AssertFalse();
	}

	[TestMethod]
	public void HasSubscriptions_MatchingSubscription_ReturnsTrue()
	{
		using var holder = CreateHolder();
		var securityId = new SecurityId { SecurityCode = "AAPL" };
		var subscription = CreateSubscription(1, "session1", securityId, DataType.Ticks);

		holder.Add(subscription);

		var result = holder.HasSubscriptions(DataType.Ticks, securityId);
		result.AssertTrue();
	}

	[TestMethod]
	public void HasSubscriptions_DifferentDataType_ReturnsFalse()
	{
		using var holder = CreateHolder();
		var securityId = new SecurityId { SecurityCode = "AAPL" };
		var subscription = CreateSubscription(1, "session1", securityId, DataType.Ticks);

		holder.Add(subscription);

		var result = holder.HasSubscriptions(DataType.Level1, securityId);
		result.AssertFalse();
	}

	[TestMethod]
	public void HasSubscriptions_DifferentSecurityId_ReturnsFalse()
	{
		using var holder = CreateHolder();
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
		using var holder = CreateHolder();
		// Should not throw - just stores the unsubscribe request mapping
		holder.AddUnsubscribeRequest(100, 50);
	}

	#endregion

	#region SubscriptionChanged Event Tests

	[TestMethod]
	public void SubscriptionChanged_TriggeredOnStateChange()
	{
		using var holder = CreateHolder();
		var subscription = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks, SubscriptionStates.Active);

		holder.Add(subscription);

		TestSubscription changedSub = null;
		holder.SubscriptionChanged += (sub) => changedSub = sub;

		holder.TryGetSubscriptionAndStop(1, out _);

		changedSub.AssertNotNull();
		changedSub.Id.AssertEqual(1);
		changedSub.State.AssertEqual(SubscriptionStates.Stopped);
	}

	#endregion

	#region Multiple Sessions Tests

	[TestMethod]
	public void MultipleSessions_IsolatedCorrectly()
	{
		using var holder = CreateHolder();
		var sub1 = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks);
		var sub2 = CreateSubscription(2, "session2", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks);

		holder.Add(sub1);
		holder.Add(sub2);

		var session1Subs = holder.GetSubscriptions("session1").ToArray();
		var session2Subs = holder.GetSubscriptions("session2").ToArray();

		session1Subs.Length.AssertEqual(1);
		session2Subs.Length.AssertEqual(1);

		holder.Remove("session1");

		holder.TryGetById(1, out var _c3).AssertFalse();
		_c3.AssertNull();
		holder.TryGetById(2, out _).AssertTrue();
	}

	#endregion

	#region Edge Cases

	[TestMethod]
	public void Add_DuplicateId_Throws_And_KeepsFirst()
	{
		using var holder = CreateHolder();
		var sub1 = CreateSubscription(1, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks);
		var sub2 = CreateSubscription(1, "session2", new SecurityId { SecurityCode = "MSFT" }, DataType.Level1);

		holder.Add(sub1);
		ThrowsExactly<ArgumentException>(() => holder.Add(sub2));
		
		holder.TryGetById(1, out var retrieved).AssertTrue();
		retrieved.AssertNotNull();
		// Should be the first subscription
		retrieved.Session.AssertEqual("session1");
		retrieved.SecurityId.SecurityCode.AssertEqual("AAPL");
	}

	[TestMethod]
	public void HasSubscriptions_AllSecurity_Checked()
	{
		using var holder = CreateHolder();
		var subscription = CreateSubscription(1, "session1", default, DataType.Ticks);

		holder.Add(subscription);

		var result = holder.HasSubscriptions(DataType.Ticks, default);
		result.AssertTrue();
	}

	[TestMethod]
	public void TryGetSubscription_ActiveState_DoesNotRemove()
	{
		using var holder = CreateHolder();
		var subscription = CreateSubscription(10, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks, SubscriptionStates.Active);

		holder.Add(subscription);

		holder.TryGetSubscription(10, SubscriptionStates.Active, out var result).AssertTrue();
		result.AssertNotNull();

		holder.TryGetById(10, out var stillThere).AssertTrue();
		stillThere.AssertNotNull();
	}

	[TestMethod]
	public void TryGetSubscriptionAndStop_RemovesFromHolder()
	{
		using var holder = CreateHolder();
		var subscription = CreateSubscription(11, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks, SubscriptionStates.Active);

		holder.Add(subscription);

		holder.TryGetSubscriptionAndStop(11, out var result).AssertTrue();
		result.AssertNotNull();
		result.State.AssertEqual(SubscriptionStates.Stopped);

		holder.TryGetById(11, out var removed).AssertFalse();
		removed.AssertNull();
	}

	[TestMethod]
	public void Remove_BySession_RaisesEventAndSetsStopped()
	{
		using var holder = CreateHolder();
		var sub1 = CreateSubscription(21, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks, SubscriptionStates.Active);
		var sub2 = CreateSubscription(22, "session1", new SecurityId { SecurityCode = "MSFT" }, DataType.Ticks, SubscriptionStates.Active);
		var sub3 = CreateSubscription(23, "session2", new SecurityId { SecurityCode = "GOOGL" }, DataType.Level1, SubscriptionStates.Active);

		holder.Add(sub1);
		holder.Add(sub2);
		holder.Add(sub3);

		var events = new List<TestSubscription>();
		holder.SubscriptionChanged += s => events.Add(s);

		var removed = holder.Remove("session1").ToArray();

		removed.Length.AssertEqual(2);
		removed.Count(s => s.Id == 21).AssertEqual(1);
		removed.Count(s => s.Id == 22).AssertEqual(1);
		removed.All(s => s.State == SubscriptionStates.Stopped).AssertTrue();

		// event raised for both removed items
		events.Count(e => e.Id == 21 || e.Id == 22).AssertEqual(2);
	}

	[TestMethod]
	public void SubscriptionChanged_TriggeredOnAdd()
	{
		using var holder = CreateHolder();
		var subscription = CreateSubscription(30, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks, SubscriptionStates.Active);

		TestSubscription added = null;
		holder.SubscriptionChanged += s => added = s;

		holder.Add(subscription);

		added.AssertNotNull();
		added.Id.AssertEqual(30);
		added.State.AssertEqual(SubscriptionStates.Active);
	}

	[TestMethod]
	public void TryGetSubscription_ErrorState_RemovesAndRaisesEvent()
	{
		using var holder = CreateHolder();
		var subscription = CreateSubscription(12, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks, SubscriptionStates.Active);

		holder.Add(subscription);

		TestSubscription changed = null;
		holder.SubscriptionChanged += s => changed = s;

		holder.TryGetSubscription(12, SubscriptionStates.Error, out var result).AssertTrue();
		result.AssertNotNull();
		result.State.AssertEqual(SubscriptionStates.Error);
		changed.AssertNotNull();
		changed.Id.AssertEqual(12);

		holder.TryGetById(12, out var removed).AssertFalse();
		removed.AssertNull();
	}

	[TestMethod]
	public void GetSubscriptions_BySession_IncludesIdZero()
	{
		using var holder = CreateHolder();
		var subWithId = CreateSubscription(40, "session1", new SecurityId { SecurityCode = "AAPL" }, DataType.Ticks, SubscriptionStates.Active);
		var subNoId = CreateSubscription(0, "session1", new SecurityId { SecurityCode = "MSFT" }, DataType.Level1, SubscriptionStates.Active);

		holder.Add(subWithId);
		holder.Add(subNoId);

		var sessionSubs = holder.GetSubscriptions("session1").ToArray();
		sessionSubs.Length.AssertEqual(2);
		sessionSubs.Count(s => s.Id == 40).AssertEqual(1);
		sessionSubs.Count(s => s.Id == 0 && s.SecurityId.SecurityCode == "MSFT").AssertEqual(1);
	}

	#endregion

	#region GetSubscriptions(Message) — Transactions Execution targeting

	private static TestSubscription CreateOrderStatusSub(long id, string session, SubscriptionStates state)
		=> new()
		{
			Id = id,
			Session = session,
			DataType = DataType.Transactions,
			State = state,
			Responses = [MessageTypes.Execution],
		};

	[TestMethod]
	public void GetSubscriptions_TransactionsExecution_OrigTxIdNonZero_OnlineSub_ReturnsOne()
	{
		using var holder = CreateHolder();
		holder.Add(CreateOrderStatusSub(100, "sessionA", SubscriptionStates.Online));
		holder.Add(CreateOrderStatusSub(101, "sessionB", SubscriptionStates.Online));

		var exec = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 100,
			OrderState = OrderStates.Active,
		};

		var matched = holder.GetSubscriptions(exec).ToArray();

		matched.Length.AssertEqual(1);
		matched[0].Id.AssertEqual(100);
	}

	[TestMethod]
	public void GetSubscriptions_TransactionsExecution_OrigTxIdNonZero_StoppedSub_FansOutToOnlineSiblings()
	{
		using var holder = CreateHolder();
		holder.Add(CreateOrderStatusSub(200, "sessionA", SubscriptionStates.Stopped));
		holder.Add(CreateOrderStatusSub(201, "sessionB", SubscriptionStates.Online));

		var exec = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 200,
			OrderState = OrderStates.Done,
		};

		var matched = holder.GetSubscriptions(exec).ToArray();

		matched.Length.AssertEqual(1);
		matched[0].Id.AssertEqual(201);
	}

	[TestMethod]
	public void GetSubscriptions_TransactionsExecution_OrigTxIdNonZero_Unknown_FansOutToAllOnline()
	{
		using var holder = CreateHolder();
		holder.Add(CreateOrderStatusSub(300, "sessionA", SubscriptionStates.Online));
		holder.Add(CreateOrderStatusSub(301, "sessionB", SubscriptionStates.Online));

		var exec = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 99999,
		};

		var matched = holder.GetSubscriptions(exec).ToArray();

		matched.Length.AssertEqual(2);
		matched.Select(s => s.Id).OrderBy(i => i).SequenceEqual([300L, 301L]).AssertTrue();
	}

	[TestMethod]
	public void GetSubscriptions_TransactionsExecution_OrigTxIdZero_FansOutToAllOnline()
	{
		using var holder = CreateHolder();
		holder.Add(CreateOrderStatusSub(400, "sessionA", SubscriptionStates.Online));
		holder.Add(CreateOrderStatusSub(401, "sessionB", SubscriptionStates.Online));
		holder.Add(CreateOrderStatusSub(402, "sessionC", SubscriptionStates.Stopped));

		var exec = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 0,
		};

		var matched = holder.GetSubscriptions(exec).ToArray();

		matched.Length.AssertEqual(2);
		matched.Select(s => s.Id).OrderBy(i => i).SequenceEqual([400L, 401L]).AssertTrue();
	}

	[TestMethod]
	public void GetSubscriptions_TransactionsExecution_OrigTxIdZero_SkipsSuspended()
	{
		using var holder = CreateHolder();
		var suspended = CreateOrderStatusSub(500, "sessionA", SubscriptionStates.Online);
		suspended.Suspend = true;
		holder.Add(suspended);
		holder.Add(CreateOrderStatusSub(501, "sessionB", SubscriptionStates.Online));

		var exec = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 0,
		};

		var matched = holder.GetSubscriptions(exec).ToArray();

		matched.Length.AssertEqual(1);
		matched[0].Id.AssertEqual(501);
	}

	[TestMethod]
	public void GetSubscriptions_TransactionsExecution_FollowUpByExecutionTxId_ReachesAllOwners()
	{
		using var holder = CreateHolder();
		var subA = CreateOrderStatusSub(700, "sessionA", SubscriptionStates.Online);
		var subB = CreateOrderStatusSub(701, "sessionB", SubscriptionStates.Online);
		holder.Add(subA);
		holder.Add(subB);

		const long orderTxId = 12345L;

		holder.GetSubscriptions(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 700,
			TransactionId = orderTxId,
		}).Count().AssertEqual(1);

		holder.GetSubscriptions(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 701,
			TransactionId = orderTxId,
		}).Count().AssertEqual(1);

		var matched = holder.GetSubscriptions(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = orderTxId,
			TradeId = 99,
		}).ToArray();

		matched.Length.AssertEqual(2);
		matched.Select(s => s.Id).OrderBy(i => i).SequenceEqual([700L, 701L]).AssertTrue();
	}

	[TestMethod]
	public void GetSubscriptions_TransactionsExecution_OrigTxIdZero_FiltersByDataType()
	{
		using var holder = CreateHolder();
		holder.Add(CreateOrderStatusSub(600, "sessionA", SubscriptionStates.Online));
		var ticks = new TestSubscription
		{
			Id = 601,
			Session = "sessionB",
			DataType = DataType.Ticks,
			State = SubscriptionStates.Online,
			Responses = [MessageTypes.Execution],
		};
		holder.Add(ticks);

		var exec = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 0,
		};

		var matched = holder.GetSubscriptions(exec).ToArray();

		matched.Length.AssertEqual(1);
		matched[0].Id.AssertEqual(600);
	}

	#endregion
}

namespace StockSharp.Tests;

[TestClass]
public class SnapshotHolderManagerTests : BaseTestClass
{
	private sealed class TestSnapshotHolder : ISnapshotHolder
	{
		public List<ISubscriptionMessage> GetSnapshotCalls { get; } = [];
		public List<Message> SnapshotsToReturn { get; set; } = [];

		public IEnumerable<Message> GetSnapshot(ISubscriptionMessage subscription)
		{
			GetSnapshotCalls.Add(subscription);
			return SnapshotsToReturn;
		}
	}

	[TestMethod]
	public void ProcessInMessage_Reset_ClearsPendingSubscriptions()
	{
		var holder = new TestSnapshotHolder();
		var manager = new SnapshotHolderManager(holder);

		var subscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Level1,
		};

		manager.ProcessInMessage(subscribe);
		var (toInner, toOut) = manager.ProcessInMessage(new ResetMessage());

		toInner.Length.AssertEqual(1);
		toInner[0].Type.AssertEqual(MessageTypes.Reset);
		toOut.Length.AssertEqual(0);

		var online = new SubscriptionOnlineMessage { OriginalTransactionId = 100 };
		var (forward, extraOut) = manager.ProcessOutMessage(online);

		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessInMessage_MarketData_Level1_AddsToPending()
	{
		var holder = new TestSnapshotHolder();
		var manager = new SnapshotHolderManager(holder);

		var subscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Level1,
		};

		var (toInner, toOut) = manager.ProcessInMessage(subscribe);

		toInner.Length.AssertEqual(1);
		toInner[0].AssertSame(subscribe);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessInMessage_MarketData_DefaultSecurityId_SkipsAdding()
	{
		var holder = new TestSnapshotHolder();
		var manager = new SnapshotHolderManager(holder);

		var subscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = default,
			DataType2 = DataType.Level1,
		};

		manager.ProcessInMessage(subscribe);

		holder.SnapshotsToReturn = [new Level1ChangeMessage { SecurityId = Helper.CreateSecurityId() }];
		var online = new SubscriptionOnlineMessage { OriginalTransactionId = 100 };
		var (forward, extraOut) = manager.ProcessOutMessage(online);

		extraOut.Length.AssertEqual(0);
		holder.GetSnapshotCalls.Count.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessInMessage_MarketData_Unsubscribe_RemovesFromPending()
	{
		var holder = new TestSnapshotHolder();
		var manager = new SnapshotHolderManager(holder);

		var subscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Level1,
		};
		manager.ProcessInMessage(subscribe);

		var unsubscribe = new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 101,
			OriginalTransactionId = 100,
		};
		manager.ProcessInMessage(unsubscribe);

		holder.SnapshotsToReturn = [new Level1ChangeMessage { SecurityId = Helper.CreateSecurityId() }];
		var online = new SubscriptionOnlineMessage { OriginalTransactionId = 100 };
		var (forward, extraOut) = manager.ProcessOutMessage(online);

		extraOut.Length.AssertEqual(0);
		holder.GetSnapshotCalls.Count.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessOutMessage_SubscriptionResponse_Error_RemovesFromPending()
	{
		var holder = new TestSnapshotHolder();
		var manager = new SnapshotHolderManager(holder);

		var subscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Level1,
		};
		manager.ProcessInMessage(subscribe);

		var response = new SubscriptionResponseMessage
		{
			OriginalTransactionId = 100,
			Error = new InvalidOperationException("Test error"),
		};
		var (forward, extraOut) = manager.ProcessOutMessage(response);

		forward.AssertSame(response);
		extraOut.Length.AssertEqual(0);

		holder.SnapshotsToReturn = [new Level1ChangeMessage { SecurityId = Helper.CreateSecurityId() }];
		var online = new SubscriptionOnlineMessage { OriginalTransactionId = 100 };
		var (forward2, extraOut2) = manager.ProcessOutMessage(online);

		extraOut2.Length.AssertEqual(0);
		holder.GetSnapshotCalls.Count.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessOutMessage_SubscriptionOnline_ReturnsSnapshots()
	{
		var holder = new TestSnapshotHolder();
		var manager = new SnapshotHolderManager(holder);

		var secId = Helper.CreateSecurityId();

		var subscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Level1,
		};
		manager.ProcessInMessage(subscribe);

		var snapshot1 = new Level1ChangeMessage { SecurityId = secId };
		var snapshot2 = new Level1ChangeMessage { SecurityId = secId };
		holder.SnapshotsToReturn = [snapshot1, snapshot2];

		var online = new SubscriptionOnlineMessage { OriginalTransactionId = 100 };
		var (forward, extraOut) = manager.ProcessOutMessage(online);

		forward.AssertSame(online);
		extraOut.Length.AssertEqual(2);
		extraOut[0].AssertSame(snapshot1);
		extraOut[1].AssertSame(snapshot2);

		((ISubscriptionIdMessage)snapshot1).OriginalTransactionId.AssertEqual(100);
		((ISubscriptionIdMessage)snapshot2).OriginalTransactionId.AssertEqual(100);
	}

	[TestMethod]
	public void ProcessOutMessage_SubscriptionOnline_UnknownSubscription_NoSnapshots()
	{
		var holder = new TestSnapshotHolder();
		var manager = new SnapshotHolderManager(holder);

		holder.SnapshotsToReturn = [new Level1ChangeMessage { SecurityId = Helper.CreateSecurityId() }];

		var online = new SubscriptionOnlineMessage { OriginalTransactionId = 999 };
		var (forward, extraOut) = manager.ProcessOutMessage(online);

		forward.AssertSame(online);
		extraOut.Length.AssertEqual(0);
		holder.GetSnapshotCalls.Count.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessOutMessage_SubscriptionFinished_RemovesFromPending()
	{
		var holder = new TestSnapshotHolder();
		var manager = new SnapshotHolderManager(holder);

		var subscribe = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Level1,
		};
		manager.ProcessInMessage(subscribe);

		var finished = new SubscriptionFinishedMessage
		{
			OriginalTransactionId = 100,
		};
		var (forward, extraOut) = manager.ProcessOutMessage(finished);

		forward.AssertSame(finished);
		extraOut.Length.AssertEqual(0);

		holder.SnapshotsToReturn = [new Level1ChangeMessage { SecurityId = Helper.CreateSecurityId() }];
		var online = new SubscriptionOnlineMessage { OriginalTransactionId = 100 };
		var (forward2, extraOut2) = manager.ProcessOutMessage(online);

		extraOut2.Length.AssertEqual(0);
		holder.GetSnapshotCalls.Count.AssertEqual(0);
	}

	[TestMethod]
	public Task ConcurrentAccess_ThreadSafe()
	{
		var holder = new TestSnapshotHolder();
		holder.SnapshotsToReturn = [new Level1ChangeMessage { SecurityId = Helper.CreateSecurityId() }];
		var manager = new SnapshotHolderManager(holder);
		var token = CancellationToken;

		var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
		{
			var secId = Helper.CreateSecurityId();

			var subscribe = new MarketDataMessage
			{
				IsSubscribe = true,
				TransactionId = i,
				SecurityId = secId,
				DataType2 = DataType.Level1,
			};
			manager.ProcessInMessage(subscribe);

			if (i % 10 == 0)
				manager.ProcessInMessage(new ResetMessage());

			var online = new SubscriptionOnlineMessage { OriginalTransactionId = i };
			manager.ProcessOutMessage(online);

		}, token)).ToArray();

		return Task.WhenAll(tasks);
	}
}

namespace StockSharp.Tests;

[TestClass]
public class SubscriptionReplayTrackerTests : BaseTestClass
{
	private static readonly IdGenerator _idGen = new IncrementalIdGenerator();

	private static MarketDataMessage CreateMdSubscription(long txId, SecurityId secId, DataType dataType, DateTime? from = null)
		=> new()
		{
			IsSubscribe = true,
			TransactionId = txId,
			SecurityId = secId,
			DataType2 = dataType,
			From = from,
		};

	private static OrderStatusMessage CreateOrderStatusSubscription(long txId)
		=> new()
		{
			IsSubscribe = true,
			TransactionId = txId,
		};

	private static PortfolioLookupMessage CreatePortfolioSubscription(long txId)
		=> new()
		{
			IsSubscribe = true,
			TransactionId = txId,
		};

	#region Track / Untrack

	[TestMethod]
	public void Track_Subscribe_IncreasesCount()
	{
		var tracker = new SubscriptionReplayTracker();
		var secId = Helper.CreateSecurityId();

		tracker.Track(CreateMdSubscription(1, secId, DataType.Ticks));

		tracker.Count.AssertEqual(1);
	}

	[TestMethod]
	public void Track_Unsubscribe_IsIgnored()
	{
		var tracker = new SubscriptionReplayTracker();

		var msg = new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 1,
			OriginalTransactionId = 100,
		};

		tracker.Track(msg);
		tracker.Count.AssertEqual(0);
	}

	[TestMethod]
	public void Track_HistoryOnly_IsIgnored()
	{
		var tracker = new SubscriptionReplayTracker();
		var secId = Helper.CreateSecurityId();

		// To != null means history-only
		var msg = CreateMdSubscription(1, secId, DataType.Ticks);
		msg.To = DateTime.UtcNow;

		tracker.Track(msg);
		tracker.Count.AssertEqual(0);
	}

	[TestMethod]
	public void Track_CountOnly_IsIgnored()
	{
		var tracker = new SubscriptionReplayTracker();
		var secId = Helper.CreateSecurityId();

		var msg = CreateMdSubscription(1, secId, DataType.Ticks);
		msg.Count = 100;

		tracker.Track(msg);
		tracker.Count.AssertEqual(0);
	}

	[TestMethod]
	public void Track_OnlineSubscription_IsTracked()
	{
		var tracker = new SubscriptionReplayTracker();
		var secId = Helper.CreateSecurityId();

		// From=null, To=null → online subscription
		tracker.Track(CreateMdSubscription(1, secId, DataType.Ticks));
		tracker.Count.AssertEqual(1);
	}

	[TestMethod]
	public void Track_HistoryPlusOnline_IsTracked()
	{
		var tracker = new SubscriptionReplayTracker();
		var secId = Helper.CreateSecurityId();

		// From != null, To = null → history + online
		tracker.Track(CreateMdSubscription(1, secId, DataType.Ticks, from: DateTime.UtcNow.AddDays(-1)));
		tracker.Count.AssertEqual(1);
	}

	[TestMethod]
	public void Untrack_RemovesSubscription()
	{
		var tracker = new SubscriptionReplayTracker();
		var secId = Helper.CreateSecurityId();

		tracker.Track(CreateMdSubscription(42, secId, DataType.Ticks));
		tracker.Count.AssertEqual(1);

		tracker.Untrack(42).AssertTrue();
		tracker.Count.AssertEqual(0);
	}

	[TestMethod]
	public void Untrack_UnknownId_ReturnsFalse()
	{
		var tracker = new SubscriptionReplayTracker();

		tracker.Untrack(999).AssertFalse();
	}

	[TestMethod]
	public void Clear_RemovesAll()
	{
		var tracker = new SubscriptionReplayTracker();
		var secId = Helper.CreateSecurityId();

		tracker.Track(CreateMdSubscription(1, secId, DataType.Ticks));
		tracker.Track(CreateMdSubscription(2, secId, DataType.MarketDepth));
		tracker.Track(CreateOrderStatusSubscription(3));
		tracker.Count.AssertEqual(3);

		tracker.Clear();
		tracker.Count.AssertEqual(0);
	}

	#endregion

	#region GetSubscriptionsForReplay

	[TestMethod]
	public void Replay_ReturnsClones_WithNullFrom()
	{
		var tracker = new SubscriptionReplayTracker();
		var secId = Helper.CreateSecurityId();

		// Track history+online subscription (From != null)
		tracker.Track(CreateMdSubscription(1, secId, DataType.Ticks, from: DateTime.UtcNow.AddDays(-1)));

		var replayed = tracker.GetSubscriptionsForReplay(_idGen).ToList();

		replayed.Count.AssertEqual(1);

		var md = (MarketDataMessage)replayed[0];
		md.From.AssertEqual(null, "Replayed subscription must have From=null (online-only)");
		md.To.AssertEqual(null, "Replayed subscription must have To=null");
		md.IsSubscribe.AssertTrue();
		md.SecurityId.AssertEqual(secId);
		md.DataType2.AssertEqual(DataType.Ticks);
	}

	[TestMethod]
	public void Replay_AssignsNewTransactionIds()
	{
		var tracker = new SubscriptionReplayTracker();
		var secId = Helper.CreateSecurityId();

		tracker.Track(CreateMdSubscription(100, secId, DataType.Ticks));
		tracker.Track(CreateMdSubscription(200, secId, DataType.MarketDepth));

		var replayed = tracker.GetSubscriptionsForReplay(_idGen).ToList();

		replayed.Count.AssertEqual(2);

		// New IDs must differ from originals
		replayed[0].TransactionId.AssertNotEqual(100L);
		replayed[1].TransactionId.AssertNotEqual(200L);

		// New IDs must be unique
		replayed[0].TransactionId.AssertNotEqual(replayed[1].TransactionId);
	}

	[TestMethod]
	public void Replay_DoesNotModifyOriginal()
	{
		var tracker = new SubscriptionReplayTracker();
		var secId = Helper.CreateSecurityId();
		var from = DateTime.UtcNow.AddDays(-1);

		tracker.Track(CreateMdSubscription(1, secId, DataType.Ticks, from: from));

		// Replay
		_ = tracker.GetSubscriptionsForReplay(_idGen).ToList();

		// Replay again — original should still have From set
		var replayed2 = tracker.GetSubscriptionsForReplay(_idGen).ToList();
		replayed2.Count.AssertEqual(1);
		// The stored subscription was cloned on Track, so the original From should still be there
		// and replay should still return From=null
		((MarketDataMessage)replayed2[0]).From.AssertEqual(null);
	}

	[TestMethod]
	public void Replay_MultipleTypes()
	{
		var tracker = new SubscriptionReplayTracker();
		var secId = Helper.CreateSecurityId();

		tracker.Track(CreateMdSubscription(1, secId, DataType.Ticks));
		tracker.Track(CreateOrderStatusSubscription(2));
		tracker.Track(CreatePortfolioSubscription(3));

		var replayed = tracker.GetSubscriptionsForReplay(_idGen).ToList();

		replayed.Count.AssertEqual(3);
		replayed.OfType<MarketDataMessage>().Count().AssertEqual(1);
		replayed.OfType<OrderStatusMessage>().Count().AssertEqual(1);
		replayed.OfType<PortfolioLookupMessage>().Count().AssertEqual(1);
	}

	[TestMethod]
	public void Replay_AfterUntrack_ExcludesRemoved()
	{
		var tracker = new SubscriptionReplayTracker();
		var secId = Helper.CreateSecurityId();

		tracker.Track(CreateMdSubscription(1, secId, DataType.Ticks));
		tracker.Track(CreateMdSubscription(2, secId, DataType.MarketDepth));

		tracker.Untrack(1);

		var replayed = tracker.GetSubscriptionsForReplay(_idGen).ToList();

		replayed.Count.AssertEqual(1);
		((MarketDataMessage)replayed[0]).DataType2.AssertEqual(DataType.MarketDepth);
	}

	#endregion

	#region Channel filtering

	[TestMethod]
	public void Replay_WithChannel_FiltersCorrectly()
	{
		var tracker = new SubscriptionReplayTracker();
		var secId = Helper.CreateSecurityId();

		tracker.Track(CreateMdSubscription(1, secId, DataType.Ticks), channel: "MarketData");
		tracker.Track(CreateOrderStatusSubscription(2), channel: "Trade");
		tracker.Track(CreatePortfolioSubscription(3), channel: "Risk");

		var tradeOnly = tracker.GetSubscriptionsForReplay(_idGen, channel: "Trade").ToList();
		tradeOnly.Count.AssertEqual(1);
		(tradeOnly[0] is OrderStatusMessage).AssertTrue();

		var riskOnly = tracker.GetSubscriptionsForReplay(_idGen, channel: "Risk").ToList();
		riskOnly.Count.AssertEqual(1);
		(riskOnly[0] is PortfolioLookupMessage).AssertTrue();

		var mdOnly = tracker.GetSubscriptionsForReplay(_idGen, channel: "MarketData").ToList();
		mdOnly.Count.AssertEqual(1);
		(mdOnly[0] is MarketDataMessage).AssertTrue();
	}

	[TestMethod]
	public void Replay_NullChannel_ReturnsAll()
	{
		var tracker = new SubscriptionReplayTracker();
		var secId = Helper.CreateSecurityId();

		tracker.Track(CreateMdSubscription(1, secId, DataType.Ticks), channel: "Trade");
		tracker.Track(CreateOrderStatusSubscription(2), channel: "Risk");
		tracker.Track(CreatePortfolioSubscription(3)); // no channel

		var all = tracker.GetSubscriptionsForReplay(_idGen).ToList();
		all.Count.AssertEqual(3);
	}

	[TestMethod]
	public void Replay_ChannelIsCaseInsensitive()
	{
		var tracker = new SubscriptionReplayTracker();
		var secId = Helper.CreateSecurityId();

		tracker.Track(CreateMdSubscription(1, secId, DataType.Ticks), channel: "Trade");

		var result = tracker.GetSubscriptionsForReplay(_idGen, channel: "trade").ToList();
		result.Count.AssertEqual(1);
	}

	[TestMethod]
	public void Replay_UnknownChannel_ReturnsEmpty()
	{
		var tracker = new SubscriptionReplayTracker();
		var secId = Helper.CreateSecurityId();

		tracker.Track(CreateMdSubscription(1, secId, DataType.Ticks), channel: "Trade");

		var result = tracker.GetSubscriptionsForReplay(_idGen, channel: "Unknown").ToList();
		result.Count.AssertEqual(0);
	}

	#endregion

	#region Edge cases

	[TestMethod]
	public void Track_SameId_OverwritesPrevious()
	{
		var tracker = new SubscriptionReplayTracker();
		var secId1 = Helper.CreateSecurityId();
		var secId2 = Helper.CreateSecurityId();

		tracker.Track(CreateMdSubscription(1, secId1, DataType.Ticks));
		tracker.Track(CreateMdSubscription(1, secId2, DataType.MarketDepth));

		tracker.Count.AssertEqual(1);

		var replayed = tracker.GetSubscriptionsForReplay(_idGen).ToList();
		((MarketDataMessage)replayed[0]).DataType2.AssertEqual(DataType.MarketDepth);
	}

	[TestMethod]
	public void Replay_EmptyTracker_ReturnsEmpty()
	{
		var tracker = new SubscriptionReplayTracker();

		var result = tracker.GetSubscriptionsForReplay(_idGen).ToList();
		result.Count.AssertEqual(0);
	}

	[TestMethod]
	public void Track_NullMessage_Throws()
	{
		var tracker = new SubscriptionReplayTracker();

		Throws<ArgumentNullException>(() => tracker.Track(null));
	}

	[TestMethod]
	public void Replay_NullIdGenerator_Throws()
	{
		var tracker = new SubscriptionReplayTracker();

		Throws<ArgumentNullException>(() =>
			tracker.GetSubscriptionsForReplay(null).ToList());
	}

	[TestMethod]
	public void Process_Subscribe_Tracks()
	{
		var tracker = new SubscriptionReplayTracker();
		var secId = Helper.CreateSecurityId();

		tracker.Process(CreateMdSubscription(1, secId, DataType.Ticks));
		tracker.Count.AssertEqual(1);
	}

	[TestMethod]
	public void Process_Unsubscribe_Untracks()
	{
		var tracker = new SubscriptionReplayTracker();
		var secId = Helper.CreateSecurityId();

		tracker.Process(CreateMdSubscription(1, secId, DataType.Ticks));
		tracker.Count.AssertEqual(1);

		tracker.Process(new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 2,
			OriginalTransactionId = 1,
		});
		tracker.Count.AssertEqual(0);
	}

	[TestMethod]
	public void Process_NullMessage_Throws()
	{
		var tracker = new SubscriptionReplayTracker();

		Throws<ArgumentNullException>(() => tracker.Process(null));
	}

	#endregion
}

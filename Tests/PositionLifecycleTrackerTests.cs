namespace StockSharp.Tests;

using StockSharp.Algo.Positions;
using StockSharp.Reporting;

[TestClass]
public class PositionLifecycleTrackerTests : BaseTestClass
{
	private static Security CreateSecurity()
	{
		return new Security
		{
			Id = "TEST@BOARD",
			Code = "TEST",
			Board = ExchangeBoard.Test,
		};
	}

	private static Position CreatePosition(Security security, string portfolio, decimal? currentValue, decimal? currentPrice, DateTime serverTime)
	{
		return new Position
		{
			Security = security,
			Portfolio = new Portfolio { Name = portfolio },
			CurrentValue = currentValue,
			CurrentPrice = currentPrice,
			ServerTime = serverTime,
		};
	}

	[TestMethod]
	public void ConstructorNullProviderThrows()
	{
		Throws<ArgumentNullException>(() => new PositionLifecycleTracker(null));
	}

	[TestMethod]
	public void ConstructorSubscribes()
	{
		var provider = new Mock<ISubscriptionProvider>();

		using var tracker = new PositionLifecycleTracker(provider.Object);

		provider.VerifyAdd(p => p.PositionReceived += It.IsAny<Action<Subscription, Position>>(), Times.Once());
	}

	[TestMethod]
	public void DisposeUnsubscribes()
	{
		var provider = new Mock<ISubscriptionProvider>();

		var tracker = new PositionLifecycleTracker(provider.Object);
		tracker.Dispose();

		provider.VerifyRemove(p => p.PositionReceived -= It.IsAny<Action<Subscription, Position>>(), Times.Once());
	}

	[TestMethod]
	public void OpenAndClose()
	{
		var tracker = CreateTracker();
		var sec = CreateSecurity();

		RoundTripClosed(tracker, sec, "pf",
			(5m, 100m, new DateTime(2024, 1, 1, 10, 0, 0)),
			(0m, 105m, new DateTime(2024, 1, 1, 11, 0, 0))
		);

		var history = tracker.History;
		history.Count.AssertEqual(1);

		var rt = history[0];
		rt.SecurityId.AssertEqual(sec.ToSecurityId());
		rt.PortfolioName.AssertEqual("pf");
		rt.OpenTime.AssertEqual(new DateTime(2024, 1, 1, 10, 0, 0));
		rt.OpenPrice.AssertEqual(100m);
		rt.CloseTime.AssertEqual(new DateTime(2024, 1, 1, 11, 0, 0));
		rt.ClosePrice.AssertEqual(105m);
		rt.MaxPosition.AssertEqual(5m);
	}

	[TestMethod]
	public void MaxPositionTracked()
	{
		var tracker = CreateTracker();
		var sec = CreateSecurity();

		RoundTripClosed(tracker, sec, "pf",
			(5m, 100m, new DateTime(2024, 1, 1, 10, 0, 0)),
			(10m, 102m, new DateTime(2024, 1, 1, 10, 30, 0)),
			(3m, 103m, new DateTime(2024, 1, 1, 10, 45, 0)),
			(0m, 105m, new DateTime(2024, 1, 1, 11, 0, 0))
		);

		var rt = tracker.History[0];
		rt.MaxPosition.AssertEqual(10m);
	}

	[TestMethod]
	public void MultipleRoundTrips()
	{
		var tracker = CreateTracker();
		var sec = CreateSecurity();

		// first round-trip
		RoundTripClosed(tracker, sec, "pf",
			(5m, 100m, new DateTime(2024, 1, 1, 10, 0, 0)),
			(0m, 105m, new DateTime(2024, 1, 1, 11, 0, 0))
		);

		// second round-trip
		RoundTripClosed(tracker, sec, "pf",
			(-3m, 110m, new DateTime(2024, 1, 1, 12, 0, 0)),
			(0m, 108m, new DateTime(2024, 1, 1, 13, 0, 0))
		);

		tracker.History.Count.AssertEqual(2);

		var rt2 = tracker.History[1];
		rt2.OpenPrice.AssertEqual(110m);
		rt2.ClosePrice.AssertEqual(108m);
		rt2.MaxPosition.AssertEqual(3m);
	}

	[TestMethod]
	public void NullPricePreserved()
	{
		var tracker = CreateTracker();
		var sec = CreateSecurity();

		RoundTripClosed(tracker, sec, "pf",
			(5m, null, new DateTime(2024, 1, 1, 10, 0, 0)),
			(0m, null, new DateTime(2024, 1, 1, 11, 0, 0))
		);

		var rt = tracker.History[0];
		rt.OpenPrice.AssertNull();
		rt.ClosePrice.AssertNull();
	}

	[TestMethod]
	public void DifferentPortfoliosTrackedSeparately()
	{
		var tracker = CreateTracker();
		var sec = CreateSecurity();

		// open two positions in different portfolios
		SendPosition(tracker, sec, "pf1", 5m, 100m, new DateTime(2024, 1, 1, 10, 0, 0));
		SendPosition(tracker, sec, "pf2", 3m, 200m, new DateTime(2024, 1, 1, 10, 0, 0));

		// close pf1
		SendPosition(tracker, sec, "pf1", 0m, 110m, new DateTime(2024, 1, 1, 11, 0, 0));

		tracker.History.Count.AssertEqual(1);
		tracker.History[0].PortfolioName.AssertEqual("pf1");

		// close pf2
		SendPosition(tracker, sec, "pf2", 0m, 210m, new DateTime(2024, 1, 1, 12, 0, 0));

		tracker.History.Count.AssertEqual(2);
		tracker.History[1].PortfolioName.AssertEqual("pf2");
	}

	[TestMethod]
	public void ZeroPositionWithoutOpenIsIgnored()
	{
		var tracker = CreateTracker();
		var sec = CreateSecurity();

		// receiving zero without prior open should not create a round-trip
		SendPosition(tracker, sec, "pf", 0m, 100m, new DateTime(2024, 1, 1, 10, 0, 0));

		tracker.History.Count.AssertEqual(0);
	}

	[TestMethod]
	public void RoundTripClosedEventFired()
	{
		var tracker = CreateTracker();
		var sec = CreateSecurity();
		var fired = new List<ReportPosition>();

		tracker.RoundTripClosed += rt => fired.Add(rt);

		RoundTripClosed(tracker, sec, "pf",
			(5m, 100m, new DateTime(2024, 1, 1, 10, 0, 0)),
			(0m, 105m, new DateTime(2024, 1, 1, 11, 0, 0))
		);

		fired.Count.AssertEqual(1);
		fired[0].ClosePrice.AssertEqual(105m);
	}

	[TestMethod]
	public void ReopenSameDirection()
	{
		var tracker = CreateTracker();
		var sec = CreateSecurity();

		// first long round-trip
		SendPosition(tracker, sec, "pf", 5m, 100m, new DateTime(2024, 1, 1, 10, 0, 0));
		SendPosition(tracker, sec, "pf", 0m, 105m, new DateTime(2024, 1, 1, 11, 0, 0));

		// immediately reopen long
		SendPosition(tracker, sec, "pf", 7m, 106m, new DateTime(2024, 1, 1, 11, 0, 1));
		SendPosition(tracker, sec, "pf", 0m, 110m, new DateTime(2024, 1, 1, 12, 0, 0));

		tracker.History.Count.AssertEqual(2);

		var rt1 = tracker.History[0];
		rt1.OpenPrice.AssertEqual(100m);
		rt1.ClosePrice.AssertEqual(105m);
		rt1.MaxPosition.AssertEqual(5m);

		var rt2 = tracker.History[1];
		rt2.OpenPrice.AssertEqual(106m);
		rt2.ClosePrice.AssertEqual(110m);
		rt2.MaxPosition.AssertEqual(7m);
	}

	[TestMethod]
	public void ReopenOppositeDirection()
	{
		var tracker = CreateTracker();
		var sec = CreateSecurity();

		// long round-trip
		SendPosition(tracker, sec, "pf", 5m, 100m, new DateTime(2024, 1, 1, 10, 0, 0));
		SendPosition(tracker, sec, "pf", 0m, 105m, new DateTime(2024, 1, 1, 11, 0, 0));

		// immediately open short
		SendPosition(tracker, sec, "pf", -3m, 106m, new DateTime(2024, 1, 1, 11, 0, 1));
		SendPosition(tracker, sec, "pf", 0m, 103m, new DateTime(2024, 1, 1, 12, 0, 0));

		tracker.History.Count.AssertEqual(2);

		var rt1 = tracker.History[0];
		rt1.OpenPrice.AssertEqual(100m);
		rt1.ClosePrice.AssertEqual(105m);

		var rt2 = tracker.History[1];
		rt2.OpenPrice.AssertEqual(106m);
		rt2.ClosePrice.AssertEqual(103m);
		rt2.MaxPosition.AssertEqual(3m);
	}

	[TestMethod]
	public void ReversalWithoutZero_NoRoundTripClosed()
	{
		// when position flips sign without going through 0,
		// current implementation does NOT close a round-trip
		var tracker = CreateTracker();
		var sec = CreateSecurity();

		SendPosition(tracker, sec, "pf", 5m, 100m, new DateTime(2024, 1, 1, 10, 0, 0));
		// direct reversal: +5 -> -3 (no zero in between)
		SendPosition(tracker, sec, "pf", -3m, 108m, new DateTime(2024, 1, 1, 11, 0, 0));

		// no round-trip closed because position never hit 0
		tracker.History.Count.AssertEqual(0);
	}

	[TestMethod]
	public void ReversalWithoutZero_ClosesOnlyWhenZero()
	{
		var tracker = CreateTracker();
		var sec = CreateSecurity();

		SendPosition(tracker, sec, "pf", 5m, 100m, new DateTime(2024, 1, 1, 10, 0, 0));
		// direct reversal without 0
		SendPosition(tracker, sec, "pf", -3m, 108m, new DateTime(2024, 1, 1, 11, 0, 0));
		// eventually close at 0
		SendPosition(tracker, sec, "pf", 0m, 105m, new DateTime(2024, 1, 1, 12, 0, 0));

		tracker.History.Count.AssertEqual(1);

		var rt = tracker.History[0];
		rt.OpenPrice.AssertEqual(100m);
		rt.ClosePrice.AssertEqual(105m);
		// max position should include the reversed side
		rt.MaxPosition.AssertEqual(5m);
	}

	private static PositionLifecycleTracker CreateTracker()
	{
		var provider = new Mock<ISubscriptionProvider>();
		return new PositionLifecycleTracker(provider.Object);
	}

	private static void SendPosition(PositionLifecycleTracker tracker, Security security, string portfolio, decimal? currentValue, decimal? currentPrice, DateTime serverTime)
	{
		var position = CreatePosition(security, portfolio, currentValue, currentPrice, serverTime);

		// use reflection to call the private OnPositionReceived method
		var method = typeof(PositionLifecycleTracker).GetMethod("OnPositionReceived", BindingFlags.NonPublic | BindingFlags.Instance);
		method.AssertNotNull();
		method.Invoke(tracker, new object[] { null, position });
	}

	private static void RoundTripClosed(PositionLifecycleTracker tracker, Security security, string portfolio, params (decimal? value, decimal? price, DateTime time)[] steps)
	{
		foreach (var (value, price, time) in steps)
			SendPosition(tracker, security, portfolio, value, price, time);
	}
}

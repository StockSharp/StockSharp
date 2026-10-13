namespace StockSharp.Tests;

[TestClass]
public class OrderBookSpreadWidenerTests : BaseTestClass
{
	private static QuoteChangeMessage Snap(SecurityId sec, (decimal Price, decimal Vol)[] bids, (decimal Price, decimal Vol)[] asks)
		=> new()
		{
			SecurityId = sec,
			State = QuoteChangeStates.SnapshotComplete,
			Bids = [.. bids.Select(b => new QuoteChange(b.Price, b.Vol))],
			Asks = [.. asks.Select(a => new QuoteChange(a.Price, a.Vol))],
		};

	private static QuoteChangeMessage Incr(SecurityId sec, (decimal Price, decimal Vol)[] bids = null, (decimal Price, decimal Vol)[] asks = null)
		=> new()
		{
			SecurityId = sec,
			State = QuoteChangeStates.Increment,
			Bids = [.. (bids ?? []).Select(b => new QuoteChange(b.Price, b.Vol))],
			Asks = [.. (asks ?? []).Select(a => new QuoteChange(a.Price, a.Vol))],
		};

	private static readonly SecurityId Sec = new() { SecurityCode = "TWT", BoardCode = "IMEX" };

	[TestMethod]
	public void ZeroPercent_IsDisabled()
	{
		new OrderBookSpreadWidener(0m).IsEnabled.AreEqual(false);
	}

	[TestMethod]
	public void NullMessage_ReturnsNull()
	{
		new OrderBookSpreadWidener(1m).Apply(null, new OrderBookSnapshotHolder(), null).AreEqual(null);
	}

	[TestMethod]
	public void NullHolder_ReturnsOriginal()
	{
		var msg = Snap(Sec, [(100m, 1m)], [(101m, 1m)]);
		ReferenceEquals(new OrderBookSpreadWidener(1m).Apply(msg, null, null), msg).AreEqual(true);
	}

	[TestMethod]
	public void FirstSnapshot_EmitsFullCollapsedSnapshot_OriginalIntact()
	{
		var holder = new OrderBookSnapshotHolder();
		var widener = new OrderBookSpreadWidener(1m);

		var snap = Snap(Sec,
			bids: [(100m, 5m), (99m, 7m), (95m, 10m)],
			asks: [(101m, 5m), (102m, 7m), (105m, 10m)]);
		holder.Process(snap);

		var result = widener.Apply(snap, holder, null);

		// Result is a clone, not the input.
		ReferenceEquals(result, snap).AreEqual(false);

		// Result holds the collapsed view.
		result.State.AreEqual(QuoteChangeStates.SnapshotComplete);
		result.Bids[0].Price.AreEqual(99m);
		result.Bids[0].Volume.AreEqual(12m);
		result.Bids[1].Price.AreEqual(95m);
		result.Asks[0].Price.AreEqual(102.01m);
		result.Asks[0].Volume.AreEqual(12m);

		// Original untouched.
		snap.Bids[0].Price.AreEqual(100m);
		snap.Bids[0].Volume.AreEqual(5m);
	}

	[TestMethod]
	public void LevelRemovalInsideSpread_EmitsCorrectDelta()
	{
		var holder = new OrderBookSnapshotHolder();
		var widener = new OrderBookSpreadWidener(1m);

		var snap = Snap(Sec, bids: [(100m, 5m), (99m, 10m)], asks: [(101m, 1m)]);
		holder.Process(snap);
		widener.Apply(snap, holder, null);

		var incr = Incr(Sec, bids: [(100m, 0m)]);
		holder.Process(incr);
		var result = widener.Apply(incr, holder, null);

		result.State.AreEqual(QuoteChangeStates.Increment);

		result.Bids.Length.AreEqual(2);
		var byPrice = result.Bids.ToDictionary(q => q.Price, q => q);
		byPrice[99m].Volume.AreEqual(0m);
		byPrice[98.01m].Volume.AreEqual(10m);
	}

	[TestMethod]
	public void NoChangeOnNewBest_EmitsEmptyDelta()
	{
		var holder = new OrderBookSnapshotHolder();
		var widener = new OrderBookSpreadWidener(1m);

		var snap = Snap(Sec, bids: [(100m, 5m)], asks: [(101m, 5m)]);
		holder.Process(snap);
		widener.Apply(snap, holder, null);

		var snap2 = Snap(Sec, bids: [(100m, 5m)], asks: [(101m, 5m)]);
		holder.Process(snap2);
		var result = widener.Apply(snap2, holder, null);

		result.State.AreEqual(QuoteChangeStates.Increment);
		result.Bids.Length.AreEqual(0);
		result.Asks.Length.AreEqual(0);
	}

	[TestMethod]
	public void TailLevelAdded_EmitsTailAddOnly()
	{
		var holder = new OrderBookSnapshotHolder();
		var widener = new OrderBookSpreadWidener(1m);

		holder.Process(Snap(Sec, bids: [(100m, 5m)], asks: [(101m, 5m)]));
		widener.Apply(Snap(Sec, bids: [(100m, 5m)], asks: [(101m, 5m)]), holder, null);

		var incr = Incr(Sec, bids: [(80m, 3m)]);
		holder.Process(incr);
		var result = widener.Apply(incr, holder, null);

		result.Bids.Length.AreEqual(1);
		result.Bids[0].Price.AreEqual(80m);
		result.Bids[0].Volume.AreEqual(3m);
		result.Asks.Length.AreEqual(0);
	}

	[TestMethod]
	public void OrdersCount_AggregatedWhenPresent()
	{
		var holder = new OrderBookSnapshotHolder();
		var widener = new OrderBookSpreadWidener(2m);

		var snap = new QuoteChangeMessage
		{
			SecurityId = Sec,
			State = QuoteChangeStates.SnapshotComplete,
			Bids = [new QuoteChange(100m, 5m, 3), new QuoteChange(99m, 7m, 2)],
			Asks = [new QuoteChange(101m, 5m, 4), new QuoteChange(102m, 7m, 1)],
		};
		holder.Process(snap);
		var result = widener.Apply(snap, holder, null);

		result.Bids.Length.AreEqual(1);
		result.Bids[0].Price.AreEqual(98m);
		result.Bids[0].Volume.AreEqual(12m);
		result.Bids[0].OrdersCount.AreEqual(5);
		result.Asks[0].Price.AreEqual(103.02m);
		result.Asks[0].OrdersCount.AreEqual(5);
	}

	[TestMethod]
	public void BoundaryLevelExactlyOnNewBest_IsCollapsed()
	{
		var holder = new OrderBookSnapshotHolder();
		var widener = new OrderBookSpreadWidener(1m);

		var snap = Snap(Sec,
			bids: [(100m, 5m)],
			asks: [(101m, 1m), (102.01m, 5m), (103m, 7m)]);
		holder.Process(snap);
		var result = widener.Apply(snap, holder, null);

		result.Asks.Length.AreEqual(2);
		result.Asks[0].Price.AreEqual(102.01m);
		result.Asks[0].Volume.AreEqual(6m);
		result.Asks[1].Price.AreEqual(103m);
	}

	[TestMethod]
	public void DeepTail_FullyPreserved()
	{
		var holder = new OrderBookSnapshotHolder();
		var widener = new OrderBookSpreadWidener(2m);

		var snap = Snap(Sec,
			bids: [(100m, 1m), (90m, 5m), (80m, 10m)],
			asks: [(101m, 1m), (110m, 5m), (120m, 10m)]);
		holder.Process(snap);
		var result = widener.Apply(snap, holder, null);

		result.Bids.Length.AreEqual(3);
		result.Bids[0].Price.AreEqual(98m);
		result.Bids[1].Price.AreEqual(90m);
		result.Bids[2].Price.AreEqual(80m);
		result.Asks.Length.AreEqual(3);
		result.Asks[0].Price.AreEqual(103.02m);
	}

	[TestMethod]
	public void ConditionChange_ReachesTheClient()
	{
		// The condition is what marks a level as the client's own order, so a level that stops
		// being theirs looks identical on price and volume and must still be sent.
		var holder = new OrderBookSnapshotHolder();
		var widener = new OrderBookSpreadWidener(1m);

		QuoteChangeMessage book(QuoteConditions condition) => new()
		{
			SecurityId = Sec,
			State = QuoteChangeStates.SnapshotComplete,
			Bids = [new QuoteChange(100m, 5m), new QuoteChange(90m, 7m) { Condition = condition }],
			Asks = [new QuoteChange(101m, 5m)],
		};

		var first = book(QuoteConditions.Active);
		holder.Process(first);
		widener.Apply(first, holder, null);

		var second = book(QuoteConditions.Indicative);
		holder.Process(second);
		var result = widener.Apply(second, holder, null);

		result.State.AreEqual(QuoteChangeStates.Increment);

		var changed = result.Bids.Where(q => q.Price == 90m).ToArray();
		changed.Length.AreEqual(1, "the level whose condition changed has to travel");
		changed[0].Condition.AreEqual(QuoteConditions.Indicative);
	}

	[TestMethod]
	public void WidenedBest_LandsOnThePriceStep()
	{
		// A price off the step grid is one nobody can trade at: the venue would be quoting a
		// level that every downstream check rounds away.
		var holder = new OrderBookSnapshotHolder();
		var widener = new OrderBookSpreadWidener(1m);

		var snap = Snap(Sec, bids: [(100.3m, 5m), (80m, 1m)], asks: [(100.7m, 5m), (120m, 1m)]);
		holder.Process(snap);

		var result = widener.Apply(snap, holder, 0.5m);

		(result.Bids[0].Price % 0.5m).AreEqual(0m, $"bid {result.Bids[0].Price} is off the grid");
		(result.Asks[0].Price % 0.5m).AreEqual(0m, $"ask {result.Asks[0].Price} is off the grid");
	}

	[TestMethod]
	public void Snapping_NeverNarrowsTheSpreadBelowWhatWasAsked()
	{
		// Rounding to the nearest step would move a bid up and an ask down by half a step, which
		// quotes a tighter spread than the one configured - the widening exists to avoid exactly
		// that.
		var holder = new OrderBookSnapshotHolder();
		var widener = new OrderBookSpreadWidener(1m);

		var snap = Snap(Sec, bids: [(100.3m, 5m), (80m, 1m)], asks: [(100.7m, 5m), (120m, 1m)]);
		holder.Process(snap);

		var result = widener.Apply(snap, holder, 0.5m);

		(result.Bids[0].Price <= 100.3m * 0.99m).AreEqual(true, $"bid {result.Bids[0].Price} sits above the widened price");
		(result.Asks[0].Price >= 100.7m * 1.01m).AreEqual(true, $"ask {result.Asks[0].Price} sits below the widened price");
	}

	[TestMethod]
	public void EmittedQuotes_AreNeverTheBookItReadFrom()
	{
		// The emitted arrays travel on to consumers that may hold them; handing out the holder's
		// own arrays would let one of them edit the book everybody else reads.
		var holder = new OrderBookSnapshotHolder();
		var widener = new OrderBookSpreadWidener(1m);

		var snap = Snap(Sec, bids: [(100m, 5m)], asks: [(101m, 5m)]);
		holder.Process(snap);

		var result = widener.Apply(snap, holder, null);

		holder.TryGetSnapshot(Sec, out var raw).AreEqual(true);
		ReferenceEquals(result.Bids, raw.Bids).AreEqual(false);
		ReferenceEquals(result.Asks, raw.Asks).AreEqual(false);
		ReferenceEquals(result.Bids, snap.Bids).AreEqual(false);
	}

	[TestMethod]
	public void ResetSnapshot_NextEmissionIsFullSnapshot()
	{
		var holder = new OrderBookSnapshotHolder();
		var widener = new OrderBookSpreadWidener(1m);

		holder.Process(Snap(Sec, bids: [(100m, 5m)], asks: [(101m, 5m)]));
		widener.Apply(Snap(Sec, bids: [(100m, 5m)], asks: [(101m, 5m)]), holder, null);

		widener.ResetSnapshot(Sec);

		var snap2 = Snap(Sec, bids: [(100m, 5m)], asks: [(101m, 5m)]);
		holder.Process(snap2);
		var result = widener.Apply(snap2, holder, null);

		result.State.AreEqual(QuoteChangeStates.SnapshotComplete);
	}
}

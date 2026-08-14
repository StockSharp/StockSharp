namespace StockSharp.Algo;

using System.Collections.Concurrent;
using System.Collections.Generic;

using StockSharp.Messages;

/// <summary>
/// Symmetric spread widener for <see cref="QuoteChangeMessage"/>. Collapses
/// every raw level inside the new (wider) spread onto the new visible best;
/// outer levels pass through. Reads current raw book state from an
/// <see cref="OrderBookSnapshotHolder"/> the caller maintains and emits
/// <see cref="QuoteChangeStates.Increment"/> frames after the initial
/// <see cref="QuoteChangeStates.SnapshotComplete"/>.
/// </summary>
public sealed class OrderBookSpreadWidener
{
	private readonly decimal _bidFactor;
	private readonly decimal _askFactor;

	private readonly ConcurrentDictionary<SecurityId, EmittedBook> _lastEmitted = [];

	private sealed class EmittedBook
	{
		// One security's diff state is only ever touched by whoever is processing that security,
		// so the lock is per book rather than one across all of them.
		public readonly Lock Sync = new();

		// Distinct from "both sides empty": a security whose book is genuinely empty must still
		// get one snapshot and increments after it, not a snapshot every time.
		public bool Seeded;

		public Dictionary<decimal, QuoteChange> Bids = [];
		public Dictionary<decimal, QuoteChange> Asks = [];
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderBookSpreadWidener"/> class.
	/// </summary>
	/// <param name="percent">Half-spread widening, in percent. Non-positive disables widening.</param>
	public OrderBookSpreadWidener(decimal percent)
	{
		Percent = percent;

		if (percent > 0m)
		{
			var p = percent / 100m;
			_bidFactor = 1m - p;
			_askFactor = 1m + p;
		}
		else
		{
			_bidFactor = 1m;
			_askFactor = 1m;
		}
	}

	/// <summary>
	/// Half-spread widening, in percent.
	/// </summary>
	public decimal Percent { get; }

	/// <summary>
	/// <see langword="true"/> if <see cref="Percent"/> is positive and widening is applied.
	/// </summary>
	public bool IsEnabled => Percent > 0m;

	/// <summary>
	/// Drops the diff-state used to emit increments, forcing the next <see cref="Apply"/>
	/// to emit a fresh <see cref="QuoteChangeStates.SnapshotComplete"/>.
	/// </summary>
	/// <param name="securityId">Security to reset, or <see langword="default"/> to reset all securities.</param>
	public void ResetSnapshot(SecurityId securityId)
	{
		if (securityId == default)
			_lastEmitted.Clear();
		else
			_lastEmitted.TryRemove(securityId, out _);
	}

	/// <summary>
	/// Returns the current collapsed view of <paramref name="securityId"/> as a
	/// fresh <c>SnapshotComplete</c>. Pure read — does not touch the diff-state
	/// used by <see cref="Apply"/>. Useful for replying to a new subscriber
	/// without re-emitting deltas from before their subscription.
	/// </summary>
	/// <param name="securityId">Security to build the collapsed snapshot for.</param>
	/// <param name="holder">Holder of the current raw order book state.</param>
	/// <param name="priceStep">
	/// Price step of the security, so the widened best lands on a price that can be traded.
	/// <see langword="null"/> when the step is unknown, and the widened price is then left as
	/// the arithmetic produced it.
	/// </param>
	/// <returns>
	/// Collapsed snapshot, or <see langword="null"/> when widening is disabled or no
	/// raw snapshot is available for <paramref name="securityId"/>.
	/// </returns>
	public QuoteChangeMessage Collapse(SecurityId securityId, OrderBookSnapshotHolder holder, decimal? priceStep)
	{
		if (!IsEnabled || holder is null)
			return null;

		if (!holder.TryPeekSnapshot(securityId, out var raw))
			return null;

		var copy = CopyHeader(raw);
		copy.Bids = Collapse(raw.Bids, _bidFactor, priceStep, descending: true);
		copy.Asks = Collapse(raw.Asks, _askFactor, priceStep, descending: false);
		copy.State = QuoteChangeStates.SnapshotComplete;

		// The stored snapshot carries whichever subscription first filled it, and this reply is
		// for a different subscriber - the caller addresses it.
		copy.OriginalTransactionId = 0;
		copy.SubscriptionId = 0;
		copy.SubscriptionIds = [];

		return copy;
	}

	/// <summary>
	/// Reads the current raw book from <paramref name="holder"/> (the caller must have
	/// applied <paramref name="msg"/> to the holder first) and rewrites it as either a
	/// full <see cref="QuoteChangeStates.SnapshotComplete"/> (first call for this
	/// security) or an <see cref="QuoteChangeStates.Increment"/> carrying only the
	/// changes against the previously-emitted collapsed view.
	/// </summary>
	/// <param name="msg">Incoming order book change message.</param>
	/// <param name="holder">Holder of the current raw order book state.</param>
	/// <param name="priceStep">
	/// Price step of the security, so the widened best lands on a price that can be traded.
	/// <see langword="null"/> when the step is unknown, and the widened price is then left as
	/// the arithmetic produced it.
	/// </param>
	/// <returns>
	/// Collapsed message; the original message when widening is disabled or no raw
	/// snapshot is available; or <see langword="null"/> if <paramref name="msg"/> is
	/// <see langword="null"/>.
	/// </returns>
	public QuoteChangeMessage Apply(QuoteChangeMessage msg, OrderBookSnapshotHolder holder, decimal? priceStep)
	{
		if (msg is null)
			return null;

		if (!IsEnabled || holder is null)
			return msg;

		if (!holder.TryPeekSnapshot(msg.SecurityId, out var raw))
			return msg;

		var newBids = Collapse(raw.Bids, _bidFactor, priceStep, descending: true);
		var newAsks = Collapse(raw.Asks, _askFactor, priceStep, descending: false);

		var copy = CopyHeader(msg);
		var book = _lastEmitted.GetOrAdd(msg.SecurityId, _ => new());

		using (book.Sync.EnterScope())
		{
			// A client that has never been told the book cannot be sent changes against it.
			var first = !book.Seeded;
			book.Seeded = true;

			var bidDelta = Diff(book.Bids, newBids, out var nextBids);
			var askDelta = Diff(book.Asks, newAsks, out var nextAsks);

			book.Bids = nextBids;
			book.Asks = nextAsks;

			if (first)
			{
				copy.Bids = newBids;
				copy.Asks = newAsks;
				copy.State = QuoteChangeStates.SnapshotComplete;
			}
			else
			{
				copy.Bids = bidDelta;
				copy.Asks = askDelta;
				copy.State = QuoteChangeStates.Increment;
			}
		}

		return copy;
	}

	// Builds a clone with all header fields copied but Bids/Asks left empty — the
	// caller fills them with the collapsed view. Avoids the wasted Bids/Asks
	// array allocation that QuoteChangeMessage.CopyTo would otherwise do.
	private static QuoteChangeMessage CopyHeader(QuoteChangeMessage src) => new()
	{
		SecurityId = src.SecurityId,
		ServerTime = src.ServerTime,
		LocalTime = src.LocalTime,
		Currency = src.Currency,
		BuildFrom = src.BuildFrom,
		IsFiltered = src.IsFiltered,
		HasPositions = src.HasPositions,
		SeqNum = src.SeqNum,
		OriginalTransactionId = src.OriginalTransactionId,
		SubscriptionId = src.SubscriptionId,
		SubscriptionIds = src.SubscriptionIds,
		BackMode = src.BackMode,
		OfflineMode = src.OfflineMode,
	};

	/// <summary>
	/// One walk produces both the changes against <paramref name="prev"/> and the state to keep
	/// for the next call, so the emitted view is never built twice.
	/// </summary>
	private static QuoteChange[] Diff(Dictionary<decimal, QuoteChange> prev, QuoteChange[] @new, out Dictionary<decimal, QuoteChange> next)
	{
		next = new(@new.Length);

		List<QuoteChange> deltas = null;

		foreach (var q in @new)
		{
			next[q.Price] = q;

			// Condition belongs here with volume and orders count: it is what marks a level as
			// the client's own, and a level that stops being theirs changes nothing else.
			if (!prev.TryGetValue(q.Price, out var was)
				|| was.Volume != q.Volume
				|| was.OrdersCount != q.OrdersCount
				|| was.Condition != q.Condition)
			{
				(deltas ??= new(@new.Length)).Add(q);
			}
		}

		foreach (var price in prev.Keys)
		{
			if (!next.ContainsKey(price))
				(deltas ??= []).Add(new QuoteChange(price, 0m));
		}

		return deltas is null ? [] : [.. deltas];
	}

	private static QuoteChange[] Collapse(QuoteChange[] side, decimal factor, decimal? priceStep, bool descending)
	{
		if (side is null || side.Length == 0)
			return [];

		// The array belongs to the holder and everyone else reading it, so nothing that leaves
		// here is ever the one that came in.
		if (side[0].Price <= 0m)
			return [.. side];

		var newBestPrice = Snap(side[0].Price * factor, priceStep, down: descending);

		decimal aggVolume = 0m;
		var hasOrdersCount = false;
		var aggOrdersCount = 0;
		var collapseCount = 0;

		for (var i = 0; i < side.Length; i++)
		{
			ref var q = ref side[i];
			var inside = descending ? q.Price >= newBestPrice : q.Price <= newBestPrice;
			if (!inside)
				break;

			aggVolume += q.Volume;
			if (q.OrdersCount is int oc)
			{
				hasOrdersCount = true;
				aggOrdersCount += oc;
			}
			collapseCount++;
		}

		// The best level is always inside the widened spread by construction - widening moves the
		// boundary away from it - so at least one level is collapsed and there is no empty case.
		var topCondition = side[0].Condition;
		var result = new QuoteChange[side.Length - collapseCount + 1];
		result[0] = new QuoteChange(newBestPrice, aggVolume, hasOrdersCount ? aggOrdersCount : null, topCondition);

		var tail = side.Length - collapseCount;
		if (tail > 0)
			Array.Copy(side, collapseCount, result, 1, tail);

		return result;
	}

	/// <summary>
	/// Moves the widened price onto the step grid, away from the market: down for a bid, up for
	/// an ask. Rounding to the nearest step instead would move it the other way half the time and
	/// quote a spread tighter than the one asked for, which is the thing widening exists to stop.
	/// </summary>
	private static decimal Snap(decimal price, decimal? priceStep, bool down)
	{
		if (priceStep is not decimal step || step <= 0m)
			return price;

		var steps = price / step;
		var whole = decimal.Truncate(steps);

		if (steps == whole)
			return price;

		return (down ? whole : whole + 1m) * step;
	}
}

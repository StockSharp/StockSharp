namespace StockSharp.Algo;

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

	private readonly Dictionary<SecurityId, EmittedBook> _lastEmitted = [];
	private readonly Lock _stateLock = new();

	private sealed class EmittedBook
	{
		public readonly Dictionary<decimal, QuoteChange> Bids = [];
		public readonly Dictionary<decimal, QuoteChange> Asks = [];
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
		using (_stateLock.EnterScope())
		{
			if (securityId == default)
				_lastEmitted.Clear();
			else
				_lastEmitted.Remove(securityId);
		}
	}

	/// <summary>
	/// Returns the current collapsed view of <paramref name="securityId"/> as a
	/// fresh <c>SnapshotComplete</c>. Pure read — does not touch the diff-state
	/// used by <see cref="Apply"/>. Useful for replying to a new subscriber
	/// without re-emitting deltas from before their subscription.
	/// </summary>
	/// <param name="securityId">Security to build the collapsed snapshot for.</param>
	/// <param name="holder">Holder of the current raw order book state.</param>
	/// <returns>
	/// Collapsed snapshot, or <see langword="null"/> when widening is disabled or no
	/// raw snapshot is available for <paramref name="securityId"/>.
	/// </returns>
	public QuoteChangeMessage Collapse(SecurityId securityId, OrderBookSnapshotHolder holder)
	{
		if (!IsEnabled || holder is null)
			return null;

		if (!holder.TryGetSnapshot(securityId, out var raw))
			return null;

		var copy = CopyHeader(raw);
		copy.Bids = Collapse(raw.Bids, _bidFactor, descending: true);
		copy.Asks = Collapse(raw.Asks, _askFactor, descending: false);
		copy.State = QuoteChangeStates.SnapshotComplete;
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
	/// <returns>
	/// Collapsed message; the original message when widening is disabled or no raw
	/// snapshot is available; or <see langword="null"/> if <paramref name="msg"/> is
	/// <see langword="null"/>.
	/// </returns>
	public QuoteChangeMessage Apply(QuoteChangeMessage msg, OrderBookSnapshotHolder holder)
	{
		if (msg is null)
			return null;

		if (!IsEnabled || holder is null)
			return msg;

		if (!holder.TryGetSnapshot(msg.SecurityId, out var raw))
			return msg;

		var newBids = Collapse(raw.Bids, _bidFactor, descending: true);
		var newAsks = Collapse(raw.Asks, _askFactor, descending: false);

		var copy = CopyHeader(msg);

		using (_stateLock.EnterScope())
		{
			if (!_lastEmitted.TryGetValue(msg.SecurityId, out var prev))
			{
				prev = new EmittedBook();
				_lastEmitted[msg.SecurityId] = prev;

				copy.Bids = newBids;
				copy.Asks = newAsks;
				copy.State = QuoteChangeStates.SnapshotComplete;
			}
			else
			{
				copy.Bids = BuildDelta(prev.Bids, newBids);
				copy.Asks = BuildDelta(prev.Asks, newAsks);
				copy.State = QuoteChangeStates.Increment;
			}

			Replace(prev.Bids, newBids);
			Replace(prev.Asks, newAsks);
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

	private static void Replace(Dictionary<decimal, QuoteChange> dst, QuoteChange[] src)
	{
		dst.Clear();
		foreach (var q in src)
			dst[q.Price] = q;
	}

	private static QuoteChange[] BuildDelta(Dictionary<decimal, QuoteChange> prev, QuoteChange[] @new)
	{
		var deltas = new List<QuoteChange>();
		var seen = new HashSet<decimal>();

		foreach (var q in @new)
		{
			seen.Add(q.Price);
			if (!prev.TryGetValue(q.Price, out var was)
				|| was.Volume != q.Volume
				|| was.OrdersCount != q.OrdersCount)
			{
				deltas.Add(q);
			}
		}

		foreach (var p in prev.Keys)
		{
			if (!seen.Contains(p))
				deltas.Add(new QuoteChange(p, 0m));
		}

		return [.. deltas];
	}

	private static QuoteChange[] Collapse(QuoteChange[] side, decimal factor, bool descending)
	{
		if (side is null || side.Length == 0)
			return side ?? [];

		if (side[0].Price <= 0m)
			return side;

		var newBestPrice = side[0].Price * factor;

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

		if (collapseCount == 0)
			return side;

		var topCondition = side[0].Condition;
		var result = new QuoteChange[side.Length - collapseCount + 1];
		result[0] = new QuoteChange(newBestPrice, aggVolume, hasOrdersCount ? aggOrdersCount : null, topCondition);

		var tail = side.Length - collapseCount;
		if (tail > 0)
			Array.Copy(side, collapseCount, result, 1, tail);

		return result;
	}
}

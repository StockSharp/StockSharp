namespace StockSharp.Messages;

/// <summary>
/// Order book builder, used incremental <see cref="QuoteChangeMessage"/>.
/// </summary>
public class OrderBookIncrementBuilder : BaseLogReceiver
{
	private const QuoteChangeStates _none = (QuoteChangeStates)(-1);
	private QuoteChangeStates _state = _none;

	// Trees, not sorted arrays: a book changes at its best prices, which is where an array has to move
	// every level it holds to make room for one.
	private readonly SortedDictionary<decimal, QuoteChange> _bids = new(new BackwardComparer<decimal>());
	private readonly SortedDictionary<decimal, QuoteChange> _asks = [];

	private readonly List<QuoteChange> _bidsByPos = [];
	private readonly List<QuoteChange> _asksByPos = [];

	// Whether the book that stands is the one kept by position: the change applied last says which.
	private bool _isByPos;

	private readonly HashSet<long> _invalidSubscriptions = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderBookIncrementBuilder"/>.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	public OrderBookIncrementBuilder(SecurityId securityId)
	{
		if (securityId == default)
			throw new ArgumentNullException(nameof(securityId));

		SecurityId = securityId;
	}

	/// <summary>
	/// Security ID.
	/// </summary>
	public readonly SecurityId SecurityId;

	/// <summary>
	/// Try create full book.
	/// </summary>
	/// <param name="change">Book change.</param>
	/// <param name="subscriptionId">Subscription.</param>
	/// <returns>Full book.</returns>
	public QuoteChangeMessage TryApply(QuoteChangeMessage change, long subscriptionId = default)
	{
		if (!TryFold(change, subscriptionId))
			return null;

		var book = GetSnapshot(null);

		book.ServerTime = change.ServerTime;
		book.OriginalTransactionId = change.OriginalTransactionId;

		return book;
	}

	/// <summary>
	/// Applies a change to the book without stating the book it leaves.
	/// </summary>
	/// <remarks>
	/// For a caller that only keeps the book up to date: stating the whole book costs as much as the book is
	/// deep, and a change is a level or two. <see cref="GetSnapshot"/> states it when it is wanted.
	/// </remarks>
	/// <param name="change">Book change.</param>
	/// <param name="subscriptionId">Subscription.</param>
	/// <returns>
	/// <see langword="true"/> when a whole book stands after the change; <see langword="false"/> when the
	/// change was refused, or belongs to a snapshot that is still being sent.
	/// </returns>
	public bool TryFold(QuoteChangeMessage change, long subscriptionId = default)
	{
		if (change is null)
			throw new ArgumentNullException(nameof(change));

		if (change.State is null)
			throw new ArgumentException(nameof(change));

		var currState = _state;
		var newState = change.State.Value;

		void WriteWarning()
		{
			var postfix = string.Empty;

			if (subscriptionId != default)
			{
				if (!_invalidSubscriptions.Add(subscriptionId))
					return;

				postfix = $" (sub={subscriptionId})";
			}

			LogWarning($"{currState}->{newState}{postfix}");
		}

		bool CheckSwitch()
		{
			switch (currState)
			{
				case _none:
				{
					if (newState is not QuoteChangeStates.SnapshotStarted and not QuoteChangeStates.SnapshotBuilding and not QuoteChangeStates.SnapshotComplete)
					{
						WriteWarning();
						return false;
					}

					break;
				}
				case QuoteChangeStates.SnapshotStarted:
				{
					if (newState is not QuoteChangeStates.SnapshotBuilding and not QuoteChangeStates.SnapshotComplete)
					{
						WriteWarning();
						return false;
					}

					break;
				}
				case QuoteChangeStates.SnapshotBuilding:
				{
					if (newState is not QuoteChangeStates.SnapshotBuilding and not QuoteChangeStates.SnapshotComplete)
					{
						WriteWarning();
						return false;
					}

					break;
				}
				case QuoteChangeStates.SnapshotComplete:
				case QuoteChangeStates.Increment:
				{
					if (newState == QuoteChangeStates.SnapshotBuilding)
					{
						WriteWarning();
						return false;
					}

					break;
				}
			}

			return true;
		}

		var resetState = newState == QuoteChangeStates.SnapshotStarted
			|| (newState == QuoteChangeStates.SnapshotComplete
				&& currState is not QuoteChangeStates.SnapshotStarted and not QuoteChangeStates.SnapshotBuilding);

		if (currState != newState || resetState)
		{
			if (!CheckSwitch())
				return false;

			if (currState == _none || resetState)
			{
				_bids.Clear();
				_asks.Clear();

				_bidsByPos.Clear();
				_asksByPos.Clear();

				_invalidSubscriptions.Clear();
			}

			_state = currState = newState;
		}

		static void Apply(IEnumerable<QuoteChange> from, SortedDictionary<decimal, QuoteChange> to)
		{
			foreach (var quote in from)
			{
				if (quote.Volume == 0)
					to.Remove(quote.Price);
				else
					to[quote.Price] = quote;
			}
		}

		bool ApplyByPos(IEnumerable<QuoteChange> from, List<QuoteChange> to)
		{
			var tmp = new List<QuoteChange>(to);

			foreach (var quote in from)
			{
				if (quote.StartPosition is not { } startPos)
				{
					// StartPosition required for positional updates
					LogWarning("StartPosition is required for positional order book updates");
					return false;
				}

				switch (quote.Action)
				{
					case QuoteChangeActions.New:
					{
						var newQuote = new QuoteChange(quote.Price, quote.Volume, quote.OrdersCount, quote.Condition);

						if (startPos > tmp.Count)
							return false;
						else if (startPos == tmp.Count)
							tmp.Add(newQuote);
						else
							tmp.Insert(startPos, newQuote);

						break;
					}
					case QuoteChangeActions.Update:
					{
						if (startPos < 0 || startPos >= tmp.Count)
							return false;

						tmp[startPos] = new QuoteChange(quote.Price, quote.Volume, quote.OrdersCount, quote.Condition);
						break;
					}
					case QuoteChangeActions.Delete:
					{
						if (startPos < 0 || startPos >= tmp.Count)
							return false;

						if (quote.EndPosition == null)
							tmp.RemoveAt(startPos);
						else
						{
							var endPos = quote.EndPosition.Value;
							if (endPos < startPos || endPos >= tmp.Count)
								return false;

							tmp.RemoveRange(startPos, (endPos - startPos) + 1);
						}

						break;
					}
					default:
						LogWarning($"Invalid action {quote.Action}");
						return false;
				}
			}

			// commit
			to.Clear();
			to.AddRange(tmp);

			return true;
		}

		if (change.HasPositions)
		{
			if (!ApplyByPos(change.Bids, _bidsByPos) || !ApplyByPos(change.Asks, _asksByPos))
				return false;
		}
		else
		{
			Apply(change.Bids, _bids);
			Apply(change.Asks, _asks);
		}

		if (currState is QuoteChangeStates.SnapshotStarted or QuoteChangeStates.SnapshotBuilding)
			return false;

		if (currState == QuoteChangeStates.SnapshotComplete)
		{
			if (!change.HasPositions)
			{
				_bidsByPos.AddRange(_bids.Values);
				_asksByPos.AddRange(_asks.Values);
			}
		}

		_isByPos = change.HasPositions;

		return true;
	}

	/// <summary>
	/// The book as it stands.
	/// </summary>
	/// <remarks>
	/// After a change <see cref="TryFold"/> answered <see langword="false"/> for, this is not a whole book: it
	/// is the part of a snapshot that has come so far, or a book the refused change left half-changed.
	/// </remarks>
	/// <param name="maxDepth">
	/// How many of the best levels of each side to state; every level when <see langword="null"/>.
	/// </param>
	/// <returns>The book. It carries no time: when it was so is the caller's to say.</returns>
	public QuoteChangeMessage GetSnapshot(int? maxDepth)
	{
		if (maxDepth is <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxDepth), maxDepth, LocalizedStrings.InvalidValue);

		static QuoteChange[] Cut(IEnumerable<QuoteChange> levels, int count, int? maxDepth)
			=> maxDepth is int depth && depth < count ? [.. levels.Take(depth)] : [.. levels];

		return new()
		{
			SecurityId = SecurityId,
			Bids = _isByPos ? Cut(_bidsByPos, _bidsByPos.Count, maxDepth) : Cut(_bids.Values, _bids.Count, maxDepth),
			Asks = _isByPos ? Cut(_asksByPos, _asksByPos.Count, maxDepth) : Cut(_asks.Values, _asks.Count, maxDepth),
		};
	}
}

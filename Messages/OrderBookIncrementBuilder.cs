namespace StockSharp.Messages;

/// <summary>
/// Order book builder, used incremental <see cref="QuoteChangeMessage"/>.
/// </summary>
public class OrderBookIncrementBuilder : BaseLogReceiver
{
	private const QuoteChangeStates _none = (QuoteChangeStates)(-1);
	private QuoteChangeStates _state = _none;

	private readonly SortedList<decimal, QuoteChange> _bids = new(new BackwardComparer<decimal>());
	private readonly SortedList<decimal, QuoteChange> _asks = [];

	private readonly List<QuoteChange> _bidsByPos = [];
	private readonly List<QuoteChange> _asksByPos = [];

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

		var resetState = newState is QuoteChangeStates.SnapshotStarted or QuoteChangeStates.SnapshotComplete;

		if (currState != newState || resetState)
		{
			if (!CheckSwitch())
				return null;

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

		static void Apply(IEnumerable<QuoteChange> from, SortedList<decimal, QuoteChange> to)
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
				return null;
		}
		else
		{
			Apply(change.Bids, _bids);
			Apply(change.Asks, _asks);
		}

		if (currState is QuoteChangeStates.SnapshotStarted or QuoteChangeStates.SnapshotBuilding)
			return null;

		if (currState == QuoteChangeStates.SnapshotComplete)
		{
			if (!change.HasPositions)
			{
				_bidsByPos.AddRange(_bids.Values);
				_asksByPos.AddRange(_asks.Values);
			}
		}

		QuoteChange[] bids;
		QuoteChange[] asks;

		if (change.HasPositions)
		{
			bids = [.. _bidsByPos];
			asks = [.. _asksByPos];
		}
		else
		{
			bids = [.. _bids.Values];
			asks = [.. _asks.Values];
		}

		return new()
		{
			SecurityId = SecurityId,
			Bids = bids,
			Asks = asks,
			ServerTime = change.ServerTime,
			OriginalTransactionId = change.OriginalTransactionId,
		};
	}
}
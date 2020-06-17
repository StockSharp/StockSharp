namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	using QuotesDict = System.Collections.Generic.SortedDictionary<decimal, System.Tuple<decimal, int?, Messages.QuoteConditions>>;
	using QuotesByPosList = System.Collections.Generic.List<System.Tuple<decimal, decimal, int?, Messages.QuoteConditions>>;

	/// <summary>
	/// Order book builder, used incremental <see cref="QuoteChangeMessage"/>.
	/// </summary>
	public class OrderBookIncrementBuilder
	{
		private const QuoteChangeStates _none = (QuoteChangeStates)(-1);
		private QuoteChangeStates State = _none;

		private readonly ILogReceiver _logs;

		private readonly QuotesDict _bids = new QuotesDict(new BackwardComparer<decimal>());
		private readonly QuotesDict _asks = new QuotesDict();

		private readonly QuotesByPosList _bidsByPos = new QuotesByPosList();
		private readonly QuotesByPosList _asksByPos = new QuotesByPosList();

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderBookIncrementBuilder"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="logs">Logs.</param>
		public OrderBookIncrementBuilder(SecurityId securityId, ILogReceiver logs)
		{
			if (securityId == default)
				throw new ArgumentNullException(nameof(securityId));

			SecurityId = securityId;
			_logs = logs ?? throw new ArgumentNullException(nameof(logs));
		}

		/// <summary>
		/// Security ID.
		/// </summary>
		public readonly SecurityId SecurityId;

		/// <summary>
		/// Try create full book.
		/// </summary>
		/// <param name="change">Book change.</param>
		/// <returns>Full book.</returns>
		public QuoteChangeMessage TryApply(QuoteChangeMessage change)
		{
			if (change is null)
				throw new ArgumentNullException(nameof(change));

			var currState = State;
			var newState = change.State.Value;

			void CheckSwitch()
			{
				switch (currState)
				{
					case _none:
					case QuoteChangeStates.SnapshotStarted:
					{
						if (newState != QuoteChangeStates.SnapshotBuilding && newState != QuoteChangeStates.SnapshotComplete)
							_logs.AddDebugLog($"{currState}->{newState}");

						break;
					}
					case QuoteChangeStates.SnapshotBuilding:
					{
						if (newState != QuoteChangeStates.SnapshotBuilding && newState != QuoteChangeStates.SnapshotComplete)
							_logs.AddDebugLog($"{currState}->{newState}");

						break;
					}
					case QuoteChangeStates.SnapshotComplete:
					case QuoteChangeStates.Increment:
					{
						if (newState == QuoteChangeStates.SnapshotBuilding)
							_logs.AddDebugLog($"{currState}->{newState}");

						break;
					}
				}
			}

			if (currState != newState)
			{
				CheckSwitch();

				if (newState == QuoteChangeStates.SnapshotStarted)
				{
					_bids.Clear();
					_asks.Clear();
				}

				switch (currState)
				{
					case _none:
					{
						_bids.Clear();
						_asks.Clear();

						break;
					}
					case QuoteChangeStates.SnapshotStarted:
						break;
					case QuoteChangeStates.SnapshotBuilding:
						break;
					case QuoteChangeStates.SnapshotComplete:
					{
						if (newState == QuoteChangeStates.SnapshotComplete)
						{
							_bids.Clear();
							_asks.Clear();
						}

						break;
					}
					case QuoteChangeStates.Increment:
						break;
					default:
						throw new ArgumentOutOfRangeException(currState.ToString());
				}

				State = currState = newState;
			}

			void Apply(IEnumerable<QuoteChange> from, QuotesDict to)
			{
				foreach (var quote in from)
				{
					if (quote.Volume == 0)
						to.Remove(quote.Price);
					else
						to[quote.Price] = Tuple.Create(quote.Volume, quote.OrdersCount, quote.Condition);
				}
			}

			void ApplyByPos(IEnumerable<QuoteChange> from, QuotesByPosList to)
			{
				foreach (var quote in from)
				{
					var startPos = quote.StartPosition.Value;

					switch (quote.Action)
					{
						case QuoteChangeActions.New:
						{
							var tuple = Tuple.Create(quote.Price, quote.Volume, quote.OrdersCount, quote.Condition);

							if (startPos > to.Count)
								throw new InvalidOperationException($"Pos={startPos}>Count={to.Count}");
							else if (startPos == to.Count)
								to.Add(tuple);
							else
								to.Insert(startPos, tuple);

							break;
						}
						case QuoteChangeActions.Update:
						{
							to[startPos] = Tuple.Create(quote.Price, quote.Volume, quote.OrdersCount, quote.Condition);
							break;
						}
						case QuoteChangeActions.Delete:
						{
							if (quote.EndPosition == null)
								to.RemoveAt(startPos);
							else
								to.RemoveRange(startPos, (quote.EndPosition.Value - startPos) + 1);

							break;
						}
						default:
							throw new ArgumentOutOfRangeException(nameof(from), quote.Action, LocalizedStrings.Str1219);
					}
				}
			}

			if (change.HasPositions)
			{
				ApplyByPos(change.Bids, _bidsByPos);
				ApplyByPos(change.Asks, _asksByPos);
			}
			else
			{
				Apply(change.Bids, _bids);
				Apply(change.Asks, _asks);
			}

			if (currState == QuoteChangeStates.SnapshotStarted || currState == QuoteChangeStates.SnapshotBuilding)
				return null;

			IEnumerable<QuoteChange> bids;
			IEnumerable<QuoteChange> asks;

			if (change.HasPositions)
			{
				bids = _bidsByPos.Select(p => new QuoteChange(p.Item1, p.Item2, p.Item3, p.Item4));
				asks = _asksByPos.Select(p => new QuoteChange(p.Item1, p.Item2, p.Item3, p.Item4));
			}
			else
			{
				bids = _bids.Select(p => new QuoteChange(p.Key, p.Value.Item1, p.Value.Item2, p.Value.Item3));
				asks = _asks.Select(p => new QuoteChange(p.Key, p.Value.Item1, p.Value.Item2, p.Value.Item3));
			}

			return new QuoteChangeMessage
			{
				SecurityId = SecurityId,
				Bids = bids.ToArray(),
				Asks = asks.ToArray(),
				IsSorted = true,
				ServerTime = change.ServerTime,
				OriginalTransactionId = change.OriginalTransactionId,
			};
		}
	}
}
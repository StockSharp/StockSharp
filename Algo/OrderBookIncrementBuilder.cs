namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Order book builder, used incremental <see cref="QuoteChangeMessage"/>.
	/// </summary>
	public class OrderBookIncrementBuilder
	{
		private const QuoteChangeStates _none = (QuoteChangeStates)(-1);
		private QuoteChangeStates _state = _none;

		private readonly ILogReceiver _logs;

		private readonly SortedList<decimal, QuoteChange> _bids = new SortedList<decimal, QuoteChange>(new BackwardComparer<decimal>());
		private readonly SortedList<decimal, QuoteChange> _asks = new SortedList<decimal, QuoteChange>();

		private readonly List<QuoteChange> _bidsByPos = new List<QuoteChange>();
		private readonly List<QuoteChange> _asksByPos = new List<QuoteChange>();

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

			if (change.State == null)
				throw new ArgumentException(nameof(change));

			var currState = _state;
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

			if (currState != newState || newState == QuoteChangeStates.SnapshotComplete)
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

				_state = currState = newState;
			}

			void Apply(IEnumerable<QuoteChange> from, SortedList<decimal, QuoteChange> to)
			{
				foreach (var quote in from)
				{
					if (quote.Volume == 0)
						to.Remove(quote.Price);
					else
						to[quote.Price] = quote;
				}
			}

			void ApplyByPos(IEnumerable<QuoteChange> from, List<QuoteChange> to)
			{
				foreach (var quote in from)
				{
					var startPos = quote.StartPosition.Value;

					switch (quote.Action)
					{
						case QuoteChangeActions.New:
						{
							var tuple = new QuoteChange(quote.Price, quote.Volume, quote.OrdersCount, quote.Condition);

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
							to[startPos] = new QuoteChange(quote.Price, quote.Volume, quote.OrdersCount, quote.Condition);
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

			QuoteChange[] bids;
			QuoteChange[] asks;

			if (change.HasPositions)
			{
				bids = _bidsByPos.ToArray();
				asks = _asksByPos.ToArray();
			}
			else
			{
				bids = _bids.Values.ToArray();
				asks = _asks.Values.ToArray();
			}

			return new QuoteChangeMessage
			{
				SecurityId = SecurityId,
				Bids = bids,
				Asks = asks,
				ServerTime = change.ServerTime,
				OriginalTransactionId = change.OriginalTransactionId,
			};
		}
	}
}
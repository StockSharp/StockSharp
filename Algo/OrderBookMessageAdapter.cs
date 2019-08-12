namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.Messages;
	using QuotesDict = System.Collections.Generic.SortedDictionary<decimal, decimal>;

	/// <summary>
	/// The messages adapter build order book and tick data from order log flow.
	/// </summary>
	public class OrderBookMessageAdapter : MessageAdapterWrapper
	{
		private readonly SynchronizedDictionary<SecurityId, int> _depths = new SynchronizedDictionary<SecurityId, int>();
		private readonly SynchronizedDictionary<SecurityId, RefTriple<QuotesDict, QuotesDict, QuoteChangeStates>> _states = new SynchronizedDictionary<SecurityId, RefTriple<QuotesDict, QuotesDict, QuoteChangeStates>>();
		private readonly bool _isSupportOrderBookDepths;
		private readonly bool _isSupportOrderBookIncrements;

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderBookMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public OrderBookMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
			_isSupportOrderBookDepths = innerAdapter.IsSupportOrderBookDepths;
			_isSupportOrderBookIncrements = innerAdapter.IsSupportOrderBookIncrements;
		}

		/// <inheritdoc />
		public override void SendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					_depths.Clear();
					_states.Clear();

					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.DataType == MarketDataTypes.MarketDepth)
					{
						if (mdMsg.IsSubscribe)
						{
							if (mdMsg.MaxDepth != null)
								_depths[mdMsg.SecurityId] = mdMsg.MaxDepth.Value;
						}
						else
							_depths.Remove(mdMsg.SecurityId);
					}

					break;
				}
			}

			base.SendInMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.QuoteChange:
				{
					var quoteMsg = (QuoteChangeMessage)message;

					if (!_isSupportOrderBookDepths && _depths.TryGetValue(quoteMsg.SecurityId, out var maxDepth))
					{
						if (quoteMsg.Bids.Length > maxDepth)
							quoteMsg.Bids = quoteMsg.Bids.Take(maxDepth).ToArray();

						if (quoteMsg.Asks.Length > maxDepth)
							quoteMsg.Asks = quoteMsg.Asks.Take(maxDepth).ToArray();
					}
					else if (_isSupportOrderBookIncrements)
					{
						RefTriple<QuotesDict, QuotesDict, QuoteChangeStates> GetCurrState()
						{
							var state = quoteMsg.State.Value;
							var tuple = _states.SafeAdd(quoteMsg.SecurityId, key => RefTuple.Create(new QuotesDict(new BackwardComparer<decimal>()), new QuotesDict(), state));

							if (tuple.Third != state)
							{
								switch (tuple.Third)
								{
									case QuoteChangeStates.SnapshotStarted:
									{
										if (state != QuoteChangeStates.SnapshotBuilding && state != QuoteChangeStates.SnapshotComplete)
											this.AddWarningLog($"{tuple.Third}->{state}");

										break;
									}
									case QuoteChangeStates.SnapshotBuilding:
									{
										if (state != QuoteChangeStates.SnapshotComplete)
											this.AddWarningLog($"{tuple.Third}->{state}");

										break;
									}
									case QuoteChangeStates.SnapshotComplete:
									case QuoteChangeStates.Increment:
									{
										if (state == QuoteChangeStates.SnapshotBuilding)
											this.AddWarningLog($"{tuple.Third}->{state}");

										break;
									}
									default:
										throw new ArgumentOutOfRangeException(tuple.Third.ToString());
								}

								tuple.Third = state;
							}

							return tuple;
						}

						void Apply(QuotesDict dict, IEnumerable<QuoteChange> quotes)
						{
							foreach (var quote in quotes)
							{
								if (quote.Volume == 0)
									dict.Remove(quote.Price);
								else
									dict[quote.Price] = quote.Volume;
							}
						}

						QuoteChangeMessage CreateOrderBook(QuotesDict bids, QuotesDict asks)
						{
							return new QuoteChangeMessage
							{
								SecurityId = quoteMsg.SecurityId,
								Bids = bids.Select(p => new QuoteChange(Sides.Buy, p.Key, p.Value)).ToArray(),
								Asks = asks.Select(p => new QuoteChange(Sides.Sell, p.Key, p.Value)).ToArray(),
								IsSorted = true,
								ServerTime = quoteMsg.ServerTime,
							};
						}

						switch (quoteMsg.State)
						{
							case null:
								break;

							case QuoteChangeStates.SnapshotStarted:
							{
								var currState = GetCurrState();

								currState.First.Clear();
								currState.Second.Clear();

								Apply(currState.First, quoteMsg.Bids);
								Apply(currState.Second, quoteMsg.Asks);

								return;
							}
							case QuoteChangeStates.SnapshotBuilding:
							{
								var currState = GetCurrState();

								Apply(currState.First, quoteMsg.Bids);
								Apply(currState.Second, quoteMsg.Asks);

								return;
							}
							case QuoteChangeStates.SnapshotComplete:
							{
								var currState = GetCurrState();

								Apply(currState.First, quoteMsg.Bids);
								Apply(currState.Second, quoteMsg.Asks);

								message = CreateOrderBook(currState.First, currState.Second);
								break;
							}
							case QuoteChangeStates.Increment:
							{
								var currState = GetCurrState();

								Apply(currState.First, quoteMsg.Bids);
								Apply(currState.Second, quoteMsg.Asks);

								message = CreateOrderBook(currState.First, currState.Second);
								break;
							}
							default:
								throw new ArgumentOutOfRangeException();
						}
					}

					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="OrderBookMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new OrderBookMessageAdapter((IMessageAdapter)InnerAdapter.Clone());
		}
	}
}
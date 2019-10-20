namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Logging;
	using StockSharp.Messages;
	using QuotesDict = System.Collections.Generic.SortedDictionary<decimal, decimal>;

	/// <summary>
	/// The messages adapter build order book from incremental updates <see cref="QuoteChangeStates.Increment"/>.
	/// </summary>
	public class OrderBookInrementMessageAdapter : MessageAdapterWrapper
	{
		private readonly SynchronizedDictionary<SecurityId, RefTriple<QuotesDict, QuotesDict, QuoteChangeStates>> _states = new SynchronizedDictionary<SecurityId, RefTriple<QuotesDict, QuotesDict, QuoteChangeStates>>();

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderBookInrementMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public OrderBookInrementMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		private int? _maxDepth;

		/// <summary>
		/// Max depth of requested order book.
		/// </summary>
		public int? MaxDepth
		{
			get => _maxDepth;
			set
			{
				if (value != null && value < 1)
					throw new ArgumentOutOfRangeException(nameof(value));

				_maxDepth = value;
			}
		}

		/// <inheritdoc />
		protected override void OnSendInMessage(Message message)
		{
			if (message.IsBack)
			{
				base.OnSendInMessage(message);
				return;
			}

			switch (message.Type)
			{
				case MessageTypes.Reset:
					_states.Clear();
					break;
			}

			base.OnSendInMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.QuoteChange:
				{
					var quoteMsg = (QuoteChangeMessage)message;

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

							if (MaxDepth != null)
							{
								var maxDepth = MaxDepth.Value;

								void Truncate(QuotesDict dict)
								{
									if (dict.Count <= maxDepth)
										return;

									foreach (var key in dict.Keys.Skip(maxDepth).ToArray())
										dict.Remove(key);
								}

								Truncate(currState.First);
								Truncate(currState.Second);
							}

							message = CreateOrderBook(currState.First, currState.Second);
							break;
						}
						default:
							throw new ArgumentOutOfRangeException();
					}

					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="OrderBookInrementMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new OrderBookInrementMessageAdapter((IMessageAdapter)InnerAdapter.Clone()) { MaxDepth = MaxDepth };
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(MaxDepth), MaxDepth);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);
			
			MaxDepth = storage.GetValue<int?>(nameof(MaxDepth));
		}
	}
}
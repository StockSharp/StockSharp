namespace StockSharp.Messages;

using System.Collections;
using System.Runtime.CompilerServices;

static partial class Extensions
{
	/// <summary>
	/// Convert <see cref="IOrderBookMessage"/> to <see cref="Level1ChangeMessage"/> value.
	/// </summary>
	/// <param name="message"><see cref="IOrderBookMessage"/> instance.</param>
	/// <returns><see cref="Level1ChangeMessage"/> instance.</returns>
	public static Level1ChangeMessage ToLevel1(this IOrderBookMessage message)
	{
		var b = message.GetBestBid();
		var a = message.GetBestAsk();

		var level1 = new Level1ChangeMessage
		{
			SecurityId = message.SecurityId,
			ServerTime = message.ServerTime,
		};

		if (b != null)
		{
			var bestBid = b.Value;

			level1.Add(Level1Fields.BestBidPrice, bestBid.Price);
			level1.Add(Level1Fields.BestBidVolume, bestBid.Volume);
		}

		if (a != null)
		{
			var bestAsk = a.Value;

			level1.Add(Level1Fields.BestAskPrice, bestAsk.Price);
			level1.Add(Level1Fields.BestAskVolume, bestAsk.Volume);
		}

		return level1;
	}

	/// <summary>
	/// Convert <see cref="CandleMessage"/> to <see cref="Level1ChangeMessage"/> value.
	/// </summary>
	/// <param name="message"><see cref="CandleMessage"/> instance.</param>
	/// <returns><see cref="Level1ChangeMessage"/> instance.</returns>
	public static Level1ChangeMessage ToLevel1(this CandleMessage message)
	{
		var level1 = new Level1ChangeMessage
		{
			SecurityId = message.SecurityId,
			ServerTime = message.CloseTime == default ? message.OpenTime : message.CloseTime,
		}
		.Add(Level1Fields.OpenPrice, message.OpenPrice)
		.Add(Level1Fields.HighPrice, message.HighPrice)
		.Add(Level1Fields.LowPrice, message.LowPrice)
		.Add(Level1Fields.ClosePrice, message.ClosePrice)
		.TryAdd(Level1Fields.Volume, message.TotalVolume)
		.TryAdd(Level1Fields.TradesCount, message.TotalTicks)
		.TryAdd(Level1Fields.OpenInterest, message.OpenInterest, true);

		return level1;
	}

	/// <summary>
	/// Convert <see cref="ExecutionMessage"/> to <see cref="Level1ChangeMessage"/> value.
	/// </summary>
	/// <param name="message"><see cref="ExecutionMessage"/> instance.</param>
	/// <returns><see cref="Level1ChangeMessage"/> instance.</returns>
	public static Level1ChangeMessage ToLevel1(this ExecutionMessage message)
	{
		var level1 = new Level1ChangeMessage
		{
			SecurityId = message.SecurityId,
			ServerTime = message.ServerTime,
		}
		.TryAdd(Level1Fields.LastTradeId, message.TradeId)
		.TryAdd(Level1Fields.LastTradePrice, message.TradePrice)
		.TryAdd(Level1Fields.LastTradeVolume, message.TradeVolume)
		.TryAdd(Level1Fields.LastTradeUpDown, message.IsUpTick)
		.TryAdd(Level1Fields.OpenInterest, message.OpenInterest, true)
		.TryAdd(Level1Fields.LastTradeOrigin, message.OriginSide);

		return level1;
	}

	/// <summary>
	/// To build level1 from the order books.
	/// </summary>
	/// <param name="quotes">Order books.</param>
	/// <returns>Level1.</returns>
	public static IEnumerable<Level1ChangeMessage> ToLevel1(this IEnumerable<QuoteChangeMessage> quotes)
	{
		if (quotes is null)
			throw new ArgumentNullException(nameof(quotes));

		foreach (var quote in quotes)
		{
			var l1Msg = new Level1ChangeMessage
			{
				SecurityId = quote.SecurityId,
				ServerTime = quote.ServerTime,
				BuildFrom = quote.BuildFrom ?? DataType.MarketDepth,
			};

			if (quote.Bids.Length > 0)
			{
				l1Msg
					.TryAdd(Level1Fields.BestBidPrice, quote.Bids[0].Price)
					.TryAdd(Level1Fields.BestBidVolume, quote.Bids[0].Volume);
			}

			if (quote.Asks.Length > 0)
			{
				l1Msg
					.TryAdd(Level1Fields.BestAskPrice, quote.Asks[0].Price)
					.TryAdd(Level1Fields.BestAskVolume, quote.Asks[0].Volume);
			}

			yield return l1Msg;
		}
	}

	private sealed class OrderLogTickEnumerable : SimpleEnumerable<ExecutionMessage>//, IEnumerableEx<ExecutionMessage>
	{
		private sealed class OrderLogTickEnumerator : IEnumerator<ExecutionMessage>
		{
			private readonly IEnumerator<ExecutionMessage> _itemsEnumerator;

			private readonly HashSet<long> _tradesByNum = [];
			private readonly HashSet<string> _tradesByString = new(StringComparer.InvariantCultureIgnoreCase);

			public OrderLogTickEnumerator(IEnumerable<ExecutionMessage> items)
			{
				if (items == null)
					throw new ArgumentNullException(nameof(items));

				_itemsEnumerator = items.GetEnumerator();
			}

			public ExecutionMessage Current { get; private set; }

			bool IEnumerator.MoveNext()
			{
				while (_itemsEnumerator.MoveNext())
				{
					var currItem = _itemsEnumerator.Current;

					if (currItem.TradeId != null)
					{
						if (TryProcess(currItem.TradeId.Value, _tradesByNum, currItem))
							return true;
					}
					else if (!currItem.TradeStringId.IsEmpty())
					{
						if (TryProcess(currItem.TradeStringId, _tradesByString, currItem))
							return true;
					}
				}

				Current = null;
				return false;
			}

			private bool TryProcess<T>(T tradeId, HashSet<T> trades, ExecutionMessage currItem)
			{
				if (!trades.Add(tradeId))
					return false;

				trades.Remove(tradeId);
				Current = currItem.ToTick();
				return true;
			}

			void IEnumerator.Reset()
			{
				_itemsEnumerator.Reset();

				_tradesByNum.Clear();
				_tradesByString.Clear();

				Current = null;
			}

			object IEnumerator.Current => Current;

			void IDisposable.Dispose()
			{
				Current = null;
				_itemsEnumerator.Dispose();

				GC.SuppressFinalize(this);
			}
		}

		//private readonly IEnumerable<ExecutionMessage> _items;

		public OrderLogTickEnumerable(IEnumerable<ExecutionMessage> items)
			: base(() => new OrderLogTickEnumerator(items))
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));

			//_items = items;
		}

		//int IEnumerableEx.Count => _items.Count;
	}

	/// <summary>
	/// To tick trade from the order log.
	/// </summary>
	/// <param name="item">Order log item.</param>
	/// <returns>Tick trade.</returns>
	public static ExecutionMessage ToTick(this ExecutionMessage item)
	{
		if (item == null)
			throw new ArgumentNullException(nameof(item));

		if (item.DataType != DataType.OrderLog)
			throw new ArgumentException(nameof(item));

		return new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = item.SecurityId,
			TradeId = item.TradeId,
			TradeStringId = item.TradeStringId,
			TradePrice = item.TradePrice,
			TradeStatus = item.TradeStatus,
			TradeVolume = item.OrderVolume,
			ServerTime = item.ServerTime,
			LocalTime = item.LocalTime,
			IsSystem = item.IsSystem,
			OpenInterest = item.OpenInterest,
			OriginSide = item.OriginSide,
			//OriginSide = prevItem.Item2 == Sides.Buy
			//	? (prevItem.Item1 > item.OrderId ? Sides.Buy : Sides.Sell)
			//	: (prevItem.Item1 > item.OrderId ? Sides.Sell : Sides.Buy),
			BuildFrom = DataType.OrderLog,
		};
	}

	/// <summary>
	/// To build tick trades from the orders log.
	/// </summary>
	/// <param name="items">Orders log lines.</param>
	/// <returns>Tick trades.</returns>
	public static IEnumerable<ExecutionMessage> ToTicks(this IEnumerable<ExecutionMessage> items)
	{
		return new OrderLogTickEnumerable(items);
	}

	private sealed class TickLevel1Enumerable : SimpleEnumerable<Level1ChangeMessage>
	{
		private sealed class TickLevel1Enumerator : IEnumerator<Level1ChangeMessage>
		{
			private readonly IEnumerator<ExecutionMessage> _itemsEnumerator;

			public TickLevel1Enumerator(IEnumerable<ExecutionMessage> items)
			{
				if (items is null)
					throw new ArgumentNullException(nameof(items));

				_itemsEnumerator = items.GetEnumerator();
			}

			public Level1ChangeMessage Current { get; private set; }

			bool IEnumerator.MoveNext()
			{
				while (_itemsEnumerator.MoveNext())
				{
					var tick = _itemsEnumerator.Current;

					var l1Msg = new Level1ChangeMessage
					{
						SecurityId = tick.SecurityId,
						ServerTime = tick.ServerTime,
						LocalTime = tick.LocalTime,
					}
					.TryAdd(Level1Fields.LastTradeId, tick.TradeId)
					.TryAdd(Level1Fields.LastTradeStringId, tick.TradeStringId)
					.TryAdd(Level1Fields.LastTradePrice, tick.TradePrice)
					.TryAdd(Level1Fields.LastTradeVolume, tick.TradeVolume)
					.TryAdd(Level1Fields.LastTradeUpDown, tick.IsUpTick)
					.TryAdd(Level1Fields.LastTradeOrigin, tick.OriginSide)
					;

					if (!l1Msg.HasChanges())
						continue;

					Current = l1Msg;
					return true;
				}

				Current = null;
				return false;
			}

			void IEnumerator.Reset()
			{
				_itemsEnumerator.Reset();
				Current = null;
			}

			object IEnumerator.Current => Current;

			void IDisposable.Dispose()
			{
				Current = null;
				_itemsEnumerator.Dispose();

				GC.SuppressFinalize(this);
			}
		}

		public TickLevel1Enumerable(IEnumerable<ExecutionMessage> items)
			: base(() => new TickLevel1Enumerator(items))
		{
			if (items is null)
				throw new ArgumentNullException(nameof(items));
		}
	}

	/// <summary>
	/// To build level1 from the orders log.
	/// </summary>
	/// <param name="items">Orders log lines.</param>
	/// <param name="builder">Order log to market depth builder.</param>
	/// <param name="interval">The interval of the order book generation. The default is <see cref="TimeSpan.Zero"/>, which means order books generation at each new item of orders log.</param>
	/// <returns>Tick trades.</returns>
	public static IEnumerable<Level1ChangeMessage> ToLevel1(this IEnumerable<ExecutionMessage> items, IOrderLogMarketDepthBuilder builder, TimeSpan interval = default)
	{
		if (builder == null)
			return new TickLevel1Enumerable(items);
		else
			return items.ToOrderBooks(builder, interval, 1).BuildIfNeed().ToLevel1();
	}


	private class TickEnumerable : SimpleEnumerable<ExecutionMessage>//, IEnumerableEx<ExecutionMessage>
	{
		private class TickEnumerator(IEnumerator<Level1ChangeMessage> level1Enumerator) : IEnumerator<ExecutionMessage>
		{
			private readonly IEnumerator<Level1ChangeMessage> _level1Enumerator = level1Enumerator ?? throw new ArgumentNullException(nameof(level1Enumerator));

			public ExecutionMessage Current { get; private set; }

			bool IEnumerator.MoveNext()
			{
				while (_level1Enumerator.MoveNext())
				{
					var level1 = _level1Enumerator.Current;

					if (!level1.IsContainsTick())
						continue;

					Current = level1.ToTick();
					return true;
				}

				Current = null;
				return false;
			}

			public void Reset()
			{
				_level1Enumerator.Reset();
				Current = null;
			}

			object IEnumerator.Current => Current;

			void IDisposable.Dispose()
			{
				Current = null;
				_level1Enumerator.Dispose();

				GC.SuppressFinalize(this);
			}
		}

		//private readonly IEnumerable<Level1ChangeMessage> _level1;

		public TickEnumerable(IEnumerable<Level1ChangeMessage> level1)
			: base(() => new TickEnumerator(level1.GetEnumerator()))
		{
			if (level1 == null)
				throw new ArgumentNullException(nameof(level1));

			//_level1 = level1;
		}

		//int IEnumerableEx.Count => _level1.Count;
	}

	/// <summary>
	/// To convert level1 data into tick data.
	/// </summary>
	/// <param name="level1">Level1 data.</param>
	/// <returns>Tick data.</returns>
	public static IEnumerable<ExecutionMessage> ToTicks(this IEnumerable<Level1ChangeMessage> level1)
	{
		return new TickEnumerable(level1);
	}

	/// <summary>
	/// To check, are there tick data in the level1 data.
	/// </summary>
	/// <param name="level1">Level1 data.</param>
	/// <returns>The test result.</returns>
	public static bool IsContainsTick(this Level1ChangeMessage level1)
	{
		if (level1 == null)
			throw new ArgumentNullException(nameof(level1));

		return level1.Changes.ContainsKey(Level1Fields.LastTradePrice);
	}

	/// <summary>
	/// To convert level1 data into tick data.
	/// </summary>
	/// <param name="level1">Level1 data.</param>
	/// <returns>Tick data.</returns>
	public static ExecutionMessage ToTick(this Level1ChangeMessage level1)
	{
		if (level1 == null)
			throw new ArgumentNullException(nameof(level1));

		return new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = level1.SecurityId,
			TradeId = (long?)level1.TryGet(Level1Fields.LastTradeId),
			TradePrice = level1.TryGetDecimal(Level1Fields.LastTradePrice),
			TradeVolume = level1.TryGetDecimal(Level1Fields.LastTradeVolume),
			OriginSide = (Sides?)level1.TryGet(Level1Fields.LastTradeOrigin),
			ServerTime = (DateTime?)level1.TryGet(Level1Fields.LastTradeTime) ?? level1.ServerTime,
			IsUpTick = (bool?)level1.TryGet(Level1Fields.LastTradeUpDown),
			LocalTime = level1.LocalTime,
			BuildFrom = level1.BuildFrom ?? DataType.Level1,
		};
	}

	/// <summary>
	/// To check, are there <see cref="DataType.IsCandles"/> in the level1 data.
	/// </summary>
	/// <param name="level1">Level1 data.</param>
	/// <returns>The test result.</returns>
	public static bool IsContainsCandle(this Level1ChangeMessage level1)
	{
		if (level1 is null)
			throw new ArgumentNullException(nameof(level1));

		var changes = level1.Changes;

		return
			changes.ContainsKey(Level1Fields.OpenPrice) ||
			changes.ContainsKey(Level1Fields.HighPrice) ||
			changes.ContainsKey(Level1Fields.LowPrice) ||
			changes.ContainsKey(Level1Fields.ClosePrice);
	}

	private class OrderBookEnumerable : SimpleEnumerable<QuoteChangeMessage>//, IEnumerableEx<QuoteChangeMessage>
	{
		private class OrderBookEnumerator(IEnumerator<Level1ChangeMessage> level1Enumerator) : IEnumerator<QuoteChangeMessage>
		{
			private readonly IEnumerator<Level1ChangeMessage> _level1Enumerator = level1Enumerator ?? throw new ArgumentNullException(nameof(level1Enumerator));

			private decimal? _prevBidPrice;
			private decimal? _prevBidVolume;
			private decimal? _prevAskPrice;
			private decimal? _prevAskVolume;

			public QuoteChangeMessage Current { get; private set; }

			bool IEnumerator.MoveNext()
			{
				while (_level1Enumerator.MoveNext())
				{
					var level1 = _level1Enumerator.Current;

					if (!level1.IsContainsQuotes())
						continue;

					var prevBidPrice = _prevBidPrice;
					var prevBidVolume = _prevBidVolume;
					var prevAskPrice = _prevAskPrice;
					var prevAskVolume = _prevAskVolume;

					_prevBidPrice = level1.TryGetDecimal(Level1Fields.BestBidPrice) ?? _prevBidPrice;
					_prevBidVolume = level1.TryGetDecimal(Level1Fields.BestBidVolume) ?? _prevBidVolume;
					_prevAskPrice = level1.TryGetDecimal(Level1Fields.BestAskPrice) ?? _prevAskPrice;
					_prevAskVolume = level1.TryGetDecimal(Level1Fields.BestAskVolume) ?? _prevAskVolume;

					if (_prevBidPrice == 0)
						_prevBidPrice = null;

					if (_prevAskPrice == 0)
						_prevAskPrice = null;

					if (prevBidPrice == _prevBidPrice && prevBidVolume == _prevBidVolume && prevAskPrice == _prevAskPrice && prevAskVolume == _prevAskVolume)
						continue;

					Current = new QuoteChangeMessage
					{
						SecurityId = level1.SecurityId,
						LocalTime = level1.LocalTime,
						ServerTime = level1.ServerTime,
						Bids = _prevBidPrice == null ? [] : [new QuoteChange(_prevBidPrice.Value, _prevBidVolume ?? 0)],
						Asks = _prevAskPrice == null ? [] : [new QuoteChange(_prevAskPrice.Value, _prevAskVolume ?? 0)],
						BuildFrom = level1.BuildFrom ?? DataType.Level1,
					};

					return true;
				}

				Current = null;
				return false;
			}

			public void Reset()
			{
				_level1Enumerator.Reset();
				Current = null;
			}

			object IEnumerator.Current => Current;

			void IDisposable.Dispose()
			{
				Current = null;
				_level1Enumerator.Dispose();

				GC.SuppressFinalize(this);
			}
		}

		//private readonly IEnumerable<Level1ChangeMessage> _level1;

		public OrderBookEnumerable(IEnumerable<Level1ChangeMessage> level1)
			: base(() => new OrderBookEnumerator(level1.GetEnumerator()))
		{
			if (level1 == null)
				throw new ArgumentNullException(nameof(level1));

			//_level1 = level1;
		}

		//int IEnumerableEx.Count => _level1.Count;
	}

	/// <summary>
	/// To convert level1 data into order books.
	/// </summary>
	/// <param name="level1">Level1 data.</param>
	/// <returns>Market depths.</returns>
	public static IEnumerable<QuoteChangeMessage> ToOrderBooks(this IEnumerable<Level1ChangeMessage> level1)
	{
		return new OrderBookEnumerable(level1);
	}

	/// <summary>
	/// To check, are there quotes in the level1.
	/// </summary>
	/// <param name="level1">Level1 data.</param>
	/// <returns>Quotes.</returns>
	public static bool IsContainsQuotes(this Level1ChangeMessage level1)
	{
		if (level1 == null)
			throw new ArgumentNullException(nameof(level1));

		return level1.Changes.ContainsKey(Level1Fields.BestBidPrice) || level1.Changes.ContainsKey(Level1Fields.BestAskPrice);
	}

	/// <summary>
	/// Build market depths from order log.
	/// </summary>
	/// <param name="items">Orders log lines.</param>
	/// <param name="builder">Order log to market depth builder.</param>
	/// <param name="interval">The interval of the order book generation. The default is <see cref="TimeSpan.Zero"/>, which means order books generation at each new item of orders log.</param>
	/// <param name="maxDepth">The maximal depth of order book. The default is <see cref="Int32.MaxValue"/>, which means endless depth.</param>
	/// <returns>Market depths.</returns>
	public static IEnumerable<QuoteChangeMessage> ToOrderBooks(this IEnumerable<ExecutionMessage> items, IOrderLogMarketDepthBuilder builder, TimeSpan interval = default, int maxDepth = int.MaxValue)
	{
		var snapshotSent = false;
		var prevTime = default(DateTime?);

		foreach (var item in items)
		{
			if (!snapshotSent)
			{
				yield return builder.GetSnapshot(item.ServerTime);
				snapshotSent = true;
			}

			var depth = builder.Update(item);
			if (depth is null)
				continue;

			if (prevTime != null && (depth.ServerTime - prevTime.Value) < interval)
				continue;

			if (maxDepth < int.MaxValue)
			{
				depth = builder.GetSnapshot(item.ServerTime); // cannot trim incremental book

				depth.Bids = [.. depth.Bids.Take(maxDepth)];
				depth.Asks = [.. depth.Asks.Take(maxDepth)];
			}
			else if (interval != default)
			{
				depth = builder.GetSnapshot(item.ServerTime); // cannot return incrementals if interval is set
			}

			yield return depth;

			prevTime = depth.ServerTime;
		}
	}

	/// <summary>
	/// To build order books from incremental updates.
	/// </summary>
	/// <param name="books">Order books (may be incremental).</param>
	/// <param name="logs">Logs.</param>
	/// <returns>Full order books.</returns>
	public static IAsyncEnumerable<QuoteChangeMessage> BuildIfNeed(this IAsyncEnumerable<QuoteChangeMessage> books, ILogReceiver logs = null)
	{
		if (books is null)
			throw new ArgumentNullException(nameof(books));

		return Impl(books, logs);

		static async IAsyncEnumerable<QuoteChangeMessage> Impl(IAsyncEnumerable<QuoteChangeMessage> books, ILogReceiver logs, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var builders = new Dictionary<SecurityId, OrderBookIncrementBuilder>();

			await foreach (var book in books.WithCancellation(cancellationToken))
			{
				if (book.State != null)
				{
					var builder = builders.SafeAdd(book.SecurityId, key => new OrderBookIncrementBuilder(key) { Parent = logs ?? LogManager.Instance?.Application });
					var change = builder.TryApply(book);

					if (change != null)
						yield return change;
				}
				else
					yield return book;
			}
		}
	}

	/// <summary>
	/// To build level1 from the order books.
	/// </summary>
	/// <param name="quotes">Order books.</param>
	/// <returns>Level1.</returns>
	public static IAsyncEnumerable<Level1ChangeMessage> ToLevel1(this IAsyncEnumerable<QuoteChangeMessage> quotes)
	{
		if (quotes is null)
			throw new ArgumentNullException(nameof(quotes));

		return Impl(quotes);

		static async IAsyncEnumerable<Level1ChangeMessage> Impl(IAsyncEnumerable<QuoteChangeMessage> quotes, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await foreach (var quote in quotes.WithCancellation(cancellationToken))
			{
				var l1Msg = new Level1ChangeMessage
				{
					SecurityId = quote.SecurityId,
					ServerTime = quote.ServerTime,
					BuildFrom = quote.BuildFrom ?? DataType.MarketDepth,
				};

				if (quote.Bids.Length > 0)
				{
					l1Msg
						.TryAdd(Level1Fields.BestBidPrice, quote.Bids[0].Price)
						.TryAdd(Level1Fields.BestBidVolume, quote.Bids[0].Volume);
				}

				if (quote.Asks.Length > 0)
				{
					l1Msg
						.TryAdd(Level1Fields.BestAskPrice, quote.Asks[0].Price)
						.TryAdd(Level1Fields.BestAskVolume, quote.Asks[0].Volume);
				}

				yield return l1Msg;
			}
		}
	}

	/// <summary>
	/// To build tick trades from the orders log.
	/// </summary>
	/// <param name="items">Orders log lines.</param>
	/// <returns>Tick trades.</returns>
	public static IAsyncEnumerable<ExecutionMessage> ToTicks(this IAsyncEnumerable<ExecutionMessage> items)
	{
		if (items is null)
			throw new ArgumentNullException(nameof(items));

		return Impl(items);

		static async IAsyncEnumerable<ExecutionMessage> Impl(IAsyncEnumerable<ExecutionMessage> items, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var tradesByNum = new HashSet<long>();
			var tradesByString = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

			await foreach (var currItem in items.WithCancellation(cancellationToken))
			{
				if (currItem.TradeId != null)
				{
					var tradeId = currItem.TradeId.Value;
					if (!tradesByNum.Add(tradeId))
						continue;

					tradesByNum.Remove(tradeId);
					yield return currItem.ToTick();
				}
				else if (!currItem.TradeStringId.IsEmpty())
				{
					var tradeId = currItem.TradeStringId;
					if (!tradesByString.Add(tradeId))
						continue;

					tradesByString.Remove(tradeId);
					yield return currItem.ToTick();
				}
			}
		}
	}

	/// <summary>
	/// To build level1 from the orders log.
	/// </summary>
	/// <param name="items">Orders log lines.</param>
	/// <param name="builder">Order log to market depth builder.</param>
	/// <param name="interval">The interval of the order book generation. The default is <see cref="TimeSpan.Zero"/>, which means order books generation at each new item of orders log.</param>
	/// <returns>Tick trades.</returns>
	public static IAsyncEnumerable<Level1ChangeMessage> ToLevel1(this IAsyncEnumerable<ExecutionMessage> items, IOrderLogMarketDepthBuilder builder, TimeSpan interval = default)
	{
		if (builder == null)
			return items.ToLevel1FromTicks();
		else
			return items.ToOrderBooks(builder, interval).BuildIfNeed().ToLevel1();
	}

	private static IAsyncEnumerable<Level1ChangeMessage> ToLevel1FromTicks(this IAsyncEnumerable<ExecutionMessage> items)
	{
		if (items is null)
			throw new ArgumentNullException(nameof(items));

		return Impl(items);

		static async IAsyncEnumerable<Level1ChangeMessage> Impl(IAsyncEnumerable<ExecutionMessage> items, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await foreach (var tick in items.WithCancellation(cancellationToken))
			{
				var l1Msg = new Level1ChangeMessage
				{
					SecurityId = tick.SecurityId,
					ServerTime = tick.ServerTime,
					LocalTime = tick.LocalTime,
				}
				.TryAdd(Level1Fields.LastTradeId, tick.TradeId)
				.TryAdd(Level1Fields.LastTradeStringId, tick.TradeStringId)
				.TryAdd(Level1Fields.LastTradePrice, tick.TradePrice)
				.TryAdd(Level1Fields.LastTradeVolume, tick.TradeVolume)
				.TryAdd(Level1Fields.LastTradeUpDown, tick.IsUpTick)
				.TryAdd(Level1Fields.LastTradeOrigin, tick.OriginSide)
				;

				if (!l1Msg.HasChanges())
					continue;

				yield return l1Msg;
			}
		}
	}

	/// <summary>
	/// To convert level1 data into tick data.
	/// </summary>
	/// <param name="level1">Level1 data.</param>
	/// <returns>Tick data.</returns>
	public static IAsyncEnumerable<ExecutionMessage> ToTicks(this IAsyncEnumerable<Level1ChangeMessage> level1)
	{
		if (level1 is null)
			throw new ArgumentNullException(nameof(level1));

		return Impl(level1);

		static async IAsyncEnumerable<ExecutionMessage> Impl(IAsyncEnumerable<Level1ChangeMessage> level1, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await foreach (var l1 in level1.WithCancellation(cancellationToken))
			{
				if (!l1.IsContainsTick())
					continue;

				yield return l1.ToTick();
			}
		}
	}

	/// <summary>
	/// To convert level1 data into order books.
	/// </summary>
	/// <param name="level1">Level1 data.</param>
	/// <returns>Market depths.</returns>
	public static IAsyncEnumerable<QuoteChangeMessage> ToOrderBooks(this IAsyncEnumerable<Level1ChangeMessage> level1)
	{
		if (level1 is null)
			throw new ArgumentNullException(nameof(level1));

		return Impl(level1);

		static async IAsyncEnumerable<QuoteChangeMessage> Impl(IAsyncEnumerable<Level1ChangeMessage> level1, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			decimal? prevBidPrice = null;
			decimal? prevBidVolume = null;
			decimal? prevAskPrice = null;
			decimal? prevAskVolume = null;

			await foreach (var l1 in level1.WithCancellation(cancellationToken))
			{
				if (!l1.IsContainsQuotes())
					continue;

				var oldBidPrice = prevBidPrice;
				var oldBidVolume = prevBidVolume;
				var oldAskPrice = prevAskPrice;
				var oldAskVolume = prevAskVolume;

				prevBidPrice = l1.TryGetDecimal(Level1Fields.BestBidPrice) ?? prevBidPrice;
				prevBidVolume = l1.TryGetDecimal(Level1Fields.BestBidVolume) ?? prevBidVolume;
				prevAskPrice = l1.TryGetDecimal(Level1Fields.BestAskPrice) ?? prevAskPrice;
				prevAskVolume = l1.TryGetDecimal(Level1Fields.BestAskVolume) ?? prevAskVolume;

				if (prevBidPrice == 0)
					prevBidPrice = null;

				if (prevAskPrice == 0)
					prevAskPrice = null;

				if (oldBidPrice == prevBidPrice && oldBidVolume == prevBidVolume && oldAskPrice == prevAskPrice && oldAskVolume == prevAskVolume)
					continue;

				yield return new QuoteChangeMessage
				{
					SecurityId = l1.SecurityId,
					LocalTime = l1.LocalTime,
					ServerTime = l1.ServerTime,
					Bids = prevBidPrice == null ? [] : [new QuoteChange(prevBidPrice.Value, prevBidVolume ?? 0)],
					Asks = prevAskPrice == null ? [] : [new QuoteChange(prevAskPrice.Value, prevAskVolume ?? 0)],
					BuildFrom = l1.BuildFrom ?? DataType.Level1,
				};
			}
		}
	}

	/// <summary>
	/// Build market depths from order log.
	/// </summary>
	/// <param name="items">Orders log lines.</param>
	/// <param name="builder">Order log to market depth builder.</param>
	/// <param name="interval">The interval of the order book generation. The default is <see cref="TimeSpan.Zero"/>, which means order books generation at each new item of orders log.</param>
	/// <param name="maxDepth">The maximal depth of order book. The default is <see cref="Int32.MaxValue"/>, which means endless depth.</param>
	/// <returns>Market depths.</returns>
	public static IAsyncEnumerable<QuoteChangeMessage> ToOrderBooks(this IAsyncEnumerable<ExecutionMessage> items, IOrderLogMarketDepthBuilder builder, TimeSpan interval = default, int maxDepth = int.MaxValue)
	{
		if (items is null)
			throw new ArgumentNullException(nameof(items));

		if (builder is null)
			throw new ArgumentNullException(nameof(builder));

		return Impl(items, builder, interval, maxDepth);

		static async IAsyncEnumerable<QuoteChangeMessage> Impl(IAsyncEnumerable<ExecutionMessage> items, IOrderLogMarketDepthBuilder builder, TimeSpan interval, int maxDepth, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var snapshotSent = false;
			var prevTime = default(DateTime?);

			await foreach (var item in items.WithCancellation(cancellationToken))
			{
				if (!snapshotSent)
				{
					yield return builder.GetSnapshot(item.ServerTime);
					snapshotSent = true;
				}

				var depth = builder.Update(item);
				if (depth is null)
					continue;

				if (prevTime != null && (depth.ServerTime - prevTime.Value) < interval)
					continue;

				if (maxDepth < int.MaxValue)
				{
					depth = builder.GetSnapshot(item.ServerTime);

					depth.Bids = [.. depth.Bids.Take(maxDepth)];
					depth.Asks = [.. depth.Asks.Take(maxDepth)];
				}
				else if (interval != default)
				{
					depth = builder.GetSnapshot(item.ServerTime);
				}

				yield return depth;

				prevTime = depth.ServerTime;
			}
		}
	}
}
namespace StockSharp.Algo.Storages.Csv;

class MarketDepthCsvSerializer(SecurityId securityId, Encoding encoding) : CsvMarketDataSerializer<QuoteChangeMessage>(securityId, encoding)
{
	private readonly struct QuoteEnumerable(IAsyncEnumerable<NullableTimeQuoteChange> quotes, SecurityId securityId) : IAsyncEnumerable<QuoteChangeMessage>
	{
		private class QuoteEnumerator(IAsyncEnumerator<NullableTimeQuoteChange> enumerator, SecurityId securityId) : IAsyncEnumerator<QuoteChangeMessage>
		{
			private readonly IAsyncEnumerator<NullableTimeQuoteChange> _enumerator = enumerator;
			private readonly SecurityId _securityId = securityId;

			private bool _resetCurrent = true;
			private bool _needMoveNext = true;

			private QuoteChangeMessage _current;
			QuoteChangeMessage IAsyncEnumerator<QuoteChangeMessage>.Current => _current;

			async ValueTask<bool> IAsyncEnumerator<QuoteChangeMessage>.MoveNextAsync()
			{
				if (_resetCurrent)
				{
					_current = null;

					if (_needMoveNext && !await _enumerator.MoveNextAsync())
						return false;
				}

				_needMoveNext = true;

				Sides? side = null;

				var bids = new List<QuoteChange>();
				var asks = new List<QuoteChange>();

				var hasPos = false;

				void Flush()
				{
					_current.Bids = [.. bids];
					_current.Asks = [.. asks];
					_current.HasPositions = hasPos;
				}

				do
				{
					var quote = _enumerator.Current
						?? throw new InvalidOperationException("quote == null");

					if (_current == null)
					{
						_current = new QuoteChangeMessage
						{
							SecurityId = _securityId,
							ServerTime = quote.ServerTime,
							LocalTime = quote.LocalTime,
							State = quote.State,
							BuildFrom = quote.BuildFrom,
							SeqNum = quote.SeqNum ?? 0L,
						};
					}
					else if (_current.ServerTime != quote.ServerTime || (side == Sides.Sell && quote.Side == Sides.Buy))
					{
						_resetCurrent = true;
						_needMoveNext = false;

						Flush();
						return true;
					}

					side = quote.Side;

					if (quote.Quote != null)
					{
						var qq = quote.Quote.Value;

						if (qq.StartPosition != default || qq.EndPosition != default)
							hasPos = true;

						var quotes = quote.Side == Sides.Buy ? bids : asks;
						quotes.Add(new QuoteChange(qq.Price, qq.Volume, qq.OrdersCount, qq.Condition));
					}
				}
				while (await _enumerator.MoveNextAsync());

				if (_current == null)
					return false;

				_resetCurrent = true;
				_needMoveNext = true;

				Flush();
				return true;
			}

			ValueTask IAsyncDisposable.DisposeAsync()
			{
				try
				{
					return _enumerator.DisposeAsync();
				}
				finally
				{
					GC.SuppressFinalize(this);
				}
			}
		}

		private readonly IAsyncEnumerable<NullableTimeQuoteChange> _quotes = quotes ?? throw new ArgumentNullException(nameof(quotes));
		private readonly SecurityId _securityId = securityId;

		IAsyncEnumerator<QuoteChangeMessage> IAsyncEnumerable<QuoteChangeMessage>.GetAsyncEnumerator(CancellationToken cancellationToken)
			=> new QuoteEnumerator(_quotes.GetAsyncEnumerator(cancellationToken), _securityId);
	}

	private readonly CsvMarketDataSerializer<NullableTimeQuoteChange> _quoteSerializer = new QuoteCsvSerializer(securityId, encoding);

	public override IMarketDataMetaInfo CreateMetaInfo(DateTime date)
	{
		return _quoteSerializer.CreateMetaInfo(date);
	}

	private static NullableTimeQuoteChange ToNullQuote(Sides side, QuoteChange quote, QuoteChangeMessage message)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		return new NullableTimeQuoteChange
		{
			ServerTime = message.ServerTime,
			LocalTime = message.LocalTime,
			Side = side,
			State = message.State,
			Quote = quote,
			BuildFrom = message.BuildFrom,
			SeqNum = message.SeqNum.DefaultAsNull(),
		};
	}

	public override ValueTask SerializeAsync(Stream stream, IEnumerable<QuoteChangeMessage> data, IMarketDataMetaInfo metaInfo, CancellationToken cancellationToken)
	{
		var csvInfo = (CsvMetaInfo)metaInfo;
		var incOnly = csvInfo.IncrementalOnly;

		var list = data.SelectMany(d =>
		{
			if (incOnly != null)
			{
				if (incOnly.Value)
				{
					if (d.State == null)
						throw new ArgumentException(LocalizedStrings.StorageRequiredIncremental.Put(true), nameof(data));
				}
				else
				{
					if (d.State != null)
						throw new ArgumentException(LocalizedStrings.StorageRequiredIncremental.Put(false), nameof(data));
				}
			}

			var items = new List<NullableTimeQuoteChange>();

			items.AddRange(d.Bids.OrderByDescending(q => q.Price).Select(q => ToNullQuote(Sides.Buy, q, d)));

			if (items.Count == 0)
			{
				items.Add(new NullableTimeQuoteChange
				{
					Side = Sides.Buy,
					ServerTime = d.ServerTime,
					State = d.State,
					BuildFrom = d.BuildFrom,
					SeqNum = d.SeqNum.DefaultAsNull(),
				});
			}

			var bidsCount = items.Count;

			items.AddRange(d.Asks.OrderBy(q => q.Price).Select(q => ToNullQuote(Sides.Sell, q, d)));

			if (items.Count == bidsCount)
			{
				items.Add(new NullableTimeQuoteChange
				{
					Side = Sides.Sell,
					ServerTime = d.ServerTime,
					State = d.State,
					BuildFrom = d.BuildFrom,
					SeqNum = d.SeqNum.DefaultAsNull(),
				});
			}

			return items;
		});

		return _quoteSerializer.SerializeAsync(stream, list, metaInfo, cancellationToken);
	}

	public override IAsyncEnumerable<QuoteChangeMessage> DeserializeAsync(Stream stream, IMarketDataMetaInfo metaInfo)
	{
		return new QuoteEnumerable(_quoteSerializer.DeserializeAsync(stream, metaInfo), SecurityId);
	}

	protected override ValueTask WriteAsync(CsvFileWriter writer, QuoteChangeMessage data, IMarketDataMetaInfo metaInfo, CancellationToken cancellationToken)
	{
		throw new NotSupportedException();
	}

	protected override QuoteChangeMessage Read(FastCsvReader reader, IMarketDataMetaInfo metaInfo)
	{
		throw new NotSupportedException();
	}
}
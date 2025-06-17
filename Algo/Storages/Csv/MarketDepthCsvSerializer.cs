namespace StockSharp.Algo.Storages.Csv;

class MarketDepthCsvSerializer(SecurityId securityId, Encoding encoding) : CsvMarketDataSerializer<QuoteChangeMessage>(securityId, encoding)
{
	private class QuoteEnumerable : SimpleEnumerable<QuoteChangeMessage>
	{
		private class QuoteEnumerator(IEnumerator<NullableTimeQuoteChange> enumerator, SecurityId securityId) : SimpleEnumerator<QuoteChangeMessage>
		{
			private bool _resetCurrent = true;
			private bool _needMoveNext = true;

			public override bool MoveNext()
			{
				if (_resetCurrent)
				{
					Current = null;

					if (_needMoveNext && !enumerator.MoveNext())
						return false;
				}

				_needMoveNext = true;

				Sides? side = null;

				var bids = new List<QuoteChange>();
				var asks = new List<QuoteChange>();

				var hasPos = false;

				void Flush()
				{
					Current.Bids = [.. bids];
					Current.Asks = [.. asks];
					Current.HasPositions = hasPos;
				}

				do
				{
					var quote = enumerator.Current
						?? throw new InvalidOperationException("quote == null");

					if (Current == null)
					{
						Current = new QuoteChangeMessage
						{
							SecurityId = securityId,
							ServerTime = quote.ServerTime,
							LocalTime = quote.LocalTime,
							State = quote.State,
							BuildFrom = quote.BuildFrom,
							SeqNum = quote.SeqNum ?? 0L,
						};
					}
					else if (Current.ServerTime != quote.ServerTime || (side == Sides.Sell && quote.Side == Sides.Buy))
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
				while (enumerator.MoveNext());

				if (Current == null)
					return false;

				_resetCurrent = true;
				_needMoveNext = true;

				Flush();
				return true;
			}

			public override void Reset()
			{
				enumerator.Reset();

				_resetCurrent = true;
				_needMoveNext = true;

				base.Reset();
			}

			protected override void DisposeManaged()
			{
				enumerator.Dispose();
				base.DisposeManaged();
			}
		}

		public QuoteEnumerable(IEnumerable<NullableTimeQuoteChange> quotes, SecurityId securityId)
			: base(() => new QuoteEnumerator(quotes.GetEnumerator(), securityId))
		{
			if (quotes == null)
				throw new ArgumentNullException(nameof(quotes));
		}
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

	public override void Serialize(Stream stream, IEnumerable<QuoteChangeMessage> data, IMarketDataMetaInfo metaInfo)
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

		_quoteSerializer.Serialize(stream, list, metaInfo);
	}

	public override IEnumerable<QuoteChangeMessage> Deserialize(Stream stream, IMarketDataMetaInfo metaInfo)
	{
		return new QuoteEnumerable(_quoteSerializer.Deserialize(stream, metaInfo), SecurityId);
	}

	protected override void Write(CsvFileWriter writer, QuoteChangeMessage data, IMarketDataMetaInfo metaInfo)
	{
		throw new NotSupportedException();
	}

	protected override QuoteChangeMessage Read(FastCsvReader reader, IMarketDataMetaInfo metaInfo)
	{
		throw new NotSupportedException();
	}
}
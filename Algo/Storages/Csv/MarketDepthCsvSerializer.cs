namespace StockSharp.Algo.Storages.Csv
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Messages;
	using StockSharp.Localization;

	class MarketDepthCsvSerializer : CsvMarketDataSerializer<QuoteChangeMessage>
	{
		private class QuoteEnumerable : SimpleEnumerable<QuoteChangeMessage>
		{
			private class QuoteEnumerator : SimpleEnumerator<QuoteChangeMessage>
			{
				private readonly IEnumerator<NullableTimeQuoteChange> _enumerator;
				private readonly SecurityId _securityId;

				private bool _resetCurrent = true;
				private bool _needMoveNext = true;

				public QuoteEnumerator(IEnumerator<NullableTimeQuoteChange> enumerator, SecurityId securityId)
				{
					_enumerator = enumerator;
					_securityId = securityId;
				}

				public override bool MoveNext()
				{
					if (_resetCurrent)
					{
						Current = null;

						if (_needMoveNext && !_enumerator.MoveNext())
							return false;
					}

					_needMoveNext = true;

					Sides? side = null;

					var bids = new List<QuoteChange>();
					var asks = new List<QuoteChange>();

					var hasPos = false;

					void Flush()
					{
						Current.Bids = bids.ToArray();
						Current.Asks = asks.ToArray();
						Current.HasPositions = hasPos;
					}

					do
					{
						var quote = _enumerator.Current;

						if (quote == null)
							throw new InvalidOperationException("quote == null");

						if (Current == null)
						{
							Current = new QuoteChangeMessage
							{
								SecurityId = _securityId,
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
					while (_enumerator.MoveNext());

					if (Current == null)
						return false;

					_resetCurrent = true;
					_needMoveNext = true;

					Flush();
					return true;
				}

				public override void Reset()
				{
					_enumerator.Reset();

					_resetCurrent = true;
					_needMoveNext = true;

					base.Reset();
				}

				protected override void DisposeManaged()
				{
					_enumerator.Dispose();
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

		private readonly CsvMarketDataSerializer<NullableTimeQuoteChange> _quoteSerializer;

		public MarketDepthCsvSerializer(SecurityId securityId)
			: base(securityId)
		{
			_quoteSerializer = new QuoteCsvSerializer(securityId);
		}

		public override IMarketDataMetaInfo CreateMetaInfo(DateTime date)
		{
			return _quoteSerializer.CreateMetaInfo(date);
		}

		private NullableTimeQuoteChange ToNullQuote(Sides side, QuoteChange quote, QuoteChangeMessage message)
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
							throw new InvalidOperationException(LocalizedStrings.StorageRequiredIncremental.Put(true));
					}
					else
					{
						if (d.State != null)
							throw new InvalidOperationException(LocalizedStrings.StorageRequiredIncremental.Put(false));
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
}
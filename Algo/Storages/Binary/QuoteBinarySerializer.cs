namespace StockSharp.Algo.Storages.Binary;

class QuoteMetaInfo(DateTime date) : BinaryMetaInfo(date)
{
	public bool IncrementalOnly { get; set; }

	public bool HasSnapshot { get; set; }

	public override void Write(Stream stream)
	{
		base.Write(stream);

		stream.WriteEx(FirstPrice);
		stream.WriteEx(LastPrice);

		WriteFractionalVolume(stream);
		WriteLocalTime(stream, MarketDataVersions.Version46);

		if (Version < MarketDataVersions.Version50)
			return;

		stream.WriteEx(ServerOffset);

		if (Version < MarketDataVersions.Version52)
			return;

		WriteOffsets(stream);

		if (Version < MarketDataVersions.Version54)
			return;

		WritePriceStep(stream);

		if (Version < MarketDataVersions.Version58)
			return;

		stream.WriteEx(IncrementalOnly);

		if (Version < MarketDataVersions.Version60)
			return;

		WriteSeqNums(stream);

		if (Version < MarketDataVersions.Version62)
			return;

		stream.WriteEx(HasSnapshot);
	}

	public override void Read(Stream stream)
	{
		base.Read(stream);

		FirstPrice = stream.Read<decimal>();
		LastPrice = stream.Read<decimal>();

		ReadFractionalVolume(stream);
		ReadLocalTime(stream, MarketDataVersions.Version46);

		if (Version < MarketDataVersions.Version50)
			return;

		ServerOffset = stream.Read<TimeSpan>();

		if (Version < MarketDataVersions.Version52)
			return;

		ReadOffsets(stream);

		if (Version < MarketDataVersions.Version54)
			return;

		ReadPriceStep(stream);

		if (Version < MarketDataVersions.Version58)
			return;

		IncrementalOnly = stream.Read<bool>();

		if (Version < MarketDataVersions.Version60)
			return;

		ReadSeqNums(stream);

		if (Version < MarketDataVersions.Version62)
			return;

		HasSnapshot = stream.Read<bool>();
	}

	public override void CopyFrom(BinaryMetaInfo src)
	{
		base.CopyFrom(src);

		var quoteInfo = (QuoteMetaInfo)src;

		IncrementalOnly = quoteInfo.IncrementalOnly;
	}
}

class QuoteBinarySerializer(SecurityId securityId, IExchangeInfoProvider exchangeInfoProvider) : BinaryMarketDataSerializer<QuoteChangeMessage, QuoteMetaInfo>(securityId, DataType.MarketDepth, 16 + 20 * 25, MarketDataVersions.Version62, exchangeInfoProvider)
{
	private readonly OrderBookIncrementBuilder _builder = new(securityId);

	/// <summary>
	/// Pass through incremental <see cref="QuoteChangeMessage"/>.
	/// </summary>
	public bool PassThroughOrderBookIncrement { get; set; }

	protected override void OnSave(BitArrayWriter writer, IEnumerable<QuoteChangeMessage> messages, QuoteMetaInfo metaInfo)
	{
		if (metaInfo.IsEmpty())
		{
			var firstDepth = messages.First();//FirstOrDefault(d => !d.Bids.IsEmpty() || !d.Asks.IsEmpty());

			//var price = firstDepth != null ? GetDepthPrice(firstDepth) : 0;

			//if (price != 0)
			//{
			//	if ((price % metaInfo.PriceStep) == 0)
			//		metaInfo.LastPrice = metaInfo.FirstPrice = price;
			//	else
			//		metaInfo.LastFractionalPrice = metaInfo.FirstFractionalPrice = price;
			//}

			metaInfo.ServerOffset = firstDepth.ServerTime.Offset;
			metaInfo.IncrementalOnly = firstDepth.State != null;
			metaInfo.FirstSeqNum = metaInfo.PrevSeqNum = firstDepth.SeqNum;
		}

		writer.WriteInt(messages.Count());

		QuoteChangeMessage prevQuoteMsg = null;

		var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version47;
		var isUtc = metaInfo.Version >= MarketDataVersions.Version50;
		var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version52;
		var isTickPrecision = metaInfo.Version >= MarketDataVersions.Version53;
		var nonAdjustPrice = metaInfo.Version >= MarketDataVersions.Version54;
		var useLong = metaInfo.Version >= MarketDataVersions.Version55;
		var buildFrom = metaInfo.Version >= MarketDataVersions.Version59;
		var seqNumAndPos = metaInfo.Version >= MarketDataVersions.Version60;
		var largeDecimal = metaInfo.Version >= MarketDataVersions.Version61;
		var saveSnapshot = metaInfo.Version >= MarketDataVersions.Version62;

		if (metaInfo.IncrementalOnly)
		{
			var idx = -1;

			foreach (var m in messages)
			{
				var fullBook = _builder.TryApply(m);
				++idx;

				if (saveSnapshot && !metaInfo.HasSnapshot && fullBook != null)
				{
					metaInfo.HasSnapshot = true;

					fullBook.Currency = m.Currency;
					fullBook.BuildFrom = m.BuildFrom;
					fullBook.IsFiltered = m.IsFiltered;
					fullBook.State = QuoteChangeStates.SnapshotComplete;
					fullBook.HasPositions = m.HasPositions;
					fullBook.SeqNum = m.SeqNum;

					var arr = messages.ToArray();
					arr[idx] = fullBook;
					messages = arr;
				}
			}
		}

		foreach (var m in messages)
		{
			var quoteMsg = m;

			if (metaInfo.IncrementalOnly)
			{
				if (quoteMsg.State == null)
					throw new InvalidOperationException(LocalizedStrings.StorageRequiredIncremental.Put(true));
			}
			else
			{
				if (quoteMsg.State != null)
					throw new InvalidOperationException(LocalizedStrings.StorageRequiredIncremental.Put(false));
			}

			//if (m.IsFullEmpty())
			//	throw new ArgumentException(LocalizedStrings.MarketDepthIsEmpty, nameof(messages));

			//var bid = quoteMsg.GetBestBid();
			//var ask = quoteMsg.GetBestAsk();

			// LMAX has equals best bid and ask
			//if (bid != null && ask != null && bid.Price > ask.Price)
			//	throw new ArgumentException();

			var lastOffset = metaInfo.LastServerOffset;
			metaInfo.LastTime = writer.WriteTime(quoteMsg.ServerTime, metaInfo.LastTime, LocalizedStrings.MarketDepth, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset);
			metaInfo.LastServerOffset = lastOffset;

			QuoteChangeMessage delta;

			if (metaInfo.IncrementalOnly)
			{
				writer.WriteInt((int)quoteMsg.State.Value);
				delta = quoteMsg;
			}
			else
			{
				var isFull = prevQuoteMsg == null;

				writer.Write(isFull);

				delta = isFull ? quoteMsg : prevQuoteMsg.GetDelta(quoteMsg);

				prevQuoteMsg = quoteMsg;
			}

			SerializeQuotes(writer, delta.Bids, metaInfo/*, isFull*/, useLong, nonAdjustPrice, largeDecimal);
			SerializeQuotes(writer, delta.Asks, metaInfo/*, isFull*/, useLong, nonAdjustPrice, largeDecimal);

			//metaInfo.LastPrice = GetDepthPrice(quoteMsg);

			if (metaInfo.Version < MarketDataVersions.Version40)
				continue;

			if (metaInfo.Version < MarketDataVersions.Version46)
				writer.WriteLong((quoteMsg.LocalTime - quoteMsg.ServerTime).Ticks);
			else
			{
				var hasLocalTime = true;

				if (metaInfo.Version >= MarketDataVersions.Version49)
				{
					hasLocalTime = quoteMsg.HasLocalTime(quoteMsg.ServerTime);
					writer.Write(hasLocalTime);
				}

				if (hasLocalTime)
				{
					lastOffset = metaInfo.LastLocalOffset;
					metaInfo.LastLocalTime = writer.WriteTime(quoteMsg.LocalTime, metaInfo.LastLocalTime, LocalizedStrings.MarketDepths, allowNonOrdered, isUtc, metaInfo.LocalOffset, allowDiffOffsets, isTickPrecision, ref lastOffset, true);
					metaInfo.LastLocalOffset = lastOffset;
				}
			}

			if (metaInfo.Version < MarketDataVersions.Version51)
				continue;

			writer.Write(quoteMsg.Currency != null);

			if (quoteMsg.Currency != null)
				writer.WriteInt((int)quoteMsg.Currency.Value);

			if (!buildFrom)
				continue;

			writer.WriteBuildFrom(quoteMsg.BuildFrom);

			if (!seqNumAndPos)
				continue;

			writer.WriteSeqNum(quoteMsg, metaInfo);
			writer.Write(quoteMsg.HasPositions);
		}
	}

	public override QuoteChangeMessage MoveNext(MarketDataEnumerator enumerator)
	{
		var metaInfo = enumerator.MetaInfo;
		var reader = enumerator.Reader;

		var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version47;
		var isUtc = metaInfo.Version >= MarketDataVersions.Version50;
		var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version52;
		var isTickPrecision = metaInfo.Version >= MarketDataVersions.Version53;
		var nonAdjustPrice = metaInfo.Version >= MarketDataVersions.Version54;
		var useLong = metaInfo.Version >= MarketDataVersions.Version55;
		var buildFrom = metaInfo.Version >= MarketDataVersions.Version59;
		var seqNumAndPos = metaInfo.Version >= MarketDataVersions.Version60;
		var largeDecimal = metaInfo.Version >= MarketDataVersions.Version61;

		var prevTime = metaInfo.FirstTime;
		var lastOffset = metaInfo.FirstServerOffset;
		var serverTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.GetTimeZone(isUtc, SecurityId, ExchangeInfoProvider), allowDiffOffsets, isTickPrecision, ref lastOffset);
		metaInfo.FirstTime = prevTime;
		metaInfo.FirstServerOffset = lastOffset;

		QuoteChangeMessage quoteMsg;

		if (metaInfo.IncrementalOnly)
		{
			quoteMsg = new QuoteChangeMessage
			{
				LocalTime = metaInfo.FirstTime,
				SecurityId = SecurityId,
				ServerTime = serverTime,

				State = (QuoteChangeStates)reader.ReadInt(),

				Bids = DeserializeQuotes(reader, metaInfo, useLong, nonAdjustPrice, largeDecimal),
				Asks = DeserializeQuotes(reader, metaInfo, useLong, nonAdjustPrice, largeDecimal),
			};
		}
		else
		{
			var isFull = reader.Read();
			var prevDepth = enumerator.Previous;

			var diff = new QuoteChangeMessage
			{
				LocalTime = metaInfo.FirstTime,
				SecurityId = SecurityId,
				ServerTime = serverTime,
				Bids = DeserializeQuotes(reader, metaInfo, useLong, nonAdjustPrice, largeDecimal),
				Asks = DeserializeQuotes(reader, metaInfo, useLong, nonAdjustPrice, largeDecimal),
			};

			if (metaInfo.Version < MarketDataVersions.Version48)
			{
				diff.Bids = [.. diff.Bids.OrderByDescending(q => q.Price)];
				diff.Asks = [.. diff.Asks.OrderBy(q => q.Price)];
			}

			if (PassThroughOrderBookIncrement)
			{
				quoteMsg = diff;
				quoteMsg.State = isFull ? QuoteChangeStates.SnapshotComplete : QuoteChangeStates.Increment;
			}
			else
				quoteMsg = isFull ? diff : prevDepth.AddDelta(diff);

			//if (depth.BestBid != null && depth.BestAsk != null && depth.BestBid.Price >= depth.BestAsk.Price)
			//	throw new InvalidOperationException("Лучший бид {0} больше или равен лучшему офферу {1}.".Put(depth.BestBid.Price, depth.BestAsk.Price));

			//metaInfo.FirstPrice = GetDepthPrice(quoteMsg);

			if (metaInfo.Version < MarketDataVersions.Version40)
				return quoteMsg;
		}

		if (metaInfo.Version < MarketDataVersions.Version46)
			quoteMsg.LocalTime = quoteMsg.ServerTime - reader.ReadLong().To<TimeSpan>() + metaInfo.LocalOffset;
		else
		{
			var hasLocalTime = true;

			if (metaInfo.Version >= MarketDataVersions.Version49)
				hasLocalTime = reader.Read();

			if (hasLocalTime)
			{
				var prevLocalTime = metaInfo.FirstLocalTime;
				lastOffset = metaInfo.FirstLocalOffset;
				var localTime = reader.ReadTime(ref prevLocalTime, allowNonOrdered, isUtc, metaInfo.LocalOffset, allowDiffOffsets, isTickPrecision, ref lastOffset);
				metaInfo.FirstLocalTime = prevLocalTime;
				quoteMsg.LocalTime = localTime;
				metaInfo.FirstLocalOffset = lastOffset;
			}
			//else
			//	quoteMsg.LocalTime = quoteMsg.Time;
		}

		if (metaInfo.Version >= MarketDataVersions.Version51)
		{
			if (reader.Read())
				quoteMsg.Currency = (CurrencyTypes)reader.ReadInt();
		}

		if (!buildFrom)
			return quoteMsg;

		quoteMsg.BuildFrom = reader.ReadBuildFrom();

		if (!seqNumAndPos)
			return quoteMsg;

		reader.ReadSeqNum(quoteMsg, metaInfo);

		quoteMsg.HasPositions = reader.Read();

		return quoteMsg;
	}

	private void SerializeQuotes(BitArrayWriter writer, QuoteChange[] quotes, QuoteMetaInfo metaInfo/*, bool isFull*/, bool useLong, bool nonAdjustPrice, bool largeDecimal)
	{
		if (writer == null)
			throw new ArgumentNullException(nameof(writer));

		if (quotes == null)
			throw new ArgumentNullException(nameof(quotes));

		if (metaInfo == null)
			throw new ArgumentNullException(nameof(metaInfo));

		var isLess56 = metaInfo.Version < MarketDataVersions.Version56;
		var isLess57 = metaInfo.Version < MarketDataVersions.Version57;
		var isLess58 = metaInfo.Version < MarketDataVersions.Version58;

		writer.WriteInt(quotes.Length);

		foreach (var quote in quotes)
		{
			// quotes for indices may have zero prices
			//if (quote.Price <= 0)
			//	throw new ArgumentOutOfRangeException();

			// some forex connectors do not translate volume
			//
			if (quote.Volume < 0/* || (isFull && quote.Volume == 0)*/)
				throw new ArgumentOutOfRangeException(nameof(quotes), quote.Volume, LocalizedStrings.Volume);

			var pricePrice = metaInfo.LastPrice;
			writer.WritePrice(quote.Price, ref pricePrice, metaInfo, SecurityId, useLong, nonAdjustPrice);
			metaInfo.LastPrice = pricePrice;

			writer.WriteVolume(quote.Volume, metaInfo, largeDecimal);

			if (isLess56)
				continue;

			writer.WriteNullableInt(quote.OrdersCount);

			if (isLess57)
				continue;

			if (quote.Condition != default)
			{
				writer.Write(true);
				writer.WriteInt((int)quote.Condition);
			}
			else
				writer.Write(false);

			if (isLess58)
				continue;

			if (quote.Action != null)
			{
				writer.Write(true);
				writer.WriteInt((int)quote.Action.Value);
			}
			else
				writer.Write(false);

			if (quote.StartPosition != null)
			{
				writer.Write(true);
				writer.WriteInt(quote.StartPosition.Value);
			}
			else
				writer.Write(false);

			if (quote.EndPosition != null)
			{
				writer.Write(true);
				writer.WriteInt(quote.EndPosition.Value);
			}
			else
				writer.Write(false);
		}
	}

	private static QuoteChange[] DeserializeQuotes(BitArrayReader reader, QuoteMetaInfo metaInfo, bool useLong, bool nonAdjustPrice, bool largeDecimal)
	{
		if (reader == null)
			throw new ArgumentNullException(nameof(reader));

		if (metaInfo == null)
			throw new ArgumentNullException(nameof(metaInfo));

		var count = reader.ReadInt();

		if (count == 0)
			return [];

		var is56 = metaInfo.Version >= MarketDataVersions.Version56;
		var is57 = metaInfo.Version >= MarketDataVersions.Version57;
		var is58 = metaInfo.Version >= MarketDataVersions.Version58;

		var quotes = new QuoteChange[count];

		for (var i = 0; i < count; i++)
		{
			var prevPrice = metaInfo.FirstPrice;
			var price = reader.ReadPrice(ref prevPrice, metaInfo, useLong, nonAdjustPrice);
			metaInfo.FirstPrice = prevPrice;

			var volume = reader.ReadVolume(metaInfo, largeDecimal);

			var ordersCount = is56
				? reader.ReadNullableInt()
				: null;

			var condition = is57
				? (QuoteConditions)(reader.ReadNullableInt() ?? 0)
				: default;

			var quote = new QuoteChange(price, volume, ordersCount, condition);

			if (is58)
			{
				if (reader.Read())
					quote.Action = (QuoteChangeActions)reader.ReadInt();

				if (reader.Read())
					quote.StartPosition = reader.ReadInt();

				if (reader.Read())
					quote.EndPosition = reader.ReadInt();
			}

			quotes[i] = quote;
		}

		return quotes;
	}
}
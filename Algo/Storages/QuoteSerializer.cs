namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	class QuoteMetaInfo : BinaryMetaInfo<QuoteMetaInfo>
	{
		public QuoteMetaInfo(DateTime date)
			: base(date)
		{
			FirstPrice = -1;
		}

		public override void Write(Stream stream)
		{
			base.Write(stream);

			stream.Write(FirstPrice);
			stream.Write(LastPrice);

			WriteFractionalVolume(stream);
			WriteLocalTime(stream, MarketDataVersions.Version46);

			if (Version < MarketDataVersions.Version50)
				return;

			stream.Write(ServerOffset);
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
		}

		protected override void CopyFrom(QuoteMetaInfo src)
		{
			base.CopyFrom(src);

			FirstPrice = src.FirstPrice;
			LastPrice = src.LastPrice;
		}
	}

	class QuoteSerializer : BinaryMarketDataSerializer<QuoteChangeMessage, QuoteMetaInfo>
	{
		public QuoteSerializer(SecurityId securityId)
			: base(securityId, 16 + 20 * 25)
		{
			Version = MarketDataVersions.Version50;
		}

		protected override void OnSave(BitArrayWriter writer, IEnumerable<QuoteChangeMessage> messages, QuoteMetaInfo metaInfo)
		{
			if (metaInfo.IsEmpty())
			{
				var firstDepth = messages.FirstOrDefault(d => !d.Bids.IsEmpty() || !d.Asks.IsEmpty());

				if (firstDepth == null)
					throw new ArgumentException(LocalizedStrings.Str931, "messages");

				metaInfo.LastPrice = metaInfo.FirstPrice = GetDepthPrice(firstDepth);

				//pyh: будет баг если первый стакан пустой и с другим временем.
				//metaInfo.FirstTime = firstDepth.LastChangeTime;

				metaInfo.ServerOffset = firstDepth.ServerTime.Offset;
			}

			writer.WriteInt(messages.Count());

			QuoteChangeMessage prevQuoteMsg = null;

			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version47;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version50;

			foreach (var quoteMsg in messages)
			{
				//if (depth.IsFullEmpty())
				//	throw new ArgumentException("Переданный стакан является пустым.", "depths");

				var bid = quoteMsg.GetBestBid();
				var ask = quoteMsg.GetBestAsk();

				// у LMAX бид и оффер могут быть равны
				if (bid != null && ask != null && bid.Price > ask.Price)
					throw new ArgumentException(LocalizedStrings.Str932Params.Put(bid.Price, ask.Price, quoteMsg.ServerTime), "messages");

				metaInfo.LastTime = writer.WriteTime(quoteMsg.ServerTime, metaInfo.LastTime, LocalizedStrings.Str933, allowNonOrdered, isUtc, metaInfo.ServerOffset);

				var isFull = prevQuoteMsg == null;

				writer.Write(isFull); // пишем, полный ли стакан или это дельта

				var delta = isFull ? quoteMsg : prevQuoteMsg.GetDelta(quoteMsg);

				prevQuoteMsg = quoteMsg;

				SerializeQuotes(writer, delta.Bids, metaInfo/*, isFull*/);
				SerializeQuotes(writer, delta.Asks, metaInfo/*, isFull*/);

				metaInfo.LastPrice = GetDepthPrice(quoteMsg);

				if (metaInfo.Version < MarketDataVersions.Version40)
					continue;

				if (metaInfo.Version < MarketDataVersions.Version46)
					writer.WriteLong(SecurityId.GetLatency(quoteMsg.ServerTime, quoteMsg.LocalTime).Ticks);
				else
				{
					var hasLocalTime = true;

					if (metaInfo.Version >= MarketDataVersions.Version49)
					{
						hasLocalTime = !quoteMsg.LocalTime.IsDefault() && quoteMsg.LocalTime != quoteMsg.ServerTime;
						writer.Write(hasLocalTime);
					}

					if (hasLocalTime)
						metaInfo.LastLocalTime = writer.WriteTime(quoteMsg.LocalTime, metaInfo.LastLocalTime, LocalizedStrings.Str934, allowNonOrdered, isUtc, metaInfo.LocalOffset);
				}
			}
		}

		public override QuoteChangeMessage MoveNext(MarketDataEnumerator enumerator)
		{
			var metaInfo = enumerator.MetaInfo;
			var reader = enumerator.Reader;

			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version47;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version50;

			var prevTime = metaInfo.FirstTime;
			var serverTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.GetTimeZone(isUtc, SecurityId));
			metaInfo.FirstTime = prevTime;

			var isFull = reader.Read();
			var prevDepth = enumerator.Previous;

			var bids = DeserializeQuotes(reader, metaInfo, Sides.Buy);
			var asks = DeserializeQuotes(reader, metaInfo, Sides.Sell);

			var diff = new QuoteChangeMessage
			{
				LocalTime = metaInfo.FirstTime,
				SecurityId = SecurityId,
				ServerTime = serverTime,
				Bids = bids,
				Asks = asks,
				IsSorted = true,
			};

			if (metaInfo.Version < MarketDataVersions.Version48)
			{
				diff.Bids = diff.Bids.OrderByDescending(q => q.Price);
				diff.Asks = diff.Asks.OrderBy(q => q.Price);
			}

			var quoteMsg = isFull ? diff : prevDepth.AddDelta(diff);

			//if (depth.BestBid != null && depth.BestAsk != null && depth.BestBid.Price >= depth.BestAsk.Price)
			//	throw new InvalidOperationException("Лучший бид {0} больше или равен лучшему офферу {1}.".Put(depth.BestBid.Price, depth.BestAsk.Price));

			metaInfo.FirstPrice = GetDepthPrice(quoteMsg);

			if (metaInfo.Version < MarketDataVersions.Version40)
				return quoteMsg;

			if (metaInfo.Version < MarketDataVersions.Version46)
				quoteMsg.LocalTime = quoteMsg.ServerTime.LocalDateTime - reader.ReadLong().To<TimeSpan>() + metaInfo.LocalOffset;
			else
			{
				var hasLocalTime = true;

				if (metaInfo.Version >= MarketDataVersions.Version49)
					hasLocalTime = reader.Read();

				if (hasLocalTime)
				{
					var prevLocalTime = metaInfo.FirstLocalTime;
					var localTime = reader.ReadTime(ref prevLocalTime, allowNonOrdered, isUtc, metaInfo.LocalOffset);
					metaInfo.FirstLocalTime = prevLocalTime;
					quoteMsg.LocalTime = localTime.LocalDateTime;
				}
				//else
				//	quoteMsg.LocalTime = quoteMsg.Time;
			}

			return quoteMsg;
		}

		private void SerializeQuotes(BitArrayWriter writer, IEnumerable<QuoteChange> quotes, QuoteMetaInfo metaInfo/*, bool isFull*/)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			if (quotes == null)
				throw new ArgumentNullException("quotes");

			if (metaInfo == null)
				throw new ArgumentNullException("metaInfo");

			var prevPrice = metaInfo.LastPrice;

			writer.WriteInt(quotes.Count());

			foreach (var quote in quotes)
			{
				// quotes for spreads may be a zero cost or less than zero
				//if (quote.Price <= 0)
				//	throw new ArgumentOutOfRangeException("quotes", quote.Price, LocalizedStrings.Str935);

				// котировки от Форекс истории не хранят объем
				//
				if (quote.Volume < 0/* || (isFull && quote.Volume == 0)*/)
					throw new ArgumentOutOfRangeException("quotes", quote.Volume, LocalizedStrings.Str936);

				writer.WritePrice(quote.Price, prevPrice, metaInfo, SecurityId);
				writer.WriteVolume(quote.Volume, metaInfo, SecurityId);

				prevPrice = quote.Price;
			}
		}

		private static IEnumerable<QuoteChange> DeserializeQuotes(BitArrayReader reader, QuoteMetaInfo metaInfo, Sides side)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");

			if (metaInfo == null)
				throw new ArgumentNullException("metaInfo");

			var list = new List<QuoteChange>();

			var deltaCount = reader.ReadInt();

			if (deltaCount == 0)
				return list;

			var prevPrice = metaInfo.FirstPrice;

			for (var i = 0; i < deltaCount; i++)
			{
				metaInfo.FirstPrice = reader.ReadPrice(metaInfo.FirstPrice, metaInfo);

				var volume = reader.ReadVolume(metaInfo);

				list.Add(new QuoteChange(side, metaInfo.FirstPrice, volume));
			}

			metaInfo.FirstPrice = prevPrice;

			return list;
		}

		private static decimal GetDepthPrice(QuoteChangeMessage message)
		{
			var quote = message.GetBestBid() ?? message.GetBestAsk();
			return quote == null ? 0 : quote.Price;
		}
	}
}
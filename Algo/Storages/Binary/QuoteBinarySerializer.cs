#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Binary.Algo
File: QuoteBinarySerializer.cs
Created: 2015, 12, 14, 1:43 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages.Binary
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

	class QuoteMetaInfo : BinaryMetaInfo
	{
		public QuoteMetaInfo(DateTime date)
			: base(date)
		{
			//FirstPrice = -1;
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

			if (Version < MarketDataVersions.Version52)
				return;

			WriteOffsets(stream);

			if (Version < MarketDataVersions.Version54)
				return;

			WritePriceStep(stream);
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
		}
	}

	class QuoteBinarySerializer : BinaryMarketDataSerializer<QuoteChangeMessage, QuoteMetaInfo>
	{
		public QuoteBinarySerializer(SecurityId securityId, IExchangeInfoProvider exchangeInfoProvider)
			: base(securityId, 16 + 20 * 25, MarketDataVersions.Version55, exchangeInfoProvider)
		{
		}

		protected override void OnSave(BitArrayWriter writer, IEnumerable<QuoteChangeMessage> messages, QuoteMetaInfo metaInfo)
		{
			if (metaInfo.IsEmpty())
			{
				var firstDepth = messages.FirstOrDefault(d => !d.Bids.IsEmpty() || !d.Asks.IsEmpty());

				//var price = firstDepth != null ? GetDepthPrice(firstDepth) : 0;

				//if (price != 0)
				//{
				//	if ((price % metaInfo.PriceStep) == 0)
				//		metaInfo.LastPrice = metaInfo.FirstPrice = price;
				//	else
				//		metaInfo.LastFractionalPrice = metaInfo.FirstFractionalPrice = price;
				//}

				metaInfo.ServerOffset = (firstDepth ?? messages.First()).ServerTime.Offset;
			}

			writer.WriteInt(messages.Count());

			QuoteChangeMessage prevQuoteMsg = null;

			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version47;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version50;
			var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version52;
			var isTickPrecision = metaInfo.Version >= MarketDataVersions.Version53;
			var nonAdjustPrice = metaInfo.Version >= MarketDataVersions.Version54;
			var useLong = metaInfo.Version >= MarketDataVersions.Version55;

			foreach (var m in messages)
			{
				var quoteMsg = m;

				//if (m.IsFullEmpty())
				//	throw new ArgumentException(LocalizedStrings.Str1309, nameof(messages));

				if (!quoteMsg.IsSorted)
				{
					quoteMsg = (QuoteChangeMessage)quoteMsg.Clone();

					quoteMsg.Bids = quoteMsg.Bids.OrderByDescending(q => q.Price).ToArray();
					quoteMsg.Asks = quoteMsg.Asks.OrderBy(q => q.Price).ToArray();
				}

				//var bid = quoteMsg.GetBestBid();
				//var ask = quoteMsg.GetBestAsk();

				// LMAX has equals best bid and ask
				//if (bid != null && ask != null && bid.Price > ask.Price)
				//	throw new ArgumentException(LocalizedStrings.Str932Params.Put(bid.Price, ask.Price, quoteMsg.ServerTime), nameof(messages));

				var lastOffset = metaInfo.LastServerOffset;
				metaInfo.LastTime = writer.WriteTime(quoteMsg.ServerTime, metaInfo.LastTime, LocalizedStrings.MarketDepth, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset);
				metaInfo.LastServerOffset = lastOffset;

				var isFull = prevQuoteMsg == null;

				writer.Write(isFull);

				var delta = isFull ? quoteMsg : prevQuoteMsg.GetDelta(quoteMsg);

				prevQuoteMsg = quoteMsg;

				SerializeQuotes(writer, delta.Bids, metaInfo/*, isFull*/, useLong, nonAdjustPrice);
				SerializeQuotes(writer, delta.Asks, metaInfo/*, isFull*/, useLong, nonAdjustPrice);

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
						metaInfo.LastLocalTime = writer.WriteTime(quoteMsg.LocalTime, metaInfo.LastLocalTime, LocalizedStrings.Str934, allowNonOrdered, isUtc, metaInfo.LocalOffset, allowDiffOffsets, isTickPrecision, ref lastOffset, true);
						metaInfo.LastLocalOffset = lastOffset;
					}
				}

				if (metaInfo.Version < MarketDataVersions.Version51)
					continue;

				writer.Write(quoteMsg.Currency != null);

				if (quoteMsg.Currency != null)
					writer.WriteInt((int)quoteMsg.Currency.Value);
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

			var prevTime = metaInfo.FirstTime;
			var lastOffset = metaInfo.FirstServerOffset;
			var serverTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.GetTimeZone(isUtc, SecurityId, ExchangeInfoProvider), allowDiffOffsets, isTickPrecision, ref lastOffset);
			metaInfo.FirstTime = prevTime;
			metaInfo.FirstServerOffset = lastOffset;

			var isFull = reader.Read();
			var prevDepth = enumerator.Previous;

			var bids = DeserializeQuotes(reader, metaInfo, Sides.Buy, useLong, nonAdjustPrice);
			var asks = DeserializeQuotes(reader, metaInfo, Sides.Sell, useLong, nonAdjustPrice);

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

			//metaInfo.FirstPrice = GetDepthPrice(quoteMsg);

			if (metaInfo.Version < MarketDataVersions.Version40)
				return quoteMsg;

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

			return quoteMsg;
		}

		private void SerializeQuotes(BitArrayWriter writer, IEnumerable<QuoteChange> quotes, QuoteMetaInfo metaInfo/*, bool isFull*/, bool useLong, bool nonAdjustPrice)
		{
			if (writer == null)
				throw new ArgumentNullException(nameof(writer));

			if (quotes == null)
				throw new ArgumentNullException(nameof(quotes));

			if (metaInfo == null)
				throw new ArgumentNullException(nameof(metaInfo));

			writer.WriteInt(quotes.Count());

			foreach (var quote in quotes)
			{
				// quotes for indecies may have zero prices
				//if (quote.Price <= 0)
				//	throw new ArgumentOutOfRangeException(nameof(quotes), quote.Price, LocalizedStrings.Str935);

				// some forex connectors do not translate volume
				//
				if (quote.Volume < 0/* || (isFull && quote.Volume == 0)*/)
					throw new ArgumentOutOfRangeException(nameof(quotes), quote.Volume, LocalizedStrings.Str936);

				var pricePrice = metaInfo.LastPrice;
				writer.WritePrice(quote.Price, ref pricePrice, metaInfo, SecurityId, useLong, nonAdjustPrice);
				metaInfo.LastPrice = pricePrice;

				writer.WriteVolume(quote.Volume, metaInfo, SecurityId);
			}
		}

		private static IEnumerable<QuoteChange> DeserializeQuotes(BitArrayReader reader, QuoteMetaInfo metaInfo, Sides side, bool useLong, bool nonAdjustPrice)
		{
			if (reader == null)
				throw new ArgumentNullException(nameof(reader));

			if (metaInfo == null)
				throw new ArgumentNullException(nameof(metaInfo));

			var list = new List<QuoteChange>();

			var deltaCount = reader.ReadInt();

			if (deltaCount == 0)
				return list;

			for (var i = 0; i < deltaCount; i++)
			{
				var prevPrice = metaInfo.FirstPrice;
				var price = reader.ReadPrice(ref prevPrice, metaInfo, useLong, nonAdjustPrice);
				metaInfo.FirstPrice = prevPrice;

				var volume = reader.ReadVolume(metaInfo);

				list.Add(new QuoteChange(side, price, volume));
			}

			return list;
		}

		//private static decimal GetDepthPrice(QuoteChangeMessage message)
		//{
		//	var quote = message.GetBestBid() ?? message.GetBestAsk();
		//	return quote == null ? 0 : quote.Price;
		//}
	}
}
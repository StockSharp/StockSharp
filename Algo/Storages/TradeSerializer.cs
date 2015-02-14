namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	class TradeMetaInfo : BinaryMetaInfo<TradeMetaInfo>
	{
		public TradeMetaInfo(DateTime date)
			: base(date)
		{
			FirstId = -1;
		}

		public long FirstId { get; set; }
		public long PrevId { get; set; }

		public override void Write(Stream stream)
		{
			base.Write(stream);

			stream.Write(FirstId);
			stream.Write(PrevId);
			stream.Write(FirstPrice);
			stream.Write(LastPrice);

			WriteNonSystemPrice(stream);
			WriteFractionalVolume(stream);

			WriteLocalTime(stream, MarketDataVersions.Version47);

			if (Version < MarketDataVersions.Version50)
				return;

			stream.Write(ServerOffset);
		}

		public override void Read(Stream stream)
		{
			base.Read(stream);

			FirstId = stream.Read<long>();
			PrevId = stream.Read<long>();
			FirstPrice = stream.Read<decimal>();
			LastPrice = stream.Read<decimal>();

			ReadNonSystemPrice(stream);
			ReadFractionalVolume(stream);

			ReadLocalTime(stream, MarketDataVersions.Version47);

			if (Version < MarketDataVersions.Version50)
				return;

			ServerOffset = stream.Read<TimeSpan>();
		}

		protected override void CopyFrom(TradeMetaInfo src)
		{
			base.CopyFrom(src);

			FirstId = src.FirstId;
			PrevId = src.PrevId;
			FirstPrice = src.FirstPrice;
			LastPrice = src.LastPrice;
		}
	}

	class TradeSerializer : BinaryMarketDataSerializer<ExecutionMessage, TradeMetaInfo>
	{
		public TradeSerializer(SecurityId securityId)
			: base(securityId, 50)
		{
			Version = MarketDataVersions.Version50;
		}

		protected override void OnSave(BitArrayWriter writer, IEnumerable<ExecutionMessage> messages, TradeMetaInfo metaInfo)
		{
			if (metaInfo.IsEmpty())
			{
				var first = messages.First();

				metaInfo.FirstId = metaInfo.PrevId = first.TradeId;
				metaInfo.ServerOffset = first.ServerTime.Offset;
			}

			writer.WriteInt(messages.Count());

			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version48;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version50;

			foreach (var msg in messages)
			{
				if (msg.ExecutionType != ExecutionTypes.Tick)
					throw new ArgumentOutOfRangeException("messages", msg.ExecutionType, LocalizedStrings.Str1019Params.Put(msg.TradeId));

				// сделки для индексов имеют нулевой номер
				if (msg.TradeId < 0)
					throw new ArgumentOutOfRangeException("messages", msg.TradeId, LocalizedStrings.Str1020);

				// execution ticks (like option execution) may be a zero cost
				// ticks for spreads may be a zero cost or less than zero
				//if (msg.TradePrice < 0)
				//	throw new ArgumentOutOfRangeException("messages", msg.TradePrice, LocalizedStrings.Str1021Params.Put(msg.TradeId));

				// pyhta4og.
				// http://stocksharp.com/forum/yaf_postsm6450_Oshibka-pri-importie-instrumientov-s-Finama.aspx#post6450

				if (msg.Volume < 0)
					throw new ArgumentOutOfRangeException("messages", msg.Volume, LocalizedStrings.Str1022Params.Put(msg.TradeId));

				metaInfo.PrevId = writer.SerializeId(msg.TradeId, metaInfo.PrevId);

				writer.WriteVolume(msg.Volume, metaInfo, SecurityId);
				writer.WritePriceEx(msg.TradePrice, metaInfo, SecurityId);
				writer.WriteSide(msg.OriginSide);

				metaInfo.LastTime = writer.WriteTime(msg.ServerTime, metaInfo.LastTime, LocalizedStrings.Str1023, allowNonOrdered, isUtc, metaInfo.ServerOffset);

				if (metaInfo.Version < MarketDataVersions.Version40)
					continue;

				if (metaInfo.Version < MarketDataVersions.Version47)
					writer.WriteLong(SecurityId.GetLatency(msg.ServerTime, msg.LocalTime).Ticks);
				else
				{
					var hasLocalTime = true;

					if (metaInfo.Version >= MarketDataVersions.Version49)
					{
						hasLocalTime = !msg.LocalTime.IsDefault() && msg.LocalTime != msg.ServerTime;
						writer.Write(hasLocalTime);
					}

					if (hasLocalTime)
						metaInfo.LastLocalTime = writer.WriteTime(msg.LocalTime, metaInfo.LastLocalTime, LocalizedStrings.Str1024, allowNonOrdered, isUtc, metaInfo.LocalOffset);
				}

				if (metaInfo.Version < MarketDataVersions.Version42)
					continue;

				writer.Write(msg.IsSystem);

				if (!msg.IsSystem)
					writer.WriteInt(msg.TradeStatus);

				var oi = msg.OpenInterest;

				if (metaInfo.Version < MarketDataVersions.Version46)
					writer.WriteVolume(oi ?? 0m, metaInfo, SecurityId);
				else
				{
					writer.Write(oi != null);

					if (oi != null)
						writer.WriteVolume(oi.Value, metaInfo, SecurityId);
				}

				if (metaInfo.Version < MarketDataVersions.Version45)
					continue;

				writer.Write(msg.IsUpTick != null);

				if (msg.IsUpTick != null)
					writer.Write(msg.IsUpTick.Value);
			}
		}

		public override ExecutionMessage MoveNext(MarketDataEnumerator enumerator)
		{
			var reader = enumerator.Reader;
			var metaInfo = enumerator.MetaInfo;

			metaInfo.FirstId += reader.ReadLong();
			var volume = reader.ReadVolume(metaInfo);
			var price = reader.ReadPriceEx(metaInfo);

			var orderDirection = reader.Read() ? (reader.Read() ? Sides.Buy : Sides.Sell) : (Sides?)null;

			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version48;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version50;

			var prevTime = metaInfo.FirstTime;
			var serverTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.GetTimeZone(isUtc, SecurityId));
			metaInfo.FirstTime = prevTime;

			var msg = new ExecutionMessage
			{
				//LocalTime = metaInfo.FirstTime,
				ExecutionType = ExecutionTypes.Tick,
				SecurityId = SecurityId,
				TradeId = metaInfo.FirstId,
				Volume = volume,
				OriginSide = orderDirection,
				TradePrice = price,
				ServerTime = serverTime,
			};

			if (metaInfo.Version < MarketDataVersions.Version40)
				return msg;

			if (metaInfo.Version < MarketDataVersions.Version47)
			{
				msg.LocalTime = msg.ServerTime.LocalDateTime - reader.ReadLong().To<TimeSpan>() + metaInfo.LocalOffset;
			}
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
					msg.LocalTime = localTime.LocalDateTime;
				}
				//else
				//	msg.LocalTime = msg.ServerTime;
			}

			if (metaInfo.Version < MarketDataVersions.Version42)
				return msg;

			msg.IsSystem = reader.Read();

			if (!msg.IsSystem)
				msg.TradeStatus = reader.ReadInt();

			if (metaInfo.Version < MarketDataVersions.Version46 || reader.Read())
				msg.OpenInterest = reader.ReadVolume(metaInfo);

			if (metaInfo.Version < MarketDataVersions.Version45)
				return msg;

			if (reader.Read())
				msg.IsUpTick = reader.Read();

			return msg;
		}
	}
}
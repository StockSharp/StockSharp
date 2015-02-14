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

	class CandleMetaInfo : BinaryMetaInfo<CandleMetaInfo>
	{
		public CandleMetaInfo(DateTime date)
			: base(date)
		{
		}

		public override void Write(Stream stream)
		{
			base.Write(stream);

			stream.Write(FirstPrice);
			stream.Write(LastPrice);

			WriteFractionalVolume(stream);

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

			if (Version < MarketDataVersions.Version50)
				return;

			ServerOffset = stream.Read<TimeSpan>();
		}

		protected override void CopyFrom(CandleMetaInfo src)
		{
			base.CopyFrom(src);

			FirstPrice = src.FirstPrice;
			LastPrice = src.LastPrice;
		}
	}

	class CandleSerializer<TCandleMessage> : BinaryMarketDataSerializer<TCandleMessage, CandleMetaInfo>
		where TCandleMessage : CandleMessage, new()
	{
		private readonly object _arg;

		public CandleSerializer(SecurityId securityId, object arg)
			: base(securityId, 74)
		{
			if (arg == null)
				throw new ArgumentNullException("arg");

			_arg = arg;
			Version = MarketDataVersions.Version50;
		}

		protected override void OnSave(BitArrayWriter writer, IEnumerable<TCandleMessage> candles, CandleMetaInfo metaInfo)
		{
			if (metaInfo.IsEmpty())
			{
				var firstCandle = candles.First();

				metaInfo.FirstPrice = firstCandle.LowPrice;
				metaInfo.LastPrice = firstCandle.LowPrice;
				metaInfo.ServerOffset = firstCandle.OpenTime.Offset;
			}

			writer.WriteInt(candles.Count());

			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version49;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version50;

			foreach (var candle in candles)
			{
				writer.WriteVolume(candle.TotalVolume, metaInfo, SecurityId);
				writer.WriteVolume(candle.RelativeVolume, metaInfo, SecurityId);

				writer.WritePrice(candle.LowPrice, metaInfo.LastPrice, metaInfo, SecurityId);
				metaInfo.LastPrice = candle.LowPrice;

				writer.WritePrice(candle.OpenPrice, metaInfo.LastPrice, metaInfo, SecurityId);
				writer.WritePrice(candle.ClosePrice, metaInfo.LastPrice, metaInfo, SecurityId);
				writer.WritePrice(candle.HighPrice, metaInfo.LastPrice, metaInfo, SecurityId);

				metaInfo.LastTime = writer.WriteTime(candle.OpenTime, metaInfo.LastTime, LocalizedStrings.Str998, allowNonOrdered, isUtc, metaInfo.ServerOffset);

				if (metaInfo.Version >= MarketDataVersions.Version46)
				{
					var isAll = !candle.HighTime.IsDefault() && !candle.LowTime.IsDefault();

					DateTimeOffset first;
					DateTimeOffset second;

					writer.Write(isAll);

					if (isAll)
					{
						var isOrdered = candle.HighTime <= candle.LowTime;
						writer.Write(isOrdered);

						first = isOrdered ? candle.HighTime : candle.LowTime;
						second = isOrdered ? candle.LowTime : candle.HighTime;
					}
					else
					{
						writer.Write(!candle.HighTime.IsDefault());
						writer.Write(!candle.LowTime.IsDefault());

						if (candle.HighTime.IsDefault())
						{
							first = candle.LowTime;
							second = default(DateTimeOffset);
						}
						else
						{
							first = candle.HighTime;
							second = default(DateTimeOffset);
						}
					}

					if (!first.IsDefault())
						metaInfo.LastTime = writer.WriteTime(first, metaInfo.LastTime, LocalizedStrings.Str999, allowNonOrdered, isUtc, metaInfo.ServerOffset);

					if (!second.IsDefault())
						metaInfo.LastTime = writer.WriteTime(second, metaInfo.LastTime, LocalizedStrings.Str1000, allowNonOrdered, isUtc, metaInfo.ServerOffset);
				}

				if (metaInfo.Version >= MarketDataVersions.Version47)
				{
					writer.Write(!candle.CloseTime.IsDefault());

					if (!candle.CloseTime.IsDefault())
						metaInfo.LastTime = writer.WriteTime(candle.CloseTime, metaInfo.LastTime, LocalizedStrings.Str1001, allowNonOrdered, isUtc, metaInfo.ServerOffset);
				}
				else
				{
					var time = writer.WriteTime(candle.CloseTime, metaInfo.LastTime, LocalizedStrings.Str1001, allowNonOrdered, isUtc, metaInfo.ServerOffset);
					
					if (metaInfo.Version >= MarketDataVersions.Version41)
						metaInfo.LastTime = time;	
				}

				if (metaInfo.Version >= MarketDataVersions.Version46)
				{
					writer.WriteVolume(candle.OpenVolume, metaInfo, SecurityId);
					writer.WriteVolume(candle.HighVolume, metaInfo, SecurityId);
					writer.WriteVolume(candle.LowVolume, metaInfo, SecurityId);
					writer.WriteVolume(candle.CloseVolume, metaInfo, SecurityId);
				}

				writer.WriteInt((int)candle.State);

				if (metaInfo.Version < MarketDataVersions.Version45)
					continue;

				var oi = candle.OpenInterest;

				if (metaInfo.Version < MarketDataVersions.Version48)
					writer.WriteVolume(oi ?? 0m, metaInfo, SecurityId);
				else
				{
					writer.Write(oi != null);

					if (oi != null)
						writer.WriteVolume(oi.Value, metaInfo, SecurityId);
				}
			}
		}

		public override TCandleMessage MoveNext(MarketDataEnumerator enumerator)
		{
			var reader = enumerator.Reader;
			var metaInfo = enumerator.MetaInfo;

			var candle = new TCandleMessage
			{
				SecurityId = SecurityId,
				TotalVolume = reader.ReadVolume(metaInfo),
				RelativeVolume = reader.ReadVolume(metaInfo),
				LowPrice = reader.ReadPrice(metaInfo.FirstPrice, metaInfo),
				Arg = _arg
			};

			candle.OpenPrice = reader.ReadPrice(candle.LowPrice, metaInfo);
			candle.ClosePrice = reader.ReadPrice(candle.LowPrice, metaInfo);
			candle.HighPrice = reader.ReadPrice(candle.LowPrice, metaInfo);

			var prevTime = metaInfo.FirstTime;
			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version49;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version50;
			var timeZone = metaInfo.GetTimeZone(isUtc, SecurityId);

			candle.OpenTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, timeZone);

			if (metaInfo.Version >= MarketDataVersions.Version46)
			{
				if (reader.Read())
				{
					var isOrdered = reader.Read();

					var first = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, timeZone);
					var second = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, timeZone);

					candle.HighTime = isOrdered ? first : second;
					candle.LowTime = isOrdered ? second : first;
				}
				else
				{
					if (reader.Read())
						candle.HighTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, timeZone);

					if (reader.Read())
						candle.LowTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, timeZone);
				}
			}

			if (metaInfo.Version >= MarketDataVersions.Version47)
			{
				if (reader.Read())
					candle.CloseTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, timeZone);
			}
			else
				candle.CloseTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.LocalOffset);

			if (metaInfo.Version >= MarketDataVersions.Version46)
			{
				candle.OpenVolume = reader.ReadVolume(metaInfo);
				candle.HighVolume = reader.ReadVolume(metaInfo);
				candle.LowVolume = reader.ReadVolume(metaInfo);
				candle.CloseVolume = reader.ReadVolume(metaInfo);
			}

			candle.State = (CandleStates)reader.ReadInt();

			metaInfo.FirstPrice = candle.LowPrice;
			metaInfo.FirstTime = metaInfo.Version <= MarketDataVersions.Version40 ? candle.OpenTime.LocalDateTime : prevTime;

			if (metaInfo.Version >= MarketDataVersions.Version45)
			{
				if (metaInfo.Version < MarketDataVersions.Version48 || reader.Read())
					candle.OpenInterest = reader.ReadVolume(metaInfo);
			}

			return candle;
		}
	}
}
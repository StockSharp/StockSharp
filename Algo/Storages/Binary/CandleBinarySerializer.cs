#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Binary.Algo
File: CandleBinarySerializer.cs
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

			if (Version < MarketDataVersions.Version53)
				return;

			WriteOffsets(stream);
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

			if (Version < MarketDataVersions.Version53)
				return;

			ReadOffsets(stream);
		}

		public override void CopyFrom(CandleMetaInfo src)
		{
			base.CopyFrom(src);

			FirstPrice = src.FirstPrice;
			LastPrice = src.LastPrice;
		}
	}

	class CandleBinarySerializer<TCandleMessage> : BinaryMarketDataSerializer<TCandleMessage, CandleMetaInfo>
		where TCandleMessage : CandleMessage, new()
	{
		private readonly object _arg;

		public CandleBinarySerializer(SecurityId securityId, object arg, IExchangeInfoProvider exchangeInfoProvider)
			: base(securityId, 74, MarketDataVersions.Version55, exchangeInfoProvider)
		{
			if (arg == null)
				throw new ArgumentNullException(nameof(arg));

			_arg = arg;
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
			var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version53;

			foreach (var candle in candles)
			{
				writer.WriteVolume(candle.TotalVolume, metaInfo, SecurityId);

				if (metaInfo.Version < MarketDataVersions.Version52)
					writer.WriteVolume(candle.RelativeVolume ?? 0, metaInfo, SecurityId);
				else
				{
					writer.Write(candle.RelativeVolume != null);

					if (candle.RelativeVolume != null)
						writer.WriteVolume(candle.RelativeVolume.Value, metaInfo, SecurityId);
				}

				writer.WritePrice(candle.LowPrice, metaInfo.LastPrice, metaInfo, SecurityId);
				metaInfo.LastPrice = candle.LowPrice;

				writer.WritePrice(candle.OpenPrice, metaInfo.LastPrice, metaInfo, SecurityId);
				writer.WritePrice(candle.ClosePrice, metaInfo.LastPrice, metaInfo, SecurityId);
				writer.WritePrice(candle.HighPrice, metaInfo.LastPrice, metaInfo, SecurityId);

				var lastOffset = metaInfo.LastServerOffset;
				metaInfo.LastTime = writer.WriteTime(candle.OpenTime, metaInfo.LastTime, LocalizedStrings.Str998, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, ref lastOffset);
				metaInfo.LastServerOffset = lastOffset;

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
					{
						if (first.Offset != lastOffset)
							throw new ArgumentException(LocalizedStrings.WrongTimeOffset.Put(first, lastOffset));

						metaInfo.LastTime = writer.WriteTime(first, metaInfo.LastTime, LocalizedStrings.Str999, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, ref lastOffset);
					}

					if (!second.IsDefault())
					{
						if (second.Offset != lastOffset)
							throw new ArgumentException(LocalizedStrings.WrongTimeOffset.Put(second, lastOffset));

						metaInfo.LastTime = writer.WriteTime(second, metaInfo.LastTime, LocalizedStrings.Str1000, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, ref lastOffset);
					}
				}

				if (metaInfo.Version >= MarketDataVersions.Version47)
				{
					writer.Write(!candle.CloseTime.IsDefault());

					if (!candle.CloseTime.IsDefault())
					{
						if (candle.CloseTime.Offset != lastOffset)
							throw new ArgumentException(LocalizedStrings.WrongTimeOffset.Put(candle.CloseTime, lastOffset));

						metaInfo.LastTime = writer.WriteTime(candle.CloseTime, metaInfo.LastTime, LocalizedStrings.Str1001, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, ref lastOffset);
					}
				}
				else
				{
					var time = writer.WriteTime(candle.CloseTime, metaInfo.LastTime, LocalizedStrings.Str1001, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, ref lastOffset);
					
					if (metaInfo.Version >= MarketDataVersions.Version41)
						metaInfo.LastTime = time;	
				}

				if (metaInfo.Version >= MarketDataVersions.Version46)
				{
					if (metaInfo.Version < MarketDataVersions.Version51)
					{
						writer.WriteVolume(candle.OpenVolume ?? 0m, metaInfo, SecurityId);
						writer.WriteVolume(candle.HighVolume ?? 0m, metaInfo, SecurityId);
						writer.WriteVolume(candle.LowVolume ?? 0m, metaInfo, SecurityId);
						writer.WriteVolume(candle.CloseVolume ?? 0m, metaInfo, SecurityId);
					}
					else
					{
						if (candle.OpenVolume == null)
							writer.Write(false);
						else
						{
							writer.Write(true);
							writer.WriteVolume(candle.OpenVolume.Value, metaInfo, SecurityId);
						}

						if (candle.HighVolume == null)
							writer.Write(false);
						else
						{
							writer.Write(true);
							writer.WriteVolume(candle.HighVolume.Value, metaInfo, SecurityId);
						}

						if (candle.LowVolume == null)
							writer.Write(false);
						else
						{
							writer.Write(true);
							writer.WriteVolume(candle.LowVolume.Value, metaInfo, SecurityId);
						}

						if (candle.CloseVolume == null)
							writer.Write(false);
						else
						{
							writer.Write(true);
							writer.WriteVolume(candle.CloseVolume.Value, metaInfo, SecurityId);
						}
					}
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

				if (metaInfo.Version < MarketDataVersions.Version52)
					continue;

				writer.Write(candle.DownTicks != null);

				if (candle.DownTicks != null)
					writer.WriteInt(candle.DownTicks.Value);

				writer.Write(candle.UpTicks != null);

				if (candle.UpTicks != null)
					writer.WriteInt(candle.UpTicks.Value);

				writer.Write(candle.TotalTicks != null);

				if (candle.TotalTicks != null)
					writer.WriteInt(candle.TotalTicks.Value);

				if (metaInfo.Version < MarketDataVersions.Version54)
					continue;

				var priceLevels = candle.PriceLevels;

				writer.Write(priceLevels != null);

				if (priceLevels == null)
					continue;

				priceLevels = priceLevels.ToArray();

				writer.WriteInt(priceLevels.Count());

				foreach (var level in priceLevels)
				{
					writer.WritePrice(level.Price, metaInfo.LastPrice, metaInfo, SecurityId);

					writer.WriteInt(level.BuyCount);
					writer.WriteInt(level.SellCount);

					writer.WriteVolume(level.BuyVolume, metaInfo, SecurityId);
					writer.WriteVolume(level.SellVolume, metaInfo, SecurityId);

					if (metaInfo.Version >= MarketDataVersions.Version55)
					{
						writer.WriteVolume(level.TotalVolume, metaInfo, SecurityId);
					}

					var volumes = level.BuyVolumes;

					if (volumes == null)
						writer.Write(false);
					else
					{
						writer.Write(true);

						volumes = volumes.ToArray();

						writer.WriteInt(volumes.Count());

						foreach (var volume in volumes)
						{
							writer.WriteVolume(volume, metaInfo, SecurityId);
						}
					}

					volumes = level.SellVolumes;

					if (volumes == null)
						writer.Write(false);
					else
					{
						writer.Write(true);

						volumes = volumes.ToArray();

						writer.WriteInt(volumes.Count());

						foreach (var volume in volumes)
						{
							writer.WriteVolume(volume, metaInfo, SecurityId);
						}
					}
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
				RelativeVolume = metaInfo.Version < MarketDataVersions.Version52 || !reader.Read() ? (decimal?)null : reader.ReadVolume(metaInfo),
				LowPrice = reader.ReadPrice(metaInfo.FirstPrice, metaInfo),
				Arg = _arg
			};

			candle.OpenPrice = reader.ReadPrice(candle.LowPrice, metaInfo);
			candle.ClosePrice = reader.ReadPrice(candle.LowPrice, metaInfo);
			candle.HighPrice = reader.ReadPrice(candle.LowPrice, metaInfo);

			var prevTime = metaInfo.FirstTime;
			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version49;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version50;
			var timeZone = metaInfo.GetTimeZone(isUtc, SecurityId, ExchangeInfoProvider);
			var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version53;

			var lastOffset = metaInfo.FirstServerOffset;
			candle.OpenTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, timeZone, allowDiffOffsets, ref lastOffset);
			metaInfo.FirstServerOffset = lastOffset;

			if (metaInfo.Version >= MarketDataVersions.Version46)
			{
				if (reader.Read())
				{
					var isOrdered = reader.Read();

					var first = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, timeZone, allowDiffOffsets, ref lastOffset);
					var second = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, timeZone, allowDiffOffsets, ref lastOffset);

					candle.HighTime = isOrdered ? first : second;
					candle.LowTime = isOrdered ? second : first;
				}
				else
				{
					if (reader.Read())
						candle.HighTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, timeZone, allowDiffOffsets, ref lastOffset);

					if (reader.Read())
						candle.LowTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, timeZone, allowDiffOffsets, ref lastOffset);
				}
			}

			if (metaInfo.Version >= MarketDataVersions.Version47)
			{
				if (reader.Read())
					candle.CloseTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, timeZone, allowDiffOffsets, ref lastOffset);
			}
			else
				candle.CloseTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.LocalOffset, allowDiffOffsets, ref lastOffset);

			if (metaInfo.Version >= MarketDataVersions.Version46)
			{
				if (metaInfo.Version < MarketDataVersions.Version51)
				{
					candle.OpenVolume = reader.ReadVolume(metaInfo);
					candle.HighVolume = reader.ReadVolume(metaInfo);
					candle.LowVolume = reader.ReadVolume(metaInfo);
					candle.CloseVolume = reader.ReadVolume(metaInfo);
				}
				else
				{
					candle.OpenVolume = reader.Read() ? reader.ReadVolume(metaInfo) : (decimal?)null;
					candle.HighVolume = reader.Read() ? reader.ReadVolume(metaInfo) : (decimal?)null;
					candle.LowVolume = reader.Read() ? reader.ReadVolume(metaInfo) : (decimal?)null;
					candle.CloseVolume = reader.Read() ? reader.ReadVolume(metaInfo) : (decimal?)null;
				}
			}

			candle.State = (CandleStates)reader.ReadInt();

			metaInfo.FirstPrice = candle.LowPrice;
			metaInfo.FirstTime = metaInfo.Version <= MarketDataVersions.Version40 ? candle.OpenTime.LocalDateTime : prevTime;

			if (metaInfo.Version >= MarketDataVersions.Version45)
			{
				if (metaInfo.Version < MarketDataVersions.Version48 || reader.Read())
					candle.OpenInterest = reader.ReadVolume(metaInfo);
			}

			if (metaInfo.Version >= MarketDataVersions.Version52)
			{
				candle.DownTicks = reader.Read() ? reader.ReadInt() : (int?)null;
				candle.UpTicks = reader.Read() ? reader.ReadInt() : (int?)null;
				candle.TotalTicks = reader.Read() ? reader.ReadInt() : (int?)null;
			}

			if (metaInfo.Version >= MarketDataVersions.Version54 && reader.Read())
			{
				var priceLevels = new CandlePriceLevel[reader.ReadInt()];

				for (var i = 0; i < priceLevels.Length; i++)
				{
					var priceLevel = new CandlePriceLevel
					{
						Price = reader.ReadPrice(candle.LowPrice, metaInfo),
						BuyCount = reader.ReadInt(),
						SellCount = reader.ReadInt(),
						BuyVolume = reader.ReadVolume(metaInfo),
						SellVolume = reader.ReadVolume(metaInfo)
					};

					if (metaInfo.Version >= MarketDataVersions.Version55)
						priceLevel.TotalVolume = reader.ReadVolume(metaInfo);

					if (reader.Read())
					{
						var volumes = new decimal[reader.ReadInt()];

						for (var j = 0; j < volumes.Length; j++)
							volumes[j] = reader.ReadVolume(metaInfo);

						priceLevel.BuyVolumes = volumes;
					}

					if (reader.Read())
					{
						var volumes = new decimal[reader.ReadInt()];

						for (var j = 0; j < volumes.Length; j++)
							volumes[j] = reader.ReadVolume(metaInfo);

						priceLevel.SellVolumes = volumes;
					}

					priceLevels[i] = priceLevel;
				}

				candle.PriceLevels = priceLevels;
			}

			return candle;
		}
	}
}
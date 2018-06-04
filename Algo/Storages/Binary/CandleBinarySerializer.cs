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

	class CandleMetaInfo : BinaryMetaInfo
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

			if (Version < MarketDataVersions.Version56)
				return;

			WriteFractionalPrice(stream);
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

			if (Version < MarketDataVersions.Version56)
				return;

			ReadFractionalPrice(stream);
		}
	}

	class CandleBinarySerializer<TCandleMessage> : BinaryMarketDataSerializer<TCandleMessage, CandleMetaInfo>
		where TCandleMessage : CandleMessage, new()
	{
		private readonly object _arg;

		public CandleBinarySerializer(SecurityId securityId, object arg, IExchangeInfoProvider exchangeInfoProvider)
			: base(securityId, 74, MarketDataVersions.Version58, exchangeInfoProvider)
		{
			_arg = arg ?? throw new ArgumentNullException(nameof(arg));
		}

		protected override void OnSave(BitArrayWriter writer, IEnumerable<TCandleMessage> candles, CandleMetaInfo metaInfo)
		{
			if (metaInfo.IsEmpty())
			{
				var firstCandle = candles.First();

				var low = firstCandle.LowPrice;

				if ((low % metaInfo.PriceStep) == 0)
					metaInfo.FirstPrice = metaInfo.LastPrice = low;
				else
					metaInfo.FirstFractionalPrice = metaInfo.LastFractionalPrice = low;

				metaInfo.ServerOffset = firstCandle.OpenTime.Offset;
			}

			writer.WriteInt(candles.Count());

			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version49;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version50;
			var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version53;
			var bigRange = metaInfo.Version >= MarketDataVersions.Version57;
			var isTickPrecision = bigRange;
			var useLong = metaInfo.Version >= MarketDataVersions.Version58;

			foreach (var candle in candles)
			{
				if (candle.State == CandleStates.Active)
					throw new ArgumentException(LocalizedStrings.CandleActiveNotSupport.Put(candle), nameof(candle));

				writer.WriteVolume(candle.TotalVolume, metaInfo, SecurityId);

				if (metaInfo.Version < MarketDataVersions.Version52)
					writer.WriteVolume(candle.RelativeVolume ?? 0, metaInfo, SecurityId);
				else
				{
					writer.Write(candle.RelativeVolume != null);

					if (candle.RelativeVolume != null)
						writer.WriteVolume(candle.RelativeVolume.Value, metaInfo, SecurityId);
				}

				if (metaInfo.Version < MarketDataVersions.Version56)
				{
					var prevPrice = metaInfo.LastPrice;
					writer.WritePrice(candle.LowPrice, ref prevPrice, metaInfo, SecurityId);
					metaInfo.LastPrice = prevPrice;

					prevPrice = metaInfo.LastPrice;
					writer.WritePrice(candle.OpenPrice, ref prevPrice, metaInfo, SecurityId);

					prevPrice = metaInfo.LastPrice;
					writer.WritePrice(candle.ClosePrice, ref prevPrice, metaInfo, SecurityId);

					prevPrice = metaInfo.LastPrice;
					writer.WritePrice(candle.HighPrice, ref prevPrice, metaInfo, SecurityId);
				}
				else
				{
					writer.WritePriceEx(candle.LowPrice, metaInfo, SecurityId, useLong);

					if (candle.OpenPrice <= candle.ClosePrice)
					{
						writer.Write(true);

						writer.WritePriceEx(candle.OpenPrice, metaInfo, SecurityId, useLong);
						writer.WritePriceEx(candle.ClosePrice, metaInfo, SecurityId, useLong);
					}
					else
					{
						writer.Write(false);

						writer.WritePriceEx(candle.ClosePrice, metaInfo, SecurityId, useLong);
						writer.WritePriceEx(candle.OpenPrice, metaInfo, SecurityId, useLong);
					}

					writer.WritePriceEx(candle.HighPrice, metaInfo, SecurityId, useLong);
				}

				if (!candle.CloseTime.IsDefault() && candle.OpenTime > candle.CloseTime)
					throw new ArgumentException(LocalizedStrings.MoreThanCloseTime.Put(candle.OpenTime, candle.CloseTime));

				var lastOffset = metaInfo.LastServerOffset;
				metaInfo.LastTime = writer.WriteTime(candle.OpenTime, metaInfo.LastTime, LocalizedStrings.Str998, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset);
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

						if (!candle.CloseTime.IsDefault() && first > candle.CloseTime)
							throw new ArgumentException(LocalizedStrings.MoreThanCloseTime.Put(first, candle.CloseTime));

						metaInfo.LastTime = writer.WriteTime(first, metaInfo.LastTime, LocalizedStrings.Str999, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset, bigRange);
					}

					if (!second.IsDefault())
					{
						if (second.Offset != lastOffset)
							throw new ArgumentException(LocalizedStrings.WrongTimeOffset.Put(second, lastOffset));

						if (!candle.CloseTime.IsDefault() && second > candle.CloseTime)
							throw new ArgumentException(LocalizedStrings.MoreThanCloseTime.Put(second, candle.CloseTime));

						metaInfo.LastTime = writer.WriteTime(second, metaInfo.LastTime, LocalizedStrings.Str1000, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset, bigRange);
					}
				}

				if (metaInfo.Version >= MarketDataVersions.Version47)
				{
					writer.Write(!candle.CloseTime.IsDefault());

					if (!candle.CloseTime.IsDefault())
					{
						if (candle.CloseTime.Offset != lastOffset)
							throw new ArgumentException(LocalizedStrings.WrongTimeOffset.Put(candle.CloseTime, lastOffset));

						metaInfo.LastTime = writer.WriteTime(candle.CloseTime, metaInfo.LastTime, LocalizedStrings.Str1001, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset, bigRange);
					}
				}
				else
				{
					var time = writer.WriteTime(candle.CloseTime, metaInfo.LastTime, LocalizedStrings.Str1001, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, false, ref lastOffset);
					
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
					if (metaInfo.Version < MarketDataVersions.Version56)
					{
						var prevPrice = metaInfo.LastPrice;
						writer.WritePrice(level.Price, ref prevPrice, metaInfo, SecurityId);
						metaInfo.LastPrice = prevPrice;
					}
					else
						writer.WritePriceEx(level.Price, metaInfo, SecurityId);

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
				Arg = _arg
			};

			var prevTime = metaInfo.FirstTime;
			var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version49;
			var isUtc = metaInfo.Version >= MarketDataVersions.Version50;
			var timeZone = metaInfo.GetTimeZone(isUtc, SecurityId, ExchangeInfoProvider);
			var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version53;
			var isTickPrecision = metaInfo.Version >= MarketDataVersions.Version57;
			var useLong = metaInfo.Version >= MarketDataVersions.Version58;

			if (metaInfo.Version < MarketDataVersions.Version56)
			{
				var prevPrice = metaInfo.FirstPrice;
				candle.LowPrice = reader.ReadPrice(ref prevPrice, metaInfo);
				metaInfo.FirstPrice = prevPrice;

				prevPrice = metaInfo.FirstPrice;
				candle.OpenPrice = reader.ReadPrice(ref prevPrice, metaInfo);

				prevPrice = metaInfo.FirstPrice;
				candle.ClosePrice = reader.ReadPrice(ref prevPrice, metaInfo);

				prevPrice = metaInfo.FirstPrice;
				candle.HighPrice = reader.ReadPrice(ref prevPrice, metaInfo);
			}
			else
			{
				candle.LowPrice = reader.ReadPriceEx(metaInfo, useLong);

				if (reader.Read())
				{
					candle.OpenPrice = reader.ReadPriceEx(metaInfo, useLong);
					candle.ClosePrice = reader.ReadPriceEx(metaInfo, useLong);
				}
				else
				{
					candle.ClosePrice = reader.ReadPriceEx(metaInfo, useLong);
					candle.OpenPrice = reader.ReadPriceEx(metaInfo, useLong);
				}

				candle.HighPrice = reader.ReadPriceEx(metaInfo, useLong);
			}

			var lastOffset = metaInfo.FirstServerOffset;
			candle.OpenTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, timeZone, allowDiffOffsets, isTickPrecision, ref lastOffset);
			metaInfo.FirstServerOffset = lastOffset;

			if (metaInfo.Version >= MarketDataVersions.Version46)
			{
				if (reader.Read())
				{
					var isOrdered = reader.Read();

					var first = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, timeZone, allowDiffOffsets, isTickPrecision, ref lastOffset);
					var second = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, timeZone, allowDiffOffsets, isTickPrecision, ref lastOffset);

					candle.HighTime = isOrdered ? first : second;
					candle.LowTime = isOrdered ? second : first;
				}
				else
				{
					if (reader.Read())
						candle.HighTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, timeZone, allowDiffOffsets, isTickPrecision, ref lastOffset);

					if (reader.Read())
						candle.LowTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, timeZone, allowDiffOffsets, isTickPrecision, ref lastOffset);
				}
			}

			if (metaInfo.Version >= MarketDataVersions.Version47)
			{
				if (reader.Read())
					candle.CloseTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, timeZone, allowDiffOffsets, isTickPrecision, ref lastOffset);
			}
			else
				candle.CloseTime = reader.ReadTime(ref prevTime, allowNonOrdered, isUtc, metaInfo.LocalOffset, allowDiffOffsets, false, ref lastOffset);

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
					var prevPrice = metaInfo.FirstPrice;

					var priceLevel = new CandlePriceLevel
					{
						Price = metaInfo.Version < MarketDataVersions.Version56
								? reader.ReadPrice(ref prevPrice, metaInfo)
								: reader.ReadPriceEx(metaInfo),
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
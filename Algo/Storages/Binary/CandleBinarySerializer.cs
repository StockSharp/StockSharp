namespace StockSharp.Algo.Storages.Binary;

class CandleMetaInfo(DateTime date) : BinaryMetaInfo(date)
{
	public override void Write(Stream stream)
	{
		base.Write(stream);

		stream.WriteEx(FirstPrice);
		stream.WriteEx(LastPrice);

		WriteFractionalVolume(stream);

		if (Version < MarketDataVersions.Version50)
			return;

		stream.WriteEx(ServerOffset);

		if (Version < MarketDataVersions.Version53)
			return;

		WriteOffsets(stream);

		if (Version < MarketDataVersions.Version56)
			return;

		WriteFractionalPrice(stream);

		if (Version < MarketDataVersions.Version60)
			return;

		WriteSeqNums(stream);
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

		if (Version < MarketDataVersions.Version60)
			return;

		ReadSeqNums(stream);
	}
}

class CandleBinarySerializer<TCandleMessage>(SecurityId securityId, DataType dataType, IExchangeInfoProvider exchangeInfoProvider) : BinaryMarketDataSerializer<TCandleMessage, CandleMetaInfo>(securityId, dataType, 74, MarketDataVersions.Version62, exchangeInfoProvider)
	where TCandleMessage : CandleMessage, new()
{
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
			metaInfo.FirstSeqNum = metaInfo.PrevSeqNum = firstCandle.SeqNum;
		}

		writer.WriteInt(candles.Count());

		var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version49;
		var isUtc = metaInfo.Version >= MarketDataVersions.Version50;
		var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version53;
		var useLevels = metaInfo.Version >= MarketDataVersions.Version54;
		var bigRange = metaInfo.Version >= MarketDataVersions.Version57;
		var isTickPrecision = bigRange;
		var useLong = metaInfo.Version >= MarketDataVersions.Version58;
		var buildFrom = metaInfo.Version >= MarketDataVersions.Version59;
		var seqNum = metaInfo.Version >= MarketDataVersions.Version60;
		var largeDecimal = metaInfo.Version >= MarketDataVersions.Version61;

		foreach (var candle in candles)
		{
			if (candle.State == CandleStates.Active)
				throw new ArgumentException(LocalizedStrings.CandleActiveNotSupport.Put(candle), nameof(candle));

			writer.WriteVolume(candle.TotalVolume, metaInfo, largeDecimal);

			if (metaInfo.Version < MarketDataVersions.Version52)
				writer.WriteVolume(candle.RelativeVolume ?? 0, metaInfo, false);
			else
			{
				writer.Write(candle.RelativeVolume != null);

				if (candle.RelativeVolume != null)
					writer.WriteVolume(candle.RelativeVolume.Value, metaInfo, largeDecimal);
			}

			if (metaInfo.Version >= MarketDataVersions.Version62)
			{
				writer.Write(candle.BuyVolume != null);

				if (candle.BuyVolume != null)
					writer.WriteVolume(candle.BuyVolume.Value, metaInfo, largeDecimal);

				writer.Write(candle.SellVolume != null);

				if (candle.SellVolume != null)
					writer.WriteVolume(candle.SellVolume.Value, metaInfo, largeDecimal);
			}

			if (metaInfo.Version < MarketDataVersions.Version56)
			{
				var prevPrice = metaInfo.LastPrice;
				writer.WritePrice(candle.LowPrice, ref prevPrice, metaInfo, SecurityId, false, false);
				metaInfo.LastPrice = prevPrice;

				prevPrice = metaInfo.LastPrice;
				writer.WritePrice(candle.OpenPrice, ref prevPrice, metaInfo, SecurityId, false, false);

				prevPrice = metaInfo.LastPrice;
				writer.WritePrice(candle.ClosePrice, ref prevPrice, metaInfo, SecurityId, false, false);

				prevPrice = metaInfo.LastPrice;
				writer.WritePrice(candle.HighPrice, ref prevPrice, metaInfo, SecurityId, false, false);
			}
			else
			{
				writer.WritePriceEx(candle.LowPrice, metaInfo, SecurityId, useLong, largeDecimal);

				if (candle.OpenPrice <= candle.ClosePrice)
				{
					writer.Write(true);

					writer.WritePriceEx(candle.OpenPrice, metaInfo, SecurityId, useLong, largeDecimal);
					writer.WritePriceEx(candle.ClosePrice, metaInfo, SecurityId, useLong, largeDecimal);
				}
				else
				{
					writer.Write(false);

					writer.WritePriceEx(candle.ClosePrice, metaInfo, SecurityId, useLong, largeDecimal);
					writer.WritePriceEx(candle.OpenPrice, metaInfo, SecurityId, useLong, largeDecimal);
				}

				writer.WritePriceEx(candle.HighPrice, metaInfo, SecurityId, useLong, largeDecimal);
			}

			if (candle.CloseTime != default && candle.OpenTime > candle.CloseTime)
				throw new ArgumentException(LocalizedStrings.MoreThanCloseTime.Put(candle.OpenTime, candle.CloseTime));

			var lastOffset = metaInfo.LastServerOffset;
			metaInfo.LastTime = writer.WriteTime(candle.OpenTime, metaInfo.LastTime, LocalizedStrings.CandleOpenTime, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset);
			metaInfo.LastServerOffset = lastOffset;

			if (metaInfo.Version >= MarketDataVersions.Version46)
			{
				var isAll = candle.HighTime != default && candle.LowTime != default;

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
					writer.Write(candle.HighTime != default);
					writer.Write(candle.LowTime != default);

					first = candle.HighTime == default ? candle.LowTime : candle.HighTime;
					second = default;
				}

				if (first != default)
				{
					if (first.Offset != lastOffset && !allowDiffOffsets)
						throw new ArgumentException(LocalizedStrings.WrongTimeOffset.Put(first, lastOffset));

					if (candle.CloseTime != default && first > candle.CloseTime)
						throw new ArgumentException(LocalizedStrings.MoreThanCloseTime.Put(first, candle.CloseTime));

					metaInfo.LastTime = writer.WriteTime(first, metaInfo.LastTime, LocalizedStrings.CandleHighTime, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset, bigRange);
				}

				if (second != default)
				{
					if (second.Offset != lastOffset && !allowDiffOffsets)
						throw new ArgumentException(LocalizedStrings.WrongTimeOffset.Put(second, lastOffset));

					if (candle.CloseTime != default && second > candle.CloseTime)
						throw new ArgumentException(LocalizedStrings.MoreThanCloseTime.Put(second, candle.CloseTime));

					metaInfo.LastTime = writer.WriteTime(second, metaInfo.LastTime, LocalizedStrings.CandleLowTime, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset, bigRange);
				}
			}

			if (metaInfo.Version >= MarketDataVersions.Version47)
			{
				writer.Write(candle.CloseTime != default);

				if (candle.CloseTime != default)
				{
					if (candle.CloseTime.Offset != lastOffset && !allowDiffOffsets)
						throw new ArgumentException(LocalizedStrings.WrongTimeOffset.Put(candle.CloseTime, lastOffset));

					metaInfo.LastTime = writer.WriteTime(candle.CloseTime, metaInfo.LastTime, LocalizedStrings.CandleCloseTime, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, isTickPrecision, ref lastOffset, bigRange);
				}
			}
			else
			{
				var time = writer.WriteTime(candle.CloseTime, metaInfo.LastTime, LocalizedStrings.CandleCloseTime, allowNonOrdered, isUtc, metaInfo.ServerOffset, allowDiffOffsets, false, ref lastOffset);

				if (metaInfo.Version >= MarketDataVersions.Version41)
					metaInfo.LastTime = time;
			}

			if (metaInfo.Version >= MarketDataVersions.Version46)
			{
				if (metaInfo.Version < MarketDataVersions.Version51)
				{
					writer.WriteVolume(candle.OpenVolume ?? 0m, metaInfo, false);
					writer.WriteVolume(candle.HighVolume ?? 0m, metaInfo, false);
					writer.WriteVolume(candle.LowVolume ?? 0m, metaInfo, false);
					writer.WriteVolume(candle.CloseVolume ?? 0m, metaInfo, false);
				}
				else
				{
					if (candle.OpenVolume == null)
						writer.Write(false);
					else
					{
						writer.Write(true);
						writer.WriteVolume(candle.OpenVolume.Value, metaInfo, largeDecimal);
					}

					if (candle.HighVolume == null)
						writer.Write(false);
					else
					{
						writer.Write(true);
						writer.WriteVolume(candle.HighVolume.Value, metaInfo, largeDecimal);
					}

					if (candle.LowVolume == null)
						writer.Write(false);
					else
					{
						writer.Write(true);
						writer.WriteVolume(candle.LowVolume.Value, metaInfo, largeDecimal);
					}

					if (candle.CloseVolume == null)
						writer.Write(false);
					else
					{
						writer.Write(true);
						writer.WriteVolume(candle.CloseVolume.Value, metaInfo, largeDecimal);
					}
				}
			}

			writer.WriteInt((int)candle.State);

			if (metaInfo.Version < MarketDataVersions.Version45)
				continue;

			var oi = candle.OpenInterest;

			if (metaInfo.Version < MarketDataVersions.Version48)
				writer.WriteVolume(oi ?? 0m, metaInfo, false);
			else
			{
				writer.Write(oi != null);

				if (oi != null)
					writer.WriteVolume(oi.Value, metaInfo, largeDecimal);
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

			if (!useLevels)
				continue;

			var priceLevels = candle.PriceLevels;

			writer.Write(priceLevels != null);

			if (priceLevels != null)
			{
				priceLevels = [.. priceLevels];

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
						writer.WritePriceEx(level.Price, metaInfo, SecurityId, false, largeDecimal);

					writer.WriteInt(level.BuyCount);
					writer.WriteInt(level.SellCount);

					writer.WriteVolume(level.BuyVolume, metaInfo, largeDecimal);
					writer.WriteVolume(level.SellVolume, metaInfo, largeDecimal);

					if (metaInfo.Version >= MarketDataVersions.Version55)
					{
						writer.WriteVolume(level.TotalVolume, metaInfo, largeDecimal);
					}

					var volumes = level.BuyVolumes;

					if (volumes == null)
						writer.Write(false);
					else
					{
						writer.Write(true);

						volumes = [.. volumes];

						writer.WriteInt(volumes.Count());

						foreach (var volume in volumes)
						{
							writer.WriteVolume(volume, metaInfo, largeDecimal);
						}
					}

					volumes = level.SellVolumes;

					if (volumes == null)
						writer.Write(false);
					else
					{
						writer.Write(true);

						volumes = [.. volumes];

						writer.WriteInt(volumes.Count());

						foreach (var volume in volumes)
						{
							writer.WriteVolume(volume, metaInfo, largeDecimal);
						}
					}
				}
			}

			if (!buildFrom)
				continue;

			writer.WriteBuildFrom(candle.BuildFrom);

			if (!seqNum)
				continue;

			writer.WriteSeqNum(candle, metaInfo);
		}
	}

	public override TCandleMessage MoveNext(MarketDataEnumerator enumerator)
	{
		var reader = enumerator.Reader;
		var metaInfo = enumerator.MetaInfo;

		var prevTime = metaInfo.FirstTime;
		var allowNonOrdered = metaInfo.Version >= MarketDataVersions.Version49;
		var isUtc = metaInfo.Version >= MarketDataVersions.Version50;
		var timeZone = metaInfo.GetTimeZone(isUtc, SecurityId, ExchangeInfoProvider);
		var allowDiffOffsets = metaInfo.Version >= MarketDataVersions.Version53;
		var useLevels = metaInfo.Version >= MarketDataVersions.Version54;
		var isTickPrecision = metaInfo.Version >= MarketDataVersions.Version57;
		var useLong = metaInfo.Version >= MarketDataVersions.Version58;
		var buildFrom = metaInfo.Version >= MarketDataVersions.Version59;
		var seqNum = metaInfo.Version >= MarketDataVersions.Version60;
		var largeDecimal = metaInfo.Version >= MarketDataVersions.Version61;

		var candle = new TCandleMessage
		{
			SecurityId = SecurityId,
			TotalVolume = reader.ReadVolume(metaInfo, largeDecimal),
			RelativeVolume = metaInfo.Version < MarketDataVersions.Version52 || !reader.Read() ? null : reader.ReadVolume(metaInfo, largeDecimal),
			BuyVolume  = metaInfo.Version < MarketDataVersions.Version62 || !reader.Read() ? null : reader.ReadVolume(metaInfo, largeDecimal),
			SellVolume = metaInfo.Version < MarketDataVersions.Version62 || !reader.Read() ? null : reader.ReadVolume(metaInfo, largeDecimal),
			DataType = DataType
		};

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
			candle.LowPrice = reader.ReadPriceEx(metaInfo, useLong, largeDecimal);

			if (reader.Read())
			{
				candle.OpenPrice = reader.ReadPriceEx(metaInfo, useLong, largeDecimal);
				candle.ClosePrice = reader.ReadPriceEx(metaInfo, useLong, largeDecimal);
			}
			else
			{
				candle.ClosePrice = reader.ReadPriceEx(metaInfo, useLong, largeDecimal);
				candle.OpenPrice = reader.ReadPriceEx(metaInfo, useLong, largeDecimal);
			}

			candle.HighPrice = reader.ReadPriceEx(metaInfo, useLong, largeDecimal);
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
				candle.OpenVolume = reader.ReadVolume(metaInfo, false);
				candle.HighVolume = reader.ReadVolume(metaInfo, false);
				candle.LowVolume = reader.ReadVolume(metaInfo, false);
				candle.CloseVolume = reader.ReadVolume(metaInfo, false);
			}
			else
			{
				candle.OpenVolume = reader.Read() ? reader.ReadVolume(metaInfo, largeDecimal) : null;
				candle.HighVolume = reader.Read() ? reader.ReadVolume(metaInfo, largeDecimal) : null;
				candle.LowVolume = reader.Read() ? reader.ReadVolume(metaInfo, largeDecimal) : null;
				candle.CloseVolume = reader.Read() ? reader.ReadVolume(metaInfo, largeDecimal) : null;
			}
		}

		candle.State = (CandleStates)reader.ReadInt();

		metaInfo.FirstTime = metaInfo.Version <= MarketDataVersions.Version40 ? candle.OpenTime.LocalDateTime : prevTime;

		if (metaInfo.Version >= MarketDataVersions.Version45)
		{
			if (metaInfo.Version < MarketDataVersions.Version48 || reader.Read())
				candle.OpenInterest = reader.ReadVolume(metaInfo, largeDecimal);
		}

		if (metaInfo.Version >= MarketDataVersions.Version52)
		{
			candle.DownTicks = reader.Read() ? reader.ReadInt() : null;
			candle.UpTicks = reader.Read() ? reader.ReadInt() : null;
			candle.TotalTicks = reader.Read() ? reader.ReadInt() : null;
		}

		if (!useLevels)
			return candle;

		if (reader.Read())
		{
			var priceLevels = new CandlePriceLevel[reader.ReadInt()];

			for (var i = 0; i < priceLevels.Length; i++)
			{
				var prevPrice = metaInfo.FirstPrice;

				var priceLevel = new CandlePriceLevel
				{
					Price = metaInfo.Version < MarketDataVersions.Version56
							? reader.ReadPrice(ref prevPrice, metaInfo)
							: reader.ReadPriceEx(metaInfo, false, largeDecimal),
					BuyCount = reader.ReadInt(),
					SellCount = reader.ReadInt(),
					BuyVolume = reader.ReadVolume(metaInfo, largeDecimal),
					SellVolume = reader.ReadVolume(metaInfo, largeDecimal)
				};

				if (metaInfo.Version >= MarketDataVersions.Version55)
					priceLevel.TotalVolume = reader.ReadVolume(metaInfo, largeDecimal);

				if (reader.Read())
				{
					var volumes = new decimal[reader.ReadInt()];

					for (var j = 0; j < volumes.Length; j++)
						volumes[j] = reader.ReadVolume(metaInfo, largeDecimal);

					priceLevel.BuyVolumes = volumes;
				}

				if (reader.Read())
				{
					var volumes = new decimal[reader.ReadInt()];

					for (var j = 0; j < volumes.Length; j++)
						volumes[j] = reader.ReadVolume(metaInfo, largeDecimal);

					priceLevel.SellVolumes = volumes;
				}

				priceLevels[i] = priceLevel;
			}

			candle.PriceLevels = priceLevels;
		}

		if (!buildFrom)
			return candle;

		candle.BuildFrom = reader.ReadBuildFrom();

		if (!seqNum)
			return candle;

		reader.ReadSeqNum(candle, metaInfo);

		return candle;
	}
}

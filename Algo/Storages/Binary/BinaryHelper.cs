namespace StockSharp.Algo.Storages.Binary;

static class BinaryHelper
{
	public static decimal WriteDecimal(this BitArrayWriter writer, decimal value, decimal prevValue)
	{
		var diff = value - prevValue;

		if (value != prevValue + diff)
			throw new ArgumentOutOfRangeException(nameof(value), LocalizedStrings.TooLowDiff.Put(value, prevValue));

		writer.WriteDecimal(diff);

		return value;
	}

	public static decimal ReadDecimal(this BitArrayReader reader, decimal prevPrice)
	{
		var diff = reader.ReadDecimal();

		return prevPrice + diff;
	}

	public static void WritePrice(this BitArrayWriter writer, decimal price, ref decimal prevPrice, BinaryMetaInfo info, SecurityId securityId, bool useLong = false, bool nonAdjustPrice = false)
	{
		var priceStep = info.LastPriceStep;

		if (priceStep == 0)
			throw new InvalidOperationException(LocalizedStrings.PriceStepNotSpecified);

		if ((price % priceStep) != 0)
		{
			if (!nonAdjustPrice)
				throw new ArgumentException(LocalizedStrings.MinPriceStepNotCorrecpondPrice.Put(priceStep, securityId, price), nameof(info));

			writer.Write(false);

			var priceStepChanged = false;

			if ((price % info.LastPriceStep) != 0)
			{
				var newPriceStep = 1m;

				var found = false;

				for (var i = 0; i < 20; i++)
				{
					if ((price % newPriceStep) == 0)
					{
						found = true;
						break;
					}

					newPriceStep /= 10;
				}

				if (!found)
					throw new ArgumentException(LocalizedStrings.MinPriceStepNotCorrecpondPrice.Put(priceStep, securityId, price), nameof(info));

				info.LastPriceStep = newPriceStep;

				//if (info.FirstPriceStep == 0)
				//	info.FirstPriceStep = info.LastPriceStep;

				priceStepChanged = true;
			}

			writer.Write(priceStepChanged);

			if (priceStepChanged)
				WriteDecimal(writer, info.LastPriceStep, 0);

			if (!info.IsFirstFractionalPriceSet)
				info.FirstFractionalPrice = info.LastFractionalPrice = price;

			var stepCount = (long)((price - info.LastFractionalPrice) / info.LastPriceStep);

			if (useLong)
				writer.WriteLong(stepCount);
			else
				writer.WriteInt((int)stepCount);

			info.LastFractionalPrice = price;
			return;
		}

		if (nonAdjustPrice)
			writer.Write(true);

		try
		{
			var stepCount = (long)((price - prevPrice) / priceStep);

			// ОЛ может содержать заявки с произвольно большими ценами
			if (useLong)
				writer.WriteLong(stepCount);
			else
			{
				if (stepCount.Abs() > int.MaxValue)
					throw new InvalidOperationException("Range is overflow.");

				writer.WriteInt((int)stepCount);
			}

			prevPrice = price;
		}
		catch (OverflowException ex)
		{
			throw new ArgumentException(LocalizedStrings.CannotConvertToInt.Put(price, prevPrice, priceStep, useLong), ex);
		}
	}

	private const decimal _largeDecLimit = 792281625142643375935439503.35m;

	private static bool TryWriteLargeDecimal(this BitArrayWriter writer, bool largeDecimal, decimal value)
	{
		if (largeDecimal)
		{
			if (value.Abs() > _largeDecLimit)
			{
				writer.Write(true);
				writer.WriteDecimal(value);
				return true;
			}
			else
			{
				writer.Write(false);
			}
		}

		return false;
	}

	private static bool TryReadLargeDecimal(this BitArrayReader reader, bool largeDecimal, out decimal value)
	{
		if (largeDecimal)
		{
			if (reader.Read())
			{
				value = reader.ReadDecimal();
				return true;
			}
		}

		value = default;
		return false;
	}

	public static void WritePriceEx(this BitArrayWriter writer, decimal price, BinaryMetaInfo info, SecurityId securityId, bool useLong, bool largeDecimal)
	{
		if (info.Version < MarketDataVersions.Version41)
		{
			var prevPrice = info.LastPrice;
			writer.WritePrice(price, ref prevPrice, info, securityId);
			info.LastPrice = price;
		}
		else
		{
			if (writer.TryWriteLargeDecimal(largeDecimal, price))
				return;

			var isAligned = (price % info.LastPriceStep) == 0;
			writer.Write(isAligned);

			if (isAligned)
			{
				if (!info.IsFirstPriceSet)
					info.FirstPrice = info.LastPrice = price;

				var prevPrice = info.LastPrice;
				writer.WritePrice(price, ref prevPrice, info, securityId, useLong);
				info.LastPrice = price;
			}
			else
			{
				if (!info.IsFirstFractionalPriceSet)
					info.FirstFractionalPrice = info.LastFractionalPrice = price;

				info.LastFractionalPrice = writer.WriteDecimal(price, info.LastFractionalPrice);
			}
		}
	}

	private static decimal ReadPrice(this BitArrayReader reader, decimal prevPrice, decimal priceStep, bool useLong)
	{
		var count = useLong ? reader.ReadLong() : reader.ReadInt();
		return prevPrice + count * priceStep;
	}

	public static decimal ReadPrice(this BitArrayReader reader, ref decimal prevPrice, BinaryMetaInfo info, bool useLong = false, bool nonAdjustPrice = false)
	{
		if (!nonAdjustPrice || reader.Read())
		{
			return prevPrice = ReadPrice(reader, prevPrice, info.PriceStep, useLong);
		}
		else
		{
			if (reader.Read())
				info.PriceStep = ReadDecimal(reader, 0);

			return info.FirstFractionalPrice = ReadPrice(reader, info.FirstFractionalPrice, info.PriceStep, useLong);
		}
	}

	public static decimal ReadPriceEx(this BitArrayReader reader, BinaryMetaInfo info, bool useLong, bool largeDecimal)
	{
		if (info.Version < MarketDataVersions.Version41)
		{
			var prevPrice = info.FirstPrice;
			return info.FirstPrice = reader.ReadPrice(ref prevPrice, info);
		}
		else
		{
			if (reader.TryReadLargeDecimal(largeDecimal, out var price))
				return price;

			if (reader.Read())
			{
				var prevPrice = info.FirstPrice;
				return info.FirstPrice = reader.ReadPrice(ref prevPrice, info, useLong);
			}
			else
			{
				return info.FirstFractionalPrice = reader.ReadDecimal(info.FirstFractionalPrice);
			}
		}
	}

	public static long SerializeId(this BitArrayWriter writer, long id, long prevId)
	{
		writer.WriteLong(id - prevId);
		return id;
	}

	public static DateTime WriteTime(this BitArrayWriter writer, DateTimeOffset dto, DateTime prevTime, string name, bool allowNonOrdered, bool isUtc, TimeSpan offset, bool allowDiffOffsets, bool isTickPrecision, ref TimeSpan prevOffset, bool bigRange = false)
	{
		if (writer == null)
			throw new ArgumentNullException(nameof(writer));

		if (allowDiffOffsets)
		{
			writer.Write(dto.Offset == prevOffset);

			if (prevOffset != dto.Offset)
			{
				prevOffset = dto.Offset;

				writer.WriteInt(prevOffset.Hours);

				writer.Write(prevOffset.Minutes == 0);

				if (prevOffset.Minutes != 0)
					writer.WriteInt(prevOffset.Minutes);
			}
		}
		else if (isUtc && dto.Offset != offset)
			throw new ArgumentException(LocalizedStrings.WrongTimeOffset.Put(dto, offset));

		if (!isTickPrecision)
			dto = dto.StorageBinaryOldTruncate();

		var time = isUtc ? dto.UtcDateTime : dto.LocalDateTime;

		var timeDiff = time - prevTime;

		if (allowNonOrdered)
		{
			if (timeDiff < TimeSpan.Zero)
			{
				writer.Write(false);
				timeDiff = new TimeSpan(-timeDiff.Ticks);
			}
			else
				writer.Write(true);

			if (timeDiff >= TimeSpan.FromMinutes(1))
			{
				writer.Write(true);

				if (timeDiff <= TimeSpan.FromHours(32))
				{
					writer.Write(true);
					writer.WriteBits(timeDiff.Days * 24 + timeDiff.Hours, 5);
				}
				else
				{
					writer.Write(false);
					writer.WriteInt(timeDiff.Hours);

					if (timeDiff.Days > 0)
					{
						if (!bigRange)
							throw new ArgumentOutOfRangeException(nameof(dto), LocalizedStrings.BigRangeError.Put(prevTime, dto));

						writer.Write(true);
						writer.WriteInt(timeDiff.Days);
					}
					else
						writer.Write(false);
				}

				writer.WriteBits(timeDiff.Minutes, 6);
				writer.WriteBits(timeDiff.Seconds, 6);
			}
			else
			{
				writer.Write(false);

				writer.WriteInt(timeDiff.Seconds);
			}
		}
		else
		{
			if (timeDiff < TimeSpan.Zero)
				throw new ArgumentException(LocalizedStrings.UnsortedData.Put(name, prevTime, time), nameof(dto));

			if (timeDiff >= TimeSpan.FromMinutes(1))
			{
				writer.Write(true);

				timeDiff = time.TimeOfDay;

				writer.WriteBits(timeDiff.Hours, 5);
				writer.WriteBits(timeDiff.Minutes, 6);
				writer.WriteBits(timeDiff.Seconds, 6);
			}
			else
			{
				writer.Write(false);

				writer.WriteInt(timeDiff.Seconds);
			}
		}
		
		writer.WriteInt(timeDiff.Milliseconds);

		if (isTickPrecision)
		{
			writer.WriteInt((int)(timeDiff.Ticks % 10000));
		}

		return time;
	}

	public static DateTimeOffset ReadTime(this BitArrayReader reader, ref DateTime prevTime, bool allowNonOrdered, bool isUtc, TimeSpan offset, bool allowDiffOffsets, bool isTickPrecision, ref TimeSpan prevOffset)
	{
		if (allowDiffOffsets)
		{
			if (!reader.Read())
			{
				prevOffset = new TimeSpan(reader.ReadInt(), reader.Read() ? 0 : reader.ReadInt(), 0);
			}

			offset = prevOffset;
		}

		long time;

		if (allowNonOrdered)
		{
			time = 0;

			var sign = reader.Read() ? 1 : -1;

			if (reader.Read())
			{
				if (reader.Read())
					time += reader.Read(5) * TimeSpan.TicksPerHour;
				else
				{
					time += reader.ReadInt() * TimeSpan.TicksPerHour;

					if (reader.Read())
						time += reader.ReadInt() * TimeSpan.TicksPerDay;
				}

				time += reader.Read(6) * TimeSpan.TicksPerMinute;
				time += reader.Read(6) * TimeSpan.TicksPerSecond;
			}
			else
			{
				time += reader.ReadInt() * TimeSpan.TicksPerSecond;
			}

			time += reader.ReadInt() * TimeSpan.TicksPerMillisecond;

			if (isTickPrecision)
				time += reader.ReadInt();

			time = prevTime.Ticks + sign * time;
		}
		else
		{
			time = prevTime.Ticks;

			if (reader.Read())
			{
				time -= time % TimeSpan.TicksPerDay;

				time += reader.Read(5) * TimeSpan.TicksPerHour;
				time += reader.Read(6) * TimeSpan.TicksPerMinute;
				time += reader.Read(6) * TimeSpan.TicksPerSecond;
			}
			else
			{
				time += reader.ReadInt() * TimeSpan.TicksPerSecond;
			}

			time += reader.ReadInt() * TimeSpan.TicksPerMillisecond;
		}

		prevTime = new DateTime(time, isUtc ? DateTimeKind.Utc : DateTimeKind.Unspecified);

		return (isUtc ? new DateTime(time + offset.Ticks) : prevTime).ApplyTimeZone(offset);
	}

	public static void WriteVolume(this BitArrayWriter writer, decimal volume, BinaryMetaInfo info, bool largeDecimal)
	{
		if (info.Version < MarketDataVersions.Version44)
		{
			var intVolume = volume.Truncate();

			if (intVolume == volume) // объем целочисленный
			{
				writer.Write(true);
				writer.WriteLong((long)intVolume);
			}
			else
			{
				writer.Write(false);
				throw new NotSupportedException(LocalizedStrings.FractionalVolumeUnsupported.Put(volume));
			}
		}
		else
		{
			if (writer.TryWriteLargeDecimal(largeDecimal, volume))
				return;

			var isAligned = (volume % info.VolumeStep) == 0;
			writer.Write(isAligned);

			if (isAligned)
			{
				writer.WriteLong((long)(volume / info.VolumeStep));
			}
			else
			{
				if (!info.IsFirstFractionalVolumeSet)
					info.FirstFractionalVolume = info.LastFractionalVolume = volume;

				info.LastFractionalVolume = writer.WriteDecimal(volume, info.LastFractionalVolume);
			}
		}
	}

	public static decimal ReadVolume(this BitArrayReader reader, BinaryMetaInfo info, bool largeDecimal)
	{
		if (info.Version < MarketDataVersions.Version44)
		{
			if (reader.Read())
				return reader.ReadLong();
			else
				throw new NotSupportedException(LocalizedStrings.FractionalVolumeUnsupported.Put("read"));
		}
		else
		{
			if (reader.TryReadLargeDecimal(largeDecimal, out var volume))
				return volume;

			if (reader.Read())
				return reader.ReadLong() * info.VolumeStep;
			else
				return info.FirstFractionalVolume = reader.ReadDecimal(info.FirstFractionalVolume);
		}
	}

	public static void WriteSide(this BitArrayWriter writer, Sides? direction)
	{
		if (direction == null)
			writer.Write(false);
		else
		{
			writer.Write(true);
			writer.Write(direction == Sides.Buy);
		}
	}

	public static Sides? ReadSide(this BitArrayReader reader)
	{
		return reader.Read() ? (reader.Read() ? Sides.Buy : Sides.Sell) : null;
	}

	public static void WriteStringEx(this BitArrayWriter writer, string value)
	{
		if (value.IsEmpty())
			writer.Write(false);
		else
		{
			writer.Write(true);
			writer.WriteString(value);
		}
	}

	public static void WriteString(this BitArrayWriter writer, string value)
	{
		var bits = value.UTF8().To<BitArray>().To<bool[]>();
		writer.WriteInt(bits.Length);
		bits.ForEach(writer.Write);
	}

	public static string ReadStringEx(this BitArrayReader reader)
	{
		return reader.Read() ? reader.ReadString() : null;
	}

	public static string ReadString(this BitArrayReader reader)
	{
		var len = reader.ReadInt();
		return reader.ReadArray(len).To<BitArray>().To<byte[]>().UTF8();
	}

	public static TimeSpan GetTimeZone(this BinaryMetaInfo metaInfo, bool isUtc, SecurityId securityId, IExchangeInfoProvider exchangeInfoProvider)
	{
		if (isUtc)
			return metaInfo.ServerOffset;

		var board = exchangeInfoProvider.TryGetExchangeBoard(securityId.BoardCode);

		return board == null ? metaInfo.LocalOffset : board.TimeZone.BaseUtcOffset;
	}

	public static void WriteNullableInt(this BitArrayWriter writer, int? value)
	{
		if (value == null)
			writer.Write(false);
		else
		{
			writer.Write(true);
			writer.WriteInt(value.Value);
		}
	}

	public static int? ReadNullableInt(this BitArrayReader reader)
	{
		if (!reader.Read())
			return null;

		return reader.ReadInt();
	}

	public static void WriteNullableLong(this BitArrayWriter writer, long? value)
	{
		if (value == null)
			writer.Write(false);
		else
		{
			writer.Write(true);
			writer.WriteLong(value.Value);
		}
	}

	public static long? ReadNullableLong(this BitArrayReader reader)
	{
		if (!reader.Read())
			return null;

		return reader.ReadLong();
	}

	public static bool HasLocalTime(this Message msg, DateTimeOffset serverTime)
	{
		return msg.LocalTime != default && msg.LocalTime != serverTime/* && (msg.LocalTime - serverTime).TotalHours.Abs() < 1*/;
	}

	public static void WriteDto(this BitArrayWriter writer, DateTimeOffset? dto)
	{
		if (dto != null)
		{
			writer.Write(true);
			writer.WriteLong(dto.Value.Ticks);
			writer.WriteInt(dto.Value.Offset.Hours);
			writer.WriteInt(dto.Value.Offset.Minutes);
		}
		else
			writer.Write(false);
	}

	public static DateTimeOffset? ReadDto(this BitArrayReader reader)
	{
		return reader.Read() ? reader.ReadLong().To<DateTime>().ApplyTimeZone(new TimeSpan(reader.ReadInt(), reader.ReadInt(), 0)) : null;
	}

	public static void WriteBuildFrom(this BitArrayWriter writer, DataType buildFrom)
	{
		writer.Write(buildFrom != null);

		if (buildFrom == null)
			return;

		if (buildFrom == DataType.Level1)
			writer.WriteInt(0);
		else if (buildFrom == DataType.MarketDepth)
			writer.WriteInt(1);
		else if (buildFrom == DataType.OrderLog)
			writer.WriteInt(2);
		else if (buildFrom == DataType.Ticks)
			writer.WriteInt(3);
		else if (buildFrom == DataType.Transactions)
			writer.WriteInt(4);
		else
		{
			writer.WriteInt(5);

			var (messageType, arg1, arg2, arg3) = buildFrom.Extract();

			writer.WriteInt(messageType);
			writer.WriteLong(arg1);

			if (arg2 == 0)
				writer.Write(false);
			else
			{
				writer.Write(true);
				writer.WriteDecimal(arg2, 0);
			}

			writer.WriteInt(arg3);
		}
	}

	public static DataType ReadBuildFrom(this BitArrayReader reader)
	{
		if (!reader.Read())
			return null;

		switch (reader.ReadInt())
		{
			case 0:
				return DataType.Level1;
			case 1:
				return DataType.MarketDepth;
			case 2:
				return DataType.OrderLog;
			case 3:
				return DataType.Ticks;
			case 4:
				return DataType.Transactions;
			case 5:
				return reader.ReadInt().ToDataType(reader.ReadLong(), reader.Read() ? reader.ReadDecimal(0) : 0M, reader.ReadInt());
			default:
				throw new InvalidOperationException();
		}
	}

	public static void WriteSeqNum<TMessage>(this BitArrayWriter writer, TMessage message, BinaryMetaInfo metaInfo)
		where TMessage : ISeqNumMessage
	{
		metaInfo.PrevSeqNum = writer.SerializeId(message.SeqNum, metaInfo.PrevSeqNum);
	}

	public static void ReadSeqNum<TMessage>(this BitArrayReader reader, TMessage message, BinaryMetaInfo metaInfo)
		where TMessage : ISeqNumMessage
	{
		metaInfo.FirstSeqNum += reader.ReadLong();
		message.SeqNum = metaInfo.FirstSeqNum;
	}

	public static void WriteNullableBool(this BitArrayWriter writer, bool? value)
	{
		writer.Write(value != null);

		if (value is null)
			return;

		writer.Write(value.Value);
	}

	public static bool? ReadNullableBool(this BitArrayReader reader)
		=> reader.Read() ? reader.Read() : null;

	public static void WriteNullableSide(this BitArrayWriter writer, Sides? value)
	{
		writer.Write(value != null);

		if (value is null)
			return;

		writer.Write(value.Value == Sides.Buy);
	}

	public static Sides? ReadNullableSide(this BitArrayReader reader)
		=> reader.Read() ? reader.Read() ? Sides.Buy : Sides.Sell : null;

	public static void WriteNullableDecimal(this BitArrayWriter writer, decimal? value)
	{
		writer.Write(value != null);

		if (value is null)
			return;

		writer.WriteDecimal(value.Value);
	}

	public static decimal? ReadNullableDecimal(this BitArrayReader reader)
		=> reader.Read() ? reader.ReadDecimal() : null;
}
namespace StockSharp.Algo.Storages
{
	using System;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	using StockSharp.Localization;

	static class IMessageSerializerHelper
	{
		public static decimal WriteDecimal(this BitArrayWriter writer, decimal value, decimal prevValue)
		{
			var diff = value - prevValue;

			if (value != prevValue + diff)
				throw new ArgumentOutOfRangeException("value", LocalizedStrings.Str1006Params.Put(value, prevValue));

			writer.Write(diff >= 0);

			if (diff < 0)
				diff = -diff;

			var decBits = decimal.GetBits(diff);

			writer.WriteInt(decBits[0]);
			writer.WriteInt(decBits[1]);
			writer.WriteInt(decBits[2]);
			writer.WriteInt((decBits[3] >> 16) & 0xff);

			return value;
		}

		public static decimal ReadDecimal(this BitArrayReader reader, decimal prevPrice)
		{
			var isPos = reader.Read();

			var diff = new decimal(new[] { reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt() << 16 });

			if (!isPos)
				diff = -diff;

			return prevPrice + diff;
		}

		public static void WritePrice<T>(this BitArrayWriter writer, decimal price, decimal prevPrice, MetaInfo<T> info, SecurityId securityId, bool useLong = false)
			where T : MetaInfo<T>
		{
			if ((price % info.PriceStep) != 0)
				throw new ArgumentException(LocalizedStrings.Str1007Params.Put(info.PriceStep, securityId, price), "info");

			try
			{
				var stepCount = (long)((price - prevPrice) / info.PriceStep);

				// ОЛ может содержать заявки с произвольно большими ценами
				if (useLong)
					writer.WriteLong(stepCount);
				else
					writer.WriteInt((int)stepCount);
			}
			catch (OverflowException ex)
			{
				throw new ArgumentException(LocalizedStrings.Str1008Params.Put(price, prevPrice, info.PriceStep, useLong), ex);
			}
		}

		public static void WritePriceEx<T>(this BitArrayWriter writer, decimal price, BinaryMetaInfo<T> info, SecurityId securityId)
			where T : BinaryMetaInfo<T>
		{
			if (info.Version < MarketDataVersions.Version41)
			{
				writer.WritePrice(price, info.LastPrice, info, securityId);
				info.LastPrice = price;
			}
			else
			{
				var isAligned = (price % info.PriceStep) == 0;
				writer.Write(isAligned);

				if (isAligned)
				{
					if (info.FirstPrice == 0)
						info.FirstPrice = info.LastPrice = price;

					writer.WritePrice(price, info.LastPrice, info, securityId);
					info.LastPrice = price;
				}
				else
				{
					if (info.FirstNonSystemPrice == 0)
						info.FirstNonSystemPrice = info.LastNonSystemPrice = price;

					info.LastNonSystemPrice = writer.WriteDecimal(price, info.LastNonSystemPrice);
				}
			}
		}

		public static decimal ReadPrice<T>(this BitArrayReader reader, decimal prevPrice, MetaInfo<T> info, bool useLong = false)
			where T : MetaInfo<T>
		{
			var count = useLong ? reader.ReadLong() : reader.ReadInt();
			return prevPrice + count * info.PriceStep;
		}

		public static decimal ReadPriceEx<T>(this BitArrayReader reader, BinaryMetaInfo<T> info)
			where T : BinaryMetaInfo<T>
		{
			if (info.Version < MarketDataVersions.Version41)
			{
				return info.FirstPrice = reader.ReadPrice(info.FirstPrice, info);
			}
			else
			{
				if (reader.Read())
				{
					return info.FirstPrice = reader.ReadPrice(info.FirstPrice, info);
				}
				else
				{
					return info.FirstNonSystemPrice = reader.ReadDecimal(info.FirstNonSystemPrice);
				}
			}
		}

		public static long SerializeId(this BitArrayWriter writer, long id, long prevId)
		{
			writer.WriteLong(id - prevId);
			return id;
		}

		public static DateTimeOffset Truncate(this DateTimeOffset time)
		{
			return time.Truncate(TimeSpan.TicksPerMillisecond);
		}

		public static DateTime WriteTime(this BitArrayWriter writer, DateTimeOffset dto, DateTime prevTime, string name, bool allowNonOrdered, bool isUtc, TimeSpan offset)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			if (isUtc && dto.Offset != offset)
				throw new ArgumentException("Время {0} имеет неправильное смещение. Ожидается {1}.".Put(dto, offset));

			dto = dto.Truncate();

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
						writer.WriteBits(timeDiff.Hours, 5);
					}
					else
					{
						writer.Write(false);
						writer.WriteInt(timeDiff.Hours);
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
					throw new ArgumentException(LocalizedStrings.Str1009Params.Put(name, prevTime, time), "dto");

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

			return time;
		}

		public static DateTimeOffset ReadTime(this BitArrayReader reader, ref DateTime prevTime, bool allowNonOrdered, bool isUtc, TimeSpan timeZone)
		{
			long time;

			if (allowNonOrdered)
			{
				time = 0;

				var sign = reader.Read() ? 1 : -1;

				if (reader.Read())
				{
					time += (reader.Read() ? reader.Read(5) : reader.ReadInt()) * TimeSpan.TicksPerHour;
					time += reader.Read(6) * TimeSpan.TicksPerMinute;
					time += reader.Read(6) * TimeSpan.TicksPerSecond;
				}
				else
				{
					time += reader.ReadInt() * TimeSpan.TicksPerSecond;
				}

				time += reader.ReadInt() * TimeSpan.TicksPerMillisecond;

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

			return isUtc
				? new DateTime(time + timeZone.Ticks).ApplyTimeZone(timeZone)
				: prevTime.ApplyTimeZone(timeZone);
		}

		public static void WriteVolume<T>(this BitArrayWriter writer, decimal volume, BinaryMetaInfo<T> info, SecurityId securityId)
			where T : BinaryMetaInfo<T>
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
					throw new NotSupportedException(LocalizedStrings.Str1010Params.Put(volume));
				}
			}
			else
			{
				var isAligned = (volume % info.VolumeStep) == 0;
				writer.Write(isAligned);

				if (isAligned)
				{
					writer.WriteLong((long)(volume / info.VolumeStep));
				}
				else
				{
					if (info.FirstFractionalVolume == 0)
						info.FirstFractionalVolume = info.LastFractionalVolume = volume;

					info.LastFractionalVolume = writer.WriteDecimal(volume, info.LastFractionalVolume);
				}
			}
		}

		public static decimal ReadVolume<T>(this BitArrayReader reader, BinaryMetaInfo<T> info)
			where T : BinaryMetaInfo<T>
		{
			if (info.Version < MarketDataVersions.Version44)
			{
				if (reader.Read())
					return reader.ReadLong();
				else
					throw new NotSupportedException(LocalizedStrings.Str1011);
			}
			else
			{
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
			return reader.Read() ? (reader.Read() ? Sides.Buy : Sides.Sell) : (Sides?)null;
		}

		public static void WriteString(this BitArrayWriter writer, string value)
		{
			throw new NotImplementedException();
		}

		public static string ReadString(this BitArrayReader reader)
		{
			throw new NotImplementedException();
		}

		public static TimeSpan GetTimeZone<TMetaInfo>(this BinaryMetaInfo<TMetaInfo> metaInfo, bool isUtc, SecurityId securityId)
			where TMetaInfo : BinaryMetaInfo<TMetaInfo>
		{
			if (isUtc)
				return metaInfo.ServerOffset;

			var board = ExchangeBoard.GetBoard(securityId.BoardCode);

			return board == null ? metaInfo.LocalOffset : board.Exchange.TimeZoneInfo.BaseUtcOffset;
		}
	}
}
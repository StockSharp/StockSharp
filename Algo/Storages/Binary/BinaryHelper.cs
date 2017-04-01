#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Binary.Algo
File: BinaryHelper.cs
Created: 2015, 12, 14, 1:43 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages.Binary
{
	using System;
	using System.Collections;
	using System.Text;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Messages;
	using StockSharp.Localization;

	static class BinaryHelper
	{
		public static decimal WriteDecimal(this BitArrayWriter writer, decimal value, decimal prevValue)
		{
			var diff = value - prevValue;

			if (value != prevValue + diff)
				throw new ArgumentOutOfRangeException(nameof(value), LocalizedStrings.Str1006Params.Put(value, prevValue));

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

		public static void WritePrice(this BitArrayWriter writer, decimal price, decimal prevPrice, MetaInfo info, SecurityId securityId, bool useLong = false)
		{
			var priceStep = info.PriceStep;

			if (priceStep == 0)
				throw new InvalidOperationException(LocalizedStrings.Str2925);

			if ((price % priceStep) != 0)
				throw new ArgumentException(LocalizedStrings.Str1007Params.Put(priceStep, securityId, price), nameof(info));

			try
			{
				var stepCount = (long)((price - prevPrice) / priceStep);

				// ОЛ может содержать заявки с произвольно большими ценами
				if (useLong)
					writer.WriteLong(stepCount);
				else
					writer.WriteInt((int)stepCount);
			}
			catch (OverflowException ex)
			{
				throw new ArgumentException(LocalizedStrings.Str1008Params.Put(price, prevPrice, priceStep, useLong), ex);
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
					if (info.FirstFractionalPrice == 0)
						info.FirstFractionalPrice = info.LastFractionalPrice = price;

					info.LastFractionalPrice = writer.WriteDecimal(price, info.LastFractionalPrice);
				}
			}
		}

		public static decimal ReadPrice(this BitArrayReader reader, decimal prevPrice, MetaInfo info, bool useLong = false)
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
					return info.FirstFractionalPrice = reader.ReadDecimal(info.FirstFractionalPrice);
				}
			}
		}

		public static long SerializeId(this BitArrayWriter writer, long id, long prevId)
		{
			writer.WriteLong(id - prevId);
			return id;
		}

		public static DateTime WriteTime(this BitArrayWriter writer, DateTimeOffset dto, DateTime prevTime, string name, bool allowNonOrdered, bool isUtc, TimeSpan offset, bool allowDiffOffsets, ref TimeSpan prevOffset, bool bigRange = false)
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

						if (timeDiff.Days > 0)
						{
							if (!bigRange)
								throw new ArgumentOutOfRangeException(LocalizedStrings.BigRangeError.Put(prevTime, dto));

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
					throw new ArgumentException(LocalizedStrings.Str1009Params.Put(name, prevTime, time), nameof(dto));

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

		public static DateTimeOffset ReadTime(this BitArrayReader reader, ref DateTime prevTime, bool allowNonOrdered, bool isUtc, TimeSpan offset, bool allowDiffOffsets, ref TimeSpan prevOffset, bool bigRange = false)
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
			var bits = Encoding.UTF8.GetBytes(value).To<BitArray>().To<bool[]>();
			writer.WriteInt(bits.Length);
			bits.ForEach(writer.Write);
		}

		public static string ReadString(this BitArrayReader reader)
		{
			var len = reader.ReadInt();
			return Encoding.UTF8.GetString(reader.ReadArray(len).To<BitArray>().To<byte[]>());
		}

		public static TimeSpan GetTimeZone<TMetaInfo>(this BinaryMetaInfo<TMetaInfo> metaInfo, bool isUtc, SecurityId securityId, IExchangeInfoProvider exchangeInfoProvider)
			where TMetaInfo : BinaryMetaInfo<TMetaInfo>
		{
			if (isUtc)
				return metaInfo.ServerOffset;

			var board = exchangeInfoProvider.GetExchangeBoard(securityId.BoardCode);

			return board == null ? metaInfo.LocalOffset : board.TimeZone.BaseUtcOffset;
		}

		public static void WriteNullableInt<T>(this BitArrayWriter writer, T? value)
			where T : struct
		{
			if (value == null)
				writer.Write(false);
			else
			{
				writer.Write(true);
				writer.WriteInt(value.To<int>());
			}
		}

		public static T? ReadNullableInt<T>(this BitArrayReader reader)
			where T : struct
		{
			if (!reader.Read())
				return null;

			return reader.ReadInt().To<T?>();
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
	}
}
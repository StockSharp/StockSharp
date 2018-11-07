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

		public static void WritePrice(this BitArrayWriter writer, decimal price, ref decimal prevPrice, BinaryMetaInfo info, SecurityId securityId, bool useLong = false, bool nonAdjustPrice = false)
		{
			var priceStep = info.LastPriceStep;

			if (priceStep == 0)
				throw new InvalidOperationException(LocalizedStrings.Str2925);

			if ((price % priceStep) != 0)
			{
				if (!nonAdjustPrice)
					throw new ArgumentException(LocalizedStrings.Str1007Params.Put(priceStep, securityId, price), nameof(info));

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
						throw new ArgumentException(LocalizedStrings.Str1007Params.Put(priceStep, securityId, price), nameof(info));

					info.LastPriceStep = newPriceStep;

					//if (info.FirstPriceStep == 0)
					//	info.FirstPriceStep = info.LastPriceStep;

					priceStepChanged = true;
				}

				writer.Write(priceStepChanged);

				if (priceStepChanged)
					WriteDecimal(writer, info.LastPriceStep, 0);

				if (info.FirstFractionalPrice == 0)
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
				throw new ArgumentException(LocalizedStrings.Str1008Params.Put(price, prevPrice, priceStep, useLong), ex);
			}
		}

		public static void WritePriceEx(this BitArrayWriter writer, decimal price, BinaryMetaInfo info, SecurityId securityId, bool useLong = false)
		{
			if (info.Version < MarketDataVersions.Version41)
			{
				var prevPrice = info.LastPrice;
				writer.WritePrice(price, ref prevPrice, info, securityId);
				info.LastPrice = price;
			}
			else
			{
				var isAligned = (price % info.LastPriceStep) == 0;
				writer.Write(isAligned);

				if (isAligned)
				{
					if (info.FirstPrice == 0)
						info.FirstPrice = info.LastPrice = price;

					var prevPrice = info.LastPrice;
					writer.WritePrice(price, ref prevPrice, info, securityId, useLong);
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

		public static decimal ReadPriceEx(this BitArrayReader reader, BinaryMetaInfo info, bool useLong = false)
		{
			if (info.Version < MarketDataVersions.Version41)
			{
				var prevPrice = info.FirstPrice;
				return info.FirstPrice = reader.ReadPrice(ref prevPrice, info);
			}
			else
			{
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
						writer.WriteBits(timeDiff.Hours, 5);
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

		public static void WriteVolume(this BitArrayWriter writer, decimal volume, BinaryMetaInfo info, SecurityId securityId)
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

		public static decimal ReadVolume(this BitArrayReader reader, BinaryMetaInfo info)
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
			var bits = Encoding.UTF8.GetBytes(value).To<BitArray>().To<bool[]>();
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
			return Encoding.UTF8.GetString(reader.ReadArray(len).To<BitArray>().To<byte[]>());
		}

		public static TimeSpan GetTimeZone(this BinaryMetaInfo metaInfo, bool isUtc, SecurityId securityId, IExchangeInfoProvider exchangeInfoProvider)
		{
			if (isUtc)
				return metaInfo.ServerOffset;

			var board = exchangeInfoProvider.GetExchangeBoard(securityId.BoardCode);

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
			return !msg.LocalTime.IsDefault() && msg.LocalTime != serverTime/* && (msg.LocalTime - serverTime).TotalHours.Abs() < 1*/;
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
			return reader.Read() ? reader.ReadLong().To<DateTime>().ApplyTimeZone(new TimeSpan(reader.ReadInt(), reader.ReadInt(), 0)) : (DateTimeOffset?)null;
		}
	}
}
namespace StockSharp.Algo.Storages.Csv
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// CSV helper class.
	/// </summary>
	static class CsvHelper
	{
		/// <summary>
		/// <see cref="DateTime"/> format.
		/// </summary>
		public const string DateFormat = "yyyyMMdd";

		/// <summary>
		/// <see cref="TimeSpan"/> format with milliseconds.
		/// </summary>
		public const string TimeMlsFormat = "hhmmssfff";

		/// <summary>
		/// <see cref="TimeSpan"/> format.
		/// </summary>
		public const string TimeFormat = "hhmmss";

		/// <summary>
		/// <see cref="DateTime"/> parser.
		/// </summary>
		public static readonly FastDateTimeParser DateParser = new(DateFormat);

		/// <summary>
		/// <see cref="TimeSpan"/> parser.
		/// </summary>
		public static readonly FastTimeSpanParser TimeMlsParser = new(TimeMlsFormat);

		/// <summary>
		/// <see cref="TimeSpan"/> parser.
		/// </summary>
		public static readonly FastTimeSpanParser TimeParser = new(TimeFormat);

		/// <summary>
		/// Read <see cref="DateTimeOffset"/>.
		/// </summary>
		/// <param name="reader">CSV reader.</param>
		/// <param name="date">Date.</param>
		/// <returns><see cref="DateTimeOffset"/>.</returns>
		public static DateTimeOffset ReadTime(this FastCsvReader reader, DateTime date)
		{
			if (reader == null)
				throw new ArgumentNullException(nameof(reader));

			return (date + reader.ReadString().ToTimeMls()).ToDateTimeOffset(TimeSpan.Parse(reader.ReadString().Remove("+")));
		}

		public static string WriteTimeMls(this TimeSpan time)
		{
			return time.ToString(TimeMlsFormat);
		}

		public static string WriteTime(this TimeSpan time)
		{
			return time.ToString(TimeFormat);
		}

		public static string WriteTimeMls(this DateTimeOffset time)
		{
			return time.UtcDateTime.TimeOfDay.WriteTimeMls();
		}

		public static string WriteDate(this DateTimeOffset time)
		{
			return time.UtcDateTime.ToString(DateFormat);
		}

		public static TimeSpan ToTimeMls(this string str)
		{
			return TimeMlsParser.Parse(str);
		}

		public static TimeSpan ToTime(this string str)
		{
			return TimeParser.Parse(str);
		}

		public static DateTime ToDateTime(this string str)
		{
			return DateParser.Parse(str);
		}

		private static readonly string[] _emptyDataType = new string[4];

		public static string[] ToCsv(this DataType dataType)
		{
			if (dataType is null)
				return _emptyDataType;

			var (messageType, arg1, arg2, arg3) = dataType.Extract();

			return new[] { messageType.To<string>(), arg1.To<string>(), arg2.To<string>(), arg3.To<string>() };
		}

		public static DataType ReadBuildFrom(this FastCsvReader reader)
		{
			if (reader is null)
				throw new ArgumentNullException(nameof(reader));

			var str = reader.ReadString();

			if (str.IsEmpty())
			{
				reader.Skip(3);
				return null;
			}
			
			return str.To<int>().ToDataType(reader.ReadLong(), reader.ReadDecimal(), reader.ReadInt());
		}

		private const string _dateTimeFormat = "yyyyMMddHHmmss";
		private static readonly FastDateTimeParser _dateTimeParser = new(_dateTimeFormat);

		public static DateTimeOffset? ReadNullableDateTime(this FastCsvReader reader)
		{
			var str = reader.ReadString();

			if (str.IsEmpty())
				return null;

			return _dateTimeParser.Parse(str).UtcKind();
		}

		public static DateTimeOffset ReadDateTime(this FastCsvReader reader)
			=> reader.ReadNullableDateTime().Value;

		public static string WriteNullableDateTime(this DateTimeOffset? dto)
			=> dto?.WriteDateTime();

		public static string WriteDateTime(this DateTimeOffset dto)
			=> dto.UtcDateTime.ToString(_dateTimeFormat);

		public static FastCsvReader CreateCsvReader(this Stream stream, Encoding encoding)
			=> new(stream, encoding, StringHelper.RN);

		public static SecurityMessage ReadSecurity(this FastCsvReader reader)
		{
			var secId = reader.ReadString().ToSecurityId();

			var sec = new SecurityMessage
			{
				SecurityId = secId,
				Name = reader.ReadString(),
			};

			/*sec.Code = */reader.ReadString();
			sec.Class = reader.ReadString();
			sec.ShortName = reader.ReadString();
			/*sec.Board = */reader.ReadString();
			sec.UnderlyingSecurityId = reader.ReadString().ToNullableSecurityId();
			sec.PriceStep = reader.ReadNullableDecimal();
			sec.VolumeStep = reader.ReadNullableDecimal();
			sec.Multiplier = reader.ReadNullableDecimal();
			sec.Decimals = reader.ReadNullableInt();
			sec.SecurityType = reader.ReadNullableEnum<SecurityTypes>();
			sec.ExpiryDate = reader.ReadNullableDateTime();
			sec.SettlementDate = reader.ReadNullableDateTime();
			sec.Strike = reader.ReadNullableDecimal();
			sec.OptionType = reader.ReadNullableEnum<OptionTypes>();
			sec.Currency = reader.ReadNullableEnum<CurrencyTypes>();

			secId.Sedol = reader.ReadString();
			secId.Cusip = reader.ReadString();
			secId.Isin = reader.ReadString();
			secId.Ric = reader.ReadString();
			secId.Bloomberg = reader.ReadString();
			secId.IQFeed = reader.ReadString();
			secId.InteractiveBrokers = reader.ReadNullableInt();
			secId.Plaza = reader.ReadString();

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			{
				sec.UnderlyingSecurityType = reader.ReadNullableEnum<SecurityTypes>();
				sec.BinaryOptionType = reader.ReadString();
				sec.CfiCode = reader.ReadString();
				sec.IssueDate = reader.ReadNullableDateTime();
				sec.IssueSize = reader.ReadNullableDecimal();
			}

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				sec.BasketCode = reader.ReadString();

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				sec.BasketExpression = reader.ReadString();

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			{
				sec.MinVolume = reader.ReadNullableDecimal();
				sec.Shortable = reader.ReadNullableBool();
			}

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				sec.UnderlyingSecurityMinVolume = reader.ReadNullableDecimal();

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				sec.MaxVolume = reader.ReadNullableDecimal();

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				sec.PrimaryId = reader.ReadString().ToNullableSecurityId();

			if ((reader.ColumnCurr + 1) < reader.ColumnCount)
			{
				sec.SettlementType = reader.ReadNullableEnum<SettlementTypes>();
				sec.OptionStyle = reader.ReadNullableEnum<OptionStyles>();
			}

			return sec;
		}

		private static readonly SynchronizedDictionary<Type, ISerializer> _legacyBoardSerializers = new();

		public static BoardMessage ReadBoard(this FastCsvReader reader, Encoding encoding)
		{
			var board = new BoardMessage
			{
				Code = reader.ReadString(),
				ExchangeCode = reader.ReadString(),
				ExpiryTime = reader.ReadString().ToTime(),
				//IsSupportAtomicReRegister = reader.ReadBool(),
				//IsSupportMarketOrders = reader.ReadBool(),
				TimeZone = reader.ReadString().To<TimeZoneInfo>(),
			};

			var time = board.WorkingTime;

			if (reader.ColumnCount == 7)
			{
				ISerializer<TItem> getSerializer<TItem>()
					=> (ISerializer<TItem>)_legacyBoardSerializers.SafeAdd(typeof(TItem), k => new JsonSerializer<TItem> { Indent = false, EnumAsString = true });

				TItem deserialize<TItem>(string value)
					where TItem : class
				{
					if (value.IsEmpty())
						return null;

					var serializer = getSerializer<TItem>();
					var bytes = encoding.GetBytes(value.Replace("'", "\""));

					return serializer.Deserialize(bytes);
				}

				time.Periods = deserialize<List<WorkingTimePeriod>>(reader.ReadString());
				time.SpecialWorkingDays = deserialize<IEnumerable<DateTime>>(reader.ReadString()).ToArray();
				time.SpecialHolidays = deserialize<IEnumerable<DateTime>>(reader.ReadString()).ToArray();
			}
			else
			{
				time.Periods.AddRange(reader.ReadString().DecodeToPeriods());
				time.SpecialDays.AddRange(reader.ReadString().DecodeToSpecialDays());

				if ((reader.ColumnCurr + 1) < reader.ColumnCount)
				{
					reader.Skip();

					time.IsEnabled = reader.ReadBool();
				}
			}

			return board;
		}
	}
}
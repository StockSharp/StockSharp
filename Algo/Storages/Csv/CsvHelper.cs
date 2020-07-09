namespace StockSharp.Algo.Storages.Csv
{
	using System;

	using Ecng.Common;

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
		public static readonly FastDateTimeParser DateParser = new FastDateTimeParser(DateFormat);

		/// <summary>
		/// <see cref="TimeSpan"/> parser.
		/// </summary>
		public static readonly FastTimeSpanParser TimeMlsParser = new FastTimeSpanParser(TimeMlsFormat);

		/// <summary>
		/// <see cref="TimeSpan"/> parser.
		/// </summary>
		public static readonly FastTimeSpanParser TimeParser = new FastTimeSpanParser(TimeFormat);

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
	}
}
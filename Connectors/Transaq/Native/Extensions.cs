namespace StockSharp.Transaq.Native
{
	using System;
	using System.Globalization;
	using System.Xml.Linq;
	
	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	static class Extensions
	{
		private const string _dateFormat = "dd.MM.yyyy";
		private const string _timeFormat = "hh\\:mm\\:ss";
		private const string _timeFormatMls = "hh\\:mm\\:ss\\.fff";
		private const string _dateTimeFormat = _dateFormat + " HH\\:mm\\:ss\\.fff";
		
		public static string ToMyString(this DateTime date)
		{
			return date.FromDateTime(_dateTimeFormat);
		}

		public static string ToMyString(this bool b)
		{
			return b.ToString().ToLower();
		}

		public static DateTime ToDate(this string s, DateTime now)
		{
			if (s == "0")
				return now.Date;

			DateTime date;

			if (DateTime.TryParseExact(s, _dateFormat, null, DateTimeStyles.None, out date))
			{
				return date + now.TimeOfDay;
			}

			TimeSpan time;

			if (TimeSpan.TryParseExact(s, _timeFormatMls, null, out time) || TimeSpan.TryParseExact(s, _timeFormat, null, out time))
			{
				return now.Date + time;
			}

			return s.ToDateTime(_dateTimeFormat);
		}

		public static bool FromYesNo(this string s)
		{
			return s.CompareIgnoreCase("yes") || s.CompareIgnoreCase("y");
		}

		public static string ToYesNo(this bool b)
		{
			return b ? "YES" : "NO";
		}

		public static T? GetElementValueNullable<T>(this XElement elem, string name, Func<DateTime> getNow = null)
			where T : struct
		{
			if (elem == null)
				throw new ArgumentNullException("elem");

			elem = elem.Element(name);

			if (elem == null)
				return null;

			var value = elem.Value;

			if (value.IsEmpty())
				return null;

			if (typeof(T) == typeof(decimal))
				return value.To<decimal>().To<T>();

			if (typeof(T) == typeof(DateTime))
			{
				if (getNow == null)
					throw new ArgumentNullException("getNow");

				return value.ToDate(getNow()).To<T>();
			}

			return value.To<T>();
		}

		public static Unit GetElementValueToUnit(this XElement elem, string name)
		{
			if (elem == null)
				throw new ArgumentNullException("elem");

			elem = elem.Element(name);

			return elem == null ? null : elem.Value.IsEmpty() ? null : elem.Value.ToUnit();
		}
	}
}
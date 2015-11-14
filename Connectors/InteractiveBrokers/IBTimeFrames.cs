namespace StockSharp.InteractiveBrokers
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;
	using StockSharp.Localization;
	
	/// <summary>
	/// Time frames for InteractiveBrokers historical candles.
	/// </summary>
	public sealed class IBTimeFrames
	{
		private static readonly CachedSynchronizedDictionary<TimeSpan, IBTimeFrames> _values = new CachedSynchronizedDictionary<TimeSpan, IBTimeFrames>();

		static IBTimeFrames()
		{
			Second1 = (TimeSpan)new IBTimeFrames("1 secs", TimeSpan.FromSeconds(1));
			Second5 = (TimeSpan)new IBTimeFrames("5 secs", TimeSpan.FromSeconds(5));
			Second15 = (TimeSpan)new IBTimeFrames("15 secs", TimeSpan.FromSeconds(15));
			Second30 = (TimeSpan)new IBTimeFrames("30 secs", TimeSpan.FromSeconds(30));
			Minute1 = (TimeSpan)new IBTimeFrames("1 min", TimeSpan.FromMinutes(1));
			Minute2 = (TimeSpan)new IBTimeFrames("2 mins", TimeSpan.FromMinutes(2));
			Minute3 = (TimeSpan)new IBTimeFrames("3 mins", TimeSpan.FromMinutes(3));
			Minute5 = (TimeSpan)new IBTimeFrames("5 mins", TimeSpan.FromMinutes(5));
			Minute15 = (TimeSpan)new IBTimeFrames("15 mins", TimeSpan.FromMinutes(15));
			Minute30 = (TimeSpan)new IBTimeFrames("30 mins", TimeSpan.FromMinutes(30));
			Hour = (TimeSpan)new IBTimeFrames("1 hour", TimeSpan.FromHours(1));
			Day = (TimeSpan)new IBTimeFrames("1 day", TimeSpan.FromDays(1));
			Week = (TimeSpan)new IBTimeFrames("1 week", TimeSpan.FromDays(7));
			Month = (TimeSpan)new IBTimeFrames("1 month", TimeSpan.FromTicks(TimeHelper.TicksPerMonth));
			Year = (TimeSpan)new IBTimeFrames("1 year", TimeSpan.FromTicks(TimeHelper.TicksPerYear));
		}

		private readonly TimeSpan _value;

		private IBTimeFrames(string interval, TimeSpan value)
		{
			_value = value;
			Interval = interval;
			_values.Add(value, this);
		}

		/// <summary>
		/// Possible time-frames.
		/// </summary>
		public static IEnumerable<TimeSpan> AllTimeFrames
		{
			get { return _values.CachedKeys; }
		}

		internal string Interval { get; private set; }

		internal static bool CanConvert(TimeSpan value)
		{
			return _values.ContainsKey(value);
		}

		/// <summary>
		/// Cast <see cref="TimeSpan"/> object to the type <see cref="IBTimeFrames"/>.
		/// </summary>
		/// <param name="value"><see cref="TimeSpan"/> value.</param>
		/// <returns>Object <see cref="IBTimeFrames"/>.</returns>
		public static implicit operator IBTimeFrames(TimeSpan value)
		{
			if (!CanConvert(value))
				throw new ArgumentException(LocalizedStrings.Str2531Params.Put(value), nameof(value));

			return _values[value];
		}

		/// <summary>
		/// Cast object from <see cref="IBTimeFrames"/> to <see cref="TimeSpan"/>.
		/// </summary>
		/// <param name="timeFrame">Object <see cref="IBTimeFrames"/>.</param>
		/// <returns><see cref="TimeSpan"/> value.</returns>
		public static explicit operator TimeSpan(IBTimeFrames timeFrame)
		{
			if (timeFrame == null)
				throw new ArgumentNullException(nameof(timeFrame));

			return timeFrame._value;
		}

		/// <summary>
		/// One second time frame.
		/// </summary>
		public static TimeSpan Second1 { get; private set; }

		/// <summary>
		/// Five second time frame.
		/// </summary>
		public static TimeSpan Second5 { get; private set; }

		/// <summary>
		/// Fifteen second time frame.
		/// </summary>
		public static TimeSpan Second15 { get; private set; }

		/// <summary>
		/// Thirty second time frame.
		/// </summary>
		public static TimeSpan Second30 { get; private set; }

		/// <summary>
		/// 1 min time-frame.
		/// </summary>
		public static TimeSpan Minute1 { get; private set; }

		/// <summary>
		/// Two minute time frame.
		/// </summary>
		public static TimeSpan Minute2 { get; private set; }

		/// <summary>
		/// Three minute time frame.
		/// </summary>
		public static TimeSpan Minute3 { get; private set; }

		/// <summary>
		/// 5 min time-frame.
		/// </summary>
		public static TimeSpan Minute5 { get; private set; }

		/// <summary>
		/// 15 min time-frame.
		/// </summary>
		public static TimeSpan Minute15 { get; private set; }

		/// <summary>
		/// 30 min time-frame.
		/// </summary>
		public static TimeSpan Minute30 { get; private set; }

		/// <summary>
		/// Hour time frame.
		/// </summary>
		public static TimeSpan Hour { get; private set; }

		/// <summary>
		/// Day time frame.
		/// </summary>
		public static TimeSpan Day { get; private set; }

		/// <summary>
		/// Weekly time frame.
		/// </summary>
		public static TimeSpan Week { get; private set; }

		/// <summary>
		/// Monthly time frame.
		/// </summary>
		public static TimeSpan Month { get; private set; }

		/// <summary>
		/// Annual time frame.
		/// </summary>
		public static TimeSpan Year { get; private set; }
	}
}
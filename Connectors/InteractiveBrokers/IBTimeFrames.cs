namespace StockSharp.InteractiveBrokers
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;
	using StockSharp.Localization;
	
	/// <summary>
	/// Тайм-фреймы для исторических свечек InteractiveBrokers.
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
		/// Все доступные тайм-фреймы.
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
		/// Привести <see cref="TimeSpan"/> значение к объекту <see cref="IBTimeFrames"/>.
		/// </summary>
		/// <param name="value"><see cref="TimeSpan"/> значение.</param>
		/// <returns>Объект <see cref="IBTimeFrames"/>.</returns>
		public static implicit operator IBTimeFrames(TimeSpan value)
		{
			if (!CanConvert(value))
				throw new ArgumentException(LocalizedStrings.Str2531Params.Put(value), "value");

			return _values[value];
		}

		/// <summary>
		/// Привести объект <see cref="IBTimeFrames"/> к <see cref="TimeSpan"/> значению.
		/// </summary>
		/// <param name="timeFrame">Объект <see cref="IBTimeFrames"/>.</param>
		/// <returns><see cref="TimeSpan"/> значение.</returns>
		public static explicit operator TimeSpan(IBTimeFrames timeFrame)
		{
			if (timeFrame == null)
				throw new ArgumentNullException("timeFrame");

			return timeFrame._value;
		}

		/// <summary>
		/// Секундный тайм-фрейм.
		/// </summary>
		public static TimeSpan Second1 { get; private set; }

		/// <summary>
		/// Пяти секундный тайм-фрейм.
		/// </summary>
		public static TimeSpan Second5 { get; private set; }

		/// <summary>
		/// Пятнадцати секундный тайм-фрейм.
		/// </summary>
		public static TimeSpan Second15 { get; private set; }

		/// <summary>
		/// Тридцати секундный тайм-фрейм.
		/// </summary>
		public static TimeSpan Second30 { get; private set; }

		/// <summary>
		/// Минутный тайм-фрейм.
		/// </summary>
		public static TimeSpan Minute1 { get; private set; }

		/// <summary>
		/// Двух минутный тайм-фрейм.
		/// </summary>
		public static TimeSpan Minute2 { get; private set; }

		/// <summary>
		/// Трех минутный тайм-фрейм.
		/// </summary>
		public static TimeSpan Minute3 { get; private set; }

		/// <summary>
		/// Пяти минутный тайм-фрейм.
		/// </summary>
		public static TimeSpan Minute5 { get; private set; }

		/// <summary>
		/// Пятнадцати минутный тайм-фрейм.
		/// </summary>
		public static TimeSpan Minute15 { get; private set; }

		/// <summary>
		/// Тридцати минутный тайм-фрейм.
		/// </summary>
		public static TimeSpan Minute30 { get; private set; }

		/// <summary>
		/// Часовой тайм-фрейм.
		/// </summary>
		public static TimeSpan Hour { get; private set; }

		/// <summary>
		/// Дневной тайм-фрейм.
		/// </summary>
		public static TimeSpan Day { get; private set; }

		/// <summary>
		/// Недельный тайм-фрейм.
		/// </summary>
		public static TimeSpan Week { get; private set; }

		/// <summary>
		/// Месячный тайм-фрейм.
		/// </summary>
		public static TimeSpan Month { get; private set; }

		/// <summary>
		/// Годовой тайм-фрейм.
		/// </summary>
		public static TimeSpan Year { get; private set; }
	}
}
namespace StockSharp.SmartCom
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.SmartCom.Native;

	using StockSharp.Localization;

	/// <summary>
	/// Тайм-фреймы для исторических свечек SmartCOM.
	/// </summary>
	public sealed class SmartComTimeFrames
	{
		private static readonly SynchronizedDictionary<SmartBarInterval, SmartComTimeFrames> _intervals = new SynchronizedDictionary<SmartBarInterval, SmartComTimeFrames>();
		private static readonly CachedSynchronizedDictionary<TimeSpan, SmartComTimeFrames> _values = new CachedSynchronizedDictionary<TimeSpan, SmartComTimeFrames>();

		private readonly TimeSpan _value;

		static SmartComTimeFrames()
		{
			//Tick = (TimeSpan)new SmartComTimeFrames(SmartBarInterval.Tick, TimeSpan.FromSeconds(0.001));
			Minute1 = (TimeSpan)new SmartComTimeFrames(SmartBarInterval.Min1, TimeSpan.FromTicks(TimeSpan.TicksPerMinute));
			Minute5 = (TimeSpan)new SmartComTimeFrames(SmartBarInterval.Min5, TimeSpan.FromTicks(TimeSpan.TicksPerMinute * 5));
			Minute10 = (TimeSpan)new SmartComTimeFrames(SmartBarInterval.Min10, TimeSpan.FromTicks(TimeSpan.TicksPerMinute * 10));
			Minute15 = (TimeSpan)new SmartComTimeFrames(SmartBarInterval.Min15, TimeSpan.FromTicks(TimeSpan.TicksPerMinute * 15));
			Minute30 = (TimeSpan)new SmartComTimeFrames(SmartBarInterval.Min30, TimeSpan.FromTicks(TimeSpan.TicksPerMinute * 30));
			Hour1 = (TimeSpan)new SmartComTimeFrames(SmartBarInterval.Min60, TimeSpan.FromTicks(TimeSpan.TicksPerHour));
			Hour2 = (TimeSpan)new SmartComTimeFrames(SmartBarInterval.Hour2, TimeSpan.FromTicks(TimeSpan.TicksPerHour * 2));
			Hour4 = (TimeSpan)new SmartComTimeFrames(SmartBarInterval.Hour4, TimeSpan.FromTicks(TimeSpan.TicksPerHour * 4));
			Day = (TimeSpan)new SmartComTimeFrames(SmartBarInterval.Day, TimeSpan.FromTicks(TimeSpan.TicksPerDay));
			Week = (TimeSpan)new SmartComTimeFrames(SmartBarInterval.Week, TimeSpan.FromTicks(TimeHelper.TicksPerWeek));
			Month1 = (TimeSpan)new SmartComTimeFrames(SmartBarInterval.Month, TimeSpan.FromTicks(TimeHelper.TicksPerMonth));
			Month3 = (TimeSpan)new SmartComTimeFrames(SmartBarInterval.Quarter, TimeSpan.FromTicks(TimeHelper.TicksPerMonth * 3));
			Year = (TimeSpan)new SmartComTimeFrames(SmartBarInterval.Year, TimeSpan.FromTicks(TimeHelper.TicksPerYear));
		}

		private SmartComTimeFrames(SmartBarInterval interval, TimeSpan value)
		{
			Interval = interval;

			_value = value;

			_intervals.Add(interval, this);
			_values.Add(value, this);
		}

		/// <summary>
		/// Все доступные тайм-фреймы.
		/// </summary>
		public static IEnumerable<TimeSpan> AllTimeFrames
		{
			get { return _values.CachedKeys; }
		}

		internal static SmartComTimeFrames GetTimeFrame(SmartBarInterval interval)
		{
			return _intervals[interval];
		}

		internal static bool CanConvert(TimeSpan value)
		{
			return _values.ContainsKey(value);
		}

		///// <summary>
		///// Тиковый тайм-фрейм.
		///// </summary>
		//public static TimeSpan Tick { get; private set; }

		/// <summary>
		/// Минутный тайм-фрейм.
		/// </summary>
		public static TimeSpan Minute1 { get; private set; }

		/// <summary>
		/// Пяти минутный тайм-фрейм.
		/// </summary>
		public static TimeSpan Minute5 { get; private set; }

		/// <summary>
		/// Десяти минутный тайм-фрейм.
		/// </summary>
		public static TimeSpan Minute10 { get; private set; }

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
		public static TimeSpan Hour1 { get; private set; }

		/// <summary>
		/// Двух часовой тайм-фрейм.
		/// </summary>
		public static TimeSpan Hour2 { get; private set; }

		/// <summary>
		/// Четырех часовой тайм-фрейм.
		/// </summary>
		public static TimeSpan Hour4 { get; private set; }

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
		public static TimeSpan Month1 { get; private set; }

		/// <summary>
		/// Трех месячный тайм-фрейм.
		/// </summary>
		public static TimeSpan Month3 { get; private set; }

		/// <summary>
		/// Годовой тайм-фрейм.
		/// </summary>
		public static TimeSpan Year { get; private set; }

		internal SmartBarInterval Interval { get; private set; }

		/// <summary>
		/// Привести <see cref="TimeSpan"/> значение к объекту <see cref="SmartComTimeFrames"/>.
		/// </summary>
		/// <param name="value"><see cref="TimeSpan"/> значение.</param>
		/// <returns>Объект <see cref="SmartComTimeFrames"/>.</returns>
		public static implicit operator SmartComTimeFrames(TimeSpan value)
		{
			if (!CanConvert(value))
				throw new ArgumentException(LocalizedStrings.WrongCandleArg.Put(value), "value");

			return _values[value];
		}

		/// <summary>
		/// Привести объект <see cref="SmartComTimeFrames"/> к <see cref="TimeSpan"/> значению.
		/// </summary>
		/// <param name="timeFrame">Объект <see cref="SmartComTimeFrames"/>.</param>
		/// <returns><see cref="TimeSpan"/> значение.</returns>
		public static explicit operator TimeSpan(SmartComTimeFrames timeFrame)
		{
			if (timeFrame == null)
				throw new ArgumentNullException("timeFrame");

			return timeFrame._value;
		}
	}
}
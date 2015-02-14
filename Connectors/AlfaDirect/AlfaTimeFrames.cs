namespace StockSharp.AlfaDirect
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	/// <summary>
	/// Тайм-фреймы для исторических свечек Альфа-Директ.
	/// </summary>
	public sealed class AlfaTimeFrames
	{
		private static readonly CachedSynchronizedDictionary<TimeSpan, AlfaTimeFrames> _values = new CachedSynchronizedDictionary<TimeSpan, AlfaTimeFrames>();

		private readonly TimeSpan _value;

		static AlfaTimeFrames()
		{
			Minute1 = (TimeSpan)new AlfaTimeFrames(0, TimeSpan.FromTicks(TimeSpan.TicksPerMinute));
			Minute5 = (TimeSpan)new AlfaTimeFrames(1, TimeSpan.FromTicks(TimeSpan.TicksPerMinute * 5));
			Minute10 = (TimeSpan)new AlfaTimeFrames(2, TimeSpan.FromTicks(TimeSpan.TicksPerMinute * 10));
			Minute15 = (TimeSpan)new AlfaTimeFrames(3, TimeSpan.FromTicks(TimeSpan.TicksPerMinute * 15));
			Minute30 = (TimeSpan)new AlfaTimeFrames(4, TimeSpan.FromTicks(TimeSpan.TicksPerMinute * 30));
			Hour = (TimeSpan)new AlfaTimeFrames(5, TimeSpan.FromTicks(TimeSpan.TicksPerHour));
			Day = (TimeSpan)new AlfaTimeFrames(6, TimeSpan.FromTicks(TimeSpan.TicksPerDay));
			Week = (TimeSpan)new AlfaTimeFrames(7, TimeSpan.FromTicks(TimeHelper.TicksPerWeek));
			Month = (TimeSpan)new AlfaTimeFrames(8, TimeSpan.FromTicks(TimeHelper.TicksPerMonth));
			Year = (TimeSpan)new AlfaTimeFrames(9, TimeSpan.FromTicks(TimeHelper.TicksPerYear));
		}

		private AlfaTimeFrames(int interval, TimeSpan value)
		{
			Interval = interval;

			_value = value;

			_values.Add(value, this);
		}

		/// <summary>
		/// Все доступные тайм-фреймы.
		/// </summary>
		public static IEnumerable<TimeSpan> AllTimeFrames
		{
			get { return _values.CachedKeys; }
		}

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

		internal int Interval { get; private set; }

		/// <summary>
		/// Привести <see cref="TimeSpan"/> значение к объекту <see cref="AlfaTimeFrames"/>.
		/// </summary>
		/// <param name="value"><see cref="TimeSpan"/> значение.</param>
		/// <returns>Объект <see cref="AlfaTimeFrames"/>.</returns>
		public static implicit operator AlfaTimeFrames(TimeSpan value)
		{
			return _values[value];
		}

		/// <summary>
		/// Привести объект <see cref="AlfaTimeFrames"/> к <see cref="TimeSpan"/> значению.
		/// </summary>
		/// <param name="timeFrame">Объект <see cref="AlfaTimeFrames"/>.</param>
		/// <returns><see cref="TimeSpan"/> значение.</returns>
		public static explicit operator TimeSpan(AlfaTimeFrames timeFrame)
		{
			if (timeFrame == null)
				throw new ArgumentNullException("timeFrame");

			return timeFrame._value;
		}

		/// <summary>
		/// Привести объект <see cref="AlfaTimeFrames"/> к <see cref="string"/> значению.
		/// </summary>
		/// <returns><see cref="string"/> значение.</returns>
		public override string ToString()
		{
			return _value.ToString();
		}

		internal static bool CanConvert(TimeSpan value)
		{
			return _values.ContainsKey(value);
		}
	}
}
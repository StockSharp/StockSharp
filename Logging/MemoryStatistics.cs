namespace StockSharp.Logging
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using MoreLinq;

	/// <summary>
	/// Класс отслеживания занимаемых объектов в памяти.
	/// </summary>
	public sealed class MemoryStatistics : BaseLogReceiver
	{
		static MemoryStatistics()
		{
			Instance = new MemoryStatistics();
		}

		/// <summary>
		/// Объект класса <see cref="MemoryStatistics"/>.
		/// </summary>
		public static MemoryStatistics Instance { get; private set; }

		private readonly Timer _timer;

		private MemoryStatistics()
		{
			var lastTime = DateTime.Now;

			_timer = ThreadingHelper.Timer(() =>
			{
				if (IsDisposed)
					return;

				if (DateTime.Now - lastTime < Interval)
					return;

				lastTime = DateTime.Now;

				this.AddInfoLog(ToString());
			}).Interval(Interval);
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			if (IsDisposed)
				return;

			_timer.Dispose();

			base.DisposeManaged();
		}

		private TimeSpan _interval = TimeSpan.FromSeconds(60);

		/// <summary>
		/// Интервал логирования статистики. По умолчанию 1 минута.
		/// </summary>
		public TimeSpan Interval
		{
			get { return _interval; }
			set
			{
				if (_interval == value)
					return;

				_interval = value;
				_timer.Change(value, value);
			}
		}

		private readonly CachedSynchronizedSet<IMemoryStatisticsValue> _values = new CachedSynchronizedSet<IMemoryStatisticsValue>();

		/// <summary>
		/// Отслеживаемые объекты.
		/// </summary>
		public IList<IMemoryStatisticsValue> Values
		{
			get { return _values; }
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Interval", Interval);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Interval = storage.GetValue<TimeSpan>("Interval");
		}

		/// <summary>
		/// Очистить статистику памяти.
		/// </summary>
		/// <param name="resetCounter">Очищать ли счетчик объектов.</param>
		public void Clear(bool resetCounter)
		{
			_values.Cache.ForEach(v => v.Clear(resetCounter));
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return _values.Select(v => "{0} = {1}".Put(v.Name, v.ObjectCount)).Join(", ");
		}

		/// <summary>
		/// Включен ли источник.
		/// </summary>
		public static bool IsEnabled
		{
			get
			{
				return ConfigManager.GetService<LogManager>().Sources.OfType<MemoryStatistics>().Any();
			}
		}

		/// <summary>
		/// Добавить или удалить источник <see cref="MemoryStatistics"/> из зарегистрированного <see cref="LogManager"/>.
		/// </summary>
		public static void AddOrRemove()
		{
			var sources = ConfigManager.GetService<LogManager>().Sources;

			var stat = sources.OfType<MemoryStatistics>().ToArray();

			if (stat.Any())
				sources.RemoveRange(stat);
			else
				sources.Add(Instance);
		}
	}
}
namespace StockSharp.Logging
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Localization;

	/// <summary>
	/// Базовый класс, который мониторит событие <see cref="ILogSource.Log"/> и сохраняет в некое хранилище.
	/// </summary>
	public abstract class LogListener : ILogListener
	{
		static LogListener()
		{
			AllWarningFilter = message => message.Level == LogLevels.Warning;
			AllErrorFilter = message => message.Level == LogLevels.Error;
		}

		/// <summary>
		/// Инициализировать <see cref="LogListener"/>.
		/// </summary>
		protected LogListener()
		{
			Filters = new List<Func<LogMessage, bool>>();
		}

		/// <summary>
		/// Фильтр, который принимает только сообщения типа <see cref="LogLevels.Warning"/>.
		/// </summary>
		public static readonly Func<LogMessage, bool> AllWarningFilter;

		/// <summary>
		/// Фильтр, который принимает только сообщения типа <see cref="LogLevels.Error"/>.
		/// </summary>
		public static readonly Func<LogMessage, bool> AllErrorFilter;

		/// <summary>
		/// Фильтры сообщений, которыми указывается, какие сообщения следует обрабатывать.
		/// </summary>
		public IList<Func<LogMessage, bool>> Filters { get; private set; }

		private string _dateFormat = "yyyy/MM/dd";

		/// <summary>
		/// Формат даты. По-умолчанию используется yyyy/MM/dd.
		/// </summary>
		public string DateFormat
		{
			get { return _dateFormat; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException("value");

				_dateFormat = value;
			}
		}

		private string _timeFormat = "HH:mm:ss.fff";

		/// <summary>
		/// Формат времени. По-умолчанию используется HH:mm:ss.fff.
		/// </summary>
		public string TimeFormat
		{
			get { return _timeFormat; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException("value");
				
				_timeFormat = value;
			}
		}

		/// <summary>
		/// Записать сообщения.
		/// </summary>
		/// <param name="messages">Отладочные сообщения.</param>
		public void WriteMessages(IEnumerable<LogMessage> messages)
		{
			if (Filters.Count > 0)
				messages = messages.Where(m => Filters.Any(f => f(m)));

			OnWriteMessages(messages);
		}

		/// <summary>
		/// Записать сообщения.
		/// </summary>
		/// <param name="messages">Отладочные сообщения.</param>
		protected virtual void OnWriteMessages(IEnumerable<LogMessage> messages)
		{
			messages.ForEach(OnWriteMessage);
		}

		/// <summary>
		/// Записать сообщение.
		/// </summary>
		/// <param name="message">Отладочное сообщение.</param>
		protected virtual void OnWriteMessage(LogMessage message)
		{
			throw new NotSupportedException(LocalizedStrings.Str17);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public virtual void Load(SettingsStorage storage)
		{
			DateFormat = storage.GetValue<string>("DateFormat");
			TimeFormat = storage.GetValue<string>("TimeFormat");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue("DateFormat", DateFormat);
			storage.SetValue("TimeFormat", TimeFormat);
		}
	}
}
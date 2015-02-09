namespace StockSharp.Logging
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Интерфейс источника логов.
	/// </summary>
	public interface ILogSource : IDisposable
	{
		/// <summary>
		/// Уникальный идентификатор источника.
		/// </summary>
		Guid Id { get; }

		/// <summary>
		/// Имя источника.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Родительский источник логов.
		/// </summary>
		ILogSource Parent { get; set; }

		/// <summary>
		/// Уровень логирования для источника.
		/// </summary>
		LogLevels LogLevel { get; set; }

		/// <summary>
		/// Текущее время, которое будет передано в <see cref="LogMessage.Time"/>.
		/// </summary>
		DateTimeOffset CurrentTime { get; }

		/// <summary>
		/// Событие нового отладочного сообщения.
		/// </summary>
		event Action<LogMessage> Log;
	}

	/// <summary>
	/// Базовая реализация <see cref="ILogSource"/>.
	/// </summary>
	public abstract class BaseLogSource : Disposable, ILogSource, IPersistable
	{
		/// <summary>
		/// Инициализировать <see cref="BaseLogSource"/>.
		/// </summary>
		protected BaseLogSource()
		{
			_name = GetType().GetDisplayName();
		}

		private Guid _id = Guid.NewGuid();

		/// <summary>
		/// Уникальный идентификатор источника.
		/// </summary>
		[Browsable(false)]
		public virtual Guid Id
		{
			get { return _id; }
			set { _id = value; }
		}

		private string _name;

		/// <summary>
		/// Название источника (для различия в лог файлах).
		/// </summary>
		[ReadOnly(true)]
		[CategoryLoc(LocalizedStrings.LoggingKey)]
		[DisplayNameLoc(LocalizedStrings.NameKey)]
		[DescriptionLoc(LocalizedStrings.Str7Key)]
		public virtual string Name
		{
			get { return _name; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException("value");

				_name = value;
			}
		}

		private ILogSource _parent;

		/// <summary>
		/// Родитель.
		/// </summary>
		[Browsable(false)]
		public ILogSource Parent
		{
			get { return _parent; }
			set
			{
				if (value == _parent)
					return;

				if (value != null && _parent != null)
					throw new ArgumentException(LocalizedStrings.Str8Params.Put(this, _parent), "value");

				_parent = value;
			}
		}

		private LogLevels _logLevel = LogLevels.Inherit;

		/// <summary>
		/// Уровень логирования. По-умолчанию установлено в <see cref="LogLevels.Inherit"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.LoggingKey)]
		[DisplayNameLoc(LocalizedStrings.Str9Key)]
		[DescriptionLoc(LocalizedStrings.Str9Key, true)]
		public virtual LogLevels LogLevel
		{
			get { return _logLevel; }
			set { _logLevel = value; }
		}

		/// <summary>
		/// Текущее время, которое будет передано в <see cref="LogMessage.Time"/>.
		/// </summary>
		[Browsable(false)]
		public virtual DateTimeOffset CurrentTime
		{
			get { return TimeHelper.Now; }
		}

		private Action<LogMessage> _log;

		/// <summary>
		/// Событие нового отладочного сообщения.
		/// </summary>
		public event Action<LogMessage> Log
		{
			add { _log += value; }
			remove { _log -= value; }
		}

		/// <summary>
		/// Вызвать событие <see cref="ILogSource.Log"/>.
		/// </summary>
		/// <param name="message">Отладочное сообщение.</param>
		protected virtual void RaiseLog(LogMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (message.Level < message.Source.LogLevel)
				return;

			//if (_log == null && Parent.IsNull())
			//	throw new InvalidOperationException("Родитель не подписан на дочерний лог.");

			_log.SafeInvoke(message);

			var parent = Parent as ILogReceiver;

			if (parent != null)
				parent.AddLog(message);
		}

		/// <summary>
		/// Получить строковое представление источника.
		/// </summary>
		/// <returns>Строковое представление источника.</returns>
		public override string ToString()
		{
			return Name;
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			Parent = null;
			base.DisposeManaged();
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public virtual void Load(SettingsStorage storage)
		{
			LogLevel = storage.GetValue("LogLevel", LogLevels.Inherit);
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue("LogLevel", LogLevel.To<string>());
		}
	}
}
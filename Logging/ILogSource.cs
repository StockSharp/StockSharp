namespace StockSharp.Logging
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Logs source interface.
	/// </summary>
	public interface ILogSource : IDisposable
	{
		/// <summary>
		/// The unique identifier of the source.
		/// </summary>
		Guid Id { get; }

		/// <summary>
		/// The source name.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Parental logs source.
		/// </summary>
		ILogSource Parent { get; set; }

		/// <summary>
		/// The logging level for the source.
		/// </summary>
		LogLevels LogLevel { get; set; }

		/// <summary>
		/// Current time, which will be passed to the <see cref="LogMessage.Time"/>.
		/// </summary>
		DateTimeOffset CurrentTime { get; }

		/// <summary>
		/// Whether the source is the root (even if <see cref="ILogSource.Parent"/> is not equal to <see langword="null" />).
		/// </summary>
		bool IsRoot { get; }

		/// <summary>
		/// New debug message event.
		/// </summary>
		event Action<LogMessage> Log;
	}

	/// <summary>
	/// The base implementation <see cref="ILogSource"/>.
	/// </summary>
	public abstract class BaseLogSource : Disposable, ILogSource, IPersistable
	{
		/// <summary>
		/// Initialize <see cref="BaseLogSource"/>.
		/// </summary>
		protected BaseLogSource()
		{
			_name = GetType().GetDisplayName();
		}

		private Guid _id = Guid.NewGuid();

		/// <summary>
		/// The unique identifier of the source.
		/// </summary>
		[Browsable(false)]
		public virtual Guid Id
		{
			get { return _id; }
			set { _id = value; }
		}

		private string _name;

		/// <summary>
		/// Source name (to distinguish in log files).
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
		/// Parent.
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
		/// The logging level. The default is set to <see cref="LogLevels.Inherit"/>.
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
		/// Current time, which will be passed to the <see cref="LogMessage.Time"/>.
		/// </summary>
		[Browsable(false)]
		public virtual DateTimeOffset CurrentTime
		{
			get { return TimeHelper.Now; }
		}

		/// <summary>
		/// Whether the source is the root (even if <see cref="ILogSource.Parent"/> is not equal to <see langword="null" />).
		/// </summary>
		[Browsable(false)]
		public bool IsRoot { get; set; }

		private Action<LogMessage> _log;

		/// <summary>
		/// New debug message event.
		/// </summary>
		public event Action<LogMessage> Log
		{
			add { _log += value; }
			remove { _log -= value; }
		}

		/// <summary>
		/// To call the event <see cref="ILogSource.Log"/>.
		/// </summary>
		/// <param name="message">A debug message.</param>
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
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return Name;
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public virtual void Load(SettingsStorage storage)
		{
			LogLevel = storage.GetValue("LogLevel", LogLevels.Inherit);
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue("LogLevel", LogLevel.To<string>());
		}
	}
}
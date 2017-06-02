#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Logging.Logging
File: ILogSource.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Logging
{
	using System;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

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

		/// <summary>
		/// The unique identifier of the source.
		/// </summary>
		[Browsable(false)]
		public virtual Guid Id { get; set; } = Guid.NewGuid();

		private string _name;

		/// <summary>
		/// Source name (to distinguish in log files).
		/// </summary>
		[ReadOnly(true)]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.NameKey,
			Description = LocalizedStrings.Str7Key,
			GroupName = LocalizedStrings.LoggingKey,
			Order = 0)]
		public virtual string Name
		{
			get => _name;
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException(nameof(value));

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
			get => _parent;
			set
			{
				if (value == _parent)
					return;

				if (value != null && _parent != null)
					throw new ArgumentException(LocalizedStrings.Str8Params.Put(this, _parent), nameof(value));

				if (value == this)
					throw new ArgumentException(LocalizedStrings.CyclicDependency.Put(this), nameof(value));

				_parent = value;
			}
		}

		/// <summary>
		/// The logging level. The default is set to <see cref="LogLevels.Inherit"/>.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str9Key,
			Description = LocalizedStrings.Str9Key + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.LoggingKey,
			Order = 1)]
		public virtual LogLevels LogLevel { get; set; } = LogLevels.Inherit;

		/// <summary>
		/// Current time, which will be passed to the <see cref="LogMessage.Time"/>.
		/// </summary>
		[Browsable(false)]
		public virtual DateTimeOffset CurrentTime => TimeHelper.NowWithOffset;

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
			add => _log += value;
			remove => _log -= value;
		}

		/// <summary>
		/// To call the event <see cref="ILogSource.Log"/>.
		/// </summary>
		/// <param name="message">A debug message.</param>
		protected virtual void RaiseLog(LogMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (message.Level < message.Source.LogLevel)
				return;

			_log?.Invoke(message);

			var parent = Parent as ILogReceiver;

			parent?.AddLog(message);
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
			LogLevel = storage.GetValue(nameof(LogLevel), LogLevels.Inherit);
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(LogLevel), LogLevel.To<string>());
		}
	}
}
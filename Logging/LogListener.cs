namespace StockSharp.Logging;

using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;
using Ecng.Collections;

using StockSharp.Localization;

/// <summary>
/// The base class that monitors the event <see cref="ILogSource.Log"/> and saves to some storage.
/// </summary>
public abstract class LogListener : Disposable, ILogListener
{
	/// <summary>
	/// Initialize <see cref="LogListener"/>.
	/// </summary>
	protected LogListener()
	{
		Filters = [];

		CanSave = GetType().GetConstructor([]) is not null;
	}

	/// <summary>
	/// Messages filters that specify which messages should be handled.
	/// </summary>
	public IList<Func<LogMessage, bool>> Filters { get; }

	private string _dateFormat = "yyyy/MM/dd";

	/// <summary>
	/// Date format. By default yyyy/MM/dd.
	/// </summary>
	public string DateFormat
	{
		get => _dateFormat;
		set
		{
			if (value.IsEmpty())
				throw new ArgumentNullException(nameof(value));

			_dateFormat = value;
		}
	}

	private string _timeFormat = "HH:mm:ss.fff";

	/// <summary>
	/// Time format. By default HH:mm:ss.fff.
	/// </summary>
	public string TimeFormat
	{
		get => _timeFormat;
		set
		{
			if (value.IsEmpty())
				throw new ArgumentNullException(nameof(value));
			
			_timeFormat = value;
		}
	}

	/// <inheritdoc />
	public virtual bool CanSave { get; }

	/// <inheritdoc />
	public void WriteMessages(IEnumerable<LogMessage> messages)
	{
		OnWriteMessages(messages.Filter(Filters));
	}

	/// <summary>
	/// To record messages.
	/// </summary>
	/// <param name="messages">Debug messages.</param>
	protected virtual void OnWriteMessages(IEnumerable<LogMessage> messages)
	{
		messages.ForEach(OnWriteMessage);
	}

	/// <summary>
	/// To record a message.
	/// </summary>
	/// <param name="message">A debug message.</param>
	protected virtual void OnWriteMessage(LogMessage message)
	{
		throw new NotSupportedException(LocalizedStrings.MethodMustBeOverrided);
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public virtual void Load(SettingsStorage storage)
	{
		DateFormat = storage.GetValue<string>(nameof(DateFormat));
		TimeFormat = storage.GetValue<string>(nameof(TimeFormat));
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public virtual void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(DateFormat), DateFormat);
		storage.SetValue(nameof(TimeFormat), TimeFormat);
	}
}
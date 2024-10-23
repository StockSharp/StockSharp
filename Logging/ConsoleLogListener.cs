namespace StockSharp.Logging;

using System;

using Ecng.Common;

using StockSharp.Localization;

/// <summary>
/// The logger that records the data to the console window.
/// </summary>
public class ConsoleLogListener : LogListener
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ConsoleLogListener"/>.
	/// </summary>
	public ConsoleLogListener()
	{
	}

	/// <inheritdoc />
	protected override void OnWriteMessage(LogMessage message)
	{
		if (message.IsDispose)
		{
			Dispose();
			return;
		}

		var color = message.Level switch
		{
			LogLevels.Verbose or LogLevels.Debug or LogLevels.Info => ConsoleHelper.Info,
			LogLevels.Warning => ConsoleHelper.Warning,
			LogLevels.Error => ConsoleHelper.Error,
			_ => throw new ArgumentOutOfRangeException(nameof(message), message.Level, LocalizedStrings.InvalidValue),
		};
		var newLine = "{0} | {1, -15} | {2}".Put(message.Time.ToString(TimeFormat), message.Source.Name, message.Message);

		newLine.ConsoleWithColor(color);
	}
}
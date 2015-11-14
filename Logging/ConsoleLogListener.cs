namespace StockSharp.Logging
{
	using System;

	using Ecng.Common;
	using Ecng.Interop;

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
			if (!WinApi.AllocateConsole())
				throw new InvalidOperationException(LocalizedStrings.CannotCreateConsoleWindow);
		}

		/// <summary>
		/// To record a message.
		/// </summary>
		/// <param name="message">A debug message.</param>
		protected override void OnWriteMessage(LogMessage message)
		{
			if (message.IsDispose)
			{
				Dispose();
				return;
			}

			ConsoleColor color;

			switch (message.Level)
			{
				case LogLevels.Debug:
				case LogLevels.Info:
					color = ConsoleHelper.Info;
					break;
				case LogLevels.Warning:
					color = ConsoleHelper.Warning;
					break;
				case LogLevels.Error:
					color = ConsoleHelper.Error;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(message));
			}

			var newLine = "{0} | {1, -15} | {2}".Put(message.Time.ToString(TimeFormat), message.Source.Name, message.Message);

			newLine.ConsoleWithColor(color);
		}
	}
}
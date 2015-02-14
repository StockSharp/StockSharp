namespace StockSharp.Logging
{
	using System;

	using Ecng.Common;
	using Ecng.Interop;

	using StockSharp.Localization;

	/// <summary>
	/// Логгер, записывающий данные в консольное окно.
	/// </summary>
	public class ConsoleLogListener : LogListener
	{
		/// <summary>
		/// Создать <see cref="ConsoleLogListener"/>.
		/// </summary>
		public ConsoleLogListener()
		{
			if (!WinApi.AllocateConsole())
				throw new InvalidOperationException(LocalizedStrings.CannotCreateConsoleWindow);
		}

		/// <summary>
		/// Записать сообщение.
		/// </summary>
		/// <param name="message">Отладочное сообщение.</param>
		protected override void OnWriteMessage(LogMessage message)
		{
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
					throw new ArgumentOutOfRangeException("message");
			}

			var newLine = "{0} | {1, -15} | {2}".Put(message.Time.ToString(TimeFormat), message.Source.Name, message.Message);

			newLine.ConsoleWithColor(color);
		}
	}
}
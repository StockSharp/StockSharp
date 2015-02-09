namespace StockSharp.Logging
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;

	using StockSharp.Localization;

	/// <summary>
	/// Логгер стратегии, записывающий данные в отладочное окно.
	/// </summary>
	public class DebugLogListener : LogListener
	{
		/// <summary>
		/// Записать сообщения.
		/// </summary>
		/// <param name="messages">Отладочные сообщения.</param>
		protected override void OnWriteMessages(IEnumerable<LogMessage> messages)
		{
			var sb = new StringBuilder();

			var currLevel = LogLevels.Info;

			foreach (var message in messages)
			{
				if (message.Level != currLevel)
				{
					Dump(currLevel, sb);
					currLevel = message.Level;
				}

				sb.AppendFormat("{0} {1}", message.Source.Name, message.Message).AppendLine();
			}

			if (sb.Length > 0)
				Dump(currLevel, sb);
		}

		private static void Dump(LogLevels level, StringBuilder builder)
		{
			var str = builder.ToString();

			switch (level)
			{
				case LogLevels.Debug:
				case LogLevels.Info:
					Trace.TraceInformation(str);
					break;
				case LogLevels.Warning:
					Trace.TraceWarning(str);
					break;
				case LogLevels.Error:
					Trace.TraceError(str);
					break;
				default:
					throw new ArgumentOutOfRangeException("level", level, LocalizedStrings.UnknownLevelLog);
			}

			builder.Clear();
		}

		///// <summary>
		///// Записать сообщение.
		///// </summary>
		///// <param name="message">Отладочное сообщение.</param>
		//protected override void OnWriteMessage(LogMessage message)
		//{
		//	switch (message.Level)
		//	{
		//		case LogLevels.Debug:
		//		case LogLevels.Info:
		//			Trace.TraceInformation("{0} {1}", message.Source.Name, message.Message);
		//			break;
		//		case LogLevels.Warning:
		//			Trace.TraceWarning("{0} {1}", message.Source.Name, message.Message);
		//			break;
		//		case LogLevels.Error:
		//			Trace.TraceError("{0} {1}", message.Source.Name, message.Message);
		//			break;
		//		default:
		//			throw new ArgumentOutOfRangeException("message");
		//	}
		//}
	}
}
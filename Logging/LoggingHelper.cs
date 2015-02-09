namespace StockSharp.Logging
{
	using System;
	using System.Linq;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Configuration;

	/// <summary>
	/// Вспомогательный класс для работы с <see cref="ILogSource"/>. 
	/// </summary>
	public static class LoggingHelper
	{
		/// <summary>
		/// Записать сообщение в лог.
		/// </summary>
		/// <param name="receiver">Получатель логов.</param>
		/// <param name="getMessage">Функция, возвращающая текст для <see cref="LogMessage.Message"/>.</param>
		public static void AddInfoLog(this ILogReceiver receiver, Func<string> getMessage)
		{
			receiver.AddLog(LogLevels.Info, getMessage);
		}

		/// <summary>
		/// Записать предупреждение в лог.
		/// </summary>
		/// <param name="receiver">Получатель логов.</param>
		/// <param name="getMessage">Функция, возвращающая текст для <see cref="LogMessage.Message"/>.</param>
		public static void AddWarningLog(this ILogReceiver receiver, Func<string> getMessage)
		{
			receiver.AddLog(LogLevels.Warning, getMessage);
		}

		/// <summary>
		/// Записать ошибку в лог.
		/// </summary>
		/// <param name="receiver">Получатель логов.</param>
		/// <param name="getMessage">Функция, возвращающая текст для <see cref="LogMessage.Message"/>.</param>
		public static void AddErrorLog(this ILogReceiver receiver, Func<string> getMessage)
		{
			receiver.AddLog(LogLevels.Error, getMessage);
		}

		/// <summary>
		/// Записать сообщение в лог.
		/// </summary>
		/// <param name="receiver">Получатель логов.</param>
		/// <param name="level">Уровень лог-сообщения.</param>
		/// <param name="getMessage">Функция, возвращающая текст для <see cref="LogMessage.Message"/>.</param>
		public static void AddLog(this ILogReceiver receiver, LogLevels level, Func<string> getMessage)
		{
			if (receiver == null)
				throw new ArgumentNullException("receiver");

			receiver.AddLog(new LogMessage(receiver, receiver.CurrentTime, level, getMessage));
		}

		/// <summary>
		/// Записать сообщение в лог.
		/// </summary>
		/// <param name="receiver">Получатель логов.</param>
		/// <param name="message">Текстовое сообщение.</param>
		/// <param name="args">Параметры текстового сообщения.
		/// Используются в случае, если message является форматирующей строкой.
		/// Подробнее, <see cref="string.Format(string,object[])"/>.</param>
		public static void AddInfoLog(this ILogReceiver receiver, string message, params object[] args)
		{
			receiver.AddMessage(LogLevels.Info, message, args);
		}

		/// <summary>
		/// Записать отладку в лог.
		/// </summary>
		/// <param name="receiver">Получатель логов.</param>
		/// <param name="message">Текстовое сообщение.</param>
		/// <param name="args">Параметры текстового сообщения.
		/// Используются в случае, если message является форматирующей строкой.
		/// Подробнее, <see cref="string.Format(string,object[])"/>.</param>
		public static void AddDebugLog(this ILogReceiver receiver, string message, params object[] args)
		{
			receiver.AddMessage(LogLevels.Debug, message, args);
		}

		/// <summary>
		/// Записать предупреждение в лог.
		/// </summary>
		/// <param name="receiver">Получатель логов.</param>
		/// <param name="message">Текстовое сообщение.</param>
		/// <param name="args">Параметры текстового сообщения.
		/// Используются в случае, если message является форматирующей строкой.
		/// Подробнее, <see cref="string.Format(string,object[])"/>.</param>
		public static void AddWarningLog(this ILogReceiver receiver, string message, params object[] args)
		{
			receiver.AddMessage(LogLevels.Warning, message, args);
		}

		/// <summary>
		/// Записать ошибку в лог.
		/// </summary>
		/// <param name="receiver">Получатель логов.</param>
		/// <param name="exception">Описание ошибки.</param>
		public static void AddErrorLog(this ILogReceiver receiver, Exception exception)
		{
			receiver.AddErrorLog(exception, null);
		}

		/// <summary>
		/// Записать ошибку в лог.
		/// </summary>
		/// <param name="receiver">Получатель логов.</param>
		/// <param name="exception">Описание ошибки.</param>
		/// <param name="message">Текстовое сообщение.</param>
		public static void AddErrorLog(this ILogReceiver receiver, Exception exception, string message)
		{
			if (receiver == null)
				throw new ArgumentNullException("receiver");

			if (exception == null)
				throw new ArgumentNullException("exception");

			receiver.AddLog(new LogMessage(receiver, receiver.CurrentTime, LogLevels.Error, () =>
			{
				var msg = exception.ToString();
				
				var refExc = exception as ReflectionTypeLoadException;

				if (refExc != null)
				{
					msg += Environment.NewLine
						+ refExc
							.LoaderExceptions
							.Select(e => e.ToString())
							.Join(Environment.NewLine);
				}

				if (message != null)
					msg = message.Put(msg);

				return msg;
			}));
		}

		/// <summary>
		/// Записать ошибку в лог.
		/// </summary>
		/// <param name="receiver">Получатель логов.</param>
		/// <param name="message">Текстовое сообщение.</param>
		/// <param name="args">Параметры текстового сообщения.
		/// Используются в случае, если message является форматирующей строкой.
		/// Подробнее, <see cref="string.Format(string,object[])"/>.</param>
		public static void AddErrorLog(this ILogReceiver receiver, string message, params object[] args)
		{
			receiver.AddMessage(LogLevels.Error, message, args);
		}

		private static void AddMessage(this ILogReceiver receiver, LogLevels level, string message, params object[] args)
		{
			if (receiver == null)
				throw new ArgumentNullException("receiver");

			if (level < receiver.LogLevel)
				return;

			receiver.AddLog(new LogMessage(receiver, receiver.CurrentTime, level, message, args));
		}

		/// <summary>
		/// Записать ошибку в <see cref="LogManager.Application"/>.
		/// </summary>
		/// <param name="error">Ошибка.</param>
		/// <param name="message">Текстовое сообщение.</param>
		public static void LogError(this Exception error, string message = null)
		{
			if (error == null)
				throw new ArgumentNullException("error");

			var manager = ConfigManager.TryGetService<LogManager>();

			if (manager != null)
				manager.Application.AddErrorLog(error, message);
		}

		/// <summary>
		/// Получить <see cref="ILogSource.LogLevel"/> для источника.
		/// Если значение равно <see cref="LogLevels.Inherit"/>, то берется уровень родительского источника.
		/// </summary>
		/// <param name="source">Источнико логов.</param>
		/// <returns>Уровень логирования.</returns>
		public static LogLevels GetLogLevel(this ILogSource source)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			var level = source.LogLevel;

			if (level != LogLevels.Inherit)
				return level;

			var parent = source.Parent;
			return parent != null ? GetLogLevel(parent) : LogLevels.Inherit;
		}
	}
}
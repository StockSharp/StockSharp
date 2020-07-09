#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Logging.Logging
File: LoggingHelper.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Logging
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Configuration;

	using StockSharp.Localization;

	/// <summary>
	/// Extension class for <see cref="ILogSource"/>.
	/// </summary>
	public static class LoggingHelper
	{
		/// <summary>
		/// To record a message to the log.
		/// </summary>
		/// <param name="receiver">Logs receiver.</param>
		/// <param name="getMessage">The function returns the text for <see cref="LogMessage.Message"/>.</param>
		public static void AddInfoLog(this ILogReceiver receiver, Func<string> getMessage)
		{
			receiver.AddLog(LogLevels.Info, getMessage);
		}

		/// <summary>
		/// To record a warning to the log.
		/// </summary>
		/// <param name="receiver">Logs receiver.</param>
		/// <param name="getMessage">The function returns the text for <see cref="LogMessage.Message"/>.</param>
		public static void AddWarningLog(this ILogReceiver receiver, Func<string> getMessage)
		{
			receiver.AddLog(LogLevels.Warning, getMessage);
		}

		/// <summary>
		/// To record an error to the log.
		/// </summary>
		/// <param name="receiver">Logs receiver.</param>
		/// <param name="getMessage">The function returns the text for <see cref="LogMessage.Message"/>.</param>
		public static void AddErrorLog(this ILogReceiver receiver, Func<string> getMessage)
		{
			receiver.AddLog(LogLevels.Error, getMessage);
		}

		/// <summary>
		/// To record a message to the log.
		/// </summary>
		/// <param name="receiver">Logs receiver.</param>
		/// <param name="level">The level of the log message.</param>
		/// <param name="getMessage">The function returns the text for <see cref="LogMessage.Message"/>.</param>
		public static void AddLog(this ILogReceiver receiver, LogLevels level, Func<string> getMessage)
		{
			if (receiver == null)
				throw new ArgumentNullException(nameof(receiver));

			receiver.AddLog(new LogMessage(receiver, receiver.CurrentTime, level, getMessage));
		}

		/// <summary>
		/// To record a message to the log.
		/// </summary>
		/// <param name="receiver">Logs receiver.</param>
		/// <param name="message">Text message.</param>
		/// <param name="args">Text message settings. Used if a message is the format string. For details, see <see cref="string.Format(string,object[])"/>.</param>
		public static void AddInfoLog(this ILogReceiver receiver, string message, params object[] args)
		{
			receiver.AddMessage(LogLevels.Info, message, args);
		}

		/// <summary>
		/// To record a verbose message to the log.
		/// </summary>
		/// <param name="receiver">Logs receiver.</param>
		/// <param name="message">Text message.</param>
		/// <param name="args">Text message settings. Used if a message is the format string. For details, see <see cref="string.Format(string,object[])"/>.</param>
		public static void AddVerboseLog(this ILogReceiver receiver, string message, params object[] args)
		{
			receiver.AddMessage(LogLevels.Verbose, message, args);
		}

		/// <summary>
		/// To record a debug message to the log.
		/// </summary>
		/// <param name="receiver">Logs receiver.</param>
		/// <param name="message">Text message.</param>
		/// <param name="args">Text message settings. Used if a message is the format string. For details, see <see cref="string.Format(string,object[])"/>.</param>
		public static void AddDebugLog(this ILogReceiver receiver, string message, params object[] args)
		{
			receiver.AddMessage(LogLevels.Debug, message, args);
		}

		/// <summary>
		/// To record a warning to the log.
		/// </summary>
		/// <param name="receiver">Logs receiver.</param>
		/// <param name="message">Text message.</param>
		/// <param name="args">Text message settings. Used if a message is the format string. For details, see <see cref="string.Format(string,object[])"/>.</param>
		public static void AddWarningLog(this ILogReceiver receiver, string message, params object[] args)
		{
			receiver.AddMessage(LogLevels.Warning, message, args);
		}

		/// <summary>
		/// To record an error to the log.
		/// </summary>
		/// <param name="receiver">Logs receiver.</param>
		/// <param name="exception">Error details.</param>
		public static void AddErrorLog(this ILogReceiver receiver, Exception exception)
		{
			receiver.AddErrorLog(exception, null);
		}

		/// <summary>
		/// To record an error to the log.
		/// </summary>
		/// <param name="receiver">Logs receiver.</param>
		/// <param name="exception">Error details.</param>
		/// <param name="format">A format string.</param>
		public static void AddErrorLog(this ILogReceiver receiver, Exception exception, string format)
		{
			if (receiver == null)
				throw new ArgumentNullException(nameof(receiver));

			if (exception == null)
				throw new ArgumentNullException(nameof(exception));

			receiver.AddLog(new LogMessage(receiver, receiver.CurrentTime, LogLevels.Error, () =>
			{
				var msg = exception.ToString();

				if (exception is ReflectionTypeLoadException refExc)
				{
					msg += Environment.NewLine
						+ refExc
							.LoaderExceptions
							.Select(e => e.ToString())
							.Join(Environment.NewLine);
				}

				if (format != null)
					msg = format.Put(msg);

				return msg;
			}));
		}

		/// <summary>
		/// To record an error to the log.
		/// </summary>
		/// <param name="message">Text message.</param>
		/// <param name="args">Text message settings. Used if a message is the format string. For details, see <see cref="string.Format(string,object[])"/>.</param>
		public static void AddErrorLog(this string message, params object[] args)
		{
			ConfigManager.TryGetService<LogManager>()?.Application.AddMessage(LogLevels.Error, message, args);
		}

		/// <summary>
		/// To record an error to the log.
		/// </summary>
		/// <param name="receiver">Logs receiver.</param>
		/// <param name="message">Text message.</param>
		/// <param name="args">Text message settings. Used if a message is the format string. For details, see <see cref="string.Format(string,object[])"/>.</param>
		public static void AddErrorLog(this ILogReceiver receiver, string message, params object[] args)
		{
			receiver.AddMessage(LogLevels.Error, message, args);
		}

		private static void AddMessage(this ILogReceiver receiver, LogLevels level, string message, params object[] args)
		{
			if (receiver == null)
				throw new ArgumentNullException(nameof(receiver));

			if (level < receiver.LogLevel)
				return;

			receiver.AddLog(new LogMessage(receiver, receiver.CurrentTime, level, message, args));
		}

		/// <summary>
		/// To record an error to the <see cref="LogManager.Application"/>.
		/// </summary>
		/// <param name="error">Error.</param>
		/// <param name="format">A format string.</param>
		public static void LogError(this Exception error, string format = null)
		{
			if (error == null)
				throw new ArgumentNullException(nameof(error));

			LogManager.Instance?.Application.AddErrorLog(error, format);
		}

		/// <summary>
		/// Get <see cref="ILogSource.LogLevel"/> for the source. If the value is equal to <see cref="LogLevels.Inherit"/>, then parental source level is taken.
		/// </summary>
		/// <param name="source">The log source.</param>
		/// <returns>Logging level.</returns>
		public static LogLevels GetLogLevel(this ILogSource source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			do
			{
				var level = source.LogLevel;

				if (level != LogLevels.Inherit)
					return level;

				source = source.Parent;
			}
			while (source != null);
			
			return LogLevels.Inherit;
		}

		/// <summary>
		/// Wrap the specified action in try/catch clause with logging.
		/// </summary>
		/// <param name="action">The action.</param>
		public static void DoWithLog(this Action action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			try
			{
				action();
			}
			catch (Exception ex)
			{
				ex.LogError();
			}
		}

		/// <summary>
		/// Wrap the specified action in try/catch clause with logging.
		/// </summary>
		/// <typeparam name="T">The type of returned result.</typeparam>
		/// <param name="action">The action.</param>
		/// <returns>The resulting value.</returns>
		public static T DoWithLog<T>(this Func<T> action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			try
			{
				return action();
			}
			catch (Exception ex)
			{
				ex.LogError();
				return default;
			}
		}

		/// <summary>
		/// Wrap the specified action in try/catch clause with logging.
		/// </summary>
		/// <typeparam name="T">The type of returned result.</typeparam>
		/// <param name="action">The action.</param>
		/// <returns>The resulting value.</returns>
		public static void DoWithLog<T>(Func<IDictionary<T, Exception>> action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			try
			{
				var dict = action();

				foreach (var pair in dict)
				{
					new InvalidOperationException(pair.Key.ToString(), pair.Value).LogError(LocalizedStrings.CorruptedFile);
				}
			}
			catch (Exception ex)
			{
				ex.LogError();
			}
		}

		/// <summary>
		/// Wrap the specified action in try/catch clause with logging.
		/// </summary>
		/// <typeparam name="T1">The type of source values.</typeparam>
		/// <typeparam name="T2">The type of returned result.</typeparam>
		/// <param name="from">Source values</param>
		/// <param name="func">Converter.</param>
		/// <returns>Result.</returns>
		public static T2[] SafeAdd<T1, T2>(this IEnumerable from, Func<T1, T2> func)
		{
			if (from == null)
				throw new ArgumentNullException(nameof(from));

			var list = new List<T2>();

			foreach (T1 item in from)
			{
				try
				{
					list.Add(func(item));
				}
				catch (Exception e)
				{
					e.LogError();
				}
			}

			return list.ToArray();
		}

		/// <summary>
		/// The filter that only accepts messages of <see cref="LogLevels.Warning"/> type.
		/// </summary>
		public static readonly Func<LogMessage, bool> OnlyWarning = message => message.Level == LogLevels.Warning;

		/// <summary>
		/// The filter that only accepts messages of <see cref="LogLevels.Error"/> type.
		/// </summary>
		public static readonly Func<LogMessage, bool> OnlyError = message => message.Level == LogLevels.Error;

		/// <summary>
		/// Filter messages.
		/// </summary>
		/// <param name="messages">Incoming messages.</param>
		/// <param name="filters">Messages filters that specify which messages should be handled.</param>
		/// <returns>Filtered collection.</returns>
		public static IEnumerable<LogMessage> Filter(this IEnumerable<LogMessage> messages, ICollection<Func<LogMessage, bool>> filters)
		{
			if (filters.Count > 0)
				messages = messages.Where(m => filters.Any(f => f(m)));

			return messages;
		}
	}
}
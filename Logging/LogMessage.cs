#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Logging.Logging
File: LogMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Logging
{
	using System;

	using Ecng.Common;

	/// <summary>
	/// A debug message.
	/// </summary>
	public class LogMessage
	{
		internal bool IsDispose;

		private Func<string> _getMessage;

		/// <summary>
		/// Initializes a new instance of the <see cref="LogMessage"/>.
		/// </summary>
		/// <param name="source">The log source.</param>
		/// <param name="time">Message creating time.</param>
		/// <param name="level">The level of the log message.</param>
		/// <param name="message">Text message.</param>
		/// <param name="args">Text message settings. Used if a message is the format string. For details, see <see cref="string.Format(string,object[])"/>.</param>
		public LogMessage(ILogSource source, DateTimeOffset time, LogLevels level, string message, params object[] args)
			: this(source, time, level, () => message.Put(args))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LogMessage"/>.
		/// </summary>
		/// <param name="source">The log source.</param>
		/// <param name="time">Message creating time.</param>
		/// <param name="level">The level of the log message.</param>
		/// <param name="getMessage">The function returns the text for <see cref="LogMessage.Message"/>.</param>
		public LogMessage(ILogSource source, DateTimeOffset time, LogLevels level, Func<string> getMessage)
		{
			Source = source ?? throw new ArgumentNullException(nameof(source));
			_getMessage = getMessage ?? throw new ArgumentNullException(nameof(getMessage));
			Time = time;
			Level = level;
		}

		/// <summary>
		/// The log source.
		/// </summary>
		public ILogSource Source { get; }

		/// <summary>
		/// Message creating time.
		/// </summary>
		public DateTimeOffset Time { get; set; }

		/// <summary>
		/// The level of the log message.
		/// </summary>
		public LogLevels Level { get; }

		private string _message;

		/// <summary>
		/// Message.
		/// </summary>
		public string Message
		{
			get
			{
				if (_message != null)
					return _message;

				try
				{
					_message = _getMessage();
				}
				catch (Exception ex)
				{
					_message = ex.ToString();
				}

				// делегат может захватить из внешнего кода лишние данные, что не будут удаляться GC
				// в случае, если LogMessage будет храниться где-то (например, в LogControl)
				_getMessage = null;

				return _message;
			}
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return "{0} {1}".Put(Time, Message);
		}
	}
}
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
			if (source == null)
				throw new ArgumentNullException("source");

			if (getMessage == null)
				throw new ArgumentNullException("getMessage");

			_getMessage = getMessage;

			Source = source;
			Time = time;
			Level = level;
		}

		/// <summary>
		/// The log source.
		/// </summary>
		public ILogSource Source { get; private set; }

		/// <summary>
		/// Message creating time.
		/// </summary>
		public DateTimeOffset Time { get; private set; }

		/// <summary>
		/// The level of the log message.
		/// </summary>
		public LogLevels Level { get; private set; }

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
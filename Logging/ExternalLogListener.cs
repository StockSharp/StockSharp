namespace StockSharp.Logging
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Логгер, отсылающий сообщения во внешний приемник <see cref="ILogListener"/>.
	/// </summary>
	public class ExternalLogListener : LogListener
	{
		/// <summary>
		/// Создать <see cref="ExternalLogListener"/>.
		/// </summary>
		/// <param name="logger">Внешний приемник сообщений.</param>
		public ExternalLogListener(ILogListener logger)
		{
			if (logger == null)
				throw new ArgumentNullException("logger");

			Logger = logger;
		}

		/// <summary>
		/// Внешний приемник сообщений.
		/// </summary>
		public ILogListener Logger { get; private set; }

		/// <summary>
		/// Записать сообщения.
		/// </summary>
		/// <param name="messages">Отладочные сообщения.</param>
		protected override void OnWriteMessages(IEnumerable<LogMessage> messages)
		{
			Logger.WriteMessages(messages);
		}
	}
}
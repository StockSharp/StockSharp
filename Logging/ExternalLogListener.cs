namespace StockSharp.Logging
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// The logger sending messages to the external recipient <see cref="ILogListener"/>.
	/// </summary>
	public class ExternalLogListener : LogListener
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ExternalLogListener"/>.
		/// </summary>
		/// <param name="logger">External recipient of messages.</param>
		public ExternalLogListener(ILogListener logger)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			Logger = logger;
		}

		/// <summary>
		/// External recipient of messages.
		/// </summary>
		public ILogListener Logger { get; }

		/// <summary>
		/// To record messages.
		/// </summary>
		/// <param name="messages">Debug messages.</param>
		protected override void OnWriteMessages(IEnumerable<LogMessage> messages)
		{
			Logger.WriteMessages(messages);
		}
	}
}
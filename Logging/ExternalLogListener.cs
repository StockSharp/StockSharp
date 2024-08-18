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
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <summary>
		/// External recipient of messages.
		/// </summary>
		public ILogListener Logger { get; }

		/// <inheritdoc />
		protected override void OnWriteMessages(IEnumerable<LogMessage> messages)
		{
			Logger.WriteMessages(messages);
		}
	}
}
namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;

	using Ecng.Xaml;

	using StockSharp.Logging;

	/// <summary>
	/// The logger recording data to visual components (for example, <see cref="Monitor"/> or <see cref="LogControl"/>) that require synchronization with the GUI threads when new messages are recorded <see cref="LogMessage"/>.
	/// </summary>
	public class GuiLogListener : LogListener
	{
		private readonly GuiDispatcher _dispatcher = GuiDispatcher.GlobalDispatcher;
		private readonly ILogListener _listener;

		/// <summary>
		/// Initializes a new instance of the <see cref="GuiLogListener"/>.
		/// </summary>
		/// <param name="listener">The visual component that requires synchronization with GUI threads when new messages are recorded <see cref="LogMessage"/>.</param>
		public GuiLogListener(ILogListener listener)
		{
			if (listener == null)
				throw new ArgumentNullException("listener");

			_listener = listener;
		}

		/// <summary>
		/// To record messages.
		/// </summary>
		/// <param name="messages">Debug messages.</param>
		protected override void OnWriteMessages(IEnumerable<LogMessage> messages)
		{
			_dispatcher.AddAction(() => _listener.WriteMessages(messages));
		}
	}
}
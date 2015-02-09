namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;

	using Ecng.Xaml;

	using StockSharp.Logging;

	/// <summary>
	/// Логгер, записывающий данные в визуальные компоненты (например, <see cref="Monitor"/> или <see cref="LogControl"/>),
	/// для которых требуется синхронизация с GUI потоков при записи новых сообщений <see cref="LogMessage"/>.
	/// </summary>
	public class GuiLogListener : LogListener
	{
		private readonly GuiDispatcher _dispatcher = GuiDispatcher.GlobalDispatcher;
		private readonly ILogListener _listener;

		/// <summary>
		/// Создать <see cref="GuiLogListener"/>.
		/// </summary>
		/// <param name="listener">Визуальный компонент, для которого требуется синхронизация с GUI потоков при записи новых сообщений <see cref="LogMessage"/>.</param>
		public GuiLogListener(ILogListener listener)
		{
			if (listener == null)
				throw new ArgumentNullException("listener");

			_listener = listener;
		}

		/// <summary>
		/// Записать сообщения.
		/// </summary>
		/// <param name="messages">Отладочные сообщения.</param>
		protected override void OnWriteMessages(IEnumerable<LogMessage> messages)
		{
			_dispatcher.AddAction(() => _listener.WriteMessages(messages));
		}
	}
}
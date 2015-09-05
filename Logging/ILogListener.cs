namespace StockSharp.Logging
{
	using System;
	using System.Collections.Generic;

	using Ecng.Serialization;

	/// <summary>
	/// Интерфейс класса, который мониторит событие <see cref="ILogSource.Log"/> и сохраняет в некое хранилище.
	/// </summary>
	public interface ILogListener : IPersistable, IDisposable
	{
		/// <summary>
		/// Записать сообщения.
		/// </summary>
		/// <param name="messages">Отладочные сообщения.</param>
		void WriteMessages(IEnumerable<LogMessage> messages);
	}
}
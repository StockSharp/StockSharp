namespace StockSharp.Logging
{
	using System;
	using System.Collections.Generic;

	using Ecng.Serialization;

	/// <summary>
	/// The class interface that monitors the event <see cref="ILogSource.Log"/> and saves to some storage.
	/// </summary>
	public interface ILogListener : IPersistable, IDisposable
	{
		/// <summary>
		/// To record messages.
		/// </summary>
		/// <param name="messages">Debug messages.</param>
		void WriteMessages(IEnumerable<LogMessage> messages);
	}
}
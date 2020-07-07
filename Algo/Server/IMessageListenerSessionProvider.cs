namespace StockSharp.Algo.Server
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// The <see cref="IMessageListenerSession"/> provider interface.
	/// </summary>
	public interface IMessageListenerSessionProvider
	{
		/// <summary>
		/// Get all portfolios.
		/// </summary>
		IEnumerable<IMessageListenerSession> Sessions { get; }

		/// <summary>
		/// Sessions list changed.
		/// </summary>
		event Action SessionsChanged;
	}
}

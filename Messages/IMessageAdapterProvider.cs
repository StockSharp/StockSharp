namespace StockSharp.Messages
{
	using System.Collections.Generic;

	/// <summary>
	/// The message adapter's provider interface. 
	/// </summary>
	public interface IMessageAdapterProvider
	{
		/// <summary>
		/// All currently available adapters.
		/// </summary>
		IEnumerable<IMessageAdapter> CurrentAdapters { get; }

		/// <summary>
		/// All possible adapters.
		/// </summary>
		IEnumerable<IMessageAdapter> PossibleAdapters { get; }
	}
}
namespace StockSharp.Messages
{
	using System.Collections.Generic;

	/// <summary>
	/// The message adapter's provider interface. 
	/// </summary>
	public interface IMessageAdapterProvider
	{
		/// <summary>
		/// All available adapters.
		/// </summary>
		IEnumerable<IMessageAdapter> Adapters { get; }
	}
}
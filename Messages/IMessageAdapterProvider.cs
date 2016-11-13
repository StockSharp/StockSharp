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

		/// <summary>
		/// Get adapter by portfolio name.
		/// </summary>
		/// <param name="portfolioName">Portfolio name.</param>
		/// <returns>The found adapter.</returns>
		IMessageAdapter GetAdapter(string portfolioName);

		/// <summary>
		/// Make association adapter and portfolio name.
		/// </summary>
		/// <param name="portfolioName">Portfolio name.</param>
		/// <param name="adapter">The adapter.</param>
		void SetAdapter(string portfolioName, IMessageAdapter adapter);
	}
}
namespace StockSharp.Messages
{
	using System.Collections.Generic;

	/// <summary>
	/// Interface describing exchanges and trading boards provider.
	/// </summary>
	public interface IBoardMessageProvider
	{
		/// <summary>
		/// Filter boards by code criteria.
		/// </summary>
		/// <param name="criteria">Criteria.</param>
		/// <returns>Found boards.</returns>
		IEnumerable<BoardMessage> Lookup(BoardLookupMessage criteria);
	}
}
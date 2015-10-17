namespace StockSharp.Algo.Storages
{
	using System.Collections.Generic;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface for access to the storage of information on instruments.
	/// </summary>
	public interface ISecurityStorage : ISecurityProvider
	{
		/// <summary>
		/// Save security.
		/// </summary>
		/// <param name="security">Security.</param>
		void Save(Security security);

		/// <summary>
		/// Delete security.
		/// </summary>
		/// <param name="security">Security.</param>
		void Delete(Security security);

		/// <summary>
		/// To delete instruments by the criterion.
		/// </summary>
		/// <param name="criteria">The criterion.</param>
		void DeleteBy(Security criteria);

		/// <summary>
		/// To get identifiers of saved instruments.
		/// </summary>
		/// <returns>IDs securities.</returns>
		IEnumerable<string> GetSecurityIds();
	}
}
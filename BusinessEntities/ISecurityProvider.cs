namespace StockSharp.BusinessEntities
{
	using System.Collections.Generic;

	/// <summary>
	/// The interface for access to provider of information about instruments.
	/// </summary>
	public interface ISecurityProvider
	{
		/// <summary>
		/// Lookup securities by criteria <paramref name="criteria" />.
		/// </summary>
		/// <param name="criteria">The instrument whose fields will be used as a filter.</param>
		/// <returns>Found instruments.</returns>
		IEnumerable<Security> Lookup(Security criteria);

		/// <summary>
		/// Get native id.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Native (internal) trading system security id.</returns>
		object GetNativeId(Security security);
	}
}
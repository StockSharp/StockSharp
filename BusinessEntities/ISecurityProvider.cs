namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// The interface for access to provider of information about instruments.
	/// </summary>
	public interface ISecurityProvider : IDisposable
	{
		/// <summary>
		/// Gets the number of instruments contained in the <see cref="ISecurityProvider"/>.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// New instrument created.
		/// </summary>
		event Action<Security> Added;

		/// <summary>
		/// Instrument deleted.
		/// </summary>
		event Action<Security> Removed;

		/// <summary>
		/// The storage was cleared.
		/// </summary>
		event Action Cleared;

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
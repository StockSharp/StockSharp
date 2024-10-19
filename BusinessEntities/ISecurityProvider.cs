namespace StockSharp.BusinessEntities;

/// <summary>
/// The interface for access to provider of information about instruments.
/// </summary>
public interface ISecurityProvider : ISecurityMessageProvider
{
	/// <summary>
	/// Gets the number of instruments contained in the <see cref="ISecurityProvider"/>.
	/// </summary>
	int Count { get; }

	/// <summary>
	/// New instruments added.
	/// </summary>
	event Action<IEnumerable<Security>> Added;

	/// <summary>
	/// Instruments removed.
	/// </summary>
	event Action<IEnumerable<Security>> Removed;

	/// <summary>
	/// The storage was cleared.
	/// </summary>
	event Action Cleared;

	/// <summary>
	/// To get the instrument by the identifier.
	/// </summary>
	/// <param name="id">Security ID.</param>
	/// <returns>The got instrument. If there is no instrument by given criteria, <see langword="null" /> is returned.</returns>
	Security LookupById(SecurityId id);

	/// <summary>
	/// Lookup securities by criteria <paramref name="criteria" />.
	/// </summary>
	/// <param name="criteria">Message security lookup for specified criteria.</param>
	/// <returns>Found instruments.</returns>
	IEnumerable<Security> Lookup(SecurityLookupMessage criteria);
}
namespace StockSharp.BusinessEntities;

using System.Threading;
using System.Threading.Tasks;

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
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>The got instrument. If there is no instrument by given criteria, <see langword="null" /> is returned.</returns>
	ValueTask<Security> LookupByIdAsync(SecurityId id, CancellationToken cancellationToken);

	/// <summary>
	/// Lookup securities by criteria <paramref name="criteria" />.
	/// </summary>
	/// <param name="criteria">Message security lookup for specified criteria.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Found instruments.</returns>
	IAsyncEnumerable<Security> LookupAsync(SecurityLookupMessage criteria, CancellationToken cancellationToken);
}
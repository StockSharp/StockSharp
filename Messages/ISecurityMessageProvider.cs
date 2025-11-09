namespace StockSharp.Messages;

/// <summary>
/// The interface for access to provider of information about instruments.
/// </summary>
public interface ISecurityMessageProvider
{
	/// <summary>
	/// To get the instrument by the identifier.
	/// </summary>
	/// <param name="id">Security ID.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>The got instrument. If there is no instrument by given criteria, <see langword="null" /> is returned.</returns>
	ValueTask<SecurityMessage> LookupMessageByIdAsync(SecurityId id, CancellationToken cancellationToken);

	/// <summary>
	/// Lookup securities by criteria <paramref name="criteria" />.
	/// </summary>
	/// <param name="criteria">Message security lookup for specified criteria.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Found instruments.</returns>
	IAsyncEnumerable<SecurityMessage> LookupMessagesAsync(SecurityLookupMessage criteria, CancellationToken cancellationToken);
}
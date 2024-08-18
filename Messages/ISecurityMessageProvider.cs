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
	/// <returns>The got instrument. If there is no instrument by given criteria, <see langword="null" /> is returned.</returns>
	SecurityMessage LookupMessageById(SecurityId id);

	/// <summary>
	/// Lookup securities by criteria <paramref name="criteria" />.
	/// </summary>
	/// <param name="criteria">Message security lookup for specified criteria.</param>
	/// <returns>Found instruments.</returns>
	IEnumerable<SecurityMessage> LookupMessages(SecurityLookupMessage criteria);
}
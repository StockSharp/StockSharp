namespace StockSharp.Messages;

/// <summary>
/// The interface describing a message that carries the numeric identity of its owner —
/// the owning user and portfolio primary keys. Carried on the message itself (and copied
/// by <c>CopyTo</c>) so the identity survives cloning and travels with the message through
/// the pipeline without an external side table.
/// </summary>
public interface IOwnerMessage
{
	/// <summary>
	/// Numeric primary key of the owning user, or 0 when unknown.
	/// </summary>
	long OwnerUserId { get; set; }

	/// <summary>
	/// Numeric primary key of the owning portfolio, or 0 when unknown.
	/// </summary>
	long OwnerPortfolioId { get; set; }
}

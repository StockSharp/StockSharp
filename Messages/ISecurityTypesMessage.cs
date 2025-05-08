namespace StockSharp.Messages;

/// <summary>
/// The interface for the message containing the securities types.
/// </summary>
public interface ISecurityTypesMessage
{
	/// <summary>
	/// Security type.
	/// </summary>
	SecurityTypes? SecurityType { get; set; }

	/// <summary>
	/// Securities types.
	/// </summary>
	SecurityTypes[] SecurityTypes { get; set; }
}

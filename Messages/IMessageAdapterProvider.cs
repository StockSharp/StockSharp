namespace StockSharp.Messages;

using System.Security;

/// <summary>
/// The message adapter's provider interface.
/// </summary>
public interface IMessageAdapterProvider
{
	/// <summary>
	/// All currently available adapters.
	/// </summary>
	IEnumerable<IMessageAdapter> CurrentAdapters { get; }

	/// <summary>
	/// All possible adapters.
	/// </summary>
	IEnumerable<IMessageAdapter> PossibleAdapters { get; }

	/// <summary>
	/// Create adapter for client-server communication.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	/// <returns>Message adapter.</returns>
	IMessageAdapter CreateTransportAdapter(IdGenerator transactionIdGenerator);

	/// <summary>
	/// Create adapters for StockSharp server connections.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	/// <param name="login">Login.</param>
	/// <param name="password">Password.</param>
	/// <returns>Adapters for StockSharp server connections.</returns>
	IEnumerable<IMessageAdapter> CreateStockSharpAdapters(IdGenerator transactionIdGenerator, string login, SecureString password);
}
namespace StockSharp.Messages;

/// <summary>
/// Describes an asynchronous message adapter capable of processing incoming messages
/// and performing common adapter operations asynchronously.
/// </summary>
public interface IAsyncMessageAdapter : IMessageAdapter
{
	/// <summary>
	/// Gets the timeout to wait for a graceful disconnect.
	/// </summary>
	TimeSpan DisconnectTimeout { get; }

	/// <summary>
	/// Gets or sets the maximum number of parallel (non-control) messages that can be processed.
	/// Must be greater than or equal to1.
	/// </summary>
	int MaxParallelMessages { get; set; }

	/// <summary>
	/// Gets or sets the delay applied between faulted iterations.
	/// </summary>
	TimeSpan FaultDelay { get; set; }

	/// <summary>
	/// Processes a connect request asynchronously.
	/// </summary>
	/// <param name="connectMsg">The connect message to process.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask ConnectAsync(ConnectMessage connectMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Processes a disconnect request asynchronously.
	/// </summary>
	/// <param name="disconnectMsg">The disconnect message to process.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask DisconnectAsync(DisconnectMessage disconnectMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Processes a reset command asynchronously. Implementations MUST NOT throw.
	/// </summary>
	/// <param name="resetMsg">The reset message to process.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask ResetAsync(ResetMessage resetMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Processes a change password request asynchronously.
	/// </summary>
	/// <param name="pwdMsg">The change password message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask ChangePasswordAsync(ChangePasswordMessage pwdMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Processes a security lookup request asynchronously.
	/// </summary>
	/// <param name="lookupMsg">The security lookup message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask SecurityLookupAsync(SecurityLookupMessage lookupMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Processes a portfolio lookup request asynchronously.
	/// </summary>
	/// <param name="lookupMsg">The portfolio lookup message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask PortfolioLookupAsync(PortfolioLookupMessage lookupMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Processes a board lookup request asynchronously.
	/// </summary>
	/// <param name="lookupMsg">The board lookup message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask BoardLookupAsync(BoardLookupMessage lookupMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Processes an order status request asynchronously.
	/// </summary>
	/// <param name="statusMsg">The order status message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask OrderStatusAsync(OrderStatusMessage statusMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Processes an order registration asynchronously.
	/// </summary>
	/// <param name="regMsg">The order register message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask RegisterOrderAsync(OrderRegisterMessage regMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Processes an order replace request asynchronously.
	/// </summary>
	/// <param name="replaceMsg">The order replace message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask ReplaceOrderAsync(OrderReplaceMessage replaceMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Processes an order cancellation asynchronously.
	/// </summary>
	/// <param name="cancelMsg">The order cancel message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask CancelOrderAsync(OrderCancelMessage cancelMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Processes an order group cancellation asynchronously.
	/// </summary>
	/// <param name="cancelMsg">The order group cancel message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask CancelOrderGroupAsync(OrderGroupCancelMessage cancelMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Processes a time message asynchronously.
	/// </summary>
	/// <param name="timeMsg">The time message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask TimeAsync(TimeMessage timeMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Processes a market data message asynchronously (subscribe/unsubscribe or history retrieval).
	/// </summary>
	/// <param name="mdMsg">The market data message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask MarketDataAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Processes a generic message asynchronously.
	/// </summary>
	/// <param name="msg">The message to process.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask SendInMessageAsync(Message msg, CancellationToken cancellationToken);
}
using System;
using System.Threading;
using System.Threading.Tasks;

using StockSharp.Logging;

namespace StockSharp.Messages;

/// <summary>
/// Message adapter with async processing support.
/// </summary>
public interface IAsyncMessageAdapter : ILogReceiver
{
	private static readonly TimeSpan _defaultDisconnectTimeout = TimeSpan.FromSeconds(5);
	private static readonly TimeSpan _defaultTransactionTimeout = TimeSpan.FromSeconds(10);

	/// <summary>
	/// Disconnect timeout.
	/// </summary>
	TimeSpan DisconnectTimeout => _defaultDisconnectTimeout;

	/// <summary>
	/// Transaction timeout.
	/// </summary>
	TimeSpan TransactionTimeout => _defaultTransactionTimeout;

	/// <summary>
	/// Process <see cref="ConnectMessage"/>.
	/// </summary>
	/// <param name="connectMsg"><see cref="ConnectMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	ValueTask ConnectAsync(ConnectMessage connectMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Process <see cref="DisconnectMessage"/>.
	/// </summary>
	/// <param name="disconnectMsg"><see cref="DisconnectMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	ValueTask DisconnectAsync(DisconnectMessage disconnectMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Process <see cref="ResetMessage"/>.
	/// </summary>
	/// <remarks>
	/// Must NOT throw.
	/// </remarks>
	/// <param name="resetMsg"><see cref="ResetMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	ValueTask ResetAsync(ResetMessage resetMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Process <see cref="SecurityLookupMessage"/>.
	/// </summary>
	/// <param name="lookupMsg"><see cref="SecurityLookupMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	ValueTask SecurityLookupAsync(SecurityLookupMessage lookupMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Process <see cref="PortfolioLookupMessage"/>.
	/// </summary>
	/// <param name="lookupMsg"><see cref="PortfolioLookupMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	ValueTask PortfolioLookupAsync(PortfolioLookupMessage lookupMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Process <see cref="BoardLookupMessage"/>.
	/// </summary>
	/// <param name="lookupMsg"><see cref="BoardLookupMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	ValueTask BoardLookupAsync(BoardLookupMessage lookupMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Process <see cref="OrderStatusMessage"/>.
	/// </summary>
	/// <param name="statusMsg"><see cref="OrderStatusMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	ValueTask OrderStatusAsync(OrderStatusMessage statusMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Process <see cref="OrderRegisterMessage"/>.
	/// </summary>
	/// <param name="regMsg"><see cref="OrderRegisterMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	ValueTask RegisterOrderAsync(OrderRegisterMessage regMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Process <see cref="OrderReplaceMessage"/>.
	/// </summary>
	/// <param name="replaceMsg"><see cref="OrderReplaceMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	ValueTask ReplaceOrderAsync(OrderReplaceMessage replaceMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Process <see cref="OrderPairReplaceMessage"/>.
	/// </summary>
	/// <param name="replaceMsg"><see cref="OrderPairReplaceMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	ValueTask ReplaceOrderPairAsync(OrderPairReplaceMessage replaceMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Process <see cref="OrderCancelMessage"/>.
	/// </summary>
	/// <param name="cancelMsg"><see cref="OrderCancelMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	ValueTask CancelOrderAsync(OrderCancelMessage cancelMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Process <see cref="OrderGroupCancelMessage"/>.
	/// </summary>
	/// <param name="cancelMsg"><see cref="OrderGroupCancelMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	ValueTask CancelOrderGroupAsync(OrderGroupCancelMessage cancelMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Process <see cref="MarketDataMessage"/>.
	/// </summary>
	/// <param name="mdMsg"><see cref="MarketDataMessage"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	ValueTask RunSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken);

	/// <summary>
	/// Process <see cref="Message"/>.
	/// </summary>
	/// <param name="msg"><see cref="Message"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	ValueTask ProcessMessageAsync(Message msg, CancellationToken cancellationToken);

	/// <summary>
	/// Handle error associated with the specified message.
	/// </summary>
	/// <param name="msg"><see cref="Message"/>.</param>
	/// <param name="err"><see cref="Exception"/>.</param>
	void HandleMessageException(Message msg, Exception err);
}
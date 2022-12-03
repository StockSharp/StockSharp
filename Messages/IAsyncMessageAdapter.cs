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
	/// </summary>
	TimeSpan DisconnectTimeout => _defaultDisconnectTimeout;

	/// <summary>
	/// </summary>
	TimeSpan TransactionTimeout => _defaultTransactionTimeout;

	/// <summary>
	/// </summary>
	ValueTask ConnectAsync(ConnectMessage msg, CancellationToken token);

	/// <summary>
	/// </summary>
	ValueTask DisconnectAsync(DisconnectMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <summary>
	/// Reset adapter. Must NOT throw.
	/// </summary>
	ValueTask ResetAsync(ResetMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <summary>
	/// </summary>
	ValueTask SecurityLookupAsync(SecurityLookupMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <summary>
	/// </summary>
	ValueTask PortfolioLookupAsync(PortfolioLookupMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <summary>
	/// </summary>
	ValueTask BoardLookupAsync(BoardLookupMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <summary>
	/// </summary>
	ValueTask OrderStatusAsync(OrderStatusMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <summary>
	/// </summary>
	ValueTask RegisterOrderAsync(OrderRegisterMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <summary>
	/// </summary>
	ValueTask ReplaceOrderAsync(OrderReplaceMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <summary>
	/// </summary>
	ValueTask ReplaceOrderPairAsync(OrderPairReplaceMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <summary>
	/// </summary>
	ValueTask CancelOrderAsync(OrderCancelMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <summary>
	/// </summary>
	ValueTask CancelOrderGroupAsync(OrderGroupCancelMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <summary>
	/// </summary>
	ValueTask RunSubscriptionAsync(MarketDataMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <summary>
	/// </summary>
	ValueTask ProcessMessageAsync(Message msg, CancellationToken token) => default; // do nothing by default

	/// <summary>
	/// </summary>
	void HandleMessageException(Message msg, Exception err);
}
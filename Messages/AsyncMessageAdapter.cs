using System;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

namespace StockSharp.Messages;

/// <summary>
/// Default implementation <see cref="IAsyncMessageAdapter"/>.
/// </summary>
public abstract class AsyncMessageAdapter : MessageAdapter, IAsyncMessageAdapter
{
	private readonly AsyncMessageProcessor _asyncMessageProcessor;

	/// <summary>
	/// Initialize <see cref="AsyncMessageAdapter"/>.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	protected AsyncMessageAdapter(IdGenerator transactionIdGenerator)
		: base(transactionIdGenerator)
	{
		_asyncMessageProcessor = new AsyncMessageProcessor(this);
	}

	/// <inheritdoc />
	public virtual TimeSpan TransactionTimeout { get; } = TimeSpan.FromSeconds(10);

	/// <inheritdoc />
	public virtual TimeSpan DisconnectTimeout { get; } = TimeSpan.FromSeconds(5);

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
		=> _asyncMessageProcessor.EnqueueMessage(message);

	/// <inheritdoc />
	protected virtual void OnHandleMessageException(Message msg, Exception err)
		=> msg.HandleErrorResponse(err, this, SendOutMessage);

	/// <inheritdoc />
	protected virtual ValueTask OnConnectAsync(ConnectMessage connectMsg, CancellationToken cancellationToken)
		=> OnProcessMessageAsync(connectMsg, cancellationToken);

	/// <inheritdoc />
	protected virtual ValueTask OnDisconnectAsync(DisconnectMessage disconnectMsg, CancellationToken cancellationToken)
		=> OnProcessMessageAsync(disconnectMsg, cancellationToken);

	/// <inheritdoc />
	protected virtual ValueTask OnResetAsync(ResetMessage resetMsg, CancellationToken cancellationToken)
		=> OnProcessMessageAsync(resetMsg, cancellationToken);

	/// <inheritdoc />
	protected virtual ValueTask OnSecurityLookupAsync(SecurityLookupMessage lookupMsg, CancellationToken cancellationToken)
		=> OnProcessMessageAsync(lookupMsg, cancellationToken);

	/// <inheritdoc />
	protected virtual ValueTask OnPortfolioLookupAsync(PortfolioLookupMessage lookupMsg, CancellationToken cancellationToken)
		=> OnProcessMessageAsync(lookupMsg, cancellationToken);

	/// <inheritdoc />
	protected virtual ValueTask OnBoardLookupAsync(BoardLookupMessage lookupMsg, CancellationToken cancellationToken)
		=> OnProcessMessageAsync(lookupMsg, cancellationToken);

	/// <inheritdoc />
	protected virtual ValueTask OnOrderStatusAsync(OrderStatusMessage statusMsg, CancellationToken cancellationToken)
		=> OnProcessMessageAsync(statusMsg, cancellationToken);

	/// <inheritdoc />
	protected virtual ValueTask OnRegisterOrderAsync(OrderRegisterMessage regMsg, CancellationToken cancellationToken)
		=> OnProcessMessageAsync(regMsg, cancellationToken);

	/// <inheritdoc />
	protected virtual ValueTask OnReplaceOrderAsync(OrderReplaceMessage replaceMsg, CancellationToken cancellationToken)
		=> OnProcessMessageAsync(replaceMsg, cancellationToken);

	/// <inheritdoc />
	protected virtual ValueTask OnReplaceOrderPairAsync(OrderPairReplaceMessage replaceMsg, CancellationToken cancellationToken)
		=> OnProcessMessageAsync(replaceMsg, cancellationToken);

	/// <inheritdoc />
	protected virtual ValueTask OnCancelOrderAsync(OrderCancelMessage cancelMsg, CancellationToken cancellationToken)
		=> OnProcessMessageAsync(cancelMsg, cancellationToken);

	/// <inheritdoc />
	protected virtual ValueTask OnCancelOrderGroupAsync(OrderGroupCancelMessage cancelMsg, CancellationToken cancellationToken)
		=> OnProcessMessageAsync(cancelMsg, cancellationToken);

	/// <inheritdoc />
	protected virtual ValueTask OnRunSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> OnProcessMessageAsync(mdMsg, cancellationToken);

	/// <inheritdoc />
	protected virtual ValueTask OnProcessMessageAsync(Message msg, CancellationToken cancellationToken)
		=> default;

	ValueTask IAsyncMessageAdapter.ConnectAsync(ConnectMessage connectMsg, CancellationToken cancellationToken)
		=> OnConnectAsync(connectMsg, cancellationToken);

	ValueTask IAsyncMessageAdapter.DisconnectAsync(DisconnectMessage disconnectMsg, CancellationToken cancellationToken)
		=> OnDisconnectAsync(disconnectMsg, cancellationToken);

	ValueTask IAsyncMessageAdapter.ResetAsync(ResetMessage resetMsg, CancellationToken cancellationToken)
		=> OnResetAsync(resetMsg, cancellationToken);

	ValueTask IAsyncMessageAdapter.SecurityLookupAsync(SecurityLookupMessage lookupMsg, CancellationToken cancellationToken)
		=> OnSecurityLookupAsync(lookupMsg, cancellationToken);

	ValueTask IAsyncMessageAdapter.PortfolioLookupAsync(PortfolioLookupMessage lookupMsg, CancellationToken cancellationToken)
		=> OnPortfolioLookupAsync(lookupMsg, cancellationToken);

	ValueTask IAsyncMessageAdapter.BoardLookupAsync(BoardLookupMessage lookupMsg, CancellationToken cancellationToken)
		=> OnBoardLookupAsync(lookupMsg, cancellationToken);

	ValueTask IAsyncMessageAdapter.OrderStatusAsync(OrderStatusMessage statusMsg, CancellationToken cancellationToken)
		=> OnOrderStatusAsync(statusMsg, cancellationToken);

	ValueTask IAsyncMessageAdapter.RegisterOrderAsync(OrderRegisterMessage regMsg, CancellationToken cancellationToken)
		=> OnRegisterOrderAsync(regMsg, cancellationToken);

	ValueTask IAsyncMessageAdapter.ReplaceOrderAsync(OrderReplaceMessage replaceMsg, CancellationToken cancellationToken)
		=> OnReplaceOrderAsync(replaceMsg, cancellationToken);

	ValueTask IAsyncMessageAdapter.ReplaceOrderPairAsync(OrderPairReplaceMessage replaceMsg, CancellationToken cancellationToken)
		=> OnReplaceOrderPairAsync(replaceMsg, cancellationToken);

	ValueTask IAsyncMessageAdapter.CancelOrderAsync(OrderCancelMessage cancelMsg, CancellationToken cancellationToken)
		=> OnCancelOrderAsync(cancelMsg, cancellationToken);

	ValueTask IAsyncMessageAdapter.CancelOrderGroupAsync(OrderGroupCancelMessage cancelMsg, CancellationToken cancellationToken)
		=> OnCancelOrderGroupAsync(cancelMsg, cancellationToken);

	ValueTask IAsyncMessageAdapter.RunSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> OnRunSubscriptionAsync(mdMsg, cancellationToken);

	ValueTask IAsyncMessageAdapter.ProcessMessageAsync(Message msg, CancellationToken cancellationToken)
		=> OnProcessMessageAsync(msg, cancellationToken);

	void IAsyncMessageAdapter.HandleMessageException(Message msg, Exception err)
		=> OnHandleMessageException(msg, err);
}
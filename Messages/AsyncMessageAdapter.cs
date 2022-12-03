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
	/// Initialize <see cref="MessageAdapter"/>.
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
	public virtual void HandleMessageException(Message msg, Exception err)
		=> msg.HandleErrorResponse(err, this, SendOutMessage);

	/// <inheritdoc />
	public virtual ValueTask ConnectAsync(ConnectMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <inheritdoc />
	public virtual ValueTask DisconnectAsync(DisconnectMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <inheritdoc />
	public virtual ValueTask ResetAsync(ResetMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <inheritdoc />
	public virtual ValueTask SecurityLookupAsync(SecurityLookupMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <inheritdoc />
	public virtual ValueTask PortfolioLookupAsync(PortfolioLookupMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <inheritdoc />
	public virtual ValueTask BoardLookupAsync(BoardLookupMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <inheritdoc />
	public virtual ValueTask OrderStatusAsync(OrderStatusMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <inheritdoc />
	public virtual ValueTask RegisterOrderAsync(OrderRegisterMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <inheritdoc />
	public virtual ValueTask ReplaceOrderAsync(OrderReplaceMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <inheritdoc />
	public virtual ValueTask ReplaceOrderPairAsync(OrderPairReplaceMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <inheritdoc />
	public virtual ValueTask CancelOrderAsync(OrderCancelMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <inheritdoc />
	public virtual ValueTask CancelOrderGroupAsync(OrderGroupCancelMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <inheritdoc />
	public virtual ValueTask RunSubscriptionAsync(MarketDataMessage msg, CancellationToken token)
		=> ProcessMessageAsync(msg, token);

	/// <inheritdoc />
	public virtual ValueTask ProcessMessageAsync(Message msg, CancellationToken token)
		=> default;
}
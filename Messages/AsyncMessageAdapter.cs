using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Collections;
using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Logging;
using StockSharp.Localization;

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
	[Browsable(false)]
	public virtual TimeSpan TransactionTimeout { get; } = TimeSpan.FromSeconds(10);

	/// <inheritdoc />
	[Browsable(false)]
	public virtual TimeSpan DisconnectTimeout { get; } = TimeSpan.FromSeconds(5);

	/// <inheritdoc />
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ParallelKey,
		Description = LocalizedStrings.ParallelDescKey,
		GroupName = LocalizedStrings.Str186Key,
		Order = 310)]
	public int MaxParallelMessages { get; set; } = IAsyncMessageAdapter.DefaultMaxParallelMessages;

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
	protected virtual ValueTask OnTimeMessageAsync(TimeMessage timeMsg, CancellationToken cancellationToken)
		=> OnProcessMessageAsync(timeMsg, cancellationToken);

	/// <inheritdoc />
	protected virtual ValueTask RunSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		var dataType = mdMsg.DataType2;

		return
			  dataType == DataType.News         ? OnNewsSubscriptionAsync(mdMsg, cancellationToken)
			: dataType == DataType.Level1       ? OnLevel1SubscriptionAsync(mdMsg, cancellationToken)
			: dataType == DataType.Ticks        ? OnTicksSubscriptionAsync(mdMsg, cancellationToken)
			: dataType == DataType.MarketDepth  ? OnMarketDepthSubscriptionAsync(mdMsg, cancellationToken)
			: dataType == DataType.OrderLog     ? OnOrderLogSubscriptionAsync(mdMsg, cancellationToken)
			: dataType.IsTFCandles              ? OnTFCandlesSubscriptionAsync(mdMsg, cancellationToken)
			: throw SubscriptionResponseMessage.NotSupported;
	}

	/// <summary>
	/// </summary>
	protected virtual ValueTask OnNewsSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <summary>
	/// </summary>
	protected virtual ValueTask OnLevel1SubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <summary>
	/// </summary>
	protected virtual ValueTask OnTicksSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <summary>
	/// </summary>
	protected virtual ValueTask OnMarketDepthSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <summary>
	/// </summary>
	protected virtual ValueTask OnOrderLogSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;

	/// <summary>
	/// </summary>
	protected virtual ValueTask OnTFCandlesSubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		=> throw SubscriptionResponseMessage.NotSupported;


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

	ValueTask IAsyncMessageAdapter.TimeMessageAsync(TimeMessage timeMsg, CancellationToken cancellationToken)
		=> OnTimeMessageAsync(timeMsg, cancellationToken);

	ValueTask IAsyncMessageAdapter.ProcessMessageAsync(Message msg, CancellationToken cancellationToken)
		=> OnProcessMessageAsync(msg, cancellationToken);

	private readonly SynchronizedDictionary<long, Task> _marketDataTasks = new();

	ValueTask IAsyncMessageAdapter.ProcessMarketDataAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		if (!mdMsg.IsSubscribe)
		{
			var subTask = _marketDataTasks.TryGetValue(mdMsg.OriginalTransactionId);
			_asyncMessageProcessor.TryCancelChildTokenByTransId(mdMsg.OriginalTransactionId);

			if (subTask == null)
			{
				this.AddVerboseLog($"subscription not found: {mdMsg.OriginalTransactionId}");
				SendSubscriptionReply(mdMsg.TransactionId);
				return default;
			}

			return new(subTask.ContinueWith(_ => SendSubscriptionReply(mdMsg.TransactionId), TaskContinuationOptions.ExecuteSynchronously));
		}

		lock (_marketDataTasks.SyncRoot)
		{
			var task = runSub();

			if(!task.IsCompleted)
				_marketDataTasks[mdMsg.TransactionId] = task.AsTask();

			return task;
		}

		void trySendFinished(bool checkSupported)
		{
			if(mdMsg.IsHistoryOnly() && (!this.IsOutMessageSupported(MessageTypes.SubscriptionFinished) || !checkSupported))
				SendSubscriptionFinished(mdMsg.TransactionId);
		}

		async ValueTask runSub()
		{
			var childToken = _asyncMessageProcessor.CreateChildTokenByTransId(mdMsg.TransactionId, cancellationToken);

			try
			{
				await RunSubscriptionAsync(mdMsg, childToken);
				trySendFinished(true);
			}
			catch (OperationCanceledException)
			{
				if (!childToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
					throw;

				trySendFinished(false);
			}
			finally
			{
				_asyncMessageProcessor.RemoveChildToken(mdMsg.TransactionId);
				_marketDataTasks.Remove(mdMsg.TransactionId);
			}
		}
	}

	void IAsyncMessageAdapter.HandleMessageException(Message msg, Exception err)
		=> OnHandleMessageException(msg, err);

	/// <inheritdoc />
	public override TimeSpan GetHistoryStepSize(DataType dataType, out TimeSpan iterationInterval)
	{
		iterationInterval = TimeSpan.Zero;
		return TimeSpan.MaxValue;
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.Set(nameof(MaxParallelMessages), MaxParallelMessages);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		MaxParallelMessages = storage.GetValue(nameof(MaxParallelMessages), MaxParallelMessages);
	}
}

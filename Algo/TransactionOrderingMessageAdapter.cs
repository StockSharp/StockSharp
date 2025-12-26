namespace StockSharp.Algo;

/// <summary>
/// Transactional messages ordering adapter.
/// </summary>
public class TransactionOrderingMessageAdapter : MessageAdapterWrapper
{
	private readonly ITransactionOrderingManager _manager;

	/// <summary>
	/// Initializes a new instance of the <see cref="TransactionOrderingMessageAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">Inner message adapter.</param>
	public TransactionOrderingMessageAdapter(IMessageAdapter innerAdapter)
		: base(innerAdapter)
	{
		_manager = new TransactionOrderingManager(this, () => IsSupportTransactionLog);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TransactionOrderingMessageAdapter"/> with a custom manager.
	/// </summary>
	/// <param name="innerAdapter">Inner message adapter.</param>
	/// <param name="manager">Transaction ordering manager.</param>
	public TransactionOrderingMessageAdapter(IMessageAdapter innerAdapter, ITransactionOrderingManager manager)
		: base(innerAdapter)
	{
		_manager = manager ?? throw new ArgumentNullException(nameof(manager));
	}

	/// <inheritdoc />
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var (toInner, toOut) = _manager.ProcessInMessage(message);

		if (toInner.Length > 0)
		{
			foreach (var msg in toInner)
				await base.OnSendInMessageAsync(msg, cancellationToken);
		}

		if (toOut.Length > 0)
		{
			foreach (var msg in toOut)
				await RaiseNewOutMessageAsync(msg, cancellationToken);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var (forward, extraOut, processSuspended) = _manager.ProcessOutMessage(message);

		if (forward != null)
			await base.OnInnerAdapterNewOutMessageAsync(forward, cancellationToken);

		if (extraOut.Length > 0)
		{
			foreach (var msg in extraOut)
			{
				await base.OnInnerAdapterNewOutMessageAsync(msg, cancellationToken);

				// Process suspended trades after each order message
				if (msg is ExecutionMessage execMsg && execMsg.HasOrderInfo)
					await ProcessSuspendedAsync(execMsg, cancellationToken);
			}
		}

		if (processSuspended && message is ExecutionMessage execMessage)
			await ProcessSuspendedAsync(execMessage, cancellationToken);
	}

	private async ValueTask ProcessSuspendedAsync(ExecutionMessage execMsg, CancellationToken cancellationToken)
	{
		var suspendedTrades = _manager.GetSuspendedTrades(execMsg);

		foreach (var trade in suspendedTrades)
			await RaiseNewOutMessageAsync(trade, cancellationToken);
	}

	/// <summary>
	/// Create a copy of <see cref="TransactionOrderingMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
	{
		return new TransactionOrderingMessageAdapter(InnerAdapter.TypedClone());
	}
}

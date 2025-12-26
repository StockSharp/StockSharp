namespace StockSharp.Algo;

/// <summary>
/// Subscription counter adapter.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SubscriptionMessageAdapter"/>.
/// </remarks>
public class SubscriptionMessageAdapter : MessageAdapterWrapper
{
	private readonly ISubscriptionManager _manager;

	/// <summary>
	/// Initializes a new instance of the <see cref="SubscriptionMessageAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">Inner message adapter.</param>
	public SubscriptionMessageAdapter(IMessageAdapter innerAdapter)
		: base(innerAdapter)
	{
		_manager = new SubscriptionManager(this, TransactionIdGenerator, () => new ProcessSuspendedMessage(this));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SubscriptionMessageAdapter"/> with a custom manager.
	/// </summary>
	/// <param name="innerAdapter">Inner message adapter.</param>
	/// <param name="manager">Subscription manager.</param>
	public SubscriptionMessageAdapter(IMessageAdapter innerAdapter, ISubscriptionManager manager)
		: base(innerAdapter)
	{
		_manager = manager ?? throw new ArgumentNullException(nameof(manager));
	}

	/// <summary>
	/// Restore subscription on reconnect.
	/// </summary>
	/// <remarks>
	/// Error case like connection lost etc.
	/// </remarks>
	public bool IsRestoreSubscriptionOnErrorReconnect { get; set; }

	/// <inheritdoc />
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var (toInner, toOut) = _manager.ProcessInMessage(message);

		if (toInner.Length > 0)
		{
			if (toInner.Length == 1)
				await base.OnSendInMessageAsync(toInner[0], cancellationToken);
			else
				await toInner.Select(reMapSubscription => base.OnSendInMessageAsync(reMapSubscription, cancellationToken)).WhenAll();
		}

		if (toOut.Length > 0)
		{
			foreach (var sendOutMsg in toOut)
				await RaiseNewOutMessageAsync(sendOutMsg, cancellationToken);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		if (!IsRestoreSubscriptionOnErrorReconnect &&
			message is ConnectionRestoredMessage restoredMsg &&
			restoredMsg.IsResetState)
		{
			await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
			return;
		}

		var (forward, extraOut) = _manager.ProcessOutMessage(message);

		if (forward != null)
			await base.OnInnerAdapterNewOutMessageAsync(forward, cancellationToken);

		if (extraOut.Length > 0)
		{
			foreach (var extra in extraOut)
				await base.OnInnerAdapterNewOutMessageAsync(extra, cancellationToken);
		}
	}

	/// <inheritdoc />
	protected override ValueTask InnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		_manager.OnInnerAdapterMessage(message);
		return base.InnerAdapterNewOutMessageAsync(message, cancellationToken);
	}

	/// <summary>
	/// Create a copy of <see cref="SubscriptionMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
	{
		return new SubscriptionMessageAdapter(InnerAdapter.TypedClone())
		{
			IsRestoreSubscriptionOnErrorReconnect = IsRestoreSubscriptionOnErrorReconnect,
		};
	}
}

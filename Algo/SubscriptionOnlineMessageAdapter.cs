namespace StockSharp.Algo;

/// <summary>
/// Online subscription counter adapter.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SubscriptionOnlineMessageAdapter"/>.
/// </remarks>
public class SubscriptionOnlineMessageAdapter : MessageAdapterWrapper
{
	private readonly ISubscriptionOnlineManager _manager;

	/// <summary>
	/// Initializes a new instance of the <see cref="SubscriptionOnlineMessageAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">Inner message adapter.</param>
	public SubscriptionOnlineMessageAdapter(IMessageAdapter innerAdapter)
		: base(innerAdapter)
	{
		_manager = new SubscriptionOnlineManager(this, IsSecurityRequired);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SubscriptionOnlineMessageAdapter"/> with a custom manager.
	/// </summary>
	/// <param name="innerAdapter">Inner message adapter.</param>
	/// <param name="manager">Online subscription manager.</param>
	public SubscriptionOnlineMessageAdapter(IMessageAdapter innerAdapter, ISubscriptionOnlineManager manager)
		: base(innerAdapter)
	{
		_manager = manager ?? throw new ArgumentNullException(nameof(manager));
	}

	/// <inheritdoc />
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var (toInner, toOut) = await _manager.ProcessInMessageAsync(message, cancellationToken);

		if (toOut.Length > 0)
		{
			foreach (var sendOutMsg in toOut)
				await RaiseNewOutMessageAsync(sendOutMsg, cancellationToken);
		}

		if (toInner.Length > 0)
		{
			if (toInner.Length == 1)
				await base.OnSendInMessageAsync(toInner[0], cancellationToken);
			else
				await toInner.Select(sendInMsg => base.OnSendInMessageAsync(sendInMsg, cancellationToken)).WhenAll();
		}
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var (forward, extraOut) = await _manager.ProcessOutMessageAsync(message, cancellationToken);

		if (extraOut.Length > 0)
		{
			foreach (var extra in extraOut)
				await base.OnInnerAdapterNewOutMessageAsync(extra, cancellationToken);
		}

		if (forward != null)
			await base.OnInnerAdapterNewOutMessageAsync(forward, cancellationToken);
	}

	/// <summary>
	/// Create a copy of <see cref="SubscriptionOnlineMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
	{
		return new SubscriptionOnlineMessageAdapter(InnerAdapter.TypedClone());
	}
}

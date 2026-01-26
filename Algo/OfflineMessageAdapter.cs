namespace StockSharp.Algo;

/// <summary>
/// The messages adapter keeping message until connection will be done.
/// </summary>
public class OfflineMessageAdapter : MessageAdapterWrapper
{
	private readonly IOfflineManager _manager;

	/// <summary>
	/// Initializes a new instance of the <see cref="OfflineMessageAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">Underlying adapter.</param>
	public OfflineMessageAdapter(IMessageAdapter innerAdapter)
		: base(innerAdapter)
	{
		_manager = new OfflineManager(this, () => new ProcessSuspendedMessage(this), new OfflineManagerState());
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OfflineMessageAdapter"/> with a custom manager.
	/// </summary>
	/// <param name="innerAdapter">Underlying adapter.</param>
	/// <param name="manager">Offline manager.</param>
	public OfflineMessageAdapter(IMessageAdapter innerAdapter, IOfflineManager manager)
		: base(innerAdapter)
	{
		_manager = manager ?? throw new ArgumentNullException(nameof(manager));
	}

	/// <summary>
	/// Max message queue count. The default value is 10000.
	/// </summary>
	/// <remarks>
	/// Value set to -1 corresponds to the size without limitations.
	/// </remarks>
	public int MaxMessageCount
	{
		get => _manager.MaxMessageCount;
		set => _manager.MaxMessageCount = value;
	}

	/// <inheritdoc />
	protected override bool SendInBackFurther => false;

	/// <inheritdoc />
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var (toInner, toOut, shouldForward) = _manager.ProcessInMessage(message);

		if (toInner.Length > 0)
		{
			if (toInner.Length == 1)
				await base.OnSendInMessageAsync(toInner[0], cancellationToken);
			else
				await toInner.Select(msg => base.OnSendInMessageAsync(msg, cancellationToken)).WhenAll();
		}

		if (toOut.Length > 0)
		{
			foreach (var outMsg in toOut)
				await RaiseNewOutMessageAsync(outMsg, cancellationToken);
		}

		if (shouldForward)
			await base.OnSendInMessageAsync(message, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var (suppressOriginal, extraOut) = _manager.ProcessOutMessage(message);

		if (!suppressOriginal)
			await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);

		if (extraOut.Length > 0)
		{
			foreach (var extra in extraOut)
				await base.OnInnerAdapterNewOutMessageAsync(extra, cancellationToken);
		}
	}

	/// <summary>
	/// Create a copy of <see cref="OfflineMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
	{
		return new OfflineMessageAdapter(InnerAdapter.TypedClone());
	}
}

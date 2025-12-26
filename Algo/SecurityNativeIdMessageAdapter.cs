namespace StockSharp.Algo;

/// <summary>
/// Security native id message adapter.
/// </summary>
public class SecurityNativeIdMessageAdapter : MessageAdapterWrapper
{
	private readonly ISecurityNativeIdManager _manager;
	private readonly bool _ownsManager;
	private bool _isInitialized;

	/// <summary>
	/// Security native identifier storage provider.
	/// </summary>
	public INativeIdStorageProvider StorageProvider { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="SecurityNativeIdMessageAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
	/// <param name="storageProvider">Security native identifier storage provider.</param>
	public SecurityNativeIdMessageAdapter(IMessageAdapter innerAdapter, INativeIdStorageProvider storageProvider)
		: base(innerAdapter)
	{
		StorageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
		_manager = new SecurityNativeIdManager(this, storageProvider, IsNativeIdentifiersPersistable);
		_manager.ProcessSuspendedRequested += OnProcessSuspendedRequestedAsync;
		_ownsManager = true;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SecurityNativeIdMessageAdapter"/> with a custom manager.
	/// </summary>
	/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
	/// <param name="storageProvider">Security native identifier storage provider.</param>
	/// <param name="manager">Security native id manager.</param>
	public SecurityNativeIdMessageAdapter(IMessageAdapter innerAdapter, INativeIdStorageProvider storageProvider, ISecurityNativeIdManager manager)
		: base(innerAdapter)
	{
		StorageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
		_manager = manager ?? throw new ArgumentNullException(nameof(manager));
		_manager.ProcessSuspendedRequested += OnProcessSuspendedRequestedAsync;
		_ownsManager = false;
	}

	/// <inheritdoc />
	public override void Dispose()
	{
		_manager.ProcessSuspendedRequested -= OnProcessSuspendedRequestedAsync;

		if (_ownsManager)
			_manager.Dispose();

		base.Dispose();
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		if (message.Type == MessageTypes.Connect && !_isInitialized)
		{
			// Initialize the manager from storage on connect
			await _manager.InitializeAsync(StorageName, cancellationToken);
			_isInitialized = true;
		}

		var (forward, extraOut, loopbackIn) = await _manager.ProcessOutMessageAsync(message, cancellationToken);

		// Forward the main message first
		if (forward != null)
			await base.OnInnerAdapterNewOutMessageAsync(forward, cancellationToken);

		// Forward extra output messages
		if (extraOut.Length > 0)
		{
			foreach (var msg in extraOut)
				await base.OnInnerAdapterNewOutMessageAsync(msg, cancellationToken);
		}

		// Send loopback input messages
		if (loopbackIn.Length > 0)
		{
			foreach (var msg in loopbackIn)
			{
				msg.LoopBack(this);
				await base.OnSendInMessageAsync(msg, cancellationToken);
			}
		}
	}

	/// <inheritdoc />
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		if (message.Type == MessageTypes.Reset)
		{
			_isInitialized = false;
		}

		var (toInner, toOut) = await _manager.ProcessInMessageAsync(message, cancellationToken);

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

	private async ValueTask OnProcessSuspendedRequestedAsync(SecurityId securityId, CancellationToken cancellationToken)
	{
		await RaiseNewOutMessageAsync(new ProcessSuspendedMessage(this, securityId), cancellationToken);
	}

	/// <summary>
	/// Create a copy of <see cref="SecurityNativeIdMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
	{
		return new SecurityNativeIdMessageAdapter(InnerAdapter.TypedClone(), StorageProvider);
	}
}

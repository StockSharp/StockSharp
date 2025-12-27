namespace StockSharp.Algo;

using StockSharp.Algo.Storages;

/// <summary>
/// Security identifier mappings message adapter.
/// </summary>
public class SecurityMappingMessageAdapter : MessageAdapterWrapper
{
	private readonly ISecurityMappingManager _manager;

	/// <summary>
	/// Initializes a new instance of the <see cref="SecurityMappingMessageAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
	/// <param name="provider">Security identifier mappings storage provider.</param>
	public SecurityMappingMessageAdapter(IMessageAdapter innerAdapter, ISecurityMappingStorageProvider provider)
		: base(innerAdapter)
	{
		Provider = provider ?? throw new ArgumentNullException(nameof(provider));
		_manager = new SecurityMappingManager(
			provider,
			() => StorageName,
			(format, arg0, arg1, arg2) => LogInfo(format, arg0, arg1, arg2));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SecurityMappingMessageAdapter"/> with a custom manager.
	/// </summary>
	/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
	/// <param name="provider">Security identifier mappings storage provider.</param>
	/// <param name="manager">Security mapping manager.</param>
	public SecurityMappingMessageAdapter(IMessageAdapter innerAdapter, ISecurityMappingStorageProvider provider, ISecurityMappingManager manager)
		: base(innerAdapter)
	{
		Provider = provider ?? throw new ArgumentNullException(nameof(provider));
		_manager = manager ?? throw new ArgumentNullException(nameof(manager));
	}

	/// <summary>
	/// Security identifier mappings storage provider.
	/// </summary>
	public ISecurityMappingStorageProvider Provider { get; }

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var (processedMessage, forward) = _manager.ProcessOutMessage(message);

		if (forward && processedMessage != null)
			await base.OnInnerAdapterNewOutMessageAsync(processedMessage, cancellationToken);
	}

	/// <inheritdoc />
	protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var (processedMessage, forward) = _manager.ProcessInMessage(message);

		if (forward && processedMessage != null)
			return base.OnSendInMessageAsync(processedMessage, cancellationToken);

		return default;
	}

	/// <summary>
	/// Create a copy of <see cref="SecurityMappingMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
	{
		return new SecurityMappingMessageAdapter(InnerAdapter.TypedClone(), Provider);
	}
}

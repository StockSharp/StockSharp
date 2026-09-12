namespace StockSharp.Algo;

/// <summary>
/// The message adapter, that save extension info into <see cref="IExtendedInfoStorage"/>.
/// </summary>
public class ExtendedInfoStorageMessageAdapter : MessageAdapterWrapper
{
	private readonly IExtendedInfoStorage _extendedInfoStorage;
	private readonly string _storageName;
	private readonly IEnumerable<(string, Type)> _fields;
	private readonly AsyncLock _sync = new();
	private IExtendedInfoStorageItem _storage;

	/// <summary>
	/// Initializes a new instance of the <see cref="MessageAdapterWrapper"/>.
	/// </summary>
	/// <param name="innerAdapter">Underlying adapter.</param>
	/// <param name="extendedInfoStorage">Extended info storage.</param>
	public ExtendedInfoStorageMessageAdapter(IMessageAdapter innerAdapter, IExtendedInfoStorage extendedInfoStorage)
		: base(innerAdapter)
	{
		if (InnerAdapter.StorageName.IsEmpty())
			throw new ArgumentException(nameof(innerAdapter));

		_extendedInfoStorage = extendedInfoStorage ?? throw new ArgumentNullException(nameof(extendedInfoStorage));
		_storageName = InnerAdapter.StorageName;
		_fields = [.. InnerAdapter.SecurityExtendedFields];
	}

	private async ValueTask<IExtendedInfoStorageItem> GetStorageAsync(CancellationToken cancellationToken)
	{
		if (_storage == null)
		{
			using (await _sync.LockAsync(cancellationToken))
				_storage ??= await _extendedInfoStorage.CreateAsync(_storageName, _fields, cancellationToken);
		}

		return _storage;
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		// The wrapper is transparent on the way out, so the message travels on before anything is
		// recorded. Every security the adapter reports is then written into the storage named after that
		// adapter, under the extended field schema it declares; a storage that hands back no item for
		// this adapter has nowhere to keep them.
		await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);

		if (message.Type != MessageTypes.Security)
			return;

		var secMsg = (SecurityMessage)message;

		if (secMsg.SecurityId == default)
			return;

		var storage = await GetStorageAsync(cancellationToken);

		storage?.Add(secMsg.SecurityId, new Dictionary<string, object>());
	}

	/// <summary>
	/// Create a copy of <see cref="ExtendedInfoStorageMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
	{
		return new ExtendedInfoStorageMessageAdapter(InnerAdapter.TypedClone(), _extendedInfoStorage);
	}
}
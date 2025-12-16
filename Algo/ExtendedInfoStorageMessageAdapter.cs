namespace StockSharp.Algo;

using Nito.AsyncEx;

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
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		var secMsg = message as SecurityMessage;

		//if (secMsg?.ExtensionInfo != null)
		//	GetStorageAsync().Add(secMsg.SecurityId, secMsg.ExtensionInfo);

		base.OnInnerAdapterNewOutMessage(message);
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
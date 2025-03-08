namespace StockSharp.Algo;

/// <summary>
/// The message adapter, that save extension info into <see cref="IExtendedInfoStorage"/>.
/// </summary>
public class ExtendedInfoStorageMessageAdapter : MessageAdapterWrapper
{
	private readonly IExtendedInfoStorage _extendedInfoStorage;
	private readonly string _storageName;
	private readonly IEnumerable<Tuple<string, Type>> _fields;

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

	private readonly SyncObject _sync = new();
	private IExtendedInfoStorageItem _storage;

	private IExtendedInfoStorageItem GetStorage()
	{
		if (_storage == null)
		{
			lock (_sync)
			{
				_storage ??= _extendedInfoStorage.Create(_storageName, _fields);
			}	
		}

		return _storage;
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		var secMsg = message as SecurityMessage;

		//if (secMsg?.ExtensionInfo != null)
		//	GetStorage().Add(secMsg.SecurityId, secMsg.ExtensionInfo);

		base.OnInnerAdapterNewOutMessage(message);
	}

	/// <summary>
	/// Create a copy of <see cref="ExtendedInfoStorageMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageChannel Clone()
	{
		return new ExtendedInfoStorageMessageAdapter(InnerAdapter.TypedClone(), _extendedInfoStorage);
	}
}
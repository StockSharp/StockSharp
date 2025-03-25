namespace StockSharp.Algo.Storages;

using System.Net;

using StockSharp.Algo.Storages.Remote;

/// <summary>
/// Remote storage of market data working via <see cref="RemoteStorageClient"/>.
/// </summary>
public class RemoteMarketDataDrive : BaseMarketDataDrive
{
	private class RemoteStorageDrive : IMarketDataStorageDrive
	{
		private readonly RemoteMarketDataDrive _parent;
		private readonly SecurityId _securityId;
		private readonly DataType _dataType;
		private readonly StorageFormats _format;

		public RemoteStorageDrive(RemoteMarketDataDrive parent, SecurityId securityId, DataType dataType, StorageFormats format)
		{
			if (securityId == default)
				throw new ArgumentNullException(nameof(securityId));

			if (dataType == null)
				throw new ArgumentNullException(nameof(dataType));

			// TODO
			//if (drive == null)
			//	throw new ArgumentNullException(nameof(drive));

			_parent = parent ?? throw new ArgumentNullException(nameof(parent));
			_securityId = securityId;
			_dataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
			_format = format;
		}

		IMarketDataDrive IMarketDataStorageDrive.Drive => _parent;

		private IEnumerable<DateTime> _dates;
		private DateTime _prevDatesSync;

		IEnumerable<DateTime> IMarketDataStorageDrive.Dates
		{
			get
			{
				if (_prevDatesSync == default || (DateTime.UtcNow - _prevDatesSync).TotalSeconds > 3)
				{
					_dates = _parent.EnsureGetClient().GetDates(_securityId, _dataType, _format);

					_prevDatesSync = DateTime.UtcNow;
				}

				return _dates;
			}
		}

		void IMarketDataStorageDrive.ClearDatesCache()
		{
			//_parent.Invoke(f => f.ClearDatesCache(_parent.SessionId, _security.Id, _dataType, _arg));
		}

		void IMarketDataStorageDrive.Delete(DateTime date)
			=> _parent.EnsureGetClient().Delete(_securityId, _dataType, _format, date);

		void IMarketDataStorageDrive.SaveStream(DateTime date, Stream stream)
			=> _parent.EnsureGetClient().SaveStream(_securityId, _dataType, _format, date, stream);

		Stream IMarketDataStorageDrive.LoadStream(DateTime date, bool readOnly)
			=> _parent.EnsureGetClient().LoadStream(_securityId, _dataType, _format, date);
	}

	private readonly SynchronizedDictionary<(SecurityId, DataType, StorageFormats), RemoteStorageDrive> _remoteStorages = [];
	private readonly Func<IMessageAdapter> _createAdapter;
	
	private readonly SyncObject _clientSync = new();
	private RemoteStorageClient _client;

	/// <summary>
	/// Default value for <see cref="Address"/>.
	/// </summary>
	public static readonly EndPoint DefaultAddress = "127.0.0.1:5002".To<EndPoint>();

	/// <summary>
	/// Default value for <see cref="TargetCompId"/>.
	/// </summary>
	public static readonly string DefaultTargetCompId = "StockSharpHydraMD";

	/// <summary>
	/// Initializes a new instance of the <see cref="RemoteMarketDataDrive"/>.
	/// </summary>
	public RemoteMarketDataDrive()
		: this(DefaultAddress)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RemoteMarketDataDrive"/>.
	/// </summary>
	/// <param name="address">Server address.</param>
	public RemoteMarketDataDrive(EndPoint address)
		: this(address, () => ServicesRegistry.AdapterProvider.CreateTransportAdapter(new IncrementalIdGenerator()))
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RemoteMarketDataDrive"/>.
	/// </summary>
	/// <param name="address">Server address.</param>
	/// <param name="adapter">Message adapter.</param>
	public RemoteMarketDataDrive(EndPoint address, IMessageAdapter adapter)
		: this(address, adapter.TypedClone)
	{
		if (adapter is null)
			throw new ArgumentNullException(nameof(adapter));
	}

	private RemoteMarketDataDrive(EndPoint address, Func<IMessageAdapter> createAdapter)
	{
		Address = address;
		_createAdapter = createAdapter ?? throw new ArgumentNullException(nameof(createAdapter));
	}

	private void ResetClient()
	{
		lock (_clientSync)
		{
			_client?.Dispose();
			_client = null;
		}
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		ResetClient();

		base.DisposeManaged();
	}

	/// <summary>
	/// Information about the login and password for access to remote storage.
	/// </summary>
	public ServerCredentials Credentials { get; } = new();

	private EndPoint _address = DefaultAddress;

	/// <summary>
	/// Server address.
	/// </summary>
	public EndPoint Address
	{
		get => _address;
		set => _address = value ?? throw new ArgumentNullException(nameof(value));
	}

	private string _targetCompId = DefaultTargetCompId;

	/// <summary>
	/// Target ID.
	/// </summary>
	public string TargetCompId
	{
		get => _targetCompId;
		set
		{
			if (value.IsEmpty())
				throw new ArgumentNullException(nameof(value));

			_targetCompId = value;
		}
	}

	private int _securityBatchSize = 1000;

	/// <summary>
	/// The new instruments request block size. By default it does not exceed 1000 elements.
	/// </summary>
	public int SecurityBatchSize
	{
		get => _securityBatchSize;
		set
		{
			if (value <= 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_securityBatchSize = value;
		}
	}

	private TimeSpan _timeout = TimeSpan.FromMinutes(2);

	/// <summary>
	/// Timeout
	/// </summary>
	public TimeSpan Timeout
	{
		get => _timeout;
		set
		{
			if (value <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_timeout = value;
		}
	}

	/// <summary>
	/// Enable binary mode.
	/// </summary>
	public bool IsBinaryEnabled { get; set; }

	/// <summary>
	/// Logs.
	/// </summary>
	public ILogSource Logs { get; set; }

	/// <inheritdoc />
	public override string Path
	{
		get => Address.To<string>();
		set
		{
			if (value.IsEmpty())
				throw new ArgumentNullException(nameof(value));

			Address = value.To<EndPoint>();
		}
	}

	/// <summary>
	/// Cache.
	/// </summary>
	public RemoteStorageCache Cache { get; set; }

	private RemoteStorageClient CreateClient()
	{
		var adapter = _createAdapter();

		((IAddressAdapter<EndPoint>)adapter).Address = Address;

		var login = Credentials.Email.IsEmpty("stocksharp");

		if (adapter is ISenderTargetAdapter sta)
		{
			sta.SenderCompId = login;
			sta.TargetCompId = TargetCompId;
		}

		if (adapter is ILoginPasswordAdapter la)
		{
			la.Login = login;
			la.Password = Credentials.Password;
		}

		if (adapter is IBinaryAdapter ba)
		{
			ba.IsBinaryEnabled = IsBinaryEnabled;
		}

		adapter.Parent ??= Logs ?? ServicesRegistry.LogManager?.Application;

		return new(adapter, Cache, SecurityBatchSize, Timeout);
	}

	private RemoteStorageClient EnsureGetClient()
	{
		lock (_clientSync)
		{
			_client ??= CreateClient();
			return _client;
		}
	}

	/// <inheritdoc />
	public override IEnumerable<SecurityId> AvailableSecurities
		=> EnsureGetClient().AvailableSecurities;

	/// <inheritdoc />
	public override IEnumerable<DataType> GetAvailableDataTypes(SecurityId securityId, StorageFormats format)
		=> EnsureGetClient().GetAvailableDataTypes(securityId, format);

	/// <inheritdoc />
	public override IMarketDataStorageDrive GetStorageDrive(SecurityId securityId, DataType dataType, StorageFormats format)
	{
		if (dataType is null)
			throw new ArgumentNullException(nameof(dataType));

		return _remoteStorages.SafeAdd((securityId, dataType, format),
			key => new(this, securityId, dataType, format));
	}

	/// <inheritdoc />
	public override void Verify() => CreateClient().Verify();

	/// <inheritdoc />
	public override void LookupSecurities(SecurityLookupMessage criteria, ISecurityProvider securityProvider, Action<SecurityMessage> newSecurity, Func<bool> isCancelled, Action<int, int> updateProgress)
		=> EnsureGetClient().LookupSecurities(criteria, securityProvider, newSecurity, isCancelled, updateProgress);

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Credentials.Load(storage, nameof(Credentials));

		TargetCompId = storage.GetValue(nameof(TargetCompId), TargetCompId);
		SecurityBatchSize = storage.GetValue(nameof(SecurityBatchSize), SecurityBatchSize);
		Timeout = storage.GetValue(nameof(Timeout), Timeout);
		IsBinaryEnabled = storage.GetValue(nameof(IsBinaryEnabled), IsBinaryEnabled);

		ResetClient();
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage
			.Set(nameof(Credentials), Credentials.Save())
			.Set(nameof(TargetCompId), TargetCompId)
			.Set(nameof(SecurityBatchSize), SecurityBatchSize)
			.Set(nameof(Timeout), Timeout)
			.Set(nameof(IsBinaryEnabled), IsBinaryEnabled);
		;
	}
}
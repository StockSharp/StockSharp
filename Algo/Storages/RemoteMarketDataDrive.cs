namespace StockSharp.Algo.Storages;

using System.Net;

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

			_parent = parent ?? throw new ArgumentNullException(nameof(parent));
			_securityId = securityId;
			_dataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
			_format = format;
		}

		IMarketDataDrive IMarketDataStorageDrive.Drive => _parent;

		private static readonly TimeSpan _datesCachePeriod = TimeSpan.FromSeconds(3);

		private readonly Lock _datesSync = new();
		private DateTime[] _dates;
		private DateTime _prevDatesSync;

		// Each change replaces the array rather than editing it, so a reader half way through the days
		// finishes the list it started on.
		private DateTime[] TryGetCachedDates()
		{
			using (_datesSync.EnterScope())
				return _dates is not null && (DateTime.UtcNow - _prevDatesSync) <= _datesCachePeriod ? _dates : null;
		}

		private DateTime[] SetDates(DateTime[] dates)
		{
			using (_datesSync.EnterScope())
			{
				_dates = dates;
				_prevDatesSync = DateTime.UtcNow;

				return dates;
			}
		}

		private void ChangeDate(DateTime date, bool remove)
		{
			date = date.UtcKind();

			using (_datesSync.EnterScope())
			{
				if (_dates is null)
					return;

				if (remove)
					_dates = [.. _dates.Where(d => d != date)];
				else if (!_dates.Contains(date))
					_dates = [.. _dates.Append(date).OrderBy(d => d)];
			}
		}

		IAsyncEnumerable<DateTime> IMarketDataStorageDrive.GetDatesAsync()
		{
			return Impl();

			async IAsyncEnumerable<DateTime> Impl([EnumeratorCancellation] CancellationToken cancellationToken = default)
			{
				var dates = TryGetCachedDates()
					?? SetDates([.. await _parent.Client.GetDatesAsync(_securityId, _dataType, _format, cancellationToken)]);

				foreach (var date in dates)
				{
					cancellationToken.ThrowIfCancellationRequested();
					yield return date;
				}
			}
		}

		ValueTask IMarketDataStorageDrive.ClearDatesCacheAsync(CancellationToken cancellationToken)
		{
			using (_datesSync.EnterScope())
			{
				_dates = null;
				_prevDatesSync = default;
			}

			return default;
		}

		async ValueTask IMarketDataStorageDrive.DeleteAsync(DateTime date, CancellationToken cancellationToken)
		{
			await _parent.Client.DeleteAsync(_securityId, _dataType, _format, date, cancellationToken);

			// A caller reads these days to decide what still has to be written, so a day this drive
			// removed is out of them at once rather than after the cache expires.
			ChangeDate(date, true);
		}

		async ValueTask IMarketDataStorageDrive.SaveStreamAsync(DateTime date, Stream stream, CancellationToken cancellationToken)
		{
			await _parent.Client.SaveStreamAsync(_securityId, _dataType, _format, date, stream, cancellationToken);

			ChangeDate(date, false);
		}

		ValueTask<Stream> IMarketDataStorageDrive.LoadStreamAsync(DateTime date, bool readOnly, CancellationToken cancellationToken)
			=> _parent.Client.LoadStreamAsync(_securityId, _dataType, _format, date, cancellationToken);
	}

	private readonly SynchronizedDictionary<(SecurityId, DataType, StorageFormats), RemoteStorageDrive> _remoteStorages = [];

	/// <summary>
	/// Default value for <see cref="Address"/>.
	/// </summary>
	public static readonly EndPoint DefaultAddress = "127.0.0.1:5002".To<EndPoint>();

	/// <summary>
	/// Default value for <see cref="TargetCompId"/>.
	/// </summary>
	public static readonly string DefaultTargetCompId = "StockSharpHydraMD";

	private readonly Lazy<IMessageAdapter> _adapter;
	private readonly bool _ownAdapter;

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
		: this(address, () => ServicesRegistry.AdapterProvider.CreateTransportAdapter(new IncrementalIdGenerator()), true)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RemoteMarketDataDrive"/>.
	/// </summary>
	/// <param name="address">Server address.</param>
	/// <param name="adapter">Message adapter.</param>
	public RemoteMarketDataDrive(EndPoint address, IMessageAdapter adapter)
		: this(address, adapter is null ? throw new ArgumentNullException(nameof(adapter)) : () => adapter, false)
	{
	}

	private RemoteMarketDataDrive(EndPoint address, Func<IMessageAdapter> adapterFactory, bool ownAdapter)
	{
		_adapter = new(adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory)));
		_ownAdapter = ownAdapter;
		Address = address ?? throw new ArgumentNullException(nameof(address));
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		Client?.Dispose();

		if (_ownAdapter && _adapter.IsValueCreated)
			_adapter.Value.Dispose();

		base.DisposeManaged();
	}

	private RemoteStorageClient _client;
	private RemoteStorageClient Client => _client ??= CreateClient();

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

	private RemoteStorageClient CreateClient()
	{
		var adapter = _adapter.Value;

		if (adapter is IAddressAdapter<EndPoint> addressAdapter)
			addressAdapter.Address = Address;

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

		adapter.Parent ??= Logs ?? ServicesRegistry.LogManager?.Application;

		return new(adapter, SecurityBatchSize);
	}

	/// <inheritdoc />
	public override IAsyncEnumerable<SecurityId> GetAvailableSecuritiesAsync()
		=> Client.GetAvailableSecuritiesAsync();

	/// <inheritdoc />
	public override IAsyncEnumerable<DataType> GetAvailableDataTypesAsync(SecurityId securityId, StorageFormats format)
		=> Client.GetAvailableDataTypesAsync(securityId, format);

	/// <inheritdoc />
	public override IMarketDataStorageDrive GetStorageDrive(SecurityId securityId, DataType dataType, StorageFormats format)
	{
		if (dataType is null)
			throw new ArgumentNullException(nameof(dataType));

		return _remoteStorages.SafeAdd((securityId, dataType, format),
			key => new(this, securityId, dataType, format));
	}

	/// <inheritdoc />
	public override ValueTask VerifyAsync(CancellationToken cancellationToken)
		=> Client.VerifyAsync(cancellationToken);

	/// <inheritdoc />
	public override IAsyncEnumerable<SecurityMessage> LookupSecuritiesAsync(SecurityLookupMessage criteria, ISecurityProvider securityProvider)
		=> Client.LookupSecuritiesAsync(criteria, securityProvider);

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Credentials.Load(storage, nameof(Credentials));

		TargetCompId = storage.GetValue(nameof(TargetCompId), TargetCompId);
		SecurityBatchSize = storage.GetValue(nameof(SecurityBatchSize), SecurityBatchSize);
		Timeout = storage.GetValue(nameof(Timeout), Timeout);
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage
			.Set(nameof(Credentials), Credentials.Save())
			.Set(nameof(TargetCompId), TargetCompId)
			.Set(nameof(SecurityBatchSize), SecurityBatchSize)
			.Set(nameof(Timeout), Timeout);
		;
	}
}

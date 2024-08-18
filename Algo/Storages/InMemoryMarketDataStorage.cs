namespace StockSharp.Algo.Storages;

/// <summary>
/// The storage, generating data in the process of operation.
/// </summary>
/// <typeparam name="T">Data type.</typeparam>
public sealed class InMemoryMarketDataStorage<T> : IMarketDataStorage<T>
	where T : Message
{
	private readonly Func<DateTimeOffset, IEnumerable<T>> _getData;

	/// <summary>
	/// Initializes a new instance of the <see cref="InMemoryMarketDataStorage{T}"/>.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="arg">The additional argument, associated with data. For example, candle arg.</param>
	/// <param name="getData">Handler for retrieving in-memory data.</param>
	/// <param name="dataType">Data type.</param>
	public InMemoryMarketDataStorage(SecurityId securityId, object arg, Func<DateTimeOffset, IEnumerable<Message>> getData, Type dataType = null)
		: this(securityId, arg, d => getData(d).Cast<T>(), dataType)
	{
		if (getData == null)
			throw new ArgumentNullException(nameof(getData));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="InMemoryMarketDataStorage{T}"/>.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="arg">The additional argument, associated with data. For example, candle arg.</param>
	/// <param name="getData">Handler for retrieving in-memory data.</param>
	/// <param name="dataType">Data type.</param>
	public InMemoryMarketDataStorage(SecurityId securityId, object arg, Func<DateTimeOffset, IEnumerable<T>> getData, Type dataType = null)
	{
		_securityId = securityId;
		_getData = getData ?? throw new ArgumentNullException(nameof(getData));
		_dataType = DataType.Create(dataType ?? typeof(T), arg);
	}

	IEnumerable<DateTime> IMarketDataStorage.Dates => throw new NotSupportedException();

	private readonly SecurityId _securityId;
	SecurityId IMarketDataStorage.SecurityId => _securityId;

	IMarketDataStorageDrive IMarketDataStorage.Drive => throw new NotSupportedException();

	bool IMarketDataStorage.AppendOnlyNew { get; set; }

	private readonly DataType _dataType;
	DataType IMarketDataStorage.DataType => _dataType;

	IMarketDataSerializer IMarketDataStorage.Serializer => ((IMarketDataStorage<T>)this).Serializer;
	IMarketDataSerializer<T> IMarketDataStorage<T>.Serializer => throw new NotSupportedException();

	/// <inheritdoc />
	public IEnumerable<T> Load(DateTime date) => _getData(date);

	IEnumerable<Message> IMarketDataStorage.Load(DateTime date) => Load(date);
	IMarketDataMetaInfo IMarketDataStorage.GetMetaInfo(DateTime date) => throw new NotSupportedException();
	
	int IMarketDataStorage.Save(IEnumerable<Message> data) => throw new NotSupportedException();
	void IMarketDataStorage.Delete(IEnumerable<Message> data) => throw new NotSupportedException();
	void IMarketDataStorage.Delete(DateTime date) => throw new NotSupportedException();
	int IMarketDataStorage<T>.Save(IEnumerable<T> data) => throw new NotSupportedException();
	void IMarketDataStorage<T>.Delete(IEnumerable<T> data) => throw new NotSupportedException();
}
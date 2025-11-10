namespace StockSharp.Algo.Storages;

using Ecng.Linq;

/// <summary>
/// The storage, generating data in the process of operation.
/// </summary>
/// <typeparam name="T">Data type.</typeparam>
public sealed class InMemoryMarketDataStorage<T> : IMarketDataStorage<T>
	where T : Message
{
	private readonly Func<DateTime, IEnumerable<T>> _getData;

	/// <summary>
	/// Initializes a new instance of the <see cref="InMemoryMarketDataStorage{T}"/>.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="arg">The additional argument, associated with data. For example, candle arg.</param>
	/// <param name="getData">Handler for retrieving in-memory data.</param>
	/// <param name="dataType">Data type.</param>
	public InMemoryMarketDataStorage(SecurityId securityId, object arg, Func<DateTime, IEnumerable<Message>> getData, Type dataType = null)
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
	public InMemoryMarketDataStorage(SecurityId securityId, object arg, Func<DateTime, IEnumerable<T>> getData, Type dataType = null)
	{
		_securityId = securityId;
		_getData = getData ?? throw new ArgumentNullException(nameof(getData));
		_dataType = DataType.Create(dataType ?? typeof(T), arg);
	}

	ValueTask<IEnumerable<DateTime>> IMarketDataStorage.GetDatesAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

	private readonly SecurityId _securityId;
	SecurityId IMarketDataStorage.SecurityId => _securityId;

	IMarketDataStorageDrive IMarketDataStorage.Drive => throw new NotSupportedException();

	bool IMarketDataStorage.AppendOnlyNew { get; set; }

	private readonly DataType _dataType;
	DataType IMarketDataStorage.DataType => _dataType;

	IMarketDataSerializer IMarketDataStorage.Serializer => ((IMarketDataStorage<T>)this).Serializer;
	IMarketDataSerializer<T> IMarketDataStorage<T>.Serializer => throw new NotSupportedException();

	/// <inheritdoc />
	public IAsyncEnumerable<T> LoadAsync(DateTime date, CancellationToken cancellationToken)
		=> _getData(date).ToAsyncEnumerable2(cancellationToken);

	IAsyncEnumerable<Message> IMarketDataStorage.LoadAsync(DateTime date, CancellationToken cancellationToken) => LoadAsync(date, cancellationToken);

	ValueTask<IMarketDataMetaInfo> IMarketDataStorage.GetMetaInfoAsync(DateTime date, CancellationToken cancellationToken) => throw new NotSupportedException();
	
	ValueTask<int> IMarketDataStorage.SaveAsync(IEnumerable<Message> data, CancellationToken cancellationToken) => throw new NotSupportedException();
	ValueTask IMarketDataStorage.DeleteAsync(IEnumerable<Message> data, CancellationToken cancellationToken) => throw new NotSupportedException();
	ValueTask IMarketDataStorage.DeleteAsync(DateTime date, CancellationToken cancellationToken) => throw new NotSupportedException();
	ValueTask<int> IMarketDataStorage<T>.SaveAsync(IEnumerable<T> data, CancellationToken cancellationToken) => throw new NotSupportedException();
	ValueTask IMarketDataStorage<T>.DeleteAsync(IEnumerable<T> data, CancellationToken cancellationToken) => throw new NotSupportedException();
}
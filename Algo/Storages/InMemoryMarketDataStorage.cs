namespace StockSharp.Algo.Storages;

/// <summary>
/// The storage, generating data in the process of operation.
/// </summary>
/// <typeparam name="T">Data type.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="InMemoryMarketDataStorage{T}"/>.
/// </remarks>
/// <param name="securityId">Security ID.</param>
/// <param name="dataType">Data type.</param>
/// <param name="getData">Handler for retrieving in-memory data.</param>
public sealed class InMemoryMarketDataStorage<T>(SecurityId securityId, DataType dataType, Func<DateTime, IAsyncEnumerable<T>> getData) : IMarketDataStorage<T>
	where T : Message
{
	private readonly Func<DateTime, IAsyncEnumerable<T>> _getData = getData ?? throw new ArgumentNullException(nameof(getData));

	ValueTask<IEnumerable<DateTime>> IMarketDataStorage.GetDatesAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

	private readonly SecurityId _securityId = securityId;
	SecurityId IMarketDataStorage.SecurityId => _securityId;

	IMarketDataStorageDrive IMarketDataStorage.Drive => throw new NotSupportedException();

	bool IMarketDataStorage.AppendOnlyNew { get; set; }

	private readonly DataType _dataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
	DataType IMarketDataStorage.DataType => _dataType;

	IMarketDataSerializer IMarketDataStorage.Serializer => ((IMarketDataStorage<T>)this).Serializer;
	IMarketDataSerializer<T> IMarketDataStorage<T>.Serializer => throw new NotSupportedException();

	/// <inheritdoc />
	public IAsyncEnumerable<T> LoadAsync(DateTime date)
		=> _getData(date);

	IAsyncEnumerable<Message> IMarketDataStorage.LoadAsync(DateTime date) => LoadAsync(date);

	ValueTask<IMarketDataMetaInfo> IMarketDataStorage.GetMetaInfoAsync(DateTime date, CancellationToken cancellationToken) => throw new NotSupportedException();
	
	ValueTask<int> IMarketDataStorage.SaveAsync(IEnumerable<Message> data, CancellationToken cancellationToken) => throw new NotSupportedException();
	ValueTask IMarketDataStorage.DeleteAsync(IEnumerable<Message> data, CancellationToken cancellationToken) => throw new NotSupportedException();
	ValueTask IMarketDataStorage.DeleteAsync(DateTime date, CancellationToken cancellationToken) => throw new NotSupportedException();
	ValueTask<int> IMarketDataStorage<T>.SaveAsync(IEnumerable<T> data, CancellationToken cancellationToken) => throw new NotSupportedException();
	ValueTask IMarketDataStorage<T>.DeleteAsync(IEnumerable<T> data, CancellationToken cancellationToken) => throw new NotSupportedException();
}
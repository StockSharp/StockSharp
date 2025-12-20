namespace StockSharp.Algo.Storages;

/// <summary>
/// Cacheable <see cref="IMarketDataStorage"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CacheableMarketDataStorage"/>.
/// </remarks>
/// <param name="underlying">Underlying source.</param>
/// <param name="cache"><see cref="MarketDataStorageCache"/>.</param>
public class CacheableMarketDataStorage(IMarketDataStorage underlying, MarketDataStorageCache cache) : IMarketDataStorage
{
	private readonly IMarketDataStorage _underlying = underlying ?? throw new ArgumentNullException(nameof(underlying));
	private readonly MarketDataStorageCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

	IMarketDataSerializer IMarketDataStorage.Serializer => _underlying.Serializer;
	ValueTask<IEnumerable<DateTime>> IMarketDataStorage.GetDatesAsync(CancellationToken cancellationToken) => _underlying.GetDatesAsync(cancellationToken);
	DataType IMarketDataStorage.DataType => _underlying.DataType;
	SecurityId IMarketDataStorage.SecurityId => _underlying.SecurityId;
	IMarketDataStorageDrive IMarketDataStorage.Drive => _underlying.Drive;
	bool IMarketDataStorage.AppendOnlyNew { get => _underlying.AppendOnlyNew; set => _underlying.AppendOnlyNew = value; }

	IAsyncEnumerable<Message> IMarketDataStorage.LoadAsync(DateTime date)
		=> _cache.GetMessagesAsync(_underlying.SecurityId, _underlying.DataType, date, _underlying.LoadAsync);

	ValueTask<int> IMarketDataStorage.SaveAsync(IEnumerable<Message> data, CancellationToken cancellationToken) => _underlying.SaveAsync(data, cancellationToken);
	ValueTask IMarketDataStorage.DeleteAsync(IEnumerable<Message> data, CancellationToken cancellationToken) => _underlying.DeleteAsync(data, cancellationToken);
	ValueTask IMarketDataStorage.DeleteAsync(DateTime date, CancellationToken cancellationToken) => _underlying.DeleteAsync(date, cancellationToken);
	ValueTask<IMarketDataMetaInfo> IMarketDataStorage.GetMetaInfoAsync(DateTime date, CancellationToken cancellationToken) => _underlying.GetMetaInfoAsync(date, cancellationToken);
}
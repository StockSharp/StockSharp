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
	IAsyncEnumerable<DateTime> IMarketDataStorage.GetDatesAsync() => _underlying.GetDatesAsync();
	DataType IMarketDataStorage.DataType => _underlying.DataType;
	SecurityId IMarketDataStorage.SecurityId => _underlying.SecurityId;
	IMarketDataStorageDrive IMarketDataStorage.Drive => _underlying.Drive;
	bool IMarketDataStorage.AppendOnlyNew { get => _underlying.AppendOnlyNew; set => _underlying.AppendOnlyNew = value; }

	IAsyncEnumerable<Message> IMarketDataStorage.LoadAsync(DateTime date)
		=> _cache.GetMessagesAsync(_underlying.SecurityId, _underlying.DataType, date, _underlying.LoadAsync);

	async ValueTask<int> IMarketDataStorage.SaveAsync(IEnumerable<Message> data, CancellationToken cancellationToken)
	{
		var list = data as IList<Message> ?? [.. data];

		var result = await _underlying.SaveAsync(list, cancellationToken);

		var dates = list
			.OfType<IServerTimeMessage>()
			.Select(m => m.ServerTime.Date)
			.Distinct();

		foreach (var date in dates)
			_cache.Invalidate(_underlying.SecurityId, _underlying.DataType, date);

		return result;
	}

	async ValueTask IMarketDataStorage.DeleteAsync(IEnumerable<Message> data, CancellationToken cancellationToken)
	{
		var list = data as IList<Message> ?? [.. data];

		await _underlying.DeleteAsync(list, cancellationToken);

		var dates = list
			.OfType<IServerTimeMessage>()
			.Select(m => m.ServerTime.Date)
			.Distinct();

		foreach (var date in dates)
			_cache.Invalidate(_underlying.SecurityId, _underlying.DataType, date);
	}

	async ValueTask IMarketDataStorage.DeleteAsync(DateTime date, CancellationToken cancellationToken)
	{
		await _underlying.DeleteAsync(date, cancellationToken);

		_cache.Invalidate(_underlying.SecurityId, _underlying.DataType, date);
	}

	ValueTask<IMarketDataMetaInfo> IMarketDataStorage.GetMetaInfoAsync(DateTime date, CancellationToken cancellationToken) => _underlying.GetMetaInfoAsync(date, cancellationToken);
}
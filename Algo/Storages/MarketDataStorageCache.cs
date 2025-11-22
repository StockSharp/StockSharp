namespace StockSharp.Algo.Storages;

/// <summary>
/// <see cref="IMarketDataStorage"/> cache.
/// </summary>
public class MarketDataStorageCache : Cloneable<MarketDataStorageCache>
{
	private readonly SynchronizedDictionary<(SecurityId, DataType, DateTime), (DateTime lastAccess, Message[] data)> _cache = [];

	private int _limit = 1000;

	/// <summary>
	/// Max count.
	/// </summary>
	public int Limit
	{
		get => _limit;
		set
		{
			if (value <= 0)
				throw new ArgumentOutOfRangeException(nameof(value));

			_limit = value;
		}
	}

	/// <inheritdoc/>
	public override MarketDataStorageCache Clone() => new() { Limit = Limit };

	/// <summary>
	/// Get data asynchronously.
	/// </summary>
	/// <typeparam name="TEnumerable">Type of data collection.</typeparam>
	/// <param name="securityId"><see cref="SecurityId"/>.</param>
	/// <param name="dataType"><see cref="DataType"/>.</param>
	/// <param name="date">Date to load.</param>
	/// <param name="loadIfNeed">Handler to load data from real storage.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Data.</returns>
	public async IAsyncEnumerable<Message> GetMessagesAsync<TEnumerable>(SecurityId securityId, DataType dataType, DateTime date, Func<DateTime, CancellationToken, TEnumerable> loadIfNeed, [EnumeratorCancellation]CancellationToken cancellationToken)
		where TEnumerable : IAsyncEnumerable<Message>
	{
		//if (dataType is null)
		//	throw new ArgumentNullException(nameof(dataType));

		if (loadIfNeed is null)
			throw new ArgumentNullException(nameof(loadIfNeed));

		var now = DateTime.UtcNow;

		var key = (securityId, dataType, date);

		if (!_cache.TryGetValue(key, out var t))
		{
			var data = await loadIfNeed(date, cancellationToken).ToArrayAsync(cancellationToken);
			t = (now, data.ToArray());

			if (_cache.Count > Limit)
			{
				lock (_cache.SyncRoot)
					_cache.RemoveRange([.. _cache.OrderBy(p => p.Value.lastAccess).Take(500)]);
			}
		}
		else
			t.lastAccess = now;

		_cache[key] = t;

		foreach (var msg in t.data)
			yield return msg;
	}
}
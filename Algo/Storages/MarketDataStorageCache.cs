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
	/// Invalidate cache for specified key.
	/// </summary>
	/// <param name="securityId"><see cref="SecurityId"/>.</param>
	/// <param name="dataType"><see cref="DataType"/>.</param>
	/// <param name="date">Date to invalidate.</param>
	public void Invalidate(SecurityId securityId, DataType dataType, DateTime date)
		=> _cache.Remove((securityId, dataType, date));

	/// <summary>
	/// Get data asynchronously.
	/// </summary>
	/// <typeparam name="TEnumerable">Type of data collection.</typeparam>
	/// <param name="securityId"><see cref="SecurityId"/>.</param>
	/// <param name="dataType"><see cref="DataType"/>.</param>
	/// <param name="date">Date to load.</param>
	/// <param name="loadIfNeed">Handler to load data from real storage.</param>
	/// <returns>Data.</returns>
	public IAsyncEnumerable<Message> GetMessagesAsync<TEnumerable>(SecurityId securityId, DataType dataType, DateTime date, Func<DateTime, TEnumerable> loadIfNeed)
		where TEnumerable : IAsyncEnumerable<Message>
	{
		if (loadIfNeed is null)
			throw new ArgumentNullException(nameof(loadIfNeed));

		return Impl(this, securityId, dataType, date, loadIfNeed);

		static async IAsyncEnumerable<Message> Impl(MarketDataStorageCache cache, SecurityId securityId, DataType dataType, DateTime date, Func<DateTime, TEnumerable> loadIfNeed, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var now = DateTime.UtcNow;

			var key = (securityId, dataType, date);

			if (!cache._cache.TryGetValue(key, out var t))
			{
				var data = await loadIfNeed(date).ToArrayAsync(cancellationToken);
				t = (now, data);

				if (cache._cache.Count > cache.Limit)
				{
					using (cache._cache.EnterScope())
						cache._cache.RemoveRange([.. cache._cache.OrderBy(p => p.Value.lastAccess).Take(500)]);
				}
			}
			else
				t.lastAccess = now;

			cache._cache[key] = t;

			foreach (var msg in t.data)
			{
				cancellationToken.ThrowIfCancellationRequested();
				yield return msg;
			}
		}
	}
}
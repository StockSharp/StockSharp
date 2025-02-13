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
	/// Get data.
	/// </summary>
	/// <param name="securityId"><see cref="SecurityId"/>.</param>
	/// <param name="dataType"><see cref="DataType"/>.</param>
	/// <param name="date">Date to load.</param>
	/// <param name="loadIfNeed">Handler to load data from real storage.</param>
	/// <returns>Data.</returns>
	public Message[] GetMessages(SecurityId securityId, DataType dataType, DateTime date, Func<DateTime, IEnumerable<Message>> loadIfNeed)
	{
		//if (dataType is null)
		//	throw new ArgumentNullException(nameof(dataType));

		if (loadIfNeed is null)
			throw new ArgumentNullException(nameof(loadIfNeed));

		var now = DateTime.UtcNow;

		var key = (securityId, dataType, date);

		if (!_cache.TryGetValue(key, out var t))
		{
			t = (now, loadIfNeed(date).ToArray());

			if (_cache.Count > Limit)
			{
				lock (_cache.SyncRoot)
					_cache.RemoveRange([.. _cache.OrderBy(p => p.Value.lastAccess).Take(500)]);
			}
		}
		else
			t.lastAccess = now;

		_cache[key] = t;

		return t.data;
	}
}
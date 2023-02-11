namespace StockSharp.Algo.Storages;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Collections;
using Ecng.Common;

using StockSharp.Messages;

/// <summary>
/// <see cref="IMarketDataStorage"/> cache.
/// </summary>
public class MarketDataStorageCache
{
	private readonly SyncObject _lock = new();
	private readonly SynchronizedDictionary<(DataType, DateTime), Message[]> _cache = new();

	/// <summary>
	/// Get data.
	/// </summary>
	/// <param name="dataType"><see cref="DataType"/>.</param>
	/// <param name="date">Date to load.</param>
	/// <param name="loadIfNeed">Handler to load data from real storage.</param>
	/// <returns>Data.</returns>
	public IEnumerable<Message> GetMessages(DataType dataType, DateTime date, Func<DateTime, IEnumerable<Message>> loadIfNeed)
	{
		if (dataType is null)
			throw new ArgumentNullException(nameof(dataType));

		if (loadIfNeed is null)
			throw new ArgumentNullException(nameof(loadIfNeed));

		var key = (dataType, date);

		if (!_cache.TryGetValue(key, out var messages))
			_cache[key] = messages = loadIfNeed(date).ToArray();

		return messages;
	}
}

namespace StockSharp.Algo.Storages;

using System;
using System.Collections.Generic;

using StockSharp.Messages;

/// <summary>
/// Cacheable <see cref="IMarketDataStorage"/>.
/// </summary>
public class CacheableMarketDataStorage : IMarketDataStorage
{
	private readonly IMarketDataStorage _underlying;
	private readonly MarketDataStorageCache _cache;

	/// <summary>
	/// Initializes a new instance of the <see cref="CacheableMarketDataStorage"/>.
	/// </summary>
	/// <param name="underlying">Underlying source.</param>
	/// <param name="cache"><see cref="MarketDataStorageCache"/>.</param>
	public CacheableMarketDataStorage(IMarketDataStorage underlying, MarketDataStorageCache cache)
	{
		_underlying = underlying ?? throw new ArgumentNullException(nameof(underlying));
		_cache = cache ?? throw new ArgumentNullException(nameof(cache));
	}

	IMarketDataSerializer IMarketDataStorage.Serializer => _underlying.Serializer;
	IEnumerable<DateTime> IMarketDataStorage.Dates => _underlying.Dates;
	DataType IMarketDataStorage.DataType => _underlying.DataType;
	SecurityId IMarketDataStorage.SecurityId => _underlying.SecurityId;
	IMarketDataStorageDrive IMarketDataStorage.Drive => _underlying.Drive;
	bool IMarketDataStorage.AppendOnlyNew { get => _underlying.AppendOnlyNew; set => _underlying.AppendOnlyNew = value; }

	IEnumerable<Message> IMarketDataStorage.Load(DateTime date)
		=> _cache.GetMessages(_underlying.SecurityId, _underlying.DataType, date, _underlying.Load);

	int IMarketDataStorage.Save(IEnumerable<Message> data) => _underlying.Save(data);
	void IMarketDataStorage.Delete(IEnumerable<Message> data) => _underlying.Delete(data);
	void IMarketDataStorage.Delete(DateTime date) => _underlying.Delete(date);
	IMarketDataMetaInfo IMarketDataStorage.GetMetaInfo(DateTime date) => _underlying.GetMetaInfo(date);
}
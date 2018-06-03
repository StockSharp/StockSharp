namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The market data storage, saving data in the cache-storage.
	/// </summary>
	/// <typeparam name="TData">Market data type.</typeparam>
	public class CacheableMarketDataStorage<TData> : IMarketDataStorage<TData>
	{
		private readonly IMarketDataStorage<TData> _cacheStorage;
		private readonly IMarketDataStorage<TData> _sourceStorage;

		/// <summary>
		/// Initializes a new instance of the <see cref="CacheableMarketDataStorage{TData}"/>.
		/// </summary>
		/// <param name="sourceStorage">The initial storage of market-data.</param>
		/// <param name="cacheStorage">The cache-storage of market-data.</param>
		public CacheableMarketDataStorage(IMarketDataStorage<TData> sourceStorage, IMarketDataStorage<TData> cacheStorage)
		{
			_sourceStorage = sourceStorage ?? throw new ArgumentNullException(nameof(sourceStorage));
			_cacheStorage = cacheStorage ?? throw new ArgumentNullException(nameof(cacheStorage));
		}

		IEnumerable<DateTime> IMarketDataStorage.Dates => _sourceStorage.Dates;

		Type IMarketDataStorage.DataType => _sourceStorage.DataType;

		Security IMarketDataStorage.Security => _sourceStorage.Security;

		object IMarketDataStorage.Arg => _sourceStorage.Arg;

		IMarketDataStorageDrive IMarketDataStorage.Drive => _sourceStorage.Drive;

		bool IMarketDataStorage.AppendOnlyNew
		{
			get => _sourceStorage.AppendOnlyNew;
			set => _sourceStorage.AppendOnlyNew = value;
		}

		int IMarketDataStorage.Save(IEnumerable data) => ((IMarketDataStorage<TData>)this).Save(data);

		void IMarketDataStorage.Delete(IEnumerable data) => ((IMarketDataStorage<TData>)this).Delete(data);

		void IMarketDataStorage.Delete(DateTime date) => ((IMarketDataStorage<TData>)this).Delete(date);

		IEnumerable IMarketDataStorage.Load(DateTime date) =>  ((IMarketDataStorage<TData>)this).Load(date);

		IMarketDataSerializer IMarketDataStorage.Serializer => ((IMarketDataStorage<TData>)this).Serializer;

		IEnumerable<TData> IMarketDataStorage<TData>.Load(DateTime date)
		{
			if (_cacheStorage.Dates.Contains(date))
				return _cacheStorage.Load(date);

			var data = _sourceStorage.Load(date);
			_cacheStorage.Save(data);
			return data;
		}

		IMarketDataSerializer<TData> IMarketDataStorage<TData>.Serializer => _sourceStorage.Serializer;

		int IMarketDataStorage<TData>.Save(IEnumerable<TData> data)
		{
			_cacheStorage.Save(data);
			return _sourceStorage.Save(data);
		}

		void IMarketDataStorage<TData>.Delete(IEnumerable<TData> data)
		{
			_cacheStorage.Delete(data);
			_sourceStorage.Delete(data);
		}

		IMarketDataMetaInfo IMarketDataStorage.GetMetaInfo(DateTime date)
		{
			if (_cacheStorage.Dates.Contains(date))
				return _cacheStorage.GetMetaInfo(date);

			return _sourceStorage.GetMetaInfo(date);
		}
	}
}
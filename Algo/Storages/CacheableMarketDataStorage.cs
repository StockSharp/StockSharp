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
		private readonly IMarketDataStorage<TData> _cacheDrive;
		private readonly IMarketDataStorage<TData> _sourceDrive;

		/// <summary>
		/// Initializes a new instance of the <see cref="CacheableMarketDataStorage{TData}"/>.
		/// </summary>
		/// <param name="sourceDrive">The initial storage of market-data.</param>
		/// <param name="cacheDrive">The cache-storage of market-data.</param>
		public CacheableMarketDataStorage(IMarketDataStorage<TData> sourceDrive, IMarketDataStorage<TData> cacheDrive)
		{
			if (sourceDrive == null)
				throw new ArgumentNullException(nameof(sourceDrive));

			if (cacheDrive == null)
				throw new ArgumentNullException(nameof(cacheDrive));

			_sourceDrive = sourceDrive;
			_cacheDrive = cacheDrive;
		}

		IEnumerable<DateTime> IMarketDataStorage.Dates => _sourceDrive.Dates;

		Type IMarketDataStorage.DataType => _sourceDrive.DataType;

		Security IMarketDataStorage.Security => _sourceDrive.Security;

		object IMarketDataStorage.Arg => _sourceDrive.Arg;

		IMarketDataStorageDrive IMarketDataStorage.Drive => _sourceDrive.Drive;

		bool IMarketDataStorage.AppendOnlyNew
		{
			get => _sourceDrive.AppendOnlyNew;
			set => _sourceDrive.AppendOnlyNew = value;
		}

		int IMarketDataStorage.Save(IEnumerable data)
		{
			return ((IMarketDataStorage<TData>)this).Save(data);
		}

		void IMarketDataStorage.Delete(IEnumerable data)
		{
			((IMarketDataStorage<TData>)this).Delete(data);
		}

		void IMarketDataStorage.Delete(DateTime date)
		{
			((IMarketDataStorage<TData>)this).Delete(date);
		}

		IEnumerable IMarketDataStorage.Load(DateTime date)
		{
			return ((IMarketDataStorage<TData>)this).Load(date);
		}

		IMarketDataSerializer IMarketDataStorage.Serializer => ((IMarketDataStorage<TData>)this).Serializer;

		IEnumerable<TData> IMarketDataStorage<TData>.Load(DateTime date)
		{
			if (_cacheDrive.Dates.Contains(date))
				return _cacheDrive.Load(date);

			var data = _sourceDrive.Load(date);
			_cacheDrive.Save(data);
			return data;
		}

		IMarketDataSerializer<TData> IMarketDataStorage<TData>.Serializer => _sourceDrive.Serializer;

		int IMarketDataStorage<TData>.Save(IEnumerable<TData> data)
		{
			_cacheDrive.Save(data);
			return _sourceDrive.Save(data);
		}

		void IMarketDataStorage<TData>.Delete(IEnumerable<TData> data)
		{
			_cacheDrive.Delete(data);
			_sourceDrive.Delete(data);
		}

		IMarketDataMetaInfo IMarketDataStorage.GetMetaInfo(DateTime date)
		{
			if (_cacheDrive.Dates.Contains(date))
				return _cacheDrive.GetMetaInfo(date);

			return _sourceDrive.GetMetaInfo(date);
		}
	}
}
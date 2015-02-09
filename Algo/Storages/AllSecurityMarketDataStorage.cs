namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Interop;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	class AllSecurityMarketDataStorage<T> : IMarketDataStorage<T>, IMarketDataStorageInfo<T>
		where T : Message
	{
		private sealed class BasketEnumerable : SimpleEnumerable<T>
		{
			public BasketEnumerable(Func<IEnumerator<T>> createEnumerator)
				: base(createEnumerator)
			{
			}
		}

		private readonly Security _security;
		private readonly BasketMarketDataStorage<T> _basket;

		public AllSecurityMarketDataStorage(Security security, object arg, Func<T, DateTimeOffset> getTime, Func<T, Security> getSecurity, Func<Security, IMarketDataDrive, IMarketDataStorage<T>> getStorage, IMarketDataStorageDrive drive)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			if (getTime == null)
				throw new ArgumentNullException("getTime");

			if (getSecurity == null)
				throw new ArgumentNullException("getSecurity");

			if (getStorage == null)
				throw new ArgumentNullException("getStorage");

			if (drive == null)
				throw new ArgumentNullException("drive");

			_security = security;
			_getTime = getTime;

			_arg = arg;
			Drive = drive;

			_basket = new BasketMarketDataStorage<T>();

			var idGenerator = new SecurityIdGenerator();

			var parts = idGenerator.Split(security.Id);
			var code = parts.Item1;

			var securities = InteropHelper
				.GetDirectories(Path.Combine(Drive.Drive.Path, code.Substring(0, 1)), code + "*")
				.Select(p => Path.GetFileName(p).FolderNameToSecurityId())
				.Select(s =>
				{
					var id = idGenerator.Split(s);

					var clone = security.Clone();
					clone.Id = s;
					clone.Board = ExchangeBoard.GetOrCreateBoard(id.Item2);
					return clone;
				});

			foreach (var sec in securities)
				_basket.InnerStorages.Add(getStorage(sec, Drive.Drive));
		}

		IMarketDataSerializer IMarketDataStorage.Serializer
		{
			get { return ((IMarketDataStorage<T>)this).Serializer; }
		}

		IMarketDataSerializer<T> IMarketDataStorage<T>.Serializer
		{
			get { throw new NotSupportedException(); }
		}

		IEnumerable<DateTime> IMarketDataStorage.Dates
		{
			get { return Drive.Dates; }
		}

		Type IMarketDataStorage.DataType
		{
			get { return typeof(T); }
		}

		Security IMarketDataStorage.Security
		{
			get { return _security; }
		}

		private readonly object _arg;

		object IMarketDataStorage.Arg
		{
			get { return _arg; }
		}

		public IMarketDataStorageDrive Drive { get; private set; }

		private bool _appendOnlyNew = true;

		bool IMarketDataStorage.AppendOnlyNew
		{
			get { return _appendOnlyNew; }
			set { _appendOnlyNew = value; }
		}

		/// <summary>
		/// Сохранить маркет-данные в хранилище.
		/// </summary>
		/// <param name="data">Маркет-данные.</param>
		public void Save(IEnumerable<T> data)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Удалить маркет-данные из хранилища.
		/// </summary>
		/// <param name="data">Маркет-данные, которые необходимо удалить.</param>
		public void Delete(IEnumerable<T> data)
		{
			throw new NotSupportedException();
		}

		void IMarketDataStorage.Delete(IEnumerable data)
		{
			throw new NotSupportedException();
		}

		void IMarketDataStorage.Delete(DateTime date)
		{
			throw new NotSupportedException();
		}

		void IMarketDataStorage.Save(IEnumerable data)
		{
			throw new NotSupportedException();
		}

		IEnumerable IMarketDataStorage.Load(DateTime date)
		{
			return Load(date);
		}

		IDataStorageReader<T> IMarketDataStorage<T>.GetReader(DateTime date)
		{
			throw new NotImplementedException();
		}

		IMarketDataMetaInfo IMarketDataStorage.GetMetaInfo(DateTime date)
		{
			return _basket.InnerStorages.First().GetMetaInfo(date);
		}

		/// <summary>
		/// Загрузить данные.
		/// </summary>
		/// <param name="date">Дата, для которой необходимо загрузить данные.</param>
		/// <returns>Данные. Если данных не существует, то будет возвращено пустое множество.</returns>
		public IEnumerableEx<T> Load(DateTime date)
		{
			return new BasketEnumerable(() => _basket.Load(date)).ToEx();
		}

		private readonly Func<T, DateTimeOffset> _getTime;

		DateTimeOffset IMarketDataStorageInfo.GetTime(object data)
		{
			return _getTime((T)data);
		}

		DateTimeOffset IMarketDataStorageInfo<T>.GetTime(T data)
		{
			return _getTime(data);
		}
	}

	class ConvertableAllSecurityMarketDataStorage<TMessage, TEntity> : AllSecurityMarketDataStorage<TMessage>, IMarketDataStorage<TEntity>, IMarketDataStorageInfo<TEntity>
		where TMessage : Message
	{
		private readonly Security _security;
		private readonly Func<TEntity, DateTimeOffset> _getEntityTime;

		public ConvertableAllSecurityMarketDataStorage(Security security,
			object arg,
			Func<TMessage, DateTimeOffset> getTime,
			Func<TMessage, Security> getSecurity,
			Func<TEntity, DateTimeOffset> getEntityTime,
			Func<Security, IMarketDataDrive, IMarketDataStorage<TMessage>> getStorage,
			IMarketDataStorageDrive drive)
			: base(security, arg, getTime, getSecurity, getStorage, drive)
		{
			if (getEntityTime == null)
				throw new ArgumentNullException("getEntityTime");

			_security = security;
			_getEntityTime = getEntityTime;
		}

		void IMarketDataStorage<TEntity>.Save(IEnumerable<TEntity> data)
		{
			throw new NotSupportedException();
		}

		void IMarketDataStorage<TEntity>.Delete(IEnumerable<TEntity> data)
		{
			throw new NotSupportedException();
		}

		IEnumerableEx<TEntity> IMarketDataStorage<TEntity>.Load(DateTime date)
		{
			if (typeof(TEntity) != typeof(Candle))
				return Load(date).ToEntities<TMessage, TEntity>(_security);

			var messages = Load(date);

			return messages
				.Cast<CandleMessage>()
				.ToEx(messages.Count)
				.ToCandles<TEntity>(_security)
				.ToEx(messages.Count);
		}

		IDataStorageReader<TEntity> IMarketDataStorage<TEntity>.GetReader(DateTime date)
		{
			throw new NotImplementedException();
		}

		IMarketDataSerializer<TEntity> IMarketDataStorage<TEntity>.Serializer
		{
			get { throw new NotSupportedException(); }
		}

		DateTimeOffset IMarketDataStorageInfo.GetTime(object data)
		{
			return _getEntityTime((TEntity)data);
		}

		DateTimeOffset IMarketDataStorageInfo<TEntity>.GetTime(TEntity data)
		{
			return _getEntityTime(data);
		}
	}
}
namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Интерфейс, описывающий хранилище маркет-данных для непрерывного инструмента.
	/// </summary>
	public interface IContinuousSecurityMarketDataStorage
	{
		/// <summary>
		/// Получить хранилище для составного инструмента.
		/// </summary>
		/// <param name="date">Дата.</param>
		/// <returns>Хранилище составного инструмента.</returns>
		IMarketDataStorage GetStorage(DateTime date);
	}

	class ContinuousSecurityMarketDataStorage<T> : IMarketDataStorage<T>, IMarketDataStorageInfo<T>, IContinuousSecurityMarketDataStorage
	{
		private readonly Func<Security, IMarketDataDrive, IMarketDataStorage<T>> _getStorage;
		private readonly ContinuousSecurity _security;
		private readonly Func<T, Security> _getSecurity;
		private readonly Func<T, DateTimeOffset> _getTime;

		public ContinuousSecurityMarketDataStorage(ContinuousSecurity security, object arg, Func<T, DateTimeOffset> getTime, Func<T, Security> getSecurity, Func<Security, IMarketDataDrive, IMarketDataStorage<T>> getStorage, IMarketDataStorageDrive drive)
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

			_getStorage = getStorage;
			_security = security;
			_getTime = getTime;
			_getSecurity = getSecurity;
			
			_arg = arg;
			Drive = drive;
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
			get
			{
				return from innerSecurity in _security.InnerSecurities
					let range = _security.ExpirationJumps.GetActivityRange(innerSecurity)
					//let dates = _getStorage(innerSecurity, Drive.Drive).Dates.ToArray()
					from date in _getStorage(innerSecurity, Drive.Drive).Dates.Where(d => range.Contains(d))
					select date;
			}
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
			set
			{
				_appendOnlyNew = value;
				_security.InnerSecurities.ForEach(s => _getStorage(s, Drive.Drive).AppendOnlyNew = value);
			}
		}

		/// <summary>
		/// Сохранить маркет-данные в хранилище.
		/// </summary>
		/// <param name="data">Маркет-данные.</param>
		public void Save(IEnumerable<T> data)
		{
			foreach (var group in data.GroupBy(_getSecurity))
			{
				_getStorage(group.Key, Drive.Drive).Save(group);
			}
		}

		/// <summary>
		/// Удалить маркет-данные из хранилища.
		/// </summary>
		/// <param name="data">Маркет-данные, которые необходимо удалить.</param>
		public void Delete(IEnumerable<T> data)
		{
			foreach (var group in data.GroupBy(d => _getTime(d).Date))
			{
				GetStorage(group.Key).Delete(group);
			}
		}

		void IMarketDataStorage.Delete(IEnumerable data)
		{
			Delete((IEnumerable<T>)data);
		}

		void IMarketDataStorage.Delete(DateTime date)
		{
			GetStorage(date).Delete(date);
		}

		void IMarketDataStorage.Save(IEnumerable data)
		{
			Save((IEnumerable<T>)data);
		}

		IEnumerable IMarketDataStorage.Load(DateTime date)
		{
			return Load(date);
		}

		IMarketDataMetaInfo IMarketDataStorage.GetMetaInfo(DateTime date)
		{
			return GetStorage(date).GetMetaInfo(date);
		}

		/// <summary>
		/// Загрузить данные.
		/// </summary>
		/// <param name="date">Дата, для которой необходимо загрузить данные.</param>
		/// <returns>Данные. Если данных не существует, то будет возвращено пустое множество.</returns>
		public IEnumerableEx<T> Load(DateTime date)
		{
			return GetStorage(date).Load(date);
		}

		/// <summary>
		/// Получить считыватель данных за указанную дату.
		/// </summary>
		/// <param name="date">Дата, для которой необходимо загрузить данные.</param>
		/// <returns>Считыватель данных.</returns>
		public IDataStorageReader<T> GetReader(DateTime date)
		{
			return GetStorage(date).GetReader(date);
		}

		private IMarketDataStorage<T> GetStorage(DateTime date)
		{
			return _getStorage(_security.GetSecurity(date), Drive.Drive);
		}

		IMarketDataStorage IContinuousSecurityMarketDataStorage.GetStorage(DateTime date)
		{
			return GetStorage(date);
		}

		DateTimeOffset IMarketDataStorageInfo.GetTime(object data)
		{
			return _getTime((T)data);
		}

		DateTimeOffset IMarketDataStorageInfo<T>.GetTime(T data)
		{
			return _getTime(data);
		}
	}

	class ConvertableContinuousSecurityMarketDataStorage<TMessage, TEntity> : ContinuousSecurityMarketDataStorage<TMessage>, IMarketDataStorage<TEntity>, IMarketDataStorageInfo<TEntity>
		where TMessage : Message
	{
		private readonly ContinuousSecurity _security;
		private readonly Func<TEntity, TMessage> _toMessage;
		private readonly Func<TEntity, DateTimeOffset> _getEntityTime;

		public ConvertableContinuousSecurityMarketDataStorage(ContinuousSecurity security, 
			object arg,
			Func<TMessage, DateTimeOffset> getTime, 
			Func<TMessage, Security> getSecurity,
			Func<TEntity, TMessage> toMessage,
			Func<TEntity, DateTimeOffset> getEntityTime,
			Func<Security, IMarketDataDrive, IMarketDataStorage<TMessage>> getStorage, 
			IMarketDataStorageDrive drive)
			: base(security, arg, getTime, getSecurity, getStorage, drive)
		{
			if (toMessage == null)
				throw new ArgumentNullException("toMessage");

			if (getEntityTime == null)
				throw new ArgumentNullException("getEntityTime");

			_security = security;
			_toMessage = toMessage;
			_getEntityTime = getEntityTime;
		}

		void IMarketDataStorage<TEntity>.Save(IEnumerable<TEntity> data)
		{
			Save(data.Select(_toMessage));
		}

		void IMarketDataStorage<TEntity>.Delete(IEnumerable<TEntity> data)
		{
			Delete(data.Select(_toMessage));
		}

		IEnumerableEx<TEntity> IMarketDataStorage<TEntity>.Load(DateTime date)
		{
			if (typeof(TEntity) == typeof(Candle))
			{
				var messages = Load(date);

				return messages
					.Cast<CandleMessage>()
					.ToEx(messages.Count)
					.ToCandles<TEntity>(_security.GetSecurity(date))
					.ToEx(messages.Count);
			}
			else
				return Load(date).ToEntities<TMessage, TEntity>(_security.GetSecurity(date));
		}

		IDataStorageReader<TEntity> IMarketDataStorage<TEntity>.GetReader(DateTime date)
		{
			throw new NotImplementedException();
		}

		IMarketDataSerializer<TEntity> IMarketDataStorage<TEntity>.Serializer
		{
			get { throw new NotSupportedException(); }
		}

		DateTimeOffset IMarketDataStorageInfo<TEntity>.GetTime(TEntity data)
		{
			return _getEntityTime(data);
		}
	}

	class CandleContinuousSecurityMarketDataStorage<TCandle> : ContinuousSecurityMarketDataStorage<CandleMessage>, IMarketDataStorage<Candle>, IMarketDataStorageInfo<Candle>
	{
		private readonly ContinuousSecurity _security;
		private readonly object _arg;

		public CandleContinuousSecurityMarketDataStorage(ContinuousSecurity security,
			object arg,
			Func<CandleMessage, DateTimeOffset> getTime,
			Func<CandleMessage, Security> getSecurity,
			Func<Security, IMarketDataDrive, IMarketDataStorage<CandleMessage>> getStorage,
			IMarketDataStorageDrive drive)
			: base(security, arg, getTime, getSecurity, getStorage, drive)
		{
			_security = security;
			_arg = arg;
		}

		void IMarketDataStorage<Candle>.Save(IEnumerable<Candle> data)
		{
			Save(data.Select(c => c.ToMessage()));
		}

		void IMarketDataStorage<Candle>.Delete(IEnumerable<Candle> data)
		{
			Delete(data.Select(c => c.ToMessage()));
		}

		IEnumerableEx<Candle> IMarketDataStorage<Candle>.Load(DateTime date)
		{
			var messages = Load(date);

			return messages
				.ToCandles<TCandle>(_security.GetSecurity(date))
				.Cast<Candle>()
				.ToEx(messages.Count);
		}

		IDataStorageReader<Candle> IMarketDataStorage<Candle>.GetReader(DateTime date)
		{
			throw new NotImplementedException();
		}

		IMarketDataSerializer<Candle> IMarketDataStorage<Candle>.Serializer
		{
			get { throw new NotSupportedException(); }
		}

		DateTimeOffset IMarketDataStorageInfo<Candle>.GetTime(Candle data)
		{
			return data.OpenTime;
		}
	}
}

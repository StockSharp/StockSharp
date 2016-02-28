#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: ContinuousSecurityMarketDataStorage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The interface, describing the storage of market-date of continuous instrument.
	/// </summary>
	public interface IContinuousSecurityMarketDataStorage
	{
		/// <summary>
		/// To get storage for the composite instrument.
		/// </summary>
		/// <param name="date">Date.</param>
		/// <returns>The storage of the composite instrument.</returns>
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
				throw new ArgumentNullException(nameof(security));

			if (getTime == null)
				throw new ArgumentNullException(nameof(getTime));

			if (getSecurity == null)
				throw new ArgumentNullException(nameof(getSecurity));

			if (getStorage == null)
				throw new ArgumentNullException(nameof(getStorage));

			if (drive == null)
				throw new ArgumentNullException(nameof(drive));

			_getStorage = getStorage;
			_security = security;
			_getTime = getTime;
			_getSecurity = getSecurity;
			
			_arg = arg;
			Drive = drive;
		}

		IMarketDataSerializer IMarketDataStorage.Serializer => ((IMarketDataStorage<T>)this).Serializer;

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
					from date in _getStorage(innerSecurity, Drive.Drive).Dates.Where(d => range.Contains(d.ApplyTimeZone(TimeZoneInfo.Utc)))
					select date;
			}
		}

		Type IMarketDataStorage.DataType => typeof(T);

		Security IMarketDataStorage.Security => _security;

		private readonly object _arg;

		object IMarketDataStorage.Arg => _arg;

		public IMarketDataStorageDrive Drive { get; }

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
		/// To save market data in storage.
		/// </summary>
		/// <param name="data">Market data.</param>
		/// <returns>Count of saved data.</returns>
		public int Save(IEnumerable<T> data)
		{
			var count = 0;
			
			foreach (var group in data.GroupBy(_getSecurity))
			{
				count += _getStorage(group.Key, Drive.Drive).Save(group);
			}

			return count;
		}

		/// <summary>
		/// To delete market data from storage.
		/// </summary>
		/// <param name="data">Market data to be deleted.</param>
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

		int IMarketDataStorage.Save(IEnumerable data)
		{
			return Save((IEnumerable<T>)data);
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
		/// To load data.
		/// </summary>
		/// <param name="date">Date, for which data shall be loaded.</param>
		/// <returns>Data. If there is no data, the empty set will be returned.</returns>
		public IEnumerable<T> Load(DateTime date)
		{
			return GetStorage(date).Load(date);
		}

		private IMarketDataStorage<T> GetStorage(DateTime date)
		{
			return _getStorage(_security.GetSecurity(date.ApplyTimeZone(TimeZoneInfo.Utc)), Drive.Drive);
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
				throw new ArgumentNullException(nameof(toMessage));

			if (getEntityTime == null)
				throw new ArgumentNullException(nameof(getEntityTime));

			_security = security;
			_toMessage = toMessage;
			_getEntityTime = getEntityTime;
		}

		int IMarketDataStorage<TEntity>.Save(IEnumerable<TEntity> data)
		{
			return Save(data.Select(_toMessage));
		}

		void IMarketDataStorage<TEntity>.Delete(IEnumerable<TEntity> data)
		{
			Delete(data.Select(_toMessage));
		}

		IEnumerable<TEntity> IMarketDataStorage<TEntity>.Load(DateTime date)
		{
			var security = _security.GetSecurity(date.ApplyTimeZone(TimeZoneInfo.Utc));

			if (typeof(TEntity) == typeof(Candle))
			{
				var messages = Load(date);

				return messages
					.Cast<CandleMessage>()
					.ToCandles<TEntity>(security);
			}
			else
				return Load(date).ToEntities<TMessage, TEntity>(security);
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

		int IMarketDataStorage<Candle>.Save(IEnumerable<Candle> data)
		{
			return Save(data.Select(c => c.ToMessage()));
		}

		void IMarketDataStorage<Candle>.Delete(IEnumerable<Candle> data)
		{
			Delete(data.Select(c => c.ToMessage()));
		}

		IEnumerable<Candle> IMarketDataStorage<Candle>.Load(DateTime date)
		{
			var messages = Load(date);

			return messages
				.ToCandles<TCandle>(_security.GetSecurity(date.ApplyTimeZone(TimeZoneInfo.Utc)))
				.Cast<Candle>();
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

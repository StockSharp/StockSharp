#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: AllSecurityMarketDataStorage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Interop;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	class AllSecurityMarketDataStorage<T> : IMarketDataStorage<T>, IMarketDataStorageInfo<T>
		where T : Message
	{
		private readonly Security _security;
		private readonly IExchangeInfoProvider _exchangeInfoProvider;
		private readonly BasketMarketDataStorage<T> _basket;

		public AllSecurityMarketDataStorage(Security security,
			object arg,
			Func<T, DateTimeOffset> getTime,
			Func<T, Security> getSecurity,
			Func<Security, IMarketDataDrive, IMarketDataStorage<T>> getStorage, 
			IMarketDataStorageDrive drive,
			IExchangeInfoProvider exchangeInfoProvider)
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

			if (exchangeInfoProvider == null)
				throw new ArgumentNullException(nameof(exchangeInfoProvider));

			_security = security;
			_exchangeInfoProvider = exchangeInfoProvider;
			_getTime = getTime;

			_arg = arg;
			Drive = drive;

			_basket = new BasketMarketDataStorage<T>();

			var id = security.Id.ToSecurityId();
			var code = id.SecurityCode;

			var securities = InteropHelper
				.GetDirectories(Path.Combine(Drive.Drive.Path, code.Substring(0, 1)), code + "*")
				.Select(p => Path.GetFileName(p).FolderNameToSecurityId())
				.Select(s =>
				{
					var idInfo = s.ToSecurityId();

					var clone = security.Clone();
					clone.Id = s;
					clone.Board = _exchangeInfoProvider.GetOrCreateBoard(idInfo.BoardCode);
					return clone;
				});

			foreach (var sec in securities)
				_basket.InnerStorages.Add(getStorage(sec, Drive.Drive));
		}

		IMarketDataSerializer IMarketDataStorage.Serializer => ((IMarketDataStorage<T>)this).Serializer;

		IMarketDataSerializer<T> IMarketDataStorage<T>.Serializer
		{
			get { throw new NotSupportedException(); }
		}

		IEnumerable<DateTime> IMarketDataStorage.Dates => Drive.Dates;

		Type IMarketDataStorage.DataType => typeof(T);

		Security IMarketDataStorage.Security => _security;

		private readonly object _arg;

		object IMarketDataStorage.Arg => _arg;

		public IMarketDataStorageDrive Drive { get; }

		bool IMarketDataStorage.AppendOnlyNew { get; set; } = true;

		/// <summary>
		/// To save market data in storage.
		/// </summary>
		/// <param name="data">Market data.</param>
		public int Save(IEnumerable<T> data)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// To delete market data from storage.
		/// </summary>
		/// <param name="data">Market data to be deleted.</param>
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

		int IMarketDataStorage.Save(IEnumerable data)
		{
			throw new NotSupportedException();
		}

		IEnumerable IMarketDataStorage.Load(DateTime date)
		{
			return Load(date);
		}

		IMarketDataMetaInfo IMarketDataStorage.GetMetaInfo(DateTime date)
		{
			return _basket.InnerStorages.First().GetMetaInfo(date);
		}

		/// <summary>
		/// To load data.
		/// </summary>
		/// <param name="date">Date, for which data shall be loaded.</param>
		/// <returns>Data. If there is no data, the empty set will be returned.</returns>
		public IEnumerable<T> Load(DateTime date)
		{
			return _basket.Load(date);
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
			IMarketDataStorageDrive drive,
			IExchangeInfoProvider exchangeInfoProvider)
			: base(security, arg, getTime, getSecurity, getStorage, drive, exchangeInfoProvider)
		{
			if (getEntityTime == null)
				throw new ArgumentNullException(nameof(getEntityTime));

			_security = security;
			_getEntityTime = getEntityTime;
		}

		int IMarketDataStorage<TEntity>.Save(IEnumerable<TEntity> data)
		{
			throw new NotSupportedException();
		}

		void IMarketDataStorage<TEntity>.Delete(IEnumerable<TEntity> data)
		{
			throw new NotSupportedException();
		}

		IEnumerable<TEntity> IMarketDataStorage<TEntity>.Load(DateTime date)
		{
			if (typeof(TEntity) != typeof(Candle))
				return Load(date).ToEntities<TMessage, TEntity>(_security);

			var messages = Load(date);

			return messages
				.Cast<CandleMessage>()
				.ToCandles<TEntity>(_security);
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
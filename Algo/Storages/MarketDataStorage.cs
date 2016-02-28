#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: MarketDataStorage.cs
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
	using System.Reflection;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	interface IMarketDataStorageInfo
	{
		DateTimeOffset GetTime(object data);
	}

	interface IMarketDataStorageInfo<in TData> : IMarketDataStorageInfo
	{
		DateTimeOffset GetTime(TData data);
	}

	[Obfuscation(Feature = "Apply to member * when abstract: renaming", Exclude = true)]
	abstract class MarketDataStorage<TData, TId> : IMarketDataStorage<TData>, IMarketDataStorageInfo<TData>
	{
		private readonly Func<TData, DateTimeOffset> _getTime;
		private readonly Func<TData, SecurityId> _getSecurityId;
		private readonly Func<TData, TId> _getId;
		private readonly SynchronizedDictionary<DateTime, SyncObject> _syncRoots = new SynchronizedDictionary<DateTime, SyncObject>();

		protected MarketDataStorage(Security security, object arg, Func<TData, DateTimeOffset> getTime, Func<TData, SecurityId> getSecurity, Func<TData, TId> getId, IMarketDataSerializer<TData> serializer, IMarketDataStorageDrive drive)
			: this(security.ToSecurityId(), arg, getTime, getSecurity, getId, serializer, drive)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (security.Id.IsEmpty())
				throw new ArgumentException(LocalizedStrings.Str1025, nameof(security));

			Security = security;
		}

		protected MarketDataStorage(SecurityId securityId, object arg, Func<TData, DateTimeOffset> getTime, Func<TData, SecurityId> getSecurityId, Func<TData, TId> getId, IMarketDataSerializer<TData> serializer, IMarketDataStorageDrive drive)
		{
			if (securityId.IsDefault())
				throw new ArgumentException(LocalizedStrings.Str1025, nameof(securityId));

			if (getTime == null)
				throw new ArgumentNullException(nameof(getTime));

			if (getSecurityId == null)
				throw new ArgumentNullException(nameof(getSecurityId));

			if (getId == null)
				throw new ArgumentNullException(nameof(getId));

			if (serializer == null)
				throw new ArgumentNullException(nameof(serializer));

			if (drive == null)
				throw new ArgumentNullException(nameof(drive));

			SecurityId = securityId;

			AppendOnlyNew = true;

			_getTime = getTime;
			_getSecurityId = getSecurityId;
			_getId = getId;
			Drive = drive;
			Serializer = serializer;
			_arg = arg;
		}

		IEnumerable<DateTime> IMarketDataStorage.Dates => Drive.Dates;

		Type IMarketDataStorage.DataType => typeof(TData);

		public SecurityId SecurityId { get; }

		public Security Security { get; }

		private readonly object _arg;

		object IMarketDataStorage.Arg => _arg;

		public bool AppendOnlyNew { get; set; }

		IMarketDataSerializer IMarketDataStorage.Serializer => Serializer;
		public IMarketDataSerializer<TData> Serializer { get; }

		public IMarketDataStorageDrive Drive { get; }

		private DateTime GetTruncatedTime(TData data)
		{
			return _getTime(data).Truncate().UtcDateTime;
		}

		private SyncObject GetSync(DateTime time)
		{
			return _syncRoots.SafeAdd(time);
		}

		private Stream LoadStream(DateTime date)
		{
			return Drive.LoadStream(date);
		}

		public int Save(IEnumerable<TData> data)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			var count = 0;

			foreach (var group in data.GroupBy(d =>
			{
				var security = _getSecurityId(d);

				if (!security.IsDefault() && (security.SecurityCode != SecurityId.SecurityCode || security.BoardCode != SecurityId.BoardCode))
					throw new ArgumentException(LocalizedStrings.Str1026Params.Put(typeof(TData).Name, security, SecurityId));

				var time = _getTime(d);

				if (time == DateTimeOffset.MinValue)
					throw new ArgumentException(LocalizedStrings.EmptyMessageTime.Put(d));

				return time.UtcDateTime.Date;
			}))
			{
				var date = group.Key;
				var newItems = group.OrderBy(_getTime).ToArray();

				lock (GetSync(date))
				{
					var stream = LoadStream(date);

					try
					{
						var metaInfo = GetInfo(stream, date);

						if (metaInfo == null)
						{
							stream = new MemoryStream();
							metaInfo = Serializer.CreateMetaInfo(date);
						}

						count += Save(stream, metaInfo, newItems, false);

						if (!(stream is MemoryStream))
							continue;

						stream.Position = 0;
						Drive.SaveStream(date, stream);
					}
					finally
					{
						stream.Dispose();
					}
				}
			}

			return count;
		}

		private int Save(Stream stream, IMarketDataMetaInfo metaInfo, TData[] data, bool isOverride)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			if (metaInfo == null)
				throw new ArgumentNullException(nameof(metaInfo));

			if (data == null)
				throw new ArgumentNullException(nameof(data));

			if (data.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(data));

			if (metaInfo.Count == 0)
			{
				var time = GetTruncatedTime(data[0]);

				metaInfo.PriceStep = Security.PriceStep ?? 0.01m;
				metaInfo.VolumeStep = Security.VolumeStep ?? 1m;
				metaInfo.LastTime = time;
				metaInfo.FirstTime = time;
			}
			else
			{
				if (AppendOnlyNew)
				{
					data = FilterNewData(data, metaInfo).ToArray();

					if (data.IsEmpty())
						return 0;
				}
			}

			var newDayData = new MemoryStream();

			Serializer.Serialize(newDayData, data, metaInfo);

			if (isOverride)
				metaInfo.Count = data.Length;
			else
				metaInfo.Count += data.Length;

			stream.Position = 0;
			metaInfo.Write(stream);

			if (isOverride)
				stream.SetLength(stream.Position);
			else
				stream.Position = stream.Length;

			newDayData.Position = 0;
			stream.WriteRaw(newDayData.To<byte[]>());

			return data.Length;
		}

		protected virtual IEnumerable<TData> FilterNewData(IEnumerable<TData> data, IMarketDataMetaInfo metaInfo)
		{
			var lastTime = metaInfo.LastTime;
			return data.Where(i => GetTruncatedTime(i) >= lastTime);
		}

		int IMarketDataStorage.Save(IEnumerable data)
		{
			return Save(data.Cast<TData>());
		}

		void IMarketDataStorage.Delete(IEnumerable data)
		{
			Delete(data.Cast<TData>());
		}

		public void Delete(IEnumerable<TData> data)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			foreach (var group in data.GroupBy(i => _getTime(i).UtcDateTime.Date))
			{
				var date = group.Key;

				lock (GetSync(date))
				{
					var stream = LoadStream(date);

					try
					{
						var metaInfo = GetInfo(stream, date);

						if (metaInfo == null)
							continue;

						var count = metaInfo.Count;

						if (count != group.Count())
						{
							var loadedData = new Dictionary<TId, List<TData>>();

							foreach (var item in Serializer.Deserialize(stream, metaInfo))
							{
								var id = _getId(item);

								var loadedItems = loadedData.TryGetValue(id);

								if (loadedItems == null)
								{
									loadedItems = new List<TData> { item };
									loadedData.Add(id, loadedItems);
								}
								else
									loadedItems.Add(item);
							}

							foreach (var item in group)
								loadedData.Remove(_getId(item));

							if (loadedData.Count > 0)
							{
								// повторная иницилизация потока, так как предыдущий раз он был закрыл выше
								// при десериализации
								stream = LoadStream(date);

								Save(stream, Serializer.CreateMetaInfo(date),
									loadedData.Values.SelectMany(l => l).ToArray(), true);

								stream.Dispose();
								stream = null;
							}
							else
							{
								((IMarketDataStorage)this).Delete(date);
								stream = null;
							}
						}
						else
						{
							stream.Dispose();
							stream = null;

							((IMarketDataStorage)this).Delete(date);
						}
					}
					catch
					{
						stream?.Dispose();
						throw;
					}
				}
			}
		}

		public IEnumerable<TData> Load(DateTime date)
		{
			lock (GetSync(date))
			{
				var stream = LoadStream(date);

				try
				{
					var metaInfo = GetInfo(stream, date);

					if (metaInfo == null)
						return Enumerable.Empty<TData>();

					// нельзя закрывать поток, так как из него будут читаться данные через энумератор
					//using (stream)
					return Serializer.Deserialize(stream, metaInfo);
				}
				catch (Exception)
				{
					stream.Dispose();
					throw;
				}
			}
		}

		IMarketDataMetaInfo IMarketDataStorage.GetMetaInfo(DateTime date)
		{
			lock (GetSync(date))
			{
				using (var stream = LoadStream(date))
					return GetInfo(stream, date);
			}
		}

		private IMarketDataMetaInfo GetInfo(Stream stream, DateTime date)
		{
			if (stream == Stream.Null)
				return null;

			var metaInfo = Serializer.CreateMetaInfo(date);
			metaInfo.Read(stream);

			return metaInfo;
		}

		void IMarketDataStorage.Delete(DateTime date)
		{
			lock (GetSync(date))
				Drive.Delete(date);
		}

		IEnumerable IMarketDataStorage.Load(DateTime date)
		{
			return Load(date);
		}

		DateTimeOffset IMarketDataStorageInfo<TData>.GetTime(TData data)
		{
			return _getTime(data);
		}

		DateTimeOffset IMarketDataStorageInfo.GetTime(object data)
		{
			return _getTime((TData)data);
		}
	}
}
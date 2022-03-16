namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

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

	abstract class MarketDataStorage<TMessage, TId> : IMarketDataStorage<TMessage>, IMarketDataStorageInfo<TMessage>
		where TMessage : Message
	{
		private readonly Func<TMessage, DateTimeOffset> _getTime;
		private readonly Func<TMessage, SecurityId> _getSecurityId;
		private readonly Func<TMessage, TId> _getId;
		private readonly Func<TMessage, bool> _isValid;
		private readonly SynchronizedDictionary<DateTime, SyncObject> _syncRoots = new();
		private readonly SynchronizedDictionary<DateTime, IMarketDataMetaInfo> _dateMetaInfos = new();

		protected MarketDataStorage(SecurityId securityId, object arg, Func<TMessage, DateTimeOffset> getTime, Func<TMessage, SecurityId> getSecurityId, Func<TMessage, TId> getId, IMarketDataSerializer<TMessage> serializer, IMarketDataStorageDrive drive, Func<TMessage, bool> isValid)
		{
			_dataType = DataType.Create(typeof(TMessage), arg);

			if (_dataType.IsSecurityRequired && securityId == default)
				throw new ArgumentException(LocalizedStrings.Str1025, nameof(securityId));

			SecurityId = securityId;

			AppendOnlyNew = true;

			_getTime = getTime ?? throw new ArgumentNullException(nameof(getTime));
			_getSecurityId = getSecurityId ?? throw new ArgumentNullException(nameof(getSecurityId));
			_getId = getId ?? throw new ArgumentNullException(nameof(getId));
			Drive = drive ?? throw new ArgumentNullException(nameof(drive));
			Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
			_isValid = isValid ?? throw new ArgumentNullException(nameof(isValid));
		}

		IEnumerable<DateTime> IMarketDataStorage.Dates => Drive.Dates;

		private readonly DataType _dataType;
		DataType IMarketDataStorage.DataType => _dataType;

		public SecurityId SecurityId { get; }

		public bool AppendOnlyNew { get; set; }

		IMarketDataSerializer IMarketDataStorage.Serializer => Serializer;
		public IMarketDataSerializer<TMessage> Serializer { get; }

		public IMarketDataStorageDrive Drive { get; }

		protected DateTime GetTruncatedTime(TMessage data) => _getTime(data).StorageTruncate(Serializer.TimePrecision).UtcDateTime;

		private SyncObject GetSync(DateTime time) => _syncRoots.SafeAdd(time);

		private Stream LoadStream(DateTime date) => Drive.LoadStream(date);

		private bool SecurityIdEqual(SecurityId securityId) => securityId.SecurityCode.EqualsIgnoreCase(SecurityId.SecurityCode) && securityId.BoardCode.EqualsIgnoreCase(SecurityId.BoardCode);

		private DateTime GetStorageDate(DateTimeOffset dto) => dto.UtcDateTime.Date;

		public int Save(IEnumerable<TMessage> data)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			var count = 0;

			foreach (var group in data.Where(_isValid).GroupBy(d =>
			{
				var securityId = _getSecurityId(d);

				if (!securityId.IsDefault() && !SecurityIdEqual(securityId))
					throw new ArgumentException(LocalizedStrings.Str1026Params.Put(typeof(TMessage).Name, securityId, SecurityId));

				var time = _getTime(d);

				if (time == DateTimeOffset.MinValue)
					throw new ArgumentException(LocalizedStrings.EmptyMessageTime.Put(d));

				return GetStorageDate(time);
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

						var diff = Save(stream, metaInfo, newItems, false);

						if (diff == 0)
							continue;

						count += diff;

						if (stream is not MemoryStream)
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

		private int Save(Stream stream, IMarketDataMetaInfo metaInfo, TMessage[] data, bool isOverride)
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
				data = FilterNewData(data, metaInfo).ToArray();

				if (data.IsEmpty())
					return 0;

				var time = GetTruncatedTime(data[0]);

				//var priceStep = Security.PriceStep;
				//var volumeStep = Security.VolumeStep;

				//metaInfo.PriceStep = priceStep == null || priceStep == 0 ? 0.01m : priceStep.Value;
				//metaInfo.VolumeStep = volumeStep == null || volumeStep == 0 ? 1m : volumeStep.Value;
				metaInfo.LastTime = time;
				metaInfo.FirstTime = time;

				/*metaInfo.FirstPriceStep = */((MetaInfo)metaInfo).LastPriceStep = metaInfo.PriceStep;
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

			if (!isOverride && _dataType == DataType.MarketDepth)
			{
				var isEmpty = metaInfo.Count == 0;
				var isIncremental = default(bool?);

				for (int i = 0; i < data.Length; i++)
				{
					if (data[i] is QuoteChangeMessage quoteMsg)
					{
						if (isEmpty)
						{
							if (isIncremental == null)
								isIncremental = quoteMsg.State != null;
							else
							{
								if (isIncremental.Value)
								{
									if (quoteMsg.State == null)
										throw new InvalidOperationException(LocalizedStrings.StorageRequiredIncremental.Put(true));
								}
								else
								{
									if (quoteMsg.State != null)
										throw new InvalidOperationException(LocalizedStrings.StorageRequiredIncremental.Put(false));
								}
							}
						}

						if (!quoteMsg.IsSorted)
							data[i] = quoteMsg.TypedClone().TrySort().To<TMessage>();
					}
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

			if (isOverride || metaInfo.IsOverride)
				stream.SetLength(stream.Position);
			else
				stream.Position = stream.Length;

			newDayData.Position = 0;
			stream.WriteRaw(newDayData.To<byte[]>());

			return data.Length;
		}

		protected virtual IEnumerable<TMessage> FilterNewData(IEnumerable<TMessage> data, IMarketDataMetaInfo metaInfo)
		{
			var lastTime = metaInfo.LastTime;

			foreach (var item in data)
			{
				var time = GetTruncatedTime(item);

				if (time < lastTime)
					continue;

				lastTime = time;
				yield return item;
			}
		}

		int IMarketDataStorage.Save(IEnumerable<Message> data)
		{
			return Save(data.Cast<TMessage>());
		}

		void IMarketDataStorage.Delete(IEnumerable<Message> data)
		{
			Delete(data.Cast<TMessage>());
		}

		public void Delete(IEnumerable<TMessage> data)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			foreach (var group in data.GroupBy(i => GetStorageDate(_getTime(i))))
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
							var loadedData = new Dictionary<TId, List<TMessage>>();

							foreach (var item in Serializer.Deserialize(stream, metaInfo))
							{
								var id = _getId(item);

								var loadedItems = loadedData.TryGetValue(id);

								if (loadedItems == null)
								{
									loadedItems = new List<TMessage> { item };
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

		public IEnumerable<TMessage> Load(DateTime date)
		{
			date = date.Date;

			lock (GetSync(date))
			{
				var stream = LoadStream(date);

				try
				{
					var metaInfo = GetInfo(stream, date);

					if (metaInfo == null)
						return Enumerable.Empty<TMessage>();

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
			date = date.Date;

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

			IMarketDataMetaInfo metaInfo;

			if (Serializer.Format == StorageFormats.Csv)
			{
				metaInfo = _dateMetaInfos.SafeAdd(date, d =>
				{
					var info = Serializer.CreateMetaInfo(date);
					info.Read(stream);
					return info;
				});
			}
			else
			{
				metaInfo = Serializer.CreateMetaInfo(date);
				metaInfo.Read(stream);
			}

			return metaInfo;
		}

		void IMarketDataStorage.Delete(DateTime date)
		{
			date = date.Date;

			lock (GetSync(date))
				Drive.Delete(date);
		}

		IEnumerable<Message> IMarketDataStorage.Load(DateTime date)
		{
			return Load(date);
		}

		DateTimeOffset IMarketDataStorageInfo<TMessage>.GetTime(TMessage data)
		{
			return _getTime(data);
		}

		DateTimeOffset IMarketDataStorageInfo.GetTime(object data)
		{
			return _getTime((TMessage)data);
		}
	}
}
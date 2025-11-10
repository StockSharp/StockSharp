namespace StockSharp.Algo.Storages;

using Ecng.Linq;

abstract class MarketDataStorage<TMessage, TId> : IMarketDataStorage<TMessage>
	where TMessage : Message, IServerTimeMessage
{
	private readonly Func<TMessage, SecurityId> _getSecurityId;
	private readonly Func<TMessage, TId> _getId;
	private readonly Func<TMessage, bool> _isValid;
	private readonly SynchronizedDictionary<DateTime, SyncObject> _syncRoots = [];
	private readonly SynchronizedDictionary<DateTime, IMarketDataMetaInfo> _dateMetaInfos = [];

	protected MarketDataStorage(SecurityId securityId, DataType dataType, Func<TMessage, SecurityId> getSecurityId, Func<TMessage, TId> getId, IMarketDataSerializer<TMessage> serializer, IMarketDataStorageDrive drive, Func<TMessage, bool> isValid)
	{
		_dataType = dataType ?? throw new ArgumentNullException(nameof(dataType));

		if (_dataType.IsSecurityRequired && securityId == default)
			throw new ArgumentException(LocalizedStrings.EmptySecId, nameof(securityId));

		SecurityId = securityId;

		AppendOnlyNew = true;

		_getSecurityId = getSecurityId ?? throw new ArgumentNullException(nameof(getSecurityId));
		_getId = getId ?? throw new ArgumentNullException(nameof(getId));
		Drive = drive ?? throw new ArgumentNullException(nameof(drive));
		Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
		_isValid = isValid ?? throw new ArgumentNullException(nameof(isValid));
	}

	ValueTask<IEnumerable<DateTime>> IMarketDataStorage.GetDatesAsync(CancellationToken cancellationToken) => Drive.GetDatesAsync(cancellationToken);

	private readonly DataType _dataType;
	DataType IMarketDataStorage.DataType => _dataType;

	public SecurityId SecurityId { get; }

	public bool AppendOnlyNew { get; set; }

	IMarketDataSerializer IMarketDataStorage.Serializer => Serializer;
	public IMarketDataSerializer<TMessage> Serializer { get; }

	public IMarketDataStorageDrive Drive { get; }

	protected DateTime GetTruncatedTime(TMessage data) => data.ServerTime.StorageTruncate(Serializer.TimePrecision);

	private SyncObject GetSync(DateTime time) => _syncRoots.SafeAdd(time);

	private Stream LoadStream(DateTime date, bool readOnly) => Drive.LoadStream(date, readOnly);

	private bool SecurityIdEqual(SecurityId securityId) => securityId.SecurityCode.EqualsIgnoreCase(SecurityId.SecurityCode) && securityId.BoardCode.EqualsIgnoreCase(SecurityId.BoardCode);

	public ValueTask<int> SaveAsync(IEnumerable<TMessage> data, CancellationToken cancellationToken)
	{
		if (data == null)
			throw new ArgumentNullException(nameof(data));

		var count = 0;

		foreach (var group in data.Where(_isValid).GroupBy(d =>
		{
			var securityId = _getSecurityId(d);

			if (securityId != default && !SecurityIdEqual(securityId))
				throw new ArgumentException(LocalizedStrings.SecIdMustBe.Put(typeof(TMessage).Name, securityId, SecurityId));

			var time = d.ServerTime;

			if (time == DateTime.MinValue)
				throw new ArgumentException(LocalizedStrings.EmptyMessageTime.Put(d));

			return time;
		}))
		{
			var date = group.Key;
			var newItems = group.OrderBy(e => e.ServerTime).ToArray();

			lock (GetSync(date))
			{
				var stream = LoadStream(date, false);

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

		return new(count);
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
			data = [.. FilterNewData(data, metaInfo)];

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
				data = [.. FilterNewData(data, metaInfo)];

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

	ValueTask<int> IMarketDataStorage.SaveAsync(IEnumerable<Message> data, CancellationToken cancellationToken)
	{
		return SaveAsync(data.Cast<TMessage>(), cancellationToken);
	}

	ValueTask IMarketDataStorage.DeleteAsync(IEnumerable<Message> data, CancellationToken cancellationToken)
	{
		return DeleteAsync(data.Cast<TMessage>(), cancellationToken);
	}

	public ValueTask DeleteAsync(IEnumerable<TMessage> data, CancellationToken cancellationToken)
	{
		if (data == null)
			throw new ArgumentNullException(nameof(data));

		foreach (var group in data.GroupBy(i => i.ServerTime))
		{
			var date = group.Key;

			lock (GetSync(date))
			{
				var stream = LoadStream(date, true);

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
								loadedItems = [item];
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
							stream = LoadStream(date, false);

							Save(stream, Serializer.CreateMetaInfo(date),
								[.. loadedData.Values.SelectMany(l => l)], true);

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

		return default;
	}

	public IAsyncEnumerable<TMessage> LoadAsync(DateTime date, CancellationToken cancellationToken)
	{
		date = date.Date;

		IEnumerable<TMessage> msgs;

		lock (GetSync(date))
		{
			var stream = LoadStream(date, true);

			try
			{
				var metaInfo = GetInfo(stream, date);

				if (metaInfo == null)
					msgs = [];
				else
				{
					// нельзя закрывать поток, так как из него будут читаться данные через энумератор
					//using (stream)
					msgs = Serializer.Deserialize(stream, metaInfo);
				}
			}
			catch (Exception)
			{
				stream.Dispose();
				throw;
			}
		}

		return msgs.ToAsyncEnumerable2(cancellationToken);
	}

	ValueTask<IMarketDataMetaInfo> IMarketDataStorage.GetMetaInfoAsync(DateTime date, CancellationToken cancellationToken)
	{
		date = date.Date;

		lock (GetSync(date))
		{
			using var stream = LoadStream(date, true);
			return new(GetInfo(stream, date));
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

	ValueTask IMarketDataStorage.DeleteAsync(DateTime date, CancellationToken cancellationToken)
	{
		date = date.Date;

		lock (GetSync(date))
		{
			Drive.Delete(date);
			_dateMetaInfos.Remove(date);
		}

		return default;
	}

	IAsyncEnumerable<Message> IMarketDataStorage.LoadAsync(DateTime date, CancellationToken cancellationToken)
	{
		return LoadAsync(date, cancellationToken);
	}
}
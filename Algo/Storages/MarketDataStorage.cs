namespace StockSharp.Algo.Storages;

using Ecng.Linq;

using Nito.AsyncEx;

abstract class MarketDataStorage<TMessage, TId> : IMarketDataStorage<TMessage>
	where TMessage : Message, IServerTimeMessage
{
	private readonly Func<TMessage, SecurityId> _getSecurityId;
	private readonly Func<TMessage, TId> _getId;
	private readonly Func<TMessage, bool> _isValid;
	private readonly SynchronizedDictionary<DateTime, AsyncReaderWriterLock> _locks = [];
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

	private AsyncReaderWriterLock GetLock(DateTime date) => _locks.SafeAdd(date);
	private AwaitableDisposable<IDisposable> GetReadSync(DateTime date, CancellationToken cancellationToken) => GetLock(date).ReaderLockAsync(cancellationToken);
	private AwaitableDisposable<IDisposable> GetWriteSync(DateTime date, CancellationToken cancellationToken) => GetLock(date).WriterLockAsync(cancellationToken);

	private ValueTask<Stream> LoadStreamAsync(DateTime date, bool readOnly, CancellationToken cancellationToken)
		=> Drive.LoadStreamAsync(date, readOnly, cancellationToken);

	private bool SecurityIdEqual(SecurityId securityId) => securityId.SecurityCode.EqualsIgnoreCase(SecurityId.SecurityCode) && securityId.BoardCode.EqualsIgnoreCase(SecurityId.BoardCode);

	public async ValueTask<int> SaveAsync(IEnumerable<TMessage> data, CancellationToken cancellationToken)
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

			return time.Date;
		}))
		{
			var date = group.Key;
			var newItems = group.OrderBy(e => e.ServerTime).ToArray();

			using var _ = await GetWriteSync(date, cancellationToken);

			var stream = await LoadStreamAsync(date, false, cancellationToken);

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
				await Drive.SaveStreamAsync(date, stream, cancellationToken);
			}
			finally
			{
				stream.Dispose();
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
		else if (AppendOnlyNew)
		{
			data = [.. FilterNewData(data, metaInfo)];

			if (data.IsEmpty())
				return 0;
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
		newDayData.CopyTo(stream);

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

	public async ValueTask DeleteAsync(IEnumerable<TMessage> data, CancellationToken cancellationToken)
	{
		if (data == null)
			throw new ArgumentNullException(nameof(data));

		foreach (var group in data.GroupBy(i => i.ServerTime.Date))
		{
			var date = group.Key;

			using var _ = await GetWriteSync(date, cancellationToken);

			var stream = await LoadStreamAsync(date, true, cancellationToken);

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

					stream.Dispose();
					stream = null;

					if (loadedData.Count > 0)
					{
						stream = await LoadStreamAsync(date, false, cancellationToken);

						Save(stream, Serializer.CreateMetaInfo(date),
							[.. loadedData.Values.SelectMany(l => l)], true);

						stream.Dispose();
						stream = null;
					}
					else
					{
						await ((IMarketDataStorage)this).DeleteAsync(date, cancellationToken);
					}
				}
				else
				{
					stream.Dispose();
					stream = null;

					await ((IMarketDataStorage)this).DeleteAsync(date, cancellationToken);
				}
			}
			finally
			{
				stream?.Dispose();
			}
		}
	}

	public async IAsyncEnumerable<TMessage> LoadAsync(DateTime date, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		date = date.Date;

		using var _ = await GetReadSync(date, cancellationToken);

		using var stream = await LoadStreamAsync(date, true, cancellationToken);

		var metaInfo = GetInfo(stream, date);

		if (metaInfo == null)
			yield break;

		var msgs = Serializer.Deserialize(stream, metaInfo);

		foreach (var msg in msgs)
		{
			cancellationToken.ThrowIfCancellationRequested();
			yield return msg;
		}
	}

	async ValueTask<IMarketDataMetaInfo> IMarketDataStorage.GetMetaInfoAsync(DateTime date, CancellationToken cancellationToken)
	{
		date = date.Date;

		using var _ = await GetReadSync(date, cancellationToken);

		using var stream = await LoadStreamAsync(date, true, cancellationToken);
		return GetInfo(stream, date);
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

	async ValueTask IMarketDataStorage.DeleteAsync(DateTime date, CancellationToken cancellationToken)
	{
		date = date.Date;

		using var _ = await GetWriteSync(date, cancellationToken);

		await Drive.DeleteAsync(date, cancellationToken);
		_dateMetaInfos.Remove(date);
	}

	IAsyncEnumerable<Message> IMarketDataStorage.LoadAsync(DateTime date, CancellationToken cancellationToken)
	{
		return LoadAsync(date, cancellationToken);
	}
}
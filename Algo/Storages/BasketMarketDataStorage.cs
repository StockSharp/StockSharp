namespace StockSharp.Algo.Storages;

using Ecng.Reflection;

/// <summary>
/// The aggregator-storage enumerator.
/// </summary>
/// <typeparam name="TMessage">Message type.</typeparam>
public interface IBasketMarketDataStorageEnumerable<TMessage> : IAsyncEnumerable<TMessage>
{
	/// <summary>
	/// Available message types.
	/// </summary>
	IEnumerable<MessageTypes> DataTypes { get; }
}

/// <summary>
/// The interface, describing a list of embedded storages of market data.
/// </summary>
public interface IBasketMarketDataStorageInnerList : ISynchronizedCollection<IMarketDataStorage>
{
	/// <summary>
	/// Add inner storage with the specified request id.
	/// </summary>
	/// <param name="storage">Market-data storage.</param>
	/// <param name="transactionId">The subscription identifier.</param>
	void Add(IMarketDataStorage storage, long transactionId);

	/// <summary>
	/// Remove inner storage.
	/// </summary>
	/// <param name="originalTransactionId">The subscription identifier.</param>
	void Remove(long originalTransactionId);
}

/// <summary>
/// The aggregator-storage, allowing to load data simultaneously from several market data storages.
/// </summary>
/// <typeparam name="TMessage">Message type.</typeparam>
public class BasketMarketDataStorage<TMessage> : Disposable, IMarketDataStorage<TMessage>
	where TMessage : Message
{
	private class BasketMarketDataStorageEnumerator : IAsyncEnumerator<TMessage>
	{
		private readonly BasketMarketDataStorage<TMessage> _storage;
		private readonly DateTime _date;
		private readonly CancellationToken _cancellationToken;
		private readonly SynchronizedQueue<(ActionTypes action, IMarketDataStorage storage, long transId)> _actions = [];
		private readonly Lock _enumsLock = new();
		private readonly Ecng.Collections.PriorityQueue<long, (IAsyncEnumerator<Message> enu, IMarketDataStorage storage, long transId)> _enumerators = new((p1, p2) => (p1 - p2).Abs());

		public BasketMarketDataStorageEnumerator(BasketMarketDataStorage<TMessage> storage, DateTime date, CancellationToken cancellationToken)
		{
			_storage = storage ?? throw new ArgumentNullException(nameof(storage));
			_date = date;
			_cancellationToken = cancellationToken;

			foreach (var s in storage._innerStorages.Cache)
			{
				if (s.GetType().GetGenericType(typeof(InMemoryMarketDataStorage<>)) == null && !s.GetDates().Contains(date))
					continue;

				_actions.Add((ActionTypes.Add, s, storage._innerStorages.TryGetTransactionId(s)));
			}

			_storage._enumerators.Add(this);
		}

		public TMessage Current { get; private set; }

		async ValueTask<bool> IAsyncEnumerator<TMessage>.MoveNextAsync()
		{
			while (true)
			{
				_cancellationToken.ThrowIfCancellationRequested();

				var action = _actions.TryDequeue2();

				if (action is null)
					break;

				var type = action.Value.action;
				var storage = action.Value.storage;

				switch (type)
				{
					case ActionTypes.Add:
					{
						if (_storage.Cache is not null)
							storage = new CacheableMarketDataStorage(storage, _storage.Cache);

						var loaded = storage.LoadAsync(_date, _cancellationToken);

						// built books slower for emulation, so this case is not real one
						//if (!_storage.PassThroughOrderBookIncrement && loaded is IEnumerable<QuoteChangeMessage> quotes)
						//{
						//	loaded = quotes.BuildIfNeed();
						//}

						await using var enu = loaded.GetAsyncEnumerator(_cancellationToken);
						var lastTime = Current?.GetServerTime() ?? DateTime.MinValue;

						var hasValues = true;

						// пропускаем данные, что меньше времени последнего сообщения (lastTime)
						while (true)
						{
							if (!await enu.MoveNextAsync())
							{
								hasValues = false;
								break;
							}

							var msg = enu.Current;

							if (msg.GetServerTime() >= lastTime)
								break;
						}

						// данных в хранилище нет больше последней даты
						if (hasValues)
						{
							using (_enumsLock.EnterScope())
								_enumerators.Enqueue(enu.Current.GetServerTime().Ticks, (enu, storage, action.Value.transId));
						}
						else
							await enu.DisposeAsync();

						break;
					}
					case ActionTypes.Remove:
					{
						using (_enumsLock.EnterScope())
							_enumerators.RemoveWhere(p => p.Item2.storage == storage);

						break;
					}
					case ActionTypes.Clear:
					{
						using (_enumsLock.EnterScope())
							_enumerators.Clear();

						break;
					}
					default:
						throw new InvalidOperationException(type.To<string>());
				}
			}

			(long, (IAsyncEnumerator<Message> enu, IMarketDataStorage, long transId) element) item;

			using (_enumsLock.EnterScope())
			{
				if (_enumerators.Count == 0)
					return false;

				item = _enumerators.Dequeue();
			}

			var element = item.element;

			var enumerator = element.enu;

			Current = TrySetTransactionId(enumerator.Current, element.transId);

			if (await enumerator.MoveNextAsync())
			{
				var serverTime = enumerator.Current.GetServerTime().Ticks;

				using (_enumsLock.EnterScope())
					_enumerators.Enqueue(serverTime, element);
			}
			else
				await enumerator.DisposeAsync();

			return true;
		}

		private static TMessage TrySetTransactionId(Message message, long transactionId)
		{
			if (transactionId > 0)
			{
				if (message is ISubscriptionIdMessage subscrMsg)
					subscrMsg.SetSubscriptionIds(subscriptionId: transactionId);
			}

			return (TMessage)message;
		}

		async ValueTask IAsyncDisposable.DisposeAsync()
		{
			foreach (var enumerator in _enumerators.CopyAndClear())
				await enumerator.Item2.enu.DisposeAsync();

			_actions.Clear();

			_storage._enumerators.Remove(this);

			GC.SuppressFinalize(this);
		}

		public void AddAction(ActionTypes type, IMarketDataStorage storage, long transactionId)
		{
			_actions.Add((type, storage, transactionId));
		}
	}

	private sealed class BasketEnumerable : IBasketMarketDataStorageEnumerable<TMessage>
	{
		private readonly BasketMarketDataStorage<TMessage> _storage;
		private readonly DateTime _date;
		private readonly CancellationToken _cancellationToken;

		public BasketEnumerable(BasketMarketDataStorage<TMessage> storage, DateTime date, CancellationToken cancellationToken)
		{
			_storage = storage ?? throw new ArgumentNullException(nameof(storage));
			_date = date;
			_cancellationToken = cancellationToken;

			var dataTypes = new List<MessageTypes>();

			foreach (var s in storage._innerStorages.Cache)
			{
				if (s.GetType().GetGenericType(typeof(InMemoryMarketDataStorage<>)) == null && !s.GetDates().Contains(date))
					continue;

				dataTypes.Add(s.DataType.ToMessageType2());
			}

			DataTypes = [.. dataTypes];
		}

		public IEnumerable<MessageTypes> DataTypes { get; }

		IAsyncEnumerator<TMessage> IAsyncEnumerable<TMessage>.GetAsyncEnumerator(CancellationToken cancellationToken)
			=> new BasketMarketDataStorageEnumerator(_storage, _date, _cancellationToken);
	}

	private enum ActionTypes
	{
		Add,
		Remove,
		Clear
	}
	
	private class BasketMarketDataStorageInnerList : CachedSynchronizedList<IMarketDataStorage>, IBasketMarketDataStorageInnerList
	{
		private readonly PairSet<IMarketDataStorage, long> _transactionIds = [];

		public long TryGetTransactionId(IMarketDataStorage storage) => _transactionIds.TryGetValue(storage);

		public void Add(IMarketDataStorage storage, long transactionId)
		{
			if (transactionId > 0)
				_transactionIds[storage] = transactionId;

			base.Add(storage);
		}

		public void Remove(long originalTransactionId)
		{
			if (_transactionIds.TryGetKey(originalTransactionId, out var storage))
				Remove(storage);
		}

		protected override bool OnRemove(IMarketDataStorage item)
		{
			_transactionIds.Remove(item);
			return base.OnRemove(item);
		}

		protected override void OnCleared()
		{
			_transactionIds.Clear();
			base.OnCleared();
		}
	}

	private class BasketMarketDataSerializer(BasketMarketDataStorage<TMessage> parent) : IMarketDataSerializer<TMessage>
	{
		private readonly BasketMarketDataStorage<TMessage> _parent = parent ?? throw new ArgumentNullException(nameof(parent));

		StorageFormats IMarketDataSerializer.Format => _parent.InnerStorages.First().Serializer.Format;

		TimeSpan IMarketDataSerializer.TimePrecision => _parent.InnerStorages.First().Serializer.TimePrecision;

		IMarketDataMetaInfo IMarketDataSerializer.CreateMetaInfo(DateTime date)
			=> throw new NotSupportedException();

		void IMarketDataSerializer.Serialize(Stream stream, IEnumerable data, IMarketDataMetaInfo metaInfo)
			=> throw new NotSupportedException();

		IEnumerable<TMessage> IMarketDataSerializer<TMessage>.Deserialize(Stream stream, IMarketDataMetaInfo metaInfo)
			=> throw new NotSupportedException();

		void IMarketDataSerializer<TMessage>.Serialize(Stream stream, IEnumerable<TMessage> data, IMarketDataMetaInfo metaInfo)
			=> throw new NotSupportedException();

		IEnumerable IMarketDataSerializer.Deserialize(Stream stream, IMarketDataMetaInfo metaInfo)
			=> throw new NotSupportedException();
	}

	private readonly BasketMarketDataStorageInnerList _innerStorages = new();
	private readonly CachedSynchronizedList<BasketMarketDataStorageEnumerator> _enumerators = [];

	/// <summary>
	/// Embedded storages of market data.
	/// </summary>
	public IBasketMarketDataStorageInnerList InnerStorages => _innerStorages;

	/// <summary>
	/// Initializes a new instance of the <see cref="BasketMarketDataStorage{T}"/>.
	/// </summary>
	public BasketMarketDataStorage()
	{
		_innerStorages.Added += InnerStoragesOnAdded;
		_innerStorages.Removed += InnerStoragesOnRemoved;
		_innerStorages.Cleared += InnerStoragesOnCleared;

		_serializer = new BasketMarketDataSerializer(this);
	}

	/// <summary>
	/// Release resources.
	/// </summary>
	protected override void DisposeManaged()
	{
		_innerStorages.Added -= InnerStoragesOnAdded;
		_innerStorages.Removed -= InnerStoragesOnRemoved;
		_innerStorages.Cleared -= InnerStoragesOnCleared;

		_innerStorages.Clear();

		base.DisposeManaged();
	}

	/// <summary>
	/// <see cref="MarketDataStorageCache"/>.
	/// </summary>
	public MarketDataStorageCache Cache { get; set; }

	private void InnerStoragesOnAdded(IMarketDataStorage storage)
		=> AddAction(ActionTypes.Add, storage, _innerStorages.TryGetTransactionId(storage));

	private void InnerStoragesOnRemoved(IMarketDataStorage storage)
		=> AddAction(ActionTypes.Remove, storage, 0);

	private void InnerStoragesOnCleared()
		=> AddAction(ActionTypes.Clear, null, 0);

	private void AddAction(ActionTypes type, IMarketDataStorage storage, long transactionId)
		=> _enumerators.Cache.ForEach(e => e.AddAction(type, storage, transactionId));

	async ValueTask<IEnumerable<DateTime>> IMarketDataStorage.GetDatesAsync(CancellationToken cancellationToken)
	{
		var dates = new HashSet<DateTime>();

		foreach (var storage in _innerStorages.Cache)
		{
			var storageDates = await storage.GetDatesAsync(cancellationToken);

			foreach (var date in storageDates)
				dates.Add(date);
		}

		return dates.OrderBy();
	}

	/// <inheritdoc />
	public virtual DataType DataType => throw new NotSupportedException();

	/// <inheritdoc />
	public virtual SecurityId SecurityId => throw new NotSupportedException();

	IMarketDataStorageDrive IMarketDataStorage.Drive => throw new NotSupportedException();

	bool IMarketDataStorage.AppendOnlyNew
	{
		get => throw new NotSupportedException();
		set => throw new NotSupportedException();
	}

	ValueTask<int> IMarketDataStorage.SaveAsync(IEnumerable<Message> data, CancellationToken cancellationToken) => throw new NotSupportedException();
	ValueTask<int> IMarketDataStorage<TMessage>.SaveAsync(IEnumerable<TMessage> data, CancellationToken cancellationToken) => throw new NotSupportedException();
	
	ValueTask IMarketDataStorage.DeleteAsync(IEnumerable<Message> data, CancellationToken cancellationToken) => throw new NotSupportedException();
	ValueTask IMarketDataStorage<TMessage>.DeleteAsync(IEnumerable<TMessage> data, CancellationToken cancellationToken) => throw new NotSupportedException();
	
	ValueTask IMarketDataStorage.DeleteAsync(DateTime date, CancellationToken cancellationToken) => throw new NotSupportedException();

	IAsyncEnumerable<Message> IMarketDataStorage.LoadAsync(DateTime date, CancellationToken cancellationToken) => LoadAsync(date, cancellationToken);
	IAsyncEnumerable<TMessage> IMarketDataStorage<TMessage>.LoadAsync(DateTime date, CancellationToken cancellationToken) => LoadAsync(date, cancellationToken);

	async ValueTask<IMarketDataMetaInfo> IMarketDataStorage.GetMetaInfoAsync(DateTime date, CancellationToken cancellationToken)
	{
		date = date.Date.UtcKind();

		foreach (var inner in _innerStorages.Cache)
		{
			var dates = await inner.GetDatesAsync(cancellationToken);

			if (dates.Contains(date))
				return await inner.GetMetaInfoAsync(date, cancellationToken);
		}

		return null;
	}
	
	private readonly IMarketDataSerializer<TMessage> _serializer;
	IMarketDataSerializer<TMessage> IMarketDataStorage<TMessage>.Serializer => _serializer;
	IMarketDataSerializer IMarketDataStorage.Serializer => ((IMarketDataStorage<TMessage>)this).Serializer;

	/// <summary>
	/// To load messages from embedded storages for specified date.
	/// </summary>
	/// <param name="date">Date.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>The messages loader.</returns>
	public IBasketMarketDataStorageEnumerable<TMessage> LoadAsync(DateTime date, CancellationToken cancellationToken)
		=> new BasketEnumerable(this, date, cancellationToken);
}
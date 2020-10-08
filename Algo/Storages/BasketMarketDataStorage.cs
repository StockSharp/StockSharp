#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: BasketMarketDataStorage.cs
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

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection;

	using MoreLinq;

	using StockSharp.Messages;

	/// <summary>
	/// The aggregator-storage enumerator.
	/// </summary>
	/// <typeparam name="TMessage">Message type.</typeparam>
	public interface IBasketMarketDataStorageEnumerable<TMessage> : IEnumerable<TMessage>
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
	public class BasketMarketDataStorage<TMessage> : Disposable, IMarketDataStorage<TMessage>, IMarketDataStorageInfo<TMessage>
		where TMessage : Message
	{
		private class BasketMarketDataStorageEnumerator : IEnumerator<TMessage>
		{
			private readonly BasketMarketDataStorage<TMessage> _storage;
			private readonly DateTime _date;
			private readonly SynchronizedQueue<Tuple<ActionTypes, IMarketDataStorage, long>> _actions = new SynchronizedQueue<Tuple<ActionTypes, IMarketDataStorage, long>>();
			private readonly OrderedPriorityQueue<DateTimeOffset, Tuple<IEnumerator<Message>, IMarketDataStorage, long>> _enumerators = new OrderedPriorityQueue<DateTimeOffset, Tuple<IEnumerator<Message>, IMarketDataStorage, long>>();

			public BasketMarketDataStorageEnumerator(BasketMarketDataStorage<TMessage> storage, DateTime date)
			{
				_storage = storage ?? throw new ArgumentNullException(nameof(storage));
				_date = date;

				foreach (var s in storage._innerStorages.Cache)
				{
					if (s.GetType().GetGenericType(typeof(InMemoryMarketDataStorage<>)) == null && !s.Dates.Contains(date))
						continue;

					_actions.Add(Tuple.Create(ActionTypes.Add, s, storage._innerStorages.TryGetTransactionId(s)));
				}

				_storage._enumerators.Add(this);
			}

			public TMessage Current { get; private set; }

			bool IEnumerator.MoveNext()
			{
				while (true)
				{
					var action = _actions.TryDequeue();

					if (action == null)
						break;

					var type = action.Item1;
					var storage = action.Item2;

					switch (type)
					{
						case ActionTypes.Add:
						{
							var loaded = storage.Load(_date);

							if (!_storage.PassThroughOrderBookInrement && loaded is IEnumerable<QuoteChangeMessage> quotes)
							{
								loaded = quotes.BuildIfNeed();
							}

							var enu = loaded.GetEnumerator();
							var lastTime = Current?.GetServerTime() ?? DateTimeOffset.MinValue;

							var hasValues = true;

							// пропускаем данные, что меньше времени последнего сообщения (lastTime)
							while (true)
							{
								if (!enu.MoveNext())
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
								_enumerators.Enqueue(GetServerTime(enu), Tuple.Create(enu, storage, action.Item3));
							else
								enu.DoDispose();

							break;
						}
						case ActionTypes.Remove:
						{
							_enumerators.RemoveWhere(p => p.Value.Item2 == storage);
							break;
						}
						case ActionTypes.Clear:
						{
							_enumerators.Clear();
							break;
						}
						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				if (_enumerators.Count == 0)
					return false;

				var pair = _enumerators.Dequeue();

				var enumerator = pair.Value.Item1;

				Current = TrySetTransactionId(enumerator.Current, pair.Value.Item3);

				if (enumerator.MoveNext())
					_enumerators.Enqueue(GetServerTime(enumerator), pair.Value);
				else
					enumerator.DoDispose();

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

			private static DateTimeOffset GetServerTime(IEnumerator<Message> enumerator)
			{
				return enumerator.Current.GetServerTime();
			}

			object IEnumerator.Current => Current;

			void IEnumerator.Reset()
			{
				foreach (var enumerator in _enumerators)
					enumerator.Value.Item1.Reset();
			}

			void IDisposable.Dispose()
			{
				foreach (var enumerator in _enumerators)
					enumerator.Value.Item1.DoDispose();

				_enumerators.Clear();

				_actions.Clear();

				_storage._enumerators.Remove(this);
			}

			public void AddAction(ActionTypes type, IMarketDataStorage storage, long transactionId)
			{
				_actions.Add(Tuple.Create(type, storage, transactionId));
			}
		}

		private sealed class BasketEnumerable : SimpleEnumerable<TMessage>, IBasketMarketDataStorageEnumerable<TMessage>
		{
			public BasketEnumerable(BasketMarketDataStorage<TMessage> storage, DateTime date)
				: base(() => new BasketMarketDataStorageEnumerator(storage, date))
			{
				if (storage == null)
					throw new ArgumentNullException(nameof(storage));

				var dataTypes = new List<MessageTypes>();

				foreach (var s in storage._innerStorages.Cache)
				{
					if (s.GetType().GetGenericType(typeof(InMemoryMarketDataStorage<>)) == null && !s.Dates.Contains(date))
						continue;

					dataTypes.Add(s.DataType.ToMessageType2());
				}

				DataTypes = dataTypes.ToArray();
			}

			public IEnumerable<MessageTypes> DataTypes { get; }
		}

		private enum ActionTypes
		{
			Add,
			Remove,
			Clear
		}
		
		private class BasketMarketDataStorageInnerList : CachedSynchronizedList<IMarketDataStorage>, IBasketMarketDataStorageInnerList
		{
			private readonly PairSet<IMarketDataStorage, long> _transactionIds = new PairSet<IMarketDataStorage, long>();

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

		private class BasketMarketDataSerializer : IMarketDataSerializer<TMessage>
		{
			private readonly BasketMarketDataStorage<TMessage> _parent;

			public BasketMarketDataSerializer(BasketMarketDataStorage<TMessage> parent)
			{
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
			}

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

		private readonly BasketMarketDataStorageInnerList _innerStorages = new BasketMarketDataStorageInnerList();
		private readonly CachedSynchronizedList<BasketMarketDataStorageEnumerator> _enumerators = new CachedSynchronizedList<BasketMarketDataStorageEnumerator>();

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
		/// Pass through incremental <see cref="QuoteChangeMessage"/>.
		/// </summary>
		public bool PassThroughOrderBookInrement { get; set; }

		private void InnerStoragesOnAdded(IMarketDataStorage storage)
			=> AddAction(ActionTypes.Add, storage, _innerStorages.TryGetTransactionId(storage));

		private void InnerStoragesOnRemoved(IMarketDataStorage storage)
			=> AddAction(ActionTypes.Remove, storage, 0);

		private void InnerStoragesOnCleared()
			=> AddAction(ActionTypes.Clear, null, 0);

		private void AddAction(ActionTypes type, IMarketDataStorage storage, long transactionId)
			=> _enumerators.Cache.ForEach(e => e.AddAction(type, storage, transactionId));

		IEnumerable<DateTime> IMarketDataStorage.Dates
			=> _innerStorages.Cache.SelectMany(s => s.Dates).OrderBy().Distinct();

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

		int IMarketDataStorage.Save(IEnumerable<Message> data) => throw new NotSupportedException();
		int IMarketDataStorage<TMessage>.Save(IEnumerable<TMessage> data) => throw new NotSupportedException();
		
		void IMarketDataStorage.Delete(IEnumerable<Message> data) => throw new NotSupportedException();
		void IMarketDataStorage<TMessage>.Delete(IEnumerable<TMessage> data) => throw new NotSupportedException();
		
		void IMarketDataStorage.Delete(DateTime date) => throw new NotSupportedException();
		
		IEnumerable<Message> IMarketDataStorage.Load(DateTime date) => Load(date);
		IEnumerable<TMessage> IMarketDataStorage<TMessage>.Load(DateTime date) => Load(date);

		IMarketDataMetaInfo IMarketDataStorage.GetMetaInfo(DateTime date)
		{
			date = date.Date.UtcKind();

			foreach (var inner in _innerStorages.Cache)
			{
				if (inner.Dates.Contains(date))
					return inner.GetMetaInfo(date);
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
		/// <returns>The messages loader.</returns>
		public IBasketMarketDataStorageEnumerable<TMessage> Load(DateTime date) => new BasketEnumerable(this, date);

		DateTimeOffset IMarketDataStorageInfo<TMessage>.GetTime(TMessage data) => data.GetServerTime();
		DateTimeOffset IMarketDataStorageInfo.GetTime(object data) => ((Message)data).GetServerTime();
	}
}
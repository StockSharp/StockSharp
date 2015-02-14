namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Хранилище-агрегатор, позволяющее загружать данные одновременно из нескольких хранилищ маркет-данных.
	/// </summary>
	/// <typeparam name="T">Тип сообщения.</typeparam>
	public class BasketMarketDataStorage<T> : Disposable
		where T : Message
	{
		/// <summary>
		/// Загрузчик сообщений хранилища-агрегатора.
		/// </summary>
		public class BasketMarketDataStorageEnumerator : IEnumerator<T>
		{
			private readonly BasketMarketDataStorage<T> _storage;
			private readonly DateTime _date;
			private readonly SynchronizedQueue<Tuple<ActionType, IMarketDataStorage>> _actions = new SynchronizedQueue<Tuple<ActionType, IMarketDataStorage>>();
			private readonly OrderedPriorityQueue<DateTimeOffset, Tuple<IEnumerator, IMarketDataStorage>> _enumerators = new OrderedPriorityQueue<DateTimeOffset, Tuple<IEnumerator, IMarketDataStorage>>();

			internal BasketMarketDataStorageEnumerator(BasketMarketDataStorage<T> storage, DateTime date)
			{
				if (storage == null)
					throw new ArgumentNullException("storage");

				_storage = storage;
				_date = date;

				var dataTypes = new List<MessageTypes>();

				foreach (var s in storage._innerStorages.Cache)
				{
					if (!s.Dates.Contains(date))
						continue;

					_actions.Add(Tuple.Create(ActionType.Add, s));

					if (s.DataType == typeof(ExecutionMessage))
						dataTypes.Add(MessageTypes.Execution);

					if (s.DataType == typeof(QuoteChangeMessage))
						dataTypes.Add(MessageTypes.QuoteChange);

					if (s.DataType == typeof(Level1ChangeMessage))
						dataTypes.Add(MessageTypes.Level1Change);

					if (s.DataType == typeof(TimeMessage))
						dataTypes.Add(MessageTypes.Time);

					if (s.DataType == typeof(TimeFrameCandleMessage))
						dataTypes.Add(MessageTypes.CandleTimeFrame);

					if (s.DataType == typeof(PnFCandleMessage))
						dataTypes.Add(MessageTypes.CandlePnF);

					if (s.DataType == typeof(RangeCandleMessage))
						dataTypes.Add(MessageTypes.CandleRange);

					if (s.DataType == typeof(RenkoCandleMessage))
						dataTypes.Add(MessageTypes.CandleRenko);

					if (s.DataType == typeof(TickCandleMessage))
						dataTypes.Add(MessageTypes.CandleTick);

					if (s.DataType == typeof(VolumeCandleMessage))
						dataTypes.Add(MessageTypes.CandleVolume);
				}

				DataTypes = dataTypes.ToArray();

				_storage._enumerators.Add(this);
			}

			/// <summary>
			/// Текущее сообщение.
			/// </summary>
			public T Current { get; private set; }

			/// <summary>
			/// Доступные типы данных.
			/// </summary>
			public IEnumerable<MessageTypes> DataTypes { get; private set; }

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
						case ActionType.Add:
						{
							var enu = storage.Load(_date).GetEnumerator();
							var lastTime = Current == null ? DateTimeOffset.MinValue : Current.GetServerTime();

							var hasValues = true;

							// пропускаем данные, что меньше времени последнего сообщения (lastTime)
							while (true)
							{
								if (!enu.MoveNext())
								{
									hasValues = false;
									break;
								}

								var msg = (Message)enu.Current;

								if (msg.GetServerTime() >= lastTime)
									break;
							}

							// данных в хранилище нет больше последней даты
							if (hasValues)
								_enumerators.Enqueue(((Message)enu.Current).GetServerTime(), Tuple.Create(enu, storage));
							else
								enu.DoDispose();

							break;
						}
						case ActionType.Remove:
						{
							_enumerators.RemoveWhere(p => p.Value.Item2 == storage);
							break;
						}
						case ActionType.Clear:
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

				Current = (T)enumerator.Current;

				if (enumerator.MoveNext())
					_enumerators.Enqueue(((Message)enumerator.Current).GetServerTime(), pair.Value);
				else
					enumerator.DoDispose();

				return true;
			}

			object IEnumerator.Current
			{
				get { return Current; }
			}

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

			internal void AddAction(ActionType type, IMarketDataStorage storage)
			{
				_actions.Add(Tuple.Create(type, storage));
			}
		}

		internal enum ActionType
		{
			Add,
			Remove,
			Clear
		}

		private readonly CachedSynchronizedList<IMarketDataStorage> _innerStorages = new CachedSynchronizedList<IMarketDataStorage>();
		private readonly CachedSynchronizedList<BasketMarketDataStorageEnumerator> _enumerators = new CachedSynchronizedList<BasketMarketDataStorageEnumerator>();

		/// <summary>
		/// Вложенные хранилища маркет-данных.
		/// </summary>
		public ISynchronizedCollection<IMarketDataStorage> InnerStorages
		{
			get { return _innerStorages; }
		}

		/// <summary>
		/// Создать <see cref="BasketMarketDataStorage{T}"/>.
		/// </summary>
		public BasketMarketDataStorage()
		{
			_innerStorages.Added += InnerStoragesOnAdded;
			_innerStorages.Removed += InnerStoragesOnRemoved;
			_innerStorages.Cleared += InnerStoragesOnCleared;
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			_innerStorages.Added -= InnerStoragesOnAdded;
			_innerStorages.Removed -= InnerStoragesOnRemoved;
			_innerStorages.Cleared -= InnerStoragesOnCleared;

			_innerStorages.Clear();

			base.DisposeManaged();
		}

		private void InnerStoragesOnAdded(IMarketDataStorage storage)
		{
			AddAction(ActionType.Add, storage);
		}

		private void InnerStoragesOnRemoved(IMarketDataStorage storage)
		{
			AddAction(ActionType.Remove, storage);
		}

		private void InnerStoragesOnCleared()
		{
			AddAction(ActionType.Clear, null);
		}

		private void AddAction(ActionType type, IMarketDataStorage storage)
		{
			_enumerators.Cache.ForEach(e => e.AddAction(type, storage));
		}

		/// <summary>
		/// Загрузить сообщения из вложенных хранилищ за указанную дату.
		/// </summary>
		/// <param name="date">Дата.</param>
		/// <returns>Загрузчик сообщений.</returns>
		public BasketMarketDataStorageEnumerator Load(DateTime date)
		{
			return new BasketMarketDataStorageEnumerator(this, date);
		}
	}

	/// <summary>
	/// Хранилище, генерирующее данные в процессе работы.
	/// </summary>
	/// <typeparam name="T">Тип данных.</typeparam>
	public sealed class InMemoryMarketDataStorage<T> : IMarketDataStorage<T>
	{
		private readonly Func<DateTime, IEnumerable<T>> _getData;

		IEnumerable<DateTime> IMarketDataStorage.Dates { get { return Enumerable.Empty<DateTime>(); } }

		Security IMarketDataStorage.Security { get { return null; } }

		object IMarketDataStorage.Arg { get { return null; } }

		IMarketDataStorageDrive IMarketDataStorage.Drive { get { return null; } }

		bool IMarketDataStorage.AppendOnlyNew { get; set; }

		Type IMarketDataStorage.DataType { get { return typeof(T); } }

		IMarketDataSerializer IMarketDataStorage.Serializer
		{
			get { return ((IMarketDataStorage<T>)this).Serializer; }
		}

		IMarketDataSerializer<T> IMarketDataStorage<T>.Serializer
		{
			get { throw new NotSupportedException(); }
		}

		IDataStorageReader<T> IMarketDataStorage<T>.GetReader(DateTime date)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Создать <see cref="InMemoryMarketDataStorage{T}"/>.
		/// </summary>
		/// <param name="getData">Метод генерации данных для указанной даты.</param>
		public InMemoryMarketDataStorage(Func<DateTime, IEnumerable<T>> getData)
		{
			if (getData == null)
				throw new ArgumentNullException("getData");

			_getData = getData;
		}

		/// <summary>
		/// Загрузить данные.
		/// </summary>
		/// <param name="date">Дата, для которой необходимо загрузить данные.</param>
		/// <returns>Данные. Если данных не существует, то будет возвращено пустое множество.</returns>
		public IEnumerableEx<T> Load(DateTime date)
		{
			return _getData(date).ToEx();
		}

		IEnumerable IMarketDataStorage.Load(DateTime date)
		{
			return Load(date);
		}

		IMarketDataMetaInfo IMarketDataStorage.GetMetaInfo(DateTime date)
		{
			return null;
		}

		void IMarketDataStorage.Save(IEnumerable data)
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

		void IMarketDataStorage<T>.Save(IEnumerable<T> data)
		{
			throw new NotSupportedException();
		}

		void IMarketDataStorage<T>.Delete(IEnumerable<T> data)
		{
			throw new NotSupportedException();
		}
	}
}
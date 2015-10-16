namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection;

	using MoreLinq;

	using StockSharp.Messages;

	/// <summary>
	/// The aggregator-storage enumerator.
	/// </summary>
	/// <typeparam name="T">Message type.</typeparam>
	public interface IBasketMarketDataStorageEnumerator<T> : IEnumerator<T>
	{
		/// <summary>
		/// Available message types.
		/// </summary>
		IEnumerable<MessageTypes> DataTypes { get; }
	}

	/// <summary>
	/// The aggregator-storage, allowing to load data simultaneously from several market data storages.
	/// </summary>
	/// <typeparam name="T">Message type.</typeparam>
	public class BasketMarketDataStorage<T> : Disposable
		where T : Message
	{
		private class BasketMarketDataStorageEnumerator : IBasketMarketDataStorageEnumerator<T>
		{
			private readonly BasketMarketDataStorage<T> _storage;
			private readonly DateTime _date;
			private readonly SynchronizedQueue<Tuple<ActionType, IMarketDataStorage>> _actions = new SynchronizedQueue<Tuple<ActionType, IMarketDataStorage>>();
			private readonly OrderedPriorityQueue<DateTimeOffset, Tuple<IEnumerator, IMarketDataStorage>> _enumerators = new OrderedPriorityQueue<DateTimeOffset, Tuple<IEnumerator, IMarketDataStorage>>();

			public BasketMarketDataStorageEnumerator(BasketMarketDataStorage<T> storage, DateTime date)
			{
				if (storage == null)
					throw new ArgumentNullException("storage");

				_storage = storage;
				_date = date;

				var dataTypes = new List<MessageTypes>();

				foreach (var s in storage._innerStorages.Cache)
				{
					if (s.GetType().GetGenericType(typeof(InMemoryMarketDataStorage<>)) == null && !s.Dates.Contains(date))
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
			/// The current message.
			/// </summary>
			public T Current { get; private set; }

			/// <summary>
			/// Available data types.
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
								_enumerators.Enqueue(GetServerTime(enu), Tuple.Create(enu, storage));
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
					_enumerators.Enqueue(GetServerTime(enumerator), pair.Value);
				else
					enumerator.DoDispose();

				return true;
			}

			private static DateTimeOffset GetServerTime(IEnumerator enumerator)
			{
				var serverTime = ((Message)enumerator.Current).GetServerTime();

				if (serverTime == null)
					throw new InvalidOperationException();

				return serverTime.Value;
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

			public void AddAction(ActionType type, IMarketDataStorage storage)
			{
				_actions.Add(Tuple.Create(type, storage));
			}
		}

		private enum ActionType
		{
			Add,
			Remove,
			Clear
		}

		private readonly CachedSynchronizedList<IMarketDataStorage> _innerStorages = new CachedSynchronizedList<IMarketDataStorage>();
		private readonly CachedSynchronizedList<BasketMarketDataStorageEnumerator> _enumerators = new CachedSynchronizedList<BasketMarketDataStorageEnumerator>();

		/// <summary>
		/// Embedded storages of market data.
		/// </summary>
		public ISynchronizedCollection<IMarketDataStorage> InnerStorages
		{
			get { return _innerStorages; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BasketMarketDataStorage{T}"/>.
		/// </summary>
		public BasketMarketDataStorage()
		{
			_innerStorages.Added += InnerStoragesOnAdded;
			_innerStorages.Removed += InnerStoragesOnRemoved;
			_innerStorages.Cleared += InnerStoragesOnCleared;
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
		/// To load messages from embedded storages for specified date.
		/// </summary>
		/// <param name="date">Date.</param>
		/// <returns>The messages loader.</returns>
		public IBasketMarketDataStorageEnumerator<T> Load(DateTime date)
		{
			return new BasketMarketDataStorageEnumerator(this, date);
		}
	}
}
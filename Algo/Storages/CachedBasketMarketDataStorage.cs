namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo.Testing;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The cached aggregator-storage, allowing to load data simultaneously from several market data storages.
	/// </summary>
	/// <typeparam name="T">Message type.</typeparam>
	public class CachedBasketMarketDataStorage<T> : BaseLogReceiver, IEnumerator<T>
		where T : Message
	{
		private sealed class BlockingPriorityQueue : BaseBlockingQueue<KeyValuePair<DateTimeOffset, Message>, OrderedPriorityQueue<DateTimeOffset, Message>>
		{
			public BlockingPriorityQueue()
				: base(new OrderedPriorityQueue<DateTimeOffset, Message>())
			{
			}

			protected override void OnEnqueue(KeyValuePair<DateTimeOffset, Message> item, bool force)
			{
				InnerCollection.Enqueue(item.Key, item.Value);
			}

			protected override KeyValuePair<DateTimeOffset, Message> OnDequeue()
			{
				return InnerCollection.Dequeue();
			}

			protected override KeyValuePair<DateTimeOffset, Message> OnPeek()
			{
				return InnerCollection.Peek();
			}
		}

		private readonly BlockingPriorityQueue _messageQueue = new BlockingPriorityQueue();
		private readonly List<Tuple<IMarketDataStorage, bool>> _actions = new List<Tuple<IMarketDataStorage, bool>>();
		private readonly SyncObject _moveNextSyncRoot  = new SyncObject();
		private readonly SyncObject _syncRoot = new SyncObject();

		private readonly BasketMarketDataStorage<T> _basketStorage;
		private readonly CancellationTokenSource _cancellationToken;

		private bool _isInitialized;
		private bool _isChanged;

		private DateTimeOffset _currentTime;

		private T _currentMessage;

		/// <summary>
		/// Embedded storages of market data.
		/// </summary>
		public IEnumerable<IMarketDataStorage> InnerStorages => _basketStorage.InnerStorages;

		/// <summary>
		/// List of all exchange boards, for which instruments are loaded.
		/// </summary>
		public IEnumerable<ExchangeBoard> Boards { get; set; }

		/// <summary>
		/// Message queue count.
		/// </summary>
		public int MessageCount => _messageQueue.Count;

		/// <summary>
		/// Max message queue count.
		/// </summary>
		/// <remarks>
		/// The default value is -1, which corresponds to the size without limitations.
		/// </remarks>
		public int MaxMessageCount
		{
			get { return _messageQueue.MaxSize; }
			set { _messageQueue.MaxSize = value; }
		}

		/// <summary>
		/// Date in history for starting the paper trading.
		/// </summary>
		public DateTimeOffset StartDate { get; set; }

		/// <summary>
		/// Date in history to stop the paper trading (date is included).
		/// </summary>
		public DateTimeOffset StopDate { get; set; }

		private int _postTradeMarketTimeChangedCount = 2;

		/// <summary>
		/// The number of the event <see cref="IConnector.MarketTimeChanged"/> calls after end of trading. By default it is equal to 2.
		/// </summary>
		/// <remarks>
		/// It is required for activation of post-trade rules (rules, basing on events, occurring after end of trading).
		/// </remarks>
		public int PostTradeMarketTimeChangedCount
		{
			get { return _postTradeMarketTimeChangedCount; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException();

				_postTradeMarketTimeChangedCount = value;
			}
		}

		private TimeSpan _marketTimeChangedInterval = TimeSpan.FromSeconds(1);

		/// <summary>
		/// The interval of message <see cref="TimeMessage"/> generation. By default, it is equal to 1 sec.
		/// </summary>
		public virtual TimeSpan MarketTimeChangedInterval
		{
			get { return _marketTimeChangedInterval; }
			set
			{
				if (value <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str196);

				_marketTimeChangedInterval = value;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CachedBasketMarketDataStorage{T}"/>.
		/// </summary>
		public CachedBasketMarketDataStorage()
			: this(new BasketMarketDataStorage<T>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CachedBasketMarketDataStorage{T}"/>.
		/// </summary>
		public CachedBasketMarketDataStorage(BasketMarketDataStorage<T> basketStorage)
		{
			if (basketStorage == null)
				throw new ArgumentNullException(nameof(basketStorage));

			_basketStorage = basketStorage;
			_basketStorage.InnerStorages.Add(new InMemoryMarketDataStorage<TimeMessage>(null, null, GetTimeLine));

			_cancellationToken = new CancellationTokenSource();

			MaxMessageCount = 1000000;

			ThreadingHelper
				.Thread(() => CultureInfo.InvariantCulture.DoInCulture(OnLoad))
				.Name("Cached marketdata storage thread.")
				.Launch();
		}

		/// <summary>
		/// Add inner market data storage.
		/// </summary>
		/// <param name="storage"></param>
		public void AddStorage(IMarketDataStorage storage)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			_isChanged = true;
			_actions.Add(Tuple.Create(storage, true));
			_syncRoot.PulseSignal();
		}

		/// <summary>
		/// Remove inner market data storage.
		/// </summary>
		/// <typeparam name="TStorage"></typeparam>
		/// <param name="security"></param>
		/// <param name="messageType"></param>
		/// <param name="arg"></param>
		public void RemoveStorage<TStorage>(Security security, MessageTypes messageType, object arg)
			where TStorage : class, IMarketDataStorage
		{
			var storage = _basketStorage
				.InnerStorages
				.OfType<TStorage>()
				.FirstOrDefault(s => s.Security == security && s.Arg.Compare(arg) == 0);

			if (storage == null)
				return;

			_isChanged = true;
			_actions.Add(Tuple.Create((IMarketDataStorage)storage, false));
			_syncRoot.PulseSignal();
		}

		#region IEnumerator<T>

		/// <summary>
		/// Advances the enumerator to the next element of the collection.
		/// </summary>
		/// <returns><see langword="true" /> if the enumerator was successfully advanced to the next element; <see langword="false" /> if the enumerator has passed the end of the collection.</returns>
		public bool MoveNext()
		{
			if (_messageQueue.Count == 0 && !_isInitialized)
				return false;

			if (_isChanged)
				_moveNextSyncRoot.WaitSignal();

			var pair = _messageQueue.Dequeue();
			var message = pair.Value;

			var serverTime = message.GetServerTime();

			if (serverTime != null)
				_currentTime = serverTime.Value;

			_currentMessage = (T)message;

			return true;
		}

		/// <summary>
		/// Sets the enumerator to its initial position, which is before the first element in the collection.
		/// </summary>
		public void Reset()
		{
			_currentMessage = null;
			_basketStorage.InnerStorages.Clear();
		}

		/// <summary>
		/// Gets the current element in the collection.
		/// </summary>
		public T Current => _currentMessage;

		object IEnumerator.Current => _currentMessage;

		#endregion

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			_cancellationToken.Cancel();
			_syncRoot.PulseSignal();

			base.DisposeManaged();
		}

		private void OnLoad()
		{
			try
			{
				var messageTypes = new[] { MessageTypes.Time, ExtendedMessageTypes.Clearing };
				var token = _cancellationToken.Token;

				while (!token.IsCancellationRequested)
				{
					_syncRoot.WaitSignal();
					_messageQueue.Clear();

					_isInitialized = true;
					_isChanged = false;

					_moveNextSyncRoot.PulseSignal();

					foreach (var action in _actions.CopyAndClear())
					{
						if (action.Item2)
							_basketStorage.InnerStorages.Add(action.Item1);
						else
							_basketStorage.InnerStorages.Remove(action.Item1);
					}

					var boards = Boards.ToArray();
					var loadDate = _currentTime != DateTimeOffset.MinValue ? _currentTime : StartDate;

					while (loadDate.Date <= StopDate.Date && !_isChanged && !token.IsCancellationRequested)
					{
						if (boards.Length == 0 || boards.Any(b => b.IsTradeDate(loadDate, true)))
						{
							this.AddInfoLog("Loading {0}", loadDate.Date);

							using (var enumerator = _basketStorage.Load(loadDate.UtcDateTime.Date))
							{
								// storage for the specified date contains only time messages and clearing events
								var noData = !enumerator.DataTypes.Except(messageTypes).Any();

								if (noData)
									EnqueueMessages(loadDate, token, GetSimpleTimeLine(loadDate).GetEnumerator());
								else
									EnqueueMessages(loadDate, token, enumerator);
							}
						}

						loadDate = loadDate.Date.AddDays(1).ApplyTimeZone(loadDate.Offset);
					}

					if (!_isChanged)
						EnqueueMessage(new LastMessage { LocalTime = StopDate });

					_isInitialized = false;
				}
			}
			catch (Exception excp)
			{
				EnqueueMessage(new ErrorMessage { Error = excp });
				EnqueueMessage(new LastMessage { IsError = true });
			}
		}

		private void EnqueueMessages(DateTimeOffset loadDate, CancellationToken token, IEnumerator<Message> enumerator)
		{
			var checkFromTime = loadDate.Date == StartDate.Date && loadDate.TimeOfDay != TimeSpan.Zero;
			var checkToTime = loadDate.Date == StopDate.Date;

			while (enumerator.MoveNext() && !_isChanged && !token.IsCancellationRequested)
			{
				var msg = enumerator.Current;

				var serverTime = msg.GetServerTime();

				if (serverTime == null)
					throw new InvalidOperationException();

				msg.LocalTime = serverTime.Value;

				if (checkFromTime)
				{
					// пропускаем только стаканы, тики и ОЛ
					if (msg.Type == MessageTypes.QuoteChange || msg.Type == MessageTypes.Execution)
					{
						if (msg.LocalTime < StartDate)
							continue;

						checkFromTime = false;
					}
				}

				if (checkToTime)
				{
					if (msg.LocalTime > StopDate)
						break;
				}

				EnqueueMessage(msg);
			}
		}

		private void EnqueueMessage(Message message)
		{
			_messageQueue.Enqueue(new KeyValuePair<DateTimeOffset, Message>(message.LocalTime, message));
		}

		private IEnumerable<Tuple<ExchangeBoard, Range<TimeSpan>>> GetOrderedRanges(DateTimeOffset date)
		{
			var orderedRanges = Boards
				.Where(b => b.IsTradeDate(date, true))
				.SelectMany(board =>
				{
					var period = board.WorkingTime.GetPeriod(date.ToLocalTime(board.TimeZone));

					return period == null || period.Times.Length == 0
						       ? new[] { Tuple.Create(board, new Range<TimeSpan>(TimeSpan.Zero, TimeHelper.LessOneDay)) }
						       : period.Times.Select(t => Tuple.Create(board, ToUtc(board, t)));
				})
				.OrderBy(i => i.Item2.Min)
				.ToList();

			for (var i = 0; i < orderedRanges.Count - 1;)
			{
				if (orderedRanges[i].Item2.Contains(orderedRanges[i + 1].Item2))
				{
					orderedRanges.RemoveAt(i + 1);
				}
				else if (orderedRanges[i + 1].Item2.Contains(orderedRanges[i].Item2))
				{
					orderedRanges.RemoveAt(i);
				}
				else if (orderedRanges[i].Item2.Intersect(orderedRanges[i + 1].Item2) != null)
				{
					orderedRanges[i] = Tuple.Create(orderedRanges[i].Item1, new Range<TimeSpan>(orderedRanges[i].Item2.Min, orderedRanges[i + 1].Item2.Max));
					orderedRanges.RemoveAt(i + 1);
				}
				else
					i++;
			}

			return orderedRanges;
		}

		private static Range<TimeSpan> ToUtc(ExchangeBoard board, Range<TimeSpan> range)
		{
			var min = DateTime.MinValue + range.Min;
			var max = DateTime.MinValue + range.Max;

			var utcMin = min.To(board.TimeZone);
			var utcMax = max.To(board.TimeZone);

			return new Range<TimeSpan>(utcMin.TimeOfDay, utcMax.TimeOfDay);
		}

		private IEnumerable<TimeMessage> GetTimeLine(DateTimeOffset date)
		{
			var ranges = GetOrderedRanges(date);
			var lastTime = TimeSpan.Zero;

			foreach (var range in ranges)
			{
				for (var time = range.Item2.Min; time <= range.Item2.Max; time += MarketTimeChangedInterval)
				{
					var serverTime = GetTime(date, time);

					if (serverTime.Date < date.Date)
						continue;

					lastTime = serverTime.TimeOfDay;
					yield return new TimeMessage { ServerTime = serverTime };
				}
			}

			foreach (var m in GetPostTradeTimeMessages(date, lastTime))
			{
				yield return m;
			}
		}

		private IEnumerable<TimeMessage> GetSimpleTimeLine(DateTimeOffset date)
		{
			var ranges = GetOrderedRanges(date);
			var lastTime = TimeSpan.Zero;

			foreach (var range in ranges)
			{
				var time = GetTime(date, range.Item2.Min);
				if (time.Date >= date.Date)
					yield return new TimeMessage { ServerTime = time };

				time = GetTime(date, range.Item2.Max);
				if (time.Date >= date.Date)
					yield return new TimeMessage { ServerTime = time };

				lastTime = range.Item2.Max;
			}

			foreach (var m in GetPostTradeTimeMessages(date, lastTime))
			{
				yield return m;
			}
		}

		private static DateTimeOffset GetTime(DateTimeOffset date, TimeSpan timeOfDay)
		{
			return (date.Date + timeOfDay).ApplyTimeZone(date.Offset);
		}

		private IEnumerable<TimeMessage> GetPostTradeTimeMessages(DateTimeOffset date, TimeSpan lastTime)
		{
			for (var i = 0; i < PostTradeMarketTimeChangedCount; i++)
			{
				lastTime += MarketTimeChangedInterval;

				if (lastTime > TimeHelper.LessOneDay)
					break;

				yield return new TimeMessage
				{
					ServerTime = GetTime(date, lastTime)
				};
			}
		}
	}
}
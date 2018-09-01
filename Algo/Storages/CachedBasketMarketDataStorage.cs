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
		private readonly MessagePriorityQueue _messageQueue = new MessagePriorityQueue();
		private readonly List<Tuple<IMarketDataStorage, long>> _actions = new List<Tuple<IMarketDataStorage, long>>();
		private readonly SyncObject _moveNextSyncRoot = new SyncObject();
		private readonly SyncObject _syncRoot = new SyncObject();

		private readonly IdGenerator _transactionIdGenerator;
		private readonly BasketMarketDataStorage<T> _basketStorage;

		private CancellationTokenSource _cancellationToken;

		private bool _isInitialized;
		private bool _isChanged;
		private bool _isTimeLineAdded;

		private DateTimeOffset _currentTime;

		///// <summary>
		///// Embedded storages of market data.
		///// </summary>
		//public IEnumerable<IMarketDataStorage> InnerStorages => _basketStorage.InnerStorages;

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
			get => _messageQueue.MaxSize;
			set => _messageQueue.MaxSize = value;
		}

		private int _postTradeMarketTimeChangedCount = 2;

		/// <summary>
		/// The number of the event <see cref="IConnector.MarketTimeChanged"/> calls after end of trading. By default it is equal to 2.
		/// </summary>
		/// <remarks>
		/// It is required for activation of post-trade rules (rules, basing on events, occurring after end of trading).
		/// </remarks>
		public int PostTradeMarketTimeChangedCount
		{
			get => _postTradeMarketTimeChangedCount;
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
		public TimeSpan MarketTimeChangedInterval
		{
			get => _marketTimeChangedInterval;
			set
			{
				if (value <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str196);

				_marketTimeChangedInterval = value;
			}
		}

		/// <summary>
		/// Check loading dates are they tradable.
		/// </summary>
		public bool CheckTradableDates { get; set; } = true;

		/// <summary>
		/// Initializes a new instance of the <see cref="CachedBasketMarketDataStorage{T}"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		public CachedBasketMarketDataStorage(IdGenerator transactionIdGenerator)
			: this(transactionIdGenerator, new BasketMarketDataStorage<T>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CachedBasketMarketDataStorage{T}"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		/// <param name="basketStorage">The aggregator-storage, allowing to load data simultaneously from several market data storages.</param>
		public CachedBasketMarketDataStorage(IdGenerator transactionIdGenerator, BasketMarketDataStorage<T> basketStorage)
		{
			_transactionIdGenerator = transactionIdGenerator ?? throw new ArgumentNullException(nameof(transactionIdGenerator));
			_basketStorage = basketStorage ?? throw new ArgumentNullException(nameof(basketStorage));
		}

		/// <summary>
		/// Add inner market data storage.
		/// </summary>
		/// <param name="storage">Market data storage.</param>
		/// <param name="transactionId">The subscription identifier.</param>
		public void AddStorage(IMarketDataStorage storage, long transactionId)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			if (transactionId == 0)
				throw new ArgumentNullException(nameof(transactionId));

			_isChanged = true;
			_actions.Add(Tuple.Create(storage, transactionId));

			_messageQueue.Close();
			_syncRoot.PulseSignal();
		}

		/// <summary>
		/// Remove inner market data storage.
		/// </summary>
		/// <param name="originalTransactionId">The subscription identifier.</param>
		public void RemoveStorage(long originalTransactionId)
		{
			if (originalTransactionId == 0)
				throw new ArgumentNullException(nameof(originalTransactionId));

			_isChanged = true;
			_actions.Add(Tuple.Create((IMarketDataStorage)null, originalTransactionId));

			_messageQueue.Close();
			_syncRoot.PulseSignal();
		}

		#region IEnumerator<T>

		/// <summary>
		/// Advances the enumerator to the next element of the collection.
		/// </summary>
		/// <returns><see langword="true" /> if the enumerator was successfully advanced to the next element; <see langword="false" /> if the enumerator has passed the end of the collection.</returns>
		public bool MoveNext()
		{
			if (MarketTimeChangedInterval != TimeSpan.Zero && !_isTimeLineAdded)
			{
				AddStorage(new InMemoryMarketDataStorage<TimeMessage>(null, null, d => GetTimeLine(d, MarketTimeChangedInterval)), _transactionIdGenerator.GetNextId());

				_isTimeLineAdded = true;
				_moveNextSyncRoot.WaitSignal();
			}

			if (_messageQueue.Count == 0 && !_isInitialized)
				return false;

			var isChanged = _isChanged;

			if (isChanged)
				_moveNextSyncRoot.WaitSignal();

			Message message;

			if (!isChanged)
			{
				if (!_messageQueue.TryDequeue(out message))
					return false;
			}
			else
				message = _messageQueue.Dequeue().Value;

			var serverTime = message.TryGetServerTime();

			if (serverTime != null)
				_currentTime = serverTime.Value;

			Current = (T)message;

			return true;
		}

		/// <summary>
		/// Sets the enumerator to its initial position, which is before the first element in the collection.
		/// </summary>
		public void Reset()
		{
			Current = null;
			_currentTime = DateTimeOffset.MinValue;
			_isTimeLineAdded = false;
            _basketStorage.InnerStorages.Clear();
		}

		/// <summary>
		/// Gets the current element in the collection.
		/// </summary>
		public T Current { get; private set; }

		object IEnumerator.Current => Current;

		#endregion

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			Stop();

			base.DisposeManaged();
		}

		/// <summary>
		/// Start data loading.
		/// </summary>
		/// <param name="startDate">Date in history for starting the paper trading.</param>
		/// <param name="stopDate">Date in history to stop the paper trading (date is included).</param>
		public void Start(DateTimeOffset startDate, DateTimeOffset stopDate)
		{
			_cancellationToken = new CancellationTokenSource();

			ThreadingHelper
				.Thread(() => CultureInfo.InvariantCulture.DoInCulture(() =>
				{
					try
					{
						var messageTypes = new[] { MessageTypes.Time, ExtendedMessageTypes.Clearing };
						var token = _cancellationToken.Token;

						while (!IsDisposed && !token.IsCancellationRequested)
						{
							_syncRoot.WaitSignal();
							_messageQueue.Clear();
							_messageQueue.Open();

							_isInitialized = true;
							_isChanged = false;

							_moveNextSyncRoot.PulseSignal();

							foreach (var action in _actions.CopyAndClear())
							{
								var storage = action.Item1;
								var subscriptionId = action.Item2;

								if (storage != null)
									_basketStorage.InnerStorages.Add(storage, subscriptionId);
								else
									_basketStorage.InnerStorages.Remove(subscriptionId);
							}

							var boards = Boards.ToArray();
							var loadDate = _currentTime != DateTimeOffset.MinValue ? _currentTime.Date : startDate;
							var startTime = _currentTime;
							var checkDates = CheckTradableDates && boards.Length > 0;

							while (loadDate.Date <= stopDate.Date && !_isChanged && !token.IsCancellationRequested)
							{
								if (!checkDates || boards.Any(b => b.IsTradeDate(loadDate, true)))
								{
									this.AddInfoLog("Loading {0}", loadDate.Date);

									var messages = _basketStorage.Load(loadDate.UtcDateTime.Date);

									// storage for the specified date contains only time messages and clearing events
									var noData = !messages.DataTypes.Except(messageTypes).Any();

									if (noData)
										EnqueueMessages(startDate, stopDate, loadDate, startTime, token, GetSimpleTimeLine(loadDate, MarketTimeChangedInterval));
									else
										EnqueueMessages(startDate, stopDate, loadDate, startTime, token, messages);
								}

								loadDate = loadDate.Date.AddDays(1).ApplyTimeZone(loadDate.Offset);
							}

							if (!_isChanged)
								EnqueueMessage(new LastMessage { LocalTime = stopDate });

							_isInitialized = false;
						}
					}
					catch (Exception ex)
					{
						EnqueueMessage(ex.ToErrorMessage());
						EnqueueMessage(new LastMessage { IsError = true });
					}
				}))
				.Name("Cached marketdata storage thread.")
				.Launch();
		}

		/// <summary>
		/// Stop data loading.
		/// </summary>
		public void Stop()
		{
			_cancellationToken?.Cancel();
			_syncRoot.PulseSignal();
		}

		private void EnqueueMessages(DateTimeOffset startDate, DateTimeOffset stopDate, DateTimeOffset loadDate, DateTimeOffset startTime, CancellationToken token, IEnumerable<Message> messages)
		{
			var checkFromTime = loadDate.Date == startDate.Date && loadDate.TimeOfDay != TimeSpan.Zero;
			var checkToTime = loadDate.Date == stopDate.Date;

			foreach (var msg in messages)
			{
				if (_isChanged || token.IsCancellationRequested)
					break;

				var serverTime = msg.GetServerTime();

				//if (serverTime == null)
				//	throw new InvalidOperationException();

				if (serverTime < startTime)
					continue;

				msg.LocalTime = serverTime;

				if (checkFromTime)
				{
					// пропускаем только стаканы, тики и ОЛ
					if (msg.Type == MessageTypes.QuoteChange || msg.Type == MessageTypes.Execution)
					{
						if (msg.LocalTime < startDate)
							continue;

						checkFromTime = false;
					}
				}

				if (checkToTime)
				{
					if (msg.LocalTime > stopDate)
						break;
				}

				EnqueueMessage(msg);
			}
		}

		private void EnqueueMessage(Message message)
		{
			_messageQueue.Enqueue(message);
		}

		private IEnumerable<Tuple<ExchangeBoard, Range<TimeSpan>>> GetOrderedRanges(DateTimeOffset date)
		{
			var orderedRanges = Boards
				.Where(b => b.IsTradeDate(date, true))
				.SelectMany(board =>
				{
					var period = board.WorkingTime.GetPeriod(date.ToLocalTime(board.TimeZone));

					return period == null || period.Times.Count == 0
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

		private IEnumerable<TimeMessage> GetTimeLine(DateTimeOffset date, TimeSpan interval)
		{
			var ranges = GetOrderedRanges(date);
			var lastTime = TimeSpan.Zero;

			foreach (var range in ranges)
			{
				for (var time = range.Item2.Min; time <= range.Item2.Max; time += interval)
				{
					var serverTime = GetTime(date, time);

					if (serverTime.Date < date.Date)
						continue;

					lastTime = serverTime.TimeOfDay;
					yield return new TimeMessage { ServerTime = serverTime };
				}
			}

			foreach (var m in GetPostTradeTimeMessages(date, lastTime, interval))
			{
				yield return m;
			}
		}

		private IEnumerable<TimeMessage> GetSimpleTimeLine(DateTimeOffset date, TimeSpan interval)
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

			foreach (var m in GetPostTradeTimeMessages(date, lastTime, interval))
			{
				yield return m;
			}
		}

		private static DateTimeOffset GetTime(DateTimeOffset date, TimeSpan timeOfDay)
		{
			return (date.Date + timeOfDay).ApplyTimeZone(date.Offset);
		}

		private IEnumerable<TimeMessage> GetPostTradeTimeMessages(DateTimeOffset date, TimeSpan lastTime, TimeSpan interval)
		{
			for (var i = 0; i < PostTradeMarketTimeChangedCount; i++)
			{
				lastTime += interval;

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
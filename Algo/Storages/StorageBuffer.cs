namespace StockSharp.Algo.Storages;

/// <summary>
/// Storage buffer.
/// </summary>
public class StorageBuffer : IStorageBuffer
{
	private class DataBuffer<TKey, TMarketData>
		where TMarketData : Message
	{
		private readonly SynchronizedDictionary<TKey, List<TMarketData>> _data = [];

		public void Add(TKey key, TMarketData data)
			=> _data.SyncDo(d => d.SafeAdd(key).Add(data));

		// Says how much was handed over as well as what: the room it frees is room for that much
		// more to be taken in.
		public IDictionary<TKey, IEnumerable<TMarketData>> Get(out int count)
		{
			var taken = 0;

			var retVal = _data.SyncGet(d =>
			{
				var v = d.ToDictionary(p => p.Key, p => (IEnumerable<TMarketData>)p.Value);
				taken = d.Sum(p => p.Value.Count);
				d.Clear();
				return v;
			});

			count = taken;
			return retVal;
		}

		public int Clear()
		{
			return _data.SyncGet(d =>
			{
				var count = d.Sum(p => p.Value.Count);
				d.Clear();
				return count;
			});
		}
	}

	private readonly DataBuffer<SecurityId, ExecutionMessage> _ticksBuffer = new();
	private readonly DataBuffer<SecurityId, QuoteChangeMessage> _orderBooksBuffer = new();
	private readonly DataBuffer<SecurityId, ExecutionMessage> _orderLogBuffer = new();
	private readonly DataBuffer<SecurityId, Level1ChangeMessage> _level1Buffer = new();
	private readonly DataBuffer<SecurityId, PositionChangeMessage> _positionChangesBuffer = new();
	private readonly DataBuffer<SecurityId, ExecutionMessage> _transactionsBuffer = new();
	private readonly SynchronizedSet<BoardStateMessage> _boardStatesBuffer = [];
	private readonly DataBuffer<(SecurityId, DataType), CandleMessage> _candleBuffer = new();
	private readonly SynchronizedSet<NewsMessage> _newsBuffer = [];
	private readonly SynchronizedPairSet<long, (DataType dt, SecurityId secId)> _subscriptionsById = [];

	// How much is waiting to be written, and what it has already cost to keep that bounded.
	private long _buffered;
	private long _dropped;

	/// <summary>
	/// How many messages may wait to be written before new ones are thrown away. Zero, the default,
	/// is no limit at all.
	/// </summary>
	/// <remarks>
	/// A storage that has stalled - a disk that is full, a server that is down - is not a reason to
	/// take the process down with it. Past this, what arrives is dropped and counted in
	/// <see cref="DroppedMessages"/>, which is data lost on purpose rather than memory lost by
	/// accident.
	/// </remarks>
	public long MaxBufferedMessages { get; set; }

	/// <summary>
	/// How many messages were thrown away because <see cref="MaxBufferedMessages"/> was reached.
	/// What is counted here is gone.
	/// </summary>
	public long DroppedMessages => Interlocked.Read(ref _dropped);

	// Room for one more, taken before it is written down so two threads cannot both take the last.
	private bool TryReserve()
	{
		var buffered = Interlocked.Increment(ref _buffered);
		var max = MaxBufferedMessages;

		if (max <= 0 || buffered <= max)
			return true;

		Interlocked.Decrement(ref _buffered);
		Interlocked.Increment(ref _dropped);

		return false;
	}

	private void Release(int count)
	{
		if (count > 0)
			Interlocked.Add(ref _buffered, -count);
	}

	/// <summary>
	/// Save data only for subscriptions.
	/// </summary>
	public bool FilterSubscription { get; set; }

	/// <summary>
	/// Enable storage.
	/// </summary>
	public bool Enabled { get; set; } = true;

	/// <summary>
	/// Enable level1 storage.
	/// </summary>
	public bool EnabledLevel1 { get; set; } = true;

	/// <summary>
	/// Enable order book storage.
	/// </summary>
	public bool EnabledOrderBook { get; set; } = true;

	/// <summary>
	/// Enable positions storage.
	/// </summary>
	public bool EnabledPositions { get; set; }

	/// <summary>
	/// Enable transactions storage.
	/// </summary>
	public bool EnabledTransactions { get; set; } = true;

	/// <summary>
	/// <see cref="BufferMessageAdapter.StartStorageTimer"/>.
	/// </summary>
	public bool DisableStorageTimer { get; set; }

	/// <summary>
	/// Ignore messages with <see cref="IGeneratedMessage.BuildFrom"/> is not <see langword="null"/>.
	/// </summary>
	public ISet<DataType> IgnoreGenerated { get; } = new HashSet<DataType>
	{
		DataType.PositionChanges,
		DataType.Transactions,
		DataType.Ticks,
		DataType.Level1,
		DataType.MarketDepth,
		DataType.FilteredMarketDepth,
		DataType.OrderLog,
	};

	/// <summary>
	/// Get accumulated <see cref="DataType.Ticks"/>.
	/// </summary>
	/// <returns>Ticks.</returns>
	public IDictionary<SecurityId, IEnumerable<ExecutionMessage>> GetTicks()
	{
		var retVal = _ticksBuffer.Get(out var count);
		Release(count);
		return retVal;
	}

	/// <summary>
	/// Get accumulated <see cref="DataType.OrderLog"/>.
	/// </summary>
	/// <returns>Order log.</returns>
	public IDictionary<SecurityId, IEnumerable<ExecutionMessage>> GetOrderLog()
	{
		var retVal = _orderLogBuffer.Get(out var count);
		Release(count);
		return retVal;
	}

	/// <summary>
	/// Get accumulated <see cref="DataType.Transactions"/>.
	/// </summary>
	/// <returns>Transactions.</returns>
	public IDictionary<SecurityId, IEnumerable<ExecutionMessage>> GetTransactions()
	{
		var retVal = _transactionsBuffer.Get(out var count);
		Release(count);
		return retVal;
	}

	/// <summary>
	/// Get accumulated <see cref="CandleMessage"/>.
	/// </summary>
	/// <returns>Candles.</returns>
	public IDictionary<(SecurityId secId, DataType dataType), IEnumerable<CandleMessage>> GetCandles()
	{
		var retVal = _candleBuffer.Get(out var count);
		Release(count);
		return retVal;
	}

	/// <summary>
	/// Get accumulated <see cref="Level1ChangeMessage"/>.
	/// </summary>
	/// <returns>Level1.</returns>
	public IDictionary<SecurityId, IEnumerable<Level1ChangeMessage>> GetLevel1()
	{
		var retVal = _level1Buffer.Get(out var count);
		Release(count);
		return retVal;
	}

	/// <summary>
	/// Get accumulated <see cref="PositionChangeMessage"/>.
	/// </summary>
	/// <returns>Position changes.</returns>
	public IDictionary<SecurityId, IEnumerable<PositionChangeMessage>> GetPositionChanges()
	{
		var retVal = _positionChangesBuffer.Get(out var count);
		Release(count);
		return retVal;
	}

	/// <summary>
	/// Get accumulated <see cref="QuoteChangeMessage"/>.
	/// </summary>
	/// <returns>Order books.</returns>
	public IDictionary<SecurityId, IEnumerable<QuoteChangeMessage>> GetOrderBooks()
	{
		var retVal = _orderBooksBuffer.Get(out var count);
		Release(count);
		return retVal;
	}

	/// <summary>
	/// Get accumulated <see cref="NewsMessage"/>.
	/// </summary>
	/// <returns>News.</returns>
	public IEnumerable<NewsMessage> GetNews()
	{
		var retVal = _newsBuffer.SyncGet(c => c.CopyAndClear());
		Release(retVal.Length);
		return retVal;
	}

	/// <summary>
	/// Get accumulated <see cref="BoardStateMessage"/>.
	/// </summary>
	/// <returns>States.</returns>
	public IEnumerable<BoardStateMessage> GetBoardStates()
	{
		var retVal = _boardStatesBuffer.SyncGet(c => c.CopyAndClear());
		Release(retVal.Length);
		return retVal;
	}

	private static bool CanStore(Message message, bool canStore, bool ignoreGenerated)
	{
		if (!canStore)
			return false;

		if (ignoreGenerated && message is IGeneratedMessage genMsg)
			return genMsg.BuildFrom == null;

		return true;
	}

	private bool CanStore(Message message)
	{
		if (!Enabled)
			return false;

		if (message is IGeneratedMessage genMsg && genMsg.BuildFrom != null)
		{
			if (message is ISubscriptionIdMessage subscrIdMsg)
			{
				if (IgnoreGenerated.Contains(subscrIdMsg.DataType))
					return false;

				if (message is CandleMessage candleMsg && IgnoreGenerated.Contains(DataType.Create(candleMsg.GetType(), default)))
					return false;
			}
		}

		static bool IsFailed(ExecutionMessage execMsg)
			=> execMsg.OrderState == OrderStates.Failed && execMsg.TransactionId != default;

		if (!FilterSubscription)
		{
			if (message is ExecutionMessage execMsg && IsFailed(execMsg))
				return false;

			return true;
		}

		switch (message.Type)
		{
			case MessageTypes.Portfolio:
			case MessageTypes.PositionChange:
				return CanStore(message, EnabledPositions, IgnoreGenerated.Contains(DataType.PositionChanges));

			case MessageTypes.OrderRegister:
			case MessageTypes.OrderReplace:
			case MessageTypes.OrderCancel:
			case MessageTypes.OrderGroupCancel:
				return CanStore(message, EnabledTransactions, IgnoreGenerated.Contains(DataType.Transactions));

			case MessageTypes.Execution:
			{
				var execMsg = (ExecutionMessage)message;

				if (execMsg.IsMarketData())
					break;

				// do not store cancellation commands into snapshot
				if (execMsg.IsCancellation)
					return false;

				if (IsFailed(execMsg))
					return false;

				return CanStore(message, EnabledTransactions, IgnoreGenerated.Contains(DataType.Transactions));
			}
		}

		if (message is ISubscriptionIdMessage subscrMsg)
			return CanStore(message, subscrMsg.GetSubscriptionIds().Any(_subscriptionsById.ContainsKey), IgnoreGenerated.Contains(subscrMsg.DataType) || (message is CandleMessage candleMsg && IgnoreGenerated.Contains(DataType.Create(candleMsg.GetType(), default))));

		return false;
	}

	/// <summary>
	/// Process message.
	/// </summary>
	/// <param name="message">Message.</param>
	public void ProcessInMessage(Message message)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		if (message.OfflineMode != MessageOfflineModes.None)
			return;

		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				Release(_ticksBuffer.Clear());
				Release(_orderBooksBuffer.Clear());
				Release(_orderLogBuffer.Clear());
				Release(_level1Buffer.Clear());
				Release(_positionChangesBuffer.Clear());
				Release(_transactionsBuffer.Clear());
				Release(_candleBuffer.Clear());
				Release(_newsBuffer.Count);
				_newsBuffer.Clear();
				_subscriptionsById.Clear();

				//SendOutMessage(new ResetMessage());
				break;
			}
			case MessageTypes.OrderRegister:
			{
				var regMsg = (OrderRegisterMessage)message;

				if (!CanStore(regMsg))
					break;

				if (TryReserve())
					_transactionsBuffer.Add(regMsg.SecurityId, regMsg.ToExec());
				break;
			}
			case MessageTypes.OrderReplace:
			{
				var replaceMsg = (OrderReplaceMessage)message;

				if (!CanStore(replaceMsg))
					break;

				if (TryReserve())
					_transactionsBuffer.Add(replaceMsg.SecurityId, replaceMsg.ToExec());
				break;
			}
			//case MessageTypes.OrderCancel:
			//{
			//	var cancelMsg = (OrderCancelMessage)message;

			//	//if (!CanStore(cancelMsg))
			//	//	break;

			//	_transactionsBuffer.Add(cancelMsg.SecurityId, new ExecutionMessage
			//	{
			//		ServerTime = DateTime.UtcNow,
			//		DataTypeEx = DataType.Transactions,
			//		SecurityId = cancelMsg.SecurityId,
			//		HasOrderInfo = true,
			//		TransactionId = cancelMsg.TransactionId,
			//		IsCancellation = true,
			//		OrderId = cancelMsg.OrderId,
			//		OrderStringId = cancelMsg.OrderStringId,
			//		OriginalTransactionId = cancelMsg.OriginalTransactionId,
			//		OrderVolume = cancelMsg.Volume,
			//		//Side = cancelMsg.Side,
			//	});

			//	break;
			//}
			case MessageTypes.MarketData:
			{
				var mdMsg = (MarketDataMessage)message;

				if (Enabled)
				{
					if (mdMsg.IsSubscribe)
						_subscriptionsById.TryAdd(mdMsg.TransactionId, (mdMsg.DataType2, mdMsg.SecurityId));
					else
						_subscriptionsById.Remove(mdMsg.OriginalTransactionId);
				}

				break;
			}
		}
	}

	private void TryStore<TMessage>(DataBuffer<SecurityId, TMessage> buffer, TMessage message)
		where TMessage : Message, ISecurityIdMessage
	{
		if (CanStore(message) && TryReserve())
			buffer.Add(message.SecurityId, message.TypedClone());
	}

	/// <summary>
	/// Puts back what could not be written, so the next round writes it rather than losing it.
	/// </summary>
	/// <remarks>
	/// Draining hands the data over and forgets it, which is what makes a failed write a loss. A
	/// caller that could not write what it took gives it back here; what no longer fits is dropped
	/// and counted, the same as anything else that arrives full.
	/// </remarks>
	/// <param name="messages">What was taken and not written.</param>
	public void PutBack(IEnumerable<Message> messages)
	{
		if (messages is null)
			throw new ArgumentNullException(nameof(messages));

		foreach (var message in messages)
		{
			if (!TryReserve())
				continue;

			switch (message)
			{
				case ExecutionMessage execMsg:
				{
					var buffer = execMsg.DataType == DataType.Ticks
						? _ticksBuffer
						: execMsg.DataType == DataType.OrderLog
							? _orderLogBuffer
							: _transactionsBuffer;

					buffer.Add(execMsg.SecurityId, execMsg);
					break;
				}
				case Level1ChangeMessage l1Msg:
					_level1Buffer.Add(l1Msg.SecurityId, l1Msg);
					break;
				case QuoteChangeMessage quoteMsg:
					_orderBooksBuffer.Add(quoteMsg.SecurityId, quoteMsg);
					break;
				case PositionChangeMessage posMsg:
					_positionChangesBuffer.Add(posMsg.SecurityId, posMsg);
					break;
				case CandleMessage candleMsg:
					_candleBuffer.Add((candleMsg.SecurityId, candleMsg.DataType), candleMsg);
					break;
				case NewsMessage newsMsg:
					_newsBuffer.Add(newsMsg);
					break;
				case BoardStateMessage stateMsg:
					_boardStatesBuffer.Add(stateMsg);
					break;
				default:
					// Nothing here ever held it, so nothing can take it back.
					Release(1);
					break;
			}
		}
	}

	/// <summary>
	/// Process message.
	/// </summary>
	/// <param name="message">Message.</param>
	public void ProcessOutMessage(Message message)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		if (message.OfflineMode != MessageOfflineModes.None)
			return;

		switch (message.Type)
		{
			case MessageTypes.Level1Change:
			{
				if (EnabledLevel1)
					TryStore(_level1Buffer, (Level1ChangeMessage)message);

				break;
			}
			case MessageTypes.QuoteChange:
			{
				if (EnabledOrderBook)
					TryStore(_orderBooksBuffer, (QuoteChangeMessage)message);

				break;
			}
			case MessageTypes.Execution:
			{
				var execMsg = (ExecutionMessage)message;

				DataBuffer<SecurityId, ExecutionMessage> buffer;

				var dataType = execMsg.DataType;

				if (dataType == DataType.Ticks)
					buffer = _ticksBuffer;
				else if (dataType == DataType.Transactions)
					buffer = _transactionsBuffer;
				else if (dataType == DataType.OrderLog)
					buffer = _orderLogBuffer;
				else
					throw new ArgumentOutOfRangeException(nameof(message), dataType, LocalizedStrings.UnknownType.Put(message));

				TryStore(buffer, execMsg);
				break;
			}
			case MessageTypes.News:
			{
				var newsMsg = (NewsMessage)message;

				if (CanStore(newsMsg))
					if (TryReserve())
						_newsBuffer.Add(newsMsg.TypedClone());

				break;
			}
			case MessageTypes.BoardState:
			{
				var stateMsg = (BoardStateMessage)message;

				if (CanStore(stateMsg))
					if (TryReserve())
						_boardStatesBuffer.Add(stateMsg.TypedClone());

				break;
			}
			case MessageTypes.PositionChange:
			{
				if (EnabledPositions)
					TryStore(_positionChangesBuffer, (PositionChangeMessage)message);

				break;
			}
			case MessageTypes.SubscriptionResponse:
			{
				var responseMsg = (SubscriptionResponseMessage)message;
				
				if (!responseMsg.IsOk())
					_subscriptionsById.Remove(responseMsg.OriginalTransactionId);

				break;
			}
			case MessageTypes.SubscriptionFinished:
			{
				var responseMsg = (SubscriptionFinishedMessage)message;
				_subscriptionsById.Remove(responseMsg.OriginalTransactionId);
				break;
			}
			default:
			{
				if (message is CandleMessage candleMsg && candleMsg.State == CandleStates.Finished)
				{
					if (CanStore(candleMsg) && TryReserve())
						_candleBuffer.Add((candleMsg.SecurityId, candleMsg.DataType), candleMsg.TypedClone());
				}

				break;
			}
		}
	}

	void IPersistable.Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(Enabled), Enabled);
		storage.SetValue(nameof(EnabledLevel1), EnabledLevel1);
		storage.SetValue(nameof(EnabledOrderBook), EnabledOrderBook);
		storage.SetValue(nameof(EnabledPositions), EnabledPositions);
		storage.SetValue(nameof(EnabledTransactions), EnabledTransactions);
		storage.SetValue(nameof(FilterSubscription), FilterSubscription);
		storage.SetValue(nameof(DisableStorageTimer), DisableStorageTimer);
		storage.SetValue(nameof(IgnoreGenerated), IgnoreGenerated.Select(dt => dt.Save()).ToArray());
	}

	void IPersistable.Load(SettingsStorage storage)
	{
		Enabled = storage.GetValue(nameof(Enabled), Enabled);
		EnabledLevel1 = storage.GetValue(nameof(EnabledLevel1), EnabledLevel1);
		EnabledOrderBook = storage.GetValue(nameof(EnabledOrderBook), EnabledOrderBook);
		EnabledPositions = storage.GetValue(nameof(EnabledPositions), EnabledPositions);
		EnabledTransactions = storage.GetValue(nameof(EnabledTransactions), EnabledTransactions);
		FilterSubscription = storage.GetValue(nameof(FilterSubscription), FilterSubscription);
		DisableStorageTimer = storage.GetValue(nameof(DisableStorageTimer), DisableStorageTimer);

		IgnoreGenerated.Clear();
		IgnoreGenerated.AddRange((storage.GetValue<IEnumerable<SettingsStorage>>(nameof(IgnoreGenerated)) ?? []).Select(s => s.Load<DataType>()));
	}

	/// <inheritdoc />
	public IStorageBuffer Clone()
	{
		var clone = new StorageBuffer();
		((IPersistable)clone).Load(((IPersistable)this).Save());
		return clone;
	}

	object ICloneable.Clone() => Clone();
}

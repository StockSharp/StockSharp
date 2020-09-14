namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Storage buffer.
	/// </summary>
	public class StorageBuffer : IPersistable
	{
		private class DataBuffer<TKey, TMarketData>
			where TMarketData : Message
		{
			private readonly SynchronizedDictionary<TKey, List<TMarketData>> _data = new SynchronizedDictionary<TKey, List<TMarketData>>();

			public void Add(TKey key, TMarketData data)
				=> _data.SyncDo(d => d.SafeAdd(key).Add(data));

			public IDictionary<TKey, IEnumerable<TMarketData>> Get()
				=> _data.SyncGet(d =>
				{
					var retVal = d.ToDictionary(p => p.Key, p => (IEnumerable<TMarketData>)p.Value);
					d.Clear();
					return retVal;
				});

			public void Clear()
				=> _data.Clear();
		}

		private readonly DataBuffer<SecurityId, ExecutionMessage> _ticksBuffer = new DataBuffer<SecurityId, ExecutionMessage>();
		private readonly DataBuffer<SecurityId, QuoteChangeMessage> _orderBooksBuffer = new DataBuffer<SecurityId, QuoteChangeMessage>();
		private readonly DataBuffer<SecurityId, ExecutionMessage> _orderLogBuffer = new DataBuffer<SecurityId, ExecutionMessage>();
		private readonly DataBuffer<SecurityId, Level1ChangeMessage> _level1Buffer = new DataBuffer<SecurityId, Level1ChangeMessage>();
		private readonly DataBuffer<SecurityId, PositionChangeMessage> _positionChangesBuffer = new DataBuffer<SecurityId, PositionChangeMessage>();
		private readonly DataBuffer<SecurityId, ExecutionMessage> _transactionsBuffer = new DataBuffer<SecurityId, ExecutionMessage>();
		private readonly SynchronizedSet<BoardStateMessage> _boardStatesBuffer = new SynchronizedSet<BoardStateMessage>();
		private readonly DataBuffer<Tuple<SecurityId, Type, object>, CandleMessage> _candleBuffer = new DataBuffer<Tuple<SecurityId, Type, object>, CandleMessage>();
		private readonly SynchronizedSet<NewsMessage> _newsBuffer = new SynchronizedSet<NewsMessage>();
		private readonly SynchronizedSet<long> _subscriptionsById = new SynchronizedSet<long>();

		/// <summary>
		/// Save data only for subscriptions.
		/// </summary>
		public bool FilterSubscription { get; set; }

		/// <summary>
		/// Enable storage.
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <summary>
		/// Enable positions storage.
		/// </summary>
		public bool EnabledPositions { get; set; }

		/// <summary>
		/// Enable transactions storage.
		/// </summary>
		public bool EnabledTransactions { get; set; } = true;

		/// <summary>
		/// Interpret tick messages as level1.
		/// </summary>
		public bool TicksAsLevel1 { get; set; } = true;

		/// <summary>
		/// <see cref="BufferMessageAdapter.StartStorageTimer"/>.
		/// </summary>
		public bool DisableStorageTimer { get; set; }

		/// <summary>
		/// Ignore market-data messages with <see cref="IGeneratedMessage.BuildFrom"/> is not <see langword="null"/>.
		/// </summary>
		public bool IgnoreGeneratedMarketData { get; set; } = true;

		/// <summary>
		/// Ignore transactional messages with <see cref="IGeneratedMessage.BuildFrom"/> is not <see langword="null"/>.
		/// </summary>
		public bool IgnoreGeneratedTransactional { get; set; } = true;

		/// <summary>
		/// Get accumulated <see cref="ExecutionTypes.Tick"/>.
		/// </summary>
		/// <returns>Ticks.</returns>
		public IDictionary<SecurityId, IEnumerable<ExecutionMessage>> GetTicks()
			=> _ticksBuffer.Get();

		/// <summary>
		/// Get accumulated <see cref="ExecutionTypes.OrderLog"/>.
		/// </summary>
		/// <returns>Order log.</returns>
		public IDictionary<SecurityId, IEnumerable<ExecutionMessage>> GetOrderLog()
			=> _orderLogBuffer.Get();

		/// <summary>
		/// Get accumulated <see cref="ExecutionTypes.Transaction"/>.
		/// </summary>
		/// <returns>Transactions.</returns>
		public IDictionary<SecurityId, IEnumerable<ExecutionMessage>> GetTransactions()
			=> _transactionsBuffer.Get();

		/// <summary>
		/// Get accumulated <see cref="CandleMessage"/>.
		/// </summary>
		/// <returns>Candles.</returns>
		public IDictionary<Tuple<SecurityId, Type, object>, IEnumerable<CandleMessage>> GetCandles()
			=> _candleBuffer.Get();

		/// <summary>
		/// Get accumulated <see cref="Level1ChangeMessage"/>.
		/// </summary>
		/// <returns>Level1.</returns>
		public IDictionary<SecurityId, IEnumerable<Level1ChangeMessage>> GetLevel1()
			=> _level1Buffer.Get();

		/// <summary>
		/// Get accumulated <see cref="PositionChangeMessage"/>.
		/// </summary>
		/// <returns>Position changes.</returns>
		public IDictionary<SecurityId, IEnumerable<PositionChangeMessage>> GetPositionChanges()
			=> _positionChangesBuffer.Get();

		/// <summary>
		/// Get accumulated <see cref="QuoteChangeMessage"/>.
		/// </summary>
		/// <returns>Order books.</returns>
		public IDictionary<SecurityId, IEnumerable<QuoteChangeMessage>> GetOrderBooks()
			=> _orderBooksBuffer.Get();

		/// <summary>
		/// Get accumulated <see cref="NewsMessage"/>.
		/// </summary>
		/// <returns>News.</returns>
		public IEnumerable<NewsMessage> GetNews()
			=> _newsBuffer.SyncGet(c => c.CopyAndClear());

		/// <summary>
		/// Get accumulated <see cref="BoardStateMessage"/>.
		/// </summary>
		/// <returns>States.</returns>
		public IEnumerable<BoardStateMessage> GetBoardStates()
			=> _boardStatesBuffer.SyncGet(c => c.CopyAndClear());

		private bool CanStore(Message message, bool canStore, bool ignoreGenerated)
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
					return CanStore(message, EnabledPositions, IgnoreGeneratedTransactional);

				case MessageTypes.OrderRegister:
				case MessageTypes.OrderReplace:
				case MessageTypes.OrderCancel:
				case MessageTypes.OrderPairReplace:
				case MessageTypes.OrderGroupCancel:
					return CanStore(message, EnabledTransactions, IgnoreGeneratedTransactional);

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

					return CanStore(message, EnabledTransactions, IgnoreGeneratedTransactional);
				}
			}

			if (message is ISubscriptionIdMessage subscrMsg)
				return CanStore(message, subscrMsg.GetSubscriptionIds().Any(_subscriptionsById.Contains), IgnoreGeneratedMarketData);

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
					_ticksBuffer.Clear();
					_orderBooksBuffer.Clear();
					_orderLogBuffer.Clear();
					_level1Buffer.Clear();
					_positionChangesBuffer.Clear();
					_transactionsBuffer.Clear();
					_candleBuffer.Clear();
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

					_transactionsBuffer.Add(regMsg.SecurityId, regMsg.ToExec());
					break;
				}
				case MessageTypes.OrderReplace:
				{
					var replaceMsg = (OrderReplaceMessage)message;

					if (!CanStore(replaceMsg))
						break;

					_transactionsBuffer.Add(replaceMsg.SecurityId, replaceMsg.ToExec());
					break;
				}
				case MessageTypes.OrderPairReplace:
				{
					var pairMsg = (OrderPairReplaceMessage)message;

					if (!CanStore(pairMsg))
						break;

					_transactionsBuffer.Add(pairMsg.Message1.SecurityId, pairMsg.Message1.ToExec());
					_transactionsBuffer.Add(pairMsg.Message2.SecurityId, pairMsg.Message2.ToExec());
					break;
				}
				//case MessageTypes.OrderCancel:
				//{
				//	var cancelMsg = (OrderCancelMessage)message;

				//	//if (!CanStore(cancelMsg))
				//	//	break;

				//	_transactionsBuffer.Add(cancelMsg.SecurityId, new ExecutionMessage
				//	{
				//		ServerTime = DateTimeOffset.Now,
				//		ExecutionType = ExecutionTypes.Transaction,
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
							_subscriptionsById.Add(mdMsg.TransactionId);
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
			if (CanStore(message))
				buffer.Add(message.SecurityId, message.TypedClone());
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
					TryStore(_level1Buffer, (Level1ChangeMessage)message);
					break;
				}
				case MessageTypes.QuoteChange:
				{
					TryStore(_orderBooksBuffer, (QuoteChangeMessage)message);
					break;
				}
				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					DataBuffer<SecurityId, ExecutionMessage> buffer;

					var execType = execMsg.ExecutionType;

					switch (execType)
					{
						case ExecutionTypes.Tick:
							buffer = _ticksBuffer;
							break;
						case ExecutionTypes.Transaction:
							buffer = _transactionsBuffer;
							break;
						case ExecutionTypes.OrderLog:
							buffer = _orderLogBuffer;
							break;
						default:
							throw new ArgumentOutOfRangeException(nameof(message), execType, LocalizedStrings.Str1695Params.Put(message));
					}

					TryStore(buffer, execMsg);
					break;
				}
				case MessageTypes.News:
				{
					var newsMsg = (NewsMessage)message;

					if (CanStore(newsMsg))
						_newsBuffer.Add(newsMsg.TypedClone());

					break;
				}
				case MessageTypes.BoardState:
				{
					var stateMsg = (BoardStateMessage)message;

					if (CanStore(stateMsg))
						_boardStatesBuffer.Add(stateMsg.TypedClone());

					break;
				}
				case MessageTypes.PositionChange:
				{
					TryStore(_positionChangesBuffer, (PositionChangeMessage)message);
					break;
				}

				default:
				{
					if (message is CandleMessage candleMsg && candleMsg.State == CandleStates.Finished)
					{
						if (CanStore(candleMsg))
							_candleBuffer.Add(Tuple.Create(candleMsg.SecurityId, candleMsg.GetType(), candleMsg.Arg), candleMsg.TypedClone());
					}

					break;
				}
			}
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Enabled), Enabled);
			storage.SetValue(nameof(EnabledPositions), EnabledPositions);
			storage.SetValue(nameof(EnabledTransactions), EnabledTransactions);
			storage.SetValue(nameof(FilterSubscription), FilterSubscription);
			storage.SetValue(nameof(TicksAsLevel1), TicksAsLevel1);
			storage.SetValue(nameof(DisableStorageTimer), DisableStorageTimer);
			storage.SetValue(nameof(IgnoreGeneratedMarketData), IgnoreGeneratedMarketData);
			storage.SetValue(nameof(IgnoreGeneratedTransactional), IgnoreGeneratedTransactional);
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			Enabled = storage.GetValue(nameof(Enabled), Enabled);
			EnabledPositions = storage.GetValue(nameof(EnabledPositions), EnabledPositions);
			EnabledTransactions = storage.GetValue(nameof(EnabledTransactions), EnabledTransactions);
			FilterSubscription = storage.GetValue(nameof(FilterSubscription), FilterSubscription);
			TicksAsLevel1 = storage.GetValue(nameof(TicksAsLevel1), TicksAsLevel1);
			DisableStorageTimer = storage.GetValue(nameof(DisableStorageTimer), DisableStorageTimer);
			IgnoreGeneratedMarketData = storage.GetValue(nameof(IgnoreGeneratedMarketData), IgnoreGeneratedMarketData);
			IgnoreGeneratedTransactional = storage.GetValue(nameof(IgnoreGeneratedTransactional), IgnoreGeneratedTransactional);
		}
	}
}
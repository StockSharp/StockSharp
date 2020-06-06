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
		private readonly DataBuffer<Tuple<SecurityId, Type, object>, CandleMessage> _candleBuffer = new DataBuffer<Tuple<SecurityId, Type, object>, CandleMessage>();
		private readonly DataBuffer<SecurityId, ExecutionMessage> _transactionsBuffer = new DataBuffer<SecurityId, ExecutionMessage>();
		private readonly SynchronizedSet<NewsMessage> _newsBuffer = new SynchronizedSet<NewsMessage>();
		private readonly SynchronizedSet<long> _subscriptionsById = new SynchronizedSet<long>();
		private readonly SynchronizedDictionary<long, SecurityId> _securityIds = new SynchronizedDictionary<long, SecurityId>();

		/// <summary>
		/// Save data only for subscriptions.
		/// </summary>
		public bool FilterSubscription { get; set; }

		/// <summary>
		/// Enable storage.
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <summary>
		/// Interpret tick messages as level1.
		/// </summary>
		public bool TicksAsLevel1 { get; set; } = true;

		/// <summary>
		/// <see cref="BufferMessageAdapter.StartStorageTimer"/>.
		/// </summary>
		public bool DisableStorageTimer { get; set; }

		/// <summary>
		/// Get accumulated ticks.
		/// </summary>
		/// <returns>Ticks.</returns>
		public IDictionary<SecurityId, IEnumerable<ExecutionMessage>> GetTicks()
			=> _ticksBuffer.Get();

		/// <summary>
		/// Get accumulated order log.
		/// </summary>
		/// <returns>Order log.</returns>
		public IDictionary<SecurityId, IEnumerable<ExecutionMessage>> GetOrderLog()
			=> _orderLogBuffer.Get();

		/// <summary>
		/// Get accumulated transactions.
		/// </summary>
		/// <returns>Transactions.</returns>
		public IDictionary<SecurityId, IEnumerable<ExecutionMessage>> GetTransactions()
			=> _transactionsBuffer.Get();

		/// <summary>
		/// Get accumulated candles.
		/// </summary>
		/// <returns>Candles.</returns>
		public IDictionary<Tuple<SecurityId, Type, object>, IEnumerable<CandleMessage>> GetCandles()
			=> _candleBuffer.Get();

		/// <summary>
		/// Get accumulated level1.
		/// </summary>
		/// <returns>Level1.</returns>
		public IDictionary<SecurityId, IEnumerable<Level1ChangeMessage>> GetLevel1()
			=> _level1Buffer.Get();

		/// <summary>
		/// Get accumulated position changes.
		/// </summary>
		/// <returns>Position changes.</returns>
		public IDictionary<SecurityId, IEnumerable<PositionChangeMessage>> GetPositionChanges()
			=> _positionChangesBuffer.Get();

		/// <summary>
		/// Get accumulated order books.
		/// </summary>
		/// <returns>Order books.</returns>
		public IDictionary<SecurityId, IEnumerable<QuoteChangeMessage>> GetOrderBooks()
			=> _orderBooksBuffer.Get();

		/// <summary>
		/// Get accumulated news.
		/// </summary>
		/// <returns>News.</returns>
		public IEnumerable<NewsMessage> GetNews()
			=> _newsBuffer.SyncGet(c => c.CopyAndClear());

		private bool CanStore(ISubscriptionIdMessage message)
		{
			if (!Enabled)
				return false;

			if (!FilterSubscription)
				return true;

			return message.GetSubscriptionIds().Any(_subscriptionsById.Contains);
		}

		/// <summary>
		/// Process message.
		/// </summary>
		/// <param name="message">Message.</param>
		public void SendInMessage(Message message)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));

			if (message.OfflineMode != MessageOfflineModes.None)
				return;

			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					_subscriptionsById.Clear();

					_ticksBuffer.Clear();
					_level1Buffer.Clear();
					_candleBuffer.Clear();
					_orderLogBuffer.Clear();
					_orderBooksBuffer.Clear();
					_transactionsBuffer.Clear();
					_newsBuffer.Clear();
					_positionChangesBuffer.Clear();

					//SendOutMessage(new ResetMessage());
					break;
				}
				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;

					//if (!CanStore<ExecutionMessage>(regMsg.SecurityId, ExecutionTypes.Transaction))
					//	break;

					// try - cause looped back messages from offline adapter
					_securityIds.TryAdd(regMsg.TransactionId, regMsg.SecurityId);

					_transactionsBuffer.Add(regMsg.SecurityId, new ExecutionMessage
					{
						ServerTime = DateTimeOffset.Now,
						ExecutionType = ExecutionTypes.Transaction,
						SecurityId = regMsg.SecurityId,
						TransactionId = regMsg.TransactionId,
						HasOrderInfo = true,
						OrderPrice = regMsg.Price,
						OrderVolume = regMsg.Volume,
						Currency = regMsg.Currency,
						PortfolioName = regMsg.PortfolioName,
						ClientCode = regMsg.ClientCode,
						BrokerCode = regMsg.BrokerCode,
						Comment = regMsg.Comment,
						Side = regMsg.Side,
						TimeInForce = regMsg.TimeInForce,
						ExpiryDate = regMsg.TillDate,
						Balance = regMsg.Volume,
						VisibleVolume = regMsg.VisibleVolume,
						LocalTime = regMsg.LocalTime,
						IsMarketMaker = regMsg.IsMarketMaker,
						IsMargin = regMsg.IsMargin,
						Slippage = regMsg.Slippage,
						IsManual = regMsg.IsManual,
						OrderType = regMsg.OrderType,
						UserOrderId = regMsg.UserOrderId,
						OrderState = OrderStates.Pending,
						Condition = regMsg.Condition?.Clone(),
						MinVolume = regMsg.MinOrderVolume,
						PositionEffect = regMsg.PositionEffect,
						PostOnly = regMsg.PostOnly,
					});

					break;
				}
				case MessageTypes.OrderCancel:
				{
					var cancelMsg = (OrderCancelMessage)message;

					//if (!CanStore<ExecutionMessage>(cancelMsg.SecurityId, ExecutionTypes.Transaction))
					//	break;

					// try - cause looped back messages from offline adapter
					_securityIds.TryAdd(cancelMsg.TransactionId, cancelMsg.SecurityId);

					_transactionsBuffer.Add(cancelMsg.SecurityId, new ExecutionMessage
					{
						ServerTime = DateTimeOffset.Now,
						ExecutionType = ExecutionTypes.Transaction,
						SecurityId = cancelMsg.SecurityId,
						HasOrderInfo = true,
						TransactionId = cancelMsg.TransactionId,
						IsCancellation = true,
						OrderId = cancelMsg.OrderId,
						OrderStringId = cancelMsg.OrderStringId,
						OriginalTransactionId = cancelMsg.OriginalTransactionId,
						OrderVolume = cancelMsg.Volume,
						//Side = cancelMsg.Side,
					});

					break;
				}
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

		/// <summary>
		/// Process message.
		/// </summary>
		/// <param name="message">Message.</param>
		public void SendOutMessage(Message message)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));

			if (message.OfflineMode != MessageOfflineModes.None)
				return;

			switch (message.Type)
			{
				case MessageTypes.Level1Change:
				{
					var level1Msg = (Level1ChangeMessage)message;

					if (CanStore(level1Msg))
						_level1Buffer.Add(level1Msg.SecurityId, level1Msg.TypedClone());

					break;
				}
				case MessageTypes.QuoteChange:
				{
					var quotesMsg = (QuoteChangeMessage)message;

					if (CanStore(quotesMsg))
						_orderBooksBuffer.Add(quotesMsg.SecurityId, quotesMsg.TypedClone());

					break;
				}
				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					DataBuffer<SecurityId, ExecutionMessage> buffer;

					var secId = execMsg.SecurityId;
					var execType = execMsg.ExecutionType;

					switch (execType)
					{
						case ExecutionTypes.Tick:
							buffer = _ticksBuffer;
							break;
						case ExecutionTypes.Transaction:
						{
							// some responses do not contains sec id
							if (secId.IsDefault() && !_securityIds.TryGetValue(execMsg.OriginalTransactionId, out secId))
								return;

							buffer = _transactionsBuffer;
							break;
						}
						case ExecutionTypes.OrderLog:
							buffer = _orderLogBuffer;
							break;
						default:
							throw new ArgumentOutOfRangeException(nameof(message), execType, LocalizedStrings.Str1695Params.Put(message));
					}

					//if (execType == ExecutionTypes.Transaction && execMsg.TransactionId == 0)
					//	break;

					if (execType == ExecutionTypes.Transaction || CanStore(execMsg))
						buffer.Add(secId, execMsg.TypedClone());

					break;
				}
				case MessageTypes.News:
				{
					var newsMsg = (NewsMessage)message;

					if (CanStore(newsMsg))
						_newsBuffer.Add(newsMsg.TypedClone());

					break;
				}
				case MessageTypes.PositionChange:
				{
					var posMsg = (PositionChangeMessage)message;
					var secId = posMsg.SecurityId;

					//if (CanStore<PositionChangeMessage>(secId))
					_positionChangesBuffer.Add(secId, posMsg.TypedClone());

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
			storage.SetValue(nameof(FilterSubscription), FilterSubscription);
			storage.SetValue(nameof(TicksAsLevel1), TicksAsLevel1);
			storage.SetValue(nameof(DisableStorageTimer), DisableStorageTimer);
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			Enabled = storage.GetValue(nameof(Enabled), Enabled);
			FilterSubscription = storage.GetValue(nameof(FilterSubscription), FilterSubscription);
			TicksAsLevel1 = storage.GetValue(nameof(TicksAsLevel1), TicksAsLevel1);
			DisableStorageTimer = storage.GetValue(nameof(DisableStorageTimer), DisableStorageTimer);
		}
	}
}
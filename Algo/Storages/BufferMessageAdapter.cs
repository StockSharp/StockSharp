#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: BufferMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
	/// Buffered message adapter.
	/// </summary>
	public class BufferMessageAdapter : MessageAdapterWrapper
	{
		/// <summary>
		/// The market data buffer.
		/// </summary>
		/// <typeparam name="TKey">The key type.</typeparam>
		/// <typeparam name="TMarketData">Market data type.</typeparam>
		private class DataBuffer<TKey, TMarketData>
			where TMarketData : Message
		{
			private readonly SynchronizedDictionary<TKey, List<TMarketData>> _data = new SynchronizedDictionary<TKey, List<TMarketData>>();

			///// <summary>
			///// The buffer size.
			///// </summary>
			//public int Size { get; set; }

			/// <summary>
			/// To add new information to the buffer.
			/// </summary>
			/// <param name="key">The key possessing new information.</param>
			/// <param name="data">New information.</param>
			public void Add(TKey key, TMarketData data)
			{
				//Add(key, new[] { data });
				_data.SyncDo(d => d.SafeAdd(key).Add(data));
			}

			///// <summary>
			///// To add new information to the buffer.
			///// </summary>
			///// <param name="key">The key possessing new information.</param>
			///// <param name="data">New information.</param>
			//public void Add(TKey key, IEnumerable<TMarketData> data)
			//{
			//	_data.SyncDo(d => d.SafeAdd(key).AddRange(data));
			//}

			/// <summary>
			/// To get accumulated data from the buffer and delete them.
			/// </summary>
			/// <returns>Gotten data.</returns>
			public IDictionary<TKey, IEnumerable<TMarketData>> Get()
			{
				return _data.SyncGet(d =>
				{
					var retVal = d.ToDictionary(p => p.Key, p => (IEnumerable<TMarketData>)p.Value);
					d.Clear();
					return retVal;
				});
			}

			///// <summary>
			///// To get accumulated data from the buffer and delete them.
			///// </summary>
			///// <param name="key">The key possessing market data.</param>
			///// <returns>Gotten data.</returns>
			//public IEnumerable<TMarketData> Get(TKey key)
			//{
			//	if (key.IsDefault())
			//		throw new ArgumentNullException(nameof(key));

			//	return _data.SyncGet(d =>
			//	{
			//		var data = d.TryGetValue(key);

			//		if (data != null)
			//		{
			//			var retVal = data.CopyAndClear();
			//			d.Remove(key);
			//			return retVal;
			//		}

			//		return Enumerable.Empty<TMarketData>();
			//	});
			//}

			public void Clear()
			{
				_data.Clear();
			}
		}

		private readonly DataBuffer<SecurityId, ExecutionMessage> _ticksBuffer = new DataBuffer<SecurityId, ExecutionMessage>();
		private readonly DataBuffer<SecurityId, QuoteChangeMessage> _orderBooksBuffer = new DataBuffer<SecurityId, QuoteChangeMessage>();
		private readonly DataBuffer<SecurityId, ExecutionMessage> _orderLogBuffer = new DataBuffer<SecurityId, ExecutionMessage>();
		private readonly DataBuffer<SecurityId, Level1ChangeMessage> _level1Buffer = new DataBuffer<SecurityId, Level1ChangeMessage>();
		private readonly DataBuffer<SecurityId, PositionChangeMessage> _positionChangesBuffer = new DataBuffer<SecurityId, PositionChangeMessage>();
		private readonly DataBuffer<Tuple<SecurityId, Type, object>, CandleMessage> _candleBuffer = new DataBuffer<Tuple<SecurityId, Type, object>, CandleMessage>();
		private readonly DataBuffer<SecurityId, ExecutionMessage> _transactionsBuffer = new DataBuffer<SecurityId, ExecutionMessage>();
		private readonly SynchronizedSet<NewsMessage> _newsBuffer = new SynchronizedSet<NewsMessage>();
		private readonly SyncObject _subscriptionsLock = new SyncObject();
		private readonly HashSet<Tuple<SecurityId, DataType>> _subscriptions = new HashSet<Tuple<SecurityId, DataType>>();
		private readonly Dictionary<long, Tuple<MarketDataMessage, Tuple<SecurityId, DataType>>> _subscriptionsById = new Dictionary<long, Tuple<MarketDataMessage, Tuple<SecurityId, DataType>>>();
		private readonly SynchronizedDictionary<long, SecurityId> _securityIds = new SynchronizedDictionary<long, SecurityId>();

		/// <summary>
		/// Initializes a new instance of the <see cref="BufferMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public BufferMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
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
		/// Interpret tick messages as level1.
		/// </summary>
		public bool TicksAsLevel1 { get; set; } = true;

		/// <summary>
		/// Update filter with new subscription.
		/// </summary>
		/// <param name="message">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
		public void Subscribe(MarketDataMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var origin = (MarketDataMessage)message.Clone();
			
			lock (_subscriptionsLock)
			{
				DataType dataType;

				if (message.DataType == MarketDataTypes.CandleTimeFrame)
				{
					if (message.IsCalcVolumeProfile)
					{
						switch (message.BuildFrom)
						{
							case MarketDataTypes.Trades:
								dataType = TicksAsLevel1 ? DataType.Level1 : DataType.Ticks;
								break;

							case MarketDataTypes.MarketDepth:
								dataType = DataType.MarketDepth;
								break;

							default:
								dataType = DataType.Level1;
								break;
						}
					}
					else if (message.AllowBuildFromSmallerTimeFrame)
					{
						// null arg means do not use DataType.Arg as a filter
						dataType = DataType.Create(typeof(TimeFrameCandleMessage), null);
					}
					else
						dataType = DataType.Create(typeof(TimeFrameCandleMessage), message.Arg);
				}
				else
					dataType = CreateDataType(message);

				var subscription = Tuple.Create(message.SecurityId, dataType);

				_subscriptionsById.Add(message.TransactionId, Tuple.Create(origin, subscription));
				Subscribe(subscription);
			}
		}

		/// <summary>
		/// Update filter with remove a subscription.
		/// </summary>
		/// <param name="message">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
		public void UnSubscribe(MarketDataMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			lock (_subscriptionsLock)
			{
				if (!_subscriptionsById.TryGetValue(message.OriginalTransactionId, out var tuple))
					return;

				_subscriptionsById.Remove(message.OriginalTransactionId);
				_subscriptions.Remove(tuple.Item2);
			}
		}

		/// <summary>
		/// Update filter with new subscription.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Data type info.</param>
		public void Subscribe(SecurityId securityId, DataType dataType)
		{
			Subscribe(Tuple.Create(securityId, dataType));
		}

		private void Subscribe(Tuple<SecurityId, DataType> subscription)
		{
			_subscriptions.TryAdd(subscription);
		}

		private DataType CreateDataType(MarketDataMessage msg)
		{
			switch (msg.DataType)
			{
				case MarketDataTypes.Level1:
					return DataType.Level1;

				case MarketDataTypes.Trades:
					return TicksAsLevel1 ? DataType.Level1 : DataType.Ticks;

				case MarketDataTypes.MarketDepth:
					return DataType.MarketDepth;

				case MarketDataTypes.OrderLog:
					return DataType.OrderLog;

				case MarketDataTypes.News:
					return DataType.News;

				case MarketDataTypes.CandleTick:
					return DataType.Create(typeof(TickCandleMessage), msg.Arg);

				case MarketDataTypes.CandleVolume:
					return DataType.Create(typeof(VolumeCandleMessage), msg.Arg);

				case MarketDataTypes.CandleRange:
					return DataType.Create(typeof(RangeCandleMessage), msg.Arg);

				case MarketDataTypes.CandlePnF:
					return DataType.Create(typeof(PnFCandleMessage), msg.Arg);

				case MarketDataTypes.CandleRenko:
					return DataType.Create(typeof(RenkoCandleMessage), msg.Arg);

				default:
					throw new ArgumentOutOfRangeException(nameof(msg), msg.DataType, LocalizedStrings.Str1219);
			}
		}

		/// <summary>
		/// Remove all subscriptions.
		/// </summary>
		public void ClearSubscriptions()
		{
			lock (_subscriptionsLock)
			{
				_subscriptions.Clear();
				_subscriptionsById.Clear();
			}
		}

		/// <summary>
		/// Get accumulated ticks.
		/// </summary>
		/// <returns>Ticks.</returns>
		public IDictionary<SecurityId, IEnumerable<ExecutionMessage>> GetTicks()
		{
			return _ticksBuffer.Get();
		}

		/// <summary>
		/// Get accumulated order log.
		/// </summary>
		/// <returns>Order log.</returns>
		public IDictionary<SecurityId, IEnumerable<ExecutionMessage>> GetOrderLog()
		{
			return _orderLogBuffer.Get();
		}

		/// <summary>
		/// Get accumulated transactions.
		/// </summary>
		/// <returns>Transactions.</returns>
		public IDictionary<SecurityId, IEnumerable<ExecutionMessage>> GetTransactions()
		{
			return _transactionsBuffer.Get();
		}

		/// <summary>
		/// Get accumulated candles.
		/// </summary>
		/// <returns>Candles.</returns>
		public IDictionary<Tuple<SecurityId, Type, object>, IEnumerable<CandleMessage>> GetCandles()
		{
			return _candleBuffer.Get();
		}

		/// <summary>
		/// Get accumulated level1.
		/// </summary>
		/// <returns>Level1.</returns>
		public IDictionary<SecurityId, IEnumerable<Level1ChangeMessage>> GetLevel1()
		{
			return _level1Buffer.Get();
		}

		/// <summary>
		/// Get accumulated position changes.
		/// </summary>
		/// <returns>Position changes.</returns>
		public IDictionary<SecurityId, IEnumerable<PositionChangeMessage>> GetPositionChanges()
		{
			return _positionChangesBuffer.Get();
		}

		/// <summary>
		/// Get accumulated order books.
		/// </summary>
		/// <returns>Order books.</returns>
		public IDictionary<SecurityId, IEnumerable<QuoteChangeMessage>> GetOrderBooks()
		{
			return _orderBooksBuffer.Get();
		}

		/// <summary>
		/// Get accumulated news.
		/// </summary>
		/// <returns>News.</returns>
		public IEnumerable<NewsMessage> GetNews()
		{
			return _newsBuffer.SyncGet(c => c.CopyAndClear());
		}

		private bool CanStore<TMessage>(SecurityId securityId, object arg = null)
			where TMessage : Message
		{
			return CanStore(securityId, typeof(TMessage), arg);
		}

		private bool CanStore(SecurityId securityId, Type messageType, object arg)
		{
			if (!Enabled)
				return false;

			if (!FilterSubscription)
				return true;

			if (TicksAsLevel1 && arg == (object)ExecutionTypes.Tick)
			{
				messageType = typeof(Level1ChangeMessage);
				arg = null;
			}

			lock (_subscriptionsLock)
			{
				if (_subscriptions.Contains(Tuple.Create(securityId, DataType.Create(messageType, arg))))
					return true;

				if (arg == null)
					return false;

				// null arg means do not use DataType.Arg as a filter
				return _subscriptions.Contains(Tuple.Create(securityId, DataType.Create(messageType, null)));
			}
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		public override void SendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					ClearSubscriptions();

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
						OrderType = regMsg.OrderType,
						UserOrderId = regMsg.UserOrderId,
						OrderState = OrderStates.Pending,
						Condition = regMsg.Condition?.Clone(),
						//RepoInfo = regMsg.RepoInfo?.Clone(),
						//RpsInfo = regMsg.RpsInfo?.Clone(),
					});
					break;
				case MessageTypes.OrderCancel:
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
						IsCancelled = true,
						OrderId = cancelMsg.OrderId,
						OrderStringId = cancelMsg.OrderStringId,
						OriginalTransactionId = cancelMsg.OrderTransactionId,
						OrderVolume = cancelMsg.Volume,
						//Side = cancelMsg.Side,
					});
					break;
			}

			base.SendInMessage(message);
		}

		/// <summary>
		/// Process <see cref="MessageAdapterWrapper.InnerAdapter"/> output message.
		/// </summary>
		/// <param name="message">The message.</param>
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			if (message.IsBack)
			{
				base.OnInnerAdapterNewOutMessage(message);
				return;
			}

			switch (message.Type)
			{
				case MessageTypes.Level1Change:
				{
					var level1Msg = (Level1ChangeMessage)message;

					if (CanStore<Level1ChangeMessage>(level1Msg.SecurityId))
						_level1Buffer.Add(level1Msg.SecurityId, (Level1ChangeMessage)level1Msg.Clone());

					break;
				}
				case MessageTypes.QuoteChange:
				{
					var quotesMsg = (QuoteChangeMessage)message;

					if (CanStore<QuoteChangeMessage>(quotesMsg.SecurityId))
						_orderBooksBuffer.Add(quotesMsg.SecurityId, (QuoteChangeMessage)quotesMsg.Clone());

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
							
							// some responses do not contains sec id
							if (secId.IsDefault() && !_securityIds.TryGetValue(execMsg.OriginalTransactionId, out secId))
							{
								base.OnInnerAdapterNewOutMessage(message);
								return;
							}

							buffer = _transactionsBuffer;
							break;
						case ExecutionTypes.OrderLog:
							buffer = _orderLogBuffer;
							break;
						default:
							throw new ArgumentOutOfRangeException(nameof(message), execType, LocalizedStrings.Str1695Params.Put(message));
					}

					if (execType == ExecutionTypes.Transaction || CanStore<ExecutionMessage>(secId, execType))
						buffer.Add(secId, (ExecutionMessage)message.Clone());

					break;
				}
				case MessageTypes.CandlePnF:
				case MessageTypes.CandleRange:
				case MessageTypes.CandleRenko:
				case MessageTypes.CandleTick:
				case MessageTypes.CandleTimeFrame:
				case MessageTypes.CandleVolume:
				{
					var candleMsg = (CandleMessage)message;

					if (CanStore(candleMsg.SecurityId, candleMsg.GetType(), candleMsg.Arg))
						_candleBuffer.Add(Tuple.Create(candleMsg.SecurityId, candleMsg.GetType(), candleMsg.Arg), (CandleMessage)candleMsg.Clone());

					break;
				}
				case MessageTypes.News:
				{
					if (CanStore<NewsMessage>(default(SecurityId)))
						_newsBuffer.Add((NewsMessage)message.Clone());

					break;
				}
				//case MessageTypes.Position:
				//	break;
				//case MessageTypes.Portfolio:
				//	break;
				case MessageTypes.PositionChange:
				{
					var posMsg = (PositionChangeMessage)message;
					var secId = posMsg.SecurityId;

					//if (CanStore<PositionChangeMessage>(secId))
					_positionChangesBuffer.Add(secId, (PositionChangeMessage)posMsg.Clone());

					break;
				}
				case MessageTypes.PortfolioChange:
					// TODO
					break;
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Enabled), Enabled);
			storage.SetValue(nameof(FilterSubscription), FilterSubscription);
			storage.SetValue(nameof(TicksAsLevel1), TicksAsLevel1);
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Enabled = storage.GetValue(nameof(Enabled), Enabled);
			FilterSubscription = storage.GetValue(nameof(FilterSubscription), FilterSubscription);
			TicksAsLevel1 = storage.GetValue(nameof(TicksAsLevel1), TicksAsLevel1);
		}

		/// <summary>
		/// Create a copy of <see cref="StorageMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new BufferMessageAdapter(InnerAdapter);
		}
	}
}
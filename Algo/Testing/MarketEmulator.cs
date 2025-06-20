namespace StockSharp.Algo.Testing;

using StockSharp.Algo.Commissions;
using StockSharp.Algo.PnL;
using StockSharp.Algo.Candles;

using QuotesDict = SortedDictionary<decimal, RefPair<LevelOrders, QuoteChange>>;

class LevelOrders : IEnumerable<ExecutionMessage>
{
	private readonly Dictionary<long, ExecutionMessage> _ordersByTrId = [];

	public int Count => _ordersByTrId.Count;
	public decimal TotalBalance { get; set; }

	private static void Validate(ExecutionMessage order)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		if (order.TransactionId == default)
			throw new ArgumentException("TransactionId == default");

		if (order.Balance == default)
			throw new ArgumentException("Balance == default");
	}

	public bool TryGetAndRemoveByTransactionId(long transactionId, out ExecutionMessage order)
	{
		if (!_ordersByTrId.TryGetAndRemove(transactionId, out order))
			return false;

		TotalBalance -= order.Balance.Value;
		return true;
	}

	public void Add(ExecutionMessage order)
	{
		Validate(order);

		_ordersByTrId[order.TransactionId] = order;

		TotalBalance += order.Balance.Value;
	}

	public void Remove(ExecutionMessage order)
	{
		Validate(order);

		_ordersByTrId.Remove(order.TransactionId);

		TotalBalance -= order.Balance.Value;
	}

	public IEnumerator<ExecutionMessage> GetEnumerator() => _ordersByTrId.Values.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Emulator.
/// </summary>
public class MarketEmulator : BaseLogReceiver, IMarketEmulator
{
	private abstract class Pool<T>
	{
		private readonly Queue<T> _pool = [];

		public T Allocate()
		{
			if (_pool.Count == 0)
				return Create();
			else
				return _pool.Dequeue();
		}

		protected abstract T Create();

		public void Free(T message) => _pool.Enqueue(message);
	}

	private class ExecMsgPool : Pool<ExecutionMessage>
	{
		protected override ExecutionMessage Create() => new() { DataTypeEx = DataType.Transactions };
	}

	private class TickPool : Pool<(Sides? side, decimal price, decimal vol, DateTimeOffset time)[]>
	{
		protected override (Sides?, decimal, decimal, DateTimeOffset)[] Create()
			=> new (Sides?, decimal, decimal, DateTimeOffset)[4];
	}

	private sealed class SecurityMarketEmulator(MarketEmulator parent, SecurityId securityId) : BaseLogReceiver//, IMarketEmulator
	{
		private readonly MarketEmulator _parent = parent ?? throw new ArgumentNullException(nameof(parent));
		private readonly Dictionary<ExecutionMessage, TimeSpan> _expirableOrders = [];
		private readonly Dictionary<long, ExecutionMessage> _activeOrders = [];
		private readonly QuotesDict _bids = new(new BackwardComparer<decimal>());
		private readonly QuotesDict _asks = [];
		private readonly Dictionary<ExecutionMessage, TimeSpan> _pendingExecutions = [];
		private DateTimeOffset _prevTime;
		private readonly MarketEmulatorSettings _settings = parent.Settings;
		private readonly Random _volumeRandom = new(DateTime.Now.Millisecond);
		private readonly Random _priceRandom = new(DateTime.Now.Millisecond);
		private readonly RandomArray<bool> _isMatch = new(100);
		private int _volumeDecimals;
		private readonly SortedDictionary<DateTimeOffset, List<(CandleMessage candle, (Sides? side, decimal price, decimal vol, DateTimeOffset time)[] ticks)>> _candleInfo = [];
		private LogLevels? _logLevel;
		private DateTime _lastStripDate;

		private decimal _totalBidVolume;
		private decimal _totalAskVolume;

		private long? _depthSubscription;
		private long? _ticksSubscription;
		private long? _l1Subscription;
		private long? _olSubscription;
		private long? _candlesSubscription;

		private bool _candlesNonFinished;

		private bool _priceStepUpdated;
		private bool _volumeStepUpdated;
		private bool _priceStepExplicit;
		private bool _volumeStepExplicit;

		private decimal _prevTickPrice;
		private decimal _currSpreadPrice;

		private decimal? _l1BidPrice;
		private decimal? _l1AskPrice;
		private decimal? _l1BidVol;
		private decimal? _l1AskVol;

		// указывает, есть ли реальные стаканы, чтобы своей псевдо генерацией не портить настоящую историю
		private DateTime _lastDepthDate;
		//private DateTime _lastTradeDate;

		private readonly ExecMsgPool _messagePool = new();
		private readonly TickPool _tickPool = new();

		private OrderBookIncrementBuilder _bookBuilder;
		private bool? _booksWithState;
		private SecurityMessage _securityDefinition;
		public SecurityMessage SecurityDefinition => _securityDefinition;

		private void LogMessage(Message message, bool isInput)
		{
			_logLevel ??= this.GetLogLevel();

			if (_logLevel != LogLevels.Debug)
				return;

			if (message.Type is not MessageTypes.Time
				and not MessageTypes.Level1Change
				and not MessageTypes.QuoteChange)
				LogDebug((isInput ? " --> {0}" : " <-- {0}"), message);
		}

		public void Process(Message message, ICollection<Message> result)
		{
			if (_prevTime == DateTimeOffset.MinValue)
				_prevTime = message.LocalTime;

			LogMessage(message, true);

			switch (message.Type)
			{
				case MessageTypes.Time:
					//ProcessTimeMessage((TimeMessage)message, result);
					break;

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					UpdatePriceLimits(execMsg, result);

					if (execMsg.DataType == DataType.Ticks)
					{
						ProcessTick(execMsg, result);
					}
					else if (execMsg.DataType == DataType.Transactions)
					{
						if (!execMsg.HasOrderInfo())
							throw new InvalidOperationException();

						if (_settings.Latency > TimeSpan.Zero)
						{
							LogInfo(LocalizedStrings.WaitingForOrder, execMsg.IsCancellation ? LocalizedStrings.Cancellation : LocalizedStrings.Registration, execMsg.TransactionId == 0 ? execMsg.OriginalTransactionId : execMsg.TransactionId);
							_pendingExecutions.Add(execMsg.TypedClone(), _settings.Latency);
						}
						else
							AcceptExecution(execMsg.LocalTime, execMsg, result);
					}
					else if (execMsg.DataType == DataType.OrderLog)
					{
						if (execMsg.TradeId == null)
							UpdateQuotes(execMsg, result);

						// добавляем в результат ОЛ только из хранилища или из генератора
						// (не из ExecutionLogConverter)
						//if (execMsg.TransactionId > 0)
						//	result.Add(execMsg);

						if (_olSubscription is not null)
							result.Add(execMsg);
					}
					else
						throw new ArgumentOutOfRangeException(nameof(message), execMsg.DataType, LocalizedStrings.InvalidValue);

					break;
				}

				case MessageTypes.OrderRegister:
				{
					var orderMsg = (OrderRegisterMessage)message;

					foreach (var m in ToExecutionLog(orderMsg, GetTotalVolume(orderMsg.Side.Invert())))
						Process(m, result);

					break;
				}

				case MessageTypes.OrderCancel:
				{
					var orderMsg = (OrderCancelMessage)message;

					foreach (var m in ToExecutionLog(orderMsg, 0))
						Process(m, result);

					break;
				}

				case MessageTypes.OrderReplace:
				{
					//при перерегистрации могут приходить заявки с нулевым объемом
					//объем при этом надо взять из старой заявки.
					var orderMsg = (OrderReplaceMessage)message;
					var oldOrder = _activeOrders.TryGetValue(orderMsg.OriginalTransactionId);

					foreach (var execMsg in ToExecutionLog(orderMsg, GetTotalVolume(orderMsg.Side.Invert())))
					{
						if (oldOrder != null)
						{
							if (!execMsg.IsCancellation && execMsg.OrderVolume == 0)
								execMsg.OrderVolume = oldOrder.Balance;

							Process(execMsg, result);
						}
						else if (execMsg.IsCancellation)
						{
							var error = LocalizedStrings.OrderForReplaceNotFound.Put(execMsg.OrderId);
							var serverTime = GetServerTime(orderMsg.LocalTime);

							// cancellation error
							result.Add(new ExecutionMessage
							{
								LocalTime = orderMsg.LocalTime,
								OriginalTransactionId = orderMsg.TransactionId,
								OrderId = execMsg.OrderId,
								DataTypeEx = DataType.Transactions,
								SecurityId = orderMsg.SecurityId,
								IsCancellation = true,
								OrderState = OrderStates.Failed,
								Error = new InvalidOperationException(error),
								ServerTime = serverTime,
								HasOrderInfo = true,
							});

							// registration error
							result.Add(new ExecutionMessage
							{
								LocalTime = orderMsg.LocalTime,
								OriginalTransactionId = orderMsg.TransactionId,
								DataTypeEx = DataType.Transactions,
								SecurityId = orderMsg.SecurityId,
								IsCancellation = false,
								OrderState = OrderStates.Failed,
								Error = new InvalidOperationException(error),
								ServerTime = serverTime,
								HasOrderInfo = true,
							});

							LogError(LocalizedStrings.OrderForReplaceNotFound, orderMsg.OriginalTransactionId);
						}
					}

					break;
				}

				case MessageTypes.OrderStatus:
				{
					var statusMsg = (OrderStatusMessage)message;
					var checkByPf = !statusMsg.PortfolioName.IsEmpty();

					var finish = false;

					foreach (var order in _activeOrders.Values)
					{
						if (checkByPf)
						{
							if (!order.PortfolioName.EqualsIgnoreCase(statusMsg.PortfolioName))
								continue;
						}
						else if (statusMsg.OrderId != null)
						{
							if (order.OrderId != statusMsg.OrderId)
								continue;

							finish = true;
						}

						var clone = order.TypedClone();
						clone.OriginalTransactionId = statusMsg.TransactionId;
						result.Add(clone);

						if (finish)
							break;
					}

					break;
				}

				case MessageTypes.QuoteChange:
					ProcessQuoteChange((QuoteChangeMessage)message, result);
					break;

				case MessageTypes.Level1Change:
					ProcessLevel1((Level1ChangeMessage)message, result);
					break;

				case MessageTypes.Security:
				{
					_securityDefinition = (SecurityMessage)message.Clone();
					_volumeDecimals = GetVolumeStep().GetCachedDecimals();
					UpdateSecurityDefinition(_securityDefinition);
					break;
				}

				case MessageTypes.Board:
				{
					//_execLogConverter.UpdateBoardDefinition((BoardMessage)message);
					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.IsSubscribe)
					{
						if (mdMsg.DataType2 == DataType.MarketDepth)
							_depthSubscription = mdMsg.TransactionId;
						else if (mdMsg.DataType2 == DataType.Ticks)
							_ticksSubscription = mdMsg.TransactionId;
						else if (mdMsg.DataType2 == DataType.Level1)
							_l1Subscription = mdMsg.TransactionId;
						else if (mdMsg.DataType2 == DataType.OrderLog)
							_olSubscription = mdMsg.TransactionId;
						else if (mdMsg.DataType2.IsCandles)
						{
							_candlesSubscription = mdMsg.TransactionId;
							_candlesNonFinished = !mdMsg.IsFinishedOnly;
						}
					}
					else
					{
						if (_depthSubscription == mdMsg.OriginalTransactionId)
							_depthSubscription = null;
						else if (_ticksSubscription == mdMsg.OriginalTransactionId)
							_ticksSubscription = null;
						else if (_l1Subscription == mdMsg.OriginalTransactionId)
							_l1Subscription = null;
						else if (_olSubscription == mdMsg.OriginalTransactionId)
							_olSubscription = null;
						else if (_candlesSubscription == mdMsg.OriginalTransactionId)
						{
							_candlesSubscription = null;
							_candlesNonFinished = false;
						}
					}

					break;
				}

				default:
				{
					if (message is CandleMessage candleMsg)
					{
						// в трейдах используется время открытия свечи, при разных MarketTimeChangedInterval и TimeFrame свечек
						// возможны ситуации, когда придет TimeMsg 11:03:00, а время закрытия будет 11:03:30
						// т.о. время уйдет вперед данных, которые построены по свечкам.
						var candles = _candleInfo.SafeAdd(candleMsg.OpenTime, key => []);

						(Sides? side, decimal price, decimal vol, DateTimeOffset time)[] ticks = null;

						if (_securityDefinition is not null && _ticksSubscription is not null)
						{
							ticks = _tickPool.Allocate();
							candleMsg.ConvertToTrades(GetVolumeStep(), _volumeDecimals, ticks);

							//ProcessTick(candleMsg.SecurityId, candleMsg.LocalTime, ticks[0], result);
						}

						candles.Add((candleMsg, ticks));

						break;
					}

					throw new ArgumentOutOfRangeException(nameof(message), message.Type, LocalizedStrings.InvalidValue);
				}
			}

			ProcessTime(message, result);

			_prevTime = message.LocalTime;

			foreach (var item in result)
				LogMessage(item, false);
		}

		private ExecutionMessage CreateMessage(DateTimeOffset localTime, DateTimeOffset serverTime, Sides side, decimal price, decimal volume, bool isCancelling = false, TimeInForce tif = TimeInForce.PutInQueue)
		{
			if (price <= 0)
				throw new ArgumentOutOfRangeException(nameof(price), price, LocalizedStrings.InvalidValue);

			if (volume <= 0)
				throw new ArgumentOutOfRangeException(nameof(volume), volume, LocalizedStrings.InvalidValue);

			return new()
			{
				Side = side,
				OrderPrice = price,
				OrderVolume = volume,
				DataTypeEx = DataType.OrderLog,
				IsCancellation = isCancelling,
				SecurityId = securityId,
				LocalTime = localTime,
				ServerTime = serverTime,
				TimeInForce = tif,
			};
		}

		private IEnumerable<ExecutionMessage> ToExecutionLog(OrderMessage message, decimal quotesVolume)
		{
			var serverTime = GetServerTime(message.LocalTime);
			var priceStep = GetPriceStep();

			bool NeedCheckVolume(OrderRegisterMessage message)
			{
				if (!_settings.IncreaseDepthVolume || _candlesSubscription is not null)
					return false;

				var orderSide = message.Side;
				var price = message.Price;

				var quotes = GetQuotes(orderSide.Invert());

				var quote = quotes.FirstOrDefault();

				if (quote.Value == null)
					return false;

				var bestPrice = quote.Key;

				return (orderSide == Sides.Buy ? price >= bestPrice : price <= bestPrice)
					&& quotesVolume <= message.Volume;
			}

			IEnumerable<ExecutionMessage> IncreaseDepthVolume(OrderRegisterMessage message)
			{
				var leftVolume = (message.Volume - quotesVolume) + 1;
				var orderSide = message.Side;

				var quotes = GetQuotes(orderSide.Invert());
				var quote = quotes.LastOrDefault();

				if (quote.Value == null)
					yield break;

				var side = orderSide.Invert();

				var lastVolume = quote.Value.Second.Volume;
				var lastPrice = quote.Value.Second.Price;

				if (lastVolume <= 0)
					throw new InvalidOperationException($"lastVolume={lastVolume}<=0");

				while (leftVolume > 0 && lastPrice != 0)
				{
					lastVolume *= 2;
					lastPrice += priceStep * (side == Sides.Buy ? -1 : 1);

					leftVolume -= lastVolume;

					yield return CreateMessage(message.LocalTime, serverTime, side, lastPrice, lastVolume);
				}
			}

			switch (message.Type)
			{
				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;

					if (NeedCheckVolume(regMsg))
					{
						foreach (var executionMessage in IncreaseDepthVolume(regMsg))
							yield return executionMessage;
					}

					yield return new ExecutionMessage
					{
						LocalTime = regMsg.LocalTime,
						ServerTime = serverTime,
						SecurityId = regMsg.SecurityId,
						DataTypeEx = DataType.Transactions,
						HasOrderInfo = true,
						TransactionId = regMsg.TransactionId,
						OrderPrice = regMsg.Price,
						OrderVolume = regMsg.Volume,
						Side = regMsg.Side,
						PortfolioName = regMsg.PortfolioName,
						OrderType = regMsg.OrderType,
						UserOrderId = regMsg.UserOrderId,
						ExpiryDate = regMsg.TillDate,
						PostOnly = regMsg.PostOnly,
						TimeInForce = regMsg.TimeInForce,
					};

					yield break;
				}
				case MessageTypes.OrderReplace:
				{
					var replaceMsg = (OrderReplaceMessage)message;

					if (NeedCheckVolume(replaceMsg))
					{
						foreach (var executionMessage in IncreaseDepthVolume(replaceMsg))
							yield return executionMessage;
					}

					yield return new ExecutionMessage
					{
						LocalTime = replaceMsg.LocalTime,
						ServerTime = serverTime,
						SecurityId = replaceMsg.SecurityId,
						DataTypeEx = DataType.Transactions,
						HasOrderInfo = true,
						IsCancellation = true,
						OrderId = replaceMsg.OldOrderId,
						OriginalTransactionId = replaceMsg.OriginalTransactionId,
						TransactionId = replaceMsg.TransactionId,
						PortfolioName = replaceMsg.PortfolioName,
						OrderType = replaceMsg.OrderType,
						// для старой заявки пользовательский идентификатор менять не надо
						//UserOrderId = replaceMsg.UserOrderId
					};

					yield return new ExecutionMessage
					{
						LocalTime = replaceMsg.LocalTime,
						ServerTime = serverTime,
						SecurityId = replaceMsg.SecurityId,
						DataTypeEx = DataType.Transactions,
						HasOrderInfo = true,
						TransactionId = replaceMsg.TransactionId,
						OrderPrice = replaceMsg.Price,
						OrderVolume = replaceMsg.Volume,
						Side = replaceMsg.Side,
						PortfolioName = replaceMsg.PortfolioName,
						OrderType = replaceMsg.OrderType,
						UserOrderId = replaceMsg.UserOrderId,
						ExpiryDate = replaceMsg.TillDate,
						PostOnly = replaceMsg.PostOnly,
						TimeInForce = replaceMsg.TimeInForce,
					};

					yield break;
				}
				case MessageTypes.OrderCancel:
				{
					var cancelMsg = (OrderCancelMessage)message;

					yield return new ExecutionMessage
					{
						DataTypeEx = DataType.Transactions,
						HasOrderInfo = true,
						IsCancellation = true,
						OrderId = cancelMsg.OrderId,
						TransactionId = cancelMsg.TransactionId,
						OriginalTransactionId = cancelMsg.OriginalTransactionId,
						PortfolioName = cancelMsg.PortfolioName,
						SecurityId = cancelMsg.SecurityId,
						LocalTime = cancelMsg.LocalTime,
						ServerTime = serverTime,
						OrderType = cancelMsg.OrderType,
						// при отмене заявки пользовательский идентификатор не меняется
						//UserOrderId = cancelMsg.UserOrderId
					};

					yield break;
				}

				case MessageTypes.OrderGroupCancel:
					throw new NotSupportedException();

				default:
					throw new ArgumentOutOfRangeException(nameof(message), message.Type, LocalizedStrings.InvalidValue);
			}
		}

		private decimal GetRandomVolume() => _volumeRandom.Next(10, 100);

		private void ProcessLevel1(Level1ChangeMessage message, ICollection<Message> result)
		{
			UpdateSecurityDefinition(message);

			var localTime = message.LocalTime;

			if (message.IsContainsTick())
			{
				ProcessTick(message.ToTick(), result);
			}
			else if (message.IsContainsQuotes() && !HasDepth(localTime))
			{
				var prevBidPrice = _l1BidPrice;
				var prevAskPrice = _l1AskPrice;
				var prevBidVol = _l1BidVol;
				var prevAskVol = _l1AskVol;

				_l1BidPrice = (decimal?)message.Changes.TryGetValue(Level1Fields.BestBidPrice) ?? _l1BidPrice;
				_l1AskPrice = (decimal?)message.Changes.TryGetValue(Level1Fields.BestAskPrice) ?? _l1AskPrice;
				_l1BidVol = (decimal?)message.Changes.TryGetValue(Level1Fields.BestBidVolume) ?? _l1BidVol;
				_l1AskVol = (decimal?)message.Changes.TryGetValue(Level1Fields.BestAskVolume) ?? _l1AskVol;

				if (_l1BidPrice == 0)
					_l1BidPrice = null;

				if (_l1AskPrice == 0)
					_l1AskPrice = null;

				if (_l1BidPrice is null && _l1AskPrice is null)
					return;

				if (_l1BidVol == 0)
					_l1BidVol = null;
				else
					_l1BidVol ??= GetRandomVolume();

				if (_l1AskVol == 0)
					_l1AskVol = null;
				else
					_l1AskVol ??= GetRandomVolume();

				if (prevBidPrice == _l1BidPrice && prevAskPrice == _l1AskPrice && prevBidVol == _l1BidVol && prevAskVol == _l1AskVol)
					return;

				void AddQuote(QuotesDict quotes, decimal price, decimal volume)
					=> quotes.Add(price, new([], new() { Price = price, Volume = volume }));

				var serverTime = message.ServerTime;

				if (_activeOrders.Count == 0)
				{
					if (_l1BidPrice is not null)
					{
						foreach (var p in _bids.Values)
						{
							foreach (var quote in p.First)
								_messagePool.Free(quote);
						}

						_bids.Clear();

						AddQuote(_bids, _l1BidPrice.Value, _totalBidVolume = _l1BidVol.Value);
					}

					if (_l1AskPrice is not null)
					{
						foreach (var p in _asks.Values)
						{
							foreach (var quote in p.First)
								_messagePool.Free(quote);
						}

						_asks.Clear();

						AddQuote(_asks, _l1AskPrice.Value, _totalAskVolume = _l1AskVol.Value);
					}

					Verify();
				}
				else
				{
					var bestBid = _bids.FirstOrDefault();
					var bestAsk = _asks.FirstOrDefault();

					void ProcessMarketOrder(QuotesDict quotes, Sides orderSide, decimal price, decimal volume)
					{
						var quotesSide = orderSide.Invert();

						Verify();

						var sign = orderSide == Sides.Buy ? -1 : 1;
						var hasQuotes = false;

						List<RefPair<LevelOrders, QuoteChange>> toRemove = null;

						foreach (var pair in quotes)
						{
							var quote = pair.Value.Second;

							if (quote.Price * sign > price * sign)
							{
								toRemove ??= [];
								toRemove.Add(pair.Value);
							}
							else
							{
								if (quote.Price == price)
								{
									toRemove ??= [];
									toRemove.Add(pair.Value);
								}
								else
								{
									if ((price - quote.Price).Abs() == _securityDefinition.PriceStep)
									{
										// если на один шаг цены выше/ниже есть котировка, то не выполняем никаких действий
										// иначе добавляем новый уровень в стакан, чтобы не было большого расхождения цен.
										hasQuotes = true;
									}

									break;
								}
							}
						}

						if (toRemove is not null)
						{
							var totalVolumeDiff = 0m;

							foreach (var pair in toRemove)
							{
								quotes.Remove(pair.Second.Price);

								totalVolumeDiff += pair.Second.Volume;

								foreach (var quote in pair.First)
								{
									if (quote.PortfolioName is not null)
									{
										if (TryRemoveActiveOrder(quote.TransactionId, out var orderMsg))
										{
											orderMsg.OriginalTransactionId = quote.TransactionId;
											
											ExecuteOrder(localTime, orderMsg, pair.Second.Price, result);
										}
									}

									_messagePool.Free(quote);
								}
							}

							AddTotalVolume(quotesSide, -totalVolumeDiff);

							Verify();
						}

						// если собрали все котировки, то оставляем заявку в стакане по цене котировки
						if (!hasQuotes)
						{
							UpdateQuote(CreateMessage(localTime, serverTime, quotesSide, price, volume), true);

							Verify();
						}
					}

					if (bestBid.Value is null)
					{
						if (_l1BidPrice is not null)
							AddQuote(_bids, _l1BidPrice.Value, _l1BidVol.Value);
					}
					else
					{
						if (_l1AskPrice <= bestBid.Key)
							ProcessMarketOrder(_bids, Sides.Sell, _l1AskPrice.Value, _l1AskVol ?? 1);
					}

					if (bestAsk.Value is null)
					{
						if (_l1AskPrice is not null)
							AddQuote(_asks, _l1AskPrice.Value, _l1AskVol.Value);
					}
					else
					{
						if (_l1BidPrice >= bestAsk.Key)
							ProcessMarketOrder(_asks, Sides.Buy, _l1BidPrice.Value, _l1BidVol ?? 1);
					}

					if (_l1BidPrice is not null && !_bids.ContainsKey(_l1BidPrice.Value))
						AddQuote(_bids, _l1BidPrice.Value, _l1BidVol.Value);

					if (_l1AskPrice is not null && !_asks.ContainsKey(_l1AskPrice.Value))
						AddQuote(_asks, _l1AskPrice.Value, _l1AskVol.Value);

					CancelWorst(localTime, serverTime);

					Verify();
				}

				if (_depthSubscription is not null)
				{
					result.Add(CreateQuoteMessage(
						message.SecurityId,
						localTime,
						serverTime));
				}
			}

			if (_l1Subscription is not null)
				result.Add(message);
		}

		private void ProcessQuoteChange(QuoteChangeMessage message, ICollection<Message> result)
		{
			var localTime = message.LocalTime;
			var serverTime = message.ServerTime;

			if (message.State is null)
			{
				if (_booksWithState is null)
					_booksWithState = false;
				else if (_booksWithState == true)
					return;

				if (!_priceStepUpdated || !_volumeStepUpdated)
				{
					var quote = message.GetBestBid() ?? message.GetBestAsk();

					if (quote != null)
						UpdateSteps(quote.Value.Price, quote.Value.Volume);
				}

				_lastDepthDate = message.LocalTime.Date;

				if (_activeOrders.Count == 0)
				{
					foreach (var p in _bids.Values)
					{
						foreach (var quote in p.First)
							_messagePool.Free(quote);
					}

					foreach (var p in _asks.Values)
					{
						foreach (var quote in p.First)
							_messagePool.Free(quote);
					}

					_bids.Clear();
					_asks.Clear();

					_totalBidVolume = 0;
					_totalAskVolume = 0;

					decimal? bestBidPrice = null;
					decimal? bestAskPrice = null;

					foreach (var bid in message.Bids)
					{
						bestBidPrice ??= bid.Price;
						_bids.Add(bid.Price, new([], new() { Price = bid.Price, Volume = bid.Volume }));

						_totalBidVolume += bid.Volume;
					}

					foreach (var ask in message.Asks)
					{
						bestAskPrice ??= ask.Price;
						_asks.Add(ask.Price, new([], new() { Price = ask.Price, Volume = ask.Volume }));

						_totalAskVolume += ask.Volume;
					}

					_currSpreadPrice = bestBidPrice.GetSpreadMiddle(bestAskPrice, null) ?? 0;

					Verify();
				}
				else
				{
					var diff = new List<ExecutionMessage>();

					decimal GetDiff(QuotesDict from, QuoteChange[] to, Sides side)
					{
						void AddExecMsg(QuoteChange quote, decimal volume, Sides side, bool isSpread)
						{
							if (volume > 0)
								diff.Add(CreateMessage(localTime, serverTime, side, quote.Price, volume));
							else
							{
								volume = volume.Abs();

								// matching only top orders (spread)
								if (isSpread && volume > 1 && _isMatch.Next())
								{
									var tradeVolume = (int)volume / 2;

									diff.Add(new ExecutionMessage
									{
										Side = side,
										TradeVolume = tradeVolume,
										DataTypeEx = DataType.Ticks,
										SecurityId = securityId,
										LocalTime = localTime,
										ServerTime = serverTime,
										TradePrice = quote.Price,
									});

									// that tick will not affect on order book
									//volume -= tradeVolume;
								}

								diff.Add(CreateMessage(localTime, serverTime, side, quote.Price, volume, true));
							}
						}

						var newBestPrice = 0m;

						var canProcessFrom = true;
						var canProcessTo = true;

						QuoteChange? currFrom = null;
						QuoteChange? currTo = null;

						var mult = side == Sides.Buy ? -1 : 1;
						bool? isSpread = null;

						using var fromEnum = from.GetEnumerator();
						using var toEnum = ((IEnumerable<QuoteChange>)to).GetEnumerator();

						var maxIter = 1000;

						while (maxIter-- > 0)
						{
							if (canProcessFrom && currFrom == null)
							{
								if (!fromEnum.MoveNext())
									canProcessFrom = false;
								else
								{
									currFrom = fromEnum.Current.Value.Second;
									isSpread = isSpread == null;
								}
							}

							if (canProcessTo && currTo == null)
							{
								if (!toEnum.MoveNext())
									canProcessTo = false;
								else
								{
									currTo = toEnum.Current;

									if (newBestPrice == 0)
										newBestPrice = currTo.Value.Price;
								}
							}

							if (currFrom == null)
							{
								if (currTo == null)
									break;
								else
								{
									var v = currTo.Value;

									AddExecMsg(v, v.Volume, side, false);
									currTo = null;
								}
							}
							else
							{
								if (currTo == null)
								{
									var v = currFrom.Value;
									AddExecMsg(v, -v.Volume, side, isSpread.Value);
									currFrom = null;
								}
								else
								{
									var f = currFrom.Value;
									var t = currTo.Value;

									if (f.Price == t.Price)
									{
										if (f.Volume != t.Volume)
										{
											AddExecMsg(t, t.Volume - f.Volume, side, isSpread.Value);
										}

										currFrom = currTo = null;
									}
									else if (f.Price * mult > t.Price * mult)
									{
										AddExecMsg(t, t.Volume, side, isSpread.Value);
										currTo = null;
									}
									else
									{
										AddExecMsg(f, -f.Volume, side, isSpread.Value);
										currFrom = null;
									}
								}
							}
						}

						return newBestPrice;
					}

					var bestBidPrice = GetDiff(_bids, message.Bids, Sides.Buy);
					var bestAskPrice = GetDiff(_asks, message.Asks, Sides.Sell);

					var spreadPrice = bestAskPrice == 0
						? bestBidPrice
						: (bestBidPrice == 0
							? bestAskPrice
							: (bestAskPrice - bestBidPrice) / 2 + bestBidPrice);

					//при обновлении стакана необходимо учитывать направление сдвига, чтобы не было ложного исполнения при наложении бидов и асков.
					//т.е. если цена сдвинулась вниз, то обновление стакана необходимо начинать с минимального бида.
					var diffs = (spreadPrice < _currSpreadPrice)
							? diff.OrderBy(m => m.OrderPrice)
							: diff.OrderByDescending(m => m.OrderPrice);

					foreach (var m in diffs)
					{
						if (m.DataType == DataType.Ticks)
						{
							m.ServerTime = message.ServerTime;
							result.Add(m);
						}
						else
							Process(m, result);
					}

					_currSpreadPrice = spreadPrice;
				}
			}
			else
			{
				if (_booksWithState is null)
					_booksWithState = true;
				else if (_booksWithState == false)
					return;

				if (message.HasPositions)
					throw new NotSupportedException("Order books with positions not supported.");

				_lastDepthDate = message.LocalTime.Date;

				var canApply = false;

				switch (message.State)
				{
					case QuoteChangeStates.SnapshotStarted:
					case QuoteChangeStates.SnapshotBuilding:
					case QuoteChangeStates.SnapshotComplete:
					{
						_bookBuilder ??= new(securityId);

						canApply = message.State == QuoteChangeStates.SnapshotComplete;
						message = _bookBuilder.TryApply(message);

						if (canApply)
							_bookBuilder = null;

						break;
					}
					case QuoteChangeStates.Increment:
						if (_bookBuilder is not null)
							return;

						canApply = true;
						break;
					default:
						throw new ArgumentOutOfRangeException(message.State.To<string>());
				}

				if (canApply)
				{
					var isIncremental = message.State == QuoteChangeStates.Increment;

					void Apply(Sides side, QuotesDict quotes, QuoteChange[] newState)
					{
						var toRemove = isIncremental ? null : quotes.Keys.ToHashSet();

						void RemoveLevel(decimal price)
						{
							if (!quotes.TryGetAndRemove(price, out var p))
								return;

							foreach (var quote in p.First)
							{
								_messagePool.Free(quote);

								if (!TryRemoveActiveOrder(quote.TransactionId, out var orderMsg))
									continue;

								ExecuteOrder(localTime, orderMsg, price, result);
							}

							AddTotalVolume(side, -p.Second.Volume);
						}

						foreach (var newQuote in newState)
						{
							var price = newQuote.Price;
							var volume = newQuote.Volume;

							if (isIncremental && volume == 0)
							{
								RemoveLevel(price);
								continue;
							}

							toRemove?.Remove(price);

							if (quotes.TryGetValue(price, out var p))
							{
								var quote = p.Second;

								var diff = quote.Volume - volume;

								AddTotalVolume(side, -diff);

								// old volume is more than new
								if (diff > 0)
								{
									foreach (var lq in p.First)
									{
										if (!TryRemoveActiveOrder(lq.TransactionId, out var orderMsg))
											continue;

										var balance = orderMsg.Balance.Value;

										if (balance <= diff)
										{
											orderMsg.Balance = 0;
											orderMsg.OrderState = OrderStates.Done;
										}
										else
										{
											orderMsg.Balance -= diff;
										}

										ProcessOrder(localTime, orderMsg, result);

										ProcessTrade(localTime, orderMsg, price, balance, result);

										diff -= balance;

										if (diff <= 0)
											break;
									}
								}

								quote.Volume = volume;
								p.Second = quote;
							}
							else
							{
								quotes.Add(price, RefTuple.Create(new LevelOrders(), newQuote));

								AddTotalVolume(side, volume);
							}
						}

						if (toRemove is not null)
						{
							foreach (var price in toRemove)
								RemoveLevel(price);
						}
					}

					Apply(Sides.Buy, _bids, message.Bids);
					Apply(Sides.Sell, _asks, message.Asks);

					while (_bids.Count > 0 && _asks.Count > 0)
					{
						var bestBid = _bids.First();
						var bestAsk = _asks.First();

						if (bestBid.Key < bestAsk.Key)
							break;

						var modified = false;

						// сдвиг идет бидами (убираем аск)
						if (message.Bids.Length > 0 && message.Bids[0].Volume > 0)
						{
							modified = true;

							_asks.Remove(bestAsk.Key);

							var levelOrders = bestAsk.Value.First;

							if (levelOrders.Count > 0)
							{
								foreach (var order in levelOrders)
								{
									ExecuteOrder(localTime, order, bestAsk.Key, result);

									_messagePool.Free(order);
								}
							}

							AddTotalVolume(Sides.Sell, -bestAsk.Value.Second.Volume);
						}

						// сдвиг идет асками (убираем бид)
						if (message.Asks.Length > 0 && message.Asks[0].Volume > 0)
						{
							modified = true;

							_bids.Remove(bestBid.Key);

							var levelOrders = bestBid.Value.First;

							if (levelOrders.Count > 0)
							{
								foreach (var order in levelOrders)
								{
									ExecuteOrder(localTime, order, bestBid.Key, result);

									_messagePool.Free(order);
								}
							}

							AddTotalVolume(Sides.Buy, -bestBid.Value.Second.Volume);
						}

						if (!modified)
							break;
					}

					Verify();
				}
			}

			if (_depthSubscription is not null)
			{
				// возращаем не входящий стакан, а тот, что сейчас хранится внутри эмулятора.
				// таким образом мы можем видеть в стакане свои цены и объемы

				result.Add(CreateQuoteMessage(
					message.SecurityId,
					localTime,
					serverTime));
			}
		}

		private void ProcessTick(ExecutionMessage tick, ICollection<Message> result)
		{
			ProcessTick(tick.SecurityId, tick.LocalTime, (tick.OriginSide, tick.GetTradePrice(), tick.TradeVolume ?? 1, tick.ServerTime), result);
		}

		private bool AddActiveOrder(ExecutionMessage orderMsg, DateTimeOffset time)
		{
			_activeOrders.Add(orderMsg.TransactionId, orderMsg);

			if (orderMsg.ExpiryDate is DateTimeOffset expiry)
			{
				var left = expiry - time;

				if (left > TimeSpan.Zero)
					_expirableOrders.Add(orderMsg, left);
				else
					return false;
			}

			return true;
		}

		private bool TryRemoveActiveOrder(long transId, out ExecutionMessage orderMsg)
		{
			if (!_activeOrders.TryGetAndRemove(transId, out orderMsg))
				return false;

			_expirableOrders.Remove(orderMsg);
			return true;
		}

		private void ProcessTick(SecurityId secId, DateTimeOffset localTime, (Sides? side, decimal price, decimal volume, DateTimeOffset time) tick, ICollection<Message> result)
		{
			var tradePrice = tick.price;
			var tradeVolume = tick.volume;
			var side = tick.side;
			var serverTime = tick.time;

			UpdateSteps(tradePrice, tradeVolume);

			// some feeds send zero volume
			if (tradeVolume == 0)
				tradeVolume = GetRandomVolume();

			var bestBid = _bids.FirstOrDefault();
			var bestAsk = _asks.FirstOrDefault();

			var hasDepth = HasDepth(localTime);

			void TryCreateOppositeOrder(Sides originSide)
			{
				var quotesSide = originSide.Invert();
				var quotes = GetQuotes(quotesSide);

				var priceStep = GetPriceStep();
				var oppositePrice = (tradePrice + _settings.SpreadSize * priceStep * (originSide == Sides.Buy ? 1 : -1)).Max(priceStep);

				var bestQuote = quotes.FirstOrDefault();

				if (bestQuote.Value == null || ((originSide == Sides.Buy && oppositePrice < bestQuote.Key) || (originSide == Sides.Sell && oppositePrice > bestQuote.Key)))
				{
					UpdateQuote(CreateMessage(localTime, serverTime, quotesSide, oppositePrice, tradeVolume), true);

					Verify();
				}
			}

			Sides GetOrderSide()
			{
				return side is null
					? tradePrice > _prevTickPrice ? Sides.Sell : Sides.Buy
					: side.Value.Invert();
			}

			void ProcessMarketOrder(Sides orderSide)
			{
				var quotesSide = orderSide.Invert();
				var quotes = GetQuotes(quotesSide);

				Verify();

				var sign = orderSide == Sides.Buy ? -1 : 1;
				var hasQuotes = false;

				List<RefPair<LevelOrders, QuoteChange>> toRemove = null;

				foreach (var pair in quotes)
				{
					var quote = pair.Value.Second;

					if (quote.Price * sign > tradePrice * sign)
					{
						toRemove ??= [];
						toRemove.Add(pair.Value);
					}
					else
					{
						if (quote.Price == tradePrice)
						{
							toRemove ??= [];
							toRemove.Add(pair.Value);
						}
						else
						{
							if ((tradePrice - quote.Price).Abs() == _securityDefinition.PriceStep)
							{
								// если на один шаг цены выше/ниже есть котировка, то не выполняем никаких действий
								// иначе добавляем новый уровень в стакан, чтобы не было большого расхождения цен.
								hasQuotes = true;
							}

							break;
						}
					}
				}

				if (toRemove is not null)
				{
					var totalVolumeDiff = 0m;

					foreach (var pair in toRemove)
					{
						quotes.Remove(pair.Second.Price);

						totalVolumeDiff += pair.Second.Volume;

						foreach (var quote in pair.First)
						{
							if (quote.PortfolioName is not null)
							{
								if (TryRemoveActiveOrder(quote.TransactionId, out var orderMsg))
								{
									orderMsg.OriginalTransactionId = quote.TransactionId;
									ExecuteOrder(localTime, orderMsg, pair.Second.Price, result);
								}
							}

							_messagePool.Free(quote);
						}
					}

					AddTotalVolume(quotesSide, -totalVolumeDiff);

					Verify();
				}

				// если собрали все котировки, то оставляем заявку в стакане по цене сделки
				if (!hasQuotes)
				{
					UpdateQuote(CreateMessage(localTime, serverTime, quotesSide, tradePrice, tradeVolume), true);

					Verify();
				}
			}

			if (bestBid.Value is not null && tradePrice <= bestBid.Key)
			{
				// тик попал в биды, значит была крупная заявка по рынку на продажу,
				// которая возможна исполнила наши заявки

				ProcessMarketOrder(Sides.Sell);

				if (!hasDepth)
				{
					// подтягиваем противоположные котировки и снимаем лишние заявки
					TryCreateOppositeOrder(Sides.Buy);
				}
			}
			else if (bestAsk.Value is not null && tradePrice >= bestAsk.Key)
			{
				// тик попал в аски, значит была крупная заявка по рынку на покупку,
				// которая возможна исполнила наши заявки

				ProcessMarketOrder(Sides.Buy);

				if (!hasDepth)
				{
					// подтягиваем противоположные котировки и снимаем лишние заявки
					TryCreateOppositeOrder(Sides.Sell);
				}
			}
			else if (bestBid.Value is not null && bestAsk.Value is not null && bestBid.Key < tradePrice && tradePrice < bestAsk.Key)
			{
				// тик попал в спред, значит в спреде до сделки была заявка.
				// создаем две лимитки с разных сторон, но одинаковой ценой.
				// если в эмуляторе есть наша заявка на этом уровне, то она исполниться.
				// если нет, то эмулятор взаимно исполнит эти заявки друг об друга

				// [upd] 2023/2/13 - не понятно как наша заявка может оказаться на этом уровне
				// если тик попал в середину спреда (и значит на уровне нет ни наших, ни сгенерированных заявок)

				var originSide = GetOrderSide();
				var spreadStep = _settings.SpreadSize * GetPriceStep();

				// try to fill depth gaps

				var newBestPrice = tradePrice + spreadStep;

				var depth = _settings.MaxDepth;
				while (--depth > 0)
				{
					if (bestAsk.Key > newBestPrice)
					{
						UpdateQuote(CreateMessage(localTime, serverTime, Sides.Sell, newBestPrice, GetRandomVolume()), true);
						newBestPrice += spreadStep * _priceRandom.Next(1, _settings.SpreadSize);
					}
					else
						break;
				}

				newBestPrice = tradePrice - spreadStep;

				depth = _settings.MaxDepth;
				while (--depth > 0)
				{
					if (newBestPrice > bestBid.Key)
					{
						UpdateQuote(CreateMessage(localTime, serverTime, Sides.Buy, newBestPrice, GetRandomVolume()), true);
						newBestPrice -= spreadStep * _priceRandom.Next(1, _settings.SpreadSize);
					}
					else
						break;
				}
			}
			else
			{
				// если у нас стакан был полу пустой, то тик формирует некий ценовой уровень в стакана,
				// так как прошедщая заявка должна была обо что-то удариться. допускаем, что после
				// прохождения сделки на этом ценовом уровне остался объем равный тиковой сделки

				var hasOpposite = true;

				Sides originSide;

				// определяем направление псевдо-ранее существовавшей заявки, из которой получился тик
				if (bestBid.Value != null)
					originSide = Sides.Sell;
				else if (bestAsk.Value != null)
					originSide = Sides.Buy;
				else
				{
					originSide = GetOrderSide();
					hasOpposite = false;
				}

				UpdateQuote(CreateMessage(localTime, serverTime, originSide, tradePrice, tradeVolume), true);

				// если стакан был полностью пустой, то формируем сразу уровень с противоположной стороны
				if (!hasOpposite)
				{
					var oppositePrice = tradePrice + _settings.SpreadSize * GetPriceStep() * (originSide == Sides.Buy ? 1 : -1);

					if (oppositePrice > 0)
						UpdateQuote(CreateMessage(localTime, serverTime, originSide.Invert(), oppositePrice, tradeVolume), true);
				}
			}

			if (!hasDepth)
			{
				CancelWorst(localTime, serverTime);
			}

			_prevTickPrice = tradePrice;

			if (_ticksSubscription is not null)
			{
				result.Add(tick.ToTickMessage(secId, localTime));
			}

			if (_depthSubscription is not null)
			{
				result.Add(CreateQuoteMessage(
					secId,
					localTime,
					serverTime));
			}
		}

		private void CancelWorst(DateTimeOffset localTime, DateTimeOffset serverTime)
		{
			void CancelWorstQuote(Sides side)
			{
				var quotes = GetQuotes(side);

				if (quotes.Count <= _settings.MaxDepth)
					return;

				var worst = quotes.Last();
				var volume = worst.Value.First.Where(e => e.PortfolioName == null).Sum(e => e.OrderVolume.Value);

				if (volume == 0)
					return;

				UpdateQuote(CreateMessage(localTime, serverTime, side, worst.Key, volume, true), false);
			}

			// если стакан слишком разросся, то удаляем его хвосты (не удаляя пользовательские заявки)
			CancelWorstQuote(Sides.Buy);
			CancelWorstQuote(Sides.Sell);
		}

		private decimal GetPriceStep() => _securityDefinition?.PriceStep ?? 0.01m;
		private bool HasDepth(DateTimeOffset time) => _lastDepthDate == time.Date;

		private void UpdateSteps(decimal price, decimal? volume)
		{
			if (!_priceStepUpdated)
			{
				_securityDefinition.PriceStep = price.GetDecimalInfo().EffectiveScale.GetPriceStep();
				_priceStepUpdated = true;
			}

			if (!_volumeStepUpdated)
			{
				if (volume > 0)
				{
					_securityDefinition.VolumeStep = volume.Value.GetDecimalInfo().EffectiveScale.GetPriceStep();
					_volumeDecimals = GetVolumeStep().GetCachedDecimals();
					_volumeStepUpdated = true;
				}
			}
		}

		private void UpdateSecurityDefinition(SecurityMessage securityDefinition)
		{
			_securityDefinition = securityDefinition ?? throw new ArgumentNullException(nameof(securityDefinition));

			if (_securityDefinition.PriceStep != null)
			{
				_priceStepUpdated = true;
				_priceStepExplicit = true;
			}

			if (_securityDefinition.VolumeStep != null)
			{
				_volumeStepUpdated = true;
				_volumeStepExplicit = true;
			}
		}

		private void UpdatePriceLimits(ExecutionMessage execution, ICollection<Message> result)
		{
			if (_lastStripDate == execution.LocalTime.Date)
				return;

			decimal price;

			if (execution.DataType == DataType.Ticks)
				price = execution.GetTradePrice();
			else if (execution.DataType == DataType.OrderLog)
			{
				if (execution.TradePrice == null)
					return;

				price = execution.TradePrice.Value;
			}
			else
				return;

			_lastStripDate = execution.LocalTime.Date;

			var priceOffset = _settings.PriceLimitOffset;
			var priceStep = GetPriceStep();

			var level1Msg =
				new Level1ChangeMessage
				{
					SecurityId = execution.SecurityId,
					LocalTime = execution.LocalTime,
					ServerTime = execution.ServerTime,
				}
				.Add(Level1Fields.MinPrice, ShrinkPrice((decimal)(price - priceOffset), priceStep))
				.Add(Level1Fields.MaxPrice, ShrinkPrice((decimal)(price + priceOffset), priceStep));

			_parent.UpdateLevel1Info(level1Msg, result, true);
		}

		private void UpdateSecurityDefinition(Level1ChangeMessage message)
		{
			if (_securityDefinition == null)
				return;

			foreach (var change in message.Changes)
			{
				switch (change.Key)
				{
					case Level1Fields.PriceStep:
						_securityDefinition.PriceStep = (decimal)change.Value;
						// при изменении шага надо пересчитать планки
						_lastStripDate = DateTime.MinValue;
						_priceStepUpdated = true;
						_priceStepExplicit = true;
						break;
					case Level1Fields.VolumeStep:
						_securityDefinition.VolumeStep = (decimal)change.Value;
						_volumeDecimals = GetVolumeStep().GetCachedDecimals();
						_volumeStepUpdated = true;
						_volumeStepExplicit = true;
						break;
					case Level1Fields.MinVolume:
						_securityDefinition.MinVolume = (decimal)change.Value;
						break;
					case Level1Fields.MaxVolume:
						_securityDefinition.MaxVolume = (decimal)change.Value;
						break;
					case Level1Fields.Multiplier:
						_securityDefinition.Multiplier = (decimal)change.Value;
						break;
				}
			}

			UpdateSecurityDefinition(_securityDefinition);
		}

		private decimal GetVolumeStep()
		{
			return _securityDefinition.VolumeStep ?? 1;
		}

		private static decimal ShrinkPrice(decimal price, decimal priceStep)
		{
			var decimals = priceStep.GetCachedDecimals();

			return price
				.Round(priceStep, decimals)
				.RemoveTrailingZeros();
		}

		private static ExecutionMessage CreateReply(ExecutionMessage original, DateTimeOffset time, Exception error)
		{
			var replyMsg = new ExecutionMessage
			{
				HasOrderInfo = true,
				DataTypeEx = DataType.Transactions,
				ServerTime = time,
				LocalTime = time,
				OriginalTransactionId = original.TransactionId,
				Error = error,
			};

			if (error != null)
				replyMsg.OrderState = OrderStates.Failed;

			return replyMsg;
		}

		private void AcceptExecution(DateTimeOffset time, ExecutionMessage execution, ICollection<Message> result)
		{
			if (_settings.Failing > 0)
			{
				if (RandomGen.GetDouble() < (_settings.Failing / 100.0))
				{
					LogError(LocalizedStrings.ErrorForOrder, execution.IsCancellation ? LocalizedStrings.Cancellation : LocalizedStrings.Registration, execution.OriginalTransactionId == 0 ? execution.TransactionId : execution.OriginalTransactionId);

					var replyMsg = CreateReply(execution, time, new InvalidOperationException(LocalizedStrings.Random));

					replyMsg.Balance = execution.OrderVolume;

					result.Add(replyMsg);
					return;
				}
			}

			if (execution.IsCancellation)
			{
				if (TryRemoveActiveOrder(execution.OriginalTransactionId, out var order))
				{
					// изменяем текущие котировки, добавляя туда наши цену и объем
					UpdateQuote(order, false);

					if (_depthSubscription is not null)
					{
						// отправляем измененный стакан
						result.Add(CreateQuoteMessage(
							order.SecurityId,
							time,
							GetServerTime(time)));
					}

					var replyMsg = CreateReply(order, time, null);

					//replyMsg.OriginalTransactionId = execution.OriginalTransactionId;
					replyMsg.OrderState = OrderStates.Done;
					replyMsg.Balance = order.Balance;
					replyMsg.OrderVolume = order.OrderVolume;

					result.Add(replyMsg);

					LogInfo(LocalizedStrings.OrderCancelled, execution.OriginalTransactionId);

					replyMsg.Commission = _parent
						.GetPortfolioInfo(execution.PortfolioName)
						.ProcessOrder(order, order.Balance.Value, result);
				}
				else
				{
					result.Add(CreateReply(execution, time, new InvalidOperationException(LocalizedStrings.OrderNotFound.Put(execution.OriginalTransactionId))));

					LogError(LocalizedStrings.OrderNotFound, execution.OriginalTransactionId);
				}
			}
			else
			{
				var error = _parent.CheckRegistration(execution, _securityDefinition, _priceStepExplicit, _volumeStepExplicit/*, result*/);

				var replyMsg = CreateReply(execution, time, error);
				result.Add(replyMsg);

				if (error is null)
				{
					if (execution.PostOnly == true)
					{
						var quotes = GetQuotes(execution.Side.Invert());

						var quote = quotes.FirstOrDefault();
						var sign = execution.Side == Sides.Buy ? 1 : -1;

						if (quote.Value != null && quote.Key * sign <= execution.OrderPrice * sign)
						{
							replyMsg.Balance = execution.OrderVolume;
							replyMsg.OrderVolume = execution.OrderVolume;
							replyMsg.OrderState = OrderStates.Done;
							return;
						}
					}

					LogInfo(LocalizedStrings.OrderRegistered, execution.TransactionId);

					// при восстановлении заявки у нее уже есть номер
					if (execution.OrderId == null)
					{
						execution.Balance = execution.OrderVolume;
						execution.OrderState = OrderStates.Active;
						execution.OrderId = _parent.OrderIdGenerator.GetNextId();
					}
					else
						execution.ServerTime = execution.ServerTime; // при восстановлении не меняем время

					replyMsg.Commission = _parent
						.GetPortfolioInfo(execution.PortfolioName)
						.ProcessOrder(execution, null, result);

					Verify();

					var matchByCandles = _candlesSubscription is not null;

					if (matchByCandles)
					{
						var candle = _candleInfo.LastOrDefault().Value?.FirstOrDefault().candle;

						if (candle is not null)
						{
							MatchOrderByCandle(execution.LocalTime, execution, candle, result);
						}
					}
					else
						MatchOrder(execution.LocalTime, execution, result, true);

					Verify();

					if (execution.OrderState == OrderStates.Active)
					{
						if (!AddActiveOrder(execution, time))
						{
							replyMsg.OrderState = OrderStates.Done;
							replyMsg.Balance = execution.Balance;
							replyMsg.OrderVolume = execution.OrderVolume;
							return;
						}

						replyMsg.OrderState = OrderStates.Active;

						if (!matchByCandles)
						{
							// изменяем текущие котировки, добавляя туда наши цену и объем
							UpdateQuote(execution, true);
						}
					}
					else if (execution.IsCanceled())
					{
						_parent
							.GetPortfolioInfo(execution.PortfolioName)
							.ProcessOrder(execution, execution.Balance.Value, result);
					}

					if (_depthSubscription is not null)
					{
						// отправляем измененный стакан
						result.Add(CreateQuoteMessage(
							execution.SecurityId,
							time,
							GetServerTime(time)));
					}
				}
				else
				{
					LogInfo(LocalizedStrings.ErrorRegOrder, execution.TransactionId, error.Message);
				}
			}
		}

		private QuoteChangeMessage CreateQuoteMessage(SecurityId securityId, DateTimeOffset timeStamp, DateTimeOffset time)
		{
			Verify();

			return new QuoteChangeMessage
			{
				SecurityId = securityId,
				LocalTime = timeStamp,
				ServerTime = time,
				Bids = BuildQuoteChanges(_bids),
				Asks = BuildQuoteChanges(_asks),
			};
		}

		private static QuoteChange[] BuildQuoteChanges(QuotesDict quotes)
		{
			return quotes.Count == 0
				? []
				: [.. quotes.Select(p => p.Value.Second)];
		}

		private void UpdateQuotes(ExecutionMessage message, ICollection<Message> result)
		{
			// матчинг заявок происходит не только для своих сделок, но и для чужих.
			// различие лишь в том, что для чужих заявок не транслируется информация о сделках.
			// матчинг чужих заявок на равне со своими дает наиболее реалистичный сценарий обновления стакана.

			if (message.TradeId != null)
				throw new ArgumentOutOfRangeException(nameof(message), message.TradeId, LocalizedStrings.TradeId);

			if (message.OrderVolume is null or <= 0)
				throw new ArgumentOutOfRangeException(nameof(message), message.OrderVolume, LocalizedStrings.OrderVolume);

			if (message.IsCancellation)
			{
				UpdateQuote(message, false);
				return;
			}

			// не ставим чужую заявку в стакан сразу, только её остаток после матчинга
			//UpdateQuote(message, true);

			if (_activeOrders.Count > 0)
			{
				foreach (var order in _activeOrders.Values.ToArray())
				{
					MatchOrder(message.LocalTime, order, result, false);

					if (order.OrderState != OrderStates.Done)
						continue;

					TryRemoveActiveOrder(order.TransactionId, out _);

					// изменяем текущие котировки, удаляя оттуда наши цену и объем
					UpdateQuote(order, false);
				}
			}

			//для чужих FOK заявок необходимо убрать ее из стакана после исполнения своих заявок
			// [upd] теперь не ставим чужую заявку сразу в стакан, поэтому и удалять не нужно
			//if (message.TimeInForce == TimeInForce.MatchOrCancel && !message.IsCancelled)
			//{
			//	UpdateQuote(new ExecutionMessage
			//	{
			//		DataTypeEx = DataType.Transactions,
			//		Side = message.Side,
			//		OrderPrice = message.OrderPrice,
			//		OrderVolume = message.OrderVolume,
			//		HasOrderInfo = true,
			//	}, false);
			//}

			// для чужих заявок заполняется только объем
			message.Balance = message.OrderVolume;

			// исполняем чужую заявку как свою. при этом результат выполнения не идет никуда
			MatchOrder(message.LocalTime, message, null, true);

			if (message.Balance > 0)
			{
				UpdateQuote(message, true, false);
			}
		}

		private QuotesDict GetQuotes(Sides side)
		{
			return side switch
			{
				Sides.Buy => _bids,
				Sides.Sell => _asks,
				_ => throw new ArgumentOutOfRangeException(nameof(side), side, LocalizedStrings.InvalidValue),
			};
		}

		private void MatchOrderByCandle(DateTimeOffset time, ExecutionMessage order, CandleMessage candle, ICollection<Message> result)
		{
			Verify();

			var balance = order.GetBalance();

			var leftBalance = candle.TotalVolume == 0 /* candle no volume info - assume candle's volume much more order's balance */
				? 0
				: (order.GetBalance() - candle.TotalVolume).Max(0);

			if (leftBalance > 0 && order.TimeInForce == TimeInForce.MatchOrCancel)
				return;

			decimal execPrice;

			if (order.OrderType == OrderTypes.Market)
			{
				execPrice = _settings.CandlePrice switch
				{
					EmulationCandlePrices.Middle => candle.GetMiddlePrice(GetPriceStep()),
					EmulationCandlePrices.Open => candle.OpenPrice,
					EmulationCandlePrices.High => candle.HighPrice,
					EmulationCandlePrices.Low => candle.LowPrice,
					EmulationCandlePrices.Close => candle.ClosePrice,
					_ => throw new InvalidOperationException(_settings.CandlePrice.To<string>()),
				};

				// price step is wrong, so adjust by candle boundaries
				if (execPrice > candle.HighPrice || execPrice < candle.LowPrice)
					execPrice = candle.ClosePrice;
			}
			else
			{
				if (order.OrderPrice > candle.HighPrice || order.OrderPrice < candle.LowPrice)
					return;

				execPrice = order.OrderPrice;
			}

			var executions = new List<(decimal price, decimal volume)>
			{
				(execPrice, balance - leftBalance)
			};

			MatchOrderPostProcess(time, order, executions, leftBalance, false, result);

			Verify();
		}

		private void MatchOrder(DateTimeOffset time, ExecutionMessage order, ICollection<Message> result, bool isNewOrder)
		{
			Verify();

			var isCrossTrade = false;

			var quotesSide = order.Side.Invert();
			var quotes = GetQuotes(quotesSide);

			var executions = result is null ? null : new List<(decimal price, decimal volume)>();

			List<QuoteChange> toRemove = null;

			var leftBalance = order.GetBalance();
			var sign = order.Side == Sides.Buy ? 1 : -1;
			var orderPrice = order.OrderPrice;
			var isMarket = order.OrderType == OrderTypes.Market;

			if (!isMarket && order.TimeInForce == TimeInForce.MatchOrCancel)
			{
				var leftBalance2 = leftBalance;

				foreach (var (price, pair) in quotes)
				{
					if (sign * price > sign * orderPrice)
						break;

					if (price == orderPrice && !_settings.MatchOnTouch)
						break;

					var qc = pair.Second;

					leftBalance2 -= qc.Volume;

					if (leftBalance2 <= 0)
						break;
				}

				if (leftBalance2 > 0)
				{
					if (result != null)
					{
						order.OrderState = OrderStates.Done;
						ProcessOrder(time, order, result);
					}

					return;
				}
			}

			foreach (var (price, pair) in quotes)
			{
				var levelOrders = pair.First;
				var qc = pair.Second;

				// для старых заявок, когда стакан пробивает уровень заявки,
				// матчим по цене ранее выставленной заявки.
				var execPrice = isNewOrder ? price : orderPrice;

				if (!isMarket)
				{
					if (sign * price > sign * orderPrice)
						break;

					if (price == orderPrice && !_settings.MatchOnTouch)
					{
						if (levelOrders.Count > 0)
							throw new NotSupportedException("MatchOnTouch doesn't support cross trades.");

						toRemove ??= [];
						toRemove.Add(qc);

						break;
					}
				}

				if (result != null && levelOrders.Count > 0)
				{
					var crossOrders = levelOrders.Where(o => o.PortfolioName == order.PortfolioName).ToArray();

					if (crossOrders.Length > 0)
					{
						// матчинг идет о заявки с таким же портфелем
						var matchError = LocalizedStrings.CrossTrades.Put(crossOrders.Select(o => o.TransactionId.To<string>()).JoinCommaSpace(), order.TransactionId);
						LogError(matchError);

						break;
					}
				}

				var levelOrdersExecVol = 0m;

				// объем заявки больше или равен всему уровню в стакане, то сразу удаляем его целиком
				if (leftBalance >= qc.Volume)
				{
					if (levelOrders.Count > 0)
					{
						foreach (var o in levelOrders)
						{
							var tradeVol = ExecuteOrder(time, o, price, result);

							_messagePool.Free(o);

							executions?.Add((price, tradeVol));

							leftBalance -= tradeVol;
							levelOrdersExecVol += tradeVol;
						}
					}

					toRemove ??= [];
					toRemove.Add(qc);

					var diff = qc.Volume - levelOrdersExecVol;

					if (diff > 0)
						executions?.Add((price, diff));
					else if (diff < 0)
						throw new InvalidOperationException("diff < 0");

					leftBalance -= qc.Volume;
				}
				else
				{
					var requiredVol = leftBalance;

					qc.Volume -= leftBalance;
					pair.Second = qc;

					AddTotalVolume(quotesSide, -leftBalance);

					if (levelOrders.Count > 0)
					{
						foreach (var o in levelOrders.ToArray())
						{
							var balance = o.Balance.Value;

							if (leftBalance >= balance)
							{
								var tradeVol = ExecuteOrder(time, o, price, result);

								levelOrders.Remove(o);
								_messagePool.Free(o);

								executions?.Add((price, tradeVol));

								leftBalance -= tradeVol;
								levelOrdersExecVol += tradeVol;
							}
							else
							{
								o.Balance = balance - leftBalance;

								ProcessOrder(time, o, result);
								ProcessTrade(time, o, price, leftBalance, result);

								levelOrders.TotalBalance -= leftBalance;

								executions?.Add((price, leftBalance));

								levelOrdersExecVol += leftBalance;
								leftBalance = 0;
							}

							if (leftBalance == 0)
								break;
						}
					}

					var diff = requiredVol - levelOrdersExecVol;

					if (diff > 0)
						executions?.Add((price, diff));
					else if (diff < 0)
						throw new InvalidOperationException("diff < 0");

					leftBalance = 0;
				}

				Verify();

				if (leftBalance == 0)
					break;
			}

			if (toRemove != null)
			{
				Verify();

				foreach (var qc in toRemove)
				{
					quotes.Remove(qc.Price);

					AddTotalVolume(quotesSide, -qc.Volume);
				}

				Verify();
			}

			// если это не пользовательская заявка
			if (result == null)
				order.Balance = leftBalance;
			else
				MatchOrderPostProcess(time, order, executions, leftBalance, isCrossTrade, result);
		}

		private void MatchOrderPostProcess(DateTimeOffset time, ExecutionMessage order, List<(decimal price, decimal volume)> executions, decimal leftBalance, bool isCrossTrade, ICollection<Message> result)
		{
			switch (order.TimeInForce)
			{
				case null:
				case TimeInForce.PutInQueue:
				{
					order.Balance = leftBalance;

					if (executions.Count > 0)
					{
						if (leftBalance == 0)
						{
							order.OrderState = OrderStates.Done;
							LogInfo(LocalizedStrings.OrderMatched, order.TransactionId);
						}

						ProcessOrder(time, order, result);
					}

					if (order.OrderType == OrderTypes.Market)
					{
						if (leftBalance > 0)
						{
							LogInfo(LocalizedStrings.OrderCancellingNotAllBalance, order.TransactionId, leftBalance);

							order.OrderState = OrderStates.Done;
							ProcessOrder(time, order, result);
						}
					}

					break;
				}

				case TimeInForce.MatchOrCancel:
				{
					if (leftBalance == 0)
						order.Balance = 0;

					LogInfo(LocalizedStrings.OrderFOKMatched, order.TransactionId);

					order.OrderState = OrderStates.Done;
					ProcessOrder(time, order, result);

					// заявка не исполнилась полностью, поэтому она вся отменяется, не влияя на стакан
					if (leftBalance > 0)
						return;

					break;
				}

				case TimeInForce.CancelBalance:
				{
					LogInfo(LocalizedStrings.OrderIOCMatched, order.TransactionId);

					order.Balance = leftBalance;
					order.OrderState = OrderStates.Done;
					ProcessOrder(time, order, result);
					break;
				}
			}

			if (isCrossTrade)
			{
				var reply = CreateReply(order, time, null);

				//reply.OrderState = OrderStates.Failed;
				//reply.OrderStatus = (long?)OrderStatus.RejectedBySystem;
				//reply.Error = new InvalidOperationException(matchError);

				reply.OrderState = OrderStates.Done;
				//reply.OrderStatus = (long?)OrderStatus.CanceledByManager;

				result.Add(reply);
			}

			foreach (var (price, volume) in executions)
				ProcessTrade(time, order, price, volume, result);
		}

		private void ProcessTime(Message message, ICollection<Message> result)
		{
			ProcessExpirableOrders(message, result);
			ProcessPendingExecutions(message, result);
			ProcessCandles(message, result);
		}

		private void ProcessCandles(Message message, ICollection<Message> result)
		{
			if (_candleInfo.Count == 0)
				return;

			var matchByCandle = _candlesSubscription is not null;

			List<DateTimeOffset> toRemove = null;

			foreach (var pair in _candleInfo)
			{
				if (pair.Key >= message.LocalTime)
					break;

				toRemove ??= [];
				toRemove.Add(pair.Key);

				if (_ticksSubscription is not null)
				{
					foreach (var (candle, ticks) in pair.Value)
					{
						if (ticks is null)
							continue;

						for (var i = 1; i < ticks.Length; i++)
						{
							var trade = ticks[i];

							if (trade == default)
								break;

							result.Add(trade.ToTickMessage(candle.SecurityId, candle.LocalTime));
						}
					}
				}

				if (_candlesNonFinished)
				{
					foreach (var (candle, ticks) in pair.Value)
					{
						var openState = candle.TypedClone();
						openState.State = CandleStates.Active;
						openState.HighPrice = openState.LowPrice = openState.ClosePrice = openState.OpenPrice;

						if (candle.OpenTime != default)
							openState.LocalTime = candle.OpenTime;

						result.Add(openState);

						var highState = openState.TypedClone();
						highState.HighPrice = candle.HighPrice;

						if (candle.HighTime != default)
							highState.LocalTime = candle.HighTime;

						result.Add(highState);

						var lowState = openState.TypedClone();
						lowState.HighPrice = candle.HighPrice;

						if (candle.LowTime != default)
							lowState.LocalTime = candle.LowTime;

						result.Add(lowState);
					}
				}

				// change current time before the candle will be processed
				result.Add(new TimeMessage { LocalTime = message.LocalTime });

				foreach (var (candle, ticks) in pair.Value)
				{
					candle.LocalTime = message.LocalTime;
					result.Add(candle);

					if (ticks is not null)
						_tickPool.Free(ticks);

					if (matchByCandle && _activeOrders.Count > 0)
					{
						foreach (var order in _activeOrders.Values.ToArray())
						{
							if (order.OrderPrice <= candle.HighPrice && order.OrderPrice >= candle.LowPrice)
							{
								MatchOrderByCandle(message.LocalTime, order, candle, result);

								if (order.OrderState == OrderStates.Done)
									TryRemoveActiveOrder(order.TransactionId, out _);
							}
						}
					}
				}
			}

			if (toRemove is not null)
			{
				foreach (var key in toRemove)
				{
					_candleInfo.Remove(key);
				}
			}
		}

		private void ProcessExpirableOrders(Message message, ICollection<Message> result)
		{
			if (_expirableOrders.Count == 0)
				return;

			var diff = message.LocalTime - _prevTime;

			foreach (var (orderMsg, l) in _expirableOrders.ToArray())
			{
				var left = l;
				left -= diff;

				if (left <= TimeSpan.Zero)
				{
					TryRemoveActiveOrder(orderMsg.TransactionId, out _);

					orderMsg.OrderState = OrderStates.Done;
					ProcessOrder(message.LocalTime, orderMsg, result);

					// изменяем текущие котировки, удаляя оттуда наши цену и объем
					UpdateQuote(orderMsg, false);

					if (_depthSubscription is not null)
					{
						// отправляем измененный стакан
						result.Add(CreateQuoteMessage(
							orderMsg.SecurityId,
							message.LocalTime,
							GetServerTime(message.LocalTime)));
					}
				}
				else
					_expirableOrders[orderMsg] = left;
			}
		}

		private void UpdateQuote(ExecutionMessage message, bool register, bool byVolume = true)
		{
			Verify();

			var quotes = GetQuotes(message.Side);

			if (!quotes.TryGetValue(message.OrderPrice, out var pair))
			{
				if (!register)
					return;

				quotes[message.OrderPrice] = pair = RefTuple.Create(new LevelOrders(), new QuoteChange(message.OrderPrice, 0));
			}

			var level = pair.First;
			var quote = pair.Second;

			var volume = byVolume ? message.SafeGetVolume() : message.GetBalance();

			if (register)
			{
				AddTotalVolume(message.Side, volume);

				quote.Volume += volume;
				pair.Second = quote;

				// сохраняем только реальные заявки
				if (message.TransactionId != default)
				{
					//если пришло увеличение объема на уровне, то всегда добавляем в конец очереди, даже для диффа стаканов
					//var clone = message.TypedClone();
					var clone = _messagePool.Allocate();

					clone.TransactionId = message.TransactionId;
					clone.OrderPrice = message.OrderPrice;
					clone.PortfolioName = message.PortfolioName;
					clone.Balance = volume;
					clone.OrderVolume = message.OrderVolume;

					level.Add(clone);
				}
			}
			else
			{
				if (message.TransactionId == 0)
				{
					if (level.Count == 0)
					{
						if (volume >= quote.Volume)
						{
							quotes.Remove(message.OrderPrice);
							AddTotalVolume(message.Side, -quote.Volume);
						}
						else
						{
							quote.Volume -= volume;
							pair.Second = quote;
							AddTotalVolume(message.Side, -volume);
						}
					}
					else
					{
						var diff = volume - level.TotalBalance;

						if (diff > 0)
						{
							quote.Volume -= diff;
							pair.Second = quote;

							AddTotalVolume(message.Side, -diff);
						}
					}
				}
				else
				{
					if (level.TryGetAndRemoveByTransactionId(message.TransactionId, out var order))
					{
						var balance = order.GetBalance();

						AddTotalVolume(message.Side, -balance);

						quote.Volume -= balance;
						pair.Second = quote;

						_messagePool.Free(order);

						if (quote.Volume <= 0)
							quotes.Remove(quote.Price);
					}
				}					
			}

			Verify();
		}

		private void AddTotalVolume(Sides side, decimal diff)
		{
			if (side == Sides.Buy)
				_totalBidVolume += diff;
			else
				_totalAskVolume += diff;
		}

		private decimal GetTotalVolume(Sides side)
		{
			return side == Sides.Buy ? _totalBidVolume : _totalAskVolume;
		}

		private void Verify()
		{
			if (!_parent.VerifyMode)
				return;

			static void Verify(QuotesDict quotes, decimal totalVolume)
			{
				if (totalVolume < 0)
					throw new InvalidOperationException();

				if (quotes.Values.Sum(p => p.Second.Volume) != totalVolume)
					throw new InvalidOperationException();

				if (quotes.Values.Any(p => p.First.Sum(o => o.Balance.Value) != p.First.TotalBalance))
					throw new InvalidOperationException();
			}

			Verify(_bids, _totalBidVolume);
			Verify(_asks, _totalAskVolume);

			if (_bids.Count == 0 || _asks.Count == 0)
				return;

			if (_bids.First().Key >= _asks.First().Key)
				throw new InvalidOperationException();
		}

		private void ProcessPendingExecutions(Message message, ICollection<Message> result)
		{
			if (_pendingExecutions.Count == 0)
				return;

			var diff = message.LocalTime - _prevTime;

			foreach (var pair in _pendingExecutions.ToArray())
			{
				var orderMsg = pair.Key;
				var left = pair.Value;
				left -= diff;

				if (left <= TimeSpan.Zero)
				{
					_pendingExecutions.Remove(orderMsg);
					AcceptExecution(message.LocalTime, orderMsg, result);
				}
				else
					_pendingExecutions[orderMsg] = left;
			}
		}

		private decimal ExecuteOrder(DateTimeOffset time, ExecutionMessage order, decimal price, ICollection<Message> result)
		{
			var balance = order.Balance.Value;

			order.Balance = 0;
			order.OrderState = OrderStates.Done;

			if (result is not null)
			{
				ProcessOrder(time, order, result);
				ProcessTrade(time, order, price, balance, result);
			}

			return balance;
		}

		private void ProcessOrder(DateTimeOffset time, ExecutionMessage message, ICollection<Message> result)
		{
			result.Add(new ExecutionMessage
			{
				LocalTime = time,
				SecurityId = message.SecurityId,
				OrderId = message.OrderId,
				OriginalTransactionId = message.TransactionId,
				Balance = message.Balance,
				OrderVolume = message.OrderVolume,
				OrderState = message.OrderState,
				PortfolioName = message.PortfolioName,
				DataTypeEx = DataType.Transactions,
				HasOrderInfo = true,
				ServerTime = GetServerTime(time),
			});
		}

		private void ProcessTrade(DateTimeOffset time, ExecutionMessage order, decimal price, decimal volume, ICollection<Message> result)
		{
			if (volume <= 0)
				throw new ArgumentOutOfRangeException(nameof(volume), volume, LocalizedStrings.InvalidValue);

			var tradeMsg = new ExecutionMessage
			{
				LocalTime = time,
				SecurityId = securityId,
				OrderId = order.OrderId,
				OriginalTransactionId = order.TransactionId,
				TradeId = _parent.TradeIdGenerator.GetNextId(),
				TradePrice = price,
				TradeVolume = volume,
				DataTypeEx = DataType.Transactions,
				ServerTime = GetServerTime(time),
				Side = order.Side,
			};
			result.Add(tradeMsg);

			LogInfo("Trade {0} of order {1} P={2} V={3}.", tradeMsg.TradeId, tradeMsg.OriginalTransactionId, price, volume);
			var info = _parent.GetPortfolioInfo(order.PortfolioName);

			info.ProcessTrade(order.Side, tradeMsg, result);

			if (_ticksSubscription is not null)
			{
				result.Add(new ExecutionMessage
				{
					LocalTime = time,
					SecurityId = securityId,
					TradeId = tradeMsg.TradeId,
					TradePrice = tradeMsg.TradePrice,
					TradeVolume = tradeMsg.TradeVolume,
					DataTypeEx = DataType.Ticks,
					ServerTime = GetServerTime(time),
				});
			}
		}

		private DateTimeOffset GetServerTime(DateTimeOffset time)
		{
			if (!_settings.ConvertTime)
				return time;

			var destTimeZone = _settings.TimeZone;

			if (destTimeZone == null)
			{
				var board = _parent._boardDefinitions.TryGetValue(securityId.BoardCode);

				if (board != null)
					destTimeZone = board.TimeZone;
			}

			if (destTimeZone == null)
				return time;

			//var sourceZone = time.Kind == DateTimeKind.Utc ? TimeZoneInfo.Utc : TimeZoneInfo.Local;

			return TimeZoneInfo.ConvertTime(time, destTimeZone);//.ApplyTimeZone(destTimeZone);
		}

		public decimal GetMarginPrice(Sides side)
		{
			var field = side == Sides.Buy ? Level1Fields.MarginBuy : Level1Fields.MarginSell;
			return (decimal?)_parent._secStates.TryGetValue(securityId)?.TryGetValue(field) ?? GetQuotes(side).FirstOr()?.Key ?? 0;
		}
	}

	private class PortfolioEmulator(MarketEmulator parent, string name)
	{
		private class PositionInfo(SecurityMarketEmulator secEmu)
		{
			public decimal BeginValue;
			public decimal Diff;
			public decimal CurrentValue => BeginValue + Diff;

			public decimal AveragePrice;

			public decimal Price
			{
				get
				{
					var pos = CurrentValue;

					if (pos == 0)
						return 0;

					return pos.Abs() * AveragePrice;
				}
			}

			public decimal TotalPrice => GetPrice(0, 0);

			public decimal GetPrice(decimal buyVol, decimal sellVol)
			{
				var price = Price;

				var buyOrderPrice = (TotalBidsVolume + buyVol) * secEmu.GetMarginPrice(Sides.Buy);
				var sellOrderPrice = (TotalAsksVolume + sellVol) * secEmu.GetMarginPrice(Sides.Sell);

				if (price != 0)
				{
					if (CurrentValue > 0)
					{
						price += buyOrderPrice;
						price = price.Max(sellOrderPrice);
					}
					else
					{
						price += sellOrderPrice;
						price = price.Max(buyOrderPrice);
					}
				}
				else
				{
					price = buyOrderPrice + sellOrderPrice;
				}

				return price;
			}

			public decimal TotalBidsVolume;
			public decimal TotalAsksVolume;
		}

		private readonly Dictionary<SecurityId, PositionInfo> _positions = [];

		private decimal _beginMoney;
		private decimal _currentMoney;

		private decimal _totalBlockedMoney;

		public PortfolioPnLManager PnLManager { get; } = new PortfolioPnLManager(name, secId => null);

		public void RequestState(PortfolioLookupMessage pfMsg, ICollection<Message> result)
		{
			var time = pfMsg.LocalTime;

			AddPortfolioChangeMessage(time, result);
		}

		public void ProcessPositionChange(PositionChangeMessage posMsg, ICollection<Message> result)
		{
			var beginValue = (decimal?)posMsg.Changes.TryGetValue(PositionChangeTypes.BeginValue);

			if (posMsg.IsMoney())
			{
				if (beginValue == null)
					return;

				_currentMoney = _beginMoney = (decimal)beginValue;

				AddPortfolioChangeMessage(posMsg.ServerTime, result);
				return;
			}

			var pos = GetPosition(posMsg.SecurityId);

			var prevPrice = pos.Price;

			pos.BeginValue = beginValue ?? 0L;
			pos.AveragePrice = (decimal?)posMsg.Changes.TryGetValue(PositionChangeTypes.AveragePrice) ?? pos.AveragePrice;

			result.Add(posMsg.Clone());

			_totalBlockedMoney = _totalBlockedMoney - prevPrice + pos.Price;

			result.Add(
				new PositionChangeMessage
				{
					SecurityId = SecurityId.Money,
					ServerTime = posMsg.ServerTime,
					LocalTime = posMsg.LocalTime,
					PortfolioName = name,
				}.Add(PositionChangeTypes.BlockedValue, _totalBlockedMoney)
			);
		}

		private PositionInfo GetPosition(SecurityId securityId)
			=> _positions.SafeAdd(securityId, k => new(parent.GetEmulator(securityId)));

		public decimal? ProcessOrder(ExecutionMessage orderMsg, decimal? cancelBalance, ICollection<Message> result)
		{
			var pos = GetPosition(orderMsg.SecurityId/*, orderMsg.LocalTime, result*/);

			var prevPrice = pos.TotalPrice;

			if (cancelBalance == null)
			{
				var balance = orderMsg.SafeGetVolume();

				if (orderMsg.Side == Sides.Buy)
					pos.TotalBidsVolume += balance;
				else
					pos.TotalAsksVolume += balance;
			}
			else
			{
				if (orderMsg.Side == Sides.Buy)
					pos.TotalBidsVolume -= cancelBalance.Value;
				else
					pos.TotalAsksVolume -= cancelBalance.Value;
			}

			_totalBlockedMoney = _totalBlockedMoney - prevPrice + pos.TotalPrice;

			var commission = parent._commissionManager.Process(orderMsg);

			AddPortfolioChangeMessage(orderMsg.ServerTime, result);

			return commission;
		}

		public void ProcessTrade(Sides side, ExecutionMessage tradeMsg, ICollection<Message> result)
		{
			var time = tradeMsg.ServerTime;

			PnLManager.ProcessMyTrade(tradeMsg, out _);
			tradeMsg.Commission = parent._commissionManager.Process(tradeMsg);

			var position = tradeMsg.TradeVolume;

			if (position == null)
				return;

			if (side == Sides.Sell)
				position *= -1;

			var pos = GetPosition(tradeMsg.SecurityId);

			var prevPrice = pos.TotalPrice;

			var tradeVol = tradeMsg.TradeVolume.Value;

			if (tradeMsg.Side == Sides.Buy)
				pos.TotalBidsVolume -= tradeVol;
			else
				pos.TotalAsksVolume -= tradeVol;

			var prevPos = pos.CurrentValue;

			pos.Diff += position.Value;

			var tradePrice = tradeMsg.TradePrice.Value;
			var currPos = pos.CurrentValue;

			if (prevPos.Sign() == currPos.Sign())
				pos.AveragePrice = (pos.AveragePrice * prevPos + position.Value * tradePrice) / currPos;
			else
				pos.AveragePrice = currPos == 0 ? 0 : tradePrice;

			_totalBlockedMoney = _totalBlockedMoney - prevPrice + pos.TotalPrice;

			result.Add(
				new PositionChangeMessage
				{
					LocalTime = time,
					ServerTime = time,
					PortfolioName = name,
					SecurityId = SecurityId.Money,
				}
				.Add(PositionChangeTypes.CurrentValue, pos.CurrentValue)
				.TryAdd(PositionChangeTypes.AveragePrice, pos.AveragePrice)
			);

			AddPortfolioChangeMessage(time, result);
		}

		public void ProcessMarginChange(DateTimeOffset time, SecurityId securityId, ICollection<Message> result)
		{
			var money = _positions.TryGetValue(securityId);

			if (money == null)
				return;

			_totalBlockedMoney = 0;

			foreach (var pair in _positions)
				_totalBlockedMoney += pair.Value.TotalPrice;

			result.Add(
				new PositionChangeMessage
				{
					SecurityId = SecurityId.Money,
					ServerTime = time,
					LocalTime = time,
					PortfolioName = name,
				}.Add(PositionChangeTypes.BlockedValue, _totalBlockedMoney)
			);
		}

		public void AddPortfolioChangeMessage(DateTimeOffset time, ICollection<Message> result)
		{
			var realizedPnL = PnLManager.RealizedPnL;
			var unrealizedPnL = PnLManager.UnrealizedPnL;
			var commission = parent._commissionManager.Commission;
			var totalPnL = PnLManager.GetPnL() - commission;

			try
			{
				_currentMoney = _beginMoney + totalPnL;
			}
			catch (OverflowException ex)
			{
				result.Add(ex.ToErrorMessage());
			}

			result.Add(new PositionChangeMessage
			{
				SecurityId = SecurityId.Money,
				ServerTime = time,
				LocalTime = time,
				PortfolioName = name,
			}
			.Add(PositionChangeTypes.RealizedPnL, realizedPnL)
			.TryAdd(PositionChangeTypes.UnrealizedPnL, unrealizedPnL, true)
			.Add(PositionChangeTypes.VariationMargin, totalPnL)
			.Add(PositionChangeTypes.CurrentValue, _currentMoney)
			.Add(PositionChangeTypes.BlockedValue, _totalBlockedMoney)
			.Add(PositionChangeTypes.Commission, commission));
		}

		public InvalidOperationException CheckRegistration(ExecutionMessage execMsg)
		{
			var settings = parent.Settings;

			if (settings.CheckMoney)
			{
				// если задан баланс, то проверям по нему (для частично исполненных заявок)
				var volume = execMsg.Balance ?? execMsg.SafeGetVolume();

				var pos = GetPosition(execMsg.SecurityId);

				var needBlock = pos.GetPrice(execMsg.Side == Sides.Buy ? volume : 0, execMsg.Side == Sides.Sell ? volume : 0);

				if (_currentMoney < needBlock)
				{
					return new InsufficientFundException(LocalizedStrings.InsufficientBalance
						.Put(execMsg.PortfolioName, execMsg.TransactionId, needBlock, _currentMoney, pos.TotalPrice));
				}
			}
			else if (settings.CheckShortable && execMsg.Side == Sides.Sell &&
					 parent._securityEmulators.TryGetValue(execMsg.SecurityId, out var secEmu) &&
					 secEmu.SecurityDefinition?.Shortable == false)
			{
				var pos = GetPosition(execMsg.SecurityId);

				var potentialPosition = pos.CurrentValue - execMsg.OrderVolume;

				if (potentialPosition < 0)
				{
					return new(LocalizedStrings.CannotShortPosition
						.Put(execMsg.PortfolioName, execMsg.TransactionId, pos.CurrentValue, execMsg.OrderVolume));
				}
			}

			return null;
		}
	}

	private readonly Dictionary<SecurityId, SecurityMarketEmulator> _securityEmulators = [];
	private readonly Dictionary<string, List<SecurityMarketEmulator>> _securityEmulatorsByBoard = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly Dictionary<string, PortfolioEmulator> _portfolios = [];
	private readonly Dictionary<string, BoardMessage> _boardDefinitions = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly Dictionary<SecurityId, Dictionary<Level1Fields, object>> _secStates = [];
	private DateTimeOffset _portfoliosPrevRecalc;
	private readonly ICommissionManager _commissionManager = new CommissionManager();
	private readonly Dictionary<string, SessionStates> _boardStates = new(StringComparer.InvariantCultureIgnoreCase);

	/// <summary>
	/// Initializes a new instance of the <see cref="MarketEmulator"/>.
	/// </summary>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	public MarketEmulator(ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IExchangeInfoProvider exchangeInfoProvider, IdGenerator transactionIdGenerator)
	{
		SecurityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
		PortfolioProvider = portfolioProvider ?? throw new ArgumentNullException(nameof(portfolioProvider));
		ExchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));
		TransactionIdGenerator = transactionIdGenerator ?? throw new ArgumentNullException(nameof(transactionIdGenerator));

		((IMessageAdapter)this).SupportedInMessages = [.. ((IMessageAdapter)this).PossibleSupportedMessages.Select(i => i.Type)];
	}

	/// <inheritdoc />
	public ISecurityProvider SecurityProvider { get; }

	/// <inheritdoc />
	public IPortfolioProvider PortfolioProvider { get; }

	/// <inheritdoc />
	public IExchangeInfoProvider ExchangeInfoProvider { get; }

	/// <summary>
	/// Transaction id generator.
	/// </summary>
	public IdGenerator TransactionIdGenerator { get; }

	/// <inheritdoc />
	public MarketEmulatorSettings Settings { get; } = new MarketEmulatorSettings();

	/// <summary>
	/// The number of processed messages.
	/// </summary>
	public long ProcessedMessageCount { get; private set; }

	/// <summary>
	/// The generator of identifiers for orders.
	/// </summary>
	public IncrementalIdGenerator OrderIdGenerator { get; set; } = new IncrementalIdGenerator();

	/// <summary>
	/// The generator of identifiers for trades.
	/// </summary>
	public IncrementalIdGenerator TradeIdGenerator { get; set; } = new IncrementalIdGenerator();

	private DateTimeOffset _currentTime;

	/// <inheritdoc />
	public override DateTimeOffset CurrentTime => _currentTime;

	/// <inheritdoc />
	public bool SendInMessage(Message message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var retVal = new List<Message>();

		switch (message.Type)
		{
			case MessageTypes.Time:
			{
				foreach (var securityEmulator in _securityEmulators.Values)
					securityEmulator.Process(message, retVal);

				// время у TimeMsg может быть больше времени сообщений из эмулятора
				//retVal.Add(message);

				break;
			}

			case MessageTypes.Execution:
			{
				var execMsg = (ExecutionMessage)message;
				GetEmulator(execMsg.SecurityId).Process(message, retVal);
				break;
			}

			case MessageTypes.QuoteChange:
			{
				var quoteMsg = (QuoteChangeMessage)message;
				GetEmulator(quoteMsg.SecurityId).Process(message, retVal);
				break;
			}

			case MessageTypes.OrderRegister:
			case MessageTypes.OrderReplace:
			case MessageTypes.OrderCancel:
			{
				var orderMsg = (OrderMessage)message;
				var secId = orderMsg.SecurityId;

				var canRegister = true;

				if (Settings.CheckTradingState)
				{
					var state = _boardStates.TryGetValue2(nameof(MarketEmulator)) ?? _boardStates.TryGetValue2(secId.BoardCode);

					switch (state)
					{
						case SessionStates.Paused:
						case SessionStates.ForceStopped:
						case SessionStates.Ended:
						{
							retVal.Add(new ExecutionMessage
							{
								DataTypeEx = DataType.Transactions,
								HasOrderInfo = true,
								ServerTime = orderMsg.LocalTime,
								LocalTime = orderMsg.LocalTime,
								OriginalTransactionId = orderMsg.TransactionId,
								OrderState = OrderStates.Failed,
								Error = new InvalidOperationException(LocalizedStrings.SessionStopped.Put(secId.BoardCode, state.Value)),
							});

							canRegister = false;
							break;
						}
					}
				}

				if (canRegister)
					GetEmulator(secId).Process(message, retVal);

				break;
			}

			case MessageTypes.Reset:
			{
				_securityEmulators.Clear();
				_securityEmulatorsByBoard.Clear();

				OrderIdGenerator.Current = Settings.InitialOrderId;
				TradeIdGenerator.Current = Settings.InitialTradeId;

				_portfolios.Clear();
				_boardDefinitions.Clear();
				_boardStates.Clear();

				_secStates.Clear();

				_portfoliosPrevRecalc = default;

				ProcessedMessageCount = 0;

				retVal.Add(new ResetMessage());
				break;
			}

			case MessageTypes.Connect:
			{
				_portfolios.SafeAdd(Extensions.SimulatorPortfolioName, key => new PortfolioEmulator(this, key));
				retVal.Add(new ConnectMessage());
				break;
			}

			//case ExtendedMessageTypes.Clearing:
			//{
			//	var clearingMsg = (ClearingMessage)message;
			//	var emu = _securityEmulators.TryGetValue(clearingMsg.SecurityId);

			//	if (emu != null)
			//	{
			//		_securityEmulators.Remove(clearingMsg.SecurityId);

			//		var emulators = _securityEmulatorsByBoard.TryGetValue(clearingMsg.SecurityId.BoardCode);

			//		if (emulators != null)
			//		{
			//			if (emulators.Remove(emu) && emulators.Count == 0)
			//				_securityEmulatorsByBoard.Remove(clearingMsg.SecurityId.BoardCode);
			//		}
			//	}

			//	break;
			//}

			//case MessageTypes.PortfolioChange:
			//{
			//	var pfChangeMsg = (PortfolioChangeMessage)message;
			//	GetPortfolioInfo(pfChangeMsg.PortfolioName).ProcessPortfolioChange(pfChangeMsg, retVal);
			//	break;
			//}

			case MessageTypes.PositionChange:
			{
				var posChangeMsg = (PositionChangeMessage)message;
				GetPortfolioInfo(posChangeMsg.PortfolioName).ProcessPositionChange(posChangeMsg, retVal);
				break;
			}

			case MessageTypes.Board:
			{
				var boardMsg = (BoardMessage)message;

				_boardDefinitions[boardMsg.Code] = boardMsg.TypedClone();

				var emulators = _securityEmulatorsByBoard.TryGetValue(boardMsg.Code);

				if (emulators != null)
				{
					foreach (var securityEmulator in emulators)
						securityEmulator.Process(boardMsg, retVal);
				}

				break;
			}

			case MessageTypes.Level1Change:
			{
				var level1Msg = (Level1ChangeMessage)message;
				GetEmulator(level1Msg.SecurityId).Process(level1Msg, retVal);
				UpdateLevel1Info(level1Msg, retVal, false);
				break;
			}

			case MessageTypes.Portfolio:
			{
				var pfMsg = (PortfolioMessage)message;

				retVal.Add(pfMsg);
				//GetPortfolioInfo(pfMsg.PortfolioName);

				break;
			}

			case MessageTypes.OrderStatus:
			{
				var statusMsg = (OrderStatusMessage)message;

				retVal.Add(statusMsg.CreateResponse());

				if (!statusMsg.IsSubscribe)
					break;

				foreach (var pair in _securityEmulators)
				{
					pair.Value.Process(message, retVal);
				}

				retVal.Add(statusMsg.CreateResult());

				break;
			}

			case MessageTypes.PortfolioLookup:
			{
				var lookupMsg = (PortfolioLookupMessage)message;

				retVal.Add(lookupMsg.CreateResponse());

				if (!lookupMsg.IsSubscribe)
					break;

				if (lookupMsg.PortfolioName.IsEmpty())
				{
					foreach (var pair in _portfolios)
					{
						retVal.Add(new PortfolioMessage
						{
							PortfolioName = pair.Key,
							OriginalTransactionId = lookupMsg.TransactionId
						});

						pair.Value.RequestState(lookupMsg, retVal);
					}
				}
				else
				{
					retVal.Add(new PortfolioMessage
					{
						PortfolioName = lookupMsg.PortfolioName,
						OriginalTransactionId = lookupMsg.TransactionId
					});

					if (_portfolios.TryGetValue(lookupMsg.PortfolioName, out var pfEmu))
					{
						pfEmu.RequestState(lookupMsg, retVal);
					}
				}

				retVal.Add(lookupMsg.CreateResult());

				break;
			}

			case MessageTypes.Security:
			{
				var secMsg = (SecurityMessage)message;

				//retVal.Add(secMsg);
				GetEmulator(secMsg.SecurityId).Process(secMsg, retVal);

				break;
			}

			case ExtendedMessageTypes.CommissionRule:
			{
				var ruleMsg = (CommissionRuleMessage)message;
				_commissionManager.Rules.Add(ruleMsg.Rule);
				break;
			}

			case MessageTypes.BoardState:
			{
				if (Settings.CheckTradingState)
				{
					var boardStateMsg = (BoardStateMessage)message;

					var board = boardStateMsg.BoardCode;

					if (board.IsEmpty())
						board = nameof(MarketEmulator);

					_boardStates[board] = boardStateMsg.State;
				}

				retVal.Add(message);

				break;
			}

			case MessageTypes.MarketData:
			{
				var secId = ((MarketDataMessage)message).SecurityId;

				if (!secId.IsAllSecurity())
					GetEmulator(secId).Process(message, retVal);

				break;
			}

			case MessageTypes.SecurityLookup:
			case MessageTypes.BoardLookup:
			case MessageTypes.DataTypeLookup:
			{
				// result will be sends as a loopback from underlying market data adapter
				break;
			}

			case MessageTypes.SubscriptionResponse:
			case MessageTypes.SubscriptionFinished:
			case MessageTypes.SubscriptionOnline:
			{
				retVal.Add(message.TypedClone());
				break;
			}

			default:
			{
				if (message is CandleMessage candleMsg)
					GetEmulator(candleMsg.SecurityId).Process(candleMsg, retVal);
				else
					retVal.Add(message);

				break;
			}
		}

		if (message.Type != MessageTypes.Reset)
			ProcessedMessageCount++;

		RecalcPnL(message.LocalTime, retVal);

		var allowStore = Settings.AllowStoreGenerateMessages;

		foreach (var msg in retVal)
		{
			if (!allowStore)
				msg.OfflineMode = MessageOfflineModes.Ignore;

			RaiseNewOutMessage(msg);
		}

		return true;
	}

	/// <inheritdoc />
	public event Action<Message> NewOutMessage;

	private void RaiseNewOutMessage(Message message)
	{
		_currentTime = message.LocalTime;
		NewOutMessage?.Invoke(message);
	}

	private SecurityMarketEmulator GetEmulator(SecurityId securityId)
	{
		// force hash code caching
		securityId.GetHashCode();

		return _securityEmulators.SafeAdd(securityId, key =>
		{
			var emulator = new SecurityMarketEmulator(this, securityId) { Parent = this };

			_securityEmulatorsByBoard.SafeAdd(securityId.BoardCode).Add(emulator);

			var sec = SecurityProvider.LookupById(securityId);

			if (sec != null)
				emulator.Process(sec.ToMessage(), []);

			var board = _boardDefinitions.TryGetValue(securityId.BoardCode);

			if (board != null)
				emulator.Process(board, []);

			return emulator;
		});
	}

	private PortfolioEmulator GetPortfolioInfo(string portfolioName)
	{
		return _portfolios.SafeAdd(portfolioName, key => new(this, key));
	}

	private void UpdateLevel1Info(Level1ChangeMessage level1Msg, ICollection<Message> retVal, bool addToResult)
	{
		var marginChanged = false;
		var state = _secStates.SafeAdd(level1Msg.SecurityId);

		foreach (var change in level1Msg.Changes)
		{
			switch (change.Key)
			{
				case Level1Fields.PriceStep:
				case Level1Fields.VolumeStep:
				case Level1Fields.MinPrice:
				case Level1Fields.MaxPrice:
					state[change.Key] = change.Value;
					break;

				case Level1Fields.State:
					if (Settings.CheckTradingState)
						state[change.Key] = change.Value;

					break;

				case Level1Fields.MarginBuy:
				case Level1Fields.MarginSell:
				{
					var oldValue = state.TryGetValue(change.Key);

					if (oldValue != null && (decimal)oldValue == (decimal)change.Value)
						break;

					state[change.Key] = change.Value;
					marginChanged = true;

					break;
				}
			}
		}

		if (addToResult)
			retVal.Add(level1Msg);

		if (!marginChanged)
			return;

		foreach (var info in _portfolios.Values)
			info.ProcessMarginChange(level1Msg.LocalTime, level1Msg.SecurityId, retVal);
	}

	private InvalidOperationException CheckRegistration(ExecutionMessage execMsg, SecurityMessage securityDefinition, bool priceStepExplicit, bool volumeStepExplicit/*, ICollection<Message> result*/)
	{
		if (Settings.CheckTradingState)
		{
			var board = _boardDefinitions.TryGetValue(execMsg.SecurityId.BoardCode);

			if (board != null)
			{
				if (!board.IsTradeTime(execMsg.ServerTime))
					return new(LocalizedStrings.SessionNotActive);
			}
		}

		var state = _secStates.TryGetValue(execMsg.SecurityId);

		var secState = (SecurityStates?)state?.TryGetValue(Level1Fields.State);

		if (secState == SecurityStates.Stoped)
			return new(LocalizedStrings.SecurityStopped.Put(execMsg.SecurityId));

		if (securityDefinition?.BasketCode.IsEmpty() == false)
			return new(LocalizedStrings.SecurityNonTradable.Put(execMsg.SecurityId));

		var priceStep = securityDefinition?.PriceStep;
		var volumeStep = securityDefinition?.VolumeStep;
		var minVolume = securityDefinition?.MinVolume;
		var maxVolume = securityDefinition?.MaxVolume;

		if (state != null && execMsg.OrderType != OrderTypes.Market)
		{
			var minPrice = (decimal?)state.TryGetValue(Level1Fields.MinPrice);
			var maxPrice = (decimal?)state.TryGetValue(Level1Fields.MaxPrice);

			priceStep ??= (decimal?)state.TryGetValue(Level1Fields.PriceStep);

			if (minPrice != null && minPrice > 0 && execMsg.OrderPrice < minPrice)
				return new(LocalizedStrings.OrderPriceTooLow.Put(execMsg.OrderPrice, execMsg.TransactionId, minPrice));

			if (maxPrice != null && maxPrice > 0 && execMsg.OrderPrice > maxPrice)
				return new(LocalizedStrings.OrderPriceTooHigh.Put(execMsg.OrderPrice, execMsg.TransactionId, maxPrice));
		}

		if (priceStepExplicit && priceStep != null && priceStep > 0 && execMsg.OrderPrice % priceStep != 0)
			return new(LocalizedStrings.OrderPriceNotMultipleOfPriceStep.Put(execMsg.OrderPrice, execMsg.TransactionId, priceStep));

		volumeStep ??= (decimal?)state?.TryGetValue(Level1Fields.VolumeStep);

		if (volumeStepExplicit && volumeStep != null && volumeStep > 0 && execMsg.OrderVolume % volumeStep != 0)
			return new(LocalizedStrings.OrderVolumeNotMultipleOfVolumeStep.Put(execMsg.OrderVolume, execMsg.TransactionId, volumeStep));

		if (minVolume != null && execMsg.OrderVolume < minVolume)
			return new(LocalizedStrings.OrderVolumeLessMin.Put(execMsg.OrderVolume, execMsg.TransactionId, minVolume));

		if (maxVolume != null && execMsg.OrderVolume > maxVolume)
			return new(LocalizedStrings.OrderVolumeMoreMax.Put(execMsg.OrderVolume, execMsg.TransactionId, maxVolume));

		return GetPortfolioInfo(execMsg.PortfolioName).CheckRegistration(execMsg/*, result*/);
	}

	private void RecalcPnL(DateTimeOffset time, ICollection<Message> messages)
	{
		if (Settings.PortfolioRecalcInterval == TimeSpan.Zero)
			return;

		if (time - _portfoliosPrevRecalc <= Settings.PortfolioRecalcInterval)
			return;

		foreach (var message in messages)
		{
			foreach (var emulator in _portfolios.Values)
				emulator.PnLManager.ProcessMessage(message);

			time = message.LocalTime;
		}

		foreach (var emulator in _portfolios.Values)
			emulator.AddPortfolioChangeMessage(time, messages);

		_portfoliosPrevRecalc = time;
	}

	ChannelStates IMessageChannel.State => ChannelStates.Started;

	IdGenerator IMessageAdapter.TransactionIdGenerator { get; } = new IncrementalIdGenerator();

	IEnumerable<MessageTypeInfo> IMessageAdapter.PossibleSupportedMessages { get; } =
	[
		MessageTypes.SecurityLookup.ToInfo(),
		MessageTypes.DataTypeLookup.ToInfo(),
		MessageTypes.BoardLookup.ToInfo(),
		MessageTypes.MarketData.ToInfo(),
		MessageTypes.PortfolioLookup.ToInfo(),
		MessageTypes.OrderStatus.ToInfo(),
		MessageTypes.OrderRegister.ToInfo(),
		MessageTypes.OrderCancel.ToInfo(),
		MessageTypes.OrderReplace.ToInfo(),
		MessageTypes.OrderGroupCancel.ToInfo(),
		MessageTypes.BoardState.ToInfo(),
		MessageTypes.Security.ToInfo(),
		MessageTypes.Portfolio.ToInfo(),
		MessageTypes.Board.ToInfo(),
		MessageTypes.Reset.ToInfo(),
		MessageTypes.QuoteChange.ToInfo(),
		MessageTypes.Level1Change.ToInfo(),
		MessageTypes.EmulationState.ToInfo(),
		ExtendedMessageTypes.CommissionRule.ToInfo(),
		//ExtendedMessageTypes.Clearing.ToInfo(),
	];
	IEnumerable<MessageTypes> IMessageAdapter.SupportedInMessages { get; set; }
	IEnumerable<MessageTypes> IMessageAdapter.NotSupportedResultMessages { get; } = [];
	IEnumerable<DataType> IMessageAdapter.GetSupportedMarketDataTypes(SecurityId securityId, DateTimeOffset? from, DateTimeOffset? to) =>
	[
		DataType.OrderLog,
		DataType.Ticks,
		DataType.CandleTimeFrame,
		DataType.MarketDepth,
	];

	IEnumerable<Level1Fields> IMessageAdapter.CandlesBuildFrom => [];

	bool IMessageAdapter.CheckTimeFrameByRequest => true;

	ReConnectionSettings IMessageAdapter.ReConnectionSettings { get; } = new ReConnectionSettings();

	TimeSpan IMessageAdapter.HeartbeatInterval { get => TimeSpan.Zero; set { } }

	string IMessageAdapter.StorageName => null;

	bool IMessageAdapter.IsNativeIdentifiersPersistable => false;
	bool IMessageAdapter.IsNativeIdentifiers => false;
	bool IMessageAdapter.IsFullCandlesOnly => false;
	bool IMessageAdapter.IsSupportSubscriptions => true;
	bool IMessageAdapter.IsSupportCandlesUpdates(MarketDataMessage subscription) => true;
	bool IMessageAdapter.IsSupportCandlesPriceLevels(MarketDataMessage subscription) => false;
	bool IMessageAdapter.IsSupportPartialDownloading => false;

	MessageAdapterCategories IMessageAdapter.Categories => default;

	IEnumerable<Tuple<string, Type>> IMessageAdapter.SecurityExtendedFields { get; } = [];
	IEnumerable<int> IMessageAdapter.SupportedOrderBookDepths => throw new NotSupportedException();
	bool IMessageAdapter.IsSupportOrderBookIncrements => false;
	bool IMessageAdapter.IsSupportExecutionsPnL => true;
	bool IMessageAdapter.IsSecurityNewsOnly => false;
	Type IMessageAdapter.OrderConditionType => null;
	bool IMessageAdapter.HeartbeatBeforConnect => false;
	Uri IMessageAdapter.Icon => null;
	bool IMessageAdapter.IsAutoReplyOnTransactonalUnsubscription => true;
	bool IMessageAdapter.EnqueueSubscriptions { get; set; }
	bool IMessageAdapter.IsSupportTransactionLog => false;
	bool IMessageAdapter.UseInChannel => false;
	bool IMessageAdapter.UseOutChannel => false;
	TimeSpan IMessageAdapter.IterationInterval => default;
	string IMessageAdapter.FeatureName => string.Empty;
	string[] IMessageAdapter.AssociatedBoards => [];
	bool? IMessageAdapter.IsPositionsEmulationRequired => true;
	bool IMessageAdapter.IsReplaceCommandEditCurrent => false;
	TimeSpan? IMessageAdapter.LookupTimeout => null;
	bool IMessageAdapter.ExtraSetup => false;

	IOrderLogMarketDepthBuilder IMessageAdapter.CreateOrderLogMarketDepthBuilder(SecurityId securityId)
		=> new OrderLogMarketDepthBuilder(securityId);

	TimeSpan IMessageAdapter.GetHistoryStepSize(SecurityId securityId, DataType dataType, out TimeSpan iterationInterval)
	{
		iterationInterval = TimeSpan.Zero;
		return TimeSpan.Zero;
	}

	int? IMessageAdapter.GetMaxCount(DataType dataType) => null;

	bool IMessageAdapter.IsAllDownloadingSupported(DataType dataType) => false;
	bool IMessageAdapter.IsSecurityRequired(DataType dataType) => dataType.IsSecurityRequired;

	void IMessageChannel.Open()
	{
	}

	void IMessageChannel.Close()
	{
	}

	void IMessageChannel.Suspend()
	{
	}

	void IMessageChannel.Resume()
	{
	}

	void IMessageChannel.Clear()
	{
	}

	event Action IMessageChannel.StateChanged
	{
		add { }
		remove { }
	}

	/// <summary>
	/// Extended verification mode.
	/// </summary>
	public bool VerifyMode { get; set; }

	IMessageChannel ICloneable<IMessageChannel>.Clone()
		=> new MarketEmulator(SecurityProvider, PortfolioProvider, ExchangeInfoProvider, TransactionIdGenerator) { VerifyMode = VerifyMode };

	object ICloneable.Clone() => ((ICloneable<IMessageChannel>)this).Clone();
}
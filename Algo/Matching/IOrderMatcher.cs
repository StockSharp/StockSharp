namespace StockSharp.Algo.Matching
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Messages;
	using StockSharp.Logging;
	using StockSharp.Localization;

	/// <summary>
	/// Interface described order matching engine.
	/// </summary>
	public interface IOrderMatcher
	{
		/// <summary>
		/// Security ID.
		/// </summary>
		SecurityId SecurityId { get; }

		/// <summary>
		/// Process <see cref="OrderRegisterMessage"/> message.
		/// </summary>
		/// <param name="message"><see cref="OrderRegisterMessage"/> message.</param>
		/// <param name="orderResult">Result messages.</param>
		/// <param name="priceResult">Result messages.</param>
		/// <returns>Reply message.</returns>
		ExecutionMessage RegisterOrder(OrderRegisterMessage message, Action<Message> orderResult, Action<Message> priceResult);

		/// <summary>
		/// Process <see cref="OrderReplaceMessage"/> message.
		/// </summary>
		/// <param name="message"><see cref="OrderReplaceMessage"/> message.</param>
		/// <param name="orderResult">Result messages.</param>
		/// <param name="priceResult">Result messages.</param>
		/// <param name="originalOrder">Original order state.</param>
		/// <returns>Reply message.</returns>
		ExecutionMessage ReplaceOrder(OrderReplaceMessage message, Action<Message> orderResult, Action<Message> priceResult, out ExecutionMessage originalOrder);

		/// <summary>
		/// Process <see cref="OrderCancelMessage"/> message.
		/// </summary>
		/// <param name="message"><see cref="OrderCancelMessage"/> message.</param>
		/// <param name="orderResult">Result messages.</param>
		/// <param name="priceResult">Result messages.</param>
		/// <param name="cancelledOrder">Cancelled order.</param>
		/// <returns>Reply message.</returns>
		ExecutionMessage CancelOrder(OrderCancelMessage message, Action<Message> orderResult, Action<Message> priceResult, out ExecutionMessage cancelledOrder);

		/// <summary>
		/// Process <see cref="OrderGroupCancelMessage"/> message.
		/// </summary>
		/// <param name="message"><see cref="OrderGroupCancelMessage"/> message.</param>
		/// <param name="result">Result messages.</param>
		/// <returns>Reply message.</returns>
		ExecutionMessage CancelOrders(OrderGroupCancelMessage message, Action<Message> result);

		/// <summary>
		/// Process <see cref="TimeMessage"/> message.
		/// </summary>
		/// <param name="time"><see cref="TimeMessage"/> message.</param>
		/// <param name="result">Result messages.</param>
		void ProcessTime(DateTimeOffset time, Action<Message> result);

		/// <summary>
		/// Process <see cref="OrderStatusMessage"/> message.
		/// </summary>
		/// <param name="message"><see cref="OrderStatusMessage"/> message.</param>
		/// <param name="result">Result messages.</param>
		void RequestOrders(OrderStatusMessage message, Action<Message> result);

		/// <summary>
		/// Get total volume.
		/// </summary>
		/// <param name="side">Side.</param>
		/// <returns>Total volume.</returns>
		decimal GetTotalVolume(Sides side);

		/// <summary>
		/// Get best quote.
		/// </summary>
		/// <param name="side">Side.</param>
		/// <returns>Best quote.</returns>
		QuoteChange? GetBest(Sides side);

		/// <summary>
		/// Get worst quote.
		/// </summary>
		/// <param name="side">Side.</param>
		/// <returns>Worst quote.</returns>
		QuoteChange? GetWorst(Sides side);

		/// <summary>
		/// Get all orders for the specified price level.
		/// </summary>
		/// <param name="side">Side.</param>
		/// <param name="price">Price level.</param>
		/// <returns>Orders.</returns>
		IEnumerable<ExecutionMessage> GetOrders(Sides side, decimal price);

		/// <summary>
		/// Get quote count.
		/// </summary>
		/// <param name="side">Side.</param>
		/// <returns>Count.</returns>
		int GetQuoteCount(Sides side);

		/// <summary>
		/// Get quotes.
		/// </summary>
		/// <param name="side">Side.</param>
		/// <returns>Quotes.</returns>
		IEnumerable<QuoteChange> GetQuotes(Sides side);
	}

	/// <summary>
	/// Default implementation of <see cref="IOrderMatcher"/>.
	/// </summary>
	public class OrderMatcher : BaseLogReceiver, IOrderMatcher
	{
		private class MessagePool<TMessage>
			where TMessage : Message, new()
		{
			private readonly Queue<TMessage> _messageQueue = new Queue<TMessage>();

			public TMessage Allocate()
			{
				if (_messageQueue.Count == 0)
				{
					var message = new TMessage();
					//queue.Enqueue(message);
					return message;
				}
				else
					return _messageQueue.Dequeue();
			}

			public void Free(TMessage message) => _messageQueue.Enqueue(message);
		}

		private class LevelQuotes : IEnumerable<ExecutionMessage>
		{
			private readonly List<ExecutionMessage> _quotes = new List<ExecutionMessage>();
			private readonly Dictionary<long, ExecutionMessage> _quotesByTrId = new Dictionary<long, ExecutionMessage>();

			public int Count => _quotes.Count;

			public ExecutionMessage this[int i]
			{
				get => _quotes[i];
				set
				{
					var prev = _quotes[i];

					if (prev.TransactionId != 0)
						_quotesByTrId.Remove(prev.TransactionId);

					_quotes[i] = value;

					if (value.TransactionId != 0)
						_quotesByTrId[value.TransactionId] = value;
				}
			}

			public ExecutionMessage TryGetByTransactionId(long transactionId) => _quotesByTrId.TryGetValue(transactionId);

			public void Add(ExecutionMessage quote)
			{
				if (quote.TransactionId != 0)
					_quotesByTrId[quote.TransactionId] = quote;

				_quotes.Add(quote);
			}

			public void RemoveAt(int index, ExecutionMessage quote = null)
			{
				if (quote == null)
					quote = _quotes[index];

				_quotes.RemoveAt(index);

				if (quote.TransactionId != 0)
					_quotesByTrId.Remove(quote.TransactionId);
			}

			public void Remove(ExecutionMessage quote) => RemoveAt(_quotes.IndexOf(quote), quote);

			public IEnumerator<ExecutionMessage> GetEnumerator() => _quotes.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}

		private readonly MessagePool<ExecutionMessage> _messagePool = new MessagePool<ExecutionMessage>();

		private readonly Dictionary<long, ExecutionMessage> _activeOrders = new Dictionary<long, ExecutionMessage>();
		private readonly Dictionary<ExecutionMessage, TimeSpan> _expirableOrders = new Dictionary<ExecutionMessage, TimeSpan>();

		private readonly SortedDictionary<decimal, RefPair<LevelQuotes, QuoteChange>> _bids = new SortedDictionary<decimal, RefPair<LevelQuotes, QuoteChange>>(new BackwardComparer<decimal>());
		private readonly SortedDictionary<decimal, RefPair<LevelQuotes, QuoteChange>> _asks = new SortedDictionary<decimal, RefPair<LevelQuotes, QuoteChange>>();
		
		private decimal _totalBidVolume;
		private decimal _totalAskVolume;

		private readonly IdGenerator _orderIdGenerator;
		private readonly IdGenerator _tradeIdGenerator;

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderMatcher"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="orderIdGenerator">The generator of identifiers for orders.</param>
		/// <param name="tradeIdGenerator">The generator of identifiers for trades.</param>
		public OrderMatcher(SecurityId securityId, IdGenerator orderIdGenerator, IdGenerator tradeIdGenerator)
		{
			SecurityId = securityId;

			_orderIdGenerator = orderIdGenerator ?? throw new ArgumentNullException(nameof(orderIdGenerator));
			_tradeIdGenerator = tradeIdGenerator ?? throw new ArgumentNullException(nameof(tradeIdGenerator));
		}

		/// <inheritdoc />
		public SecurityId SecurityId { get; }

		private QuoteChangeMessage AddBookChange(SecurityId securityId, Sides side, QuoteChange changedQuote)
		{
			//// отправляем измененный стакан
			//result.Add(CreateQuoteMessage(
			//	execution.SecurityId,
			//	time,
			//	GetServerTime(time)));

			var bookMsg = new QuoteChangeMessage
			{
				SecurityId = securityId,
				State = QuoteChangeStates.Increment,
			};

			if (side == Sides.Buy)
				bookMsg.Bids = new[] { changedQuote };
			else
				bookMsg.Asks = new[] { changedQuote };

			return bookMsg;
		}

		/// <inheritdoc />
		public ExecutionMessage RegisterOrder(OrderRegisterMessage regMsg, Action<Message> orderResult, Action<Message> priceResult)
		{
			if (regMsg is null)
				throw new ArgumentNullException(nameof(regMsg));

			//if (result is null)
			//	throw new ArgumentNullException(nameof(result));

			this.AddInfoLog(LocalizedStrings.Str1157Params, regMsg.TransactionId);

			var execution = regMsg.ToExec();

			// при восстановлении заявки у нее уже есть номер
			if (execution.OrderId == null)
			{
				execution.Balance = execution.OrderVolume;
				execution.OrderState = OrderStates.Active;
				execution.OrderId = _orderIdGenerator.GetNextId();
			}
			else
				execution.ServerTime = execution.ServerTime; // при восстановлении не меняем время

			ExecutionMessage replyMsg = null;

			if (orderResult != null)
			{
				replyMsg = regMsg.CreateReply();
				replyMsg.LocalTime = regMsg.LocalTime;
				replyMsg.OrderState = OrderStates.Active;

				orderResult(replyMsg);
			}

			MatchOrder(execution.LocalTime, execution, orderResult, true);

			if (execution.OrderState == OrderStates.Active)
			{
				_activeOrders.Add(execution.TransactionId, execution);

				if (execution.ExpiryDate != null)
					_expirableOrders.Add(execution, execution.ExpiryDate.Value.EndOfDay() - regMsg.LocalTime);

				// изменяем текущие котировки, добавляя туда наши цену и объем
				var changedQuote = UpdateQuote(execution, true, true);

				if (changedQuote != null)
					priceResult(AddBookChange(regMsg.SecurityId, regMsg.Side, changedQuote.Value));
			}
			else if (execution.IsCanceled())
			{
				//_parent
				//	.GetPortfolioInfo(execution.PortfolioName)
				//	.ProcessOrder(execution, execution.Balance.Value, result);
			}

			return replyMsg;
		}

		/// <inheritdoc />
		public ExecutionMessage ReplaceOrder(OrderReplaceMessage replaceMsg, Action<Message> orderResult, Action<Message> priceResult, out ExecutionMessage originalOrder)
		{
			if (replaceMsg is null)
				throw new ArgumentNullException(nameof(replaceMsg));

			//if (result is null)
			//	throw new ArgumentNullException(nameof(result));

			//if (replaceMsg.OldOrderVolume is null)
			//	replaceMsg.Volume = oldOrder.Balance.Value;

			var replyMsg = CancelOrderImpl(replaceMsg, orderResult, replaceMsg.OldOrderPrice != replaceMsg.Price ? priceResult : null, out originalOrder);

			if (replyMsg.Error is null)
			{
				replyMsg = RegisterOrder(replaceMsg, orderResult, priceResult);
			}

			return replyMsg;
		}

		/// <inheritdoc />
		public ExecutionMessage CancelOrder(OrderCancelMessage cancelMsg, Action<Message> orderResult, Action<Message> priceResult, out ExecutionMessage cancelledOrder)
		{
			return CancelOrderImpl(cancelMsg, orderResult, priceResult, out cancelledOrder);
		}

		private ExecutionMessage CancelOrderImpl(OrderMessage cancelMsg, Action<Message> orderResult, Action<Message> priceResult, out ExecutionMessage cancelledOrder)
		{
			if (cancelMsg is null)
				throw new ArgumentNullException(nameof(cancelMsg));

			ExecutionMessage replyMsg = null;

			if (_activeOrders.TryGetAndRemove(cancelMsg.OriginalTransactionId, out cancelledOrder))
			{
				replyMsg = CancelOrderImpl(cancelMsg.OriginalTransactionId, cancelMsg.LocalTime, cancelledOrder, orderResult, priceResult);
			}
			else
			{
				if (orderResult != null)
				{
					this.AddErrorLog(LocalizedStrings.Str1156Params, cancelMsg.OriginalTransactionId);

					replyMsg = cancelMsg.CreateReply(new InvalidOperationException(LocalizedStrings.Str1156Params.Put(cancelMsg.OriginalTransactionId)));
					orderResult(replyMsg);
				}
			}

			return replyMsg;
		}

		private ExecutionMessage CancelOrderImpl(long transactionId, DateTimeOffset time, ExecutionMessage cancelledOrder, Action<Message> orderResult, Action<Message> priceResult)
		{
			_expirableOrders.Remove(cancelledOrder);

			ExecutionMessage replyMsg = null;

			// изменяем текущие котировки, добавляя туда наши цену и объем
			var changedQuote = UpdateQuote(cancelledOrder, false, true);

			if (orderResult != null)
			{
				replyMsg = transactionId.CreateOrderReply(time);
				replyMsg.LocalTime = time;
				//replyMsg.OriginalTransactionId = execution.OriginalTransactionId;
				replyMsg.OrderState = OrderStates.Done;

				orderResult(replyMsg);

				this.AddInfoLog(LocalizedStrings.Str1155Params, transactionId);
			}

			// отправляем измененный стакан
			//result.Add(CreateQuoteMessage(
			//	order.SecurityId,
			//	time,
			//	GetServerTime(time)));

			if (priceResult != null && changedQuote != null)
				priceResult(AddBookChange(cancelledOrder.SecurityId, cancelledOrder.Side, changedQuote.Value));

			return replyMsg;
		}

		/// <inheritdoc />
		public ExecutionMessage CancelOrders(OrderGroupCancelMessage message, Action<Message> result)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));

			if (result is null)
				throw new ArgumentNullException(nameof(result));

			var checkByPf = !message.PortfolioName.IsEmpty();

			var orders = _activeOrders.Values.Where(order =>
			{
				if (message.Side != null && message.Side.Value != order.Side)
					return false;

				if (message.SecurityId != default && message.SecurityId != order.SecurityId)
					return false;

				if (checkByPf && message.PortfolioName.CompareIgnoreCase(order.PortfolioName))
					return false;

				return true;
			}).ToArray();

			var bids = new HashSet<decimal>();
			var asks = new HashSet<decimal>();

			foreach (var order in orders)
			{
				CancelOrderImpl(message.TransactionId, message.LocalTime, order, result, null);
				(order.Side == Sides.Buy ? bids : asks).Add(order.OrderPrice);
			}

			ExecutionMessage replyMsg = message.CreateReply();
			result(replyMsg);

			var book = new QuoteChangeMessage
			{
				SecurityId = SecurityId,
				ServerTime = message.LocalTime,
				LocalTime = message.LocalTime,
				State = QuoteChangeStates.Increment,
				Bids = bids.Select(p => _bids.TryGetValue(p)?.Second).Where(q => q != null).Select(q => q.Value).ToArray(),
				Asks = asks.Select(p => _asks.TryGetValue(p)?.Second).Where(q => q != null).Select(q => q.Value).ToArray(),
			};

			result(book);

			return replyMsg;
		}

		/// <inheritdoc />
		public void RequestOrders(OrderStatusMessage statusMsg, Action<Message> result)
		{
			if (statusMsg is null)
				throw new ArgumentNullException(nameof(statusMsg));

			if (result is null)
				throw new ArgumentNullException(nameof(result));

			var checkByPf = !statusMsg.PortfolioName.IsEmpty();

			var finish = false;

			foreach (var order in _activeOrders.Values)
			{
				if (checkByPf)
				{
					if (!order.PortfolioName.CompareIgnoreCase(statusMsg.PortfolioName))
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
				result(clone);

				if (finish)
					break;
			}
		}

		//private void UpdateQuotes(ExecutionMessage message, Action<Message> result)
		//{
		//	// матчинг заявок происходит не только для своих сделок, но и для чужих.
		//	// различие лишь в том, что для чужих заявок не транслируется информация о сделках.
		//	// матчинг чужих заявок на равне со своими дает наиболее реалистичный сценарий обновления стакана.

		//	if (message.TradeId != null)
		//		throw new ArgumentException(LocalizedStrings.Str1159, nameof(message));

		//	if (message.OrderVolume == null || message.OrderVolume <= 0)
		//		throw new ArgumentOutOfRangeException(nameof(message), message.OrderVolume, LocalizedStrings.Str1160Params.Put(message.TransactionId));

		//	if (message.IsCancellation)
		//	{
		//		UpdateQuote(message, false, true);
		//		return;
		//	}

		//	// не ставим чужую заявку в стакан сразу, только её остаток после матчинга
		//	//UpdateQuote(message, true);

		//	if (_activeOrders.Count > 0)
		//	{
		//		foreach (var order in _activeOrders.Values.ToArray())
		//		{
		//			MatchOrder(message.LocalTime, order, result, false);

		//			if (order.OrderState != OrderStates.Done)
		//				continue;

		//			_activeOrders.Remove(order.TransactionId);
		//			_expirableOrders.Remove(order);

		//			// изменяем текущие котировки, удаляя оттуда наши цену и объем
		//			UpdateQuote(order, false, true);
		//		}
		//	}

		//	//для чужих FOK заявок необходимо убрать ее из стакана после исполнения своих заявок
		//	// [upd] теперь не ставим чужую заявку сразу в стакан, поэтому и удалять не нужно 
		//	//if (message.TimeInForce == TimeInForce.MatchOrCancel && !message.IsCancelled)
		//	//{
		//	//	UpdateQuote(new ExecutionMessage
		//	//	{
		//	//		ExecutionType = ExecutionTypes.Transaction,
		//	//		Side = message.Side,
		//	//		OrderPrice = message.OrderPrice,
		//	//		OrderVolume = message.OrderVolume,
		//	//		HasOrderInfo = true,
		//	//	}, false);
		//	//}

		//	// для чужих заявок заполняется только объем
		//	message.Balance = message.OrderVolume;

		//	// исполняем чужую заявку как свою. при этом результат выполнения не идет никуда
		//	MatchOrder(message.LocalTime, message, null, true);

		//	if (message.Balance > 0)
		//	{
		//		UpdateQuote(message, true, false);
		//	}
		//}

		private QuoteChange? UpdateQuote(ExecutionMessage message, bool register, bool byVolume)
		{
			var quotes = GetQuotes(message.Side);

			var pair = quotes.TryGetValue(message.OrderPrice);

			if (pair == null)
			{
				if (!register)
					return null;

				quotes[message.OrderPrice] = pair = RefTuple.Create(new LevelQuotes(), new QuoteChange(message.OrderPrice, 0));
			}

			var level = pair.First;

			var volume = byVolume ? message.SafeGetVolume() : message.GetBalance();

			if (register)
			{
				//если пришло увеличение объема на уровне, то всегда добавляем в конец очереди, даже для диффа стаканов
				//var clone = message.TypedClone();
				var clone = _messagePool.Allocate();
					
				clone.TransactionId = message.TransactionId;
				clone.OrderPrice = message.OrderPrice;
				clone.PortfolioName = message.PortfolioName;
				clone.Balance = byVolume ? message.OrderVolume : message.Balance;
				clone.OrderVolume = message.OrderVolume;

				AddTotalVolume(message.Side, volume);

				var q = pair.Second;
				q.Volume += volume;
				pair.Second = q;
				level.Add(clone);
			}
			else
			{
				if (message.TransactionId == 0)
				{
					var leftBalance = volume;

					// пришел дифф по стакану - начиная с конца убираем снятый объем
					for (var i = level.Count - 1; i >= 0 && leftBalance > 0; i--)
					{
						var quote = level[i];

						if (quote.TransactionId != message.TransactionId)
							continue;

						var balance = quote.GetBalance();
						leftBalance -= balance;

						if (leftBalance < 0)
						{
							leftBalance = -leftBalance;

							//var clone = message.TypedClone();
							var clone = _messagePool.Allocate();

							clone.TransactionId = message.TransactionId;
							clone.OrderPrice = message.OrderPrice;
							clone.PortfolioName = message.PortfolioName;
							clone.Balance = leftBalance;
							clone.OrderVolume = message.OrderVolume;

							var diff = leftBalance - balance;
							AddTotalVolume(message.Side, diff);

							var q1 = pair.Second;
							q1.Volume += diff;
							pair.Second = q1;

							level[i] = clone;
							break;
						}

						AddTotalVolume(message.Side, -balance);

						var q = pair.Second;
						q.Volume -= balance;
						pair.Second = q;
						level.RemoveAt(i, quote);
						_messagePool.Free(quote);
					}
				}
				else
				{
					var quote = level.TryGetByTransactionId(message.TransactionId);

					//TODO при перерегистрации номер транзакции может совпадать для двух заявок
					//if (quote == null)
					//	throw new InvalidOperationException("Котировка для отмены с номером транзакции {0} не найдена.".Put(message.TransactionId));

					if (quote != null)
					{
						var balance = quote.GetBalance();

						AddTotalVolume(message.Side, -balance);

						var q = pair.Second;
						q.Volume -= balance;
						pair.Second = q;
						level.Remove(quote);
						_messagePool.Free(quote);
					}
				}

				if (level.Count == 0)
					quotes.Remove(message.OrderPrice);
			}

			return pair.Second;
		}

		private void MatchOrder(DateTimeOffset time, ExecutionMessage order, Action<Message> result, bool isNewOrder)
		{
			//string matchError = null;
			var isCrossTrade = false;

			var executions = result == null ? null : new Dictionary<decimal, decimal>();

			var quotes = GetQuotes(order.Side.Invert());

			List<decimal> toRemove = null;

			var leftBalance = order.GetBalance();
			var sign = order.Side == Sides.Buy ? 1 : -1;
			var orderPrice = order.OrderPrice;
			var isMarket = order.OrderType == OrderTypes.Market;
			var postOnly = order.PostOnly == true;

			foreach (var pair in quotes)
			{
				var price = pair.Key;
				var levelQuotes = pair.Value.First;
				var qc = pair.Value.Second;

				// для старых заявок, когда стакан пробивает уровень заявки,
				// матчим по цене ранее выставленной заявки.
				var execPrice = isNewOrder ? price : orderPrice;

				if (!isMarket)
				{
					if (sign * price > sign * orderPrice)
						break;

					//if (price == orderPrice && !Settings.MatchOnTouch)
					//	break;

					if (postOnly)
					{
						order.OrderState = OrderStates.Done;
						result(ToOrder(time, order));
						return;
					}
				}

				// объем заявки больше или равен всему уровню в стакане, то сразу удаляем его целиком
				if (leftBalance >= qc.Volume)
				{
					if (executions != null)
					{
						for (var i = 0; i < levelQuotes.Count; i++)
						{
							var quote = levelQuotes[i];

							// если это пользовательская заявка и матчинг идет о заявку с таким же портфелем
							if (quote.PortfolioName == order.PortfolioName)
							{
								var matchError = LocalizedStrings.Str1161Params.Put(quote.TransactionId, order.TransactionId);
								this.AddErrorLog(matchError);

								isCrossTrade = true;
								break;
							}

							var volume = quote.GetBalance().Min(leftBalance);

							if (volume <= 0)
								throw new InvalidOperationException(LocalizedStrings.Str1162);

							executions[execPrice] = executions.TryGetValue(execPrice) + volume;
							this.AddInfoLog(LocalizedStrings.Str1163Params, order.TransactionId, volume, execPrice);

							levelQuotes.RemoveAt(i, quote);
							_messagePool.Free(quote);
						}
					}
					else
					{
						if (toRemove == null)
							toRemove = new List<decimal>();

						toRemove.Add(price);

						foreach (var quote in levelQuotes)
							_messagePool.Free(quote);
							
						AddTotalVolume(order.Side.Invert(), -qc.Volume);
					}

					leftBalance -= qc.Volume;
				}
				else
				{
					for (var i = 0; i < levelQuotes.Count; i++)
					{
						var quote = levelQuotes[i];

						// если это пользовательская заявка и матчинг идет о заявку с таким же портфелем
						if (executions != null && quote.PortfolioName == order.PortfolioName)
						{
							var matchError = LocalizedStrings.Str1161Params.Put(quote.TransactionId, order.TransactionId);
							this.AddErrorLog(matchError);

							isCrossTrade = true;
							break;
						}

						var volume = quote.GetBalance().Min(leftBalance);

						if (volume <= 0)
							throw new InvalidOperationException(LocalizedStrings.Str1162);

						// если это пользовательская заявка
						if (executions != null)
						{
							executions[execPrice] = executions.TryGetValue(execPrice) + volume;
							this.AddInfoLog(LocalizedStrings.Str1163Params, order.TransactionId, volume, execPrice);
						}

						quote.Balance -= volume;

						if (quote.Balance == 0)
						{
							levelQuotes.RemoveAt(i, quote);
							i--;

							_messagePool.Free(quote);

							if (levelQuotes.Count == 0)
							{
								if (toRemove == null)
									toRemove = new List<decimal>();

								toRemove.Add(price);
							}
						}

						AddTotalVolume(order.Side.Invert(), -volume);
						qc.Volume -= volume;
						leftBalance -= volume;

						if (leftBalance == 0)
							break;
					}
				}

				if (leftBalance == 0 || isCrossTrade)
					break;
			}

			if (toRemove != null)
			{
				foreach (var value in toRemove)
					quotes.Remove(value);
			}

			// если это не пользовательская заявка
			if (result == null)
			{
				order.Balance = leftBalance;
				return;
			}

			leftBalance = order.GetBalance() - executions.Values.Sum();

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
							this.AddInfoLog(LocalizedStrings.Str1164Params, order.TransactionId);
						}

						result(ToOrder(time, order));
					}
							
					if (isMarket)
					{
						if (leftBalance > 0)
						{
							this.AddInfoLog(LocalizedStrings.Str1165Params, order.TransactionId, leftBalance);

							order.OrderState = OrderStates.Done;
							result(ToOrder(time, order));	
						}
					}

					break;
				}

				case TimeInForce.MatchOrCancel:
				{
					if (leftBalance == 0)
						order.Balance = 0;

					this.AddInfoLog(LocalizedStrings.Str1166Params, order.TransactionId);

					order.OrderState = OrderStates.Done;
					result(ToOrder(time, order));

					// заявка не исполнилась полностью, поэтому она вся отменяется, не влияя на стакан
					if (leftBalance > 0)
						return;

					break;
				}

				case TimeInForce.CancelBalance:
				{
					this.AddInfoLog(LocalizedStrings.Str1167Params, order.TransactionId);

					order.Balance = leftBalance;
					order.OrderState = OrderStates.Done;
					result(ToOrder(time, order));
					break;
				}
			}

			if (isCrossTrade)
			{
				//var reply = order.OriginalTransactionId.CreateOrderReply(time);
				//reply.LocalTime = time;

				//reply.OrderState = OrderStates.Failed;
				//reply.OrderStatus = (long?)OrderStatus.RejectedBySystem;
				//reply.Error = new InvalidOperationException(matchError);

				//reply.OrderState = OrderStates.Done;
				//reply.OrderStatus = (long?)OrderStatus.CanceledByManager;

				//result(reply);

				order.OrderState = OrderStates.Done;
				result(ToOrder(time, order));
			}

			foreach (var execution in executions)
			{
				var tradeMsg = ToMyTrade(time, order, execution.Key, execution.Value);
				result(tradeMsg);

				this.AddInfoLog(LocalizedStrings.Str1168Params, tradeMsg.TradeId, tradeMsg.OriginalTransactionId, execution.Key, execution.Value);
			}
		}

		private ExecutionMessage ToMyTrade(DateTimeOffset time, ExecutionMessage order, decimal price, decimal volume)
		{
			return new ExecutionMessage
			{
				LocalTime = time,
				SecurityId = order.SecurityId,
				OrderId = order.OrderId,
				OriginalTransactionId = order.TransactionId,
				TradeId = _tradeIdGenerator.GetNextId(),
				TradePrice = price,
				TradeVolume = volume,
				ExecutionType = ExecutionTypes.Transaction,
				HasTradeInfo = true,
				ServerTime = time,
				Side = order.Side,
			};
		}

		void IOrderMatcher.ProcessTime(DateTimeOffset time, Action<Message> result)
		{
			ProcessExpirableOrders(time, result);
		}

		private DateTimeOffset _prevTime;

		private void ProcessExpirableOrders(DateTimeOffset time, Action<Message> result)
		{
			if (_expirableOrders.Count == 0)
				return;

			if (_prevTime == default)
				_prevTime = time;

			var diff = time - _prevTime;

			foreach (var pair in _expirableOrders.ToArray())
			{
				var orderMsg = pair.Key;
				var left = pair.Value;
				left -= diff;

				if (left <= TimeSpan.Zero)
				{
					_expirableOrders.Remove(orderMsg);
					_activeOrders.Remove(orderMsg.TransactionId);

					orderMsg.OrderState = OrderStates.Done;
					result(ToOrder(time, orderMsg));

					// изменяем текущие котировки, удаляя оттуда наши цену и объем
					var changedQuote = UpdateQuote(orderMsg, false, true);

					if (changedQuote != null)
						result(AddBookChange(orderMsg.SecurityId, orderMsg.Side, changedQuote.Value));
				}
				else
					_expirableOrders[orderMsg] = left;
			}

			_prevTime = time;
		}

		private ExecutionMessage ToOrder(DateTimeOffset time, ExecutionMessage message)
		{
			return new ExecutionMessage
			{
				LocalTime = time,
				SecurityId = message.SecurityId,
				OrderId = message.OrderId,
				OriginalTransactionId = message.TransactionId,
				Balance = message.Balance,
				OrderState = message.OrderState,
				PortfolioName = message.PortfolioName,
				ExecutionType = ExecutionTypes.Transaction,
				HasOrderInfo = true,
				ServerTime = time,
			};
		}

		private void AddTotalVolume(Sides side, decimal diff)
		{
			if (side == Sides.Buy)
				_totalBidVolume += diff;
			else
				_totalAskVolume += diff;
		}

		private SortedDictionary<decimal, RefPair<LevelQuotes, QuoteChange>> GetQuotes(Sides side)
		{
			switch (side)
			{
				case Sides.Buy:
					return _bids;
				case Sides.Sell:
					return _asks;
				default:
					throw new ArgumentOutOfRangeException(nameof(side), side, LocalizedStrings.Str1219);
			}
		}

		decimal IOrderMatcher.GetTotalVolume(Sides side)
			=> side == Sides.Buy ? _totalBidVolume : _totalAskVolume;

		QuoteChange? IOrderMatcher.GetBest(Sides side)
			=> GetQuotes(side).FirstOr()?.Value.Second;

		QuoteChange? IOrderMatcher.GetWorst(Sides side)
			=> GetQuotes(side).LastOr()?.Value.Second;

		IEnumerable<ExecutionMessage> IOrderMatcher.GetOrders(Sides side, decimal price)
			=> GetQuotes(side)[price].First;

		int IOrderMatcher.GetQuoteCount(Sides side)	=> GetQuotes(side).Count;

		IEnumerable<QuoteChange> IOrderMatcher.GetQuotes(Sides side)
			=> GetQuotes(side).Select(p => p.Value.Second);
	}
}
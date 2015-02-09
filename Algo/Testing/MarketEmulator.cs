namespace StockSharp.Algo.Testing
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Algo.Commissions;
	using StockSharp.Algo.PnL;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Algo.Candles;
	using StockSharp.Localization;

	/// <summary>
	/// Эмулятор торгов.
	/// </summary>
	public class MarketEmulator : BaseLogReceiver, IMarketEmulator
	{
		private class MessagePool
		{
			private readonly Queue<Message>[] _messageQueues;

			public MessagePool()
			{
				_messageQueues = new Queue<Message>[Enumerator.GetValues<MessageTypes>().Count()];

				for (var i = 0; i < _messageQueues.Length; i++)
					_messageQueues[i] = new Queue<Message>();
			}

			public TMessage Allocate<TMessage>(MessageTypes type)
				where TMessage : Message, new()
			{
				var queue = _messageQueues[(int)type];

				if (queue.Count == 0)
				{
					var message = new TMessage();
					//queue.Enqueue(message);
					return message;
				}
				else
					return (TMessage)queue.Dequeue();
			}

			public void Free<TMessage>(TMessage message)
				where TMessage : Message
			{
				var queue = _messageQueues[(int)message.Type];
				queue.Enqueue(message);
			}
		}

		// TODO сделать хранение описания инструментов и сделать эмуляцию на Level1

		private sealed class SecurityMarketEmulator : BaseLogReceiver//, IMarketEmulator
		{
			private readonly MarketEmulator _parent;
			
			private readonly Dictionary<ExecutionMessage, TimeSpan> _expirableOrders = new Dictionary<ExecutionMessage, TimeSpan>();
			private readonly Dictionary<long, ExecutionMessage> _activeOrders = new Dictionary<long, ExecutionMessage>();
			private readonly SortedDictionary<decimal, RefPair<List<ExecutionMessage>, QuoteChange>> _bids = new SortedDictionary<decimal, RefPair<List<ExecutionMessage>, QuoteChange>>(new BackwardComparer<decimal>());
			private readonly SortedDictionary<decimal, RefPair<List<ExecutionMessage>, QuoteChange>> _asks = new SortedDictionary<decimal, RefPair<List<ExecutionMessage>, QuoteChange>>();
			private readonly Dictionary<ExecutionMessage, TimeSpan> _pendingExecutions = new Dictionary<ExecutionMessage, TimeSpan>();
			private DateTime _prevTime;
			private readonly ExecutionLogConverter _execLogConverter;
			private SecurityMessage _securityDefinition;
			private int _volumeDecimals;
			private readonly SortedDictionary<DateTimeOffset, Tuple<List<CandleMessage>, List<ExecutionMessage>>> _candleInfo = new SortedDictionary<DateTimeOffset, Tuple<List<CandleMessage>, List<ExecutionMessage>>>();
			private TradeGenerator _tradeGenerator;
			private MarketDepthGenerator _depthGenerator;
			private OrderLogGenerator _olGenerator;
			private LogLevels? _logLevel;
			private DateTime _lastStripDate;

			private decimal _totalBidVolume;
			private decimal _totalAskVolume;

			private readonly MessagePool _messagePool = new MessagePool();

			public SecurityMarketEmulator(MarketEmulator parent, SecurityId securityId)
			{
				if (parent == null)
					throw new ArgumentNullException("parent");

				_parent = parent;
				_execLogConverter = new ExecutionLogConverter(securityId, _bids, _asks, _parent.Settings);
			}

			public IEnumerable<Message> Process(Message message)
			{
				var result = new List<Message>();
				Process(message, result);
				return result;
			}

			private void LogMessage(Message message, bool isInput)
			{
				if (_logLevel == null)
					_logLevel = this.GetLogLevel();

				if (_logLevel != LogLevels.Debug)
					return;

				if (message.Type != MessageTypes.Time &&
					message.Type != MessageTypes.Level1Change &&
					message.Type != MessageTypes.QuoteChange)
					this.AddDebugLog((isInput ? " --> {0}" : " <-- {0}"), message);
			}

			private void Process(Message message, ICollection<Message> result)
			{
				if (_prevTime == DateTime.MinValue)
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

						switch (execMsg.ExecutionType)
						{
							case ExecutionTypes.Tick:
							{
								foreach (var m in _execLogConverter.ToExecutionLog(execMsg))
									Process(m, result);

								result.Add(execMsg);

								result.Add(CreateQuoteMessage(
									execMsg.SecurityId,
									execMsg.LocalTime,
									execMsg.ServerTime));
								break;
							}
							case ExecutionTypes.Order:
							{
								if (_parent._settings.Latency > TimeSpan.Zero)
								{
									this.AddInfoLog(LocalizedStrings.Str1145Params, execMsg.IsCancelled ? LocalizedStrings.Str1146 : LocalizedStrings.Str1147, execMsg.TransactionId == 0 ? execMsg.OriginalTransactionId : execMsg.TransactionId);
									_pendingExecutions.Add((ExecutionMessage)execMsg.Clone(), _parent._settings.Latency);
								}
								else
									AcceptExecution(execMsg.LocalTime, execMsg, result);

								break;
							}
							case ExecutionTypes.Trade:
								throw new InvalidOperationException();
							case ExecutionTypes.OrderLog:
							{
								if (execMsg.TradeId == 0)
									UpdateQuotes(execMsg, result);

								// добавляем в результат ОЛ только из хранилища или из генератора
								// (не из ExecutionLogConverter)
								if (execMsg.TransactionId > 0)
									result.Add(execMsg);

								break;
							}
							default:
								throw new ArgumentOutOfRangeException();
						}

						break;
					}

					case MessageTypes.OrderRegister:
					{
						var orderMsg = (OrderRegisterMessage)message;

						foreach (var m in _execLogConverter.ToExecutionLog(orderMsg, GetTotalVolume(orderMsg.Side.Invert())))
							Process(m, result);

						break;
					}

					case MessageTypes.OrderCancel:
					{
						var orderMsg = (OrderCancelMessage)message;

						foreach (var m in _execLogConverter.ToExecutionLog(orderMsg, 0))
							Process(m, result);

						break;
					}

					case MessageTypes.OrderReplace:
					{
						//при перерегистрации могут приходить заявки с нулевым объемом
						//объем при этом надо взять из старой заявки.
						var orderMsg = (OrderReplaceMessage)message;
						var oldOrder = _activeOrders.TryGetValue(orderMsg.OldTransactionId);

						foreach (var execMsg in _execLogConverter.ToExecutionLog(orderMsg, GetTotalVolume(orderMsg.Side.Invert())))
						{
							if (oldOrder != null)
							{
								if (!execMsg.IsCancelled && execMsg.Volume == 0)
									execMsg.Volume = oldOrder.Balance;

								Process(execMsg, result);
							}
							else if (execMsg.IsCancelled)
							{
								var error = LocalizedStrings.Str1148Params.Put(execMsg.OrderId);

								// ошибка отмены
								result.Add(new ExecutionMessage
								{
									LocalTime = orderMsg.LocalTime,
									OriginalTransactionId = orderMsg.TransactionId,
									OrderId = execMsg.OrderId,
									ExecutionType = ExecutionTypes.Order,
									SecurityId = orderMsg.SecurityId,
									IsCancelled = true,
									OrderState = OrderStates.Failed,
									Error = new InvalidOperationException(error),
								});

								// ошибка регистрации
								result.Add(new ExecutionMessage
								{
									LocalTime = orderMsg.LocalTime,
									OriginalTransactionId = orderMsg.TransactionId,
									ExecutionType = ExecutionTypes.Order,
									SecurityId = orderMsg.SecurityId,
									IsCancelled = false,
									OrderState = OrderStates.Failed,
									Error = new InvalidOperationException(error),
								});

								this.AddErrorLog(LocalizedStrings.Str1148Params, orderMsg.OldTransactionId);
							}
						}

						break;
					}

					case MessageTypes.QuoteChange:
					{
						var quoteMsg = (QuoteChangeMessage)message;

						foreach (var m in _execLogConverter.ToExecutionLog(quoteMsg))
							Process(m, result);

						// возращаем не входящий стакан, а тот, что сейчас хранится внутри эмулятора.
						// таким образом мы можем видеть в стакане свои цены и объемы

						result.Add(CreateQuoteMessage(
							quoteMsg.SecurityId,
							quoteMsg.LocalTime,
							quoteMsg.ServerTime));

						break;
					}

					case MessageTypes.Level1Change:
					{
						var level1Msg = (Level1ChangeMessage)message;

						UpdateSecurityDefinition(level1Msg);

						foreach (var m in _execLogConverter.ToExecutionLog(level1Msg))
							Process(m, result);

						break;
					}

					case MessageTypes.Security:
					{
						_securityDefinition = (SecurityMessage)message.Clone();
						_volumeDecimals = _securityDefinition.VolumeStep.GetCachedDecimals();
						_execLogConverter.UpdateSecurityDefinition(_securityDefinition);
						break;
					}

					case MessageTypes.Board:
					{
						_execLogConverter.UpdateBoardDefinition((BoardMessage)message);
						break;
					}

					case MessageTypes.CandleTimeFrame:
					case MessageTypes.CandlePnF:
					case MessageTypes.CandleRange:
					case MessageTypes.CandleRenko:
					case MessageTypes.CandleTick:
					case MessageTypes.CandleVolume:
					{
						var candleMsg = (CandleMessage)message;

						// в трейдах используется время открытия свечи, при разных MarketTimeChangedInterval и TimeFrame свечек
						// возможны ситуации, когда придет TimeMsg 11:03:00, а время закрытия будет 11:03:30
						// т.о. время уйдет вперед данных, которые построены по свечкам.
						var info = _candleInfo.SafeAdd(candleMsg.OpenTime, key => Tuple.Create(new List<CandleMessage>(), new List<ExecutionMessage>()));

						info.Item1.Add((CandleMessage)candleMsg.Clone());

						if (_securityDefinition != null && _parent._settings.UseCandlesTimeFrame != null)
						{
							var trades = candleMsg.ToTrades(_securityDefinition.VolumeStep, _volumeDecimals).ToArray();
							Process(trades[0], result);
							info.Item2.AddRange(trades.Skip(1));	
						}
						
						break;
					}

					case ExtendedMessageTypes.Generator:
					{
						var generatorMsg = (GeneratorMessage)message;

						switch (generatorMsg.DataType)
						{
							case MarketDataTypes.MarketDepth:
							{
								_depthGenerator = generatorMsg.IsSubscribe
										? (MarketDepthGenerator)generatorMsg.Generator
										: null;

								break;
							}
							case MarketDataTypes.Trades:
							{
								_tradeGenerator = generatorMsg.IsSubscribe
										? (TradeGenerator)generatorMsg.Generator
										: null;

								break;
							}
							case MarketDataTypes.OrderLog:
							{
								_olGenerator = generatorMsg.IsSubscribe
										? (OrderLogGenerator)generatorMsg.Generator
										: null;

								break;
							}
							default:
								throw new ArgumentOutOfRangeException();
						}

						if (generatorMsg.IsSubscribe)
						{
							generatorMsg.Generator.Init();

							if (_securityDefinition != null)
							{
								var board = _parent._boardDefinitions.TryGetValue(_securityDefinition.SecurityId.BoardCode);

								if (board == null)
								{
									this.AddWarningLog(LocalizedStrings.Str1149);
									break;
								}

								ProcessGenerator(generatorMsg.Generator, _securityDefinition, result);
								ProcessGenerator(generatorMsg.Generator, board, result);
							}
							else
								this.AddWarningLog(LocalizedStrings.Str1150);
						}

						break;
					}

					default:
						throw new ArgumentOutOfRangeException();
				}

				ProcessTime(message, result);

				_prevTime = message.LocalTime;

				foreach (var item in result)
					LogMessage(item, false);
			}

			private void UpdatePriceLimits(ExecutionMessage execution, ICollection<Message> result)
			{
				if (_lastStripDate == execution.LocalTime.Date)
					return;

				decimal price;

				switch (execution.ExecutionType)
				{
					case ExecutionTypes.Tick:
						price = execution.TradePrice;
						break;

					case ExecutionTypes.OrderLog:
						price = execution.Price;
						break;

					default:
						return;
				}

				_lastStripDate = execution.LocalTime.Date;

				var priceOffset = _parent.Settings.PriceLimitOffset;
				var priceStep = _securityDefinition == null || _securityDefinition.PriceStep == 0
					? 0.01m
					: _securityDefinition.PriceStep;

				var level1Msg =
					new Level1ChangeMessage
					{
						SecurityId = execution.SecurityId,
						LocalTime = execution.LocalTime,
						ServerTime = execution.ServerTime,
					}
					.Add(Level1Fields.MinPrice, ShrinkPrice((decimal)(price - priceOffset), priceStep))
					.Add(Level1Fields.MaxPrice, ShrinkPrice((decimal)(price + priceOffset), priceStep));

				_parent.UpdateLevel1Info(level1Msg, result);
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
							break;
						case Level1Fields.VolumeStep:
							_securityDefinition.VolumeStep = (decimal)change.Value;
							_volumeDecimals = _securityDefinition.VolumeStep.GetCachedDecimals();
							break;
						case Level1Fields.Multiplier:
							_securityDefinition.Multiplier = (decimal)change.Value;
							break;
					}
				}
			}

			private static decimal ShrinkPrice(decimal price, decimal priceStep)
			{
				var decimals = priceStep.GetCachedDecimals();

				return price
					.Round(priceStep, decimals, null)
					.RemoveTrailingZeros();
			}

			private static ExecutionMessage CreateReply(ExecutionMessage original, DateTimeOffset time)
			{
				var replyMsg = (ExecutionMessage)original.Clone();

				replyMsg.ServerTime = time;
				replyMsg.LocalTime = time.LocalDateTime;
				replyMsg.OriginalTransactionId = original.TransactionId;

				return replyMsg;
			}

			private void AcceptExecution(DateTime time, ExecutionMessage execution, ICollection<Message> result)
			{
				if (_parent._settings.Failing > 0)
				{
					if (RandomGen.GetDouble() < (_parent._settings.Failing / 100.0))
					{
						this.AddErrorLog(LocalizedStrings.Str1151Params, execution.IsCancelled ? LocalizedStrings.Str1152 : LocalizedStrings.Str1153, execution.OriginalTransactionId == 0 ? execution.TransactionId : execution.OriginalTransactionId);

						var replyMsg = CreateReply(execution, time);

						replyMsg.Balance = execution.Volume;
						replyMsg.OrderState = OrderStates.Failed;
						replyMsg.Error = new InvalidOperationException(LocalizedStrings.Str1154);
						replyMsg.LocalTime = time;
						result.Add(replyMsg);
						return;
					}
				}

				if (execution.IsCancelled)
				{
					var order = _activeOrders.TryGetValue(execution.OriginalTransactionId);

					if (_activeOrders.Remove(execution.OriginalTransactionId))
					{
						var replyMsg = CreateReply(order, time);

						replyMsg.OriginalTransactionId = execution.OriginalTransactionId;
						replyMsg.OrderState = OrderStates.Done;
						_expirableOrders.Remove(replyMsg);

						// изменяем текущие котировки, добавляя туда наши цену и объем
						UpdateQuote(order, false);

						// отправляем измененный стакан
						result.Add(CreateQuoteMessage(
							replyMsg.SecurityId,
							time,
							time));

						result.Add(replyMsg);

						this.AddInfoLog(LocalizedStrings.Str1155Params, execution.OriginalTransactionId);

						replyMsg.Commission = _parent
							.GetPortfolioInfo(execution.PortfolioName)
							.ProcessOrder(order, false, result);
					}
					else
					{
						var replyMsg = CreateReply(execution, time);

						replyMsg.OrderState = OrderStates.Failed;
						replyMsg.Error = new InvalidOperationException(LocalizedStrings.Str1156Params.Put(replyMsg.OrderId));

						result.Add(replyMsg);

						this.AddErrorLog(LocalizedStrings.Str1156Params, execution.OriginalTransactionId);
					}
				}
				else
				{
					var message = _parent.CheckRegistration(execution);

					var replyMsg = CreateReply(execution, time);

					if (message == null)
					{
						this.AddInfoLog(LocalizedStrings.Str1157Params, execution.TransactionId);

						// при восстановлении заявки у нее уже есть номер
						if (replyMsg.OrderId == 0)
						{
							replyMsg.Balance = execution.Volume;
							replyMsg.OrderState = OrderStates.Active;
							replyMsg.OrderId = _parent._orderIdGenerator.GetNextId();
						}
						else
							replyMsg.ServerTime = execution.ServerTime; // при восстановлении не меняем время

						result.Add(replyMsg);

						replyMsg.Commission = _parent
							.GetPortfolioInfo(execution.PortfolioName)
							.ProcessOrder(execution, true, result);

						MatchOrder(execution.LocalTime, replyMsg, result, true);

						if (replyMsg.OrderState == OrderStates.Active)
						{
							_activeOrders.Add(replyMsg.TransactionId, replyMsg);

							if (replyMsg.ExpiryDate != DateTimeOffset.MinValue && replyMsg.ExpiryDate != DateTimeOffset.MaxValue)
								_expirableOrders.Add(replyMsg, replyMsg.ExpiryDate.EndOfDay() - replyMsg.LocalTime);

							// изменяем текущие котировки, добавляя туда наши цену и объем
							UpdateQuote(replyMsg, true);

							// отправляем измененный стакан
							result.Add(CreateQuoteMessage(
								replyMsg.SecurityId,
								time,
								time));
						}
					}
					else
					{
						replyMsg.OrderState = OrderStates.Failed;
						replyMsg.Error = new InvalidOperationException(message);
						result.Add(replyMsg);

						this.AddInfoLog(LocalizedStrings.Str1158Params, execution.TransactionId, message);
					}
				}
			}

			private QuoteChangeMessage CreateQuoteMessage(SecurityId securityId, DateTime timeStamp, DateTimeOffset time)
			{
				return new QuoteChangeMessage
				{
					SecurityId = securityId,
					LocalTime = timeStamp,
					ServerTime = time,
					Bids = BuildQuoteChanges(_bids),
					Asks = BuildQuoteChanges(_asks),
					IsSorted = true,
				};
			}

			private static IEnumerable<QuoteChange> BuildQuoteChanges(SortedDictionary<decimal, RefPair<List<ExecutionMessage>, QuoteChange>> quotes)
			{
				return quotes.Count == 0
					? Enumerable.Empty<QuoteChange>()
					: quotes.Select(p => p.Value.Second.Clone()).ToArray();
			}

			private void UpdateQuotes(ExecutionMessage message, ICollection<Message> result)
			{
				// матчинг заявок происходит не только для своих сделок, но и для чужих.
				// различие лишь в том, что для чужих заявок не транслируется информация о сделках.
				// матчинг чужих заявок на равне со своими дает наиболее реалистичный сценарий обновления стакана.

				if (message.TradeId != 0)
					throw new ArgumentException(LocalizedStrings.Str1159, "message");

				if (message.Volume <= 0)
					throw new ArgumentOutOfRangeException("message", message.Volume, LocalizedStrings.Str1160Params.Put(message.TransactionId));

				UpdateQuote(message, !message.IsCancelled);

				if (_activeOrders.Count > 0 && !message.IsCancelled)
				{
					foreach (var order in _activeOrders.Values.ToArray())
					{
						MatchOrder(message.LocalTime, order, result, false);

						if (order.OrderState != OrderStates.Done)
							continue;

						_activeOrders.Remove(order.TransactionId);
						_expirableOrders.Remove(order);

						// изменяем текущие котировки, удаляя оттуда наши цену и объем
						UpdateQuote(order, false);
					}
				}

				//для чужих FOK заявок необходимо убрать ее из стакана после исполнения своих заявок
				if (message.TimeInForce == TimeInForce.MatchOrCancel && !message.IsCancelled)
				{
					UpdateQuote(new ExecutionMessage
					{
						ExecutionType = ExecutionTypes.Order,
						Side = message.Side,
						Price = message.Price,
						Volume = message.Volume
					}, false);
				}

				// для чужих заявок заполняется только объем
				message.Balance = message.Volume;

				// исполняем чужую заявку как свою. при этом результат выполнения не идет никуда
				MatchOrder(message.LocalTime, message, null, true);
			}

			private SortedDictionary<decimal, RefPair<List<ExecutionMessage>, QuoteChange>> GetQuotes(Sides side)
			{
				switch (side)
				{
					case Sides.Buy:
						return _bids;
					case Sides.Sell:
						return _asks;
					default:
						throw new ArgumentOutOfRangeException("side");
				}
				//return _quotes.SafeAdd(side, key => new SortedDictionary<decimal, List<ExecutionMessage>>(side == Sides.Buy ? new BackwardComparer<decimal>() : null));
			}

			private void MatchOrder(DateTime time, ExecutionMessage order, ICollection<Message> result, bool isNewOrder)
			{
				string matchError = null;

				var executions = result == null ? null : new Dictionary<decimal, decimal>();

				var quotes = GetQuotes(order.Side.Invert());

				var leftBalance = order.Balance;
				var sign = order.Side == Sides.Buy ? 1 : -1;

				foreach (var pair in quotes.ToArray())
				{
					var price = pair.Key;
					var levelQuotes = pair.Value.First;

					// для старых заявок, когда стакан пробивает уровень заявки,
					// матчим по цене ранее выставленной заявки.
					var execPrice = isNewOrder ? price : order.Price;

					foreach (var quote in levelQuotes.ToArray())
					{
						if (order.OrderType != OrderTypes.Market)
						{
							if (sign * price > sign * order.Price)
								break;

							if (price == order.Price && !_parent._settings.MatchOnTouch)
								break;
						}

						// если это пользовательская заявка и матчинг идет о заявку с таким же портфелем
						if (executions != null && quote.PortfolioName == order.PortfolioName)
						{
							matchError = LocalizedStrings.Str1161Params.Put(quote.TransactionId, order.TransactionId);
							this.AddErrorLog(matchError);
							break;
						}

						var volume = quote.Balance.Min(leftBalance);

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
							levelQuotes.Remove(quote);
							_messagePool.Free(quote);

							if (levelQuotes.Count == 0)
								quotes.Remove(price);
						}

						AddTotalVolume(order.Side.Invert(), -volume);
						pair.Value.Second.Volume -= volume;
						leftBalance -= volume;

						if (leftBalance == 0)
							break;
					}

					if (leftBalance == 0 || matchError != null)
						break;
				}

				// если это не пользовательская заявка
				if (result == null)
					return;

				leftBalance = order.Balance - executions.Values.Sum();

				switch (order.TimeInForce)
				{
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

							result.Add(ToOrder(time, order));
						}
							
						if (order.OrderType == OrderTypes.Market)
						{
							if (leftBalance > 0)
							{
								this.AddInfoLog(LocalizedStrings.Str1165Params, order.TransactionId, leftBalance);

								order.OrderState = OrderStates.Done;
								result.Add(ToOrder(time, order));	
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
						result.Add(ToOrder(time, order));

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
						result.Add(ToOrder(time, order));
						break;
					}
				}

				if (matchError != null)
				{
					var reply = CreateReply(order, time);

					reply.OrderState = OrderStates.Failed;
					reply.OrderStatus = OrderStatus.RejectedBySystem;
					reply.Error = new InvalidOperationException(matchError);

					result.Add(reply);
				}

				foreach (var execution in executions)
				{
					var tradeMsg = ToMyTrade(time, order, execution.Key, execution.Value);
					result.Add(tradeMsg);

					this.AddInfoLog(LocalizedStrings.Str1168Params, tradeMsg.TradeId, tradeMsg.OriginalTransactionId, execution.Key, execution.Value);

					var info = _parent.GetPortfolioInfo(order.PortfolioName);

					info.ProcessMyTrade(tradeMsg, result);

					var tickTime = (_parent.Settings.ConvertTime) ? ConvertTime(time, tradeMsg.SecurityId) : time;

					result.Add(new ExecutionMessage
					{
						LocalTime = tickTime,
						SecurityId = tradeMsg.SecurityId,
						TradeId = tradeMsg.TradeId,
						TradePrice = tradeMsg.TradePrice,
						Volume = tradeMsg.Volume,
						ExecutionType = ExecutionTypes.Tick,
						ServerTime = tickTime,
					});
				}
			}

			private void ProcessTime(Message message, ICollection<Message> result)
			{
				ProcessExpirableOrders(message, result);
				ProcessPendingExecutions(message, result);
				ProcessCandleTrades(message, result);
				ProcessGenerators(message, result);
			}

			private void ProcessGenerators(Message message, ICollection<Message> result)
			{
				ProcessGenerator(_tradeGenerator, message, result);
				ProcessGenerator(_olGenerator, message, result);
				ProcessGenerator(_depthGenerator, message, result);
			}

			private void ProcessGenerator(MarketDataGenerator generator, Message message, ICollection<Message> result)
			{
				if (generator == null)
					return;

				var msg = generator.Process(message);

				if (msg == null)
					return;

				result.Add(msg);
				Process(msg, result);
			}

			private void ProcessCandleTrades(Message message, ICollection<Message> result)
			{
				foreach (var pair in _candleInfo.ToArray())
				{
					if (pair.Key < message.LocalTime)
					{
						_candleInfo.Remove(pair.Key);

						foreach (var trade in pair.Value.Item2)
							Process(trade, result);

						// добавляем сами свечи
						// esper. эти данные уходят в BaseTrader, а он не умеет работать со свечами, а в EmuTrader свеча уже обработана.
						//result.AddRange(pair.Value.Item1);
					}
				}
			}

			private void ProcessExpirableOrders(Message message, ICollection<Message> result)
			{
				var diff = message.LocalTime - _prevTime;

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
						result.Add(ToOrder(message.LocalTime, orderMsg));

						// изменяем текущие котировки, удаляя оттуда наши цену и объем
						UpdateQuote(orderMsg, false);

						// отправляем измененный стакан
						result.Add(CreateQuoteMessage(
							orderMsg.SecurityId,
							message.LocalTime,
							message.LocalTime));
					}
					else
						_expirableOrders[orderMsg] = left;
				}
			}

			private void UpdateQuote(ExecutionMessage message, bool register)
			{
				var quotes = GetQuotes(message.Side);

				var pair = quotes.TryGetValue(message.Price);

				if (pair == null)
				{
					if (!register)
						return;

					quotes[message.Price] = pair = new RefPair<List<ExecutionMessage>, QuoteChange>(new List<ExecutionMessage>(), new QuoteChange(message.Side, message.Price, 0));
				}

				var level = pair.First;

				if (register)
				{
					//если пришло увеличение объема на уровне, то всегда добавляем в конец очереди, даже для диффа стаканов
					//var clone = (ExecutionMessage)message.Clone();
					var clone = _messagePool.Allocate<ExecutionMessage>(MessageTypes.Execution);
					
					clone.TransactionId = message.TransactionId;
					clone.Price = message.Price;
					clone.PortfolioName = message.PortfolioName;
					clone.Balance = message.Volume;
					clone.Volume = message.Volume;

					AddTotalVolume(message.Side, message.Volume);

					pair.Second.Volume += message.Volume;
					level.Add(clone);
				}
				else
				{
					if (message.TransactionId == 0)
					{
						var leftBalance = message.Volume;

						// пришел дифф по стакану - начиная с конца убираем снятый объем
						for (var i = level.Count - 1; i >= 0 && leftBalance > 0; i--)
						{
							var msg = level[i];

							if (msg.TransactionId != message.TransactionId)
								continue;

							leftBalance -= msg.Balance;

							if (leftBalance < 0)
							{
								//var clone = (ExecutionMessage)message.Clone();
								var clone = _messagePool.Allocate<ExecutionMessage>(MessageTypes.Execution);

								clone.TransactionId = message.TransactionId;
								clone.Price = message.Price;
								clone.PortfolioName = message.PortfolioName;
								clone.Balance = leftBalance.Abs();
								clone.Volume = message.Volume;

								var diff = clone.Balance - msg.Balance;
								AddTotalVolume(message.Side, diff);
								pair.Second.Volume += diff;

								level[i] = clone;
								break;
							}

							AddTotalVolume(message.Side, -msg.Balance);

							pair.Second.Volume -= msg.Balance;
							level.RemoveAt(i);
							_messagePool.Free(msg);
						}
					}
					else
					{
						var quote = level.FirstOrDefault(i => i.TransactionId == message.TransactionId);

						//TODO при перерегистрации номер транзакции может совпадать для двух заявок
						//if (quote == null)
						//	throw new InvalidOperationException("Котировка для отмены с номером транзакции {0} не найдена.".Put(message.TransactionId));

						if (quote != null)
						{
							AddTotalVolume(message.Side, -quote.Balance);

							pair.Second.Volume -= quote.Balance;
							level.Remove(quote);
							_messagePool.Free(quote);
						}
					}

					if (level.Count == 0)
						quotes.Remove(message.Price);
				}
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

			private void ProcessPendingExecutions(Message message, ICollection<Message> result)
			{
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

			private ExecutionMessage ToOrder(DateTime time, ExecutionMessage message)
			{
				if (_parent.Settings.ConvertTime)
					time = ConvertTime(time, message.SecurityId);

				return new ExecutionMessage
				{
					LocalTime = time,
					SecurityId = message.SecurityId,
					OrderId = message.OrderId,
					OriginalTransactionId = message.TransactionId,
					Balance = message.Balance,
					OrderState = message.OrderState,
					PortfolioName = message.PortfolioName,
					ExecutionType = ExecutionTypes.Order,
					ServerTime = time,
				};
			}

			private ExecutionMessage ToMyTrade(DateTime time, ExecutionMessage message, decimal price, decimal volume)
			{
				if (_parent.Settings.ConvertTime)
					time = ConvertTime(time, message.SecurityId);

				return new ExecutionMessage
				{
					LocalTime = time,
					SecurityId = message.SecurityId,
					OrderId = message.OrderId,
					OriginalTransactionId = message.TransactionId,
					TradeId = _parent._tradeIdGenerator.GetNextId(),
					TradePrice = price,
					Volume = volume,
					ExecutionType = ExecutionTypes.Trade,
					ServerTime = time,
					Side = message.Side,
				};
			}

			private DateTime ConvertTime(DateTime time, SecurityId securityId)
			{
				var board = _parent._boardDefinitions.TryGetValue(securityId.BoardCode);

				if (board == null)
					return time;

				var sourceZone = time.Kind == DateTimeKind.Utc ? TimeZoneInfo.Utc : TimeZoneInfo.Local;

				return TimeZoneInfo.ConvertTime(time, sourceZone, board.TimeZoneInfo);
			}

			public decimal? GetBestPrice(Sides side)
			{
				var quotes = side == Sides.Buy ? _asks : _bids;
				var pair = quotes.FirstOr();

				if (pair == null)
					return null;

				return pair.Value.Key;
			}
		}

		private sealed class PortfolioEmulator
		{
			private readonly MarketEmulator _parent;
			private readonly string _name;
			private readonly PortfolioPnLManager _pnLManager;
			private readonly Dictionary<SecurityId, RefPair<decimal, decimal>> _positions;

			private decimal _beginValue;
			private decimal _currentValue;
			private decimal _blockedValue;

			public PortfolioPnLManager PnLManager { get { return _pnLManager; } }

			public PortfolioEmulator(MarketEmulator parent, string name)
			{
				_parent = parent;
				_name = name;

				_pnLManager = new PortfolioPnLManager(name);
				_positions = new Dictionary<SecurityId, RefPair<decimal, decimal>>();
			}

			public void ProcessPositionChange(PositionChangeMessage posMsg, ICollection<Message> result)
			{
				var beginValue = (posMsg.Changes.TryGetValue(PositionChangeTypes.BeginValue) ?? 0m).To<decimal>();

				if (!_positions.ContainsKey(posMsg.SecurityId))
				{
					result.Add(new PositionMessage
					{
						SecurityId = posMsg.SecurityId,
						PortfolioName = posMsg.PortfolioName,
						DepoName = posMsg.DepoName,
						LocalTime = posMsg.LocalTime
					});
				}

				_positions[posMsg.SecurityId] = new RefPair<decimal, decimal>(beginValue, 0);

				if (beginValue == 0m)
					return;

				result.Add(posMsg.Clone());

				var reqMoney = GetRequiredMoney(posMsg.SecurityId, beginValue > 0 ? Sides.Buy : Sides.Sell);

				_blockedValue += reqMoney * beginValue.Abs();

				result.Add(
					new PortfolioChangeMessage
					{
						ServerTime = posMsg.ServerTime,
						LocalTime = posMsg.LocalTime,
						PortfolioName = _name,
					}.Add(PositionChangeTypes.BlockedValue, _blockedValue)
				);
			}

			public void ProcessPortfolioChange(PortfolioChangeMessage pfChangeMsg, ICollection<Message> result)
			{
				var beginValue = pfChangeMsg.Changes.TryGetValue(PositionChangeTypes.BeginValue);

				if (beginValue == null)
					return;

				_currentValue = _beginValue = (decimal)beginValue;

				result.Add(CreateChangeMessage(pfChangeMsg.ServerTime));
			}

			public decimal? ProcessOrder(ExecutionMessage orderMsg, bool register, ICollection<Message> result)
			{
				var reqMoney = GetRequiredMoney(orderMsg.SecurityId, orderMsg.Side, orderMsg.Price);

				var pos = _positions.SafeAdd(orderMsg.SecurityId, k => new RefPair<decimal, decimal>(0, 0));

				var sign = orderMsg.Side == Sides.Buy ? 1 : -1;
				var totalPos = pos.First + pos.Second;

				if (register)
					pos.Second += orderMsg.Volume * sign;
				else
					pos.Second -= orderMsg.Balance * sign;

				var commission = _parent._commissionManager.ProcessExecution(orderMsg);

				_blockedValue += ((pos.First + pos.Second).Abs() - totalPos.Abs()) * reqMoney;

				result.Add(CreateChangeMessage(orderMsg.ServerTime));

				return commission;
			}

			public void ProcessMyTrade(ExecutionMessage tradeMsg, ICollection<Message> result)
			{
				var time = tradeMsg.ServerTime;

				PnLInfo info;
				_pnLManager.ProcessMyTrade(tradeMsg, out info);
				tradeMsg.Commission = _parent._commissionManager.ProcessExecution(tradeMsg);

				bool isNew;
				var pos = _positions.SafeAdd(tradeMsg.SecurityId, k => new RefPair<decimal, decimal>(0, 0), out isNew);

				var totalPos = pos.First + pos.Second;
				var positionChange = tradeMsg.GetPosition();
				var reqMoney = GetRequiredMoney(tradeMsg.SecurityId, tradeMsg.Side);
				
				pos.First += positionChange;
				pos.Second -= positionChange;

				_blockedValue += ((pos.First + pos.Second).Abs() - totalPos.Abs()) * reqMoney;

				if (isNew)
				{
					result.Add(new PositionMessage
					{
						LocalTime = time.LocalDateTime,
						PortfolioName = _name,
						SecurityId = tradeMsg.SecurityId,
					});
				}

				result.Add(
					new PositionChangeMessage
					{
						LocalTime = time.LocalDateTime,
						ServerTime = time,
						PortfolioName = _name,
						SecurityId = tradeMsg.SecurityId,
					}
					.Add(PositionChangeTypes.CurrentValue, pos.First));

				result.Add(CreateChangeMessage(time));
			}

			public void ProcessMarginChange(DateTime time, SecurityId securityId, ICollection<Message> result)
			{
				var pos = _positions.TryGetValue(securityId);

				if (pos == null)
					return;

				_blockedValue = 0;

				foreach (var position in _positions)
				{
					var value = position.Value.First + position.Value.Second;
					var reqMoney = GetRequiredMoney(position.Key, value > 0 ? Sides.Buy : Sides.Sell);

					_blockedValue += reqMoney * value.Abs();
				}

				result.Add(
					new PortfolioChangeMessage
					{
						ServerTime = time,
						LocalTime = time,
						PortfolioName = _name,
					}.Add(PositionChangeTypes.BlockedValue, _blockedValue)
				);
			}

			public PortfolioChangeMessage CreateChangeMessage(DateTimeOffset time)
			{
				var realizedPnL = _pnLManager.RealizedPnL;
				var unrealizedPnL = _pnLManager.UnrealizedPnL;
				var commission = _parent._commissionManager.Commission;
				var totalPnL = realizedPnL + unrealizedPnL - commission;

				_currentValue = _beginValue + totalPnL;

				return new PortfolioChangeMessage
				{
					ServerTime = time,
					LocalTime = time.LocalDateTime,
					PortfolioName = _name,
				}
				.Add(PositionChangeTypes.RealizedPnL, realizedPnL)
				.Add(PositionChangeTypes.UnrealizedPnL, unrealizedPnL)
				.Add(PositionChangeTypes.VariationMargin, totalPnL)
				.Add(PositionChangeTypes.CurrentValue, _currentValue)
				.Add(PositionChangeTypes.BlockedValue, _blockedValue)
				.Add(PositionChangeTypes.Commission, commission);
			}

			public string CheckRegistration(ExecutionMessage execMsg)
			{
				var reqMoney = GetRequiredMoney(execMsg.SecurityId, execMsg.Side, execMsg.Price);

				// если задан баланс, то проверям по нему (для частично исполненных заявок)
				var volume = execMsg.Balance != 0 ? execMsg.Balance : execMsg.Volume;

				var pos = _positions.SafeAdd(execMsg.SecurityId, k => new RefPair<decimal, decimal>(0, 0));

				var sign = execMsg.Side == Sides.Buy ? 1 : -1;
				var totalPos = pos.First + pos.Second;

				var needBlock = ((totalPos + volume * sign).Abs() - totalPos.Abs()) * reqMoney;

				// при отрицательном значении снимается часть блокировки
				if (needBlock < 0)
					return null;

				if ((_currentValue - _blockedValue) < needBlock)
				{
					return LocalizedStrings.Str1169Params
							.Put(execMsg.PortfolioName, execMsg.TransactionId, needBlock, _currentValue, _blockedValue);
				}

				return null;
			}

			private decimal GetRequiredMoney(SecurityId securityId, Sides side, decimal price = 0)
			{
				var state = _parent._secStates.TryGetValue(securityId);

				var reqMoney = state == null ? null : (decimal?)state.TryGetValue(side == Sides.Buy ? Level1Fields.MarginBuy : Level1Fields.MarginSell);

				// отсутствует информация о ГО
				if (reqMoney == null)
				{
					if (price != 0)
						reqMoney = price;
					else
					{
						var secEmu = _parent._securityEmulators.TryGetValue(securityId);
						reqMoney = secEmu.GetBestPrice(side) ?? 0;
					}
				}

				return reqMoney.Value;
			}
		}

		private IncrementalIdGenerator _orderIdGenerator = new IncrementalIdGenerator();
		private IncrementalIdGenerator _tradeIdGenerator = new IncrementalIdGenerator();
		private readonly Dictionary<SecurityId, SecurityMarketEmulator> _securityEmulators = new Dictionary<SecurityId, SecurityMarketEmulator>();
		private readonly Dictionary<string, PortfolioEmulator> _portfolios = new Dictionary<string, PortfolioEmulator>();
		private readonly Dictionary<string, BoardMessage> _boardDefinitions = new Dictionary<string, BoardMessage>(StringComparer.InvariantCultureIgnoreCase);
		private readonly Dictionary<SecurityId, Dictionary<Level1Fields, object>> _secStates = new Dictionary<SecurityId, Dictionary<Level1Fields, object>>();
		private bool? _needBuffer;
		private readonly List<Message> _buffer = new List<Message>();
		private DateTime _bufferPrevFlush;
		private DateTime _portfoliosPrevRecalc;
		private readonly ICommissionManager _commissionManager = new CommissionManager();

		/// <summary>
		/// Создать <see cref="MarketEmulator"/>.
		/// </summary>
		public MarketEmulator()
		{
		}

		private readonly MarketEmulatorSettings _settings = new MarketEmulatorSettings();

		/// <summary>
		/// Настройки эмулятора.
		/// </summary>
		public MarketEmulatorSettings Settings
		{
			get { return _settings; }
		}

		/// <summary>
		/// Генератор номеров для заявок.
		/// </summary>
		public IncrementalIdGenerator OrderIdGenerator
		{
			get { return _orderIdGenerator; }
			set { _orderIdGenerator = value; }
		}

		/// <summary>
		/// Генератор номеров для сделок.
		/// </summary>
		public IncrementalIdGenerator TradeIdGenerator
		{
			get { return _tradeIdGenerator; }
			set { _tradeIdGenerator = value; }
		}

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		public void SendInMessage(Message message)
		{
			if (message == null) 
				throw new ArgumentNullException("message");

			var retVal = new List<Message>();

			switch (message.Type)
			{
				case MessageTypes.Time:
				{
					foreach (var securityEmulator in _securityEmulators.Values)
						retVal.AddRange(securityEmulator.Process(message));

					// время у TimeMsg может быть больше времени сообщений из эмулятора
					retVal.Add(message);

					break;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;
					retVal.AddRange(GetEmulator(execMsg.SecurityId).Process(message));
					break;
				}

				case MessageTypes.QuoteChange:
				{
					var quoteMsg = (QuoteChangeMessage)message;
					retVal.AddRange(GetEmulator(quoteMsg.SecurityId).Process(message));
					break;
				}

				case MessageTypes.OrderRegister:
				case MessageTypes.OrderReplace:
				case MessageTypes.OrderCancel:
				{
					var orderMsg = (OrderMessage)message;
					retVal.AddRange(GetEmulator(orderMsg.SecurityId).Process(message));
					break;
				}

				case ExtendedMessageTypes.Reset:
				{
					_securityEmulators.Clear();

					_orderIdGenerator.Current = _settings.InitialOrderId;
					_tradeIdGenerator.Current = _settings.InitialTradeId;

					_portfolios.Clear();
					_boardDefinitions.Clear();

					_secStates.Clear();

					_buffer.Clear();
					_needBuffer = null;

					_bufferPrevFlush = default(DateTime);
					_portfoliosPrevRecalc = default(DateTime);
					break;
				}

				case ExtendedMessageTypes.Clearing:
				{
					var clearingMsg = (ClearingMessage)message;
					var emu = _securityEmulators.TryGetValue(clearingMsg.SecurityId);
					
					if (emu != null)
					{
						_securityEmulators.Remove(clearingMsg.SecurityId);
						emu.Parent = this;
					}

					break;
				}

				case MessageTypes.PortfolioChange:
				{
					var pfChangeMsg = (PortfolioChangeMessage)message;
					GetPortfolioInfo(pfChangeMsg.PortfolioName).ProcessPortfolioChange(pfChangeMsg, retVal);
					break;
				}

				case MessageTypes.PositionChange:
				{
					var posChangeMsg = (PositionChangeMessage)message;
					GetPortfolioInfo(posChangeMsg.PortfolioName).ProcessPositionChange(posChangeMsg, retVal);
					break;
				}

				case MessageTypes.Board:
				{
					var boardMsg = (BoardMessage)message;
					_boardDefinitions[boardMsg.Code] = (BoardMessage)boardMsg.Clone();

					foreach (var securityEmulator in _securityEmulators)
					{
						if (securityEmulator.Key.BoardCode.CompareIgnoreCase(boardMsg.Code))
							securityEmulator.Value.Process(boardMsg);
					}

					break;
				}

				case MessageTypes.Level1Change:
				{
					var level1Msg = (Level1ChangeMessage)message;

					retVal.AddRange(GetEmulator(level1Msg.SecurityId).Process(message));

					UpdateLevel1Info(level1Msg, retVal);
					break;
				}

				case MessageTypes.CandleTimeFrame:
				case MessageTypes.CandlePnF:
				case MessageTypes.CandleRange:
				case MessageTypes.CandleRenko:
				case MessageTypes.CandleTick:
				case MessageTypes.CandleVolume:
				{
					var candleMsg = (CandleMessage)message;
					retVal.AddRange(GetEmulator(candleMsg.SecurityId).Process(candleMsg));
					break;
				}

				case MessageTypes.Portfolio:
				{
					var pfMsg = (PortfolioMessage)message;

					retVal.Add(pfMsg);
					//GetPortfolioInfo(pfMsg.PortfolioName);
					
					break;
				}

				case MessageTypes.PortfolioLookup:
				{
					var pfMsg = (PortfolioLookupMessage)message;

					retVal.Add(new PortfolioLookupResultMessage { OriginalTransactionId = pfMsg.TransactionId });

					break;
				}

				case MessageTypes.Security:
				{
					var secMsg = (SecurityMessage)message;
					retVal.Add(secMsg);
					retVal.AddRange(GetEmulator(secMsg.SecurityId).Process(secMsg));
					break;
				}

				case ExtendedMessageTypes.Generator:
				{
					var generatorMsg = (GeneratorMessage)message;
					retVal.AddRange(GetEmulator(generatorMsg.SecurityId).Process(generatorMsg));
					break;
				}

				case ExtendedMessageTypes.CommissionRule:
				{
					var ruleMsg = (CommissionRuleMessage)message;
					_commissionManager.Rules.Add(ruleMsg.Rule);
					break;
				}

				default:
					retVal.Add(message);
					break;
			}

			RecalcPnL(retVal);

			BufferResult(retVal, message.LocalTime).ForEach(RaiseNewOutMessage);
		}

		/// <summary>
		/// Событие появления нового сообщения.
		/// </summary>
		public event Action<Message> NewOutMessage;

		private void RaiseNewOutMessage(Message message)
		{
			NewOutMessage.SafeInvoke(message);
		}

		private SecurityMarketEmulator GetEmulator(SecurityId securityId)
		{
			return _securityEmulators.SafeAdd(securityId, key =>
				new SecurityMarketEmulator(this, securityId) { Parent = this });
		}

		private IEnumerable<Message> BufferResult(IEnumerable<Message> result, DateTime time)
		{
			if (_needBuffer == null)
				_needBuffer = _settings.BufferTime > TimeSpan.Zero;

			if (_needBuffer == false)
				return result;

			_buffer.AddRange(result);

			if ((time - _bufferPrevFlush) > _settings.BufferTime)
			{
				_bufferPrevFlush = time;
				return _buffer.CopyAndClear();
			}
			else
			{
				return Enumerable.Empty<Message>();
			}
		}

		private PortfolioEmulator GetPortfolioInfo(string portfolioName)
		{
			bool isNew;
			return _portfolios.SafeAdd(portfolioName, key => new PortfolioEmulator(this, key), out isNew);
		}

		private void UpdateLevel1Info(Level1ChangeMessage level1Msg, ICollection<Message> retVal)
		{
			var marginChanged = false;
			var state = _secStates.SafeAdd(level1Msg.SecurityId);

			foreach (var change in level1Msg.Changes)
			{
				switch (change.Key)
				{
					case Level1Fields.MinPrice:
					case Level1Fields.MaxPrice:
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

			retVal.Add(level1Msg);

			if (!marginChanged)
				return;

			foreach (var info in _portfolios.Values)
				info.ProcessMarginChange(level1Msg.LocalTime, level1Msg.SecurityId, retVal);
		}

		private string CheckRegistration(ExecutionMessage execMsg)
		{
			var board = _boardDefinitions.TryGetValue(execMsg.SecurityId.BoardCode);

			if (board != null)
			{
				if (execMsg.OrderType == OrderTypes.Market && !board.IsSupportMarketOrders)
					return LocalizedStrings.Str1170Params.Put(board.Code);

				if (!board.WorkingTime.IsTradeTime(execMsg.ServerTime.Convert(board.TimeZoneInfo).DateTime))
					return LocalizedStrings.Str1171;
			}

			var state = _secStates.TryGetValue(execMsg.SecurityId);

			if (state != null && execMsg.OrderType != OrderTypes.Market)
			{
				var minPrice = (decimal?)state.TryGetValue(Level1Fields.MinPrice);
				var maxPrice = (decimal?)state.TryGetValue(Level1Fields.MaxPrice);

				if (minPrice != null && minPrice > 0 && execMsg.Price < minPrice)
					return LocalizedStrings.Str1172Params.Put(execMsg.Price, execMsg.TransactionId, minPrice);

				if (maxPrice != null && maxPrice > 0 && execMsg.Price > maxPrice)
					return LocalizedStrings.Str1173Params.Put(execMsg.Price, execMsg.TransactionId, maxPrice);
			}

			var info = GetPortfolioInfo(execMsg.PortfolioName);

			return info.CheckRegistration(execMsg);
		}

		private void RecalcPnL(ICollection<Message> messages)
		{
			if (_settings.PortfolioRecalcInterval == TimeSpan.Zero)
				return;

			var time = _portfoliosPrevRecalc;

			foreach (var message in messages)
			{
				foreach (var emulator in _portfolios.Values)
					emulator.PnLManager.ProcessMessage(message);

				time = message.LocalTime;
			}

			if (time - _portfoliosPrevRecalc <= _settings.PortfolioRecalcInterval)
				return;

			foreach (var emulator in _portfolios.Values)
				messages.Add(emulator.CreateChangeMessage(time));

			_portfoliosPrevRecalc = time;
		}
	}
}
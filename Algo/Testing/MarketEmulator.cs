#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Testing.Algo
File: MarketEmulator.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Testing
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo.Commissions;
	using StockSharp.Algo.PnL;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Algo.Candles;
	using StockSharp.Localization;
	using StockSharp.BusinessEntities;
	using StockSharp.Algo.Storages;

	/// <summary>
	/// Emulator.
	/// </summary>
	public class MarketEmulator : BaseLogReceiver, IMarketEmulator
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

		private sealed class SecurityMarketEmulator : BaseLogReceiver//, IMarketEmulator
		{
			private readonly MarketEmulator _parent;
			private readonly SecurityId _securityId;

			private readonly Dictionary<ExecutionMessage, TimeSpan> _expirableOrders = new Dictionary<ExecutionMessage, TimeSpan>();
			private readonly Dictionary<long, ExecutionMessage> _activeOrders = new Dictionary<long, ExecutionMessage>();
			private readonly SortedDictionary<decimal, RefPair<LevelQuotes, QuoteChange>> _bids = new SortedDictionary<decimal, RefPair<LevelQuotes, QuoteChange>>(new BackwardComparer<decimal>());
			private readonly SortedDictionary<decimal, RefPair<LevelQuotes, QuoteChange>> _asks = new SortedDictionary<decimal, RefPair<LevelQuotes, QuoteChange>>();
			private readonly Dictionary<ExecutionMessage, TimeSpan> _pendingExecutions = new Dictionary<ExecutionMessage, TimeSpan>();
			private DateTimeOffset _prevTime;
			private readonly ExecutionLogConverter _execLogConverter;
			private int _volumeDecimals;
			private readonly SortedDictionary<DateTimeOffset, Tuple<List<CandleMessage>, List<ExecutionMessage>>> _candleInfo = new SortedDictionary<DateTimeOffset, Tuple<List<CandleMessage>, List<ExecutionMessage>>>();
			private LogLevels? _logLevel;
			private DateTime _lastStripDate;

			private decimal _totalBidVolume;
			private decimal _totalAskVolume;

			private readonly MessagePool<ExecutionMessage> _messagePool = new MessagePool<ExecutionMessage>();

			public SecurityMarketEmulator(MarketEmulator parent, SecurityId securityId)
			{
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
				_securityId = securityId;
				_execLogConverter = new ExecutionLogConverter(securityId, _bids, _asks, _parent.Settings, GetServerTime);
			}

			private SecurityMessage _securityDefinition;

			public SecurityMessage SecurityDefinition => _securityDefinition;

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
							case ExecutionTypes.Transaction:
							{
								if (!execMsg.HasOrderInfo())
									throw new InvalidOperationException();

								if (_parent.Settings.Latency > TimeSpan.Zero)
								{
									this.AddInfoLog(LocalizedStrings.Str1145Params, execMsg.IsCancellation ? LocalizedStrings.Str1146 : LocalizedStrings.Str1147, execMsg.TransactionId == 0 ? execMsg.OriginalTransactionId : execMsg.TransactionId);
									_pendingExecutions.Add(execMsg.TypedClone(), _parent.Settings.Latency);
								}
								else
									AcceptExecution(execMsg.LocalTime, execMsg, result);

								break;
							}
							case ExecutionTypes.OrderLog:
							{
								if (execMsg.TradeId == null)
									UpdateQuotes(execMsg, result);

								// добавляем в результат ОЛ только из хранилища или из генератора
								// (не из ExecutionLogConverter)
								//if (execMsg.TransactionId > 0)
								//	result.Add(execMsg);

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
						var oldOrder = _activeOrders.TryGetValue(orderMsg.OriginalTransactionId);

						foreach (var execMsg in _execLogConverter.ToExecutionLog(orderMsg, GetTotalVolume(orderMsg.Side.Invert())))
						{
							if (oldOrder != null)
							{
								if (!execMsg.IsCancellation && execMsg.OrderVolume == 0)
									execMsg.OrderVolume = oldOrder.Balance;

								Process(execMsg, result);
							}
							else if (execMsg.IsCancellation)
							{
								var error = LocalizedStrings.Str1148Params.Put(execMsg.OrderId);
								var serverTime = GetServerTime(orderMsg.LocalTime);

								// cancellation error
								result.Add(new ExecutionMessage
								{
									LocalTime = orderMsg.LocalTime,
									OriginalTransactionId = orderMsg.TransactionId,
									OrderId = execMsg.OrderId,
									ExecutionType = ExecutionTypes.Transaction,
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
									ExecutionType = ExecutionTypes.Transaction,
									SecurityId = orderMsg.SecurityId,
									IsCancellation = false,
									OrderState = OrderStates.Failed,
									Error = new InvalidOperationException(error),
									ServerTime = serverTime,
									HasOrderInfo = true,
								});

								this.AddErrorLog(LocalizedStrings.Str1148Params, orderMsg.OriginalTransactionId);
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
							result.Add(clone);

							if (finish)
								break;
						}

						break;
					}

					case MessageTypes.QuoteChange:
					{
						var quoteMsg = (QuoteChangeMessage)message;

						foreach (var m in _execLogConverter.ToExecutionLog(quoteMsg))
						{
							if (m.ExecutionType == ExecutionTypes.Tick)
							{
								m.ServerTime = quoteMsg.ServerTime;
								result.Add(m);
							}
							else
								Process(m, result);
						}

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
						_volumeDecimals = GetVolumeStep().GetCachedDecimals();
						_execLogConverter.UpdateSecurityDefinition(_securityDefinition);
						break;
					}

					case MessageTypes.Board:
					{
						//_execLogConverter.UpdateBoardDefinition((BoardMessage)message);
						break;
					}

					default:
					{
						if (message is CandleMessage candleMsg)
						{
							// в трейдах используется время открытия свечи, при разных MarketTimeChangedInterval и TimeFrame свечек
							// возможны ситуации, когда придет TimeMsg 11:03:00, а время закрытия будет 11:03:30
							// т.о. время уйдет вперед данных, которые построены по свечкам.
							var info = _candleInfo.SafeAdd(candleMsg.OpenTime, key => Tuple.Create(new List<CandleMessage>(), new List<ExecutionMessage>()));

							info.Item1.Add(candleMsg.TypedClone());

							if (_securityDefinition != null/* && _parent._settings.UseCandlesTimeFrame != null*/)
							{
								var trades = candleMsg.ToTrades(GetVolumeStep(), _volumeDecimals).ToArray();
								Process(trades[0], result);
								info.Item2.AddRange(trades.Skip(1));	
							}
						
							break;
						}

						throw new ArgumentOutOfRangeException();
					}
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
						price = execution.GetTradePrice();
						break;

					case ExecutionTypes.OrderLog:
						if (execution.TradePrice == null)
							return;

						price = execution.TradePrice.Value;
						break;

					default:
						return;
				}

				_lastStripDate = execution.LocalTime.Date;

				var priceOffset = _parent.Settings.PriceLimitOffset;
				var priceStep = _securityDefinition?.PriceStep ?? 0.01m;

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
							break;
						case Level1Fields.VolumeStep:
							_securityDefinition.VolumeStep = (decimal)change.Value;
							_volumeDecimals = GetVolumeStep().GetCachedDecimals();
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

				_execLogConverter.UpdateSecurityDefinition(_securityDefinition);
			}

			private decimal GetVolumeStep()
			{
				return _securityDefinition.VolumeStep ?? 1;
			}

			private static decimal ShrinkPrice(decimal price, decimal priceStep)
			{
				var decimals = priceStep.GetCachedDecimals();

				return price
					.Round(priceStep, decimals, null)
					.RemoveTrailingZeros();
			}

			private static ExecutionMessage CreateReply(ExecutionMessage original, DateTimeOffset time, Exception error)
			{
				var replyMsg = new ExecutionMessage
				{
					HasOrderInfo = true,
					ExecutionType = ExecutionTypes.Transaction,
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
				if (_parent.Settings.Failing > 0)
				{
					if (RandomGen.GetDouble() < (_parent.Settings.Failing / 100.0))
					{
						this.AddErrorLog(LocalizedStrings.Str1151Params, execution.IsCancellation ? LocalizedStrings.Str1152 : LocalizedStrings.Str1153, execution.OriginalTransactionId == 0 ? execution.TransactionId : execution.OriginalTransactionId);

						var replyMsg = CreateReply(execution, time, new InvalidOperationException(LocalizedStrings.Str1154));

						replyMsg.Balance = execution.OrderVolume;

						result.Add(replyMsg);
						return;
					}
				}

				if (execution.IsCancellation)
				{
					if (_activeOrders.TryGetAndRemove(execution.OriginalTransactionId, out var order))
					{
						_expirableOrders.Remove(order);

						// изменяем текущие котировки, добавляя туда наши цену и объем
						UpdateQuote(order, false);

						// отправляем измененный стакан
						result.Add(CreateQuoteMessage(
							order.SecurityId,
							time,
							GetServerTime(time)));

						var replyMsg = CreateReply(order, time, null);

						//replyMsg.OriginalTransactionId = execution.OriginalTransactionId;
						replyMsg.OrderState = OrderStates.Done;

						result.Add(replyMsg);

						this.AddInfoLog(LocalizedStrings.Str1155Params, execution.OriginalTransactionId);

						replyMsg.Commission = _parent
							.GetPortfolioInfo(execution.PortfolioName)
							.ProcessOrder(order, order.Balance.Value, result);
					}
					else
					{
						result.Add(CreateReply(execution, time, new InvalidOperationException(LocalizedStrings.Str1156Params.Put(execution.OriginalTransactionId))));

						this.AddErrorLog(LocalizedStrings.Str1156Params, execution.OriginalTransactionId);
					}
				}
				else
				{
					var message = _parent.CheckRegistration(execution, _securityDefinition/*, result*/);

					var replyMsg = CreateReply(execution, time, message == null ? null : new InvalidOperationException(message));
					result.Add(replyMsg);

					if (message == null)
					{
						this.AddInfoLog(LocalizedStrings.Str1157Params, execution.TransactionId);

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

						MatchOrder(execution.LocalTime, execution, result, true);

						if (execution.OrderState == OrderStates.Active)
						{
							_activeOrders.Add(execution.TransactionId, execution);

							if (execution.ExpiryDate != null)
								_expirableOrders.Add(execution, execution.ExpiryDate.Value.EndOfDay() - time);

							// изменяем текущие котировки, добавляя туда наши цену и объем
							UpdateQuote(execution, true);
						}
						else if (execution.IsCanceled())
						{
							_parent
								.GetPortfolioInfo(execution.PortfolioName)
								.ProcessOrder(execution, execution.Balance.Value, result);
						}

						// отправляем измененный стакан
						result.Add(CreateQuoteMessage(
							execution.SecurityId,
							time,
							GetServerTime(time)));
					}
					else
					{
						this.AddInfoLog(LocalizedStrings.Str1158Params, execution.TransactionId, message);
					}
				}
			}

			private QuoteChangeMessage CreateQuoteMessage(SecurityId securityId, DateTimeOffset timeStamp, DateTimeOffset time)
			{
				return new QuoteChangeMessage
				{
					SecurityId = securityId,
					LocalTime = timeStamp,
					ServerTime = time,
					Bids = BuildQuoteChanges(_bids),
					Asks = BuildQuoteChanges(_asks),
				};
			}

			private static QuoteChange[] BuildQuoteChanges(SortedDictionary<decimal, RefPair<LevelQuotes, QuoteChange>> quotes)
			{
				return quotes.Count == 0
					? ArrayHelper.Empty<QuoteChange>()
					: quotes.Select(p => p.Value.Second).ToArray();
			}

			private void UpdateQuotes(ExecutionMessage message, ICollection<Message> result)
			{
				// матчинг заявок происходит не только для своих сделок, но и для чужих.
				// различие лишь в том, что для чужих заявок не транслируется информация о сделках.
				// матчинг чужих заявок на равне со своими дает наиболее реалистичный сценарий обновления стакана.

				if (message.TradeId != null)
					throw new ArgumentException(LocalizedStrings.Str1159, nameof(message));

				if (message.OrderVolume == null || message.OrderVolume <= 0)
					throw new ArgumentOutOfRangeException(nameof(message), message.OrderVolume, LocalizedStrings.Str1160Params.Put(message.TransactionId));

				var isRegister = !message.IsCancellation;

				if (!isRegister)
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

						_activeOrders.Remove(order.TransactionId);
						_expirableOrders.Remove(order);

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
				//		ExecutionType = ExecutionTypes.Transaction,
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
				//return _quotes.SafeAdd(side, key => new SortedDictionary<decimal, List<ExecutionMessage>>(side == Sides.Buy ? new BackwardComparer<decimal>() : null));
			}

			private void MatchOrder(DateTimeOffset time, ExecutionMessage order, ICollection<Message> result, bool isNewOrder)
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

						if (price == orderPrice && !_parent.Settings.MatchOnTouch)
							break;
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

							result.Add(ToOrder(time, order));
						}
							
						if (isMarket)
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

				foreach (var execution in executions)
				{
					var tradeMsg = ToMyTrade(time, order, execution.Key, execution.Value);
					result.Add(tradeMsg);

					this.AddInfoLog(LocalizedStrings.Str1168Params, tradeMsg.TradeId, tradeMsg.OriginalTransactionId, execution.Key, execution.Value);

					var info = _parent.GetPortfolioInfo(order.PortfolioName);

					info.ProcessMyTrade(order.Side, tradeMsg, result);

					result.Add(new ExecutionMessage
					{
						LocalTime = time,
						SecurityId = tradeMsg.SecurityId,
						TradeId = tradeMsg.TradeId,
						TradePrice = tradeMsg.TradePrice,
						TradeVolume = tradeMsg.TradeVolume,
						ExecutionType = ExecutionTypes.Tick,
						ServerTime = GetServerTime(time),
					});
				}
			}

			private void ProcessTime(Message message, ICollection<Message> result)
			{
				ProcessExpirableOrders(message, result);
				ProcessPendingExecutions(message, result);
				ProcessCandleTrades(message, result);
			}

			private void ProcessCandleTrades(Message message, ICollection<Message> result)
			{
				if (_candleInfo.Count == 0)
					return;

				foreach (var pair in _candleInfo.ToArray())
				{
					if (pair.Key < message.LocalTime)
					{
						_candleInfo.Remove(pair.Key);

						foreach (var trade in pair.Value.Item2)
							result.Add(trade);

						// change current time before the candle will be processed
						result.Add(new TimeMessage { LocalTime = message.LocalTime });

						foreach (var candle in pair.Value.Item1)
						{
							candle.LocalTime = message.LocalTime;
							result.Add(candle);
						}
					}
				}
			}

			private void ProcessExpirableOrders(Message message, ICollection<Message> result)
			{
				if (_expirableOrders.Count == 0)
					return;

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
							GetServerTime(message.LocalTime)));
					}
					else
						_expirableOrders[orderMsg] = left;
				}
			}

			private void UpdateQuote(ExecutionMessage message, bool register, bool byVolume = true)
			{
				var quotes = GetQuotes(message.Side);

				var pair = quotes.TryGetValue(message.OrderPrice);

				if (pair == null)
				{
					if (!register)
						return;

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
					ServerTime = GetServerTime(time),
				};
			}

			private ExecutionMessage ToMyTrade(DateTimeOffset time, ExecutionMessage message, decimal price, decimal volume)
			{
				return new ExecutionMessage
				{
					LocalTime = time,
					SecurityId = message.SecurityId,
					OrderId = message.OrderId,
					OriginalTransactionId = message.TransactionId,
					TradeId = _parent.TradeIdGenerator.GetNextId(),
					TradePrice = price,
					TradeVolume = volume,
					ExecutionType = ExecutionTypes.Transaction,
					HasTradeInfo = true,
					ServerTime = GetServerTime(time),
					Side = message.Side,
				};
			}

			private DateTimeOffset GetServerTime(DateTimeOffset time)
			{
				if (!_parent.Settings.ConvertTime)
					return time;

				var destTimeZone = _parent.Settings.TimeZone;

				if (destTimeZone == null)
				{
					var board = _parent._boardDefinitions.TryGetValue(_securityId.BoardCode);

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
				return (decimal?)_parent._secStates.TryGetValue(_securityId)?.TryGetValue(field) ?? GetQuotes(side).FirstOr()?.Key ?? 0;
			}
		}

		private sealed class PortfolioEmulator
		{
			private class MoneyInfo
			{
				private readonly SecurityMarketEmulator _secEmu;

				public MoneyInfo(SecurityMarketEmulator secEmu)
				{
					_secEmu = secEmu;
				}

				public decimal PositionBeginValue;
				public decimal PositionDiff;
				public decimal PositionCurrentValue => PositionBeginValue + PositionDiff;

				public decimal PositionAveragePrice;

				public decimal PositionPrice
				{
					get
					{
						var pos = PositionCurrentValue;

						if (pos == 0)
							return 0;

						return pos.Abs() * PositionAveragePrice;
					}
				}

				public decimal TotalPrice => GetPrice(0, 0);

				public decimal GetPrice(decimal buyVol, decimal sellVol)
				{
					var totalMoney = PositionPrice;

					var buyOrderPrice = (TotalBidsVolume + buyVol) * _secEmu.GetMarginPrice(Sides.Buy);
					var sellOrderPrice = (TotalAsksVolume + sellVol) * _secEmu.GetMarginPrice(Sides.Sell);

					if (totalMoney != 0)
					{
						if (PositionCurrentValue > 0)
						{
							totalMoney += buyOrderPrice;
							totalMoney = totalMoney.Max(sellOrderPrice);
						}
						else
						{
							totalMoney += sellOrderPrice;
							totalMoney = totalMoney.Max(buyOrderPrice);
						}
					}
					else
					{
						totalMoney = buyOrderPrice + sellOrderPrice;
					}

					return totalMoney;
				}

				public decimal TotalBidsVolume;
				public decimal TotalAsksVolume;
			}

			private readonly MarketEmulator _parent;
			private readonly string _name;
			private readonly Dictionary<SecurityId, MoneyInfo> _moneys = new Dictionary<SecurityId, MoneyInfo>();

			private decimal _beginMoney;
			private decimal _currentMoney;

			private decimal _totalBlockedMoney;

			public PortfolioPnLManager PnLManager { get; }

			public PortfolioEmulator(MarketEmulator parent, string name)
			{
				_parent = parent;
				_name = name;

				PnLManager = new PortfolioPnLManager(name);
			}

			public void RequestState(PortfolioLookupMessage pfMsg, ICollection<Message> result)
			{
				var time = pfMsg.LocalTime;

				AddPortfolioChangeMessage(time, result);

				foreach (var pair in _moneys)
				{
					var money = pair.Value;

					result.Add(
						new PositionChangeMessage
						{
							LocalTime = time,
							ServerTime = time,
							PortfolioName = _name,
							SecurityId = pair.Key,
							OriginalTransactionId = pfMsg.TransactionId,
						}
						.Add(PositionChangeTypes.CurrentValue, money.PositionCurrentValue)
						.TryAdd(PositionChangeTypes.AveragePrice, money.PositionAveragePrice)
					);
				}
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

				//if (!_moneys.ContainsKey(posMsg.SecurityId))
				//{
				//	result.Add(new PositionMessage
				//	{
				//		SecurityId = posMsg.SecurityId,
				//		PortfolioName = posMsg.PortfolioName,
				//		DepoName = posMsg.DepoName,
				//		LocalTime = posMsg.LocalTime
				//	});
				//}

				var money = GetMoney(posMsg.SecurityId/*, posMsg.LocalTime, result*/);

				var prevPrice = money.PositionPrice;

				money.PositionBeginValue = beginValue ?? 0L;
				money.PositionAveragePrice = posMsg.Changes.TryGetValue(PositionChangeTypes.AveragePrice).To<decimal?>() ?? money.PositionAveragePrice;

				//if (beginValue == 0m)
				//	return;

				result.Add(posMsg.Clone());

				_totalBlockedMoney = _totalBlockedMoney - prevPrice + money.PositionPrice;

				result.Add(
					new PositionChangeMessage
					{
						SecurityId = SecurityId.Money,
						ServerTime = posMsg.ServerTime,
						LocalTime = posMsg.LocalTime,
						PortfolioName = _name,
					}.Add(PositionChangeTypes.BlockedValue, _totalBlockedMoney)
				);
			}

			private MoneyInfo GetMoney(SecurityId securityId/*, DateTimeOffset time, ICollection<Message> result*/)
			{
				//bool isNew;
				var money = _moneys.SafeAdd(securityId, k => new MoneyInfo(_parent.GetEmulator(securityId)));

				//if (isNew)
				//{
				//	result.Add(new PositionMessage
				//	{
				//		LocalTime = time,
				//		PortfolioName = _name,
				//		SecurityId = securityId,
				//	});
				//}

				return money;
			}

			public decimal? ProcessOrder(ExecutionMessage orderMsg, decimal? cancelBalance, ICollection<Message> result)
			{
				var money = GetMoney(orderMsg.SecurityId/*, orderMsg.LocalTime, result*/);

				var prevPrice = money.TotalPrice;

				if (cancelBalance == null)
				{
					var balance = orderMsg.SafeGetVolume();

					if (orderMsg.Side == Sides.Buy)
						money.TotalBidsVolume += balance;
					else
						money.TotalAsksVolume += balance;
				}
				else
				{
					if (orderMsg.Side == Sides.Buy)
						money.TotalBidsVolume -= cancelBalance.Value;
					else
						money.TotalAsksVolume -= cancelBalance.Value;
				}

				_totalBlockedMoney = _totalBlockedMoney - prevPrice + money.TotalPrice;

				var commission = _parent._commissionManager.Process(orderMsg);

				AddPortfolioChangeMessage(orderMsg.ServerTime, result);

				return commission;
			}

			public void ProcessMyTrade(Sides side, ExecutionMessage tradeMsg, ICollection<Message> result)
			{
				var time = tradeMsg.ServerTime;

				PnLManager.ProcessMyTrade(tradeMsg, out _);
				tradeMsg.Commission = _parent._commissionManager.Process(tradeMsg);

				var position = tradeMsg.TradeVolume;

				if (position == null)
					return;

				if (side == Sides.Sell)
					position *= -1;

				var money = GetMoney(tradeMsg.SecurityId/*, time, result*/);

				var prevPrice = money.TotalPrice;

				var tradeVol = tradeMsg.TradeVolume.Value;

				if (tradeMsg.Side == Sides.Buy)
					money.TotalBidsVolume -= tradeVol;
				else
					money.TotalAsksVolume -= tradeVol;

				var prevPos = money.PositionCurrentValue;

				money.PositionDiff += position.Value;

				var tradePrice = tradeMsg.TradePrice.Value;
				var currPos = money.PositionCurrentValue;

				if (prevPos.Sign() == currPos.Sign())
					money.PositionAveragePrice = (money.PositionAveragePrice * prevPos + position.Value * tradePrice) / currPos;
				else
					money.PositionAveragePrice = currPos == 0 ? 0 : tradePrice;

				_totalBlockedMoney = _totalBlockedMoney - prevPrice + money.TotalPrice;

				result.Add(
					new PositionChangeMessage
					{
						LocalTime = time,
						ServerTime = time,
						PortfolioName = _name,
						SecurityId = tradeMsg.SecurityId,
					}
					.Add(PositionChangeTypes.CurrentValue, money.PositionCurrentValue)
					.TryAdd(PositionChangeTypes.AveragePrice, money.PositionAveragePrice)
				);

				AddPortfolioChangeMessage(time, result);
			}

			public void ProcessMarginChange(DateTimeOffset time, SecurityId securityId, ICollection<Message> result)
			{
				var money = _moneys.TryGetValue(securityId);

				if (money == null)
					return;

				_totalBlockedMoney = 0;

				foreach (var pair in _moneys)
					_totalBlockedMoney += pair.Value.TotalPrice;

				result.Add(
					new PositionChangeMessage
					{
						SecurityId = SecurityId.Money,
						ServerTime = time,
						LocalTime = time,
						PortfolioName = _name,
					}.Add(PositionChangeTypes.BlockedValue, _totalBlockedMoney)
				);
			}

			public void AddPortfolioChangeMessage(DateTimeOffset time, ICollection<Message> result)
			{
				var realizedPnL = PnLManager.RealizedPnL;
				var unrealizedPnL = PnLManager.UnrealizedPnL;
				var commission = _parent._commissionManager.Commission;
				var totalPnL = PnLManager.PnL - commission;

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
					PortfolioName = _name,
				}
				.Add(PositionChangeTypes.RealizedPnL, realizedPnL)
				.TryAdd(PositionChangeTypes.UnrealizedPnL, unrealizedPnL, true)
				.Add(PositionChangeTypes.VariationMargin, totalPnL)
				.Add(PositionChangeTypes.CurrentValue, _currentMoney)
				.Add(PositionChangeTypes.BlockedValue, _totalBlockedMoney)
				.Add(PositionChangeTypes.Commission, commission));
			}

			public string CheckRegistration(ExecutionMessage execMsg)
			{
				if (_parent.Settings.CheckMoney)
				{
					// если задан баланс, то проверям по нему (для частично исполненных заявок)
					var volume = execMsg.Balance ?? execMsg.SafeGetVolume();

					var money = GetMoney(execMsg.SecurityId/*, execMsg.LocalTime, result*/);

					var needBlock = money.GetPrice(execMsg.Side == Sides.Buy ? volume : 0, execMsg.Side == Sides.Sell ? volume : 0);

					if (_currentMoney < needBlock)
					{
						return LocalizedStrings.Str1169Params
						                       .Put(execMsg.PortfolioName, execMsg.TransactionId, needBlock, _currentMoney, money.TotalPrice);
					}
				}
				else if (_parent.Settings.CheckShortable && execMsg.Side == Sides.Sell &&
				         _parent._securityEmulators.TryGetValue(execMsg.SecurityId, out var secEmu) &&
						 secEmu.SecurityDefinition?.Shortable == false)
				{
					var money = GetMoney(execMsg.SecurityId/*, execMsg.LocalTime, result*/);

					var potentialPosition = money.PositionCurrentValue - execMsg.OrderVolume;

					if (potentialPosition < 0)
					{
						return LocalizedStrings.CannotShortPosition
						                       .Put(execMsg.PortfolioName, execMsg.TransactionId, money.PositionCurrentValue, execMsg.OrderVolume);
					}
				}

				return null;
			}
		}

		private readonly Dictionary<SecurityId, SecurityMarketEmulator> _securityEmulators = new Dictionary<SecurityId, SecurityMarketEmulator>();
		private readonly Dictionary<string, List<SecurityMarketEmulator>> _securityEmulatorsByBoard = new Dictionary<string, List<SecurityMarketEmulator>>(StringComparer.InvariantCultureIgnoreCase);
		private readonly Dictionary<string, PortfolioEmulator> _portfolios = new Dictionary<string, PortfolioEmulator>();
		private readonly Dictionary<string, BoardMessage> _boardDefinitions = new Dictionary<string, BoardMessage>(StringComparer.InvariantCultureIgnoreCase);
		private readonly Dictionary<SecurityId, Dictionary<Level1Fields, object>> _secStates = new Dictionary<SecurityId, Dictionary<Level1Fields, object>>();
		private bool? _needBuffer;
		private readonly List<Message> _buffer = new List<Message>();
		private DateTimeOffset _bufferPrevFlush;
		private DateTimeOffset _portfoliosPrevRecalc;
		private readonly ICommissionManager _commissionManager = new CommissionManager();
		private readonly Dictionary<string, SessionStates> _boardStates = new Dictionary<string, SessionStates>(StringComparer.InvariantCultureIgnoreCase);

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

			((IMessageAdapter)this).SupportedInMessages = ((IMessageAdapter)this).PossibleSupportedMessages.Select(i => i.Type).ToArray();
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
									ExecutionType = ExecutionTypes.Transaction,
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

					_buffer.Clear();
					_needBuffer = null;

					_bufferPrevFlush = default;
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

					if (!statusMsg.IsSubscribe)
						break;

					foreach (var pair in _securityEmulators)
					{
						pair.Value.Process(message, retVal);
					}

					if (statusMsg.To == null)
						retVal.Add(new SubscriptionOnlineMessage { OriginalTransactionId = statusMsg.TransactionId });

					break;
				}

				case MessageTypes.PortfolioLookup:
				{
					var lookupMsg = (PortfolioLookupMessage)message;

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
				case MessageTypes.SecurityLookup:
				case MessageTypes.BoardLookup:
				case MessageTypes.TimeFrameLookup:
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

			foreach (var msg in BufferResult(retVal, message.LocalTime))
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
			return _securityEmulators.SafeAdd(securityId, key =>
			{
				var emulator = new SecurityMarketEmulator(this, securityId) { Parent = this };

				_securityEmulatorsByBoard.SafeAdd(securityId.BoardCode).Add(emulator);

				var sec = SecurityProvider.LookupById(securityId);

				if (sec != null)
					emulator.Process(sec.ToMessage(), new List<Message>());

				var board = _boardDefinitions.TryGetValue(securityId.BoardCode);

				if (board != null)
					emulator.Process(board, new List<Message>());

				return emulator;
			});
		}

		private IEnumerable<Message> BufferResult(IEnumerable<Message> result, DateTimeOffset time)
		{
			if (_needBuffer == null)
				_needBuffer = Settings.BufferTime > TimeSpan.Zero;

			if (_needBuffer == false)
				return result;

			_buffer.AddRange(result);

			if ((time - _bufferPrevFlush) > Settings.BufferTime)
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
			return _portfolios.SafeAdd(portfolioName, key => new PortfolioEmulator(this, key));
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

		private string CheckRegistration(ExecutionMessage execMsg, SecurityMessage securityDefinition/*, ICollection<Message> result*/)
		{
			if (Settings.CheckTradingState)
			{
				var board = _boardDefinitions.TryGetValue(execMsg.SecurityId.BoardCode);

				if (board != null)
				{
					//if (execMsg.OrderType == OrderTypes.Market && !board.IsSupportMarketOrders)
					//if (!Settings.IsSupportAtomicReRegister)
					//	return LocalizedStrings.Str1170Params.Put(board.Code);

					if (!board.IsTradeTime(execMsg.ServerTime))
						return LocalizedStrings.Str1171;
				}
			}

			var state = _secStates.TryGetValue(execMsg.SecurityId);

			var secState = (SecurityStates?)state?.TryGetValue(Level1Fields.State);

			if (secState == SecurityStates.Stoped)
				return LocalizedStrings.SecurityStopped.Put(execMsg.SecurityId);

			if (securityDefinition?.BasketCode.IsEmpty() == false)
				return LocalizedStrings.SecurityNonTradable.Put(execMsg.SecurityId);

			var priceStep = securityDefinition?.PriceStep;
			var volumeStep = securityDefinition?.VolumeStep;
			var minVolume = securityDefinition?.MinVolume;
			var maxVolume = securityDefinition?.MaxVolume;

			if (state != null && execMsg.OrderType != OrderTypes.Market)
			{
				var minPrice = (decimal?)state.TryGetValue(Level1Fields.MinPrice);
				var maxPrice = (decimal?)state.TryGetValue(Level1Fields.MaxPrice);

				if (priceStep == null)
					priceStep = (decimal?)state.TryGetValue(Level1Fields.PriceStep);

				if (minPrice != null && minPrice > 0 && execMsg.OrderPrice < minPrice)
					return LocalizedStrings.Str1172Params.Put(execMsg.OrderPrice, execMsg.TransactionId, minPrice);

				if (maxPrice != null && maxPrice > 0 && execMsg.OrderPrice > maxPrice)
					return LocalizedStrings.Str1173Params.Put(execMsg.OrderPrice, execMsg.TransactionId, maxPrice);
			}

			if (priceStep != null && priceStep > 0 && execMsg.OrderPrice % priceStep != 0)
				return LocalizedStrings.OrderPriceNotMultipleOfPriceStep.Put(execMsg.OrderPrice, execMsg.TransactionId, priceStep);

			if (volumeStep == null)
				volumeStep = (decimal?)state?.TryGetValue(Level1Fields.VolumeStep);

			if (volumeStep != null && volumeStep > 0 && execMsg.OrderVolume % volumeStep != 0)
				return LocalizedStrings.OrderVolumeNotMultipleOfVolumeStep.Put(execMsg.OrderVolume, execMsg.TransactionId, volumeStep);

			if (minVolume != null && execMsg.OrderVolume < minVolume)
				return LocalizedStrings.OrderVolumeLessMin.Put(execMsg.OrderVolume, execMsg.TransactionId, minVolume);

			if (maxVolume != null && execMsg.OrderVolume > maxVolume)
				return LocalizedStrings.OrderVolumeMoreMax.Put(execMsg.OrderVolume, execMsg.TransactionId, maxVolume);

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

		IEnumerable<MessageTypeInfo> IMessageAdapter.PossibleSupportedMessages { get; } = new[]
		{
			MessageTypes.SecurityLookup.ToInfo(),
			MessageTypes.TimeFrameLookup.ToInfo(),
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
			ExtendedMessageTypes.EmulationState.ToInfo(),
			ExtendedMessageTypes.CommissionRule.ToInfo(),
			//ExtendedMessageTypes.Clearing.ToInfo(),
		};
		IEnumerable<MessageTypes> IMessageAdapter.SupportedInMessages { get; set; }
		IEnumerable<MessageTypes> IMessageAdapter.SupportedOutMessages { get; } = Enumerable.Empty<MessageTypes>();
		IEnumerable<MessageTypes> IMessageAdapter.SupportedResultMessages { get; } = new[]
		{
			MessageTypes.SecurityLookup,
			MessageTypes.PortfolioLookup,
			MessageTypes.TimeFrameLookup,
			MessageTypes.BoardLookup,
		};
		IEnumerable<DataType> IMessageAdapter.SupportedMarketDataTypes { get; } = new[]
		{
			DataType.OrderLog,
			DataType.Ticks,
			DataType.CandleTimeFrame,
			DataType.MarketDepth,
		};

		IDictionary<string, RefPair<SecurityTypes, string>> IMessageAdapter.SecurityClassInfo { get; } = new Dictionary<string, RefPair<SecurityTypes, string>>();

		IEnumerable<Level1Fields> IMessageAdapter.CandlesBuildFrom => Enumerable.Empty<Level1Fields>();

		bool IMessageAdapter.CheckTimeFrameByRequest => true;

		ReConnectionSettings IMessageAdapter.ReConnectionSettings { get; } = new ReConnectionSettings();

		TimeSpan IMessageAdapter.HeartbeatInterval { get => TimeSpan.Zero; set { } }

		string IMessageAdapter.StorageName => null;

		bool IMessageAdapter.IsNativeIdentifiersPersistable => false;
		bool IMessageAdapter.IsNativeIdentifiers => false;
		bool IMessageAdapter.IsFullCandlesOnly => false;
		bool IMessageAdapter.IsSupportSubscriptions => true;
		bool IMessageAdapter.IsSupportCandlesUpdates => true;
		bool IMessageAdapter.IsSupportCandlesPriceLevels => false;

		MessageAdapterCategories IMessageAdapter.Categories => default;

		IEnumerable<Tuple<string, Type>> IMessageAdapter.SecurityExtendedFields { get; } = Enumerable.Empty<Tuple<string, Type>>();
		IEnumerable<int> IMessageAdapter.SupportedOrderBookDepths => throw new NotImplementedException();
		bool IMessageAdapter.IsSupportOrderBookIncrements => false;
		bool IMessageAdapter.IsSupportExecutionsPnL => true;
		bool IMessageAdapter.IsSecurityNewsOnly => false;
		Type IMessageAdapter.OrderConditionType => null;
		bool IMessageAdapter.HeartbeatBeforConnect => false;
		Uri IMessageAdapter.Icon => null;
		bool IMessageAdapter.IsAutoReplyOnTransactonalUnsubscription => true;
		bool IMessageAdapter.EnqueueSubscriptions { get; set; }
		bool IMessageAdapter.IsSupportTransactionLog => false;
		bool IMessageAdapter.UseChannels => false;
		TimeSpan IMessageAdapter.IterationInterval => default;
		string IMessageAdapter.FeatureName => string.Empty;
		bool? IMessageAdapter.IsPositionsEmulationRequired => null;
		bool IMessageAdapter.IsReplaceCommandEditCurrent => false;
		bool IMessageAdapter.GenerateOrderBookFromLevel1 { get; set; }

		IOrderLogMarketDepthBuilder IMessageAdapter.CreateOrderLogMarketDepthBuilder(SecurityId securityId)
			=> new OrderLogMarketDepthBuilder(securityId);

		IEnumerable<object> IMessageAdapter.GetCandleArgs(Type candleType, SecurityId securityId, DateTimeOffset? from, DateTimeOffset? to)
			=> Enumerable.Empty<object>();

		TimeSpan IMessageAdapter.GetHistoryStepSize(DataType dataType, out TimeSpan iterationInterval)
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

		IMessageChannel ICloneable<IMessageChannel>.Clone()
			=> new MarketEmulator(SecurityProvider, PortfolioProvider, ExchangeInfoProvider, TransactionIdGenerator);

		object ICloneable.Clone() => ((ICloneable<IMessageChannel>)this).Clone();
	}
}
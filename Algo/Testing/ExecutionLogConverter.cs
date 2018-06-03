#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Testing.Algo
File: ExecutionLogConverter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Testing
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The converter of <see cref="QuoteChangeMessage"/> and <see cref="ExecutionMessage"/> type messages (associated with tick trade) into single stream <see cref="ExecutionMessage"/> (associated with orders log).
	/// </summary>
	class ExecutionLogConverter
	{
		private readonly Random _volumeRandom = new Random(TimeHelper.Now.Millisecond);
		private readonly Random _priceRandom = new Random(TimeHelper.Now.Millisecond);
		private readonly SortedDictionary<decimal, RefPair<LevelQuotes, QuoteChange>> _bids;
		private readonly SortedDictionary<decimal, RefPair<LevelQuotes, QuoteChange>> _asks;
		private decimal _currSpreadPrice;
		private readonly MarketEmulatorSettings _settings;
		private readonly Func<DateTimeOffset, DateTimeOffset> _getServerTime;
		private decimal _prevTickPrice;
		// указывает, есть ли реальные стаканы, чтобы своей псевдо генерацией не портить настоящую историю
		private DateTime _lastDepthDate;
		//private DateTime _lastTradeDate;
		private SecurityMessage _securityDefinition = new SecurityMessage
		{
			PriceStep = 1,
			VolumeStep = 1,
		};
		private bool _priceStepUpdated;
		private bool _volumeStepUpdated;

		private decimal? _prevBidPrice;
		private decimal? _prevBidVolume;
		private decimal? _prevAskPrice;
		private decimal? _prevAskVolume;

		public ExecutionLogConverter(SecurityId securityId,
			SortedDictionary<decimal, RefPair<LevelQuotes, QuoteChange>> bids,
			SortedDictionary<decimal, RefPair<LevelQuotes, QuoteChange>> asks,
			MarketEmulatorSettings settings, Func<DateTimeOffset, DateTimeOffset> getServerTime)
		{
			_bids = bids ?? throw new ArgumentNullException(nameof(bids));
			_asks = asks ?? throw new ArgumentNullException(nameof(asks));
			_settings = settings ?? throw new ArgumentNullException(nameof(settings));
			_getServerTime = getServerTime ?? throw new ArgumentNullException(nameof(getServerTime));
			SecurityId = securityId;
		}

		/// <summary>
		/// Security ID.
		/// </summary>
		public SecurityId SecurityId { get; }

		/// <summary>
		/// To convert quotes.
		/// </summary>
		/// <param name="message">Quotes.</param>
		/// <returns>Stream <see cref="ExecutionMessage"/>.</returns>
		public IEnumerable<ExecutionMessage> ToExecutionLog(QuoteChangeMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (!_priceStepUpdated || !_volumeStepUpdated)
			{
				var quote = message.GetBestBid() ?? message.GetBestAsk();

				if (quote != null)
				{
					if (!_priceStepUpdated)
					{
						_securityDefinition.PriceStep = quote.Price.GetDecimalInfo().EffectiveScale.GetPriceStep();
						_priceStepUpdated = true;
					}

					if (!_volumeStepUpdated)
					{
						_securityDefinition.VolumeStep = quote.Volume.GetDecimalInfo().EffectiveScale.GetPriceStep();
						_volumeStepUpdated = true;
					}
				}
			}

			_lastDepthDate = message.LocalTime.Date;

			// чтобы склонировать внутренние котировки
			//message = (QuoteChangeMessage)message.Clone();
			// TODO для ускорения идет shallow copy котировок
			var newBids = message.IsSorted ? message.Bids : message.Bids.OrderByDescending(q => q.Price);
			var newAsks = message.IsSorted ? message.Asks : message.Asks.OrderBy(q => q.Price);

			return ProcessQuoteChange(message.LocalTime, message.ServerTime, newBids.ToArray(), newAsks.ToArray());
		}

		private IEnumerable<ExecutionMessage> ProcessQuoteChange(DateTimeOffset time, DateTimeOffset serverTime, QuoteChange[] newBids, QuoteChange[] newAsks)
		{
			var diff = new List<ExecutionMessage>();

			GetDiff(diff, time, serverTime, _bids, newBids, Sides.Buy, out var bestBidPrice);
			GetDiff(diff, time, serverTime, _asks, newAsks, Sides.Sell, out var bestAskPrice);

			var spreadPrice = bestAskPrice == 0
				? bestBidPrice
				: (bestBidPrice == 0
					? bestAskPrice
					: (bestAskPrice - bestBidPrice) / 2 + bestBidPrice);

			try
			{
				//при обновлении стакана необходимо учитывать направление сдвига, чтобы не было ложного исполнения при наложении бидов и асков.
				//т.е. если цена сдвинулась вниз, то обновление стакана необходимо начинать с минимального бида.
				return (spreadPrice < _currSpreadPrice)
					? diff.OrderBy(m => m.OrderPrice)
					: diff.OrderByDescending(m => m.OrderPrice);
			}
			finally
			{
				_currSpreadPrice = spreadPrice;
			}
		}

		private void GetDiff(List<ExecutionMessage> diff, DateTimeOffset time, DateTimeOffset serverTime, SortedDictionary<decimal, RefPair<LevelQuotes, QuoteChange>> from, IEnumerable<QuoteChange> to, Sides side, out decimal newBestPrice)
		{
			newBestPrice = 0;

			var canProcessFrom = true;
			var canProcessTo = true;

			QuoteChange currFrom = null;
			QuoteChange currTo = null;

			// TODO
			//List<ExecutionMessage> currOrders = null;

			var mult = side == Sides.Buy ? -1 : 1;
			bool? isSpread = null;

			using (var fromEnum = from.GetEnumerator())
			using (var toEnum = to.GetEnumerator())
			{
				while (true)
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
								newBestPrice = currTo.Price;
						}
					}

					if (currFrom == null)
					{
						if (currTo == null)
							break;
						else
						{
							AddExecMsg(diff, time, serverTime, currTo, currTo.Volume, false);
							currTo = null;
						}
					}
					else
					{
						if (currTo == null)
						{
							AddExecMsg(diff, time, serverTime, currFrom, -currFrom.Volume, isSpread.Value);
							currFrom = null;
						}
						else
						{
							if (currFrom.Price == currTo.Price)
							{
								if (currFrom.Volume != currTo.Volume)
								{
									AddExecMsg(diff, time, serverTime, currTo, currTo.Volume - currFrom.Volume, isSpread.Value);
								}

								currFrom = currTo = null;
							}
							else if (currFrom.Price * mult > currTo.Price * mult)
							{
								AddExecMsg(diff, time, serverTime, currTo, currTo.Volume, isSpread.Value);
								currTo = null;
							}
							else
							{
								AddExecMsg(diff, time, serverTime, currFrom, -currFrom.Volume, isSpread.Value);
								currFrom = null;
							}
						}
					}
				}
			}
		}

		private readonly RandomArray<bool> _isMatch = new RandomArray<bool>(100);

		private void AddExecMsg(List<ExecutionMessage> diff, DateTimeOffset time, DateTimeOffset serverTime, QuoteChange quote, decimal volume, bool isSpread)
		{
			if (volume > 0)
				diff.Add(CreateMessage(time, serverTime, quote.Side, quote.Price, volume));
			else
			{
				volume = volume.Abs();

				// matching only top orders (spread)
				if (isSpread && volume > 1 && _isMatch.Next())
				{
					var tradeVolume = (int)volume / 2;

					diff.Add(new ExecutionMessage
					{
						Side = quote.Side,
						TradeVolume = tradeVolume,
						ExecutionType = ExecutionTypes.Tick,
						SecurityId = SecurityId,
						LocalTime = time,
						ServerTime = serverTime,
						TradePrice = quote.Price,
					});

					// that tick will not affect on order book
					//volume -= tradeVolume;
				}

				diff.Add(CreateMessage(time, serverTime, quote.Side, quote.Price, volume, true));
			}
		}

		/// <summary>
		/// To convert the tick trade.
		/// </summary>
		/// <param name="tick">Tick trade.</param>
		/// <returns>Stream <see cref="ExecutionMessage"/>.</returns>
		public IEnumerable<ExecutionMessage> ToExecutionLog(ExecutionMessage tick)
		{
			if (tick == null)
				throw new ArgumentNullException(nameof(tick));

			if (!_priceStepUpdated)
			{
				_securityDefinition.PriceStep = tick.GetTradePrice().GetDecimalInfo().EffectiveScale.GetPriceStep();
				_priceStepUpdated = true;
			}

			if (!_volumeStepUpdated)
			{
				var tickVolume = tick.TradeVolume;

				if (tickVolume != null)
				{
					_securityDefinition.VolumeStep = tickVolume.Value.GetDecimalInfo().EffectiveScale.GetPriceStep();
					_volumeStepUpdated = true;
				}
			}

			//if (tick.ExecutionType != ExecutionTypes.Tick)
			//	throw new ArgumentOutOfRangeException(nameof(tick), tick.ExecutionType, LocalizedStrings.Str1655);

			//_lastTradeDate = message.LocalTime.Date;

			var retVal = new List<ExecutionMessage>();

			var bestBid = _bids.FirstOrDefault();
			var bestAsk = _asks.FirstOrDefault();

			var tradePrice = tick.GetTradePrice();
			var volume = tick.TradeVolume ?? 1;
			var time = tick.LocalTime;

			if (bestBid.Value != null && tradePrice <= bestBid.Key)
			{
				// тик попал в биды, значит была крупная заявка по рынку на продажу,
				// которая возможна исполнила наши заявки

				ProcessMarketOrder(retVal, _bids, tick.ServerTime, tick.LocalTime, Sides.Sell, tradePrice, volume);

				// подтягиваем противоположные котировки и снимаем лишние заявки
				TryCreateOppositeOrder(retVal, _asks, time, tick.ServerTime, tradePrice, volume, Sides.Buy);
			}
			else if (bestAsk.Value != null && tradePrice >= bestAsk.Key)
			{
				// тик попал в аски, значит была крупная заявка по рынку на покупку,
				// которая возможна исполнила наши заявки

				ProcessMarketOrder(retVal, _asks, tick.ServerTime, tick.LocalTime, Sides.Buy, tradePrice, volume);

				TryCreateOppositeOrder(retVal, _bids, time, tick.ServerTime, tradePrice, volume, Sides.Sell);
			}
			else if (bestBid.Value != null && bestAsk.Value != null && bestBid.Key < tradePrice && tradePrice < bestAsk.Key)
			{
				// тик попал в спред, значит в спреде до сделки была заявка.
				// создаем две лимитки с разных сторон, но одинаковой ценой.
				// если в эмуляторе есть наша заявка на этом уровне, то она исполниться.
				// если нет, то эмулятор взаимно исполнит эти заявки друг об друга

				var originSide = GetOrderSide(tick);

				retVal.Add(CreateMessage(time, tick.ServerTime, originSide, tradePrice, volume + (_securityDefinition.VolumeStep ?? 1 * _settings.VolumeMultiplier), tif: TimeInForce.MatchOrCancel));

				var spreadStep = _settings.SpreadSize * GetPriceStep();

				// try to fill depth gaps

				var newBestPrice = tradePrice + spreadStep;

				var depth = _settings.MaxDepth;
				while (--depth > 0)
				{
					var diff = bestAsk.Key - newBestPrice;

					if (diff > 0)
					{
						retVal.Add(CreateMessage(time, tick.ServerTime, Sides.Sell, newBestPrice, 0));
						newBestPrice += spreadStep * _priceRandom.Next(1, _settings.SpreadSize);
					}
					else
						break;
				}

				newBestPrice = tradePrice - spreadStep;

				depth = _settings.MaxDepth;
				while (--depth > 0)
				{
					var diff = newBestPrice - bestBid.Key;

					if (diff > 0)
					{
						retVal.Add(CreateMessage(time, tick.ServerTime, Sides.Buy, newBestPrice, 0));
						newBestPrice -= spreadStep * _priceRandom.Next(1, _settings.SpreadSize);
					}
					else
						break;
				}

				retVal.Add(CreateMessage(time, tick.ServerTime, originSide.Invert(), tradePrice, volume, tif: TimeInForce.MatchOrCancel));
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
					originSide = GetOrderSide(tick);
					hasOpposite = false;
				}

				retVal.Add(CreateMessage(time, tick.ServerTime, originSide, tradePrice, volume));

				// если стакан был полностью пустой, то формируем сразу уровень с противоположной стороны
				if (!hasOpposite)
				{
					var oppositePrice = tradePrice + _settings.SpreadSize * GetPriceStep() * (originSide == Sides.Buy ? 1 : -1);

					if (oppositePrice > 0)
						retVal.Add(CreateMessage(time, tick.ServerTime, originSide.Invert(), oppositePrice, volume));
				}
			}

			if (!HasDepth(time))
			{
				// если стакан слишком разросся, то удаляем его хвосты (не удаляя пользовательские заявки)
				CancelWorstQuote(retVal, time, tick.ServerTime, Sides.Buy, _bids);
				CancelWorstQuote(retVal, time, tick.ServerTime, Sides.Sell, _asks);	
			}

			_prevTickPrice = tradePrice;

			return retVal;
		}

		private Sides GetOrderSide(ExecutionMessage message)
		{
			if (message.OriginSide == null)
				return message.TradePrice > _prevTickPrice ? Sides.Sell : Sides.Buy;
			else
				return message.OriginSide.Value.Invert();
		}

		/// <summary>
		/// To convert first level of market data.
		/// </summary>
		/// <param name="message">Level 1.</param>
		/// <returns>Stream <see cref="Message"/>.</returns>
		public IEnumerable<Message> ToExecutionLog(Level1ChangeMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (message.IsContainsTick())
				yield return message.ToTick();

			if (message.IsContainsQuotes() && !HasDepth(message.LocalTime))
			{
				var prevBidPrice = _prevBidPrice;
				var prevBidVolume = _prevBidVolume;
				var prevAskPrice = _prevAskPrice;
				var prevAskVolume = _prevAskVolume;

				_prevBidPrice = (decimal?)message.Changes.TryGetValue(Level1Fields.BestBidPrice) ?? _prevBidPrice;
				_prevBidVolume = (decimal?)message.Changes.TryGetValue(Level1Fields.BestBidVolume) ?? _prevBidVolume;
				_prevAskPrice = (decimal?)message.Changes.TryGetValue(Level1Fields.BestAskPrice) ?? _prevAskPrice;
				_prevAskVolume = (decimal?)message.Changes.TryGetValue(Level1Fields.BestAskVolume) ?? _prevAskVolume;

				if (_prevBidPrice == 0)
					_prevBidPrice = null;

				if (_prevAskPrice == 0)
					_prevAskPrice = null;

				if (prevBidPrice == _prevBidPrice && prevBidVolume == _prevBidVolume && prevAskPrice == _prevAskPrice && prevAskVolume == _prevAskVolume)
					yield break;

				yield return new QuoteChangeMessage
				{
					SecurityId = message.SecurityId,
					LocalTime = message.LocalTime,
					ServerTime = message.ServerTime,
					Bids = _prevBidPrice == null ? Enumerable.Empty<QuoteChange>() : new[] { new QuoteChange(Sides.Buy, _prevBidPrice.Value, _prevBidVolume ?? 0) },
					Asks = _prevAskPrice == null ? Enumerable.Empty<QuoteChange>() : new[] { new QuoteChange(Sides.Sell, _prevAskPrice.Value, _prevAskVolume ?? 0) },
				};
			}
		}

		private void ProcessMarketOrder(List<ExecutionMessage> retVal, SortedDictionary<decimal, RefPair<LevelQuotes, QuoteChange>> quotes, DateTimeOffset time, DateTimeOffset localTime, Sides orderSide, decimal tradePrice, decimal volume)
		{
			// вычисляем объем заявки по рынку, который смог бы пробить текущие котировки.

			// bigOrder - это наша большая рыночная заявка, которая способствовала появлению tradeMessage
			var bigOrder = CreateMessage(localTime, time, orderSide, tradePrice, 0, tif: TimeInForce.MatchOrCancel);
			var sign = orderSide == Sides.Buy ? -1 : 1;
			var hasQuotes = false;

			foreach (var pair in quotes)
			{
				var quote = pair.Value.Second;

				if (quote.Price * sign > tradePrice * sign)
				{
					bigOrder.OrderVolume += quote.Volume;
				}
				else
				{
					if (quote.Price == tradePrice)
					{
						bigOrder.OrderVolume += volume;

						//var diff = tradeMessage.Volume - quote.Volume;

						//// если объем котиовки был меньше объема сделки
						//if (diff > 0)
						//	retVal.Add(CreateMessage(tradeMessage.LocalTime, quote.Side, quote.Price, diff));
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

					//// если котировки с ценой сделки вообще не было в стакане
					//else if (quote.Price * sign < tradeMessage.TradePrice * sign)
					//{
					//	retVal.Add(CreateMessage(tradeMessage.LocalTime, quote.Side, tradeMessage.Price, tradeMessage.Volume));
					//}
				}
			}

			retVal.Add(bigOrder);

			// если собрали все котировки, то оставляем заявку в стакане по цене сделки
			if (!hasQuotes)
				retVal.Add(CreateMessage(localTime, time, orderSide.Invert(), tradePrice, volume));
		}

		private void TryCreateOppositeOrder(List<ExecutionMessage> retVal, SortedDictionary<decimal, RefPair<LevelQuotes, QuoteChange>> quotes, DateTimeOffset localTime, DateTimeOffset serverTime, decimal tradePrice, decimal volume, Sides originSide)
		{
			if (HasDepth(localTime))
				return;

			var priceStep = GetPriceStep();
            var oppositePrice = (tradePrice + _settings.SpreadSize * priceStep * (originSide == Sides.Buy ? 1 : -1)).Max(priceStep);

			var bestQuote = quotes.FirstOrDefault();

			if (bestQuote.Value == null || ((originSide == Sides.Buy && oppositePrice < bestQuote.Key) || (originSide == Sides.Sell && oppositePrice > bestQuote.Key)))
				retVal.Add(CreateMessage(localTime, serverTime, originSide.Invert(), oppositePrice, volume));
		}

		private void CancelWorstQuote(List<ExecutionMessage> retVal, DateTimeOffset time, DateTimeOffset serverTime, Sides side, SortedDictionary<decimal, RefPair<LevelQuotes, QuoteChange>> quotes)
		{
			if (quotes.Count <= _settings.MaxDepth)
				return;

			var worst = quotes.Last();
			var volume = worst.Value.First.Where(e => e.PortfolioName == null).Sum(e => e.OrderVolume.Value);

			if (volume == 0)
				return;

			retVal.Add(CreateMessage(time, serverTime, side, worst.Key, volume, true));
		}

		private ExecutionMessage CreateMessage(DateTimeOffset localTime, DateTimeOffset serverTime, Sides side, decimal price, decimal volume, bool isCancelling = false, TimeInForce tif = TimeInForce.PutInQueue)
		{
			if (price <= 0)
				throw new ArgumentOutOfRangeException(nameof(price), price, LocalizedStrings.Str1144);

			//if (volume <= 0)
			//	throw new ArgumentOutOfRangeException(nameof(volume), volume, LocalizedStrings.Str3344);

			if (volume == 0)
				volume = _volumeRandom.Next(10, 100);

			return new ExecutionMessage
			{
				Side = side,
				OrderPrice = price,
				OrderVolume = volume,
				ExecutionType = ExecutionTypes.OrderLog,
				IsCancelled = isCancelling,
				SecurityId = SecurityId,
				LocalTime = localTime,
				ServerTime = serverTime,
				TimeInForce = tif,
			};
		}

		private bool HasDepth(DateTimeOffset time)
		{
			return _lastDepthDate == time.Date;
		}

		/// <summary>
		/// Convert transaction.
		/// </summary>
		/// <param name="message">Transaction.</param>
		/// <param name="quotesVolume">Order book volume.</param>
		/// <returns>Stream <see cref="ExecutionMessage"/>.</returns>
		public IEnumerable<ExecutionMessage> ToExecutionLog(OrderMessage message, decimal quotesVolume)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var serverTime = _getServerTime(message.LocalTime);

			switch (message.Type)
			{
				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;
					
					if (NeedCheckVolume(regMsg, quotesVolume))
					{
						foreach (var executionMessage in IncreaseDepthVolume(regMsg, serverTime, quotesVolume))
							yield return executionMessage;
					}

					yield return new ExecutionMessage
					{
						LocalTime = regMsg.LocalTime,
						ServerTime = serverTime,
						SecurityId = regMsg.SecurityId,
						ExecutionType = ExecutionTypes.Transaction,
						HasOrderInfo = true,
						TransactionId = regMsg.TransactionId,
						OrderPrice = regMsg.Price,
						OrderVolume = regMsg.Volume,
						Side = regMsg.Side,
						PortfolioName = regMsg.PortfolioName,
						OrderType = regMsg.OrderType,
						UserOrderId = regMsg.UserOrderId
					};

					yield break;
				}
				case MessageTypes.OrderReplace:
				{
					var replaceMsg = (OrderReplaceMessage)message;
					
					if (NeedCheckVolume(replaceMsg, quotesVolume))
					{
						foreach (var executionMessage in IncreaseDepthVolume(replaceMsg, serverTime, quotesVolume))
							yield return executionMessage;
					}

					yield return new ExecutionMessage
					{
						LocalTime = replaceMsg.LocalTime,
						ServerTime = serverTime,
						SecurityId = replaceMsg.SecurityId,
						ExecutionType = ExecutionTypes.Transaction,
						HasOrderInfo = true,
						IsCancelled = true,
						OrderId = replaceMsg.OldOrderId,
						OriginalTransactionId = replaceMsg.OldTransactionId,
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
						ExecutionType = ExecutionTypes.Transaction,
						HasOrderInfo = true,
						TransactionId = replaceMsg.TransactionId,
						OrderPrice = replaceMsg.Price,
						OrderVolume = replaceMsg.Volume,
						Side = replaceMsg.Side,
						PortfolioName = replaceMsg.PortfolioName,
						OrderType = replaceMsg.OrderType,
						UserOrderId = replaceMsg.UserOrderId
					};

					yield break;
				}
				case MessageTypes.OrderCancel:
				{
					var cancelMsg = (OrderCancelMessage)message;

					yield return new ExecutionMessage
					{
						ExecutionType = ExecutionTypes.Transaction,
						HasOrderInfo = true,
						IsCancelled = true,
						OrderId = cancelMsg.OrderId,
						TransactionId = cancelMsg.TransactionId,
						OriginalTransactionId = cancelMsg.OrderTransactionId,
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

				case MessageTypes.OrderPairReplace:
				case MessageTypes.OrderGroupCancel:
					throw new NotSupportedException();

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private bool NeedCheckVolume(OrderRegisterMessage message, decimal quotesVolume)
		{
			if (!_settings.IncreaseDepthVolume)
				return false;

			var orderSide = message.Side;
			var price = message.Price;

			var quotes = orderSide == Sides.Buy ? _asks : _bids;

			var quote = quotes.FirstOrDefault();

			if (quote.Value == null)
				return false;

			var bestPrice = quote.Key;

			return (orderSide == Sides.Buy ? price >= bestPrice : price <= bestPrice)
				&& quotesVolume <= message.Volume;
		}

		private IEnumerable<ExecutionMessage> IncreaseDepthVolume(OrderRegisterMessage message, DateTimeOffset serverTime, decimal quotesVolume)
		{
			var leftVolume = (message.Volume - quotesVolume) + 1;
			var orderSide = message.Side;

			var quotes = orderSide == Sides.Buy ? _asks : _bids;
			var quote = quotes.LastOrDefault();

			if (quote.Value == null)
				yield break;

			var side = orderSide.Invert();

			var lastVolume = quote.Value.Second.Volume;
			var lastPrice = quote.Value.Second.Price;

			while (leftVolume > 0 && lastPrice != 0)
			{
				lastVolume *= 2;
				lastPrice += GetPriceStep() * (side == Sides.Buy ? -1 : 1);

				leftVolume -= lastVolume;

				yield return CreateMessage(message.LocalTime, serverTime, side, lastPrice, lastVolume);
			}
		}

		private decimal GetPriceStep()
		{
			return _securityDefinition.PriceStep ?? 0.01m;
		}

		public void UpdateSecurityDefinition(SecurityMessage securityDefinition)
		{
			_securityDefinition = securityDefinition ?? throw new ArgumentNullException(nameof(securityDefinition));

			_priceStepUpdated = _securityDefinition.PriceStep != null;
			_volumeStepUpdated = _securityDefinition.VolumeStep != null;
		}
	}
}
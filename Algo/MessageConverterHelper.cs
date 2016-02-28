#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: MessageConverterHelper.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The auxiliary class for conversion of business-objects (<see cref="BusinessEntities"/>) into messages (<see cref="Messages"/>) and vice versa.
	/// </summary>
	public static class MessageConverterHelper
	{
		/// <summary>
		/// Cast <see cref="MarketDepth"/> to the <see cref="QuoteChangeMessage"/>.
		/// </summary>
		/// <param name="depth"><see cref="MarketDepth"/>.</param>
		/// <returns><see cref="QuoteChangeMessage"/>.</returns>
		public static QuoteChangeMessage ToMessage(this MarketDepth depth)
		{
			if (depth == null)
				throw new ArgumentNullException(nameof(depth));

			var securityId = depth.Security.ToSecurityId();

			return new QuoteChangeMessage
			{
				LocalTime = depth.LocalTime,
				SecurityId = securityId,
				Bids = depth.Bids.Select(q => q.ToQuoteChange()).ToArray(),
				Asks = depth.Asks.Select(q => q.ToQuoteChange()).ToArray(),
				ServerTime = depth.LastChangeTime,
				IsSorted = true,
				Currency = depth.Currency,
			};
		}

		private static readonly PairSet<Type, Type> _candleTypes = new PairSet<Type, Type>
		{
			{ typeof(TimeFrameCandle), typeof(TimeFrameCandleMessage) },
			{ typeof(TickCandle), typeof(TickCandleMessage) },
			{ typeof(VolumeCandle), typeof(VolumeCandleMessage) },
			{ typeof(RangeCandle), typeof(RangeCandleMessage) },
			{ typeof(PnFCandle), typeof(PnFCandleMessage) },
			{ typeof(RenkoCandle), typeof(RenkoCandleMessage) },
		};

		private static readonly PairSet<MessageTypes, MarketDataTypes> _candleDataTypes = new PairSet<MessageTypes, MarketDataTypes>
		{
			{ MessageTypes.CandleTimeFrame, MarketDataTypes.CandleTimeFrame },
			{ MessageTypes.CandleTick, MarketDataTypes.CandleTick },
			{ MessageTypes.CandleVolume, MarketDataTypes.CandleVolume },
			{ MessageTypes.CandleRange, MarketDataTypes.CandleRange },
			{ MessageTypes.CandlePnF, MarketDataTypes.CandlePnF },
			{ MessageTypes.CandleRenko, MarketDataTypes.CandleRenko },
		};

		private static readonly PairSet<Type, MarketDataTypes> _candleMarketDataTypes = new PairSet<Type, MarketDataTypes>
		{
			{ typeof(TimeFrameCandleMessage), MarketDataTypes.CandleTimeFrame },
			{ typeof(TickCandleMessage), MarketDataTypes.CandleTick },
			{ typeof(VolumeCandleMessage), MarketDataTypes.CandleVolume },
			{ typeof(RangeCandleMessage), MarketDataTypes.CandleRange },
			{ typeof(PnFCandleMessage), MarketDataTypes.CandlePnF },
			{ typeof(RenkoCandleMessage), MarketDataTypes.CandleRenko },
		};

		/// <summary>
		/// Преобразовать тип свечек <see cref="MarketDataTypes"/> в тип сообщения <see cref="CandleMessage"/>.
		/// </summary>
		/// <param name="type">Тип свечек.</param>
		/// <returns>Тип сообщения <see cref="CandleMessage"/>.</returns>
		public static Type ToCandleMessage(this MarketDataTypes type)
		{
			var messageType = _candleMarketDataTypes.TryGetKey(type);

			if (messageType == null)
				throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.WrongCandleType);

			return messageType;
		}

		/// <summary>
		/// Cast message type <see cref="CandleMessage"/> to the <see cref="MarketDataTypes"/>.
		/// </summary>
		/// <param name="messageType">The type of the message <see cref="CandleMessage"/>.</param>
		/// <returns><see cref="MarketDataTypes"/>.</returns>
		public static MarketDataTypes ToCandleMarketDataType(this Type messageType)
		{
			if (messageType == null)
				throw new ArgumentNullException(nameof(messageType));

			var dataType = _candleMarketDataTypes.TryGetValue2(messageType);

			if (dataType == null)
				throw new ArgumentOutOfRangeException(nameof(messageType), messageType, LocalizedStrings.WrongCandleType);

			return dataType.Value;
		}

		/// <summary>
		/// Cast candle type <see cref="Candle"/> to the message <see cref="CandleMessage"/>.
		/// </summary>
		/// <param name="candleType">The type of the candle <see cref="Candle"/>.</param>
		/// <returns>The type of the message <see cref="CandleMessage"/>.</returns>
		public static Type ToCandleMessageType(this Type candleType)
		{
			if (candleType == null)
				throw new ArgumentNullException(nameof(candleType));

			var messageType = _candleTypes.TryGetValue(candleType);

			if (messageType == null)
				throw new ArgumentOutOfRangeException(nameof(candleType), candleType, LocalizedStrings.WrongCandleType);

			return messageType;
		}

		/// <summary>
		/// Cast message type <see cref="CandleMessage"/> to the candle type <see cref="Candle"/>.
		/// </summary>
		/// <param name="messageType">The type of the message <see cref="CandleMessage"/>.</param>
		/// <returns>The type of the candle <see cref="Candle"/>.</returns>
		public static Type ToCandleType(this Type messageType)
		{
			if (messageType == null)
				throw new ArgumentNullException(nameof(messageType));

			var candleType = _candleTypes.TryGetKey(messageType);

			if (candleType == null)
				throw new ArgumentOutOfRangeException(nameof(messageType), messageType, LocalizedStrings.WrongCandleType);

			return candleType;
		}

		/// <summary>
		/// To convert the type of candles <see cref="MarketDataTypes"/> into type of message <see cref="MessageTypes"/>.
		/// </summary>
		/// <param name="type">The candles type.</param>
		/// <returns>Message type.</returns>
		public static MessageTypes ToCandleMessageType(this MarketDataTypes type)
		{
			return _candleDataTypes[type];
		}

		/// <summary>
		/// To convert the type of message <see cref="MessageTypes"/> into type of candles <see cref="MarketDataTypes"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		/// <returns>The candles type.</returns>
		public static MarketDataTypes ToCandleMarketDataType(this MessageTypes type)
		{
			return _candleDataTypes[type];
		}

		/// <summary>
		/// To convert the candle into message.
		/// </summary>
		/// <param name="candle">Candle.</param>
		/// <returns>Message.</returns>
		public static CandleMessage ToMessage(this Candle candle)
		{
			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

			CandleMessage message;

			if (candle is TimeFrameCandle)
				message = new TimeFrameCandleMessage();
			else if (candle is TickCandle)
				message = new TickCandleMessage();
			else if (candle is VolumeCandle)
				message = new VolumeCandleMessage();
			else if (candle is RangeCandle)
				message = new RangeCandleMessage();
			else if (candle is PnFCandle)
				message = new PnFCandleMessage();
			else if (candle is RenkoCandle)
				message = new RenkoCandleMessage();
			else
				throw new ArgumentException("Неизвестный тип '{0}' свечки.".Put(candle.GetType()), nameof(candle));

			message.LocalTime = candle.OpenTime;
			message.SecurityId = candle.Security.ToSecurityId();
			message.OpenTime = candle.OpenTime;
			message.HighTime = candle.HighTime;
			message.LowTime = candle.LowTime;
			message.CloseTime = candle.CloseTime;
			message.OpenPrice = candle.OpenPrice;
			message.HighPrice = candle.HighPrice;
			message.LowPrice = candle.LowPrice;
			message.ClosePrice = candle.ClosePrice;
			message.TotalVolume = candle.TotalVolume;
			message.OpenInterest = candle.OpenInterest;
			message.OpenVolume = candle.OpenVolume;
			message.HighVolume = candle.HighVolume;
			message.LowVolume = candle.LowVolume;
			message.CloseVolume = candle.CloseVolume;
			message.RelativeVolume = candle.RelativeVolume;
			message.Arg = candle.Arg;
			message.PriceLevels = candle.PriceLevels?.Select(l => l.Clone()).ToArray();
			message.State = candle.State;

			return message;
		}

		/// <summary>
		/// To convert the own trade into message.
		/// </summary>
		/// <param name="trade">Own trade.</param>
		/// <returns>Message.</returns>
		public static ExecutionMessage ToMessage(this MyTrade trade)
		{
			if (trade == null)
				throw new ArgumentNullException(nameof(trade));

			var tick = trade.Trade;
			var order = trade.Order;

			return new ExecutionMessage
			{
				TradeId = tick.Id,
				TradeStringId = tick.StringId,
				TradePrice = tick.Price,
				TradeVolume = tick.Volume,
				OriginalTransactionId = order.TransactionId,
				OrderId = order.Id,
				OrderStringId = order.StringId,
				OrderType = order.Type,
				Side = order.Direction,
				OrderPrice = order.Price,
				SecurityId = order.Security.ToSecurityId(),
				PortfolioName = order.Portfolio.Name,
				ExecutionType = ExecutionTypes.Transaction,
				HasOrderInfo = true,
				HasTradeInfo = true,
				ServerTime = tick.Time,
				OriginSide = tick.OrderDirection,
				Currency = tick.Currency,
				Position = trade.Position,
				PnL = trade.PnL,
				Slippage = trade.Slippage,
				Commission = trade.Commission,
			};
		}

		/// <summary>
		/// To convert thee order into message.
		/// </summary>
		/// <param name="order">Order.</param>
		/// <returns>Message.</returns>
		public static ExecutionMessage ToMessage(this Order order)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			var message = new ExecutionMessage
			{
				OrderId = order.Id,
				OrderStringId = order.StringId,
				TransactionId = order.TransactionId,
				OriginalTransactionId = order.TransactionId,
				SecurityId = order.Security.ToSecurityId(),
				PortfolioName = order.Portfolio.Name,
				ExecutionType = ExecutionTypes.Transaction,
				HasOrderInfo = true,
				OrderPrice = order.Price,
				OrderType = order.Type,
				OrderVolume = order.Volume,
				Balance = order.Balance,
				Side = order.Direction,
				OrderState = order.State,
				OrderStatus = order.Status,
				TimeInForce = order.TimeInForce,
				ServerTime = order.LastChangeTime,
				LocalTime = order.LocalTime,
				ExpiryDate = order.ExpiryDate,
				UserOrderId = order.UserOrderId,
				Commission = order.Commission,
				IsSystem = order.IsSystem,
				Comment = order.Comment,
				VisibleVolume = order.VisibleVolume,
				Currency = order.Currency,
			};

			return message;
		}

		/// <summary>
		/// To convert the error description into message.
		/// </summary>
		/// <param name="fail">Error detais.</param>
		/// <returns>Message.</returns>
		public static ExecutionMessage ToMessage(this OrderFail fail)
		{
			if (fail == null)
				throw new ArgumentNullException(nameof(fail));

			return new ExecutionMessage
			{
				OrderId = fail.Order.Id,
				OrderStringId = fail.Order.StringId,
				TransactionId = fail.Order.TransactionId,
				OriginalTransactionId = fail.Order.TransactionId,
				SecurityId = fail.Order.Security.ToSecurityId(),
				PortfolioName = fail.Order.Portfolio.Name,
				Error = fail.Error,
				ExecutionType = ExecutionTypes.Transaction,
				HasOrderInfo = true,
				OrderState = OrderStates.Failed,
				ServerTime = fail.ServerTime,
				LocalTime = fail.LocalTime,
			};
		}

		/// <summary>
		/// To convert the tick trade into message.
		/// </summary>
		/// <param name="trade">Tick trade.</param>
		/// <returns>Message.</returns>
		public static ExecutionMessage ToMessage(this Trade trade)
		{
			if (trade == null)
				throw new ArgumentNullException(nameof(trade));

			return new ExecutionMessage
			{
				LocalTime = trade.LocalTime,
				SecurityId = trade.Security.ToSecurityId(),
				TradeId = trade.Id,
				ServerTime = trade.Time,
				TradePrice = trade.Price,
				TradeVolume = trade.Volume,
				IsSystem = trade.IsSystem,
				TradeStatus = trade.Status,
				OpenInterest = trade.OpenInterest,
				OriginSide = trade.OrderDirection,
				ExecutionType = ExecutionTypes.Tick,
				Currency = trade.Currency,
			};
		}

		/// <summary>
		/// To convert the string of orders log onto message.
		/// </summary>
		/// <param name="item">Order log item.</param>
		/// <returns>Message.</returns>
		public static ExecutionMessage ToMessage(this OrderLogItem item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			var order = item.Order;
			var trade = item.Trade;

			return new ExecutionMessage
			{
				LocalTime = order.LocalTime,
				SecurityId = order.Security.ToSecurityId(),
				OrderId = order.Id,
				OrderStringId = order.StringId,
				TransactionId = order.TransactionId,
				OriginalTransactionId = trade == null ? 0 : order.TransactionId,
				ServerTime = order.Time,
				OrderPrice = order.Price,
				OrderVolume = order.Volume,
				Balance = order.Balance,
				Side = order.Direction,
				IsSystem = order.IsSystem,
				OrderState = order.State,
				OrderStatus = order.Status,
				TimeInForce = order.TimeInForce,
				ExpiryDate = order.ExpiryDate,
				PortfolioName = order.Portfolio?.Name,
				ExecutionType = ExecutionTypes.OrderLog,
				IsCancelled = (order.State == OrderStates.Done && trade == null),
				TradeId = trade?.Id,
				TradePrice = trade?.Price,
				Currency = order.Currency,
			};
		}

		/// <summary>
		/// To create the message of new order registration.
		/// </summary>
		/// <param name="order">Order.</param>
		/// <param name="securityId">Security ID.</param>
		/// <returns>Message.</returns>
		public static OrderRegisterMessage CreateRegisterMessage(this Order order, SecurityId securityId)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			var msg = new OrderRegisterMessage
			{
				TransactionId = order.TransactionId,
				PortfolioName = order.Portfolio.Name,
				Side = order.Direction,
				Price = order.Price,
				Volume = order.Volume,
				VisibleVolume = order.VisibleVolume,
				OrderType = order.Type,
				Comment = order.Comment,
				Condition = order.Condition,
				TimeInForce = order.TimeInForce,
				TillDate = order.ExpiryDate,
				RepoInfo = order.RepoInfo,
				RpsInfo = order.RpsInfo,
				//IsSystem = order.IsSystem,
				UserOrderId = order.UserOrderId,
				BrokerCode = order.BrokerCode,
				ClientCode = order.ClientCode,
				Currency = order.Currency,
			};

			order.Security.ToMessage(securityId).CopyTo(msg);

			return msg;
		}

		/// <summary>
		/// To create the message of cancelling old order.
		/// </summary>
		/// <param name="order">Order.</param>
		/// <param name="securityId">Security ID.</param>
		/// <param name="transactionId">The transaction number.</param>
		/// <param name="volume">The volume been cancelled.</param>
		/// <returns>Message.</returns>
		public static OrderCancelMessage CreateCancelMessage(this Order order, SecurityId securityId, long transactionId, decimal? volume = null)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			var msg = new OrderCancelMessage
			{
				PortfolioName = order.Portfolio.Name,
				OrderType = order.Type,
				OrderTransactionId = order.TransactionId,
				TransactionId = transactionId,
				OrderId = order.Id,
				OrderStringId = order.StringId,
				Volume = volume,
				UserOrderId = order.UserOrderId,
				BrokerCode = order.BrokerCode,
				ClientCode = order.ClientCode,
				Side = order.Direction
			};

			order.Security.ToMessage(securityId).CopyTo(msg);

			return msg;
		}

		/// <summary>
		/// To create the message of replacing old order with new one.
		/// </summary>
		/// <param name="oldOrder">Old order.</param>
		/// <param name="newOrder">New order.</param>
		/// <param name="securityId">Security ID.</param>
		/// <returns>Message.</returns>
		public static OrderReplaceMessage CreateReplaceMessage(this Order oldOrder, Order newOrder, SecurityId securityId)
		{
			if (oldOrder == null)
				throw new ArgumentNullException(nameof(oldOrder));

			if (newOrder == null)
				throw new ArgumentNullException(nameof(newOrder));

			var msg = new OrderReplaceMessage
			{
				TransactionId = newOrder.TransactionId,
				PortfolioName = newOrder.Portfolio.Name,
				Side = newOrder.Direction,
				Price = newOrder.Price,
				Volume = newOrder.Volume,
				VisibleVolume = newOrder.VisibleVolume,
				OrderType = newOrder.Type,
				Comment = newOrder.Comment,
				Condition = newOrder.Condition,
				TimeInForce = newOrder.TimeInForce,
				TillDate = newOrder.ExpiryDate,
				RepoInfo = newOrder.RepoInfo,
				RpsInfo = newOrder.RpsInfo,
				//IsSystem = newOrder.IsSystem,

				OldOrderId = oldOrder.Id,
				OldOrderStringId = oldOrder.StringId,
				OldTransactionId = oldOrder.TransactionId,

				UserOrderId = oldOrder.UserOrderId,

				BrokerCode = oldOrder.BrokerCode,
				ClientCode = oldOrder.ClientCode,

				Currency = newOrder.Currency,
			};

			oldOrder.Security.ToMessage(securityId).CopyTo(msg);

			return msg;
		}

		/// <summary>
		/// To create the message of replacing pair of old orders to new ones.
		/// </summary>
		/// <param name="oldOrder1">Old order.</param>
		/// <param name="newOrder1">New order.</param>
		/// <param name="security1">Security ID.</param>
		/// <param name="oldOrder2">Old order.</param>
		/// <param name="newOrder2">New order.</param>
		/// <param name="security2">Security ID.</param>
		/// <returns>Message.</returns>
		public static OrderPairReplaceMessage CreateReplaceMessage(this Order oldOrder1, Order newOrder1, SecurityId security1,
			Order oldOrder2, Order newOrder2, SecurityId security2)
		{
			var msg = new OrderPairReplaceMessage
			{
				Message1 = oldOrder1.CreateReplaceMessage(newOrder1, security1),
				Message2 = oldOrder2.CreateReplaceMessage(newOrder2, security2)
			};

			oldOrder1.Security.ToMessage(security1).CopyTo(msg);

			return msg;
		}

		/// <summary>
		/// To create the message of orders mass cancelling.
		/// </summary>
		/// <param name="transactionId">Transaction ID.</param>
		/// <param name="isStopOrder"><see langword="true" />, if cancel only a stop orders, <see langword="false" /> - if regular orders, <see langword="null" /> - both.</param>
		/// <param name="portfolio">Portfolio. If the value is equal to <see langword="null" />, then the portfolio does not match the orders cancel filter.</param>
		/// <param name="direction">Order side. If the value is <see langword="null" />, the direction does not use.</param>
		/// <param name="board">Trading board. If the value is equal to <see langword="null" />, then the board does not match the orders cancel filter.</param>
		/// <param name="securityId">Security ID.</param>
		/// <param name="security">Security.</param>
		/// <returns>Message.</returns>
		public static OrderGroupCancelMessage CreateGroupCancelMessage(long transactionId, bool? isStopOrder, Portfolio portfolio, Sides? direction, ExchangeBoard board, SecurityId securityId, Security security)
		{
			var msg = new OrderGroupCancelMessage
			{
				TransactionId = transactionId,

				SecurityId = new SecurityId
				{
					BoardCode = board == null ? null : board.Code,
					SecurityCode = securityId.SecurityCode,
					Native = securityId.Native,
				},
			};

			if (portfolio != null)
				msg.PortfolioName = portfolio.Name;

			if (isStopOrder != null)
				msg.OrderType = isStopOrder == true ? OrderTypes.Conditional : OrderTypes.Limit;

			if (direction != null)
				msg.Side = direction.Value;

			if (security != null)
				security.ToMessage(securityId).CopyTo(msg);

			return msg;
		}

		/// <summary>
		/// To convert the instrument into message.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="securityId">Security ID.</param>
		/// <returns>Message.</returns>
		public static SecurityMessage ToMessage(this Security security, SecurityId? securityId = null)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return new SecurityMessage
			{
				SecurityId = securityId ?? security.ToSecurityId(),
				Name = security.Name,
				ShortName = security.ShortName,
				PriceStep = security.PriceStep,
				Decimals = security.Decimals,
				VolumeStep = security.VolumeStep,
				Multiplier = security.Multiplier,
				Currency = security.Currency,
				SecurityType = security.Type,
				OptionType = security.OptionType,
				Strike = security.Strike,
				BinaryOptionType = security.BinaryOptionType,
				UnderlyingSecurityCode = security.UnderlyingSecurityId.IsEmpty() ? null : security.UnderlyingSecurityId.Split('@')[0],
				SettlementDate = security.SettlementDate,
				ExpiryDate = security.ExpiryDate,
			};
		}

		/// <summary>
		/// Convert <see cref="Security"/> criteria to <see cref="SecurityLookupMessage"/>.
		/// </summary>
		/// <param name="criteria">Criteria.</param>
		/// <param name="securityId">Security ID.</param>
		/// <returns>Message.</returns>
		public static SecurityLookupMessage ToLookupMessage(this Security criteria, SecurityId? securityId = null)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			return new SecurityLookupMessage
			{
				//LocalTime = CurrentTime,
				SecurityId = securityId ?? criteria.ToSecurityId(),
				Name = criteria.Name,
				Class = criteria.Class,
				SecurityType = criteria.Type,
				ExpiryDate = criteria.ExpiryDate,
				ShortName = criteria.ShortName,
				VolumeStep = criteria.VolumeStep,
				Multiplier = criteria.Multiplier,
				PriceStep = criteria.PriceStep,
				Decimals = criteria.Decimals,
				Currency = criteria.Currency,
				SettlementDate = criteria.SettlementDate,
				OptionType = criteria.OptionType,
				Strike = criteria.Strike,
				BinaryOptionType = criteria.BinaryOptionType,
				UnderlyingSecurityCode = criteria.UnderlyingSecurityId.IsEmpty() ? null : _defaultGenerator.Split(criteria.UnderlyingSecurityId).SecurityCode
			};
		}

		/// <summary>
		/// To convert the message into instrument.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>Security.</returns>
		public static Security ToSecurity(this SecurityMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			return new Security
			{
				Id = message.SecurityId.ToStringId(),
				Code = message.SecurityId.SecurityCode,
				Board = ExchangeBoard.GetOrCreateBoard(message.SecurityId.BoardCode),
				Type = message.SecurityType ?? message.SecurityId.SecurityType,
				Strike = message.Strike,
				OptionType = message.OptionType,
				Name = message.Name,
				ShortName = message.ShortName,
				Class = message.Class,
				BinaryOptionType = message.BinaryOptionType,
				ExternalId = message.SecurityId.ToExternalId(),
				ExpiryDate = message.ExpiryDate,
				SettlementDate = message.SettlementDate,
				UnderlyingSecurityId = message.UnderlyingSecurityCode + "@" + message.SecurityId.BoardCode,
				Currency = message.Currency,
				PriceStep = message.PriceStep,
				Decimals = message.Decimals,
				VolumeStep = message.VolumeStep,
				Multiplier = message.Multiplier,
				ExtensionInfo = new Dictionary<object, object>()
			};
		}

		private static readonly SecurityIdGenerator _defaultGenerator = new SecurityIdGenerator();

		/// <summary>
		/// Convert <see cref="SecurityId"/> to <see cref="Security.Id"/> value.
		/// </summary>
		/// <param name="securityId"><see cref="SecurityId"/> value.</param>
		/// <param name="generator">The instrument identifiers generator <see cref="Security.Id"/>. Can be <see langword="null"/>.</param>
		/// <returns><see cref="Security.Id"/> value.</returns>
		public static string ToStringId(this SecurityId securityId, SecurityIdGenerator generator = null)
		{
			return (generator ?? _defaultGenerator).GenerateId(securityId.SecurityCode, securityId.BoardCode);
		}

		/// <summary>
		/// To convert the portfolio into message.
		/// </summary>
		/// <param name="portfolio">Portfolio.</param>
		/// <returns>Message.</returns>
		public static PortfolioMessage ToMessage(this Portfolio portfolio)
		{
			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			return new PortfolioMessage
			{
				PortfolioName = portfolio.Name,
				BoardCode = portfolio.Board == null ? null : portfolio.Board.Code,
				Currency = portfolio.Currency,
			};
		}

		/// <summary>
		/// To convert the portfolio into message.
		/// </summary>
		/// <param name="portfolio">Portfolio.</param>
		/// <returns>Message.</returns>
		public static PortfolioChangeMessage ToChangeMessage(this Portfolio portfolio)
		{
			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			return new PortfolioChangeMessage
			{
				PortfolioName = portfolio.Name,
				BoardCode = portfolio.Board == null ? null : portfolio.Board.Code,
				LocalTime = portfolio.LocalTime,
				ServerTime = portfolio.LastChangeTime,
			}
			.Add(PositionChangeTypes.BeginValue, portfolio.BeginValue)
			.Add(PositionChangeTypes.CurrentValue, portfolio.CurrentValue);
		}

		/// <summary>
		/// To convert the position into message.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <returns>Message.</returns>
		public static PositionMessage ToMessage(this Position position)
		{
			if (position == null)
				throw new ArgumentNullException(nameof(position));

			return new PositionMessage
			{
				PortfolioName = position.Portfolio.Name,
				SecurityId = position.Security.ToSecurityId(),
				DepoName = position.DepoName,
				LimitType = position.LimitType,
			};
		}

		/// <summary>
		/// To convert the position into message.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <returns>Message.</returns>
		public static PositionChangeMessage ToChangeMessage(this Position position)
		{
			if (position == null)
				throw new ArgumentNullException(nameof(position));

			return new PositionChangeMessage
			{
				LocalTime = position.LocalTime,
				ServerTime = position.LastChangeTime,
				PortfolioName = position.Portfolio.Name,
				SecurityId = position.Security.ToSecurityId(),
			}
			.Add(PositionChangeTypes.BeginValue, position.CurrentValue)
			.Add(PositionChangeTypes.CurrentValue, position.CurrentValue)
			.Add(PositionChangeTypes.BlockedValue, position.BlockedValue);
		}

		/// <summary>
		/// To convert the board into message.
		/// </summary>
		/// <param name="board">Board.</param>
		/// <returns>Message.</returns>
		public static BoardMessage ToMessage(this ExchangeBoard board)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			return new BoardMessage
			{
				Code = board.Code,
				ExchangeCode = board.Exchange.Name,
				WorkingTime = board.WorkingTime.Clone(),
				IsSupportMarketOrders = board.IsSupportMarketOrders,
				IsSupportAtomicReRegister = board.IsSupportAtomicReRegister,
				ExpiryTime = board.ExpiryTime,
				TimeZone = board.TimeZone
			};
		}

		/// <summary>
		/// To convert the message into exchange.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>Exchange.</returns>
		public static Exchange ToExchange(this BoardMessage message)
		{
			return message.ToExchange(new Exchange { Name = message.ExchangeCode });
		}

		/// <summary>
		/// To convert the message into exchange.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="exchange">Exchange.</param>
		/// <returns>Exchange.</returns>
		public static Exchange ToExchange(this BoardMessage message, Exchange exchange)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (exchange == null)
				throw new ArgumentNullException(nameof(exchange));

			return exchange;
		}

		/// <summary>
		/// To convert the message into board.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>Board.</returns>
		public static ExchangeBoard ToBoard(this BoardMessage message)
		{
			return message.ToBoard(new ExchangeBoard { Code = message.Code, Exchange = new Exchange { Name = message.ExchangeCode } });
		}

		/// <summary>
		/// To convert the message into board.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="board">Board.</param>
		/// <returns>Board.</returns>
		public static ExchangeBoard ToBoard(this BoardMessage message, ExchangeBoard board)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (board == null)
				throw new ArgumentNullException(nameof(board));

			board.WorkingTime = message.WorkingTime;
			board.IsSupportAtomicReRegister = message.IsSupportAtomicReRegister;
			board.IsSupportMarketOrders = message.IsSupportMarketOrders;
			board.ExpiryTime = message.ExpiryTime;
			board.TimeZone = message.TimeZone;

			return board;
		}

		private class ToMessagesEnumerable<TEntity, TMessage> : IEnumerable<TMessage>
		{
			private readonly IEnumerable<TEntity> _entities;

			public ToMessagesEnumerable(IEnumerable<TEntity> entities)
			{
				if (entities == null)
					throw new ArgumentNullException(nameof(entities));

				_entities = entities;
			}

			public IEnumerator<TMessage> GetEnumerator()
			{
				return _entities.Select(Convert).GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			//int IEnumerableEx.Count => _entities.Count;

			private static TMessage Convert(TEntity value)
			{
				if (value is OrderLogItem)
					return value.To<OrderLogItem>().ToMessage().To<TMessage>();
				else if (value is MarketDepth)
					return value.To<MarketDepth>().ToMessage().To<TMessage>();
				else if (value is Trade)
					return value.To<Trade>().ToMessage().To<TMessage>();
				else if (value is Candle)
					return value.To<Candle>().ToMessage().To<TMessage>();
				else
					throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// To convert trading objects into messages.
		/// </summary>
		/// <typeparam name="TEntity">The type of trading object.</typeparam>
		/// <typeparam name="TMessage">Message type.</typeparam>
		/// <param name="entities">Trading objects.</param>
		/// <returns>Messages.</returns>
		public static IEnumerable<TMessage> ToMessages<TEntity, TMessage>(this IEnumerable<TEntity> entities)
		{
			return new ToMessagesEnumerable<TEntity, TMessage>(entities);
		}

		private class ToEntitiesEnumerable<TMessage, TEntity> : IEnumerable<TEntity>
			where TMessage : Message
		{
			private readonly IEnumerable<TMessage> _messages;
			private readonly Security _security;
			//private readonly object _candleArg;
			private readonly Type _candleType;

			public ToEntitiesEnumerable(IEnumerable<TMessage> messages, Security security)
			{
				if (messages == null)
					throw new ArgumentNullException(nameof(messages));

				if (security == null)
					throw new ArgumentNullException(nameof(security));

				_messages = messages;
				_security = security;
			}
			
			public ToEntitiesEnumerable(IEnumerable<TMessage> messages, Security security, Type candleType)
				: this (messages, security)
			{
				_candleType = candleType;
				//_candleArg = candleArg;
			}

			public IEnumerator<TEntity> GetEnumerator()
			{
				return _messages.Select(Convert).GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			//int IEnumerableEx.Count => _messages.Count;

			private TEntity Convert(TMessage message)
			{
				switch (message.Type)
				{
					case MessageTypes.Execution:
					{
						var execMsg = message.To<ExecutionMessage>();

						switch (execMsg.ExecutionType)
						{
							case ExecutionTypes.Tick:
								return execMsg.ToTrade(_security).To<TEntity>();

							case ExecutionTypes.OrderLog:
								return execMsg.ToOrderLog(_security).To<TEntity>();

							case ExecutionTypes.Transaction:
								return execMsg.ToOrder(_security).To<TEntity>();

							default:
								throw new ArgumentOutOfRangeException(nameof(message), LocalizedStrings.Str1122Params.Put(execMsg.ExecutionType));
						}
					}

					case MessageTypes.QuoteChange:
						return message.To<QuoteChangeMessage>().ToMarketDepth(_security).To<TEntity>();

					case MessageTypes.News:
						return message.To<NewsMessage>().ToNews().To<TEntity>();

					default:
					{
						var candleMsg = message as CandleMessage;

						if (candleMsg == null)
							throw new ArgumentOutOfRangeException();

						return candleMsg.ToCandle(_candleType, _security).To<TEntity>();
					}
				}
			}
		}

		/// <summary>
		/// To convert messages into trading objects.
		/// </summary>
		/// <typeparam name="TMessage">Message type.</typeparam>
		/// <typeparam name="TEntity">The type of trading object.</typeparam>
		/// <param name="messages">Messages.</param>
		/// <param name="security">Security.</param>
		/// <returns>Trading objects.</returns>
		public static IEnumerable<TEntity> ToEntities<TMessage, TEntity>(this IEnumerable<TMessage> messages, Security security)
			where TMessage : Message
		{
			return new ToEntitiesEnumerable<TMessage, TEntity>(messages, security);
		}

		/// <summary>
		/// To convert messages into trading objects.
		/// </summary>
		/// <typeparam name="TCandle">The candle type.</typeparam>
		/// <param name="messages">Messages.</param>
		/// <param name="security">Security.</param>
		/// <param name="candleType">The type of the candle. It is used, if <typeparamref name="TCandle" /> equals to <see cref="Candle"/>.</param>
		/// <returns>Trading objects.</returns>
		public static IEnumerable<TCandle> ToCandles<TCandle>(this IEnumerable<CandleMessage> messages, Security security, Type candleType = null)
		{
			return new ToEntitiesEnumerable<CandleMessage, TCandle>(messages, security, candleType ?? typeof(TCandle));
		}

		/// <summary>
		/// To convert <see cref="CandleMessage"/> into candle.
		/// </summary>
		/// <typeparam name="TCandle">The candle type.</typeparam>
		/// <param name="message">Message.</param>
		/// <param name="series">Series.</param>
		/// <returns>Candle.</returns>
		public static TCandle ToCandle<TCandle>(this CandleMessage message, CandleSeries series)
			where TCandle : Candle, new()
		{
			return (TCandle)message.ToCandle(series);
		}

		/// <summary>
		/// To convert <see cref="CandleMessage"/> into candle.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="series">Series.</param>
		/// <returns>Candle.</returns>
		public static Candle ToCandle(this CandleMessage message, CandleSeries series)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var candle = message.ToCandle(series.CandleType, series.Security);
			//candle.Series = series;

			if (candle.Arg.IsNull(true))
				candle.Arg = series.Arg;

			return candle;
		}

		/// <summary>
		/// To convert <see cref="CandleMessage"/> into candle.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="security">Security.</param>
		/// <returns>Candle.</returns>
		public static Candle ToCandle(this CandleMessage message, Security security)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			return message.ToCandle(message.GetType().ToCandleType(), security);
		}

		/// <summary>
		/// To convert <see cref="CandleMessage"/> into candle.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="type">The candle type.</param>
		/// <param name="security">Security.</param>
		/// <returns>Candle.</returns>
		public static Candle ToCandle(this CandleMessage message, Type type, Security security)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (type == null)
				throw new ArgumentNullException(nameof(type));

			//if (arg == null)
			//	throw new ArgumentNullException("arg");

			var candle = type.CreateInstance<Candle>();

			candle.Security = security;
			candle.Arg = message.Arg;

			candle.OpenPrice = message.OpenPrice;
			candle.OpenVolume = message.OpenVolume;
			candle.OpenTime = message.OpenTime;

			candle.HighPrice = message.HighPrice;
			candle.HighVolume = message.HighVolume;
			candle.HighTime = message.HighTime;

			candle.LowPrice = message.LowPrice;
			candle.LowVolume = message.LowVolume;
			candle.LowTime = message.LowTime;

			candle.ClosePrice = message.ClosePrice;
			candle.CloseVolume = message.CloseVolume;
			candle.CloseTime = message.CloseTime;

			candle.TotalVolume = message.TotalVolume;
			candle.RelativeVolume = message.RelativeVolume;

			candle.OpenInterest = message.OpenInterest;

			candle.TotalTicks = message.TotalTicks;
			candle.UpTicks = message.UpTicks;
			candle.DownTicks = message.DownTicks;

			candle.PriceLevels = message.PriceLevels?.Select(l => l.Clone()).ToArray();

			candle.State = message.State;

			return candle;
		}

		/// <summary>
		/// To convert the message into tick trade.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="security">Security.</param>
		/// <returns>Tick trade.</returns>
		public static Trade ToTrade(this ExecutionMessage message, Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return message.ToTrade(new Trade { Security = security });
		}

		/// <summary>
		/// To convert the message into tick trade.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="trade">Tick trade.</param>
		/// <returns>Tick trade.</returns>
		public static Trade ToTrade(this ExecutionMessage message, Trade trade)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			trade.Id = message.TradeId ?? 0;
			trade.Price = message.TradePrice ?? 0;
			trade.Volume = message.TradeVolume ?? 0;
			trade.Status = message.TradeStatus;
			trade.IsSystem = message.IsSystem;
			trade.Time = message.ServerTime;
			trade.LocalTime = message.LocalTime;
			trade.OpenInterest = message.OpenInterest;
			trade.OrderDirection = message.OriginSide;
			trade.IsUpTick = message.IsUpTick;
			trade.Currency = message.Currency;

			return trade;
		}

		/// <summary>
		/// To convert the message into order.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="security">Security.</param>
		/// <returns>Order.</returns>
		public static Order ToOrder(this ExecutionMessage message, Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return message.ToOrder(new Order { Security = security });
		}

		/// <summary>
		/// To convert the message into order.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="order">The order.</param>
		/// <returns>Order.</returns>
		public static Order ToOrder(this ExecutionMessage message, Order order)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			order.Id = message.OrderId;
			order.StringId = message.OrderStringId;
			order.TransactionId = message.TransactionId;
			order.Portfolio = new Portfolio { Board = order.Security.Board, Name = message.PortfolioName };
			order.Direction = message.Side;
			order.Price = message.OrderPrice;
			order.Volume = message.OrderVolume ?? 0;
			order.Balance = message.Balance ?? 0;
			order.VisibleVolume = message.VisibleVolume;
			order.Type = message.OrderType;
			order.Status = message.OrderStatus;
			order.IsSystem = message.IsSystem;
			order.Time = message.ServerTime;
			order.LastChangeTime = message.ServerTime;
			order.LocalTime = message.LocalTime;
			order.TimeInForce = message.TimeInForce;
			order.ExpiryDate = message.ExpiryDate;
			order.UserOrderId = message.UserOrderId;
			order.Comment = message.Comment;
			order.Commission = message.Commission;
			order.Currency = message.Currency;

			if (message.OrderState != null)
				order.State = (OrderStates)message.OrderState;

			return order;
		}

		/// <summary>
		/// To convert the message into order book.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="security">Security.</param>
		/// <param name="getSecurity">The function for getting instrument.</param>
		/// <returns>Market depth.</returns>
		public static MarketDepth ToMarketDepth(this QuoteChangeMessage message, Security security, Func<SecurityId, Security> getSecurity = null)
		{
			return message.ToMarketDepth(new MarketDepth(security), getSecurity);
		}

		/// <summary>
		/// To convert the message into order book.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="marketDepth">Market depth.</param>
		/// <param name="getSecurity">The function for getting instrument.</param>
		/// <returns>Market depth.</returns>
		public static MarketDepth ToMarketDepth(this QuoteChangeMessage message, MarketDepth marketDepth, Func<SecurityId, Security> getSecurity = null)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (marketDepth == null)
				throw new ArgumentNullException(nameof(marketDepth));

			var security = marketDepth.Security;

			var depth = marketDepth.Update(
				message.Bids.Select(c => c.ToQuote(security, getSecurity)),
				message.Asks.Select(c => c.ToQuote(security, getSecurity)),
				message.IsSorted, message.ServerTime);

			depth.LocalTime = message.LocalTime;
			depth.Currency = message.Currency;

			return depth;
		}

		/// <summary>
		/// To convert the quote into message.
		/// </summary>
		/// <param name="quote">Quote.</param>
		/// <returns>Message.</returns>
		public static QuoteChange ToQuoteChange(this Quote quote)
		{
			return new QuoteChange(quote.OrderDirection, quote.Price, quote.Volume);
		}

		/// <summary>
		/// To convert the message into quote.
		/// </summary>
		/// <param name="change">Message.</param>
		/// <param name="security">Security.</param>
		/// <param name="getSecurity">The function for getting instrument.</param>
		/// <returns>Quote.</returns>
		public static Quote ToQuote(this QuoteChange change, Security security, Func<SecurityId, Security> getSecurity = null)
		{
			if (!change.BoardCode.IsEmpty() && getSecurity != null)
				security = getSecurity(new SecurityId { SecurityCode = security.Code, BoardCode = change.BoardCode });

			var quote = new Quote(security, change.Price, change.Volume, change.Side);
			change.CopyExtensionInfo(quote);
			return quote;
		}

		/// <summary>
		/// To convert the message into orders log string.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="security">Security.</param>
		/// <returns>Order log item.</returns>
		public static OrderLogItem ToOrderLog(this ExecutionMessage message, Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return message.ToOrderLog(new OrderLogItem
			{
				Order = new Order { Security = security },
				Trade = message.TradeId != null ? new Trade { Security = security } : null
			});
		}

		/// <summary>
		/// To convert the message into orders log string.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="item">Order log item.</param>
		/// <returns>Order log item.</returns>
		public static OrderLogItem ToOrderLog(this ExecutionMessage message, OrderLogItem item)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (item == null)
				throw new ArgumentNullException(nameof(item));

			var order = item.Order;

			order.Portfolio = Portfolio.AnonymousPortfolio;

			order.Id = message.OrderId;
			order.StringId = message.OrderStringId;
			order.TransactionId = message.TransactionId;
			order.Price = message.OrderPrice;
			order.Volume = message.OrderVolume ?? 0;
			order.Balance = message.Balance ?? 0;
			order.Direction = message.Side;
			order.Time = message.ServerTime;
			order.LastChangeTime = message.ServerTime;
			order.LocalTime = message.LocalTime;
			
			order.Status = message.OrderStatus;
			order.TimeInForce = message.TimeInForce;
			order.IsSystem = message.IsSystem;
			order.Currency = message.Currency;

			if (message.OrderState != null)
				order.State = message.OrderState.Value;
			else
				order.State = message.IsCancelled || message.TradeId != null ? OrderStates.Done : OrderStates.Active;

			if (message.TradeId != null)
			{
				var trade = item.Trade;

				trade.Id = message.TradeId ?? 0;
				trade.Price = message.TradePrice ?? 0;
				trade.Time = message.ServerTime;
				trade.Volume = message.OrderVolume ?? 0;
				trade.IsSystem = message.IsSystem;
				trade.Status = message.TradeStatus;
			}

			return item;
		}

		/// <summary>
		/// To convert news into message.
		/// </summary>
		/// <param name="news">News.</param>
		/// <returns>Message.</returns>
		public static NewsMessage ToMessage(this News news)
		{
			if (news == null)
				throw new ArgumentNullException(nameof(news));

			return new NewsMessage
			{
				LocalTime = news.LocalTime,
				ServerTime = news.ServerTime,
				Id = news.Id,
				Story = news.Story,
				Source = news.Source,
				Headline = news.Headline,
				SecurityId = news.Security == null ? (SecurityId?)null : news.Security.ToSecurityId(),
				BoardCode = news.Board == null ? string.Empty : news.Board.Code,
				Url = news.Url,
			};
		}

		/// <summary>
		/// To convert the instrument into <see cref="SecurityId"/>.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="idGenerator">The instrument identifiers generator <see cref="Security.Id"/>.</param>
		/// <returns>Security ID.</returns>
		public static SecurityId ToSecurityId(this Security security, SecurityIdGenerator idGenerator = null)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			string secCode;
			string boardCode;

			// http://stocksharp.com/forum/yaf_postsm32581_Security-SPFB-RTS-FORTS.aspx#post32581
			// иногда в Security.Code может быть записано неправильное, и необходимо опираться на Security.Id
			if (!security.Id.IsEmpty())
			{
				var id = (idGenerator ?? new SecurityIdGenerator()).Split(security.Id);

				secCode = id.SecurityCode;

				// http://stocksharp.com/forum/yaf_postst5143findunread_API-4-2-4-0-Nie-vystavliaiutsia-zaiavki-po-niekotorym-instrumientam-FORTS.aspx
				// для Quik необходимо соблюдение регистра в коде инструмента при выставлении заявок
				if (secCode.CompareIgnoreCase(security.Code))
					secCode = security.Code;

				//if (!boardCode.CompareIgnoreCase(ExchangeBoard.Test.Code))
				boardCode = id.BoardCode;
			}
			else
			{
				if (security.Code.IsEmpty())
					throw new ArgumentException(LocalizedStrings.Str1123);

				if (security.Board == null)
					throw new ArgumentException(LocalizedStrings.Str1124Params.Put(security.Code));

				secCode = security.Code;
				boardCode = security.Board.Code;
			}

			return security.ExternalId.ToSecurityId(secCode, boardCode, security.Type);
		}

		/// <summary>
		/// Cast <see cref="SecurityId"/> to the <see cref="SecurityExternalId"/>.
		/// </summary>
		/// <param name="securityId"><see cref="SecurityId"/>.</param>
		/// <returns><see cref="SecurityExternalId"/>.</returns>
		public static SecurityExternalId ToExternalId(this SecurityId securityId)
		{
			return new SecurityExternalId
			{
				Bloomberg = securityId.Bloomberg,
				Cusip = securityId.Cusip,
				IQFeed = securityId.IQFeed,
				Isin = securityId.Isin,
				Ric = securityId.Ric,
				Sedol = securityId.Sedol,
				InteractiveBrokers = securityId.InteractiveBrokers,
				Plaza = securityId.Plaza,
			};
		}

		/// <summary>
		/// To check, if <see cref="SecurityId"/> contains identifiers of external sources.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <returns><see langword="true" />, if there are identifiers of external sources, otherwise, <see langword="false" />.</returns>
		public static bool HasExternalId(this SecurityId securityId)
		{
			return !securityId.Bloomberg.IsEmpty() ||
				   !securityId.Cusip.IsEmpty() ||
				   !securityId.IQFeed.IsEmpty() ||
				   !securityId.Isin.IsEmpty() ||
				   !securityId.Ric.IsEmpty() ||
				   !securityId.Sedol.IsEmpty() ||
				   !securityId.InteractiveBrokers.IsDefault() ||
				   !securityId.Plaza.IsEmpty();
		}

		/// <summary>
		/// Cast <see cref="SecurityExternalId"/> to the <see cref="SecurityId"/>.
		/// </summary>
		/// <param name="externalId"><see cref="SecurityExternalId"/>.</param>
		/// <param name="securityCode">Security code.</param>
		/// <param name="boardCode">Board code.</param>
		/// <param name="securityType">Security type.</param>
		/// <returns><see cref="SecurityId"/>.</returns>
		public static SecurityId ToSecurityId(this SecurityExternalId externalId, string securityCode, string boardCode, SecurityTypes? securityType)
		{
			//if (externalId == null)
			//	throw new ArgumentNullException(nameof(externalId));

			return new SecurityId
			{
				SecurityCode = securityCode,
				BoardCode = boardCode,
				SecurityType = securityType,
				Bloomberg = externalId.Bloomberg,
				Cusip = externalId.Cusip,
				IQFeed = externalId.IQFeed,
				Isin = externalId.Isin,
				Ric = externalId.Ric,
				Sedol = externalId.Sedol,
				InteractiveBrokers = externalId.InteractiveBrokers,
				Plaza = externalId.Plaza,
			};
		}

		/// <summary>
		/// To fill the message with information about instrument.
		/// </summary>
		/// <param name="message">The message for market data subscription.</param>
		/// <param name="connector">Connection to the trading system.</param>
		/// <param name="security">Security.</param>
		/// <returns>The message for market data subscription.</returns>
		public static MarketDataMessage FillSecurityInfo(this MarketDataMessage message, Connector connector, Security security)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (connector == null)
				throw new ArgumentNullException(nameof(connector));

			if (security == null)
				throw new ArgumentNullException(nameof(security));

			security.ToMessage(connector.GetSecurityId(security)).CopyTo(message);
			return message;
		}

		/// <summary>
		/// Cast <see cref="Level1ChangeMessage"/> to the <see cref="MarketDepth"/>.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="security">Security.</param>
		/// <returns>Market depth.</returns>
		public static MarketDepth ToMarketDepth(this Level1ChangeMessage message, Security security)
		{
			return new MarketDepth(security) { LocalTime = message.LocalTime }.Update(
				new[] { message.CreateQuote(security, Sides.Buy, Level1Fields.BestBidPrice, Level1Fields.BestBidVolume) },
				new[] { message.CreateQuote(security, Sides.Sell, Level1Fields.BestAskPrice, Level1Fields.BestAskVolume) },
				true, message.ServerTime);
		}

		private static Quote CreateQuote(this Level1ChangeMessage message, Security security, Sides side, Level1Fields priceField, Level1Fields volumeField)
		{
			var changes = message.Changes;
			return new Quote(security, (decimal)changes[priceField], (decimal?)changes.TryGetValue(volumeField) ?? 0m, side);
		}

		/// <summary>
		/// Cast <see cref="NewsMessage"/> to the <see cref="News"/>.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>News.</returns>
		public static News ToNews(this NewsMessage message)
		{
			return new News
			{
				Id = message.Id,
				Source = message.Source,
				ServerTime = message.ServerTime,
				Story = message.Story,
				Url = message.Url,
				Headline = message.Headline,
				Board = message.BoardCode.IsEmpty() ? null : ExchangeBoard.GetOrCreateBoard(message.BoardCode),
				LocalTime = message.LocalTime,
				Security = message.SecurityId == null ? null : new Security
				{
					Id = message.SecurityId.Value.SecurityCode
				}
			};
		}

		/// <summary>
		/// Cast <see cref="PortfolioMessage"/> to the <see cref="Portfolio"/>.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="portfolio">Portfolio.</param>
		/// <returns>Portfolio.</returns>
		public static Portfolio ToPortfolio(this PortfolioMessage message, Portfolio portfolio)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			portfolio.Board = message.BoardCode.IsEmpty() ? null : ExchangeBoard.GetOrCreateBoard(message.BoardCode);

			if (message.Currency != null)
				portfolio.Currency = message.Currency;

			if (message.State != null)
				portfolio.State = message.State;

			message.CopyExtensionInfo(portfolio);

			return portfolio;
		}

		/// <summary>
		/// To convert the type of business object into type of message.
		/// </summary>
		/// <param name="dataType">The type of business object.</param>
		/// <param name="arg">The data parameter.</param>
		/// <returns>Message type.</returns>
		public static Type ToMessageType(this Type dataType, ref object arg)
		{
			if (dataType == typeof(Trade))
			{
				arg = ExecutionTypes.Tick;
				return typeof(ExecutionMessage);
			}
			else if (dataType == typeof(MarketDepth))
				return typeof(QuoteChangeMessage);
			else if (dataType == typeof(Order) || dataType == typeof(MyTrade))
			{
				arg = ExecutionTypes.Transaction;
				return typeof(ExecutionMessage);
			}
			else if (dataType == typeof(OrderLogItem))
			{
				arg = ExecutionTypes.OrderLog;
				return typeof(ExecutionMessage);
			}
			else if (dataType.IsCandle())
			{
				if (arg == null)
					throw new ArgumentNullException(nameof(arg));

				return dataType.ToCandleMessageType();
			}
			else if (dataType == typeof(News))
				return typeof(NewsMessage);
			else if (dataType == typeof(Security))
				return typeof(SecurityMessage);
			else
				throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.Str721);
		}
	}
}
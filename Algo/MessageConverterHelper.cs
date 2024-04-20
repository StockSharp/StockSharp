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
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The auxiliary class for conversion of business-objects (<see cref="BusinessEntities"/>) into messages (<see cref="Messages"/>) and vice versa.
	/// </summary>
	public static class MessageConverterHelper
	{
		static MessageConverterHelper()
		{
#pragma warning disable CS0618 // Type or member is obsolete
			RegisterCandle(() => new TimeFrameCandle(), () => new TimeFrameCandleMessage());
			RegisterCandle(() => new TickCandle(), () => new TickCandleMessage());
			RegisterCandle(() => new VolumeCandle(), () => new VolumeCandleMessage());
			RegisterCandle(() => new RangeCandle(), () => new RangeCandleMessage());
			RegisterCandle(() => new PnFCandle(), () => new PnFCandleMessage());
			RegisterCandle(() => new RenkoCandle(), () => new RenkoCandleMessage());
			RegisterCandle(() => new HeikinAshiCandle(), () => new HeikinAshiCandleMessage());
#pragma warning restore CS0618 // Type or member is obsolete
		}

		/// <summary>
		/// Cast <see cref="MarketDepth"/> to the <see cref="QuoteChangeMessage"/>.
		/// </summary>
		/// <param name="depth"><see cref="MarketDepth"/>.</param>
		/// <returns><see cref="QuoteChangeMessage"/>.</returns>
		[Obsolete("Use IOrderBookMessage.")]
		public static QuoteChangeMessage ToMessage(this MarketDepth depth)
		{
			if (depth == null)
				throw new ArgumentNullException(nameof(depth));

			var securityId = depth.Security.ToSecurityId();

			return new QuoteChangeMessage
			{
				LocalTime = depth.LocalTime,
				SecurityId = securityId,
				Bids = depth.Bids.ToArray(),
				Asks = depth.Asks.ToArray(),
				ServerTime = depth.ServerTime,
				Currency = depth.Currency,
				SeqNum = depth.SeqNum,
				BuildFrom = depth.BuildFrom,
			};
		}

		private static readonly CachedSynchronizedPairSet<Type, Type> _candleTypes = new();

		/// <summary>
		/// Cast candle type <see cref="Candle"/> to the message <see cref="CandleMessage"/>.
		/// </summary>
		/// <param name="candleType">The type of the candle <see cref="Candle"/>.</param>
		/// <returns>The type of the message <see cref="CandleMessage"/>.</returns>
		public static Type ToCandleMessageType(this Type candleType)
		{
			if (candleType is null)
				throw new ArgumentNullException(nameof(candleType));

			if (!_candleTypes.TryGetValue(candleType, out var messageType))
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
			if (messageType is null)
				throw new ArgumentNullException(nameof(messageType));

			if (!_candleTypes.TryGetKey(messageType, out var candleType))
				throw new ArgumentOutOfRangeException(nameof(messageType), messageType, LocalizedStrings.WrongCandleType);

			return candleType;
		}

		/// <summary>
		/// To convert the candle into message.
		/// </summary>
		/// <param name="candle">Candle.</param>
		/// <returns>Message.</returns>
		[Obsolete("Conversion reduce performance.")]
		public static CandleMessage ToMessage(this Candle candle)
		{
			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

			if (!_candleTypes.TryGetValue(candle.GetType(), out var messageType))
				throw new ArgumentException(LocalizedStrings.UnknownCandleType.Put(candle.GetType()), nameof(candle));

			var message = CreateCandleMessage(messageType);

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
			message.BuyVolume = candle.BuyVolume;
			message.SellVolume = candle.SellVolume;
			message.OpenInterest = candle.OpenInterest;
			message.OpenVolume = candle.OpenVolume;
			message.HighVolume = candle.HighVolume;
			message.LowVolume = candle.LowVolume;
			message.CloseVolume = candle.CloseVolume;
			message.RelativeVolume = candle.RelativeVolume;
			message.DataType = DataType.Create(messageType, candle.Arg);
			message.PriceLevels = candle.PriceLevels?/*.Select(l => l.Clone())*/.ToArray();
			message.State = candle.State;
			message.SeqNum = candle.SeqNum;
			message.BuildFrom = candle.BuildFrom;

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
				Side = order.Side,
				OrderPrice = order.Price,
				SecurityId = order.Security.ToSecurityId(),
				PortfolioName = order.Portfolio.Name,
				DataTypeEx = DataType.Transactions,
				HasOrderInfo = true,
				ServerTime = tick.ServerTime,
				LocalTime = tick.LocalTime,
				OriginSide = tick.OriginSide,
				Currency = tick.Currency,
				Position = trade.Position,
				PnL = trade.PnL,
				Slippage = trade.Slippage,
				Commission = trade.Commission,
				CommissionCurrency = trade.CommissionCurrency,
				SeqNum = trade.Trade.SeqNum,
				OrderBuyId = trade.Trade.OrderBuyId,
				OrderSellId = trade.Trade.OrderSellId,
			};
		}

		/// <summary>
		/// To convert the order into message.
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
				DataTypeEx = DataType.Transactions,
				HasOrderInfo = true,
				OrderPrice = order.Price,
				OrderType = order.Type,
				OrderVolume = order.Volume,
				Balance = order.Balance,
				Side = order.Side,
				OrderState = order.State,
				OrderStatus = order.Status,
				TimeInForce = order.TimeInForce,
				ServerTime = order.ServerTime,
				LocalTime = order.LocalTime,
				ExpiryDate = order.ExpiryDate,
				UserOrderId = order.UserOrderId,
				StrategyId = order.StrategyId,
				Commission = order.Commission,
				CommissionCurrency = order.CommissionCurrency,
				IsSystem = order.IsSystem,
				Comment = order.Comment,
				VisibleVolume = order.VisibleVolume,
				Currency = order.Currency,
				SeqNum = order.SeqNum,
			};

			return message;
		}

		/// <summary>
		/// To convert the error description into message.
		/// </summary>
		/// <param name="fail">Error details.</param>
		/// <param name="originalTransactionId">ID of original transaction, for which this message is the answer.</param>
		/// <returns>Message.</returns>
		public static ExecutionMessage ToMessage(this OrderFail fail, long originalTransactionId)
		{
			if (fail == null)
				throw new ArgumentNullException(nameof(fail));

			var order = fail.Order ?? throw new InvalidOperationException();

			return new ExecutionMessage
			{
				OrderId = order.Id,
				OrderStringId = order.StringId,
				TransactionId = order.TransactionId,
				OriginalTransactionId = originalTransactionId,
				SecurityId = order.Security?.ToSecurityId() ?? default,
				PortfolioName = order.Portfolio?.Name,
				Error = fail.Error,
				DataTypeEx = DataType.Transactions,
				HasOrderInfo = true,
				OrderState = OrderStates.Failed,
				ServerTime = fail.ServerTime,
				LocalTime = fail.LocalTime,
				SeqNum = fail.SeqNum,
			};
		}

		/// <summary>
		/// To convert the tick trade into message.
		/// </summary>
		/// <param name="trade">Tick trade.</param>
		/// <returns>Message.</returns>
		[Obsolete("Use ITickTradeMessage.")]
		public static ExecutionMessage ToMessage(this Trade trade)
		{
			if (trade == null)
				throw new ArgumentNullException(nameof(trade));

			return new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				LocalTime = trade.LocalTime,
				ServerTime = trade.ServerTime,
				SecurityId = trade.Security.ToSecurityId(),
				TradeId = trade.Id,
				TradeStringId = trade.StringId,
				TradePrice = trade.Price,
				TradeVolume = trade.Volume,
				IsSystem = trade.IsSystem,
				TradeStatus = trade.Status,
				OpenInterest = trade.OpenInterest,
				OriginSide = trade.OriginSide,
				IsUpTick = trade.IsUpTick,
				Currency = trade.Currency,
				SeqNum = trade.SeqNum,
				BuildFrom = trade.BuildFrom,
				Yield = trade.Yield,
				OrderBuyId = trade.OrderBuyId,
				OrderSellId = trade.OrderSellId,
			};
		}

		/// <summary>
		/// To convert the string of orders log onto message.
		/// </summary>
		/// <param name="item">Order log item.</param>
		/// <returns>Message.</returns>
		[Obsolete("Use OrderLogMessage.")]
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
				Side = order.Side,
				IsSystem = order.IsSystem,
				OrderState = order.State,
				OrderStatus = order.Status,
				TimeInForce = order.TimeInForce,
				ExpiryDate = order.ExpiryDate,
				PortfolioName = order.Portfolio?.Name,
				DataTypeEx = DataType.OrderLog,
				TradeId = trade?.Id,
				TradeStringId = trade?.StringId,
				TradePrice = trade?.Price,
				Currency = order.Currency,
				SeqNum = order.SeqNum,
				OrderBuyId = trade?.OrderBuyId,
				OrderSellId = trade?.OrderSellId,
				TradeStatus = trade?.Status,
				IsUpTick = trade?.IsUpTick,
				Yield = trade?.Yield,
				OpenInterest = trade?.OpenInterest,
				OriginSide = trade?.OriginSide,
			};
		}

		/// <summary>
		/// To create the message of new order registration.
		/// </summary>
		/// <param name="order">Order.</param>
		/// <param name="securityId">Security ID.</param>
		/// <returns>Message.</returns>
		public static OrderRegisterMessage CreateRegisterMessage(this Order order, SecurityId? securityId = null)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			var msg = new OrderRegisterMessage
			{
				TransactionId = order.TransactionId,
				PortfolioName = order.Portfolio.Name,
				Side = order.Side,
				Price = order.Price,
				Volume = order.Volume,
				VisibleVolume = order.VisibleVolume,
				OrderType = order.Type,
				Comment = order.Comment,
				Condition = order.Condition?.TypedClone(),
				TimeInForce = order.TimeInForce,
				TillDate = order.ExpiryDate,
				//IsSystem = order.IsSystem,
				UserOrderId = order.UserOrderId,
				StrategyId = order.StrategyId,
				BrokerCode = order.BrokerCode,
				ClientCode = order.ClientCode,
				Currency = order.Currency,
				IsMarketMaker = order.IsMarketMaker,
				MarginMode = order.MarginMode,
				Slippage = order.Slippage,
				IsManual = order.IsManual,
				MinOrderVolume = order.MinVolume,
				PositionEffect = order.PositionEffect,
				PostOnly = order.PostOnly,
				Leverage = order.Leverage,
			};

			order.Security.ToMessage(securityId).CopyTo(msg, false);

			return msg;
		}

		/// <summary>
		/// To create the message of cancelling old order.
		/// </summary>
		/// <param name="order">Order.</param>
		/// <param name="securityId">Security ID.</param>
		/// <param name="transactionId">The transaction number.</param>
		/// <returns>Message.</returns>
		public static OrderCancelMessage CreateCancelMessage(this Order order, SecurityId securityId, long transactionId)
		{
			if (order is null)
				throw new ArgumentNullException(nameof(order));

			var msg = new OrderCancelMessage
			{
				PortfolioName = order.Portfolio.Name,
				OrderType = order.Type,
				OriginalTransactionId = order.TransactionId,
				TransactionId = transactionId,
				UserOrderId = order.UserOrderId,
				StrategyId = order.StrategyId,
				BrokerCode = order.BrokerCode,
				ClientCode = order.ClientCode,
				OrderId = order.Id,
				OrderStringId = order.StringId,
				Balance = order.Balance,
				Volume = order.Volume,
				Side = order.Side,
				MarginMode = order.MarginMode,
			};

			order.Security.ToMessage(securityId).CopyTo(msg, false);

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
				Side = newOrder.Side,
				Price = newOrder.Price,
				Volume = newOrder.Volume,
				VisibleVolume = newOrder.VisibleVolume,
				OrderType = newOrder.Type,
				Comment = newOrder.Comment,
				Condition = newOrder.Condition,
				TimeInForce = newOrder.TimeInForce,
				TillDate = newOrder.ExpiryDate,
				//IsSystem = newOrder.IsSystem,

				OldOrderId = oldOrder.Id,
				OldOrderStringId = oldOrder.StringId,
				OriginalTransactionId = oldOrder.TransactionId,

				OldOrderPrice = oldOrder.Price,
				OldOrderVolume = oldOrder.Volume,

				UserOrderId = oldOrder.UserOrderId,
				StrategyId = oldOrder.StrategyId,

				BrokerCode = oldOrder.BrokerCode,
				ClientCode = oldOrder.ClientCode,

				Currency = newOrder.Currency,

				IsManual = newOrder.IsManual,
				IsMarketMaker = newOrder.IsMarketMaker,
				MarginMode = newOrder.MarginMode,

				Slippage = newOrder.Slippage,

				MinOrderVolume = newOrder.MinVolume,
				PositionEffect = newOrder.PositionEffect,
				PostOnly = newOrder.PostOnly,
				Leverage = newOrder.Leverage,
			};

			oldOrder.Security.ToMessage(securityId).CopyTo(msg, false);

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

			oldOrder1.Security.ToMessage(security1).CopyTo(msg.Message1, false);

			return msg;
		}

		/// <summary>
		/// To convert the instrument into message.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="securityId">Security ID.</param>
		/// <param name="originalTransactionId">ID of original transaction, for which this message is the answer.</param>
		/// <param name="copyExtendedId">Copy <see cref="Security.ExternalId"/> and <see cref="Security.Type"/>.</param>
		/// <returns>Message.</returns>
		public static SecurityMessage ToMessage(this Security security, SecurityId? securityId = null, long originalTransactionId = 0, bool copyExtendedId = false)
		{
			if (security is null)
				throw new ArgumentNullException(nameof(security));

			if (security.IsAllSecurity())
			{
				return new SecurityMessage();

				// not immutable
				//return Messages.Extensions.AllSecurity;
			}

			return security.FillMessage(new SecurityMessage
			{
				SecurityId = securityId ?? security.ToSecurityId(copyExtended: copyExtendedId),
				OriginalTransactionId = originalTransactionId,
			});
		}

		/// <summary>
		/// To convert the instrument into message.
		/// </summary>
		/// <typeparam name="TMessage">Message type.</typeparam>
		/// <param name="security">Security.</param>
		/// <param name="message">Message.</param>
		/// <returns>Message.</returns>
		public static TMessage FillMessage<TMessage>(this Security security, TMessage message)
			where TMessage : SecurityMessage
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			message.Name = security.Name;
			message.ShortName = security.ShortName;
			message.PriceStep = security.PriceStep;
			message.Decimals = security.Decimals;
			message.VolumeStep = security.VolumeStep;
			message.MinVolume = security.MinVolume;
			message.MaxVolume = security.MaxVolume;
			message.Multiplier = security.Multiplier;
			message.Currency = security.Currency;
			message.SecurityType = security.Type;
			message.Class = security.Class;
			message.CfiCode = security.CfiCode;
			message.OptionType = security.OptionType;
			message.Strike = security.Strike;
			message.BinaryOptionType = security.BinaryOptionType;
			message.UnderlyingSecurityId = security.UnderlyingSecurityId.IsEmpty() ? default : security.UnderlyingSecurityId.ToSecurityId();
			message.SettlementDate = security.SettlementDate;
			message.ExpiryDate = security.ExpiryDate;
			message.IssueSize = security.IssueSize;
			message.IssueDate = security.IssueDate;
			message.UnderlyingSecurityType = security.UnderlyingSecurityType;
			message.UnderlyingSecurityMinVolume = security.UnderlyingSecurityMinVolume;
			message.Shortable = security.Shortable;
			message.BasketCode = security.BasketCode;
			message.BasketExpression = security.BasketExpression;
			message.FaceValue = security.FaceValue;
			message.OptionStyle = security.OptionStyle;
			message.SettlementType = security.SettlementType;

			if (!security.PrimaryId.IsEmpty())
				message.PrimaryId = security.PrimaryId.ToSecurityId();

			return message;
		}

		/// <summary>
		/// Convert <see cref="SecurityLookupMessage"/> message to <see cref="Security"/> criteria.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
		/// <returns>Criteria.</returns>
		public static Security ToLookupCriteria(this SecurityLookupMessage message, IExchangeInfoProvider exchangeInfoProvider)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (exchangeInfoProvider == null)
				throw new ArgumentNullException(nameof(exchangeInfoProvider));

			if (message.IsLookupAll())
				return TraderHelper.LookupAllCriteria;

			var criteria = new Security();
			criteria.ApplyChanges(message, exchangeInfoProvider);

			criteria.Type ??= message.GetSecurityTypes().FirstOr();

			return criteria;
		}

		/// <summary>
		/// Convert <see cref="Security"/> criteria to <see cref="SecurityLookupMessage"/>.
		/// </summary>
		/// <param name="criteria">Criteria.</param>
		/// <returns>Message.</returns>
		public static SecurityLookupMessage ToLookupMessage(this Security criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			var message = new SecurityLookupMessage();

			if (criteria.Id.IsEmpty())
			{
				message.SecurityId = criteria.ExternalId.ToSecurityId(new()
				{
					SecurityCode = criteria.Code,
					BoardCode = criteria.Board?.Code,
				});

				criteria.FillMessage(message);
			}
			else
			{
				message.SecurityId = criteria.Id.ToSecurityId();
			}

			return message;
		}

		/// <summary>
		/// To convert the message into instrument.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
		/// <returns>Security.</returns>
		public static Security ToSecurity(this SecurityMessage message, IExchangeInfoProvider exchangeInfoProvider)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (exchangeInfoProvider == null)
				throw new ArgumentNullException(nameof(exchangeInfoProvider));

			var security = new Security
			{
				Id = message.SecurityId == default ? null : message.SecurityId.ToStringId(nullIfEmpty: message is SecurityLookupMessage)
			};

			security.ApplyChanges(message, exchangeInfoProvider);

			return security;
		}

		/// <summary>
		/// To convert the portfolio into message.
		/// </summary>
		/// <param name="portfolio">Portfolio.</param>
		/// <param name="originalTransactionId">ID of original transaction, for which this message is the answer.</param>
		/// <returns>Message.</returns>
		public static PortfolioMessage ToMessage(this Portfolio portfolio, long originalTransactionId = 0)
		{
			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			return new PortfolioMessage
			{
				PortfolioName = portfolio.Name,
				BoardCode = portfolio.Board?.Code,
				Currency = portfolio.Currency,
				ClientCode = portfolio.ClientCode,
				OriginalTransactionId = originalTransactionId,
			};
		}

		/// <summary>
		/// To convert the portfolio into message.
		/// </summary>
		/// <param name="portfolio">Portfolio.</param>
		/// <returns>Message.</returns>
		public static PositionChangeMessage ToChangeMessage(this Portfolio portfolio)
		{
			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			return new PositionChangeMessage
			{
				SecurityId = SecurityId.Money,
				PortfolioName = portfolio.Name,
				BoardCode = portfolio.Board?.Code,
				LocalTime = portfolio.LocalTime,
				ServerTime = portfolio.LastChangeTime,
				ClientCode = portfolio.ClientCode,
			}
			.TryAdd(PositionChangeTypes.BeginValue, portfolio.BeginValue, true)
			.TryAdd(PositionChangeTypes.CurrentValue, portfolio.CurrentValue, true);
		}

		/// <summary>
		/// Convert <see cref="Portfolio"/> to <see cref="PortfolioLookupMessage"/> value.
		/// </summary>
		/// <param name="criteria">The criterion which fields will be used as a filter.</param>
		/// <returns>Message portfolio lookup for specified criteria.</returns>
		public static PortfolioLookupMessage ToLookupCriteria(this Portfolio criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			return new PortfolioLookupMessage
			{
				IsSubscribe = true,
				BoardCode = criteria.Board?.Code,
				Currency = criteria.Currency,
				PortfolioName = criteria.Name,
				ClientCode = criteria.ClientCode,
			};
		}

		/// <summary>
		/// Convert <see cref="Order"/> to <see cref="OrderStatusMessage"/> value.
		/// </summary>
		/// <param name="criteria">The criterion which fields will be used as a filter.</param>
		/// <param name="volume">Volume.</param>
		/// <param name="side">Order side.</param>
		/// <returns>A message requesting current registered orders and trades.</returns>
		public static OrderStatusMessage ToLookupCriteria(this Order criteria, decimal? volume, Sides? side)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			var statusMsg = new OrderStatusMessage
			{
				IsSubscribe = true,
				PortfolioName = criteria.Portfolio?.Name,
				OrderId = criteria.Id,
				OrderStringId = criteria.StringId,
				OrderType = criteria.Type,
				UserOrderId = criteria.UserOrderId,
				StrategyId = criteria.StrategyId,
				BrokerCode = criteria.BrokerCode,
				ClientCode = criteria.ClientCode,
				Volume = volume,
				Side = side,
			};

			criteria.Security?.ToMessage().CopyTo(statusMsg);

			return statusMsg;
		}

		/// <summary>
		/// To convert the position into message.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="originalTransactionId">ID of original transaction, for which this message is the answer.</param>
		/// <returns>Message.</returns>
		public static PositionChangeMessage ToChangeMessage(this Position position, long originalTransactionId = 0)
		{
			if (position is null)
				throw new ArgumentNullException(nameof(position));

			return new PositionChangeMessage
			{
				LocalTime = position.LocalTime,
				ServerTime = position.LastChangeTime,
				PortfolioName = position.Portfolio.Name,
				SecurityId = position.Security.ToSecurityId(),
				ClientCode = position.ClientCode,
				StrategyId = position.StrategyId,
				Side = position.Side,
				OriginalTransactionId = originalTransactionId,
			}
			.TryAdd(PositionChangeTypes.BeginValue, position.BeginValue, true)
			.TryAdd(PositionChangeTypes.CurrentValue, position.CurrentValue, true)
			.TryAdd(PositionChangeTypes.BlockedValue, position.BlockedValue, true);
		}

		/// <summary>
		/// To convert the board into message.
		/// </summary>
		/// <param name="board">Board.</param>
		/// <param name="originalTransactionId">ID of original transaction, for which this message is the answer.</param>
		/// <returns>Message.</returns>
		public static BoardMessage ToMessage(this ExchangeBoard board, long originalTransactionId = 0)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			return new BoardMessage
			{
				Code = board.Code,
				ExchangeCode = board.Exchange.Name,
				WorkingTime = board.WorkingTime.Clone(),
				//IsSupportMarketOrders = board.IsSupportMarketOrders,
				//IsSupportAtomicReRegister = board.IsSupportAtomicReRegister,
				ExpiryTime = board.ExpiryTime,
				TimeZone = board.TimeZone,
				OriginalTransactionId = originalTransactionId,
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
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var board = new ExchangeBoard
			{
				Code = message.Code,
				Exchange = new Exchange { Name = message.ExchangeCode }
			};
			board.ApplyChanges(message);
			return board;
		}

		/// <summary>
		/// To convert the message into board.
		/// </summary>
		/// <param name="board">Board.</param>
		/// <param name="message">Message.</param>
		/// <returns>Board.</returns>
		public static ExchangeBoard ApplyChanges(this ExchangeBoard board, BoardMessage message)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			board.WorkingTime = message.WorkingTime;
			//board.IsSupportAtomicReRegister = message.IsSupportAtomicReRegister;
			//board.IsSupportMarketOrders = message.IsSupportMarketOrders;

			if (message.ExpiryTime != default)
				board.ExpiryTime = message.ExpiryTime;

			if (message.TimeZone != null)
				board.TimeZone = message.TimeZone;

			return board;
		}

		[Obsolete]
		private class ToMessagesEnumerable<TEntity, TMessage> : IEnumerable<TMessage>
		{
			private readonly IEnumerable<TEntity> _entities;

			public ToMessagesEnumerable(IEnumerable<TEntity> entities)
			{
				_entities = entities ?? throw new ArgumentNullException(nameof(entities));
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
				else if (value is MyTrade)
					return value.To<MyTrade>().ToMessage().To<TMessage>();
				else if (value is Candle)
					return value.To<Candle>().ToMessage().To<TMessage>();
				else if (value is Order)
					return value.To<Order>().ToMessage().To<TMessage>();

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
		[Obsolete("Conversion reduce performance.")]
		public static IEnumerable<TMessage> ToMessages<TEntity, TMessage>(this IEnumerable<TEntity> entities)
		{
			return new ToMessagesEnumerable<TEntity, TMessage>(entities);
		}

		[Obsolete]
		private class ToEntitiesEnumerable<TMessage, TEntity> : IEnumerable<TEntity>
			where TMessage : Message
		{
			private readonly IEnumerable<TMessage> _messages;
			private readonly Security _security;
			private readonly IExchangeInfoProvider _exchangeInfoProvider;
			//private readonly object _candleArg;

			public ToEntitiesEnumerable(IEnumerable<TMessage> messages, Security security, IExchangeInfoProvider exchangeInfoProvider)
			{
				if (typeof(TMessage) != typeof(NewsMessage))
				{
					if (security == null)
						throw new ArgumentNullException(nameof(security));
				}

				_messages = messages ?? throw new ArgumentNullException(nameof(messages));
				_security = security;
				_exchangeInfoProvider = exchangeInfoProvider;
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

						if (execMsg.DataType == DataType.Ticks)
							return execMsg.ToTrade(_security).To<TEntity>();
						else if (execMsg.DataType == DataType.OrderLog)
							return execMsg.ToOrderLog(_security).To<TEntity>();
						else if (execMsg.DataType == DataType.Transactions)
							return execMsg.ToOrder(_security).To<TEntity>();
						else
							throw new ArgumentOutOfRangeException(nameof(message), LocalizedStrings.UnsupportedType.Put(execMsg.DataType));
					}

					case MessageTypes.QuoteChange:
						return message.To<QuoteChangeMessage>().ToMarketDepth(_security).To<TEntity>();

					case MessageTypes.News:
						return message.To<NewsMessage>().ToNews(_exchangeInfoProvider).To<TEntity>();

					case MessageTypes.BoardState:
						return message.To<TEntity>();

					default:
					{
						if (message is CandleMessage candleMsg)
							return candleMsg.ToCandle(_security).To<TEntity>();

						throw new ArgumentOutOfRangeException(nameof(message), message.Type, LocalizedStrings.InvalidValue);
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
		/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
		/// <returns>Trading objects.</returns>
		[Obsolete("Conversion reduce performance.")]
		public static IEnumerable<TEntity> ToEntities<TMessage, TEntity>(this IEnumerable<TMessage> messages, Security security, IExchangeInfoProvider exchangeInfoProvider = null)
			where TMessage : Message
		{
			if (messages is IEnumerable<QuoteChangeMessage> books)
				messages = books.BuildIfNeed().To<IEnumerable<TMessage>>();

			return new ToEntitiesEnumerable<TMessage, TEntity>(messages, security, exchangeInfoProvider);
		}

		/// <summary>
		/// To convert messages into trading objects.
		/// </summary>
		/// <typeparam name="TCandle">The candle type.</typeparam>
		/// <param name="messages">Messages.</param>
		/// <param name="security">Security.</param>
		/// <returns>Trading objects.</returns>
		[Obsolete("Conversion reduce performance.")]
		public static IEnumerable<TCandle> ToCandles<TCandle>(this IEnumerable<CandleMessage> messages, Security security)
		{
			return new ToEntitiesEnumerable<CandleMessage, TCandle>(messages, security, null);
		}

		/// <summary>
		/// To convert <see cref="CandleMessage"/> into candle.
		/// </summary>
		/// <typeparam name="TCandle">The candle type.</typeparam>
		/// <param name="message">Message.</param>
		/// <param name="series">Series.</param>
		/// <returns>Candle.</returns>
		[Obsolete("Conversion reduce performance.")]
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
		[Obsolete("Conversion reduce performance.")]
		public static Candle ToCandle(this CandleMessage message, CandleSeries series)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var candle = message.ToCandle(series.Security);
			//candle.Series = series;

			if (candle.Arg.IsNull(true))
				candle.Arg = series.Arg;

			return candle;
		}

#pragma warning disable CS0618 // Type or member is obsolete
		private static readonly SynchronizedDictionary<Type, Func<Candle>> _candleCreators = new();
#pragma warning restore CS0618 // Type or member is obsolete
		private static readonly SynchronizedDictionary<Type, Func<CandleMessage>> _candleMessageCreators = new();

		/// <summary>
		/// Create instance of <see cref="CandleMessage"/>.
		/// </summary>
		/// <param name="messageType">The type of candle message.</param>
		/// <returns>Instance of <see cref="CandleMessage"/>.</returns>
		public static CandleMessage CreateCandleMessage(this Type messageType)
		{
			if (messageType == null)
				throw new ArgumentNullException(nameof(messageType));

			if (!_candleMessageCreators.TryGetValue(messageType, out var creator))
				throw new ArgumentException(LocalizedStrings.UnknownCandleType.Put(messageType), nameof(messageType));

			return creator();
		}

		/// <summary>
		/// All registered candle types.
		/// </summary>
		public static IEnumerable<Type> AllCandleTypes => _candleTypes.CachedKeys;

		/// <summary>
		/// All registered candle message types.
		/// </summary>
		public static IEnumerable<Type> AllCandleMessageTypes => _candleTypes.CachedValues;

		/// <summary>
		/// Register new candle type.
		/// </summary>
		/// <typeparam name="TCandle">Candle type.</typeparam>
		/// <typeparam name="TMessage">The type of candle message.</typeparam>
		/// <param name="candleCreator"><see cref="Candle"/> instance creator.</param>
		/// <param name="candleMessageCreator"><see cref="CandleMessage"/> instance creator.</param>
		[Obsolete("Conversion reduce performance.")]
		public static void RegisterCandle<TCandle, TMessage>(Func<TCandle> candleCreator, Func<TMessage> candleMessageCreator)
			where TCandle : Candle
			where TMessage : CandleMessage
		{
			RegisterCandle(typeof(TCandle), typeof(TMessage), candleCreator, candleMessageCreator);
		}

		/// <summary>
		/// Register new candle type.
		/// </summary>
		/// <param name="candleType">Candle type.</param>
		/// <param name="messageType">The type of candle message.</param>
		/// <param name="candleCreator"><see cref="Candle"/> instance creator.</param>
		/// <param name="candleMessageCreator"><see cref="CandleMessage"/> instance creator.</param>
		[Obsolete("Conversion reduce performance.")]
		public static void RegisterCandle(Type candleType, Type messageType, Func<Candle> candleCreator, Func<CandleMessage> candleMessageCreator)
		{
			if (messageType == null)
				throw new ArgumentNullException(nameof(messageType));

			if (candleCreator == null)
				throw new ArgumentNullException(nameof(candleCreator));

			if (candleMessageCreator == null)
				throw new ArgumentNullException(nameof(candleMessageCreator));

			_candleTypes.Add(candleType, messageType);
			_candleCreators.Add(candleType, candleCreator);
			_candleMessageCreators.Add(messageType, candleMessageCreator);
		}

		/// <summary>
		/// To convert <see cref="CandleMessage"/> into candle.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="security">Security.</param>
		/// <returns>Candle.</returns>
		[Obsolete("Conversion reduce performance.")]
		public static Candle ToCandle(this CandleMessage message, Security security)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (!_candleTypes.TryGetKey(message.GetType(), out var candleType) || !_candleCreators.TryGetValue(candleType, out var creator))
				throw new ArgumentOutOfRangeException(nameof(message), message.Type, LocalizedStrings.WrongCandleType);

			var candle = creator();

			candle.Security = security;
			candle.Arg = message.DataType.Arg;

			return candle.Update(message);
		}

		/// <summary>
		/// Update candle from <see cref="CandleMessage"/>.
		/// </summary>
		/// <param name="candle">Candle.</param>
		/// <param name="message">Message.</param>
		/// <returns>Candle.</returns>
		[Obsolete("Conversion reduce performance.")]
		public static Candle Update(this Candle candle, CandleMessage message)
		{
			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

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

			candle.BuyVolume = message.BuyVolume;
			candle.SellVolume = message.SellVolume;

			candle.OpenInterest = message.OpenInterest;

			candle.TotalTicks = message.TotalTicks;
			candle.UpTicks = message.UpTicks;
			candle.DownTicks = message.DownTicks;

			candle.PriceLevels = message.PriceLevels?/*.Select(l => l.Clone())*/.ToArray();

			candle.State = message.State;
			candle.SeqNum = message.SeqNum;
			candle.BuildFrom = message.BuildFrom;

			return candle;
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
			order.Side = message.Side;
			order.Price = message.OrderPrice;
			order.Volume = message.OrderVolume ?? 0;
			order.Balance = message.Balance ?? 0;
			order.VisibleVolume = message.VisibleVolume;
			order.Type = message.OrderType;
			order.Status = message.OrderStatus;
			order.IsSystem = message.IsSystem;
			order.Time = message.ServerTime;
			order.ServerTime = message.ServerTime;
			order.LocalTime = message.LocalTime;
			order.TimeInForce = message.TimeInForce;
			order.ExpiryDate = message.ExpiryDate;
			order.UserOrderId = message.UserOrderId;
			order.StrategyId = message.StrategyId;
			order.Comment = message.Comment;
			order.Commission = message.Commission;
			order.CommissionCurrency = message.CommissionCurrency;
			order.Currency = message.Currency;
			order.IsMarketMaker = message.IsMarketMaker;
			order.MarginMode = message.MarginMode;
			order.Slippage = message.Slippage;
			order.IsManual = message.IsManual;
			order.AveragePrice = message.AveragePrice;
			order.Yield = message.Yield;
			order.MinVolume = message.MinVolume;
			order.PositionEffect = message.PositionEffect;
			order.PostOnly = message.PostOnly;
			order.SeqNum = message.SeqNum;
			order.Leverage = message.Leverage;

			if (message.OrderState != null)
				order.ApplyNewState(message.OrderState.Value);

			return order;
		}

		/// <summary>
		/// To convert the message into order book.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="security">Security.</param>
		/// <returns>Market depth.</returns>
		[Obsolete("Use IOrderBookMessage.")]
		public static MarketDepth ToMarketDepth(this QuoteChangeMessage message, Security security)
		{
			return message.ToMarketDepth(new MarketDepth(security));
		}

		/// <summary>
		/// To convert the message into order book.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="marketDepth">Market depth.</param>
		/// <returns>Market depth.</returns>
		[Obsolete("Use IOrderBookMessage.")]
		public static MarketDepth ToMarketDepth(this QuoteChangeMessage message, MarketDepth marketDepth)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (marketDepth == null)
				throw new ArgumentNullException(nameof(marketDepth));

			marketDepth.Update(
				message.Bids,
				message.Asks,
				message.ServerTime);

			marketDepth.LocalTime = message.LocalTime;
			marketDepth.Currency = message.Currency;
			marketDepth.SeqNum = message.SeqNum;
			marketDepth.BuildFrom = message.BuildFrom;

			return marketDepth;
		}

		/// <summary>
		/// To convert the message into orders log string.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="security">Security.</param>
		/// <returns>Order log item.</returns>
		[Obsolete("Use IOrderLogMessage.")]
		public static OrderLogItem ToOrderLog(this ExecutionMessage message, Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return message.ToOrderLog(new OrderLogItem
			{
				Order = new() { Security = security },
				Trade = message.HasTradeInfo ? new() { Security = security } : null
			});
		}

		/// <summary>
		/// To convert the message into orders log string.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="item">Order log item.</param>
		/// <returns>Order log item.</returns>
		[Obsolete("Use IOrderLogMessage.")]
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
			order.Side = message.Side;
			order.Time = message.ServerTime;
			order.ServerTime = message.ServerTime;
			order.LocalTime = message.LocalTime;

			order.Status = message.OrderStatus;
			order.TimeInForce = message.TimeInForce;
			order.IsSystem = message.IsSystem;
			order.Currency = message.Currency;
			order.SeqNum = message.SeqNum;

			order.ApplyNewState(message.OrderState ?? (message.HasTradeInfo ? OrderStates.Done : OrderStates.Active));

			if (message.HasTradeInfo)
			{
				var trade = item.Trade;

				trade.Id = message.TradeId;
				trade.StringId = message.TradeStringId;
				trade.Price = message.TradePrice ?? default;
				trade.ServerTime = message.ServerTime;
				trade.Volume = message.OrderVolume ?? default;
				trade.IsSystem = message.IsSystem;
				trade.Status = message.TradeStatus;
				trade.OrderBuyId = message.OrderBuyId;
				trade.OrderSellId = message.OrderSellId;
				trade.OriginSide = message.OriginSide;
				trade.OpenInterest = message.OpenInterest;
				trade.IsUpTick = message.IsUpTick;
				trade.Yield = message.Yield;
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
				SecurityId = news.Security?.ToSecurityId(),
				BoardCode = news.Board == null ? string.Empty : news.Board.Code,
				Url = news.Url,
				Priority = news.Priority,
				Language = news.Language,
				ExpiryDate = news.ExpiryDate,
				SeqNum = news.SeqNum,
			};
		}

		/// <summary>
		/// To convert the instrument into <see cref="SecurityId"/>.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="idGenerator">The instrument identifiers generator <see cref="Security.Id"/>.</param>
		/// <param name="boardIsRequired"><see cref="Security.Board"/> is required.</param>
		/// <param name="copyExtended">Copy <see cref="Security.ExternalId"/> and <see cref="Security.Type"/>.</param>
		/// <returns>Security ID.</returns>
		public static SecurityId ToSecurityId(this Security security, SecurityIdGenerator idGenerator = null, bool boardIsRequired = true, bool copyExtended = false)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (security.IsAllSecurity())
				return default;

			string secCode;
			string boardCode;

			// http://stocksharp.com/forum/yaf_postsm32581_Security-SPFB-RTS-FORTS.aspx#post32581
			// иногда в Security.Code может быть записано неправильное, и необходимо опираться на Security.Id
			if (!security.Id.IsEmpty())
			{
				var id = idGenerator.EnsureGetGenerator().Split(security.Id);

				secCode = id.SecurityCode;

				// http://stocksharp.com/forum/yaf_postst5143findunread_API-4-2-4-0-Nie-vystavliaiutsia-zaiavki-po-niekotorym-instrumientam-FORTS.aspx
				// для Quik необходимо соблюдение регистра в коде инструмента при выставлении заявок
				if (secCode.EqualsIgnoreCase(security.Code))
					secCode = security.Code;

				//if (!boardCode.EqualsIgnoreCase(ExchangeBoard.Test.Code))
				boardCode = id.BoardCode;
			}
			else
			{
				if (security.Code.IsEmpty())
				{
					if (!security.BasketCode.IsEmpty())
					{
						return new SecurityId
						{
							SecurityCode = security.BasketExpression.Replace('@', '_'),
							BoardCode = security.Board?.Code ?? SecurityId.AssociatedBoardCode
						};
					}

					throw new ArgumentException(LocalizedStrings.SecurityNotContainsId);
				}

				if (security.Board == null && boardIsRequired)
					throw new ArgumentException(LocalizedStrings.SecurityNotContainsBoard.Put(security.Code));

				secCode = security.Code;
				boardCode = security.Board?.Code;
			}

			SecurityId secId;

			if (copyExtended)
				secId = security.ExternalId.ToSecurityId(secCode, boardCode);
			else
			{
				secId = new()
				{
					SecurityCode = secCode,
					BoardCode = boardCode,
				};
			}

			// force hash code caching
			secId.GetHashCode();

			return secId;
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
				   securityId.InteractiveBrokers != default ||
				   !securityId.Plaza.IsEmpty();
		}

		/// <summary>
		/// Cast <see cref="SecurityExternalId"/> to the <see cref="SecurityId"/>.
		/// </summary>
		/// <param name="externalId"><see cref="SecurityExternalId"/>.</param>
		/// <param name="securityCode">Security code.</param>
		/// <param name="boardCode">Board code.</param>
		/// <returns><see cref="SecurityId"/>.</returns>
		public static SecurityId ToSecurityId(this SecurityExternalId externalId, string securityCode, string boardCode)
		{
			return externalId.ToSecurityId(new SecurityId
			{
				SecurityCode = securityCode,
				BoardCode = boardCode,
			});
		}

		/// <summary>
		/// Cast <see cref="SecurityExternalId"/> to the <see cref="SecurityId"/>.
		/// </summary>
		/// <param name="externalId"><see cref="SecurityExternalId"/>.</param>
		/// <param name="secId"><see cref="SecurityId"/>.</param>
		/// <returns><see cref="SecurityId"/>.</returns>
		public static SecurityId ToSecurityId(this SecurityExternalId externalId, SecurityId secId)
		{
			if (externalId == null)
				throw new ArgumentNullException(nameof(externalId));

			secId.Bloomberg = externalId.Bloomberg;
			secId.Cusip = externalId.Cusip;
			secId.IQFeed = externalId.IQFeed;
			secId.Isin = externalId.Isin;
			secId.Ric = externalId.Ric;
			secId.Sedol = externalId.Sedol;
			secId.InteractiveBrokers = externalId.InteractiveBrokers;
			secId.Plaza = externalId.Plaza;

			return secId;
		}

		/// <summary>
		/// Cast <see cref="Level1ChangeMessage"/> to the <see cref="MarketDepth"/>.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="security">Security.</param>
		/// <returns>Market depth.</returns>
		[Obsolete("Use IOrderBookMessage.")]
		public static MarketDepth ToMarketDepth(this Level1ChangeMessage message, Security security)
		{
			return new MarketDepth(security) { LocalTime = message.LocalTime }.Update(
				new[] { message.CreateQuote(Level1Fields.BestBidPrice, Level1Fields.BestBidVolume) },
				new[] { message.CreateQuote(Level1Fields.BestAskPrice, Level1Fields.BestAskVolume) },
				message.ServerTime);
		}

		private static QuoteChange CreateQuote(this Level1ChangeMessage message, Level1Fields priceField, Level1Fields volumeField)
		{
			var changes = message.Changes;
			return new QuoteChange((decimal)changes[priceField], (decimal?)changes.TryGetValue(volumeField) ?? 0m);
		}

		/// <summary>
		/// Cast <see cref="NewsMessage"/> to the <see cref="News"/>.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
		/// <returns>News.</returns>
		public static News ToNews(this NewsMessage message, IExchangeInfoProvider exchangeInfoProvider)
		{
			return new News
			{
				Id = message.Id,
				Source = message.Source,
				ServerTime = message.ServerTime,
				Story = message.Story,
				Url = message.Url,
				Headline = message.Headline,
				Board = message.BoardCode.IsEmpty() ? null : exchangeInfoProvider?.GetOrCreateBoard(message.BoardCode),
				LocalTime = message.LocalTime,
				Priority = message.Priority,
				Language = message.Language,
				ExpiryDate = message.ExpiryDate,
				Security = message.SecurityId == null ? null : new Security
				{
					Id = message.SecurityId.Value.SecurityCode
				},
				SeqNum = message.SeqNum,
			};
		}

		/// <summary>
		/// Cast <see cref="PortfolioMessage"/> to the <see cref="Portfolio"/>.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="portfolio">Portfolio.</param>
		/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
		/// <returns>Portfolio.</returns>
		public static Portfolio ToPortfolio(this PortfolioMessage message, Portfolio portfolio, IExchangeInfoProvider exchangeInfoProvider)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			if (!message.BoardCode.IsEmpty())
				portfolio.Board = exchangeInfoProvider.GetOrCreateBoard(message.BoardCode);

			if (message.Currency != null)
				portfolio.Currency = message.Currency;

			if (!message.ClientCode.IsEmpty())
				portfolio.ClientCode = message.ClientCode;

			//if (message.State != null)
			//	portfolio.State = message.State;

			return portfolio;
		}

		/// <summary>
		/// To convert the type of business object into type of message.
		/// </summary>
		/// <param name="dataType">The type of business object.</param>
		/// <param name="arg">The data parameter.</param>
		/// <returns>Message type.</returns>
		[Obsolete("Conversion reduce performance.")]
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
				throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.InvalidValue);
		}

		/// <summary>
		/// Cast <see cref="CandleSeries"/> to <see cref="MarketDataMessage"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="isSubscribe">The message is subscription.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		/// <param name="count">Candles count.</param>
		/// <param name="throwIfInvalidType">Throw an error if <see cref="MarketDataMessage.DataType2"/> isn't candle type.</param>
		/// <returns>Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</returns>
		public static MarketDataMessage ToMarketDataMessage(this CandleSeries series, bool isSubscribe, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, bool throwIfInvalidType = true)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var mdMsg = new MarketDataMessage
			{
				IsSubscribe = isSubscribe,
				From = from ?? series.From,
				To = to ?? series.To,
				Count = count ?? series.Count,
				BuildMode = series.BuildCandlesMode,
				BuildFrom = series.BuildCandlesFrom2,
				BuildField = series.BuildCandlesField,
				IsCalcVolumeProfile = series.IsCalcVolumeProfile,
				AllowBuildFromSmallerTimeFrame = series.AllowBuildFromSmallerTimeFrame,
				IsRegularTradingHours = series.IsRegularTradingHours,
				IsFinishedOnly = series.IsFinishedOnly,
			};

			if (series.CandleType == null)
			{
				if (throwIfInvalidType)
					throw new ArgumentException(LocalizedStrings.WrongCandleType);
			}
			else
			{
				var msgType = series
					.CandleType
					.ToCandleMessageType();

				mdMsg.DataType2 = DataType.Create(msgType, series.Arg);
			}

			mdMsg.ValidateBounds();
			series.Security?.ToMessage(copyExtendedId: true).CopyTo(mdMsg, false);

			return mdMsg;
		}

		/// <summary>
		/// Cast <see cref="MarketDataMessage"/> to <see cref="CandleSeries"/>.
		/// </summary>
		/// <param name="message">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
		/// <param name="security">Security.</param>
		/// <param name="throwIfInvalidType">Throw an error if <see cref="MarketDataMessage.DataType2"/> isn't candle type.</param>
		/// <returns>Candles series.</returns>
		public static CandleSeries ToCandleSeries(this MarketDataMessage message, Security security, bool throwIfInvalidType)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			var series = new CandleSeries { Security = security };
			message.ToCandleSeries(series, throwIfInvalidType);
			return series;
		}

		/// <summary>
		/// Cast <see cref="MarketDataMessage"/> to <see cref="CandleSeries"/>.
		/// </summary>
		/// <param name="message">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
		/// <param name="series">Candles series.</param>
		/// <param name="throwIfInvalidType">Throw an error if <see cref="MarketDataMessage.DataType2"/> isn't candle type.</param>
		public static void ToCandleSeries(this MarketDataMessage message, CandleSeries series, bool throwIfInvalidType)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (series == null)
				throw new ArgumentNullException(nameof(series));

			if (message.DataType2.IsCandles)
			{
				series.CandleType = message.DataType2.MessageType.ToCandleType();
				series.Arg = message.GetArg();
			}
			else
			{
				if (throwIfInvalidType)
					throw new ArgumentException(LocalizedStrings.UnknownCandleType.Put(message.DataType2), nameof(message));
			}

			series.From = message.From;
			series.To = message.To;
			series.Count = message.Count;
			series.BuildCandlesMode = message.BuildMode;
			series.BuildCandlesFrom2 = message.BuildFrom;
			series.BuildCandlesField = message.BuildField;
			series.IsCalcVolumeProfile = message.IsCalcVolumeProfile;
			series.AllowBuildFromSmallerTimeFrame = message.AllowBuildFromSmallerTimeFrame;
			series.IsRegularTradingHours = message.IsRegularTradingHours;
			series.IsFinishedOnly = message.IsFinishedOnly;
		}

		/// <summary>
		/// Format data type into into human-readable string.
		/// </summary>
		/// <param name="message">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
		/// <returns>String.</returns>
		public static string ToDataTypeString(this MarketDataMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var str = message.DataType2.MessageType.GetDisplayName();

			if (message.DataType2.IsCandles)
				str += " " + message.GetArg();

			return str;
		}

		/// <summary>
		/// Convert <see cref="DataType"/> to <see cref="CandleSeries"/> value.
		/// </summary>
		/// <param name="dataType">Data type info.</param>
		/// <param name="security">The instrument to be used for candles formation.</param>
		/// <returns>Candles series.</returns>
		public static CandleSeries ToCandleSeries(this DataType dataType, Security security)
		{
			if (dataType is null)
				throw new ArgumentNullException(nameof(dataType));

			return new CandleSeries
			{
				CandleType = dataType.MessageType.ToCandleType(),
				Arg = dataType.Arg,
				Security = security,
			};
		}

		/// <summary>
		/// Convert <see cref="DataType"/> to <see cref="CandleSeries"/> value.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Data type info.</returns>
		public static DataType ToDataType(this CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			return DataType.Create(series.CandleType.ToCandleMessageType(), series.Arg);
		}

		/// <summary>
		/// Convert <see cref="DataType"/> to <see cref="Subscription"/> value.
		/// </summary>
		/// <param name="dataType">Data type info.</param>
		/// <returns>Subscription.</returns>
		public static Subscription ToSubscription(this DataType dataType) => new(dataType, (SecurityMessage)null);
	}
}

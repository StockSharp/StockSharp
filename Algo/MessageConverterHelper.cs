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
	/// Вспомогательный класс для конвертации бизнес-объектов (<see cref="StockSharp.BusinessEntities"/>) в сообщения (<see cref="StockSharp.Messages"/>) и обратно.
	/// </summary>
	public static class MessageConverterHelper
	{
		/// <summary>
		/// Преобразовать <see cref="MarketDepth"/> в <see cref="QuoteChangeMessage"/>.
		/// </summary>
		/// <param name="depth"><see cref="MarketDepth"/>.</param>
		/// <returns><see cref="QuoteChangeMessage"/>.</returns>
		public static QuoteChangeMessage ToMessage(this MarketDepth depth)
		{
			if (depth == null)
				throw new ArgumentNullException("depth");

			var securityId = depth.Security.ToSecurityId();

			return new QuoteChangeMessage
			{
				LocalTime = depth.LocalTime,
				SecurityId = securityId,
				Bids = depth.Bids.Select(q => q.ToQuoteChange()).ToArray(),
				Asks = depth.Asks.Select(q => q.ToQuoteChange()).ToArray(),
				ServerTime = depth.LastChangeTime,
				IsSorted = true,
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

		/// <summary>
		/// Преобразовать тип свечи <see cref="Candle"/> в тип сообщения <see cref="CandleMessage"/>.
		/// </summary>
		/// <param name="candleType">Тип свечи <see cref="Candle"/>.</param>
		/// <returns>Тип сообщения <see cref="CandleMessage"/>.</returns>
		public static Type ToCandleMessageType(this Type candleType)
		{
			if (candleType == null)
				throw new ArgumentNullException("candleType");

			return _candleTypes.GetValue(candleType);
		}

		/// <summary>
		/// Преобразовать тип сообщения <see cref="CandleMessage"/> в тип свечи <see cref="Candle"/>.
		/// </summary>
		/// <param name="messageType">Тип сообщения <see cref="CandleMessage"/>.</param>
		/// <returns>Тип свечи <see cref="Candle"/>.</returns>
		public static Type ToCandleType(this Type messageType)
		{
			if (messageType == null)
				throw new ArgumentNullException("messageType");

			return _candleTypes.GetKey(messageType);
		}

		/// <summary>
		/// Преобразовать свечу в сообщение.
		/// </summary>
		/// <param name="candle">Свеча.</param>
		/// <returns>Сообщение.</returns>
		public static CandleMessage ToMessage(this Candle candle)
		{
			if (candle == null)
				throw new ArgumentNullException("candle");

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
				throw new ArgumentException("Неизвестный тип '{0}' свечки.".Put(candle.GetType()), "candle");

			message.LocalTime = candle.OpenTime.LocalDateTime;
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
			message.State = candle.State;

			return message;
		}

		/// <summary>
		/// Преобразовать собственную сделку в сообщение.
		/// </summary>
		/// <param name="trade">Собственная сделка.</param>
		/// <returns>Сообщение.</returns>
		public static ExecutionMessage ToMessage(this MyTrade trade)
		{
			if (trade == null)
				throw new ArgumentNullException("trade");

			return new ExecutionMessage
			{
				TradeId = trade.Trade.Id,
				TradeStringId = trade.Trade.StringId,
				TradePrice = trade.Trade.Price,
				Volume = trade.Trade.Volume,
				OriginalTransactionId = trade.Order.TransactionId,
				OrderId = trade.Order.Id,
				OrderStringId = trade.Order.StringId,
				OrderType = trade.Order.Type,
				Side = trade.Order.Direction,
				Price = trade.Order.Price,
				SecurityId = trade.Order.Security.ToSecurityId(),
				PortfolioName = trade.Order.Portfolio.Name,
				ExecutionType = ExecutionTypes.Trade,
				ServerTime = trade.Trade.Time,
				OriginSide = trade.Trade.OrderDirection == null ? (Sides?)null : trade.Trade.OrderDirection.Value,
			};
		}

		/// <summary>
		/// Преобразовать заявку в сообщение.
		/// </summary>
		/// <param name="order">Заявка.</param>
		/// <returns>Сообщение.</returns>
		public static ExecutionMessage ToMessage(this Order order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			var message = new ExecutionMessage
			{
				OrderId = order.Id,
				OrderStringId = order.StringId,
				TransactionId = order.TransactionId,
				OriginalTransactionId = order.TransactionId,
				SecurityId = order.Security.ToSecurityId(),
				PortfolioName = order.Portfolio.Name,
				ExecutionType = ExecutionTypes.Order,
				Price = order.Price,
				OrderType = order.Type,
				Volume = order.Volume,
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
			};

			return message;
		}

		/// <summary>
		/// Преобразовать описание ошибки в сообщение.
		/// </summary>
		/// <param name="fail">Описание ошибки.</param>
		/// <returns>Сообщение.</returns>
		public static ExecutionMessage ToMessage(this OrderFail fail)
		{
			if (fail == null)
				throw new ArgumentNullException("fail");

			return new ExecutionMessage
			{
				OrderId = fail.Order.Id,
				OrderStringId = fail.Order.StringId,
				TransactionId = fail.Order.TransactionId,
				OriginalTransactionId = fail.Order.TransactionId,
				SecurityId = fail.Order.Security.ToSecurityId(),
				PortfolioName = fail.Order.Portfolio.Name,
				Error = fail.Error,
				ExecutionType = ExecutionTypes.Order,
				OrderState = OrderStates.Failed,
				ServerTime = fail.ServerTime,
				LocalTime = fail.LocalTime,
			};
		}

		/// <summary>
		/// Преобразовать тиковую сделку в сообщение.
		/// </summary>
		/// <param name="trade">Тиковая сделка.</param>
		/// <returns>Сообщение.</returns>
		public static ExecutionMessage ToMessage(this Trade trade)
		{
			if (trade == null)
				throw new ArgumentNullException("trade");

			return new ExecutionMessage
			{
				LocalTime = trade.LocalTime,
				SecurityId = trade.Security.ToSecurityId(),
				TradeId = trade.Id,
				ServerTime = trade.Time,
				TradePrice = trade.Price,
				Volume = trade.Volume,
				IsSystem = trade.IsSystem,
				TradeStatus = trade.Status,
				OpenInterest = trade.OpenInterest,
				OriginSide = trade.OrderDirection == null ? (Sides?)null : trade.OrderDirection.Value,
				ExecutionType = ExecutionTypes.Tick
			};
		}

		/// <summary>
		/// Преобразовать строчку лога заявок в сообщение.
		/// </summary>
		/// <param name="item">Строчка лога заявок.</param>
		/// <returns>Сообщение.</returns>
		public static ExecutionMessage ToMessage(this OrderLogItem item)
		{
			if (item == null)
				throw new ArgumentNullException("item");

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
				Price = order.Price,
				Volume = order.Volume,
				Balance = order.Balance,
				Side = order.Direction,
				IsSystem = order.IsSystem,
				OrderState = order.State,
				OrderStatus = order.Status,
				TimeInForce = order.TimeInForce,
				ExpiryDate = order.ExpiryDate,
				PortfolioName = order.Portfolio == null ? null : order.Portfolio.Name,
				ExecutionType = ExecutionTypes.OrderLog,
				IsCancelled = (order.State == OrderStates.Done && trade == null),
				TradeId = trade != null ? trade.Id : 0,
				TradePrice = trade != null ? trade.Price : 0,
			};
		}

		/// <summary>
		/// Создать сообщение регистрации новой заявки.
		/// </summary>
		/// <param name="order">Заявка.</param>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <returns>Сообщение.</returns>
		public static OrderRegisterMessage CreateRegisterMessage(this Order order, SecurityId securityId)
		{
			if (order == null)
				throw new ArgumentNullException("order");

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
				IsSystem = order.IsSystem,
				UserOrderId = order.UserOrderId
			};

			order.Security.ToMessage(securityId).CopyTo(msg);

			return msg;
		}

		/// <summary>
		/// Создать сообщение снятия старой заявки.
		/// </summary>
		/// <param name="order">Заявка.</param>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <param name="transactionId">Номер транзакции.</param>
		/// <returns>Сообщение.</returns>
		public static OrderCancelMessage CreateCancelMessage(this Order order, SecurityId securityId, long transactionId)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			var msg = new OrderCancelMessage
			{
				PortfolioName = order.Portfolio.Name,
				OrderType = order.Type,
				OrderTransactionId = order.TransactionId,
				TransactionId = transactionId,
				OrderId = order.Id,
				OrderStringId = order.StringId,
				Volume = order.Balance,
				UserOrderId = order.UserOrderId
			};

			order.Security.ToMessage(securityId).CopyTo(msg);

			return msg;
		}

		/// <summary>
		/// Создать сообщение замены старой заявки на новую.
		/// </summary>
		/// <param name="oldOrder">Старая заявка.</param>
		/// <param name="newOrder">Новая заявка.</param>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <returns>Сообщение.</returns>
		public static OrderReplaceMessage CreateReplaceMessage(this Order oldOrder, Order newOrder, SecurityId securityId)
		{
			if (oldOrder == null)
				throw new ArgumentNullException("oldOrder");

			if (newOrder == null)
				throw new ArgumentNullException("newOrder");

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
				IsSystem = newOrder.IsSystem,

				OldOrderId = oldOrder.Id,
				OldOrderStringId = oldOrder.StringId,
				OldTransactionId = oldOrder.TransactionId,

				UserOrderId = oldOrder.UserOrderId
			};

			oldOrder.Security.ToMessage(securityId).CopyTo(msg);

			return msg;
		}

		/// <summary>
		/// Создать сообщение замены пары старых заявок на новые.
		/// </summary>
		/// <param name="oldOrder1">Старая заявка.</param>
		/// <param name="newOrder1">Новая заявка.</param>
		/// <param name="security1">Идентификатор инструмента.</param>
		/// <param name="oldOrder2">Старая заявка.</param>
		/// <param name="newOrder2">Новая заявка.</param>
		/// <param name="security2">Идентификатор инструмента.</param>
		/// <returns>Сообщение.</returns>
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
		/// Создать сообщение массового снятие заявок.
		/// </summary>
		/// <param name="transactionId">Идентификатор транзакции.</param>
		/// <param name="isStopOrder"><see langword="true"/>, если нужно отменить только стоп-заявки, false - если только обычный и null - если оба типа.</param>
		/// <param name="portfolio">Портфель. Если значение равно null, то портфель не попадает в фильтр снятия заявок.</param>
		/// <param name="direction">Направление заявки. Если значение равно null, то направление не попадает в фильтр снятия заявок.</param>
		/// <param name="board">Торговая площадка. Если значение равно null, то площадка не попадает в фильтр снятия заявок.</param>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <param name="security">Инструмент.</param>
		/// <returns>Сообщение.</returns>
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
		/// Преобразовать инструмент в сообщение.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <param name="originalTransactionId">Идентификатор запроса на поиск инструментов.</param>
		/// <returns>Сообщение.</returns>
		public static SecurityMessage ToMessage(this Security security, SecurityId securityId, long originalTransactionId = 0)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			return new SecurityMessage
			{
				SecurityId = securityId,
				Name = security.Name,
				ShortName = security.ShortName,
				PriceStep = security.PriceStep,
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
				OriginalTransactionId = originalTransactionId
			};
		}

		/// <summary>
		/// Преобразовать сообщение в инструмент.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <returns>Инструмент.</returns>
		public static Security ToSecurity(this SecurityMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			return new Security
			{
				Id = message.SecurityId.SecurityCode + "@" + message.SecurityId.BoardCode,
				Code = message.SecurityId.SecurityCode,
				Board = ExchangeBoard.GetOrCreateBoard(message.SecurityId.BoardCode),
				Type = message.SecurityType ?? message.SecurityId.SecurityType,
				PriceStep = message.PriceStep,
				VolumeStep = message.VolumeStep,
				OptionType = message.OptionType,
				Strike = message.Strike,
				Name = message.Name,
				ShortName = message.ShortName,
				Class = message.Class,
				BinaryOptionType = message.BinaryOptionType,
				ExternalId = message.SecurityId.ToExternalId(),
				ExpiryDate = message.ExpiryDate,
				SettlementDate = message.SettlementDate,
				UnderlyingSecurityId = message.UnderlyingSecurityCode + "@" + message.SecurityId.BoardCode,
				Multiplier = message.Multiplier,
				Currency = message.Currency
			};
		}

		/// <summary>
		/// Преобразовать портфель в сообщение.
		/// </summary>
		/// <param name="portfolio">Портфель.</param>
		/// <returns>Сообщение.</returns>
		public static PortfolioMessage ToMessage(this Portfolio portfolio)
		{
			if (portfolio == null)
				throw new ArgumentNullException("portfolio");

			return new PortfolioMessage
			{
				PortfolioName = portfolio.Name,
				BoardCode = portfolio.Board == null ? null : portfolio.Board.Code,
				Currency = portfolio.Currency,
			};
		}

		/// <summary>
		/// Преобразовать портфель в сообщение.
		/// </summary>
		/// <param name="portfolio">Портфель.</param>
		/// <returns>Сообщение.</returns>
		public static PortfolioChangeMessage ToChangeMessage(this Portfolio portfolio)
		{
			if (portfolio == null)
				throw new ArgumentNullException("portfolio");

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
		/// Преобразовать позицию в сообщение.
		/// </summary>
		/// <param name="position">Позиция.</param>
		/// <param name="localTime">Метка локального времени.</param>
		/// <returns>Сообщение.</returns>
		public static PositionMessage ToMessage(this Position position, DateTime localTime)
		{
			if (position == null)
				throw new ArgumentNullException("position");

			return new PositionMessage
			{
				PortfolioName = position.Portfolio.Name,
				SecurityId = position.Security.ToSecurityId(),
				DepoName = position.DepoName,
				LimitType = position.LimitType,
				LocalTime = localTime,
			};
		}

		/// <summary>
		/// Преобразовать позицию в сообщение.
		/// </summary>
		/// <param name="position">Позиция.</param>
		/// <returns>Сообщение.</returns>
		public static PositionChangeMessage ToChangeMessage(this Position position)
		{
			if (position == null)
				throw new ArgumentNullException("position");

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
		/// Преобразовать площадку в сообщение.
		/// </summary>
		/// <param name="board">Площадка.</param>
		/// <returns>Сообщение.</returns>
		public static BoardMessage ToMessage(this ExchangeBoard board)
		{
			if (board == null)
				throw new ArgumentNullException("board");

			return new BoardMessage
			{
				Code = board.Code,
				ExchangeCode = board.Exchange.Name,
				WorkingTime = board.WorkingTime.Clone(),
				IsSupportMarketOrders = board.IsSupportMarketOrders,
				IsSupportAtomicReRegister = board.IsSupportAtomicReRegister,
				ExpiryTime = board.ExpiryTime,
				TimeZoneInfo = board.Exchange.TimeZoneInfo
			};
		}

		private class ToMessagesEnumerableEx<TEntity, TMessage> : IEnumerableEx<TMessage>
		{
			private readonly IEnumerableEx<TEntity> _entities;

			public ToMessagesEnumerableEx(IEnumerableEx<TEntity> entities)
			{
				if (entities == null)
					throw new ArgumentNullException("entities");

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

			int IEnumerableEx.Count
			{
				get { return _entities.Count; }
			}

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
		/// Преобразовать торговые объекты в сообщения.
		/// </summary>
		/// <typeparam name="TEntity">Тип торгового объекта.</typeparam>
		/// <typeparam name="TMessage">Тип сообщения.</typeparam>
		/// <param name="entities">Торговые объекты.</param>
		/// <returns>Сообщения.</returns>
		public static IEnumerableEx<TMessage> ToMessages<TEntity, TMessage>(this IEnumerableEx<TEntity> entities)
		{
			return new ToMessagesEnumerableEx<TEntity, TMessage>(entities);
		}

		private class ToEntitiesEnumerableEx<TMessage, TEntity> : IEnumerableEx<TEntity>
			where TMessage : Message
		{
			private readonly IEnumerableEx<TMessage> _messages;
			private readonly Security _security;
			//private readonly object _candleArg;
			private readonly Type _candleType;

			public ToEntitiesEnumerableEx(IEnumerableEx<TMessage> messages, Security security)
			{
				if (messages == null)
					throw new ArgumentNullException("messages");

				if (security == null)
					throw new ArgumentNullException("security");

				_messages = messages;
				_security = security;
			}
			
			public ToEntitiesEnumerableEx(IEnumerableEx<TMessage> messages, Security security, Type candleType)
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

			int IEnumerableEx.Count
			{
				get { return _messages.Count; }
			}

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

							case ExecutionTypes.Order:
								return execMsg.ToOrder(_security).To<TEntity>();

							default:
								throw new ArgumentOutOfRangeException("message", LocalizedStrings.Str1122Params.Put(execMsg.ExecutionType));
						}
					}

					case MessageTypes.QuoteChange:
						return message.To<QuoteChangeMessage>().ToMarketDepth(_security).To<TEntity>();

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
		/// Преобразовать сообщения в торговые объекты.
		/// </summary>
		/// <typeparam name="TMessage">Тип сообщения.</typeparam>
		/// <typeparam name="TEntity">Тип торгового объекта.</typeparam>
		/// <param name="messages">Сообщения.</param>
		/// <param name="security">Инструмент.</param>
		/// <returns>Торговые объекты.</returns>
		public static IEnumerableEx<TEntity> ToEntities<TMessage, TEntity>(this IEnumerableEx<TMessage> messages, Security security)
			where TMessage : Message
		{
			return new ToEntitiesEnumerableEx<TMessage, TEntity>(messages, security);
		}

		/// <summary>
		/// Преобразовать сообщения в торговые объекты.
		/// </summary>
		/// <typeparam name="TCandle">Тип свечи.</typeparam>
		/// <param name="messages">Сообщения.</param>
		/// <param name="security">Инструмент.</param>
		/// <param name="candleType">Тип свечи. Еспользуется, если <typeparamref name="TCandle"/> равен <see cref="Candle"/>.</param>
		/// <returns>Торговые объекты.</returns>
		public static IEnumerableEx<TCandle> ToCandles<TCandle>(this IEnumerableEx<CandleMessage> messages, Security security, Type candleType = null)
		{
			return new ToEntitiesEnumerableEx<CandleMessage, TCandle>(messages, security, candleType ?? typeof(TCandle));
		}

		/// <summary>
		/// Преобразовать <see cref="CandleMessage"/> в свечу.
		/// </summary>
		/// <typeparam name="TCandle">Тип свечи.</typeparam>
		/// <param name="message">Сообщение.</param>
		/// <param name="series">Серия.</param>
		/// <returns>Свеча.</returns>
		public static TCandle ToCandle<TCandle>(this CandleMessage message, CandleSeries series)
			where TCandle : Candle, new()
		{
			return (TCandle)message.ToCandle(series);
		}

		/// <summary>
		/// Преобразовать <see cref="CandleMessage"/> в свечу.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <param name="series">Серия.</param>
		/// <returns>Свеча.</returns>
		public static Candle ToCandle(this CandleMessage message, CandleSeries series)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (series == null)
				throw new ArgumentNullException("series");

			var candle = message.ToCandle(series.CandleType, series.Security);
			candle.Series = series;

			if (candle.Arg.IsNull(true))
				candle.Arg = series.Arg;

			return candle;
		}

		/// <summary>
		/// Преобразовать <see cref="CandleMessage"/> в свечу.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <param name="type">Тип свечи.</param>
		/// <param name="security">Инструмент.</param>
		/// <returns>Свеча.</returns>
		public static Candle ToCandle(this CandleMessage message, Type type, Security security)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (security == null)
				throw new ArgumentNullException("security");

			if (type == null)
				throw new ArgumentNullException("type");

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

			candle.State = message.State;

			return candle;
		}

		/// <summary>
		/// Преобразовать сообщение в тиковую сделку.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <param name="security">Инструмент.</param>
		/// <returns>Тиковая сделка.</returns>
		public static Trade ToTrade(this ExecutionMessage message, Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			return message.ToTrade(new Trade { Security = security });
		}

		/// <summary>
		/// Преобразовать сообщение в тиковую сделку.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <param name="trade">Тиковая сделка.</param>
		/// <returns>Тиковая сделка.</returns>
		public static Trade ToTrade(this ExecutionMessage message, Trade trade)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			trade.Id = message.TradeId;
			trade.Price = message.TradePrice;
			trade.Volume = message.Volume;
			trade.Status = message.TradeStatus;
			trade.IsSystem = message.IsSystem;
			trade.Time = message.ServerTime;
			trade.LocalTime = message.LocalTime;
			trade.OpenInterest = message.OpenInterest;
			trade.OrderDirection = message.OriginSide == null ? (Sides?)null : message.OriginSide.Value;
			trade.IsUpTick = message.IsUpTick;

			return trade;
		}

		/// <summary>
		/// Преобразовать сообщение в заявку.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <param name="security">Инструмент.</param>
		/// <returns>Заявка.</returns>
		public static Order ToOrder(this ExecutionMessage message, Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			return message.ToOrder(new Order { Security = security });
		}

		/// <summary>
		/// Преобразовать сообщение в заявку.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <param name="order">заявка.</param>
		/// <returns>Заявка.</returns>
		public static Order ToOrder(this ExecutionMessage message, Order order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			order.Id = message.OrderId;
			order.StringId = message.OrderStringId;
			order.TransactionId = message.TransactionId;
			order.Portfolio = new Portfolio { Board = order.Security.Board, Name = message.PortfolioName };
			order.Direction = message.Side;
			order.Price = message.Price;
			order.Volume = message.Volume;
			order.Balance = message.Balance;
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

			if (message.OrderState != null)
				order.State = (OrderStates)message.OrderState;

			return order;
		}

		/// <summary>
		/// Преобразовать сообщение в стакан.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <param name="security">Инструмент.</param>
		/// <param name="getSecurity">Функция для получения инструмента.</param>
		/// <returns>Стакан.</returns>
		public static MarketDepth ToMarketDepth(this QuoteChangeMessage message, Security security, Func<SecurityId, Security> getSecurity = null)
		{
			return message.ToMarketDepth(new MarketDepth(security), getSecurity);
		}

		/// <summary>
		/// Преобразовать сообщение в стакан.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <param name="marketDepth">Стакан.</param>
		/// <param name="getSecurity">Функция для получения инструмента.</param>
		/// <returns>Стакан.</returns>
		public static MarketDepth ToMarketDepth(this QuoteChangeMessage message, MarketDepth marketDepth, Func<SecurityId, Security> getSecurity = null)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (marketDepth == null)
				throw new ArgumentNullException("marketDepth");

			var security = marketDepth.Security;

			var depth = marketDepth.Update(
				message.Bids.Select(c => c.ToQuote(security, getSecurity)),
				message.Asks.Select(c => c.ToQuote(security, getSecurity)),
				message.IsSorted, message.ServerTime);

			depth.LocalTime = message.LocalTime;
			return depth;
		}

		/// <summary>
		/// Преобразовать котировку в сообщение.
		/// </summary>
		/// <param name="quote">Котировка.</param>
		/// <returns>Сообщение.</returns>
		public static QuoteChange ToQuoteChange(this Quote quote)
		{
			return new QuoteChange(quote.OrderDirection, quote.Price, quote.Volume);
		}

		/// <summary>
		/// Преобразовать сообщение в котировку.
		/// </summary>
		/// <param name="change">Сообщение.</param>
		/// <param name="security">Инструмент.</param>
		/// <param name="getSecurity">Функция для получения инструмента.</param>
		/// <returns>Котировка.</returns>
		public static Quote ToQuote(this QuoteChange change, Security security, Func<SecurityId, Security> getSecurity = null)
		{
			if (!change.BoardCode.IsEmpty() && getSecurity != null)
				security = getSecurity(new SecurityId { SecurityCode = security.Code, BoardCode = change.BoardCode });

			var quote = new Quote(security, change.Price, change.Volume, change.Side);
			change.CopyExtensionInfo(quote);
			return quote;
		}

		/// <summary>
		/// Преобразовать сообщение в строчку лога заявок.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <param name="security">Инструмент.</param>
		/// <returns>Строчка лога заявок.</returns>
		public static OrderLogItem ToOrderLog(this ExecutionMessage message, Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			return message.ToOrderLog(new OrderLogItem
			{
				Order = new Order { Security = security },
				Trade = message.TradeId != 0 ? new Trade { Security = security } : null
			});
		}

		/// <summary>
		/// Преобразовать сообщение в строчку лога заявок.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <param name="item">Строчка лога заявок.</param>
		/// <returns>Строчка лога заявок.</returns>
		public static OrderLogItem ToOrderLog(this ExecutionMessage message, OrderLogItem item)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (item == null)
				throw new ArgumentNullException("item");

			var order = item.Order;

			order.Portfolio = Portfolio.AnonymousPortfolio;

			order.Id = message.OrderId;
			order.StringId = message.OrderStringId;
			order.TransactionId = message.TransactionId;
			order.Price = message.Price;
			order.Volume = message.Volume;
			order.Balance = message.Balance;
			order.Direction = message.Side;
			order.Time = message.ServerTime;
			order.LastChangeTime = message.ServerTime;
			order.LocalTime = message.LocalTime;
			
			order.Status = message.OrderStatus;
			order.TimeInForce = message.TimeInForce;
			order.IsSystem = message.IsSystem;

			if (message.OrderState != null)
				order.State = message.OrderState.Value;
			else
				order.State = message.IsCancelled || message.TradeId != 0 ? OrderStates.Done : OrderStates.Active;

			if (message.TradeId != 0)
			{
				var trade = item.Trade;

				trade.Id = message.TradeId;
				trade.Price = message.TradePrice;
				trade.Time = message.ServerTime;
				trade.Volume = message.Volume;
				trade.IsSystem = message.IsSystem;
				trade.Status = message.TradeStatus;
			}

			return item;
		}

		/// <summary>
		/// Преобразовать новость в сообщение.
		/// </summary>
		/// <param name="news">Новость.</param>
		/// <returns>Сообщение.</returns>
		public static NewsMessage ToMessage(this News news)
		{
			if (news == null)
				throw new ArgumentNullException("news");

			return new NewsMessage
			{
				LocalTime = news.LocalTime,
				ServerTime = news.ServerTime,
				Id = news.Id,
				Story = news.Story,
				Headline = news.Headline,
				SecurityId = news.Security == null ? default(SecurityId) : news.Security.ToSecurityId(),
				BoardCode = news.Board == null ? string.Empty : news.Board.Code,
			};
		}

		/// <summary>
		/// Преобразовать инструмент в <see cref="SecurityId"/>.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="idGenerator">Генератор идентификаторов инструментов <see cref="Security.Id"/>.</param>
		/// <returns>Идентификатор инструмента.</returns>
		public static SecurityId ToSecurityId(this Security security, SecurityIdGenerator idGenerator = null)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			string secCode;
			string boardCode;

			// http://stocksharp.com/forum/yaf_postsm32581_Security-SPFB-RTS-FORTS.aspx#post32581
			// иногда в Security.Code может быть записано неправильное, и необходимо опираться на Security.Id
			if (!security.Id.IsEmpty())
			{
				var parts = (idGenerator ?? new SecurityIdGenerator()).Split(security.Id);

				secCode = parts.Item1;

				// http://stocksharp.com/forum/yaf_postst5143findunread_API-4-2-4-0-Nie-vystavliaiutsia-zaiavki-po-niekotorym-instrumientam-FORTS.aspx
				// для Quik необходимо соблюдение регистра в коде инструмента при выставлении заявок
				if (secCode.CompareIgnoreCase(security.Code))
					secCode = security.Code;

				//if (!boardCode.CompareIgnoreCase(ExchangeBoard.Test.Code))
				boardCode = parts.Item2;
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
		/// Преобразовать <see cref="SecurityId"/> в <see cref="SecurityExternalId"/>.
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
		/// Проверить содержит ли <see cref="SecurityId"/> идентификаторы внешних источников.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <returns><see langword="true"/>, если есть идентификаторы внешних источников, иначе, <see langword="false"/>.</returns>
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
		/// Преобразовать <see cref="SecurityExternalId"/> в <see cref="SecurityId"/>.
		/// </summary>
		/// <param name="externalId"><see cref="SecurityExternalId"/>.</param>
		/// <param name="securityCode">Код инструмента.</param>
		/// <param name="boardCode">Код площадки.</param>
		/// <param name="securityType">Тип инструмента.</param>
		/// <returns><see cref="SecurityId"/>.</returns>
		public static SecurityId ToSecurityId(this SecurityExternalId externalId, string securityCode, string boardCode, SecurityTypes? securityType)
		{
			if (externalId == null)
				throw new ArgumentNullException("externalId");

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
		/// Заполнить сообщение информацией об инструменте.
		/// </summary>
		/// <param name="message">Сообщение на подписку маркет-данных.</param>
		/// <param name="connector">Подключение к торговой системе.</param>
		/// <param name="security">Инструмент.</param>
		/// <returns>Сообщение на подписку маркет-данных.</returns>
		public static MarketDataMessage FillSecurityInfo(this MarketDataMessage message, Connector connector, Security security)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (connector == null)
				throw new ArgumentNullException("connector");

			if (security == null)
				throw new ArgumentNullException("security");

			security.ToMessage(connector.GetSecurityId(security)).CopyTo(message);
			return message;
		}

		/// <summary>
		/// Преобразовать <see cref="Level1ChangeMessage"/> в <see cref="MarketDepth"/>.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <param name="security">Инструмент.</param>
		/// <returns>Стакан.</returns>
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
	}
}
namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using MoreLinq;

	/// <summary>
	/// Вспомогательный класс.
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// Создать <see cref="PortfolioChangeMessage"/>.
		/// </summary>
		/// <param name="adapter">Апаптер к торговой системе.</param>
		/// <param name="pfName">Название портфеля.</param>
		/// <returns>Сообщение об изменении портфеля.</returns>
		public static PortfolioChangeMessage CreatePortfolioChangeMessage(this IMessageAdapter adapter, string pfName)
		{
			if (adapter == null)
				throw new ArgumentNullException("adapter");

			var time = adapter.CurrentTime;

			return new PortfolioChangeMessage
			{
				PortfolioName = pfName,
				LocalTime = time.LocalDateTime,
				ServerTime = time,
			};
		}

		/// <summary>
		/// Создать <see cref="PositionChangeMessage"/>.
		/// </summary>
		/// <param name="adapter">Апаптер к торговой системе.</param>
		/// <param name="pfName">Название портфеля.</param>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <returns>Сообщение об изменении позиции.</returns>
		public static PositionChangeMessage CreatePositionChangeMessage(this IMessageAdapter adapter, string pfName, SecurityId securityId)
		{
			if (adapter == null)
				throw new ArgumentNullException("adapter");

			var time = adapter.CurrentTime;

			return new PositionChangeMessage
			{
				PortfolioName = pfName,
				SecurityId = securityId,
				LocalTime = time.LocalDateTime,
				ServerTime = time,
			};
		}

		/// <summary>
		/// Получить лучший бид.
		/// </summary>
		/// <param name="message">Стакан.</param>
		/// <returns>Лучший бид, или <see langword="null"/>, если котировки на покупку отсутствуют.</returns>
		public static QuoteChange GetBestBid(this QuoteChangeMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			return (message.IsSorted ? message.Bids : message.Bids.OrderByDescending(q => q.Price)).FirstOrDefault();
		}

		/// <summary>
		/// Получить лучший оффер.
		/// </summary>
		/// <param name="message">Стакан.</param>
		/// <returns>Лучший оффер, или <see langword="null"/>, если котировки на продажу отсутствуют.</returns>
		public static QuoteChange GetBestAsk(this QuoteChangeMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			return (message.IsSorted ? message.Asks : message.Asks.OrderBy(q => q.Price)).FirstOrDefault();
		}

		/// <summary>
		/// Преобразовать <see cref="OrderMessage"/> в <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="message"><see cref="OrderMessage"/></param>
		/// <returns><see cref="ExecutionMessage"/></returns>
		public static ExecutionMessage ToExecutionMessage(this OrderMessage message)
		{
			switch (message.Type)
			{
				case MessageTypes.OrderRegister:
					return ((OrderRegisterMessage)message).ToExecutionMessage();

				case MessageTypes.OrderCancel:
					return ((OrderCancelMessage)message).ToExecutionMessage();

				case MessageTypes.OrderGroupCancel:
					return ((OrderGroupCancelMessage)message).ToExecutionMessage();

				case MessageTypes.OrderReplace:
					return ((OrderReplaceMessage)message).ToExecutionMessage();

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Преобразовать <see cref="OrderGroupCancelMessage"/> в <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="message"><see cref="OrderGroupCancelMessage"/></param>
		/// <returns><see cref="ExecutionMessage"/></returns>
		public static ExecutionMessage ToExecutionMessage(this OrderGroupCancelMessage message)
		{
			return new ExecutionMessage
			{
				OriginalTransactionId = message.TransactionId,
				ExecutionType = ExecutionTypes.Order,
			};
		}

		/// <summary>
		/// Преобразовать <see cref="OrderPairReplaceMessage"/> в <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="message"><see cref="OrderPairReplaceMessage"/></param>
		/// <returns><see cref="ExecutionMessage"/></returns>
		public static ExecutionMessage ToExecutionMessage(this OrderPairReplaceMessage message)
		{
			throw new NotImplementedException();
			//return new ExecutionMessage
			//{
			//	LocalTime = message.LocalTime,
			//	OriginalTransactionId = message.TransactionId,
			//	Action = ExecutionActions.Canceled,
			//};
		}

		/// <summary>
		/// Преобразовать <see cref="OrderCancelMessage"/> в <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="message"><see cref="OrderCancelMessage"/></param>
		/// <returns><see cref="ExecutionMessage"/></returns>
		public static ExecutionMessage ToExecutionMessage(this OrderCancelMessage message)
		{
			return new ExecutionMessage
			{
				SecurityId = message.SecurityId,
				OriginalTransactionId = message.TransactionId,
				//OriginalTransactionId = message.OriginalTransactionId,
				OrderId = message.OrderId,
				OrderType = message.OrderType,
				PortfolioName = message.PortfolioName,
				ExecutionType = ExecutionTypes.Order,
				UserOrderId = message.UserOrderId,
			};
		}

		/// <summary>
		/// Преобразовать <see cref="OrderReplaceMessage"/> в <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="message"><see cref="OrderReplaceMessage"/></param>
		/// <returns><see cref="ExecutionMessage"/></returns>
		public static ExecutionMessage ToExecutionMessage(this OrderReplaceMessage message)
		{
			return new ExecutionMessage
			{
				SecurityId = message.SecurityId,
				OriginalTransactionId = message.TransactionId,
				OrderType = message.OrderType,
				Price = message.Price,
				Volume = message.Volume,
				Side = message.Side,
				PortfolioName = message.PortfolioName,
				ExecutionType = ExecutionTypes.Order,
				Condition = message.Condition,
				UserOrderId = message.UserOrderId,
			};
		}

		/// <summary>
		/// Преобразовать <see cref="OrderRegisterMessage"/> в <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="message"><see cref="OrderRegisterMessage"/></param>
		/// <returns><see cref="ExecutionMessage"/></returns>
		public static ExecutionMessage ToExecutionMessage(this OrderRegisterMessage message)
		{
			return new ExecutionMessage
			{
				SecurityId = message.SecurityId,
				OriginalTransactionId = message.TransactionId,
				OrderType = message.OrderType,
				Price = message.Price,
				Volume = message.Volume,
				Balance = message.Volume,
				Side = message.Side,
				PortfolioName = message.PortfolioName,
				ExecutionType = ExecutionTypes.Order,
				Condition = message.Condition,
				UserOrderId = message.UserOrderId,
			};
		}

		/// <summary>
		/// Скопировать расширенную информацию.
		/// </summary>
		/// <param name="from">Объект, из которого копируется расширенная информация.</param>
		/// <param name="to">Объект, в который копируется расширенная информация.</param>
		public static void CopyExtensionInfo(this IExtendableEntity from, IExtendableEntity to)
		{
			if (from == null)
				throw new ArgumentNullException("from");

			if (to == null)
				throw new ArgumentNullException("to");

			if (from.ExtensionInfo == null)
				return;

			if (to.ExtensionInfo == null)
				to.ExtensionInfo = new Dictionary<object, object>();

			foreach (var pair in from.ExtensionInfo)
			{
				to.ExtensionInfo[pair.Key] = pair.Value;
			}
		}

		/// <summary>
		/// Получить серверное время сообщения.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <returns>Серверное время сообщения. Если значение равно <see langword="null"/>, то сообщение не содержит серверное время.</returns>
		public static DateTimeOffset? GetServerTime(this Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Execution:
					return ((ExecutionMessage)message).ServerTime;
				case MessageTypes.QuoteChange:
					return ((QuoteChangeMessage)message).ServerTime;
				case MessageTypes.Level1Change:
					return ((Level1ChangeMessage)message).ServerTime;
				case MessageTypes.Time:
					return ((TimeMessage)message).ServerTime;
				default:
				{
					var candleMsg = message as CandleMessage;
					return candleMsg == null ? (DateTimeOffset?)null : candleMsg.OpenTime;
				}
			}
		}

		/// <summary>
		/// Заполнить <see cref="IMessageAdapter.SupportedMessages"/> типами сообщений, относящихся к транзакционным.
		/// </summary>
		/// <param name="adapter">Адаптер.</param>
		public static void AddTransactionalSupport(this IMessageAdapter adapter)
		{
			adapter.AddSupportedMessage(MessageTypes.OrderCancel);
			adapter.AddSupportedMessage(MessageTypes.OrderGroupCancel);
			adapter.AddSupportedMessage(MessageTypes.OrderPairReplace);
			adapter.AddSupportedMessage(MessageTypes.OrderRegister);
			adapter.AddSupportedMessage(MessageTypes.OrderReplace);
			adapter.AddSupportedMessage(MessageTypes.OrderStatus);
			adapter.AddSupportedMessage(MessageTypes.Portfolio);
			adapter.AddSupportedMessage(MessageTypes.PortfolioLookup);
			adapter.AddSupportedMessage(MessageTypes.Position);
		}

		/// <summary>
		/// Удалить из <see cref="IMessageAdapter.SupportedMessages"/> типы сообщений, относящихся к транзакционным.
		/// </summary>
		/// <param name="adapter">Адаптер.</param>
		public static void RemoveTransactionalSupport(this IMessageAdapter adapter)
		{
			adapter.RemoveSupportedMessage(MessageTypes.OrderCancel);
			adapter.RemoveSupportedMessage(MessageTypes.OrderGroupCancel);
			adapter.RemoveSupportedMessage(MessageTypes.OrderPairReplace);
			adapter.RemoveSupportedMessage(MessageTypes.OrderRegister);
			adapter.RemoveSupportedMessage(MessageTypes.OrderReplace);
			adapter.RemoveSupportedMessage(MessageTypes.OrderStatus);
			adapter.RemoveSupportedMessage(MessageTypes.Portfolio);
			adapter.RemoveSupportedMessage(MessageTypes.PortfolioLookup);
			adapter.RemoveSupportedMessage(MessageTypes.Position);
		}

		/// <summary>
		/// Заполнить <see cref="IMessageAdapter.SupportedMessages"/> типами сообщений, относящихся к маркет-данным.
		/// </summary>
		/// <param name="adapter">Адаптер.</param>
		public static void AddMarketDataSupport(this IMessageAdapter adapter)
		{
			adapter.AddSupportedMessage(MessageTypes.MarketData);
			adapter.AddSupportedMessage(MessageTypes.SecurityLookup);
		}

		/// <summary>
		/// Удалить из <see cref="IMessageAdapter.SupportedMessages"/> типы сообщений, относящихся к маркет-данным.
		/// </summary>
		/// <param name="adapter">Адаптер.</param>
		public static void RemoveMarketDataSupport(this IMessageAdapter adapter)
		{
			adapter.RemoveSupportedMessage(MessageTypes.MarketData);
			adapter.RemoveSupportedMessage(MessageTypes.SecurityLookup);
		}

		/// <summary>
		/// Добавить тип сообщения в <see cref="IMessageAdapter.SupportedMessages"/>.
		/// </summary>
		/// <param name="adapter">Адаптер.</param>
		/// <param name="type">Тип сообщения.</param>
		public static void AddSupportedMessage(this IMessageAdapter adapter, MessageTypes type)
		{
			if (adapter == null)
				throw new ArgumentNullException("adapter");

			adapter.SupportedMessages = adapter.SupportedMessages.Concat(type).ToArray();
		}

		/// <summary>
		/// Удалить тип сообщения из <see cref="IMessageAdapter.SupportedMessages"/>.
		/// </summary>
		/// <param name="adapter">Адаптер.</param>
		/// <param name="type">Тип сообщения.</param>
		public static void RemoveSupportedMessage(this IMessageAdapter adapter, MessageTypes type)
		{
			if (adapter == null)
				throw new ArgumentNullException("adapter");

			adapter.SupportedMessages = adapter.SupportedMessages.Except(new[] { type }).ToArray();
		}
	}
}
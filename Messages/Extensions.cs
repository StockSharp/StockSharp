namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using MoreLinq;

	/// <summary>
	/// Extension class.
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PortfolioChangeMessage"/>.
		/// </summary>
		/// <param name="adapter">Trading system adapter.</param>
		/// <param name="pfName">Portfolio name.</param>
		/// <returns>Portfolio change message.</returns>
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
		/// Initializes a new instance of the <see cref="PositionChangeMessage"/>.
		/// </summary>
		/// <param name="adapter">Trading system adapter.</param>
		/// <param name="pfName">Portfolio name.</param>
		/// <param name="securityId">Security ID.</param>
		/// <returns>Position change message.</returns>
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
		/// Get best bid.
		/// </summary>
		/// <param name="message">Market depth.</param>
		/// <returns>Best bid, or <see langword="null" />, if no bids are empty.</returns>
		public static QuoteChange GetBestBid(this QuoteChangeMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			return (message.IsSorted ? message.Bids : message.Bids.OrderByDescending(q => q.Price)).FirstOrDefault();
		}

		/// <summary>
		/// Get best ask.
		/// </summary>
		/// <param name="message">Market depth.</param>
		/// <returns>Best ask, or <see langword="null" />, if no asks are empty.</returns>
		public static QuoteChange GetBestAsk(this QuoteChangeMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			return (message.IsSorted ? message.Asks : message.Asks.OrderBy(q => q.Price)).FirstOrDefault();
		}

		/// <summary>
		/// Cast <see cref="OrderMessage"/> to the <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="message"><see cref="OrderMessage"/>.</param>
		/// <returns><see cref="ExecutionMessage"/>.</returns>
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
		/// Cast <see cref="OrderGroupCancelMessage"/> to the <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="message"><see cref="OrderGroupCancelMessage"/>.</param>
		/// <returns><see cref="ExecutionMessage"/>.</returns>
		public static ExecutionMessage ToExecutionMessage(this OrderGroupCancelMessage message)
		{
			return new ExecutionMessage
			{
				OriginalTransactionId = message.TransactionId,
				ExecutionType = ExecutionTypes.Order,
			};
		}

		/// <summary>
		/// Cast <see cref="OrderPairReplaceMessage"/> to the <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="message"><see cref="OrderPairReplaceMessage"/>.</param>
		/// <returns><see cref="ExecutionMessage"/>.</returns>
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
		/// Cast <see cref="OrderCancelMessage"/> to the <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="message"><see cref="OrderCancelMessage"/>.</param>
		/// <returns><see cref="ExecutionMessage"/>.</returns>
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
		/// Cast <see cref="OrderReplaceMessage"/> to the <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="message"><see cref="OrderReplaceMessage"/>.</param>
		/// <returns><see cref="ExecutionMessage"/>.</returns>
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
		/// Cast <see cref="OrderRegisterMessage"/> to the <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="message"><see cref="OrderRegisterMessage"/>.</param>
		/// <returns><see cref="ExecutionMessage"/>.</returns>
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
		/// Copy extended info.
		/// </summary>
		/// <param name="from">The object of which is copied to extended information.</param>
		/// <param name="to">The object, which is copied to extended information.</param>
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
		/// Get message server time.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>Server time message. If the value is <see langword="null" />, the message does not contain the server time.</returns>
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
		/// Fill the <see cref="IMessageAdapter.SupportedMessages"/> message types related to transactional.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
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
		/// Remove from <see cref="IMessageAdapter.SupportedMessages"/> message types related to transactional.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
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
		/// Fill the <see cref="IMessageAdapter.SupportedMessages"/> message types related to market-data.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		public static void AddMarketDataSupport(this IMessageAdapter adapter)
		{
			adapter.AddSupportedMessage(MessageTypes.MarketData);
			adapter.AddSupportedMessage(MessageTypes.SecurityLookup);
		}

		/// <summary>
		/// Remove from <see cref="IMessageAdapter.SupportedMessages"/> message types related to market-data.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		public static void RemoveMarketDataSupport(this IMessageAdapter adapter)
		{
			adapter.RemoveSupportedMessage(MessageTypes.MarketData);
			adapter.RemoveSupportedMessage(MessageTypes.SecurityLookup);
		}

		/// <summary>
		/// Add the message type info <see cref="IMessageAdapter.SupportedMessages"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="type">Message type.</param>
		public static void AddSupportedMessage(this IMessageAdapter adapter, MessageTypes type)
		{
			if (adapter == null)
				throw new ArgumentNullException("adapter");

			adapter.SupportedMessages = adapter.SupportedMessages.Concat(type).ToArray();
		}

		/// <summary>
		/// Remove the message type from <see cref="IMessageAdapter.SupportedMessages"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="type">Message type.</param>
		public static void RemoveSupportedMessage(this IMessageAdapter adapter, MessageTypes type)
		{
			if (adapter == null)
				throw new ArgumentNullException("adapter");

			adapter.SupportedMessages = adapter.SupportedMessages.Except(new[] { type }).ToArray();
		}

		/// <summary>
		/// Determines whether the specified message type is supported by the adapter.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="type">Message type.</param>
		/// <returns><see langword="true"/> if the specified message type is supported, otherwise, <see langword="false"/>.</returns>
		public static bool IsMessageSupported(this IMessageAdapter adapter, MessageTypes type)
		{
			if (adapter == null)
				throw new ArgumentNullException("adapter");

			return adapter.SupportedMessages.Contains(type);
		}
	}
}
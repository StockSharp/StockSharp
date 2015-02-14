namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	static class Helper
	{
		public static void ChechOrderState(this Order order, bool checkVolume = true)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			if (order.Type == OrderTypes.Conditional && order.Condition == null)
				throw new ArgumentException(LocalizedStrings.Str889, "order");

			if (order.Security == null)
				throw new ArgumentException(LocalizedStrings.Str890, "order");

			if (order.Portfolio == null)
				throw new ArgumentException(LocalizedStrings.Str891, "order");

			if (order.Price < 0)
				throw new ArgumentOutOfRangeException("order", order.Price, LocalizedStrings.Str892);

			if (order.Price == 0 && (order.Type == OrderTypes.Limit || order.Type == OrderTypes.ExtRepo || order.Type == OrderTypes.Repo || order.Type == OrderTypes.Rps))
				throw new ArgumentException(LocalizedStrings.Str893, "order");

			if (checkVolume && order.Volume == 0)
				throw new ArgumentException(LocalizedStrings.Str894, "order");

			if (checkVolume && order.Volume < 0)
				throw new ArgumentOutOfRangeException("order", order.Volume, LocalizedStrings.Str895);
		}

		public static void CheckOnNew(this Order order, bool checkVolume = true, bool checkTransactionId = true)
		{
			order.ChechOrderState(checkVolume);

			if (order.Id != 0 || !order.StringId.IsEmpty())
				throw new ArgumentException(LocalizedStrings.Str896Params.Put(order.Id == 0 ? order.StringId : order.Id.To<string>()), "order");

			if (!checkTransactionId)
				return;

			if (order.TransactionId != 0)
				throw new ArgumentException(LocalizedStrings.Str897Params.Put(order.TransactionId), "order");

			if (order.State != OrderStates.None)
				throw new ArgumentException(LocalizedStrings.Str898Params.Put(order.State), "order");
		}

		public static void InitOrder(this Order order, IConnector connector, IdGenerator transactionIdGenerator)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			if (connector == null)
				throw new ArgumentNullException("connector");

			if (transactionIdGenerator == null)
				throw new ArgumentNullException("transactionIdGenerator");

			order.Balance = order.Volume;

			if (order.ExtensionInfo == null)
				order.ExtensionInfo = new Dictionary<object, object>();

			//order.InitializationTime = trader.MarketTime;
			if (order.TransactionId == 0)
				order.TransactionId = transactionIdGenerator.GetNextId();
			
			order.Connector = connector;

			if (order.Security is ContinuousSecurity)
				order.Security = ((ContinuousSecurity)order.Security).GetSecurity(order.Security.ToExchangeTime(connector.CurrentTime));

			order.LocalTime = connector.CurrentTime.LocalDateTime;
			order.State = OrderStates.Pending;
		}

		public static void CheckOnOld(this Order order)
		{
			order.ChechOrderState(false);

			if (order.TransactionId == 0 && order.Id == 0 && order.StringId.IsEmpty())
				throw new ArgumentException(LocalizedStrings.Str899, "order");
		}

		public static void CheckOption(this Security option)
		{
			if (option == null)
				throw new ArgumentNullException("option");

			if (option.Type != SecurityTypes.Option)
				throw new ArgumentException(LocalizedStrings.Str900Params.Put(option.Type), "option");

			if (option.OptionType == null)
				throw new ArgumentException(LocalizedStrings.Str703Params.Put(option), "option");

			if (option.ExpiryDate == null)
				throw new ArgumentException(LocalizedStrings.Str901Params.Put(option), "option");

			if (option.UnderlyingSecurityId == null)
				throw new ArgumentException(LocalizedStrings.Str902Params.Put(option), "option");
		}

		public static ExchangeBoard CheckExchangeBoard(this Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			if (security.Board == null)
				throw new ArgumentException(LocalizedStrings.Str903Params.Put(security), "security");

			return security.Board;
		}

		public static IConnector CheckTrader(this Order order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			if (order.Connector == null)
				throw new ArgumentException(LocalizedStrings.Str904Params.Put(order.TransactionId), "order");

			return order.Connector;
		}

		public static bool ChangeContinuousSecurity(this IConnector connector, Order order)
		{
			if (connector == null)
				throw new ArgumentNullException("connector");

			if (order == null)
				throw new ArgumentNullException("order");

			var cs = order.Security as ContinuousSecurity;

			while (cs != null)
			{
				order.Security = cs.GetSecurity(order.Security.ToExchangeTime(connector.CurrentTime));
				cs = order.Security as ContinuousSecurity;
			}

			return true;
		}

		public static Security CheckPriceStep(this Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			if (security.PriceStep == 0)
				throw new ArgumentException(LocalizedStrings.Str905Params.Put(security.Id));

			return security;
		}

		public static int ChangeSubscribers<T>(this CachedSynchronizedDictionary<T, int> subscribers, T subscriber, int delta)
		{
			if (subscribers == null)
				throw new ArgumentNullException("subscribers");

			lock (subscribers.SyncRoot)
			{
				var value = subscribers.TryGetValue2(subscriber) ?? 0;

				value += delta;

				if (value > 0)
					subscribers[subscriber] = value;
				else
					subscribers.Remove(subscriber);

				return value;
			}
		}
	}
}
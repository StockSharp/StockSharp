namespace StockSharp.InteractiveBrokers
{
	using System;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The helper class that is used to access to specific Interactive Brokers information via <see cref="IExtendableEntity.ExtensionInfo"/>.
	/// </summary>
	public static class Extensions
	{
		//#region ContractId

		//private const string _contractId = "ContractId";

		///// <summary>
		///// Получить идентификатор инструмента в системе Interactive Brokers.
		///// </summary>
		///// <param name="security">Инструмент.</param>
		///// <returns>Идентификатор инструмента в системе Interactive Brokers. Если идентификатор отсутствует, то будет возвращено <see langword="null"/>.</returns>
		//public static int? GetContractId(this Security security)
		//{
		//	return security.GetValue<int?>(_contractId);
		//}

		///// <summary>
		///// Задать идентификатор инструмента в системе Interactive Brokers.
		///// </summary>
		///// <param name="security">Инструмент.</param>
		///// <param name="contractId">Идентификатор инструмента в системе Interactive Brokers.</param>
		//public static void SetContractId(this Security security, int contractId)
		//{
		//	security.AddValue(_contractId, contractId);
		//}

		//#endregion

		//#region LocalCode

		//private const string _localCode = "LocalCode";

		///// <summary>
		///// Получить локальный код инструмента.
		///// </summary>
		///// <param name="security">Инструмент.</param>
		///// <returns>Локальный код инструмента. Если код отсутствует, то будет возвращено <see langword="null"/>.</returns>
		//public static string GetLocalCode(this Security security)
		//{
		//	return security.GetValue<string>(_localCode);
		//}

		//internal static void SetLocalCode(this Security security, string localCode)
		//{
		//	security.AddValue(_localCode, localCode);
		//}

		//#endregion

		//#region Multiplier

		//private const string _multiplier = "Multiplier";

		///// <summary>
		///// Получить множитель дериватива.
		///// </summary>
		///// <param name="security">Инструмент.</param>
		///// <returns>Множитель. Если множитель отсутствует, то будет возвращено <see langword="null"/>.</returns>
		//public static decimal? GetMultiplier(this Security security)
		//{
		//	return security.GetValue<decimal?>(_multiplier);
		//}

		//internal static void SetMultiplier(this Security security, decimal? multiplier)
		//{
		//	security.AddValue(_multiplier, multiplier);
		//}

		//#endregion

		#region RoutingExchange

		private const string _routingExchange = "RoutingBoard";

		/// <summary>
		/// To get the exchange board of orders fulfilment for the instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>The exchange board of orders fulfilment. If the board does not exist then <see langword="null" /> will be returned.</returns>
		public static ExchangeBoard GetRoutingBoard(this Security security)
		{
			return ExchangeBoard.GetOrCreateBoard(security.GetValue<string>(_routingExchange));
		}

		internal static string GetRoutingBoard(this SecurityMessage security)
		{
			return security.GetValue<string>(_routingExchange);
		}

		internal static void SetRoutingBoard(this SecurityMessage security, string exchange)
		{
			security.AddValue(_routingExchange, exchange);
		}

		#endregion

		private const string _marketName = "MarketName";

		internal static void SetMarketName(this SecurityMessage security, string marketName)
		{
			security.AddValue(_marketName, marketName);
		}

		private const string _orderTypes = "OrderTypes";

		internal static void SetOrderTypes(this SecurityMessage security, string orderTypes)
		{
			security.AddValue(_orderTypes, orderTypes);
		}

		private const string _validExchanges = "ValidExchanges";

		internal static void SetValidExchanges(this SecurityMessage security, string validExchanges)
		{
			security.AddValue(_validExchanges, validExchanges);
		}

		private const string _priceMagnifier = "PriceMagnifier";

		internal static void SetPriceMagnifier(this SecurityMessage security, decimal priceMagnifier)
		{
			security.AddValue(_priceMagnifier, priceMagnifier);
		}

		private const string _contractMonth = "ContractMonth";

		internal static void SetContractMonth(this SecurityMessage security, string contractMonth)
		{
			security.AddValue(_contractMonth, contractMonth);
		}

		private const string _industry = "Industry";

		internal static void SetIndustry(this SecurityMessage security, string industry)
		{
			security.AddValue(_industry, industry);
		}

		private const string _category = "Category";

		internal static void SetCategory(this SecurityMessage security, string category)
		{
			security.AddValue(_category, category);
		}

		private const string _subCategory = "SubCategory";

		internal static void SetSubCategory(this SecurityMessage security, string subCategory)
		{
			security.AddValue(_subCategory, subCategory);
		}

		private const string _timeZoneId = "TimeZoneId";

		internal static void SetTimeZoneId(this SecurityMessage security, string timeZoneId)
		{
			security.AddValue(_timeZoneId, timeZoneId);
		}

		private const string _tradingHours = "TradingHours";

		internal static void SetTradingHours(this SecurityMessage security, string tradingHours)
		{
			security.AddValue(_tradingHours, tradingHours);
		}

		private const string _liquidHours = "LiquidHours";

		internal static void SetLiquidHours(this SecurityMessage security, string liquidHours)
		{
			security.AddValue(_liquidHours, liquidHours);
		}

		internal static void SetEvRule(this SecurityMessage security, string evRule)
		{
			security.AddValue(_evRule, evRule);
		}

		internal static void SetEvMultiplier(this SecurityMessage security, decimal evMultiplier)
		{
			security.AddValue(_evMultiplier, evMultiplier);
		}

		private const string _marketValue = "MarketValue";

		internal static void SetMarketValue(this SecurityMessage position, decimal marketValue)
		{
			position.AddValue(_marketValue, marketValue);
		}

		#region ClientId

		private const string _clientId = "ClientId";

		/// <summary>
		/// To get the customer identifier.
		/// </summary>
		/// <param name="trade">Own trade.</param>
		/// <returns>The customer identifier. If the identifier does not exist then <see langword="null" /> will be returned.</returns>
		public static int? GetClientId(this MyTrade trade)
		{
			return trade.GetValue<int?>(_clientId);
		}

		internal static void SetClientId(this ExecutionMessage trade, int clientId)
		{
			trade.AddValue(_clientId, clientId);
		}

		#endregion

		#region Liquidation

		private const string _liquidation = "Liquidation";

		/// <summary>
		/// To get the liquidation position.
		/// </summary>
		/// <param name="trade">Own trade.</param>
		/// <returns>The liquidation position. If the position does not exist then <see langword="null" /> will be returned.</returns>
		public static int? GetLiquidation(this MyTrade trade)
		{
			return trade.GetValue<int?>(_liquidation);
		}

		internal static void SetLiquidation(this ExecutionMessage trade, int liquidation)
		{
			trade.AddValue(_liquidation, liquidation);
		}

		#endregion

		#region PermId

		private const string _permId = "PermId";

		//public static int GetPermId(this MyTrade trade)
		//{
		//	return trade.GetValue<int>(_permId);
		//}

		internal static void SetPermId(this ExecutionMessage trade, int permId)
		{
			trade.AddValue(_permId, permId);
		}

		#endregion

		#region CumulativeQuantity

		private const string _cumulativeQuantity = "CumulativeQuantity";

		/// <summary>
		/// To get a number of profitable contracts in the trade.
		/// </summary>
		/// <param name="trade">Own trade.</param>
		/// <returns>A number of profitable contracts in the trade. If a number does not exist then <see langword="null" /> will be returned.</returns>
		public static int? GetCumulativeQuantity(this MyTrade trade)
		{
			return trade.GetValue<int?>(_cumulativeQuantity);
		}

		internal static void SetCumulativeQuantity(this ExecutionMessage trade, int cummVolume)
		{
			trade.AddValue(_cumulativeQuantity, cummVolume);
		}

		#endregion

		#region AveragePrice

		private const string _averagePrice = "AveragePrice";

		/// <summary>
		/// To get the average price of matched order.
		/// </summary>
		/// <param name="trade">Own trade.</param>
		/// <returns>The average price of the matched order. If the price does not exist then <see langword="null" /> will be returned.</returns>
		public static decimal? GetAveragePrice(this MyTrade trade)
		{
			return trade.GetValue<decimal?>(_averagePrice);
		}

		internal static void SetAveragePrice(this ExecutionMessage trade, decimal price)
		{
			trade.AddValue(_averagePrice, price);
		}

		#endregion

		#region OrderRef

		private const string _orderRef = "OrderRef";

		/// <summary>
		/// To get the order description.
		/// </summary>
		/// <param name="trade">Own trade.</param>
		/// <returns>The order description. If the description does not exist then <see langword="null" /> will be returned.</returns>
		public static string GetOrderRef(this MyTrade trade)
		{
			return trade.GetValue<string>(_orderRef);
		}

		internal static void SetOrderRef(this ExecutionMessage trade, string orderRef)
		{
			trade.AddValue(_orderRef, orderRef);
		}
		
		#endregion

		#region Rule

		private const string _evRule = "EvRule";

		/// <summary>
		/// To get the Economic Value rule.
		/// </summary>
		/// <param name="trade">Own trade.</param>
		/// <returns>The Economic Value rule. If the rule does not exist then <see langword="null" /> will be returned.</returns>
		public static string GetEvRule(this MyTrade trade)
		{
			return trade.GetValue<string>(_evRule);
		}

		internal static void SetEvRule(this ExecutionMessage trade, string rule)
		{
			trade.AddValue(_evRule, rule);
		}

		#endregion

		#region Multiplier

		private const string _evMultiplier = "EvMultiplier";

		/// <summary>
		/// To get the contract market price factor when the price changes by 1.
		/// </summary>
		/// <param name="trade">Own trade.</param>
		/// <returns>The market price factor. If the factor does not exist then <see langword="null" /> will be returned.</returns>
		public static decimal? GetEvMultiplier(this MyTrade trade)
		{
			return trade.GetValue<decimal?>(_evMultiplier);
		}

		internal static void SetEvMultiplier(this ExecutionMessage trade, decimal multiplier)
		{
			trade.AddValue(_evMultiplier, multiplier);
		}

		#endregion

		#region PermId

		//public static int GetPermId(this Order order)
		//{
		//	return order.GetValue<int>(_permId);
		//}

		internal static void SetPermId(this Order order, int permId)
		{
			order.AddValue(_permId, permId);
		}

		#endregion

		#region ClientId

		/// <summary>
		/// To get the customer identifier.
		/// </summary>
		/// <param name="order">Order.</param>
		/// <returns>The customer identifier. If the identifier does not exist then <see langword="null" /> will be returned.</returns>
		public static int? GetClientId(this Order order)
		{
			return order.GetValue<int?>(_clientId);
		}

		#endregion

		#region LastTradePrice

		private const string _lastTradePrice = "LastTradePrice";

		/// <summary>
		/// To get the last trade price.
		/// </summary>
		/// <param name="order">Order.</param>
		/// <returns>The last trade price. If the price does not exist then <see langword="null" /> will be returned.</returns>
		public static decimal GetLastTradePrice(this Order order)
		{
			return order.GetValue<decimal>(_lastTradePrice);
		}

		internal static void SetLastTradePrice(this ExecutionMessage order, decimal price)
		{
			order.AddValue(_lastTradePrice, price);
		}

		#endregion

		#region WhyHeld

		private const string _whyHeld = "WhyHeld";

		/// <summary>
		/// To get the order retention reason.
		/// </summary>
		/// <param name="order">Order.</param>
		/// <returns>The order retention reason. If the reason does not exist then <see langword="null" /> will be returned.</returns>
		public static string GetWhyHeld(this Order order)
		{
			return order.GetValue<string>(_whyHeld);
		}

		internal static void SetWhyHeld(this ExecutionMessage order, string whyHeld)
		{
			order.AddValue(_whyHeld, whyHeld);
		}
		
		#endregion

		internal static OrderStates ToOrderState(this OrderStatus status)
		{
			switch (status)
			{
				case OrderStatus.SentToServer:
				case OrderStatus.ReceiveByServer:
					return OrderStates.Pending;
				case OrderStatus.GateError:
					return OrderStates.Failed;
				case OrderStatus.SentToCanceled:
				case OrderStatus.Accepted:
					return OrderStates.Active;
				case OrderStatus.Cancelled:
				case OrderStatus.Matched:
					return OrderStates.Done;
				default:
					throw new InvalidOperationException(LocalizedStrings.Str2527Params.Put(status));
			}
		}
	}
}
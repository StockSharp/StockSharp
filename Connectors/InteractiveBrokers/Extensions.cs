namespace StockSharp.InteractiveBrokers
{
	using System;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Вспомагательный класс, который используется для доступа к специфичной Interactive Brokers информации через <see cref="IExtendableEntity.ExtensionInfo"/>.
	/// </summary>
	public static class Extensions
	{
		//#region ContractId

		//private const string _contractId = "ContractId";

		///// <summary>
		///// Получить идентификатор инструмента в системе Interactive Brokers.
		///// </summary>
		///// <param name="security">Инструмент.</param>
		///// <returns>Идентификатор инструмента в системе Interactive Brokers. Если идентификатор отсутствует, то будет возвращено null.</returns>
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
		///// <returns>Локальный код инструмента. Если код отсутствует, то будет возвращено null.</returns>
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
		///// <returns>Множитель. Если множитель отсутствует, то будет возвращено null.</returns>
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
		/// Получить для инструмента биржевую площадку исполнения заявок.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns>Биржевая площадка исполнения заявок. Если площадка отсутствует, то будет возвращено null.</returns>
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
		/// Получить идентификатор клиента.
		/// </summary>
		/// <param name="trade">Собственная сделка.</param>
		/// <returns>Идентификатор клиента. Если идентификатор отсутствует, то будет возвращено null.</returns>
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
		/// Получить позицию ликвидации.
		/// </summary>
		/// <param name="trade">Собственная сделка.</param>
		/// <returns>Позиция ликвидации. Если позиция отсутствует, то будет возвращено null.</returns>
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
		/// Получить количество прибыльных контрактов в сделке.
		/// </summary>
		/// <param name="trade">>Собственная сделка.</param>
		/// <returns>Количество прибыльных контрактов в сделке. Если количество отсутствует, то будет возвращено null.</returns>
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
		/// Получить среднюю цену исполнения сделки.
		/// </summary>
		/// <param name="trade">Собственная сделка.</param>
		/// <returns>Средняя цена исполнения сделки. Если цена отсутствует, то будет возвращено null.</returns>
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
		/// Получить описание заявки.
		/// </summary>
		/// <param name="trade">Собственная сделка.</param>
		/// <returns>Описание заявки. Если описание отсутствует, то будет возвращено null.</returns>
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
		/// Получить Economic Value правило.
		/// </summary>
		/// <param name="trade">Собственная сделка.</param>
		/// <returns>Economic Value правило. Если правило отсутствует, то будет возвращено null.</returns>
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
		/// Получить множитель рыночной цены контракта при изменении цены на 1.
		/// </summary>
		/// <param name="trade">Собственная сделка.</param>
		/// <returns>Множитель рыночной цены. Если множитель отсутствует, то будет возвращено null.</returns>
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
		/// Получить идентификатор клиента.
		/// </summary>
		/// <param name="order">Заявка.</param>
		/// <returns>Идентификатор клиента. Если идентификатор отсутствует, то будет возвращено null.</returns>
		public static int? GetClientId(this Order order)
		{
			return order.GetValue<int?>(_clientId);
		}

		#endregion

		#region LastTradePrice

		private const string _lastTradePrice = "LastTradePrice";

		/// <summary>
		/// Получить цену последней сделки.
		/// </summary>
		/// <param name="order">Заявка.</param>
		/// <returns>Цена последней сделки. Если цена отсутствует, то будет возвращено null.</returns>
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
		/// Получить причину удержания заявки.
		/// </summary>
		/// <param name="order">Заявка.</param>
		/// <returns>Причина удержания. Если причина отсутствует, то будет возвращено null.</returns>
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
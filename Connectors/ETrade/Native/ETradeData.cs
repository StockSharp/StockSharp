#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.ETrade.Native.ETrade
File: ETradeData.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.ETrade.Native
{
	using System;
	using System.Collections.Generic;

	// these classes made public only for XML serialization/deserialization. no xml comment needed.
	#pragma warning disable 1591

	#region Accounts

	/// <summary>
	/// accountlist response.
	/// </summary>
	[Serializable]
	public class AccountListResponse
	{
		public List<AccountInfo> response {get; set;} 
	}

	/// <summary>
	/// The account information.
	/// </summary>
	[Serializable]
	public class AccountInfo
	{
		public string accountDesc {get; set;}
		public int accountId {get; set;}
		public string marginLevel {get; set;}
		public double netAccountValue {get; set;}
		public string registrationType {get; set;}
	}

	#endregion

	#region Positions

	/// <summary>
	/// accountpositions response.
	/// </summary>
	[Serializable]
	public class AccountPositionsResponse
	{
		public string accountId { get; set; }
		public int count { get; set; }
		public string marker { get; set; }
		public List<PositionInfo> response {get; set;} 
	}

	/// <summary>
	/// The position information.
	/// </summary>
	[Serializable]
	public class PositionInfo
	{
		public double costBasis { get; set; }
		public string description { get; set; }
		public string longOrShort { get; set; }
		public bool marginable { get; set; }
		public double qty { get; set; }
		public double currentPrice { get; set; }
		public double closePrice { get; set; }
		public double marketValue { get; set; }
		public string quoteType { get; set; }
		public bool adjustedOption { get; set; }
		public string deliverableStr { get; set; }
		public ProductId productId { get; set; }
	}

	/// <summary>
	/// Position security information.
	/// </summary>
	[Serializable]
	public class ProductId
	{
		public string symbol { get; set; }
		public string typeCode { get; set; }
		public string callPut { get; set; }
		public double strikePrice { get; set; }
		public int expYear { get; set; }
		public int expMonth { get; set; }
		public int expDay { get; set; }
	}

	#endregion

	#region OrderList

	/// <summary>
	/// OrderList response.
	/// </summary>
	[Serializable]
	public class GetOrderListResponse
	{
		public OrderListResponse orderListResponse { get; set; }
	}

	/// <summary>
	/// OrderList response.
	/// </summary>
	[Serializable]
	public class OrderListResponse
	{
		public int count { get; set; }
		public string marker { get; set; }
		public List<OrderDetails> orderDetails { get; set; }
	}

	/// <summary>
	/// ETrade data container for an order.
	/// </summary>
	[Serializable]
	public class OrderDetails
	{
		public Order order { get; set; }
	}

	/// <summary>
	/// ETrade order leg.
	/// </summary>
	[Serializable]
	public class LegDetails
	{
		public long legNumber { get; set; }
		public string symbolDescription { get; set; }
		public string orderAction { get; set; }
		public double orderedQuantity { get; set; }
		public double filledQuantity { get; set; }
		public double executedPrice { get; set; }
		public double estimatedCommission { get; set; }
		public double estimatedFees { get; set; }
		public double reserveQuantity { get; set; }
		public SymbolInfo symbolInfo { get; set; }
	}

	/// <summary>
	/// The instrument information.
	/// </summary>
	[Serializable]
	public class SymbolInfo
	{
		public string symbol { get; set; }
	}

	/// <summary>
	/// THe order information ETrade.
	/// </summary>
	[Serializable]
	public class Order
	{
		public long orderId { get; set; }
		public long orderPlacedTime { get; set; }
		public long orderExecutedTime { get; set; }
		public double orderValue { get; set; }
		public string orderStatus { get; set; }
		public string orderType { get; set; }
		public string orderTerm { get; set; }
		public string priceType { get; set; }
		public double limitPrice { get; set; }
		public double stopPrice { get; set; }
		public double stopLimitPrice { get; set; }
		public string routingDestination { get; set; }
		public long replacedByOrderId { get; set; }
		public bool allOrNone { get; set; }
		public double bracketLimitPrice { get; set; }
		public double initialStopPrice { get; set; }
		public double trailPrice { get; set; }
		public double triggerPrice { get; set; }
		public double conditionPrice { get; set; }
		public string conditionSymbol { get; set; }
		public string conditionType { get; set; }
		public string conditionFollowPrice { get; set; }
		public List<LegDetails> legDetails { get; set; }
	}

	#endregion

	#region OrderRequest

	/// <summary>
	/// Base class for ETrade requests.
	/// </summary>
	[Serializable]
	public abstract class OrderRequestBase
	{
		public long accountId { get; set; }
		public string previewId { get; set; }
		public string clientOrderId { get; set; }
		public string priceType { get; set; }
		public double limitPrice { get; set; }
		public double stopPrice { get; set; }
		public bool allOrNone { get; set; }
		public int quantity { get; set; }
		public bool reserveOrder { get; set; }
		public string reserveQuantity { get; set; }
		public string orderTerm { get; set; }
	}


	/// <summary>
	/// The order registration request.
	/// </summary>
	[Serializable]
	public class EquityOrderRequest : OrderRequestBase
	{
		public string symbol { get; set; }
		public string orderAction { get; set; }
		public string marketSession { get; set; }
		public string routingDestination { get; set; }
	}

	/// <summary>
	/// The order registration response.
	/// </summary>
	[Serializable]
	public class PlaceEquityOrderResponse
	{
		public PlaceEquityOrderResponse2 EquityOrderResponse { get; set; }
	}

	/// <summary>
	/// The order registration response.
	/// </summary>
	[Serializable]
	public class PlaceEquityOrderResponse2
	{
		public long accountId { get; set; }
		public bool allOrNone { get; set; }
		public double estimatedCommission { get; set; }
		public double estimatedTotalAmount { get; set; }
		public List<PlaceOrderResponseMessage> messageList { get; set; }
		public int orderNum { get; set; }
		public long orderTime { get; set; }
		public string symbolDesc { get; set; }
		public string symbol { get; set; }
		public int quantity { get; set; }
		public bool reserverOrder { get; set; }
		public int reserveQuantity { get; set; }
		public string orderTerm { get; set; }
		public string orderAction { get; set; }
		public string priceType { get; set; }
		public double limitPrice { get; set; }
		public double stopPrice { get; set; }
		public string routingDestination { get; set; }
	}

	/// <summary>
	/// ETrade text message about the order.
	/// </summary>
	[Serializable]
	public class PlaceOrderResponseMessage
	{
		public string msgDesc { get; set; }
		public int msgCode { get; set; }
	}

	#endregion

	#region OrderChange

	/// <summary>
	/// The order modification request.
	/// </summary>
	[Serializable]
	public class changeEquityOrderRequest : OrderRequestBase
	{
		public int orderNum { get; set; }
	}

	/// <summary>
	/// The order modification response.
	/// </summary>
	[Serializable]
	public class placeChangeEquityOrderResponse
	{
		public PlaceEquityOrderResponse2 equityOrderResponse { get; set; }
	}

	#endregion

	#region OrderCancel

	/// <summary>
	/// The order cancellation request.
	/// </summary>
	[Serializable]
	public class cancelOrderRequest
	{
		public long accountId { get; set; }
		public int orderNum { get; set; }
	}

	/// <summary>
	/// The order cancellation response.
	/// </summary>
	[Serializable]
	public class cancelOrderResponse
	{
		public CancelOrderResponse2 cancelResponse { get; set; }
	}

	/// <summary>
	/// The order cancellation response.
	/// </summary>
	[Serializable]
	public class CancelOrderResponse2
	{
		public long accountId { get; set; }
		public int orderNum { get; set; }
		public long cancelTime { get; set; }
		public string resultMessage { get; set; }
	}

	#endregion

	#region lookup product

	/// <summary>
	/// ProductLookup response.
	/// </summary>
	[Serializable]
	public class productLookupResponse
	{
		public List<ProductInfo> productList { get; set; }
	}

	/// <summary>
	/// The instrument information.
	/// </summary>
	[Serializable]
	public class ProductInfo
	{
		public string companyName {get; set;}
		public string exchange {get; set;}
		public string securityType {get; set;}
		public string symbol {get; set;}
	}

	#endregion

	#region Rate Limits

	/// <summary>
	/// limits response.
	/// </summary>
	[Serializable]
	class RateLimitStatus
	{
		public string consumerKey { get; set; }
		public int limitIntervalInSeconds { get; set; }
		public int requestsLimit { get; set; }
		public int requestsRemaining { get; set; }
		public string resetTime { get; set; }
		public long resetTimeEpochSeconds { get; set; }
	}

	#endregion

	#pragma warning restore 1591
}

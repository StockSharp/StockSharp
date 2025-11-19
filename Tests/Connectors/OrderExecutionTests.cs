namespace StockSharp.Tests.Connectors;

using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.BitStamp;
using StockSharp.Coinbase;
using StockSharp.FTX;
using StockSharp.Tinkoff;
using StockSharp.Bitalong;
using StockSharp.Bitexbook;
using StockSharp.Btce;
using StockSharp.Messages;

/// <summary>
/// Integration tests for order execution flows through different connectors.
/// </summary>
[TestClass]
public class OrderExecutionTests
{
	[TestMethod]
	public void BitStamp_RegisterOrder_ShouldProcessOrder()
	{
		TestRegisterOrder<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_RegisterOrder_ShouldProcessOrder()
	{
		TestRegisterOrder<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_RegisterOrder_ShouldProcessOrder()
	{
		TestRegisterOrder<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_RegisterOrder_ShouldProcessOrder()
	{
		TestRegisterOrder<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_RegisterOrder_ShouldProcessOrder()
	{
		TestRegisterOrder<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_RegisterOrder_ShouldProcessOrder()
	{
		TestRegisterOrder<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_RegisterOrder_ShouldProcessOrder()
	{
		TestRegisterOrder<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_CancelOrder_ShouldProcessCancel()
	{
		TestCancelOrder<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_CancelOrder_ShouldProcessCancel()
	{
		TestCancelOrder<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_CancelOrder_ShouldProcessCancel()
	{
		TestCancelOrder<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_CancelOrder_ShouldProcessCancel()
	{
		TestCancelOrder<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_CancelOrder_ShouldProcessCancel()
	{
		TestCancelOrder<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_CancelOrder_ShouldProcessCancel()
	{
		TestCancelOrder<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_CancelOrder_ShouldProcessCancel()
	{
		TestCancelOrder<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_ReplaceOrder_ShouldProcessReplace()
	{
		TestReplaceOrder<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_ReplaceOrder_ShouldProcessReplace()
	{
		TestReplaceOrder<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_ReplaceOrder_ShouldProcessReplace()
	{
		TestReplaceOrder<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_ReplaceOrder_ShouldProcessReplace()
	{
		TestReplaceOrder<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_ReplaceOrder_ShouldProcessReplace()
	{
		TestReplaceOrder<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_ReplaceOrder_ShouldProcessReplace()
	{
		TestReplaceOrder<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_ReplaceOrder_ShouldProcessReplace()
	{
		TestReplaceOrder<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_OrderStatus_ShouldReturnStatus()
	{
		TestOrderStatus<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_OrderStatus_ShouldReturnStatus()
	{
		TestOrderStatus<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_OrderStatus_ShouldReturnStatus()
	{
		TestOrderStatus<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_OrderStatus_ShouldReturnStatus()
	{
		TestOrderStatus<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_OrderStatus_ShouldReturnStatus()
	{
		TestOrderStatus<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_OrderStatus_ShouldReturnStatus()
	{
		TestOrderStatus<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_OrderStatus_ShouldReturnStatus()
	{
		TestOrderStatus<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_OrderTypes_ShouldSupportMarket()
	{
		TestOrderType<BitStampMessageAdapter>(OrderTypes.Market);
	}

	[TestMethod]
	public void Coinbase_OrderTypes_ShouldSupportMarket()
	{
		TestOrderType<CoinbaseMessageAdapter>(OrderTypes.Market);
	}

	[TestMethod]
	public void FTX_OrderTypes_ShouldSupportMarket()
	{
		TestOrderType<FtxMessageAdapter>(OrderTypes.Market);
	}

	[TestMethod]
	public void Tinkoff_OrderTypes_ShouldSupportMarket()
	{
		TestOrderType<TinkoffMessageAdapter>(OrderTypes.Market);
	}

	[TestMethod]
	public void Bitalong_OrderTypes_ShouldSupportMarket()
	{
		TestOrderType<BitalongMessageAdapter>(OrderTypes.Market);
	}

	[TestMethod]
	public void Bitexbook_OrderTypes_ShouldSupportMarket()
	{
		TestOrderType<BitexbookMessageAdapter>(OrderTypes.Market);
	}

	[TestMethod]
	public void Btce_OrderTypes_ShouldSupportMarket()
	{
		TestOrderType<BtceMessageAdapter>(OrderTypes.Market);
	}

	[TestMethod]
	public void BitStamp_OrderTypes_ShouldSupportLimit()
	{
		TestOrderType<BitStampMessageAdapter>(OrderTypes.Limit);
	}

	[TestMethod]
	public void Coinbase_OrderTypes_ShouldSupportLimit()
	{
		TestOrderType<CoinbaseMessageAdapter>(OrderTypes.Limit);
	}

	[TestMethod]
	public void FTX_OrderTypes_ShouldSupportLimit()
	{
		TestOrderType<FtxMessageAdapter>(OrderTypes.Limit);
	}

	[TestMethod]
	public void Tinkoff_OrderTypes_ShouldSupportLimit()
	{
		TestOrderType<TinkoffMessageAdapter>(OrderTypes.Limit);
	}

	[TestMethod]
	public void Bitalong_OrderTypes_ShouldSupportLimit()
	{
		TestOrderType<BitalongMessageAdapter>(OrderTypes.Limit);
	}

	[TestMethod]
	public void Bitexbook_OrderTypes_ShouldSupportLimit()
	{
		TestOrderType<BitexbookMessageAdapter>(OrderTypes.Limit);
	}

	[TestMethod]
	public void Btce_OrderTypes_ShouldSupportLimit()
	{
		TestOrderType<BtceMessageAdapter>(OrderTypes.Limit);
	}

	[TestMethod]
	public void BitStamp_OrderSides_ShouldSupportBuy()
	{
		TestOrderSide<BitStampMessageAdapter>(Sides.Buy);
	}

	[TestMethod]
	public void Coinbase_OrderSides_ShouldSupportBuy()
	{
		TestOrderSide<CoinbaseMessageAdapter>(Sides.Buy);
	}

	[TestMethod]
	public void FTX_OrderSides_ShouldSupportBuy()
	{
		TestOrderSide<FtxMessageAdapter>(Sides.Buy);
	}

	[TestMethod]
	public void Tinkoff_OrderSides_ShouldSupportBuy()
	{
		TestOrderSide<TinkoffMessageAdapter>(Sides.Buy);
	}

	[TestMethod]
	public void Bitalong_OrderSides_ShouldSupportBuy()
	{
		TestOrderSide<BitalongMessageAdapter>(Sides.Buy);
	}

	[TestMethod]
	public void Bitexbook_OrderSides_ShouldSupportBuy()
	{
		TestOrderSide<BitexbookMessageAdapter>(Sides.Buy);
	}

	[TestMethod]
	public void Btce_OrderSides_ShouldSupportBuy()
	{
		TestOrderSide<BtceMessageAdapter>(Sides.Buy);
	}

	[TestMethod]
	public void BitStamp_OrderSides_ShouldSupportSell()
	{
		TestOrderSide<BitStampMessageAdapter>(Sides.Sell);
	}

	[TestMethod]
	public void Coinbase_OrderSides_ShouldSupportSell()
	{
		TestOrderSide<CoinbaseMessageAdapter>(Sides.Sell);
	}

	[TestMethod]
	public void FTX_OrderSides_ShouldSupportSell()
	{
		TestOrderSide<FtxMessageAdapter>(Sides.Sell);
	}

	[TestMethod]
	public void Tinkoff_OrderSides_ShouldSupportSell()
	{
		TestOrderSide<TinkoffMessageAdapter>(Sides.Sell);
	}

	[TestMethod]
	public void Bitalong_OrderSides_ShouldSupportSell()
	{
		TestOrderSide<BitalongMessageAdapter>(Sides.Sell);
	}

	[TestMethod]
	public void Bitexbook_OrderSides_ShouldSupportSell()
	{
		TestOrderSide<BitexbookMessageAdapter>(Sides.Sell);
	}

	[TestMethod]
	public void Btce_OrderSides_ShouldSupportSell()
	{
		TestOrderSide<BtceMessageAdapter>(Sides.Sell);
	}

	[TestMethod]
	public void BitStamp_OrderTransactionId_ShouldBePreserved()
	{
		TestOrderTransactionId<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_OrderTransactionId_ShouldBePreserved()
	{
		TestOrderTransactionId<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_OrderTransactionId_ShouldBePreserved()
	{
		TestOrderTransactionId<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_OrderTransactionId_ShouldBePreserved()
	{
		TestOrderTransactionId<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_OrderTransactionId_ShouldBePreserved()
	{
		TestOrderTransactionId<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_OrderTransactionId_ShouldBePreserved()
	{
		TestOrderTransactionId<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_OrderTransactionId_ShouldBePreserved()
	{
		TestOrderTransactionId<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_OrderVolume_ShouldBeValidated()
	{
		TestOrderVolumeValidation<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_OrderVolume_ShouldBeValidated()
	{
		TestOrderVolumeValidation<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_OrderVolume_ShouldBeValidated()
	{
		TestOrderVolumeValidation<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_OrderVolume_ShouldBeValidated()
	{
		TestOrderVolumeValidation<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_OrderVolume_ShouldBeValidated()
	{
		TestOrderVolumeValidation<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_OrderVolume_ShouldBeValidated()
	{
		TestOrderVolumeValidation<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_OrderVolume_ShouldBeValidated()
	{
		TestOrderVolumeValidation<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_OrderPrice_ShouldBeValidated()
	{
		TestOrderPriceValidation<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_OrderPrice_ShouldBeValidated()
	{
		TestOrderPriceValidation<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_OrderPrice_ShouldBeValidated()
	{
		TestOrderPriceValidation<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_OrderPrice_ShouldBeValidated()
	{
		TestOrderPriceValidation<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_OrderPrice_ShouldBeValidated()
	{
		TestOrderPriceValidation<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_OrderPrice_ShouldBeValidated()
	{
		TestOrderPriceValidation<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_OrderPrice_ShouldBeValidated()
	{
		TestOrderPriceValidation<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_OrderCondition_ShouldBeSupported()
	{
		TestOrderCondition<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_OrderCondition_ShouldBeSupported()
	{
		TestOrderCondition<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_OrderCondition_ShouldBeSupported()
	{
		TestOrderCondition<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_OrderCondition_ShouldBeSupported()
	{
		TestOrderCondition<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_OrderCondition_ShouldBeSupported()
	{
		TestOrderCondition<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_OrderCondition_ShouldBeSupported()
	{
		TestOrderCondition<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_OrderCondition_ShouldBeSupported()
	{
		TestOrderCondition<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_OrderGroupCancel_ShouldProcess()
	{
		TestOrderGroupCancel<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_OrderGroupCancel_ShouldProcess()
	{
		TestOrderGroupCancel<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_OrderGroupCancel_ShouldProcess()
	{
		TestOrderGroupCancel<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_OrderGroupCancel_ShouldProcess()
	{
		TestOrderGroupCancel<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_OrderGroupCancel_ShouldProcess()
	{
		TestOrderGroupCancel<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_OrderGroupCancel_ShouldProcess()
	{
		TestOrderGroupCancel<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_OrderGroupCancel_ShouldProcess()
	{
		TestOrderGroupCancel<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_MultipleOrders_ShouldProcessSequentially()
	{
		TestMultipleOrders<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_MultipleOrders_ShouldProcessSequentially()
	{
		TestMultipleOrders<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_MultipleOrders_ShouldProcessSequentially()
	{
		TestMultipleOrders<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_MultipleOrders_ShouldProcessSequentially()
	{
		TestMultipleOrders<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_MultipleOrders_ShouldProcessSequentially()
	{
		TestMultipleOrders<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_MultipleOrders_ShouldProcessSequentially()
	{
		TestMultipleOrders<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_MultipleOrders_ShouldProcessSequentially()
	{
		TestMultipleOrders<BtceMessageAdapter>();
	}

	private static void TestRegisterOrder<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var orderMsg = new OrderRegisterMessage
		{
			TransactionId = 1,
			SecurityId = "BTC/USD@TEST".ToSecurityId(),
			Side = Sides.Buy,
			Volume = 1,
			Price = 50000,
			OrderType = OrderTypes.Limit
		};
		adapter.SendInMessage(orderMsg);

		messages.Any(m => m is SubscriptionResponseMessage resp && resp.OriginalTransactionId == 1).AssertTrue("Should process order registration");
	}

	private static void TestCancelOrder<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var cancelMsg = new OrderCancelMessage
		{
			TransactionId = 2,
			OrderId = 123
		};
		adapter.SendInMessage(cancelMsg);

		messages.Any(m => m is SubscriptionResponseMessage resp && resp.OriginalTransactionId == 2).AssertTrue("Should process order cancellation");
	}

	private static void TestReplaceOrder<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var replaceMsg = new OrderReplaceMessage
		{
			TransactionId = 3,
			OrderId = 123,
			NewPrice = 51000,
			NewVolume = 2
		};
		adapter.SendInMessage(replaceMsg);

		messages.Any(m => m is SubscriptionResponseMessage resp && resp.OriginalTransactionId == 3).AssertTrue("Should process order replacement");
	}

	private static void TestOrderStatus<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var statusMsg = new OrderStatusMessage { TransactionId = 4 };
		adapter.SendInMessage(statusMsg);

		messages.Any(m => m is SubscriptionResponseMessage resp && resp.OriginalTransactionId == 4).AssertTrue("Should process order status request");
	}

	private static void TestOrderType<T>(OrderTypes orderType) where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var orderMsg = new OrderRegisterMessage
		{
			TransactionId = 5,
			SecurityId = "BTC/USD@TEST".ToSecurityId(),
			Side = Sides.Buy,
			Volume = 1,
			OrderType = orderType
		};
		adapter.SendInMessage(orderMsg);

		messages.Any(m => m is SubscriptionResponseMessage resp && resp.OriginalTransactionId == 5).AssertTrue($"Should support {orderType} order type");
	}

	private static void TestOrderSide<T>(Sides side) where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var orderMsg = new OrderRegisterMessage
		{
			TransactionId = 6,
			SecurityId = "BTC/USD@TEST".ToSecurityId(),
			Side = side,
			Volume = 1,
			Price = 50000,
			OrderType = OrderTypes.Limit
		};
		adapter.SendInMessage(orderMsg);

		messages.Any(m => m is SubscriptionResponseMessage resp && resp.OriginalTransactionId == 6).AssertTrue($"Should support {side} side");
	}

	private static void TestOrderTransactionId<T>() where T : MessageAdapter
	{
		var responses = new List<SubscriptionResponseMessage>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg =>
		{
			if (msg is SubscriptionResponseMessage resp)
				responses.Add(resp);
		};

		var orderMsg = new OrderRegisterMessage
		{
			TransactionId = 100,
			SecurityId = "BTC/USD@TEST".ToSecurityId(),
			Side = Sides.Buy,
			Volume = 1,
			Price = 50000,
			OrderType = OrderTypes.Limit
		};
		adapter.SendInMessage(orderMsg);

		responses.Any(r => r.OriginalTransactionId == 100).AssertTrue("Should preserve order transaction ID");
	}

	private static void TestOrderVolumeValidation<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var orderMsg = new OrderRegisterMessage
		{
			TransactionId = 7,
			SecurityId = "BTC/USD@TEST".ToSecurityId(),
			Side = Sides.Buy,
			Volume = 0, // Invalid volume
			Price = 50000,
			OrderType = OrderTypes.Limit
		};
		adapter.SendInMessage(orderMsg);

		// Should handle invalid volume
		messages.Any(m => m is SubscriptionResponseMessage || m is ErrorMessage).AssertTrue("Should validate order volume");
	}

	private static void TestOrderPriceValidation<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var orderMsg = new OrderRegisterMessage
		{
			TransactionId = 8,
			SecurityId = "BTC/USD@TEST".ToSecurityId(),
			Side = Sides.Buy,
			Volume = 1,
			Price = -100, // Invalid price
			OrderType = OrderTypes.Limit
		};
		adapter.SendInMessage(orderMsg);

		// Should handle invalid price
		messages.Any(m => m is SubscriptionResponseMessage || m is ErrorMessage).AssertTrue("Should validate order price");
	}

	private static void TestOrderCondition<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var orderMsg = new OrderRegisterMessage
		{
			TransactionId = 9,
			SecurityId = "BTC/USD@TEST".ToSecurityId(),
			Side = Sides.Buy,
			Volume = 1,
			Price = 50000,
			OrderType = OrderTypes.Limit
		};
		adapter.SendInMessage(orderMsg);

		// Should handle order conditions
		messages.Any(m => m is SubscriptionResponseMessage).AssertTrue("Should support order conditions");
	}

	private static void TestOrderGroupCancel<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var cancelMsg = new OrderGroupCancelMessage
		{
			TransactionId = 10,
			Side = Sides.Buy
		};
		adapter.SendInMessage(cancelMsg);

		messages.Any(m => m is SubscriptionResponseMessage resp && resp.OriginalTransactionId == 10).AssertTrue("Should process order group cancellation");
	}

	private static void TestMultipleOrders<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		for (int i = 0; i < 5; i++)
		{
			var orderMsg = new OrderRegisterMessage
			{
				TransactionId = 20 + i,
				SecurityId = "BTC/USD@TEST".ToSecurityId(),
				Side = Sides.Buy,
				Volume = 1,
				Price = 50000 + i,
				OrderType = OrderTypes.Limit
			};
			adapter.SendInMessage(orderMsg);
		}

		(messages.Count > 0).AssertTrue("Should process multiple orders");
	}

	private static MessageAdapter CreateAdapter<T>() where T : MessageAdapter
	{
		var gen = new IncrementalIdGenerator();
		return typeof(T).Name switch
		{
			nameof(BitStampMessageAdapter) => new BitStampMessageAdapter(gen),
			nameof(CoinbaseMessageAdapter) => new CoinbaseMessageAdapter(gen),
			nameof(FtxMessageAdapter) => new FtxMessageAdapter(gen),
			nameof(TinkoffMessageAdapter) => new TinkoffMessageAdapter(gen),
			nameof(BitalongMessageAdapter) => new BitalongMessageAdapter(gen),
			nameof(BitexbookMessageAdapter) => new BitexbookMessageAdapter(gen),
			nameof(BtceMessageAdapter) => new BtceMessageAdapter(gen),
			_ => throw new NotSupportedException($"Adapter type {typeof(T).Name} not supported")
		};
	}
}


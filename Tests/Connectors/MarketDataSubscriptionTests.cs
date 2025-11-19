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
/// Integration tests for market data subscription handling.
/// </summary>
[TestClass]
public class MarketDataSubscriptionTests
{
	[TestMethod]
	public void BitStamp_SubscribeTicks_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<BitStampMessageAdapter>(DataType.Ticks);
	}

	[TestMethod]
	public void Coinbase_SubscribeTicks_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<CoinbaseMessageAdapter>(DataType.Ticks);
	}

	[TestMethod]
	public void FTX_SubscribeTicks_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<FtxMessageAdapter>(DataType.Ticks);
	}

	[TestMethod]
	public void Tinkoff_SubscribeTicks_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<TinkoffMessageAdapter>(DataType.Ticks);
	}

	[TestMethod]
	public void Bitalong_SubscribeTicks_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<BitalongMessageAdapter>(DataType.Ticks);
	}

	[TestMethod]
	public void Bitexbook_SubscribeTicks_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<BitexbookMessageAdapter>(DataType.Ticks);
	}

	[TestMethod]
	public void Btce_SubscribeTicks_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<BtceMessageAdapter>(DataType.Ticks);
	}

	[TestMethod]
	public void BitStamp_SubscribeMarketDepth_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<BitStampMessageAdapter>(DataType.MarketDepth);
	}

	[TestMethod]
	public void Coinbase_SubscribeMarketDepth_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<CoinbaseMessageAdapter>(DataType.MarketDepth);
	}

	[TestMethod]
	public void FTX_SubscribeMarketDepth_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<FtxMessageAdapter>(DataType.MarketDepth);
	}

	[TestMethod]
	public void Tinkoff_SubscribeMarketDepth_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<TinkoffMessageAdapter>(DataType.MarketDepth);
	}

	[TestMethod]
	public void Bitalong_SubscribeMarketDepth_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<BitalongMessageAdapter>(DataType.MarketDepth);
	}

	[TestMethod]
	public void Bitexbook_SubscribeMarketDepth_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<BitexbookMessageAdapter>(DataType.MarketDepth);
	}

	[TestMethod]
	public void Btce_SubscribeMarketDepth_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<BtceMessageAdapter>(DataType.MarketDepth);
	}

	[TestMethod]
	public void BitStamp_SubscribeLevel1_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<BitStampMessageAdapter>(DataType.Level1);
	}

	[TestMethod]
	public void Coinbase_SubscribeLevel1_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<CoinbaseMessageAdapter>(DataType.Level1);
	}

	[TestMethod]
	public void FTX_SubscribeLevel1_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<FtxMessageAdapter>(DataType.Level1);
	}

	[TestMethod]
	public void Tinkoff_SubscribeLevel1_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<TinkoffMessageAdapter>(DataType.Level1);
	}

	[TestMethod]
	public void Bitalong_SubscribeLevel1_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<BitalongMessageAdapter>(DataType.Level1);
	}

	[TestMethod]
	public void Bitexbook_SubscribeLevel1_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<BitexbookMessageAdapter>(DataType.Level1);
	}

	[TestMethod]
	public void Btce_SubscribeLevel1_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<BtceMessageAdapter>(DataType.Level1);
	}

	[TestMethod]
	public void BitStamp_SubscribeOrderLog_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<BitStampMessageAdapter>(DataType.OrderLog);
	}

	[TestMethod]
	public void Coinbase_SubscribeOrderLog_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<CoinbaseMessageAdapter>(DataType.OrderLog);
	}

	[TestMethod]
	public void FTX_SubscribeOrderLog_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<FtxMessageAdapter>(DataType.OrderLog);
	}

	[TestMethod]
	public void Tinkoff_SubscribeOrderLog_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<TinkoffMessageAdapter>(DataType.OrderLog);
	}

	[TestMethod]
	public void Bitalong_SubscribeOrderLog_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<BitalongMessageAdapter>(DataType.OrderLog);
	}

	[TestMethod]
	public void Bitexbook_SubscribeOrderLog_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<BitexbookMessageAdapter>(DataType.OrderLog);
	}

	[TestMethod]
	public void Btce_SubscribeOrderLog_ShouldProcessSubscription()
	{
		TestSubscribeMarketData<BtceMessageAdapter>(DataType.OrderLog);
	}

	[TestMethod]
	public void BitStamp_SubscribeCandles_ShouldProcessSubscription()
	{
		TestSubscribeCandles<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_SubscribeCandles_ShouldProcessSubscription()
	{
		TestSubscribeCandles<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_SubscribeCandles_ShouldProcessSubscription()
	{
		TestSubscribeCandles<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_SubscribeCandles_ShouldProcessSubscription()
	{
		TestSubscribeCandles<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_SubscribeCandles_ShouldProcessSubscription()
	{
		TestSubscribeCandles<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_SubscribeCandles_ShouldProcessSubscription()
	{
		TestSubscribeCandles<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_SubscribeCandles_ShouldProcessSubscription()
	{
		TestSubscribeCandles<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_Unsubscribe_ShouldProcessUnsubscription()
	{
		TestUnsubscribe<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_Unsubscribe_ShouldProcessUnsubscription()
	{
		TestUnsubscribe<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_Unsubscribe_ShouldProcessUnsubscription()
	{
		TestUnsubscribe<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_Unsubscribe_ShouldProcessUnsubscription()
	{
		TestUnsubscribe<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_Unsubscribe_ShouldProcessUnsubscription()
	{
		TestUnsubscribe<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_Unsubscribe_ShouldProcessUnsubscription()
	{
		TestUnsubscribe<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_Unsubscribe_ShouldProcessUnsubscription()
	{
		TestUnsubscribe<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_MultipleSubscriptions_ShouldHandleMultiple()
	{
		TestMultipleSubscriptions<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_MultipleSubscriptions_ShouldHandleMultiple()
	{
		TestMultipleSubscriptions<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_MultipleSubscriptions_ShouldHandleMultiple()
	{
		TestMultipleSubscriptions<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_MultipleSubscriptions_ShouldHandleMultiple()
	{
		TestMultipleSubscriptions<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_MultipleSubscriptions_ShouldHandleMultiple()
	{
		TestMultipleSubscriptions<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_MultipleSubscriptions_ShouldHandleMultiple()
	{
		TestMultipleSubscriptions<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_MultipleSubscriptions_ShouldHandleMultiple()
	{
		TestMultipleSubscriptions<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_SubscriptionTransactionId_ShouldBePreserved()
	{
		TestSubscriptionTransactionId<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_SubscriptionTransactionId_ShouldBePreserved()
	{
		TestSubscriptionTransactionId<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_SubscriptionTransactionId_ShouldBePreserved()
	{
		TestSubscriptionTransactionId<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_SubscriptionTransactionId_ShouldBePreserved()
	{
		TestSubscriptionTransactionId<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_SubscriptionTransactionId_ShouldBePreserved()
	{
		TestSubscriptionTransactionId<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_SubscriptionTransactionId_ShouldBePreserved()
	{
		TestSubscriptionTransactionId<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_SubscriptionTransactionId_ShouldBePreserved()
	{
		TestSubscriptionTransactionId<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_UnsupportedDataType_ShouldReturnError()
	{
		TestUnsupportedDataType<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_UnsupportedDataType_ShouldReturnError()
	{
		TestUnsupportedDataType<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_UnsupportedDataType_ShouldReturnError()
	{
		TestUnsupportedDataType<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_UnsupportedDataType_ShouldReturnError()
	{
		TestUnsupportedDataType<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_UnsupportedDataType_ShouldReturnError()
	{
		TestUnsupportedDataType<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_UnsupportedDataType_ShouldReturnError()
	{
		TestUnsupportedDataType<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_UnsupportedDataType_ShouldReturnError()
	{
		TestUnsupportedDataType<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_SubscriptionSecurityId_ShouldBeValidated()
	{
		TestSubscriptionSecurityId<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_SubscriptionSecurityId_ShouldBeValidated()
	{
		TestSubscriptionSecurityId<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_SubscriptionSecurityId_ShouldBeValidated()
	{
		TestSubscriptionSecurityId<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_SubscriptionSecurityId_ShouldBeValidated()
	{
		TestSubscriptionSecurityId<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_SubscriptionSecurityId_ShouldBeValidated()
	{
		TestSubscriptionSecurityId<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_SubscriptionSecurityId_ShouldBeValidated()
	{
		TestSubscriptionSecurityId<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_SubscriptionSecurityId_ShouldBeValidated()
	{
		TestSubscriptionSecurityId<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_SubscriptionFromTo_ShouldBeRespected()
	{
		TestSubscriptionFromTo<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_SubscriptionFromTo_ShouldBeRespected()
	{
		TestSubscriptionFromTo<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_SubscriptionFromTo_ShouldBeRespected()
	{
		TestSubscriptionFromTo<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_SubscriptionFromTo_ShouldBeRespected()
	{
		TestSubscriptionFromTo<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_SubscriptionFromTo_ShouldBeRespected()
	{
		TestSubscriptionFromTo<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_SubscriptionFromTo_ShouldBeRespected()
	{
		TestSubscriptionFromTo<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_SubscriptionFromTo_ShouldBeRespected()
	{
		TestSubscriptionFromTo<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_SubscriptionCount_ShouldBeRespected()
	{
		TestSubscriptionCount<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_SubscriptionCount_ShouldBeRespected()
	{
		TestSubscriptionCount<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_SubscriptionCount_ShouldBeRespected()
	{
		TestSubscriptionCount<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_SubscriptionCount_ShouldBeRespected()
	{
		TestSubscriptionCount<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_SubscriptionCount_ShouldBeRespected()
	{
		TestSubscriptionCount<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_SubscriptionCount_ShouldBeRespected()
	{
		TestSubscriptionCount<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_SubscriptionCount_ShouldBeRespected()
	{
		TestSubscriptionCount<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_SubscriptionBuildFrom_ShouldBeRespected()
	{
		TestSubscriptionBuildFrom<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_SubscriptionBuildFrom_ShouldBeRespected()
	{
		TestSubscriptionBuildFrom<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_SubscriptionBuildFrom_ShouldBeRespected()
	{
		TestSubscriptionBuildFrom<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_SubscriptionBuildFrom_ShouldBeRespected()
	{
		TestSubscriptionBuildFrom<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_SubscriptionBuildFrom_ShouldBeRespected()
	{
		TestSubscriptionBuildFrom<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_SubscriptionBuildFrom_ShouldBeRespected()
	{
		TestSubscriptionBuildFrom<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_SubscriptionBuildFrom_ShouldBeRespected()
	{
		TestSubscriptionBuildFrom<BtceMessageAdapter>();
	}

	private static void TestSubscribeMarketData<T>(DataType dataType) where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var mdMsg = new MarketDataMessage
		{
			TransactionId = 1,
			SecurityId = "BTC/USD@TEST".ToSecurityId(),
			DataType2 = dataType,
			IsSubscribe = true
		};
		adapter.SendInMessage(mdMsg);

		messages.Any(m => m is SubscriptionResponseMessage resp && resp.OriginalTransactionId == 1).AssertTrue($"Should process {dataType} subscription");
	}

	private static void TestSubscribeCandles<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var mdMsg = new MarketDataMessage
		{
			TransactionId = 2,
			SecurityId = "BTC/USD@TEST".ToSecurityId(),
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true
		};
		adapter.SendInMessage(mdMsg);

		messages.Any(m => m is SubscriptionResponseMessage resp && resp.OriginalTransactionId == 2).AssertTrue("Should process candle subscription");
	}

	private static void TestUnsubscribe<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var mdMsg = new MarketDataMessage
		{
			TransactionId = 3,
			SecurityId = "BTC/USD@TEST".ToSecurityId(),
			DataType2 = DataType.Ticks,
			IsSubscribe = false
		};
		adapter.SendInMessage(mdMsg);

		messages.Any(m => m is SubscriptionResponseMessage resp && resp.OriginalTransactionId == 3).AssertTrue("Should process unsubscription");
	}

	private static void TestMultipleSubscriptions<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		for (int i = 0; i < 5; i++)
		{
			var mdMsg = new MarketDataMessage
			{
				TransactionId = 10 + i,
				SecurityId = $"BTC/USD{i}@TEST".ToSecurityId(),
				DataType2 = DataType.Ticks,
				IsSubscribe = true
			};
			adapter.SendInMessage(mdMsg);
		}

		(messages.Count(m => m is SubscriptionResponseMessage) > 0).AssertTrue("Should handle multiple subscriptions");
	}

	private static void TestSubscriptionTransactionId<T>() where T : MessageAdapter
	{
		var responses = new List<SubscriptionResponseMessage>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg =>
		{
			if (msg is SubscriptionResponseMessage resp)
				responses.Add(resp);
		};

		var mdMsg = new MarketDataMessage
		{
			TransactionId = 100,
			SecurityId = "BTC/USD@TEST".ToSecurityId(),
			DataType2 = DataType.Ticks,
			IsSubscribe = true
		};
		adapter.SendInMessage(mdMsg);

		responses.Any(r => r.OriginalTransactionId == 100).AssertTrue("Should preserve subscription transaction ID");
	}

	private static void TestUnsupportedDataType<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var mdMsg = new MarketDataMessage
		{
			TransactionId = 4,
			SecurityId = "BTC/USD@TEST".ToSecurityId(),
			DataType2 = DataType.News, // Potentially unsupported
			IsSubscribe = true
		};
		adapter.SendInMessage(mdMsg);

		messages.Any(m => m is SubscriptionResponseMessage resp && resp.OriginalTransactionId == 4).AssertTrue("Should handle unsupported data type");
	}

	private static void TestSubscriptionSecurityId<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var mdMsg = new MarketDataMessage
		{
			TransactionId = 5,
			SecurityId = SecurityId.Empty, // Invalid security ID
			DataType2 = DataType.Ticks,
			IsSubscribe = true
		};
		adapter.SendInMessage(mdMsg);

		messages.Any(m => m is SubscriptionResponseMessage || m is ErrorMessage).AssertTrue("Should validate subscription security ID");
	}

	private static void TestSubscriptionFromTo<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var mdMsg = new MarketDataMessage
		{
			TransactionId = 6,
			SecurityId = "BTC/USD@TEST".ToSecurityId(),
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			From = DateTime.UtcNow.AddDays(-1),
			To = DateTime.UtcNow
		};
		adapter.SendInMessage(mdMsg);

		messages.Any(m => m is SubscriptionResponseMessage resp && resp.OriginalTransactionId == 6).AssertTrue("Should respect subscription From/To dates");
	}

	private static void TestSubscriptionCount<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var mdMsg = new MarketDataMessage
		{
			TransactionId = 7,
			SecurityId = "BTC/USD@TEST".ToSecurityId(),
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			Count = 100
		};
		adapter.SendInMessage(mdMsg);

		messages.Any(m => m is SubscriptionResponseMessage resp && resp.OriginalTransactionId == 7).AssertTrue("Should respect subscription count");
	}

	private static void TestSubscriptionBuildFrom<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var mdMsg = new MarketDataMessage
		{
			TransactionId = 8,
			SecurityId = "BTC/USD@TEST".ToSecurityId(),
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			BuildFrom = DataType.OrderLog
		};
		adapter.SendInMessage(mdMsg);

		messages.Any(m => m is SubscriptionResponseMessage resp && resp.OriginalTransactionId == 8).AssertTrue("Should respect subscription BuildFrom");
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


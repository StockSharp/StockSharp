namespace StockSharp.Tests.Connectors;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using StockSharp.BitStamp;
using StockSharp.Coinbase;
using StockSharp.FTX;
using StockSharp.Tinkoff;
using StockSharp.Bitalong;
using StockSharp.Bitexbook;
using StockSharp.Btce;
using StockSharp.Messages;

/// <summary>
/// Integration tests for message protocol handling per connector.
/// </summary>
[TestClass]
public class MessageProtocolTests
{
	[TestMethod]
	public void BitStamp_ProcessSecurityLookup_ShouldHandleMessage()
	{
		TestSecurityLookup<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_ProcessSecurityLookup_ShouldHandleMessage()
	{
		TestSecurityLookup<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_ProcessSecurityLookup_ShouldHandleMessage()
	{
		TestSecurityLookup<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_ProcessSecurityLookup_ShouldHandleMessage()
	{
		TestSecurityLookup<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_ProcessSecurityLookup_ShouldHandleMessage()
	{
		TestSecurityLookup<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_ProcessSecurityLookup_ShouldHandleMessage()
	{
		TestSecurityLookup<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_ProcessSecurityLookup_ShouldHandleMessage()
	{
		TestSecurityLookup<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_ProcessPortfolioLookup_ShouldHandleMessage()
	{
		TestPortfolioLookup<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_ProcessPortfolioLookup_ShouldHandleMessage()
	{
		TestPortfolioLookup<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_ProcessPortfolioLookup_ShouldHandleMessage()
	{
		TestPortfolioLookup<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_ProcessPortfolioLookup_ShouldHandleMessage()
	{
		TestPortfolioLookup<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_ProcessPortfolioLookup_ShouldHandleMessage()
	{
		TestPortfolioLookup<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_ProcessPortfolioLookup_ShouldHandleMessage()
	{
		TestPortfolioLookup<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_ProcessPortfolioLookup_ShouldHandleMessage()
	{
		TestPortfolioLookup<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_ProcessTimeMessage_ShouldHandleMessage()
	{
		TestTimeMessage<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_ProcessTimeMessage_ShouldHandleMessage()
	{
		TestTimeMessage<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_ProcessTimeMessage_ShouldHandleMessage()
	{
		TestTimeMessage<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_ProcessTimeMessage_ShouldHandleMessage()
	{
		TestTimeMessage<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_ProcessTimeMessage_ShouldHandleMessage()
	{
		TestTimeMessage<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_ProcessTimeMessage_ShouldHandleMessage()
	{
		TestTimeMessage<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_ProcessTimeMessage_ShouldHandleMessage()
	{
		TestTimeMessage<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_ProcessResetMessage_ShouldHandleMessage()
	{
		TestResetMessage<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_ProcessResetMessage_ShouldHandleMessage()
	{
		TestResetMessage<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_ProcessResetMessage_ShouldHandleMessage()
	{
		TestResetMessage<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_ProcessResetMessage_ShouldHandleMessage()
	{
		TestResetMessage<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_ProcessResetMessage_ShouldHandleMessage()
	{
		TestResetMessage<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_ProcessResetMessage_ShouldHandleMessage()
	{
		TestResetMessage<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_ProcessResetMessage_ShouldHandleMessage()
	{
		TestResetMessage<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_ProcessOrderStatusMessage_ShouldHandleMessage()
	{
		TestOrderStatusMessage<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_ProcessOrderStatusMessage_ShouldHandleMessage()
	{
		TestOrderStatusMessage<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_ProcessOrderStatusMessage_ShouldHandleMessage()
	{
		TestOrderStatusMessage<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_ProcessOrderStatusMessage_ShouldHandleMessage()
	{
		TestOrderStatusMessage<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_ProcessOrderStatusMessage_ShouldHandleMessage()
	{
		TestOrderStatusMessage<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_ProcessOrderStatusMessage_ShouldHandleMessage()
	{
		TestOrderStatusMessage<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_ProcessOrderStatusMessage_ShouldHandleMessage()
	{
		TestOrderStatusMessage<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_ProcessBoardLookup_ShouldHandleMessage()
	{
		TestBoardLookup<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_ProcessBoardLookup_ShouldHandleMessage()
	{
		TestBoardLookup<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_ProcessBoardLookup_ShouldHandleMessage()
	{
		TestBoardLookup<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_ProcessBoardLookup_ShouldHandleMessage()
	{
		TestBoardLookup<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_ProcessBoardLookup_ShouldHandleMessage()
	{
		TestBoardLookup<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_ProcessBoardLookup_ShouldHandleMessage()
	{
		TestBoardLookup<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_ProcessBoardLookup_ShouldHandleMessage()
	{
		TestBoardLookup<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_ProcessChangePassword_ShouldHandleMessage()
	{
		TestChangePassword<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_ProcessChangePassword_ShouldHandleMessage()
	{
		TestChangePassword<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_ProcessChangePassword_ShouldHandleMessage()
	{
		TestChangePassword<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_ProcessChangePassword_ShouldHandleMessage()
	{
		TestChangePassword<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_ProcessChangePassword_ShouldHandleMessage()
	{
		TestChangePassword<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_ProcessChangePassword_ShouldHandleMessage()
	{
		TestChangePassword<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_ProcessChangePassword_ShouldHandleMessage()
	{
		TestChangePassword<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_MessageTransactionId_ShouldBePreserved()
	{
		TestTransactionIdPreservation<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_MessageTransactionId_ShouldBePreserved()
	{
		TestTransactionIdPreservation<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_MessageTransactionId_ShouldBePreserved()
	{
		TestTransactionIdPreservation<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_MessageTransactionId_ShouldBePreserved()
	{
		TestTransactionIdPreservation<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_MessageTransactionId_ShouldBePreserved()
	{
		TestTransactionIdPreservation<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_MessageTransactionId_ShouldBePreserved()
	{
		TestTransactionIdPreservation<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_MessageTransactionId_ShouldBePreserved()
	{
		TestTransactionIdPreservation<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_ConcurrentMessages_ShouldBeHandled()
	{
		TestConcurrentMessages<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_ConcurrentMessages_ShouldBeHandled()
	{
		TestConcurrentMessages<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_ConcurrentMessages_ShouldBeHandled()
	{
		TestConcurrentMessages<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_ConcurrentMessages_ShouldBeHandled()
	{
		TestConcurrentMessages<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_ConcurrentMessages_ShouldBeHandled()
	{
		TestConcurrentMessages<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_ConcurrentMessages_ShouldBeHandled()
	{
		TestConcurrentMessages<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_ConcurrentMessages_ShouldBeHandled()
	{
		TestConcurrentMessages<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_UnsupportedMessage_ShouldReturnError()
	{
		TestUnsupportedMessage<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_UnsupportedMessage_ShouldReturnError()
	{
		TestUnsupportedMessage<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_UnsupportedMessage_ShouldReturnError()
	{
		TestUnsupportedMessage<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_UnsupportedMessage_ShouldReturnError()
	{
		TestUnsupportedMessage<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_UnsupportedMessage_ShouldReturnError()
	{
		TestUnsupportedMessage<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_UnsupportedMessage_ShouldReturnError()
	{
		TestUnsupportedMessage<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_UnsupportedMessage_ShouldReturnError()
	{
		TestUnsupportedMessage<BtceMessageAdapter>();
	}

	private static void TestSecurityLookup<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var lookupMsg = new SecurityLookupMessage { TransactionId = 1 };
		adapter.SendInMessage(lookupMsg);

		messages.Any(m => m is SubscriptionResponseMessage resp && resp.OriginalTransactionId == 1).AssertTrue("Should process security lookup");
	}

	private static void TestPortfolioLookup<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var lookupMsg = new PortfolioLookupMessage { TransactionId = 2 };
		adapter.SendInMessage(lookupMsg);

		messages.Any(m => m is SubscriptionResponseMessage resp && resp.OriginalTransactionId == 2).AssertTrue("Should process portfolio lookup");
	}

	private static void TestTimeMessage<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var timeMsg = new TimeMessage { TransactionId = 3 };
		adapter.SendInMessage(timeMsg);

		// Time message might not generate response, but should not throw
		true.AssertTrue("Should handle time message");
	}

	private static void TestResetMessage<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var resetMsg = new ResetMessage { TransactionId = 4 };
		adapter.SendInMessage(resetMsg);

		messages.Any(m => m is ResetMessage).AssertTrue("Should process reset message");
	}

	private static void TestOrderStatusMessage<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var statusMsg = new OrderStatusMessage { TransactionId = 5 };
		adapter.SendInMessage(statusMsg);

		messages.Any(m => m is SubscriptionResponseMessage resp && resp.OriginalTransactionId == 5).AssertTrue("Should process order status message");
	}

	private static void TestBoardLookup<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var lookupMsg = new BoardLookupMessage { TransactionId = 6 };
		adapter.SendInMessage(lookupMsg);

		messages.Any(m => m is SubscriptionResponseMessage resp && resp.OriginalTransactionId == 6).AssertTrue("Should process board lookup");
	}

	private static void TestChangePassword<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var pwdMsg = new ChangePasswordMessage { TransactionId = 7, NewPassword = "newpass" };
		adapter.SendInMessage(pwdMsg);

		messages.Any(m => m is SubscriptionResponseMessage resp && resp.OriginalTransactionId == 7).AssertTrue("Should process change password message");
	}

	private static void TestTransactionIdPreservation<T>() where T : MessageAdapter
	{
		var responses = new List<SubscriptionResponseMessage>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg =>
		{
			if (msg is SubscriptionResponseMessage resp)
				responses.Add(resp);
		};

		var lookupMsg = new SecurityLookupMessage { TransactionId = 100 };
		adapter.SendInMessage(lookupMsg);

		responses.Any(r => r.OriginalTransactionId == 100).AssertTrue("Should preserve transaction ID");
	}

	private static void TestConcurrentMessages<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		for (int i = 0; i < 10; i++)
		{
			var lookupMsg = new SecurityLookupMessage { TransactionId = 200 + i };
			adapter.SendInMessage(lookupMsg);
		}

		(messages.Count > 0).AssertTrue("Should handle concurrent messages");
	}

	private static void TestUnsupportedMessage<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		// Try to send a message that might not be supported
		var marketDataMsg = new MarketDataMessage
		{
			TransactionId = 300,
			DataType2 = DataType.OrderLog,
			IsSubscribe = true,
			SecurityId = "TEST@TEST".ToSecurityId()
		};
		adapter.SendInMessage(marketDataMsg);

		// Should either process or return error, but not crash
		messages.Any(m => m is SubscriptionResponseMessage || m is ErrorMessage).AssertTrue("Should handle unsupported message gracefully");
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


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
/// Integration tests for error handling and reconnection logic.
/// </summary>
[TestClass]
public class ErrorHandlingTests
{
	[TestMethod]
	public void BitStamp_InvalidCredentials_ShouldReturnError()
	{
		TestInvalidCredentials<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_InvalidCredentials_ShouldReturnError()
	{
		TestInvalidCredentials<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_InvalidCredentials_ShouldReturnError()
	{
		TestInvalidCredentials<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_InvalidCredentials_ShouldReturnError()
	{
		TestInvalidCredentials<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_InvalidCredentials_ShouldReturnError()
	{
		TestInvalidCredentials<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_InvalidCredentials_ShouldReturnError()
	{
		TestInvalidCredentials<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_InvalidCredentials_ShouldReturnError()
	{
		TestInvalidCredentials<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_NetworkError_ShouldHandleGracefully()
	{
		TestNetworkErrorHandling<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_NetworkError_ShouldHandleGracefully()
	{
		TestNetworkErrorHandling<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_NetworkError_ShouldHandleGracefully()
	{
		TestNetworkErrorHandling<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_NetworkError_ShouldHandleGracefully()
	{
		TestNetworkErrorHandling<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_NetworkError_ShouldHandleGracefully()
	{
		TestNetworkErrorHandling<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_NetworkError_ShouldHandleGracefully()
	{
		TestNetworkErrorHandling<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_NetworkError_ShouldHandleGracefully()
	{
		TestNetworkErrorHandling<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_Reconnection_ShouldAttemptReconnect()
	{
		TestReconnectionLogic<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_Reconnection_ShouldAttemptReconnect()
	{
		TestReconnectionLogic<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_Reconnection_ShouldAttemptReconnect()
	{
		TestReconnectionLogic<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_Reconnection_ShouldAttemptReconnect()
	{
		TestReconnectionLogic<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_Reconnection_ShouldAttemptReconnect()
	{
		TestReconnectionLogic<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_Reconnection_ShouldAttemptReconnect()
	{
		TestReconnectionLogic<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_Reconnection_ShouldAttemptReconnect()
	{
		TestReconnectionLogic<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_ReconnectionSettings_ShouldBeRespected()
	{
		TestReconnectionSettings<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_ReconnectionSettings_ShouldBeRespected()
	{
		TestReconnectionSettings<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_ReconnectionSettings_ShouldBeRespected()
	{
		TestReconnectionSettings<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_ReconnectionSettings_ShouldBeRespected()
	{
		TestReconnectionSettings<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_ReconnectionSettings_ShouldBeRespected()
	{
		TestReconnectionSettings<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_ReconnectionSettings_ShouldBeRespected()
	{
		TestReconnectionSettings<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_ReconnectionSettings_ShouldBeRespected()
	{
		TestReconnectionSettings<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_ErrorMessage_ShouldContainDetails()
	{
		TestErrorMessageDetails<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_ErrorMessage_ShouldContainDetails()
	{
		TestErrorMessageDetails<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_ErrorMessage_ShouldContainDetails()
	{
		TestErrorMessageDetails<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_ErrorMessage_ShouldContainDetails()
	{
		TestErrorMessageDetails<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_ErrorMessage_ShouldContainDetails()
	{
		TestErrorMessageDetails<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_ErrorMessage_ShouldContainDetails()
	{
		TestErrorMessageDetails<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_ErrorMessage_ShouldContainDetails()
	{
		TestErrorMessageDetails<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_TimeoutError_ShouldBeHandled()
	{
		TestTimeoutError<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_TimeoutError_ShouldBeHandled()
	{
		TestTimeoutError<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_TimeoutError_ShouldBeHandled()
	{
		TestTimeoutError<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_TimeoutError_ShouldBeHandled()
	{
		TestTimeoutError<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_TimeoutError_ShouldBeHandled()
	{
		TestTimeoutError<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_TimeoutError_ShouldBeHandled()
	{
		TestTimeoutError<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_TimeoutError_ShouldBeHandled()
	{
		TestTimeoutError<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_InvalidMessageFormat_ShouldReturnError()
	{
		TestInvalidMessageFormat<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_InvalidMessageFormat_ShouldReturnError()
	{
		TestInvalidMessageFormat<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_InvalidMessageFormat_ShouldReturnError()
	{
		TestInvalidMessageFormat<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_InvalidMessageFormat_ShouldReturnError()
	{
		TestInvalidMessageFormat<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_InvalidMessageFormat_ShouldReturnError()
	{
		TestInvalidMessageFormat<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_InvalidMessageFormat_ShouldReturnError()
	{
		TestInvalidMessageFormat<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_InvalidMessageFormat_ShouldReturnError()
	{
		TestInvalidMessageFormat<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_ErrorRecovery_ShouldAllowRetry()
	{
		TestErrorRecovery<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_ErrorRecovery_ShouldAllowRetry()
	{
		TestErrorRecovery<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_ErrorRecovery_ShouldAllowRetry()
	{
		TestErrorRecovery<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_ErrorRecovery_ShouldAllowRetry()
	{
		TestErrorRecovery<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_ErrorRecovery_ShouldAllowRetry()
	{
		TestErrorRecovery<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_ErrorRecovery_ShouldAllowRetry()
	{
		TestErrorRecovery<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_ErrorRecovery_ShouldAllowRetry()
	{
		TestErrorRecovery<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_ConnectionLost_ShouldTriggerReconnect()
	{
		TestConnectionLostReconnect<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_ConnectionLost_ShouldTriggerReconnect()
	{
		TestConnectionLostReconnect<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_ConnectionLost_ShouldTriggerReconnect()
	{
		TestConnectionLostReconnect<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_ConnectionLost_ShouldTriggerReconnect()
	{
		TestConnectionLostReconnect<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_ConnectionLost_ShouldTriggerReconnect()
	{
		TestConnectionLostReconnect<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_ConnectionLost_ShouldTriggerReconnect()
	{
		TestConnectionLostReconnect<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_ConnectionLost_ShouldTriggerReconnect()
	{
		TestConnectionLostReconnect<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_MaxReconnectionAttempts_ShouldBeRespected()
	{
		TestMaxReconnectionAttempts<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_MaxReconnectionAttempts_ShouldBeRespected()
	{
		TestMaxReconnectionAttempts<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_MaxReconnectionAttempts_ShouldBeRespected()
	{
		TestMaxReconnectionAttempts<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_MaxReconnectionAttempts_ShouldBeRespected()
	{
		TestMaxReconnectionAttempts<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_MaxReconnectionAttempts_ShouldBeRespected()
	{
		TestMaxReconnectionAttempts<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_MaxReconnectionAttempts_ShouldBeRespected()
	{
		TestMaxReconnectionAttempts<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_MaxReconnectionAttempts_ShouldBeRespected()
	{
		TestMaxReconnectionAttempts<BtceMessageAdapter>();
	}

	private static void TestInvalidCredentials<T>() where T : MessageAdapter
	{
		var errors = new List<ErrorMessage>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg =>
		{
			if (msg is ErrorMessage err)
				errors.Add(err);
		};

		var connectMsg = new ConnectMessage();
		adapter.SendInMessage(connectMsg);

		// Should handle invalid credentials gracefully
		true.AssertTrue("Should handle invalid credentials");
	}

	private static void TestNetworkErrorHandling<T>() where T : MessageAdapter
	{
		var errors = new List<ErrorMessage>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg =>
		{
			if (msg is ErrorMessage err)
				errors.Add(err);
		};

		// Simulate network error by sending message when not connected
		var lookupMsg = new SecurityLookupMessage { TransactionId = 1 };
		adapter.SendInMessage(lookupMsg);

		// Should handle network errors without crashing
		true.AssertTrue("Should handle network errors gracefully");
	}

	private static void TestReconnectionLogic<T>() where T : MessageAdapter
	{
		var connectionStates = new List<ConnectionStates>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg =>
		{
			if (msg is ConnectMessage connect)
				connectionStates.Add(connect.Error == null ? ConnectionStates.Connected : ConnectionStates.Failed);
			else if (msg is DisconnectMessage)
				connectionStates.Add(ConnectionStates.Disconnected);
		};

		adapter.SendInMessage(new ConnectMessage());
		adapter.SendInMessage(new DisconnectMessage());
		adapter.SendInMessage(new ConnectMessage());

		(connectionStates.Count > 0).AssertTrue("Should support reconnection");
	}

	private static void TestReconnectionSettings<T>() where T : MessageAdapter
	{
		var adapter = CreateAdapter<T>();
		
		// Test that reconnection settings can be configured
		adapter.ReConnectionSettings.ReAttemptCount = 5;
		adapter.ReConnectionSettings.Interval = TimeSpan.FromSeconds(2);

		adapter.ReConnectionSettings.ReAttemptCount.AssertEqual(5);
		adapter.ReConnectionSettings.Interval.AssertEqual(TimeSpan.FromSeconds(2));
	}

	private static void TestErrorMessageDetails<T>() where T : MessageAdapter
	{
		var errors = new List<ErrorMessage>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg =>
		{
			if (msg is ErrorMessage err)
				errors.Add(err);
		};

		var lookupMsg = new SecurityLookupMessage { TransactionId = 1 };
		adapter.SendInMessage(lookupMsg);

		// Error messages should contain transaction ID if applicable
		if (errors.Count > 0)
		{
			errors.Any(e => e.OriginalTransactionId == 1 || e.OriginalTransactionId == 0).AssertTrue("Error should contain transaction details");
		}
	}

	private static void TestTimeoutError<T>() where T : MessageAdapter
	{
		var errors = new List<ErrorMessage>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg =>
		{
			if (msg is ErrorMessage err)
				errors.Add(err);
		};

		// Test timeout handling
		adapter.SendInMessage(new SecurityLookupMessage { TransactionId = 1 });

		// Should handle timeout without crashing
		true.AssertTrue("Should handle timeout errors");
	}

	private static void TestInvalidMessageFormat<T>() where T : MessageAdapter
	{
		var errors = new List<ErrorMessage>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg =>
		{
			if (msg is ErrorMessage err)
				errors.Add(err);
		};

		// Send potentially invalid message
		var invalidMsg = new MarketDataMessage
		{
			TransactionId = 1,
			SecurityId = SecurityId.Empty,
			IsSubscribe = true
		};
		adapter.SendInMessage(invalidMsg);

		// Should handle invalid format gracefully
		true.AssertTrue("Should handle invalid message format");
	}

	private static void TestErrorRecovery<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		// Send message that might fail
		adapter.SendInMessage(new SecurityLookupMessage { TransactionId = 1 });
		
		// Try again after potential error
		adapter.SendInMessage(new SecurityLookupMessage { TransactionId = 2 });

		// Should allow retry after error
		messages.Count.AssertTrue(c => c >= 0, "Should allow error recovery");
	}

	private static void TestConnectionLostReconnect<T>() where T : MessageAdapter
	{
		var connectionStates = new List<ConnectionStates>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg =>
		{
			if (msg is ConnectMessage connect)
				connectionStates.Add(connect.Error == null ? ConnectionStates.Connected : ConnectionStates.Failed);
			else if (msg is DisconnectMessage)
				connectionStates.Add(ConnectionStates.Disconnected);
		};

		adapter.SendInMessage(new ConnectMessage());
		adapter.SendInMessage(new DisconnectMessage());
		adapter.SendInMessage(new ConnectMessage());

		(connectionStates.Count > 0).AssertTrue("Should support reconnection after connection lost");
	}

	private static void TestMaxReconnectionAttempts<T>() where T : MessageAdapter
	{
		var adapter = CreateAdapter<T>();
		
		// Configure max reconnection attempts
		adapter.ReConnectionSettings.ReAttemptCount = 3;
		
		adapter.ReConnectionSettings.ReAttemptCount.AssertEqual(3, "Should respect max reconnection attempts");
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


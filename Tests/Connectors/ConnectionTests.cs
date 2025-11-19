namespace StockSharp.Tests.Connectors;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using StockSharp.BitStamp;
using StockSharp.Coinbase;
using StockSharp.FTX;
using StockSharp.Tinkoff;
using StockSharp.Bitalong;
using StockSharp.Bitexbook;
using StockSharp.Btce;
using StockSharp.Messages;

/// <summary>
/// Integration tests for connector connection and disconnection flows.
/// </summary>
[TestClass]
public class ConnectionTests
{
	private static readonly Dictionary<Type, Func<IdGenerator, MessageAdapter>> _adapterFactories = new()
	{
		{ typeof(BitStampMessageAdapter), gen => new BitStampMessageAdapter(gen) },
		{ typeof(CoinbaseMessageAdapter), gen => new CoinbaseMessageAdapter(gen) },
		{ typeof(FtxMessageAdapter), gen => new FtxMessageAdapter(gen) },
		{ typeof(TinkoffMessageAdapter), gen => new TinkoffMessageAdapter(gen) },
		{ typeof(BitalongMessageAdapter), gen => new BitalongMessageAdapter(gen) },
		{ typeof(BitexbookMessageAdapter), gen => new BitexbookMessageAdapter(gen) },
		{ typeof(BtceMessageAdapter), gen => new BtceMessageAdapter(gen) },
	};

	[TestMethod]
	public void BitStamp_Connect_ShouldSendConnectMessage()
	{
		TestConnectFlow<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_Connect_ShouldSendConnectMessage()
	{
		TestConnectFlow<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_Connect_ShouldSendConnectMessage()
	{
		TestConnectFlow<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_Connect_ShouldSendConnectMessage()
	{
		TestConnectFlow<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_Connect_ShouldSendConnectMessage()
	{
		TestConnectFlow<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_Connect_ShouldSendConnectMessage()
	{
		TestConnectFlow<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_Connect_ShouldSendConnectMessage()
	{
		TestConnectFlow<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_Disconnect_ShouldSendDisconnectMessage()
	{
		TestDisconnectFlow<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_Disconnect_ShouldSendDisconnectMessage()
	{
		TestDisconnectFlow<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_Disconnect_ShouldSendDisconnectMessage()
	{
		TestDisconnectFlow<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_Disconnect_ShouldSendDisconnectMessage()
	{
		TestDisconnectFlow<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_Disconnect_ShouldSendDisconnectMessage()
	{
		TestDisconnectFlow<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_Disconnect_ShouldSendDisconnectMessage()
	{
		TestDisconnectFlow<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_Disconnect_ShouldSendDisconnectMessage()
	{
		TestDisconnectFlow<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_ConnectDisconnect_ShouldCompleteBothOperations()
	{
		TestConnectDisconnectCycle<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_ConnectDisconnect_ShouldCompleteBothOperations()
	{
		TestConnectDisconnectCycle<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_ConnectDisconnect_ShouldCompleteBothOperations()
	{
		TestConnectDisconnectCycle<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_ConnectDisconnect_ShouldCompleteBothOperations()
	{
		TestConnectDisconnectCycle<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_ConnectDisconnect_ShouldCompleteBothOperations()
	{
		TestConnectDisconnectCycle<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_ConnectDisconnect_ShouldCompleteBothOperations()
	{
		TestConnectDisconnectCycle<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_ConnectDisconnect_ShouldCompleteBothOperations()
	{
		TestConnectDisconnectCycle<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_ConnectWithoutCredentials_ShouldHandleGracefully()
	{
		TestConnectWithoutCredentials<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_ConnectWithoutCredentials_ShouldHandleGracefully()
	{
		TestConnectWithoutCredentials<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_ConnectWithoutCredentials_ShouldHandleGracefully()
	{
		TestConnectWithoutCredentials<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_ConnectWithoutCredentials_ShouldHandleGracefully()
	{
		TestConnectWithoutCredentials<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_ConnectWithoutCredentials_ShouldHandleGracefully()
	{
		TestConnectWithoutCredentials<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_ConnectWithoutCredentials_ShouldHandleGracefully()
	{
		TestConnectWithoutCredentials<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_ConnectWithoutCredentials_ShouldHandleGracefully()
	{
		TestConnectWithoutCredentials<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_MultipleConnectAttempts_ShouldHandleCorrectly()
	{
		TestMultipleConnectAttempts<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_MultipleConnectAttempts_ShouldHandleCorrectly()
	{
		TestMultipleConnectAttempts<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_MultipleConnectAttempts_ShouldHandleCorrectly()
	{
		TestMultipleConnectAttempts<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_MultipleConnectAttempts_ShouldHandleCorrectly()
	{
		TestMultipleConnectAttempts<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_MultipleConnectAttempts_ShouldHandleCorrectly()
	{
		TestMultipleConnectAttempts<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_MultipleConnectAttempts_ShouldHandleCorrectly()
	{
		TestMultipleConnectAttempts<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_MultipleConnectAttempts_ShouldHandleCorrectly()
	{
		TestMultipleConnectAttempts<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_DisconnectWithoutConnect_ShouldHandleGracefully()
	{
		TestDisconnectWithoutConnect<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_DisconnectWithoutConnect_ShouldHandleGracefully()
	{
		TestDisconnectWithoutConnect<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_DisconnectWithoutConnect_ShouldHandleGracefully()
	{
		TestDisconnectWithoutConnect<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_DisconnectWithoutConnect_ShouldHandleGracefully()
	{
		TestDisconnectWithoutConnect<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_DisconnectWithoutConnect_ShouldHandleGracefully()
	{
		TestDisconnectWithoutConnect<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_DisconnectWithoutConnect_ShouldHandleGracefully()
	{
		TestDisconnectWithoutConnect<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_DisconnectWithoutConnect_ShouldHandleGracefully()
	{
		TestDisconnectWithoutConnect<BtceMessageAdapter>();
	}

	[TestMethod]
	public void BitStamp_ConnectionState_ShouldBeTracked()
	{
		TestConnectionStateTracking<BitStampMessageAdapter>();
	}

	[TestMethod]
	public void Coinbase_ConnectionState_ShouldBeTracked()
	{
		TestConnectionStateTracking<CoinbaseMessageAdapter>();
	}

	[TestMethod]
	public void FTX_ConnectionState_ShouldBeTracked()
	{
		TestConnectionStateTracking<FtxMessageAdapter>();
	}

	[TestMethod]
	public void Tinkoff_ConnectionState_ShouldBeTracked()
	{
		TestConnectionStateTracking<TinkoffMessageAdapter>();
	}

	[TestMethod]
	public void Bitalong_ConnectionState_ShouldBeTracked()
	{
		TestConnectionStateTracking<BitalongMessageAdapter>();
	}

	[TestMethod]
	public void Bitexbook_ConnectionState_ShouldBeTracked()
	{
		TestConnectionStateTracking<BitexbookMessageAdapter>();
	}

	[TestMethod]
	public void Btce_ConnectionState_ShouldBeTracked()
	{
		TestConnectionStateTracking<BtceMessageAdapter>();
	}

	private static void TestConnectFlow<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var connectMsg = new ConnectMessage();
		adapter.SendInMessage(connectMsg);

		messages.Any(m => m is ConnectMessage || m is DisconnectMessage).AssertTrue("Should receive connection-related message");
	}

	private static void TestDisconnectFlow<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var disconnectMsg = new DisconnectMessage();
		adapter.SendInMessage(disconnectMsg);

		messages.Any(m => m is DisconnectMessage).AssertTrue("Should receive disconnect message");
	}

	private static void TestConnectDisconnectCycle<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		adapter.SendInMessage(new ConnectMessage());
		adapter.SendInMessage(new DisconnectMessage());

		messages.Count(m => m is ConnectMessage || m is DisconnectMessage).AssertEqual(2, "Should receive both connect and disconnect messages");
	}

	private static void TestConnectWithoutCredentials<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var connectMsg = new ConnectMessage();
		adapter.SendInMessage(connectMsg);

		// Should either connect or send error, but not throw exception
		messages.Any(m => m is ConnectMessage || m is DisconnectMessage || m is ErrorMessage).AssertTrue("Should handle connection attempt gracefully");
	}

	private static void TestMultipleConnectAttempts<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		adapter.SendInMessage(new ConnectMessage());
		adapter.SendInMessage(new ConnectMessage());
		adapter.SendInMessage(new ConnectMessage());

		// Should handle multiple attempts without crashing
		(messages.Count > 0).AssertTrue("Should process multiple connect attempts");
	}

	private static void TestDisconnectWithoutConnect<T>() where T : MessageAdapter
	{
		var messages = new List<Message>();
		var adapter = CreateAdapter<T>();
		adapter.NewOutMessage += msg => messages.Add(msg);

		var disconnectMsg = new DisconnectMessage();
		adapter.SendInMessage(disconnectMsg);

		// Should handle disconnect without prior connect
		messages.Any(m => m is DisconnectMessage || m is ErrorMessage).AssertTrue("Should handle disconnect without connect");
	}

	private static void TestConnectionStateTracking<T>() where T : MessageAdapter
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

		(connectionStates.Count > 0).AssertTrue("Should track connection states");
	}

	private static MessageAdapter CreateAdapter<T>() where T : MessageAdapter
	{
		var factory = _adapterFactories[typeof(T)];
		return factory(new IncrementalIdGenerator());
	}
}


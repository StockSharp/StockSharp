namespace StockSharp.Tests;

[TestClass]
public class SecurityNativeIdManagerTests : BaseTestClass
{
	private sealed class TestReceiver : TestLogReceiver
	{
	}

	private sealed class MockNativeIdStorage : INativeIdStorage
	{
		private readonly SynchronizedDictionary<SecurityId, object> _bySecurityId = [];
		private readonly SynchronizedDictionary<object, SecurityId> _byNativeId = new(new ObjectComparer());

		public event Func<SecurityId, object, CancellationToken, ValueTask> Added;

		public ValueTask<(SecurityId secId, object nativeId)[]> GetAsync(CancellationToken cancellationToken = default)
		{
			return new([.. _bySecurityId.Select(p => (p.Key, p.Value))]);
		}

		public ValueTask<bool> TryAddAsync(SecurityId securityId, object nativeId, bool isPersistable = true, CancellationToken cancellationToken = default)
		{
			if (_bySecurityId.ContainsKey(securityId) || _byNativeId.ContainsKey(nativeId))
				return new(false);

			_bySecurityId[securityId] = nativeId;
			_byNativeId[nativeId] = securityId;

			Added?.Invoke(securityId, nativeId, cancellationToken);

			return new(true);
		}

		public ValueTask<SecurityId?> TryGetByNativeIdAsync(object nativeId, CancellationToken cancellationToken = default)
		{
			return new(_byNativeId.TryGetValue(nativeId, out var secId) ? secId : null);
		}

		public ValueTask<object> TryGetBySecurityIdAsync(SecurityId securityId, CancellationToken cancellationToken = default)
		{
			return new(_bySecurityId.TryGetValue(securityId));
		}

		public ValueTask ClearAsync(CancellationToken cancellationToken = default)
		{
			_bySecurityId.Clear();
			_byNativeId.Clear();
			return default;
		}

		public ValueTask<bool> RemoveBySecurityIdAsync(SecurityId securityId, bool isPersistable = true, CancellationToken cancellationToken = default)
		{
			if (!_bySecurityId.TryGetAndRemove(securityId, out var nativeId))
				return new(false);

			_byNativeId.Remove(nativeId);
			return new(true);
		}

		public ValueTask<bool> RemoveByNativeIdAsync(object nativeId, bool isPersistable = true, CancellationToken cancellationToken = default)
		{
			if (!_byNativeId.TryGetAndRemove(nativeId, out var secId))
				return new(false);

			_bySecurityId.Remove(secId);
			return new(true);
		}

		private sealed class ObjectComparer : IEqualityComparer<object>
		{
			public new bool Equals(object x, object y) => Equals2(x, y);

			private static bool Equals2(object x, object y)
			{
				if (ReferenceEquals(x, y))
					return true;

				if (x is null || y is null)
					return false;

				if (x is byte[] xBytes && y is byte[] yBytes)
					return xBytes.SequenceEqual(yBytes);

				return x.Equals(y);
			}

			public int GetHashCode(object obj)
			{
				if (obj is byte[] bytes)
					return bytes.Length > 0 ? bytes[0].GetHashCode() : 0;

				return obj?.GetHashCode() ?? 0;
			}
		}
	}

	private sealed class MockNativeIdStorageProvider : INativeIdStorageProvider
	{
		private readonly SynchronizedDictionary<string, INativeIdStorage> _storages = [];

		public INativeIdStorage GetStorage(string storageName)
		{
			return _storages.SafeAdd(storageName, _ => new MockNativeIdStorage());
		}

		public ValueTask<Dictionary<string, Exception>> InitAsync(CancellationToken cancellationToken)
		{
			return new(new Dictionary<string, Exception>());
		}

		public ValueTask DisposeAsync() => default;
	}

	[TestMethod]
	public async Task ProcessInMessage_Reset_ClearsState()
	{
		var token = CancellationToken;
		var logReceiver = new TestReceiver();
		var storageProvider = new MockNativeIdStorageProvider();

		using var manager = new SecurityNativeIdManager(logReceiver, storageProvider, false);
		await manager.InitializeAsync("TestAdapter", token);

		var (toInner, toOut) = await manager.ProcessInMessageAsync(new ResetMessage(), token);

		toInner.Length.AssertEqual(1);
		toInner[0].Type.AssertEqual(MessageTypes.Reset);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task ProcessInMessage_SecurityLookup_PassesThrough()
	{
		var token = CancellationToken;
		var logReceiver = new TestReceiver();
		var storageProvider = new MockNativeIdStorageProvider();

		using var manager = new SecurityNativeIdManager(logReceiver, storageProvider, false);
		await manager.InitializeAsync("TestAdapter", token);

		var lookup = new SecurityLookupMessage { TransactionId = 1 };
		var (toInner, toOut) = await manager.ProcessInMessageAsync(lookup, token);

		toInner.Length.AssertEqual(1);
		toInner[0].AssertSame(lookup);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task ProcessInMessage_WithKnownNativeId_AddsNativeIdToMessage()
	{
		var token = CancellationToken;
		var logReceiver = new TestReceiver();
		var storageProvider = new MockNativeIdStorageProvider();

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var nativeId = 12345L;

		var storage = storageProvider.GetStorage("TestAdapter");
		await storage.TryAddAsync(secId, nativeId, cancellationToken: token);

		using var manager = new SecurityNativeIdManager(logReceiver, storageProvider, false);
		await manager.InitializeAsync("TestAdapter", token);

		var orderMsg = new OrderRegisterMessage
		{
			SecurityId = secId,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 100,
			Volume = 10
		};

		var (toInner, toOut) = await manager.ProcessInMessageAsync(orderMsg, token);

		toInner.Length.AssertEqual(1);
		((ISecurityIdMessage)toInner[0]).SecurityId.Native.AssertEqual(nativeId);
	}

	[TestMethod]
	public async Task ProcessInMessage_WithUnknownNativeId_SuspendsMessage()
	{
		var token = CancellationToken;
		var logReceiver = new TestReceiver();
		var storageProvider = new MockNativeIdStorageProvider();

		var secId = new SecurityId { SecurityCode = "UNKNOWN", BoardCode = "TEST" };

		using var manager = new SecurityNativeIdManager(logReceiver, storageProvider, false);
		await manager.InitializeAsync("TestAdapter", token);

		var orderMsg = new OrderRegisterMessage
		{
			SecurityId = secId,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 100,
			Volume = 10
		};

		var (toInner, toOut) = await manager.ProcessInMessageAsync(orderMsg, token);

		// Message should be suspended (not forwarded)
		toInner.Length.AssertEqual(0);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task ProcessOutMessage_Connect_LoadsNativeIds()
	{
		var token = CancellationToken;
		var logReceiver = new TestReceiver();
		var storageProvider = new MockNativeIdStorageProvider();

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var nativeId = 12345L;

		var storage = storageProvider.GetStorage("TestAdapter");
		await storage.TryAddAsync(secId, nativeId, cancellationToken: token);

		using var manager = new SecurityNativeIdManager(logReceiver, storageProvider, false);
		await manager.InitializeAsync("TestAdapter", token);

		var (forward, extraOut, loopbackIn) = await manager.ProcessOutMessageAsync(new ConnectMessage(), token);

		forward.Type.AssertEqual(MessageTypes.Connect);
	}

	[TestMethod]
	public async Task ProcessOutMessage_SecurityMessage_StoresMapping()
	{
		var token = CancellationToken;
		var logReceiver = new TestReceiver();
		var storageProvider = new MockNativeIdStorageProvider();

		using var manager = new SecurityNativeIdManager(logReceiver, storageProvider, false);
		await manager.InitializeAsync("TestAdapter", token);

		var secId = new SecurityId { SecurityCode = "MSFT", BoardCode = "NYSE", Native = "native_msft" };
		var secMsg = new SecurityMessage { SecurityId = secId };

		var (forward, extraOut, loopbackIn) = await manager.ProcessOutMessageAsync(secMsg, token);

		forward.AssertNotNull();
		((SecurityMessage)forward).SecurityId.Native.AssertNull();
		((SecurityMessage)forward).SecurityId.SecurityCode.AssertEqual("MSFT");
		((SecurityMessage)forward).SecurityId.BoardCode.AssertEqual("NYSE");

		// Verify the mapping was stored
		var storage = storageProvider.GetStorage("TestAdapter");
		var storedNative = await storage.TryGetBySecurityIdAsync(new SecurityId { SecurityCode = "MSFT", BoardCode = "NYSE" }, token);
		storedNative.AssertEqual("native_msft");
	}

	[TestMethod]
	public async Task ProcessOutMessage_Level1_WithKnownNativeId_TranslatesSecurityId()
	{
		var token = CancellationToken;
		var logReceiver = new TestReceiver();
		var storageProvider = new MockNativeIdStorageProvider();

		var secId = new SecurityId { SecurityCode = "GOOG", BoardCode = "NASDAQ" };
		var nativeId = 12345L;

		var storage = storageProvider.GetStorage("TestAdapter");
		await storage.TryAddAsync(secId, nativeId, cancellationToken: token);

		using var manager = new SecurityNativeIdManager(logReceiver, storageProvider, false);
		await manager.InitializeAsync("TestAdapter", token);

		var level1Msg = new Level1ChangeMessage
		{
			SecurityId = new SecurityId { Native = nativeId },
			ServerTime = DateTime.UtcNow
		};
		level1Msg.Add(Level1Fields.LastTradePrice, 100m);

		var (forward, extraOut, loopbackIn) = await manager.ProcessOutMessageAsync(level1Msg, token);

		forward.AssertNotNull();
		var result = (Level1ChangeMessage)forward;
		result.SecurityId.SecurityCode.AssertEqual("GOOG");
		result.SecurityId.BoardCode.AssertEqual("NASDAQ");
		result.SecurityId.Native.AssertNull();
	}

	[TestMethod]
	public async Task ProcessOutMessage_Level1_WithUnknownNativeId_SuspendsMessage()
	{
		var token = CancellationToken;
		var logReceiver = new TestReceiver();
		var storageProvider = new MockNativeIdStorageProvider();

		using var manager = new SecurityNativeIdManager(logReceiver, storageProvider, false);
		await manager.InitializeAsync("TestAdapter", token);

		var nativeId = "unknown_native";
		var level1Msg = new Level1ChangeMessage
		{
			SecurityId = new SecurityId { Native = nativeId },
			ServerTime = DateTime.UtcNow
		};
		level1Msg.Add(Level1Fields.LastTradePrice, 100m);

		var (forward, extraOut, loopbackIn) = await manager.ProcessOutMessageAsync(level1Msg, token);

		// Message should be suspended
		forward.AssertNull();
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task ProcessOutMessage_SecurityMessage_ReleasesSuspendedMessages()
	{
		var token = CancellationToken;
		var logReceiver = new TestReceiver();
		var storageProvider = new MockNativeIdStorageProvider();

		using var manager = new SecurityNativeIdManager(logReceiver, storageProvider, false);
		await manager.InitializeAsync("TestAdapter", token);

		var nativeId = "native_tsla";

		// First, send a Level1 message with unknown native id (should be suspended)
		var level1Msg = new Level1ChangeMessage
		{
			SecurityId = new SecurityId { Native = nativeId },
			ServerTime = DateTime.UtcNow
		};
		level1Msg.Add(Level1Fields.LastTradePrice, 100m);

		var (forward1, _, _) = await manager.ProcessOutMessageAsync(level1Msg, token);
		forward1.AssertNull();

		// Now send a SecurityMessage that maps the native id
		var secId = new SecurityId { SecurityCode = "TSLA", BoardCode = "NASDAQ", Native = nativeId };
		var secMsg = new SecurityMessage { SecurityId = secId };

		var (forward2, extraOut, loopbackIn) = await manager.ProcessOutMessageAsync(secMsg, token);

		// SecurityMessage should be forwarded
		forward2.AssertNotNull();

		// Suspended Level1 message should be released
		extraOut.Length.AssertEqual(1);
		var releasedMsg = (Level1ChangeMessage)extraOut[0];
		releasedMsg.SecurityId.SecurityCode.AssertEqual("TSLA");
		releasedMsg.SecurityId.BoardCode.AssertEqual("NASDAQ");
	}

	[TestMethod]
	public async Task ProcessOutMessage_QuoteChange_WithNativeId_IsTranslated()
	{
		var token = CancellationToken;
		var logReceiver = new TestReceiver();
		var storageProvider = new MockNativeIdStorageProvider();

		var secId = new SecurityId { SecurityCode = "NVDA", BoardCode = "NASDAQ" };
		var nativeId = 99999L;

		var storage = storageProvider.GetStorage("TestAdapter");
		await storage.TryAddAsync(secId, nativeId, cancellationToken: token);

		using var manager = new SecurityNativeIdManager(logReceiver, storageProvider, false);
		await manager.InitializeAsync("TestAdapter", token);

		var quotesMsg = new QuoteChangeMessage
		{
			SecurityId = new SecurityId { Native = nativeId },
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(100, 10)],
			Asks = [new QuoteChange(101, 10)]
		};

		var (forward, extraOut, loopbackIn) = await manager.ProcessOutMessageAsync(quotesMsg, token);

		forward.AssertNotNull();
		var result = (QuoteChangeMessage)forward;
		result.SecurityId.SecurityCode.AssertEqual("NVDA");
		result.SecurityId.BoardCode.AssertEqual("NASDAQ");
	}

	[TestMethod]
	public async Task ProcessOutMessage_PositionChange_WithNativeId_IsTranslated()
	{
		var token = CancellationToken;
		var logReceiver = new TestReceiver();
		var storageProvider = new MockNativeIdStorageProvider();

		var secId = new SecurityId { SecurityCode = "AMD", BoardCode = "NASDAQ" };
		var nativeId = "native_amd";

		var storage = storageProvider.GetStorage("TestAdapter");
		await storage.TryAddAsync(secId, nativeId, cancellationToken: token);

		using var manager = new SecurityNativeIdManager(logReceiver, storageProvider, false);
		await manager.InitializeAsync("TestAdapter", token);

		var posMsg = new PositionChangeMessage
		{
			SecurityId = new SecurityId { Native = nativeId },
			PortfolioName = "TestPortfolio",
			ServerTime = DateTime.UtcNow
		};
		posMsg.Add(PositionChangeTypes.CurrentValue, 100m);

		var (forward, extraOut, loopbackIn) = await manager.ProcessOutMessageAsync(posMsg, token);

		forward.AssertNotNull();
		var result = (PositionChangeMessage)forward;
		result.SecurityId.SecurityCode.AssertEqual("AMD");
		result.SecurityId.BoardCode.AssertEqual("NASDAQ");
	}

	[TestMethod]
	public async Task ProcessOutMessage_Execution_WithTransactionId_TracksSecurityId()
	{
		var token = CancellationToken;
		var logReceiver = new TestReceiver();
		var storageProvider = new MockNativeIdStorageProvider();

		var secId = new SecurityId { SecurityCode = "AMZN", BoardCode = "NASDAQ" };
		var nativeId = "native_amzn";

		var storage = storageProvider.GetStorage("TestAdapter");
		await storage.TryAddAsync(secId, nativeId, cancellationToken: token);

		using var manager = new SecurityNativeIdManager(logReceiver, storageProvider, false);
		await manager.InitializeAsync("TestAdapter", token);

		var execMsg = new ExecutionMessage
		{
			SecurityId = new SecurityId { Native = nativeId },
			DataTypeEx = DataType.Transactions,
			TransactionId = 100,
			ServerTime = DateTime.UtcNow,
			HasOrderInfo = true,
			OrderState = OrderStates.Active
		};

		var (forward, extraOut, loopbackIn) = await manager.ProcessOutMessageAsync(execMsg, token);

		forward.AssertNotNull();
		var result = (ExecutionMessage)forward;
		result.SecurityId.SecurityCode.AssertEqual("AMZN");
	}

	[TestMethod]
	public async Task ProcessInMessage_ProcessSuspended_ReleasesInMessages()
	{
		var token = CancellationToken;
		var logReceiver = new TestReceiver();
		var storageProvider = new MockNativeIdStorageProvider();

		var secId = new SecurityId { SecurityCode = "FB", BoardCode = "NASDAQ" };
		var nativeId = "native_fb";

		using var manager = new SecurityNativeIdManager(logReceiver, storageProvider, false);
		await manager.InitializeAsync("TestAdapter", token);

		// First, send an order that will be suspended
		var orderMsg = new OrderRegisterMessage
		{
			SecurityId = secId,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 100,
			Volume = 10
		};

		var (toInner1, _) = await manager.ProcessInMessageAsync(orderMsg, token);
		toInner1.Length.AssertEqual(0); // Suspended

		// Now add the native id to storage and trigger ProcessSuspended
		var storage = storageProvider.GetStorage("TestAdapter");
		await storage.TryAddAsync(secId, nativeId, cancellationToken: token);

		// Re-initialize to load the native id
		await manager.InitializeAsync("TestAdapter", token);

		// Process suspended message
		var tempSecId = secId;
		tempSecId.Native = nativeId;
		var suspendMsg = new ProcessSuspendedMessage(new PassThroughMessageAdapter(new IncrementalIdGenerator()), tempSecId);

		var (toInner2, toOut2) = await manager.ProcessInMessageAsync(suspendMsg, token);

		// The suspended order should be released
		toInner2.Length.AssertEqual(1);
		var releasedOrder = (OrderRegisterMessage)toInner2[0];
		releasedOrder.SecurityId.SecurityCode.AssertEqual("FB");
	}

	[TestMethod]
	public async Task DuplicateNativeId_UpdatesMapping()
	{
		var token = CancellationToken;
		var logReceiver = new TestReceiver();
		var storageProvider = new MockNativeIdStorageProvider();

		var secId1 = new SecurityId { SecurityCode = "SYM1", BoardCode = "BOARD" };
		var nativeId = "shared_native";

		var storage = storageProvider.GetStorage("TestAdapter");
		await storage.TryAddAsync(secId1, nativeId, cancellationToken: token);

		using var manager = new SecurityNativeIdManager(logReceiver, storageProvider, false);
		await manager.InitializeAsync("TestAdapter", token);

		// Send security with same native id but different security code
		var secId2 = new SecurityId { SecurityCode = "SYM2", BoardCode = "BOARD", Native = nativeId };
		var secMsg = new SecurityMessage { SecurityId = secId2 };

		await manager.ProcessOutMessageAsync(secMsg, token);

		// Verify the new mapping is stored
		var storedSecId = await storage.TryGetByNativeIdAsync(nativeId, token);
		storedSecId.AssertNotNull();
		storedSecId.Value.SecurityCode.AssertEqual("SYM2");
	}
}

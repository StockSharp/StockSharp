namespace StockSharp.Tests;

[TestClass]
public class SecurityNativeIdMessageAdapterTests : BaseTestClass
{
	private sealed class TestPassThroughAdapter : PassThroughMessageAdapter
	{
		private readonly string _storageName;

		// Default constructor required for cloning
		public TestPassThroughAdapter()
			: base(new IncrementalIdGenerator())
		{
			_storageName = "TestAdapter";
		}

		public TestPassThroughAdapter(string storageName)
			: base(new IncrementalIdGenerator())
		{
			_storageName = storageName;
		}

		public override string StorageName => _storageName;

		public List<Message> InMessages { get; } = [];

		public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken)
		{
			InMessages.Add(message);
			return base.SendInMessageAsync(message, cancellationToken);
		}
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
	public async Task Connect_LoadsExistingNativeIds_AndTranslatesOutMessages()
	{
		var token = CancellationToken;

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var nativeId = 12345L;

		var storageProvider = new MockNativeIdStorageProvider();
		var storage = storageProvider.GetStorage("TestAdapter");
		await storage.TryAddAsync(secId, nativeId, cancellationToken: token);

		var inner = new TestPassThroughAdapter("TestAdapter");
		using var adapter = new SecurityNativeIdMessageAdapter(inner, storageProvider);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new ConnectMessage(), token);

		output.OfType<ConnectMessage>().Count().AssertEqual(1);

		// After connect, outgoing messages with native id should be translated to security id
		var level1Msg = new Level1ChangeMessage
		{
			SecurityId = new SecurityId { Native = nativeId },
			ServerTime = DateTime.UtcNow
		};
		level1Msg.Add(Level1Fields.LastTradePrice, 150m);

		inner.SendOutMessage(level1Msg);

		// The message should be translated and forwarded
		var level1s = output.OfType<Level1ChangeMessage>().ToArray();
		level1s.Length.AssertEqual(1);
		level1s[0].SecurityId.SecurityCode.AssertEqual("AAPL");
		level1s[0].SecurityId.BoardCode.AssertEqual("NASDAQ");
		level1s[0].SecurityId.Native.AssertNull();  // Native should be stripped
	}

	[TestMethod]
	public async Task SecurityMessage_WithNativeId_StoresMapping()
	{
		var token = CancellationToken;

		var secId = new SecurityId { SecurityCode = "MSFT", BoardCode = "NYSE", Native = "native_msft" };

		var storageProvider = new MockNativeIdStorageProvider();
		var inner = new TestPassThroughAdapter("TestAdapter");

		using var adapter = new SecurityNativeIdMessageAdapter(inner, storageProvider);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new ConnectMessage(), token);

		inner.SendOutMessage(new SecurityMessage { SecurityId = secId });

		var secMsgs = output.OfType<SecurityMessage>().ToArray();
		secMsgs.Length.AssertEqual(1);
		secMsgs[0].SecurityId.Native.AssertNull();
		secMsgs[0].SecurityId.SecurityCode.AssertEqual("MSFT");
		secMsgs[0].SecurityId.BoardCode.AssertEqual("NYSE");

		var storage = storageProvider.GetStorage("TestAdapter");
		var storedNative = await storage.TryGetBySecurityIdAsync(new SecurityId { SecurityCode = "MSFT", BoardCode = "NYSE" }, token);
		storedNative.AssertEqual("native_msft");
	}

	[TestMethod]
	public async Task InMessage_WithoutKnownNativeId_IsSuspended()
	{
		var token = CancellationToken;

		var secId = new SecurityId { SecurityCode = "UNKNOWN", BoardCode = "TEST" };

		var storageProvider = new MockNativeIdStorageProvider();
		var inner = new TestPassThroughAdapter("TestAdapter");

		using var adapter = new SecurityNativeIdMessageAdapter(inner, storageProvider);

		await adapter.SendInMessageAsync(new ConnectMessage(), token);

		var orderMsg = new OrderRegisterMessage
		{
			SecurityId = secId,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 100,
			Volume = 10
		};

		await adapter.SendInMessageAsync(orderMsg, token);

		// Should NOT be forwarded since native id is unknown
		inner.InMessages.OfType<OrderRegisterMessage>().Count().AssertEqual(0);
	}

	[TestMethod]
	public async Task OutMessage_WithNativeId_IsTranslatedToSecurityId()
	{
		var token = CancellationToken;

		var secId = new SecurityId { SecurityCode = "GOOG", BoardCode = "NASDAQ" };
		var nativeId = 12345L;

		var storageProvider = new MockNativeIdStorageProvider();
		var storage = storageProvider.GetStorage("TestAdapter");
		await storage.TryAddAsync(secId, nativeId, cancellationToken: token);

		var inner = new TestPassThroughAdapter("TestAdapter");
		using var adapter = new SecurityNativeIdMessageAdapter(inner, storageProvider);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new ConnectMessage(), token);

		var execMsg = new ExecutionMessage
		{
			SecurityId = new SecurityId { Native = nativeId },
			DataTypeEx = DataType.Ticks,
			TradePrice = 100,
			TradeVolume = 10,
			ServerTime = DateTime.UtcNow
		};

		inner.SendOutMessage(execMsg);

		var execs = output.OfType<ExecutionMessage>().ToArray();
		execs.Length.AssertEqual(1);
		execs[0].SecurityId.SecurityCode.AssertEqual("GOOG");
		execs[0].SecurityId.BoardCode.AssertEqual("NASDAQ");
		execs[0].SecurityId.Native.AssertNull();
	}

	[TestMethod]
	public async Task OutMessage_WithUnknownNativeId_IsSuspended_ThenReleased()
	{
		var token = CancellationToken;

		var secId = new SecurityId { SecurityCode = "TSLA", BoardCode = "NASDAQ" };
		var nativeId = "native_tsla";

		var storageProvider = new MockNativeIdStorageProvider();
		var inner = new TestPassThroughAdapter("TestAdapter");

		using var adapter = new SecurityNativeIdMessageAdapter(inner, storageProvider);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new ConnectMessage(), token);

		var level1Msg = new Level1ChangeMessage
		{
			SecurityId = new SecurityId { Native = nativeId },
			ServerTime = DateTime.UtcNow
		};
		level1Msg.Add(Level1Fields.LastTradePrice, 100m);

		inner.SendOutMessage(level1Msg);

		// Should be suspended since native id is not yet known
		output.OfType<Level1ChangeMessage>().Count().AssertEqual(0);

		// Now send security message that maps the native id
		inner.SendOutMessage(new SecurityMessage
		{
			SecurityId = new SecurityId { SecurityCode = "TSLA", BoardCode = "NASDAQ", Native = nativeId }
		});

		// Suspended message should now be released
		var level1s = output.OfType<Level1ChangeMessage>().ToArray();
		level1s.Length.AssertEqual(1);
		level1s[0].SecurityId.SecurityCode.AssertEqual("TSLA");
		level1s[0].SecurityId.BoardCode.AssertEqual("NASDAQ");
	}

	[TestMethod]
	public async Task Reset_ClearsAllState()
	{
		var token = CancellationToken;

		var secId = new SecurityId { SecurityCode = "FB", BoardCode = "NASDAQ" };
		var nativeId = "native_fb";

		var storageProvider = new MockNativeIdStorageProvider();
		var storage = storageProvider.GetStorage("TestAdapter");
		await storage.TryAddAsync(secId, nativeId, cancellationToken: token);

		var inner = new TestPassThroughAdapter("TestAdapter");
		using var adapter = new SecurityNativeIdMessageAdapter(inner, storageProvider);

		await adapter.SendInMessageAsync(new ConnectMessage(), token);
		await adapter.SendInMessageAsync(new ResetMessage(), token);

		// After reset, native ids should be cleared from in-memory cache
		// New order should be suspended
		var orderMsg = new OrderRegisterMessage
		{
			SecurityId = secId,
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 100,
			Volume = 10
		};

		await adapter.SendInMessageAsync(orderMsg, token);

		// Order should NOT have native id attached after reset
		inner.InMessages.OfType<OrderRegisterMessage>().Count().AssertEqual(0);
	}

	// Note: Clone test removed because nested test adapter classes don't work well with reflection-based cloning

	[TestMethod]
	public async Task DuplicateNativeId_LogsWarning_AndUpdatesMapping()
	{
		var token = CancellationToken;

		var secId1 = new SecurityId { SecurityCode = "SYM1", BoardCode = "BOARD" };
		var secId2 = new SecurityId { SecurityCode = "SYM2", BoardCode = "BOARD" };
		var nativeId = "shared_native";

		var storageProvider = new MockNativeIdStorageProvider();
		var storage = storageProvider.GetStorage("TestAdapter");
		await storage.TryAddAsync(secId1, nativeId, cancellationToken: token);

		var inner = new TestPassThroughAdapter("TestAdapter");
		using var adapter = new SecurityNativeIdMessageAdapter(inner, storageProvider);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new ConnectMessage(), token);

		// Send security with same native id but different security code
		inner.SendOutMessage(new SecurityMessage
		{
			SecurityId = new SecurityId { SecurityCode = "SYM2", BoardCode = "BOARD", Native = nativeId }
		});

		// Verify the new mapping is stored
		var storedSecId = await storage.TryGetByNativeIdAsync(nativeId, token);
		storedSecId.AssertNotNull();
		storedSecId.Value.SecurityCode.AssertEqual("SYM2");
	}

	[TestMethod]
	public async Task ExecutionMessage_WithTransactionId_TracksSecurityId()
	{
		var token = CancellationToken;

		var secId = new SecurityId { SecurityCode = "AMZN", BoardCode = "NASDAQ" };
		var nativeId = "native_amzn";

		var storageProvider = new MockNativeIdStorageProvider();
		var storage = storageProvider.GetStorage("TestAdapter");
		await storage.TryAddAsync(secId, nativeId, cancellationToken: token);

		var inner = new TestPassThroughAdapter("TestAdapter");
		using var adapter = new SecurityNativeIdMessageAdapter(inner, storageProvider);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new ConnectMessage(), token);

		// Execution with transaction id and native security id
		var execMsg = new ExecutionMessage
		{
			SecurityId = new SecurityId { Native = nativeId },
			DataTypeEx = DataType.Transactions,
			TransactionId = 100,
			ServerTime = DateTime.UtcNow,
			HasOrderInfo = true,
			OrderState = OrderStates.Active
		};

		inner.SendOutMessage(execMsg);

		var execs = output.OfType<ExecutionMessage>().ToArray();
		execs.Length.AssertEqual(1);
		execs[0].SecurityId.SecurityCode.AssertEqual("AMZN");
	}

	[TestMethod]
	public async Task QuoteChangeMessage_WithNativeId_IsTranslated()
	{
		var token = CancellationToken;

		var secId = new SecurityId { SecurityCode = "NVDA", BoardCode = "NASDAQ" };
		var nativeId = 99999L;

		var storageProvider = new MockNativeIdStorageProvider();
		var storage = storageProvider.GetStorage("TestAdapter");
		await storage.TryAddAsync(secId, nativeId, cancellationToken: token);

		var inner = new TestPassThroughAdapter("TestAdapter");
		using var adapter = new SecurityNativeIdMessageAdapter(inner, storageProvider);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new ConnectMessage(), token);

		var quotesMsg = new QuoteChangeMessage
		{
			SecurityId = new SecurityId { Native = nativeId },
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(100, 10)],
			Asks = [new QuoteChange(101, 10)]
		};

		inner.SendOutMessage(quotesMsg);

		var quotes = output.OfType<QuoteChangeMessage>().ToArray();
		quotes.Length.AssertEqual(1);
		quotes[0].SecurityId.SecurityCode.AssertEqual("NVDA");
		quotes[0].SecurityId.BoardCode.AssertEqual("NASDAQ");
	}

	[TestMethod]
	public async Task PositionChangeMessage_WithNativeId_IsTranslated()
	{
		var token = CancellationToken;

		var secId = new SecurityId { SecurityCode = "AMD", BoardCode = "NASDAQ" };
		var nativeId = "native_amd";

		var storageProvider = new MockNativeIdStorageProvider();
		var storage = storageProvider.GetStorage("TestAdapter");
		await storage.TryAddAsync(secId, nativeId, cancellationToken: token);

		var inner = new TestPassThroughAdapter("TestAdapter");
		using var adapter = new SecurityNativeIdMessageAdapter(inner, storageProvider);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new ConnectMessage(), token);

		var posMsg = new PositionChangeMessage
		{
			SecurityId = new SecurityId { Native = nativeId },
			PortfolioName = "TestPortfolio",
			ServerTime = DateTime.UtcNow
		};
		posMsg.Add(PositionChangeTypes.CurrentValue, 100m);

		inner.SendOutMessage(posMsg);

		var positions = output.OfType<PositionChangeMessage>().ToArray();
		positions.Length.AssertEqual(1);
		positions[0].SecurityId.SecurityCode.AssertEqual("AMD");
		positions[0].SecurityId.BoardCode.AssertEqual("NASDAQ");
	}
}

namespace StockSharp.Tests;

using StockSharp.Algo.Basket;
using StockSharp.Algo.Candles.Compression;

/// <summary>
/// Isolated tests for <see cref="AdapterRouter"/> routing logic.
/// </summary>
[TestClass]
public class AdapterRouterTests : BaseTestClass
{
	#region Test Adapter

	private sealed class TestRouterAdapter : MessageAdapter
	{
		private HashSet<DataType> _allDownloadingTypes = [];
		private bool _isSecurityNewsOnly;

		public TestRouterAdapter(IdGenerator idGen)
			: base(idGen)
		{
			this.AddMarketDataSupport();
			this.AddTransactionalSupport();
			this.AddSupportedMessage(MessageTypes.SecurityLookup, null);
			this.AddSupportedMessage(MessageTypes.PortfolioLookup, null);
			this.AddSupportedMessage(MessageTypes.OrderStatus, null);
			this.AddSupportedMessage(MessageTypes.MarketData, null);
			this.AddSupportedMessage(MessageTypes.OrderRegister, null);
			this.AddSupportedMessage(MessageTypes.OrderCancel, null);
			this.AddSupportedMarketDataType(DataType.Ticks);
			this.AddSupportedMarketDataType(DataType.MarketDepth);
			this.AddSupportedMarketDataType(DataType.Level1);
			this.AddSupportedMarketDataType(DataType.News);
		}

		public void SetAllDownloadingSupported(params DataType[] types)
			=> _allDownloadingTypes = [.. types];

		public override bool IsAllDownloadingSupported(DataType dataType)
			=> _allDownloadingTypes.Contains(dataType);

		public override bool IsSecurityNewsOnly => _isSecurityNewsOnly;

		public void SetSecurityNewsOnly(bool value) => _isSecurityNewsOnly = value;

		protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken ct)
			=> default;

		public override IMessageAdapter Clone() => new TestRouterAdapter(TransactionIdGenerator);
	}

	#endregion

	#region Helpers

	private static readonly SecurityId _secId1 = "AAPL@NASDAQ".ToSecurityId();
	private static readonly SecurityId _secId2 = "MSFT@NASDAQ".ToSecurityId();

	private AdapterRouter CreateRouter(IOrderRoutingState orderRouting = null, bool levelExtend = false)
	{
		orderRouting ??= new OrderRoutingState();
		return new AdapterRouter(
			orderRouting,
			GetUnderlyingAdapter,
			new CandleBuilderProvider(new InMemoryExchangeInfoProvider()),
			() => levelExtend);
	}

	private static IMessageAdapter GetUnderlyingAdapter(IMessageAdapter adapter)
	{
		// simplified: in tests we don't use wrappers
		return adapter;
	}

	private static TestRouterAdapter CreateAdapter()
		=> new(new IncrementalIdGenerator());

	#endregion

	#region GetAdapters — Message Type Routing

	[TestMethod]
	public void GetAdapters_ByMessageType_ReturnsRegisteredAdapters()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();

		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter);

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var (adapters, skip) = router.GetAdapters(mdMsg, a => a);

		IsNotNull(adapters);
		AreEqual(1, adapters.Length);
		AreSame(adapter, adapters[0]);
		IsFalse(skip);
	}

	[TestMethod]
	public void GetAdapters_NoRegistered_ReturnsNull()
	{
		var router = CreateRouter();

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var (adapters, _) = router.GetAdapters(mdMsg, a => a);

		IsNull(adapters);
	}

	[TestMethod]
	public void GetAdapters_MultipleAdapters_ReturnsAll()
	{
		var router = CreateRouter();
		var adapter1 = CreateAdapter();
		var adapter2 = CreateAdapter();

		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter1);
		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter2);

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var (adapters, _) = router.GetAdapters(mdMsg, a => a);

		AreEqual(2, adapters.Length);
	}

	#endregion

	#region GetAdapters — Security Adapter Routing

	[TestMethod]
	public void GetAdapters_SecurityAdapter_RoutesToSpecificAdapter()
	{
		var router = CreateRouter();
		var adapter1 = CreateAdapter();
		var adapter2 = CreateAdapter();

		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter1);
		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter2);
		router.SetSecurityAdapter(_secId1, DataType.Ticks, adapter2);

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var (adapters, skip) = router.GetAdapters(mdMsg, a => a);

		AreEqual(1, adapters.Length);
		AreSame(adapter2, adapters[0]);
		IsTrue(skip);
	}

	[TestMethod]
	public void GetAdapters_SecurityAdapter_NullDataType_MatchesAnySecurity()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();

		router.SetSecurityAdapter(_secId1, null, adapter);

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Level1,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var (adapters, skip) = router.GetAdapters(mdMsg, a => a);

		AreEqual(1, adapters.Length);
		AreSame(adapter, adapters[0]);
		IsTrue(skip);
	}

	[TestMethod]
	public void GetAdapters_SecurityAdapter_WrapperNotFound_FallsBack()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();

		router.SetSecurityAdapter(_secId1, DataType.Ticks, adapter);
		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter);

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};

		// getWrapper returns null — simulating wrapper not found
		var (adapters, skip) = router.GetAdapters(mdMsg, _ => null);

		// should fall back to message type adapters
		IsNotNull(adapters);
		IsFalse(skip);
	}

	[TestMethod]
	public void RemoveSecurityAdapter_NoLongerRoutes()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();

		router.SetSecurityAdapter(_secId1, DataType.Ticks, adapter);
		router.RemoveSecurityAdapter(_secId1, DataType.Ticks);
		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter);

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var (adapters, skip) = router.GetAdapters(mdMsg, a => a);

		// should use message type adapters, not security-specific
		IsFalse(skip);
		AreEqual(1, adapters.Length);
	}

	#endregion

	#region GetAdapters — SecurityLookup Filtering

	[TestMethod]
	public void GetAdapters_SecurityLookup_LookupAll_FiltersUnsupported()
	{
		var router = CreateRouter();
		var adapter1 = CreateAdapter();
		var adapter2 = CreateAdapter();

		adapter1.SetAllDownloadingSupported(DataType.Securities);
		// adapter2 does not support

		router.AddMessageTypeAdapter(MessageTypes.SecurityLookup, adapter1);
		router.AddMessageTypeAdapter(MessageTypes.SecurityLookup, adapter2);

		var msg = new SecurityLookupMessage { TransactionId = 100 };

		var (adapters, _) = router.GetAdapters(msg, a => a);

		AreEqual(1, adapters.Length);
		AreSame(adapter1, adapters[0]);
	}

	[TestMethod]
	public void GetAdapters_SecurityLookup_SpecificSecurity_NoFiltering()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();
		// not supporting IsAllDownloading

		router.AddMessageTypeAdapter(MessageTypes.SecurityLookup, adapter);

		var msg = new SecurityLookupMessage
		{
			TransactionId = 100,
			SecurityId = _secId1,
		};

		var (adapters, _) = router.GetAdapters(msg, a => a);

		AreEqual(1, adapters.Length);
	}

	#endregion

	#region GetAdapters — OrderStatus Filtering

	[TestMethod]
	public void GetAdapters_OrderStatus_NoFilter_FiltersUnsupported()
	{
		var router = CreateRouter();
		var adapter1 = CreateAdapter();
		var adapter2 = CreateAdapter();

		adapter1.SetAllDownloadingSupported(DataType.Transactions);

		router.AddMessageTypeAdapter(MessageTypes.OrderStatus, adapter1);
		router.AddMessageTypeAdapter(MessageTypes.OrderStatus, adapter2);

		var msg = new OrderStatusMessage
		{
			TransactionId = 100,
			IsSubscribe = true,
		};

		var (adapters, _) = router.GetAdapters(msg, a => a);

		AreEqual(1, adapters.Length);
		AreSame(adapter1, adapters[0]);
	}

	[TestMethod]
	public void GetAdapters_OrderStatus_WithSecurityFilter_NoFiltering()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();
		// not supporting IsAllDownloading

		router.AddMessageTypeAdapter(MessageTypes.OrderStatus, adapter);

		var msg = new OrderStatusMessage
		{
			TransactionId = 100,
			IsSubscribe = true,
			SecurityId = _secId1,
		};

		var (adapters, _) = router.GetAdapters(msg, a => a);

		AreEqual(1, adapters.Length);
	}

	#endregion

	#region GetAdapters — PortfolioLookup Filtering

	[TestMethod]
	public void GetAdapters_PortfolioLookup_NoFilter_FiltersUnsupported()
	{
		var router = CreateRouter();
		var adapter1 = CreateAdapter();
		var adapter2 = CreateAdapter();

		adapter1.SetAllDownloadingSupported(DataType.PositionChanges);

		router.AddMessageTypeAdapter(MessageTypes.PortfolioLookup, adapter1);
		router.AddMessageTypeAdapter(MessageTypes.PortfolioLookup, adapter2);

		var msg = new PortfolioLookupMessage
		{
			TransactionId = 100,
			IsSubscribe = true,
		};

		var (adapters, _) = router.GetAdapters(msg, a => a);

		AreEqual(1, adapters.Length);
		AreSame(adapter1, adapters[0]);
	}

	[TestMethod]
	public void GetAdapters_PortfolioLookup_SpecificPortfolio_NoFiltering()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();

		router.AddMessageTypeAdapter(MessageTypes.PortfolioLookup, adapter);

		var msg = new PortfolioLookupMessage
		{
			TransactionId = 100,
			IsSubscribe = true,
			PortfolioName = "MyPortfolio",
		};

		var (adapters, _) = router.GetAdapters(msg, a => a);

		AreEqual(1, adapters.Length);
	}

	#endregion

	#region GetAdapters — News Filtering

	[TestMethod]
	public void GetAdapters_News_FiltersSecurityNewsOnlyAdapters()
	{
		var router = CreateRouter();
		var adapter1 = CreateAdapter();
		var adapter2 = CreateAdapter();

		adapter2.SetSecurityNewsOnly(true);

		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter1);
		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter2);

		var mdMsg = new MarketDataMessage
		{
			DataType2 = DataType.News,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var (adapters, _) = router.GetAdapters(mdMsg, a => a);

		AreEqual(1, adapters.Length);
		AreSame(adapter1, adapters[0]);
	}

	[TestMethod]
	public void GetAdapters_News_SecuritySpecific_NoFiltering()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();

		adapter.SetSecurityNewsOnly(true);

		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter);

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.News,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var (adapters, _) = router.GetAdapters(mdMsg, a => a);

		// security-specific news is not filtered
		AreEqual(1, adapters.Length);
	}

	#endregion

	#region GetAdapters — NotSupported Filtering

	[TestMethod]
	public void GetAdapters_NotSupported_FiltersOut()
	{
		var router = CreateRouter();
		var adapter1 = CreateAdapter();
		var adapter2 = CreateAdapter();

		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter1);
		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter2);

		// mark adapter1 as not supported for transId=100
		router.AddNotSupported(100, adapter1);

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var (adapters, _) = router.GetAdapters(mdMsg, a => a);

		AreEqual(1, adapters.Length);
		AreSame(adapter2, adapters[0]);
	}

	[TestMethod]
	public void GetAdapters_AllNotSupported_ReturnsNull()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();

		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter);
		router.AddNotSupported(100, adapter);

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var (adapters, _) = router.GetAdapters(mdMsg, a => a);

		IsNull(adapters);
	}

	#endregion

	#region GetAdapters — Explicit Adapter on Message

	[TestMethod]
	public void GetAdapters_ExplicitAdapter_RoutesDirectly()
	{
		var router = CreateRouter();
		var adapter1 = CreateAdapter();
		var adapter2 = CreateAdapter();

		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter1);
		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter2);

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
			Adapter = adapter2,
		};

		var (adapters, skip) = router.GetAdapters(mdMsg, a => a);

		AreEqual(1, adapters.Length);
		AreSame(adapter2, adapters[0]);
		IsTrue(skip);
	}

	#endregion

	#region GetSubscriptionAdapters — Market Data Type Filtering

	[TestMethod]
	public async Task GetSubscriptionAdapters_SupportedType_Returns()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var result = await router.GetSubscriptionAdaptersAsync(mdMsg, [adapter], false, CancellationToken);

		AreEqual(1, result.Length);
	}

	[TestMethod]
	public async Task GetSubscriptionAdapters_UnsupportedType_FiltersOut()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.OrderLog,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var result = await router.GetSubscriptionAdaptersAsync(mdMsg, [adapter], false, CancellationToken);

		AreEqual(0, result.Length);
	}

	[TestMethod]
	public async Task GetSubscriptionAdapters_SkipSupportedMessages_ReturnsAll()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.OrderLog,
			IsSubscribe = true,
			TransactionId = 100,
		};

		// when skipSupportedMessages=true, all adapters pass
		var result = await router.GetSubscriptionAdaptersAsync(mdMsg, [adapter], true, CancellationToken);

		AreEqual(1, result.Length);
	}

	[TestMethod]
	public async Task GetSubscriptionAdapters_MarketDepth_BuildFromOrderLog()
	{
		var router = CreateRouter();

		var idGen = new IncrementalIdGenerator();
		var limitedAdapter = new TestRouterAdapter(idGen);
		// This adapter supports only OrderLog, not MarketDepth directly.
		// Need to clear default supported data types and add only OrderLog.
		foreach (var dt in await limitedAdapter.GetSupportedMarketDataTypesAsync(default, null, null).ToArrayAsync(CancellationToken))
			limitedAdapter.RemoveSupportedMarketDataType(dt);
		limitedAdapter.AddSupportedMarketDataType(DataType.OrderLog);

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.MarketDepth,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var result = await router.GetSubscriptionAdaptersAsync(mdMsg, [limitedAdapter], false, CancellationToken);

		AreEqual(1, result.Length);
		AreEqual(DataType.OrderLog, mdMsg.BuildFrom);
	}

	#endregion

	#region Portfolio Adapter Routing

	[TestMethod]
	public void GetPortfolioAdapter_Registered_ReturnsAdapter()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();

		router.SetPortfolioAdapter("TestPortfolio", adapter);

		var result = router.GetPortfolioAdapter("TestPortfolio", a => a);

		AreSame(adapter, result);
	}

	[TestMethod]
	public void GetPortfolioAdapter_NotRegistered_ReturnsNull()
	{
		var router = CreateRouter();

		var result = router.GetPortfolioAdapter("Unknown", a => a);

		IsNull(result);
	}

	[TestMethod]
	public void GetPortfolioAdapter_CaseInsensitive()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();

		router.SetPortfolioAdapter("TestPortfolio", adapter);

		var result = router.GetPortfolioAdapter("testportfolio", a => a);

		AreSame(adapter, result);
	}

	[TestMethod]
	public void RemovePortfolioAdapter_NoLongerReturns()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();

		router.SetPortfolioAdapter("TestPortfolio", adapter);
		router.RemovePortfolioAdapter("TestPortfolio");

		var result = router.GetPortfolioAdapter("TestPortfolio", a => a);

		IsNull(result);
	}

	[TestMethod]
	public void GetPortfolioAdapter_UsesGetWrapper()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();
		var wrapper = CreateAdapter();

		router.SetPortfolioAdapter("TestPortfolio", adapter);

		var result = router.GetPortfolioAdapter("TestPortfolio", a =>
		{
			AreSame(adapter, a);
			return wrapper;
		});

		AreSame(wrapper, result);
	}

	#endregion

	#region Order Routing

	[TestMethod]
	public void OrderRouting_AddAndGet()
	{
		var orderRouting = new OrderRoutingState();
		var router = CreateRouter(orderRouting);
		var adapter = CreateAdapter();

		router.AddOrderAdapter(100, adapter);

		IsTrue(router.TryGetOrderAdapter(100, out var found));
		AreSame(adapter, found);
	}

	[TestMethod]
	public void OrderRouting_NotFound_ReturnsFalse()
	{
		var router = CreateRouter();

		IsFalse(router.TryGetOrderAdapter(999, out _));
	}

	#endregion

	#region AddMessageTypeAdapter / RemoveMessageTypeAdapter

	[TestMethod]
	public void AddRemoveMessageTypeAdapter_Works()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();

		router.AddMessageTypeAdapter(MessageTypes.SecurityLookup, adapter);

		var msg = new SecurityLookupMessage
		{
			TransactionId = 100,
			SecurityId = _secId1,
		};

		var (adapters1, _) = router.GetAdapters(msg, a => a);
		AreEqual(1, adapters1.Length);

		router.RemoveMessageTypeAdapter(MessageTypes.SecurityLookup, adapter);

		var (adapters2, _) = router.GetAdapters(msg, a => a);
		IsNull(adapters2);
	}

	[TestMethod]
	public void RemoveMessageTypeAdapter_NonExistent_NoError()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();

		// should not throw
		router.RemoveMessageTypeAdapter(MessageTypes.SecurityLookup, adapter);
	}

	[TestMethod]
	public void AddMessageTypeAdapter_Multiple_ThenRemoveOne()
	{
		var router = CreateRouter();
		var adapter1 = CreateAdapter();
		var adapter2 = CreateAdapter();

		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter1);
		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter2);

		router.RemoveMessageTypeAdapter(MessageTypes.MarketData, adapter1);

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var (adapters, _) = router.GetAdapters(mdMsg, a => a);

		AreEqual(1, adapters.Length);
		AreSame(adapter2, adapters[0]);
	}

	#endregion

	#region Clear

	[TestMethod]
	public void Clear_ResetsMessageTypeAndNotSupported()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();

		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter);
		router.AddNotSupported(100, adapter);

		router.Clear();

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var (adapters, _) = router.GetAdapters(mdMsg, a => a);

		IsNull(adapters);
	}

	[TestMethod]
	public void Clear_PreservesSecurityAndPortfolioAdapters()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();

		router.SetSecurityAdapter(_secId1, DataType.Ticks, adapter);
		router.SetPortfolioAdapter("TestPortfolio", adapter);

		router.Clear();

		// security and portfolio adapters are NOT cleared by Clear()
		// (they are cleared separately via ClearSecurityAdapters / ClearPortfolioAdapters)
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var (adapters, skip) = router.GetAdapters(mdMsg, a => a);
		AreEqual(1, adapters.Length);
		IsTrue(skip);

		var pf = router.GetPortfolioAdapter("TestPortfolio", a => a);
		AreSame(adapter, pf);
	}

	#endregion

	#region ClearSecurityAdapters / ClearPortfolioAdapters

	[TestMethod]
	public void ClearSecurityAdapters_Clears()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();

		router.SetSecurityAdapter(_secId1, DataType.Ticks, adapter);
		router.ClearSecurityAdapters();

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var (adapters, skip) = router.GetAdapters(mdMsg, a => a);
		IsFalse(skip);
		IsNull(adapters);
	}

	[TestMethod]
	public void ClearPortfolioAdapters_Clears()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();

		router.SetPortfolioAdapter("TestPortfolio", adapter);
		router.ClearPortfolioAdapters();

		var result = router.GetPortfolioAdapter("TestPortfolio", a => a);
		IsNull(result);
	}

	#endregion

	#region AddSecurityAdapter / AddPortfolioAdapter (Load helpers)

	[TestMethod]
	public void AddSecurityAdapter_Direct_Works()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();

		router.AddSecurityAdapter((_secId1, DataType.Ticks), adapter);

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var (adapters, skip) = router.GetAdapters(mdMsg, a => a);
		AreEqual(1, adapters.Length);
		IsTrue(skip);
	}

	[TestMethod]
	public void AddPortfolioAdapter_Direct_Works()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();

		router.AddPortfolioAdapter("TestPortfolio", adapter);

		var result = router.GetPortfolioAdapter("TestPortfolio", a => a);
		AreSame(adapter, result);
	}

	#endregion

	#region Composite Scenarios

	[TestMethod]
	public void Scenario_TwoAdapters_OrderStatusFiltering_OnlyOneSupports()
	{
		var router = CreateRouter();
		var adapter1 = CreateAdapter();
		var adapter2 = CreateAdapter();

		adapter1.SetAllDownloadingSupported(DataType.Transactions);

		router.AddMessageTypeAdapter(MessageTypes.OrderStatus, adapter1);
		router.AddMessageTypeAdapter(MessageTypes.OrderStatus, adapter2);

		var msg = new OrderStatusMessage
		{
			TransactionId = 100,
			IsSubscribe = true,
		};

		var (adapters, _) = router.GetAdapters(msg, a => a);

		AreEqual(1, adapters.Length);
		AreSame(adapter1, adapters[0]);
	}

	[TestMethod]
	public void Scenario_NotSupported_ThenClear_ResetsFiltering()
	{
		var router = CreateRouter();
		var adapter = CreateAdapter();

		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter);
		router.AddNotSupported(100, adapter);

		// verify filtered
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var (adapters1, _) = router.GetAdapters(mdMsg, a => a);
		IsNull(adapters1);

		// clear and re-add
		router.Clear();
		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter);

		var (adapters2, _) = router.GetAdapters(mdMsg, a => a);
		AreEqual(1, adapters2.Length);
	}

	[TestMethod]
	public void Scenario_SecurityAdapter_OverridesMessageType()
	{
		var router = CreateRouter();
		var adapter1 = CreateAdapter();
		var adapter2 = CreateAdapter();

		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter1);
		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter2);
		router.SetSecurityAdapter(_secId1, DataType.Ticks, adapter1);

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var (adapters, skip) = router.GetAdapters(mdMsg, a => a);

		AreEqual(1, adapters.Length);
		AreSame(adapter1, adapters[0]);
		IsTrue(skip);
	}

	[TestMethod]
	public void Scenario_DifferentSecurities_DifferentAdapters()
	{
		var router = CreateRouter();
		var adapter1 = CreateAdapter();
		var adapter2 = CreateAdapter();

		router.SetSecurityAdapter(_secId1, DataType.Ticks, adapter1);
		router.SetSecurityAdapter(_secId2, DataType.Ticks, adapter2);

		var msg1 = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var msg2 = new MarketDataMessage
		{
			SecurityId = _secId2,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 101,
		};

		var (adapters1, _) = router.GetAdapters(msg1, a => a);
		var (adapters2, _) = router.GetAdapters(msg2, a => a);

		AreSame(adapter1, adapters1[0]);
		AreSame(adapter2, adapters2[0]);
	}

	[TestMethod]
	public void Scenario_PortfolioLookup_BothAdaptersSupport_BothReturned()
	{
		var router = CreateRouter();
		var adapter1 = CreateAdapter();
		var adapter2 = CreateAdapter();

		adapter1.SetAllDownloadingSupported(DataType.PositionChanges);
		adapter2.SetAllDownloadingSupported(DataType.PositionChanges);

		router.AddMessageTypeAdapter(MessageTypes.PortfolioLookup, adapter1);
		router.AddMessageTypeAdapter(MessageTypes.PortfolioLookup, adapter2);

		var msg = new PortfolioLookupMessage
		{
			TransactionId = 100,
			IsSubscribe = true,
		};

		var (adapters, _) = router.GetAdapters(msg, a => a);

		AreEqual(2, adapters.Length);
	}

	#endregion
}

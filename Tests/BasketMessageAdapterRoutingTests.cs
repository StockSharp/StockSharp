namespace StockSharp.Tests;

using System.Collections.Concurrent;

using StockSharp.Algo.Basket;
using StockSharp.Algo.Candles.Compression;

/// <summary>
/// Tests for correct routing behavior in BasketMessageAdapter (Замечания 1-3 из PLAN.md).
/// These tests describe EXPECTED CORRECT behavior, not current behavior.
/// Some may initially fail, proving bugs exist.
/// </summary>
[TestClass]
public class BasketMessageAdapterRoutingTests : BaseTestClass
{
	#region Test Adapter

	private sealed class TestRoutingInnerAdapter : MessageAdapter
	{
		private readonly ConcurrentQueue<Message> _inMessages = new();
		private HashSet<DataType> _allDownloadingTypes = [];

		public TestRoutingInnerAdapter(IdGenerator idGen)
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

		public IReadOnlyList<Message> ReceivedMessages => [.. _inMessages];
		public IEnumerable<T> GetMessages<T>() where T : Message => _inMessages.OfType<T>();
		public bool AutoRespond { get; set; } = true;
		public bool RespondNotSupported { get; set; }

		/// <summary>
		/// Configure which data types support "download all" (IsAllDownloadingSupported).
		/// </summary>
		public void SetAllDownloadingSupported(params DataType[] types)
		{
			_allDownloadingTypes = [.. types];
		}

		public override bool IsAllDownloadingSupported(DataType dataType)
			=> _allDownloadingTypes.Contains(dataType);

		protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken ct)
		{
			_inMessages.Enqueue(message.TypedClone());

			if (!AutoRespond)
				return default;

			switch (message.Type)
			{
				case MessageTypes.Reset:
					SendOutMessage(new ResetMessage());
					break;
				case MessageTypes.Connect:
					SendOutMessage(new ConnectMessage());
					break;
				case MessageTypes.Disconnect:
					SendOutMessage(new DisconnectMessage());
					break;
				case MessageTypes.MarketData:
				{
					var md = (MarketDataMessage)message;
					if (RespondNotSupported)
					{
						SendOutMessage(md.TransactionId.CreateNotSupported());
					}
					else
					{
						SendOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = md.TransactionId });
						if (md.IsSubscribe)
							SendOutMessage(new SubscriptionOnlineMessage { OriginalTransactionId = md.TransactionId });
					}
					break;
				}
				case MessageTypes.SecurityLookup:
				{
					var sl = (SecurityLookupMessage)message;
					if (RespondNotSupported)
					{
						SendOutMessage(sl.TransactionId.CreateNotSupported());
					}
					else
					{
						SendOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = sl.TransactionId });
						SendOutMessage(new SubscriptionFinishedMessage { OriginalTransactionId = sl.TransactionId });
					}
					break;
				}
				case MessageTypes.PortfolioLookup:
				{
					var pl = (PortfolioLookupMessage)message;
					if (RespondNotSupported)
					{
						SendOutMessage(pl.TransactionId.CreateNotSupported());
					}
					else
					{
						SendOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = pl.TransactionId });
						SendOutMessage(new SubscriptionFinishedMessage { OriginalTransactionId = pl.TransactionId });
					}
					break;
				}
				case MessageTypes.OrderStatus:
				{
					var os = (OrderStatusMessage)message;
					if (RespondNotSupported)
					{
						SendOutMessage(os.TransactionId.CreateNotSupported());
					}
					else
					{
						SendOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = os.TransactionId });
						SendOutMessage(new SubscriptionOnlineMessage { OriginalTransactionId = os.TransactionId });
					}
					break;
				}
			}

			return default;
		}

		public void EmitOut(Message msg) => SendOutMessage(msg);

		public override IMessageAdapter Clone() => new TestRoutingInnerAdapter(TransactionIdGenerator);
	}

	#endregion

	#region Helpers

	private static readonly SecurityId _secId1 = "AAPL@NASDAQ".ToSecurityId();
	private ConcurrentQueue<Message> _outMessages;

	private (BasketMessageAdapter basket, TestRoutingInnerAdapter adapter1, TestRoutingInnerAdapter adapter2)
		CreateBasket(
			ISubscriptionRoutingState subscriptionRouting = null,
			IParentChildMap parentChildMap = null,
			bool twoAdapters = true)
	{
		var idGen = new IncrementalIdGenerator();

		var basket = new BasketMessageAdapter(
			idGen,
			new CandleBuilderProvider(new InMemoryExchangeInfoProvider()),
			new InMemorySecurityMessageAdapterProvider(),
			new InMemoryPortfolioMessageAdapterProvider(),
			null,
			null, null, null, null,
			subscriptionRouting,
			parentChildMap,
			null,
			null);

		basket.IgnoreExtraAdapters = true;
		basket.LatencyManager = null;
		basket.SlippageManager = null;
		basket.CommissionManager = null;

		var adapter1 = new TestRoutingInnerAdapter(idGen);
		basket.InnerAdapters.Add(adapter1);
		basket.ApplyHeartbeat(adapter1, false);

		TestRoutingInnerAdapter adapter2 = null;

		if (twoAdapters)
		{
			adapter2 = new TestRoutingInnerAdapter(idGen);
			basket.InnerAdapters.Add(adapter2);
			basket.ApplyHeartbeat(adapter2, false);
		}

		_outMessages = new ConcurrentQueue<Message>();
		basket.NewOutMessageAsync += async (msg, ct) =>
		{
			// Re-process loopback messages (simulates Connector behavior)
			if (msg.IsBack())
			{
				await ((IMessageTransport)basket).SendInMessageAsync(msg, ct);
				return;
			}

			_outMessages.Enqueue(msg);
		};

		return (basket, adapter1, adapter2);
	}

	private static async Task SendToBasket(BasketMessageAdapter basket, Message message, CancellationToken ct = default)
	{
		await ((IMessageTransport)basket).SendInMessageAsync(message, ct);
	}

	private T[] GetOut<T>() where T : Message
		=> [.. _outMessages.OfType<T>()];

	private void ClearOut() => _outMessages = new();

	private async Task ConnectBasket(BasketMessageAdapter basket, CancellationToken ct)
	{
		await SendToBasket(basket, new ConnectMessage(), ct);
		ClearOut();
	}

	#endregion

	#region Замечание 1: Единый путь MarketData — ParentChildMap для всех типов данных

	/// <summary>
	/// Замечание 1: Подписка на Ticks (не News/Board) должна использовать ToChild() и записывать маппинг в ParentChildMap.
	/// Текущее поведение: Ticks идёт через Path B — без ToChild, без ParentChildMap.
	/// Ожидаемое: ParentChildMap.AddMapping вызван для Ticks, как и для News.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark1_TicksSubscription_UsesParentChildMap()
	{
		var parentChildMap = new ParentChildMap();
		var subscriptionRouting = new SubscriptionRoutingState();

		var (basket, adapter1, _) = CreateBasket(
			subscriptionRouting: subscriptionRouting,
			parentChildMap: parentChildMap,
			twoAdapters: false);

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		};
		await SendToBasket(basket, mdMsg, TestContext.CancellationToken);

		// Ожидание: ParentChildMap содержит маппинг для этой подписки
		// Адаптер получил child-ID, отличный от parent transId
		var received = adapter1.GetMessages<MarketDataMessage>().ToArray();
		received.Length.AssertGreater(0, "Adapter should receive MarketDataMessage");

		var childTransId = received.First().TransactionId;

		// ParentChildMap должен знать о child → parent маппинге
		parentChildMap.TryGetParent(childTransId, out var parentId)
			.AssertTrue("ParentChildMap should have child→parent mapping for Ticks subscription");
		parentId.AssertEqual(transId, "Parent ID should match original transaction ID");
	}

	/// <summary>
	/// Замечание 1: Подписка на News использует ToChild() и ParentChildMap — baseline тест.
	/// Этот тест должен проходить и сейчас.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark1_NewsSubscription_UsesParentChildMap()
	{
		var parentChildMap = new ParentChildMap();
		var subscriptionRouting = new SubscriptionRoutingState();

		var (basket, adapter1, _) = CreateBasket(
			subscriptionRouting: subscriptionRouting,
			parentChildMap: parentChildMap,
			twoAdapters: false);

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			DataType2 = DataType.News,
			IsSubscribe = true,
			TransactionId = transId,
		};
		await SendToBasket(basket, mdMsg, TestContext.CancellationToken);

		// News уже идёт через ToChild() — ParentChildMap должен иметь маппинг
		var received = adapter1.GetMessages<MarketDataMessage>().ToArray();
		received.Length.AssertGreater(0, "Adapter should receive MarketDataMessage for News");

		var childTransId = received.First().TransactionId;

		parentChildMap.TryGetParent(childTransId, out var parentId)
			.AssertTrue("ParentChildMap should have child→parent mapping for News subscription");
		parentId.AssertEqual(transId, "Parent ID should match original transaction ID");
	}

	/// <summary>
	/// Замечание 1: Подписка на Level1 должна записывать маппинг в ParentChildMap.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark1_Level1Subscription_UsesParentChildMap()
	{
		var parentChildMap = new ParentChildMap();

		var (basket, adapter1, _) = CreateBasket(
			parentChildMap: parentChildMap,
			twoAdapters: false);

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Level1,
			IsSubscribe = true,
			TransactionId = transId,
		};
		await SendToBasket(basket, mdMsg, TestContext.CancellationToken);

		var received = adapter1.GetMessages<MarketDataMessage>().ToArray();
		received.Length.AssertGreater(0, "Adapter should receive MarketDataMessage for Level1");

		var childTransId = received.First().TransactionId;

		parentChildMap.TryGetParent(childTransId, out var parentId)
			.AssertTrue("ParentChildMap should have child→parent mapping for Level1 subscription");
		parentId.AssertEqual(transId, "Parent ID should match original transaction ID");
	}

	/// <summary>
	/// Замечание 1: Подписка на MarketDepth должна записывать маппинг в ParentChildMap.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark1_MarketDepthSubscription_UsesParentChildMap()
	{
		var parentChildMap = new ParentChildMap();

		var (basket, adapter1, _) = CreateBasket(
			parentChildMap: parentChildMap,
			twoAdapters: false);

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.MarketDepth,
			IsSubscribe = true,
			TransactionId = transId,
		};
		await SendToBasket(basket, mdMsg, TestContext.CancellationToken);

		var received = adapter1.GetMessages<MarketDataMessage>().ToArray();
		received.Length.AssertGreater(0, "Adapter should receive MarketDataMessage for MarketDepth");

		var childTransId = received.First().TransactionId;

		parentChildMap.TryGetParent(childTransId, out var parentId)
			.AssertTrue("ParentChildMap should have child→parent mapping for MarketDepth subscription");
		parentId.AssertEqual(transId, "Parent ID should match original transaction ID");
	}

	/// <summary>
	/// Замечание 1: Ответ SubscriptionResponse для Ticks должен ремапить childId → parentId.
	/// При едином пути через ToChild, ответ от адаптера приходит с child transId,
	/// а basket должен вернуть наружу parent transId.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark1_TicksResponse_RemapsChildToParentId()
	{
		var parentChildMap = new ParentChildMap();

		var (basket, adapter1, _) = CreateBasket(
			parentChildMap: parentChildMap,
			twoAdapters: false);

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		};
		await SendToBasket(basket, mdMsg, TestContext.CancellationToken);

		// Verify: SubscriptionResponse has parent transId (not child)
		var responses = GetOut<SubscriptionResponseMessage>();
		responses.Any(r => r.OriginalTransactionId == transId)
			.AssertTrue("SubscriptionResponse should have parent transId after remapping");

		var onlines = GetOut<SubscriptionOnlineMessage>();
		onlines.Any(r => r.OriginalTransactionId == transId)
			.AssertTrue("SubscriptionOnline should have parent transId after remapping");
	}

	#endregion

	#region Замечание 2: Фильтрация IsAllDownloadingSupported для OrderStatus и PortfolioLookup

	/// <summary>
	/// Замечание 2: SecurityLookup с IsLookupAll=true, адаптер поддерживает IsAllDownloading → доставлено.
	/// Baseline — уже работает.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark2_SecurityLookup_AllDownloadingSupported_Delivered()
	{
		var (basket, adapter1, _) = CreateBasket(twoAdapters: false);
		adapter1.SetAllDownloadingSupported(DataType.Securities);

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		// IsLookupAll: empty SecurityId, empty Name, etc.
		var lookupMsg = new SecurityLookupMessage { TransactionId = transId };
		await SendToBasket(basket, lookupMsg, TestContext.CancellationToken);

		adapter1.GetMessages<SecurityLookupMessage>().Any()
			.AssertTrue("Adapter supporting IsAllDownloading should receive SecurityLookup");
	}

	/// <summary>
	/// Замечание 2: SecurityLookup с IsLookupAll=true, адаптер НЕ поддерживает IsAllDownloading → отфильтровано.
	/// Baseline — уже работает.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark2_SecurityLookup_AllDownloadingNotSupported_Filtered()
	{
		var (basket, adapter1, _) = CreateBasket(twoAdapters: false);
		// adapter1 does NOT support IsAllDownloading for Securities (default)

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var lookupMsg = new SecurityLookupMessage { TransactionId = transId };
		await SendToBasket(basket, lookupMsg, TestContext.CancellationToken);

		adapter1.GetMessages<SecurityLookupMessage>().Any()
			.AssertFalse("Adapter NOT supporting IsAllDownloading should NOT receive SecurityLookup with IsLookupAll");
	}

	/// <summary>
	/// Замечание 2: SecurityLookup с конкретным SecurityId, адаптер НЕ поддерживает IsAllDownloading → доставлено
	/// (фильтр IsAllDownloading применяется только для "lookup all").
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark2_SecurityLookup_SpecificSecurity_Delivered()
	{
		var (basket, adapter1, _) = CreateBasket(twoAdapters: false);
		// NOT supporting IsAllDownloading

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var lookupMsg = new SecurityLookupMessage
		{
			TransactionId = transId,
			SecurityId = _secId1,
		};
		await SendToBasket(basket, lookupMsg, TestContext.CancellationToken);

		adapter1.GetMessages<SecurityLookupMessage>().Any()
			.AssertTrue("Adapter should receive SecurityLookup for specific security regardless of IsAllDownloading");
	}

	/// <summary>
	/// Замечание 2: OrderStatus без критериев (все ордера), адаптер поддерживает IsAllDownloading → доставлено.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark2_OrderStatus_AllDownloadingSupported_Delivered()
	{
		var (basket, adapter1, _) = CreateBasket(twoAdapters: false);
		adapter1.SetAllDownloadingSupported(DataType.Transactions);

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var osMsg = new OrderStatusMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
			// No SecurityId filter — "give me all orders"
		};
		await SendToBasket(basket, osMsg, TestContext.CancellationToken);

		adapter1.GetMessages<OrderStatusMessage>().Any()
			.AssertTrue("Adapter supporting IsAllDownloading(Transactions) should receive OrderStatus");
	}

	/// <summary>
	/// Замечание 2: OrderStatus без критериев, адаптер НЕ поддерживает IsAllDownloading → отфильтровано.
	/// BUG: В текущей реализации фильтр IsAllDownloading не проверяется для OrderStatus.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark2_OrderStatus_AllDownloadingNotSupported_Filtered()
	{
		var (basket, adapter1, _) = CreateBasket(twoAdapters: false);
		// adapter1 does NOT support IsAllDownloading for Transactions (default)

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var osMsg = new OrderStatusMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
			// No specific SecurityId — requesting all orders
		};
		await SendToBasket(basket, osMsg, TestContext.CancellationToken);

		adapter1.GetMessages<OrderStatusMessage>().Any()
			.AssertFalse("Adapter NOT supporting IsAllDownloading(Transactions) should NOT receive OrderStatus for all orders");
	}

	/// <summary>
	/// Замечание 2: OrderStatus с конкретным SecurityId, адаптер НЕ поддерживает IsAllDownloading → доставлено.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark2_OrderStatus_SpecificSecurity_Delivered()
	{
		var (basket, adapter1, _) = CreateBasket(twoAdapters: false);
		// NOT supporting IsAllDownloading

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var osMsg = new OrderStatusMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
			SecurityId = _secId1,
		};
		await SendToBasket(basket, osMsg, TestContext.CancellationToken);

		adapter1.GetMessages<OrderStatusMessage>().Any()
			.AssertTrue("Adapter should receive OrderStatus for specific security regardless of IsAllDownloading");
	}

	/// <summary>
	/// Замечание 2: PortfolioLookup без критериев, адаптер поддерживает IsAllDownloading → доставлено.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark2_PortfolioLookup_AllDownloadingSupported_Delivered()
	{
		var (basket, adapter1, _) = CreateBasket(twoAdapters: false);
		adapter1.SetAllDownloadingSupported(DataType.PositionChanges);

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var plMsg = new PortfolioLookupMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
		};
		await SendToBasket(basket, plMsg, TestContext.CancellationToken);

		adapter1.GetMessages<PortfolioLookupMessage>().Any()
			.AssertTrue("Adapter supporting IsAllDownloading(PositionChanges) should receive PortfolioLookup");
	}

	/// <summary>
	/// Замечание 2: PortfolioLookup без критериев, адаптер НЕ поддерживает IsAllDownloading → отфильтровано.
	/// BUG: В текущей реализации фильтр IsAllDownloading не проверяется для PortfolioLookup.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark2_PortfolioLookup_AllDownloadingNotSupported_Filtered()
	{
		var (basket, adapter1, _) = CreateBasket(twoAdapters: false);
		// NOT supporting IsAllDownloading for PositionChanges

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var plMsg = new PortfolioLookupMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
		};
		await SendToBasket(basket, plMsg, TestContext.CancellationToken);

		adapter1.GetMessages<PortfolioLookupMessage>().Any()
			.AssertFalse("Adapter NOT supporting IsAllDownloading(PositionChanges) should NOT receive PortfolioLookup for all");
	}

	/// <summary>
	/// Замечание 2: PortfolioLookup с конкретным PortfolioName, адаптер НЕ поддерживает IsAllDownloading → доставлено.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark2_PortfolioLookup_SpecificPortfolio_Delivered()
	{
		var (basket, adapter1, _) = CreateBasket(twoAdapters: false);
		// NOT supporting IsAllDownloading

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var plMsg = new PortfolioLookupMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
			PortfolioName = "Portfolio1",
		};
		await SendToBasket(basket, plMsg, TestContext.CancellationToken);

		adapter1.GetMessages<PortfolioLookupMessage>().Any()
			.AssertTrue("Adapter should receive PortfolioLookup for specific portfolio regardless of IsAllDownloading");
	}

	/// <summary>
	/// Замечание 2: Два адаптера, один поддерживает IsAllDownloading, другой нет.
	/// OrderStatus без критериев → только к поддерживающему.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark2_OrderStatus_TwoAdapters_OnlySupporting_Receives()
	{
		var (basket, adapter1, adapter2) = CreateBasket(twoAdapters: true);
		adapter1.SetAllDownloadingSupported(DataType.Transactions);
		// adapter2 does NOT support IsAllDownloading for Transactions

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var osMsg = new OrderStatusMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
		};
		await SendToBasket(basket, osMsg, TestContext.CancellationToken);

		adapter1.GetMessages<OrderStatusMessage>().Any()
			.AssertTrue("Adapter1 (supporting) should receive OrderStatus");
		adapter2.GetMessages<OrderStatusMessage>().Any()
			.AssertFalse("Adapter2 (not supporting) should NOT receive OrderStatus");
	}

	/// <summary>
	/// Замечание 2: Два адаптера, оба поддерживают IsAllDownloading.
	/// OrderStatus без критериев → оба получают.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark2_OrderStatus_TwoAdapters_BothSupporting_BothReceive()
	{
		var (basket, adapter1, adapter2) = CreateBasket(twoAdapters: true);
		adapter1.SetAllDownloadingSupported(DataType.Transactions);
		adapter2.SetAllDownloadingSupported(DataType.Transactions);

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var osMsg = new OrderStatusMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
		};
		await SendToBasket(basket, osMsg, TestContext.CancellationToken);

		adapter1.GetMessages<OrderStatusMessage>().Any()
			.AssertTrue("Adapter1 should receive OrderStatus");
		adapter2.GetMessages<OrderStatusMessage>().Any()
			.AssertTrue("Adapter2 should receive OrderStatus");
	}

	/// <summary>
	/// Замечание 2: Два адаптера, один поддерживает IsAllDownloading, другой нет.
	/// PortfolioLookup без критериев → только к поддерживающему.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark2_PortfolioLookup_TwoAdapters_OnlySupporting_Receives()
	{
		var (basket, adapter1, adapter2) = CreateBasket(twoAdapters: true);
		adapter1.SetAllDownloadingSupported(DataType.PositionChanges);
		// adapter2 does NOT support

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var plMsg = new PortfolioLookupMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
		};
		await SendToBasket(basket, plMsg, TestContext.CancellationToken);

		adapter1.GetMessages<PortfolioLookupMessage>().Any()
			.AssertTrue("Adapter1 (supporting) should receive PortfolioLookup");
		adapter2.GetMessages<PortfolioLookupMessage>().Any()
			.AssertFalse("Adapter2 (not supporting) should NOT receive PortfolioLookup");
	}

	#endregion

	#region Замечание 3: NotSupported retry для не-MarketData подписок

	/// <summary>
	/// Замечание 3: SecurityLookup → NotSupported от одного адаптера → retry → второй адаптер получает запрос.
	/// BUG: В текущей реализации _nonSupportedAdapters фильтруется только для MessageTypes.MarketData,
	/// поэтому retry для SecurityLookup зацикливается или не работает.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark3_SecurityLookup_NotSupported_RetriesToNextAdapter()
	{
		var subscriptionRouting = new SubscriptionRoutingState();
		var parentChildMap = new ParentChildMap();

		var (basket, adapter1, adapter2) = CreateBasket(
			subscriptionRouting: subscriptionRouting,
			parentChildMap: parentChildMap,
			twoAdapters: true);

		// Both support IsAllDownloading for Securities
		adapter1.SetAllDownloadingSupported(DataType.Securities);
		adapter2.SetAllDownloadingSupported(DataType.Securities);

		// adapter1 will respond with NotSupported
		adapter1.RespondNotSupported = true;
		// adapter2 responds normally
		adapter2.RespondNotSupported = false;

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var lookupMsg = new SecurityLookupMessage
		{
			TransactionId = transId,
		};
		await SendToBasket(basket, lookupMsg, TestContext.CancellationToken);

		// adapter2 should have received the SecurityLookup (after adapter1 returned NotSupported)
		adapter2.GetMessages<SecurityLookupMessage>().Any()
			.AssertTrue("Adapter2 should receive SecurityLookup after adapter1 returned NotSupported");

		// The response should eventually arrive at the output with parent transId
		// Either SubscriptionFinishedMessage (success from adapter2) or aggregated error
		var finished = GetOut<SubscriptionFinishedMessage>();
		var responses = GetOut<SubscriptionResponseMessage>();

		// At least some successful outcome should be present
		(finished.Any(f => f.OriginalTransactionId == transId) ||
		 responses.Any(r => r.OriginalTransactionId == transId && r.Error == null))
			.AssertTrue("Basket should eventually deliver successful response from adapter2");
	}

	/// <summary>
	/// Замечание 3: OrderStatus → NotSupported → не должен зацикливаться.
	/// Если _nonSupportedAdapters не фильтруется для OrderStatus — loopback будет отправлять
	/// на тот же адаптер снова и снова.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark3_OrderStatus_NotSupported_DoesNotLoop()
	{
		var (basket, adapter1, _) = CreateBasket(twoAdapters: false);
		adapter1.SetAllDownloadingSupported(DataType.Transactions);
		adapter1.RespondNotSupported = true;

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var osMsg = new OrderStatusMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
		};
		await SendToBasket(basket, osMsg, TestContext.CancellationToken);

		// With only 1 adapter that returns NotSupported, after retry the adapter should be filtered out.
		// The result should be a NotSupported or error response, NOT an infinite loop.
		// Count how many times adapter1 received OrderStatusMessage — should be at most 2 (original + 1 retry)
		var received = adapter1.GetMessages<OrderStatusMessage>().Count();
		(received <= 2).AssertTrue("Adapter should receive OrderStatus at most twice (original + retry), not loop infinitely");

		// Output should contain some response indicating failure
		var responses = GetOut<SubscriptionResponseMessage>();
		responses.Any(r => r.OriginalTransactionId == transId)
			.AssertTrue("Basket should emit response for failed OrderStatus subscription");
	}

	/// <summary>
	/// Замечание 3: MarketData → NotSupported → retry → фильтрация работает (baseline).
	/// Этот тест подтверждает что для MarketData текущий механизм работает.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark3_MarketData_NotSupported_RetryWorks()
	{
		var subscriptionRouting = new SubscriptionRoutingState();

		var (basket, adapter1, adapter2) = CreateBasket(
			subscriptionRouting: subscriptionRouting,
			twoAdapters: true);

		// adapter1 returns NotSupported
		adapter1.RespondNotSupported = true;
		adapter2.RespondNotSupported = false;

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		};
		await SendToBasket(basket, mdMsg, TestContext.CancellationToken);

		// adapter2 should receive the MarketData request after adapter1 returned NotSupported
		adapter2.GetMessages<MarketDataMessage>().Any()
			.AssertTrue("Adapter2 should receive MarketData after adapter1 returned NotSupported");

		// Successful response should come through
		GetOut<SubscriptionOnlineMessage>().Any(m => m.OriginalTransactionId == transId)
			.AssertTrue("Basket should emit SubscriptionOnline with parent transId after successful retry");
	}

	/// <summary>
	/// Замечание 3: PortfolioLookup → NotSupported → не должен зацикливаться.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark3_PortfolioLookup_NotSupported_DoesNotLoop()
	{
		var (basket, adapter1, _) = CreateBasket(twoAdapters: false);
		adapter1.SetAllDownloadingSupported(DataType.PositionChanges);
		adapter1.RespondNotSupported = true;

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var plMsg = new PortfolioLookupMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
		};
		await SendToBasket(basket, plMsg, TestContext.CancellationToken);

		// Should not loop: adapter should receive at most 2 messages
		var received = adapter1.GetMessages<PortfolioLookupMessage>().Count();
		(received <= 2).AssertTrue("Adapter should receive PortfolioLookup at most twice, not loop");

		// Output should contain some response
		var responses = GetOut<SubscriptionResponseMessage>();
		responses.Any(r => r.OriginalTransactionId == transId)
			.AssertTrue("Basket should emit response for failed PortfolioLookup subscription");
	}

	#endregion
}

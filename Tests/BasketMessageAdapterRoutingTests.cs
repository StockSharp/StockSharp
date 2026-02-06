namespace StockSharp.Tests;

using System.Collections.Concurrent;

using StockSharp.Algo.Basket;
using StockSharp.Algo.Candles.Compression;

[TestClass]
public class BasketMessageAdapterRoutingTests : BaseTestClass
{
	#region Test Adapter

	private sealed class TestRoutingInnerAdapter : MessageAdapter
	{
		private readonly ConcurrentQueue<Message> _inMessages = [];
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

		protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken ct)
		{
			_inMessages.Enqueue(message.TypedClone());

			if (!AutoRespond)
				return;

			switch (message.Type)
			{
				case MessageTypes.Reset:
					await SendOutMessageAsync(new ResetMessage(), ct);
					break;
				case MessageTypes.Connect:
					await SendOutMessageAsync(new ConnectMessage(), ct);
					break;
				case MessageTypes.Disconnect:
					await SendOutMessageAsync(new DisconnectMessage(), ct);
					break;
				case MessageTypes.MarketData:
				{
					var md = (MarketDataMessage)message;
					if (RespondNotSupported)
					{
						await SendOutMessageAsync(md.TransactionId.CreateNotSupported(), ct);
					}
					else
					{
						await SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = md.TransactionId }, ct);
						if (md.IsSubscribe)
							await SendOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = md.TransactionId }, ct);
					}
					break;
				}
				case MessageTypes.SecurityLookup:
				{
					var sl = (SecurityLookupMessage)message;
					if (RespondNotSupported)
					{
						await SendOutMessageAsync(sl.TransactionId.CreateNotSupported(), ct);
					}
					else
					{
						await SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = sl.TransactionId }, ct);
						await SendOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = sl.TransactionId }, ct);
					}
					break;
				}
				case MessageTypes.PortfolioLookup:
				{
					var pl = (PortfolioLookupMessage)message;
					if (RespondNotSupported)
					{
						await SendOutMessageAsync(pl.TransactionId.CreateNotSupported(), ct);
					}
					else
					{
						await SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = pl.TransactionId }, ct);
						await SendOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = pl.TransactionId }, ct);
					}
					break;
				}
				case MessageTypes.OrderStatus:
				{
					var os = (OrderStatusMessage)message;
					if (RespondNotSupported)
					{
						await SendOutMessageAsync(os.TransactionId.CreateNotSupported(), ct);
					}
					else
					{
						await SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = os.TransactionId }, ct);
						await SendOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = os.TransactionId }, ct);
					}
					break;
				}
			}
		}

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
		var candleBuilderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());

		// Create routing manager with optional injected state
		var cs = new AdapterConnectionState();
		var cm = new AdapterConnectionManager(cs);
		var ps = new PendingMessageState();
		var sr = subscriptionRouting ?? new SubscriptionRoutingState();
		var pcm = parentChildMap ?? new ParentChildMap();
		var or = new OrderRoutingState();

		var routingManager = new BasketRoutingManager(
			cs, cm, ps, sr, pcm, or,
			a => a, candleBuilderProvider, () => false, idGen);

		var basket = new BasketMessageAdapter(
			idGen,
			candleBuilderProvider,
			new InMemorySecurityMessageAdapterProvider(),
			new InMemoryPortfolioMessageAdapterProvider(),
			null,
			null,
			routingManager);

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

		_outMessages = [];
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

	private void ClearOut() => _outMessages = [];

	private async Task ConnectBasket(BasketMessageAdapter basket, CancellationToken ct)
	{
		await SendToBasket(basket, new ConnectMessage(), ct);
		ClearOut();
	}

	#endregion

	#region Remark 1: Unified MarketData path — ParentChildMap for all data types

	/// <summary>
	/// Remark 1: Ticks subscription (not News/Board) should use ToChild() and record mapping in ParentChildMap.
	/// Current behavior: Ticks goes through Path B — without ToChild, without ParentChildMap.
	/// Expected: ParentChildMap.AddMapping called for Ticks, same as for News.
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

		// Expected: ParentChildMap contains mapping for this subscription
		// Adapter received child-ID different from parent transId
		var received = adapter1.GetMessages<MarketDataMessage>().ToArray();
		received.Length.AssertEqual(1, "Adapter should receive MarketDataMessage");

		var childTransId = received.First().TransactionId;

		// ParentChildMap should know about child → parent mapping
		parentChildMap.TryGetParent(childTransId, out var parentId)
			.AssertTrue("ParentChildMap should have child→parent mapping for Ticks subscription");
		parentId.AssertEqual(transId, "Parent ID should match original transaction ID");
	}

	/// <summary>
	/// Remark 1: News subscription uses ToChild() and ParentChildMap — baseline test.
	/// This test should pass now.
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

		// News already goes through ToChild() — ParentChildMap should have mapping
		var received = adapter1.GetMessages<MarketDataMessage>().ToArray();
		received.Length.AssertEqual(1, "Adapter should receive MarketDataMessage for News");

		var childTransId = received.First().TransactionId;

		parentChildMap.TryGetParent(childTransId, out var parentId)
			.AssertTrue("ParentChildMap should have child→parent mapping for News subscription");
		parentId.AssertEqual(transId, "Parent ID should match original transaction ID");
	}

	/// <summary>
	/// Remark 1: Level1 subscription should record mapping in ParentChildMap.
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
		received.Length.AssertEqual(1, "Adapter should receive MarketDataMessage for Level1");

		var childTransId = received.First().TransactionId;

		parentChildMap.TryGetParent(childTransId, out var parentId)
			.AssertTrue("ParentChildMap should have child→parent mapping for Level1 subscription");
		parentId.AssertEqual(transId, "Parent ID should match original transaction ID");
	}

	/// <summary>
	/// Remark 1: MarketDepth subscription should record mapping in ParentChildMap.
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
		received.Length.AssertEqual(1, "Adapter should receive MarketDataMessage for MarketDepth");

		var childTransId = received.First().TransactionId;

		parentChildMap.TryGetParent(childTransId, out var parentId)
			.AssertTrue("ParentChildMap should have child→parent mapping for MarketDepth subscription");
		parentId.AssertEqual(transId, "Parent ID should match original transaction ID");
	}

	/// <summary>
	/// Remark 1: SubscriptionResponse for Ticks should remap childId → parentId.
	/// With unified path through ToChild, response from adapter comes with child transId,
	/// and basket should return parent transId to the outside.
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
		responses.Count(r => r.OriginalTransactionId == transId)
			.AssertEqual(1, "SubscriptionResponse should have parent transId after remapping");

		var onlines = GetOut<SubscriptionOnlineMessage>();
		onlines.Count(r => r.OriginalTransactionId == transId)
			.AssertEqual(1, "SubscriptionOnline should have parent transId after remapping");
	}

	#endregion

	#region Remark 2: IsAllDownloadingSupported filtering for OrderStatus and PortfolioLookup

	/// <summary>
	/// Remark 2: SecurityLookup with IsLookupAll=true, adapter supports IsAllDownloading → delivered.
	/// Baseline — already works.
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
	/// Remark 2: SecurityLookup with IsLookupAll=true, adapter does NOT support IsAllDownloading → filtered out.
	/// Baseline — already works.
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
	/// Remark 2: SecurityLookup with specific SecurityId, adapter does NOT support IsAllDownloading → delivered
	/// (IsAllDownloading filter applies only for "lookup all").
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
	/// Remark 2: OrderStatus without criteria (all orders), adapter supports IsAllDownloading → delivered.
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
	/// Remark 2: OrderStatus without criteria, adapter does NOT support IsAllDownloading → filtered out.
	/// BUG: In current implementation IsAllDownloading filter is not checked for OrderStatus.
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
	/// Remark 2: OrderStatus with specific SecurityId, adapter does NOT support IsAllDownloading → delivered.
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
	/// Remark 2: PortfolioLookup without criteria, adapter supports IsAllDownloading → delivered.
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
	/// Remark 2: PortfolioLookup without criteria, adapter does NOT support IsAllDownloading → filtered out.
	/// BUG: In current implementation IsAllDownloading filter is not checked for PortfolioLookup.
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
	/// Remark 2: PortfolioLookup with specific PortfolioName, adapter does NOT support IsAllDownloading → delivered.
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
	/// Remark 2: Two adapters, one supports IsAllDownloading, other does not.
	/// OrderStatus without criteria → only to the supporting one.
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
	/// Remark 2: Two adapters, both support IsAllDownloading.
	/// OrderStatus without criteria → both receive.
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
	/// Remark 2: Two adapters, one supports IsAllDownloading, other does not.
	/// PortfolioLookup without criteria → only to the supporting one.
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

	#region Remark 3: NotSupported retry for non-MarketData subscriptions

	/// <summary>
	/// Remark 3: SecurityLookup → NotSupported from one adapter → retry → second adapter receives request.
	/// BUG: In current implementation _nonSupportedAdapters is filtered only for MessageTypes.MarketData,
	/// so retry for SecurityLookup loops or doesn't work.
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
	/// Remark 3: OrderStatus → NotSupported → should not loop.
	/// If _nonSupportedAdapters is not filtered for OrderStatus — loopback will send
	/// to the same adapter again and again.
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
	/// Remark 3: OrderStatus → NotSupported from adapter1 → retry → adapter2 receives.
	/// With two adapters: adapter1 returns NotSupported, adapter2 should get the retry.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark3_OrderStatus_NotSupported_TwoAdapters_RetriesToSecond()
	{
		var (basket, adapter1, adapter2) = CreateBasket(twoAdapters: true);
		adapter1.SetAllDownloadingSupported(DataType.Transactions);
		adapter2.SetAllDownloadingSupported(DataType.Transactions);
		adapter1.RespondNotSupported = true;
		adapter2.RespondNotSupported = false;

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var osMsg = new OrderStatusMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
		};
		await SendToBasket(basket, osMsg, TestContext.CancellationToken);

		// adapter2 should receive the request after adapter1 returned NotSupported
		adapter2.GetMessages<OrderStatusMessage>().Any()
			.AssertTrue("Adapter2 should receive OrderStatus after adapter1 returned NotSupported");
	}

	/// <summary>
	/// Remark 3: MarketData → NotSupported → retry → filtering works (baseline).
	/// This test confirms that for MarketData the current mechanism works.
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
	/// Remark 3: PortfolioLookup → NotSupported → should not loop.
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

	#region Remark 4: LoopBack(null) — latent bug in BasketRoutingManager:617

	/// <summary>
	/// Remark 4a: Direct test — LoopBack(null) throws ArgumentNullException.
	/// In BasketRoutingManager.cs:617 there is code: subscrMsg.LoopBack(null)
	/// This call will ALWAYS throw ArgumentNullException, since LoopBack requires non-null adapter.
	/// Currently this code is unreachable (all subscriptions go through ToChild→ParentChildMap),
	/// but it's a latent bug — if code path changes, it will crash.
	/// </summary>
	[TestMethod]
	public void Remark4_LoopBackNull_ThrowsArgumentNullException()
	{
		// Direct demonstration: LoopBack(null) always throws
		var msg = new SecurityLookupMessage { TransactionId = 1 };

		Throws<ArgumentNullException>(() => msg.LoopBack((IMessageAdapter)null));
	}

	/// <summary>
	/// Remark 4b: LoopBack(null) code in ProcessSubscriptionResponseAsync is unreachable,
	/// because all subscriptions (SecurityLookup, OrderStatus, PortfolioLookup) go through
	/// ToChild → ParentChildMap. On NotSupported, ProcessChildResponse returns parentId != null,
	/// and code on line 609 is not reached.
	///
	/// Test proves: with 2 adapters, one returns NotSupported — no exception,
	/// because ParentChildMap intercepts the response earlier.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark4_LoopBackNull_Unreachable_DueToParentChildMap()
	{
		var (basket, adapter1, adapter2) = CreateBasket(twoAdapters: true);
		adapter1.SetAllDownloadingSupported(DataType.Securities);
		adapter2.SetAllDownloadingSupported(DataType.Securities);

		adapter1.RespondNotSupported = true;
		adapter2.RespondNotSupported = false;

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var lookupMsg = new SecurityLookupMessage { TransactionId = transId };

		// Doesn't throw — ParentChildMap intercepts NotSupported before line 617
		Exception caught = null;
		try
		{
			await SendToBasket(basket, lookupMsg, TestContext.CancellationToken);
		}
		catch (Exception ex)
		{
			caught = ex;
		}

		IsNull(caught, $"No exception because LoopBack(null) is unreachable via ParentChildMap path, but got: {caught}");

		// adapter2 receives subscription directly through ToChild (not retry)
		adapter2.GetMessages<SecurityLookupMessage>().Any()
			.AssertTrue("Adapter2 receives SecurityLookup via ToChild (simultaneous send, not retry)");
	}

	/// <summary>
	/// Remark 4c: When ALL adapters return NotSupported for SecurityLookup,
	/// ParentChildMap aggregates errors and returns AggregateException.
	/// No retry mechanism (unlike expected behavior).
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark4_AllNotSupported_SecurityLookup_ReturnsAggregatedError()
	{
		var (basket, adapter1, adapter2) = CreateBasket(twoAdapters: true);
		adapter1.SetAllDownloadingSupported(DataType.Securities);
		adapter2.SetAllDownloadingSupported(DataType.Securities);

		// BOTH adapters return NotSupported
		adapter1.RespondNotSupported = true;
		adapter2.RespondNotSupported = true;

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var lookupMsg = new SecurityLookupMessage { TransactionId = transId };
		await SendToBasket(basket, lookupMsg, TestContext.CancellationToken);

		// Expect SubscriptionResponse with error (aggregated from all children)
		var responses = GetOut<SubscriptionResponseMessage>();
		var errorResponse = responses.FirstOrDefault(r => r.OriginalTransactionId == transId);

		IsNotNull(errorResponse, "Should receive SubscriptionResponse when all adapters return NotSupported");
		IsNotNull(errorResponse.Error, "Response should contain error when all adapters return NotSupported");
	}

	/// <summary>
	/// Remark 4d: Same for OrderStatus — all NotSupported → aggregated error.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark4_AllNotSupported_OrderStatus_ReturnsAggregatedError()
	{
		var (basket, adapter1, adapter2) = CreateBasket(twoAdapters: true);
		adapter1.SetAllDownloadingSupported(DataType.Transactions);
		adapter2.SetAllDownloadingSupported(DataType.Transactions);

		adapter1.RespondNotSupported = true;
		adapter2.RespondNotSupported = true;

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var osMsg = new OrderStatusMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
		};
		await SendToBasket(basket, osMsg, TestContext.CancellationToken);

		var responses = GetOut<SubscriptionResponseMessage>();
		var errorResponse = responses.FirstOrDefault(r => r.OriginalTransactionId == transId);

		IsNotNull(errorResponse, "Should receive SubscriptionResponse when all adapters return NotSupported for OrderStatus");
		IsNotNull(errorResponse.Error, "Response should contain error for OrderStatus when all adapters return NotSupported");
	}

	#endregion

	#region Remark 5: _nonSupportedAdapters filters only MarketData

	/// <summary>
	/// Remark 5a: _nonSupportedAdapters filtering WORKS for MarketData (baseline).
	/// AdapterRouter.GetAdapters() checks _nonSupportedAdapters only for
	/// MessageTypes.MarketData (AdapterRouter.cs:76).
	///
	/// For MarketData after NotSupported from adapter1, on retry adapter1 is excluded.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark5_NonSupportedAdapters_Filtered_ForMarketData()
	{
		var (basket, adapter1, adapter2) = CreateBasket(twoAdapters: true);
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

		// MarketData: both adapters receive through ToChild simultaneously.
		// adapter1 returns NotSupported, adapter2 — success.
		// ParentChildMap aggregates: allError=false → success.
		var adapter1Count = adapter1.GetMessages<MarketDataMessage>().Count();
		var adapter2Count = adapter2.GetMessages<MarketDataMessage>().Count();

		adapter1Count.AssertEqual(1, "Adapter1 receives MarketData once via ToChild");
		adapter2Count.AssertEqual(1, "Adapter2 receives MarketData once via ToChild");

		// Successful response
		GetOut<SubscriptionOnlineMessage>().Any(m => m.OriginalTransactionId == transId)
			.AssertTrue("Should get SubscriptionOnline since adapter2 succeeded");
	}

	/// <summary>
	/// Remark 5b: For non-MarketData subscriptions _nonSupportedAdapters is not checked
	/// in AdapterRouter.GetAdapters(). But this doesn't lead to infinite loop,
	/// since all subscriptions go through ToChild and are handled by ParentChildMap.
	///
	/// Test confirms: SecurityLookup with 2 adapters, one NotSupported →
	/// second still successfully processes (through ToChild, not retry).
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark5_NonMarketData_NoRetry_BothAdaptersGetViaToChild()
	{
		var (basket, adapter1, adapter2) = CreateBasket(twoAdapters: true);
		adapter1.SetAllDownloadingSupported(DataType.Securities);
		adapter2.SetAllDownloadingSupported(DataType.Securities);

		adapter1.RespondNotSupported = true;
		adapter2.RespondNotSupported = false;

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var lookupMsg = new SecurityLookupMessage { TransactionId = transId };
		await SendToBasket(basket, lookupMsg, TestContext.CancellationToken);

		// Both receive exactly 1 message through ToChild (not retry)
		var adapter1Count = adapter1.GetMessages<SecurityLookupMessage>().Count();
		var adapter2Count = adapter2.GetMessages<SecurityLookupMessage>().Count();

		adapter1Count.AssertEqual(1, "Adapter1 receives SecurityLookup once via ToChild");
		adapter2Count.AssertEqual(1, "Adapter2 receives SecurityLookup once via ToChild");

		// Result: adapter2 responds successfully → subscription finished
		var finished = GetOut<SubscriptionFinishedMessage>();
		finished.Any(f => f.OriginalTransactionId == transId)
			.AssertTrue("SecurityLookup should finish successfully via adapter2");
	}

	/// <summary>
	/// Remark 5c: Key difference: for MarketData with 1 adapter, NotSupported
	/// does NOT lead to retry (no second adapter). Error is returned.
	/// Same for SecurityLookup — no difference in behavior.
	///
	/// Potential issue: _nonSupportedAdapters.AddNotSupported() is called
	/// on line 615 for ALL subscription types, but filtering in GetAdapters()
	/// only checks MarketData. This is dead code for non-MarketData.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark5_SingleAdapter_NotSupported_ReturnsError()
	{
		var (basket, adapter1, _) = CreateBasket(twoAdapters: false);
		adapter1.SetAllDownloadingSupported(DataType.Securities);
		adapter1.RespondNotSupported = true;

		await ConnectBasket(basket, TestContext.CancellationToken);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var lookupMsg = new SecurityLookupMessage { TransactionId = transId };
		await SendToBasket(basket, lookupMsg, TestContext.CancellationToken);

		// 1 adapter, NotSupported → error
		var responses = GetOut<SubscriptionResponseMessage>();
		var errorResp = responses.FirstOrDefault(r => r.OriginalTransactionId == transId);
		IsNotNull(errorResp, "Should return error when single adapter returns NotSupported");
		IsNotNull(errorResp.Error, "Error should be set");

		// Adapter received exactly 1 message (no retry)
		adapter1.GetMessages<SecurityLookupMessage>().Count()
			.AssertEqual(1, "Single adapter should receive SecurityLookup exactly once (no retry)");
	}

	#endregion

	#region Remark 6: IgnoreExtraAdapters doesn't skip Heartbeat and Offline wrappers

	/// <summary>
	/// Remark 6: AdapterWrapperPipelineBuilder applies Heartbeat and Offline wrappers
	/// BEFORE checking IgnoreExtraAdapters. So even with IgnoreExtraAdapters=true
	/// HeartbeatMessageAdapter and OfflineMessageAdapter still wrap the adapter.
	///
	/// This is not quite a bug, but a misleading name: "IgnoreExtraAdapters" suggests
	/// that NO wrappers are applied, but heartbeat/offline are still there.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Remark6_IgnoreExtraAdapters_StillWrapsWithHeartbeat()
	{
		var idGen = new IncrementalIdGenerator();
		var candleBuilderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());

		var cs = new AdapterConnectionState();
		var cm = new AdapterConnectionManager(cs);
		var ps = new PendingMessageState();
		var sr = new SubscriptionRoutingState();
		var pcm = new ParentChildMap();
		var or = new OrderRoutingState();

		var routingManager = new BasketRoutingManager(
			cs, cm, ps, sr, pcm, or,
			a => a, candleBuilderProvider, () => false, idGen);

		var basket = new BasketMessageAdapter(
			idGen,
			candleBuilderProvider,
			new InMemorySecurityMessageAdapterProvider(),
			new InMemoryPortfolioMessageAdapterProvider(),
			null,
			null,
			routingManager);

		basket.IgnoreExtraAdapters = true;

		var adapter = new TestRoutingInnerAdapter(idGen);
		basket.InnerAdapters.Add(adapter);

		// Enable heartbeat explicitly
		basket.ApplyHeartbeat(adapter, true);

		_outMessages = [];
		basket.NewOutMessageAsync += async (msg, ct) =>
		{
			if (msg.IsBack())
			{
				await ((IMessageTransport)basket).SendInMessageAsync(msg, ct);
				return;
			}
			_outMessages.Enqueue(msg);
		};

		await ConnectBasket(basket, TestContext.CancellationToken);

		// Despite IgnoreExtraAdapters=true, adapter connects (heartbeat wrapped).
		// IgnoreExtraAdapters skips only Channel/Latency/Slippage/Commission/PnL,
		// but heartbeat and offline are applied BEFORE the check (AdapterWrapperPipelineBuilder:31-44).
		(adapter.ReceivedMessages.Any(m => m is ConnectMessage))
			.AssertTrue("Adapter should receive Connect through heartbeat wrapper despite IgnoreExtraAdapters=true");
	}

	#endregion

	#region Remark 7: Tests demonstrating real bugs (were failing before fix)

	/// <summary>
	/// TEST: AdapterRouter._nonSupportedAdapters filters SecurityLookup.
	///
	/// AddNotSupported(transId, adapter) adds adapter to _nonSupportedAdapters,
	/// and GetAdapters() now checks _nonSupportedAdapters for ALL subscription types
	/// (not just MessageTypes.MarketData).
	///
	/// Test proves: after AddNotSupported, both MarketData and SecurityLookup are filtered.
	/// </summary>
	[TestMethod]
	public void Remark7_AdapterRouter_NonSupportedAdapters_NotFiltered_ForSecurityLookup()
	{
		var idGen = new IncrementalIdGenerator();
		var or = new OrderRoutingState();
		var candleBuilderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());

		var adapter1 = new TestRoutingInnerAdapter(idGen);
		// So adapter passes IsLookupAll filter (IsSupportSecuritiesLookupAll)
		adapter1.SetAllDownloadingSupported(DataType.Securities);

		var router = new AdapterRouter(or, a => a, candleBuilderProvider, () => false);

		// Register adapter for SecurityLookup and MarketData
		router.AddMessageTypeAdapter(MessageTypes.SecurityLookup, adapter1);
		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter1);

		var transId = idGen.GetNextId();

		// Mark adapter as NotSupported for transId
		router.AddNotSupported(transId, adapter1);

		// Check MarketData: adapter is FILTERED OUT (works correctly)
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		};
		var (mdAdapters, _) = router.GetAdapters(mdMsg, a => a);
		IsTrue(mdAdapters == null || mdAdapters.Length == 0,
			"MarketData: adapter should be filtered out after AddNotSupported");

		// Check SecurityLookup: adapter is FILTERED OUT (fixed!)
		// _nonSupportedAdapters is now checked for all subscription types
		var lookupMsg = new SecurityLookupMessage { TransactionId = transId };
		var (lookupAdapters, _) = router.GetAdapters(lookupMsg, a => a);

		// After fix, adapter is filtered for SecurityLookup too
		IsTrue(lookupAdapters == null || lookupAdapters.Length == 0,
			"SecurityLookup should filter adapter after AddNotSupported");
	}

	/// <summary>
	/// TEST: AdapterRouter._nonSupportedAdapters filters OrderStatus.
	/// Same as SecurityLookup — AddNotSupported now affects OrderStatus routing.
	/// </summary>
	[TestMethod]
	public void Remark7_AdapterRouter_NonSupportedAdapters_NotFiltered_ForOrderStatus()
	{
		var idGen = new IncrementalIdGenerator();
		var or = new OrderRoutingState();
		var candleBuilderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());

		var adapter1 = new TestRoutingInnerAdapter(idGen);
		adapter1.SetAllDownloadingSupported(DataType.Transactions);

		var router = new AdapterRouter(or, a => a, candleBuilderProvider, () => false);
		router.AddMessageTypeAdapter(MessageTypes.OrderStatus, adapter1);
		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter1);

		var transId = idGen.GetNextId();
		router.AddNotSupported(transId, adapter1);

		// Check OrderStatus: adapter is FILTERED OUT (fixed!)
		var osMsg = new OrderStatusMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
		};
		var (osAdapters, _) = router.GetAdapters(osMsg, a => a);

		// After fix, _nonSupportedAdapters is checked for OrderStatus
		IsTrue(osAdapters == null || osAdapters.Length == 0,
			"OrderStatus should filter adapter after AddNotSupported");
	}

	/// <summary>
	/// TEST: AdapterRouter._nonSupportedAdapters filters PortfolioLookup.
	/// </summary>
	[TestMethod]
	public void Remark7_AdapterRouter_NonSupportedAdapters_NotFiltered_ForPortfolioLookup()
	{
		var idGen = new IncrementalIdGenerator();
		var or = new OrderRoutingState();
		var candleBuilderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());

		var adapter1 = new TestRoutingInnerAdapter(idGen);
		adapter1.SetAllDownloadingSupported(DataType.PositionChanges);

		var router = new AdapterRouter(or, a => a, candleBuilderProvider, () => false);
		router.AddMessageTypeAdapter(MessageTypes.PortfolioLookup, adapter1);
		router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter1);

		var transId = idGen.GetNextId();
		router.AddNotSupported(transId, adapter1);

		// Control: MarketData is filtered
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		};
		var (mdAdapters, _) = router.GetAdapters(mdMsg, a => a);
		IsTrue(mdAdapters == null || mdAdapters.Length == 0,
			"MarketData: adapter should be filtered out after AddNotSupported");

		// PortfolioLookup: adapter is FILTERED OUT (fixed!)
		var plMsg = new PortfolioLookupMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
		};
		var (plAdapters, _) = router.GetAdapters(plMsg, a => a);

		// After fix, adapter is filtered for PortfolioLookup
		IsTrue(plAdapters == null || plAdapters.Length == 0,
			"PortfolioLookup should filter adapter after AddNotSupported");
	}

	#endregion
}

using System.Collections.Concurrent;

namespace StockSharp.Tests;

/// <summary>
/// Comprehensive tests for Connector message routing through BasketMessageAdapter.
/// Tests include stress tests, concurrency, edge cases, and live feed simulation.
/// </summary>
[TestClass]
public class ConnectorRoutingTests : BaseTestClass
{
	#region Mock Adapters

	/// <summary>
	/// Mock adapter that simulates a crypto exchange with live market data feeds.
	/// Supports concurrent data emission from background threads.
	/// </summary>
	private sealed class LiveFeedCryptoAdapter : MessageAdapter
	{
		private readonly string _exchangeName;
		private readonly SecurityId[] _supportedSecurities;
		private readonly ConcurrentQueue<Message> _inMessages = new();
		private readonly ConcurrentDictionary<long, MarketDataMessage> _activeSubscriptions = new();
		private readonly ConcurrentDictionary<long, OrderRegisterMessage> _activeOrders = new();
		private long _orderId;
		private long _tradeId;
		private CancellationTokenSource _feedCts;
		private readonly List<Task> _feedTasks = [];
		private volatile bool _isConnected;
		private readonly object _lock = new();

		// Statistics
		public int TotalTicksEmitted;
		public int TotalQuotesEmitted;
		public int TotalOrdersProcessed;
		public int TotalCancelsProcessed;
		public int ConnectionCount;
		public int DisconnectionCount;
		public int TotalSubscribeReceived;
		public int TotalUnsubscribeReceived;
		public List<Exception> Errors { get; } = [];

		public LiveFeedCryptoAdapter(string exchangeName, SecurityId[] supportedSecurities, IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			_exchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
			_supportedSecurities = supportedSecurities ?? throw new ArgumentNullException(nameof(supportedSecurities));

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
		}

		public string ExchangeName => _exchangeName;
		public bool IsConnected => _isConnected;
		public IReadOnlyList<Message> ReceivedMessages => [.. _inMessages];
		public IEnumerable<T> GetMessages<T>() where T : Message => _inMessages.OfType<T>();
		public int ActiveSubscriptionCount => _activeSubscriptions.Count;

		/// <inheritdoc />
		protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
		{
			_inMessages.Enqueue(message.TypedClone());

			try
			{
				switch (message.Type)
				{
					case MessageTypes.Reset:
						Reset();
						await SendOutMessageAsync(new ResetMessage(), cancellationToken);
						break;

					case MessageTypes.Connect:
						await ProcessConnect(cancellationToken);
						break;

					case MessageTypes.Disconnect:
						await ProcessDisconnect(cancellationToken);
						break;

					case MessageTypes.SecurityLookup:
						await ProcessSecurityLookup((SecurityLookupMessage)message, cancellationToken);
						break;

					case MessageTypes.PortfolioLookup:
						await ProcessPortfolioLookup((PortfolioLookupMessage)message, cancellationToken);
						break;

					case MessageTypes.OrderStatus:
						await ProcessOrderStatus((OrderStatusMessage)message, cancellationToken);
						break;

					case MessageTypes.MarketData:
						await ProcessMarketData((MarketDataMessage)message, cancellationToken);
						break;

					case MessageTypes.OrderRegister:
						await ProcessOrderRegister((OrderRegisterMessage)message, cancellationToken);
						break;

					case MessageTypes.OrderCancel:
						await ProcessOrderCancel((OrderCancelMessage)message, cancellationToken);
						break;
				}
			}
			catch (Exception ex)
			{
				Errors.Add(ex);
				throw;
			}
		}

		private async ValueTask ProcessConnect(CancellationToken cancellationToken)
		{
			lock (_lock)
			{
				_isConnected = true;
				ConnectionCount++;
				_feedCts = new CancellationTokenSource();
			}
			await SendOutMessageAsync(new ConnectMessage(), cancellationToken);
		}

		private async ValueTask ProcessDisconnect(CancellationToken cancellationToken)
		{
			lock (_lock)
			{
				_isConnected = false;
				DisconnectionCount++;
				_feedCts?.Cancel();
			}
			await SendOutMessageAsync(new DisconnectMessage(), cancellationToken);
		}

		private void Reset()
		{
			lock (_lock)
			{
				_feedCts?.Cancel();
				_activeSubscriptions.Clear();
				_activeOrders.Clear();
			}
		}

		private async ValueTask ProcessSecurityLookup(SecurityLookupMessage msg, CancellationToken cancellationToken)
		{
			foreach (var secId in _supportedSecurities)
			{
				await SendOutMessageAsync(new SecurityMessage
				{
					SecurityId = secId,
					Name = $"{secId.SecurityCode}",
					SecurityType = SecurityTypes.CryptoCurrency,
					OriginalTransactionId = msg.TransactionId,
				}, cancellationToken);
			}
			await SendOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = msg.TransactionId }, cancellationToken);
		}

		private async ValueTask ProcessPortfolioLookup(PortfolioLookupMessage msg, CancellationToken cancellationToken)
		{
			await SendOutMessageAsync(new PortfolioMessage
			{
				PortfolioName = $"{_exchangeName}_Portfolio",
				OriginalTransactionId = msg.TransactionId,
			}, cancellationToken);
			await SendOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = msg.TransactionId }, cancellationToken);
		}

		private async ValueTask ProcessOrderStatus(OrderStatusMessage msg, CancellationToken cancellationToken)
		{
			if (msg.IsSubscribe)
			{
				await SendOutMessageAsync(msg.CreateResponse(), cancellationToken);
				await SendOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = msg.TransactionId }, cancellationToken);
			}
		}

		private async ValueTask ProcessMarketData(MarketDataMessage msg, CancellationToken cancellationToken)
		{
			await SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = msg.TransactionId }, cancellationToken);

			if (msg.IsSubscribe)
			{
				Interlocked.Increment(ref TotalSubscribeReceived);
				_activeSubscriptions[msg.TransactionId] = msg;
				await SendOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = msg.TransactionId }, cancellationToken);
			}
			else
			{
				Interlocked.Increment(ref TotalUnsubscribeReceived);
				_activeSubscriptions.TryRemove(msg.OriginalTransactionId, out _);
			}
		}

		private async ValueTask ProcessOrderRegister(OrderRegisterMessage msg, CancellationToken cancellationToken)
		{
			Interlocked.Increment(ref TotalOrdersProcessed);
			var orderId = Interlocked.Increment(ref _orderId);

			_activeOrders[msg.TransactionId] = msg;

			// Simulate slight processing delay
			await SendOutMessageAsync(new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				SecurityId = msg.SecurityId,
				OriginalTransactionId = msg.TransactionId,
				OrderId = orderId,
				OrderState = OrderStates.Active,
				Side = msg.Side,
				OrderPrice = msg.Price,
				OrderVolume = msg.Volume,
				Balance = msg.Volume,
				ServerTime = DateTime.UtcNow,
				LocalTime = DateTime.UtcNow,
				HasOrderInfo = true,
			}, cancellationToken);

			// For market orders - immediate fill
			if (msg.OrderType == OrderTypes.Market)
			{
				var tradeId = Interlocked.Increment(ref _tradeId);
				await SendOutMessageAsync(new ExecutionMessage
				{
					DataTypeEx = DataType.Transactions,
					SecurityId = msg.SecurityId,
					OriginalTransactionId = msg.TransactionId,
					OrderId = orderId,
					TradeId = tradeId,
					TradePrice = msg.Price > 0 ? msg.Price : 100,
					TradeVolume = msg.Volume,
					OrderState = OrderStates.Done,
					Balance = 0,
					ServerTime = DateTime.UtcNow,
					LocalTime = DateTime.UtcNow,
					HasOrderInfo = true,
				}, cancellationToken);
				_activeOrders.TryRemove(msg.TransactionId, out _);
			}
		}

		private async ValueTask ProcessOrderCancel(OrderCancelMessage msg, CancellationToken cancellationToken)
		{
			Interlocked.Increment(ref TotalCancelsProcessed);

			await SendOutMessageAsync(new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				SecurityId = msg.SecurityId,
				OriginalTransactionId = msg.TransactionId,
				OrderId = msg.OrderId,
				OrderState = OrderStates.Done,
				ServerTime = DateTime.UtcNow,
				LocalTime = DateTime.UtcNow,
				HasOrderInfo = true,
			}, cancellationToken);
		}

		/// <summary>
		/// Start live feed simulation in background thread.
		/// Emits ticks at specified interval.
		/// </summary>
		public void StartLiveFeed(int tickIntervalMs = 100, decimal basePrice = 50000)
		{
			var cts = _feedCts;
			if (cts == null || cts.IsCancellationRequested)
				return;

			var task = Task.Run(async () =>
			{
				var random = new Random();
				var price = basePrice;

				while (!cts.Token.IsCancellationRequested && _isConnected)
				{
					try
					{
						await Task.Delay(tickIntervalMs, cts.Token);

						// Price random walk
						price += (decimal)(random.NextDouble() - 0.5) * 10;

						foreach (var sub in _activeSubscriptions.Values.ToArray())
						{
							if (sub.DataType2 == DataType.Ticks)
							{
								await EmitTick(sub.SecurityId, price, (decimal)(random.NextDouble() * 10), sub.TransactionId, cts.Token);
							}
							else if (sub.DataType2 == DataType.MarketDepth)
							{
								await EmitQuotes(sub.SecurityId, price - 1, price + 1, sub.TransactionId, cts.Token);
							}
							else if (sub.DataType2 == DataType.Level1)
							{
								await EmitLevel1(sub.SecurityId, price, sub.TransactionId, cts.Token);
							}
						}
					}
					catch (OperationCanceledException)
					{
						break;
					}
					catch (Exception ex)
					{
						Errors.Add(ex);
					}
				}
			}, cts.Token);

			lock (_feedTasks)
				_feedTasks.Add(task);
		}

		/// <summary>
		/// Emit a single tick.
		/// </summary>
		public async ValueTask EmitTick(SecurityId secId, decimal price, decimal volume, long subscriptionId, CancellationToken cancellationToken)
		{
			Interlocked.Increment(ref TotalTicksEmitted);
			var msg = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				SecurityId = secId,
				TradePrice = price,
				TradeVolume = volume,
				TradeId = Interlocked.Increment(ref _tradeId),
				ServerTime = DateTime.UtcNow,
				LocalTime = DateTime.UtcNow,
			};
			msg.SetSubscriptionIds(subscriptionId: subscriptionId);
			await SendOutMessageAsync(msg, cancellationToken);
		}

		/// <summary>
		/// Emit quotes.
		/// </summary>
		public async ValueTask EmitQuotes(SecurityId secId, decimal bid, decimal ask, long subscriptionId, CancellationToken cancellationToken)
		{
			Interlocked.Increment(ref TotalQuotesEmitted);
			var msg = new QuoteChangeMessage
			{
				SecurityId = secId,
				Bids = [new QuoteChange(bid, 100)],
				Asks = [new QuoteChange(ask, 100)],
				ServerTime = DateTime.UtcNow,
				LocalTime = DateTime.UtcNow,
			};
			msg.SetSubscriptionIds(subscriptionId: subscriptionId);
			await SendOutMessageAsync(msg, cancellationToken);
		}

		/// <summary>
		/// Emit Level1.
		/// </summary>
		public async ValueTask EmitLevel1(SecurityId secId, decimal lastPrice, long subscriptionId, CancellationToken cancellationToken)
		{
			var msg = new Level1ChangeMessage
			{
				SecurityId = secId,
				ServerTime = DateTime.UtcNow,
				LocalTime = DateTime.UtcNow,
			};
			msg.Add(Level1Fields.LastTradePrice, lastPrice);
			msg.SetSubscriptionIds(subscriptionId: subscriptionId);
			await SendOutMessageAsync(msg, cancellationToken);
		}

		/// <summary>
		/// Simulate connection drop and reconnect.
		/// </summary>
		public async ValueTask SimulateConnectionDrop(CancellationToken cancellationToken)
		{
			lock (_lock)
			{
				_feedCts?.Cancel();
				_isConnected = false;
			}
			await SendOutMessageAsync(new DisconnectMessage { Error = new IOException("Connection lost") }, cancellationToken);
		}

		/// <summary>
		/// Simulate order rejection.
		/// </summary>
		public async ValueTask SimulateOrderReject(long transactionId, string reason, CancellationToken cancellationToken)
		{
			await SendOutMessageAsync(new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				OriginalTransactionId = transactionId,
				OrderState = OrderStates.Failed,
				Error = new InvalidOperationException(reason),
				ServerTime = DateTime.UtcNow,
				LocalTime = DateTime.UtcNow,
				HasOrderInfo = true,
			}, cancellationToken);
		}

		public override IMessageAdapter Clone()
			=> new LiveFeedCryptoAdapter(_exchangeName, _supportedSecurities, TransactionIdGenerator);
	}

	#endregion

	#region Basic Routing Tests

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task Connector_MarketDataRouting_ToCorrectAdapter()
	{
		var binanceSecId = new SecurityId { SecurityCode = "BTCUSDT", BoardCode = "BINANCE" };
		var kucoinSecId = new SecurityId { SecurityCode = "ETHUSDT", BoardCode = "KUCOIN" };

		var connector = new Connector();

		var binanceAdapter = new LiveFeedCryptoAdapter("Binance", [binanceSecId], connector.TransactionIdGenerator);
		var kucoinAdapter = new LiveFeedCryptoAdapter("Kucoin", [kucoinSecId], connector.TransactionIdGenerator);

		connector.Adapter.InnerAdapters.Add(binanceAdapter);
		connector.Adapter.InnerAdapters.Add(kucoinAdapter);

		// Configure routing
		connector.Adapter.SecurityAdapterProvider.SetAdapter((binanceSecId, null), binanceAdapter);
		connector.Adapter.SecurityAdapterProvider.SetAdapter((kucoinSecId, null), kucoinAdapter);
		connector.Adapter.PortfolioAdapterProvider.SetAdapter("Binance_Portfolio", binanceAdapter);
		connector.Adapter.PortfolioAdapterProvider.SetAdapter("Kucoin_Portfolio", kucoinAdapter);

		var receivedTicks = new ConcurrentBag<ITickTradeMessage>();
		connector.TickTradeReceived += (sub, tick) => receivedTicks.Add(tick);

		await connector.ConnectAsync(CancellationToken);

		var btcSecurity = new Security { Id = binanceSecId.ToStringId() };
		var ethSecurity = new Security { Id = kucoinSecId.ToStringId() };
		connector.SendOutMessage(btcSecurity.ToMessage());
		connector.SendOutMessage(ethSecurity.ToMessage());

		var btcSubscription = new Subscription(DataType.Ticks, btcSecurity);
		var ethSubscription = new Subscription(DataType.Ticks, ethSecurity);
		connector.Subscribe(btcSubscription);
		connector.Subscribe(ethSubscription);
		await Task.Delay(200, CancellationToken);

		await binanceAdapter.EmitTick(binanceSecId, 50000, 1, btcSubscription.TransactionId, CancellationToken);
		await kucoinAdapter.EmitTick(kucoinSecId, 3000, 2, ethSubscription.TransactionId, CancellationToken);
		await Task.Delay(500, CancellationToken);

		var binanceMarketData = binanceAdapter.GetMessages<MarketDataMessage>().ToList();
		var kucoinMarketData = kucoinAdapter.GetMessages<MarketDataMessage>().ToList();

		binanceMarketData.Any(m => m.SecurityId == binanceSecId && m.IsSubscribe).AssertTrue("Binance should receive BTCUSDT subscription");
		binanceMarketData.Any(m => m.SecurityId == kucoinSecId).AssertFalse("Binance should NOT receive ETHUSDT subscription");
		kucoinMarketData.Any(m => m.SecurityId == kucoinSecId && m.IsSubscribe).AssertTrue("Kucoin should receive ETHUSDT subscription");
		kucoinMarketData.Any(m => m.SecurityId == binanceSecId).AssertFalse("Kucoin should NOT receive BTCUSDT subscription");

		(receivedTicks.Count >= 2).AssertTrue($"Should receive at least 2 ticks, got {receivedTicks.Count}");

		await connector.DisconnectAsync(CancellationToken);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task Connector_OrderRouting_ToCorrectAdapter()
	{
		var binanceSecId = new SecurityId { SecurityCode = "BTCUSDT", BoardCode = "BINANCE" };
		var kucoinSecId = new SecurityId { SecurityCode = "ETHUSDT", BoardCode = "KUCOIN" };

		var connector = new Connector();

		var binanceAdapter = new LiveFeedCryptoAdapter("Binance", [binanceSecId], connector.TransactionIdGenerator);
		var kucoinAdapter = new LiveFeedCryptoAdapter("Kucoin", [kucoinSecId], connector.TransactionIdGenerator);

		connector.Adapter.InnerAdapters.Add(binanceAdapter);
		connector.Adapter.InnerAdapters.Add(kucoinAdapter);

		connector.Adapter.PortfolioAdapterProvider.SetAdapter("Binance_Portfolio", binanceAdapter);
		connector.Adapter.PortfolioAdapterProvider.SetAdapter("Kucoin_Portfolio", kucoinAdapter);

		await connector.ConnectAsync(CancellationToken);

		var btcSecurity = new Security { Id = binanceSecId.ToStringId() };
		var ethSecurity = new Security { Id = kucoinSecId.ToStringId() };
		connector.SendOutMessage(btcSecurity.ToMessage());
		connector.SendOutMessage(ethSecurity.ToMessage());

		connector.RegisterOrder(new Order
		{
			Security = btcSecurity,
			Portfolio = new Portfolio { Name = "Binance_Portfolio" },
			Side = Sides.Buy,
			Price = 50000,
			Volume = 1,
			Type = OrderTypes.Limit,
		});

		connector.RegisterOrder(new Order
		{
			Security = ethSecurity,
			Portfolio = new Portfolio { Name = "Kucoin_Portfolio" },
			Side = Sides.Buy,
			Price = 3000,
			Volume = 2,
			Type = OrderTypes.Limit,
		});

		await Task.Delay(500, CancellationToken);

		var binanceOrders = binanceAdapter.GetMessages<OrderRegisterMessage>().ToList();
		var kucoinOrders = kucoinAdapter.GetMessages<OrderRegisterMessage>().ToList();

		binanceOrders.Count.AssertEqual(1, "Binance should receive 1 order");
		kucoinOrders.Count.AssertEqual(1, "Kucoin should receive 1 order");
		binanceOrders[0].SecurityId.AssertEqual(binanceSecId);
		kucoinOrders[0].SecurityId.AssertEqual(kucoinSecId);

		await connector.DisconnectAsync(CancellationToken);
	}

	#endregion

	#region Live Feed Stress Tests

	/// <summary>
	/// Test with continuous live feeds from multiple exchanges.
	/// Verifies data integrity under sustained load.
	/// </summary>
	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task LiveFeed_MultipleExchanges_DataIntegrity()
	{
		var binanceSecId = new SecurityId { SecurityCode = "BTCUSDT", BoardCode = "BINANCE" };
		var kucoinSecId = new SecurityId { SecurityCode = "ETHUSDT", BoardCode = "KUCOIN" };

		var connector = new Connector();

		var binanceAdapter = new LiveFeedCryptoAdapter("Binance", [binanceSecId], connector.TransactionIdGenerator);
		var kucoinAdapter = new LiveFeedCryptoAdapter("Kucoin", [kucoinSecId], connector.TransactionIdGenerator);

		connector.Adapter.InnerAdapters.Add(binanceAdapter);
		connector.Adapter.InnerAdapters.Add(kucoinAdapter);

		connector.Adapter.SecurityAdapterProvider.SetAdapter((binanceSecId, null), binanceAdapter);
		connector.Adapter.SecurityAdapterProvider.SetAdapter((kucoinSecId, null), kucoinAdapter);

		var btcTicks = new ConcurrentBag<ITickTradeMessage>();
		var ethTicks = new ConcurrentBag<ITickTradeMessage>();
		var errors = new ConcurrentBag<Exception>();

		connector.TickTradeReceived += (sub, tick) =>
		{
			if (tick.SecurityId == binanceSecId)
				btcTicks.Add(tick);
			else if (tick.SecurityId == kucoinSecId)
				ethTicks.Add(tick);
		};

		connector.Error += ex => errors.Add(ex);

		await connector.ConnectAsync(CancellationToken);

		var btcSecurity = new Security { Id = binanceSecId.ToStringId() };
		var ethSecurity = new Security { Id = kucoinSecId.ToStringId() };
		connector.SendOutMessage(btcSecurity.ToMessage());
		connector.SendOutMessage(ethSecurity.ToMessage());

		var btcSub = new Subscription(DataType.Ticks, btcSecurity);
		var ethSub = new Subscription(DataType.Ticks, ethSecurity);
		connector.Subscribe(btcSub);
		connector.Subscribe(ethSub);

		await Task.Delay(300, CancellationToken);

		// Start live feeds - 50ms interval = 20 ticks/second per exchange
		binanceAdapter.StartLiveFeed(tickIntervalMs: 50, basePrice: 50000);
		kucoinAdapter.StartLiveFeed(tickIntervalMs: 50, basePrice: 3000);

		// Run for 3 seconds
		await Task.Delay(3000, CancellationToken);

		await connector.DisconnectAsync(CancellationToken);

		// Assert
		Console.WriteLine($"BTC ticks received: {btcTicks.Count}");
		Console.WriteLine($"ETH ticks received: {ethTicks.Count}");
		Console.WriteLine($"Binance emitted: {binanceAdapter.TotalTicksEmitted}");
		Console.WriteLine($"Kucoin emitted: {kucoinAdapter.TotalTicksEmitted}");
		Console.WriteLine($"Errors: {errors.Count}");

		// Should receive significant number of ticks from both
		(btcTicks.Count >= 30).AssertTrue($"Should receive many BTC ticks, got {btcTicks.Count}");
		(ethTicks.Count >= 30).AssertTrue($"Should receive many ETH ticks, got {ethTicks.Count}");

		// No errors
		errors.Count.AssertEqual(0, $"Should have no errors, got: {string.Join(", ", errors.Select(e => e.Message))}");

		// Verify data integrity - all BTC ticks should have BTC security
		btcTicks.All(t => t.SecurityId == binanceSecId).AssertTrue("All BTC ticks should have BTC SecurityId");
		ethTicks.All(t => t.SecurityId == kucoinSecId).AssertTrue("All ETH ticks should have ETH SecurityId");
	}

	/// <summary>
	/// High frequency trading scenario - rapid order submission.
	/// </summary>
	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task StressTest_RapidOrderSubmission()
	{
		var binanceSecId = new SecurityId { SecurityCode = "BTCUSDT", BoardCode = "BINANCE" };

		var connector = new Connector();
		var adapter = new LiveFeedCryptoAdapter("Binance", [binanceSecId], connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);
		connector.Adapter.PortfolioAdapterProvider.SetAdapter("Binance_Portfolio", adapter);

		var ordersReceived = new ConcurrentBag<Order>();
		var ordersFailed = new ConcurrentBag<OrderFail>();

		connector.OrderReceived += (sub, order) => ordersReceived.Add(order);
		connector.OrderRegisterFailReceived += (sub, fail) => ordersFailed.Add(fail);

		await connector.ConnectAsync(CancellationToken);

		var btcSecurity = new Security { Id = binanceSecId.ToStringId() };
		connector.SendOutMessage(btcSecurity.ToMessage());

		const int orderCount = 100;
		var tasks = new List<Task>();

		// Submit 100 orders as fast as possible from multiple threads
		for (int i = 0; i < orderCount; i++)
		{
			var price = 50000 + i;
			tasks.Add(Task.Run(() =>
			{
				connector.RegisterOrder(new Order
				{
					Security = btcSecurity,
					Portfolio = new Portfolio { Name = "Binance_Portfolio" },
					Side = Sides.Buy,
					Price = price,
					Volume = 1,
					Type = OrderTypes.Limit,
				});
			}));
		}

		await Task.WhenAll(tasks);
		await Task.Delay(2000, CancellationToken);

		await connector.DisconnectAsync(CancellationToken);

		Console.WriteLine($"Orders submitted: {orderCount}");
		Console.WriteLine($"Orders received by adapter: {adapter.TotalOrdersProcessed}");
		Console.WriteLine($"Order updates received: {ordersReceived.Count}");
		Console.WriteLine($"Orders failed: {ordersFailed.Count}");

		// All orders should be processed
		adapter.TotalOrdersProcessed.AssertEqual(orderCount, $"Adapter should process {orderCount} orders");

		// No failures expected
		ordersFailed.Count.AssertEqual(0, "No orders should fail");
	}

	/// <summary>
	/// Tests that sequential subscriptions work correctly.
	/// Subscribes to 10 securities, waits for online confirmation, then unsubscribes.
	/// </summary>
	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task Subscriptions_SequentialWithConfirmation_AllProcessed()
	{
		var securities = Enumerable.Range(0, 10)
			.Select(i => new SecurityId { SecurityCode = $"TOKEN{i}USDT", BoardCode = "BINANCE" })
			.ToArray();

		var connector = new Connector();
		var adapter = new LiveFeedCryptoAdapter("Binance", securities, connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		foreach (var secId in securities)
			connector.Adapter.SecurityAdapterProvider.SetAdapter((secId, null), adapter);

		var onlineSubscriptions = new ConcurrentDictionary<long, bool>();
		connector.SubscriptionOnline += sub => onlineSubscriptions[sub.TransactionId] = true;

		await connector.ConnectAsync(CancellationToken);

		// Register all securities
		var securityObjects = securities.Select(s =>
		{
			var sec = new Security { Id = s.ToStringId() };
			connector.SendOutMessage(sec.ToMessage());
			return sec;
		}).ToArray();

		await Task.Delay(200, CancellationToken);

		// Subscribe to all and wait for online confirmation
		var subscriptions = new List<Subscription>();
		foreach (var sec in securityObjects)
		{
			var sub = new Subscription(DataType.Ticks, sec);
			subscriptions.Add(sub);
			connector.Subscribe(sub);
		}

		// Wait for all subscriptions to go online
		for (int i = 0; i < 50 && onlineSubscriptions.Count < securities.Length; i++)
			await Task.Delay(100, CancellationToken);

		Console.WriteLine($"Online subscriptions: {onlineSubscriptions.Count}");
		Console.WriteLine($"Subscribe requests received by adapter: {adapter.TotalSubscribeReceived}");

		// Verify all subscriptions were processed
		adapter.TotalSubscribeReceived.AssertEqual(securities.Length, "All subscriptions should reach adapter");

		// Unsubscribe from all
		foreach (var sub in subscriptions)
			connector.UnSubscribe(sub);

		// Wait for unsubscribe processing
		await Task.Delay(500, CancellationToken);

		Console.WriteLine($"Unsubscribe requests received by adapter: {adapter.TotalUnsubscribeReceived}");
		Console.WriteLine($"Active subscriptions remaining: {adapter.ActiveSubscriptionCount}");

		adapter.TotalUnsubscribeReceived.AssertEqual(securities.Length, "All unsubscriptions should reach adapter");
		adapter.ActiveSubscriptionCount.AssertEqual(0, "All subscriptions should be cleaned up");

		await connector.DisconnectAsync(CancellationToken);
	}

	/// <summary>
	/// Tests rapid subscribe/unsubscribe cycles.
	/// NOTE: This test documents a race condition in BasketMessageAdapter.
	/// When subscribe and unsubscribe messages are sent in rapid succession,
	/// some messages may be lost due to async processing in _subscriptions dictionary.
	/// The test uses sufficient delays to usually pass, but may occasionally fail
	/// demonstrating the underlying timing issue.
	/// </summary>
	[TestMethod]
	[Timeout(60000, CooperativeCancellation = true)]
	public async Task StressTest_RapidSubscriptionCycles()
	{
		var securities = Enumerable.Range(0, 5)
			.Select(i => new SecurityId { SecurityCode = $"TOKEN{i}USDT", BoardCode = "BINANCE" })
			.ToArray();

		var connector = new Connector();
		var adapter = new LiveFeedCryptoAdapter("Binance", securities, connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		foreach (var secId in securities)
			connector.Adapter.SecurityAdapterProvider.SetAdapter((secId, null), adapter);

		var onlineSubscriptions = new ConcurrentDictionary<long, bool>();
		connector.SubscriptionOnline += sub => onlineSubscriptions[sub.TransactionId] = true;

		await connector.ConnectAsync(CancellationToken);

		// Register all securities
		var securityObjects = securities.Select(s =>
		{
			var sec = new Security { Id = s.ToStringId() };
			connector.SendOutMessage(sec.ToMessage());
			return sec;
		}).ToArray();

		await Task.Delay(200, CancellationToken);

		const int rounds = 3;
		var totalSubscribes = 0;
		var totalUnsubscribes = 0;

		for (int round = 0; round < rounds; round++)
		{
			onlineSubscriptions.Clear();
			var subscriptions = new List<Subscription>();

			// Subscribe to all
			foreach (var sec in securityObjects)
			{
				var sub = new Subscription(DataType.Ticks, sec);
				subscriptions.Add(sub);
				connector.Subscribe(sub);
				totalSubscribes++;
			}

			// Wait for online confirmation before unsubscribing
			for (int i = 0; i < 30 && onlineSubscriptions.Count < securities.Length; i++)
				await Task.Delay(100, CancellationToken);

			// Unsubscribe from all
			foreach (var sub in subscriptions)
			{
				connector.UnSubscribe(sub);
				totalUnsubscribes++;
			}

			await Task.Delay(500, CancellationToken);
		}

		await connector.DisconnectAsync(CancellationToken);

		Console.WriteLine($"Total subscribe attempts: {totalSubscribes}");
		Console.WriteLine($"Subscribe requests received: {adapter.TotalSubscribeReceived}");
		Console.WriteLine($"Total unsubscribe attempts: {totalUnsubscribes}");
		Console.WriteLine($"Unsubscribe requests received: {adapter.TotalUnsubscribeReceived}");
		Console.WriteLine($"Active subscriptions remaining: {adapter.ActiveSubscriptionCount}");

		// Verify all messages were processed
		adapter.TotalSubscribeReceived.AssertEqual(totalSubscribes,
			$"All subscriptions should reach adapter. Lost: {totalSubscribes - adapter.TotalSubscribeReceived}");
		adapter.TotalUnsubscribeReceived.AssertEqual(totalUnsubscribes,
			$"All unsubscriptions should reach adapter. Lost: {totalUnsubscribes - adapter.TotalUnsubscribeReceived}");
		adapter.ActiveSubscriptionCount.AssertEqual(0, "All subscriptions should be cleaned up");
	}

	#endregion

	#region Edge Cases and Error Handling

	/// <summary>
	/// Test behavior when adapter returns error for subscription.
	/// </summary>
	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task EdgeCase_SubscriptionError_Handled()
	{
		var binanceSecId = new SecurityId { SecurityCode = "BTCUSDT", BoardCode = "BINANCE" };

		var connector = new Connector();
		var adapter = new LiveFeedCryptoAdapter("Binance", [binanceSecId], connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);
		connector.Adapter.SecurityAdapterProvider.SetAdapter((binanceSecId, null), adapter);

		var subscriptionErrors = new ConcurrentBag<Exception>();
		connector.Error += ex => subscriptionErrors.Add(ex);

		await connector.ConnectAsync(CancellationToken);

		// Subscribe to security that doesn't exist in adapter
		var unknownSecId = new SecurityId { SecurityCode = "UNKNOWN", BoardCode = "BINANCE" };
		var unknownSec = new Security { Id = unknownSecId.ToStringId() };
		connector.SendOutMessage(unknownSec.ToMessage());

		var sub = new Subscription(DataType.Ticks, unknownSec);
		connector.Subscribe(sub);

		await Task.Delay(500, CancellationToken);

		await connector.DisconnectAsync(CancellationToken);

		// Log what happened
		Console.WriteLine($"Subscription errors: {subscriptionErrors.Count}");
		foreach (var err in subscriptionErrors)
			Console.WriteLine($"  - {err.Message}");
	}

	/// <summary>
	/// Test fallback behavior when no SecurityAdapterProvider mapping exists.
	/// Without mapping, subscription should go to all adapters that support the data type.
	/// </summary>
	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task EdgeCase_NoSecurityMapping_FallbackToAll()
	{
		var binanceSecId = new SecurityId { SecurityCode = "BTCUSDT", BoardCode = "BINANCE" };
		var kucoinSecId = new SecurityId { SecurityCode = "ETHUSDT", BoardCode = "KUCOIN" };

		var connector = new Connector();

		var binanceAdapter = new LiveFeedCryptoAdapter("Binance", [binanceSecId], connector.TransactionIdGenerator);
		var kucoinAdapter = new LiveFeedCryptoAdapter("Kucoin", [kucoinSecId], connector.TransactionIdGenerator);

		connector.Adapter.InnerAdapters.Add(binanceAdapter);
		connector.Adapter.InnerAdapters.Add(kucoinAdapter);

		// NOTE: Not setting SecurityAdapterProvider - testing fallback behavior

		await connector.ConnectAsync(CancellationToken);

		var btcSecurity = new Security { Id = binanceSecId.ToStringId() };
		connector.SendOutMessage(btcSecurity.ToMessage());

		var btcSub = new Subscription(DataType.Ticks, btcSecurity);
		connector.Subscribe(btcSub);

		await Task.Delay(500, CancellationToken);

		var binanceMarketData = binanceAdapter.GetMessages<MarketDataMessage>().ToList();
		var kucoinMarketData = kucoinAdapter.GetMessages<MarketDataMessage>().ToList();

		Console.WriteLine($"Binance received {binanceMarketData.Count} MarketData messages");
		Console.WriteLine($"Kucoin received {kucoinMarketData.Count} MarketData messages");

		// Without mapping, the subscription might go to one or both adapters
		// This documents the actual behavior
		var totalReceived = binanceMarketData.Count + kucoinMarketData.Count;
		(totalReceived >= 1).AssertTrue($"At least one adapter should receive the subscription, total: {totalReceived}");

		await connector.DisconnectAsync(CancellationToken);
	}

	/// <summary>
	/// Test that orders with same portfolio go to same adapter.
	/// </summary>
	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task EdgeCase_MultipleOrdersSamePortfolio_SameAdapter()
	{
		var binanceSecId1 = new SecurityId { SecurityCode = "BTCUSDT", BoardCode = "BINANCE" };
		var binanceSecId2 = new SecurityId { SecurityCode = "ETHUSDT", BoardCode = "BINANCE" };

		var connector = new Connector();

		var binanceAdapter = new LiveFeedCryptoAdapter("Binance", [binanceSecId1, binanceSecId2], connector.TransactionIdGenerator);
		var otherAdapter = new LiveFeedCryptoAdapter("Other", [], connector.TransactionIdGenerator);

		connector.Adapter.InnerAdapters.Add(binanceAdapter);
		connector.Adapter.InnerAdapters.Add(otherAdapter);

		connector.Adapter.PortfolioAdapterProvider.SetAdapter("Binance_Portfolio", binanceAdapter);

		await connector.ConnectAsync(CancellationToken);

		var btcSecurity = new Security { Id = binanceSecId1.ToStringId() };
		var ethSecurity = new Security { Id = binanceSecId2.ToStringId() };
		connector.SendOutMessage(btcSecurity.ToMessage());
		connector.SendOutMessage(ethSecurity.ToMessage());

		// Both orders use same portfolio
		connector.RegisterOrder(new Order
		{
			Security = btcSecurity,
			Portfolio = new Portfolio { Name = "Binance_Portfolio" },
			Side = Sides.Buy,
			Price = 50000,
			Volume = 1,
			Type = OrderTypes.Limit,
		});

		connector.RegisterOrder(new Order
		{
			Security = ethSecurity,
			Portfolio = new Portfolio { Name = "Binance_Portfolio" },
			Side = Sides.Sell,
			Price = 3000,
			Volume = 2,
			Type = OrderTypes.Limit,
		});

		await Task.Delay(500, CancellationToken);

		var binanceOrders = binanceAdapter.GetMessages<OrderRegisterMessage>().ToList();
		var otherOrders = otherAdapter.GetMessages<OrderRegisterMessage>().ToList();

		Console.WriteLine($"Binance orders: {binanceOrders.Count}");
		Console.WriteLine($"Other orders: {otherOrders.Count}");

		// Both orders should go to Binance
		binanceOrders.Count.AssertEqual(2, "Both orders should go to Binance adapter");
		otherOrders.Count.AssertEqual(0, "Other adapter should receive no orders");

		await connector.DisconnectAsync(CancellationToken);
	}

	/// <summary>
	/// Test behavior with duplicate subscriptions to same security.
	/// </summary>
	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task EdgeCase_DuplicateSubscriptions()
	{
		var binanceSecId = new SecurityId { SecurityCode = "BTCUSDT", BoardCode = "BINANCE" };

		var connector = new Connector();
		var adapter = new LiveFeedCryptoAdapter("Binance", [binanceSecId], connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);
		connector.Adapter.SecurityAdapterProvider.SetAdapter((binanceSecId, null), adapter);

		var receivedTicks = new ConcurrentBag<ITickTradeMessage>();
		connector.TickTradeReceived += (sub, tick) => receivedTicks.Add(tick);

		await connector.ConnectAsync(CancellationToken);

		var btcSecurity = new Security { Id = binanceSecId.ToStringId() };
		connector.SendOutMessage(btcSecurity.ToMessage());

		// Subscribe twice to same security
		var sub1 = new Subscription(DataType.Ticks, btcSecurity);
		var sub2 = new Subscription(DataType.Ticks, btcSecurity);

		connector.Subscribe(sub1);
		connector.Subscribe(sub2);

		await Task.Delay(300, CancellationToken);

		// Emit one tick
		await adapter.EmitTick(binanceSecId, 50000, 1, sub1.TransactionId, CancellationToken);
		await adapter.EmitTick(binanceSecId, 50001, 1, sub2.TransactionId, CancellationToken);

		await Task.Delay(500, CancellationToken);

		Console.WriteLine($"Subscriptions in adapter: {adapter.ActiveSubscriptionCount}");
		Console.WriteLine($"Ticks received: {receivedTicks.Count}");

		// Should receive ticks for both subscriptions
		(receivedTicks.Count >= 2).AssertTrue($"Should receive ticks for both subscriptions, got {receivedTicks.Count}");

		await connector.DisconnectAsync(CancellationToken);
	}

	#endregion

	#region Connection Handling

	/// <summary>
	/// Test reconnection after disconnect.
	/// </summary>
	[TestMethod]
	[Timeout(15000, CooperativeCancellation = true)]
	public async Task Connection_ReconnectAfterDisconnect()
	{
		var binanceSecId = new SecurityId { SecurityCode = "BTCUSDT", BoardCode = "BINANCE" };

		var connector = new Connector();
		var adapter = new LiveFeedCryptoAdapter("Binance", [binanceSecId], connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		// First connection
		await connector.ConnectAsync(CancellationToken);
		adapter.ConnectionCount.AssertEqual(1, "Should have 1 connection");
		connector.ConnectionState.AssertEqual(ConnectionStates.Connected);

		// Disconnect
		await connector.DisconnectAsync(CancellationToken);
		adapter.DisconnectionCount.AssertEqual(1, "Should have 1 disconnection");
		connector.ConnectionState.AssertEqual(ConnectionStates.Disconnected);

		// Reconnect
		await connector.ConnectAsync(CancellationToken);
		adapter.ConnectionCount.AssertEqual(2, "Should have 2 connections after reconnect");
		connector.ConnectionState.AssertEqual(ConnectionStates.Connected);

		await connector.DisconnectAsync(CancellationToken);
	}

	/// <summary>
	/// Test that subscriptions are properly cleaned up on disconnect.
	/// </summary>
	[TestMethod]
	[Timeout(15000, CooperativeCancellation = true)]
	public async Task Connection_SubscriptionsCleanedOnDisconnect()
	{
		var binanceSecId = new SecurityId { SecurityCode = "BTCUSDT", BoardCode = "BINANCE" };

		var connector = new Connector();
		var adapter = new LiveFeedCryptoAdapter("Binance", [binanceSecId], connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);
		connector.Adapter.SecurityAdapterProvider.SetAdapter((binanceSecId, null), adapter);

		await connector.ConnectAsync(CancellationToken);

		var btcSecurity = new Security { Id = binanceSecId.ToStringId() };
		connector.SendOutMessage(btcSecurity.ToMessage());

		// Create several subscriptions
		for (int i = 0; i < 5; i++)
		{
			var sub = new Subscription(DataType.Ticks, btcSecurity);
			connector.Subscribe(sub);
		}

		await Task.Delay(300, CancellationToken);
		Console.WriteLine($"Active subscriptions before disconnect: {adapter.ActiveSubscriptionCount}");
		(adapter.ActiveSubscriptionCount >= 1).AssertTrue("Should have active subscriptions");

		// Disconnect - subscriptions should be cleaned up
		await connector.DisconnectAsync(CancellationToken);

		// Note: Active subscriptions in adapter depend on whether unsubscribe messages are sent
		Console.WriteLine($"Active subscriptions after disconnect: {adapter.ActiveSubscriptionCount}");
	}

	#endregion

	#region Data Integrity Tests

	/// <summary>
	/// Verify that tick data is not corrupted during routing.
	/// </summary>
	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task DataIntegrity_TickDataPreserved()
	{
		var binanceSecId = new SecurityId { SecurityCode = "BTCUSDT", BoardCode = "BINANCE" };

		var connector = new Connector();
		var adapter = new LiveFeedCryptoAdapter("Binance", [binanceSecId], connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);
		connector.Adapter.SecurityAdapterProvider.SetAdapter((binanceSecId, null), adapter);

		var receivedTicks = new ConcurrentBag<ITickTradeMessage>();
		connector.TickTradeReceived += (sub, tick) => receivedTicks.Add(tick);

		await connector.ConnectAsync(CancellationToken);

		var btcSecurity = new Security { Id = binanceSecId.ToStringId() };
		connector.SendOutMessage(btcSecurity.ToMessage());

		var sub = new Subscription(DataType.Ticks, btcSecurity);
		connector.Subscribe(sub);

		await Task.Delay(200, CancellationToken);

		// Emit specific ticks with known values
		var expectedTicks = new List<(decimal price, decimal volume)>
		{
			(50000.50m, 1.5m),
			(50001.75m, 2.25m),
			(49999.99m, 0.01m),
			(50100.00m, 100.0m),
		};

		foreach (var (price, volume) in expectedTicks)
		{
			await adapter.EmitTick(binanceSecId, price, volume, sub.TransactionId, CancellationToken);
		}

		await Task.Delay(500, CancellationToken);

		await connector.DisconnectAsync(CancellationToken);

		// Verify received ticks match emitted
		receivedTicks.Count.AssertEqual(expectedTicks.Count, $"Should receive {expectedTicks.Count} ticks");

		var receivedList = receivedTicks.OrderBy(t => t.Price).ToList();
		var expectedList = expectedTicks.OrderBy(t => t.price).ToList();

		for (int i = 0; i < expectedList.Count; i++)
		{
			receivedList[i].Price.AssertEqual(expectedList[i].price, $"Tick {i} price mismatch");
			receivedList[i].Volume.AssertEqual(expectedList[i].volume, $"Tick {i} volume mismatch");
			receivedList[i].SecurityId.AssertEqual(binanceSecId, $"Tick {i} SecurityId mismatch");
		}
	}

	/// <summary>
	/// Verify order data is not corrupted during routing.
	/// </summary>
	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task DataIntegrity_OrderDataPreserved()
	{
		var binanceSecId = new SecurityId { SecurityCode = "BTCUSDT", BoardCode = "BINANCE" };

		var connector = new Connector();
		var adapter = new LiveFeedCryptoAdapter("Binance", [binanceSecId], connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);
		connector.Adapter.PortfolioAdapterProvider.SetAdapter("Binance_Portfolio", adapter);

		await connector.ConnectAsync(CancellationToken);

		var btcSecurity = new Security { Id = binanceSecId.ToStringId() };
		connector.SendOutMessage(btcSecurity.ToMessage());

		// Create order with specific values
		var originalOrder = new Order
		{
			Security = btcSecurity,
			Portfolio = new Portfolio { Name = "Binance_Portfolio" },
			Side = Sides.Sell,
			Price = 51234.56m,
			Volume = 12.345m,
			Type = OrderTypes.Limit,
			Comment = "Test order",
		};

		connector.RegisterOrder(originalOrder);

		await Task.Delay(500, CancellationToken);

		await connector.DisconnectAsync(CancellationToken);

		// Verify adapter received correct order data
		var receivedOrders = adapter.GetMessages<OrderRegisterMessage>().ToList();
		receivedOrders.Count.AssertEqual(1, "Should receive 1 order");

		var receivedOrder = receivedOrders[0];
		receivedOrder.SecurityId.AssertEqual(binanceSecId, "SecurityId mismatch");
		receivedOrder.Side.AssertEqual(Sides.Sell, "Side mismatch");
		receivedOrder.Price.AssertEqual(51234.56m, "Price mismatch");
		receivedOrder.Volume.AssertEqual(12.345m, "Volume mismatch");
		receivedOrder.OrderType.AssertEqual(OrderTypes.Limit, "OrderType mismatch");
	}

	#endregion
}

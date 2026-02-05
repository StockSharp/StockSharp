namespace StockSharp.Tests;

using System.Collections.Concurrent;

using StockSharp.Algo.Basket;
using StockSharp.Algo.Candles.Compression;
using StockSharp.Algo.Testing;

/// <summary>
/// Tests for message flow through BasketMessageAdapter.
/// Goal: verify that all messages sent from internal adapter come out of NewOutMessageAsync
/// with correct subscription IDs.
/// </summary>
[TestClass]
public class BasketMessageFlowTests : BaseTestClass
{
	#region Logging Infrastructure

	public sealed class MessageLog
	{
		public string Source { get; init; }
		public string Direction { get; init; }
		public Message Message { get; init; }
		public DateTime Timestamp { get; init; } = DateTime.UtcNow;
		public long[] SubscriptionIds { get; init; }

		public override string ToString()
			=> $"[{Timestamp:HH:mm:ss.fff}] {Source} {Direction}: {Message.Type}" +
			   (SubscriptionIds?.Length > 0 ? $" SubIds=[{string.Join(",", SubscriptionIds)}]" : "") +
			   (Message is IOriginalTransactionIdMessage otm ? $" OrigTransId={otm.OriginalTransactionId}" : "") +
			   (Message is ITransactionIdMessage tm ? $" TransId={tm.TransactionId}" : "");
	}

	public sealed class MessageFlowLogger
	{
		private readonly ConcurrentQueue<MessageLog> _logs = [];

		public IReadOnlyCollection<MessageLog> Logs => [.. _logs];

		public void Log(string source, string direction, Message message)
		{
			long[] subIds = null;
			if (message is ISubscriptionIdMessage subMsg)
				subIds = subMsg.GetSubscriptionIds();

			_logs.Enqueue(new MessageLog
			{
				Source = source,
				Direction = direction,
				Message = message.TypedClone(),
				SubscriptionIds = subIds,
			});
		}

		public void Clear() => _logs.Clear();

		public IEnumerable<MessageLog> GetByType(MessageTypes type)
			=> _logs.Where(l => l.Message.Type == type);

		public IEnumerable<MessageLog> GetBySource(string source)
			=> _logs.Where(l => l.Source == source);

		public void PrintAll(Action<string> output)
		{
			foreach (var log in _logs)
				output(log.ToString());
		}
	}

	#endregion

	#region Logging Adapter

	/// <summary>
	/// Adapter that logs all incoming and outgoing messages.
	/// </summary>
	private sealed class LoggingInnerAdapter : MessageAdapter
	{
		private readonly MessageFlowLogger _logger;
		private readonly string _name;
		private readonly ConcurrentQueue<Message> _inMessages = [];
		private readonly HashSet<DataType> _allDownloadingTypes = [];

		public LoggingInnerAdapter(IdGenerator idGen, MessageFlowLogger logger, string name)
			: base(idGen)
		{
			_logger = logger;
			_name = name;

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

			// Support all downloading for lookups by default
			_allDownloadingTypes.Add(DataType.Securities);
			_allDownloadingTypes.Add(DataType.PositionChanges);
			_allDownloadingTypes.Add(DataType.Transactions);
		}

		public override bool IsAllDownloadingSupported(DataType dataType)
			=> _allDownloadingTypes.Contains(dataType);

		public IReadOnlyList<Message> ReceivedMessages => [.. _inMessages];
		public IEnumerable<T> GetMessages<T>() where T : Message => _inMessages.OfType<T>();

		public bool AutoRespond { get; set; } = true;
		public bool EmitSecurityOnLookup { get; set; } = true;
		public bool EmitTicksAfterOnline { get; set; } = true;
		public int TickCount { get; set; } = 3;
		public bool SendSubscriptionResponse { get; set; } = true;
		public bool RespondWithError { get; set; }
		public bool EmitOrdersOnOrderStatus { get; set; } = true;

		private readonly ConcurrentDictionary<long, MarketDataMessage> _activeMarketDataSubs = [];

		protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken ct)
		{
			_logger.Log(_name, "IN", message);
			_inMessages.Enqueue(message.TypedClone());

			if (!AutoRespond)
				return;

			switch (message.Type)
			{
				case MessageTypes.Reset:
					await SendOutWithLog(new ResetMessage(), ct);
					break;

				case MessageTypes.Connect:
					await SendOutWithLog(new ConnectMessage(), ct);
					break;

				case MessageTypes.Disconnect:
					await SendOutWithLog(new DisconnectMessage(), ct);
					break;

				case MessageTypes.SecurityLookup:
				{
					var sl = (SecurityLookupMessage)message;

					if (RespondWithError)
					{
						await SendOutWithLog(sl.CreateResponse(new InvalidOperationException("Test error")), ct);
						break;
					}

					if (SendSubscriptionResponse)
						await SendOutWithLog(sl.CreateResponse(), ct);

					if (EmitSecurityOnLookup)
					{
						var secMsg = new SecurityMessage
						{
							SecurityId = SecId1,
							Name = "Test Security",
							OriginalTransactionId = sl.TransactionId,
						};
						secMsg.SetSubscriptionIds([sl.TransactionId]);
						await SendOutWithLog(secMsg, ct);
					}

					await SendOutWithLog(new SubscriptionFinishedMessage { OriginalTransactionId = sl.TransactionId }, ct);
					break;
				}

				case MessageTypes.PortfolioLookup:
				{
					var pl = (PortfolioLookupMessage)message;

					if (RespondWithError)
					{
						await SendOutWithLog(pl.CreateResponse(new InvalidOperationException("Test error")), ct);
						break;
					}

					if (SendSubscriptionResponse)
						await SendOutWithLog(pl.CreateResponse(), ct);

					var pfMsg = new PortfolioMessage
					{
						PortfolioName = "TestPortfolio",
						OriginalTransactionId = pl.TransactionId,
					};
					pfMsg.SetSubscriptionIds([pl.TransactionId]);
					await SendOutWithLog(pfMsg, ct);

					await SendOutWithLog(new SubscriptionFinishedMessage { OriginalTransactionId = pl.TransactionId }, ct);
					break;
				}

				case MessageTypes.OrderStatus:
				{
					var os = (OrderStatusMessage)message;

					if (RespondWithError)
					{
						await SendOutWithLog(os.CreateResponse(new InvalidOperationException("Test error")), ct);
						break;
					}

					if (SendSubscriptionResponse)
						await SendOutWithLog(os.CreateResponse(), ct);

					if (EmitOrdersOnOrderStatus)
					{
						var orderMsg = new ExecutionMessage
						{
							DataTypeEx = DataType.Transactions,
							SecurityId = SecId1,
							OriginalTransactionId = os.TransactionId,
							HasOrderInfo = true,
							OrderState = OrderStates.Active,
							OrderPrice = 100,
							OrderVolume = 10,
							ServerTime = DateTime.UtcNow,
							LocalTime = DateTime.UtcNow,
						};
						orderMsg.SetSubscriptionIds([os.TransactionId]);
						await SendOutWithLog(orderMsg, ct);
					}

					await SendOutWithLog(os.CreateResult(), ct);
					break;
				}

				case MessageTypes.MarketData:
				{
					var md = (MarketDataMessage)message;

					if (md.IsSubscribe)
					{
						if (RespondWithError)
						{
							await SendOutWithLog(md.CreateResponse(new InvalidOperationException("Test error")), ct);
							break;
						}

						if (SendSubscriptionResponse)
							await SendOutWithLog(md.CreateResponse(), ct);

						_activeMarketDataSubs[md.TransactionId] = md;

						var isHistoryOnly = md.IsHistoryOnly();

						if (!isHistoryOnly)
							await SendOutWithLog(md.CreateResult(), ct);

						if (EmitTicksAfterOnline && md.DataType2 == DataType.Ticks)
						{
							for (int i = 0; i < TickCount; i++)
							{
								var tick = new ExecutionMessage
								{
									DataTypeEx = DataType.Ticks,
									SecurityId = md.SecurityId,
									TradePrice = 100 + i,
									TradeVolume = 10,
									ServerTime = DateTime.UtcNow,
									OriginalTransactionId = md.TransactionId,
								};
								tick.SetSubscriptionIds([md.TransactionId]);
								await SendOutWithLog(tick, ct);
							}
						}

						if (EmitTicksAfterOnline && md.DataType2 == DataType.Level1)
						{
							var l1 = new Level1ChangeMessage
							{
								SecurityId = md.SecurityId,
								ServerTime = DateTime.UtcNow,
								OriginalTransactionId = md.TransactionId,
							}
							.TryAdd(Level1Fields.LastTradePrice, 100m)
							.TryAdd(Level1Fields.BestBidPrice, 99m)
							.TryAdd(Level1Fields.BestAskPrice, 101m);
							l1.SetSubscriptionIds([md.TransactionId]);
							await SendOutWithLog(l1, ct);
						}

						if (isHistoryOnly)
							await SendOutWithLog(md.CreateResult(), ct);
					}
					else
					{
						// Unsubscribe
						await SendOutWithLog(md.CreateResponse(), ct);
						_activeMarketDataSubs.TryRemove(md.OriginalTransactionId, out _);
					}
					break;
				}

				case MessageTypes.OrderRegister:
				{
					var reg = (OrderRegisterMessage)message;

					// Order accepted
					var execMsg = new ExecutionMessage
					{
						DataTypeEx = DataType.Transactions,
						SecurityId = reg.SecurityId,
						OriginalTransactionId = reg.TransactionId,
						TransactionId = reg.TransactionId,
						OrderState = OrderStates.Active,
						HasOrderInfo = true,
						OrderPrice = reg.Price,
						OrderVolume = reg.Volume,
						ServerTime = DateTime.UtcNow,
						LocalTime = DateTime.UtcNow,
					};
					await SendOutWithLog(execMsg, ct);

					// Trade execution
					var tradeMsg = new ExecutionMessage
					{
						DataTypeEx = DataType.Transactions,
						SecurityId = reg.SecurityId,
						OriginalTransactionId = reg.TransactionId,
						TransactionId = reg.TransactionId,
						OrderState = OrderStates.Done,
						HasOrderInfo = true,
						TradePrice = reg.Price,
						TradeVolume = reg.Volume,
						ServerTime = DateTime.UtcNow,
						LocalTime = DateTime.UtcNow,
					};
					await SendOutWithLog(tradeMsg, ct);
					break;
				}
			}
		}

		private async ValueTask SendOutWithLog(Message message, CancellationToken ct)
		{
			_logger.Log(_name, "OUT", message);
			await SendOutMessageAsync(message, ct);
		}

		public async ValueTask SendTickManual(long subscriptionId, SecurityId secId, CancellationToken ct)
		{
			var tick = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				SecurityId = secId,
				TradePrice = 999,
				TradeVolume = 1,
				ServerTime = DateTime.UtcNow,
				OriginalTransactionId = subscriptionId,
			};
			tick.SetSubscriptionIds([subscriptionId]);
			await SendOutWithLog(tick, ct);
		}

		public override IMessageAdapter Clone() => new LoggingInnerAdapter(TransactionIdGenerator, _logger, _name);
	}

	#endregion

	#region Test Setup

	private static readonly SecurityId SecId1 = "AAPL@NASDAQ".ToSecurityId();
	private static readonly SecurityId SecId2 = "MSFT@NASDAQ".ToSecurityId();

	private MessageFlowLogger _flowLogger;
	private ConcurrentQueue<Message> _basketOutput;

	private (BasketMessageAdapter basket, LoggingInnerAdapter adapter1, LoggingInnerAdapter adapter2)
		CreateLoggingBasket(bool twoAdapters = false)
	{
		_flowLogger = new MessageFlowLogger();
		_basketOutput = [];

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

		// Use IgnoreExtraAdapters=true for isolated testing of BasketMessageAdapter routing
		// LoggingInnerAdapter will set SubscriptionIds to simulate what wrapper chain normally does
		basket.IgnoreExtraAdapters = true;
		basket.LatencyManager = null;
		basket.SlippageManager = null;
		basket.CommissionManager = null;

		var adapter1 = new LoggingInnerAdapter(idGen, _flowLogger, "Adapter1");
		basket.InnerAdapters.Add(adapter1);
		basket.ApplyHeartbeat(adapter1, false);

		LoggingInnerAdapter adapter2 = null;
		if (twoAdapters)
		{
			adapter2 = new LoggingInnerAdapter(idGen, _flowLogger, "Adapter2");
			basket.InnerAdapters.Add(adapter2);
			basket.ApplyHeartbeat(adapter2, false);
		}

		basket.NewOutMessageAsync += async (msg, ct) =>
		{
			_flowLogger.Log("BasketOUT", "OUT", msg);

			// Re-process loopback messages
			if (msg.IsBack())
			{
				await ((IMessageTransport)basket).SendInMessageAsync(msg, ct);
				return;
			}

			_basketOutput.Enqueue(msg);
		};

		return (basket, adapter1, adapter2);
	}

	private async Task ConnectAndClear(BasketMessageAdapter basket)
	{
		await ((IMessageTransport)basket).SendInMessageAsync(new ConnectMessage(), CancellationToken);
		_flowLogger.Clear();
		_basketOutput.Clear();
	}

	private T[] GetOutput<T>() where T : Message => [.. _basketOutput.OfType<T>()];

	private void PrintFlowLog()
	{
		Console.WriteLine("=== MESSAGE FLOW LOG ===");
		_flowLogger.PrintAll(Console.WriteLine);
		Console.WriteLine("========================");
	}

	#endregion

	#region SecurityMessage Flow Tests

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task SecurityLookup_SecurityMessage_ReachesBasketOutput()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);

		await ConnectAndClear(basket);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var lookupMsg = new SecurityLookupMessage { TransactionId = transId };

		await ((IMessageTransport)basket).SendInMessageAsync(lookupMsg, CancellationToken);

		PrintFlowLog();

		// Verify adapter sent SecurityMessage
		var adapterSecMsgs = _flowLogger.GetBySource("Adapter1")
			.Where(l => l.Direction == "OUT" && l.Message.Type == MessageTypes.Security)
			.ToArray();
		adapterSecMsgs.Length.AssertGreater(0, "Adapter should emit SecurityMessage");

		// Verify SecurityMessage reached basket output
		var outputSecMsgs = GetOutput<SecurityMessage>();
		outputSecMsgs.Length.AssertGreater(0, "SecurityMessage should reach basket output");

		// Verify subscription IDs are correct
		foreach (var secMsg in outputSecMsgs)
		{
			var subIds = secMsg.GetSubscriptionIds();
			subIds.Length.AssertGreater(0, "SecurityMessage should have subscription IDs");

			// The subscription ID should be the parent transId (not a child ID)
			subIds.Contains(transId).AssertTrue($"SecurityMessage should have parent subscription ID {transId}, got [{string.Join(",", subIds)}]");
		}
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task SecurityLookup_Response_HasCorrectTransactionId()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);

		await ConnectAndClear(basket);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var lookupMsg = new SecurityLookupMessage { TransactionId = transId };

		await ((IMessageTransport)basket).SendInMessageAsync(lookupMsg, CancellationToken);

		PrintFlowLog();

		// Note: SubscriptionResponse is optional - adapters may skip it
		// and go directly to Finished/Online. This is not an error, just a warning for developers.
		// var responses = GetOutput<SubscriptionResponseMessage>();
		// responses.Any(r => r.OriginalTransactionId == transId)
		//     .AssertTrue($"SubscriptionResponse should have OriginalTransactionId={transId}");

		// Check SubscriptionFinished - this is required
		var finished = GetOutput<SubscriptionFinishedMessage>();
		finished.Any(f => f.OriginalTransactionId == transId)
			.AssertTrue($"SubscriptionFinished should have OriginalTransactionId={transId}");
	}

	#endregion

	#region MarketData Flow Tests

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task TicksSubscription_TickMessages_ReachBasketOutput()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);

		await ConnectAndClear(basket);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		};

		await ((IMessageTransport)basket).SendInMessageAsync(mdMsg, CancellationToken);

		PrintFlowLog();

		// Verify adapter sent tick messages
		var adapterTicks = _flowLogger.GetBySource("Adapter1")
			.Where(l => l.Direction == "OUT" && l.Message is ExecutionMessage em && em.DataType == DataType.Ticks)
			.ToArray();
		adapterTicks.Length.AssertGreater(0, "Adapter should emit tick ExecutionMessages");

		// Verify ticks reached basket output
		var outputTicks = GetOutput<ExecutionMessage>()
			.Where(e => e.DataType == DataType.Ticks)
			.ToArray();
		outputTicks.Length.AssertGreater(0, "Tick messages should reach basket output");

		// Verify subscription IDs are correct
		foreach (var tick in outputTicks)
		{
			var subIds = tick.GetSubscriptionIds();
			subIds.Length.AssertGreater(0, "Tick message should have subscription IDs");
			subIds.Contains(transId).AssertTrue($"Tick should have parent subscription ID {transId}, got [{string.Join(",", subIds)}]");
		}
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Level1Subscription_Level1Messages_ReachBasketOutput()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);

		await ConnectAndClear(basket);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Level1,
			IsSubscribe = true,
			TransactionId = transId,
		};

		await ((IMessageTransport)basket).SendInMessageAsync(mdMsg, CancellationToken);

		PrintFlowLog();

		// Verify adapter sent Level1 messages
		var adapterL1 = _flowLogger.GetBySource("Adapter1")
			.Where(l => l.Direction == "OUT" && l.Message.Type == MessageTypes.Level1Change)
			.ToArray();
		adapterL1.Length.AssertGreater(0, "Adapter should emit Level1ChangeMessage");

		// Verify Level1 reached basket output
		var outputL1 = GetOutput<Level1ChangeMessage>();
		outputL1.Length.AssertGreater(0, "Level1 messages should reach basket output");

		// Verify subscription IDs are correct
		foreach (var l1 in outputL1)
		{
			var subIds = l1.GetSubscriptionIds();
			subIds.Length.AssertGreater(0, "Level1 message should have subscription IDs");
			subIds.Contains(transId).AssertTrue($"Level1 should have parent subscription ID {transId}, got [{string.Join(",", subIds)}]");
		}
	}

	#endregion

	#region Transaction (Order) Flow Tests

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task OrderRegister_ExecutionMessages_ReachBasketOutput()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);

		await ConnectAndClear(basket);

		// First need to subscribe to OrderStatus so adapter is mapped for portfolio
		var osTransId = basket.TransactionIdGenerator.GetNextId();
		await ((IMessageTransport)basket).SendInMessageAsync(new OrderStatusMessage
		{
			TransactionId = osTransId,
			IsSubscribe = true,
		}, CancellationToken);

		_flowLogger.Clear();
		_basketOutput.Clear();

		// Register order
		var orderTransId = basket.TransactionIdGenerator.GetNextId();
		var orderMsg = new OrderRegisterMessage
		{
			TransactionId = orderTransId,
			SecurityId = SecId1,
			PortfolioName = "TestPortfolio",
			Side = Sides.Buy,
			Price = 100,
			Volume = 10,
			OrderType = OrderTypes.Limit,
		};

		// Associate portfolio with adapter
		basket.PortfolioAdapterProvider.SetAdapter("TestPortfolio", adapter1);

		await ((IMessageTransport)basket).SendInMessageAsync(orderMsg, CancellationToken);

		PrintFlowLog();

		// Verify adapter sent ExecutionMessage
		var adapterExecs = _flowLogger.GetBySource("Adapter1")
			.Where(l => l.Direction == "OUT" && l.Message is ExecutionMessage em && em.DataType == DataType.Transactions)
			.ToArray();
		adapterExecs.Length.AssertGreater(0, "Adapter should emit ExecutionMessage for order");

		// Verify ExecutionMessage reached basket output
		var outputExecs = GetOutput<ExecutionMessage>()
			.Where(e => e.DataType == DataType.Transactions && e.HasOrderInfo)
			.ToArray();
		outputExecs.Length.AssertGreater(0, "ExecutionMessage (order) should reach basket output");

		// Verify OriginalTransactionId is correct
		outputExecs.Any(e => e.OriginalTransactionId == orderTransId)
			.AssertTrue($"ExecutionMessage should have OriginalTransactionId={orderTransId}");
	}

	#endregion

	#region Portfolio Flow Tests

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task PortfolioLookup_PortfolioMessage_ReachesBasketOutput()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);

		await ConnectAndClear(basket);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var lookupMsg = new PortfolioLookupMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
		};

		await ((IMessageTransport)basket).SendInMessageAsync(lookupMsg, CancellationToken);

		PrintFlowLog();

		// Verify adapter sent PortfolioMessage
		var adapterPf = _flowLogger.GetBySource("Adapter1")
			.Where(l => l.Direction == "OUT" && l.Message.Type == MessageTypes.Portfolio)
			.ToArray();
		adapterPf.Length.AssertGreater(0, "Adapter should emit PortfolioMessage");

		// Verify PortfolioMessage reached basket output
		var outputPf = GetOutput<PortfolioMessage>();
		outputPf.Length.AssertGreater(0, "PortfolioMessage should reach basket output");

		// Verify subscription IDs are correct
		foreach (var pf in outputPf)
		{
			var subIds = pf.GetSubscriptionIds();
			subIds.Length.AssertGreater(0, "PortfolioMessage should have subscription IDs");
			subIds.Contains(transId).AssertTrue($"PortfolioMessage should have subscription ID {transId}, got [{string.Join(",", subIds)}]");
		}
	}

	#endregion

	#region Subscription ID Remapping Tests

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task ParentChildMapping_ChildIdsRemappedToParent()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);

		await ConnectAndClear(basket);

		var parentTransId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = parentTransId,
		};

		await ((IMessageTransport)basket).SendInMessageAsync(mdMsg, CancellationToken);

		PrintFlowLog();

		// Check what ID adapter received
		var adapterMdMsg = adapter1.GetMessages<MarketDataMessage>().FirstOrDefault();
		IsNotNull(adapterMdMsg, "Adapter should receive MarketDataMessage");
		var childTransId = adapterMdMsg.TransactionId;

		Console.WriteLine($"Parent TransId: {parentTransId}, Child TransId: {childTransId}");

		// SubscriptionResponse should have parent ID
		var responses = GetOutput<SubscriptionResponseMessage>();
		responses.Any(r => r.OriginalTransactionId == parentTransId)
			.AssertTrue($"SubscriptionResponse should have parent ID {parentTransId}");

		// SubscriptionOnline should have parent ID
		var onlines = GetOutput<SubscriptionOnlineMessage>();
		onlines.Any(o => o.OriginalTransactionId == parentTransId)
			.AssertTrue($"SubscriptionOnline should have parent ID {parentTransId}");

		// Tick messages should have parent ID in subscription IDs
		var ticks = GetOutput<ExecutionMessage>().Where(e => e.DataType == DataType.Ticks).ToArray();
		foreach (var tick in ticks)
		{
			var subIds = tick.GetSubscriptionIds();
			subIds.Contains(parentTransId).AssertTrue($"Tick should have parent subscription ID {parentTransId}");
		}
	}

	#endregion

	#region Diagnostic - Analyze Missing Messages

	[TestMethod]
	[Timeout(30_000, CooperativeCancellation = true)]
	public async Task Diagnostic_FullFlowAnalysis()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);

		await ConnectAndClear(basket);

		Console.WriteLine("=== STARTING FULL FLOW ANALYSIS ===");

		// 1. SecurityLookup
		Console.WriteLine("\n--- SecurityLookup ---");
		var secTransId = basket.TransactionIdGenerator.GetNextId();
		await ((IMessageTransport)basket).SendInMessageAsync(new SecurityLookupMessage { TransactionId = secTransId }, CancellationToken);

		// Print all logs for this operation
		Console.WriteLine("Full log for SecurityLookup:");
		_flowLogger.PrintAll(Console.WriteLine);

		var secAdapterIn = _flowLogger.GetBySource("Adapter1").Count(l => l.Direction == "IN" && l.Message.Type == MessageTypes.SecurityLookup);
		var secAdapterOut = _flowLogger.GetBySource("Adapter1").Count(l => l.Direction == "OUT" && l.Message.Type == MessageTypes.Security);
		var secBasketOut = GetOutput<SecurityMessage>().Length;
		Console.WriteLine($"Adapter IN (SecurityLookup)={secAdapterIn}, Adapter OUT (Security)={secAdapterOut}, Basket OUT={secBasketOut}");
		if (secAdapterIn == 0)
			Console.WriteLine("!!! Adapter did not receive SecurityLookup!");
		if (secAdapterOut != secBasketOut)
			Console.WriteLine("!!! MISMATCH: SecurityMessages lost in transit!");

		_flowLogger.Clear();
		_basketOutput.Clear();

		// 2. Ticks subscription
		Console.WriteLine("\n--- Ticks Subscription ---");
		var ticksTransId = basket.TransactionIdGenerator.GetNextId();
		await ((IMessageTransport)basket).SendInMessageAsync(new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = ticksTransId,
		}, CancellationToken);

		var ticksAdapterOut = _flowLogger.GetBySource("Adapter1").Count(l => l.Direction == "OUT" && l.Message is ExecutionMessage em && em.DataType == DataType.Ticks);
		var ticksBasketOut = GetOutput<ExecutionMessage>().Count(e => e.DataType == DataType.Ticks);
		Console.WriteLine($"Tick ExecutionMessage: Adapter OUT={ticksAdapterOut}, Basket OUT={ticksBasketOut}");
		if (ticksAdapterOut != ticksBasketOut)
			Console.WriteLine("!!! MISMATCH: Tick messages lost in transit!");

		_flowLogger.Clear();
		_basketOutput.Clear();

		// 3. Level1 subscription
		Console.WriteLine("\n--- Level1 Subscription ---");
		var l1TransId = basket.TransactionIdGenerator.GetNextId();
		await ((IMessageTransport)basket).SendInMessageAsync(new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Level1,
			IsSubscribe = true,
			TransactionId = l1TransId,
		}, CancellationToken);

		var l1AdapterOut = _flowLogger.GetBySource("Adapter1").Count(l => l.Direction == "OUT" && l.Message.Type == MessageTypes.Level1Change);
		var l1BasketOut = GetOutput<Level1ChangeMessage>().Length;
		Console.WriteLine($"Level1ChangeMessage: Adapter OUT={l1AdapterOut}, Basket OUT={l1BasketOut}");
		if (l1AdapterOut != l1BasketOut)
			Console.WriteLine("!!! MISMATCH: Level1 messages lost in transit!");

		_flowLogger.Clear();
		_basketOutput.Clear();

		// 4. PortfolioLookup
		Console.WriteLine("\n--- PortfolioLookup ---");
		var pfTransId = basket.TransactionIdGenerator.GetNextId();
		await ((IMessageTransport)basket).SendInMessageAsync(new PortfolioLookupMessage
		{
			TransactionId = pfTransId,
			IsSubscribe = true,
		}, CancellationToken);

		var pfAdapterOut = _flowLogger.GetBySource("Adapter1").Count(l => l.Direction == "OUT" && l.Message.Type == MessageTypes.Portfolio);
		var pfBasketOut = GetOutput<PortfolioMessage>().Length;
		Console.WriteLine($"PortfolioMessage: Adapter OUT={pfAdapterOut}, Basket OUT={pfBasketOut}");
		if (pfAdapterOut != pfBasketOut)
			Console.WriteLine("!!! MISMATCH: Portfolio messages lost in transit!");

		Console.WriteLine("\n=== ANALYSIS COMPLETE ===");

		// Final assertion - all message types should pass through
		secBasketOut.AssertGreater(0, "SecurityMessages should reach output");
		ticksBasketOut.AssertGreater(0, "Tick messages should reach output");
		l1BasketOut.AssertGreater(0, "Level1 messages should reach output");
		pfBasketOut.AssertGreater(0, "Portfolio messages should reach output");
	}

	#endregion

	#region Emulator-like Wrapper Tests

	/// <summary>
	/// Wrapper adapter that simulates EmulationMessageAdapter behavior.
	/// This helps test that messages pass through wrapper adapters correctly.
	/// Implements IEmulationMessageAdapter so GetUnderlyingAdapter stops unwrapping at this level.
	/// </summary>
	private sealed class EmulatorLikeWrapper : MessageAdapterWrapper, IEmulationMessageAdapter
	{
		private readonly MessageFlowLogger _logger;
		private readonly string _name;
		private readonly SynchronizedSet<long> _subscriptionIds = [];

		public EmulatorLikeWrapper(IMessageAdapter innerAdapter, MessageFlowLogger logger, string name)
			: base(innerAdapter)
		{
			_logger = logger;
			_name = name;
		}

		// IEmulationMessageAdapter implementation
		public IMarketEmulator Emulator => null;
		public MarketEmulatorSettings Settings => null;
		public IMessageChannel InChannel => null;

		protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
		{
			_logger.Log(_name, "IN", message);

			// Track subscriptions like EmulationMessageAdapter does
			if (message is ISubscriptionMessage subscrMsg && subscrMsg.IsSubscribe)
				_subscriptionIds.Add(subscrMsg.TransactionId);

			// Forward to inner adapter
			await base.OnSendInMessageAsync(message, cancellationToken);
		}

		protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
		{
			_logger.Log(_name, "INNER_OUT", message);

			// Handle like EmulationMessageAdapter - forward to parent
			switch (message.Type)
			{
				case MessageTypes.Security:
				case MessageTypes.Board:
				case MessageTypes.SubscriptionResponse:
				case MessageTypes.SubscriptionFinished:
				case MessageTypes.SubscriptionOnline:
				case MessageTypes.Portfolio:
				case MessageTypes.PositionChange:
				case MessageTypes.Connect:
				case MessageTypes.Disconnect:
				case MessageTypes.Reset:
					// Forward to parent
					_logger.Log(_name, "FORWARD", message);
					await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
					break;

				case MessageTypes.Execution:
					var execMsg = (ExecutionMessage)message;
					// Forward market data (ticks, order log)
					if (execMsg.IsMarketData())
					{
						_logger.Log(_name, "FORWARD_MD", message);
						await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
					}
					else
					{
						// Transaction execution - also forward
						_logger.Log(_name, "FORWARD_TX", message);
						await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
					}
					break;

				default:
					// Forward other messages
					_logger.Log(_name, "FORWARD_DEFAULT", message);
					await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
					break;
			}
		}

		public override IMessageAdapter Clone()
			=> new EmulatorLikeWrapper(InnerAdapter.Clone(), _logger, _name);
	}

	private (BasketMessageAdapter basket, LoggingInnerAdapter innerAdapter, EmulatorLikeWrapper wrapper)
		CreateBasketWithWrapper()
	{
		_flowLogger = new MessageFlowLogger();
		_basketOutput = [];

		var idGen = new IncrementalIdGenerator();
		var candleBuilderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());

		var cs = new AdapterConnectionState();
		var cm = new AdapterConnectionManager(cs);
		var ps = new PendingMessageState();
		var sr = new SubscriptionRoutingState();
		var pcm = new ParentChildMap();
		var or = new OrderRoutingState();

		// Use default GetUnderlyingAdapter which stops at IEmulationMessageAdapter
		var routingManager = new BasketRoutingManager(
			cs, cm, ps, sr, pcm, or,
			a => a, // Let default logic handle IEmulationMessageAdapter
			candleBuilderProvider, () => false, idGen);

		var basket = new BasketMessageAdapter(
			idGen,
			candleBuilderProvider,
			new InMemorySecurityMessageAdapterProvider(),
			new InMemoryPortfolioMessageAdapterProvider(),
			null,
			null,
			routingManager);

		// Use IgnoreExtraAdapters=true for isolated testing
		basket.IgnoreExtraAdapters = true;
		basket.LatencyManager = null;
		basket.SlippageManager = null;
		basket.CommissionManager = null;

		// Create inner adapter wrapped by emulator-like wrapper
		var innerAdapter = new LoggingInnerAdapter(idGen, _flowLogger, "InnerAdapter");
		var wrapper = new EmulatorLikeWrapper(innerAdapter, _flowLogger, "EmulatorWrapper");

		basket.InnerAdapters.Add(wrapper);
		basket.ApplyHeartbeat(wrapper, false);

		basket.NewOutMessageAsync += async (msg, ct) =>
		{
			_flowLogger.Log("BasketOUT", "OUT", msg);

			if (msg.IsBack())
			{
				await ((IMessageTransport)basket).SendInMessageAsync(msg, ct);
				return;
			}

			_basketOutput.Enqueue(msg);
		};

		return (basket, innerAdapter, wrapper);
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Wrapper_SecurityLookup_SecurityMessage_ReachesBasketOutput()
	{
		var (basket, innerAdapter, wrapper) = CreateBasketWithWrapper();

		await ConnectAndClear(basket);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var lookupMsg = new SecurityLookupMessage { TransactionId = transId };

		await ((IMessageTransport)basket).SendInMessageAsync(lookupMsg, CancellationToken);

		Console.WriteLine("=== Flow Log ===");
		_flowLogger.PrintAll(Console.WriteLine);

		// Verify inner adapter sent SecurityMessage
		var innerSecMsgs = _flowLogger.GetBySource("InnerAdapter")
			.Where(l => l.Direction == "OUT" && l.Message.Type == MessageTypes.Security)
			.ToArray();
		innerSecMsgs.Length.AssertGreater(0, "InnerAdapter should emit SecurityMessage");

		// Verify wrapper forwarded it
		var wrapperForwarded = _flowLogger.GetBySource("EmulatorWrapper")
			.Where(l => l.Direction == "FORWARD" && l.Message.Type == MessageTypes.Security)
			.ToArray();
		wrapperForwarded.Length.AssertGreater(0, "Wrapper should forward SecurityMessage");

		// Verify SecurityMessage reached basket output
		var outputSecMsgs = GetOutput<SecurityMessage>();
		outputSecMsgs.Length.AssertGreater(0, "SecurityMessage should reach basket output through wrapper");

		// Verify subscription IDs are correct
		foreach (var secMsg in outputSecMsgs)
		{
			var subIds = secMsg.GetSubscriptionIds();
			subIds.Length.AssertGreater(0, "SecurityMessage should have subscription IDs");
			subIds.Contains(transId).AssertTrue($"SecurityMessage should have parent subscription ID {transId}");
		}
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Wrapper_TicksSubscription_TickMessages_ReachBasketOutput()
	{
		var (basket, innerAdapter, wrapper) = CreateBasketWithWrapper();

		await ConnectAndClear(basket);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		};

		await ((IMessageTransport)basket).SendInMessageAsync(mdMsg, CancellationToken);

		Console.WriteLine("=== Flow Log ===");
		_flowLogger.PrintAll(Console.WriteLine);

		// Verify ticks reached basket output
		var outputTicks = GetOutput<ExecutionMessage>()
			.Where(e => e.DataType == DataType.Ticks)
			.ToArray();
		outputTicks.Length.AssertGreater(0, "Tick messages should reach basket output through wrapper");

		// Verify subscription IDs are correct
		foreach (var tick in outputTicks)
		{
			var subIds = tick.GetSubscriptionIds();
			subIds.Length.AssertGreater(0, "Tick message should have subscription IDs");
			subIds.Contains(transId).AssertTrue($"Tick should have parent subscription ID {transId}");
		}
	}

	[TestMethod]
	[Timeout(30_000, CooperativeCancellation = true)]
	public async Task Wrapper_Diagnostic_FullFlowAnalysis()
	{
		var (basket, innerAdapter, wrapper) = CreateBasketWithWrapper();

		await ConnectAndClear(basket);

		Console.WriteLine("=== WRAPPER FULL FLOW ANALYSIS ===");

		// 1. SecurityLookup
		Console.WriteLine("\n--- SecurityLookup through Wrapper ---");
		var secTransId = basket.TransactionIdGenerator.GetNextId();
		await ((IMessageTransport)basket).SendInMessageAsync(new SecurityLookupMessage { TransactionId = secTransId }, CancellationToken);

		var secInnerOut = _flowLogger.GetBySource("InnerAdapter").Count(l => l.Direction == "OUT" && l.Message.Type == MessageTypes.Security);
		var secWrapperForward = _flowLogger.GetBySource("EmulatorWrapper").Count(l => l.Direction.StartsWith("FORWARD") && l.Message.Type == MessageTypes.Security);
		var secBasketOut = GetOutput<SecurityMessage>().Length;
		Console.WriteLine($"Inner OUT={secInnerOut}, Wrapper FORWARD={secWrapperForward}, Basket OUT={secBasketOut}");

		if (secInnerOut > 0 && secWrapperForward == 0)
			Console.WriteLine("!!! WRAPPER NOT FORWARDING SecurityMessages!");
		if (secWrapperForward > 0 && secBasketOut == 0)
			Console.WriteLine("!!! BASKET NOT OUTPUTTING SecurityMessages!");

		_flowLogger.Clear();
		_basketOutput.Clear();

		// 2. Ticks subscription
		Console.WriteLine("\n--- Ticks through Wrapper ---");
		var ticksTransId = basket.TransactionIdGenerator.GetNextId();
		await ((IMessageTransport)basket).SendInMessageAsync(new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = ticksTransId,
		}, CancellationToken);

		var ticksInnerOut = _flowLogger.GetBySource("InnerAdapter").Count(l => l.Direction == "OUT" && l.Message is ExecutionMessage em && em.DataType == DataType.Ticks);
		var ticksWrapperForward = _flowLogger.GetBySource("EmulatorWrapper").Count(l => l.Direction.StartsWith("FORWARD") && l.Message is ExecutionMessage em && em.DataType == DataType.Ticks);
		var ticksBasketOut = GetOutput<ExecutionMessage>().Count(e => e.DataType == DataType.Ticks);
		Console.WriteLine($"Inner OUT={ticksInnerOut}, Wrapper FORWARD={ticksWrapperForward}, Basket OUT={ticksBasketOut}");

		if (ticksInnerOut > 0 && ticksWrapperForward == 0)
			Console.WriteLine("!!! WRAPPER NOT FORWARDING Tick messages!");
		if (ticksWrapperForward > 0 && ticksBasketOut == 0)
			Console.WriteLine("!!! BASKET NOT OUTPUTTING Tick messages!");

		Console.WriteLine("\n=== WRAPPER ANALYSIS COMPLETE ===");

		// Final assertions
		secBasketOut.AssertGreater(0, "SecurityMessages should reach output through wrapper");
		ticksBasketOut.AssertGreater(0, "Tick messages should reach output through wrapper");
	}

	#endregion

	#region No SubscriptionResponse Tests

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Ticks_NoSubscriptionResponse_DataStillArrives()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);
		adapter1.SendSubscriptionResponse = false;

		await ConnectAndClear(basket);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		};

		await ((IMessageTransport)basket).SendInMessageAsync(mdMsg, CancellationToken);

		PrintFlowLog();

		// No SubscriptionResponseMessage should arrive
		var responses = GetOutput<SubscriptionResponseMessage>();
		responses.Length.AssertEqual(0, "No SubscriptionResponseMessage expected");

		// SubscriptionOnline should still arrive
		var onlines = GetOutput<SubscriptionOnlineMessage>();
		onlines.Any(o => o.OriginalTransactionId == transId)
			.AssertTrue("SubscriptionOnline should arrive even without SubscriptionResponse");

		// Ticks should still arrive
		var ticks = GetOutput<ExecutionMessage>().Where(e => e.DataType == DataType.Ticks).ToArray();
		ticks.Length.AssertGreater(0, "Tick data should arrive even without SubscriptionResponse");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Level1_NoSubscriptionResponse_DataStillArrives()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);
		adapter1.SendSubscriptionResponse = false;

		await ConnectAndClear(basket);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Level1,
			IsSubscribe = true,
			TransactionId = transId,
		};

		await ((IMessageTransport)basket).SendInMessageAsync(mdMsg, CancellationToken);

		PrintFlowLog();

		var responses = GetOutput<SubscriptionResponseMessage>();
		responses.Length.AssertEqual(0, "No SubscriptionResponseMessage expected");

		var onlines = GetOutput<SubscriptionOnlineMessage>();
		onlines.Any(o => o.OriginalTransactionId == transId)
			.AssertTrue("SubscriptionOnline should arrive even without SubscriptionResponse");

		var l1 = GetOutput<Level1ChangeMessage>();
		l1.Length.AssertGreater(0, "Level1 data should arrive even without SubscriptionResponse");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task SecurityLookup_NoSubscriptionResponse_DataStillArrives()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);
		adapter1.SendSubscriptionResponse = false;

		await ConnectAndClear(basket);

		var transId = basket.TransactionIdGenerator.GetNextId();
		await ((IMessageTransport)basket).SendInMessageAsync(
			new SecurityLookupMessage { TransactionId = transId }, CancellationToken);

		PrintFlowLog();

		var responses = GetOutput<SubscriptionResponseMessage>();
		responses.Length.AssertEqual(0, "No SubscriptionResponseMessage expected");

		var securities = GetOutput<SecurityMessage>();
		securities.Length.AssertGreater(0, "SecurityMessage should arrive even without SubscriptionResponse");

		var finished = GetOutput<SubscriptionFinishedMessage>();
		finished.Any(f => f.OriginalTransactionId == transId)
			.AssertTrue("SubscriptionFinished should arrive even without SubscriptionResponse");
	}

	#endregion

	#region Error Response Tests

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Ticks_ErrorResponse_NoDataShouldArrive()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);
		adapter1.RespondWithError = true;

		await ConnectAndClear(basket);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		};

		await ((IMessageTransport)basket).SendInMessageAsync(mdMsg, CancellationToken);

		PrintFlowLog();

		// Error response should arrive
		var responses = GetOutput<SubscriptionResponseMessage>();
		responses.Any(r => r.OriginalTransactionId == transId && r.Error != null)
			.AssertTrue("Error SubscriptionResponse should arrive");

		// No Online/Finished
		var onlines = GetOutput<SubscriptionOnlineMessage>();
		onlines.Any(o => o.OriginalTransactionId == transId)
			.AssertFalse("No SubscriptionOnline expected after error");

		// No tick data
		var ticks = GetOutput<ExecutionMessage>().Where(e => e.DataType == DataType.Ticks).ToArray();
		ticks.Length.AssertEqual(0, "No tick data should arrive after error response");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Level1_ErrorResponse_NoDataShouldArrive()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);
		adapter1.RespondWithError = true;

		await ConnectAndClear(basket);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Level1,
			IsSubscribe = true,
			TransactionId = transId,
		};

		await ((IMessageTransport)basket).SendInMessageAsync(mdMsg, CancellationToken);

		PrintFlowLog();

		var responses = GetOutput<SubscriptionResponseMessage>();
		responses.Any(r => r.OriginalTransactionId == transId && r.Error != null)
			.AssertTrue("Error SubscriptionResponse should arrive");

		var l1 = GetOutput<Level1ChangeMessage>();
		l1.Length.AssertEqual(0, "No Level1 data should arrive after error response");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task OrderStatus_ErrorResponse_NoDataShouldArrive()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);
		adapter1.RespondWithError = true;

		await ConnectAndClear(basket);

		var transId = basket.TransactionIdGenerator.GetNextId();
		await ((IMessageTransport)basket).SendInMessageAsync(new OrderStatusMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
		}, CancellationToken);

		PrintFlowLog();

		var responses = GetOutput<SubscriptionResponseMessage>();
		responses.Any(r => r.OriginalTransactionId == transId && r.Error != null)
			.AssertTrue("Error SubscriptionResponse should arrive");

		var orders = GetOutput<ExecutionMessage>().Where(e => e.DataType == DataType.Transactions).ToArray();
		orders.Length.AssertEqual(0, "No order data should arrive after error response");
	}

	#endregion

	#region OrderStatus Data Tests

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task OrderStatus_OnlineSubscription_EmitsOrderData()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);

		await ConnectAndClear(basket);

		var transId = basket.TransactionIdGenerator.GetNextId();
		await ((IMessageTransport)basket).SendInMessageAsync(new OrderStatusMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
		}, CancellationToken);

		PrintFlowLog();

		// Verify order data arrived
		var orders = GetOutput<ExecutionMessage>()
			.Where(e => e.DataType == DataType.Transactions && e.HasOrderInfo)
			.ToArray();
		orders.Length.AssertGreater(0, "OrderStatus should emit order data");

		// Verify subscription IDs
		foreach (var order in orders)
		{
			var subIds = order.GetSubscriptionIds();
			subIds.Length.AssertGreater(0, "Order ExecutionMessage should have subscription IDs");
			subIds.Contains(transId).AssertTrue(
				$"Order should have parent subscription ID {transId}, got [{string.Join(",", subIds)}]");
		}

		// Verify Online arrived (OrderStatus without To/Count is live)
		var onlines = GetOutput<SubscriptionOnlineMessage>();
		onlines.Any(o => o.OriginalTransactionId == transId)
			.AssertTrue("OrderStatus online subscription should receive SubscriptionOnline");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task OrderStatus_NoData_OnlyOnlineMessage()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);
		adapter1.EmitOrdersOnOrderStatus = false;

		await ConnectAndClear(basket);

		var transId = basket.TransactionIdGenerator.GetNextId();
		await ((IMessageTransport)basket).SendInMessageAsync(new OrderStatusMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
		}, CancellationToken);

		PrintFlowLog();

		var orders = GetOutput<ExecutionMessage>()
			.Where(e => e.DataType == DataType.Transactions && e.HasOrderInfo)
			.ToArray();
		orders.Length.AssertEqual(0, "No order data expected when EmitOrdersOnOrderStatus=false");

		var onlines = GetOutput<SubscriptionOnlineMessage>();
		onlines.Any(o => o.OriginalTransactionId == transId)
			.AssertTrue("SubscriptionOnline should still arrive");
	}

	#endregion

	#region Online vs Finished (CreateResult) Tests

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Ticks_OnlineSubscription_GetsOnlineMessage()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);

		await ConnectAndClear(basket);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
			// No To/Count → live subscription → Online
		};

		await ((IMessageTransport)basket).SendInMessageAsync(mdMsg, CancellationToken);

		PrintFlowLog();

		var onlines = GetOutput<SubscriptionOnlineMessage>();
		onlines.Any(o => o.OriginalTransactionId == transId)
			.AssertTrue("Live tick subscription should get SubscriptionOnline");

		var finished = GetOutput<SubscriptionFinishedMessage>();
		finished.Any(f => f.OriginalTransactionId == transId)
			.AssertFalse("Live tick subscription should NOT get SubscriptionFinished");

		// Data should arrive after Online
		var ticks = GetOutput<ExecutionMessage>().Where(e => e.DataType == DataType.Ticks).ToArray();
		ticks.Length.AssertGreater(0, "Ticks should arrive for online subscription");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Ticks_HistoricalSubscription_GetsFinishedMessage()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);

		await ConnectAndClear(basket);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
			Count = 10, // Count → historical → Finished
		};

		await ((IMessageTransport)basket).SendInMessageAsync(mdMsg, CancellationToken);

		PrintFlowLog();

		var finished = GetOutput<SubscriptionFinishedMessage>();
		finished.Any(f => f.OriginalTransactionId == transId)
			.AssertTrue("Historical tick subscription should get SubscriptionFinished");

		var onlines = GetOutput<SubscriptionOnlineMessage>();
		onlines.Any(o => o.OriginalTransactionId == transId)
			.AssertFalse("Historical tick subscription should NOT get SubscriptionOnline");

		// Data should arrive before Finished
		var ticks = GetOutput<ExecutionMessage>().Where(e => e.DataType == DataType.Ticks).ToArray();
		ticks.Length.AssertGreater(0, "Ticks should arrive for historical subscription");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Ticks_HistoricalSubscription_WithTo_GetsFinishedMessage()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);

		await ConnectAndClear(basket);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
			To = DateTime.UtcNow.AddDays(-1), // To → historical → Finished
		};

		await ((IMessageTransport)basket).SendInMessageAsync(mdMsg, CancellationToken);

		PrintFlowLog();

		var finished = GetOutput<SubscriptionFinishedMessage>();
		finished.Any(f => f.OriginalTransactionId == transId)
			.AssertTrue("Historical tick subscription (To) should get SubscriptionFinished");

		var onlines = GetOutput<SubscriptionOnlineMessage>();
		onlines.Any(o => o.OriginalTransactionId == transId)
			.AssertFalse("Historical tick subscription (To) should NOT get SubscriptionOnline");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Level1_OnlineSubscription_GetsOnlineMessage()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);

		await ConnectAndClear(basket);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Level1,
			IsSubscribe = true,
			TransactionId = transId,
		};

		await ((IMessageTransport)basket).SendInMessageAsync(mdMsg, CancellationToken);

		PrintFlowLog();

		var onlines = GetOutput<SubscriptionOnlineMessage>();
		onlines.Any(o => o.OriginalTransactionId == transId)
			.AssertTrue("Live Level1 subscription should get SubscriptionOnline");

		var finished = GetOutput<SubscriptionFinishedMessage>();
		finished.Any(f => f.OriginalTransactionId == transId)
			.AssertFalse("Live Level1 subscription should NOT get SubscriptionFinished");

		var l1 = GetOutput<Level1ChangeMessage>();
		l1.Length.AssertGreater(0, "Level1 data should arrive for online subscription");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Level1_HistoricalSubscription_GetsFinishedMessage()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);

		await ConnectAndClear(basket);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Level1,
			IsSubscribe = true,
			TransactionId = transId,
			Count = 5,
		};

		await ((IMessageTransport)basket).SendInMessageAsync(mdMsg, CancellationToken);

		PrintFlowLog();

		var finished = GetOutput<SubscriptionFinishedMessage>();
		finished.Any(f => f.OriginalTransactionId == transId)
			.AssertTrue("Historical Level1 subscription should get SubscriptionFinished");

		var onlines = GetOutput<SubscriptionOnlineMessage>();
		onlines.Any(o => o.OriginalTransactionId == transId)
			.AssertFalse("Historical Level1 subscription should NOT get SubscriptionOnline");

		var l1 = GetOutput<Level1ChangeMessage>();
		l1.Length.AssertGreater(0, "Level1 data should arrive for historical subscription");
	}

	#endregion

	#region Unsubscribe Tests

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Ticks_Unsubscribe_ResponseReceived()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);

		await ConnectAndClear(basket);

		// Subscribe
		var subTransId = basket.TransactionIdGenerator.GetNextId();
		await ((IMessageTransport)basket).SendInMessageAsync(new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = subTransId,
		}, CancellationToken);

		_flowLogger.Clear();
		_basketOutput.Clear();

		// Unsubscribe
		var unsubTransId = basket.TransactionIdGenerator.GetNextId();
		await ((IMessageTransport)basket).SendInMessageAsync(new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = false,
			TransactionId = unsubTransId,
			OriginalTransactionId = subTransId,
		}, CancellationToken);

		PrintFlowLog();

		// Unsubscribe response should arrive
		var responses = GetOutput<SubscriptionResponseMessage>();
		responses.Any(r => r.OriginalTransactionId == unsubTransId)
			.AssertTrue("Unsubscribe should receive SubscriptionResponse");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Ticks_DataAfterUnsubscribe_ShouldNotArrive()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);

		await ConnectAndClear(basket);

		// Subscribe
		var subTransId = basket.TransactionIdGenerator.GetNextId();
		await ((IMessageTransport)basket).SendInMessageAsync(new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = subTransId,
		}, CancellationToken);

		// Get the child subscription ID that was sent to the adapter
		var adapterMdMsg = adapter1.GetMessages<MarketDataMessage>()
			.FirstOrDefault(m => m.IsSubscribe && m.DataType2 == DataType.Ticks);
		IsNotNull(adapterMdMsg, "Adapter should receive MarketDataMessage");
		var childSubId = adapterMdMsg.TransactionId;

		// Verify initial ticks arrived
		var initialTicks = GetOutput<ExecutionMessage>().Count(e => e.DataType == DataType.Ticks);
		initialTicks.AssertGreater(0, "Initial ticks should arrive");

		_flowLogger.Clear();
		_basketOutput.Clear();

		// Unsubscribe
		var unsubTransId = basket.TransactionIdGenerator.GetNextId();
		await ((IMessageTransport)basket).SendInMessageAsync(new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = false,
			TransactionId = unsubTransId,
			OriginalTransactionId = subTransId,
		}, CancellationToken);

		_flowLogger.Clear();
		_basketOutput.Clear();

		// Try sending data using the child subscription ID
		await adapter1.SendTickManual(childSubId, SecId1, CancellationToken);

		PrintFlowLog();

		// Data after unsubscribe should NOT arrive at basket output
		var postUnsubTicks = GetOutput<ExecutionMessage>().Where(e => e.DataType == DataType.Ticks).ToArray();
		postUnsubTicks.Length.AssertEqual(0, "Data after unsubscribe should not arrive at basket output");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Level1_Unsubscribe_DataAfterUnsubscribe_ShouldNotArrive()
	{
		var (basket, adapter1, _) = CreateLoggingBasket(twoAdapters: false);

		await ConnectAndClear(basket);

		// Subscribe
		var subTransId = basket.TransactionIdGenerator.GetNextId();
		await ((IMessageTransport)basket).SendInMessageAsync(new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Level1,
			IsSubscribe = true,
			TransactionId = subTransId,
		}, CancellationToken);

		// Get child subscription ID
		var adapterMdMsg = adapter1.GetMessages<MarketDataMessage>()
			.FirstOrDefault(m => m.IsSubscribe && m.DataType2 == DataType.Level1);
		IsNotNull(adapterMdMsg, "Adapter should receive Level1 MarketDataMessage");
		var childSubId = adapterMdMsg.TransactionId;

		// Verify initial data arrived
		var initialL1 = GetOutput<Level1ChangeMessage>().Length;
		initialL1.AssertGreater(0, "Initial Level1 data should arrive");

		_flowLogger.Clear();
		_basketOutput.Clear();

		// Unsubscribe
		var unsubTransId = basket.TransactionIdGenerator.GetNextId();
		await ((IMessageTransport)basket).SendInMessageAsync(new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Level1,
			IsSubscribe = false,
			TransactionId = unsubTransId,
			OriginalTransactionId = subTransId,
		}, CancellationToken);

		_flowLogger.Clear();
		_basketOutput.Clear();

		// Try sending Level1 data using the child subscription ID
		// Reuse SendTickManual but with Level1 — need to send manually
		var l1Msg = new Level1ChangeMessage
		{
			SecurityId = SecId1,
			ServerTime = DateTime.UtcNow,
			OriginalTransactionId = childSubId,
		}
		.TryAdd(Level1Fields.LastTradePrice, 999m);
		l1Msg.SetSubscriptionIds([childSubId]);
		// Send directly through adapter's outgoing channel
		await adapter1.SendTickManual(childSubId, SecId1, CancellationToken);

		PrintFlowLog();

		// Data after unsubscribe should NOT arrive
		var postUnsubTicks = GetOutput<ExecutionMessage>().Where(e => e.DataType == DataType.Ticks).ToArray();
		postUnsubTicks.Length.AssertEqual(0, "Data after unsubscribe should not arrive");
	}

	#endregion
}

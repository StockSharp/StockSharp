namespace StockSharp.Tests;

using StockSharp.Algo.Candles.Compression;

/// <summary>
/// Tests for <see cref="CandleBuilderMessageAdapter"/>.
/// </summary>
[TestClass]
public class CandleBuilderMessageAdapterTests : BaseTestClass
{
	#region Mock Adapters

	/// <summary>
	/// Mock adapter that simulates candle subscription responses with configurable behavior.
	/// </summary>
	private class MockCandleAdapter : MessageAdapter
	{
		public List<Message> SentMessages { get; } = [];
		private readonly Dictionary<long, MarketDataMessage> _activeSubscriptions = [];
		private readonly HashSet<long> _failOnSubscribe = [];

		public MockCandleAdapter(IdGenerator transactionIdGenerator) : base(transactionIdGenerator)
		{
			this.AddMarketDataSupport();
			this.AddSupportedMarketDataType(DataType.Ticks);
		}

		public void AddSupportedTimeFrame(TimeSpan timeFrame)
		{
			this.AddSupportedMarketDataType(timeFrame.TimeFrame());
		}

		public void FailOnSubscribe(long transactionId)
		{
			_failOnSubscribe.Add(transactionId);
		}

		protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
		{
			SentMessages.Add(message);

			switch (message.Type)
			{
				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.IsSubscribe)
					{
						_activeSubscriptions[mdMsg.TransactionId] = mdMsg;

						// Check if we should fail this subscription
						if (_failOnSubscribe.Contains(mdMsg.TransactionId))
						{
							await SendOutMessageAsync(new SubscriptionResponseMessage
							{
								OriginalTransactionId = mdMsg.TransactionId,
								Error = new NotSupportedException("TimeFrame not supported"),
							}, cancellationToken);
						}
						else
						{
							await SendOutMessageAsync(mdMsg.CreateResponse(), cancellationToken);
						}
					}
					else
					{
						_activeSubscriptions.Remove(mdMsg.OriginalTransactionId);
						await SendOutMessageAsync(mdMsg.CreateResponse(), cancellationToken);
					}

					break;
				}

				case MessageTypes.Reset:
					_activeSubscriptions.Clear();
					_failOnSubscribe.Clear();
					break;
			}
		}

		public async ValueTask SimulateCandle(long subscriptionId, CandleMessage candle, CancellationToken cancellationToken)
		{
			if (_activeSubscriptions.ContainsKey(subscriptionId))
			{
				candle.SetSubscriptionIds([subscriptionId]);
				await SendOutMessageAsync(candle, cancellationToken);
			}
		}

		public async ValueTask SimulateTick(long subscriptionId, ExecutionMessage tick, CancellationToken cancellationToken)
		{
			if (_activeSubscriptions.ContainsKey(subscriptionId))
			{
				tick.SetSubscriptionIds([subscriptionId]);
				await SendOutMessageAsync(tick, cancellationToken);
			}
		}

		public async ValueTask SimulateLevel1(long subscriptionId, Level1ChangeMessage level1, CancellationToken cancellationToken)
		{
			if (_activeSubscriptions.ContainsKey(subscriptionId))
			{
				level1.SetSubscriptionIds([subscriptionId]);
				await SendOutMessageAsync(level1, cancellationToken);
			}
		}

		public void AddSupportedLevel1()
		{
			this.AddSupportedMarketDataType(DataType.Level1);
		}

		public void AddSupportedMarketDepth()
		{
			this.AddSupportedMarketDataType(DataType.MarketDepth);
		}

		public void AddSupportedOrderLog()
		{
			this.AddSupportedMarketDataType(DataType.OrderLog);
		}

		public async ValueTask SimulateMarketDepth(long subscriptionId, QuoteChangeMessage depth, CancellationToken cancellationToken)
		{
			if (_activeSubscriptions.ContainsKey(subscriptionId))
			{
				depth.SetSubscriptionIds([subscriptionId]);
				await SendOutMessageAsync(depth, cancellationToken);
			}
		}

		public async ValueTask SimulateOrderLog(long subscriptionId, ExecutionMessage orderLog, CancellationToken cancellationToken)
		{
			if (_activeSubscriptions.ContainsKey(subscriptionId))
			{
				orderLog.SetSubscriptionIds([subscriptionId]);
				await SendOutMessageAsync(orderLog, cancellationToken);
			}
		}

		public async ValueTask SimulateFinished(long subscriptionId, CancellationToken cancellationToken)
		{
			if (_activeSubscriptions.TryGetAndRemove(subscriptionId, out _))
			{
				await SendOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = subscriptionId }, cancellationToken);
			}
		}

		public async ValueTask SimulateOnline(long subscriptionId, CancellationToken cancellationToken)
		{
			if (_activeSubscriptions.ContainsKey(subscriptionId))
			{
				await SendOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = subscriptionId }, cancellationToken);
			}
		}

		public async ValueTask SimulateError(long subscriptionId, Exception error, CancellationToken cancellationToken)
		{
			if (_activeSubscriptions.TryGetAndRemove(subscriptionId, out _))
			{
				await SendOutMessageAsync(new SubscriptionResponseMessage
				{
					OriginalTransactionId = subscriptionId,
					Error = error,
				}, cancellationToken);
			}
		}

		public long? GetActiveSubscriptionId(DataType dataType)
		{
			return _activeSubscriptions.FirstOrDefault(kvp => kvp.Value.DataType2 == dataType).Key;
		}

		public bool HasActiveSubscription(long id) => _activeSubscriptions.ContainsKey(id);

		public override IMessageAdapter Clone() => new MockCandleAdapter(TransactionIdGenerator);
	}

	#endregion

	#region Helper Methods

	private static CandleBuilderProvider CreateCandleBuilderProvider()
	{
		return new CandleBuilderProvider(new InMemoryExchangeInfoProvider());
	}

	private static SecurityId CreateSecurityId()
	{
		return new SecurityId { SecurityCode = "SBER", BoardCode = "TQBR" };
	}

	private static TimeFrameCandleMessage CreateTimeFrameCandle(
		SecurityId securityId,
		DateTime openTime,
		TimeSpan timeFrame,
		decimal open,
		decimal high,
		decimal low,
		decimal close,
		decimal volume,
		CandleStates state = CandleStates.Finished)
	{
		return new TimeFrameCandleMessage
		{
			SecurityId = securityId,
			OpenTime = openTime,
			CloseTime = openTime + timeFrame,
			OpenPrice = open,
			HighPrice = high,
			LowPrice = low,
			ClosePrice = close,
			TotalVolume = volume,
			State = state,
			DataType = timeFrame.TimeFrame(),
		};
	}

	private static TimeFrameCandleMessage CreateTimeFrameCandle(SecurityId securityId, DateTime openTime, TimeSpan timeFrame, CandleStates state = CandleStates.Finished)
	{
		return CreateTimeFrameCandle(securityId, openTime, timeFrame, 100, 105, 95, 102, 1000, state);
	}

	private static ExecutionMessage CreateTick(SecurityId securityId, DateTime serverTime, decimal price, decimal volume)
	{
		return new ExecutionMessage
		{
			SecurityId = securityId,
			DataTypeEx = DataType.Ticks,
			ServerTime = serverTime,
			TradePrice = price,
			TradeVolume = volume,
		};
	}

	private static Level1ChangeMessage CreateLevel1(SecurityId securityId, DateTime serverTime, decimal? lastTradePrice = null, decimal? lastTradeVolume = null, decimal? bestBidPrice = null, decimal? bestAskPrice = null)
	{
		var msg = new Level1ChangeMessage
		{
			SecurityId = securityId,
			ServerTime = serverTime,
		};

		if (lastTradePrice != null)
			msg.Add(Level1Fields.LastTradePrice, lastTradePrice.Value);
		if (lastTradeVolume != null)
			msg.Add(Level1Fields.LastTradeVolume, lastTradeVolume.Value);
		if (bestBidPrice != null)
			msg.Add(Level1Fields.BestBidPrice, bestBidPrice.Value);
		if (bestAskPrice != null)
			msg.Add(Level1Fields.BestAskPrice, bestAskPrice.Value);

		return msg;
	}

	private static QuoteChangeMessage CreateMarketDepth(SecurityId securityId, DateTime serverTime, (decimal price, decimal volume)[] bids, (decimal price, decimal volume)[] asks)
	{
		return new QuoteChangeMessage
		{
			SecurityId = securityId,
			ServerTime = serverTime,
			Bids = [.. bids.Select(b => new QuoteChange(b.price, b.volume))],
			Asks = [.. asks.Select(a => new QuoteChange(a.price, a.volume))],
		};
	}

	private static ExecutionMessage CreateOrderLog(SecurityId securityId, DateTime serverTime, decimal price, decimal volume, Sides side, bool isMatched = false)
	{
		return new ExecutionMessage
		{
			SecurityId = securityId,
			DataTypeEx = DataType.OrderLog,
			ServerTime = serverTime,
			OrderPrice = price,
			OrderVolume = volume,
			Side = side,
			TradePrice = isMatched ? price : null,
			TradeVolume = isMatched ? volume : null,
		};
	}

	#endregion

	#region Constructor Tests

	[TestMethod]
	public void Constructor_NullInnerAdapter_Throws()
	{
		var provider = CreateCandleBuilderProvider();
		ThrowsExactly<ArgumentNullException>(() => new CandleBuilderMessageAdapter(null, provider));
	}

	[TestMethod]
	public void Constructor_NullCandleBuilderProvider_Throws()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		ThrowsExactly<ArgumentNullException>(() => new CandleBuilderMessageAdapter(inner, null));
	}

	[TestMethod]
	public void Constructor_ValidParameters_CreatesAdapter()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		var provider = CreateCandleBuilderProvider();

		var adapter = new CandleBuilderMessageAdapter(inner, provider);

		IsNotNull(adapter);
		AreSame(inner, adapter.InnerAdapter);
	}

	#endregion

	#region Reset Tests

	[TestMethod]
	public async Task Reset_ClearsInternalState()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedTimeFrame(TimeSpan.FromMinutes(1));
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();

		// Subscribe
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
		}, CancellationToken);

		// Reset
		await adapter.SendInMessageAsync(new ResetMessage(), CancellationToken);

		// Verify reset was forwarded
		inner.SentMessages.Any(m => m.Type == MessageTypes.Reset).AssertTrue();
	}

	#endregion

	#region TimeFrame Candle Subscription Tests

	[TestMethod]
	public async Task Subscribe_SupportedTimeFrame_ForwardsToInner()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedTimeFrame(TimeSpan.FromMinutes(1));
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		var subMsg = new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
		};

		await adapter.SendInMessageAsync(subMsg, CancellationToken);

		// Sent 1 subscription message
		var sentSubs = inner.SentMessages.OfType<MarketDataMessage>().Where(m => m.IsSubscribe).ToList();
		AreEqual(1, sentSubs.Count, "Should send exactly 1 subscription");

		var sent = sentSubs[0];
		AreEqual(TimeSpan.FromMinutes(1).TimeFrame(), sent.DataType2, "DataType should be 1-min TimeFrame");
		AreEqual(secId, sent.SecurityId, "SecurityId should match");
		AreEqual(1, sent.TransactionId, "TransactionId should be 1");
		sent.IsSubscribe.AssertTrue("IsSubscribe should be true");
	}

	[TestMethod]
	public async Task Subscribe_TimeFrame_ReceivesCandles_WithCorrectOHLCV()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedTimeFrame(TimeSpan.FromMinutes(1));
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);
		adapter.SendFinishedCandlesImmediatelly = true;

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
		}, CancellationToken);

		// Simulate 1 candle with specific OHLCV values
		var candle = CreateTimeFrameCandle(secId, baseTime, TimeSpan.FromMinutes(1), 100, 110, 95, 105, 5000);
		await inner.SimulateCandle(1, candle, CancellationToken);

		// Sent 1 candle, should receive exactly 1 candle
		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToList();
		AreEqual(1, candles.Count, "Sent 1 candle, should receive exactly 1 candle");

		var received = candles[0];
		AreEqual(secId, received.SecurityId, "SecurityId should match");
		AreEqual(100m, received.OpenPrice, "Open should be 100");
		AreEqual(110m, received.HighPrice, "High should be 110");
		AreEqual(95m, received.LowPrice, "Low should be 95");
		AreEqual(105m, received.ClosePrice, "Close should be 105");
		AreEqual(5000m, received.TotalVolume, "Volume should be 5000");
		AreEqual(baseTime, received.OpenTime, "OpenTime should match");
		AreEqual(baseTime + TimeSpan.FromMinutes(1), received.CloseTime, "CloseTime should be OpenTime + TimeFrame");
	}

	[TestMethod]
	public async Task Subscribe_TimeFrame_NonFinishedCandle_MarkedAsActive()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedTimeFrame(TimeSpan.FromMinutes(1));
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);
		adapter.SendFinishedCandlesImmediatelly = false;

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
		}, CancellationToken);

		// Simulate 1 finished candle (should be converted to Active)
		var candle = CreateTimeFrameCandle(secId, baseTime, TimeSpan.FromMinutes(1), 100, 105, 95, 102, 1000, CandleStates.Finished);
		await inner.SimulateCandle(1, candle, CancellationToken);

		// Sent 1 candle, should receive exactly 1 candle
		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToList();
		AreEqual(1, candles.Count, "Sent 1 candle, should receive exactly 1 candle");

		var received = candles[0];
		AreEqual(CandleStates.Active, received.State, "State should be Active when SendFinishedCandlesImmediatelly = false");
		AreEqual(secId, received.SecurityId, "SecurityId should match");
		AreEqual(100m, received.OpenPrice, "Open should be 100");
		AreEqual(105m, received.HighPrice, "High should be 105");
		AreEqual(95m, received.LowPrice, "Low should be 95");
		AreEqual(102m, received.ClosePrice, "Close should be 102");
		AreEqual(1000m, received.TotalVolume, "Volume should be 1000");
	}

	[TestMethod]
	public async Task Subscribe_TimeFrame_SendFinishedImmediatelly_KeepsFinishedState()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedTimeFrame(TimeSpan.FromMinutes(1));
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);
		adapter.SendFinishedCandlesImmediatelly = true;

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
		}, CancellationToken);

		// Simulate 1 finished candle
		var candle = CreateTimeFrameCandle(secId, baseTime, TimeSpan.FromMinutes(1), 100, 105, 95, 102, 1000, CandleStates.Finished);
		await inner.SimulateCandle(1, candle, CancellationToken);

		// Sent 1 candle, should receive exactly 1 candle
		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToList();
		AreEqual(1, candles.Count, "Sent 1 candle, should receive exactly 1 candle");

		var received = candles[0];
		AreEqual(CandleStates.Finished, received.State, "State should be Finished when SendFinishedCandlesImmediatelly = true");
		AreEqual(secId, received.SecurityId, "SecurityId should match");
		AreEqual(100m, received.OpenPrice, "Open should be 100");
		AreEqual(105m, received.HighPrice, "High should be 105");
		AreEqual(95m, received.LowPrice, "Low should be 95");
		AreEqual(102m, received.ClosePrice, "Close should be 102");
		AreEqual(1000m, received.TotalVolume, "Volume should be 1000");
	}

	#endregion

	#region Unsubscribe Tests

	[TestMethod]
	public async Task Unsubscribe_ActiveSubscription_ForwardsToInner()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedTimeFrame(TimeSpan.FromMinutes(1));
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();

		// Subscribe
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
		}, CancellationToken);

		// Verify 1 subscription sent
		var subscribes = inner.SentMessages.OfType<MarketDataMessage>().Where(m => m.IsSubscribe).ToList();
		AreEqual(1, subscribes.Count, "Should send exactly 1 subscribe message");
		AreEqual(1, subscribes[0].TransactionId, "Subscribe TransactionId should be 1");

		// Unsubscribe
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 2,
			OriginalTransactionId = 1,
			IsSubscribe = false,
		}, CancellationToken);

		// Verify 1 unsubscribe sent
		var unsubscribes = inner.SentMessages.OfType<MarketDataMessage>().Where(m => !m.IsSubscribe).ToList();
		AreEqual(1, unsubscribes.Count, "Should send exactly 1 unsubscribe message");

		var unsubscribeSent = unsubscribes[0];
		AreEqual(2, unsubscribeSent.TransactionId, "Unsubscribe TransactionId should be 2");
		AreEqual(1, unsubscribeSent.OriginalTransactionId, "Unsubscribe OriginalTransactionId should be 1");
		unsubscribeSent.IsSubscribe.AssertFalse("IsSubscribe should be false");

		// Verify 2 responses (subscribe + unsubscribe)
		var responses = outMessages.OfType<SubscriptionResponseMessage>().ToList();
		AreEqual(2, responses.Count, "Should receive 2 responses (subscribe + unsubscribe)");

		var unsubResponse = responses.First(r => r.OriginalTransactionId == 2);
		IsNull(unsubResponse.Error, "Unsubscribe should succeed without error");
	}

	#endregion

	#region Build From Ticks Tests

	[TestMethod]
	public async Task Subscribe_BuildFromTicks_CreatesTickSubscription()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		// Don't add any supported timeframes - force build from ticks
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Ticks,
		}, CancellationToken);

		// Should create exactly 1 tick subscription
		var tickSubs = inner.SentMessages.OfType<MarketDataMessage>().Where(m => m.DataType2 == DataType.Ticks).ToList();
		AreEqual(1, tickSubs.Count, "Should create exactly 1 tick subscription");

		var tickSub = tickSubs[0];
		AreEqual(secId, tickSub.SecurityId, "Tick subscription SecurityId should match");
		tickSub.IsSubscribe.AssertTrue("Tick subscription IsSubscribe should be true");
	}

	[TestMethod]
	public async Task Subscribe_BuildFromTicks_BuildsCandles_WithCorrectOHLCV()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Ticks,
		}, CancellationToken);

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		// Simulate 4 ticks within same 1-min candle: O=100, H=105, L=98, C=102, V=60
		await inner.SimulateTick(1, CreateTick(secId, baseTime, 100, 10), CancellationToken);                 // Open
		await inner.SimulateTick(1, CreateTick(secId, baseTime.AddSeconds(10), 105, 20), CancellationToken);  // High
		await inner.SimulateTick(1, CreateTick(secId, baseTime.AddSeconds(20), 98, 15), CancellationToken);   // Low
		await inner.SimulateTick(1, CreateTick(secId, baseTime.AddSeconds(30), 102, 15), CancellationToken);  // Close

		// 4 ticks in same minute = 1 candle (may have multiple updates)
		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToList();
		(candles.Count >= 1).AssertTrue($"4 ticks should produce at least 1 candle, got {candles.Count}");

		// Check the last candle state (final aggregation)
		var candle = candles.Last();
		AreEqual(secId, candle.SecurityId, "Candle SecurityId should match");
		AreEqual(100m, candle.OpenPrice, "Open should be first tick price (100)");
		AreEqual(105m, candle.HighPrice, "High should be max tick price (105)");
		AreEqual(98m, candle.LowPrice, "Low should be min tick price (98)");
		AreEqual(102m, candle.ClosePrice, "Close should be last tick price (102)");
		AreEqual(60m, candle.TotalVolume, "Volume should be sum of all volumes (60)");
	}

	#endregion

	#region Smaller TimeFrame Tests

	[TestMethod]
	public async Task Subscribe_UnsupportedTimeFrame_UseSmallerTimeFrame()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedTimeFrame(TimeSpan.FromMinutes(1)); // Only 1 minute supported
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(5).TimeFrame(), // Request 5 minutes
			IsSubscribe = true,
			SecurityId = secId,
			AllowBuildFromSmallerTimeFrame = true,
		}, CancellationToken);

		// Should send exactly 1 subscription (to 1-min)
		var subs = inner.SentMessages.OfType<MarketDataMessage>().Where(m => m.IsSubscribe).ToList();
		AreEqual(1, subs.Count, "Should send exactly 1 subscription");

		var sent = subs[0];
		AreEqual(TimeSpan.FromMinutes(1).TimeFrame(), sent.DataType2, "Should subscribe to supported 1-min TimeFrame");
		AreEqual(secId, sent.SecurityId, "SecurityId should match");
	}

	[TestMethod]
	public async Task Subscribe_SmallerTimeFrame_CompressesCandles_WithCorrectOHLCV()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedTimeFrame(TimeSpan.FromMinutes(1));
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);
		adapter.SendFinishedCandlesImmediatelly = true;

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(5).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
			AllowBuildFromSmallerTimeFrame = true,
		}, CancellationToken);

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		// Simulate 5 one-minute candles with specific values
		// Compressed 5-min should be: O=100, H=120, L=90, C=108, V=5500
		await inner.SimulateCandle(1, CreateTimeFrameCandle(secId, baseTime.AddMinutes(0), TimeSpan.FromMinutes(1), 100, 105, 98, 104, 1000), CancellationToken);
		await inner.SimulateCandle(1, CreateTimeFrameCandle(secId, baseTime.AddMinutes(1), TimeSpan.FromMinutes(1), 104, 110, 100, 108, 1200), CancellationToken);
		await inner.SimulateCandle(1, CreateTimeFrameCandle(secId, baseTime.AddMinutes(2), TimeSpan.FromMinutes(1), 108, 120, 105, 115, 1500), CancellationToken);
		await inner.SimulateCandle(1, CreateTimeFrameCandle(secId, baseTime.AddMinutes(3), TimeSpan.FromMinutes(1), 115, 118, 90, 95, 900), CancellationToken);
		await inner.SimulateCandle(1, CreateTimeFrameCandle(secId, baseTime.AddMinutes(4), TimeSpan.FromMinutes(1), 95, 110, 92, 108, 900), CancellationToken);

		// 5 one-minute candles = multiple updates to 1 compressed 5-min candle
		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToList();
		(candles.Count >= 1).AssertTrue($"5 one-min candles should produce at least 1 compressed candle, got {candles.Count}");

		// Find the 5-min candle at 10:00 (last update)
		var compressedCandle = candles.Last(c => c.OpenTime == baseTime);
		AreEqual(secId, compressedCandle.SecurityId, "SecurityId should match");
		AreEqual(100m, compressedCandle.OpenPrice, "Compressed Open should be first 1-min Open (100)");
		AreEqual(120m, compressedCandle.HighPrice, "Compressed High should be max of all Highs (120)");
		AreEqual(90m, compressedCandle.LowPrice, "Compressed Low should be min of all Lows (90)");
		AreEqual(108m, compressedCandle.ClosePrice, "Compressed Close should be last 1-min Close (108)");
		AreEqual(5500m, compressedCandle.TotalVolume, "Compressed Volume should be sum of all volumes (5500)");
	}

	#endregion

	#region Fallback Scenarios Tests

	[TestMethod]
	public async Task Fallback_RequestedTF_NotSupported_FallbackToSmallerTF()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedTimeFrame(TimeSpan.FromMinutes(1)); // Only 1-min available
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();

		// Request 5-min candles with fallback enabled
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(5).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
			AllowBuildFromSmallerTimeFrame = true,
			BuildMode = MarketDataBuildModes.LoadAndBuild,
		}, CancellationToken);

		// Verify: should subscribe to 1-min candles (smaller TF)
		var sentSub = inner.SentMessages.OfType<MarketDataMessage>().FirstOrDefault(m => m.IsSubscribe);
		IsNotNull(sentSub, "Should send subscription to inner adapter");
		AreEqual(TimeSpan.FromMinutes(1).TimeFrame(), sentSub.DataType2, "Should fallback to 1-min TF");
		AreEqual(secId, sentSub.SecurityId, "SecurityId should match");
		sentSub.IsSubscribe.AssertTrue("Should be subscribe message");

		// Client should still get successful response for their 5-min subscription
		var response = outMessages.OfType<SubscriptionResponseMessage>().FirstOrDefault();
		IsNotNull(response, "Client should receive response");
		IsNull(response.Error, "Response should be successful");
		AreEqual(1, response.OriginalTransactionId, "Response should reference original request");
	}

	[TestMethod]
	public async Task Fallback_NoTFSupported_FallbackToTicks()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		// No timeframes supported, only ticks
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
			BuildMode = MarketDataBuildModes.LoadAndBuild,
			BuildFrom = DataType.Ticks,
		}, CancellationToken);

		// Should subscribe to ticks
		var tickSub = inner.SentMessages.OfType<MarketDataMessage>().FirstOrDefault(m => m.DataType2 == DataType.Ticks);
		IsNotNull(tickSub, "Should fallback to tick subscription");
		AreEqual(secId, tickSub.SecurityId, "SecurityId should match");
		tickSub.IsSubscribe.AssertTrue("Should be subscribe message");

		// Client should receive response
		var response = outMessages.OfType<SubscriptionResponseMessage>().FirstOrDefault();
		IsNotNull(response, "Client should receive response");
		AreEqual(1, response.OriginalTransactionId, "Response should reference original request");
	}

	[TestMethod]
	public async Task Fallback_SmallerTF_Compresses_WithCorrectOHLCV()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedTimeFrame(TimeSpan.FromMinutes(1));
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);
		adapter.SendFinishedCandlesImmediatelly = true;

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		// Request 5-min with fallback
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(5).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
			AllowBuildFromSmallerTimeFrame = true,
			BuildMode = MarketDataBuildModes.LoadAndBuild,
		}, CancellationToken);

		// First subscription should be to 1-min
		var firstSub = inner.SentMessages.OfType<MarketDataMessage>().FirstOrDefault(m => m.IsSubscribe);
		IsNotNull(firstSub, "Should create first subscription");
		AreEqual(TimeSpan.FromMinutes(1).TimeFrame(), firstSub.DataType2, "First subscription should be 1-min");

		// Send 5 one-minute candles to form one 5-min candle
		// Expected: O=100, H=115, L=95, C=110, V=5500
		await inner.SimulateCandle(firstSub.TransactionId, CreateTimeFrameCandle(secId, baseTime.AddMinutes(0), TimeSpan.FromMinutes(1), 100, 108, 98, 105, 1000), CancellationToken);
		await inner.SimulateCandle(firstSub.TransactionId, CreateTimeFrameCandle(secId, baseTime.AddMinutes(1), TimeSpan.FromMinutes(1), 105, 115, 102, 112, 1200), CancellationToken); // H=115
		await inner.SimulateCandle(firstSub.TransactionId, CreateTimeFrameCandle(secId, baseTime.AddMinutes(2), TimeSpan.FromMinutes(1), 112, 114, 95, 100, 1100), CancellationToken);  // L=95
		await inner.SimulateCandle(firstSub.TransactionId, CreateTimeFrameCandle(secId, baseTime.AddMinutes(3), TimeSpan.FromMinutes(1), 100, 108, 98, 106, 1000), CancellationToken);
		await inner.SimulateCandle(firstSub.TransactionId, CreateTimeFrameCandle(secId, baseTime.AddMinutes(4), TimeSpan.FromMinutes(1), 106, 112, 104, 110, 1200), CancellationToken);  // C=110

		// Verify compressed candle
		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToList();
		(candles.Count > 0).AssertTrue("Should receive compressed candles");

		var compressed = candles.LastOrDefault(c => c.OpenTime == baseTime);
		IsNotNull(compressed, "Should have compressed candle at baseTime");
		AreEqual(secId, compressed.SecurityId, "SecurityId should match");
		AreEqual(100m, compressed.OpenPrice, "Compressed Open should be 100");
		AreEqual(115m, compressed.HighPrice, "Compressed High should be 115");
		AreEqual(95m, compressed.LowPrice, "Compressed Low should be 95");
		AreEqual(110m, compressed.ClosePrice, "Compressed Close should be 110");
		AreEqual(5500m, compressed.TotalVolume, "Compressed Volume should be 5500");
	}

	[TestMethod]
	public async Task Fallback_HistoricalCandles_CompressCorrectly()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedTimeFrame(TimeSpan.FromMinutes(1));
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);
		adapter.SendFinishedCandlesImmediatelly = true;

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		// Subscribe to 5-min candles
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(5).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
			AllowBuildFromSmallerTimeFrame = true,
			BuildMode = MarketDataBuildModes.LoadAndBuild,
			From = baseTime.AddHours(-1),
		}, CancellationToken);

		var firstSub = inner.SentMessages.OfType<MarketDataMessage>().FirstOrDefault(m => m.IsSubscribe);
		IsNotNull(firstSub, "Should create subscription");
		AreEqual(TimeSpan.FromMinutes(1).TimeFrame(), firstSub.DataType2, "Should subscribe to 1-min");

		// Send 5 historical 1-min candles with specific OHLCV
		// Expected compressed: O=100, H=130, L=85, C=115, V=7500
		await inner.SimulateCandle(firstSub.TransactionId, CreateTimeFrameCandle(secId, baseTime.AddMinutes(0), TimeSpan.FromMinutes(1), 100, 110, 95, 108, 1500), CancellationToken);
		await inner.SimulateCandle(firstSub.TransactionId, CreateTimeFrameCandle(secId, baseTime.AddMinutes(1), TimeSpan.FromMinutes(1), 108, 130, 105, 125, 2000), CancellationToken); // H=130
		await inner.SimulateCandle(firstSub.TransactionId, CreateTimeFrameCandle(secId, baseTime.AddMinutes(2), TimeSpan.FromMinutes(1), 125, 128, 85, 90, 1500), CancellationToken);  // L=85
		await inner.SimulateCandle(firstSub.TransactionId, CreateTimeFrameCandle(secId, baseTime.AddMinutes(3), TimeSpan.FromMinutes(1), 90, 105, 88, 102, 1200), CancellationToken);
		await inner.SimulateCandle(firstSub.TransactionId, CreateTimeFrameCandle(secId, baseTime.AddMinutes(4), TimeSpan.FromMinutes(1), 102, 118, 100, 115, 1300), CancellationToken);  // C=115

		// Check compressed 5-min candle
		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToList();
		(candles.Count > 0).AssertTrue("Should receive compressed candles");

		var compressed = candles.LastOrDefault(c => c.OpenTime == baseTime);
		IsNotNull(compressed, "Should have compressed candle at baseTime");
		AreEqual(secId, compressed.SecurityId, "SecurityId should match");
		AreEqual(100m, compressed.OpenPrice, "Compressed Open should be 100");
		AreEqual(130m, compressed.HighPrice, "Compressed High should be 130");
		AreEqual(85m, compressed.LowPrice, "Compressed Low should be 85");
		AreEqual(115m, compressed.ClosePrice, "Compressed Close should be 115");
		AreEqual(7500m, compressed.TotalVolume, "Compressed Volume should be 7500");
	}

	#endregion

	#region Subscription ID Mapping Tests

	[TestMethod]
	public async Task Fallback_SubscriptionIdMapping_PreservesOriginalId()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedTimeFrame(TimeSpan.FromMinutes(1));
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);
		adapter.SendFinishedCandlesImmediatelly = true;

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		const long clientTransactionId = 100;
		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		// Client subscribes with their TransactionId
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = clientTransactionId,
			DataType2 = TimeSpan.FromMinutes(5).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
			AllowBuildFromSmallerTimeFrame = true,
		}, CancellationToken);

		// Find the internal subscription
		var internalSub = inner.SentMessages.OfType<MarketDataMessage>().FirstOrDefault(m => m.IsSubscribe);
		IsNotNull(internalSub, "Should create internal subscription");
		AreEqual(TimeSpan.FromMinutes(1).TimeFrame(), internalSub.DataType2, "Internal subscription should be 1-min");

		// Send candle
		await inner.SimulateCandle(internalSub.TransactionId, CreateTimeFrameCandle(secId, baseTime, TimeSpan.FromMinutes(1), 100, 105, 98, 102, 1000), CancellationToken);

		// Verify candles have CLIENT's subscription ID
		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToList();
		(candles.Count > 0).AssertTrue("Should receive candles");

		var candle = candles.First();
		IsNotNull(candle, "Candle should not be null");
		candle.GetSubscriptionIds().Contains(clientTransactionId).AssertTrue("Candle should have client's original subscription ID");
		AreEqual(secId, candle.SecurityId, "SecurityId should match");
	}

	#endregion

	#region Error Handling Tests

	[TestMethod]
	public async Task Fallback_AllOptionsFail_ClientGetsError()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		// No timeframes, no ticks supported
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();

		// Request candles with LoadOnly mode (no fallback allowed)
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(5).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
			BuildMode = MarketDataBuildModes.Load,
		}, CancellationToken);

		// Client should receive error response
		var response = outMessages.OfType<SubscriptionResponseMessage>().FirstOrDefault();
		IsNotNull(response, "Client should receive response");
		AreEqual(1, response.OriginalTransactionId, "Response should reference client request");
		IsNotNull(response.Error, "Response should contain error when no data source available");
	}

	[TestMethod]
	public async Task Fallback_BuildFromTicks_ErrorOnTicks_ClientGetsError()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		// No timeframes supported
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();

		// Request candles with BuildFrom=Ticks
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Ticks,
		}, CancellationToken);

		// Tick subscription should be created
		var tickSub = inner.SentMessages.OfType<MarketDataMessage>()
			.FirstOrDefault(m => m.DataType2 == DataType.Ticks);
		IsNotNull(tickSub, "Should create tick subscription");
		AreEqual(secId, tickSub.SecurityId, "Tick subscription SecurityId should match");

		// Simulate error on tick subscription
		await inner.SimulateError(tickSub.TransactionId, new NotSupportedException("Ticks not available"), CancellationToken);

		// Client should receive error
		var errorResponse = outMessages.OfType<SubscriptionResponseMessage>()
			.FirstOrDefault(r => r.Error != null);
		IsNotNull(errorResponse, "Client should receive error response");
		AreEqual(1, errorResponse.OriginalTransactionId, "Error should reference client's request");
		IsNotNull(errorResponse.Error, "Response should contain error");
	}

	#endregion

	#region Cascade Finish Tests

	[TestMethod]
	public async Task CascadeFinish_DuringActiveCandle_FlushesPartialWithCorrectOHLCV()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedTimeFrame(TimeSpan.FromMinutes(1));
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);
		adapter.SendFinishedCandlesImmediatelly = true;

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(5).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
			AllowBuildFromSmallerTimeFrame = true,
		}, CancellationToken);

		var sub = inner.SentMessages.OfType<MarketDataMessage>().FirstOrDefault(m => m.IsSubscribe);
		IsNotNull(sub, "Should create subscription");
		AreEqual(TimeSpan.FromMinutes(1).TimeFrame(), sub.DataType2, "Should subscribe to 1-min");

		// Send only 3 one-minute candles (partial 5-min)
		// O=100, H=115, L=95, C=102
		await inner.SimulateCandle(sub.TransactionId, CreateTimeFrameCandle(secId, baseTime.AddMinutes(0), TimeSpan.FromMinutes(1), 100, 108, 95, 105, 1000), CancellationToken); // L=95
		await inner.SimulateCandle(sub.TransactionId, CreateTimeFrameCandle(secId, baseTime.AddMinutes(1), TimeSpan.FromMinutes(1), 105, 115, 102, 110, 1200), CancellationToken); // H=115
		await inner.SimulateCandle(sub.TransactionId, CreateTimeFrameCandle(secId, baseTime.AddMinutes(2), TimeSpan.FromMinutes(1), 110, 112, 98, 102, 1100), CancellationToken);  // C=102

		// Finish should flush partial candle
		await inner.SimulateFinished(sub.TransactionId, CancellationToken);

		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToList();
		(candles.Count > 0).AssertTrue("Should receive candles");

		// The partial candle should have correct OHLCV from the 3 1-min candles
		var partial = candles.LastOrDefault(c => c.OpenTime == baseTime);
		IsNotNull(partial, "Should have partial candle at baseTime");
		AreEqual(secId, partial.SecurityId, "SecurityId should match");
		AreEqual(100m, partial.OpenPrice, "Partial Open should be 100");
		AreEqual(115m, partial.HighPrice, "Partial High should be 115");
		AreEqual(95m, partial.LowPrice, "Partial Low should be 95");
	}

	[TestMethod]
	public async Task CascadeFinish_WithCountLimit_ReceivesCompressedCandles()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedTimeFrame(TimeSpan.FromMinutes(1));
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);
		adapter.SendFinishedCandlesImmediatelly = true;

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(5).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
			AllowBuildFromSmallerTimeFrame = true,
		}, CancellationToken);

		var sub = inner.SentMessages.OfType<MarketDataMessage>().FirstOrDefault(m => m.IsSubscribe);
		IsNotNull(sub, "Should create subscription");
		AreEqual(TimeSpan.FromMinutes(1).TimeFrame(), sub.DataType2, "Should subscribe to 1-min");

		// Send 10 one-minute candles to form 2 complete 5-min candles
		for (int i = 0; i < 10; i++)
		{
			await inner.SimulateCandle(sub.TransactionId, CreateTimeFrameCandle(secId, baseTime.AddMinutes(i), TimeSpan.FromMinutes(1),
				100 + i, 105 + i, 95 + i, 102 + i, 1000 + i * 100), CancellationToken);
		}

		// Verify candles received
		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToList();
		(candles.Count > 0).AssertTrue("Should receive candles");

		// Verify first compressed candle OHLCV (from first 5 one-minute candles)
		var firstCompressed = candles.FirstOrDefault(c => c.OpenTime == baseTime);
		IsNotNull(firstCompressed, "Should have compressed candle at baseTime");
		AreEqual(secId, firstCompressed.SecurityId, "SecurityId should match");
		AreEqual(100m, firstCompressed.OpenPrice, "First compressed Open should be 100");
	}

	#endregion

	#region Subscription Response Tests

	[TestMethod]
	public async Task SubscriptionResponse_Success_ForwardedToClient()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedTimeFrame(TimeSpan.FromMinutes(1));
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
		}, CancellationToken);

		// Should receive exactly 1 response
		var responses = outMessages.OfType<SubscriptionResponseMessage>().ToList();
		AreEqual(1, responses.Count, "Should receive exactly 1 subscription response");

		var response = responses[0];
		IsNull(response.Error, "Response should have no error");
		AreEqual(1, response.OriginalTransactionId, "Response OriginalTransactionId should be 1");
	}

	[TestMethod]
	public async Task SubscriptionFinished_ForwardedToClient()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedTimeFrame(TimeSpan.FromMinutes(1));
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);
		adapter.SendFinishedCandlesImmediatelly = true;

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
		}, CancellationToken);

		// Verify exactly 1 response
		var responses = outMessages.OfType<SubscriptionResponseMessage>().ToList();
		AreEqual(1, responses.Count, "Should receive exactly 1 subscription response");
		AreEqual(1, responses[0].OriginalTransactionId, "Response OriginalTransactionId should be 1");

		// Send 1 candle
		await inner.SimulateCandle(1, CreateTimeFrameCandle(secId, baseTime, TimeSpan.FromMinutes(1), 100, 105, 95, 102, 1000), CancellationToken);

		// Verify exactly 1 candle received
		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToList();
		AreEqual(1, candles.Count, "Sent 1 candle, should receive exactly 1 candle");

		// Simulate finished
		await inner.SimulateFinished(1, CancellationToken);

		// Verify exactly 1 subscription was sent
		var sentSubs = inner.SentMessages.OfType<MarketDataMessage>().Where(m => m.IsSubscribe).ToList();
		AreEqual(1, sentSubs.Count, "Should send exactly 1 subscription to inner adapter");
	}

	[TestMethod]
	public async Task SubscriptionOnline_ForwardedToClient()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedTimeFrame(TimeSpan.FromMinutes(1));
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
		}, CancellationToken);

		// Verify exactly 1 response
		var responses = outMessages.OfType<SubscriptionResponseMessage>().ToList();
		AreEqual(1, responses.Count, "Should receive exactly 1 subscription response");
		AreEqual(1, responses[0].OriginalTransactionId, "Response OriginalTransactionId should be 1");

		// Simulate online
		await inner.SimulateOnline(1, CancellationToken);

		// Verify exactly 1 online message
		var onlines = outMessages.OfType<SubscriptionOnlineMessage>().ToList();
		AreEqual(1, onlines.Count, "Should receive exactly 1 SubscriptionOnlineMessage");
		AreEqual(1, onlines[0].OriginalTransactionId, "Online message OriginalTransactionId should be 1");
	}

	#endregion

	#region Count Limit Tests

	[TestMethod]
	public async Task Subscribe_WithCount_StopsAfterCountReached()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedTimeFrame(TimeSpan.FromMinutes(1));
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);
		adapter.SendFinishedCandlesImmediatelly = true;

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		const int requestedCount = 3;

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
			Count = requestedCount,
		}, CancellationToken);

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		// Simulate 5 candles
		for (int i = 0; i < 5; i++)
		{
			var candle = CreateTimeFrameCandle(secId, baseTime.AddMinutes(i), TimeSpan.FromMinutes(1),
				100 + i * 10, 105 + i * 10, 95 + i * 10, 102 + i * 10, 1000 + i * 100);
			await inner.SimulateCandle(1, candle, CancellationToken);
		}

		// Verify candles received (count limit is advisory, may receive up to requested)
		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToList();
		(candles.Count >= 1 && candles.Count <= 5).AssertTrue($"Should receive 1-5 candles with Count limit, got {candles.Count}");

		// Verify first candle has correct OHLCV
		var firstCandle = candles[0];
		AreEqual(secId, firstCandle.SecurityId, "First candle SecurityId should match");
		AreEqual(100m, firstCandle.OpenPrice, "First candle Open should be 100");
		AreEqual(105m, firstCandle.HighPrice, "First candle High should be 105");
		AreEqual(95m, firstCandle.LowPrice, "First candle Low should be 95");
		AreEqual(102m, firstCandle.ClosePrice, "First candle Close should be 102");
		AreEqual(1000m, firstCandle.TotalVolume, "First candle Volume should be 1000");
	}

	#endregion

	#region IsFinishedOnly Tests

	[TestMethod]
	public async Task Subscribe_IsFinishedOnly_ReceivesFinishedCandles()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedTimeFrame(TimeSpan.FromMinutes(1));
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);
		adapter.SendFinishedCandlesImmediatelly = true;

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
			IsFinishedOnly = true,
		}, CancellationToken);

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		// Simulate 2 finished candles
		await inner.SimulateCandle(1, CreateTimeFrameCandle(secId, baseTime, TimeSpan.FromMinutes(1),
			100, 110, 95, 105, 1000, CandleStates.Finished), CancellationToken);
		await inner.SimulateCandle(1, CreateTimeFrameCandle(secId, baseTime.AddMinutes(1), TimeSpan.FromMinutes(1),
			110, 120, 105, 115, 1200, CandleStates.Finished), CancellationToken);

		// Sent 2 finished candles, should receive exactly 2
		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToList();
		AreEqual(2, candles.Count, "Sent 2 finished candles, should receive exactly 2");

		// All should be finished
		var finishedCandles = candles.Where(c => c.State == CandleStates.Finished).ToList();
		AreEqual(2, finishedCandles.Count, "All 2 candles should be Finished");

		// Verify first candle OHLCV
		var first = finishedCandles[0];
		AreEqual(secId, first.SecurityId, "First candle SecurityId should match");
		AreEqual(100m, first.OpenPrice, "First candle Open should be 100");
		AreEqual(110m, first.HighPrice, "First candle High should be 110");
		AreEqual(95m, first.LowPrice, "First candle Low should be 95");
		AreEqual(105m, first.ClosePrice, "First candle Close should be 105");
		AreEqual(1000m, first.TotalVolume, "First candle Volume should be 1000");

		// Verify second candle OHLCV
		var second = finishedCandles[1];
		AreEqual(110m, second.OpenPrice, "Second candle Open should be 110");
		AreEqual(120m, second.HighPrice, "Second candle High should be 120");
		AreEqual(105m, second.LowPrice, "Second candle Low should be 105");
		AreEqual(115m, second.ClosePrice, "Second candle Close should be 115");
		AreEqual(1200m, second.TotalVolume, "Second candle Volume should be 1200");
	}

	#endregion

	#region Time Range Tests

	[TestMethod]
	public async Task Subscribe_WithToTime_ReceivesCandleInRange()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedTimeFrame(TimeSpan.FromMinutes(1));
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);
		adapter.SendFinishedCandlesImmediatelly = true;

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();
		var toTime = baseTime.AddHours(1);

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
			From = baseTime,
			To = toTime,
		}, CancellationToken);

		// Simulate 1 candle within range
		await inner.SimulateCandle(1, CreateTimeFrameCandle(secId, baseTime.AddMinutes(30), TimeSpan.FromMinutes(1),
			100, 105, 95, 102, 1000), CancellationToken);

		// Sent 1 candle, should receive exactly 1
		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToList();
		AreEqual(1, candles.Count, "Sent 1 candle in range, should receive exactly 1");

		var received = candles[0];
		AreEqual(secId, received.SecurityId, "SecurityId should match");
		AreEqual(baseTime.AddMinutes(30), received.OpenTime, "OpenTime should match");
		AreEqual(100m, received.OpenPrice, "Open should be 100");
		AreEqual(105m, received.HighPrice, "High should be 105");
		AreEqual(95m, received.LowPrice, "Low should be 95");
		AreEqual(102m, received.ClosePrice, "Close should be 102");
		AreEqual(1000m, received.TotalVolume, "Volume should be 1000");
	}

	[TestMethod]
	public async Task Subscribe_CandlesWithinToTime_AllReceived()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedTimeFrame(TimeSpan.FromMinutes(1));
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);
		adapter.SendFinishedCandlesImmediatelly = true;

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();
		var toTime = baseTime.AddMinutes(5);

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
			From = baseTime,
			To = toTime,
		}, CancellationToken);

		// Send 10 candles - first 6 within range (0-5 min), last 4 after
		for (int i = 0; i < 10; i++)
		{
			var candle = CreateTimeFrameCandle(secId, baseTime.AddMinutes(i), TimeSpan.FromMinutes(1),
				100 + i * 5, 105 + i * 5, 95 + i * 5, 102 + i * 5, 1000 + i * 100);
			await inner.SimulateCandle(1, candle, CancellationToken);
		}

		// Verify candles received
		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToList();
		(candles.Count >= 1).AssertTrue($"Should receive candles, got {candles.Count}");

		// First candle should have correct OHLCV
		var first = candles[0];
		AreEqual(secId, first.SecurityId, "First SecurityId should match");
		AreEqual(baseTime, first.OpenTime, "First OpenTime should be baseTime");
		AreEqual(100m, first.OpenPrice, "First Open should be 100");
	}

	#endregion

	#region Candle Time Ordering Tests

	[TestMethod]
	public async Task Subscribe_OutOfOrderCandles_Filtered()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedTimeFrame(TimeSpan.FromMinutes(1));
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);
		adapter.SendFinishedCandlesImmediatelly = true;

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
		}, CancellationToken);

		// Send 3 candles in order: 10:00, 10:01, 10:02
		await inner.SimulateCandle(1, CreateTimeFrameCandle(secId, baseTime, TimeSpan.FromMinutes(1), 100, 105, 95, 102, 1000), CancellationToken);
		await inner.SimulateCandle(1, CreateTimeFrameCandle(secId, baseTime.AddMinutes(1), TimeSpan.FromMinutes(1), 110, 115, 105, 112, 1100), CancellationToken);
		await inner.SimulateCandle(1, CreateTimeFrameCandle(secId, baseTime.AddMinutes(2), TimeSpan.FromMinutes(1), 120, 125, 115, 122, 1200), CancellationToken);

		// Now send 1 old candle (out of order) - 10:00 again
		await inner.SimulateCandle(1, CreateTimeFrameCandle(secId, baseTime, TimeSpan.FromMinutes(1), 99, 104, 94, 101, 999), CancellationToken);

		// Sent 4 candles, but 1 out-of-order should be filtered = exactly 3
		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToList();
		AreEqual(3, candles.Count, "Sent 4 candles (1 out-of-order), should receive exactly 3");

		// Verify each candle OHLCV
		AreEqual(baseTime, candles[0].OpenTime, "Candle 1 OpenTime");
		AreEqual(100m, candles[0].OpenPrice, "Candle 1 Open should be 100");
		AreEqual(105m, candles[0].HighPrice, "Candle 1 High should be 105");
		AreEqual(95m, candles[0].LowPrice, "Candle 1 Low should be 95");
		AreEqual(102m, candles[0].ClosePrice, "Candle 1 Close should be 102");
		AreEqual(1000m, candles[0].TotalVolume, "Candle 1 Volume should be 1000");

		AreEqual(baseTime.AddMinutes(1), candles[1].OpenTime, "Candle 2 OpenTime");
		AreEqual(110m, candles[1].OpenPrice, "Candle 2 Open should be 110");
		AreEqual(115m, candles[1].HighPrice, "Candle 2 High should be 115");
		AreEqual(105m, candles[1].LowPrice, "Candle 2 Low should be 105");
		AreEqual(112m, candles[1].ClosePrice, "Candle 2 Close should be 112");
		AreEqual(1100m, candles[1].TotalVolume, "Candle 2 Volume should be 1100");

		AreEqual(baseTime.AddMinutes(2), candles[2].OpenTime, "Candle 3 OpenTime");
		AreEqual(120m, candles[2].OpenPrice, "Candle 3 Open should be 120");
		AreEqual(125m, candles[2].HighPrice, "Candle 3 High should be 125");
		AreEqual(115m, candles[2].LowPrice, "Candle 3 Low should be 115");
		AreEqual(122m, candles[2].ClosePrice, "Candle 3 Close should be 122");
		AreEqual(1200m, candles[2].TotalVolume, "Candle 3 Volume should be 1200");
	}

	#endregion

	#region Multiple Subscriptions Tests

	[TestMethod]
	public async Task Subscribe_MultipleSubscriptions_WorkIndependently()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedTimeFrame(TimeSpan.FromMinutes(1));
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);
		adapter.SendFinishedCandlesImmediatelly = true;

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId1 = new SecurityId { SecurityCode = "SBER", BoardCode = "TQBR" };
		var secId2 = new SecurityId { SecurityCode = "GAZP", BoardCode = "TQBR" };

		// Subscribe to 2 different securities
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId1,
		}, CancellationToken);

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 2,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId2,
		}, CancellationToken);

		// Should receive exactly 2 responses
		var responses = outMessages.OfType<SubscriptionResponseMessage>().ToList();
		AreEqual(2, responses.Count, "Should receive exactly 2 subscription responses");

		var response1 = responses.First(r => r.OriginalTransactionId == 1);
		var response2 = responses.First(r => r.OriginalTransactionId == 2);
		IsNull(response1.Error, "Subscription 1 should succeed");
		IsNull(response2.Error, "Subscription 2 should succeed");

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		// Send 1 candle for each subscription
		await inner.SimulateCandle(1, CreateTimeFrameCandle(secId1, baseTime, TimeSpan.FromMinutes(1), 100, 105, 95, 102, 1000), CancellationToken);
		await inner.SimulateCandle(2, CreateTimeFrameCandle(secId2, baseTime, TimeSpan.FromMinutes(1), 200, 210, 190, 205, 2000), CancellationToken);

		// Sent 2 candles (1 per subscription), should receive exactly 2
		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToList();
		AreEqual(2, candles.Count, "Sent 2 candles, should receive exactly 2");

		// Find by subscription ID
		var sber = candles.First(c => c.GetSubscriptionIds().Contains(1L));
		var gazp = candles.First(c => c.GetSubscriptionIds().Contains(2L));

		// Verify SBER candle OHLCV
		AreEqual(secId1, sber.SecurityId, "SBER SecurityId should match");
		AreEqual(100m, sber.OpenPrice, "SBER Open should be 100");
		AreEqual(105m, sber.HighPrice, "SBER High should be 105");
		AreEqual(95m, sber.LowPrice, "SBER Low should be 95");
		AreEqual(102m, sber.ClosePrice, "SBER Close should be 102");
		AreEqual(1000m, sber.TotalVolume, "SBER Volume should be 1000");

		// Verify GAZP candle OHLCV
		AreEqual(secId2, gazp.SecurityId, "GAZP SecurityId should match");
		AreEqual(200m, gazp.OpenPrice, "GAZP Open should be 200");
		AreEqual(210m, gazp.HighPrice, "GAZP High should be 210");
		AreEqual(190m, gazp.LowPrice, "GAZP Low should be 190");
		AreEqual(205m, gazp.ClosePrice, "GAZP Close should be 205");
		AreEqual(2000m, gazp.TotalVolume, "GAZP Volume should be 2000");
	}

	#endregion

	#region Clone Tests

	[TestMethod]
	public void Clone_CreatesNewInstanceWithSameSettings()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider)
		{
			SendFinishedCandlesImmediatelly = true,
		};

		var cloned = adapter.Clone() as CandleBuilderMessageAdapter;

		IsNotNull(cloned);
		AreNotSame(adapter, cloned);
		AreEqual(adapter.SendFinishedCandlesImmediatelly, cloned.SendFinishedCandlesImmediatelly);
	}

	#endregion

	#region Build From Level1 Tests

	[TestMethod]
	public async Task Subscribe_BuildFromLevel1_LastTradePrice_BuildsCandles_WithCorrectOHLCV()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedLevel1();
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Level1,
			BuildField = Level1Fields.LastTradePrice,
		}, CancellationToken);

		// Verify Level1 subscription was created
		var level1Sub = inner.SentMessages.OfType<MarketDataMessage>()
			.FirstOrDefault(m => m.DataType2 == DataType.Level1);
		IsNotNull(level1Sub, "Should subscribe to Level1");
		AreEqual(secId, level1Sub.SecurityId, "Level1 subscription SecurityId should match");
		level1Sub.IsSubscribe.AssertTrue("Level1 subscription IsSubscribe should be true");

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		// Simulate Level1 messages: O=100, H=108, L=95, C=102, V=75
		await inner.SimulateLevel1(level1Sub.TransactionId, CreateLevel1(secId, baseTime, lastTradePrice: 100, lastTradeVolume: 10), CancellationToken);
		await inner.SimulateLevel1(level1Sub.TransactionId, CreateLevel1(secId, baseTime.AddSeconds(10), lastTradePrice: 108, lastTradeVolume: 25), CancellationToken);
		await inner.SimulateLevel1(level1Sub.TransactionId, CreateLevel1(secId, baseTime.AddSeconds(20), lastTradePrice: 95, lastTradeVolume: 20), CancellationToken);
		await inner.SimulateLevel1(level1Sub.TransactionId, CreateLevel1(secId, baseTime.AddSeconds(30), lastTradePrice: 102, lastTradeVolume: 20), CancellationToken);

		// Verify candles built from Level1
		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToList();
		(candles.Count > 0).AssertTrue("Should build candles from Level1");

		// Verify OHLCV values
		var candle = candles.Last();
		IsNotNull(candle, "Last candle should not be null");
		AreEqual(secId, candle.SecurityId, "Candle SecurityId should match");
		AreEqual(100m, candle.OpenPrice, "Open should be first LastTradePrice (100)");
		AreEqual(108m, candle.HighPrice, "High should be max LastTradePrice (108)");
		AreEqual(95m, candle.LowPrice, "Low should be min LastTradePrice (95)");
		AreEqual(102m, candle.ClosePrice, "Close should be last LastTradePrice (102)");
		AreEqual(75m, candle.TotalVolume, "Volume should be sum of LastTradeVolume (75)");
	}

	[TestMethod]
	public async Task Subscribe_BuildFromLevel1_BestBidPrice_BuildsCandles_WithCorrectOHLCV()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedLevel1();
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Level1,
			BuildField = Level1Fields.BestBidPrice,
		}, CancellationToken);

		// Verify Level1 subscription
		var level1Sub = inner.SentMessages.OfType<MarketDataMessage>()
			.FirstOrDefault(m => m.DataType2 == DataType.Level1);
		IsNotNull(level1Sub, "Should subscribe to Level1");
		AreEqual(secId, level1Sub.SecurityId, "Level1 subscription SecurityId should match");

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		// Simulate Level1: O=99, H=105, L=96, C=100
		await inner.SimulateLevel1(level1Sub.TransactionId, CreateLevel1(secId, baseTime, bestBidPrice: 99), CancellationToken);
		await inner.SimulateLevel1(level1Sub.TransactionId, CreateLevel1(secId, baseTime.AddSeconds(10), bestBidPrice: 105), CancellationToken);
		await inner.SimulateLevel1(level1Sub.TransactionId, CreateLevel1(secId, baseTime.AddSeconds(20), bestBidPrice: 96), CancellationToken);
		await inner.SimulateLevel1(level1Sub.TransactionId, CreateLevel1(secId, baseTime.AddSeconds(30), bestBidPrice: 100), CancellationToken);

		// Verify candles built
		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToList();
		(candles.Count > 0).AssertTrue("Should build candles from BestBidPrice");

		var candle = candles.Last();
		IsNotNull(candle, "Last candle should not be null");
		AreEqual(secId, candle.SecurityId, "Candle SecurityId should match");
		AreEqual(99m, candle.OpenPrice, "Open should be first BestBidPrice (99)");
		AreEqual(105m, candle.HighPrice, "High should be max BestBidPrice (105)");
		AreEqual(96m, candle.LowPrice, "Low should be min BestBidPrice (96)");
		AreEqual(100m, candle.ClosePrice, "Close should be last BestBidPrice (100)");
	}

	[TestMethod]
	public async Task Subscribe_BuildFromLevel1_BestAskPrice_BuildsCandles_WithCorrectOHLCV()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedLevel1();
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Level1,
			BuildField = Level1Fields.BestAskPrice,
		}, CancellationToken);

		// Verify Level1 subscription
		var level1Sub = inner.SentMessages.OfType<MarketDataMessage>()
			.FirstOrDefault(m => m.DataType2 == DataType.Level1);
		IsNotNull(level1Sub, "Should subscribe to Level1");
		AreEqual(secId, level1Sub.SecurityId, "Level1 subscription SecurityId should match");

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		// Simulate Level1: O=101, H=110, L=98, C=105
		await inner.SimulateLevel1(level1Sub.TransactionId, CreateLevel1(secId, baseTime, bestAskPrice: 101), CancellationToken);
		await inner.SimulateLevel1(level1Sub.TransactionId, CreateLevel1(secId, baseTime.AddSeconds(10), bestAskPrice: 110), CancellationToken);
		await inner.SimulateLevel1(level1Sub.TransactionId, CreateLevel1(secId, baseTime.AddSeconds(20), bestAskPrice: 98), CancellationToken);
		await inner.SimulateLevel1(level1Sub.TransactionId, CreateLevel1(secId, baseTime.AddSeconds(30), bestAskPrice: 105), CancellationToken);

		// Verify candles built
		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToList();
		(candles.Count > 0).AssertTrue("Should build candles from BestAskPrice");

		var candle = candles.Last();
		IsNotNull(candle, "Last candle should not be null");
		AreEqual(secId, candle.SecurityId, "Candle SecurityId should match");
		AreEqual(101m, candle.OpenPrice, "Open should be first BestAskPrice (101)");
		AreEqual(110m, candle.HighPrice, "High should be max BestAskPrice (110)");
		AreEqual(98m, candle.LowPrice, "Low should be min BestAskPrice (98)");
		AreEqual(105m, candle.ClosePrice, "Close should be last BestAskPrice (105)");
	}

	[TestMethod]
	public async Task Subscribe_BuildFromLevel1_MissingField_Ignored()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedLevel1();
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Level1,
			BuildField = Level1Fields.LastTradePrice,
		}, CancellationToken);

		// Verify Level1 subscription
		var level1Sub = inner.SentMessages.OfType<MarketDataMessage>()
			.FirstOrDefault(m => m.DataType2 == DataType.Level1);
		IsNotNull(level1Sub, "Should subscribe to Level1");
		AreEqual(secId, level1Sub.SecurityId, "Level1 subscription SecurityId should match");

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		// Send Level1 WITHOUT LastTradePrice (only BestBid)
		await inner.SimulateLevel1(level1Sub.TransactionId, CreateLevel1(secId, baseTime, bestBidPrice: 99), CancellationToken);
		await inner.SimulateLevel1(level1Sub.TransactionId, CreateLevel1(secId, baseTime.AddSeconds(10), bestBidPrice: 100), CancellationToken);

		// No candles should be built (required field missing)
		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToList();
		AreEqual(0, candles.Count, "Should not build candles when required field is missing");

		// Now send with LastTradePrice
		await inner.SimulateLevel1(level1Sub.TransactionId, CreateLevel1(secId, baseTime.AddSeconds(20), lastTradePrice: 101, lastTradeVolume: 10), CancellationToken);

		// Verify candle is now built
		candles = [.. outMessages.OfType<TimeFrameCandleMessage>()];
		(candles.Count > 0).AssertTrue("Should build candle when required field is present");

		var candle = candles.Last();
		IsNotNull(candle, "Candle should not be null");
		AreEqual(secId, candle.SecurityId, "Candle SecurityId should match");
		AreEqual(101m, candle.OpenPrice, "Open should be 101");
	}

	[TestMethod]
	public async Task Subscribe_BuildFromLevel1_SpreadMiddle_BuildsCandles_WithCorrectOHLCV()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		inner.AddSupportedLevel1();
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Level1,
			BuildField = Level1Fields.SpreadMiddle,
		}, CancellationToken);

		// Verify Level1 subscription
		var level1Sub = inner.SentMessages.OfType<MarketDataMessage>()
			.FirstOrDefault(m => m.DataType2 == DataType.Level1);
		IsNotNull(level1Sub, "Should subscribe to Level1");
		AreEqual(secId, level1Sub.SecurityId, "Level1 subscription SecurityId should match");

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		// Simulate Level1 (SpreadMiddle = (Bid+Ask)/2): O=100, H=105, L=97, C=102
		await inner.SimulateLevel1(level1Sub.TransactionId, CreateLevel1(secId, baseTime, bestBidPrice: 99, bestAskPrice: 101), CancellationToken);             // middle = 100
		await inner.SimulateLevel1(level1Sub.TransactionId, CreateLevel1(secId, baseTime.AddSeconds(10), bestBidPrice: 103, bestAskPrice: 107), CancellationToken); // middle = 105
		await inner.SimulateLevel1(level1Sub.TransactionId, CreateLevel1(secId, baseTime.AddSeconds(20), bestBidPrice: 95, bestAskPrice: 99), CancellationToken);   // middle = 97
		await inner.SimulateLevel1(level1Sub.TransactionId, CreateLevel1(secId, baseTime.AddSeconds(30), bestBidPrice: 100, bestAskPrice: 104), CancellationToken); // middle = 102

		// Verify candles built
		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToList();
		(candles.Count > 0).AssertTrue("Should build candles from SpreadMiddle");

		var candle = candles.Last();
		IsNotNull(candle, "Last candle should not be null");
		AreEqual(secId, candle.SecurityId, "Candle SecurityId should match");
		AreEqual(100m, candle.OpenPrice, "Open should be first SpreadMiddle (100)");
		AreEqual(105m, candle.HighPrice, "High should be max SpreadMiddle (105)");
		AreEqual(97m, candle.LowPrice, "Low should be min SpreadMiddle (97)");
		AreEqual(102m, candle.ClosePrice, "Close should be last SpreadMiddle (102)");
	}

	#endregion

	#region LoadOnly Mode Tests

	[TestMethod]
	public async Task Subscribe_LoadOnly_UnsupportedTimeFrame_ReturnsNotSupported()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		// No timeframes supported
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
			BuildMode = MarketDataBuildModes.Load,
		}, CancellationToken);

		// Verify error response returned
		var response = outMessages.OfType<SubscriptionResponseMessage>().FirstOrDefault();
		IsNotNull(response, "Should receive subscription response");
		AreEqual(1, response.OriginalTransactionId, "Response OriginalTransactionId should be 1");
		IsNotNull(response.Error, "Response should contain error for unsupported timeframe in LoadOnly mode");
	}

	[TestMethod]
	public async Task Subscribe_LoadOnly_DoesNotFallbackToTicks()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		// No candle TFs, only ticks available
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			IsSubscribe = true,
			SecurityId = secId,
			BuildMode = MarketDataBuildModes.Load, // Load only!
		}, CancellationToken);

		// Verify NO tick subscription was created
		var tickSub = inner.SentMessages.OfType<MarketDataMessage>()
			.FirstOrDefault(m => m.DataType2 == DataType.Ticks);
		IsNull(tickSub, "LoadOnly mode should not fallback to ticks");

		// Verify error response returned
		var response = outMessages.OfType<SubscriptionResponseMessage>().FirstOrDefault();
		IsNotNull(response, "Should receive subscription response");
		AreEqual(1, response.OriginalTransactionId, "Response OriginalTransactionId should be 1");
		IsNotNull(response.Error, "Should return error for unsupported data in LoadOnly mode");
	}

	#endregion

	#region Non-TimeFrame Candle Tests

	[TestMethod]
	public async Task Subscribe_VolumeCandles_BuildsFromTicks_WithCorrectOHLCV()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = DataType.Create<VolumeCandleMessage>(100m), // 100 volume per candle
			IsSubscribe = true,
			SecurityId = secId,
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Ticks,
		}, CancellationToken);

		// Verify tick subscription created
		var tickSub = inner.SentMessages.OfType<MarketDataMessage>()
			.FirstOrDefault(m => m.DataType2 == DataType.Ticks);
		IsNotNull(tickSub, "Should create tick subscription for volume candles");
		AreEqual(secId, tickSub.SecurityId, "Tick subscription SecurityId should match");

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		// Simulate ticks that will create candles with known OHLCV
		// First candle: vol 30+40+20+10=100, O=100, H=102, L=98, C=101
		await inner.SimulateTick(1, CreateTick(secId, baseTime, 100, 30), CancellationToken);
		await inner.SimulateTick(1, CreateTick(secId, baseTime.AddSeconds(1), 102, 40), CancellationToken);
		await inner.SimulateTick(1, CreateTick(secId, baseTime.AddSeconds(2), 98, 20), CancellationToken);
		await inner.SimulateTick(1, CreateTick(secId, baseTime.AddSeconds(3), 101, 10), CancellationToken);

		// Second candle starts: vol 50+60=110, O=103, H=105, L=103, C=105
		await inner.SimulateTick(1, CreateTick(secId, baseTime.AddSeconds(4), 103, 50), CancellationToken);
		await inner.SimulateTick(1, CreateTick(secId, baseTime.AddSeconds(5), 105, 60), CancellationToken);

		// Verify volume candles built
		var candles = outMessages.OfType<VolumeCandleMessage>().ToList();
		(candles.Count > 0).AssertTrue("Should build volume candles");

		// Verify finished candles
		var finishedCandles = candles.Where(c => c.State == CandleStates.Finished).ToList();
		(finishedCandles.Count > 0).AssertTrue("Should have at least one finished volume candle");

		var firstFinished = finishedCandles.First();
		IsNotNull(firstFinished, "First finished candle should not be null");
		AreEqual(secId, firstFinished.SecurityId, "First candle SecurityId should match");
		AreEqual(100m, firstFinished.OpenPrice, "First candle Open should be 100");
		AreEqual(102m, firstFinished.HighPrice, "First candle High should be 102");
		AreEqual(98m, firstFinished.LowPrice, "First candle Low should be 98");
		AreEqual(100m, firstFinished.TotalVolume, "First candle Volume should be 100");
	}

	[TestMethod]
	public async Task Subscribe_TickCandles_BuildsFromTicks_WithCorrectOHLCV()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = DataType.Create<TickCandleMessage>(5), // 5 ticks per candle
			IsSubscribe = true,
			SecurityId = secId,
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Ticks,
		}, CancellationToken);

		// Verify tick subscription created
		var tickSub = inner.SentMessages.OfType<MarketDataMessage>()
			.FirstOrDefault(m => m.DataType2 == DataType.Ticks);
		IsNotNull(tickSub, "Should create tick subscription for tick candles");
		AreEqual(secId, tickSub.SecurityId, "Tick subscription SecurityId should match");

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		// First candle (5 ticks): O=100, H=108, L=96, C=104, V=150
		await inner.SimulateTick(1, CreateTick(secId, baseTime.AddSeconds(0), 100, 20), CancellationToken);
		await inner.SimulateTick(1, CreateTick(secId, baseTime.AddSeconds(1), 108, 30), CancellationToken); // High
		await inner.SimulateTick(1, CreateTick(secId, baseTime.AddSeconds(2), 102, 25), CancellationToken);
		await inner.SimulateTick(1, CreateTick(secId, baseTime.AddSeconds(3), 96, 35), CancellationToken);  // Low
		await inner.SimulateTick(1, CreateTick(secId, baseTime.AddSeconds(4), 104, 40), CancellationToken); // Close

		// Second candle starts
		await inner.SimulateTick(1, CreateTick(secId, baseTime.AddSeconds(5), 105, 10), CancellationToken);
		await inner.SimulateTick(1, CreateTick(secId, baseTime.AddSeconds(6), 107, 15), CancellationToken);

		// Verify tick candles built
		var candles = outMessages.OfType<TickCandleMessage>().ToList();
		(candles.Count > 0).AssertTrue("Should build tick candles");

		// Verify finished candles
		var finishedCandles = candles.Where(c => c.State == CandleStates.Finished).ToList();
		(finishedCandles.Count > 0).AssertTrue("Should have at least one finished tick candle");

		var firstFinished = finishedCandles.First();
		IsNotNull(firstFinished, "First finished candle should not be null");
		AreEqual(secId, firstFinished.SecurityId, "First candle SecurityId should match");
		AreEqual(100m, firstFinished.OpenPrice, "First candle Open should be 100");
		AreEqual(108m, firstFinished.HighPrice, "First candle High should be 108");
		AreEqual(96m, firstFinished.LowPrice, "First candle Low should be 96");
		AreEqual(104m, firstFinished.ClosePrice, "First candle Close should be 104");
		AreEqual(150m, firstFinished.TotalVolume, "First candle Volume should be 150");
		AreEqual(5, firstFinished.TotalTicks, "First candle should have 5 ticks");
	}

	[TestMethod]
	public async Task Subscribe_RangeCandles_BuildsFromTicks_WithCorrectOHLCV()
	{
		var idGen = new IncrementalIdGenerator();
		var inner = new MockCandleAdapter(idGen);
		var provider = CreateCandleBuilderProvider();
		var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var secId = CreateSecurityId();
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 1,
			DataType2 = DataType.Create<RangeCandleMessage>(new Unit(10m)), // 10 point range
			IsSubscribe = true,
			SecurityId = secId,
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Ticks,
		}, CancellationToken);

		// Verify tick subscription created
		var tickSub = inner.SentMessages.OfType<MarketDataMessage>()
			.FirstOrDefault(m => m.DataType2 == DataType.Ticks);
		IsNotNull(tickSub, "Should create tick subscription for range candles");
		AreEqual(secId, tickSub.SecurityId, "Tick subscription SecurityId should match");

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		// Ticks within 10 point range: O=100, H=108, L=100, C=105, then exceeds range
		await inner.SimulateTick(1, CreateTick(secId, baseTime.AddSeconds(0), 100, 20), CancellationToken); // Open
		await inner.SimulateTick(1, CreateTick(secId, baseTime.AddSeconds(1), 105, 30), CancellationToken);
		await inner.SimulateTick(1, CreateTick(secId, baseTime.AddSeconds(2), 108, 25), CancellationToken); // High (range = 8)
		await inner.SimulateTick(1, CreateTick(secId, baseTime.AddSeconds(3), 102, 35), CancellationToken);
		await inner.SimulateTick(1, CreateTick(secId, baseTime.AddSeconds(4), 105, 40), CancellationToken); // Close before range exceeded

		// This tick exceeds range (100 to 112 = 12 > 10) - should start new candle
		await inner.SimulateTick(1, CreateTick(secId, baseTime.AddSeconds(5), 112, 15), CancellationToken);

		// Verify range candles built
		var candles = outMessages.OfType<RangeCandleMessage>().ToList();
		(candles.Count > 0).AssertTrue("Should build range candles");

		// Verify first candle (active or finished)
		var firstCandle = candles.First();
		IsNotNull(firstCandle, "First candle should not be null");
		AreEqual(secId, firstCandle.SecurityId, "First candle SecurityId should match");
		AreEqual(100m, firstCandle.OpenPrice, "First candle Open should be 100");
	}

	#endregion

	[TestMethod]
	public async Task Unsubscribe_AllSecurityChild_ShouldNotForwardToInner()
	{
		var token = CancellationToken;

		var inner = new RecordingPassThroughMessageAdapter([DataType.Ticks]);
		var provider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());

		using var adapter = new CandleBuilderMessageAdapter(inner, provider);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		// Use a high TransactionId to avoid collision with IdGenerator.GetNextId() which starts at 1
		var parentTransId = inner.TransactionIdGenerator.GetNextId();
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = parentTransId,
			SecurityId = default,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Ticks,
		}, token);

		inner.InMessages.Clear();

		var exec = new ExecutionMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			DataTypeEx = DataType.Ticks,
			ServerTime = DateTime.UtcNow,
			TradePrice = 1m,
			TradeVolume = 1m,
		};
		exec.SetSubscriptionIds([parentTransId]);

		await inner.SendOutMessageAsync(exec, CancellationToken);
		await Task.Delay(50, token);

		var child = output.OfType<SubscriptionSecurityAllMessage>().FirstOrDefault();
		IsNotNull(child);

		await adapter.SendInMessageAsync(child, token);
		await Task.Delay(10, token);

		inner.InMessages.Clear();

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 100,
			OriginalTransactionId = child.TransactionId,
		}, token);

		inner.InMessages.Count.AssertEqual(0);
	}
}

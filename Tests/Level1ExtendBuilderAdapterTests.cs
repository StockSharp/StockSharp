namespace StockSharp.Tests;

/// <summary>
/// Tests for <see cref="Level1ExtendBuilderAdapter"/>.
/// Verifies subscription rewriting and data conversion behavior.
/// </summary>
[TestClass]
public class Level1ExtendBuilderAdapterTests : BaseTestClass
{
	#region Test Infrastructure

	private sealed class TestInnerAdapter : MessageAdapter
	{
		private readonly List<Message> _received = [];

		public override bool UseInChannel => false;
		public override bool UseOutChannel => false;

		public TestInnerAdapter(IdGenerator idGen = null)
			: base(idGen ?? new IncrementalIdGenerator())
		{
			this.AddMarketDataSupport();
			this.AddSupportedMessage(MessageTypes.MarketData, null);
			this.AddSupportedMarketDataType(DataType.Ticks);
			this.AddSupportedMarketDataType(DataType.MarketDepth);
			this.AddSupportedMarketDataType(DataType.Level1);
			this.AddSupportedMarketDataType(DataType.CandleTimeFrame);
		}

		public IReadOnlyList<Message> Received => _received;
		public IEnumerable<T> GetMessages<T>() where T : Message => _received.OfType<T>();

		protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
		{
			_received.Add(message.TypedClone());
			return default;
		}

		public override IMessageAdapter Clone() => new TestInnerAdapter(TransactionIdGenerator);

		/// <summary>
		/// Simulate inner adapter emitting an outgoing message.
		/// </summary>
		public ValueTask EmitOutMessage(Message message, CancellationToken cancellationToken)
			=> SendOutMessageAsync(message, cancellationToken);
	}

	private static readonly SecurityId _testSecId = new()
	{
		SecurityCode = "BTCUSDT",
		BoardCode = "BNBFT",
	};

	private static (Level1ExtendBuilderAdapter wrapper, TestInnerAdapter inner, List<Message> output) CreateAdapter()
	{
		var inner = new TestInnerAdapter();
		var wrapper = new Level1ExtendBuilderAdapter(inner);
		var output = new List<Message>();

		wrapper.NewOutMessageAsync += (msg, ct) =>
		{
			output.Add(msg.TypedClone());
			return default;
		};

		return (wrapper, inner, output);
	}

	#endregion

	#region Subscription Rewriting

	/// <summary>
	/// Level1 subscription without BuildFrom should be converted to MarketDepth subscription.
	/// This is the default behavior: adapter rewrites DataType2 from Level1 to MarketDepth.
	/// </summary>
	[TestMethod]
	public async Task Subscribe_Level1_NoBuildFrom_ConvertedToMarketDepth()
	{
		var (wrapper, inner, _) = CreateAdapter();

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _testSecId,
			DataType2 = DataType.Level1,
			IsSubscribe = true,
			TransactionId = 100,
		};

		await wrapper.SendInMessageAsync(mdMsg, CancellationToken);

		var forwarded = inner.GetMessages<MarketDataMessage>().Single();
		AreEqual(DataType.MarketDepth, forwarded.DataType2);
		IsNull(forwarded.BuildFrom);
	}

	/// <summary>
	/// Level1 subscription with BuildFrom=Ticks should be converted to Ticks subscription.
	/// </summary>
	[TestMethod]
	public async Task Subscribe_Level1_BuildFromTicks_ConvertedToTicks()
	{
		var (wrapper, inner, _) = CreateAdapter();

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _testSecId,
			DataType2 = DataType.Level1,
			BuildFrom = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 101,
		};

		await wrapper.SendInMessageAsync(mdMsg, CancellationToken);

		var forwarded = inner.GetMessages<MarketDataMessage>().Single();
		AreEqual(DataType.Ticks, forwarded.DataType2);
		IsNull(forwarded.BuildFrom);
	}

	/// <summary>
	/// Level1 subscription with BuildFrom=CandleTimeFrame should be converted to CandleTimeFrame subscription.
	/// </summary>
	[TestMethod]
	public async Task Subscribe_Level1_BuildFromCandles_ConvertedToCandles()
	{
		var (wrapper, inner, _) = CreateAdapter();

		var tfDataType = TimeSpan.FromMinutes(5).TimeFrame();

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _testSecId,
			DataType2 = DataType.Level1,
			BuildFrom = tfDataType,
			IsSubscribe = true,
			TransactionId = 102,
		};

		await wrapper.SendInMessageAsync(mdMsg, CancellationToken);

		var forwarded = inner.GetMessages<MarketDataMessage>().Single();
		AreEqual(tfDataType, forwarded.DataType2);
		IsNull(forwarded.BuildFrom);
	}

	/// <summary>
	/// BuildMode=Load should bypass interception — subscription passes through unchanged.
	/// </summary>
	[TestMethod]
	public async Task Subscribe_Level1_BuildModeLoad_PassesThrough()
	{
		var (wrapper, inner, _) = CreateAdapter();

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _testSecId,
			DataType2 = DataType.Level1,
			BuildMode = MarketDataBuildModes.Load,
			IsSubscribe = true,
			TransactionId = 103,
		};

		await wrapper.SendInMessageAsync(mdMsg, CancellationToken);

		var forwarded = inner.GetMessages<MarketDataMessage>().Single();
		AreEqual(DataType.Level1, forwarded.DataType2);
	}

	/// <summary>
	/// SecurityId == default should bypass interception.
	/// </summary>
	[TestMethod]
	public async Task Subscribe_Level1_DefaultSecurityId_PassesThrough()
	{
		var (wrapper, inner, _) = CreateAdapter();

		var mdMsg = new MarketDataMessage
		{
			SecurityId = default,
			DataType2 = DataType.Level1,
			IsSubscribe = true,
			TransactionId = 104,
		};

		await wrapper.SendInMessageAsync(mdMsg, CancellationToken);

		var forwarded = inner.GetMessages<MarketDataMessage>().Single();
		AreEqual(DataType.Level1, forwarded.DataType2);
	}

	/// <summary>
	/// Subscription with To != null (historical range request) should bypass interception.
	/// </summary>
	[TestMethod]
	public async Task Subscribe_Level1_WithToDate_PassesThrough()
	{
		var (wrapper, inner, _) = CreateAdapter();

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _testSecId,
			DataType2 = DataType.Level1,
			IsSubscribe = true,
			TransactionId = 105,
			To = DateTime.UtcNow,
		};

		await wrapper.SendInMessageAsync(mdMsg, CancellationToken);

		var forwarded = inner.GetMessages<MarketDataMessage>().Single();
		AreEqual(DataType.Level1, forwarded.DataType2);
	}

	/// <summary>
	/// Non-Level1 subscription should pass through unchanged.
	/// </summary>
	[TestMethod]
	public async Task Subscribe_NonLevel1_PassesThrough()
	{
		var (wrapper, inner, _) = CreateAdapter();

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _testSecId,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 106,
		};

		await wrapper.SendInMessageAsync(mdMsg, CancellationToken);

		var forwarded = inner.GetMessages<MarketDataMessage>().Single();
		AreEqual(DataType.Ticks, forwarded.DataType2);
	}

	#endregion

	#region Data Conversion: QuoteChange → Level1

	/// <summary>
	/// QuoteChangeMessage with active Level1 subscription should be converted to Level1ChangeMessage
	/// containing only BestBid/BestAsk fields (no LastTradePrice).
	/// </summary>
	[TestMethod]
	public async Task QuoteChange_ConvertedToLevel1_OnlyBidAsk()
	{
		var (wrapper, inner, output) = CreateAdapter();

		// Subscribe Level1 (will be rewritten to MarketDepth)
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _testSecId,
			DataType2 = DataType.Level1,
			IsSubscribe = true,
			TransactionId = 200,
		};
		await wrapper.SendInMessageAsync(mdMsg, CancellationToken);

		// Simulate subscription response
		var response = new SubscriptionResponseMessage { OriginalTransactionId = 200 };
		await inner.EmitOutMessage(response, CancellationToken);

		output.Clear();

		// Simulate QuoteChange from inner adapter
		var quotes = new QuoteChangeMessage
		{
			SecurityId = _testSecId,
			ServerTime = new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc),
			Bids = [new QuoteChange(61000m, 1.5m)],
			Asks = [new QuoteChange(61001m, 2.0m)],
		};
		quotes.SetSubscriptionIds([200]);

		await inner.EmitOutMessage(quotes, CancellationToken);

		// Should produce Level1 message, not QuoteChange
		var l1Messages = output.OfType<Level1ChangeMessage>().ToList();
		AreEqual(1, l1Messages.Count);

		var l1 = l1Messages[0];
		IsTrue(l1.Changes.ContainsKey(Level1Fields.BestBidPrice));
		IsTrue(l1.Changes.ContainsKey(Level1Fields.BestBidVolume));
		IsTrue(l1.Changes.ContainsKey(Level1Fields.BestAskPrice));
		IsTrue(l1.Changes.ContainsKey(Level1Fields.BestAskVolume));
		AreEqual(61000m, (decimal)l1.Changes[Level1Fields.BestBidPrice]);
		AreEqual(61001m, (decimal)l1.Changes[Level1Fields.BestAskPrice]);

		// Must NOT contain LastTradePrice — this is the key limitation
		IsFalse(l1.Changes.ContainsKey(Level1Fields.LastTradePrice));

		// QuoteChange should NOT be passed through (fully consumed)
		var quoteMessages = output.OfType<QuoteChangeMessage>().ToList();
		AreEqual(0, quoteMessages.Count);
	}

	#endregion

	#region Data Conversion: Ticks → Level1

	/// <summary>
	/// ExecutionMessage (tick) with active Level1 subscription (BuildFrom=Ticks)
	/// should be converted to Level1 containing LastTradePrice (no BestBid/BestAsk).
	/// </summary>
	[TestMethod]
	public async Task Tick_ConvertedToLevel1_OnlyLastTrade()
	{
		var (wrapper, inner, output) = CreateAdapter();

		// Subscribe Level1 with BuildFrom=Ticks
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _testSecId,
			DataType2 = DataType.Level1,
			BuildFrom = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 300,
		};
		await wrapper.SendInMessageAsync(mdMsg, CancellationToken);

		// Simulate subscription response
		var response = new SubscriptionResponseMessage { OriginalTransactionId = 300 };
		await inner.EmitOutMessage(response, CancellationToken);

		output.Clear();

		// Simulate tick from inner adapter
		var tick = new ExecutionMessage
		{
			SecurityId = _testSecId,
			DataTypeEx = DataType.Ticks,
			ServerTime = new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc),
			TradePrice = 61050m,
			TradeVolume = 0.5m,
			TradeId = 12345,
			OriginSide = Sides.Buy,
		};
		tick.SetSubscriptionIds([300]);

		await inner.EmitOutMessage(tick, CancellationToken);

		// Should produce Level1 with LastTradePrice
		var l1Messages = output.OfType<Level1ChangeMessage>().ToList();
		AreEqual(1, l1Messages.Count);

		var l1 = l1Messages[0];
		IsTrue(l1.Changes.ContainsKey(Level1Fields.LastTradePrice));
		AreEqual(61050m, (decimal)l1.Changes[Level1Fields.LastTradePrice]);
		IsTrue(l1.Changes.ContainsKey(Level1Fields.LastTradeVolume));
		IsTrue(l1.Changes.ContainsKey(Level1Fields.LastTradeOrigin));

		// Must NOT contain BestBid/BestAsk
		IsFalse(l1.Changes.ContainsKey(Level1Fields.BestBidPrice));
		IsFalse(l1.Changes.ContainsKey(Level1Fields.BestAskPrice));

		// Tick should NOT be passed through (fully consumed)
		var tickMessages = output.OfType<ExecutionMessage>().ToList();
		AreEqual(0, tickMessages.Count);
	}

	#endregion

	#region Data Conversion: Candle → Level1

	/// <summary>
	/// TimeFrameCandleMessage with active Level1 subscription (BuildFrom=CandleTimeFrame)
	/// should be converted to Level1 containing OHLCV fields.
	/// </summary>
	[TestMethod]
	public async Task Candle_ConvertedToLevel1_OHLCV()
	{
		var (wrapper, inner, output) = CreateAdapter();

		var tfDataType = TimeSpan.FromMinutes(5).TimeFrame();

		// Subscribe Level1 with BuildFrom=CandleTimeFrame(5min)
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _testSecId,
			DataType2 = DataType.Level1,
			BuildFrom = tfDataType,
			IsSubscribe = true,
			TransactionId = 400,
		};
		await wrapper.SendInMessageAsync(mdMsg, CancellationToken);

		// Simulate subscription response
		var response = new SubscriptionResponseMessage { OriginalTransactionId = 400 };
		await inner.EmitOutMessage(response, CancellationToken);

		output.Clear();

		// Simulate candle from inner adapter
		var candle = new TimeFrameCandleMessage
		{
			SecurityId = _testSecId,
			TypedArg = TimeSpan.FromMinutes(5),
			OpenTime = new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc),
			CloseTime = new DateTime(2024, 3, 1, 12, 5, 0, DateTimeKind.Utc),
			OpenPrice = 61000m,
			HighPrice = 61200m,
			LowPrice = 60900m,
			ClosePrice = 61100m,
			TotalVolume = 100m,
		};
		candle.SetSubscriptionIds([400]);

		await inner.EmitOutMessage(candle, CancellationToken);

		// Should produce Level1 with OHLCV
		var l1Messages = output.OfType<Level1ChangeMessage>().ToList();
		AreEqual(1, l1Messages.Count);

		var l1 = l1Messages[0];
		AreEqual(61000m, (decimal)l1.Changes[Level1Fields.OpenPrice]);
		AreEqual(61200m, (decimal)l1.Changes[Level1Fields.HighPrice]);
		AreEqual(60900m, (decimal)l1.Changes[Level1Fields.LowPrice]);
		AreEqual(61100m, (decimal)l1.Changes[Level1Fields.ClosePrice]);

		// Candle should NOT be passed through (fully consumed)
		AreEqual(0, output.OfType<TimeFrameCandleMessage>().Count());
	}

	#endregion

	#region Mixed Subscriptions

	/// <summary>
	/// When a message has subscription IDs for both Level1 (intercepted) and non-Level1 subscriptions,
	/// Level1 gets the converted message and the original message is forwarded to remaining subscriptions.
	/// </summary>
	[TestMethod]
	public async Task MixedSubscriptions_SplitsMessage()
	{
		var (wrapper, inner, output) = CreateAdapter();

		// Subscribe Level1 (will be rewritten to MarketDepth)
		var l1Sub = new MarketDataMessage
		{
			SecurityId = _testSecId,
			DataType2 = DataType.Level1,
			IsSubscribe = true,
			TransactionId = 500,
		};
		await wrapper.SendInMessageAsync(l1Sub, CancellationToken);

		// Simulate subscription response
		await inner.EmitOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 500 }, CancellationToken);

		output.Clear();

		// QuoteChange with both L1 subscription (500) and a direct depth subscription (600)
		var quotes = new QuoteChangeMessage
		{
			SecurityId = _testSecId,
			ServerTime = new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc),
			Bids = [new QuoteChange(61000m, 1.5m)],
			Asks = [new QuoteChange(61001m, 2.0m)],
		};
		quotes.SetSubscriptionIds([500, 600]);

		await inner.EmitOutMessage(quotes, CancellationToken);

		// Should produce both Level1 (for sub 500) and QuoteChange (for sub 600)
		var l1Messages = output.OfType<Level1ChangeMessage>().ToList();
		AreEqual(1, l1Messages.Count);
		CollectionAssert.AreEqual(new long[] { 500 }, l1Messages[0].GetSubscriptionIds());

		var quoteMessages = output.OfType<QuoteChangeMessage>().ToList();
		AreEqual(1, quoteMessages.Count);
		CollectionAssert.AreEqual(new long[] { 600 }, quoteMessages[0].GetSubscriptionIds());
	}

	#endregion

	#region Reset

	/// <summary>
	/// After Reset, adapter should not convert any messages.
	/// </summary>
	[TestMethod]
	public async Task Reset_ClearsSubscriptions()
	{
		var (wrapper, inner, output) = CreateAdapter();

		// Subscribe Level1
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _testSecId,
			DataType2 = DataType.Level1,
			IsSubscribe = true,
			TransactionId = 700,
		};
		await wrapper.SendInMessageAsync(mdMsg, CancellationToken);
		await inner.EmitOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 700 }, CancellationToken);

		// Reset
		await wrapper.SendInMessageAsync(new ResetMessage(), CancellationToken);

		output.Clear();

		// Now send QuoteChange with sub 700 — should pass through as-is (no conversion)
		var quotes = new QuoteChangeMessage
		{
			SecurityId = _testSecId,
			ServerTime = new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc),
			Bids = [new QuoteChange(61000m, 1.5m)],
			Asks = [new QuoteChange(61001m, 2.0m)],
		};
		quotes.SetSubscriptionIds([700]);

		await inner.EmitOutMessage(quotes, CancellationToken);

		// Should get QuoteChange, not Level1
		AreEqual(0, output.OfType<Level1ChangeMessage>().Count());
		AreEqual(1, output.OfType<QuoteChangeMessage>().Count());
	}

	#endregion

	#region Unsubscribe

	/// <summary>
	/// After unsubscribing, messages should pass through without conversion.
	/// </summary>
	[TestMethod]
	public async Task Unsubscribe_StopsConversion()
	{
		var (wrapper, inner, output) = CreateAdapter();

		// Subscribe
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _testSecId,
			DataType2 = DataType.Level1,
			IsSubscribe = true,
			TransactionId = 800,
		};
		await wrapper.SendInMessageAsync(mdMsg, CancellationToken);
		await inner.EmitOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 800 }, CancellationToken);

		// Unsubscribe
		var unsub = new MarketDataMessage
		{
			IsSubscribe = false,
			OriginalTransactionId = 800,
			TransactionId = 801,
		};
		await wrapper.SendInMessageAsync(unsub, CancellationToken);

		output.Clear();

		// Send QuoteChange — should pass through as-is
		var quotes = new QuoteChangeMessage
		{
			SecurityId = _testSecId,
			ServerTime = new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc),
			Bids = [new QuoteChange(61000m, 1.5m)],
			Asks = [new QuoteChange(61001m, 2.0m)],
		};
		quotes.SetSubscriptionIds([800]);

		await inner.EmitOutMessage(quotes, CancellationToken);

		AreEqual(0, output.OfType<Level1ChangeMessage>().Count());
		AreEqual(1, output.OfType<QuoteChangeMessage>().Count());
	}

	#endregion

	#region Subscription Error

	/// <summary>
	/// If subscription response is an error, the subscription should be removed.
	/// </summary>
	[TestMethod]
	public async Task SubscriptionError_RemovesSubscription()
	{
		var (wrapper, inner, output) = CreateAdapter();

		// Subscribe
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _testSecId,
			DataType2 = DataType.Level1,
			IsSubscribe = true,
			TransactionId = 900,
		};
		await wrapper.SendInMessageAsync(mdMsg, CancellationToken);

		// Error response
		var errorResponse = new SubscriptionResponseMessage
		{
			OriginalTransactionId = 900,
			Error = new InvalidOperationException("test error"),
		};
		await inner.EmitOutMessage(errorResponse, CancellationToken);

		output.Clear();

		// QuoteChange should pass through as-is
		var quotes = new QuoteChangeMessage
		{
			SecurityId = _testSecId,
			ServerTime = new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc),
			Bids = [new QuoteChange(61000m, 1.5m)],
			Asks = [new QuoteChange(61001m, 2.0m)],
		};
		quotes.SetSubscriptionIds([900]);

		await inner.EmitOutMessage(quotes, CancellationToken);

		AreEqual(0, output.OfType<Level1ChangeMessage>().Count());
		AreEqual(1, output.OfType<QuoteChangeMessage>().Count());
	}

	#endregion

	#region SubscriptionFinished

	/// <summary>
	/// SubscriptionFinished should remove the subscription from tracking.
	/// </summary>
	[TestMethod]
	public async Task SubscriptionFinished_RemovesSubscription()
	{
		var (wrapper, inner, output) = CreateAdapter();

		// Subscribe
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _testSecId,
			DataType2 = DataType.Level1,
			IsSubscribe = true,
			TransactionId = 1000,
		};
		await wrapper.SendInMessageAsync(mdMsg, CancellationToken);
		await inner.EmitOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 1000 }, CancellationToken);

		// Finished
		await inner.EmitOutMessage(new SubscriptionFinishedMessage { OriginalTransactionId = 1000 }, CancellationToken);

		output.Clear();

		// QuoteChange should pass through as-is
		var quotes = new QuoteChangeMessage
		{
			SecurityId = _testSecId,
			ServerTime = new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc),
			Bids = [new QuoteChange(61000m, 1.5m)],
			Asks = [new QuoteChange(61001m, 2.0m)],
		};
		quotes.SetSubscriptionIds([1000]);

		await inner.EmitOutMessage(quotes, CancellationToken);

		AreEqual(0, output.OfType<Level1ChangeMessage>().Count());
		AreEqual(1, output.OfType<QuoteChangeMessage>().Count());
	}

	#endregion

	#region QuoteChange State Messages

	/// <summary>
	/// QuoteChangeMessage with State != null (e.g., snapshot/incremental markers) should not be converted.
	/// </summary>
	[TestMethod]
	public async Task QuoteChange_WithState_NotConverted()
	{
		var (wrapper, inner, output) = CreateAdapter();

		// Subscribe Level1
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _testSecId,
			DataType2 = DataType.Level1,
			IsSubscribe = true,
			TransactionId = 1100,
		};
		await wrapper.SendInMessageAsync(mdMsg, CancellationToken);
		await inner.EmitOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 1100 }, CancellationToken);

		output.Clear();

		// QuoteChange with State set (e.g., snapshot marker)
		var quotes = new QuoteChangeMessage
		{
			SecurityId = _testSecId,
			ServerTime = new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc),
			Bids = [new QuoteChange(61000m, 1.5m)],
			Asks = [new QuoteChange(61001m, 2.0m)],
			State = QuoteChangeStates.SnapshotComplete,
		};
		quotes.SetSubscriptionIds([1100]);

		await inner.EmitOutMessage(quotes, CancellationToken);

		// State messages pass through as QuoteChange, not converted
		AreEqual(0, output.OfType<Level1ChangeMessage>().Count());
		AreEqual(1, output.OfType<QuoteChangeMessage>().Count());
	}

	#endregion

	#region Non-Tick Execution Messages

	/// <summary>
	/// ExecutionMessage with DataType != Ticks (e.g., order log or transactions) should not be converted.
	/// </summary>
	[TestMethod]
	public async Task Execution_NonTick_NotConverted()
	{
		var (wrapper, inner, output) = CreateAdapter();

		// Subscribe Level1 with BuildFrom=Ticks
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _testSecId,
			DataType2 = DataType.Level1,
			BuildFrom = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 1200,
		};
		await wrapper.SendInMessageAsync(mdMsg, CancellationToken);
		await inner.EmitOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 1200 }, CancellationToken);

		output.Clear();

		// ExecutionMessage that is NOT a tick (e.g., transaction)
		var exec = new ExecutionMessage
		{
			SecurityId = _testSecId,
			DataTypeEx = DataType.Transactions,
			ServerTime = new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc),
		};
		exec.SetSubscriptionIds([1200]);

		await inner.EmitOutMessage(exec, CancellationToken);

		// Should pass through as ExecutionMessage, not converted
		AreEqual(0, output.OfType<Level1ChangeMessage>().Count());
		AreEqual(1, output.OfType<ExecutionMessage>().Count());
	}

	#endregion

	#region Wrong DataType Mismatch

	/// <summary>
	/// When subscription was rewritten to MarketDepth, tick messages should NOT be converted
	/// (TryConvertAsync checks that info.Origin.DataType2 matches the incoming data type).
	/// </summary>
	[TestMethod]
	public async Task Tick_WithDepthSubscription_NotConverted()
	{
		var (wrapper, inner, output) = CreateAdapter();

		// Subscribe Level1 without BuildFrom (defaults to MarketDepth)
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _testSecId,
			DataType2 = DataType.Level1,
			IsSubscribe = true,
			TransactionId = 1300,
		};
		await wrapper.SendInMessageAsync(mdMsg, CancellationToken);
		await inner.EmitOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 1300 }, CancellationToken);

		output.Clear();

		// Send tick with this subscription ID — should NOT be converted
		// because the subscription expects MarketDepth, not Ticks
		var tick = new ExecutionMessage
		{
			SecurityId = _testSecId,
			DataTypeEx = DataType.Ticks,
			ServerTime = new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc),
			TradePrice = 61050m,
			TradeVolume = 0.5m,
		};
		tick.SetSubscriptionIds([1300]);

		await inner.EmitOutMessage(tick, CancellationToken);

		// Tick passes through unchanged
		AreEqual(0, output.OfType<Level1ChangeMessage>().Count());
		AreEqual(1, output.OfType<ExecutionMessage>().Count());
	}

	/// <summary>
	/// When subscription was rewritten to Ticks (BuildFrom=Ticks), QuoteChange should NOT be converted.
	/// </summary>
	[TestMethod]
	public async Task QuoteChange_WithTickSubscription_NotConverted()
	{
		var (wrapper, inner, output) = CreateAdapter();

		// Subscribe Level1 with BuildFrom=Ticks
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _testSecId,
			DataType2 = DataType.Level1,
			BuildFrom = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 1400,
		};
		await wrapper.SendInMessageAsync(mdMsg, CancellationToken);
		await inner.EmitOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 1400 }, CancellationToken);

		output.Clear();

		// Send QuoteChange — should NOT be converted (subscription is for Ticks, not Depth)
		var quotes = new QuoteChangeMessage
		{
			SecurityId = _testSecId,
			ServerTime = new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc),
			Bids = [new QuoteChange(61000m, 1.5m)],
			Asks = [new QuoteChange(61001m, 2.0m)],
		};
		quotes.SetSubscriptionIds([1400]);

		await inner.EmitOutMessage(quotes, CancellationToken);

		// QuoteChange passes through unchanged
		AreEqual(0, output.OfType<Level1ChangeMessage>().Count());
		AreEqual(1, output.OfType<QuoteChangeMessage>().Count());
	}

	#endregion
}

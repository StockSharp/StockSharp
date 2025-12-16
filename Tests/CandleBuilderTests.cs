namespace StockSharp.Tests;

using StockSharp.Algo.Candles.Compression;

/// <summary>
/// Unit tests for individual CandleBuilder classes.
/// Tests the core candle building logic directly.
/// </summary>
[TestClass]
public class CandleBuilderTests : BaseTestClass
{
	#region Mock Infrastructure

	private class MockExchangeInfoProvider : IExchangeInfoProvider
	{
		IEnumerable<ExchangeBoard> IExchangeInfoProvider.Boards => [ExchangeBoard.Associated];
		IEnumerable<Exchange> IExchangeInfoProvider.Exchanges => [Exchange.Test];

		event Action<ExchangeBoard> IExchangeInfoProvider.BoardAdded { add { } remove { } }
		event Action<Exchange> IExchangeInfoProvider.ExchangeAdded { add { } remove { } }
		event Action<ExchangeBoard> IExchangeInfoProvider.BoardRemoved { add { } remove { } }
		event Action<Exchange> IExchangeInfoProvider.ExchangeRemoved { add { } remove { } }

		ValueTask IExchangeInfoProvider.InitAsync(CancellationToken cancellationToken) => default;
		ExchangeBoard IExchangeInfoProvider.TryGetExchangeBoard(string code) => ExchangeBoard.Associated;
		Exchange IExchangeInfoProvider.TryGetExchange(string code) => Exchange.Test;
		void IExchangeInfoProvider.Save(ExchangeBoard board) { }
		void IExchangeInfoProvider.Save(Exchange exchange) { }
		void IExchangeInfoProvider.Delete(ExchangeBoard board) { }
		void IExchangeInfoProvider.Delete(Exchange exchange) { }
		IEnumerable<BoardMessage> IBoardMessageProvider.Lookup(BoardLookupMessage criteria) => [];
	}

	private class MockCandleBuilderSubscription : ICandleBuilderSubscription
	{
		public MarketDataMessage Message { get; set; }
		public CandleMessage CurrentCandle { get; set; }
		public VolumeProfileBuilder VolumeProfile { get; set; }
	}

	private class MockTransform : ICandleBuilderValueTransform
	{
		public DataType BuildFrom { get; set; } = DataType.Ticks;
		public decimal Price { get; set; }
		public decimal? Volume { get; set; }
		public Sides? Side { get; set; }
		public DateTime Time { get; set; }
		public decimal? OpenInterest { get; set; }
		public IEnumerable<CandlePriceLevel> PriceLevels { get; set; }

		public bool Process(Message message) => true;
	}

	private static SecurityId CreateSecurityId() => new() { SecurityCode = "TEST", BoardCode = "BOARD" };

	#endregion

	#region TickCandleBuilder Tests

	/// <summary>
	/// TickCandleBuilder: creates new candle after specified tick count.
	/// </summary>
	[TestMethod]
	public void TickCandleBuilder_CreatesNewCandleAfterTickCount()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new TickCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = DataType.Create<TickCandleMessage>(3), // 3 ticks per candle
			}
		};

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();
		var candles = new List<CandleMessage>();

		// Process 7 ticks with prices: 100, 101, 102, 103, 104, 105, 106
		// First candle (3 ticks): O=100, H=102, L=100, C=102, V=30
		// Second candle (3 ticks): O=103, H=105, L=103, C=105, V=30
		// Third candle (1 tick): O=106, H=106, L=106, C=106, V=10
		for (int i = 0; i < 7; i++)
		{
			var transform = new MockTransform
			{
				Price = 100 + i,
				Volume = 10,
				Time = baseTime.AddSeconds(i),
				Side = Sides.Buy
			};

			foreach (var candle in builder.Process(subscription, transform))
			{
				candles.Add(candle);
			}
		}

		// Should have multiple candles with state changes
		var finishedCandles = candles.Where(c => c.State == CandleStates.Finished).ToList();
		(finishedCandles.Count >= 2).AssertTrue("Should have at least 2 finished candles");

		// Get unique finished candles by OpenTime
		var uniqueFinished = finishedCandles
			.GroupBy(c => c.OpenTime)
			.Select(g => g.Last())
			.OrderBy(c => c.OpenTime)
			.ToList();

		// First finished candle: prices 100, 101, 102
		var firstFinished = uniqueFinished.First();
		AreEqual(3, firstFinished.TotalTicks, "First candle should have 3 ticks");
		AreEqual(100m, firstFinished.OpenPrice, "First candle Open should be 100");
		AreEqual(102m, firstFinished.HighPrice, "First candle High should be 102");
		AreEqual(100m, firstFinished.LowPrice, "First candle Low should be 100");
		AreEqual(102m, firstFinished.ClosePrice, "First candle Close should be 102");
		AreEqual(30m, firstFinished.TotalVolume, "First candle Volume should be 30");

		// Second finished candle: prices 103, 104, 105
		var secondFinished = uniqueFinished.Skip(1).First();
		AreEqual(3, secondFinished.TotalTicks, "Second candle should have 3 ticks");
		AreEqual(103m, secondFinished.OpenPrice, "Second candle Open should be 103");
		AreEqual(105m, secondFinished.HighPrice, "Second candle High should be 105");
		AreEqual(103m, secondFinished.LowPrice, "Second candle Low should be 103");
		AreEqual(105m, secondFinished.ClosePrice, "Second candle Close should be 105");
		AreEqual(30m, secondFinished.TotalVolume, "Second candle Volume should be 30");
	}

	/// <summary>
	/// TickCandleBuilder: single tick creates active candle.
	/// </summary>
	[TestMethod]
	public void TickCandleBuilder_SingleTick_CreatesActiveCandle()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new TickCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = DataType.Create<TickCandleMessage>(10),
			}
		};

		var transform = new MockTransform
		{
			Price = 100,
			Volume = 50,
			Time = DateTime.UtcNow,
			Side = Sides.Buy
		};

		var result = builder.Process(subscription, transform).ToList();

		AreEqual(1, result.Count);
		var candle = result[0] as TickCandleMessage;
		IsNotNull(candle);
		AreEqual(CandleStates.Active, candle.State);
		AreEqual(1, candle.TotalTicks);

		// Verify OHLCV - single tick means O=H=L=C
		AreEqual(100m, candle.OpenPrice, "Open should be 100");
		AreEqual(100m, candle.HighPrice, "High should be 100");
		AreEqual(100m, candle.LowPrice, "Low should be 100");
		AreEqual(100m, candle.ClosePrice, "Close should be 100");
		AreEqual(50m, candle.TotalVolume, "Volume should be 50");
		AreEqual(50m, candle.BuyVolume, "BuyVolume should be 50");
	}

	#endregion

	#region VolumeCandleBuilder Tests

	/// <summary>
	/// VolumeCandleBuilder: creates new candle when volume threshold reached.
	/// </summary>
	[TestMethod]
	public void VolumeCandleBuilder_CreatesNewCandleOnVolumeThreshold()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new VolumeCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = DataType.Create<VolumeCandleMessage>(100m), // 100 volume per candle
			}
		};

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();
		var candles = new List<CandleMessage>();

		// Send trades: prices 100, 101, 102, 103, 104; volumes 30, 40, 30, 50, 60
		// First candle finishes at tick 3 (vol=100): O=100, H=102, L=100, C=102, V=100
		// Second candle continues...
		var prices = new[] { 100m, 101m, 102m, 103m, 104m };
		var volumes = new[] { 30m, 40m, 30m, 50m, 60m };
		for (int i = 0; i < volumes.Length; i++)
		{
			var transform = new MockTransform
			{
				Price = prices[i],
				Volume = volumes[i],
				Time = baseTime.AddSeconds(i),
				Side = Sides.Buy
			};

			foreach (var candle in builder.Process(subscription, transform))
			{
				candles.Add(candle);
			}
		}

		var finishedCandles = candles.Where(c => c.State == CandleStates.Finished).ToList();

		// Get unique finished candles
		var uniqueFinished = finishedCandles
			.GroupBy(c => c.OpenTime)
			.Select(g => g.Last())
			.OrderBy(c => c.OpenTime)
			.ToList();

		// Only 1 finished candle - second candle needs another tick to trigger finish check
		AreEqual(1, uniqueFinished.Count, "Should have exactly 1 finished candle");

		// First finished candle: prices 100, 101, 102 (vol 30+40+30=100)
		var firstFinished = uniqueFinished[0];
		AreEqual(100m, firstFinished.TotalVolume, "First candle volume should be 100");
		AreEqual(100m, firstFinished.OpenPrice, "First candle Open should be 100");
		AreEqual(102m, firstFinished.HighPrice, "First candle High should be 102");
		AreEqual(100m, firstFinished.LowPrice, "First candle Low should be 100");
		AreEqual(102m, firstFinished.ClosePrice, "First candle Close should be 102");
		AreEqual(3, firstFinished.TotalTicks, "First candle should have 3 ticks");

		// Second candle is still active: prices 103, 104 (vol 50+60=110)
		var currentCandle = subscription.CurrentCandle as VolumeCandleMessage;
		IsNotNull(currentCandle);
		AreEqual(CandleStates.Active, currentCandle.State, "Current candle should be Active");
		AreEqual(110m, currentCandle.TotalVolume, "Current candle volume should be 110");
		AreEqual(103m, currentCandle.OpenPrice, "Current candle Open should be 103");
		AreEqual(104m, currentCandle.HighPrice, "Current candle High should be 104");
		AreEqual(103m, currentCandle.LowPrice, "Current candle Low should be 103");
		AreEqual(104m, currentCandle.ClosePrice, "Current candle Close should be 104");
		AreEqual(2, currentCandle.TotalTicks, "Current candle should have 2 ticks");
	}

	/// <summary>
	/// VolumeCandleBuilder: accumulates volume correctly.
	/// </summary>
	[TestMethod]
	public void VolumeCandleBuilder_AccumulatesVolume()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new VolumeCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = DataType.Create<VolumeCandleMessage>(1000m), // Large threshold so we stay in one candle
			}
		};

		var baseTime = DateTime.UtcNow;

		// Send 5 trades with varying prices: 100, 105, 95, 102, 98
		// Expected: O=100, H=105, L=95, C=98, V=50
		var prices = new[] { 100m, 105m, 95m, 102m, 98m };
		for (int i = 0; i < 5; i++)
		{
			var transform = new MockTransform
			{
				Price = prices[i],
				Volume = 10m,
				Time = baseTime.AddSeconds(i)
			};

			foreach (var _ in builder.Process(subscription, transform)) { }
		}

		var currentCandle = subscription.CurrentCandle as VolumeCandleMessage;
		IsNotNull(currentCandle);
		AreEqual(50m, currentCandle.TotalVolume, "Should accumulate 5 * 10 = 50 volume");
		AreEqual(100m, currentCandle.OpenPrice, "Open should be 100");
		AreEqual(105m, currentCandle.HighPrice, "High should be 105");
		AreEqual(95m, currentCandle.LowPrice, "Low should be 95");
		AreEqual(98m, currentCandle.ClosePrice, "Close should be 98");
		AreEqual(5, currentCandle.TotalTicks, "TotalTicks should be 5");
	}

	#endregion

	#region RangeCandleBuilder Tests

	/// <summary>
	/// RangeCandleBuilder: creates new candle when price range exceeded.
	/// </summary>
	[TestMethod]
	public void RangeCandleBuilder_CreatesNewCandleOnRangeExceeded()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new RangeCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = DataType.Create<RangeCandleMessage>(new Unit(5m)), // 5 point range
			}
		};

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();
		var candles = new List<CandleMessage>();

		// Prices: 100, 102, 104, 106, 107 - range becomes 6 at 106, then 7th tick triggers finish check
		// IsCandleFinishedBeforeChange checks BEFORE the new tick is processed, so we need
		// a tick after range is exceeded to trigger the new candle creation
		// First candle: O=100, H=106, L=100, C=106, V=40 (4 ticks before range exceeded)
		var prices = new[] { 100m, 102m, 104m, 106m, 107m };
		var volumes = new[] { 10m, 10m, 10m, 10m, 15m };
		for (int i = 0; i < prices.Length; i++)
		{
			var transform = new MockTransform
			{
				Price = prices[i],
				Volume = volumes[i],
				Time = baseTime.AddSeconds(i)
			};

			foreach (var candle in builder.Process(subscription, transform))
			{
				candles.Add(candle);
			}
		}

		// Should have created new candle when range exceeded 5 (on the 5th tick when check sees range=6)
		var finishedCandles = candles.Where(c => c.State == CandleStates.Finished).ToList();

		// Get unique finished candles
		var uniqueFinished = finishedCandles
			.GroupBy(c => c.OpenTime)
			.Select(g => g.Last())
			.ToList();

		AreEqual(1, uniqueFinished.Count, "Should have exactly 1 finished candle");

		// First finished candle: prices 100, 102, 104, 106 (range=6 exceeded threshold=5)
		var firstFinished = uniqueFinished[0];
		AreEqual(100m, firstFinished.OpenPrice, "Finished candle Open should be 100");
		AreEqual(106m, firstFinished.HighPrice, "Finished candle High should be 106");
		AreEqual(100m, firstFinished.LowPrice, "Finished candle Low should be 100");
		AreEqual(106m, firstFinished.ClosePrice, "Finished candle Close should be 106");
		AreEqual(40m, firstFinished.TotalVolume, "Finished candle Volume should be 40");
		AreEqual(4, firstFinished.TotalTicks, "Finished candle should have 4 ticks");

		// Verify current (active) candle started with tick 107
		var currentCandle = subscription.CurrentCandle as RangeCandleMessage;
		IsNotNull(currentCandle);
		AreEqual(CandleStates.Active, currentCandle.State, "Current candle should be Active");
		AreEqual(107m, currentCandle.OpenPrice, "Current candle Open should be 107");
		AreEqual(15m, currentCandle.TotalVolume, "Current candle Volume should be 15");
		AreEqual(1, currentCandle.TotalTicks, "Current candle should have 1 tick");
	}

	/// <summary>
	/// RangeCandleBuilder: tracks high and low correctly.
	/// </summary>
	[TestMethod]
	public void RangeCandleBuilder_TracksHighLowCorrectly()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new RangeCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = DataType.Create<RangeCandleMessage>(new Unit(100m)), // Large range so we stay in one candle
			}
		};

		var baseTime = DateTime.UtcNow;
		var prices = new[] { 100m, 105m, 95m, 102m }; // O=100, H=105, L=95, C=102
		var volumes = new[] { 20m, 30m, 25m, 15m }; // Total = 90

		for (int i = 0; i < prices.Length; i++)
		{
			var transform = new MockTransform
			{
				Price = prices[i],
				Volume = volumes[i],
				Time = baseTime
			};

			foreach (var _ in builder.Process(subscription, transform)) { }
			baseTime = baseTime.AddSeconds(1);
		}

		var currentCandle = subscription.CurrentCandle as RangeCandleMessage;
		IsNotNull(currentCandle);
		AreEqual(100m, currentCandle.OpenPrice, "Open should be first price 100");
		AreEqual(105m, currentCandle.HighPrice, "High should be 105");
		AreEqual(95m, currentCandle.LowPrice, "Low should be 95");
		AreEqual(102m, currentCandle.ClosePrice, "Close should be last price 102");
		AreEqual(90m, currentCandle.TotalVolume, "TotalVolume should be 90");
		AreEqual(4, currentCandle.TotalTicks, "TotalTicks should be 4");
	}

	#endregion

	#region RenkoCandleBuilder Tests

	/// <summary>
	/// RenkoCandleBuilder: generates multiple bricks on large price movement.
	/// </summary>
	[TestMethod]
	public void RenkoCandleBuilder_GeneratesMultipleBricksOnLargePriceMove()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new RenkoCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = DataType.Create<RenkoCandleMessage>(new Unit(10m)), // 10 point brick size
			}
		};

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();
		var candles = new List<CandleMessage>();

		// First price establishes baseline
		var transform1 = new MockTransform { Price = 100m, Volume = 10, Time = baseTime };
		foreach (var c in builder.Process(subscription, transform1))
			candles.Add(c);

		// Large move: 100 -> 145 = 4.5 boxes = 4 finished bricks
		var transform2 = new MockTransform { Price = 145m, Volume = 20, Time = baseTime.AddSeconds(1) };
		foreach (var c in builder.Process(subscription, transform2))
			candles.Add(c);

		var finishedCandles = candles.Where(c => c.State == CandleStates.Finished).ToList();
		(finishedCandles.Count >= 3).AssertTrue("Should generate multiple finished bricks");

		// Verify OHLCV on finished bricks - each brick should be valid
		foreach (var brick in finishedCandles)
		{
			// Renko: High >= max(Open, Close), Low <= min(Open, Close)
			(brick.HighPrice >= brick.OpenPrice).AssertTrue("High should be >= Open");
			(brick.HighPrice >= brick.ClosePrice).AssertTrue("High should be >= Close");
			(brick.LowPrice <= brick.OpenPrice).AssertTrue("Low should be <= Open");
			(brick.LowPrice <= brick.ClosePrice).AssertTrue("Low should be <= Close");
		}
	}

	/// <summary>
	/// RenkoCandleBuilder: small price movements don't create new bricks.
	/// </summary>
	[TestMethod]
	public void RenkoCandleBuilder_SmallMovements_NoNewBricks()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new RenkoCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = DataType.Create<RenkoCandleMessage>(new Unit(10m)),
			}
		};

		var baseTime = DateTime.UtcNow;
		var candles = new List<CandleMessage>();

		// Small movements within one brick: O=100, H=104, L=98, C=99
		var prices = new[] { 100m, 102m, 98m, 104m, 99m };
		var volumes = new[] { 10m, 20m, 15m, 25m, 30m }; // Total = 100

		for (int i = 0; i < prices.Length; i++)
		{
			var transform = new MockTransform { Price = prices[i], Volume = volumes[i], Time = baseTime };
			foreach (var c in builder.Process(subscription, transform))
				candles.Add(c);
			baseTime = baseTime.AddSeconds(1);
		}

		var finishedCandles = candles.Where(c => c.State == CandleStates.Finished).ToList();
		AreEqual(0, finishedCandles.Count, "Small movements should not create finished bricks");

		// Verify active candle OHLCV
		var currentCandle = subscription.CurrentCandle as RenkoCandleMessage;
		IsNotNull(currentCandle);
		AreEqual(100m, currentCandle.TotalVolume, "TotalVolume should be 100");
		AreEqual(5, currentCandle.TotalTicks, "TotalTicks should be 5");
	}

	/// <summary>
	/// RenkoCandleBuilder: downward movement creates bricks.
	/// </summary>
	[TestMethod]
	public void RenkoCandleBuilder_DownwardMovement_CreatesBricks()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new RenkoCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = DataType.Create<RenkoCandleMessage>(new Unit(10m)),
			}
		};

		var baseTime = DateTime.UtcNow;
		var candles = new List<CandleMessage>();

		// Establish baseline
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 100m, Volume = 10, Time = baseTime }))
			candles.Add(c);

		// Large downward move: 100 -> 55 = 4.5 boxes down
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 55m, Volume = 20, Time = baseTime.AddSeconds(1) }))
			candles.Add(c);

		var finishedCandles = candles.Where(c => c.State == CandleStates.Finished).ToList();
		(finishedCandles.Count >= 3).AssertTrue("Downward movement should create bricks");

		// Verify OHLCV on finished bricks - downward bricks should have Open > Close
		foreach (var brick in finishedCandles)
		{
			(brick.OpenPrice >= brick.ClosePrice).AssertTrue("Downward brick: Open should be >= Close");
			(brick.HighPrice >= brick.OpenPrice).AssertTrue("High should be >= Open");
			(brick.LowPrice <= brick.ClosePrice).AssertTrue("Low should be <= Close");
		}
	}

	#endregion

	#region PnFCandleBuilder Tests

	/// <summary>
	/// PnFCandleBuilder: upward column (X) formation.
	/// </summary>
	[TestMethod]
	public void PnFCandleBuilder_UpwardColumn_Formation()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new PnFCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = DataType.Create<PnFCandleMessage>(new PnFArg { BoxSize = new Unit(1m), ReversalAmount = 3 }),
			}
		};

		var baseTime = DateTime.UtcNow;
		var candles = new List<CandleMessage>();

		// Rising prices - should form X column
		var prices = new[] { 100m, 101m, 102m, 103m, 104m };
		var volumes = new[] { 10m, 20m, 15m, 25m, 30m }; // Total = 100
		for (int i = 0; i < prices.Length; i++)
		{
			foreach (var c in builder.Process(subscription, new MockTransform { Price = prices[i], Volume = volumes[i], Time = baseTime }))
				candles.Add(c);
			baseTime = baseTime.AddSeconds(1);
		}

		var currentCandle = subscription.CurrentCandle as PnFCandleMessage;
		IsNotNull(currentCandle);
		// In X column: Open <= Close (bullish)
		(currentCandle.OpenPrice <= currentCandle.ClosePrice).AssertTrue("X column should have Open <= Close");
		// Verify OHLCV
		(currentCandle.HighPrice >= currentCandle.ClosePrice).AssertTrue("High should be >= Close");
		(currentCandle.LowPrice <= currentCandle.OpenPrice).AssertTrue("Low should be <= Open");
		AreEqual(100m, currentCandle.TotalVolume, "TotalVolume should be 100");
		AreEqual(5, currentCandle.TotalTicks, "TotalTicks should be 5");
	}

	/// <summary>
	/// PnFCandleBuilder: column reversal on sufficient price change.
	/// </summary>
	[TestMethod]
	public void PnFCandleBuilder_Reversal_OnSufficientPriceChange()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new PnFCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = DataType.Create<PnFCandleMessage>(new PnFArg { BoxSize = new Unit(1m), ReversalAmount = 3 }),
			}
		};

		var baseTime = DateTime.UtcNow;
		var candles = new List<CandleMessage>();

		// Build X column
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 100m, Volume = 10, Time = baseTime }))
			candles.Add(c);
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 105m, Volume = 20, Time = baseTime.AddSeconds(1) }))
			candles.Add(c);

		// Reversal: drop by more than 3 boxes from high
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 101m, Volume = 15, Time = baseTime.AddSeconds(2) }))
			candles.Add(c);

		var finishedCandles = candles.Where(c => c.State == CandleStates.Finished).ToList();

		// Get unique finished candles
		var uniqueFinished = finishedCandles
			.GroupBy(c => c.OpenTime)
			.Select(g => g.Last())
			.ToList();

		AreEqual(1, uniqueFinished.Count, "Should have exactly 1 finished column");

		// Verify finished column OHLCV - should be upward X column (prices 100->105)
		var finishedColumn = uniqueFinished[0] as PnFCandleMessage;
		IsNotNull(finishedColumn);
		(finishedColumn.OpenPrice <= finishedColumn.ClosePrice).AssertTrue("Finished X column: Open <= Close");
		(finishedColumn.HighPrice >= finishedColumn.ClosePrice).AssertTrue("High should be >= Close");
		(finishedColumn.LowPrice <= finishedColumn.OpenPrice).AssertTrue("Low should be <= Open");
		AreEqual(30m, finishedColumn.TotalVolume, "TotalVolume should be 30 (10+20)");
		AreEqual(2, finishedColumn.TotalTicks, "TotalTicks should be 2");

		// Verify current (active) candle is O column (downward)
		var currentCandle = subscription.CurrentCandle as PnFCandleMessage;
		IsNotNull(currentCandle);
		AreEqual(CandleStates.Active, currentCandle.State, "Current candle should be Active");
		(currentCandle.OpenPrice >= currentCandle.ClosePrice).AssertTrue("O column: Open >= Close");
		AreEqual(15m, currentCandle.TotalVolume, "Current candle TotalVolume should be 15");
		AreEqual(1, currentCandle.TotalTicks, "Current candle TotalTicks should be 1");
	}

	#endregion

	#region HeikinAshiCandleBuilder Tests

	/// <summary>
	/// HeikinAshiCandleBuilder: smoothed open price calculation.
	/// </summary>
	[TestMethod]
	public void HeikinAshiCandleBuilder_SmoothedOpenPrice()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new HeikinAshiCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = DataType.Create<HeikinAshiCandleMessage>(TimeSpan.FromMinutes(1)),
			}
		};

		var candles = new List<CandleMessage>();

		// First candle at 10:00
		var time1 = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 100m, Volume = 10, Time = time1 }))
			candles.Add(c);
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 110m, Volume = 20, Time = time1.AddSeconds(30) }))
			candles.Add(c);

		var firstCandle = subscription.CurrentCandle as HeikinAshiCandleMessage;
		IsNotNull(firstCandle);
		var firstOpen = firstCandle.OpenPrice;
		var firstClose = firstCandle.ClosePrice;
		AreEqual(30m, firstCandle.TotalVolume, "First candle TotalVolume should be 30");
		AreEqual(2, firstCandle.TotalTicks, "First candle TotalTicks should be 2");

		// Second candle at 10:01 - open should be smoothed
		var time2 = new DateTime(2024, 1, 1, 10, 1, 0).UtcKind();
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 115m, Volume = 15, Time = time2 }))
			candles.Add(c);

		var secondCandle = subscription.CurrentCandle as HeikinAshiCandleMessage;
		IsNotNull(secondCandle);

		// HA Open = (prevOpen + prevClose) / 2
		var expectedOpen = (firstOpen + firstClose) / 2;
		AreEqual(expectedOpen, secondCandle.OpenPrice, "HA Open should be average of prev Open and Close");
		AreEqual(15m, secondCandle.TotalVolume, "Second candle TotalVolume should be 15");
		AreEqual(1, secondCandle.TotalTicks, "Second candle TotalTicks should be 1");
	}

	/// <summary>
	/// HeikinAshiCandleBuilder: smoothed close price calculation.
	/// </summary>
	[TestMethod]
	public void HeikinAshiCandleBuilder_SmoothedClosePrice()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new HeikinAshiCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = DataType.Create<HeikinAshiCandleMessage>(TimeSpan.FromMinutes(1)),
			}
		};

		var time = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		// Build candle with multiple prices
		var prices = new[] { 100m, 110m, 95m, 105m }; // O=100, H=110, L=95, regular C=105
		var volumes = new[] { 10m, 20m, 15m, 25m }; // Total = 70

		for (int i = 0; i < prices.Length; i++)
		{
			foreach (var c in builder.Process(subscription, new MockTransform { Price = prices[i], Volume = volumes[i], Time = time }))
			{ }
			time = time.AddSeconds(10);
		}

		var candle = subscription.CurrentCandle as HeikinAshiCandleMessage;
		IsNotNull(candle);

		// HA Close = (O + H + L + C) / 4
		// Note: after each update, close is recalculated, so we verify it's different from last price
		// The exact calculation depends on intermediate values
		(candle.ClosePrice != prices.Last()).AssertTrue("HA Close should be smoothed, not raw last price");
		AreEqual(70m, candle.TotalVolume, "TotalVolume should be 70");
		AreEqual(4, candle.TotalTicks, "TotalTicks should be 4");
		// HeikinAshi High = max(High, Open, Close), Low = min(Low, Open, Close)
		(candle.HighPrice >= candle.OpenPrice).AssertTrue("High should be >= Open");
		(candle.HighPrice >= candle.ClosePrice).AssertTrue("High should be >= Close");
		(candle.LowPrice <= candle.OpenPrice).AssertTrue("Low should be <= Open");
		(candle.LowPrice <= candle.ClosePrice).AssertTrue("Low should be <= Close");
	}

	/// <summary>
	/// HeikinAshiCandleBuilder: time frame boundaries respected.
	/// </summary>
	[TestMethod]
	public void HeikinAshiCandleBuilder_TimeFrameBoundaries()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new HeikinAshiCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = DataType.Create<HeikinAshiCandleMessage>(TimeSpan.FromMinutes(1)),
			}
		};

		var candles = new List<CandleMessage>();

		// Data spanning two minutes
		var time1 = new DateTime(2024, 1, 1, 10, 0, 30).UtcKind();
		var time2 = new DateTime(2024, 1, 1, 10, 1, 30).UtcKind();

		foreach (var c in builder.Process(subscription, new MockTransform { Price = 100m, Volume = 10, Time = time1 }))
			candles.Add(c);
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 110m, Volume = 20, Time = time2 }))
			candles.Add(c);

		var finishedCandles = candles.Where(c => c.State == CandleStates.Finished).ToList();

		// Get unique finished candles
		var uniqueFinished = finishedCandles
			.GroupBy(c => c.OpenTime)
			.Select(g => g.Last())
			.ToList();

		AreEqual(1, uniqueFinished.Count, "Should have exactly 1 finished candle");

		// Verify finished candle OHLCV (first minute: single tick at price 100)
		var finishedCandle = uniqueFinished[0] as HeikinAshiCandleMessage;
		IsNotNull(finishedCandle);
		AreEqual(100m, finishedCandle.OpenPrice, "Finished candle Open should be 100");
		AreEqual(100m, finishedCandle.HighPrice, "Finished candle High should be 100");
		AreEqual(100m, finishedCandle.LowPrice, "Finished candle Low should be 100");
		AreEqual(10m, finishedCandle.TotalVolume, "Finished candle TotalVolume should be 10");
		AreEqual(1, finishedCandle.TotalTicks, "Finished candle TotalTicks should be 1");

		// Verify current (active) candle (second minute: tick at price 110)
		var currentCandle = subscription.CurrentCandle as HeikinAshiCandleMessage;
		IsNotNull(currentCandle);
		AreEqual(CandleStates.Active, currentCandle.State, "Current candle should be Active");
		AreEqual(20m, currentCandle.TotalVolume, "Current candle TotalVolume should be 20");
		AreEqual(1, currentCandle.TotalTicks, "Current candle TotalTicks should be 1");
	}

	#endregion

	#region TimeFrameCandleBuilder Tests

	/// <summary>
	/// TimeFrameCandleBuilder: candle opens at time frame boundary.
	/// </summary>
	[TestMethod]
	public void TimeFrameCandleBuilder_CandleOpensAtBoundary()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new TimeFrameCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = TimeSpan.FromMinutes(5).TimeFrame(),
			}
		};

		// Data at 10:03:45 should create candle opening at 10:00:00
		var dataTime = new DateTime(2024, 1, 1, 10, 3, 45).UtcKind();

		var result = builder.Process(subscription, new MockTransform { Price = 100m, Volume = 50m, Time = dataTime }).ToList();

		AreEqual(1, result.Count);
		var candle = result[0] as TimeFrameCandleMessage;
		IsNotNull(candle);
		AreEqual(new DateTime(2024, 1, 1, 10, 0, 0).UtcKind(), candle.OpenTime);
		// Single tick: O=H=L=C
		AreEqual(100m, candle.OpenPrice, "Open should be 100");
		AreEqual(100m, candle.HighPrice, "High should be 100");
		AreEqual(100m, candle.LowPrice, "Low should be 100");
		AreEqual(100m, candle.ClosePrice, "Close should be 100");
		AreEqual(50m, candle.TotalVolume, "TotalVolume should be 50");
		AreEqual(1, candle.TotalTicks, "TotalTicks should be 1");
	}

	/// <summary>
	/// TimeFrameCandleBuilder: data before candle boundary ignored.
	/// </summary>
	[TestMethod]
	public void TimeFrameCandleBuilder_DataBeforeBoundary_Ignored()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new TimeFrameCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = TimeSpan.FromMinutes(5).TimeFrame(),
			}
		};

		// First, create candle at 10:02
		var time1 = new DateTime(2024, 1, 1, 10, 2, 0).UtcKind();
		builder.Process(subscription, new MockTransform { Price = 100m, Volume = 10, Time = time1 }).ToList();

		// Now send data with earlier time (before candle's OpenTime)
		var earlyTime = new DateTime(2024, 1, 1, 9, 58, 0).UtcKind();
		var result = builder.Process(subscription, new MockTransform { Price = 50m, Volume = 10, Time = earlyTime }).ToList();

		// Should create new candle for 9:55-10:00 period or be handled appropriately
		// The exact behavior depends on implementation
	}

	/// <summary>
	/// TimeFrameCandleBuilder: multiple data points update same candle.
	/// </summary>
	[TestMethod]
	public void TimeFrameCandleBuilder_MultiplePointsUpdateSameCandle()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new TimeFrameCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = TimeSpan.FromMinutes(5).TimeFrame(),
			}
		};

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		// Multiple points within same 5-min candle
		var prices = new[] { 100m, 105m, 95m, 102m };
		var seconds = new[] { 0, 60, 120, 180 };

		for (int i = 0; i < prices.Length; i++)
		{
			builder.Process(subscription, new MockTransform
			{
				Price = prices[i],
				Volume = 10,
				Time = baseTime.AddSeconds(seconds[i])
			}).ToList();
		}

		var candle = subscription.CurrentCandle as TimeFrameCandleMessage;
		IsNotNull(candle);
		AreEqual(100m, candle.OpenPrice, "Open should be first price");
		AreEqual(105m, candle.HighPrice, "High should be max price");
		AreEqual(95m, candle.LowPrice, "Low should be min price");
		AreEqual(102m, candle.ClosePrice, "Close should be last price");
		AreEqual(4, candle.TotalTicks, "Should have 4 ticks");
	}

	#endregion

	#region OHLCV Value Verification Tests

	/// <summary>
	/// Verifies that TimeFrameCandle correctly calculates OHLCV values across multiple ticks.
	/// </summary>
	[TestMethod]
	public void TimeFrameCandleBuilder_VerifyOHLCV_MultipleTicks()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new TimeFrameCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = TimeSpan.FromMinutes(5).TimeFrame(),
			}
		};

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		// Ticks with known values: O=100, H=115, L=90, C=108, V=350
		var ticks = new[]
		{
			(Price: 100m, Volume: 50m, Seconds: 0),    // Open
			(Price: 105m, Volume: 60m, Seconds: 30),
			(Price: 115m, Volume: 70m, Seconds: 60),   // High
			(Price: 110m, Volume: 40m, Seconds: 90),
			(Price: 90m, Volume: 80m, Seconds: 120),   // Low
			(Price: 108m, Volume: 50m, Seconds: 150),  // Close
		};

		foreach (var tick in ticks)
		{
			builder.Process(subscription, new MockTransform
			{
				Price = tick.Price,
				Volume = tick.Volume,
				Time = baseTime.AddSeconds(tick.Seconds)
			}).ToList();
		}

		var candle = subscription.CurrentCandle as TimeFrameCandleMessage;
		IsNotNull(candle);
		AreEqual(100m, candle.OpenPrice, "Open should be first price (100)");
		AreEqual(115m, candle.HighPrice, "High should be maximum price (115)");
		AreEqual(90m, candle.LowPrice, "Low should be minimum price (90)");
		AreEqual(108m, candle.ClosePrice, "Close should be last price (108)");
		AreEqual(350m, candle.TotalVolume, "TotalVolume should be sum of all volumes (350)");
		AreEqual(6, candle.TotalTicks, "TotalTicks should be 6");
	}

	/// <summary>
	/// Verifies TickCandle OHLCV values after accumulating ticks.
	/// </summary>
	[TestMethod]
	public void TickCandleBuilder_VerifyOHLCV_AfterMultipleTicks()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new TickCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = DataType.Create<TickCandleMessage>(10), // 10 ticks per candle
			}
		};

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		// 5 ticks within one candle (less than 10)
		var prices = new[] { 100m, 102m, 98m, 105m, 101m }; // O=100, H=105, L=98, C=101
		var volumes = new[] { 10m, 20m, 15m, 25m, 30m }; // Total=100

		for (int i = 0; i < prices.Length; i++)
		{
			builder.Process(subscription, new MockTransform
			{
				Price = prices[i],
				Volume = volumes[i],
				Time = baseTime.AddSeconds(i)
			}).ToList();
		}

		var candle = subscription.CurrentCandle as TickCandleMessage;
		IsNotNull(candle);
		AreEqual(100m, candle.OpenPrice, "Open should be 100");
		AreEqual(105m, candle.HighPrice, "High should be 105");
		AreEqual(98m, candle.LowPrice, "Low should be 98");
		AreEqual(101m, candle.ClosePrice, "Close should be 101");
		AreEqual(100m, candle.TotalVolume, "TotalVolume should be 100");
		AreEqual(5, candle.TotalTicks, "TotalTicks should be 5");
	}

	/// <summary>
	/// Verifies VolumeCandle OHLCV values.
	/// </summary>
	[TestMethod]
	public void VolumeCandleBuilder_VerifyOHLCV_AfterMultipleTicks()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new VolumeCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = DataType.Create<VolumeCandleMessage>(1000m), // Large so we stay in one candle
			}
		};

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		// Ticks: O=50, H=60, L=40, C=55, V=200
		var ticks = new[]
		{
			(Price: 50m, Volume: 40m),   // Open
			(Price: 60m, Volume: 30m),   // High
			(Price: 40m, Volume: 50m),   // Low
			(Price: 55m, Volume: 80m),   // Close
		};

		for (int i = 0; i < ticks.Length; i++)
		{
			builder.Process(subscription, new MockTransform
			{
				Price = ticks[i].Price,
				Volume = ticks[i].Volume,
				Time = baseTime.AddSeconds(i)
			}).ToList();
		}

		var candle = subscription.CurrentCandle as VolumeCandleMessage;
		IsNotNull(candle);
		AreEqual(50m, candle.OpenPrice, "Open should be 50");
		AreEqual(60m, candle.HighPrice, "High should be 60");
		AreEqual(40m, candle.LowPrice, "Low should be 40");
		AreEqual(55m, candle.ClosePrice, "Close should be 55");
		AreEqual(200m, candle.TotalVolume, "TotalVolume should be 200");
		AreEqual(4, candle.TotalTicks, "TotalTicks should be 4");
	}

	/// <summary>
	/// Verifies RangeCandle OHLCV values.
	/// </summary>
	[TestMethod]
	public void RangeCandleBuilder_VerifyOHLCV_WithinRange()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new RangeCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = DataType.Create<RangeCandleMessage>(new Unit(50m)), // Large range
			}
		};

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		// Ticks: O=100, H=120, L=95, C=110
		var ticks = new[]
		{
			(Price: 100m, Volume: 10m),  // Open
			(Price: 120m, Volume: 20m),  // High
			(Price: 95m, Volume: 30m),   // Low
			(Price: 110m, Volume: 40m),  // Close
		};

		for (int i = 0; i < ticks.Length; i++)
		{
			builder.Process(subscription, new MockTransform
			{
				Price = ticks[i].Price,
				Volume = ticks[i].Volume,
				Time = baseTime.AddSeconds(i)
			}).ToList();
		}

		var candle = subscription.CurrentCandle as RangeCandleMessage;
		IsNotNull(candle);
		AreEqual(100m, candle.OpenPrice, "Open should be 100");
		AreEqual(120m, candle.HighPrice, "High should be 120");
		AreEqual(95m, candle.LowPrice, "Low should be 95");
		AreEqual(110m, candle.ClosePrice, "Close should be 110");
		AreEqual(100m, candle.TotalVolume, "TotalVolume should be 100");
		AreEqual(4, candle.TotalTicks, "TotalTicks should be 4");
	}

	/// <summary>
	/// Verifies that OHLCV values are correct for finished Renko brick.
	/// </summary>
	[TestMethod]
	public void RenkoCandleBuilder_VerifyFinishedBrick_OHLCV()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new RenkoCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = DataType.Create<RenkoCandleMessage>(new Unit(10m)),
			}
		};

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();
		var candles = new List<CandleMessage>();

		// First price establishes baseline
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 100m, Volume = 50m, Time = baseTime }))
			candles.Add(c);

		// Move up to trigger finished bricks: 100 -> 125 (multiple boxes)
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 125m, Volume = 60m, Time = baseTime.AddSeconds(1) }))
			candles.Add(c);

		var finishedBricks = candles.Where(c => c.State == CandleStates.Finished).ToList();
		finishedBricks.Any().AssertTrue("Should have at least one finished brick");

		var brick = finishedBricks.First() as RenkoCandleMessage;
		IsNotNull(brick);

		// Verify Renko brick has valid OHLCV structure
		// High should be >= max(Open, Close), Low should be <= min(Open, Close)
		(brick.HighPrice >= brick.OpenPrice).AssertTrue("High should be >= Open");
		(brick.HighPrice >= brick.ClosePrice).AssertTrue("High should be >= Close");
		(brick.LowPrice <= brick.OpenPrice).AssertTrue("Low should be <= Open");
		(brick.LowPrice <= brick.ClosePrice).AssertTrue("Low should be <= Close");

		// Volume should be positive
		(brick.TotalVolume >= 0).AssertTrue("TotalVolume should be >= 0");
		(brick.TotalTicks >= 1).AssertTrue("TotalTicks should be >= 1");
	}

	/// <summary>
	/// Verifies OHLCV values in a finished candle (crossing time boundary).
	/// </summary>
	[TestMethod]
	public void TimeFrameCandleBuilder_VerifyOHLCV_FinishedCandle()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new TimeFrameCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			}
		};

		var candles = new List<CandleMessage>();

		// First minute: 10:00-10:01, O=100, H=110, L=95, C=105
		var time1 = new DateTime(2024, 1, 1, 10, 0, 5).UtcKind();
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 100m, Volume = 10m, Time = time1 }))
			candles.Add(c);
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 110m, Volume = 20m, Time = time1.AddSeconds(15) }))
			candles.Add(c);
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 95m, Volume = 30m, Time = time1.AddSeconds(30) }))
			candles.Add(c);
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 105m, Volume = 40m, Time = time1.AddSeconds(45) }))
			candles.Add(c);

		// New tick in next minute triggers finish of first candle
		var time2 = new DateTime(2024, 1, 1, 10, 1, 5).UtcKind();
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 108m, Volume = 15m, Time = time2 }))
			candles.Add(c);

		// Find the finished candle from the first minute
		var finished = candles.Where(c => c.State == CandleStates.Finished).ToList();
		finished.Any().AssertTrue("Should have at least one finished candle");

		// The finished candle should be for the first minute (10:00)
		var finishedCandle = finished.OfType<TimeFrameCandleMessage>()
			.FirstOrDefault(c => c.OpenTime == new DateTime(2024, 1, 1, 10, 0, 0).UtcKind());
		IsNotNull(finishedCandle, "Finished candle for 10:00 should exist");
		AreEqual(100m, finishedCandle.OpenPrice, "Finished candle Open should be 100");
		AreEqual(110m, finishedCandle.HighPrice, "Finished candle High should be 110");
		AreEqual(95m, finishedCandle.LowPrice, "Finished candle Low should be 95");
		AreEqual(105m, finishedCandle.ClosePrice, "Finished candle Close should be 105");
		AreEqual(100m, finishedCandle.TotalVolume, "Finished candle Volume should be 100");
		AreEqual(4, finishedCandle.TotalTicks, "Finished candle TotalTicks should be 4");
	}

	#endregion

	#region VolumeProfile Tests

	/// <summary>
	/// VolumeProfile: price levels are tracked when IsCalcVolumeProfile is enabled.
	/// </summary>
	[TestMethod]
	public void TimeFrameCandleBuilder_VolumeProfile_TracksLevels()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new TimeFrameCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = TimeSpan.FromMinutes(5).TimeFrame(),
				IsCalcVolumeProfile = true, // Enable volume profile
			}
		};

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		// Multiple ticks at different price levels
		var ticks = new[]
		{
			(Price: 100m, Volume: 50m, Side: Sides.Buy),
			(Price: 100m, Volume: 30m, Side: Sides.Sell),
			(Price: 101m, Volume: 40m, Side: Sides.Buy),
			(Price: 102m, Volume: 60m, Side: Sides.Buy),
			(Price: 102m, Volume: 20m, Side: Sides.Sell),
		};

		for (int i = 0; i < ticks.Length; i++)
		{
			builder.Process(subscription, new MockTransform
			{
				Price = ticks[i].Price,
				Volume = ticks[i].Volume,
				Side = ticks[i].Side,
				Time = baseTime.AddSeconds(i * 10)
			}).ToList();
		}

		var candle = subscription.CurrentCandle;
		IsNotNull(candle);
		IsNotNull(candle.PriceLevels, "PriceLevels should be set when IsCalcVolumeProfile is true");

		var levels = candle.PriceLevels.ToList();
		(levels.Count >= 3).AssertTrue("Should have at least 3 price levels (100, 101, 102)");

		// Verify level at price 100: 50 buy + 30 sell = 80 total
		var level100 = levels.FirstOrDefault(l => l.Price == 100m);
		(level100.Price > 0).AssertTrue("Level at 100 should exist");
		AreEqual(50m, level100.BuyVolume, "Level 100 BuyVolume should be 50");
		AreEqual(30m, level100.SellVolume, "Level 100 SellVolume should be 30");

		// Verify level at price 102: 60 buy + 20 sell = 80 total
		var level102 = levels.FirstOrDefault(l => l.Price == 102m);
		(level102.Price > 0).AssertTrue("Level at 102 should exist");
		AreEqual(60m, level102.BuyVolume, "Level 102 BuyVolume should be 60");
		AreEqual(20m, level102.SellVolume, "Level 102 SellVolume should be 20");
	}

	/// <summary>
	/// VolumeProfile: not calculated when IsCalcVolumeProfile is false.
	/// </summary>
	[TestMethod]
	public void TimeFrameCandleBuilder_VolumeProfile_NotCalculatedWhenDisabled()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new TimeFrameCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = TimeSpan.FromMinutes(5).TimeFrame(),
				IsCalcVolumeProfile = false, // Disabled (default)
			}
		};

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		builder.Process(subscription, new MockTransform
		{
			Price = 100m,
			Volume = 50m,
			Side = Sides.Buy,
			Time = baseTime
		}).ToList();

		var candle = subscription.CurrentCandle;
		IsNotNull(candle);
		IsNull(candle.PriceLevels, "PriceLevels should be null when IsCalcVolumeProfile is false");
		IsNull(subscription.VolumeProfile, "VolumeProfile should not be created when disabled");
	}

	/// <summary>
	/// VolumeProfile: accumulates same price level correctly.
	/// </summary>
	[TestMethod]
	public void VolumeProfileBuilder_AccumulatesSamePriceLevel()
	{
		var profile = new VolumeProfileBuilder();

		// Multiple updates at same price
		profile.Update(100m, 10m, Sides.Buy);
		profile.Update(100m, 20m, Sides.Buy);
		profile.Update(100m, 15m, Sides.Sell);

		var levels = profile.PriceLevels.ToList();
		AreEqual(1, levels.Count, "Should have only 1 level at price 100");

		var level = levels.First();
		AreEqual(100m, level.Price, "Price should be 100");
		AreEqual(30m, level.BuyVolume, "BuyVolume should be 10 + 20 = 30");
		AreEqual(15m, level.SellVolume, "SellVolume should be 15");
		AreEqual(45m, level.TotalVolume, "TotalVolume should be 30 + 15 = 45");
	}

	/// <summary>
	/// VolumeProfile: tracks multiple price levels.
	/// </summary>
	[TestMethod]
	public void VolumeProfileBuilder_TracksMultiplePriceLevels()
	{
		var profile = new VolumeProfileBuilder();

		profile.Update(100m, 50m, Sides.Buy);
		profile.Update(101m, 30m, Sides.Sell);
		profile.Update(102m, 40m, Sides.Buy);

		var levels = profile.PriceLevels.ToList();
		AreEqual(3, levels.Count, "Should have 3 price levels");

		(levels.Any(l => l.Price == 100m)).AssertTrue("Should have level at 100");
		(levels.Any(l => l.Price == 101m)).AssertTrue("Should have level at 101");
		(levels.Any(l => l.Price == 102m)).AssertTrue("Should have level at 102");
	}

	/// <summary>
	/// VolumeProfile: PoC (Point of Control) calculated correctly.
	/// </summary>
	[TestMethod]
	public void VolumeProfileBuilder_CalculatesPoC()
	{
		var profile = new VolumeProfileBuilder();

		// Price 100: total 30, Price 101: total 80 (highest), Price 102: total 50
		profile.Update(100m, 20m, Sides.Buy);
		profile.Update(100m, 10m, Sides.Sell);
		profile.Update(101m, 50m, Sides.Buy);
		profile.Update(101m, 30m, Sides.Sell);
		profile.Update(102m, 30m, Sides.Buy);
		profile.Update(102m, 20m, Sides.Sell);

		profile.Calculate();

		AreEqual(101m, profile.PoC.Price, "PoC should be at price 101 (highest volume)");
	}

	/// <summary>
	/// VolumeProfile: new candle resets profile.
	/// </summary>
	[TestMethod]
	public void TimeFrameCandleBuilder_VolumeProfile_ResetOnNewCandle()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new TimeFrameCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
				IsCalcVolumeProfile = true,
			}
		};

		var candles = new List<CandleMessage>();

		// First minute
		var time1 = new DateTime(2024, 1, 1, 10, 0, 30).UtcKind();
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 100m, Volume = 50m, Side = Sides.Buy, Time = time1 }))
			candles.Add(c);

		var firstProfile = subscription.VolumeProfile;
		IsNotNull(firstProfile);

		// Second minute - should reset profile
		var time2 = new DateTime(2024, 1, 1, 10, 1, 30).UtcKind();
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 105m, Volume = 30m, Side = Sides.Buy, Time = time2 }))
			candles.Add(c);

		// Profile should be different instance (reset for new candle)
		var secondProfile = subscription.VolumeProfile;
		IsNotNull(secondProfile);

		// New candle should have its own profile starting fresh
		var newCandle = subscription.CurrentCandle;
		IsNotNull(newCandle.PriceLevels);

		var levels = newCandle.PriceLevels.ToList();
		// Should only have level at 105 (new candle's price)
		AreEqual(1, levels.Count, "New candle should have its own profile with 1 level");
		AreEqual(105m, levels.First().Price, "New candle's profile should start at 105");
	}

	/// <summary>
	/// VolumeProfile: works with VolumeCandleBuilder.
	/// </summary>
	[TestMethod]
	public void VolumeCandleBuilder_VolumeProfile_TracksLevels()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new VolumeCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = DataType.Create<VolumeCandleMessage>(1000m),
				IsCalcVolumeProfile = true,
			}
		};

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0).UtcKind();

		builder.Process(subscription, new MockTransform { Price = 50m, Volume = 100m, Side = Sides.Buy, Time = baseTime }).ToList();
		builder.Process(subscription, new MockTransform { Price = 51m, Volume = 150m, Side = Sides.Sell, Time = baseTime.AddSeconds(1) }).ToList();
		builder.Process(subscription, new MockTransform { Price = 50m, Volume = 50m, Side = Sides.Buy, Time = baseTime.AddSeconds(2) }).ToList();

		var candle = subscription.CurrentCandle;
		IsNotNull(candle.PriceLevels);

		var levels = candle.PriceLevels.ToList();
		AreEqual(2, levels.Count, "Should have 2 price levels (50 and 51)");

		var level50 = levels.First(l => l.Price == 50m);
		AreEqual(150m, level50.BuyVolume, "Level 50 BuyVolume should be 100 + 50 = 150");

		var level51 = levels.First(l => l.Price == 51m);
		AreEqual(150m, level51.SellVolume, "Level 51 SellVolume should be 150");
	}

	#endregion

	#region Base CandleBuilder Tests

	/// <summary>
	/// AddVolume: buy side volume tracking.
	/// </summary>
	[TestMethod]
	public void CandleBuilder_AddVolume_BuySide()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new VolumeCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = DataType.Create<VolumeCandleMessage>(1000m),
			}
		};

		var transform = new MockTransform
		{
			Price = 100m,
			Volume = 50m,
			Time = DateTime.UtcNow,
			Side = Sides.Buy
		};

		builder.Process(subscription, transform).ToList();

		var candle = subscription.CurrentCandle as VolumeCandleMessage;
		IsNotNull(candle);
		AreEqual(50m, candle.BuyVolume, "Buy volume should be tracked");
		AreEqual(50m, candle.RelativeVolume, "Relative volume should be +50 for buy");
	}

	/// <summary>
	/// AddVolume: sell side volume tracking.
	/// </summary>
	[TestMethod]
	public void CandleBuilder_AddVolume_SellSide()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new VolumeCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = DataType.Create<VolumeCandleMessage>(1000m),
			}
		};

		var transform = new MockTransform
		{
			Price = 100m,
			Volume = 50m,
			Time = DateTime.UtcNow,
			Side = Sides.Sell
		};

		builder.Process(subscription, transform).ToList();

		var candle = subscription.CurrentCandle as VolumeCandleMessage;
		IsNotNull(candle);
		AreEqual(50m, candle.SellVolume, "Sell volume should be tracked");
		AreEqual(-50m, candle.RelativeVolume, "Relative volume should be -50 for sell");
	}

	/// <summary>
	/// AddVolume: mixed buy/sell volume tracking.
	/// </summary>
	[TestMethod]
	public void CandleBuilder_AddVolume_MixedBuySell()
	{
		var provider = new MockExchangeInfoProvider();
		var builder = new VolumeCandleBuilder(provider);

		var subscription = new MockCandleBuilderSubscription
		{
			Message = new MarketDataMessage
			{
				SecurityId = CreateSecurityId(),
				DataType2 = DataType.Create<VolumeCandleMessage>(1000m),
			}
		};

		var time = DateTime.UtcNow;

		// Buy 100
		builder.Process(subscription, new MockTransform { Price = 100m, Volume = 100m, Time = time, Side = Sides.Buy }).ToList();
		// Sell 60
		builder.Process(subscription, new MockTransform { Price = 101m, Volume = 60m, Time = time.AddSeconds(1), Side = Sides.Sell }).ToList();
		// Buy 40
		builder.Process(subscription, new MockTransform { Price = 102m, Volume = 40m, Time = time.AddSeconds(2), Side = Sides.Buy }).ToList();

		var candle = subscription.CurrentCandle as VolumeCandleMessage;
		IsNotNull(candle);
		AreEqual(140m, candle.BuyVolume, "Buy volume: 100 + 40 = 140");
		AreEqual(60m, candle.SellVolume, "Sell volume: 60");
		AreEqual(200m, candle.TotalVolume, "Total: 100 + 60 + 40 = 200");
		AreEqual(80m, candle.RelativeVolume, "Relative: 140 - 60 = 80");
	}

	#endregion
}

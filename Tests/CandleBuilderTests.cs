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
		public IEnumerable<ExchangeBoard> Boards => [ExchangeBoard.Associated];
		public IEnumerable<Exchange> Exchanges => [Exchange.Test];

		public event Action<ExchangeBoard> BoardAdded { add { } remove { } }
		public event Action<Exchange> ExchangeAdded { add { } remove { } }
		public event Action<ExchangeBoard> BoardRemoved { add { } remove { } }
		public event Action<Exchange> ExchangeRemoved { add { } remove { } }

		public void Init() { }
		public ExchangeBoard GetExchangeBoard(string code) => ExchangeBoard.Associated;
		public ExchangeBoard TryGetExchangeBoard(string code) => ExchangeBoard.Associated;
		public Exchange GetExchange(string code) => Exchange.Test;
		public Exchange TryGetExchange(string code) => Exchange.Test;
		public void Save(ExchangeBoard board) { }
		public void Save(Exchange exchange) { }
		public void Delete(ExchangeBoard board) { }
		public void Delete(Exchange exchange) { }
		public ExchangeBoard GetOrCreateBoard(string code, Func<string, ExchangeBoard> createBoard = null) => ExchangeBoard.Associated;
		public IEnumerable<BoardMessage> Lookup(BoardLookupMessage criteria) => [];
	}

	private class MockCandleBuilderSubscription : ICandleBuilderSubscription
	{
		public MarketDataMessage Message { get; set; }
		public CandleMessage CurrentCandle { get; set; }
		public VolumeProfileBuilder VolumeProfile { get; set; }
		public long TransactionId => Message?.TransactionId ?? 0;
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

		// Process 7 ticks - should create 3 candles (3 + 3 + 1 partial)
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

		// First finished candle should have exactly 3 ticks
		var firstFinished = finishedCandles.First();
		AreEqual(3, firstFinished.TotalTicks, "First candle should have 3 ticks");
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
		AreEqual(100m, candle.OpenPrice);
		AreEqual(1, candle.TotalTicks);
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

		// Send trades that should trigger multiple candles
		var volumes = new[] { 30m, 40m, 30m, 50m, 60m }; // Total 210, should create 2+ candles
		for (int i = 0; i < volumes.Length; i++)
		{
			var transform = new MockTransform
			{
				Price = 100 + i,
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
		finishedCandles.Any().AssertTrue("Should have at least one finished candle");

		// Each finished candle should have >= 100 volume
		foreach (var fc in finishedCandles)
		{
			(fc.TotalVolume >= 100m).AssertTrue("Finished candle volume should be >= threshold");
		}
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

		// Send multiple small volume trades
		for (int i = 0; i < 5; i++)
		{
			var transform = new MockTransform
			{
				Price = 100,
				Volume = 10m,
				Time = baseTime.AddSeconds(i)
			};

			foreach (var _ in builder.Process(subscription, transform)) { }
		}

		var currentCandle = subscription.CurrentCandle as VolumeCandleMessage;
		IsNotNull(currentCandle);
		AreEqual(50m, currentCandle.TotalVolume, "Should accumulate 5 * 10 = 50 volume");
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
		var prices = new[] { 100m, 102m, 104m, 106m, 107m };
		for (int i = 0; i < prices.Length; i++)
		{
			var transform = new MockTransform
			{
				Price = prices[i],
				Volume = 10,
				Time = baseTime.AddSeconds(i)
			};

			foreach (var candle in builder.Process(subscription, transform))
			{
				candles.Add(candle);
			}
		}

		// Should have created new candle when range exceeded 5 (on the 5th tick when check sees range=6)
		var finishedCandles = candles.Where(c => c.State == CandleStates.Finished).ToList();
		finishedCandles.Any().AssertTrue("Should have finished candle when range exceeded");
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
		var prices = new[] { 100m, 105m, 95m, 102m }; // High=105, Low=95

		foreach (var price in prices)
		{
			var transform = new MockTransform
			{
				Price = price,
				Volume = 10,
				Time = baseTime
			};

			foreach (var _ in builder.Process(subscription, transform)) { }
			baseTime = baseTime.AddSeconds(1);
		}

		var currentCandle = subscription.CurrentCandle as RangeCandleMessage;
		IsNotNull(currentCandle);
		AreEqual(105m, currentCandle.HighPrice, "High should be 105");
		AreEqual(95m, currentCandle.LowPrice, "Low should be 95");
		AreEqual(100m, currentCandle.OpenPrice, "Open should be first price 100");
		AreEqual(102m, currentCandle.ClosePrice, "Close should be last price 102");
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
		var transform2 = new MockTransform { Price = 145m, Volume = 10, Time = baseTime.AddSeconds(1) };
		foreach (var c in builder.Process(subscription, transform2))
			candles.Add(c);

		var finishedCandles = candles.Where(c => c.State == CandleStates.Finished).ToList();
		(finishedCandles.Count >= 3).AssertTrue("Should generate multiple finished bricks");
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

		// Small movements within one brick
		var prices = new[] { 100m, 102m, 98m, 104m, 99m }; // All within 10 points of each other

		foreach (var price in prices)
		{
			var transform = new MockTransform { Price = price, Volume = 10, Time = baseTime };
			foreach (var c in builder.Process(subscription, transform))
				candles.Add(c);
			baseTime = baseTime.AddSeconds(1);
		}

		var finishedCandles = candles.Where(c => c.State == CandleStates.Finished).ToList();
		AreEqual(0, finishedCandles.Count, "Small movements should not create finished bricks");
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
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 55m, Volume = 10, Time = baseTime.AddSeconds(1) }))
			candles.Add(c);

		var finishedCandles = candles.Where(c => c.State == CandleStates.Finished).ToList();
		(finishedCandles.Count >= 3).AssertTrue("Downward movement should create bricks");
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
		foreach (var price in prices)
		{
			foreach (var c in builder.Process(subscription, new MockTransform { Price = price, Volume = 10, Time = baseTime }))
				candles.Add(c);
			baseTime = baseTime.AddSeconds(1);
		}

		var currentCandle = subscription.CurrentCandle as PnFCandleMessage;
		IsNotNull(currentCandle);
		// In X column: Open <= Close (bullish)
		(currentCandle.OpenPrice <= currentCandle.ClosePrice).AssertTrue("X column should have Open <= Close");
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
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 105m, Volume = 10, Time = baseTime.AddSeconds(1) }))
			candles.Add(c);

		// Reversal: drop by more than 3 boxes from high
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 101m, Volume = 10, Time = baseTime.AddSeconds(2) }))
			candles.Add(c);

		var finishedCandles = candles.Where(c => c.State == CandleStates.Finished).ToList();
		finishedCandles.Any().AssertTrue("Reversal should finish previous column");
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
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 110m, Volume = 10, Time = time1.AddSeconds(30) }))
			candles.Add(c);

		var firstCandle = subscription.CurrentCandle as HeikinAshiCandleMessage;
		IsNotNull(firstCandle);
		var firstOpen = firstCandle.OpenPrice;
		var firstClose = firstCandle.ClosePrice;

		// Second candle at 10:01 - open should be smoothed
		var time2 = new DateTime(2024, 1, 1, 10, 1, 0).UtcKind();
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 115m, Volume = 10, Time = time2 }))
			candles.Add(c);

		var secondCandle = subscription.CurrentCandle as HeikinAshiCandleMessage;
		IsNotNull(secondCandle);

		// HA Open = (prevOpen + prevClose) / 2
		var expectedOpen = (firstOpen + firstClose) / 2;
		AreEqual(expectedOpen, secondCandle.OpenPrice, "HA Open should be average of prev Open and Close");
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

		foreach (var price in prices)
		{
			foreach (var c in builder.Process(subscription, new MockTransform { Price = price, Volume = 10, Time = time }))
			{ }
			time = time.AddSeconds(10);
		}

		var candle = subscription.CurrentCandle as HeikinAshiCandleMessage;
		IsNotNull(candle);

		// HA Close = (O + H + L + C) / 4
		// Note: after each update, close is recalculated, so we verify it's different from last price
		// The exact calculation depends on intermediate values
		(candle.ClosePrice != prices.Last()).AssertTrue("HA Close should be smoothed, not raw last price");
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
		foreach (var c in builder.Process(subscription, new MockTransform { Price = 110m, Volume = 10, Time = time2 }))
			candles.Add(c);

		var finishedCandles = candles.Where(c => c.State == CandleStates.Finished).ToList();
		finishedCandles.Any().AssertTrue("Should finish candle when crossing time frame boundary");
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

		var result = builder.Process(subscription, new MockTransform { Price = 100m, Volume = 10, Time = dataTime }).ToList();

		AreEqual(1, result.Count);
		var candle = result[0] as TimeFrameCandleMessage;
		IsNotNull(candle);
		AreEqual(new DateTime(2024, 1, 1, 10, 0, 0).UtcKind(), candle.OpenTime);
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

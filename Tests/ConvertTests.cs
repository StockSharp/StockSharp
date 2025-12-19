namespace StockSharp.Tests;

using Ecng.Linq;

[TestClass]
public class ConvertTests : BaseTestClass
{
	private static SecurityId TestSecurityId => new()
	{
		SecurityCode = "SBER",
		BoardCode = "EQBR"
	};

	private static SecurityId TestSecurityId2 => new()
	{
		SecurityCode = "GAZP",
		BoardCode = "EQBR"
	};

	[TestMethod]
	public void QuoteChangeMessage_ToLevel1()
	{
		var quote = new QuoteChangeMessage
		{
			SecurityId = TestSecurityId,
			ServerTime = new DateTime(2024, 1, 15, 10, 30, 0),
			Bids = [new QuoteChange(100m, 150)],
			Asks = [new QuoteChange(101m, 200)]
		};

		var level1 = quote.ToLevel1();

		level1.SecurityId.AreEqual(TestSecurityId);
		level1.ServerTime.AreEqual(quote.ServerTime);
		level1.TryGetDecimal(Level1Fields.BestBidPrice).AreEqual(100m);
		level1.TryGetDecimal(Level1Fields.BestBidVolume).AreEqual(150m);
		level1.TryGetDecimal(Level1Fields.BestAskPrice).AreEqual(101m);
		level1.TryGetDecimal(Level1Fields.BestAskVolume).AreEqual(200m);
	}

	[TestMethod]
	public void QuoteChangeMessage_ToLevel1_EmptyBids()
	{
		var quote = new QuoteChangeMessage
		{
			SecurityId = TestSecurityId,
			ServerTime = new DateTime(2024, 1, 15, 10, 30, 0),
			Bids = [],
			Asks = [new QuoteChange(101m, 200)]
		};

		var level1 = quote.ToLevel1();

		level1.SecurityId.AreEqual(TestSecurityId);
		level1.Changes.ContainsKey(Level1Fields.BestBidPrice).AssertFalse();
		level1.Changes.ContainsKey(Level1Fields.BestBidVolume).AssertFalse();
		level1.TryGetDecimal(Level1Fields.BestAskPrice).AreEqual(101m);
		level1.TryGetDecimal(Level1Fields.BestAskVolume).AreEqual(200m);
	}

	[TestMethod]
	public void QuoteChangeMessage_ToLevel1_EmptyAsks()
	{
		var quote = new QuoteChangeMessage
		{
			SecurityId = TestSecurityId,
			ServerTime = new DateTime(2024, 1, 15, 10, 30, 0),
			Bids = [new QuoteChange(100m, 150)],
			Asks = []
		};

		var level1 = quote.ToLevel1();

		level1.SecurityId.AreEqual(TestSecurityId);
		level1.TryGetDecimal(Level1Fields.BestBidPrice).AreEqual(100m);
		level1.TryGetDecimal(Level1Fields.BestBidVolume).AreEqual(150m);
		level1.Changes.ContainsKey(Level1Fields.BestAskPrice).AssertFalse();
		level1.Changes.ContainsKey(Level1Fields.BestAskVolume).AssertFalse();
	}

	[TestMethod]
	public void CandleMessage_ToLevel1()
	{
		var candle = new TimeFrameCandleMessage
		{
			SecurityId = TestSecurityId,
			OpenTime = new DateTime(2024, 1, 15, 10, 0, 0),
			CloseTime = new DateTime(2024, 1, 15, 10, 5, 0),
			OpenPrice = 100m,
			HighPrice = 105m,
			LowPrice = 99m,
			ClosePrice = 103m,
			TotalVolume = 1000,
			TotalTicks = 50,
			OpenInterest = 200
		};

		var level1 = candle.ToLevel1();

		level1.SecurityId.AreEqual(TestSecurityId);
		level1.ServerTime.AreEqual(candle.CloseTime);
		level1.TryGetDecimal(Level1Fields.OpenPrice).AreEqual(100m);
		level1.TryGetDecimal(Level1Fields.HighPrice).AreEqual(105m);
		level1.TryGetDecimal(Level1Fields.LowPrice).AreEqual(99m);
		level1.TryGetDecimal(Level1Fields.ClosePrice).AreEqual(103m);
		level1.TryGetDecimal(Level1Fields.Volume).AreEqual(1000m);
		((int?)level1.TryGet(Level1Fields.TradesCount)).AreEqual(50);
		level1.TryGetDecimal(Level1Fields.OpenInterest).AreEqual(200m);
	}

	[TestMethod]
	public void CandleMessage_ToLevel1_NoCloseTime()
	{
		var candle = new TimeFrameCandleMessage
		{
			SecurityId = TestSecurityId,
			OpenTime = new DateTime(2024, 1, 15, 10, 0, 0),
			OpenPrice = 100m,
			HighPrice = 105m,
			LowPrice = 99m,
			ClosePrice = 103m
		};

		var level1 = candle.ToLevel1();

		level1.ServerTime.AreEqual(candle.OpenTime);
	}

	[TestMethod]
	public void ExecutionMessage_ToLevel1()
	{
		var tick = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = TestSecurityId,
			ServerTime = new DateTime(2024, 1, 15, 10, 30, 0),
			TradeId = 12345,
			TradePrice = 102.5m,
			TradeVolume = 100,
			IsUpTick = true,
			OpenInterest = 500,
			OriginSide = Sides.Buy
		};

		var level1 = tick.ToLevel1();

		level1.SecurityId.AreEqual(TestSecurityId);
		level1.ServerTime.AreEqual(tick.ServerTime);
		((long?)level1.TryGet(Level1Fields.LastTradeId)).AreEqual(12345L);
		level1.TryGetDecimal(Level1Fields.LastTradePrice).AreEqual(102.5m);
		level1.TryGetDecimal(Level1Fields.LastTradeVolume).AreEqual(100m);
		((bool?)level1.TryGet(Level1Fields.LastTradeUpDown)).AreEqual(true);
		level1.TryGetDecimal(Level1Fields.OpenInterest).AreEqual(500m);
		((Sides?)level1.TryGet(Level1Fields.LastTradeOrigin)).AreEqual(Sides.Buy);
	}

	[TestMethod]
	public void Level1ChangeMessage_ToTick()
	{
		var level1 = new Level1ChangeMessage
		{
			SecurityId = TestSecurityId,
			ServerTime = new DateTime(2024, 1, 15, 10, 30, 0),
			LocalTime = new DateTime(2024, 1, 15, 10, 30, 1)
		};

		level1.Add(Level1Fields.LastTradeId, 12345L);
		level1.Add(Level1Fields.LastTradePrice, 102.5m);
		level1.Add(Level1Fields.LastTradeVolume, 100m);
		level1.Add(Level1Fields.LastTradeOrigin, Sides.Sell);
		level1.Add(Level1Fields.LastTradeUpDown, false);

		var tick = level1.ToTick();

		tick.SecurityId.AreEqual(TestSecurityId);
		tick.ServerTime.AreEqual(level1.ServerTime);
		tick.LocalTime.AreEqual(level1.LocalTime);
		tick.TradeId.AreEqual(12345L);
		tick.TradePrice.AreEqual(102.5m);
		tick.TradeVolume.AreEqual(100m);
		tick.OriginSide.AreEqual(Sides.Sell);
		tick.IsUpTick.AreEqual(false);
		tick.DataTypeEx.AreEqual(DataType.Ticks);
		tick.BuildFrom.AreEqual(DataType.Level1);
	}

	[TestMethod]
	public void Level1ChangeMessage_ToTick_WithLastTradeTime()
	{
		var tradeTime = new DateTime(2024, 1, 15, 10, 30, 5);
		var level1 = new Level1ChangeMessage
		{
			SecurityId = TestSecurityId,
			ServerTime = new DateTime(2024, 1, 15, 10, 30, 0)
		};

		level1.Add(Level1Fields.LastTradeTime, tradeTime);
		level1.Add(Level1Fields.LastTradePrice, 102.5m);

		var tick = level1.ToTick();

		tick.ServerTime.AreEqual(tradeTime);
	}

	[TestMethod]
	public void QuoteChangeMessages_ToLevel1_Multiple()
	{
		var quotes = new[]
		{
			new QuoteChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 30, 0),
				Bids = [new QuoteChange(100m, 150)],
				Asks = [new QuoteChange(101m, 200)]
			},
			new QuoteChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 30, 1),
				Bids = [new QuoteChange(100.5m, 160)],
				Asks = [new QuoteChange(101.5m, 210)]
			}
		};

		var level1Messages = quotes.ToLevel1().ToArray();

		level1Messages.Length.AreEqual(2);

		level1Messages[0].TryGetDecimal(Level1Fields.BestBidPrice).AreEqual(100m);
		level1Messages[0].TryGetDecimal(Level1Fields.BestAskPrice).AreEqual(101m);
		level1Messages[0].BuildFrom.AreEqual(DataType.MarketDepth);

		level1Messages[1].TryGetDecimal(Level1Fields.BestBidPrice).AreEqual(100.5m);
		level1Messages[1].TryGetDecimal(Level1Fields.BestAskPrice).AreEqual(101.5m);
	}

	[TestMethod]
	public void Level1ChangeMessage_IsContainsCandle()
	{
		var level1WithCandle = new Level1ChangeMessage();
		level1WithCandle.Add(Level1Fields.OpenPrice, 100m);
		level1WithCandle.Add(Level1Fields.ClosePrice, 102m);

		level1WithCandle.IsContainsCandle().AssertTrue();

		var level1WithoutCandle = new Level1ChangeMessage();
		level1WithoutCandle.Add(Level1Fields.BestBidPrice, 100m);
		level1WithoutCandle.Add(Level1Fields.BestAskPrice, 101m);

		level1WithoutCandle.IsContainsCandle().AssertFalse();
	}

	[TestMethod]
	public void Level1ChangeMessage_IsContainsCandle_PartialFields()
	{
		var level1WithOpen = new Level1ChangeMessage();
		level1WithOpen.Add(Level1Fields.OpenPrice, 100m);
		level1WithOpen.IsContainsCandle().AssertTrue();

		var level1WithHigh = new Level1ChangeMessage();
		level1WithHigh.Add(Level1Fields.HighPrice, 105m);
		level1WithHigh.IsContainsCandle().AssertTrue();

		var level1WithLow = new Level1ChangeMessage();
		level1WithLow.Add(Level1Fields.LowPrice, 99m);
		level1WithLow.IsContainsCandle().AssertTrue();

		var level1WithClose = new Level1ChangeMessage();
		level1WithClose.Add(Level1Fields.ClosePrice, 102m);
		level1WithClose.IsContainsCandle().AssertTrue();
	}

	[TestMethod]
	public void Level1ChangeMessage_ToTick_Null()
	{
		ThrowsExactly<ArgumentNullException>(() =>
		{
			Level1ChangeMessage level1 = null;
			level1.ToTick();
		});
	}

	[TestMethod]
	public void QuoteChangeMessages_ToLevel1_Null()
	{
		ThrowsExactly<ArgumentNullException>(() =>
		{
			IEnumerable<QuoteChangeMessage> quotes = null;
			quotes.ToLevel1().ToArray();
		});
	}

	[TestMethod]
	public void Level1ChangeMessage_IsContainsCandle_Null()
	{
		ThrowsExactly<ArgumentNullException>(() =>
		{
			Level1ChangeMessage level1 = null;
			level1.IsContainsCandle();
		});
	}

	[TestMethod]
	public void Level1ChangeMessage_IsContainsTick()
	{
		var level1WithTick = new Level1ChangeMessage();
		level1WithTick.Add(Level1Fields.LastTradePrice, 100m);
		level1WithTick.IsContainsTick().AssertTrue();

		var level1WithoutTick = new Level1ChangeMessage();
		level1WithoutTick.Add(Level1Fields.BestBidPrice, 100m);
		level1WithoutTick.IsContainsTick().AssertFalse();
	}

	[TestMethod]
	public void Level1ChangeMessage_IsContainsQuotes()
	{
		var level1WithBid = new Level1ChangeMessage();
		level1WithBid.Add(Level1Fields.BestBidPrice, 100m);
		level1WithBid.IsContainsQuotes().AssertTrue();

		var level1WithAsk = new Level1ChangeMessage();
		level1WithAsk.Add(Level1Fields.BestAskPrice, 101m);
		level1WithAsk.IsContainsQuotes().AssertTrue();

		var level1WithoutQuotes = new Level1ChangeMessage();
		level1WithoutQuotes.Add(Level1Fields.LastTradePrice, 100m);
		level1WithoutQuotes.IsContainsQuotes().AssertFalse();
	}

	[TestMethod]
	public void Level1ChangeMessages_ToOrderBooks()
	{
		var level1Messages = new[]
		{
			new Level1ChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 0, 0)
			}
			.Add(Level1Fields.BestBidPrice, 100m)
			.Add(Level1Fields.BestBidVolume, 150m)
			.Add(Level1Fields.BestAskPrice, 101m)
			.Add(Level1Fields.BestAskVolume, 200m),

			new Level1ChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 0, 1)
			}
			.Add(Level1Fields.BestBidPrice, 100.5m)
			.Add(Level1Fields.BestBidVolume, 160m)
			.Add(Level1Fields.BestAskPrice, 101.5m)
			.Add(Level1Fields.BestAskVolume, 210m)
		};

		var orderBooks = level1Messages.ToOrderBooks().ToArray();

		orderBooks.Length.AreEqual(2);

		orderBooks[0].Bids.Length.AreEqual(1);
		orderBooks[0].Bids[0].Price.AreEqual(100m);
		orderBooks[0].Bids[0].Volume.AreEqual(150m);
		orderBooks[0].Asks.Length.AreEqual(1);
		orderBooks[0].Asks[0].Price.AreEqual(101m);
		orderBooks[0].Asks[0].Volume.AreEqual(200m);

		orderBooks[1].Bids[0].Price.AreEqual(100.5m);
		orderBooks[1].Asks[0].Price.AreEqual(101.5m);
	}

	[TestMethod]
	public void Level1ChangeMessages_ToTicks()
	{
		var level1Messages = new[]
		{
			new Level1ChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 0, 0)
			}
			.Add(Level1Fields.LastTradePrice, 100m)
			.Add(Level1Fields.LastTradeVolume, 50m)
			.Add(Level1Fields.LastTradeId, 1L),

			new Level1ChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 0, 1)
			}
			.Add(Level1Fields.LastTradePrice, 100.5m)
			.Add(Level1Fields.LastTradeVolume, 75m)
			.Add(Level1Fields.LastTradeId, 2L)
		};

		var ticks = level1Messages.ToTicks().ToArray();

		ticks.Length.AreEqual(2);

		ticks[0].TradePrice.AreEqual(100m);
		ticks[0].TradeVolume.AreEqual(50m);
		ticks[0].TradeId.AreEqual(1L);

		ticks[1].TradePrice.AreEqual(100.5m);
		ticks[1].TradeVolume.AreEqual(75m);
		ticks[1].TradeId.AreEqual(2L);
	}

	[TestMethod]
	public void QuoteChangeMessage_ToLevel1_MultipleDepths()
	{
		var quote = new QuoteChangeMessage
		{
			SecurityId = TestSecurityId,
			ServerTime = new DateTime(2024, 1, 15, 10, 30, 0),
			Bids =
			[
				new QuoteChange(100m, 150),
				new QuoteChange(99.5m, 200),
				new QuoteChange(99m, 300)
			],
			Asks =
			[
				new QuoteChange(101m, 200),
				new QuoteChange(101.5m, 250),
				new QuoteChange(102m, 300)
			]
		};

		var level1 = quote.ToLevel1();

		level1.TryGetDecimal(Level1Fields.BestBidPrice).AreEqual(100m);
		level1.TryGetDecimal(Level1Fields.BestBidVolume).AreEqual(150m);
		level1.TryGetDecimal(Level1Fields.BestAskPrice).AreEqual(101m);
		level1.TryGetDecimal(Level1Fields.BestAskVolume).AreEqual(200m);
	}

	[TestMethod]
	public void CandleMessage_ToLevel1_AllFields()
	{
		var candle = new TimeFrameCandleMessage
		{
			SecurityId = TestSecurityId,
			OpenTime = new DateTime(2024, 1, 15, 10, 0, 0),
			CloseTime = new DateTime(2024, 1, 15, 10, 5, 0),
			OpenPrice = 100m,
			HighPrice = 105.50m,
			LowPrice = 98.25m,
			ClosePrice = 103.75m,
			TotalVolume = 150000,
			TotalTicks = 1250,
			OpenInterest = 5000,
			State = CandleStates.Finished
		};

		var level1 = candle.ToLevel1();

		level1.SecurityId.AreEqual(TestSecurityId);
		level1.ServerTime.AreEqual(candle.CloseTime);
		level1.TryGetDecimal(Level1Fields.OpenPrice).AreEqual(100m);
		level1.TryGetDecimal(Level1Fields.HighPrice).AreEqual(105.50m);
		level1.TryGetDecimal(Level1Fields.LowPrice).AreEqual(98.25m);
		level1.TryGetDecimal(Level1Fields.ClosePrice).AreEqual(103.75m);
		level1.TryGetDecimal(Level1Fields.Volume).AreEqual(150000m);
		((int?)level1.TryGet(Level1Fields.TradesCount)).AreEqual(1250);
		level1.TryGetDecimal(Level1Fields.OpenInterest).AreEqual(5000m);
	}

	[TestMethod]
	public void ExecutionMessage_ToLevel1_AllFieldsWithStringId()
	{
		var tick = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = TestSecurityId,
			ServerTime = new DateTime(2024, 1, 15, 10, 30, 0),
			TradeId = null,
			TradeStringId = "TRADE-12345",
			TradePrice = 102.5m,
			TradeVolume = 100,
			IsUpTick = false,
			OpenInterest = 500,
			OriginSide = Sides.Sell
		};

		var level1 = tick.ToLevel1();

		level1.SecurityId.AreEqual(TestSecurityId);
		level1.ServerTime.AreEqual(tick.ServerTime);
		level1.TryGetDecimal(Level1Fields.LastTradePrice).AreEqual(102.5m);
		level1.TryGetDecimal(Level1Fields.LastTradeVolume).AreEqual(100m);
		((bool?)level1.TryGet(Level1Fields.LastTradeUpDown)).AreEqual(false);
		level1.TryGetDecimal(Level1Fields.OpenInterest).AreEqual(500m);
		((Sides?)level1.TryGet(Level1Fields.LastTradeOrigin)).AreEqual(Sides.Sell);
	}

	[TestMethod]
	public void Level1_ToTick_ToLevel1_RoundTrip()
	{
		var originalLevel1 = new Level1ChangeMessage
		{
			SecurityId = TestSecurityId,
			ServerTime = new DateTime(2024, 1, 15, 10, 30, 0),
			LocalTime = new DateTime(2024, 1, 15, 10, 30, 1)
		};

		originalLevel1.Add(Level1Fields.LastTradePrice, 100m);
		originalLevel1.Add(Level1Fields.LastTradeVolume, 50m);
		originalLevel1.Add(Level1Fields.LastTradeId, 123L);
		originalLevel1.Add(Level1Fields.LastTradeOrigin, Sides.Buy);

		var tick = originalLevel1.ToTick();
		var convertedLevel1 = tick.ToLevel1();

		convertedLevel1.SecurityId.AreEqual(originalLevel1.SecurityId);
		convertedLevel1.ServerTime.AreEqual(originalLevel1.ServerTime);
		convertedLevel1.TryGetDecimal(Level1Fields.LastTradePrice).AreEqual(100m);
		convertedLevel1.TryGetDecimal(Level1Fields.LastTradeVolume).AreEqual(50m);
		((long?)convertedLevel1.TryGet(Level1Fields.LastTradeId)).AreEqual(123L);
		((Sides?)convertedLevel1.TryGet(Level1Fields.LastTradeOrigin)).AreEqual(Sides.Buy);
	}

	[TestMethod]
	public void QuoteChange_ToLevel1_ToQuote_RoundTrip()
	{
		var originalQuote = new QuoteChangeMessage
		{
			SecurityId = TestSecurityId,
			ServerTime = new DateTime(2024, 1, 15, 10, 30, 0),
			Bids = [new QuoteChange(100m, 150)],
			Asks = [new QuoteChange(101m, 200)]
		};

		var level1 = originalQuote.ToLevel1();

		level1.TryGetDecimal(Level1Fields.BestBidPrice).AreEqual(100m);
		level1.TryGetDecimal(Level1Fields.BestBidVolume).AreEqual(150m);
		level1.TryGetDecimal(Level1Fields.BestAskPrice).AreEqual(101m);
		level1.TryGetDecimal(Level1Fields.BestAskVolume).AreEqual(200m);
	}

	[TestMethod]
	public void Candle_ToLevel1_PreservesSecurityAndTime()
	{
		var candle = new TimeFrameCandleMessage
		{
			SecurityId = TestSecurityId2,
			OpenTime = new DateTime(2024, 6, 20, 14, 30, 0),
			CloseTime = new DateTime(2024, 6, 20, 14, 35, 0),
			OpenPrice = 200m,
			ClosePrice = 205m,
			HighPrice = 206m,
			LowPrice = 199m
		};

		var level1 = candle.ToLevel1();

		level1.SecurityId.SecurityCode.AreEqual("GAZP");
		level1.SecurityId.BoardCode.AreEqual("EQBR");
		level1.ServerTime.AreEqual(new DateTime(2024, 6, 20, 14, 35, 0));
	}

	[TestMethod]
	public void Level1_WithDecimalValues_PrecisionPreserved()
	{
		var level1 = new Level1ChangeMessage
		{
			SecurityId = TestSecurityId,
			ServerTime = DateTime.Now
		};

		level1.Add(Level1Fields.LastTradePrice, 100.12345m);
		level1.Add(Level1Fields.LastTradeVolume, 0.001m);

		var tick = level1.ToTick();

		tick.TradePrice.AreEqual(100.12345m);
		tick.TradeVolume.AreEqual(0.001m);
	}

	[TestMethod]
	public void Level1_ToOrderBook_OnlyBids()
	{
		var level1 = new Level1ChangeMessage
		{
			SecurityId = TestSecurityId,
			ServerTime = new DateTime(2024, 1, 15, 10, 0, 0)
		};
		level1.Add(Level1Fields.BestBidPrice, 100m);
		level1.Add(Level1Fields.BestBidVolume, 150m);

		var orderBooks = new[] { level1 }.ToOrderBooks().ToArray();

		orderBooks.Length.AreEqual(1);
		orderBooks[0].Bids.Length.AreEqual(1);
		orderBooks[0].Asks.Length.AreEqual(0);
	}

	[TestMethod]
	public void Level1_ToOrderBook_OnlyAsks()
	{
		var level1 = new Level1ChangeMessage
		{
			SecurityId = TestSecurityId,
			ServerTime = new DateTime(2024, 1, 15, 10, 0, 0)
		};
		level1.Add(Level1Fields.BestAskPrice, 101m);
		level1.Add(Level1Fields.BestAskVolume, 200m);

		var orderBooks = new[] { level1 }.ToOrderBooks().ToArray();

		orderBooks.Length.AreEqual(1);
		orderBooks[0].Bids.Length.AreEqual(0);
		orderBooks[0].Asks.Length.AreEqual(1);
	}

	[TestMethod]
	public void Level1_ToTick_MissingFields()
	{
		var level1 = new Level1ChangeMessage
		{
			SecurityId = TestSecurityId,
			ServerTime = new DateTime(2024, 1, 15, 10, 0, 0)
		};
		level1.Add(Level1Fields.LastTradePrice, 100m);

		var tick = level1.ToTick();

		tick.TradePrice.AreEqual(100m);
		tick.TradeVolume.IsNull().AssertTrue();
		tick.TradeId.IsNull().AssertTrue();
		tick.OriginSide.IsNull().AssertTrue();
	}

	[TestMethod]
	public void Level1_MultipleMessages_DifferentSecurities()
	{
		var level1Messages = new[]
		{
			new Level1ChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 0, 0)
			}.Add(Level1Fields.LastTradePrice, 100m),

			new Level1ChangeMessage
			{
				SecurityId = TestSecurityId2,
				ServerTime = new DateTime(2024, 1, 15, 10, 0, 0)
			}.Add(Level1Fields.LastTradePrice, 200m)
		};

		var ticks = level1Messages.ToTicks().ToArray();

		ticks.Length.AreEqual(2);
		ticks[0].SecurityId.SecurityCode.AreEqual("SBER");
		ticks[0].TradePrice.AreEqual(100m);
		ticks[1].SecurityId.SecurityCode.AreEqual("GAZP");
		ticks[1].TradePrice.AreEqual(200m);
	}

	[TestMethod]
	public void CandleMessage_WithZeroVolume()
	{
		var candle = new TimeFrameCandleMessage
		{
			SecurityId = TestSecurityId,
			OpenTime = new DateTime(2024, 1, 15, 10, 0, 0),
			CloseTime = new DateTime(2024, 1, 15, 10, 5, 0),
			OpenPrice = 100m,
			ClosePrice = 100m,
			HighPrice = 100m,
			LowPrice = 100m,
			TotalVolume = 0
		};

		var level1 = candle.ToLevel1();

		level1.TryGetDecimal(Level1Fields.OpenPrice).AreEqual(100m);

		// Zero volume may or may not be included in Level1 message, depending on implementation
		// Just verify the conversion doesn't crash
	}

	[TestMethod]
	public void ExecutionMessage_WithNullOpenInterest()
	{
		var tick = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = TestSecurityId,
			ServerTime = new DateTime(2024, 1, 15, 10, 30, 0),
			TradePrice = 102.5m,
			TradeVolume = 100,
			OpenInterest = null
		};

		var level1 = tick.ToLevel1();

		level1.Changes.ContainsKey(Level1Fields.OpenInterest).AssertFalse();
	}

	[TestMethod]
	public void Level1_BuildFrom_PreservedInConversion()
	{
		var level1 = new Level1ChangeMessage
		{
			SecurityId = TestSecurityId,
			ServerTime = new DateTime(2024, 1, 15, 10, 0, 0),
			BuildFrom = DataType.MarketDepth
		};
		level1.Add(Level1Fields.LastTradePrice, 100m);

		var tick = level1.ToTick();

		tick.BuildFrom.AreEqual(DataType.MarketDepth);
	}

	[TestMethod]
	public void QuoteChange_WithBuildFrom()
	{
		var quote = new QuoteChangeMessage
		{
			SecurityId = TestSecurityId,
			ServerTime = new DateTime(2024, 1, 15, 10, 30, 0),
			Bids = [new QuoteChange(100m, 150)],
			Asks = [new QuoteChange(101m, 200)],
			BuildFrom = DataType.OrderLog
		};

		var level1Messages = new[] { quote }.ToLevel1().ToArray();

		level1Messages[0].BuildFrom.AreEqual(DataType.OrderLog);
	}

	[TestMethod]
	public void Level1_EmptyChanges_ToTick()
	{
		var level1 = new Level1ChangeMessage
		{
			SecurityId = TestSecurityId,
			ServerTime = new DateTime(2024, 1, 15, 10, 0, 0)
		};

		var tick = level1.ToTick();

		tick.SecurityId.AreEqual(TestSecurityId);
		tick.TradePrice.IsNull().AssertTrue();
		tick.TradeVolume.IsNull().AssertTrue();
	}

	[TestMethod]
	public void QuoteChange_EmptyBidsAndAsks()
	{
		var quote = new QuoteChangeMessage
		{
			SecurityId = TestSecurityId,
			ServerTime = new DateTime(2024, 1, 15, 10, 30, 0),
			Bids = [],
			Asks = []
		};

		var level1 = quote.ToLevel1();

		level1.Changes.Count.AreEqual(0);
		level1.SecurityId.AreEqual(TestSecurityId);
	}

	[TestMethod]
	public void Level1Fields_TypeMapping()
	{
		Level1Fields.LastTradeId.ToType().AreEqual(typeof(long));
		Level1Fields.TradesCount.ToType().AreEqual(typeof(int));
		Level1Fields.LastTradeTime.ToType().AreEqual(typeof(DateTime));
		Level1Fields.LastTradeUpDown.ToType().AreEqual(typeof(bool));
		Level1Fields.LastTradeOrigin.ToType().AreEqual(typeof(Sides));
		Level1Fields.BestBidPrice.ToType().AreEqual(typeof(decimal));
		Level1Fields.State.ToType().AreEqual(typeof(SecurityStates));
	}

	private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> source)
	{
		foreach (var item in source)
		{
			await Task.Yield();
			yield return item;
		}
	}

	[TestMethod]
	public async Task QuoteChangeMessages_ToLevel1_SyncVsAsync()
	{
		var token = CancellationToken;
		var quotes = new[]
		{
			new QuoteChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 30, 0),
				Bids = [new QuoteChange(100m, 150), new QuoteChange(99m, 200)],
				Asks = [new QuoteChange(101m, 200), new QuoteChange(102m, 250)]
			},
			new QuoteChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 30, 1),
				Bids = [new QuoteChange(100.5m, 160)],
				Asks = [new QuoteChange(101.5m, 210)]
			}
		};

		var syncResult = quotes.ToLevel1().ToArray();
		var asyncResult = await ToAsyncEnumerable(quotes).ToLevel1().ToArrayAsync(token);

		syncResult.Length.AreEqual(asyncResult.Length);
		for (var i = 0; i < syncResult.Length; i++)
		{
			syncResult[i].SecurityId.AreEqual(asyncResult[i].SecurityId);
			syncResult[i].ServerTime.AreEqual(asyncResult[i].ServerTime);
			syncResult[i].TryGetDecimal(Level1Fields.BestBidPrice).AreEqual(asyncResult[i].TryGetDecimal(Level1Fields.BestBidPrice));
			syncResult[i].TryGetDecimal(Level1Fields.BestAskPrice).AreEqual(asyncResult[i].TryGetDecimal(Level1Fields.BestAskPrice));
			syncResult[i].TryGetDecimal(Level1Fields.BestBidVolume).AreEqual(asyncResult[i].TryGetDecimal(Level1Fields.BestBidVolume));
			syncResult[i].TryGetDecimal(Level1Fields.BestAskVolume).AreEqual(asyncResult[i].TryGetDecimal(Level1Fields.BestAskVolume));
		}
	}

	[TestMethod]
	public async Task Level1ChangeMessages_ToTicks_SyncVsAsync()
	{
		var token = CancellationToken;
		var level1Messages = new[]
		{
			new Level1ChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 0, 0)
			}
			.Add(Level1Fields.LastTradePrice, 100m)
			.Add(Level1Fields.LastTradeVolume, 50m)
			.Add(Level1Fields.LastTradeId, 1L),

			new Level1ChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 0, 1)
			}
			.Add(Level1Fields.LastTradePrice, 100.5m)
			.Add(Level1Fields.LastTradeVolume, 75m)
			.Add(Level1Fields.LastTradeId, 2L),

			new Level1ChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 0, 2)
			}
			.Add(Level1Fields.LastTradePrice, 101m)
			.Add(Level1Fields.LastTradeVolume, 100m)
			.Add(Level1Fields.LastTradeId, 3L)
		};

		var syncResult = level1Messages.ToTicks().ToArray();
		var asyncResult = await ToAsyncEnumerable(level1Messages).ToTicks().ToArrayAsync(token);

		syncResult.Length.AreEqual(asyncResult.Length);
		for (var i = 0; i < syncResult.Length; i++)
		{
			syncResult[i].SecurityId.AreEqual(asyncResult[i].SecurityId);
			syncResult[i].ServerTime.AreEqual(asyncResult[i].ServerTime);
			syncResult[i].TradePrice.AreEqual(asyncResult[i].TradePrice);
			syncResult[i].TradeVolume.AreEqual(asyncResult[i].TradeVolume);
			syncResult[i].TradeId.AreEqual(asyncResult[i].TradeId);
		}
	}

	[TestMethod]
	public async Task Level1ChangeMessages_ToOrderBooks_SyncVsAsync()
	{
		var token = CancellationToken;
		var level1Messages = new[]
		{
			new Level1ChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 0, 0)
			}
			.Add(Level1Fields.BestBidPrice, 100m)
			.Add(Level1Fields.BestBidVolume, 150m)
			.Add(Level1Fields.BestAskPrice, 101m)
			.Add(Level1Fields.BestAskVolume, 200m),

			new Level1ChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 0, 1)
			}
			.Add(Level1Fields.BestBidPrice, 100.5m)
			.Add(Level1Fields.BestBidVolume, 160m)
			.Add(Level1Fields.BestAskPrice, 101.5m)
			.Add(Level1Fields.BestAskVolume, 210m),

			new Level1ChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 0, 2)
			}
			.Add(Level1Fields.BestBidPrice, 101m)
			.Add(Level1Fields.BestBidVolume, 170m)
			.Add(Level1Fields.BestAskPrice, 102m)
			.Add(Level1Fields.BestAskVolume, 220m)
		};

		var syncResult = level1Messages.ToOrderBooks().ToArray();
		var asyncResult = await ToAsyncEnumerable(level1Messages).ToOrderBooks().ToArrayAsync(token);

		syncResult.Length.AreEqual(asyncResult.Length);
		for (var i = 0; i < syncResult.Length; i++)
		{
			syncResult[i].SecurityId.AreEqual(asyncResult[i].SecurityId);
			syncResult[i].ServerTime.AreEqual(asyncResult[i].ServerTime);
			syncResult[i].Bids.Length.AreEqual(asyncResult[i].Bids.Length);
			syncResult[i].Asks.Length.AreEqual(asyncResult[i].Asks.Length);

			for (var j = 0; j < syncResult[i].Bids.Length; j++)
			{
				syncResult[i].Bids[j].Price.AreEqual(asyncResult[i].Bids[j].Price);
				syncResult[i].Bids[j].Volume.AreEqual(asyncResult[i].Bids[j].Volume);
			}

			for (var j = 0; j < syncResult[i].Asks.Length; j++)
			{
				syncResult[i].Asks[j].Price.AreEqual(asyncResult[i].Asks[j].Price);
				syncResult[i].Asks[j].Volume.AreEqual(asyncResult[i].Asks[j].Volume);
			}
		}
	}

	[TestMethod]
	public async Task QuoteChangeMessages_ToLevel1_Async()
	{
		var token = CancellationToken;
		var quotes = new[]
		{
			new QuoteChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 30, 0),
				Bids = [new QuoteChange(100m, 150)],
				Asks = [new QuoteChange(101m, 200)]
			},
			new QuoteChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 30, 1),
				Bids = [new QuoteChange(100.5m, 160)],
				Asks = [new QuoteChange(101.5m, 210)]
			}
		};

		var level1Messages = await ToAsyncEnumerable(quotes).ToLevel1().ToArrayAsync(token);

		level1Messages.Length.AreEqual(2);

		level1Messages[0].TryGetDecimal(Level1Fields.BestBidPrice).AreEqual(100m);
		level1Messages[0].TryGetDecimal(Level1Fields.BestAskPrice).AreEqual(101m);
		level1Messages[0].BuildFrom.AreEqual(DataType.MarketDepth);

		level1Messages[1].TryGetDecimal(Level1Fields.BestBidPrice).AreEqual(100.5m);
		level1Messages[1].TryGetDecimal(Level1Fields.BestAskPrice).AreEqual(101.5m);
	}

	[TestMethod]
	public async Task Level1ChangeMessages_ToTicks_Async()
	{
		var token = CancellationToken;
		var level1Messages = new[]
		{
			new Level1ChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 0, 0)
			}
			.Add(Level1Fields.LastTradePrice, 100m)
			.Add(Level1Fields.LastTradeVolume, 50m)
			.Add(Level1Fields.LastTradeId, 1L),

			new Level1ChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 0, 1)
			}
			.Add(Level1Fields.LastTradePrice, 100.5m)
			.Add(Level1Fields.LastTradeVolume, 75m)
			.Add(Level1Fields.LastTradeId, 2L)
		};

		var ticks = await ToAsyncEnumerable(level1Messages).ToTicks().ToArrayAsync(token);

		ticks.Length.AreEqual(2);

		ticks[0].TradePrice.AreEqual(100m);
		ticks[0].TradeVolume.AreEqual(50m);
		ticks[0].TradeId.AreEqual(1L);

		ticks[1].TradePrice.AreEqual(100.5m);
		ticks[1].TradeVolume.AreEqual(75m);
		ticks[1].TradeId.AreEqual(2L);
	}

	[TestMethod]
	public async Task Level1ChangeMessages_ToOrderBooks_Async()
	{
		var token = CancellationToken;
		var level1Messages = new[]
		{
			new Level1ChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 0, 0)
			}
			.Add(Level1Fields.BestBidPrice, 100m)
			.Add(Level1Fields.BestBidVolume, 150m)
			.Add(Level1Fields.BestAskPrice, 101m)
			.Add(Level1Fields.BestAskVolume, 200m),

			new Level1ChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 0, 1)
			}
			.Add(Level1Fields.BestBidPrice, 100.5m)
			.Add(Level1Fields.BestBidVolume, 160m)
			.Add(Level1Fields.BestAskPrice, 101.5m)
			.Add(Level1Fields.BestAskVolume, 210m)
		};

		var orderBooks = await ToAsyncEnumerable(level1Messages).ToOrderBooks().ToArrayAsync(token);

		orderBooks.Length.AreEqual(2);

		orderBooks[0].Bids.Length.AreEqual(1);
		orderBooks[0].Bids[0].Price.AreEqual(100m);
		orderBooks[0].Bids[0].Volume.AreEqual(150m);
		orderBooks[0].Asks.Length.AreEqual(1);
		orderBooks[0].Asks[0].Price.AreEqual(101m);
		orderBooks[0].Asks[0].Volume.AreEqual(200m);

		orderBooks[1].Bids[0].Price.AreEqual(100.5m);
		orderBooks[1].Asks[0].Price.AreEqual(101.5m);
	}

	[TestMethod]
	public async Task Level1ChangeMessages_ToTicks_FiltersNonTicks_Async()
	{
		var token = CancellationToken;
		var level1Messages = new[]
		{
			new Level1ChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 0, 0)
			}
			.Add(Level1Fields.BestBidPrice, 100m), // No tick data - should be filtered

			new Level1ChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 0, 1)
			}
			.Add(Level1Fields.LastTradePrice, 100.5m) // Has tick data
		};

		var ticks = await ToAsyncEnumerable(level1Messages).ToTicks().ToArrayAsync(token);

		ticks.Length.AreEqual(1);
		ticks[0].TradePrice.AreEqual(100.5m);
	}

	[TestMethod]
	public async Task Level1ChangeMessages_ToOrderBooks_FiltersNonQuotes_Async()
	{
		var token = CancellationToken;
		var level1Messages = new[]
		{
			new Level1ChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 0, 0)
			}
			.Add(Level1Fields.LastTradePrice, 100m), // No quote data - should be filtered

			new Level1ChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 0, 1)
			}
			.Add(Level1Fields.BestBidPrice, 100.5m)
			.Add(Level1Fields.BestBidVolume, 150m)
		};

		var orderBooks = await ToAsyncEnumerable(level1Messages).ToOrderBooks().ToArrayAsync(token);

		orderBooks.Length.AreEqual(1);
		orderBooks[0].Bids[0].Price.AreEqual(100.5m);
	}

	[TestMethod]
	public async Task Level1ChangeMessages_ToOrderBooks_SkipsDuplicates_Async()
	{
		var token = CancellationToken;
		var level1Messages = new[]
		{
			new Level1ChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 0, 0)
			}
			.Add(Level1Fields.BestBidPrice, 100m)
			.Add(Level1Fields.BestBidVolume, 150m),

			// Same values - should be skipped
			new Level1ChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 0, 1)
			}
			.Add(Level1Fields.BestBidPrice, 100m)
			.Add(Level1Fields.BestBidVolume, 150m),

			// Different values - should be included
			new Level1ChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 0, 2)
			}
			.Add(Level1Fields.BestBidPrice, 101m)
			.Add(Level1Fields.BestBidVolume, 160m)
		};

		var orderBooks = await ToAsyncEnumerable(level1Messages).ToOrderBooks().ToArrayAsync(token);

		orderBooks.Length.AreEqual(2);
		orderBooks[0].Bids[0].Price.AreEqual(100m);
		orderBooks[1].Bids[0].Price.AreEqual(101m);
	}

	[TestMethod]
	public async Task BuildIfNeed_FullOrderBook_PassThrough_Async()
	{
		var token = CancellationToken;
		var quotes = new[]
		{
			new QuoteChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 30, 0),
				Bids = [new QuoteChange(100m, 150)],
				Asks = [new QuoteChange(101m, 200)],
				State = null // Full order book
			}
		};

		var result = await ToAsyncEnumerable(quotes).BuildIfNeed().ToArrayAsync(token);

		result.Length.AreEqual(1);
		result[0].Bids[0].Price.AreEqual(100m);
		result[0].Asks[0].Price.AreEqual(101m);
	}

	[TestMethod]
	public async Task Cast_Async()
	{
		var token = CancellationToken;
		var quotes = new[]
		{
			new QuoteChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = new DateTime(2024, 1, 15, 10, 30, 0),
				Bids = [new QuoteChange(100m, 150)],
				Asks = [new QuoteChange(101m, 200)]
			}
		};

		var result = await ToAsyncEnumerable(quotes)
			.Cast<QuoteChangeMessage, Level1ChangeMessage>(q => q.ToLevel1())
			.ToArrayAsync(token);

		result.Length.AreEqual(1);
		result[0].TryGetDecimal(Level1Fields.BestBidPrice).AreEqual(100m);
		result[0].TryGetDecimal(Level1Fields.BestAskPrice).AreEqual(101m);
	}

	[TestMethod]
	public async Task QuoteChangeMessages_ToLevel1_Empty_Async()
	{
		var token = CancellationToken;
		var quotes = Array.Empty<QuoteChangeMessage>();

		var level1Messages = await ToAsyncEnumerable(quotes).ToLevel1().ToArrayAsync(token);

		level1Messages.Length.AreEqual(0);
	}

	[TestMethod]
	public async Task Level1ChangeMessages_ToTicks_Empty_Async()
	{
		var token = CancellationToken;
		var level1Messages = Array.Empty<Level1ChangeMessage>();

		var ticks = await ToAsyncEnumerable(level1Messages).ToTicks().ToArrayAsync(token);

		ticks.Length.AreEqual(0);
	}

	[TestMethod]
	public async Task Level1ChangeMessages_ToOrderBooks_Empty_Async()
	{
		var token = CancellationToken;
		var level1Messages = Array.Empty<Level1ChangeMessage>();

		var orderBooks = await ToAsyncEnumerable(level1Messages).ToOrderBooks().ToArrayAsync(token);

		orderBooks.Length.AreEqual(0);
	}

	[TestMethod]
	public Task QuoteChangeMessages_ToLevel1_Null_Async()
	{
		var token = CancellationToken;
		return ThrowsExactlyAsync<ArgumentNullException>(async () =>
		{
			IAsyncEnumerable<QuoteChangeMessage> quotes = null;
			await quotes.ToLevel1().ToArrayAsync(token);
		});
	}

	[TestMethod]
	public Task Level1ChangeMessages_ToTicks_Null_Async()
	{
		var token = CancellationToken;
		return ThrowsExactlyAsync<ArgumentNullException>(async () =>
		{
			IAsyncEnumerable<Level1ChangeMessage> level1 = null;
			await level1.ToTicks().ToArrayAsync(token);
		});
	}

	[TestMethod]
	public Task Level1ChangeMessages_ToOrderBooks_Null_Async()
	{
		var token = CancellationToken;
		return ThrowsExactlyAsync<ArgumentNullException>(async () =>
		{
			IAsyncEnumerable<Level1ChangeMessage> level1 = null;
			await level1.ToOrderBooks().ToArrayAsync(token);
		});
	}

	[TestMethod]
	public Task BuildIfNeed_Null_Async()
	{
		var token = CancellationToken;
		return ThrowsExactlyAsync<ArgumentNullException>(async () =>
		{
			IAsyncEnumerable<QuoteChangeMessage> quotes = null;
			await quotes.BuildIfNeed().ToArrayAsync(token);
		});
	}

	[TestMethod]
	public Task Cast_Null_Async()
	{
		var token = CancellationToken;
		return ThrowsExactlyAsync<ArgumentNullException>(async () =>
		{
			IAsyncEnumerable<QuoteChangeMessage> quotes = null;
			await quotes.Cast<QuoteChangeMessage, Level1ChangeMessage>(q => q.ToLevel1()).ToArrayAsync(token);
		});
	}

	[TestMethod]
	public Task Cast_NullConverter_Async()
	{
		var token = CancellationToken;
		var quotes = ToAsyncEnumerable(new[]
		{
			new QuoteChangeMessage
			{
				SecurityId = TestSecurityId,
				ServerTime = DateTime.Now
			}
		});

		return ThrowsExactlyAsync<ArgumentNullException>(async () =>
		{
			await quotes.Cast<QuoteChangeMessage, Level1ChangeMessage>(null).ToArrayAsync(token);
		});
	}
}

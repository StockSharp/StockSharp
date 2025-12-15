namespace StockSharp.Tests;

using StockSharp.Algo.Candles.Compression;

/// <summary>
/// Unit tests for ICandleBuilderValueTransform implementations.
/// Tests the data extraction logic from various market data sources.
/// </summary>
[TestClass]
public class CandleBuilderValueTransformTests : BaseTestClass
{
	#region TickCandleBuilderValueTransform Tests

	[TestMethod]
	public void TickTransform_ValidTick_ExtractsCorrectly()
	{
		var transform = new TickCandleBuilderValueTransform();

		var tick = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			ServerTime = new DateTime(2024, 1, 1, 10, 0, 0),
			TradePrice = 100.50m,
			TradeVolume = 1000m,
			OriginSide = Sides.Buy,
			OpenInterest = 50000m,
		};

		var result = transform.Process(tick);

		IsTrue(result, "Should process valid tick");
		AreEqual(new DateTime(2024, 1, 1, 10, 0, 0), ((ICandleBuilderValueTransform)transform).Time);
		AreEqual(100.50m, ((ICandleBuilderValueTransform)transform).Price);
		AreEqual(1000m, ((ICandleBuilderValueTransform)transform).Volume);
		AreEqual(Sides.Buy, ((ICandleBuilderValueTransform)transform).Side);
		AreEqual(50000m, ((ICandleBuilderValueTransform)transform).OpenInterest);
	}

	[TestMethod]
	public void TickTransform_NonTickMessage_ReturnsFalse()
	{
		var transform = new TickCandleBuilderValueTransform();

		var level1 = new Level1ChangeMessage
		{
			ServerTime = DateTime.Now,
		};

		var result = transform.Process(level1);

		IsFalse(result, "Should not process non-tick message");
	}

	[TestMethod]
	public void TickTransform_OrderLogMessage_ReturnsFalse()
	{
		var transform = new TickCandleBuilderValueTransform();

		var orderLog = new ExecutionMessage
		{
			DataTypeEx = DataType.OrderLog,
			ServerTime = DateTime.Now,
			OrderPrice = 100m,
		};

		var result = transform.Process(orderLog);

		IsFalse(result, "Should not process order log as tick");
	}

	[TestMethod]
	public void TickTransform_Reset_ClearsState()
	{
		var transform = new TickCandleBuilderValueTransform();

		// First process a tick
		var tick = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			ServerTime = DateTime.Now,
			TradePrice = 100m,
			TradeVolume = 500m,
		};
		transform.Process(tick);

		// Then reset
		transform.Process(new ResetMessage());

		// State should be cleared
		AreEqual(default(DateTime), ((ICandleBuilderValueTransform)transform).Time);
		AreEqual(0m, ((ICandleBuilderValueTransform)transform).Price);
	}

	#endregion

	#region Level1CandleBuilderValueTransform Tests

	[TestMethod]
	public void Level1Transform_LastTradePrice_ExtractsCorrectly()
	{
		var transform = new Level1CandleBuilderValueTransform(null, null)
		{
			Type = Level1Fields.LastTradePrice
		};

		var level1 = new Level1ChangeMessage
		{
			ServerTime = new DateTime(2024, 1, 1, 10, 0, 0),
		};
		level1.Add(Level1Fields.LastTradePrice, 100.25m);
		level1.Add(Level1Fields.LastTradeVolume, 500m);

		var result = transform.Process(level1);

		IsTrue(result, "Should process Level1 with LastTradePrice");
		AreEqual(100.25m, ((ICandleBuilderValueTransform)transform).Price);
		AreEqual(500m, ((ICandleBuilderValueTransform)transform).Volume);
	}

	[TestMethod]
	public void Level1Transform_LastTradePrice_MissingField_ReturnsFalse()
	{
		var transform = new Level1CandleBuilderValueTransform(null, null)
		{
			Type = Level1Fields.LastTradePrice
		};

		var level1 = new Level1ChangeMessage
		{
			ServerTime = DateTime.Now,
		};
		// No LastTradePrice field
		level1.Add(Level1Fields.BestBidPrice, 99m);

		var result = transform.Process(level1);

		IsFalse(result, "Should not process Level1 without LastTradePrice");
	}

	[TestMethod]
	public void Level1Transform_BestBidPrice_ExtractsCorrectly()
	{
		var transform = new Level1CandleBuilderValueTransform(null, null)
		{
			Type = Level1Fields.BestBidPrice
		};

		var level1 = new Level1ChangeMessage
		{
			ServerTime = new DateTime(2024, 1, 1, 10, 0, 0),
		};
		level1.Add(Level1Fields.BestBidPrice, 99.50m);
		level1.Add(Level1Fields.BestBidVolume, 1000m);

		var result = transform.Process(level1);

		IsTrue(result, "Should process Level1 with BestBidPrice");
		AreEqual(99.50m, ((ICandleBuilderValueTransform)transform).Price);
		AreEqual(1000m, ((ICandleBuilderValueTransform)transform).Volume);
		AreEqual(Sides.Buy, ((ICandleBuilderValueTransform)transform).Side);
	}

	[TestMethod]
	public void Level1Transform_BestAskPrice_ExtractsCorrectly()
	{
		var transform = new Level1CandleBuilderValueTransform(null, null)
		{
			Type = Level1Fields.BestAskPrice
		};

		var level1 = new Level1ChangeMessage
		{
			ServerTime = new DateTime(2024, 1, 1, 10, 0, 0),
		};
		level1.Add(Level1Fields.BestAskPrice, 100.50m);
		level1.Add(Level1Fields.BestAskVolume, 800m);

		var result = transform.Process(level1);

		IsTrue(result, "Should process Level1 with BestAskPrice");
		AreEqual(100.50m, ((ICandleBuilderValueTransform)transform).Price);
		AreEqual(800m, ((ICandleBuilderValueTransform)transform).Volume);
		AreEqual(Sides.Sell, ((ICandleBuilderValueTransform)transform).Side);
	}

	[TestMethod]
	public void Level1Transform_SpreadMiddle_CalculatesCorrectly()
	{
		var transform = new Level1CandleBuilderValueTransform(0.01m, 1m)
		{
			Type = Level1Fields.SpreadMiddle
		};

		var level1 = new Level1ChangeMessage
		{
			ServerTime = new DateTime(2024, 1, 1, 10, 0, 0),
		};
		level1.Add(Level1Fields.BestBidPrice, 99m);
		level1.Add(Level1Fields.BestAskPrice, 101m);

		var result = transform.Process(level1);

		IsTrue(result, "Should process Level1 for SpreadMiddle");
		AreEqual(100m, ((ICandleBuilderValueTransform)transform).Price, "SpreadMiddle should be (99+101)/2 = 100");
	}

	[TestMethod]
	public void Level1Transform_SpreadMiddle_OneSideMissing_ReturnsFalse()
	{
		var transform = new Level1CandleBuilderValueTransform(0.01m, 1m)
		{
			Type = Level1Fields.SpreadMiddle
		};

		var level1 = new Level1ChangeMessage
		{
			ServerTime = DateTime.Now,
		};
		level1.Add(Level1Fields.BestBidPrice, 99m);
		// No BestAskPrice

		var result = transform.Process(level1);

		// First message with only one side should return false
		IsFalse(result, "SpreadMiddle requires both bid and ask");
	}

	[TestMethod]
	public void Level1Transform_SpreadMiddle_CachesValues()
	{
		var transform = new Level1CandleBuilderValueTransform(0.01m, 1m)
		{
			Type = Level1Fields.SpreadMiddle
		};

		// First message with bid
		var level1a = new Level1ChangeMessage { ServerTime = DateTime.Now };
		level1a.Add(Level1Fields.BestBidPrice, 99m);
		transform.Process(level1a);

		// Second message with ask only
		var level1b = new Level1ChangeMessage { ServerTime = DateTime.Now };
		level1b.Add(Level1Fields.BestAskPrice, 101m);

		var result = transform.Process(level1b);

		// Should use cached bid value
		IsTrue(result, "Should calculate SpreadMiddle using cached values");
		AreEqual(100m, ((ICandleBuilderValueTransform)transform).Price);
	}

	#endregion

	#region QuoteCandleBuilderValueTransform Tests

	[TestMethod]
	public void QuoteTransform_BestBid_ExtractsCorrectly()
	{
		var transform = new QuoteCandleBuilderValueTransform(0.01m, 1m)
		{
			Type = Level1Fields.BestBidPrice
		};

		var depth = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2024, 1, 1, 10, 0, 0),
			Bids = [new QuoteChange(99.50m, 1000m), new QuoteChange(99.00m, 2000m)],
			Asks = [new QuoteChange(100.50m, 800m)],
		};

		var result = transform.Process(depth);

		IsTrue(result, "Should process depth for BestBid");
		AreEqual(99.50m, ((ICandleBuilderValueTransform)transform).Price, "Should use best bid price");
		AreEqual(1000m, ((ICandleBuilderValueTransform)transform).Volume, "Should use best bid volume");
		AreEqual(Sides.Buy, ((ICandleBuilderValueTransform)transform).Side);
	}

	[TestMethod]
	public void QuoteTransform_BestAsk_ExtractsCorrectly()
	{
		var transform = new QuoteCandleBuilderValueTransform(0.01m, 1m)
		{
			Type = Level1Fields.BestAskPrice
		};

		var depth = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2024, 1, 1, 10, 0, 0),
			Bids = [new QuoteChange(99.50m, 1000m)],
			Asks = [new QuoteChange(100.50m, 800m), new QuoteChange(101.00m, 1200m)],
		};

		var result = transform.Process(depth);

		IsTrue(result, "Should process depth for BestAsk");
		AreEqual(100.50m, ((ICandleBuilderValueTransform)transform).Price, "Should use best ask price");
		AreEqual(800m, ((ICandleBuilderValueTransform)transform).Volume, "Should use best ask volume");
		AreEqual(Sides.Sell, ((ICandleBuilderValueTransform)transform).Side);
	}

	[TestMethod]
	public void QuoteTransform_EmptyBids_BestBid_ReturnsFalse()
	{
		var transform = new QuoteCandleBuilderValueTransform(0.01m, 1m)
		{
			Type = Level1Fields.BestBidPrice
		};

		var depth = new QuoteChangeMessage
		{
			ServerTime = DateTime.Now,
			Bids = [],
			Asks = [new QuoteChange(100m, 500m)],
		};

		var result = transform.Process(depth);

		IsFalse(result, "Should not process depth without bids for BestBid type");
	}

	[TestMethod]
	public void QuoteTransform_EmptyAsks_BestAsk_ReturnsFalse()
	{
		var transform = new QuoteCandleBuilderValueTransform(0.01m, 1m)
		{
			Type = Level1Fields.BestAskPrice
		};

		var depth = new QuoteChangeMessage
		{
			ServerTime = DateTime.Now,
			Bids = [new QuoteChange(99m, 500m)],
			Asks = [],
		};

		var result = transform.Process(depth);

		IsFalse(result, "Should not process depth without asks for BestAsk type");
	}

	[TestMethod]
	public void QuoteTransform_SpreadMiddle_CalculatesCorrectly()
	{
		var transform = new QuoteCandleBuilderValueTransform(0.01m, 1m)
		{
			Type = Level1Fields.SpreadMiddle
		};

		var depth = new QuoteChangeMessage
		{
			ServerTime = new DateTime(2024, 1, 1, 10, 0, 0),
			Bids = [new QuoteChange(99m, 1000m)],
			Asks = [new QuoteChange(101m, 1000m)],
		};

		var result = transform.Process(depth);

		IsTrue(result, "Should process depth for SpreadMiddle");
		AreEqual(100m, ((ICandleBuilderValueTransform)transform).Price, "SpreadMiddle should be (99+101)/2 = 100");
	}

	[TestMethod]
	public void QuoteTransform_SpreadMiddle_OneSideMissing_ReturnsFalse()
	{
		var transform = new QuoteCandleBuilderValueTransform(0.01m, 1m)
		{
			Type = Level1Fields.SpreadMiddle
		};

		var depth = new QuoteChangeMessage
		{
			ServerTime = DateTime.Now,
			Bids = [],
			Asks = [],
		};

		var result = transform.Process(depth);

		IsFalse(result, "SpreadMiddle requires both sides");
	}

	#endregion

	#region OrderLogCandleBuilderValueTransform Tests

	[TestMethod]
	public void OrderLogTransform_MatchedTrade_ExtractsCorrectly()
	{
		var transform = new OrderLogCandleBuilderValueTransform
		{
			Type = Level1Fields.LastTradePrice
		};

		var orderLog = new ExecutionMessage
		{
			DataTypeEx = DataType.OrderLog,
			ServerTime = new DateTime(2024, 1, 1, 10, 0, 0),
			TradePrice = 100.25m,
			TradeVolume = 500m,
			OriginSide = Sides.Buy,
			OpenInterest = 10000m,
		};

		var result = transform.Process(orderLog);

		IsTrue(result, "Should process matched order log entry");
		AreEqual(100.25m, ((ICandleBuilderValueTransform)transform).Price);
		AreEqual(500m, ((ICandleBuilderValueTransform)transform).Volume);
		AreEqual(Sides.Buy, ((ICandleBuilderValueTransform)transform).Side);
	}

	[TestMethod]
	public void OrderLogTransform_NonMatchedOrder_ReturnsFalse()
	{
		var transform = new OrderLogCandleBuilderValueTransform
		{
			Type = Level1Fields.LastTradePrice
		};

		var orderLog = new ExecutionMessage
		{
			DataTypeEx = DataType.OrderLog,
			ServerTime = DateTime.Now,
			OrderPrice = 100m,
			OrderVolume = 500m,
			// TradePrice is null - not matched
		};

		var result = transform.Process(orderLog);

		IsFalse(result, "Should not process non-matched order log for LastTradePrice type");
	}

	[TestMethod]
	public void OrderLogTransform_PriceBookType_ExtractsOrderPrice()
	{
		var transform = new OrderLogCandleBuilderValueTransform
		{
			Type = Level1Fields.PriceBook
		};

		var orderLog = new ExecutionMessage
		{
			DataTypeEx = DataType.OrderLog,
			ServerTime = new DateTime(2024, 1, 1, 10, 0, 0),
			OrderPrice = 99.75m,
			OrderVolume = 1000m,
			Side = Sides.Buy,
		};

		var result = transform.Process(orderLog);

		IsTrue(result, "Should process order log for PriceBook type");
		AreEqual(99.75m, ((ICandleBuilderValueTransform)transform).Price, "Should use OrderPrice for PriceBook type");
		AreEqual(1000m, ((ICandleBuilderValueTransform)transform).Volume);
	}

	[TestMethod]
	public void OrderLogTransform_NonOrderLogMessage_ReturnsFalse()
	{
		var transform = new OrderLogCandleBuilderValueTransform();

		var tick = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			ServerTime = DateTime.Now,
			TradePrice = 100m,
		};

		var result = transform.Process(tick);

		IsFalse(result, "Should not process tick message as order log");
	}

	#endregion

	#region BuildFrom Property Tests

	[TestMethod]
	public void Transform_BuildFrom_TickTransform_ReturnsTicks()
	{
		var transform = new TickCandleBuilderValueTransform();
		AreEqual(DataType.Ticks, ((ICandleBuilderValueTransform)transform).BuildFrom);
	}

	[TestMethod]
	public void Transform_BuildFrom_Level1Transform_ReturnsLevel1()
	{
		var transform = new Level1CandleBuilderValueTransform(null, null);
		AreEqual(DataType.Level1, ((ICandleBuilderValueTransform)transform).BuildFrom);
	}

	[TestMethod]
	public void Transform_BuildFrom_QuoteTransform_ReturnsMarketDepth()
	{
		var transform = new QuoteCandleBuilderValueTransform(null, null);
		AreEqual(DataType.MarketDepth, ((ICandleBuilderValueTransform)transform).BuildFrom);
	}

	[TestMethod]
	public void Transform_BuildFrom_OrderLogTransform_ReturnsOrderLog()
	{
		var transform = new OrderLogCandleBuilderValueTransform();
		AreEqual(DataType.OrderLog, ((ICandleBuilderValueTransform)transform).BuildFrom);
	}

	#endregion

	#region Edge Cases

	[TestMethod]
	public void TickTransform_ZeroVolume_Accepted()
	{
		var transform = new TickCandleBuilderValueTransform();

		var tick = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			ServerTime = DateTime.Now,
			TradePrice = 100m,
			TradeVolume = 0m, // Zero volume
		};

		var result = transform.Process(tick);

		IsTrue(result, "Should accept zero volume tick");
		AreEqual(0m, ((ICandleBuilderValueTransform)transform).Volume);
	}

	[TestMethod]
	public void TickTransform_NegativePrice_Accepted()
	{
		var transform = new TickCandleBuilderValueTransform();

		var tick = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			ServerTime = DateTime.Now,
			TradePrice = -37.63m, // Negative price (like WTI oil in 2020)
			TradeVolume = 100m,
		};

		var result = transform.Process(tick);

		IsTrue(result, "Should accept negative price");
		AreEqual(-37.63m, ((ICandleBuilderValueTransform)transform).Price);
	}

	[TestMethod]
	public void TickTransform_VerySmallPrice_PrecisionMaintained()
	{
		var transform = new TickCandleBuilderValueTransform();

		var tick = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			ServerTime = DateTime.Now,
			TradePrice = 0.00000001m, // Very small price (like SHIB)
			TradeVolume = 1000000000m,
		};

		var result = transform.Process(tick);

		IsTrue(result, "Should accept very small price");
		AreEqual(0.00000001m, ((ICandleBuilderValueTransform)transform).Price);
	}

	[TestMethod]
	public void Level1Transform_Reset_ClearsCachedValues()
	{
		var transform = new Level1CandleBuilderValueTransform(0.01m, 1m)
		{
			Type = Level1Fields.SpreadMiddle
		};

		// Cache some values
		var level1a = new Level1ChangeMessage { ServerTime = DateTime.Now };
		level1a.Add(Level1Fields.BestBidPrice, 99m);
		level1a.Add(Level1Fields.BestAskPrice, 101m);
		transform.Process(level1a);

		// Reset
		transform.Process(new ResetMessage());

		// Try with only one side - should fail because cache is cleared
		var level1b = new Level1ChangeMessage { ServerTime = DateTime.Now };
		level1b.Add(Level1Fields.BestBidPrice, 98m);

		var result = transform.Process(level1b);

		IsFalse(result, "After reset, cached values should be cleared");
	}

	#endregion
}

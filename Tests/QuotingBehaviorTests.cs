namespace StockSharp.Tests;

using StockSharp.Algo.Derivatives;
using StockSharp.Algo.Strategies.Quoting;

[TestClass]
public class QuotingBehaviorTests
{
	private Mock<IMarketDataProvider> _providerMock;
	private Mock<IBlackScholes> _bsMock;
	private Security _security;
	private QuoteChange[] _bids;
	private QuoteChange[] _asks;
	private decimal _lastTradePrice = 100.50m;

	[TestInitialize]
	public void Setup()
	{
		_providerMock = new Mock<IMarketDataProvider>();
		_bsMock = new Mock<IBlackScholes>();
		_security = Helper.CreateSecurity();
		_security.PriceStep = 0.01m;

		_bids =
		[
			new QuoteChange(100.50m, 1000),
			new QuoteChange(100.49m, 500),
			new QuoteChange(100.48m, 300)
		];
		
		_asks =
		[
			new QuoteChange(100.51m, 800),
			new QuoteChange(100.52m, 400),
			new QuoteChange(100.53m, 200)
		];
	}

	[TestMethod]
	public void Market_CalculateBestPrice_Following_BuySide_ReturnsCorrectPrice()
	{
		// Arrange
		IQuotingBehavior behavior = new MarketQuotingBehavior(new Unit(0.01m), new Unit(0.01m), MarketPriceTypes.Following);
		
		// Act
		var result = behavior.CalculateBestPrice(_security, _providerMock.Object, Sides.Buy, 100.50m, 100.51m, _lastTradePrice, _bids, _asks);
		
		// Assert
		result.AssertEqual(100.51m); // bestBidPrice + priceOffset
	}

	[TestMethod]
	public void Market_CalculateBestPrice_Following_SellSide_ReturnsCorrectPrice()
	{
		// Arrange
		IQuotingBehavior behavior = new MarketQuotingBehavior(new Unit(0.01m), new Unit(0.01m), MarketPriceTypes.Following);
		
		// Act
		var result = behavior.CalculateBestPrice(_security, _providerMock.Object, Sides.Sell, 100.50m, 100.51m, _lastTradePrice, _bids, _asks);
		
		// Assert
		result.AssertEqual(100.50m); // bestAskPrice - priceOffset
	}

	[TestMethod]
	public void Market_CalculateBestPrice_Opposite_BuySide_ReturnsCorrectPrice()
	{
		// Arrange
		IQuotingBehavior behavior = new MarketQuotingBehavior(new Unit(0.01m), new Unit(0.01m), MarketPriceTypes.Opposite);
		
		// Act
		var result = behavior.CalculateBestPrice(_security, _providerMock.Object, Sides.Buy, 100.50m, 100.51m, _lastTradePrice, _bids, _asks);
		
		// Assert
		result.AssertEqual(100.52m); // bestAskPrice + priceOffset
	}

	[TestMethod]
	public void Market_CalculateBestPrice_Middle_ReturnsCorrectPrice()
	{
		// Arrange
		IQuotingBehavior behavior = new MarketQuotingBehavior(new Unit(0.01m), new Unit(0.01m), MarketPriceTypes.Middle);
		
		// Act
		var result = behavior.CalculateBestPrice(_security, _providerMock.Object, Sides.Buy, 100.50m, 100.52m, _lastTradePrice, _bids, _asks);
		
		// Assert
		result.AssertEqual(100.52m); // (100.50 + 100.52) / 2 + 0.01 = 100.51 + 0.01
	}

	[TestMethod]
	public void Market_CalculateBestPrice_NoBestPrices_UsesLastTradePrice()
	{
		// Arrange
		IQuotingBehavior behavior = new MarketQuotingBehavior(new Unit(0.01m), new Unit(0.01m), MarketPriceTypes.Following);
		
		// Act
		var result = behavior.CalculateBestPrice(_security, _providerMock.Object, Sides.Buy, null, null, _lastTradePrice, _bids, _asks);
		
		// Assert
		result.AssertEqual(100.51m); // lastTradePrice + priceOffset
	}

	[TestMethod]
	public void Market_NeedQuoting_NoBestPrice_ReturnsNull()
	{
		// Arrange
		IQuotingBehavior behavior = new MarketQuotingBehavior(new Unit(0.01m), new Unit(0.01m));
		
		// Act
		var result = behavior.NeedQuoting(_security, _providerMock.Object, DateTimeOffset.Now, 100.50m, 1000, 1000, null);

		// Assert
		result.AssertNull();
	}

	[TestMethod]
	public void Market_NeedQuoting_NoCurrentPrice_ReturnsBestPrice()
	{
		// Arrange
		IQuotingBehavior behavior = new MarketQuotingBehavior(new Unit(0.01m), new Unit(0.01m));
		
		// Act
		var result = behavior.NeedQuoting(_security, _providerMock.Object, DateTimeOffset.Now, null, 1000, 1000, 100.50m);
		
		// Assert
		result.AssertEqual(100.50m);
	}

	[TestMethod]
	public void Market_NeedQuoting_PriceDifferenceExceedsOffset_ReturnsBestPrice()
	{
		// Arrange
		IQuotingBehavior behavior = new MarketQuotingBehavior(new Unit(0.01m), new Unit(0.01m));
		
		// Act
		var result = behavior.NeedQuoting(_security, _providerMock.Object, DateTimeOffset.Now, 100.50m, 1000, 1000, 100.52m);
		
		// Assert
		result.AssertEqual(100.52m);
	}

	[TestMethod]
	public void Market_NeedQuoting_VolumeChanged_ReturnsBestPrice()
	{
		// Arrange
		IQuotingBehavior behavior = new MarketQuotingBehavior(new Unit(0.01m), new Unit(0.01m));
		
		// Act
		var result = behavior.NeedQuoting(_security, _providerMock.Object, DateTimeOffset.Now, 100.50m, 1000, 1500, 100.50m);
		
		// Assert
		result.AssertEqual(100.50m);
	}

	[TestMethod]
	public void Market_NeedQuoting_NoChangeNeeded_ReturnsNull()
	{
		// Arrange
		IQuotingBehavior behavior = new MarketQuotingBehavior(new Unit(0.01m), new Unit(0.01m));
		
		// Act
		var result = behavior.NeedQuoting(_security, _providerMock.Object, DateTimeOffset.Now, 100.50m, 1000, 1000, 100.50m);

		// Assert
		result.AssertNull();
	}

	[TestMethod]
	public void Limit_CalculateBestPrice_AlwaysReturnsLimitPrice()
	{
		// Arrange
		var limitPrice = 100.75m;
		IQuotingBehavior behavior = new LimitQuotingBehavior(limitPrice);
		
		// Act
		var result = behavior.CalculateBestPrice(_security, _providerMock.Object, Sides.Buy, 100.50m, 100.51m, 100.50m, _bids, _asks);
		
		// Assert
		result.AssertEqual(limitPrice);
	}

	[TestMethod]
	public void Limit_NeedQuoting_CurrentPriceEqualsBestPrice_SameVolume_ReturnsNull()
	{
		// Arrange
		IQuotingBehavior behavior = new LimitQuotingBehavior(100.75m);
		
		// Act
		var result = behavior.NeedQuoting(_security, _providerMock.Object, DateTimeOffset.Now, 100.75m, 1000, 1000, 100.75m);
		
		// Assert
		result.AssertNull();
	}

	[TestMethod]
	public void Limit_NeedQuoting_CurrentPriceDifferent_ReturnsBestPrice()
	{
		// Arrange
		IQuotingBehavior behavior = new LimitQuotingBehavior(100.75m);
		
		// Act
		var result = behavior.NeedQuoting(_security, _providerMock.Object, DateTimeOffset.Now, 100.70m, 1000, 1000, 100.75m);
		
		// Assert
		result.AssertEqual(100.75m);
	}

	[TestMethod]
	public void BestByVolume_CalculateBestPrice_BuySide_VolumeThresholdExceeded_ReturnsCorrectPrice()
	{
		// Arrange
		IQuotingBehavior behavior = new BestByVolumeQuotingBehavior(new Unit(1400m));
		
		// Act
		var result = behavior.CalculateBestPrice(_security, _providerMock.Object, Sides.Buy, 100.50m, 100.51m, _lastTradePrice, _bids, _asks);
		
		// Assert
		result.AssertEqual(_bids.ElementAt(1).Price); // Volume 1000 + 500 = 1500 > 1400, so second level price
	}

	[TestMethod]
	public void BestByVolume_CalculateBestPrice_SellSide_VolumeThresholdNotExceeded_ReturnsLastQuote()
	{
		// Arrange
		IQuotingBehavior behavior = new BestByVolumeQuotingBehavior(new Unit(1500m));
		
		// Act
		var result = behavior.CalculateBestPrice(_security, _providerMock.Object, Sides.Sell, 100.50m, 100.51m, _lastTradePrice, _bids, _asks);
		
		// Assert
		result.AssertEqual(_asks.Last().Price); // Total volume 800+400+200 = 1400 < 1500, so last quote
	}

	[TestMethod]
	public void BestByVolume_CalculateBestPrice_NoQuotes_ReturnsLastTradePrice()
	{
		// Arrange
		IQuotingBehavior behavior = new BestByVolumeQuotingBehavior(new Unit(500m));
		var emptyQuotes = Array.Empty<QuoteChange>();
		
		// Act
		var result = behavior.CalculateBestPrice(_security, _providerMock.Object, Sides.Buy, 100.50m, 100.51m, _lastTradePrice, emptyQuotes, _asks);
		
		// Assert
		result.AssertEqual(_lastTradePrice);
	}

	[TestMethod]
	public void Level_CalculateBestPrice_BuySide_ValidRange_ReturnsMidpoint()
	{
		// Arrange
		var level = new Range<int>(1, 2); // Levels 1-2
		IQuotingBehavior behavior = new LevelQuotingBehavior(level, false);
		
		// Act
		var result = behavior.CalculateBestPrice(_security, _providerMock.Object, Sides.Buy, 100.50m, 100.51m, _lastTradePrice, _bids, _asks);
		
		// Assert
		result.AssertEqual(100.48m); // (100.49 + 100.48) / 2 = 100.485 -> 100.48
	}

	[TestMethod]
	public void Level_CalculateBestPrice_SellSide_ValidRange_ReturnsMidpoint()
	{
		// Arrange
		var level = new Range<int>(0, 1); // Levels 0-1
		IQuotingBehavior behavior = new LevelQuotingBehavior(level, false);
		
		// Act
		var result = behavior.CalculateBestPrice(_security, _providerMock.Object, Sides.Sell, 100.50m, 100.51m, _lastTradePrice, _bids, _asks);
		
		// Assert
		result.AssertEqual(100.52m); // (100.51 + 100.52) / 2 = 100.515 -> 100.52
	}

	[TestMethod]
	public void Level_CalculateBestPrice_MaxLevelOutOfRange_OwnLevelTrue_CreatesCustomLevel()
	{
		// Arrange
		var level = new Range<int>(1, 5); // Level 5 doesn't exist
		IQuotingBehavior behavior = new LevelQuotingBehavior(level, true);
		
		// Act
		var result = behavior.CalculateBestPrice(_security, _providerMock.Object, Sides.Buy, 100.50m, 100.51m, _lastTradePrice, _bids, _asks);
		
		// Assert
		var expectedToPrice = 100.49m + (-1) * 4 * 0.01m; // fromPrice + direction * length * pip
		var expectedResult = Math.Round((100.49m + expectedToPrice) / 2, 2); // округление до 2 знаков
		result.AssertEqual(expectedResult);
	}

	[TestMethod]
	public void Level_CalculateBestPrice_MaxLevelOutOfRange_OwnLevelFalse_UsesLastPrice()
	{
		// Arrange
		var level = new Range<int>(1, 5); // Level 5 doesn't exist
		IQuotingBehavior behavior = new LevelQuotingBehavior(level, false);
		
		// Act
		var result = behavior.CalculateBestPrice(_security, _providerMock.Object, Sides.Buy, 100.50m, 100.51m, _lastTradePrice, _bids, _asks);
		
		// Assert
		var expectedResult = Math.Round((100.49m + 100.48m) / 2, 2); // (fromPrice + lastPrice) / 2, округление до 2 знаков
		result.AssertEqual(expectedResult);
	}

	[TestMethod]
	public void Level_CalculateBestPrice_NoQuotes_ReturnsLastTradePrice()
	{
		// Arrange
		var level = new Range<int>(0, 1);
		IQuotingBehavior behavior = new LevelQuotingBehavior(level, false);
		var emptyQuotes = Array.Empty<QuoteChange>();
		
		// Act
		var result = behavior.CalculateBestPrice(_security, _providerMock.Object, Sides.Buy, 100.50m, 100.51m, _lastTradePrice, emptyQuotes, _asks);
		
		// Assert
		result.AssertEqual(_lastTradePrice);
	}

	[TestMethod]
	public void Level_CalculateBestPrice_MinLevelOutOfRange_ReturnsNull()
	{
		// Arrange
		var level = new Range<int>(10, 15); // No such levels exist
		IQuotingBehavior behavior = new LevelQuotingBehavior(level, false);
		
		// Act
		var result = behavior.CalculateBestPrice(_security, _providerMock.Object, Sides.Buy, 100.50m, 100.51m, _lastTradePrice, _bids, _asks);
		
		// Assert
		result.AssertNull();
	}

	[TestMethod]
	public void LastTrade_CalculateBestPrice_AlwaysReturnsLastTradePrice()
	{
		// Arrange
		IQuotingBehavior behavior = new LastTradeQuotingBehavior(new Unit(0.01m));
		
		// Act
		var result = behavior.CalculateBestPrice(_security, _providerMock.Object, Sides.Buy, 100.50m, 100.51m, _lastTradePrice, _bids, _asks);
		
		// Assert
		result.AssertEqual(_lastTradePrice);
	}

	[TestMethod]
	public void LastTrade_CalculateBestPrice_NoLastTradePrice_ReturnsNull()
	{
		// Arrange
		IQuotingBehavior behavior = new LastTradeQuotingBehavior(new Unit(0.01m));
		
		// Act
		var result = behavior.CalculateBestPrice(_security, _providerMock.Object, Sides.Buy, 100.50m, 100.51m, null, _bids, _asks);
		
		// Assert
		result.AssertNull();
	}

	[TestMethod]
	public void LastTrade_NeedQuoting_PriceDifferenceExceedsOffset_ReturnsBestPrice()
	{
		// Arrange
		IQuotingBehavior behavior = new LastTradeQuotingBehavior(new Unit(0.01m));
		
		// Act
		var result = behavior.NeedQuoting(_security, _providerMock.Object, DateTimeOffset.Now, 100.50m, 1000, 1000, 100.52m);
		
		// Assert
		result.AssertEqual(100.52m);
	}

	[TestMethod]
	public void LastTrade_Constructor_NullBestPriceOffset_CreatesDefaultUnit()
	{
		// Arrange & Act
		IQuotingBehavior behavior = new LastTradeQuotingBehavior(null);
		
		// Assert - Should not throw
		var result = behavior.CalculateBestPrice(_security, _providerMock.Object, Sides.Buy, 
			100.50m, 100.51m, 100.50m, [], []);
		
		result.AssertEqual(100.50m); // округление 100.505 -> 100.50
	}

	[TestMethod]
	public void TheorPrice_CalculateBestPrice_BuySide_ReturnsBestBidPrice()
	{
		// Arrange
		var range = new Range<Unit>(new Unit(-0.05m), new Unit(0.05m));
		IQuotingBehavior behavior = new TheorPriceQuotingBehavior(range);
		
		// Act
		var result = behavior.CalculateBestPrice(_security, _providerMock.Object, Sides.Buy, 100.50m, 100.51m, _lastTradePrice, _bids, _asks);
		
		// Assert
		result.AssertEqual(_bids.First().Price);
	}

	[TestMethod]
	public void TheorPrice_NeedQuoting_NoTheorPrice_ReturnsNull()
	{
		// Arrange
		var range = new Range<Unit>(new Unit(-0.05m), new Unit(0.05m));
		IQuotingBehavior behavior = new TheorPriceQuotingBehavior(range);
		_providerMock
			.Setup(p => p.GetSecurityValue(_security, Level1Fields.TheorPrice))
			.Returns((decimal?)null);
		
		// Act
		var result = behavior.NeedQuoting(_security, _providerMock.Object, DateTimeOffset.Now, 100.50m, 1000, 1000, 100.50m);
		
		// Assert
		result.AssertNull();
	}

	[TestMethod]
	public void Volatility_NeedQuoting_PriceWithinRange_SameVolume_ReturnsNull()
	{
		// Arrange
		var ivRange = new Range<decimal>(0.15m, 0.25m);
		IQuotingBehavior behavior = new VolatilityQuotingBehavior(ivRange, _bsMock.Object);
		var currentTime = DateTimeOffset.Now;

		_bsMock.Setup(m => m.Premium(It.IsAny<DateTimeOffset>(), 0.0015m, null)).Returns(100.45m);
		_bsMock.Setup(m => m.Premium(It.IsAny<DateTimeOffset>(), 0.0025m, null)).Returns(100.55m);

		// Act
		var result = behavior.NeedQuoting(_security, _providerMock.Object, currentTime, 100.50m, 1000, 1000, 100.50m);
		
		// Assert
		result.AssertNull();
	}

	[TestMethod]
	public void Volatility_NeedQuoting_PriceOutsideRange_ReturnsAveragePrice()
	{
		// Arrange
		var ivRange = new Range<decimal>(0.15m, 0.25m);
		IQuotingBehavior behavior = new VolatilityQuotingBehavior(ivRange, _bsMock.Object);
		var currentTime = DateTimeOffset.Now;

		_bsMock.Setup(m => m.Premium(currentTime, 0.0015m, null)).Returns(100.45m); // minPrice
		_bsMock.Setup(m => m.Premium(currentTime, 0.0025m, null)).Returns(100.55m); // maxPrice
		
		// Act
		var result = behavior.NeedQuoting(_security, _providerMock.Object, currentTime, 100.40m, 1000, 1000, 100.50m);
		
		// Assert
		result.AssertEqual(100.50m); // (100.45 + 100.55) / 2
	}

	[TestMethod]
	public void Volatility_NeedQuoting_VolumeChanged_ReturnsCurrentPrice()
	{
		// Arrange
		var ivRange = new Range<decimal>(0.15m, 0.25m);
		IQuotingBehavior behavior = new VolatilityQuotingBehavior(ivRange, _bsMock.Object);
		var currentTime = DateTimeOffset.Now;

		_bsMock.Setup(m => m.Premium(currentTime, 0.0015m, null)).Returns(100.45m);
		_bsMock.Setup(m => m.Premium(currentTime, 0.0025m, null)).Returns(100.55m);
		
		// Act
		var result = behavior.NeedQuoting(_security, _providerMock.Object, currentTime, 100.50m, 1000, 1500, 100.50m);
		
		// Assert
		result.AssertEqual(100.50m);
	}

	[TestMethod]
	public void Volatility_Constructor_NullIvRange_ThrowsArgumentNullException()
	{
		// Arrange & Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() => 
			new VolatilityQuotingBehavior(null, _bsMock.Object));
	}

	[TestMethod]
	public void Volatility_Constructor_NullModel_ThrowsArgumentNullException()
	{
		// Arrange
		var ivRange = new Range<decimal>(0.15m, 0.25m);
		
		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() => 
			new VolatilityQuotingBehavior(ivRange, null));
	}

	[TestMethod]
	public void Market_CalculateBestPrice_InvalidPriceType_ThrowsInvalidOperationException()
	{
		// Arrange
		IQuotingBehavior behavior = new MarketQuotingBehavior(new Unit(0.01m), new Unit(0.01m), (MarketPriceTypes)999);
		
		// Act & Assert
		Assert.ThrowsExactly<InvalidOperationException>(() =>
			behavior.CalculateBestPrice(_security, _providerMock.Object, Sides.Buy, 100.50m, 100.51m, _lastTradePrice, [], []));
	}

	[TestMethod]
	public void Market_Constructor_NullPriceOffset_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() => 
			new MarketQuotingBehavior(null, new Unit(0.01m)));
	}

	[TestMethod]
	public void Market_Constructor_NullBestPriceOffset_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() => 
			new MarketQuotingBehavior(new Unit(0.01m), null));
	}

	[TestMethod]
	public void BestByVolume_Constructor_NullVolumeExchange_CreatesDefaultUnit()
	{
		// Arrange & Act
		IQuotingBehavior behavior = new BestByVolumeQuotingBehavior(null);
		
		// Assert - Should not throw and behavior should work with default Unit
		var result = behavior.CalculateBestPrice(_security, _providerMock.Object, Sides.Buy, 100.50m, 100.51m, _lastTradePrice, 
			[new QuoteChange(100.50m, 1000)], 
			[new QuoteChange(100.51m, 800)]);
		
		result.AssertEqual(100.50m);
	}

	[TestMethod]
	public void Level_Constructor_NullLevel_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() => 
			new LevelQuotingBehavior(null, false));
	}

	[TestMethod]
	public void Level_CalculateBestPrice_NullPriceStep_UsesDefaultPip()
	{
		// Arrange
		var securityWithoutPriceStep = new Security { Id = "TEST", PriceStep = null };
		var level = new Range<int>(1, 3);
		IQuotingBehavior behavior = new LevelQuotingBehavior(level, true);
		var bids = new[]
		{
			new QuoteChange(100.50m, 1000),
			new QuoteChange(100.49m, 500)
		};
		
		// Act
		var result = behavior.CalculateBestPrice(securityWithoutPriceStep, _providerMock.Object, Sides.Buy, 
			100.50m, 100.51m, _lastTradePrice, bids, []);

		result.AssertEqual(100.48m);
	}

	[TestMethod]
	public void TheorPrice_Constructor_NullTheorPriceOffset_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() => 
			new TheorPriceQuotingBehavior(null));
	}

	[TestMethod]
	public void AllBehaviors_EmptyQuote()
	{
		// Arrange
		var emptyBids = Array.Empty<QuoteChange>();
		var emptyAsks = Array.Empty<QuoteChange>();

		var behaviors = new IQuotingBehavior[]
		{
			new MarketQuotingBehavior(new Unit(0.01m), new Unit(0.01m)),
			new BestByPriceQuotingBehavior(new Unit(0.01m)),
			new LimitQuotingBehavior(100.50m),
			new BestByVolumeQuotingBehavior(new Unit(500m)),
			new LevelQuotingBehavior(new Range<int>(0, 1), false),
			new LastTradeQuotingBehavior(new Unit(0.01m))
		};

		foreach (IQuotingBehavior behavior in behaviors)
		{
			// Act
			var result = behavior.CalculateBestPrice(_security, _providerMock.Object, Sides.Buy, 
				null, null, _lastTradePrice, emptyBids, emptyAsks);
			
			// Assert - Should handle gracefully without throwing
			// Most should return lastTradePrice or null, LimitQuotingBehavior should return limit price
			if (behavior is LimitQuotingBehavior)
			{
				result.AssertEqual(_lastTradePrice);
			}
			else if (behavior is MarketQuotingBehavior)
			{
				result.AssertEqual((decimal)(_lastTradePrice + new Unit(0.01m)));
			}
			else
			{
				result.AssertEqual(_lastTradePrice);
			}
		}
	}

	[TestMethod]
	public void AllBehaviors_NullInputs()
	{
		// Arrange
		var behaviors = new IQuotingBehavior[]
		{
			new MarketQuotingBehavior(new Unit(0.01m), new Unit(0.01m)),
			new BestByPriceQuotingBehavior(new Unit(0.01m)),
			new LimitQuotingBehavior(100.50m),
			new BestByVolumeQuotingBehavior(new Unit(500m)),
			new LevelQuotingBehavior(new Range<int>(0, 1), false),
			new LastTradeQuotingBehavior(new Unit(0.01m))
		};

		foreach (IQuotingBehavior behavior in behaviors)
		{
			// Act - Test with all null prices
			var result = behavior.CalculateBestPrice(_security, _providerMock.Object, Sides.Buy, 
				null, null, null, [], []);
			
			// Assert - Should handle gracefully
			if (behavior is LimitQuotingBehavior)
			{
				result.AssertEqual(100.50m); // Always returns limit price
			}
			else
			{
				result.AssertNull(); // Others should return null when no price data available
			}
		}
	}
}
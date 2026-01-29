namespace StockSharp.Tests;

[TestClass]
public class BasketSecurityTests : BaseTestClass
{
	[TestMethod]
	public void WeightedIndex()
	{
		CreateSpot(out var lkoh, out var sber);

		var basket = new WeightedIndexSecurity
		{
			Id = "LKOH_SBER_WEI@TQBR",
			Board = ExchangeBoard.Associated,
		};
		basket.Weights[lkoh.ToSecurityId()] = 1;
		basket.Weights[sber.ToSecurityId()] = -10;

		Do(basket, prices => prices[0] * 1 + prices[1] * (-10), lkoh, sber);
	}

	[TestMethod]
	public void ExpressionIndex()
	{
		CreateSpot(out var lkoh, out var sber);

		var basket = new ExpressionIndexSecurity
		{
			Id = "LKOH_SBER_EXP@TQBR",
			Board = ExchangeBoard.MicexTqbr,
			BasketExpression = "LKOH@TQBR - 10 * SBER@TQBR",
		};

		Do(basket, prices => prices[0] - 10 * prices[1], lkoh, sber);
	}

	[TestMethod]
	public void ExpirationContinuous()
	{
		CreateFut(out var riu, out var riz);

		var basket = new ExpirationContinuousSecurity
		{
			Id = "RI@FORTS",
			Board = ExchangeBoard.Forts,
		};

		basket.ExpirationJumps.Add(riu.ToSecurityId(), riu.ExpiryDate.Value);
		basket.ExpirationJumps.Add(riz.ToSecurityId(), riz.ExpiryDate.Value);

		Do(basket, prices => prices[0], riu, riz);
	}

	[TestMethod]
	public void VolumeContinuous()
	{
		CreateFut(out var riu, out var riz);

		var basket = new VolumeContinuousSecurity
		{
			Id = "RI@FORTS",
			Board = ExchangeBoard.Forts,
			// High VolumeLevel to prevent switching during test (switching is tested separately)
			VolumeLevel = decimal.MaxValue / 2,
		};

		basket.InnerSecurities.Add(riu.ToSecurityId());
		basket.InnerSecurities.Add(riz.ToSecurityId());

		Do(basket, prices => prices[0], riu, riz);
	}

	private static void CreateFut(out Security riu, out Security riz)
	{
		riu = new Security
		{
			Id = "RIU8@FORTS",
			Code = "RIU8",
			Name = "RIU8",
			PriceStep = 10m,
			Board = ExchangeBoard.Associated,
#pragma warning disable CS0618 // Type or member is obsolete
			LastTick = new ExecutionMessage { DataTypeEx = DataType.Ticks, TradePrice = 98440 },
#pragma warning restore CS0618 // Type or member is obsolete
			ExpiryDate = new DateTime(2018, 09, 15).UtcKind(),
		};

		riz = new Security
		{
			Id = "RIZ8@FORTS",
			Code = "RIZ8",
			Name = "RIZ8",
			PriceStep = 10m,
			Board = ExchangeBoard.Associated,
#pragma warning disable CS0618 // Type or member is obsolete
			LastTick = new ExecutionMessage { DataTypeEx = DataType.Ticks, TradePrice = 100440 },
#pragma warning restore CS0618 // Type or member is obsolete
			ExpiryDate = new DateTime(2018, 12, 15).UtcKind(),
		};
	}

	private static void CreateSpot(out Security lkoh, out Security sber)
	{
		lkoh = new Security
		{
			Id = "LKOH@TQBR",
			Code = "LKOH",
			Name = "LKOH",
			PriceStep = 0.01m,
			Decimals = 2,
			Board = ExchangeBoard.Associated,
#pragma warning disable CS0618 // Type or member is obsolete
			LastTick = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = 854.98m
			}
#pragma warning restore CS0618 // Type or member is obsolete
		};

		sber = new Security
		{
			Id = "SBER@TQBR",
			Code = "SBER",
			Name = "SBER",
			PriceStep = 0.01m,
			Decimals = 2,
			Board = ExchangeBoard.Associated,
#pragma warning disable CS0618 // Type or member is obsolete
			LastTick = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = 99.13m
			}
#pragma warning restore CS0618 // Type or member is obsolete
		};
	}

	private static void Do(BasketSecurity basketSecurity, Func<decimal[], decimal> validateFormula, params Security[] securities)
	{
		var processorProvider = new BasketSecurityProcessorProvider();
		var secProvider = new CollectionSecurityProvider(securities);
		securities = [.. basketSecurity.GetInnerSecurities(secProvider)];
		var legsCount = securities.Length;

		var start = securities.First().ExpiryDate?.Subtract(TimeSpan.FromHours(5)) ?? DateTime.UtcNow;

		// TODO Implement tests to support all types of messages and data

		//// Проверка глубины
		//var depths = securities.ToDictionary(s => s, s => s.RandomDepths(1000, start: start));
		//var allDepths = depths.Values.SelectMany(d => d).OrderBy(d => d.ServerTime).ToArray();
		//var basketDepths = allDepths.ToBasket(basketSecurity, processorProvider).ToArray();
		//for (int i = 0; i < basketDepths.Length; i++)
		//{
		//	var innerDepths = allDepths.Skip(i * legsCount).Take(legsCount).ToArray();
		//	//ValidateBasketResult(basketDepths[i], innerDepths, msg => msg.GetBestBid().Price ?? 0, validateFormula);
		//}

		//// Проверка ордеров
		//var ol = securities.ToDictionary(s => s, s => s.RandomOrderLog(1000, start: start));
		//var allOrders = ol.Values.SelectMany(o => o).OrderBy(o => o.ServerTime).ToArray();
		//var basketOl = allOrders.ToBasket(basketSecurity, processorProvider).ToArray();
		//for (int i = 0; i < basketOl.Length; i++)
		//{
		//	var innerOrders = allOrders.Skip(i * legsCount).Take(legsCount).ToArray();
		//	ValidateBasketResult(basketOl[i], innerOrders, msg => msg.OrderPrice, validateFormula);
		//}

		//// Проверка тиков
		var ticks = securities.ToDictionary(s => s, s => s.RandomTicks(10000, true, start: start));
		//var allTicks = ticks.Values.SelectMany(t => t).OrderBy(t => t.ServerTime).ToArray();
		//var basketTicks = allTicks.ToBasket(basketSecurity, processorProvider).ToArray();
		//for (int i = 0; i < basketTicks.Length; i++)
		//{
		//	var innerTicks = allTicks.Skip(i * legsCount).Take(legsCount).ToArray();
		//	ValidateBasketResult(basketTicks[i], innerTicks, msg => msg.TradePrice ?? 0, validateFormula);
		//}

		// Проверка свечей
		foreach (var dt in new[]
		{
			TimeSpan.FromMinutes(1).TimeFrame(),
			//100.Tick(),
			//99.9M.Volume(),
			//new Unit(100.1M).Range(),
			//new PnFArg { BoxSize = new(100.1M) }.PnF(),
			//new Unit(100.1M).Renko(),
		})
		{
			var innerCandles = securities.SelectMany(s => ticks[s].ToCandles(new Subscription(dt, s))).OrderBy(c => c.OpenTime).ToArray();
			var basketCandles = innerCandles.ToBasket(basketSecurity, processorProvider).ToArray();
			foreach (var candle in basketCandles)
			{
				var innerValues = innerCandles.Where(c => c.OpenTime == candle.OpenTime).ToArray();
				ValidateBasketResult(candle, innerValues, msg => msg.OpenPrice, validateFormula);
			}
		}
	}

	private static void ValidateBasketResult<T>(T basketValue, T[] innerValues, Func<T, decimal> getDecimal, Func<decimal[], decimal> validateFormula)
	{
		var prices = innerValues.Select(getDecimal).ToArray();
		var expected = validateFormula(prices);
		getDecimal(basketValue).AssertEqual(expected);
	}

	[TestMethod]
	public void ToBasket_Sync_EmptyCollection_ReturnsEmpty()
	{
		CreateSpot(out var lkoh, out var sber);

		var basket = new WeightedIndexSecurity
		{
			Id = "LKOH_SBER_WEI@TQBR",
			Board = ExchangeBoard.Associated,
		};
		basket.Weights[lkoh.ToSecurityId()] = 1;
		basket.Weights[sber.ToSecurityId()] = 1;

		var processorProvider = new BasketSecurityProcessorProvider();
		var messages = Array.Empty<ExecutionMessage>();

		var result = messages.ToBasket(basket, processorProvider).ToArray();

		result.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task ToBasket_Async_EmptyCollection_ReturnsEmpty()
	{
		CreateSpot(out var lkoh, out var sber);

		var basket = new WeightedIndexSecurity
		{
			Id = "LKOH_SBER_WEI@TQBR",
			Board = ExchangeBoard.Associated,
		};
		basket.Weights[lkoh.ToSecurityId()] = 1;
		basket.Weights[sber.ToSecurityId()] = 1;

		var processorProvider = new BasketSecurityProcessorProvider();
		var messages = Array.Empty<ExecutionMessage>().ToAsyncEnumerable();

		var result = await messages.ToBasket(basket, processorProvider).ToArrayAsync(CancellationToken);

		result.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ToBasket_Async_NullMessages_ThrowsArgumentNullException()
	{
		CreateSpot(out var lkoh, out var sber);

		var basket = new WeightedIndexSecurity
		{
			Id = "LKOH_SBER_WEI@TQBR",
			Board = ExchangeBoard.Associated,
		};
		basket.Weights[lkoh.ToSecurityId()] = 1;
		basket.Weights[sber.ToSecurityId()] = 1;

		var processorProvider = new BasketSecurityProcessorProvider();
		IAsyncEnumerable<ExecutionMessage> messages = null;

		ThrowsExactly<ArgumentNullException>(() => messages.ToBasket(basket, processorProvider));
	}

	[TestMethod]
	public async Task ToBasket_Sync_AndAsync_ProduceSameResults()
	{
		CreateSpot(out var lkoh, out var sber);

		var basket = new WeightedIndexSecurity
		{
			Id = "LKOH_SBER_WEI@TQBR",
			Board = ExchangeBoard.Associated,
		};
		basket.Weights[lkoh.ToSecurityId()] = 1;
		basket.Weights[sber.ToSecurityId()] = -1;

		var processorProvider = new BasketSecurityProcessorProvider();
		var serverTime = DateTime.UtcNow;

		var ticks = new ExecutionMessage[]
		{
			new()
			{
				SecurityId = lkoh.ToSecurityId(),
				DataTypeEx = DataType.Ticks,
				ServerTime = serverTime,
				TradePrice = 100,
				TradeVolume = 10,
			},
			new()
			{
				SecurityId = sber.ToSecurityId(),
				DataTypeEx = DataType.Ticks,
				ServerTime = serverTime,
				TradePrice = 50,
				TradeVolume = 20,
			},
		};

		var syncResult = ticks.ToBasket(basket, processorProvider).ToArray();
		var asyncResult = await ticks.ToAsyncEnumerable().ToBasket(basket, processorProvider).ToArrayAsync(CancellationToken);

		syncResult.Length.AssertEqual(asyncResult.Length);
	}

	[TestMethod]
	public void ToBasket_Sync_WithTicks_ProcessesMessages()
	{
		CreateSpot(out var lkoh, out var sber);

		var basket = new WeightedIndexSecurity
		{
			Id = "LKOH_SBER_WEI@TQBR",
			Board = ExchangeBoard.Associated,
		};
		basket.Weights[lkoh.ToSecurityId()] = 1;
		basket.Weights[sber.ToSecurityId()] = -1;

		var processorProvider = new BasketSecurityProcessorProvider();
		var serverTime = DateTime.UtcNow;

		var ticks = new ExecutionMessage[]
		{
			new()
			{
				SecurityId = lkoh.ToSecurityId(),
				DataTypeEx = DataType.Ticks,
				ServerTime = serverTime,
				TradePrice = 100,
				TradeVolume = 10,
			},
			new()
			{
				SecurityId = sber.ToSecurityId(),
				DataTypeEx = DataType.Ticks,
				ServerTime = serverTime,
				TradePrice = 50,
				TradeVolume = 20,
			},
		};

		var result = ticks.ToBasket(basket, processorProvider).ToArray();

		result.Length.AssertGreater(0);
	}

	[TestMethod]
	public async Task ToBasket_Async_WithTicks_ProcessesMessages()
	{
		CreateSpot(out var lkoh, out var sber);

		var basket = new WeightedIndexSecurity
		{
			Id = "LKOH_SBER_WEI@TQBR",
			Board = ExchangeBoard.Associated,
		};
		basket.Weights[lkoh.ToSecurityId()] = 1;
		basket.Weights[sber.ToSecurityId()] = -1;

		var processorProvider = new BasketSecurityProcessorProvider();
		var serverTime = DateTime.UtcNow;

		var ticks = new ExecutionMessage[]
		{
			new()
			{
				SecurityId = lkoh.ToSecurityId(),
				DataTypeEx = DataType.Ticks,
				ServerTime = serverTime,
				TradePrice = 100,
				TradeVolume = 10,
			},
			new()
			{
				SecurityId = sber.ToSecurityId(),
				DataTypeEx = DataType.Ticks,
				ServerTime = serverTime,
				TradePrice = 50,
				TradeVolume = 20,
			},
		};

		var result = await ticks.ToAsyncEnumerable().ToBasket(basket, processorProvider).ToArrayAsync(CancellationToken);

		result.Length.AssertGreater(0);
	}

	[TestMethod]
	public void BasketSecurity_MissingBasketCodeAttribute_ThrowsMeaningfulException()
	{
		// Custom basket without BasketCodeAttribute - should throw meaningful exception
		var basket = new TestBasketSecurityWithoutAttribute
		{
			Id = "TEST@TEST",
			Board = ExchangeBoard.Test,
		};

		// Should throw InvalidOperationException, not NullReferenceException
		ThrowsExactly<InvalidOperationException>(() =>
		{
			var _ = basket.BasketCode;
		});
	}

	[TestMethod]
	public void ContinuousSecurity_InvalidExpressionFormat_ThrowsMeaningfulException()
	{
		var basket = new ExpirationContinuousSecurity
		{
			Id = "RI@FORTS",
			Board = ExchangeBoard.Forts,
		};

		// Invalid format - missing '=' separator
		ThrowsExactly<InvalidOperationException>(() =>
		{
			basket.BasketExpression = "RIU8@FORTS"; // Missing "=date" part
		});
	}

	[TestMethod]
	public void ContinuousSecurity_EmptyParts_ThrowsMeaningfulException()
	{
		var basket = new ExpirationContinuousSecurity
		{
			Id = "RI@FORTS",
			Board = ExchangeBoard.Forts,
		};

		// Invalid format - only separator, no content
		ThrowsExactly<InvalidOperationException>(() =>
		{
			basket.BasketExpression = "=";
		});
	}

	// Test basket security without BasketCodeAttribute for testing
	private class TestBasketSecurityWithoutAttribute : BasketSecurity
	{
		public override IEnumerable<SecurityId> InnerSecurityIds => [];
		protected override string ToSerializedString() => string.Empty;
		protected override void FromSerializedString(string text) { }
	}

	[TestMethod]
	public void WeightedPortfolio_PositionWithMissingWeight_SkipsGracefully()
	{
		var connector = new Mock<IConnector>(MockBehavior.Loose);

		// Create portfolios
		var portfolio1 = new Portfolio { Name = "P1" };
		var portfolio2 = new Portfolio { Name = "P2" }; // This one will have no weight

		var security = new Security { Id = "SEC@TEST", Board = ExchangeBoard.Test };

		// Create positions
		var pos1 = new Position { Portfolio = portfolio1, Security = security, CurrentValue = 100 };
		var pos2 = new Position { Portfolio = portfolio2, Security = security, CurrentValue = 50 };

		connector.Setup(c => c.Positions).Returns([pos1, pos2]);

		var basket = new WeightedPortfolio(connector.Object);
		// Only add weight for portfolio1, NOT for portfolio2
		basket.Weights.Add(portfolio1, 1.0m);
		// portfolio2 is NOT added to Weights

		// Bug: Before fix, accessing InnerPositions would throw KeyNotFoundException
		// After fix, it should skip positions with missing weights gracefully
		var innerPositions = basket.InnerPositions.ToArray();

		// Should work without exception
		innerPositions.Length.AssertEqual(1); // Only one security
		// The position value should only include portfolio1's contribution (100 * 1.0 = 100)
		innerPositions[0].CurrentValue.AssertEqual(100m);
	}

	[TestMethod]
	public void WeightedPortfolio_EmptyWeights_NoDivisionByZero()
	{
		var connector = new Mock<IConnector>(MockBehavior.Loose);
		connector.Setup(c => c.Positions).Returns([]);

		var basket = new WeightedPortfolio(connector.Object);
		// Don't add any weights - Count will be 0

		// should be 0 when Count == 0
		basket.Leverage.AssertEqual(0m);
	}
}
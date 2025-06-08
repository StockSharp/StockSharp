namespace StockSharp.Tests;

[TestClass]
public class BasketSecurityTests
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
			VolumeLevel = 1000,
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
			LastTick = new ExecutionMessage { DataTypeEx = DataType.Ticks, TradePrice = 98440 },
			ExpiryDate = new DateTime(2018, 09, 15).ApplyMoscow(),
		};

		riz = new Security
		{
			Id = "RIZ8@FORTS",
			Code = "RIZ8",
			Name = "RIZ8",
			PriceStep = 10m,
			Board = ExchangeBoard.Associated,
			LastTick = new ExecutionMessage { DataTypeEx = DataType.Ticks, TradePrice = 100440 },
			ExpiryDate = new DateTime(2018, 12, 15).ApplyMoscow(),
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
			LastTick = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = 854.98m
			}
		};

		sber = new Security
		{
			Id = "SBER@TQBR",
			Code = "SBER",
			Name = "SBER",
			PriceStep = 0.01m,
			Decimals = 2,
			Board = ExchangeBoard.Associated,
			LastTick = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = 99.13m
			}
		};
	}

	private static void Do(BasketSecurity basketSecurity, Func<decimal[], decimal> validateFormula, params Security[] securities)
	{
		var processorProvider = new BasketSecurityProcessorProvider();
		var secProvider = new CollectionSecurityProvider(securities);
		securities = [.. basketSecurity.GetInnerSecurities(secProvider)];
		var legsCount = securities.Length;

		var start = securities.First().ExpiryDate?.Subtract(TimeSpan.FromHours(5)) ?? DateTimeOffset.UtcNow;

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
}
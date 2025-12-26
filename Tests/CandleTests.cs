namespace StockSharp.Tests;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Candles.Compression;

[TestClass]
public class CandleTests : BaseTestClass
{
	[TestMethod]
	public void SmallTimeFrame()
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var trades = new[]
		{
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = 11,
				TradeVolume = 11,
				SecurityId = secId,
				ServerTime = new DateTime(2000, 1, 1, 1, 1, 1, 100).UtcKind()
			},
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = 12,
				TradeVolume = 12,
				SecurityId = secId,
				ServerTime = new DateTime(2000, 1, 1, 1, 1, 1, 200).UtcKind()
			},
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = 13,
				TradeVolume = 13,
				SecurityId = secId,
				ServerTime = new DateTime(2000, 1, 1, 1, 1, 1, 300).UtcKind()
			}
		};

		var sub = new Subscription(TimeSpan.FromMilliseconds(100).TimeFrame(), security);
		var mdMsg = sub.MarketData;
		mdMsg.IsFinishedOnly = false;

		var candles = trades.ToCandles(mdMsg).ToArray();

		candles.Length.AssertEqual(3);

		var iter = 1;
		foreach (var candle in candles)
		{
			candle.TotalVolume.AssertEqual(10 + iter);
			//candle.TotalPrice.AssertEqual(10 + iter);
			candle.OpenTime.AssertEqual(trades[iter - 1].ServerTime);
			//candle.CloseTime.Millisecond.AssertEqual(100 * (iter + 1));
			candle.State.AssertEqual(iter == 3 ? CandleStates.Active : CandleStates.Finished);
			iter++;
		}
	}

	[TestMethod]
	public void NonStandardTimeFrame()
	{
		// http://stocksharp.com/forum/yaf_postsm13562_SampleHistoryTesting---vopros-po-formirovaniiu-sviechiek.aspx

		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		secId.BoardCode = BoardCodes.Forts;
		security.Id = secId.ToStringId();

		var trades = new[]
		{
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = 11,
				TradeVolume = 11,
				SecurityId = secId,
				ServerTime = new DateTime(2000, 1, 10, 9, 55, 0).UtcKind()
			},
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = 12,
				TradeVolume = 12,
				SecurityId = secId,
				ServerTime = new DateTime(2000, 1, 10, 9, 56, 0).UtcKind()
			},
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = 13,
				TradeVolume = 13,
				SecurityId = secId,
				ServerTime = new DateTime(2000, 1, 10, 10, 0, 0).UtcKind()
			}
		};

		var sub = new Subscription(TimeSpan.FromMinutes(7).TimeFrame(), security);
		var mdMsg = sub.MarketData;
		mdMsg.IsFinishedOnly = false;

		var candles = trades.ToCandles(mdMsg).ToArray();

		candles.Length.AssertEqual(1);

		var candle = candles.First();

		candle.TotalVolume.AssertEqual(11 + 12 + 13);
		//candle.TotalPrice.AssertEqual(11 + 12 + 13);
		candle.OpenTime.AssertEqual(trades.First().ServerTime);
		candle.CloseTime.AssertEqual(trades.Last().ServerTime);
	}

	[TestMethod]
	public async Task Storage()
	{
		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();
		var tf = TimeSpan.FromMinutes(1);
		var storageRegistry = Helper.GetStorage(Paths.HistoryDataPath, Helper.FileSystem);
		var token = CancellationToken;

		var loadedCandles = await storageRegistry.GetTimeFrameCandleMessageStorage(secId, tf).LoadAsync(Paths.HistoryBeginDate, Paths.HistoryBeginDate.AddDays(1)).ToArrayAsync(token);

		var sub = new Subscription(tf.TimeFrame(), new SecurityMessage { SecurityId = secId });
		var mdMsg = sub.MarketData;
		mdMsg.IsFinishedOnly = false;

		var loadedTicks = await storageRegistry.GetTickMessageStorage(secId).LoadAsync(loadedCandles.First().OpenTime, loadedCandles.Last().OpenTime + tf.AddMicroseconds(-1)).ToArrayAsync(token);
		var buildedCandles = loadedTicks.ToCandles(mdMsg).ToArray();

		buildedCandles.CompareCandles(loadedCandles, checkExtended: false);
	}

	[TestMethod]
	public void RandomNoProfile()
	{
		var security = Helper.CreateSecurity(100);

		var ticks = security.RandomTicks(100000, true).ToArray();

		var candles = GenerateCandles(ticks, security, PriceRange.Pips(security), TotalTicks, TimeFrame, VolumeRange, BoxSize, PnF(security), false);
		CheckCandles(candles, false);
	}

	[TestMethod]
	public void RandomWithProfile()
	{
		var security = Helper.CreateSecurity(100);

		var ticks = security.RandomTicks(100000, true).ToArray();

		var candles = GenerateCandles(ticks, security, PriceRange.Pips(security), TotalTicks, TimeFrame, VolumeRange, BoxSize, PnF(security), true);
		CheckCandles(candles, true);
	}

	public static readonly TimeSpan TimeFrame = TimeSpan.FromMinutes(5);
	public const int TotalTicks = 100;
	public const decimal VolumeRange = 100;
	public const decimal PriceRange = 50;
	public const decimal BoxSize = 0.2m;
	public static PnFArg PnF(Security security, decimal boxSize = BoxSize) => new() { BoxSize = (boxSize / 10).Pips(security), ReversalAmount = 3 };

	internal static List<CandleMessage> GenerateCandles(ExecutionMessage[] trades, Security security, Unit priceRange, int totalTicks, TimeSpan timeFrame, decimal volumeRange, Unit boxSize, PnFArg pnf, bool isCalcVolumeProfile, int maxCandles = int.MaxValue)
	{
		var candleList = new List<CandleMessage>();

		void Do(DataType dt)
		{
			var sub = new Subscription(dt, security);
			var mdMsg = sub.MarketData;
			mdMsg.IsCalcVolumeProfile = isCalcVolumeProfile;
			mdMsg.IsFinishedOnly = false;

			candleList.AddRange(trades.ToCandles(mdMsg).Take(maxCandles));
		}

		Do(priceRange.Range());
		Do(volumeRange.Volume());
		Do(timeFrame.TimeFrame());
		Do(totalTicks.Tick());
		Do(boxSize.Renko());
		Do(pnf.PnF());

		return candleList;
	}

	private static void CheckCandles(List<CandleMessage> candles, bool calcProfile)
	{
		candles.All(c => c.State != CandleStates.None).AssertTrue();

		var rangeCandles = candles.OfType<RangeCandleMessage>().ToArray();
		var tickCandles = candles.OfType<TickCandleMessage>().ToArray();
		var timeFrameCandles = candles.OfType<TimeFrameCandleMessage>().ToArray();
		var volumeCandles = candles.OfType<VolumeCandleMessage>().ToArray();
		var renkoCandles = candles.OfType<RenkoCandleMessage>().ToArray();
		var pnfCandles = candles.OfType<PnFCandleMessage>().ToArray();

		void CheckCandle(CandleMessage c)
		{
			(c.OpenPrice < c.LowPrice).AssertFalse();
			(c.ClosePrice < c.LowPrice).AssertFalse();
			(c.HighPrice < c.LowPrice).AssertFalse();

			(c.OpenPrice > c.HighPrice).AssertFalse();
			(c.ClosePrice > c.HighPrice).AssertFalse();
			(c.LowPrice > c.HighPrice).AssertFalse();

			(c.TotalVolume == (c.BuyVolume ?? 0) + (c.SellVolume ?? 0)).AssertTrue();

			if (calcProfile)
			{
				c.PriceLevels.AssertNotNull();
				c.PriceLevels.Any().AssertTrue();

				foreach (var lvl in c.PriceLevels)
				{
					(lvl.Price > 0).AssertTrue();
					(lvl.TotalVolume > 0).AssertTrue();
				}
			}
		}

		foreach (var candle in candles)
		{
			CheckCandle(candle);
		}

		(rangeCandles.Length > 2000).AssertTrue($"CNT(RG)={rangeCandles.Length}");
		(tickCandles.Length == 1000).AssertTrue($"CNT(TCK)={tickCandles.Length}");
		(volumeCandles.Length > 8000).AssertTrue($"CNT(VOL)={volumeCandles.Length}");
		(timeFrameCandles.Length > 100).AssertTrue($"CNT(TF)={timeFrameCandles.Length}");
		(renkoCandles.Length > 200000).AssertTrue($"CNT(RK)={renkoCandles.Length}");
		(pnfCandles.Length > 40000).AssertTrue($"CNT(PF)={pnfCandles.Length}");
	}

	[TestMethod]
	public void CandleBounds()
	{
		var date = new DateTime(2013, 5, 15).UtcKind();

		void CheckCandleBounds(TimeSpan tf, TimeSpan exchangeTimeOfDay, ExchangeBoard board, TimeSpan exchangeMin, TimeSpan exchangeMax)
		{
			var currTime = date.Add(exchangeTimeOfDay).ApplyTimeZone(board.TimeZone).UtcDateTime;
			var range = tf.GetCandleBounds(currTime, board);

			var min = date.Add(exchangeMin).ApplyTimeZone(board.TimeZone);
			var max = date.Add(exchangeMax).ApplyTimeZone(board.TimeZone);

			min.UtcDateTime.AssertEqual(range.Min);
			max.UtcDateTime.AssertEqual(range.Max);
		}

		CheckCandleBounds(TimeSpan.FromMinutes(5), new(7, 49, 33), ExchangeBoard.MicexJunior, new(7, 45, 0), new(7, 50, 0));
		CheckCandleBounds(TimeSpan.FromMinutes(5), new(14, 1, 33), ExchangeBoard.Forts, new(14, 0, 0), new(14, 5, 0));

		CheckCandleBounds(TimeSpan.FromMinutes(15), new(23, 46, 0), ExchangeBoard.Forts, new(23, 45, 0), new(23, 50, 0));

		CheckCandleBounds(TimeSpan.FromMinutes(30), new(19, 0, 0), ExchangeBoard.Forts, new(19, 0, 0), new(19, 30, 0));
		CheckCandleBounds(TimeSpan.FromMinutes(30), new(18, 3, 0), ExchangeBoard.Forts, new(18, 0, 0), new(18, 30, 0));

		CheckCandleBounds(TimeSpan.FromHours(1), new(23, 30, 0), ExchangeBoard.Forts, new(23, 00, 0), new(23, 50, 0));
		CheckCandleBounds(TimeSpan.FromHours(1), new(18, 30, 0), ExchangeBoard.Forts, new(18, 0, 0), new(18, 45, 0));

		CheckCandleBounds(TimeSpan.FromHours(2), new(18, 30, 0), ExchangeBoard.Forts, new(18, 0, 0), new(20, 00, 0));

		CheckCandleBounds(TimeSpan.FromHours(24), new(23, 30, 0), ExchangeBoard.Forts, new(10, 0, 0), new(23, 50, 0));
		CheckCandleBounds(TimeSpan.FromHours(48), new(23, 30, 0), ExchangeBoard.Forts, new(10, 0, 0), new(1, 23, 50, 0));
	}

	[TestMethod]
	public void TimeFrameCount()
	{
		var tf = TimeSpan.FromMinutes(5);

		static Range<DateTime> Create(DateTime from, DateTime to)
			=> new(from.UtcKind(), to.UtcKind());

		Create(new DateTime(2011, 1, 1, 9, 0, 0), new DateTime(2011, 1, 1, 23, 59, 59)).GetTimeFrameCount(tf, ExchangeBoard.Forts).AssertEqual(162);
		Create(new DateTime(2011, 1, 1, 9, 0, 0), new DateTime(2011, 1, 2, 23, 59, 59)).GetTimeFrameCount(tf, ExchangeBoard.Forts).AssertEqual(162 * 2);
		Create(new DateTime(2011, 1, 1, 23, 0, 0), new DateTime(2011, 1, 1, 23, 59, 59)).GetTimeFrameCount(tf, ExchangeBoard.Forts).AssertEqual(10);
		Create(new DateTime(2011, 1, 1, 23, 0, 0), new DateTime(2011, 1, 2, 9, 0, 0)).GetTimeFrameCount(tf, ExchangeBoard.Forts).AssertEqual(10);
		Create(new DateTime(2011, 1, 1, 23, 0, 0), new DateTime(2011, 1, 2, 10, 0, 0)).GetTimeFrameCount(tf, ExchangeBoard.Forts).AssertEqual(10);
		Create(new DateTime(2011, 1, 1, 23, 0, 0), new DateTime(2011, 1, 2, 10, 5, 0)).GetTimeFrameCount(tf, ExchangeBoard.Forts).AssertEqual(11);

		var testBoard = new ExchangeBoard
		{
			WorkingTime =
			{
				Periods =
				[
					new WorkingTimePeriod
					{
						Till = DateTime.MaxValue,
						Times =
						[
							new Range<TimeSpan>(new TimeSpan(10, 0, 0), new TimeSpan(10, 3, 0)),
							new Range<TimeSpan>(new TimeSpan(10, 5, 0), new TimeSpan(10, 7, 0))
						]
					}
				]
			}
		};

		Create(new DateTime(2011, 1, 1, 9, 0, 0), new DateTime(2011, 1, 1, 23, 59, 59)).GetTimeFrameCount(tf, testBoard).AssertEqual(0);
	}

	[TestMethod]
	public void BiggerTimeFrameCandleCompressor()
	{
		var candleBuilderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());
		var tf1 = TimeSpan.FromMinutes(1);
		var tf5 = TimeSpan.FromMinutes(5);

		static void CompareMessages(TimeFrameCandleMessage m1, TimeFrameCandleMessage m2)
		{
			if(ReferenceEquals(m1, m2))
				return;

			var endTime = m1.OpenTime + m1.TypedArg;

			m1.DataType.Equals(m2.DataType).AssertTrue();
			(m1.OpenTime          == m2.OpenTime).AssertTrue();
			(m1.CloseTime         < endTime).AssertTrue();
			(m2.CloseTime         < endTime).AssertTrue();
			(m1.OpenPrice         == m2.OpenPrice).AssertTrue();
			(m1.HighPrice         == m2.HighPrice).AssertTrue();
			(m1.LowPrice          == m2.LowPrice).AssertTrue();
			(m1.ClosePrice        == m2.ClosePrice).AssertTrue();
		}

		var security = Helper.CreateSecurity(100);
		var ticks = security.RandomTicks(1000000, true, interval: TimeSpan.FromMilliseconds(190)).ToArray();

		var sub1 = new Subscription(tf1.TimeFrame(), security);
		var mdMsg1 = sub1.MarketData;
		mdMsg1.IsFinishedOnly = false;

		var sub5 = new Subscription(tf5.TimeFrame(), security);
		var mdMsg5 = sub5.MarketData;
		mdMsg5.IsFinishedOnly = false;
		mdMsg5.AllowBuildFromSmallerTimeFrame = true;

		var candles1FromTicks = new List<CandleMessage>();
		var candles5FromTicks = new List<CandleMessage>();

		candles1FromTicks.AddRange(ticks.ToCandles(mdMsg1));
		candles5FromTicks.AddRange(ticks.ToCandles(mdMsg5));

		var compressor = new BiggerTimeFrameCandleCompressor(mdMsg5, candleBuilderProvider.Get(typeof(TimeFrameCandleMessage)), mdMsg1.DataType2);

		CandleMessage prevMessage5 = null;
		var c5FromTicksIndex = 0;
		var openPrice = candles1FromTicks[0].OpenPrice;
		var vol = 0m;

		foreach (var c1 in candles1FromTicks)
		{
			var messages = compressor.Process(c1);
			foreach (var c5 in messages)
			{
				if(prevMessage5 == null)
					prevMessage5 = c5;
				else
					CompareMessages((TimeFrameCandleMessage)prevMessage5, (TimeFrameCandleMessage)c5);

				if (c5.State == CandleStates.Finished)
				{
					CompareMessages((TimeFrameCandleMessage)c5, (TimeFrameCandleMessage)candles5FromTicks[c5FromTicksIndex++]);
					prevMessage5 = null;
					openPrice = c1.OpenPrice;
					vol = 0;
					continue;
				}

				vol += c1.TotalVolume;

				(c1.HighPrice   <= c5.HighPrice).AssertTrue();
				(c1.LowPrice    >= c5.LowPrice).AssertTrue();
				(c1.OpenPrice   <= c5.HighPrice && c1.OpenPrice >= c5.LowPrice).AssertTrue();
				(c1.ClosePrice  == c5.ClosePrice).AssertTrue();
				(c5.OpenPrice   == openPrice).AssertTrue();
				(c5.TotalVolume == vol).AssertTrue();

				(c1.OpenTime >= c5.OpenTime && c1.CloseTime < c5.OpenTime + tf5).AssertTrue();
			}
		}

		(c5FromTicksIndex == candles5FromTicks.Count - 1).AssertTrue();
		(prevMessage5?.State == CandleStates.Active).AssertTrue();
		CompareMessages((TimeFrameCandleMessage)prevMessage5, (TimeFrameCandleMessage)candles5FromTicks.Last());
	}

	[TestMethod]
	public void TotalPrice()
	{
		var provider = new InMemoryExchangeInfoProvider();
		var builders = new CandleBuilderProvider(provider);
		var builder = (TimeFrameCandleBuilder)builders.Get(typeof(TimeFrameCandleMessage));

		var md = new MarketDataMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
		};

		var sub = new CandleBuilderSubscription(md);
		var transform = new TickCandleBuilderValueTransform();

		var now = DateTime.UtcNow;

		var tick1 = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = md.SecurityId,
			ServerTime = now,
			TradePrice = 100m,
			TradeVolume = 1m,
			OriginSide = Sides.Buy
		};

		transform.Process(tick1);
		var candle1 = builder.Process(sub, transform).Cast<TimeFrameCandleMessage>().Single();

		var tick2 = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = md.SecurityId,
			ServerTime = now.AddSeconds(1),
			TradePrice = 105m,
			TradeVolume = 2m,
			OriginSide = Sides.Sell
		};

		transform.Process(tick2);
		var candle2 = builder.Process(sub, transform).Cast<TimeFrameCandleMessage>().Single();

		candle1.AssertSame(candle2);

		var tick3 = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = md.SecurityId,
			ServerTime = now.AddMinutes(1),
			TradePrice = 106m,
			TradeVolume = 1m,
			OriginSide = Sides.Buy
		};

		transform.Process(tick3);
		var candles = builder.Process(sub, transform).Cast<TimeFrameCandleMessage>().ToArray();
		
		candles.Length.AssertEqual(2);

		var finished = candles.First();

		candle1.AssertSame(finished);

		finished.TotalPrice.AssertEqual(310m);
	}

	[TestMethod]
	public void PriceLevels()
	{
		var provider = new InMemoryExchangeInfoProvider();
		var builders = new CandleBuilderProvider(provider);
		var builder = (TimeFrameCandleBuilder)builders.Get(typeof(TimeFrameCandleMessage));

		var md = new MarketDataMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = TimeSpan.FromMinutes(2).TimeFrame(),
			IsCalcVolumeProfile = true
		};

		var compressor = new BiggerTimeFrameCandleCompressor(md, builder, TimeSpan.FromMinutes(1).TimeFrame());

		var candle1 = new TimeFrameCandleMessage
		{
			OpenTime = new DateTime(2020, 1, 1, 0, 0, 0).UtcKind(),
			CloseTime = new DateTime(2020, 1, 1, 0, 1, 0).UtcKind(),
			HighTime = new DateTime(2020, 1, 1, 0, 0, 0).UtcKind(),
			LowTime = new DateTime(2020, 1, 1, 0, 0, 0).UtcKind(),
			OpenPrice = 100m,
			HighPrice = 101m,
			LowPrice = 100m,
			ClosePrice = 100m,
			TotalVolume = 1m,
		};

		var bigCandle = compressor.Process(candle1).Cast<TimeFrameCandleMessage>().Single();
		bigCandle.AssertNotNull();

		var lvlBig = bigCandle.PriceLevels.ToArray();
		lvlBig.Length.AreEqual(2);

		var lvlBig1 = lvlBig[0];
		lvlBig1.Price.AreEqual(100m);
		lvlBig1.BuyVolume.AreEqual(0m);
		lvlBig1.BuyCount.AreEqual(0);
		lvlBig1.TotalVolume.AreEqual(1m);

		var lvlBig2 = lvlBig[1];
		lvlBig2.Price.AreEqual(101m);
		lvlBig2.BuyVolume.AreEqual(0m);
		lvlBig2.BuyCount.AreEqual(0);
		lvlBig2.TotalVolume.AreEqual(0m);

		var candle2 = new TimeFrameCandleMessage
		{
			OpenTime = new DateTime(2020, 1, 1, 0, 1, 0).UtcKind(),
			CloseTime = new DateTime(2020, 1, 1, 0, 2, 0).UtcKind(),
			HighTime = new DateTime(2020, 1, 1, 0, 1, 0).UtcKind(),
			LowTime = new DateTime(2020, 1, 1, 0, 1, 0).UtcKind(),
			OpenPrice = 102m,
			HighPrice = 103m,
			LowPrice = 102m,
			ClosePrice = 103m,
			TotalVolume = 2m,
		};

		var results = compressor.Process(candle2).Cast<TimeFrameCandleMessage>().ToArray();
		var big = results.Single();
		big.PriceLevels.Count().AssertEqual(4);
	}

	[TestMethod]
	public void VolumeProfile_CreatesAndUpdatesLevel()
	{
		var builder = new VolumeProfileBuilder();

		builder.Update(100m, 1m, Sides.Buy);

		var level1 = builder.PriceLevels.Single();
		level1.Price.AreEqual(100m);
		level1.BuyVolume.AreEqual(1m);
		level1.BuyCount.AreEqual(1);
		level1.SellVolume.AreEqual(0m);
		level1.SellCount.AreEqual(0);
		level1.TotalVolume.AreEqual(1m);

		builder.Update(100m, 2m, Sides.Sell);

		var level2 = builder.PriceLevels.Single();
		level2.Price.AreEqual(100m);
		level2.BuyVolume.AreEqual(1m);
		level2.BuyCount.AreEqual(1);
		level2.SellVolume.AreEqual(2m);
		level2.SellCount.AreEqual(1);
		level2.TotalVolume.AreEqual(3m);
	}

	[TestMethod]
	public void VolumeProfile_MergesVolumes()
	{
		var builder = new VolumeProfileBuilder();

		builder.Update(new CandlePriceLevel
		{
			Price = 101m,
			BuyVolume = 1m,
			BuyCount = 1,
			BuyVolumes = [1m]
		});

		var afterBuy = builder.PriceLevels.Single();
		afterBuy.Price.AreEqual(101m);
		afterBuy.BuyVolume.AreEqual(1m);
		afterBuy.BuyCount.AreEqual(1);
		afterBuy.SellVolume.AreEqual(0m);
		afterBuy.SellCount.AreEqual(0);
		afterBuy.TotalVolume.AreEqual(1m);

		builder.Update(new CandlePriceLevel
		{
			Price = 101m,
			SellVolume = 2m,
			SellCount = 1,
			SellVolumes = [2m]
		});

		var afterSell = builder.PriceLevels.Single();
		afterSell.Price.AreEqual(101m);
		afterSell.BuyVolume.AreEqual(1m);
		afterSell.BuyCount.AreEqual(1);
		afterSell.SellVolume.AreEqual(2m);
		afterSell.SellCount.AreEqual(1);
		afterSell.TotalVolume.AreEqual(3m);
		afterSell.BuyVolumes.ToArray().AssertEqual([1m]);
		afterSell.SellVolumes.ToArray().AssertEqual([2m]);
	}

	[TestMethod]
	public void VolumeProfile_SetsPocAndArea()
	{
		var levels = new List<CandlePriceLevel>
		{
			new() { Price = 99m, TotalVolume = 1m, BuyVolume = 0.5m, SellVolume = 0.5m },
			new() { Price = 100m, TotalVolume = 3m, BuyVolume = 2m, SellVolume = 1m },
			new() { Price = 101m, TotalVolume = 2m, BuyVolume = 1m, SellVolume = 1m }
		};

		var builder = new VolumeProfileBuilder();

		foreach (var level in levels)
			builder.Update(level);

		builder.Calculate();

		var levelsArr = builder.PriceLevels.OrderBy(l => l.Price).ToArray();
		levelsArr[0].Price.AreEqual(99m);
		levelsArr[0].TotalVolume.AreEqual(1m);
		levelsArr[1].Price.AreEqual(100m);
		levelsArr[1].TotalVolume.AreEqual(3m);
		levelsArr[2].Price.AreEqual(101m);
		levelsArr[2].TotalVolume.AreEqual(2m);

		builder.PoC.Price.AreEqual(100m);
		builder.High.Price.AreEqual(101m);
		builder.Low.Price.AreEqual(99m);
	}

	[TestMethod]
	public void VolumeProfile_MultiLevel()
	{
		var levels = new List<CandlePriceLevel>
		{
			new() { Price = 10m, TotalVolume = 2m, BuyVolume = 1m, SellVolume = 1m },
			new() { Price = 11m, TotalVolume = 5m, BuyVolume = 3m, SellVolume = 2m },
			new() { Price = 12m, TotalVolume = 8m, BuyVolume = 4m, SellVolume = 4m, BuyCount = 1, SellCount = 1 },
			new() { Price = 13m, TotalVolume = 3m, BuyVolume = 2m, SellVolume = 1m },
			new() { Price = 14m, TotalVolume = 1m, BuyVolume = 0.5m, SellVolume = 0.5m }
		};

		var builder = new VolumeProfileBuilder();

		foreach (var level in levels)
		{
			builder.Update(level);

			var l = builder.PriceLevels.First(x => x.Price == level.Price);
			l.Price.AreEqual(level.Price);
			l.BuyVolume.AreEqual(level.BuyVolume);
			l.SellVolume.AreEqual(level.SellVolume);
			l.TotalVolume.AreEqual(level.TotalVolume);
		}

		builder.Calculate();

		var arr = builder.PriceLevels.OrderBy(x => x.Price).ToArray();
		arr[0].Price.AreEqual(10m);
		arr[1].Price.AreEqual(11m);
		arr[2].Price.AreEqual(12m);
		arr[3].Price.AreEqual(13m);
		arr[4].Price.AreEqual(14m);

		builder.PoC.Price.AreEqual(12m);

		(builder.High.Price > builder.PoC.Price || builder.High.Price == builder.PoC.Price).AssertTrue();
		(builder.Low.Price < builder.PoC.Price || builder.Low.Price == builder.PoC.Price).AssertTrue();

		builder.Update(15m, 2m, Sides.Buy);
		builder.PriceLevels.Any(l => l.Price == 15m).AssertTrue();
		var lvl15 = builder.PriceLevels.First(l => l.Price == 15m);
		lvl15.BuyVolume.AreEqual(2m);
		lvl15.TotalVolume.AreEqual(2m);
		lvl15.BuyCount.AreEqual(1);
		lvl15.SellCount.AreEqual(0);

		var update = new CandlePriceLevel { Price = 12m, BuyVolume = 1m, SellVolume = 2m, BuyCount = 1, SellCount = 1, TotalVolume = 3m };
		builder.Update(update);
		var lvl12 = builder.PriceLevels.First(l => l.Price == 12m);
		lvl12.BuyVolume.AreEqual(5m); // 4+1
		lvl12.SellVolume.AreEqual(6m); // 4+2
		lvl12.TotalVolume.AreEqual(8m+3m); // 8+3
		lvl12.BuyCount.AreEqual(2); // +1
		lvl12.SellCount.AreEqual(2); // +1

		builder.VolumePercent = 90;
		builder.Calculate();
		builder.High.AssertEqual(default);
		builder.Low.AssertEqual(default);
	}

	[TestMethod]
	public void VolumeProfile_ResetsHighLowOnRecalculate()
	{
		var levels = new List<CandlePriceLevel>
		{
			new() { Price = 10m, TotalVolume = 2m, BuyVolume = 1m, SellVolume = 1m },
			new() { Price = 11m, TotalVolume = 5m, BuyVolume = 3m, SellVolume = 2m },
			new() { Price = 12m, TotalVolume = 8m, BuyVolume = 4m, SellVolume = 4m },
			new() { Price = 13m, TotalVolume = 3m, BuyVolume = 2m, SellVolume = 1m },
			new() { Price = 14m, TotalVolume = 1m, BuyVolume = 0.5m, SellVolume = 0.5m },
		};

		var builder = new VolumeProfileBuilder();
		foreach (var level in levels)
			builder.Update(level);

		builder.Calculate();

		builder.High.AssertNotEqual(default);
		builder.Low.AssertNotEqual(default);

		builder.VolumePercent = 100m;
		builder.Calculate();

		builder.High.AssertEqual(default);
		builder.Low.AssertEqual(default);
	}

	[TestMethod]
	public void VolumeProfile_StopsAtThreshold_OnlyBelow()
	{
		var builder = new VolumeProfileBuilder();

		// PoC at 100 with volume 50
		builder.Update(new CandlePriceLevel { Price = 100m, BuyVolume = 25m, SellVolume = 25m });
		// below PoC levels: 99 (10), 97 (15) will be combined into a single node (25) with price 97
		builder.Update(new CandlePriceLevel { Price = 99m, BuyVolume = 10m });
		builder.Update(new CandlePriceLevel { Price = 97m, BuyVolume = 15m });
		// another below level, which should NOT be included once threshold reached
		builder.Update(new CandlePriceLevel { Price = 95m, BuyVolume = 5m });

		// total sum = 80, threshold = 56; first combined node already exceeds threshold
		builder.Calculate();

		builder.High.Price.AssertEqual(100m);
		builder.Low.Price.AssertEqual(97m);
	}

	[TestMethod]
	public void VolumeProfile_StopsAtThreshold_OnlyAbove()
	{
		var builder = new VolumeProfileBuilder();

		// PoC at 100 with volume 50
		builder.Update(new CandlePriceLevel { Price = 100m, BuyVolume = 50m });
		// above PoC levels: 101 (20), 102 (10) will be combined into a single node (30) with price 102
		builder.Update(new CandlePriceLevel { Price = 101m, BuyVolume = 20m });
		builder.Update(new CandlePriceLevel { Price = 102m, BuyVolume = 10m });
		// another above level, which should NOT be selected once threshold reached
		builder.Update(new CandlePriceLevel { Price = 103m, BuyVolume = 1m });

		// total sum = 81, threshold = 56; first combined node already exceeds threshold
		builder.Calculate();

		builder.Low.Price.AssertEqual(100m);
		builder.High.Price.AssertEqual(102m);
	}

	[TestMethod]
	public void VolumeProfile_OnlyBelow()
	{
		var builder = new VolumeProfileBuilder();

		// Make PoC the maximum volume
		builder.Update(new() { Price = 100m, BuyVolume = 1500m });
		// Below-only side with two combined nodes each exceeding the remaining threshold part
		builder.Update(new() { Price = 99m, BuyVolume = 400m });
		builder.Update(new() { Price = 98m, BuyVolume = 400m });
		builder.Update(new() { Price = 97m, BuyVolume = 400m });
		builder.Update(new() { Price = 96m, BuyVolume = 400m });

		// Total = 1500 + 400 + 400 + 400 + 400 = 3100
		// Threshold = round(3100 * 0.7) = 2170; currVolume = 1500; need > 670
		// Combined nodes below: (99+98)=800 at 98, (97+96)=800 at 96 -> both exceed
		builder.Calculate();

		builder.High.Price.AssertEqual(100m);
		builder.Low.Price.AssertEqual(98m);
	}

	[TestMethod]
	public void VolumeProfile_OnlyAbove()
	{
		var builder = new VolumeProfileBuilder();

		// Make PoC the maximum volume
		builder.Update(new() { Price = 100m, BuyVolume = 1500m });
		// Above-only side with two combined nodes each exceeding the remaining threshold part
		builder.Update(new() { Price = 101m, BuyVolume = 400m });
		builder.Update(new() { Price = 102m, BuyVolume = 400m });
		builder.Update(new() { Price = 103m, BuyVolume = 400m });
		builder.Update(new() { Price = 104m, BuyVolume = 400m });

		// Total = 3100; threshold ~ 2170; remaining > 670; combined nodes: (101+102)=800 at 102, (103+104)=800 at 104
		builder.Calculate();

		builder.Low.Price.AssertEqual(100m);
		builder.High.Price.AssertEqual(102m);
	}

	[TestMethod]
	public void VolumeProfile_CombineAboveLevels()
	{
		var builder = new VolumeProfileBuilder();

		// Simple case with PoC and two levels above
		builder.Update(new() { Price = 100m, BuyVolume = 1000m }); // PoC
		builder.Update(new() { Price = 101m, BuyVolume = 10m });
		builder.Update(new() { Price = 102m, BuyVolume = 10m });

		// Set low threshold so combined level definitely exceeds it
		builder.VolumePercent = 50; // 50% of 1020 = 510, already exceeded by PoC (1000)

		builder.Calculate();

		// Expect combined levels (101+102) to give price 102 (farther from PoC)
		builder.High.Price.AssertEqual(102m);
		builder.Low.Price.AssertEqual(100m);
	}

	[TestMethod]
	public void VolumeProfile_CombineBelowLevels()
	{
		var builder = new VolumeProfileBuilder();

		// Simple case with PoC and two levels below
		builder.Update(new() { Price = 100m, BuyVolume = 1000m }); // PoC
		builder.Update(new() { Price = 99m, BuyVolume = 10m });
		builder.Update(new() { Price = 98m, BuyVolume = 10m });

		// Set low threshold so combined level definitely exceeds it
		builder.VolumePercent = 50; // 50% of 1020 = 510, already exceeded by PoC (1000)

		builder.Calculate();

		// Expect combined levels (99+98) to give price 98 (closer to PoC from sorted pair)
		builder.Low.Price.AssertEqual(98m);
		builder.High.Price.AssertEqual(100m);
	}

	[TestMethod]
	public void VolumeProfile_MultipleAbovePairs()
	{
		var builder = new VolumeProfileBuilder();

		// PoC + four levels above (form two pairs after combining)
		builder.Update(new() { Price = 100m, BuyVolume = 50m }); // PoC
		builder.Update(new() { Price = 101m, BuyVolume = 5m });
		builder.Update(new() { Price = 102m, BuyVolume = 5m }); // First pair: volume = 10
		builder.Update(new() { Price = 103m, BuyVolume = 20m });
		builder.Update(new() { Price = 104m, BuyVolume = 20m }); // Second pair: volume = 40

		// Total = 100, threshold = 70, currVolume after PoC = 50
		// First pair (10) doesn't exceed: 50 + 10 = 60 < 70
		// Second pair (40) exceeds: 60 + 40 = 100 > 70
		builder.VolumePercent = 70;

		builder.Calculate();

		// Should stop at second pair (103+104) with price 104 (farther from PoC)
		builder.High.Price.AssertEqual(104m);
		builder.Low.Price.AssertEqual(100m);
	}

	[TestMethod]
	public void VolumeProfile_MultipleBelowPairs()
	{
		var builder = new VolumeProfileBuilder();

		// PoC + four levels below (form two pairs after combining)
		builder.Update(new() { Price = 100m, BuyVolume = 50m }); // PoC
		builder.Update(new() { Price = 99m, BuyVolume = 5m });
		builder.Update(new() { Price = 98m, BuyVolume = 5m }); // First pair: volume = 10
		builder.Update(new() { Price = 97m, BuyVolume = 20m });
		builder.Update(new() { Price = 96m, BuyVolume = 20m }); // Second pair: volume = 40

		// Total = 100, threshold = 70, currVolume after PoC = 50
		// First pair (10) doesn't exceed: 50 + 10 = 60 < 70
		// Second pair (40) exceeds: 60 + 40 = 100 > 70
		builder.VolumePercent = 70;

		builder.Calculate();

		// Should stop at second pair (97+96) with price 96 (closer to PoC from sorted pair)
		builder.Low.Price.AssertEqual(96m);
		builder.High.Price.AssertEqual(100m);
	}

	[TestMethod]
	public void VolumeProfile_EmptyBuilder_CalculateDoesNotCrash()
	{
		// Test that Calculate() on empty builder doesn't crash with InvalidOperationException
		var builder = new VolumeProfileBuilder();

		// Should not throw InvalidOperationException from Max() on empty sequence
		builder.Calculate();

		// Should return default values
		builder.PoC.Price.AssertEqual(0m);
		builder.High.Price.AssertEqual(0m);
		builder.Low.Price.AssertEqual(0m);
	}

	[TestMethod]
	public void CandleHelper_PoC_EmptyCollection_ReturnsDefault()
	{
		// Test that PoC() on empty collection doesn't crash with InvalidOperationException
		var builder = new VolumeProfileBuilder();

		// Should not throw InvalidOperationException from Max() on empty sequence
		var poc = builder.PoC();

		// Should return default value
		poc.Price.AssertEqual(0m);
		poc.TotalVolume.AssertEqual(0m);
	}

	[TestMethod]
	public void CandleHelper_PriceLevelOfMaxDelta_EmptyCollection_ReturnsDefault()
	{
		// Test that PriceLevelOfMaxDelta() on empty collection doesn't crash
		var builder = new VolumeProfileBuilder();

		// Should not throw InvalidOperationException from Max() on empty sequence
		var level = builder.PriceLevelOfMaxDelta();

		// Should return default value
		level.Price.AssertEqual(0m);
		level.TotalVolume.AssertEqual(0m);
	}

	[TestMethod]
	public void CandleHelper_PriceLevelOfMinDelta_EmptyCollection_ReturnsDefault()
	{
		// Test that PriceLevelOfMinDelta() on empty collection doesn't crash
		var builder = new VolumeProfileBuilder();

		// Should not throw InvalidOperationException from Min() on empty sequence
		var level = builder.PriceLevelOfMinDelta();

		// Should return default value
		level.Price.AssertEqual(0m);
		level.TotalVolume.AssertEqual(0m);
	}

	[TestMethod]
	public void CandleHelper_BuyVolAbovePoC_EmptyCollection_ReturnsZero()
	{
		// Test that BuyVolAbovePoC() handles empty collection correctly
		var builder = new VolumeProfileBuilder();

		// Should return 0 instead of throwing or calculating with invalid PoC
		var vol = builder.BuyVolAbovePoC();

		vol.AssertEqual(0m);
	}

	[TestMethod]
	public void CandleHelper_BuyVolBelowPoC_EmptyCollection_ReturnsZero()
	{
		// Test that BuyVolBelowPoC() handles empty collection correctly
		var builder = new VolumeProfileBuilder();

		// Should return 0 instead of throwing or calculating with invalid PoC
		var vol = builder.BuyVolBelowPoC();

		vol.AssertEqual(0m);
	}

	[TestMethod]
	public void CandleHelper_SellVolAbovePoC_EmptyCollection_ReturnsZero()
	{
		// Test that SellVolAbovePoC() handles empty collection correctly
		var builder = new VolumeProfileBuilder();

		// Should return 0 instead of throwing or calculating with invalid PoC
		var vol = builder.SellVolAbovePoC();

		vol.AssertEqual(0m);
	}

	[TestMethod]
	public void CandleHelper_SellVolBelowPoC_EmptyCollection_ReturnsZero()
	{
		// Test that SellVolBelowPoC() handles empty collection correctly
		var builder = new VolumeProfileBuilder();

		// Should return 0 instead of throwing or calculating with invalid PoC
		var vol = builder.SellVolBelowPoC();

		vol.AssertEqual(0m);
	}

	[TestMethod]
	public void CandleHelper_TotalBuyVolume_EmptyCollection_ReturnsZero()
	{
		// Test that TotalBuyVolume() handles empty collection correctly
		var builder = new VolumeProfileBuilder();

		var vol = builder.TotalBuyVolume();

		vol.AssertEqual(0m);
	}

	[TestMethod]
	public void CandleHelper_TotalSellVolume_EmptyCollection_ReturnsZero()
	{
		// Test that TotalSellVolume() handles empty collection correctly
		var builder = new VolumeProfileBuilder();

		var vol = builder.TotalSellVolume();

		vol.AssertEqual(0m);
	}

	[TestMethod]
	public void CandleHelper_Delta_EmptyCollection_ReturnsZero()
	{
		// Test that Delta() handles empty collection correctly
		var builder = new VolumeProfileBuilder();

		var delta = builder.Delta();

		delta.AssertEqual(0m);
	}

	[TestMethod]
	public void CandleHelper_PoC_WithData_ReturnsMaxVolumeLevel()
	{
		// Test that PoC() correctly returns the level with maximum volume
		var builder = new VolumeProfileBuilder();

		builder.Update(100m, 10m, Sides.Buy);
		builder.Update(101m, 50m, Sides.Buy);  // This should be PoC
		builder.Update(102m, 20m, Sides.Sell);

		var poc = builder.PoC();

		poc.Price.AssertEqual(101m);
		(poc.BuyVolume + poc.SellVolume).AssertEqual(50m);
	}

	[TestMethod]
	public void CandleHelper_PriceLevelOfMaxDelta_WithData_ReturnsCorrectLevel()
	{
		// Test that PriceLevelOfMaxDelta() correctly finds maximum buy-sell delta
		var builder = new VolumeProfileBuilder();

		builder.Update(100m, 10m, Sides.Buy);   // delta = +10
		builder.Update(101m, 50m, Sides.Buy);   // delta = +50 (MAX)
		builder.Update(102m, 20m, Sides.Sell);  // delta = -20

		var level = builder.PriceLevelOfMaxDelta();

		level.Price.AssertEqual(101m);
		(level.BuyVolume - level.SellVolume).AssertEqual(50m);
	}

	[TestMethod]
	public void CandleHelper_PriceLevelOfMinDelta_WithData_ReturnsCorrectLevel()
	{
		// Test that PriceLevelOfMinDelta() correctly finds minimum buy-sell delta
		var builder = new VolumeProfileBuilder();

		builder.Update(100m, 10m, Sides.Buy);   // delta = +10
		builder.Update(101m, 5m, Sides.Sell);   // delta = -5
		builder.Update(102m, 30m, Sides.Sell);  // delta = -30 (MIN)

		var level = builder.PriceLevelOfMinDelta();

		level.Price.AssertEqual(102m);
		(level.BuyVolume - level.SellVolume).AssertEqual(-30m);
	}

	[TestMethod]
	public void CandleHelper_VolumesAboveBelowPoC_WithData_CalculatesCorrectly()
	{
		// Test that volume calculations above/below PoC work correctly
		var builder = new VolumeProfileBuilder();

		builder.Update(98m, 5m, Sides.Buy);     // Below PoC
		builder.Update(99m, 10m, Sides.Sell);   // Below PoC
		builder.Update(100m, 100m, Sides.Buy);  // This is PoC (max volume)
		builder.Update(101m, 15m, Sides.Buy);   // Above PoC
		builder.Update(102m, 20m, Sides.Sell);  // Above PoC

		// Verify PoC is correct
		var poc = builder.PoC();
		poc.Price.AssertEqual(100m);

		// Test volumes above PoC
		builder.BuyVolAbovePoC().AssertEqual(15m);
		builder.SellVolAbovePoC().AssertEqual(20m);
		builder.VolumeAbovePoC().AssertEqual(35m);

		// Test volumes below PoC
		builder.BuyVolBelowPoC().AssertEqual(5m);
		builder.SellVolBelowPoC().AssertEqual(10m);
		builder.VolumeBelowPoC().AssertEqual(15m);

		// Test delta calculations
		builder.DeltaAbovePoC().AssertEqual(-5m);  // 15 - 20
		builder.DeltaBelowPoC().AssertEqual(-5m);  // 5 - 10
	}

	[TestMethod]
	public void CandleBounds_WeekCrossMonth()
	{
		var board = ExchangeBoard.Forts;
		var current = new DateTime(2024, 4, 29, 12, 0, 0).UtcKind(); // Monday29 Apr2024
		var bounds = TimeSpan.FromDays(7).GetCandleBounds(current, board);
		bounds.Min.AssertEqual(new DateTime(2024, 4, 29).ApplyTimeZone(board.TimeZone).UtcDateTime);
		bounds.Max.AssertEqual(new DateTime(2024, 5, 5).EndOfDay().ApplyTimeZone(board.TimeZone).UtcDateTime);
		(bounds.Max.DayOfWeek == DayOfWeek.Sunday).AssertTrue();
	}

	[TestMethod]
	public void CandleBounds_WeekOnSunday()
	{
		var board = ExchangeBoard.Forts;
		var current = new DateTime(2024, 5, 5, 10, 0, 0).UtcKind(); // Sunday
		var bounds = TimeSpan.FromDays(7).GetCandleBounds(current, board);
		bounds.Min.AssertEqual(new DateTime(2024, 4, 29).ApplyTimeZone(board.TimeZone).UtcDateTime);
		bounds.Max.AssertEqual(new DateTime(2024, 5, 5).EndOfDay().ApplyTimeZone(board.TimeZone).UtcDateTime);
		(bounds.Max.DayOfWeek == DayOfWeek.Sunday).AssertTrue();
	}

	[TestMethod]
	public void CandleBounds_Month()
	{
		var board = ExchangeBoard.Forts;
		var current = new DateTime(2024, 4, 15, 8, 0, 0).UtcKind();
		var monthTf = new TimeSpan(TimeHelper.TicksPerMonth);
		var bounds = monthTf.GetCandleBounds(current, board);
		bounds.Min.AssertEqual(new DateTime(2024, 4, 1).ApplyTimeZone(board.TimeZone).UtcDateTime);
		bounds.Max.AssertEqual(new DateTime(2024, 4, 30).EndOfDay().ApplyTimeZone(board.TimeZone).UtcDateTime);
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
	public async Task ToCandles_FromTicks_SyncVsAsync()
	{
		var token = CancellationToken;
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var trades = new[]
		{
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = 100,
				TradeVolume = 10,
				SecurityId = secId,
				ServerTime = new DateTime(2000, 1, 1, 10, 0, 0, 0).UtcKind()
			},
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = 101,
				TradeVolume = 15,
				SecurityId = secId,
				ServerTime = new DateTime(2000, 1, 1, 10, 0, 30, 0).UtcKind()
			},
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = 99,
				TradeVolume = 20,
				SecurityId = secId,
				ServerTime = new DateTime(2000, 1, 1, 10, 1, 0, 0).UtcKind()
			},
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = 102,
				TradeVolume = 25,
				SecurityId = secId,
				ServerTime = new DateTime(2000, 1, 1, 10, 1, 30, 0).UtcKind()
			}
		};

		var sub = new Subscription(TimeSpan.FromMinutes(1).TimeFrame(), security);
		var mdMsg = sub.MarketData;
		mdMsg.IsFinishedOnly = false;

		var syncResult = trades.ToCandles(mdMsg).ToArray();
		var asyncResult = await ToAsyncEnumerable(trades).ToCandles(mdMsg).ToArrayAsync(token);

		syncResult.Length.AssertEqual(asyncResult.Length);
		for (var i = 0; i < syncResult.Length; i++)
		{
			syncResult[i].SecurityId.AssertEqual(asyncResult[i].SecurityId);
			syncResult[i].OpenTime.AssertEqual(asyncResult[i].OpenTime);
			syncResult[i].CloseTime.AssertEqual(asyncResult[i].CloseTime);
			syncResult[i].OpenPrice.AssertEqual(asyncResult[i].OpenPrice);
			syncResult[i].HighPrice.AssertEqual(asyncResult[i].HighPrice);
			syncResult[i].LowPrice.AssertEqual(asyncResult[i].LowPrice);
			syncResult[i].ClosePrice.AssertEqual(asyncResult[i].ClosePrice);
			syncResult[i].TotalVolume.AssertEqual(asyncResult[i].TotalVolume);
			syncResult[i].State.AssertEqual(asyncResult[i].State);
		}
	}

	[TestMethod]
	public async Task ToTrades_FromCandles_SyncVsAsync()
	{
		var token = CancellationToken;
		var secId = Helper.CreateSecurityId();

		var candles = new[]
		{
			new TimeFrameCandleMessage
			{
				SecurityId = secId,
				OpenTime = new DateTime(2024, 1, 15, 10, 0, 0).UtcKind(),
				CloseTime = new DateTime(2024, 1, 15, 10, 5, 0).UtcKind(),
				OpenPrice = 100m,
				HighPrice = 105m,
				LowPrice = 99m,
				ClosePrice = 103m,
				TotalVolume = 1000
			},
			new TimeFrameCandleMessage
			{
				SecurityId = secId,
				OpenTime = new DateTime(2024, 1, 15, 10, 5, 0).UtcKind(),
				CloseTime = new DateTime(2024, 1, 15, 10, 10, 0).UtcKind(),
				OpenPrice = 103m,
				HighPrice = 108m,
				LowPrice = 102m,
				ClosePrice = 107m,
				TotalVolume = 1500
			}
		};

		var volumeStep = 0.01m;
		var syncResult = candles.ToTrades(volumeStep).ToArray();
		var asyncResult = await ToAsyncEnumerable(candles).ToTrades(volumeStep).ToArrayAsync(token);

		syncResult.Length.AssertEqual(asyncResult.Length);
		for (var i = 0; i < syncResult.Length; i++)
		{
			syncResult[i].SecurityId.AssertEqual(asyncResult[i].SecurityId);
			syncResult[i].ServerTime.AssertEqual(asyncResult[i].ServerTime);
			syncResult[i].TradePrice.AssertEqual(asyncResult[i].TradePrice);
			syncResult[i].TradeVolume.AssertEqual(asyncResult[i].TradeVolume);
		}
	}

	[TestMethod]
	public async Task Compress_SyncVsAsync()
	{
		var token = CancellationToken;
		var candleBuilderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());
		var tf1 = TimeSpan.FromMinutes(1);
		var tf5 = TimeSpan.FromMinutes(5);

		var security = Helper.CreateSecurity(100);
		var secId = security.ToSecurityId();

		var candles1 = new[]
		{
			new TimeFrameCandleMessage
			{
				SecurityId = secId,
				OpenTime = new DateTime(2024, 1, 15, 10, 0, 0).UtcKind(),
				CloseTime = new DateTime(2024, 1, 15, 10, 1, 0).UtcKind(),
				OpenPrice = 100m,
				HighPrice = 102m,
				LowPrice = 99m,
				ClosePrice = 101m,
				TotalVolume = 100,
				State = CandleStates.Finished
			},
			new TimeFrameCandleMessage
			{
				SecurityId = secId,
				OpenTime = new DateTime(2024, 1, 15, 10, 1, 0).UtcKind(),
				CloseTime = new DateTime(2024, 1, 15, 10, 2, 0).UtcKind(),
				OpenPrice = 101m,
				HighPrice = 103m,
				LowPrice = 100m,
				ClosePrice = 102m,
				TotalVolume = 150,
				State = CandleStates.Finished
			},
			new TimeFrameCandleMessage
			{
				SecurityId = secId,
				OpenTime = new DateTime(2024, 1, 15, 10, 2, 0).UtcKind(),
				CloseTime = new DateTime(2024, 1, 15, 10, 3, 0).UtcKind(),
				OpenPrice = 102m,
				HighPrice = 104m,
				LowPrice = 101m,
				ClosePrice = 103m,
				TotalVolume = 200,
				State = CandleStates.Finished
			},
			new TimeFrameCandleMessage
			{
				SecurityId = secId,
				OpenTime = new DateTime(2024, 1, 15, 10, 3, 0).UtcKind(),
				CloseTime = new DateTime(2024, 1, 15, 10, 4, 0).UtcKind(),
				OpenPrice = 103m,
				HighPrice = 105m,
				LowPrice = 102m,
				ClosePrice = 104m,
				TotalVolume = 250,
				State = CandleStates.Finished
			},
			new TimeFrameCandleMessage
			{
				SecurityId = secId,
				OpenTime = new DateTime(2024, 1, 15, 10, 4, 0).UtcKind(),
				CloseTime = new DateTime(2024, 1, 15, 10, 5, 0).UtcKind(),
				OpenPrice = 104m,
				HighPrice = 106m,
				LowPrice = 103m,
				ClosePrice = 105m,
				TotalVolume = 300,
				State = CandleStates.Finished
			}
		};

		var mdMsg5 = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = tf5.TimeFrame(),
			IsFinishedOnly = false
		};

		var compressor1 = new BiggerTimeFrameCandleCompressor(mdMsg5, candleBuilderProvider.Get(typeof(TimeFrameCandleMessage)), tf1.TimeFrame());
		var compressor2 = new BiggerTimeFrameCandleCompressor(mdMsg5, candleBuilderProvider.Get(typeof(TimeFrameCandleMessage)), tf1.TimeFrame());

		var syncResult = candles1.Cast<CandleMessage>().Compress(compressor1, includeLastCandle: true).ToArray();
		var asyncResult = await ToAsyncEnumerable(candles1.Cast<CandleMessage>()).Compress(compressor2, includeLastCandle: true).ToArrayAsync(token);

		syncResult.Length.AssertEqual(asyncResult.Length);
		for (var i = 0; i < syncResult.Length; i++)
		{
			syncResult[i].SecurityId.AssertEqual(asyncResult[i].SecurityId);
			syncResult[i].OpenTime.AssertEqual(asyncResult[i].OpenTime);
			syncResult[i].OpenPrice.AssertEqual(asyncResult[i].OpenPrice);
			syncResult[i].HighPrice.AssertEqual(asyncResult[i].HighPrice);
			syncResult[i].LowPrice.AssertEqual(asyncResult[i].LowPrice);
			syncResult[i].ClosePrice.AssertEqual(asyncResult[i].ClosePrice);
			syncResult[i].TotalVolume.AssertEqual(asyncResult[i].TotalVolume);
			syncResult[i].State.AssertEqual(asyncResult[i].State);
		}
	}

	[TestMethod]
	public async Task ToCandles_FromTicks_Async()
	{
		var token = CancellationToken;
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var trades = new[]
		{
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = 11,
				TradeVolume = 11,
				SecurityId = secId,
				ServerTime = new DateTime(2000, 1, 1, 1, 1, 1, 100).UtcKind()
			},
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = 12,
				TradeVolume = 12,
				SecurityId = secId,
				ServerTime = new DateTime(2000, 1, 1, 1, 1, 1, 200).UtcKind()
			},
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = 13,
				TradeVolume = 13,
				SecurityId = secId,
				ServerTime = new DateTime(2000, 1, 1, 1, 1, 1, 300).UtcKind()
			}
		};

		var sub = new Subscription(TimeSpan.FromMilliseconds(100).TimeFrame(), security);
		var mdMsg = sub.MarketData;
		mdMsg.IsFinishedOnly = false;

		var candles = await ToAsyncEnumerable(trades).ToCandles(mdMsg).ToArrayAsync(token);

		candles.Length.AssertEqual(3);

		var iter = 1;
		foreach (var candle in candles)
		{
			candle.TotalVolume.AssertEqual(10 + iter);
			candle.OpenTime.AssertEqual(trades[iter - 1].ServerTime);
			candle.State.AssertEqual(iter == 3 ? CandleStates.Active : CandleStates.Finished);
			iter++;
		}
	}

	[TestMethod]
	public async Task ToCandles_FromQuotes_Async()
	{
		var token = CancellationToken;
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();

		var quotes = new[]
		{
			new QuoteChangeMessage
			{
				SecurityId = secId,
				ServerTime = new DateTime(2000, 1, 1, 1, 1, 1, 100).UtcKind(),
				Bids = [new QuoteChange(100m, 10)],
				Asks = [new QuoteChange(101m, 10)]
			},
			new QuoteChangeMessage
			{
				SecurityId = secId,
				ServerTime = new DateTime(2000, 1, 1, 1, 1, 1, 200).UtcKind(),
				Bids = [new QuoteChange(100.5m, 15)],
				Asks = [new QuoteChange(101.5m, 15)]
			},
			new QuoteChangeMessage
			{
				SecurityId = secId,
				ServerTime = new DateTime(2000, 1, 1, 1, 1, 1, 300).UtcKind(),
				Bids = [new QuoteChange(101m, 20)],
				Asks = [new QuoteChange(102m, 20)]
			}
		};

		var sub = new Subscription(TimeSpan.FromMilliseconds(100).TimeFrame(), security);
		var mdMsg = sub.MarketData;
		mdMsg.IsFinishedOnly = false;

		var candles = await ToAsyncEnumerable(quotes).ToCandles(mdMsg, Level1Fields.SpreadMiddle).ToArrayAsync(token);

		candles.Length.AssertEqual(3);
		// SpreadMiddle for first quote: (100 + 101) / 2 = 100.5
		candles[0].OpenPrice.AssertEqual(100.5m);
	}

	[TestMethod]
	public async Task ToTrades_FromCandles_Async()
	{
		var token = CancellationToken;
		var secId = Helper.CreateSecurityId();

		var candles = new[]
		{
			new TimeFrameCandleMessage
			{
				SecurityId = secId,
				OpenTime = new DateTime(2024, 1, 15, 10, 0, 0).UtcKind(),
				CloseTime = new DateTime(2024, 1, 15, 10, 5, 0).UtcKind(),
				OpenPrice = 100m,
				HighPrice = 105m,
				LowPrice = 99m,
				ClosePrice = 103m,
				TotalVolume = 1000
			}
		};

		var volumeStep = 0.01m;
		var ticks = await ToAsyncEnumerable(candles).ToTrades(volumeStep).ToArrayAsync(token);

		// Each candle generates up to 4 ticks (OHLC)
		ticks.Length.AssertEqual(4);
		ticks[0].TradePrice.AssertEqual(100m); // Open
		ticks[1].TradePrice.AssertEqual(105m); // High
		ticks[2].TradePrice.AssertEqual(99m);  // Low
		ticks[3].TradePrice.AssertEqual(103m); // Close
	}

	[TestMethod]
	public async Task Compress_Async()
	{
		var token = CancellationToken;
		var candleBuilderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());
		var tf1 = TimeSpan.FromMinutes(1);
		var tf5 = TimeSpan.FromMinutes(5);

		var security = Helper.CreateSecurity(100);
		var secId = security.ToSecurityId();

		var candles1 = new[]
		{
			new TimeFrameCandleMessage
			{
				SecurityId = secId,
				OpenTime = new DateTime(2024, 1, 15, 10, 0, 0).UtcKind(),
				CloseTime = new DateTime(2024, 1, 15, 10, 1, 0).UtcKind(),
				OpenPrice = 100m,
				HighPrice = 102m,
				LowPrice = 99m,
				ClosePrice = 101m,
				TotalVolume = 100,
				State = CandleStates.Finished
			},
			new TimeFrameCandleMessage
			{
				SecurityId = secId,
				OpenTime = new DateTime(2024, 1, 15, 10, 1, 0).UtcKind(),
				CloseTime = new DateTime(2024, 1, 15, 10, 2, 0).UtcKind(),
				OpenPrice = 101m,
				HighPrice = 103m,
				LowPrice = 100m,
				ClosePrice = 102m,
				TotalVolume = 150,
				State = CandleStates.Finished
			},
			new TimeFrameCandleMessage
			{
				SecurityId = secId,
				OpenTime = new DateTime(2024, 1, 15, 10, 2, 0).UtcKind(),
				CloseTime = new DateTime(2024, 1, 15, 10, 3, 0).UtcKind(),
				OpenPrice = 102m,
				HighPrice = 104m,
				LowPrice = 101m,
				ClosePrice = 103m,
				TotalVolume = 200,
				State = CandleStates.Finished
			},
			new TimeFrameCandleMessage
			{
				SecurityId = secId,
				OpenTime = new DateTime(2024, 1, 15, 10, 3, 0).UtcKind(),
				CloseTime = new DateTime(2024, 1, 15, 10, 4, 0).UtcKind(),
				OpenPrice = 103m,
				HighPrice = 105m,
				LowPrice = 102m,
				ClosePrice = 104m,
				TotalVolume = 250,
				State = CandleStates.Finished
			},
			new TimeFrameCandleMessage
			{
				SecurityId = secId,
				OpenTime = new DateTime(2024, 1, 15, 10, 4, 0).UtcKind(),
				CloseTime = new DateTime(2024, 1, 15, 10, 5, 0).UtcKind(),
				OpenPrice = 104m,
				HighPrice = 106m,
				LowPrice = 103m,
				ClosePrice = 105m,
				TotalVolume = 300,
				State = CandleStates.Finished
			}
		};

		var mdMsg5 = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = tf5.TimeFrame(),
			IsFinishedOnly = false
		};

		var compressor = new BiggerTimeFrameCandleCompressor(mdMsg5, candleBuilderProvider.Get(typeof(TimeFrameCandleMessage)), tf1.TimeFrame());

		var candles5 = await ToAsyncEnumerable(candles1.Cast<CandleMessage>()).Compress(compressor, includeLastCandle: true).ToArrayAsync(token);

		candles5.Length.AssertEqual(1);
		var c5 = candles5[0];
		c5.OpenPrice.AssertEqual(100m);
		c5.HighPrice.AssertEqual(106m);
		c5.LowPrice.AssertEqual(99m);
		c5.ClosePrice.AssertEqual(105m);
		c5.TotalVolume.AssertEqual(1000m);
	}

	[TestMethod]
	public async Task ToCandles_FromTicks_Empty_Async()
	{
		var token = CancellationToken;
		var security = Helper.CreateSecurity();
		var trades = Array.Empty<ExecutionMessage>();

		var sub = new Subscription(TimeSpan.FromMinutes(1).TimeFrame(), security);
		var mdMsg = sub.MarketData;

		var candles = await ToAsyncEnumerable(trades).ToCandles(mdMsg).ToArrayAsync(token);

		candles.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task ToTrades_FromCandles_Empty_Async()
	{
		var token = CancellationToken;
		var candles = Array.Empty<TimeFrameCandleMessage>();

		var ticks = await ToAsyncEnumerable(candles).ToTrades(0.01m).ToArrayAsync(token);

		ticks.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task Compress_Empty_Async()
	{
		var token = CancellationToken;
		var candleBuilderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());
		var secId = Helper.CreateSecurityId();

		var mdMsg = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(5).TimeFrame()
		};

		var compressor = new BiggerTimeFrameCandleCompressor(mdMsg, candleBuilderProvider.Get(typeof(TimeFrameCandleMessage)), TimeSpan.FromMinutes(1).TimeFrame());

		var candles = await ToAsyncEnumerable(Array.Empty<CandleMessage>()).Compress(compressor, includeLastCandle: false).ToArrayAsync(token);

		candles.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task Compress_IncludeLastCandle_False_Async()
	{
		var token = CancellationToken;
		var candleBuilderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());
		var tf1 = TimeSpan.FromMinutes(1);
		var tf5 = TimeSpan.FromMinutes(5);

		var security = Helper.CreateSecurity(100);
		var secId = security.ToSecurityId();

		// Only 2 candles - not enough for complete 5-min candle
		var candles1 = new[]
		{
			new TimeFrameCandleMessage
			{
				SecurityId = secId,
				OpenTime = new DateTime(2024, 1, 15, 10, 0, 0).UtcKind(),
				CloseTime = new DateTime(2024, 1, 15, 10, 1, 0).UtcKind(),
				OpenPrice = 100m,
				HighPrice = 102m,
				LowPrice = 99m,
				ClosePrice = 101m,
				TotalVolume = 100,
				State = CandleStates.Finished
			},
			new TimeFrameCandleMessage
			{
				SecurityId = secId,
				OpenTime = new DateTime(2024, 1, 15, 10, 1, 0).UtcKind(),
				CloseTime = new DateTime(2024, 1, 15, 10, 2, 0).UtcKind(),
				OpenPrice = 101m,
				HighPrice = 103m,
				LowPrice = 100m,
				ClosePrice = 102m,
				TotalVolume = 150,
				State = CandleStates.Finished
			}
		};

		var mdMsg5 = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = tf5.TimeFrame(),
			IsFinishedOnly = false
		};

		var compressor = new BiggerTimeFrameCandleCompressor(mdMsg5, candleBuilderProvider.Get(typeof(TimeFrameCandleMessage)), tf1.TimeFrame());

		// includeLastCandle = false - should not include incomplete candle
		var candles5 = await ToAsyncEnumerable(candles1.Cast<CandleMessage>()).Compress(compressor, includeLastCandle: false).ToArrayAsync(token);

		candles5.Length.AssertEqual(0);
	}

	[TestMethod]
	public Task ToCandles_FromTicks_Null_Async()
	{
		var token = CancellationToken;
		var security = Helper.CreateSecurity();
		var sub = new Subscription(TimeSpan.FromMinutes(1).TimeFrame(), security);

		return ThrowsExactlyAsync<ArgumentNullException>(async () =>
		{
			IAsyncEnumerable<ExecutionMessage> trades = null;
			await trades.ToCandles(sub.MarketData).ToArrayAsync(token);
		});
	}

	[TestMethod]
	public Task ToTrades_FromCandles_Null_Async()
	{
		var token = CancellationToken;
		return ThrowsExactlyAsync<ArgumentNullException>(async () =>
		{
			IAsyncEnumerable<TimeFrameCandleMessage> candles = null;
			await candles.ToTrades(0.01m).ToArrayAsync(token);
		});
	}

	[TestMethod]
	public Task Compress_Null_Async()
	{
		var token = CancellationToken;
		var candleBuilderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());
		var secId = Helper.CreateSecurityId();

		var mdMsg = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(5).TimeFrame()
		};

		var compressor = new BiggerTimeFrameCandleCompressor(mdMsg, candleBuilderProvider.Get(typeof(TimeFrameCandleMessage)), TimeSpan.FromMinutes(1).TimeFrame());

		return ThrowsExactlyAsync<ArgumentNullException>(async () =>
		{
			IAsyncEnumerable<CandleMessage> candles = null;
			await candles.Compress(compressor, includeLastCandle: false).ToArrayAsync(token);
		});
	}
}
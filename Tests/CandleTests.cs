namespace StockSharp.Tests;

using StockSharp.Algo.Candles.Compression;

[TestClass]
public class CandleTests
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
				ServerTime = new DateTime(2000, 1, 1, 1, 1, 1, 100).ApplyMoscow()
			},
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = 12,
				TradeVolume = 12,
				SecurityId = secId,
				ServerTime = new DateTime(2000, 1, 1, 1, 1, 1, 200).ApplyMoscow()
			},
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = 13,
				TradeVolume = 13,
				SecurityId = secId,
				ServerTime = new DateTime(2000, 1, 1, 1, 1, 1, 300).ApplyMoscow()
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
		security.Board = ExchangeBoard.Forts;

		var trades = new[]
		{
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = 11,
				TradeVolume = 11,
				SecurityId = secId,
				ServerTime = new DateTime(2000, 1, 10, 10, 0, 0).ApplyMoscow()
			},
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = 12,
				TradeVolume = 12,
				SecurityId = secId,
				ServerTime = new DateTime(2000, 1, 10, 10, 1, 0).ApplyMoscow()
			},
			new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradePrice = 13,
				TradeVolume = 13,
				SecurityId = secId,
				ServerTime = new DateTime(2000, 1, 10, 10, 2, 0).ApplyMoscow()
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
	public void Storage()
	{
		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();
		var tf = TimeSpan.FromMinutes(1);
		var storageRegistry = Helper.GetStorage(Paths.HistoryDataPath);

		var loadedCandles = storageRegistry.GetTimeFrameCandleMessageStorage(secId, tf).Load(Paths.HistoryBeginDate, Paths.HistoryBeginDate.AddDays(1)).ToArray();

		var sub = new Subscription(tf.TimeFrame(), new SecurityMessage { SecurityId = secId });
		var mdMsg = sub.MarketData;
		mdMsg.IsFinishedOnly = false;

		var loadedTicks = storageRegistry.GetTickMessageStorage(secId).Load(loadedCandles.First().OpenTime, loadedCandles.Last().OpenTime + tf.AddMicroseconds(-1)).ToArray();
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

		(rangeCandles.Length > 2000).AssertTrue();
		(tickCandles.Length == 1000).AssertTrue();
		(volumeCandles.Length > 9000).AssertTrue();
		(timeFrameCandles.Length > 100).AssertTrue();
		(renkoCandles.Length > 200000).AssertTrue();
		(pnfCandles.Length > 40000).AssertTrue();
	}

	[TestMethod]
	public void CandleBounds()
	{
		var date = new DateTime(2013, 5, 15).ApplyTimeZone(ExchangeBoard.Micex.TimeZone);

		CheckCandleBounds(TimeSpan.FromMinutes(5).GetCandleBounds(date.Add(new TimeSpan(7, 49, 33)), ExchangeBoard.MicexJunior), date.Add(new TimeSpan(7, 45, 0)), date.Add(new TimeSpan(7, 50, 0)));
		CheckCandleBounds(TimeSpan.FromMinutes(5).GetCandleBounds(date.Add(new TimeSpan(14, 1, 33)), ExchangeBoard.Forts), date.Add(new TimeSpan(14, 0, 0)), date.Add(new TimeSpan(14, 5, 0)));

		CheckCandleBounds(TimeSpan.FromMinutes(15).GetCandleBounds(date.Add(new TimeSpan(23, 46, 0)), ExchangeBoard.Forts), date.Add(new TimeSpan(23, 45, 0)), date.Add(new TimeSpan(23, 50, 0)));

		CheckCandleBounds(TimeSpan.FromMinutes(30).GetCandleBounds(date.Add(new TimeSpan(19, 0, 0)), ExchangeBoard.Forts), date.Add(new TimeSpan(19, 0, 0)), date.Add(new TimeSpan(19, 30, 0)));
		CheckCandleBounds(TimeSpan.FromMinutes(30).GetCandleBounds(date.Add(new TimeSpan(18, 3, 0)), ExchangeBoard.Forts), date.Add(new TimeSpan(18, 0, 0)), date.Add(new TimeSpan(18, 30, 0)));

		CheckCandleBounds(TimeSpan.FromHours(1).GetCandleBounds(date.Add(new TimeSpan(23, 30, 0)), ExchangeBoard.Forts), date.Add(new TimeSpan(23, 00, 0)), date.Add(new TimeSpan(23, 50, 0)));
		CheckCandleBounds(TimeSpan.FromHours(1).GetCandleBounds(date.Add(new TimeSpan(18, 30, 0)), ExchangeBoard.Forts), date.Add(new TimeSpan(18, 0, 0)), date.Add(new TimeSpan(18, 45, 0)));

		CheckCandleBounds(TimeSpan.FromHours(2).GetCandleBounds(date.Add(new TimeSpan(18, 30, 0)), ExchangeBoard.Forts), date.Add(new TimeSpan(18, 0, 0)), date.Add(new TimeSpan(20, 00, 0)));

		CheckCandleBounds(TimeSpan.FromHours(24).GetCandleBounds(date.Add(new TimeSpan(23, 30, 0)), ExchangeBoard.Forts), date.Add(new TimeSpan(10, 0, 0)), date.Add(new TimeSpan(23, 50, 0)));
		CheckCandleBounds(TimeSpan.FromHours(48).GetCandleBounds(date.Add(new TimeSpan(23, 30, 0)), ExchangeBoard.Forts), date.Add(new TimeSpan(10, 0, 0)), date.Add(new TimeSpan(1, 23, 50, 0)));
	}

	private static void CheckCandleBounds(Range<DateTimeOffset> range, DateTimeOffset min, DateTimeOffset max)
	{
		min.AssertEqual(range.Min);
		max.AssertEqual(range.Max);
	}

	[TestMethod]
	public void TimeFrameCount()
	{
		var tf = TimeSpan.FromMinutes(5);

		static Range<DateTimeOffset> Create(DateTime from, DateTime to)
			=> new(from.ApplyMoscow(), to.ApplyMoscow());

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

		var now = DateTimeOffset.Now;

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
}
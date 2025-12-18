using StockSharp.Algo.Candles.Compression;

namespace StockSharp.Tests;

[TestClass]
public class StorageHelperLoadMessagesTests : BaseTestClass
{
	private static (StorageCoreSettings settings, CandleBuilderProvider candleBuilderProvider, SecurityId secId) CreateEnv(StorageModes mode = StorageModes.Incremental)
	{
		var registry = Helper.GetStorage(Helper.GetSubTemp());

		var settings = new StorageCoreSettings
		{
			StorageRegistry = registry,
			Drive = registry.DefaultDrive,
			Format = StorageFormats.Binary,
			Mode = mode,
		};

		var candleBuilderProvider = new CandleBuilderProvider(registry.ExchangeInfoProvider);

		var secId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };

		return (settings, candleBuilderProvider, secId);
	}

	private static ExecutionMessage CreateTick(SecurityId secId, DateTime serverTime, long tradeId, decimal price, decimal volume = 1m) => new()
	{
		SecurityId = secId,
		DataTypeEx = DataType.Ticks,
		ServerTime = serverTime,
		TradeId = tradeId,
		TradePrice = price,
		TradeVolume = volume,
	};

	private static ExecutionMessage CreateOrderLogTrade(SecurityId secId, DateTime serverTime, long tradeId, decimal price, decimal volume = 1m) => new()
	{
		SecurityId = secId,
		DataTypeEx = DataType.OrderLog,
		ServerTime = serverTime,
		TransactionId = tradeId,
		SeqNum = tradeId,
		OrderId = tradeId,
		OrderState = OrderStates.Done,
		OrderPrice = price,
		OrderVolume = volume,
		Side = Sides.Buy,
		TradeId = tradeId,
		TradePrice = price,
		TradeVolume = volume,
	};

	private static void AssertTimeFrameCandle(
		TimeFrameCandleMessage candle,
		SecurityId secId,
		TimeSpan tf,
		DateTime openTime,
		DateTime closeTime,
		decimal open,
		decimal high,
		decimal low,
		decimal close,
		decimal totalVolume,
		int? totalTicks,
		CandleStates state,
		DataType buildFrom,
		long transactionId)
	{
		candle.SecurityId.AssertEqual(secId);
		candle.DataType.GetTimeFrame().AssertEqual(tf);

		candle.OpenTime.AssertEqual(openTime);
		candle.CloseTime.AssertEqual(closeTime);

		candle.OpenPrice.AssertEqual(open);
		candle.HighPrice.AssertEqual(high);
		candle.LowPrice.AssertEqual(low);
		candle.ClosePrice.AssertEqual(close);

		candle.TotalVolume.AssertEqual(totalVolume);

		if (totalTicks is null)
			candle.TotalTicks.AssertNull();
		else
		{
			candle.TotalTicks.AssertNotNull();
			candle.TotalTicks.Value.AssertEqual(totalTicks.Value);
		}

		candle.State.AssertEqual(state);

		if (buildFrom is null)
			candle.BuildFrom.AssertNull();
		else
			candle.BuildFrom.AssertEqual(buildFrom);

		candle.OriginalTransactionId.AssertEqual(transactionId);
		candle.SubscriptionId.AssertEqual(transactionId);
		candle.OfflineMode.AssertEqual(MessageOfflineModes.Ignore);
	}

	private static Level1ChangeMessage CreateLevel1(SecurityId secId, DateTime serverTime, decimal? lastPrice = null, decimal? lastVolume = null, decimal? bid = null, decimal? bidVol = null, decimal? ask = null, decimal? askVol = null)
	{
		var msg = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = serverTime,
		};

		if (lastPrice != null)
			msg.Add(Level1Fields.LastTradePrice, lastPrice.Value);

		if (lastVolume != null)
			msg.Add(Level1Fields.LastTradeVolume, lastVolume.Value);

		if (bid != null)
			msg.Add(Level1Fields.BestBidPrice, bid.Value);

		if (bidVol != null)
			msg.Add(Level1Fields.BestBidVolume, bidVol.Value);

		if (ask != null)
			msg.Add(Level1Fields.BestAskPrice, ask.Value);

		if (askVol != null)
			msg.Add(Level1Fields.BestAskVolume, askVol.Value);

		return msg;
	}

	private static QuoteChangeMessage CreateQuotes(SecurityId secId, DateTime serverTime, decimal bid, decimal ask) => new()
	{
		SecurityId = secId,
		ServerTime = serverTime,
		Bids = [new QuoteChange(bid, 1)],
		Asks = [new QuoteChange(ask, 1)],
	};

	private static TimeFrameCandleMessage CreateTfCandle(SecurityId secId, DateTime openTime, TimeSpan tf, decimal open, decimal high, decimal low, decimal close, decimal volume, int? totalTicks = null, IEnumerable<CandlePriceLevel> priceLevels = null) => new()
	{
		SecurityId = secId,
		TypedArg = tf,
		OpenTime = openTime,
		HighTime = openTime,
		LowTime = openTime,
		CloseTime = openTime + tf,
		OpenPrice = open,
		HighPrice = high,
		LowPrice = low,
		ClosePrice = close,
		TotalVolume = volume,
		TotalTicks = totalTicks,
		State = CandleStates.Finished,
		DataType = tf.TimeFrame(),
		PriceLevels = priceLevels,
	};

	[TestMethod]
	public void FromNull_ReturnsNull_AndSendsNothing()
	{
		var (settings, provider, secId) = CreateEnv();

		var output = new List<Message>();

		var result = settings.LoadMessages(provider, new MarketDataMessage
		{
			TransactionId = 1,
			IsSubscribe = true,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			From = null,
		}, output.Add);

		result.AssertNull();
		output.Count.AssertEqual(0);
	}

	[TestMethod]
	public async Task Ticks_Load_RespectsSkipAndCount()
	{
		var token = CancellationToken;
		var (settings, provider, secId) = CreateEnv(StorageModes.Incremental);

		var date = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var ticks = new[]
		{
			CreateTick(secId, date.AddMinutes(0), tradeId: 1, price: 100),
			CreateTick(secId, date.AddMinutes(1), tradeId: 2, price: 101),
			CreateTick(secId, date.AddMinutes(2), tradeId: 3, price: 102),
		};

		await settings.GetStorage<ExecutionMessage>(secId, DataType.Ticks).SaveAsync(ticks, token);

		var outMessages = new List<Message>();

		var result = settings.LoadMessages(provider, new MarketDataMessage
		{
			TransactionId = 100,
			IsSubscribe = true,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			From = date,
			To = date.AddMinutes(10),
			BuildMode = MarketDataBuildModes.Load,
			Skip = 1,
			Count = 1,
		}, outMessages.Add);

		result.AssertNotNull();
		result.Value.lastDate.AssertEqual(ticks[1].ServerTime);
		result.Value.left.AssertEqual(0L);

		outMessages.Count.AssertEqual(2);
		outMessages[0].AssertOfType<SubscriptionResponseMessage>();
		((SubscriptionResponseMessage)outMessages[0]).OriginalTransactionId.AssertEqual(100);

		outMessages[1].AssertOfType<ExecutionMessage>();
		var tick = (ExecutionMessage)outMessages[1];
		tick.TradeId.AssertEqual(2L);
		tick.OfflineMode.AssertEqual(MessageOfflineModes.Ignore);
		tick.OriginalTransactionId.AssertEqual(100L);
		tick.SubscriptionId.AssertEqual(100L);
	}

	[TestMethod]
	public async Task Level1_BuildFromMarketDepth_BuildsAndSends()
	{
		var token = CancellationToken;
		var (settings, provider, secId) = CreateEnv(StorageModes.Incremental);

		var date = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);

		await settings.GetStorage<QuoteChangeMessage>(secId, DataType.MarketDepth).SaveAsync(
		[
			CreateQuotes(secId, date.AddMinutes(0), bid: 99, ask: 101),
			CreateQuotes(secId, date.AddMinutes(1), bid: 100, ask: 102),
		], token);

		var outMessages = new List<Message>();

		var result = settings.LoadMessages(provider, new MarketDataMessage
		{
			TransactionId = 200,
			IsSubscribe = true,
			SecurityId = secId,
			DataType2 = DataType.Level1,
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.MarketDepth,
			From = date.AddMinutes(-1),
			To = date.AddMinutes(10),
			Count = 10,
		}, outMessages.Add);

		result.AssertNotNull();
		outMessages.OfType<SubscriptionResponseMessage>().Any().AssertTrue();

		var level1 = outMessages.OfType<Level1ChangeMessage>().ToArray();
		(level1.Length > 0).AssertTrue();

		level1.All(m => m.SubscriptionId == 200 && m.OriginalTransactionId == 200 && m.OfflineMode == MessageOfflineModes.Ignore).AssertTrue();
	}

	[TestMethod]
	public async Task MarketDepth_BuildFromLevel1_BuildsAndSends()
	{
		var token = CancellationToken;
		var (settings, provider, secId) = CreateEnv(StorageModes.Incremental);

		var date = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);

		await settings.GetStorage<Level1ChangeMessage>(secId, DataType.Level1).SaveAsync(
		[
			CreateLevel1(secId, date.AddMinutes(0), bid: 99, bidVol: 1, ask: 101, askVol: 2),
			CreateLevel1(secId, date.AddMinutes(1), bid: 100, bidVol: 1, ask: 102, askVol: 2),
		], token);

		var outMessages = new List<Message>();

		var result = settings.LoadMessages(provider, new MarketDataMessage
		{
			TransactionId = 300,
			IsSubscribe = true,
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Level1,
			From = date.AddMinutes(-1),
			To = date.AddMinutes(10),
			Count = 10,
		}, outMessages.Add);

		result.AssertNotNull();
		outMessages.OfType<SubscriptionResponseMessage>().Any().AssertTrue();

		var depths = outMessages.OfType<QuoteChangeMessage>().ToArray();
		(depths.Length > 0).AssertTrue();
		depths.All(m => m.SubscriptionId == 300 && m.OriginalTransactionId == 300 && m.OfflineMode == MessageOfflineModes.Ignore).AssertTrue();
	}

	[TestMethod]
	public async Task Ticks_BuildFromLevel1_BuildsAndSends()
	{
		var token = CancellationToken;
		var (settings, provider, secId) = CreateEnv(StorageModes.Incremental);

		var date = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);

		await settings.GetStorage<Level1ChangeMessage>(secId, DataType.Level1).SaveAsync(
		[
			CreateLevel1(secId, date.AddSeconds(0), lastPrice: 100, lastVolume: 1, bid: 99, ask: 101),
			CreateLevel1(secId, date.AddSeconds(30), lastPrice: 101, lastVolume: 2, bid: 100, ask: 102),
		], token);

		var outMessages = new List<Message>();

		var result = settings.LoadMessages(provider, new MarketDataMessage
		{
			TransactionId = 400,
			IsSubscribe = true,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Level1,
			From = date.AddMinutes(-1),
			To = date.AddMinutes(10),
			Count = 10,
		}, outMessages.Add);

		result.AssertNotNull();
		outMessages.OfType<SubscriptionResponseMessage>().Any().AssertTrue();

		var ticks = outMessages.OfType<ExecutionMessage>().ToArray();
		(ticks.Length > 0).AssertTrue();
		ticks.All(m => m.SubscriptionId == 400 && m.OriginalTransactionId == 400 && m.OfflineMode == MessageOfflineModes.Ignore).AssertTrue();
	}

	[TestMethod]
	public async Task Candles_BuildFromTicks_BuildsAndSends()
	{
		var token = CancellationToken;
		var (settings, provider, secId) = CreateEnv(StorageModes.Incremental);

		var date = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);

		await settings.GetStorage<ExecutionMessage>(secId, DataType.Ticks).SaveAsync(
		[
			CreateTick(secId, date.AddSeconds(0), tradeId: 1, price: 100, volume: 10),
			CreateTick(secId, date.AddSeconds(30), tradeId: 2, price: 101, volume: 20),
			CreateTick(secId, date.AddSeconds(60), tradeId: 3, price: 99, volume: 30),
			CreateTick(secId, date.AddSeconds(90), tradeId: 4, price: 102, volume: 40),
		], token);

		var outMessages = new List<Message>();

		var tf = TimeSpan.FromMinutes(1);

		var result = settings.LoadMessages(provider, new MarketDataMessage
		{
			TransactionId = 500,
			IsSubscribe = true,
			SecurityId = secId,
			DataType2 = tf.TimeFrame(),
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Ticks,
			From = date.AddMinutes(-1),
			To = date.AddMinutes(10),
			Count = 10,
		}, outMessages.Add);

		result.AssertNotNull();
		outMessages.OfType<SubscriptionResponseMessage>().Any().AssertTrue();

		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToArray();
		candles.Length.AssertEqual(2);

		AssertTimeFrameCandle(candles[0], secId, tf, date, date.AddSeconds(30), 100, 101, 100, 101, totalVolume: 30, totalTicks: 2, CandleStates.Finished, DataType.Ticks, transactionId: 500);
		AssertTimeFrameCandle(candles[1], secId, tf, date.AddMinutes(1), date.AddSeconds(90), 99, 102, 99, 102, totalVolume: 70, totalTicks: 2, CandleStates.Active, DataType.Ticks, transactionId: 500);
	}

	[TestMethod]
	public async Task Candles_BuildFromLevel1_SpreadMiddle_BuildsAndSends()
	{
		var token = CancellationToken;
		var (settings, provider, secId) = CreateEnv(StorageModes.Incremental);

		var date = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);

		await settings.GetStorage<Level1ChangeMessage>(secId, DataType.Level1).SaveAsync(
		[
			CreateLevel1(secId, date.AddSeconds(0), bid: 99, bidVol: 1, ask: 101, askVol: 1),
			CreateLevel1(secId, date.AddSeconds(30), bid: 100, bidVol: 1, ask: 102, askVol: 1),
			CreateLevel1(secId, date.AddSeconds(60), bid: 98, bidVol: 1, ask: 100, askVol: 1),
			CreateLevel1(secId, date.AddSeconds(90), bid: 99, bidVol: 1, ask: 101, askVol: 1),
		], token);

		var outMessages = new List<Message>();
		var tf = TimeSpan.FromMinutes(1);

		var result = settings.LoadMessages(provider, new MarketDataMessage
		{
			TransactionId = 600,
			IsSubscribe = true,
			SecurityId = secId,
			DataType2 = tf.TimeFrame(),
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.Level1,
			BuildField = Level1Fields.SpreadMiddle,
			From = date.AddMinutes(-1),
			To = date.AddMinutes(10),
			Count = 10,
		}, outMessages.Add);

		result.AssertNotNull();
		outMessages.OfType<SubscriptionResponseMessage>().Any().AssertTrue();

		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToArray();
		candles.Length.AssertEqual(2);

		AssertTimeFrameCandle(candles[0], secId, tf, date, date.AddSeconds(30), 100, 101, 100, 101, totalVolume: 2, totalTicks: 2, CandleStates.Finished, DataType.MarketDepth, transactionId: 600);
		AssertTimeFrameCandle(candles[1], secId, tf, date.AddMinutes(1), date.AddSeconds(90), 99, 100, 99, 100, totalVolume: 2, totalTicks: 2, CandleStates.Active, DataType.MarketDepth, transactionId: 600);
	}

	[TestMethod]
	public async Task Candles_BuildFromMarketDepth_DefaultField_BuildsAndSends()
	{
		var token = CancellationToken;
		var (settings, provider, secId) = CreateEnv(StorageModes.Incremental);

		var date = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);

		await settings.GetStorage<QuoteChangeMessage>(secId, DataType.MarketDepth).SaveAsync(
		[
			CreateQuotes(secId, date.AddSeconds(0), bid: 99, ask: 101),
			CreateQuotes(secId, date.AddSeconds(30), bid: 100, ask: 102),
			CreateQuotes(secId, date.AddSeconds(60), bid: 98, ask: 100),
			CreateQuotes(secId, date.AddSeconds(90), bid: 99, ask: 101),
		], token);

		var outMessages = new List<Message>();
		var tf = TimeSpan.FromMinutes(1);

		var result = settings.LoadMessages(provider, new MarketDataMessage
		{
			TransactionId = 700,
			IsSubscribe = true,
			SecurityId = secId,
			DataType2 = tf.TimeFrame(),
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.MarketDepth,
			From = date.AddMinutes(-1),
			To = date.AddMinutes(10),
			Count = 10,
		}, outMessages.Add);

		result.AssertNotNull();
		outMessages.OfType<SubscriptionResponseMessage>().Any().AssertTrue();

		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToArray();
		candles.Length.AssertEqual(2);

		AssertTimeFrameCandle(candles[0], secId, tf, date, date.AddSeconds(30), 100, 101, 100, 101, totalVolume: 2, totalTicks: 2, CandleStates.Finished, DataType.MarketDepth, transactionId: 700);
		AssertTimeFrameCandle(candles[1], secId, tf, date.AddMinutes(1), date.AddSeconds(90), 99, 100, 99, 100, totalVolume: 2, totalTicks: 2, CandleStates.Active, DataType.MarketDepth, transactionId: 700);
	}

	[TestMethod]
	public async Task Candles_BuildFromOrderLog_LastTradePrice_BuildsAndSends()
	{
		var token = CancellationToken;
		var (settings, provider, secId) = CreateEnv(StorageModes.Incremental);

		var date = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);

		await settings.GetStorage<ExecutionMessage>(secId, DataType.OrderLog).SaveAsync(
		[
			CreateOrderLogTrade(secId, date.AddSeconds(0), tradeId: 1, price: 100, volume: 10),
			CreateOrderLogTrade(secId, date.AddSeconds(30), tradeId: 2, price: 101, volume: 20),
			CreateOrderLogTrade(secId, date.AddSeconds(60), tradeId: 3, price: 99, volume: 30),
			CreateOrderLogTrade(secId, date.AddSeconds(90), tradeId: 4, price: 102, volume: 40),
		], token);

		var outMessages = new List<Message>();
		var tf = TimeSpan.FromMinutes(1);

		var result = settings.LoadMessages(provider, new MarketDataMessage
		{
			TransactionId = 800,
			IsSubscribe = true,
			SecurityId = secId,
			DataType2 = tf.TimeFrame(),
			BuildMode = MarketDataBuildModes.Build,
			BuildFrom = DataType.OrderLog,
			From = date.AddMinutes(-1),
			To = date.AddMinutes(10),
			Count = 10,
		}, outMessages.Add);

		result.AssertNotNull();
		outMessages.OfType<SubscriptionResponseMessage>().Any().AssertTrue();

		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToArray();
		candles.Length.AssertEqual(2);

		AssertTimeFrameCandle(candles[0], secId, tf, date, date.AddSeconds(30), 100, 101, 100, 101, totalVolume: 30, totalTicks: 2, CandleStates.Finished, DataType.OrderLog, transactionId: 800);
		AssertTimeFrameCandle(candles[1], secId, tf, date.AddMinutes(1), date.AddSeconds(90), 99, 102, 99, 102, totalVolume: 70, totalTicks: 2, CandleStates.Active, DataType.OrderLog, transactionId: 800);
	}

	[TestMethod]
	public async Task Candles_Load_VolumeProfileFilter_FiltersOutCandlesWithoutPriceLevels()
	{
		var token = CancellationToken;
		var (settings, provider, secId) = CreateEnv(StorageModes.Incremental);

		var tf = TimeSpan.FromMinutes(1);
		var date = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);

		await settings.GetStorage<CandleMessage>(secId, tf.TimeFrame()).SaveAsync(
		[
			CreateTfCandle(secId, date.AddMinutes(0), tf, 100, 101, 99, 100, 10, totalTicks: 1, priceLevels: null),
			CreateTfCandle(secId, date.AddMinutes(1), tf, 100, 102, 98, 101, 20, totalTicks: 2, priceLevels: [new CandlePriceLevel { Price = 100m, TotalVolume = 20m }]),
		], token);

		var outMessages = new List<Message>();

		var result = settings.LoadMessages(provider, new MarketDataMessage
		{
			TransactionId = 900,
			IsSubscribe = true,
			SecurityId = secId,
			DataType2 = tf.TimeFrame(),
			BuildMode = MarketDataBuildModes.Load,
			IsCalcVolumeProfile = true,
			From = date,
			To = date.AddMinutes(10),
			Count = 10,
		}, outMessages.Add);

		result.AssertNotNull();

		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToArray();
		candles.Length.AssertEqual(1);

		AssertTimeFrameCandle(candles[0], secId, tf, date.AddMinutes(1), date.AddMinutes(2), 100, 102, 98, 101, totalVolume: 20, totalTicks: 2, CandleStates.Finished, buildFrom: null, transactionId: 900);

		candles[0].PriceLevels.AssertNotNull();
		var levels = candles[0].PriceLevels.ToArray();
		levels.Length.AssertEqual(1);
		levels[0].Price.AssertEqual(100m);
		levels[0].TotalVolume.AssertEqual(20m);
	}

	[TestMethod]
	public async Task Candles_LoadAndBuild_LoadsFromStorage_ThenBuildsRemaining()
	{
		var token = CancellationToken;
		var (settings, provider, secId) = CreateEnv(StorageModes.Incremental);

		var tf = TimeSpan.FromMinutes(5);
		var date = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);

		await settings.GetStorage<CandleMessage>(secId, tf.TimeFrame()).SaveAsync(
		[
			CreateTfCandle(secId, date.AddMinutes(0), tf, 100, 105, 95, 102, 1000, totalTicks: 100),
		], token);

		await settings.GetStorage<ExecutionMessage>(secId, DataType.Ticks).SaveAsync(
		[
			CreateTick(secId, date.AddMinutes(5), tradeId: 1, price: 103, volume: 1),
			CreateTick(secId, date.AddMinutes(6), tradeId: 2, price: 104, volume: 2),
			CreateTick(secId, date.AddMinutes(9), tradeId: 3, price: 101, volume: 3),
		], token);

		var outMessages = new List<Message>();

		var result = settings.LoadMessages(provider, new MarketDataMessage
		{
			TransactionId = 1000,
			IsSubscribe = true,
			SecurityId = secId,
			DataType2 = tf.TimeFrame(),
			BuildMode = MarketDataBuildModes.LoadAndBuild,
			BuildFrom = DataType.Ticks,
			AllowBuildFromSmallerTimeFrame = false,
			From = date,
			To = date.AddMinutes(10),
			Count = 2,
		}, outMessages.Add);

		result.AssertNotNull();

		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToArray();
		candles.Length.AssertEqual(2);

		AssertTimeFrameCandle(candles[0], secId, tf, date, date.AddMinutes(5), 100, 105, 95, 102, totalVolume: 1000, totalTicks: 100, CandleStates.Finished, buildFrom: null, transactionId: 1000);
		AssertTimeFrameCandle(candles[1], secId, tf, date.AddMinutes(5), date.AddMinutes(9), 103, 104, 101, 101, totalVolume: 6, totalTicks: 3, CandleStates.Active, DataType.Ticks, transactionId: 1000);
	}

	[TestMethod]
	public async Task Candles_AllowBuildFromSmallerTimeFrame_BuildsHigherTimeFrameFromSmaller()
	{
		var token = CancellationToken;
		var (settings, provider, secId) = CreateEnv(StorageModes.Incremental);

		var smallTf = TimeSpan.FromMinutes(1);
		var bigTf = TimeSpan.FromMinutes(5);
		var date = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);

		var oneMinCandles = Enumerable.Range(0, 10)
			.Select(i => CreateTfCandle(secId, date.AddMinutes(i), smallTf, 100 + i, 101 + i, 99 + i, 100 + i, 100))
			.Cast<CandleMessage>()
			.ToArray();

		await settings.GetStorage<CandleMessage>(secId, smallTf.TimeFrame()).SaveAsync(oneMinCandles, token);

		var outMessages = new List<Message>();

		var result = settings.LoadMessages(provider, new MarketDataMessage
		{
			TransactionId = 1100,
			IsSubscribe = true,
			SecurityId = secId,
			DataType2 = bigTf.TimeFrame(),
			AllowBuildFromSmallerTimeFrame = true,
			BuildMode = MarketDataBuildModes.Load,
			From = date,
			To = date.AddMinutes(10),
			Count = 10,
		}, outMessages.Add);

		result.AssertNotNull();

		var bigCandles = outMessages.OfType<TimeFrameCandleMessage>().ToArray();
		bigCandles.Length.AssertEqual(2);

		AssertTimeFrameCandle(bigCandles[0], secId, bigTf, date, date.AddMinutes(4), 100, 105, 99, 104, totalVolume: 500, totalTicks: 20, CandleStates.Finished, smallTf.TimeFrame(), transactionId: 1100);
		AssertTimeFrameCandle(bigCandles[1], secId, bigTf, date.AddMinutes(5), date.AddMinutes(9), 105, 110, 104, 109, totalVolume: 500, totalTicks: 20, CandleStates.Finished, smallTf.TimeFrame(), transactionId: 1100);
	}

	[TestMethod]
	public async Task Candles_LoadAndBuild_AllowBuildFromSmallerTimeFrame_LoadsFromSmaller_ThenBuildsFromTicks()
	{
		var token = CancellationToken;
		var (settings, provider, secId) = CreateEnv(StorageModes.Incremental);

		var smallTf = TimeSpan.FromMinutes(1);
		var bigTf = TimeSpan.FromMinutes(5);
		var date = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);

		await settings.GetStorage<CandleMessage>(secId, smallTf.TimeFrame()).SaveAsync(
		[
			CreateTfCandle(secId, date.AddMinutes(0), smallTf, 100, 110, 90, 105, 10),
			CreateTfCandle(secId, date.AddMinutes(1), smallTf, 105, 108, 100, 102, 20),
			CreateTfCandle(secId, date.AddMinutes(2), smallTf, 102, 112, 101, 111, 30),
			CreateTfCandle(secId, date.AddMinutes(3), smallTf, 111, 115, 109, 110, 40),
			CreateTfCandle(secId, date.AddMinutes(4), smallTf, 110, 113, 107, 108, 50),
		], token);

		await settings.GetStorage<ExecutionMessage>(secId, DataType.Ticks).SaveAsync(
		[
			CreateTick(secId, date.AddMinutes(5), tradeId: 1, price: 200, volume: 2),
			CreateTick(secId, date.AddMinutes(5).AddSeconds(20), tradeId: 2, price: 198, volume: 1),
			CreateTick(secId, date.AddMinutes(6), tradeId: 3, price: 205, volume: 3),
			CreateTick(secId, date.AddMinutes(9).AddSeconds(50), tradeId: 4, price: 202, volume: 4),
		], token);

		var outMessages = new List<Message>();

		var result = settings.LoadMessages(provider, new MarketDataMessage
		{
			TransactionId = 1200,
			IsSubscribe = true,
			SecurityId = secId,
			DataType2 = bigTf.TimeFrame(),
			AllowBuildFromSmallerTimeFrame = true,
			BuildMode = MarketDataBuildModes.LoadAndBuild,
			BuildFrom = DataType.Ticks,
			From = date,
			To = date.AddMinutes(10),
			Count = 2,
		}, outMessages.Add);

		result.AssertNotNull();

		var candles = outMessages.OfType<TimeFrameCandleMessage>().ToArray();
		candles.Length.AssertEqual(2);

		// 10:00 candle is loaded via buildable storage from 1m candles, 10:05 candle is built from ticks.
		AssertTimeFrameCandle(candles[0], secId, bigTf, date, date.AddMinutes(4), 100, 115, 90, 108, totalVolume: 150, totalTicks: 20, CandleStates.Finished, smallTf.TimeFrame(), transactionId: 1200);
		AssertTimeFrameCandle(candles[1], secId, bigTf, date.AddMinutes(5), date.AddMinutes(9).AddSeconds(50), 200, 205, 198, 202, totalVolume: 10, totalTicks: 4, CandleStates.Active, DataType.Ticks, transactionId: 1200);
	}
}

namespace StockSharp.Tests;

using Ecng.Reflection;

using StockSharp.Algo.Storages.Csv;

static class Helper
{
	public static LogManager LogManager = new(false);

	public const string ResFolder = "../../../Resources/";

	public static SecurityLookupMessage LookupAll => StockSharp.Messages.Extensions.LookupAllCriteriaMessage;

	public static IStorageRegistry GetStorage(string path)
	{
		return new StorageRegistry
		{
			DefaultDrive = new LocalMarketDataDrive(path),
		};
	}

	public static IStorageRegistry GetResourceStorage()
		=> GetStorage(ResFolder);

	public const string TempFolder = "temp";

	public static string GetSubTemp(string subName)
	{
		var dir = Path.Combine(TempFolder, Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(dir);
		return Path.Combine(dir, subName);
	}

	public static void ClearTemp()
	{
		if (Directory.Exists(TempFolder))
			IOHelper.ClearDirectory(TempFolder);
		else
			Directory.CreateDirectory(TempFolder);
	}

	public static IEntityRegistry GetEntityRegistry()
		=> new CsvEntityRegistry(string.Empty);

	public static ExecutionMessage[] RandomTicks(this Security security, int count, bool generateOriginSide, TimeSpan? interval = null, DateTimeOffset? start = null)
	{
		var trades = new List<ExecutionMessage>();

		var secMsg = security.ToMessage();

		var tradeGenerator = new RandomWalkTradeGenerator(secMsg.SecurityId)
		{
			IdGenerator = new TickIncrementalIdGenerator(),
			Interval = interval ?? TimeSpan.FromMilliseconds(367),
			GenerateOriginSide = generateOriginSide
		};

		tradeGenerator.Init();

		tradeGenerator.Process(secMsg);

		var dt = start ?? DateTimeOffset.UtcNow;

		tradeGenerator.Process(new Level1ChangeMessage
		{
			SecurityId = secMsg.SecurityId,
			ServerTime = dt//.StorageTruncate()
		}.TryAdd(Level1Fields.LastTradePrice, security.LastTick.Price));

		//var rnd = new Random((int)DateTime.Now.Ticks);

		dt = dt.AddTicks(RandomGen.GetInt((int)tradeGenerator.Interval.Ticks));

		var totalIterations = count * 100;
		var i = 0;
		while (trades.Count < count)
		{
			if (totalIterations-- < 0)
				throw new InvalidOperationException();

			dt = dt.AddTicks(RandomGen.GetInt((int)tradeGenerator.Interval.Ticks));

			var msg = (ExecutionMessage)tradeGenerator.Process(new TimeMessage { ServerTime = dt });

			if (msg == null)
				continue;

			if (RandomGen.GetBool())
				msg.TradeId = ++i;
			else
				msg.TradeId = null;

			if (RandomGen.GetBool())
				msg.SeqNum = i;

			if (RandomGen.GetBool())
				msg.BuildFrom = DataType.OrderLog;

			if (RandomGen.GetBool())
				msg.OrderBuyId = RandomGen.GetInt(100, 10000);

			if (RandomGen.GetBool())
				msg.OrderSellId = RandomGen.GetInt(100, 10000);

			if (RandomGen.GetBool())
				msg.TradeStringId = Guid.NewGuid().To<string>();

			if (RandomGen.GetBool())
				msg.Yield = RandomGen.GetDecimal();

			if (RandomGen.GetBool())
				msg.IsUpTick = RandomGen.GetBool();

			if (RandomGen.GetBool())
				msg.OriginSide = RandomGen.GetEnum<Sides>();

			if (RandomGen.GetBool())
				msg.TradeStatus = RandomGen.GetLong();

			trades.Add(msg);
		}

		trades.Count.AssertEqual(count);

		return [.. trades];
	}

	public static ExecutionMessage[] RandomOrderLog(this Security security, int count, DateTimeOffset? start = null)
	{
		var items = new List<ExecutionMessage>();

		var secMsg = security.ToMessage();

		var generator = new OrderLogGenerator(secMsg.SecurityId)
		{
			IdGenerator = new TickIncrementalIdGenerator(),
			TradeGenerator = { IdGenerator = new TickIncrementalIdGenerator() }
		};

		generator.Init();

		generator.Process(secMsg);

		var dt = start ?? DateTimeOffset.UtcNow;

		generator.Process(new Level1ChangeMessage
		{
			SecurityId = secMsg.SecurityId,
			ServerTime = dt
		}.TryAdd(Level1Fields.LastTradePrice, security.LastTick.Price));

		if (generator.Interval.Ticks == 0)
			throw new InvalidOperationException();

		dt = dt.AddTicks(RandomGen.GetInt((int)generator.Interval.Ticks));

		var totalIterations = count * 100;
		var i = 0;
		while (items.Count < count)
		{
			if (totalIterations-- < 0)
				throw new InvalidOperationException();

			dt = dt.AddTicks((int)generator.Interval.Ticks);

			var ol = (ExecutionMessage)generator.Process(new TimeMessage
			{
				ServerTime = dt
			});

			if (ol == null)
				continue;

			items.Add(ol);

			if (RandomGen.GetBool())
				ol.SeqNum = ++i;

			if (RandomGen.GetBool())
			{
				ol.OrderStringId = Guid.NewGuid().To<string>();

				if (RandomGen.GetBool())
					ol.OrderId = null;
			}

			if (ol.TradePrice is not null)
			{
				if (RandomGen.GetBool())
				{
					ol.TradeStringId = Guid.NewGuid().To<string>();

					if (RandomGen.GetBool())
						ol.TradeId = default;
				}

				if (RandomGen.GetBool())
					ol.OrderBuyId = ol.OrderId;

				if (RandomGen.GetBool())
					ol.OrderSellId = ol.OrderId;

				if (RandomGen.GetBool())
					ol.Yield = RandomGen.GetDecimal();

				if (RandomGen.GetBool())
					ol.IsUpTick = RandomGen.GetBool();

				if (RandomGen.GetBool())
					ol.OriginSide = RandomGen.GetEnum<Sides>();

				if (RandomGen.GetBool())
					ol.TradeStatus = RandomGen.GetLong();
			}
		}

		items.Count.AssertEqual(count);

		return [.. items];
	}

	public static QuoteChangeMessage[] RandomDepths(this Security security, int count, TimeSpan? interval = null, DateTimeOffset? start = null, bool ordersCount = false)
	{
		var generator = new TrendMarketDepthGenerator(security.ToSecurityId())
		{
			GenerateOrdersCount = ordersCount
		};

		if (interval != null)
			generator.Interval = interval.Value;

		return security.RandomDepths(count, generator, start);
	}

	public static QuoteChangeMessage[] RandomDepths(this Security security, int count, TrendMarketDepthGenerator depthGenerator, DateTimeOffset? start = null)
	{
		depthGenerator.Init();

		var secMsg = security.ToMessage();

		depthGenerator.Process(secMsg);
		depthGenerator.Process(security.Board.ToMessage());

		var depths = new List<QuoteChangeMessage>();

		var dt = start ?? DateTimeOffset.UtcNow;

		var totalIterations = count * 100;
		var i = 0;
		while (depths.Count < count)
		{
			if (totalIterations-- < 0)
				throw new InvalidOperationException();

			dt = dt.AddTicks(RandomGen.GetInt((int)depthGenerator.Interval.Ticks));

			var msg = (QuoteChangeMessage)depthGenerator.Process(new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				SecurityId = secMsg.SecurityId,
				TradePrice = RandomGen.GetInt(100, 120),
				TradeVolume = RandomGen.GetInt(1, 20),
				ServerTime = dt,
			});

			if (msg is null)
				continue;

			if (RandomGen.GetBool())
				msg.SeqNum = i;

			if (RandomGen.GetBool())
				msg.BuildFrom = DataType.OrderLog;

			depths.Add(msg);
		}

		depths.Count.AssertEqual(count);

		return [.. depths];
	}

	private static readonly IEnumerable<Level1Fields> _l1Fields = [.. Enumerator.GetValues<Level1Fields>().ExcludeObsolete()];

	public static Level1ChangeMessage RandomLevel1(Security security, SecurityId secId, DateTimeOffset serverTime, bool isFractional, bool diffDays, bool diffTimeZones, Func<decimal> getTickPrice)
	{
		var msg = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = serverTime,
		};

		foreach (var field in _l1Fields)
		{
			if (!RandomGen.GetBool())
				continue;

			var type = field.ToType();

			if (type == null)
				continue;

			if (field == Level1Fields.PriceStep)
			{
				msg.Add(field, security.PriceStep ?? 0.01m);
				continue;
			}
			else if (field == Level1Fields.VolumeStep)
			{
				msg.Add(field, security.VolumeStep ?? 1);
				continue;
			}

			if (type == typeof(int))
				msg.Add(field, RandomGen.GetInt());
			else if (type == typeof(long))
				msg.Add(field, (long)RandomGen.GetInt());
			else if (type == typeof(bool))
				msg.Add(field, RandomGen.GetBool());
			else if (type == typeof(decimal))
			{
				var price = getTickPrice();

				if (RandomGen.GetBool())
				{
					price *= 10m.Pow(RandomGen.GetInt(1, 5));
				}

				if (isFractional)
					price /= 10m.Pow(RandomGen.GetInt(1, 5));

				msg.Add(field, price);
			}
			else if (type == typeof(DateTimeOffset))
			{
				var time = serverTime.AddMilliseconds(RandomGen.GetInt(-10, 10));

				if (diffDays)
					time = time.AddDays(RandomGen.GetInt(-10, 10));

				if (diffTimeZones)
					time = time.ConvertToEst();

				msg.Add(field, time);
			}
			else if (type.IsEnum)
			{
				var values = type.GetValues().ToArray();
				msg.Add(field, values[RandomGen.GetInt(values.Length - 1)]);
			}
			else if (type == typeof(string))
				msg.Add(field, Guid.NewGuid().To<string>());
		}

		return msg;
	}

	public static Level1ChangeMessage[] RandomLevel1(this Security security, bool isFractional = true, bool diffTimeZones = false, bool diffDays = false, int count = 100000)
	{
		var serverTime = DateTimeOffset.UtcNow;
		var securityId = security.ToSecurityId();

		var ticks = security.RandomTicks(count, false);

		var testValues = new List<Level1ChangeMessage>();

		var ticksIndex = 0;

		for (var i = 0; i < count; i++)
		{
			var msg = RandomLevel1(security, securityId, serverTime, isFractional, diffDays, diffTimeZones, () =>
			{
				var price = ticks[ticksIndex].TradePrice.Value;

				ticksIndex++;

				if (ticksIndex >= ticks.Length)
					ticksIndex = 0;

				return price;
			});

			testValues.Add(msg);

			if (RandomGen.GetBool())
				msg.SeqNum = i;

			if (RandomGen.GetBool())
				msg.BuildFrom = DataType.OrderLog;

			serverTime = serverTime.AddMilliseconds(RandomGen.GetInt(100000));
		}

		return [.. testValues];
	}

	private static readonly PositionChangeTypes[] _posFields =
		[.. Enumerator
			.GetValues<PositionChangeTypes>()
			.Where(t => t != PositionChangeTypes.Currency && t != PositionChangeTypes.State && !t.IsObsolete())];

	public static PositionChangeMessage RandomPositionChange(SecurityId secId)
	{
		var posMsg = new PositionChangeMessage
		{
			SecurityId = secId,
			ServerTime = DateTimeOffset.UtcNow,
			LocalTime = DateTimeOffset.UtcNow,
			PortfolioName = $"Pf{RandomGen.GetInt(2)}",
			ClientCode = RandomGen.GetBool() ? $"ClCode{RandomGen.GetInt(2)}" : null,
			DepoName = RandomGen.GetBool() ? $"Depo{RandomGen.GetInt(2)}" : null,
			LimitType = RandomGen.GetBool() ? (TPlusLimits)RandomGen.GetInt(365) : null,
			StrategyId = RandomGen.GetBool() ? Guid.NewGuid().To<string>() : null,
			BuildFrom = RandomGen.GetBool() ? DataType.Transactions : null,
			Side = RandomGen.GetBool() ? RandomGen.GetEnum<Sides>() : null,
		};

		if (RandomGen.GetBool())
			posMsg.Add(PositionChangeTypes.Currency, RandomGen.GetEnum<CurrencyTypes>());

		if (RandomGen.GetBool())
			posMsg.Add(PositionChangeTypes.State, RandomGen.GetEnum<PortfolioStates>());

		var type = _posFields[RandomGen.GetInt(_posFields.Length - 1)];

		var tt = type.ToType();

		if (tt == typeof(decimal))
		{
			var value = (decimal)RandomGen.GetInt(100, 1000) / RandomGen.GetInt(100, 1000);
			posMsg.Add(type, value.Round(5));
		}
		else if (tt == typeof(DateTimeOffset))
		{
			var time = posMsg.ServerTime.AddMilliseconds(RandomGen.GetInt(-10, 10));

			time = time.AddDays(RandomGen.GetInt(-10, 10));
			time = time.ConvertToEst();

			posMsg.Add(type, time);
		}

		return posMsg;
	}

	public static ExecutionMessage[] RandomTransactions(this Security security, int count)
	{
		var transactions = new List<ExecutionMessage>();

		var secId = security.ToSecurityId();

		for (var i = 0; i < count; i++)
		{
			transactions.Add(RandomTransaction(secId, i));
		}

		return [.. transactions];
	}

	public static ExecutionMessage RandomTransaction(SecurityId secId, int i)
	{
		var msg = new ExecutionMessage
		{
			SecurityId = secId,
			Currency = RandomGen.GetBool() ? RandomGen.GetEnum<CurrencyTypes>() : null,
			Commission = RandomGen.GetBool() ? RandomGen.GetInt(10) : null,
			IsSystem = RandomGen.GetBool() ? RandomGen.GetBool() : null,
			//IsUpTick = RandomGen.GetBool() ? RandomGen.GetBool() : (bool?)null,
			TransactionId = RandomGen.GetInt(),
			OriginalTransactionId = RandomGen.GetInt(),
			Error = RandomGen.GetInt(10) == 5 ? new InvalidOperationException("Test error") : null,
			Latency = RandomGen.GetBool() ? ((long)RandomGen.GetInt()).To<TimeSpan>() : null,
			DataTypeEx = DataType.Transactions,
			ServerTime = DateTimeOffset.UtcNow,
			LocalTime = DateTimeOffset.UtcNow,
			HasOrderInfo = RandomGen.GetBool(),
		};

		if (msg.HasOrderInfo)
		{
			msg.OrderPrice = RandomGen.GetInt(10);
			msg.OrderState = RandomGen.GetBool() ? RandomGen.GetEnum<OrderStates>() : null;
			msg.OrderStatus = RandomGen.GetBool() ? RandomGen.GetInt(-1000, 1000) : null;
			msg.TimeInForce = RandomGen.GetBool() ? RandomGen.GetEnum<TimeInForce>() : null;
			msg.Balance = RandomGen.GetBool() ? RandomGen.GetInt(10) : null;
			msg.OrderVolume = RandomGen.GetBool() ? RandomGen.GetInt(1, 10) : null;
			msg.Side = RandomGen.GetBool() ? Sides.Buy : Sides.Sell;
			msg.UserOrderId = RandomGen.GetBool() ? "test user id" + i : null;
			msg.ClientCode = RandomGen.GetBool() ? "test 'code'" + i : null;
			msg.PortfolioName = RandomGen.GetBool() ? "test pf" + i : null;
			msg.DepoName = RandomGen.GetBool() ? "test ;depo" + i : null;
			msg.OrderType = RandomGen.GetBool() ? RandomGen.GetEnum<OrderTypes>() : null;
			msg.VisibleVolume = RandomGen.GetBool() ? RandomGen.GetInt(10) : null;
			msg.Comment = RandomGen.GetBool() ? "test comment" + i : null;
			msg.SystemComment = RandomGen.GetBool() ? @"test
																comment" + i : null;
			msg.OrderBoardId = RandomGen.GetBool() ? "test board id" + i : null;
			msg.ExpiryDate = RandomGen.GetBool() ? DateTimeOffset.UtcNow : null;

			if (RandomGen.GetBool())
				msg.OrderId = RandomGen.GetInt();
			else
				msg.OrderStringId = RandomGen.GetBool() ? "test order id" + i : null;

			msg.IsMarketMaker = RandomGen.GetBool() ? RandomGen.GetBool() : null;
			msg.MarginMode = RandomGen.GetBool() ? RandomGen.GetEnum<MarginModes>() : null;
			msg.IsManual = RandomGen.GetBool() ? RandomGen.GetBool() : null;

			msg.MinVolume = RandomGen.GetBool() ? RandomGen.GetInt(1, 10) : null;
			msg.PositionEffect = RandomGen.GetBool() ? RandomGen.GetEnum<OrderPositionEffects>() : null;

			msg.Initiator = RandomGen.GetBool() ? RandomGen.GetBool() : null;

			msg.StrategyId = RandomGen.GetBool() ? Guid.NewGuid().To<string>() : null;
			msg.SeqNum = RandomGen.GetBool() ? RandomGen.GetInt(1, 10) : 0;
		}

		if (RandomGen.GetBool())
		{
			msg.TradeVolume = RandomGen.GetBool() ? RandomGen.GetInt(1, 10) : null;
			msg.TradePrice = RandomGen.GetInt(1, 10);
			msg.TradeStatus = RandomGen.GetBool() ? RandomGen.GetInt(10) : null;
			msg.OriginSide = RandomGen.GetBool() ? (RandomGen.GetBool() ? Sides.Buy : Sides.Sell) : null;
			msg.OpenInterest = RandomGen.GetBool() ? RandomGen.GetInt(10000) : null;
			msg.PnL = RandomGen.GetBool() ? RandomGen.GetInt(10) : null;
			msg.Position = RandomGen.GetBool() ? RandomGen.GetInt(10) : null;
			msg.Slippage = RandomGen.GetBool() ? RandomGen.GetInt(10) : null;

			if (RandomGen.GetBool())
				msg.TradeId = RandomGen.GetInt();
			else
				msg.TradeStringId = RandomGen.GetBool() ? "test trade id" + i : null;
		}

		return msg;
	}

	public static PositionChangeMessage[] RandomPositionChanges(this Security security, int count = 10000)
	{
		var testValues = new List<PositionChangeMessage>();

		var secId = security.ToSecurityId();

		for (var i = 0; i < count; i++)
		{
			testValues.Add(RandomPositionChange(secId));
		}

		return [.. testValues];
	}

	public static NewsMessage[] RandomNews()
	{
		return
		[
			new NewsMessage
			{
				Headline = "Headline 1",
				Source = "reuters",
				BoardCode = BoardCodes.Forts,
				ServerTime = DateTimeOffset.UtcNow,
				SeqNum = RandomGen.GetInt(0, 100),
			},

			new NewsMessage
			{
				Headline = "Headline 2",
				Url = "http://google.com",
				SecurityId = "AAPL@NASDAQ".ToSecurityId(),
				ServerTime = DateTimeOffset.UtcNow,
			},

			new NewsMessage
			{
				Headline = "Headline 3",
				Priority = NewsPriorities.High,
				ServerTime = DateTimeOffset.UtcNow,
				ExpiryDate = DateTimeOffset.UtcNow,
				SeqNum = RandomGen.GetInt(0, 100),
			},

			new NewsMessage
			{
				Headline = "Headline 4",
				Language = "FR",
				ServerTime = DateTimeOffset.UtcNow,
			},
		];
	}

	public static BoardStateMessage[] RandomBoardStates()
	{
		return
		[
			new BoardStateMessage
			{
				State = SessionStates.Active,
				BoardCode = ExchangeBoard.Forts.Code,
				ServerTime = DateTimeOffset.UtcNow,
			},

			new BoardStateMessage
			{
				State = SessionStates.Paused,
				ServerTime = DateTimeOffset.UtcNow,
			},
		];
	}

	public static SecurityMessage[] RandomSecurities(int count = 10000)
	{
		var securities = new List<SecurityMessage>();

		for (var i = 0; i < count; i++)
		{
			var s = new SecurityMessage
			{
				SecurityId = CreateSecurityId(),
				Name = "TestName",
				PriceStep = RandomGen.GetBool() ? RandomGen.GetDecimal(5, 5) : null,
				VolumeStep = RandomGen.GetBool() ? RandomGen.GetDecimal(5, 5) : null,
				Decimals = RandomGen.GetBool() ? RandomGen.GetInt(1, 10) : null,
				Multiplier = RandomGen.GetBool() ? RandomGen.GetDecimal(5, 5) : null,
				SecurityType = RandomGen.GetBool() ? RandomGen.GetEnum<SecurityTypes>() : null,
				Currency = RandomGen.GetBool() ? RandomGen.GetEnum<CurrencyTypes>() : null,
			};

			if (s.SecurityType == SecurityTypes.Option)
			{
				s.OptionType = RandomGen.GetEnum<OptionTypes>();
				s.Strike = (decimal)RandomGen.GetInt(1, 100) / RandomGen.GetInt(1, 100);
			}

			securities.Add(s);
		}

		return [.. securities];
	}

	public static BoardMessage[] RandomBoards(int count = 10000)
	{
		var boards = new List<BoardMessage>();
		var timeZones = TimeZoneInfo.GetSystemTimeZones();

		for (var i = 0; i < count; i++)
		{
			var b = new BoardMessage
			{
				Code = "TestBoard" + Guid.NewGuid().GetFileNameWithoutExtension(null),
				ExchangeCode = "TestBoardName",
				ExpiryTime = RandomGen.GetBool() ? new TimeSpan(RandomGen.GetInt(0, 18), 0, 0) : default,
				TimeZone = RandomGen.GetElement(timeZones),
				WorkingTime = new()
				{
					IsEnabled = RandomGen.GetBool(),
				},
			};
			boards.Add(b);
		}

		return [.. boards];
	}

	public static void DeleteWithCheck<T>(this IMarketDataStorage<T> storage)
		where T : Message
	{
		storage.Delete();
		storage.Load().Count().AssertEqual(0);
	}

	public static SecurityId CreateSecurityId()
	{
		return new() { SecurityCode = "TestSecurity" + Guid.NewGuid().GetFileNameWithoutExtension(null), BoardCode = BoardCodes.Test };
	}

	public static Security CreateSecurity(decimal lastTickPrice = default)
	{
		var security = new Security
		{
			Id = "TestSecurity" + Guid.NewGuid().GetFileNameWithoutExtension(null),
			Code = "TestCode",
			//Name = "TestName",
			PriceStep = 0.1m,
			StepPrice = 2,
			//Decimals = 1,
			Board = ExchangeBoard.Test
		};
		security.LastTick = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = security.ToSecurityId(),
			TradePrice = lastTickPrice,
		};
		return security;
	}

	public static Security CreateStorageSecurity()
	{
		var security = CreateSecurity();
		security.BestBid = new() { Price = 87 };
		return security;
	}

	public static Portfolio CreatePortfolio()
	{
		return Portfolio.CreateSimulator();
	}

	public static void CheckEqual<T>(T expected, T actual, bool isMls = false, bool isSerializer = false, bool checkExtended = false)
	{
		if (expected.IsNull(true) && actual.IsNull(true))
			return;

		expected.AssertNotNull();

		var type = expected.GetType();

		void CheckPosition()
		{
			var p1 = expected.To<Position>();
			var p2 = actual.To<Position>();

			p2.CurrentValue.AssertEqual(p1.CurrentValue);
			p2.BlockedValue.AssertEqual(p1.BlockedValue);
			p2.BeginValue.AssertEqual(p1.BeginValue);
			p2.LastChangeTime.AssertEqual(p1.LastChangeTime);
			CheckEqual(p1.Security, p2.Security, isMls, isSerializer);
			CheckEqual(p1.Portfolio, p2.Portfolio, isMls, isSerializer);
			p2.BeginValue.AssertEqual(p1.BeginValue);
			p2.Commission.AssertEqual(p1.Commission);
			p2.Leverage.AssertEqual(p1.Leverage);
			p2.Side.AssertEqual(p1.Side);
		}

		if (type == typeof(Security))
		{
			var s1 = expected.To<Security>();
			var s2 = actual.To<Security>();
			//CheckEntity(s1.BestAsk, s2.BestAsk);
			//CheckEntity(s1.BestBid, s2.BestBid);
			s2.Id.AssertEqual(s1.Id);
			s2.Class.AssertEqual(s1.Class);
			s2.Code.AssertEqual(s1.Code);
			s2.Decimals.AssertEqual(s1.Decimals);
			CheckEqual(s1.Board, s2.Board, isMls, isSerializer);
			s2.ExpiryDate.AssertEqual(s1.ExpiryDate);
			//CheckEntity(s1.LastTrade, s2.LastTrade);

			if (!isSerializer)
			{
				s2.OpenPrice.AssertEqual(s1.OpenPrice);
				s2.HighPrice.AssertEqual(s1.HighPrice);
				s2.LowPrice.AssertEqual(s1.LowPrice);
				s2.ClosePrice.AssertEqual(s1.ClosePrice);

				s2.MarginBuy.AssertEqual(s1.MarginBuy);
				s2.MarginSell.AssertEqual(s1.MarginSell);

				s2.MaxPrice.AssertEqual(s1.MaxPrice);
				s2.MinPrice.AssertEqual(s1.MinPrice);

				s2.StepPrice.AssertEqual(s1.StepPrice);
				s2.State.AssertEqual(s1.State);
			}

			s2.VolumeStep.AssertEqual(s1.VolumeStep);
			s2.PriceStep.AssertEqual(s1.PriceStep);
			s2.Multiplier.AssertEqual(s1.Multiplier);
			s2.Name.AssertEqual(s1.Name);
			s2.SettlementDate.AssertEqual(s1.SettlementDate);
			s2.ShortName.AssertEqual(s1.ShortName);
			s2.Type.AssertEqual(s1.Type);
			s2.OptionType.AssertEqual(s1.OptionType);
			s2.Strike.AssertEqual(s1.Strike);
			//s2.BinaryOptionType.AssertEqual(s1.BinaryOptionType);
		}
		else if (type == typeof(Order))
		{
			var o1 = expected.To<Order>();
			var o2 = actual.To<Order>();

			CheckEqual(o1.ToMessage(), o2.ToMessage(), isMls, isSerializer);
			//CheckEntity(o1.DerivedOrder, o2.DerivedOrder, isMls, isSerializer);
		}
		else if (type == typeof(OrderFail))
		{
			var o1 = expected.To<OrderFail>();
			var o2 = actual.To<OrderFail>();

			o2.Error.AssertEqual(o1.Error);
			CheckEqual(o1.Order, o2.Order, isMls, isSerializer);
			o2.SeqNum.AssertEqual(o1.SeqNum);
		}
		else if (type == typeof(MyTrade))
		{
			var t1 = expected.To<MyTrade>();
			var t2 = actual.To<MyTrade>();

			CheckEqual(t1.Trade, t2.Trade, isMls, isSerializer);
			CheckEqual(t1.Order, t2.Order, isMls, isSerializer);
		}
		else if (type == typeof(Position))
		{
			CheckPosition();
		}
		else if (type == typeof(Portfolio))
		{
			var p1 = expected.To<Portfolio>();
			var p2 = actual.To<Portfolio>();

			p2.Name.AssertEqual(p1.Name);
			p2.State.AssertEqual(p1.State);

			CheckPosition();
		}
#pragma warning disable CS0618 // Type or member is obsolete
		else if (type == typeof(Trade))
		{
			var t1 = expected.To<Trade>().ToMessage();
			var t2 = actual.To<Trade>().ToMessage();

			CheckEqual(t1, t2, isMls, isSerializer);
		}
		else if (type == typeof(MarketDepth))
		{
			var e = expected.To<MarketDepth>().ToMessage();
			var a = actual.To<MarketDepth>().ToMessage();

			CheckEqual(e, a, isMls, isSerializer);
		}
		else if (type == typeof(OrderLogItem))
		{
			var q1 = expected.To<OrderLogItem>();
			var q2 = actual.To<OrderLogItem>();

			CheckEqual(q1.Order, q2.Order, isMls, isSerializer);
			CheckEqual(q1.Trade, q2.Trade, isMls, isSerializer);
		}
#pragma warning restore CS0618 // Type or member is obsolete
		else if (type == typeof(IDictionary<string, object>))
		{
			var d1 = expected.To<IDictionary<string, object>>();
			var d2 = actual.To<IDictionary<string, object>>();

			if (d1 == null)
			{
				d2.AssertNull();
				return;
			}

			d2.Count.AssertEqual(d1.Count);

			for (var i = 0; i < d1.Count; i++)
			{
				var p1 = d1.ElementAt(i);
				var p2 = d2.ElementAt(i);

				p2.Key.AssertEqual(p1.Key);
				p2.Value.AssertEqual(p1.Value);
			}
		}
		else if (type == typeof(ExchangeBoard))
		{
			var e = expected.To<ExchangeBoard>();
			var a = actual.To<ExchangeBoard>();

			CheckEqual(e.ToMessage(), a.ToMessage(), isMls, isSerializer);
			CheckEqual(e.Exchange, a.Exchange, isMls, isSerializer);
		}
		else if (type == typeof(Exchange))
		{
			var ex1 = expected.To<Exchange>();
			var ex2 = actual.To<Exchange>();

			ex1.AssertEqual(ex2);

			ex2.CountryCode.AssertEqual(ex1.CountryCode);
			ex2.FullNameLoc.AssertEqual(ex1.FullNameLoc);
		}
		else if (type == typeof(News))
		{
			var n1 = expected.To<News>();
			var n2 = actual.To<News>();

			CheckEqual(n1.ToMessage(), n2.ToMessage(), isMls, isSerializer);
		}
		else if (type == typeof(NewsMessage))
		{
			var n1 = expected.To<NewsMessage>();
			var n2 = actual.To<NewsMessage>();

			n2.BoardCode.AssertEqual(n1.BoardCode, true);
			n2.Headline.AssertEqual(n1.Headline, true);
			n2.Id.AssertEqual(n1.Id, true);
			n2.SecurityId.AssertEqual(n1.SecurityId);
			n2.ServerTime.AssertEqual(n1.ServerTime);
			n2.Source.AssertEqual(n1.Source, true);
			n2.Story.AssertEqual(n1.Story, true);
			n2.Url.AssertEqual(n1.Url, true);
			n2.Priority.AssertEqual(n1.Priority);
			n2.Language.AssertEqual(n1.Language, true);
			n2.ExpiryDate.AssertEqual(n1.ExpiryDate);
			n2.SeqNum.AssertEqual(n1.SeqNum);
		}
		else if (type == typeof(BoardStateMessage))
		{
			var n1 = expected.To<BoardStateMessage>();
			var n2 = actual.To<BoardStateMessage>();

			n2.ServerTime.AssertEqual(n1.ServerTime);
			n2.BoardCode.AssertEqual(n1.BoardCode);
			n2.State.AssertEqual(n1.State);
			n2.ServerTime.AssertEqual(n1.ServerTime);
		}
		else if (type == typeof(Level1ChangeMessage))
		{
			var e = expected.To<Level1ChangeMessage>();
			var a = actual.To<Level1ChangeMessage>();

			a.SecurityId.AssertEqual(e.SecurityId);
			a.ServerTime.AssertEqual(e.ServerTime);

			a.SeqNum.AssertEqual(e.SeqNum);
			a.BuildFrom.AssertEqual(e.BuildFrom);

			if (!isSerializer)
			{
				var d1 = e.Changes;
				var d2 = a.Changes;

				//d2.Count.AssertEqual(d1.Count);

				var notFound = new HashSet<Level1Fields>();

				foreach (var p1 in d1)
				{
					if (!d2.TryGetValue(p1.Key, out var value))
					{
						notFound.Add(p1.Key);
						continue;
					}

					var expectedValue = p1.Value;

					if (expectedValue is DateTimeOffset dto)
						expectedValue = dto.TruncateTime(isMls);

					value.AssertEqual(expectedValue);
				}

				notFound.Count.AssertEqual(0);
			}
		}
		else if (type == typeof(PositionChangeMessage))
		{
			var e = expected.To<PositionChangeMessage>();
			var a = actual.To<PositionChangeMessage>();

			a.SecurityId.AssertEqual(e.SecurityId);
			a.ServerTime.AssertEqual(e.ServerTime);
			a.PortfolioName.AssertEqual(e.PortfolioName);
			a.ClientCode.AssertEqual(e.ClientCode, true);
			a.BoardCode.AssertEqual(e.BoardCode, true);
			a.Description.AssertEqual(e.Description, true);
			a.DepoName.AssertEqual(e.DepoName, true);
			a.LimitType.AssertEqual(e.LimitType);
			(a.StrategyId ?? string.Empty).AssertEqual(e.StrategyId ?? string.Empty);
			a.BuildFrom.AssertEqual(e.BuildFrom);
			a.Side.AssertEqual(e.Side);

			if (!isSerializer)
			{
				var d1 = e.Changes;
				var d2 = a.Changes;

				//d2.Count.AssertEqual(d1.Count);

				var notFound = new HashSet<PositionChangeTypes>();

				foreach (var p1 in d1)
				{
					if (!d2.TryGetValue(p1.Key, out var value))
					{
						notFound.Add(p1.Key);
						continue;
					}

					value.AssertEqual(p1.Value);
				}

				notFound.Count.AssertEqual(0);
			}
		}
		else if (type == typeof(QuoteChangeMessage))
		{
			var e = expected.To<QuoteChangeMessage>();
			var a = actual.To<QuoteChangeMessage>();

			a.SecurityId.AssertEqual(e.SecurityId);
			a.ServerTime.AssertEqual(e.ServerTime);
			a.Currency.AssertEqual(e.Currency);
			a.HasPositions.AssertEqual(e.HasPositions);
			a.State.AssertEqual(e.State);
			a.BuildFrom.AssertEqual(e.BuildFrom);
			a.IsFiltered.AssertEqual(e.IsFiltered);
			a.SeqNum.AssertEqual(e.SeqNum);

			a.Asks.Length.AssertEqual(e.Asks.Length);
			a.Bids.Length.AssertEqual(e.Bids.Length);

			for (var i = 0; i < e.Asks.Length; i++)
				CheckEqual(a.Asks[i], e.Asks[i], isMls, isSerializer);

			for (var i = 0; i < e.Bids.Length; i++)
				CheckEqual(a.Bids[i], e.Bids[i], isMls, isSerializer);
		}
		else if (type == typeof(QuoteChange))
		{
			var e = expected.To<QuoteChange>();
			var a = actual.To<QuoteChange>();

			a.Price.AssertEqual(e.Price);
			a.Volume.AssertEqual(e.Volume);
			a.OrdersCount.AssertEqual(e.OrdersCount);
			a.BoardCode.AssertEqual(e.BoardCode);
			a.Condition.AssertEqual(e.Condition);
			a.Action.AssertEqual(e.Action);
			a.StartPosition.AssertEqual(e.StartPosition);
			a.EndPosition.AssertEqual(e.EndPosition);
		}
		else if (type == typeof(ExecutionMessage))
		{
			var e = expected.To<ExecutionMessage>();
			var a = actual.To<ExecutionMessage>();

			a.Balance.AssertEqual(e.Balance);
			a.ServerTime.AssertEqual(e.ServerTime.TruncateTime(isMls));
			//a.LocalTime.AssertEqual(e.LocalTime.TruncateTime(isMls));
			a.Comment.AssertEqual(e.Comment, true);
			a.SystemComment.AssertEqual(e.SystemComment, true);
			a.TimeInForce.AssertEqual(e.TimeInForce);
			a.Side.AssertEqual(e.Side);
			a.OrderId.AssertEqual(e.OrderId);
			a.OrderStringId.AssertEqual(e.OrderStringId, true);
			a.UserOrderId.AssertEqual(e.UserOrderId, true);
			a.OrderBoardId.AssertEqual(e.OrderBoardId, true);
			a.Latency.AssertEqual(e.Latency);
			a.PortfolioName.AssertEqual(e.PortfolioName, true);
			a.OrderPrice.AssertEqual(e.OrderPrice);
			a.SecurityId.AssertEqual(e.SecurityId);
			a.OrderState.AssertEqual(e.OrderState);
			a.OrderStatus.AssertEqual(e.OrderStatus);
			CheckEqual(e.Condition, a.Condition, isMls, isSerializer);
			a.TransactionId.AssertEqual(e.TransactionId);
			a.OrderType.AssertEqual(e.OrderType);
			a.OrderVolume.AssertEqual(e.OrderVolume);
			a.IsSystem.AssertEqual(e.IsSystem);
			var t = a.ExpiryDate?.TruncateTime(isMls);
			t.AssertEqual(e.ExpiryDate?.TruncateTime(isMls));
			a.Currency.AssertEqual(e.Currency);
			a.BrokerCode.AssertEqual(e.BrokerCode, true);
			a.ClientCode.AssertEqual(e.ClientCode, true);
			a.Commission.AssertEqual(e.Commission);
			a.IsMarketMaker.AssertEqual(e.IsMarketMaker);
			a.MarginMode.AssertEqual(e.MarginMode);
			a.IsManual.AssertEqual(e.IsManual);
			a.TradeId.AssertEqual(e.TradeId);
			a.TradeStringId.AssertEqual(e.TradeStringId, true);
			a.OriginSide.AssertEqual(e.OriginSide);
			a.TradePrice.AssertEqual(e.TradePrice);
			a.TradeVolume.AssertEqual(e.TradeVolume);
			a.IsUpTick.AssertEqual(e.IsUpTick);
			a.Currency.AssertEqual(e.Currency);
			a.MinVolume.AssertEqual(e.MinVolume);
			a.PositionEffect.AssertEqual(e.PositionEffect);
			a.PostOnly.AssertEqual(e.PostOnly);
			a.Initiator.AssertEqual(e.Initiator);
			a.SeqNum.AssertEqual(e.SeqNum);
			(a.StrategyId ?? string.Empty).AssertEqual(e.StrategyId ?? string.Empty);
			a.BuildFrom.AssertEqual(e.BuildFrom);
			a.OrderBuyId.AssertEqual(e.OrderBuyId);
			a.OrderSellId.AssertEqual(e.OrderSellId);
		}
		else if (type == typeof(BoardLookupMessage))
		{
			var e = expected.To<BoardLookupMessage>();
			var a = actual.To<BoardLookupMessage>();

			a.TransactionId.AssertEqual(e.TransactionId);
			a.IsSubscribe.AssertEqual(e.IsSubscribe);
			a.Like.AssertEqual(e.Like);
		}
		else if (type == typeof(BoardMessage))
		{
			var e = expected.To<BoardMessage>();
			var a = actual.To<BoardMessage>();

			a.ExpiryTime.AssertEqual(e.ExpiryTime);
			//a.IsMicex().AssertEqual(e.IsMicex());
			//a.IsUxStock().AssertEqual(e.IsUxStock());
			//a.IsSupportAtomicReRegister.AssertEqual(e.IsSupportAtomicReRegister);
			//a.IsSupportMarketOrders.AssertEqual(e.IsSupportMarketOrders);
			a.TimeZone.AssertEqual(e.TimeZone);

			a.WorkingTime.SpecialHolidays.Length.AssertEqual(e.WorkingTime.SpecialHolidays.Length);

			for (var i = 0; i < e.WorkingTime.SpecialHolidays.Length; i++)
			{
				var p1 = e.WorkingTime.SpecialHolidays.ElementAt(i);
				var p2 = a.WorkingTime.SpecialHolidays.ElementAt(i);

				p2.AssertEqual(p1);
			}

			a.WorkingTime.SpecialWorkingDays.Length.AssertEqual(e.WorkingTime.SpecialWorkingDays.Length);

			for (var i = 0; i < e.WorkingTime.SpecialWorkingDays.Length; i++)
			{
				var p1 = e.WorkingTime.SpecialWorkingDays.ElementAt(i);
				var p2 = a.WorkingTime.SpecialWorkingDays.ElementAt(i);

				p2.AssertEqual(p1);
			}

			a.WorkingTime.Periods.Count.AssertEqual(e.WorkingTime.Periods.Count);

			for (var i = 0; i < e.WorkingTime.Periods.Count; i++)
			{
				var p1 = e.WorkingTime.Periods.ElementAt(i);
				var p2 = a.WorkingTime.Periods.ElementAt(i);

				p2.Till.AssertEqual(p1.Till);

				p2.Times.Count.AssertEqual(p1.Times.Count);

				for (var j = 0; j < p1.Times.Count; j++)
				{
					var t1 = p1.Times.ElementAt(j);
					var t2 = p2.Times.ElementAt(j);

					t2.AssertEqual(t1);
				}
			}
		}
		else if (type == typeof(ChangePasswordMessage))
		{
			var e = expected.To<ChangePasswordMessage>();
			var a = actual.To<ChangePasswordMessage>();

			a.TransactionId.AssertEqual(e.TransactionId);

			if (!isSerializer)
			{
				a.Error.AssertEqual(e.Error);
				a.NewPassword.AssertEqual(e.NewPassword);
			}
		}
		else if (type == typeof(CommandMessage))
		{
			var e = expected.To<CommandMessage>();
			var a = actual.To<CommandMessage>();

			a.Command.AssertEqual(e.Command);
			a.Scope.AssertEqual(e.Scope);
			a.ObjectId.AssertEqual(e.ObjectId);
		}
		else if (type == typeof(ConnectMessage) || type == typeof(DisconnectMessage))
		{
			var e = expected.To<BaseConnectionMessage>();
			var a = actual.To<BaseConnectionMessage>();

			if (!isSerializer)
			{
				a.Error.AssertEqual(e.Error);
			}
		}
		else if (type == typeof(ErrorMessage))
		{
			var e = expected.To<ErrorMessage>();
			var a = actual.To<ErrorMessage>();

			a.OriginalTransactionId.AssertEqual(e.OriginalTransactionId);

			if (!isSerializer)
			{
				a.Error.AssertEqual(e.Error);
			}
		}
		else if (type == typeof(SettingsStorage))
		{
			var e = expected.To<SettingsStorage>();
			var a = actual.To<SettingsStorage>();

			a.Count.AssertEqual(e.Count);

			if (!isSerializer)
			{
				foreach (var key in a.Keys.OrderBy())
				{
					CheckEqual(a[key], e[key]);
				}
			}
		}
		else if (type == typeof(SettingsStorage[]))
		{
			var e = expected.To<SettingsStorage[]>();
			var a = actual.To<SettingsStorage[]>();

			a.Length.AssertEqual(e.Length);

			for (var i = 0; i < a.Length; i++)
			{
				CheckEqual(a[i], e[i]);
			}
		}
		else if (type.Is<ICandleMessage>())
		{
			var e = expected.To<ICandleMessage>();
			var a = actual.To<ICandleMessage>();

			a.OpenTime.AssertEqual(e.OpenTime.TruncateTime(isMls));

			if (checkExtended)
			{
				a.CloseTime.AssertEqual(e.CloseTime.TruncateTime(isMls));
				a.HighTime.AssertEqual(e.HighTime.TruncateTime(isMls));
				a.LowTime.AssertEqual(e.LowTime.TruncateTime(isMls));
			}

			a.OpenPrice.AssertEqual(e.OpenPrice);
			a.HighPrice.AssertEqual(e.HighPrice);
			a.LowPrice.AssertEqual(e.LowPrice);
			a.ClosePrice.AssertEqual(e.ClosePrice);

			if (checkExtended)
			{
				a.OpenVolume.AssertEqual(e.OpenVolume);
				a.HighVolume.AssertEqual(e.HighVolume);
				a.LowVolume.AssertEqual(e.LowVolume);
				a.CloseVolume.AssertEqual(e.CloseVolume);
			}

			a.TotalVolume.AssertEqual(e.TotalVolume);

			if (checkExtended)
			{
				a.BuyVolume.AssertEqual(e.BuyVolume);
				a.SellVolume.AssertEqual(e.SellVolume);
			}

			a.DataType.AssertEqual(e.DataType);

			if (e.PriceLevels is null)
			{
				a.PriceLevels.AssertNull();
				return;
			}

			a.PriceLevels.AssertNotNull();

			var expectedLevels = e.PriceLevels.ToArray();
			var actualLevels = a.PriceLevels.ToArray();

			actualLevels.Length.AssertEqual(expectedLevels.Length);

			var j = 0;

			foreach (var expectedLevel in expectedLevels)
			{
				var actualLevel = actualLevels[j++];

				actualLevel.BuyCount.AssertEqual(expectedLevel.BuyCount);
				actualLevel.BuyVolume.AssertEqual(expectedLevel.BuyVolume);
				actualLevel.TotalVolume.AssertEqual(expectedLevel.TotalVolume);
				actualLevel.Price.AssertEqual(expectedLevel.Price);
				actualLevel.SellCount.AssertEqual(expectedLevel.SellCount);
				actualLevel.SellVolume.AssertEqual(expectedLevel.SellVolume);

				//var k = 0;

				if (expectedLevel.BuyVolumes is null)
				{
					actualLevel.BuyVolumes.AssertNull();
				}
				else
				{
					// TODO построение детализованных уровне было выключено по причине оптимизации

					//actualLevel.BuyVolumes.Count().AssertEqual(expectedLevel.BuyVolumes.Count());

					//foreach (var expecedBuyVolume in expectedLevel.BuyVolumes)
					//{
					//	var actualBuyVolume = actualLevel.BuyVolumes.ElementAt(k++);

					//	actualBuyVolume.AssertEqual(expecedBuyVolume);
					//}
				}

				//k = 0;

				if (expectedLevel.SellVolumes is null)
				{
					actualLevel.SellVolumes.AssertNull();
				}
				else
				{
					//actualLevel.SellVolumes.Count().AssertEqual(expectedLevel.SellVolumes.Count());

					//foreach (var expecedSellVolume in expectedLevel.SellVolumes)
					//{
					//	var actualSellVolume = actualLevel.SellVolumes.ElementAt(k++);

					//	actualSellVolume.AssertEqual(expecedSellVolume);
					//}
				}
			}
		}
		else if (type == typeof(WorkingTime))
		{
			var e = expected.To<WorkingTime>();
			var a = actual.To<WorkingTime>();

			a.IsEnabled.AssertEqual(e.IsEnabled);

			a.Periods.Count.AssertEqual(e.Periods.Count);

			for (var i = 0; i < a.Periods.Count; i++)
				CheckEqual(a.Periods[i], e.Periods[i]);

			a.SpecialDays.Count.AssertEqual(e.SpecialDays.Count);

			var aSpecialDays = a.SpecialDays.ToArray();
			var eSpecialDays = e.SpecialDays.ToArray();

			for (var i = 0; i < aSpecialDays.Length; i++)
			{
				aSpecialDays[i].Key.AssertEqual(eSpecialDays[i].Key);
				aSpecialDays[i].Value.Length.AssertEqual(eSpecialDays[i].Value.Length);

				for (var j = 0; j < aSpecialDays[i].Value.Length; j++)
				{
					aSpecialDays[i].Value[j].AssertEqual(eSpecialDays[i].Value[j]);
				}
			}
		}
		else
			actual.AssertEqual(expected);
	}

	public static void CompareMessages<T>(this IList<T> actual, IList<T> expected)
		where T : IServerTimeMessage
	{
		actual.Count.AssertEqual(expected.Count);

		for (var i = 0; i < expected.Count; i++)
		{
			var d1 = expected[i];
			var d2 = actual.ElementAt(i);

			CheckEqual(d1, d2);
		}
	}

	public static void CompareCandles<T>(this T[] actualCandles, T[] expectedCandles, StorageFormats format)
		where T : ICandleMessage
	{
		var checkExtended = format != StorageFormats.Csv;

		CompareCandles(actualCandles, expectedCandles, checkExtended);
	}

	public static void CompareCandles<T>(this T[] actualCandles, T[] expectedCandles, bool checkExtended = true)
		where T : ICandleMessage
	{
		actualCandles.Length.AssertEqual(expectedCandles.Length);

		var i = 0;
		foreach (var c1 in actualCandles)
		{
			var c2 = expectedCandles[i++];
			CheckEqual(c2, c1, checkExtended: checkExtended);
		}
	}

	public static DateTimeOffset TruncateTime(this DateTimeOffset dt, bool isMls)
	{
		return isMls ? dt.Truncate(TimeSpan.FromMilliseconds(1)) : dt;
	}

	public static INativeIdStorage CreateNativeIdStorage()
	{
		return new InMemoryNativeIdStorage();
	}

	public static IEnumerable<TCandle> IsAllFinished<TCandle>(this IEnumerable<TCandle> candles)
		where TCandle : CandleMessage
	{
		foreach (var candle in candles)
		{
			candle.State.AssertEqual(CandleStates.Finished);
		}

		return candles;
	}

	public static PropertyInfo[] GetModifiableProps(this Type type)
		=> [.. type
			.GetProperties(BindingFlags.Instance | BindingFlags.Public)
			.Where(p => p.IsModifiable())];
}

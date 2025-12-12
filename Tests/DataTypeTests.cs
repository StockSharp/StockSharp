namespace StockSharp.Tests;

[TestClass]
public class DataTypeTests : BaseTestClass
{
	[TestMethod]
	public void Equality_And_HashCode()
	{
		var dt1 = DataType.Create<Level1ChangeMessage>();
		var dt2 = DataType.Level1.Clone();

		dt1.Equals(dt2).AssertTrue();
		dt2.GetHashCode().AreEqual(dt1.GetHashCode());

		var dt3 = DataType.Create<Level1ChangeMessage>(1);
		dt1.Equals(dt3).AssertFalse();
	}

	[TestMethod]
	public void IsSecurityRequired_IsNonSecurity()
	{
		// Candles
		var tf = DataType.Create<TimeFrameCandleMessage>(TimeSpan.FromMinutes(1));
		tf.IsCandles.AssertTrue();
		tf.IsMarketData.AssertTrue();
		tf.IsSecurityRequired.AssertTrue();
		tf.IsNonSecurity.AssertFalse();

		// Specific TF candles shortcut
		DataType.CandleTimeFrame.IsCandles.AssertTrue();
		DataType.CandleTimeFrame.IsTFCandles.AssertTrue();

		// Level1 and MarketDepth
		DataType.Level1.IsMarketData.AssertTrue();
		DataType.Level1.IsSecurityRequired.AssertTrue();
		DataType.MarketDepth.IsMarketData.AssertTrue();
		DataType.MarketDepth.IsSecurityRequired.AssertTrue();

		// Executions: Tick is market-data, Transaction is not
		DataType.Ticks.IsMarketData.AssertTrue();
		DataType.Ticks.IsSecurityRequired.AssertTrue();
		DataType.Transactions.IsMarketData.AssertFalse();
		DataType.Transactions.IsSecurityRequired.AssertFalse();

		// Non-security types
		DataType.Securities.IsNonSecurity.AssertTrue();
		DataType.News.IsNonSecurity.AssertTrue();
		DataType.Board.IsNonSecurity.AssertTrue();
		DataType.BoardState.IsNonSecurity.AssertTrue();
		DataType.DataTypeInfo.IsNonSecurity.AssertTrue();
	}

	[TestMethod]
	public void CandleSources_Contains_Expected()
	{
		Contains(DataType.Ticks, DataType.CandleSources);
		Contains(DataType.Level1, DataType.CandleSources);
		Contains(DataType.MarketDepth, DataType.CandleSources);
		Contains(DataType.OrderLog, DataType.CandleSources);
	}

	[TestMethod]
	public void SerializableString_NonCandle()
	{
		var dt = DataType.Create<Level1ChangeMessage>();
		var s = dt.ToSerializableString();
		var back = DataType.FromSerializableString(s);

		back.AreEqual(dt);
		back.AreEqual(DataType.Level1);
	}

	[TestMethod]
	public void SerializableString_Candle()
	{
		var dt = DataType.Create<TimeFrameCandleMessage>(TimeSpan.FromMinutes(5));
		var s = dt.ToSerializableString();
		var back = DataType.FromSerializableString(s);

		back.AreEqual(dt);
	}

	[TestMethod]
	public void SerializableString_AllKnownTypes()
	{
		static void Roundtrip(DataType dt)
		{
			var s = dt.ToSerializableString();
			var back = DataType.FromSerializableString(s);
			back.AreEqual(dt, $"Roundtrip failed for {dt} -> '{s}'");
		}

		var types = new[]
		{
			// Static well-known types
			DataType.Level1,
			DataType.MarketDepth,
			DataType.FilteredMarketDepth,
			DataType.PositionChanges,
			DataType.News,
			DataType.Securities,
			DataType.Ticks,
			DataType.OrderLog,
			DataType.Transactions,
			DataType.Board,
			DataType.BoardState,
			DataType.Users,
			DataType.DataTypeInfo,
			DataType.CandleTimeFrame,
			DataType.CandleVolume,
			DataType.CandleTick,
			DataType.CandleRange,
			DataType.CandleRenko,
			DataType.CandlePnF,
			DataType.SecurityLegs,
			DataType.Command,
			DataType.RemoteFile,
			DataType.SecurityMapping,

			// Dynamic instances to cover args path
			DataType.Create<Level1ChangeMessage>(),
			DataType.Create<QuoteChangeMessage>(),
			DataType.Create<PositionChangeMessage>(),
			DataType.Create<TimeFrameCandleMessage>(TimeSpan.FromMinutes(1)),
			DataType.Create<TickCandleMessage>(100),
			DataType.Create<VolumeCandleMessage>(123.45m),
			DataType.Create<RangeCandleMessage>(new Unit(1)),
			DataType.Create<RenkoCandleMessage>(new Unit(2)),
			DataType.Create<PnFCandleMessage>(new PnFArg { BoxSize = new Unit(1), ReversalAmount = 3 }),
			DataType.Create<HeikinAshiCandleMessage>(TimeSpan.FromMinutes(2)),
		};

		foreach (var dt in types)
			Roundtrip(dt);
	}

	[TestMethod]
	public void Save_Load_NonCandle()
	{
		var dt = DataType.Create<Level1ChangeMessage>();
		var storage = new SettingsStorage();
		dt.Save(storage);

		var loaded = new DataType();
		loaded.Load(storage);

		loaded.AreEqual(dt);
		loaded.AreEqual(DataType.Level1);
	}

	[TestMethod]
	public void Save_Load()
	{
		var dt = DataType.Create<TimeFrameCandleMessage>(TimeSpan.FromMinutes(1));
		var storage = new SettingsStorage();
		dt.Save(storage);

		var loaded = new DataType();
		loaded.Load(storage);

		loaded.AreEqual(dt);
	}

	[TestMethod]
	public void Name_Does_Not_Affect_Equality_Or_Hash()
	{
		var dt1 = DataType.Create<Level1ChangeMessage>().SetName("A");
		var dt2 = DataType.Create<Level1ChangeMessage>().SetName("B");

		dt2.AreEqual(dt1);
		dt2.GetHashCode().AreEqual(dt1.GetHashCode());
	}

	[TestMethod]
	public void SerializableString_Aliases()
	{
		static void TestAlias(DataType dt, string expected)
		{
			var s = dt.ToSerializableString();
			s.AssertEqual(expected);
			var back = DataType.FromSerializableString(s);
			back.AreEqual(dt);

			// also check case-insensitive
			var upper = expected.ToUpperInvariant();
			var back2 = DataType.FromSerializableString(upper);
			back2.AreEqual(dt);
		}

		TestAlias(DataType.Level1, "level1");
		TestAlias(DataType.Ticks, "ticks");
		TestAlias(DataType.MarketDepth, "marketdepth");
		TestAlias(DataType.OrderLog, "orderlog");
	}

	[TestMethod]
	public void CustomAliases()
	{
		var custom = DataType.Create<Level1ChangeMessage>("custom");

		// register new alias for custom instance (same MessageType/Arg as Level1)
		DataType.RegisterAlias("l1_custom", custom);
		DataType.FromSerializableString("l1_custom").AreEqual(custom);
		custom.ToSerializableString().AssertEqual("l1_custom");

		// replacing alias for existing built-in should be denied
		ThrowsExactly<ArgumentException>(() => DataType.RegisterAlias("ticks_custom", DataType.Ticks));
		ThrowsExactly<ArgumentException>(() => DataType.RegisterAlias("l1_custom", custom));

		// clean up
		DataType.UnRegisterAlias("l1_custom").AssertTrue();
		DataType.UnRegisterAlias(custom).AssertFalse();

		DataType.RegisterAlias("l1_custom", custom);
		DataType.UnRegisterAlias(custom).AssertTrue();
		DataType.UnRegisterAlias("l1_custom").AssertFalse();
	}

	[TestMethod]
	public void CandleTypeAliases()
	{
		var dt = TimeSpan.FromMinutes(1).TimeFrame();

		// custom candle data type cannot be unregistered
		DataType.UnRegisterAlias(dt).AssertFalse();

		// ensure serialization uses short token
		var s = dt.ToSerializableString();
		s.AssertEqual("tf:00-01-00");

		// roundtrip using short token
		var back = DataType.FromSerializableString("TF:00-01-00");
		back.AreEqual(dt);

		back = DataType.FromSerializableString("tf:00-01-00");
		back.AreEqual(dt);
	}

	[TestMethod]
	public void CandleType_TimeFrame_Large()
	{
		var tfs = new[]
		{
			TimeSpan.FromDays(1),
			TimeSpan.FromDays(7),
			TimeSpan.FromDays(30),
			TimeSpan.FromDays(365),
			new TimeSpan(days: 10, hours: 12, minutes: 0, seconds: 0)
		};

		foreach (var tf in tfs)
		{
			var dt = tf.TimeFrame();

			// roundtrip via single-string form
			var s = dt.ToSerializableString();
			var back = DataType.FromSerializableString(s);
			back.AreEqual(dt, $"To/FromSerializableString failed for TF={tf} -> '{s}'");

			// roundtrip via (type,arg) form
			var (type, arg) = dt.FormatToString();
			var s2 = $"{type}:{arg}";
			var back2 = DataType.FromSerializableString(s2);
			back2.AreEqual(dt, $"FormatToString/FromSerializableString failed for TF={tf} -> '{s2}'");

			// also through Extensions.ToDataType(type,arg)
			var back3 = type.ToDataType(arg);
			back3.AreEqual(dt, $"ToDataType(type,arg) failed for TF={tf}");
		}
	}

	[TestMethod]
	public void CandleType_HeikinAshi_Large()
	{
		var tfs = new[]
		{
			TimeSpan.FromHours(12),
			TimeSpan.FromDays(1),
			TimeSpan.FromDays(5)
		};

		foreach (var tf in tfs)
		{
			var dt = DataType.Create<HeikinAshiCandleMessage>(tf);

			var s = dt.ToSerializableString();
			var back = DataType.FromSerializableString(s);
			back.AreEqual(dt);

			var (type, arg) = dt.FormatToString();
			var back2 = type.ToDataType(arg);
			back2.AreEqual(dt);
		}
	}

	[TestMethod]
	public void CandleType_LargeArgs()
	{
		var cases = new List<DataType>
		{
			DataType.Create<TickCandleMessage>(1_000_000),
			DataType.Create<VolumeCandleMessage>(1_000_000.123m),
			DataType.Create<RangeCandleMessage>(new Unit(1_000)),
			DataType.Create<RenkoCandleMessage>(new Unit(50)),
			DataType.Create<PnFCandleMessage>(new PnFArg { BoxSize = new Unit(100), ReversalAmount = 10 }),
		};

		foreach (var dt in cases)
		{
			var s = dt.ToSerializableString();
			var back = DataType.FromSerializableString(s);
			back.AreEqual(dt, $"Single-string roundtrip failed for {dt} -> '{s}'");

			var (type, arg) = dt.FormatToString();
			var s2 = $"{type}:{arg}";
			var back2 = DataType.FromSerializableString(s2);
			back2.AreEqual(dt, $"(type:arg) roundtrip failed for {dt} -> '{s2}'");

			var back3 = type.ToDataType(arg);
			back3.AreEqual(dt, $"ToDataType(type,arg) failed for {dt}");
		}
	}

	[TestMethod]
	public void CandleType_ShortAliases_Check()
	{
		// mapping message types to expected short aliases
		var aliasMap = new Dictionary<Type, string>
		{
			{ typeof(TimeFrameCandleMessage), "tf" },
			{ typeof(VolumeCandleMessage), "volume" },
			{ typeof(TickCandleMessage), "tick_candle" },
			{ typeof(RangeCandleMessage), "range" },
			{ typeof(RenkoCandleMessage), "renko" },
			{ typeof(PnFCandleMessage), "pnf" },
		};

		var cases = new List<DataType>
		{
			TimeSpan.FromDays(7).TimeFrame(),
			TimeSpan.FromDays(30).TimeFrame(),
			TimeSpan.FromDays(365).TimeFrame(),
			new TimeSpan(10, 12, 0, 0).TimeFrame(),
			DataType.Create<VolumeCandleMessage>(10_000m),
			DataType.Create<TickCandleMessage>(50_000),
			DataType.Create<RangeCandleMessage>(new Unit(250)),
			DataType.Create<RenkoCandleMessage>(new Unit(25)),
			DataType.Create<PnFCandleMessage>(new PnFArg { BoxSize = new Unit(5), ReversalAmount = 3 }),
		};

		foreach (var dt in cases)
		{
			var s = dt.ToSerializableString();

			var expectedPrefix = aliasMap[dt.MessageType];
			(s.StartsWith(expectedPrefix + ":", StringComparison.InvariantCultureIgnoreCase)).AssertTrue($"Expected '{expectedPrefix}:' prefix for {dt}, got '{s}'");

			var expected = expectedPrefix + ":" + dt.DataTypeArgToString();
			s.AssertEqual(expected);

			// ensure long name not used
			(s.Contains("StockSharp.Messages.") == false).AssertTrue($"Long type name leakage detected in '{s}'");

			// roundtrip as well
			var back = DataType.FromSerializableString(s);
			back.AreEqual(dt);
		}
	}
}

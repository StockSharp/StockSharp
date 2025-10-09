namespace StockSharp.Tests;

[TestClass]
public class DataTypeTests
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
		Assert.Contains(DataType.Ticks, DataType.CandleSources);
		Assert.Contains(DataType.Level1, DataType.CandleSources);
		Assert.Contains(DataType.MarketDepth, DataType.CandleSources);
		Assert.Contains(DataType.OrderLog, DataType.CandleSources);
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
		Assert.ThrowsExactly<ArgumentException>(() => DataType.RegisterAlias("ticks_custom", DataType.Ticks));
		Assert.ThrowsExactly<ArgumentException>(() => DataType.RegisterAlias("l1_custom", custom));

		// replace for ticks
		DataType.UnRegisterAlias("ticks").AssertTrue();
		DataType.UnRegisterAlias(DataType.Ticks).AssertFalse();
		DataType.RegisterAlias("ticks", DataType.Ticks);
		DataType.UnRegisterAlias(DataType.Ticks).AssertTrue();
	}
}

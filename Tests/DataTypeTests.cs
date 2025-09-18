namespace StockSharp.Tests;

[TestClass]
public class DataTypeTests
{
	[TestMethod]
	public void Equality_And_HashCode()
	{
		var dt1 = DataType.Create<ExecutionMessage>(ExecutionTypes.Tick);
		var dt2 = DataType.Ticks.Clone();

		Assert.IsTrue(dt1.Equals(dt2));
		Assert.AreEqual(dt1.GetHashCode(), dt2.GetHashCode());

		var dt3 = DataType.Create<ExecutionMessage>(ExecutionTypes.OrderLog);
		Assert.IsFalse(dt1.Equals(dt3));
	}

	[TestMethod]
	public void IsSecurityRequired_IsNonSecurity()
	{
		// Candles
		var tf = DataType.Create<TimeFrameCandleMessage>(TimeSpan.FromMinutes(1));
		Assert.IsTrue(tf.IsCandles);
		Assert.IsTrue(tf.IsMarketData);
		Assert.IsTrue(tf.IsSecurityRequired);
		Assert.IsFalse(tf.IsNonSecurity);

		// Specific TF candles shortcut
		Assert.IsTrue(DataType.CandleTimeFrame.IsCandles);
		Assert.IsTrue(DataType.CandleTimeFrame.IsTFCandles);

		// Level1 and MarketDepth
		Assert.IsTrue(DataType.Level1.IsMarketData);
		Assert.IsTrue(DataType.Level1.IsSecurityRequired);
		Assert.IsTrue(DataType.MarketDepth.IsMarketData);
		Assert.IsTrue(DataType.MarketDepth.IsSecurityRequired);

		// Executions: Tick is market-data, Transaction is not
		Assert.IsTrue(DataType.Ticks.IsMarketData);
		Assert.IsTrue(DataType.Ticks.IsSecurityRequired);
		Assert.IsFalse(DataType.Transactions.IsMarketData);
		Assert.IsFalse(DataType.Transactions.IsSecurityRequired);

		// Non-security types
		Assert.IsTrue(DataType.Securities.IsNonSecurity);
		Assert.IsTrue(DataType.News.IsNonSecurity);
		Assert.IsTrue(DataType.Board.IsNonSecurity);
		Assert.IsTrue(DataType.BoardState.IsNonSecurity);
		Assert.IsTrue(DataType.DataTypeInfo.IsNonSecurity);
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
		var dt = DataType.Create<ExecutionMessage>(ExecutionTypes.Tick);
		var s = dt.ToSerializableString();
		var back = DataType.FromSerializableString(s);

		Assert.AreEqual(dt, back);
	}

	[TestMethod]
	public void SerializableString_Candle()
	{
		var dt = DataType.Create<TimeFrameCandleMessage>(TimeSpan.FromMinutes(5));
		var s = dt.ToSerializableString();
		var back = DataType.FromSerializableString(s);

		Assert.AreEqual(dt, back);
	}

	[TestMethod]
	public void SerializableString_AllKnownTypes()
	{
		static void Roundtrip(DataType dt)
		{
			var s = dt.ToSerializableString();
			var back = DataType.FromSerializableString(s);
			Assert.AreEqual(dt, back, $"Roundtrip failed for {dt} -> '{s}'");
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
			DataType.Create<ExecutionMessage>(ExecutionTypes.Tick),
			DataType.Create<ExecutionMessage>(ExecutionTypes.OrderLog),
			DataType.Create<ExecutionMessage>(ExecutionTypes.Transaction),
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
		var dt = DataType.Create<ExecutionMessage>(ExecutionTypes.OrderLog);
		var storage = new SettingsStorage();
		dt.Save(storage);

		var loaded = new DataType();
		loaded.Load(storage);

		Assert.AreEqual(dt, loaded);
	}

	[TestMethod]
	public void Save_Load()
	{
		var dt = DataType.Create<TimeFrameCandleMessage>(TimeSpan.FromMinutes(1));
		var storage = new SettingsStorage();
		dt.Save(storage);

		var loaded = new DataType();
		loaded.Load(storage);

		Assert.AreEqual(dt, loaded);
	}

	[TestMethod]
	public void Name_Does_Not_Affect_Equality_Or_Hash()
	{
		var dt1 = DataType.Create<ExecutionMessage>(ExecutionTypes.Tick).SetName("A");
		var dt2 = DataType.Create<ExecutionMessage>(ExecutionTypes.Tick).SetName("B");

		Assert.AreEqual(dt1, dt2);
		Assert.AreEqual(dt1.GetHashCode(), dt2.GetHashCode());
	}
}

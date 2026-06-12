namespace StockSharp.Tests;

using Ecng.Reflection;

using StockSharp.Algo.Storages.Binary.Snapshot;

[TestClass]
public class SerializationTests
{
	[TestMethod]
	public void SerializeBoard()
	{
		var boards = typeof(ExchangeBoard)
			.GetProperties(BindingFlags.Static | BindingFlags.Public)
			.Where(p => p.PropertyType == typeof(ExchangeBoard))
			.Select(p => (ExchangeBoard)p.GetValue(null))
			.ToArray();

		foreach (var board in boards)
			SerializeEntity(board);
	}

	[TestMethod]
	public void SerializeExchange()
	{
		var exchanges = typeof(Exchange)
			.GetProperties(BindingFlags.Static | BindingFlags.Public)
			.Where(p => p.PropertyType == typeof(Exchange))
			.Select(p => (Exchange)p.GetValue(null))
			.ToArray();

		foreach (var exchange in exchanges)
		{
			SerializeEntity(exchange);
		}
	}

	[TestMethod]
	public void SerializePersistables()
	{
		var assemblies = new[]
		{
			typeof(Message).Assembly,
			typeof(Order).Assembly,
			typeof(Connector).Assembly
		};

		var objects = assemblies
			.SelectMany(a => a.FindImplementations<IPersistable>(false, false, extraFilter: t => t.GetConstructor(Type.EmptyTypes) != null))
			.Select(t => t.CreateInstance<IPersistable>())
			.ToArray();

		var genMethod = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Static).First(m => m.Name == nameof(SerializeEntity) && m.GetParameters().Length == 1);

		for (var i=0; i<objects.Length; ++i)
		{
			var o = objects[i];

			if(o is IIndicator ind)
				ind.Reset();

			genMethod.Make(o.GetType()).Invoke(null, [o]);
		}
	}

	private static void SerializeEntity<T>(T entity)
		where T : class, IPersistable
	{
		ArgumentNullException.ThrowIfNull(entity);

		var ser = Paths.CreateSerializer<T>();

		Helper.CheckEqual(entity.Save(), ser.Deserialize(ser.Serialize(entity)).Save());
	}

	private static ExecutionMessage CreateTransaction(OrderCondition condition)
	{
		return new ExecutionMessage
		{
			SecurityId = new SecurityId
			{
				SecurityCode = "AAPL",
				BoardCode = BoardCodes.Nasdaq
			},
			DataTypeEx = DataType.Transactions,
			TransactionId = new IncrementalIdGenerator().GetNextId(),
			Condition = condition
		};
	}

	private static ExecutionMessage RoundTrip(ExecutionMessage origin)
	{
		ISnapshotSerializer<string, ExecutionMessage> serializer = new TransactionBinarySnapshotSerializer();

		var bytes = serializer.Serialize(serializer.Version, origin);
		return serializer.Deserialize(serializer.Version, bytes);
	}

	// Verifies the previously completely unguarded condition-parameter serialization
	// path of TransactionBinarySnapshotSerializer for the value types it explicitly
	// supports (decimal and bool). The condition type and every parameter value must
	// survive the binary round-trip. The whole-message Helper.CheckEqual cannot be used
	// for the assertion because OrderCondition has no value equality, so the parameters
	// are compared explicitly.
	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void TransactionsSnapshotConditionSupportedTypes()
	{
		var condition = new StopOrderCondition
		{
			ActivationPrice = 123.45m,
			ClosePositionPrice = 67.89m,
			TrailingOffset = 1.5m,
			IsTrailing = true,
			IsActivationPricePercent = true,
		};

		var expected = condition.Parameters.Where(p => p.Value != null).ToArray();

		var loaded = RoundTrip(CreateTransaction(condition));

		loaded.Condition.AssertNotNull();
		loaded.Condition.GetType().AssertEqual(typeof(StopOrderCondition));

		// every supported parameter value must be preserved
		loaded.Condition.Parameters.Count.AssertEqual(expected.Length);

		foreach (var p in expected)
		{
			loaded.Condition.Parameters.TryGetValue(p.Key, out var actual).AssertTrue($"missing param '{p.Key}'");
			actual.AssertEqual(p.Value, $"param '{p.Key}'");
		}

		// typed accessors must reflect the same values
		var loadedCond = (StopOrderCondition)loaded.Condition;
		loadedCond.ActivationPrice.AssertEqual(123.45m);
		loadedCond.ClosePositionPrice.AssertEqual(67.89m);
		loadedCond.TrailingOffset.AssertEqual(1.5m);
		loadedCond.IsTrailing.AssertEqual(true);
		loadedCond.IsActivationPricePercent.AssertEqual(true);
	}

	// Guards the contract that a condition parameter survives the binary round-trip
	// regardless of its CLR type. A raw string parameter falls into the serializer's
	// "default: // Unknown type - skip" branch, which writes an empty payload and thus
	// silently drops the value on deserialize. The correct behavior is full preservation,
	// so this test asserts the string is restored (expected to fail until the engine no
	// longer discards string-typed condition parameters).
	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void TransactionsSnapshotConditionStringParamRoundTrip()
	{
		const string key = "RawStringParam";
		const string value = "hello-world-42";

		var condition = new StopOrderCondition();
		// store a raw string value directly through the parameter bag
		condition.Parameters[key] = value;

		var loaded = RoundTrip(CreateTransaction(condition));

		loaded.Condition.AssertNotNull();
		loaded.Condition.Parameters.TryGetValue(key, out var actual)
			.AssertTrue($"string param '{key}' was dropped during snapshot serialization");
		actual.AssertEqual(value);
	}

	// Guards round-tripping of an integer-typed condition parameter (the serializer's
	// TypeCode.Int64 branch). Long values are written as Int64 and must be restored
	// with both value and CLR type preserved.
	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void TransactionsSnapshotConditionLongParamRoundTrip()
	{
		const string key = "LongParam";
		const long value = 9_876_543_210L;

		var condition = new StopOrderCondition();
		condition.Parameters[key] = value;

		var loaded = RoundTrip(CreateTransaction(condition));

		loaded.Condition.AssertNotNull();
		loaded.Condition.Parameters.TryGetValue(key, out var actual)
			.AssertTrue($"long param '{key}' was dropped during snapshot serialization");
		actual.AssertEqual(value);
	}

	// A transaction without a condition must round-trip with a null condition (the
	// conditionType string is empty, so no condition is reconstructed).
	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void TransactionsSnapshotNoCondition()
	{
		var loaded = RoundTrip(CreateTransaction(null));

		loaded.Condition.AssertNull();
	}
}
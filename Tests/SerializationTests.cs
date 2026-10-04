namespace StockSharp.Tests;

using System.Collections;

using Ecng.Reflection;

using StockSharp.Algo.Storages.Binary.Snapshot;

[TestClass]
public class SerializationTests : BaseTestClass
{
	[TestMethod]
	public async Task SerializeBoard()
	{
		var boards = typeof(ExchangeBoard)
			.GetProperties(BindingFlags.Static | BindingFlags.Public)
			.Where(p => p.PropertyType == typeof(ExchangeBoard))
			.Select(p => (ExchangeBoard)p.GetValue(null))
			.ToArray();

		foreach (var board in boards)
			await SerializeEntityAsync(board, CancellationToken);
	}

	[TestMethod]
	public async Task SerializeExchange()
	{
		var exchanges = typeof(Exchange)
			.GetProperties(BindingFlags.Static | BindingFlags.Public)
			.Where(p => p.PropertyType == typeof(Exchange))
			.Select(p => (Exchange)p.GetValue(null))
			.ToArray();

		foreach (var exchange in exchanges)
		{
			await SerializeEntityAsync(exchange, CancellationToken);
		}
	}

	// Comparing two entities left at their defaults proves nothing: a property that is never saved
	// is absent from both sides and passes. Every entity is therefore given values distinct from
	// its defaults first, and the restored object is compared property by property.
	[TestMethod]
	public async Task SerializePersistables()
	{
		var assemblies = new[]
		{
			typeof(Message).Assembly,
			typeof(Order).Assembly,
			typeof(Connector).Assembly
		};

		var objects = assemblies
			.SelectMany(a => a.FindImplementations<IAsyncPersistable>(false, false, extraFilter: t => t.GetConstructor(Type.EmptyTypes) != null))
			.Select(t => t.CreateInstance<IAsyncPersistable>())
			.ToArray();

		var genMethod = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Static).First(m => m.Name == nameof(FillAndRoundTripAsync));

		var failures = new List<string>();
		var skipped = new List<string>();
		var compared = 0;
		var covered = 0;

		for (var i = 0; i < objects.Length; ++i)
		{
			var o = objects[i];

			if (o is IIndicator ind)
				ind.Reset();

			var result = await (Task<PersistenceSweepResult>)genMethod.Make(o.GetType()).Invoke(null, [o, CancellationToken]);

			compared += result.Filled.Count;

			if (result.Filled.Count > 0)
				++covered;

			failures.AddRange(result.Failures);
			skipped.AddRange(result.Skipped);
		}

		Console.WriteLine($"types={objects.Length}, types with compared properties={covered}, compared properties={compared}, skipped properties={skipped.Count}");

		foreach (var s in skipped)
			Console.WriteLine($"skipped: {s}");

		failures.IsEmpty().AssertTrue($"{failures.Count} property value(s) did not survive the round-trip:{Environment.NewLine}{failures.JoinN()}");
	}

	// Pins that the filler really produces values: without it the sweep above could quietly
	// degrade back into comparing two blank objects and still report success.
	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void PersistableSweepFillsEveryScalarProperty()
	{
		var entity = new RepoOrderInfo();
		var fresh = new RepoOrderInfo();

		var result = new PersistenceSweepResult();

		FillWithNonDefaults(entity, result);

		result.Skipped.IsEmpty().AssertTrue(result.Skipped.JoinN());

		var expected = typeof(RepoOrderInfo)
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.GetSetMethod(false) != null)
			.Select(p => p.Name)
			.OrderBy(n => n)
			.ToArray();

		result.Filled.Select(f => f.prop.Name).OrderBy(n => n).ToArray().AssertEqual(expected);

		// every assigned value must differ from what a freshly built entity holds
		foreach (var (prop, value) in result.Filled)
			ValuesEqual(value, prop.GetValue(fresh)).AssertFalse(prop.Name);
	}

	// The positive control for the sweep: an object that saves one property and forgets the other
	// must be reported for exactly the forgotten one.
	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task PersistableSweepReportsUnsavedProperty()
	{
		var result = await FillAndRoundTripAsync(new ForgetfulSettings(), CancellationToken);

		result.Filled.Count.AssertEqual(2);
		result.Failures.Count.AssertEqual(1, result.Failures.JoinN());
		result.Failures[0].AssertContains(nameof(ForgetfulSettings.Dropped));
	}

	// A settings object that forgets one of its properties on save, used to pin the sweep's own
	// detection instead of trusting it.
	private class ForgetfulSettings : IAsyncPersistable
	{
		public int Kept { get; set; }
		public int Dropped { get; set; }

		Task IAsyncPersistable.LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
		{
			Kept = storage.GetValue<int>(nameof(Kept));

			return Task.CompletedTask;
		}

		Task IAsyncPersistable.SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
		{
			storage.SetValue(nameof(Kept), Kept);

			return Task.CompletedTask;
		}
	}

	// What one entity's round-trip produced: the properties that carried a value into it, the ones
	// that could not be given a value, and the values that did not come back.
	private sealed class PersistenceSweepResult
	{
		public List<(PropertyInfo prop, object expected)> Filled { get; } = [];
		public List<string> Skipped { get; } = [];
		public List<string> Failures { get; } = [];
	}

	private static async Task SerializeEntityAsync<T>(T entity, CancellationToken cancellationToken)
		where T : class, IAsyncPersistable
	{
		ArgumentNullException.ThrowIfNull(entity);

		var ser = Paths.CreateSerializer<T>();

		Helper.CheckEqual(await entity.SaveAsync(cancellationToken), await (await ser.DeserializeAsync(await entity.SerializeAsync(true, cancellationToken), cancellationToken)).SaveAsync(cancellationToken));
	}

	private static async Task<PersistenceSweepResult> FillAndRoundTripAsync<T>(T entity, CancellationToken cancellationToken)
		where T : class, IAsyncPersistable
	{
		ArgumentNullException.ThrowIfNull(entity);

		var result = new PersistenceSweepResult();
		var name = entity.GetType().Name;

		FillWithNonDefaults(entity, result);

		var ser = Paths.CreateSerializer<T>();

		T restored;

		try
		{
			restored = await ser.DeserializeAsync(await entity.SerializeAsync(true, cancellationToken), cancellationToken);
		}
		catch (Exception ex)
		{
			// reported rather than thrown, so one broken type does not hide the rest of the sweep
			result.Failures.Add($"{name}: round-trip threw {ex.GetBaseException().GetType().Name} - {ex.GetBaseException().Message}");
			return result;
		}

		foreach (var (prop, expected) in result.Filled)
		{
			object actual;

			try
			{
				actual = prop.GetValue(restored);
			}
			catch (Exception ex)
			{
				result.Failures.Add($"{name}.{prop.Name}: getter threw {ex.GetBaseException().GetType().Name} after restore");
				continue;
			}

			if (!ValuesEqual(actual, expected))
				result.Failures.Add($"{name}.{prop.Name}: expected '{Format(expected)}', restored '{Format(actual)}'");
		}

		// the storages must still agree as well, so a value dropped by both sides stays visible
		try
		{
			Helper.CheckEqual(await entity.SaveAsync(cancellationToken), await restored.SaveAsync(cancellationToken));
		}
		catch (Exception ex)
		{
			result.Failures.Add($"{name}: Save() differs after restore - {ex.Message}");
		}

		return result;
	}

	// Assigns every public settable property a value that differs from the one a freshly built
	// entity holds. A property that cannot take an arbitrary value is recorded, not asserted on.
	private static void FillWithNonDefaults(object entity, PersistenceSweepResult result)
	{
		var type = entity.GetType();
		var name = type.Name;

		var assigned = new List<(PropertyInfo prop, object before)>();

		var props = type
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.GetIndexParameters().Length == 0 && p.GetGetMethod(false) != null && p.GetSetMethod(false) != null)
			.OrderBy(p => p.Name);

		foreach (var prop in props)
		{
			if (prop.IsObsolete())
			{
				result.Skipped.Add($"{name}.{prop.Name}: obsolete");
				continue;
			}

			// Identity and logging plumbing is not a setting: a restored object is entitled to its own
			// identifier and its own place in the log tree.
			if (typeof(ILogSource).IsAssignableFrom(prop.DeclaringType) && prop.Name is nameof(ILogSource.Id) or nameof(ILogSource.IsRoot) or nameof(ILogSource.Parent) or nameof(ILogSource.LogLevel))
			{
				result.Skipped.Add($"{name}.{prop.Name}: log source identity");
				continue;
			}

			object before;

			try
			{
				before = prop.GetValue(entity);
			}
			catch (Exception ex)
			{
				result.Skipped.Add($"{name}.{prop.Name}: getter threw {ex.GetBaseException().GetType().Name}");
				continue;
			}

			if (!TryCreateValue(prop.PropertyType, before, out var value))
			{
				result.Skipped.Add($"{name}.{prop.Name}: no value for {prop.PropertyType.Name}");
				continue;
			}

			try
			{
				prop.SetValue(entity, value);
			}
			catch (Exception ex)
			{
				result.Skipped.Add($"{name}.{prop.Name}: setter threw {ex.GetBaseException().GetType().Name}");
				continue;
			}

			assigned.Add((prop, before));
		}

		// Read back only after every assignment: a setter can normalize its input and another
		// property can reset it, and only a value that actually stuck can be asserted on.
		foreach (var (prop, before) in assigned)
		{
			object final;

			try
			{
				final = prop.GetValue(entity);
			}
			catch (Exception ex)
			{
				result.Skipped.Add($"{name}.{prop.Name}: getter threw {ex.GetBaseException().GetType().Name}");
				continue;
			}

			if (ValuesEqual(final, before))
			{
				result.Skipped.Add($"{name}.{prop.Name}: value did not stick");
				continue;
			}

			result.Filled.Add((prop, final));
		}
	}

	// Values are fixed rather than random so a failure reproduces, and each candidate is checked
	// against the current value so "distinct from the default" holds whatever the default is.
	private static bool TryCreateValue(Type type, object current, out object value)
	{
		value = null;

		if (type.IsArray)
		{
			var elemType = type.GetElementType();

			if (!TryCreateValue(elemType, null, out var elem))
				return false;

			var array = Array.CreateInstance(elemType, 1);
			array.SetValue(elem, 0);

			if (ValuesEqual(array, current))
				return false;

			value = array;
			return true;
		}

		var underlying = Nullable.GetUnderlyingType(type) ?? type;

		IEnumerable<object> candidates;

		if (underlying.IsEnum)
			candidates = Enum.GetValues(underlying).Cast<object>();
		else if (underlying.IsNumeric())
			candidates = [7m.To(underlying), 11m.To(underlying)];
		else
			candidates = GetCandidates(underlying);

		if (candidates is null)
			return false;

		foreach (var candidate in candidates)
		{
			if (ValuesEqual(candidate, current))
				continue;

			value = candidate;
			return true;
		}

		return false;
	}

	// Two candidates per type, so a property already holding the first one still gets a new value.
	private static IEnumerable<object> GetCandidates(Type type)
	{
		if (type == typeof(string))
			return ["sweep-value", "sweep-other"];

		if (type == typeof(bool))
			return [true, false];

		if (type == typeof(char))
			return ['Z', 'Y'];

		if (type == typeof(DateTime))
			return [new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc), new DateTime(2023, 4, 5, 6, 7, 8, DateTimeKind.Utc)];

		if (type == typeof(TimeSpan))
			return [TimeSpan.FromSeconds(37), TimeSpan.FromSeconds(53)];

		if (type == typeof(Guid))
			return [new Guid("2f1b6a3c-1d4e-4a7b-9c5d-8e0f1a2b3c4d"), new Guid("7c9e5b1a-3f2d-4e6b-8a0c-1d2e3f4a5b6c")];

		if (type == typeof(Type))
			return [typeof(Unit), typeof(SecurityIdMapping)];

		if (type == typeof(TimeZoneInfo))
			return [TimeHelper.Moscow, TimeZoneInfo.Utc];

		if (type == typeof(SecurityId))
			return [new SecurityId { SecurityCode = "AAPL", BoardCode = BoardCodes.Nasdaq }, new SecurityId { SecurityCode = "MSFT", BoardCode = BoardCodes.Nasdaq }];

		if (type == typeof(Unit))
			return [new Unit(3m), new Unit(5m, UnitTypes.Percent)];

		if (type == typeof(DataType))
			return [DataType.Ticks, DataType.Level1];

		return null;
	}

	private static bool ValuesEqual(object first, object second)
	{
		if (first is null || second is null)
			return first is null && second is null;

		if (first is string || second is string)
			return first.Equals(second);

		if (first is IEnumerable firstSeq && second is IEnumerable secondSeq)
			return firstSeq.Cast<object>().SequenceEqual(secondSeq.Cast<object>());

		return first.Equals(second);
	}

	private static string Format(object value)
		=> value switch
		{
			null => "null",
			string s => s,
			IEnumerable e => e.Cast<object>().Select(Format).JoinComma(),
			_ => value.ToString(),
		};

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

	private async Task<ExecutionMessage> RoundTripAsync(ExecutionMessage origin)
	{
		ISnapshotSerializer<string, ExecutionMessage> serializer = new TransactionBinarySnapshotSerializer();

		var bytes = await serializer.SerializeAsync(serializer.Version, origin, CancellationToken);
		return await serializer.DeserializeAsync(serializer.Version, bytes, CancellationToken);
	}

	// Verifies the previously completely unguarded condition-parameter serialization
	// path of TransactionBinarySnapshotSerializer for the value types it explicitly
	// supports (decimal and bool). The condition type and every parameter value must
	// survive the binary round-trip. The whole-message Helper.CheckEqual cannot be used
	// for the assertion because OrderCondition has no value equality, so the parameters
	// are compared explicitly.
	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task TransactionsSnapshotConditionSupportedTypes()
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

		var loaded = await RoundTripAsync(CreateTransaction(condition));

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
	public async Task TransactionsSnapshotConditionStringParamRoundTrip()
	{
		const string key = "RawStringParam";
		const string value = "hello-world-42";

		var condition = new StopOrderCondition();
		// store a raw string value directly through the parameter bag
		condition.Parameters[key] = value;

		var loaded = await RoundTripAsync(CreateTransaction(condition));

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
	public async Task TransactionsSnapshotConditionLongParamRoundTrip()
	{
		const string key = "LongParam";
		const long value = 9_876_543_210L;

		var condition = new StopOrderCondition();
		condition.Parameters[key] = value;

		var loaded = await RoundTripAsync(CreateTransaction(condition));

		loaded.Condition.AssertNotNull();
		loaded.Condition.Parameters.TryGetValue(key, out var actual)
			.AssertTrue($"long param '{key}' was dropped during snapshot serialization");
		actual.AssertEqual(value);
	}

	// The sweep above cannot reach these three: it has no value to put in a BankDetails property, so it
	// records them as skipped. A withdraw carries three separate sets of details, each has to come back
	// in its own slot with its own values, and a name read from the wrong key would swap them silently.
	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task WithdrawInfoKeepsItsThreeSetsOfBankDetailsApart()
	{
		var info = new WithdrawInfo
		{
			Type = WithdrawTypes.BankWire,
			Express = true,
			ChargeFee = 12.5m,
			PaymentId = "pay-991",
			CryptoAddress = "bc1qexampleaddress",
			CardNumber = "4111111111111111",
			Comment = "quarterly payout",
			BankDetails = CreateBankDetails("beneficiary", CurrencyTypes.EUR),
			IntermediaryBankDetails = CreateBankDetails("intermediary", CurrencyTypes.USD),
			CompanyDetails = CreateBankDetails("company", CurrencyTypes.GBP),
		};

		var restored = await (await info.SaveAsync(CancellationToken)).LoadAsync<WithdrawInfo>(CancellationToken);

		restored.Type.AssertEqual(WithdrawTypes.BankWire);
		restored.Express.AssertEqual(true);
		restored.ChargeFee.AssertEqual(12.5m);
		restored.PaymentId.AssertEqual("pay-991");
		restored.CryptoAddress.AssertEqual("bc1qexampleaddress");
		restored.CardNumber.AssertEqual("4111111111111111");
		restored.Comment.AssertEqual("quarterly payout");

		AssertBankDetails(restored.BankDetails, "beneficiary", CurrencyTypes.EUR);
		AssertBankDetails(restored.IntermediaryBankDetails, "intermediary", CurrencyTypes.USD);
		AssertBankDetails(restored.CompanyDetails, "company", CurrencyTypes.GBP);
	}

	// Clone on these two is a hand-written property list, so a property added later is dropped without
	// a compiler error. Every public settable property is given a value that differs from a fresh
	// object's, and the copy must carry all of them.
	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void RepoAndNtmOrderInfoCloneEveryProperty()
	{
		AssertCloneCarriesEveryProperty(new RepoOrderInfo(), i => i.Clone());
		AssertCloneCarriesEveryProperty(new NtmOrderInfo(), i => i.Clone());
	}

	private static BankDetails CreateBankDetails(string tag, CurrencyTypes currency) => new()
	{
		Account = $"{tag}-account",
		AccountName = $"{tag}-account-name",
		Name = $"{tag}-name",
		Address = $"{tag}-address",
		Country = $"{tag}-country",
		City = $"{tag}-city",
		Swift = $"{tag}-swift",
		Bic = $"{tag}-bic",
		Iban = $"{tag}-iban",
		PostalCode = $"{tag}-postal",
		Currency = currency,
	};

	private static void AssertBankDetails(BankDetails actual, string tag, CurrencyTypes currency)
	{
		actual.AssertNotNull(tag);
		actual.Account.AssertEqual($"{tag}-account");
		actual.AccountName.AssertEqual($"{tag}-account-name");
		actual.Name.AssertEqual($"{tag}-name");
		actual.Address.AssertEqual($"{tag}-address");
		actual.Country.AssertEqual($"{tag}-country");
		actual.City.AssertEqual($"{tag}-city");
		actual.Swift.AssertEqual($"{tag}-swift");
		actual.Bic.AssertEqual($"{tag}-bic");
		actual.Iban.AssertEqual($"{tag}-iban");
		actual.PostalCode.AssertEqual($"{tag}-postal");
		actual.Currency.AssertEqual(currency);
	}

	private static void AssertCloneCarriesEveryProperty<T>(T entity, Func<T, T> clone)
		where T : class
	{
		var name = typeof(T).Name;
		var result = new PersistenceSweepResult();

		FillWithNonDefaults(entity, result);

		result.Skipped.IsEmpty().AssertTrue($"{name}: {result.Skipped.JoinN()}");
		result.Filled.IsEmpty().AssertFalse(name);

		var copy = clone(entity);

		copy.AssertNotNull(name);
		copy.AssertNotSame(entity, name);

		foreach (var (prop, expected) in result.Filled)
		{
			var actual = prop.GetValue(copy);

			ValuesEqual(actual, expected).AssertTrue($"{name}.{prop.Name}: expected '{Format(expected)}', clone has '{Format(actual)}'");
		}
	}

	// A transaction without a condition must round-trip with a null condition (the
	// conditionType string is empty, so no condition is reconstructed).
	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task TransactionsSnapshotNoCondition()
	{
		var loaded = await RoundTripAsync(CreateTransaction(null));

		loaded.Condition.AssertNull();
	}
}
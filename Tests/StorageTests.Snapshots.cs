namespace StockSharp.Tests;

using StockSharp.Algo.Storages.Binary.Snapshot;

/// <summary>
/// Snapshot serializers and the snapshot storage files they are kept in.
/// </summary>
partial class StorageTests
{
	private sealed class CapturingLogListener : LogListener
	{
		public List<LogMessage> Messages { get; } = [];

		protected override void OnWriteMessage(LogMessage message)
			=> Messages.Add(message);
	}

	[TestMethod]
	public void SnapshotWithoutSide()
	{
		var secId = Helper.CreateStorageSecurity().ToSecurityId();
		var serializer = new TransactionBinarySnapshotSerializer();

		ISnapshotSerializer<string, ExecutionMessage> typed = serializer;

		var withoutSide = Helper.RandomTransaction(secId, 1);
		withoutSide.Side = null;

		var restored = typed.Deserialize(typed.Version, typed.Serialize(typed.Version, withoutSide));

		restored.Side.AssertNull();
		restored.OrderBoardId.AssertEqual(withoutSide.OrderBoardId);
		restored.OrderState.AssertEqual(withoutSide.OrderState);
		restored.Balance.AssertEqual(withoutSide.Balance);

		var withSide = Helper.RandomTransaction(secId, 2);
		withSide.Side = Sides.Sell;

		typed.Deserialize(typed.Version, typed.Serialize(typed.Version, withSide)).Side.AssertEqual(Sides.Sell);
	}


	[TestMethod]
	public void Snapshot()
	{
		static void Check<TKey, TMessage>(ISnapshotSerializer<TKey, TMessage> serializer, TMessage message, bool skipOriginalTransactionId = false)
			where TMessage : Message
		{
			Helper.CheckEqual(message, serializer.Deserialize(serializer.Version, serializer.Serialize(serializer.Version, message)), skipOriginalTransactionId: skipOriginalTransactionId);
		}

		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var books = security.RandomDepths(100);

		foreach (var book in books)
		{
			Check(new QuotesBinarySnapshotSerializer(), book);
		}

		for (var i = 0; i < 100; i++)
		{
			Check(new Level1BinarySnapshotSerializer(), Helper.RandomLevel1(security, secId, DateTime.UtcNow, RandomGen.GetBool(), RandomGen.GetBool(), () => 1m));
		}

		for (var i = 0; i < 100; i++)
		{
			Check(new PositionBinarySnapshotSerializer(), Helper.RandomPositionChange(secId));
		}

		for (var i = 0; i < 100; i++)
		{
			Check(new TransactionBinarySnapshotSerializer(), Helper.RandomTransaction(secId, i), skipOriginalTransactionId: true);
		}
	}

	/// <summary>
	/// A wholly corrupt snapshot file must not remain active and fail on every registry start. Keep
	/// its bytes as a backup for inspection while allowing the registry to create a clean snapshot.
	/// </summary>
	[TestMethod]
	public void CorruptSnapshotFileIsQuarantined()
	{
		var fs = Helper.MemorySystem;
		var path = fs.GetSubTemp();
		var date = new DateTime(2025, 11, 3, 0, 0, 0, DateTimeKind.Utc);

		var dir = Path.Combine(path, LocalMarketDataDrive.GetDirName(date));
		fs.CreateDirectory(dir);

		var fileName = Path.Combine(dir, "level1.bin");

		// Version bytes, then a record that says it is ten bytes long and is not.
		fs.WriteAllBytes(fileName, [1, 0, 10, 0, 0, 0, 1, 2, 3]);

		using var registry = new SnapshotRegistry(fs, path);

		var storage = ((ISnapshotRegistry)registry).GetSnapshotStorage(DataType.Level1);

		// Reading is what trips over the damaged record.
		storage.Get(new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test });

		fs.FileExists(fileName).AssertFalse("a wholly corrupt snapshot must not remain at the active path");
		fs.FileExists(fileName.MakeBackup()).AssertTrue("the corrupt bytes must be retained for inspection");
	}

	/// <summary>
	/// One damaged row must not make a snapshot file containing other readable rows disappear. The
	/// readable state remains available, while the rejected row is visible in the application log.
	/// </summary>
	[TestMethod]
	[DoNotParallelize]
	public void PartiallyCorruptSnapshotFile_KeepsReadableRowsAndActiveFile()
	{
		var fs = Helper.MemorySystem;
		var path = fs.GetSubTemp();
		var date = new DateTime(2025, 11, 3, 0, 0, 0, DateTimeKind.Utc);
		var secId1 = new SecurityId { SecurityCode = "ONE", BoardCode = BoardCodes.Test };
		var secId2 = new SecurityId { SecurityCode = "TWO", BoardCode = BoardCodes.Test };
		var serializer = (ISnapshotSerializer<SecurityId, Level1ChangeMessage>)new Level1BinarySnapshotSerializer();

		var dir = Path.Combine(path, LocalMarketDataDrive.GetDirName(date));
		fs.CreateDirectory(dir);

		var fileName = Path.Combine(dir, "level1.bin");

		using (var stream = fs.OpenWrite(fileName))
		{
			stream.WriteByte((byte)serializer.Version.Major);
			stream.WriteByte((byte)serializer.Version.Minor);

			foreach (var message in new[]
			{
				new Level1ChangeMessage { SecurityId = secId1, ServerTime = date.AddHours(10), LocalTime = date.AddHours(10) }
					.TryAdd(Level1Fields.LastTradePrice, 101m),
				new Level1ChangeMessage { SecurityId = secId2, ServerTime = date.AddHours(11), LocalTime = date.AddHours(11) }
					.TryAdd(Level1Fields.LastTradePrice, 202m),
			})
				stream.WriteEx(serializer.Serialize(serializer.Version, message));

			stream.WriteEx(new byte[] { 1, 2, 3 });
		}

		var listener = new CapturingLogListener();
		Helper.LogManager.Listeners.Add(listener);

		try
		{
			using var registry = new SnapshotRegistry(fs, path);
			var storage = (ISnapshotStorage<SecurityId, Level1ChangeMessage>)
				((ISnapshotRegistry)registry).GetSnapshotStorage(DataType.Level1);

			var first = storage.Get(secId1);
			var second = storage.Get(secId2);

			first.AssertNotNull("the valid row before the damaged row remains readable");
			second.AssertNotNull("all valid rows remain readable");
			((decimal)first.Changes[Level1Fields.LastTradePrice]).AssertEqual(101m);
			((decimal)second.Changes[Level1Fields.LastTradePrice]).AssertEqual(202m);

			fs.FileExists(fileName).AssertTrue("a partly readable snapshot file must remain at its active path");
			fs.FileExists(fileName.MakeBackup()).AssertFalse("a partly readable file is not wholly quarantined");
			listener.Messages.Any(m => m.Level == LogLevels.Error).AssertTrue("the rejected snapshot row must be logged");
		}
		finally
		{
			Helper.LogManager.Listeners.Remove(listener);
			listener.Dispose();
		}
	}
}

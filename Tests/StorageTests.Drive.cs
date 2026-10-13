namespace StockSharp.Tests;

/// <summary>
/// Local market data drive: folder names, the date index and what the drive reports as available.
/// </summary>
partial class StorageTests
{
	[TestMethod]
	public void FolderNames()
	{
		var secIds = new[]
		{
			"USD/EUR@DUCAS",
			"AAPL@NASDAQ",
			"AAPL@NASDAQ",
			"A:A:PL@NASDAQ",
			":AAPL:@NASDAQ",
			"::AAPL:@NASDAQ",
			"*AAPL*@NASDAQ",
			"*AA*PL*@NASDAQ",
			"AA*PL@NASDAQ",
			"NUL@NASDAQ",
			"NULL@NASDAQ",
			"SCOM5@NASDAQ",
			"COM5S@NASDAQ",
			"LPT9*@NASDAQ",
			":LPT9*@NASDAQ",
			"COM|5S@NASDAQ",
			"LPT9|@NASDAQ",
			"|LPT9*@NASDAQ",
			".LPT9@NASDAQ",
			"..LPT9@NASDAQ",
			".LPT9.@NASDAQ",
			"...LPT9.@NASDAQ",
			".?9.@NASDAQ",
			"?@NASDAQ",
			"?@?",
			"USD\\EUR@DUCAS",
			"USD\\EUR@DUCAS\\GLOBAL",
		};

		const string namesFolder = "FolderNames";
		var fs = Helper.FileSystem;

		// GetSubTemp creates a brand new real directory on every call, so hoisting it out of the
		// loop turns 27 throwaway temp roots into one shared root for all the names.
		var root = fs.GetSubTemp(namesFolder);

		foreach (var secId in secIds)
		{
			var folderName = secId.SecurityIdToFolderName();
			folderName.FolderNameToSecurityId().AssertEqual(secId);

			var di = Directory.CreateDirectory(Path.Combine(root, folderName));
			di.Parent.Name.AssertEqual(namesFolder);
		}
	}


	[TestMethod]
	public void Index_AddAndGetDates()
	{
		var index = new LocalMarketDataDrive.Index();
		var secId = new SecurityId { SecurityCode = "SBER", BoardCode = "MOEX" };
		var date1 = new DateTime(2024, 1, 1);
		var date2 = new DateTime(2024, 1, 2);

		// Add dates
		index.ChangeDate(secId, StorageFormats.Binary, DataType.Ticks, date1, false);
		index.ChangeDate(secId, StorageFormats.Binary, DataType.Ticks, date2, false);

		// Get dates
		var dates = index.GetDates(secId, DataType.Ticks, StorageFormats.Binary).ToArray();
		dates.Length.AssertEqual(2);
		dates[0].AssertEqual(date1);
		dates[1].AssertEqual(date2);
	}

	[TestMethod]
	public void Index_RemoveDate()
	{
		var index = new LocalMarketDataDrive.Index();
		var secId = new SecurityId { SecurityCode = "SBER", BoardCode = "MOEX" };
		var date1 = new DateTime(2024, 1, 1);
		var date2 = new DateTime(2024, 1, 2);

		// Add dates
		index.ChangeDate(secId, StorageFormats.Binary, DataType.Ticks, date1, false);
		index.ChangeDate(secId, StorageFormats.Binary, DataType.Ticks, date2, false);

		// Remove one date
		index.ChangeDate(secId, StorageFormats.Binary, DataType.Ticks, date1, true);

		// Check remaining date
		var dates = index.GetDates(secId, DataType.Ticks, StorageFormats.Binary).ToArray();
		dates.Length.AssertEqual(1);
		dates[0].AssertEqual(date2);
	}

	[TestMethod]
	public void Index_AvailableSecurities()
	{
		var index = new LocalMarketDataDrive.Index();
		var secId1 = new SecurityId { SecurityCode = "SBER", BoardCode = "MOEX" };
		var secId2 = new SecurityId { SecurityCode = "GAZP", BoardCode = "MOEX" };
		var date = new DateTime(2024, 1, 1);

		index.ChangeDate(secId1, StorageFormats.Binary, DataType.Ticks, date, false);
		index.ChangeDate(secId2, StorageFormats.Binary, DataType.Ticks, date, false);

		var securities = index.AvailableSecurities.ToArray();
		securities.Length.AssertEqual(2);
		securities.Count(s => s == secId1).AssertEqual(1);
		securities.Count(s => s == secId2).AssertEqual(1);
	}

	[TestMethod]
	public void Index_GetAvailableDataTypes()
	{
		var index = new LocalMarketDataDrive.Index();
		var secId = new SecurityId { SecurityCode = "SBER", BoardCode = "MOEX" };
		var date = new DateTime(2024, 1, 1);

		index.ChangeDate(secId, StorageFormats.Binary, DataType.Ticks, date, false);
		index.ChangeDate(secId, StorageFormats.Binary, DataType.MarketDepth, date, false);
		index.ChangeDate(secId, StorageFormats.Csv, DataType.Level1, date, false);

		// Get data types for Binary format
		var dataTypes = index.GetAvailableDataTypes(secId, StorageFormats.Binary).ToArray();
		dataTypes.Length.AssertEqual(2);
		dataTypes.Count(d => d == DataType.Ticks).AssertEqual(1);
		dataTypes.Count(d => d == DataType.MarketDepth).AssertEqual(1);

		// Get data types for Csv format
		var dataTypesCsv = index.GetAvailableDataTypes(secId, StorageFormats.Csv).ToArray();
		dataTypesCsv.Length.AssertEqual(1);
		dataTypesCsv[0].AssertEqual(DataType.Level1);
	}

	[TestMethod]
	public void Index_SaveAndLoad()
	{
		var index = new LocalMarketDataDrive.Index();
		var secId = new SecurityId { SecurityCode = "SBER", BoardCode = "MOEX" };
		var date1 = new DateTime(2024, 1, 1);
		var date2 = new DateTime(2024, 1, 2);

		// Add data
		index.ChangeDate(secId, StorageFormats.Binary, DataType.Ticks, date1, false);
		index.ChangeDate(secId, StorageFormats.Binary, DataType.Ticks, date2, false);
		index.ChangeDate(secId, StorageFormats.Binary, DataType.MarketDepth, date1, false);

		// Save to stream
		using var stream = new MemoryStream();
		index.Save(stream);

		// Load from stream
		var loadedIndex = new LocalMarketDataDrive.Index();
		loadedIndex.Load(stream.ToArray());

		// Verify loaded data
		var dates = loadedIndex.GetDates(secId, DataType.Ticks, StorageFormats.Binary).ToArray();
		dates.Length.AssertEqual(2);
		dates[0].AssertEqual(date1);
		dates[1].AssertEqual(date2);

		var dataTypes = loadedIndex.GetAvailableDataTypes(secId, StorageFormats.Binary).ToArray();
		dataTypes.Length.AssertEqual(2);
		dataTypes.Count(d => d == DataType.Ticks).AssertEqual(1);
		dataTypes.Count(d => d == DataType.MarketDepth).AssertEqual(1);
	}

	[TestMethod]
	public void Index_NeedSave()
	{
		var index = new LocalMarketDataDrive.Index();
		var secId = new SecurityId { SecurityCode = "SBER", BoardCode = "MOEX" };
		var date = new DateTime(2024, 1, 1);

		// Initially should not need save
		index.NeedSave(TimeSpan.Zero).AssertFalse();

		// Add data
		index.ChangeDate(secId, StorageFormats.Binary, DataType.Ticks, date, false);

		// Should need save immediately
		index.NeedSave(TimeSpan.Zero).AssertTrue();

		// Should not need save with large delay
		index.NeedSave(TimeSpan.FromDays(1)).AssertFalse();
	}

	[TestMethod]
	public void Index_MultipleFormatsAndDataTypes()
	{
		var index = new LocalMarketDataDrive.Index();
		var secId = new SecurityId { SecurityCode = "SBER", BoardCode = "MOEX" };
		var date = new DateTime(2024, 1, 1);

		// Add same date for different formats and data types
		index.ChangeDate(secId, StorageFormats.Binary, DataType.Ticks, date, false);
		index.ChangeDate(secId, StorageFormats.Csv, DataType.Ticks, date, false);
		index.ChangeDate(secId, StorageFormats.Binary, DataType.MarketDepth, date, false);

		// Verify Binary format has both data types
		var binaryTypes = index.GetAvailableDataTypes(secId, StorageFormats.Binary).ToArray();
		binaryTypes.Length.AssertEqual(2);

		// Verify Csv format has only Ticks
		var csvTypes = index.GetAvailableDataTypes(secId, StorageFormats.Csv).ToArray();
		csvTypes.Length.AssertEqual(1);
		csvTypes[0].AssertEqual(DataType.Ticks);
	}

	[TestMethod]
	public void Index_CandleDataTypes()
	{
		var index = new LocalMarketDataDrive.Index();
		var secId = new SecurityId { SecurityCode = "SBER", BoardCode = "MOEX" };
		var date = new DateTime(2024, 1, 1);

		var tf5min = TimeSpan.FromMinutes(5).TimeFrame();
		var tf1hour = TimeSpan.FromHours(1).TimeFrame();

		index.ChangeDate(secId, StorageFormats.Binary, tf5min, date, false);
		index.ChangeDate(secId, StorageFormats.Binary, tf1hour, date, false);

		var dataTypes = index.GetAvailableDataTypes(secId, StorageFormats.Binary).ToArray();
		dataTypes.Length.AssertEqual(2);
		dataTypes.Count(d => d == tf5min).AssertEqual(1);
		dataTypes.Count(d => d == tf1hour).AssertEqual(1);

		// Save and reload to test candle serialization
		using var stream = new MemoryStream();
		index.Save(stream);

		var loadedIndex = new LocalMarketDataDrive.Index();
		loadedIndex.Load(stream.ToArray());

		var loadedTypes = loadedIndex.GetAvailableDataTypes(secId, StorageFormats.Binary).ToArray();
		loadedTypes.Length.AssertEqual(2);
	}

	private static LocalMarketDataDrive CreateDrive(string path = null)
	{
		var fs = Helper.MemorySystem;
		return new(fs, path ?? fs.GetSubTemp());
	}

	private async Task SetupTestDataAsync(LocalMarketDataDrive drive, SecurityId securityId, DataType dataType, StorageFormats format, DateTime[] dates)
	{
		var token = CancellationToken;
		var storageDrive = drive.GetStorageDrive(securityId, dataType, format);

		foreach (var date in dates)
		{
			using var stream = new MemoryStream();
			using var writer = new BinaryWriter(stream);

			// Write minimal valid data
			stream.Position = 0;
			await storageDrive.SaveStreamAsync(date, stream, token);
		}
	}

	[TestMethod]
	public async Task GetAvailableSecuritiesAsync_EmptyDrive_ReturnsEmpty()
	{
		var drive = CreateDrive();
		var token = CancellationToken;

		var securities = await drive.GetAvailableSecuritiesAsync().ToArrayAsync(token);

		securities.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task GetAvailableSecuritiesAsync_WithSecurities_ReturnsSecurities()
	{
		var drive = CreateDrive();
		var security1 = new SecurityId { SecurityCode = "TEST1", BoardCode = BoardCodes.Test };
		var security2 = new SecurityId { SecurityCode = "TEST2", BoardCode = BoardCodes.Test };
		var dates = new[] { DateTime.UtcNow.Date };
		var token = CancellationToken;

		await SetupTestDataAsync(drive, security1, DataType.Ticks, StorageFormats.Binary, dates);
		await SetupTestDataAsync(drive, security2, DataType.Ticks, StorageFormats.Binary, dates);

		var securities = await drive.GetAvailableSecuritiesAsync().ToArrayAsync(token);

		securities.Length.AssertEqual(2);
		securities.Count(s => s.SecurityCode == "TEST1" && s.BoardCode == BoardCodes.Test).AssertEqual(1);
		securities.Count(s => s.SecurityCode == "TEST2" && s.BoardCode == BoardCodes.Test).AssertEqual(1);
	}

	[TestMethod]
	public async Task GetAvailableSecuritiesAsync_CancellationRequested_ThrowsOperationCanceledException()
	{
		var drive = CreateDrive();
		var security = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var dates = new[] { DateTime.UtcNow.Date };

		await SetupTestDataAsync(drive, security, DataType.Ticks, StorageFormats.Binary, dates);

		var cts = new CancellationTokenSource();
		var token = cts.Token;
		cts.Cancel();

		await ThrowsAsync<OperationCanceledException>(async () =>
			await drive.GetAvailableSecuritiesAsync().ToArrayAsync(token));
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task GetAvailableDataTypesAsync_EmptySecurity_ReturnsEmpty(StorageFormats format)
	{
		var drive = CreateDrive();
		var securityId = new SecurityId { SecurityCode = "EMPTY", BoardCode = BoardCodes.Test };
		var token = CancellationToken;

		var dataTypes = await drive.GetAvailableDataTypesAsync(securityId, format).ToArrayAsync(token);

		dataTypes.AssertNotNull();
		dataTypes.Length.AssertEqual(0);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task GetAvailableDataTypesAsync_WithData_ReturnsDataTypes(StorageFormats format)
	{
		var drive = CreateDrive();
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var dates = new[] { DateTime.UtcNow.Date };
		var token = CancellationToken;

		await SetupTestDataAsync(drive, securityId, DataType.Ticks, format, dates);
		await SetupTestDataAsync(drive, securityId, DataType.Level1, format, dates);

		var dataTypes = await drive.GetAvailableDataTypesAsync(securityId, format).ToArrayAsync(token);

		dataTypes.Length.AssertEqual(2);
		dataTypes.Count(d => d == DataType.Ticks).AssertEqual(1);
		dataTypes.Count(d => d == DataType.Level1).AssertEqual(1);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task GetAvailableDataTypesAsync_DefaultSecurityId_ReturnsAllDataTypes(StorageFormats format)
	{
		var drive = CreateDrive();
		var security1 = new SecurityId { SecurityCode = "TEST1", BoardCode = BoardCodes.Test };
		var security2 = new SecurityId { SecurityCode = "TEST2", BoardCode = BoardCodes.Test };
		var dates = new[] { DateTime.UtcNow.Date };
		var token = CancellationToken;

		await SetupTestDataAsync(drive, security1, DataType.Ticks, format, dates);
		await SetupTestDataAsync(drive, security2, DataType.Level1, format, dates);
		await SetupTestDataAsync(drive, security2, DataType.MarketDepth, format, dates);

		var dataTypes = await drive.GetAvailableDataTypesAsync(default, format).ToArrayAsync(token);

		dataTypes.Length.AssertEqual(3);
		dataTypes.Count(d => d == DataType.Ticks).AssertEqual(1);
		dataTypes.Count(d => d == DataType.Level1).AssertEqual(1);
		dataTypes.Count(d => d == DataType.MarketDepth).AssertEqual(1);
	}

	/// <summary>
	/// The format is part of the question, not a hint: asking a drive what it holds in csv must not be
	/// answered with what it holds in binary. A listing that names data the format does not have sends
	/// every export, replay and backtest built on it looking for files that are not there.
	/// </summary>
	[TestMethod]
	public async Task AvailableDataTypesAreListedPerFormatForTheWholeDrive()
	{
		var drive = CreateDrive();
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var dates = new[] { DateTime.UtcNow.Date };
		var token = CancellationToken;

		await SetupTestDataAsync(drive, securityId, DataType.Ticks, StorageFormats.Binary, dates);

		var binary = await drive.GetAvailableDataTypesAsync(default, StorageFormats.Binary).ToArrayAsync(token);
		binary.Length.AssertEqual(1);
		binary[0].AssertEqual(DataType.Ticks);

		var csv = await drive.GetAvailableDataTypesAsync(default, StorageFormats.Csv).ToArrayAsync(token);
		csv.Length.AssertEqual(0, "nothing was ever written to this drive in csv form");
	}

	/// <summary>
	/// What a drive holds is on disk, and the answer to "what is there" has to come from there too.
	/// A collector, another process or a restored backup writing into the same folder is the normal
	/// way data arrives, and a listing answered from a cache that was filled once hides all of it.
	/// </summary>
	[TestMethod]
	public async Task AvailableDataTypesIncludeWhatAppearedOnDiskAfterTheFirstQuestion()
	{
		var fs = Helper.MemorySystem;
		var drive = CreateDrive();
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var date = DateTime.UtcNow.Date;
		var token = CancellationToken;

		// The first question is asked while the drive is still empty.
		(await drive.GetAvailableDataTypesAsync(default, StorageFormats.Binary).ToArrayAsync(token)).Length.AssertEqual(0);

		// Data appears in the folder the way it does when someone else collected it.
		var dir = Path.Combine(drive.GetSecurityPath(securityId), LocalMarketDataDrive.GetDirName(date));
		fs.CreateDirectory(dir);
		fs.WriteAllBytes(Path.Combine(dir, LocalMarketDataDrive.GetFileName(DataType.Ticks, StorageFormats.Binary)), []);

		var dataTypes = await drive.GetAvailableDataTypesAsync(default, StorageFormats.Binary).ToArrayAsync(token);

		dataTypes.AssertContains(DataType.Ticks, "data that is on disk has to be listed");
	}

	/// <summary>
	/// Listing what a whole drive holds walks every instrument, every day and every file under it, so
	/// a store where that cost matters can hold the answer for a while. The deal it buys is precise:
	/// what this drive writes itself is listed at once, and only what another writer put into the same
	/// folder waits out the period.
	/// </summary>
	[TestMethod]
	public async Task AvailableDataTypesAreHeldForTheConfiguredPeriodButNeverForThisDrivesOwnWrites()
	{
		var fs = Helper.MemorySystem;
		var drive = CreateDrive();
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var date = DateTime.UtcNow.Date;
		var token = CancellationToken;

		// Long enough that nothing below can expire it by running slowly.
		drive.AvailableDataTypesCachePeriod = TimeSpan.FromMinutes(10);

		await SetupTestDataAsync(drive, securityId, DataType.Ticks, StorageFormats.Binary, [date]);

		(await drive.GetAvailableDataTypesAsync(default, StorageFormats.Binary).ToArrayAsync(token))
			.AssertContains(DataType.Ticks);

		// Another writer on the same folder, which this drive learns of only when the period is out.
		var other = CreateDrive(drive.Path);
		await SetupTestDataAsync(other, securityId, DataType.MarketDepth, StorageFormats.Binary, [date]);

		var held = await drive.GetAvailableDataTypesAsync(default, StorageFormats.Binary).ToArrayAsync(token);

		held.AssertContains(DataType.Ticks);
		held.Contains(DataType.MarketDepth).AssertFalse("the answer is held for the period it was given");

		// What this drive writes is another matter: it knows about it and says so straight away.
		await SetupTestDataAsync(drive, securityId, DataType.Level1, StorageFormats.Binary, [date]);

		(await drive.GetAvailableDataTypesAsync(default, StorageFormats.Binary).ToArrayAsync(token))
			.AssertContains(DataType.Level1, "a type this drive has just written cannot be missing from its own answer");
	}

	[TestMethod]
	public async Task VerifyAsync_ExistingPath_DoesNotThrow()
	{
		var drive = CreateDrive();

		await drive.VerifyAsync(CancellationToken);

		// Should not throw
		true.AssertTrue();
	}

	[TestMethod]
	public Task VerifyAsync_NonExistingPath_ThrowsInvalidOperationException()
	{
		var drive = new LocalMarketDataDrive(Helper.MemorySystem, Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
		var token = CancellationToken;

		return ThrowsExactlyAsync<InvalidOperationException>(async () =>
		{
			await drive.VerifyAsync(token);
		});
	}

	[TestMethod]
	public async Task LookupSecuritiesAsync_EmptyDrive_ReturnsEmpty()
	{
		var drive = CreateDrive();
		var criteria = Messages.Extensions.LookupAllCriteriaMessage;
		var securityProvider = new CollectionSecurityProvider([]);
		var token = CancellationToken;

		var securities = await drive.LookupSecuritiesAsync(criteria, securityProvider).ToArrayAsync(token);

		securities.Length.AssertEqual(0);
	}


	[TestMethod]
	public Task LookupSecuritiesAsync_NullCriteria_ThrowsArgumentNullException()
	{
		var drive = CreateDrive();
		var securityProvider = new CollectionSecurityProvider([]);
		var token = CancellationToken;

		return ThrowsExactlyAsync<ArgumentNullException>(async () =>
		{
			await drive.LookupSecuritiesAsync(null, securityProvider).ToArrayAsync(token);
		});
	}

	[TestMethod]
	public Task LookupSecuritiesAsync_NullSecurityProvider_ThrowsArgumentNullException()
	{
		var drive = CreateDrive();
		var criteria = Messages.Extensions.LookupAllCriteriaMessage;
		var token = CancellationToken;

		return ThrowsExactlyAsync<ArgumentNullException>(async () =>
		{
			await drive.LookupSecuritiesAsync(criteria, null).ToArrayAsync(token);
		});
	}

	[TestMethod]
	public async Task LookupSecuritiesAsync_CancellationRequested_ThrowsOperationCanceledException()
	{
		var drive = CreateDrive();
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var dates = new[] { DateTime.UtcNow.Date };

		await SetupTestDataAsync(drive, securityId, DataType.Ticks, StorageFormats.Binary, dates);

		var criteria = Messages.Extensions.LookupAllCriteriaMessage;
		var securityProvider = new CollectionSecurityProvider([]);

		var cts = new CancellationTokenSource();
		var token = cts.Token;
		cts.Cancel();

		await ThrowsExactlyAsync<OperationCanceledException>(async () =>
		{
			await drive.LookupSecuritiesAsync(criteria, securityProvider).ToArrayAsync(token);
		});
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task GetAvailableDataTypesAsync_MultipleDates_ReturnsDataTypes(StorageFormats format)
	{
		var drive = CreateDrive();
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var dates = new[]
		{
			DateTime.UtcNow.Date.AddDays(-2),
			DateTime.UtcNow.Date.AddDays(-1),
			DateTime.UtcNow.Date
		};
		var token = CancellationToken;

		await SetupTestDataAsync(drive, securityId, DataType.Ticks, format, dates);

		var dataTypes = await drive.GetAvailableDataTypesAsync(securityId, format).ToArrayAsync(token);

		dataTypes.Length.AssertEqual(1);
		dataTypes.Count(d => d == DataType.Ticks).AssertEqual(1);
	}

	[TestMethod]
	public async Task GetAvailableSecuritiesAsync_MultipleSecurities_ReturnsAllSecurities()
	{
		var drive = CreateDrive();
		var securities = new[]
		{
			new SecurityId { SecurityCode = "AAPL", BoardCode = BoardCodes.Test },
			new SecurityId { SecurityCode = "MSFT", BoardCode = BoardCodes.Test },
			new SecurityId { SecurityCode = "GOOGL", BoardCode = BoardCodes.Test }
		};
		var dates = new[] { DateTime.UtcNow.Date };
		var token = CancellationToken;

		foreach (var secId in securities)
		{
			await SetupTestDataAsync(drive, secId, DataType.Ticks, StorageFormats.Binary, dates);
		}

		var result = await drive.GetAvailableSecuritiesAsync().ToArrayAsync(token);

		result.Length.AssertEqual(3);
		foreach (var secId in securities)
		{
			result.Count(s => s.SecurityCode == secId.SecurityCode && s.BoardCode == secId.BoardCode).AssertEqual(1);
		}
	}

	[TestMethod]
	public Task VerifyAsync_NestedNonExistingPath_ThrowsInvalidOperationException()
	{
		var invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "nested", "path");
		var drive = new LocalMarketDataDrive(Helper.MemorySystem, invalidPath);
		var token = CancellationToken;

		return ThrowsExactlyAsync<InvalidOperationException>(async () =>
		{
			await drive.VerifyAsync(token);
		});
	}
}

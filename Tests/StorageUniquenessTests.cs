namespace StockSharp.Tests;

[TestClass]
public class StorageUniquenessTests : BaseTestClass
{
	private static LocalMarketDataDrive CreateDrive(string path = null)
	{
		var fs = Helper.MemorySystem;
		return new(fs, path ?? fs.GetSubTemp());
	}

	private async Task SetupTestDataAsync(LocalMarketDataDrive drive, SecurityId securityId, DataType dataType, StorageFormats format, DateTime[] dates)
	{
		var storageDrive = drive.GetStorageDrive(securityId, dataType, format);

		foreach (var date in dates)
		{
			using var stream = new MemoryStream();
			stream.Position = 0;
			await storageDrive.SaveStreamAsync(date, stream, CancellationToken);
		}
	}

	// ==========================================
	// GetAvailableSecuritiesAsync uniqueness
	// ==========================================

	[TestMethod]
	public async Task GetAvailableSecuritiesAsync_WithIndex_NoDuplicates()
	{
		// Setup: save data so both filesystem dirs AND index exist
		var fs = Helper.MemorySystem;
		var path = fs.GetSubTemp();
		var drive = new LocalMarketDataDrive(fs, path);

		var sec1 = new SecurityId { SecurityCode = "AAPL", BoardCode = BoardCodes.Test };
		var sec2 = new SecurityId { SecurityCode = "MSFT", BoardCode = BoardCodes.Test };
		var dates = new[] { DateTime.UtcNow.Date };

		await SetupTestDataAsync(drive, sec1, DataType.Ticks, StorageFormats.Binary, dates);
		await SetupTestDataAsync(drive, sec2, DataType.Ticks, StorageFormats.Binary, dates);

		// Build index — now securities are in BOTH index.bin and filesystem dirs
		await drive.BuildIndexAsync(null, (_, _) => { }, CancellationToken);

		// Create a fresh drive to force index load from file
		var drive2 = new LocalMarketDataDrive(fs, path);

		var securities = await drive2.GetAvailableSecuritiesAsync().ToArrayAsync(CancellationToken);

		// Must contain both securities
		securities.Any(s => s.SecurityCode == "AAPL").AssertTrue("AAPL should be present");
		securities.Any(s => s.SecurityCode == "MSFT").AssertTrue("MSFT should be present");

		// Must have NO duplicates — each security should appear exactly once
		var distinct = securities.Distinct().ToArray();
		distinct.Length.AssertEqual(securities.Length, "GetAvailableSecuritiesAsync returned duplicates");
	}

	[TestMethod]
	public async Task GetAvailableSecuritiesAsync_SingleSecurity_WithIndex_NoDuplicates()
	{
		var fs = Helper.MemorySystem;
		var path = fs.GetSubTemp();
		var drive = new LocalMarketDataDrive(fs, path);

		var sec = new SecurityId { SecurityCode = "SBER", BoardCode = BoardCodes.Test };
		var dates = new[] { DateTime.UtcNow.Date };

		await SetupTestDataAsync(drive, sec, DataType.Ticks, StorageFormats.Binary, dates);
		await SetupTestDataAsync(drive, sec, DataType.Level1, StorageFormats.Binary, dates);

		// Build index
		await drive.BuildIndexAsync(null, (_, _) => { }, CancellationToken);

		var drive2 = new LocalMarketDataDrive(fs, path);
		var securities = await drive2.GetAvailableSecuritiesAsync().ToArrayAsync(CancellationToken);

		// SBER should appear exactly once, not twice (index + filesystem)
		var sberCount = securities.Count(s => s.SecurityCode == "SBER" && s.BoardCode == BoardCodes.Test);
		sberCount.AssertEqual(1, "Security SBER should appear exactly once");
	}

	[TestMethod]
	public async Task GetAvailableSecuritiesAsync_ManySecurities_WithIndex_AllUnique()
	{
		var fs = Helper.MemorySystem;
		var path = fs.GetSubTemp();
		var drive = new LocalMarketDataDrive(fs, path);

		var codes = new[] { "AAA", "BBB", "CCC", "DDD", "EEE" };
		var dates = new[] { DateTime.UtcNow.Date };

		foreach (var code in codes)
		{
			var secId = new SecurityId { SecurityCode = code, BoardCode = BoardCodes.Test };
			await SetupTestDataAsync(drive, secId, DataType.Ticks, StorageFormats.Binary, dates);
		}

		await drive.BuildIndexAsync(null, (_, _) => { }, CancellationToken);

		var drive2 = new LocalMarketDataDrive(fs, path);
		var securities = await drive2.GetAvailableSecuritiesAsync().ToArrayAsync(CancellationToken);

		var uniqueCount = securities.Distinct().Count();
		uniqueCount.AssertEqual(securities.Length, "All securities must be unique (no duplicates from index + filesystem)");
		uniqueCount.AssertEqual(codes.Length, "Should return exactly the number of saved securities");
	}

	[TestMethod]
	public async Task GetAvailableSecuritiesAsync_WithIndex_NewDataAfterIndex()
	{
		// Setup: build index, then add NEW security to filesystem without rebuilding index
		var fs = Helper.MemorySystem;
		var path = fs.GetSubTemp();
		var drive = new LocalMarketDataDrive(fs, path);

		var sec1 = new SecurityId { SecurityCode = "OLD1", BoardCode = BoardCodes.Test };
		var dates = new[] { DateTime.UtcNow.Date };

		await SetupTestDataAsync(drive, sec1, DataType.Ticks, StorageFormats.Binary, dates);
		await drive.BuildIndexAsync(null, (_, _) => { }, CancellationToken);

		// Add new security AFTER index was built
		var sec2 = new SecurityId { SecurityCode = "NEW1", BoardCode = BoardCodes.Test };
		var drive2 = new LocalMarketDataDrive(fs, path);
		await SetupTestDataAsync(drive2, sec2, DataType.Ticks, StorageFormats.Binary, dates);

		// Fresh drive — loads index from file, but sec2 only exists on filesystem
		var drive3 = new LocalMarketDataDrive(fs, path);
		var securities = await drive3.GetAvailableSecuritiesAsync().ToArrayAsync(CancellationToken);

		// Both old (from index) and new (from filesystem) must be present
		securities.Any(s => s.SecurityCode == "OLD1").AssertTrue("OLD1 (from index) should be present");
		securities.Any(s => s.SecurityCode == "NEW1").AssertTrue("NEW1 (from filesystem) should be present");

		// No duplicates
		var distinct = securities.Distinct().ToArray();
		distinct.Length.AssertEqual(securities.Length, "No duplicates expected");
	}

	[TestMethod]
	public async Task GetAvailableSecuritiesAsync_WithoutIndex_NoDuplicates()
	{
		// Without index — only filesystem. Should still be unique.
		var drive = CreateDrive();

		var sec1 = new SecurityId { SecurityCode = "TEST1", BoardCode = BoardCodes.Test };
		var sec2 = new SecurityId { SecurityCode = "TEST2", BoardCode = BoardCodes.Test };
		var dates = new[] { DateTime.UtcNow.Date };

		await SetupTestDataAsync(drive, sec1, DataType.Ticks, StorageFormats.Binary, dates);
		await SetupTestDataAsync(drive, sec2, DataType.Ticks, StorageFormats.Binary, dates);

		// Save same security with different data type — should not create duplicate SecurityId
		await SetupTestDataAsync(drive, sec1, DataType.Level1, StorageFormats.Binary, dates);

		var securities = await drive.GetAvailableSecuritiesAsync().ToArrayAsync(CancellationToken);

		var distinct = securities.Distinct().ToArray();
		distinct.Length.AssertEqual(securities.Length, "Filesystem-only: no duplicate securities expected");
	}

	// ==========================================
	// GetAvailableDataTypesAsync uniqueness
	// ==========================================

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task GetAvailableDataTypesAsync_SpecificSecurity_NoDuplicates(StorageFormats format)
	{
		var drive = CreateDrive();
		var secId = new SecurityId { SecurityCode = "UNQ1", BoardCode = BoardCodes.Test };
		var dates = new[]
		{
			DateTime.UtcNow.Date.AddDays(-1),
			DateTime.UtcNow.Date
		};

		// Save same data type for multiple dates — should not duplicate data type
		await SetupTestDataAsync(drive, secId, DataType.Ticks, format, dates);
		await SetupTestDataAsync(drive, secId, DataType.Level1, format, dates);

		var dataTypes = await drive.GetAvailableDataTypesAsync(secId, format).ToArrayAsync(CancellationToken);

		var distinct = dataTypes.Distinct().ToArray();
		distinct.Length.AssertEqual(dataTypes.Length, "Data types for a specific security must be unique");
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task GetAvailableDataTypesAsync_DefaultSecurity_NoDuplicates(StorageFormats format)
	{
		var drive = CreateDrive();
		var sec1 = new SecurityId { SecurityCode = "UNQ2", BoardCode = BoardCodes.Test };
		var sec2 = new SecurityId { SecurityCode = "UNQ3", BoardCode = BoardCodes.Test };
		var dates = new[] { DateTime.UtcNow.Date };

		// Both securities have Ticks — when querying default(SecurityId), Ticks should appear once
		await SetupTestDataAsync(drive, sec1, DataType.Ticks, format, dates);
		await SetupTestDataAsync(drive, sec2, DataType.Ticks, format, dates);
		await SetupTestDataAsync(drive, sec1, DataType.Level1, format, dates);

		var dataTypes = await drive.GetAvailableDataTypesAsync(default, format).ToArrayAsync(CancellationToken);

		var distinct = dataTypes.Distinct().ToArray();
		distinct.Length.AssertEqual(dataTypes.Length, "Data types across all securities (default) must be unique");
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task GetAvailableDataTypesAsync_WithIndex_NoDuplicates(StorageFormats format)
	{
		var fs = Helper.MemorySystem;
		var path = fs.GetSubTemp();
		var drive = new LocalMarketDataDrive(fs, path);

		var sec1 = new SecurityId { SecurityCode = "IDX1", BoardCode = BoardCodes.Test };
		var sec2 = new SecurityId { SecurityCode = "IDX2", BoardCode = BoardCodes.Test };
		var dates = new[] { DateTime.UtcNow.Date };

		await SetupTestDataAsync(drive, sec1, DataType.Ticks, format, dates);
		await SetupTestDataAsync(drive, sec2, DataType.Ticks, format, dates);
		await SetupTestDataAsync(drive, sec1, DataType.Level1, format, dates);

		await drive.BuildIndexAsync(null, (_, _) => { }, CancellationToken);

		var drive2 = new LocalMarketDataDrive(fs, path);

		// With index present
		var dataTypesDefault = await drive2.GetAvailableDataTypesAsync(default, format).ToArrayAsync(CancellationToken);
		dataTypesDefault.Distinct().Count().AssertEqual(dataTypesDefault.Length, "Data types (default secId, with index) must be unique");

		var dataTypesSec1 = await drive2.GetAvailableDataTypesAsync(sec1, format).ToArrayAsync(CancellationToken);
		dataTypesSec1.Distinct().Count().AssertEqual(dataTypesSec1.Length, "Data types (specific secId, with index) must be unique");
	}

	// ==========================================
	// GetDatesAsync uniqueness
	// ==========================================

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task GetDatesAsync_NoDuplicates(StorageFormats format)
	{
		var drive = CreateDrive();
		var secId = new SecurityId { SecurityCode = "DT1", BoardCode = BoardCodes.Test };
		var dates = new[]
		{
			DateTime.UtcNow.Date.AddDays(-3),
			DateTime.UtcNow.Date.AddDays(-2),
			DateTime.UtcNow.Date.AddDays(-1),
			DateTime.UtcNow.Date,
		};

		await SetupTestDataAsync(drive, secId, DataType.Ticks, format, dates);

		var storageDrive = drive.GetStorageDrive(secId, DataType.Ticks, format);
		var resultDates = await storageDrive.GetDatesAsync().ToArrayAsync(CancellationToken);

		var distinct = resultDates.Distinct().ToArray();
		distinct.Length.AssertEqual(resultDates.Length, "Dates must be unique");
		distinct.Length.AssertEqual(dates.Length, "Should return all saved dates");
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task GetDatesAsync_SaveSameDateTwice_NoDuplicates(StorageFormats format)
	{
		var drive = CreateDrive();
		var secId = new SecurityId { SecurityCode = "DT2", BoardCode = BoardCodes.Test };
		var date = DateTime.UtcNow.Date;

		// Save same date twice
		await SetupTestDataAsync(drive, secId, DataType.Ticks, format, [date]);
		await SetupTestDataAsync(drive, secId, DataType.Ticks, format, [date]);

		var storageDrive = drive.GetStorageDrive(secId, DataType.Ticks, format);
		var resultDates = await storageDrive.GetDatesAsync().ToArrayAsync(CancellationToken);

		resultDates.Length.AssertEqual(1, "Same date saved twice should still return 1 date");
	}

	[TestMethod]
	public async Task GetDatesAsync_WithIndex_NoDuplicates()
	{
		var fs = Helper.MemorySystem;
		var path = fs.GetSubTemp();
		var drive = new LocalMarketDataDrive(fs, path);

		var secId = new SecurityId { SecurityCode = "DT3", BoardCode = BoardCodes.Test };
		var dates = new[]
		{
			DateTime.UtcNow.Date.AddDays(-2),
			DateTime.UtcNow.Date.AddDays(-1),
			DateTime.UtcNow.Date,
		};

		await SetupTestDataAsync(drive, secId, DataType.Ticks, StorageFormats.Binary, dates);
		await drive.BuildIndexAsync(null, (_, _) => { }, CancellationToken);

		var drive2 = new LocalMarketDataDrive(fs, path);
		var storageDrive = drive2.GetStorageDrive(secId, DataType.Ticks, StorageFormats.Binary);
		var resultDates = await storageDrive.GetDatesAsync().ToArrayAsync(CancellationToken);

		var distinct = resultDates.Distinct().ToArray();
		distinct.Length.AssertEqual(resultDates.Length, "Dates with index must be unique");
		distinct.Length.AssertEqual(dates.Length, "Should return all saved dates");
	}
}

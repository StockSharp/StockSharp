namespace StockSharp.Tests;

using IOPath = System.IO.Path;

using StockSharp.Algo.Storages;

[TestClass]
public class Level1StorageBackwardCompatTests : BaseTestClass
{
	private static IStorageRegistry GetStorageRegistry(IFileSystem fs)
	{
		return fs.GetStorage(fs.GetSubTemp());
	}

	#region FileNameToDataType / DataTypeToFileName

	[TestMethod]
	public void DataTypeToFileName_Level1_ReturnsLevel1()
	{
		DataType.Level1.DataTypeToFileName().AssertEqual("level1");
	}

	[TestMethod]
	public void FileNameToDataType_Level1_ReturnsLevel1()
	{
		"level1".FileNameToDataType().AssertEqual(DataType.Level1);
	}

	[TestMethod]
	public void FileNameToDataType_Security_ReturnsLevel1_BackwardCompat()
	{
		// old filename "security" should still resolve to Level1
		"security".FileNameToDataType().AssertEqual(DataType.Level1);
	}

	[TestMethod]
	public void FileNameToDataType_SecurityCaseInsensitive()
	{
		"Security".FileNameToDataType().AssertEqual(DataType.Level1);
		"SECURITY".FileNameToDataType().AssertEqual(DataType.Level1);
	}

	[TestMethod]
	public void DataTypeToFileName_Level1_Roundtrip()
	{
		var fn = DataType.Level1.DataTypeToFileName();
		fn.FileNameToDataType().AssertEqual(DataType.Level1);
	}

	#endregion

	#region Storage read/write

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task Level1_NewData_WritesToLevel1File(StorageFormats format)
	{
		var fs = Helper.MemorySystem;
		var registry = GetStorageRegistry(fs);
		var token = CancellationToken;

		var security = Helper.CreateSecurity();
		var securityId = security.ToSecurityId();
		var testValues = security.RandomLevel1(count: 100);

		var storage = registry.GetLevel1MessageStorage(securityId, null, format);
		await storage.SaveAsync(testValues, token);

		// verify data was written to "level1" file, not "security"
		var expectedFileName = "level1" + LocalMarketDataDrive.GetExtension(format);
		var drive = (LocalMarketDataDrive)storage.Drive.Drive;

		var loaded = await storage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token);
		loaded.Length.AssertEqual(testValues.Length);

		await storage.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task Level1_ReadFromLegacySecurityFile(StorageFormats format)
	{
		var fs = Helper.MemorySystem;
		var token = CancellationToken;

		var security = Helper.CreateSecurity();
		var securityId = security.ToSecurityId();
		var testValues = security.RandomLevel1(count: 100);

		// Step 1: save using a storage that writes to "level1" file
		var registry1 = GetStorageRegistry(fs);
		var storage1 = registry1.GetLevel1MessageStorage(securityId, null, format);
		await storage1.SaveAsync(testValues, token);

		// Step 2: find the written file and rename it from "level1.xxx" to "security.xxx"
		var ext = LocalMarketDataDrive.GetExtension(format);
		var drive = (LocalMarketDataDrive)storage1.Drive.Drive;
		var secPath = drive.GetSecurityPath(securityId);

		var dateDirs = fs.GetDirectories(secPath);

		foreach (var dir in dateDirs)
		{
			var newFile = IOPath.Combine(dir, "level1" + ext);
			var legacyFile = IOPath.Combine(dir, "security" + ext);

			if (fs.FileExists(newFile))
			{
				var data = fs.ReadAllBytes(newFile);
				fs.WriteAllBytes(legacyFile, data);
				fs.DeleteFile(newFile);
			}
		}

		// also rename/delete dates cache so it gets rebuilt
		await storage1.Drive.ClearDatesCacheAsync(token);

		// Step 3: create a fresh storage and load — should find "security.xxx" files
		var registry2 = fs.GetStorage(drive.Path);
		var storage2 = registry2.GetLevel1MessageStorage(securityId, null, format);

		var loaded = await storage2.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token);
		loaded.Length.AssertEqual(testValues.Length);
		loaded.CompareMessages(testValues, skipLocalTime: format == StorageFormats.Csv);

		// cleanup
		await storage2.DeleteWithCheckAsync(token);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task Level1_NewWriteDoesNotUseLegacyName(StorageFormats format)
	{
		var fs = Helper.MemorySystem;
		var registry = GetStorageRegistry(fs);
		var token = CancellationToken;

		var security = Helper.CreateSecurity();
		var securityId = security.ToSecurityId();
		var testValues = security.RandomLevel1(count: 50);

		var storage = registry.GetLevel1MessageStorage(securityId, null, format);
		await storage.SaveAsync(testValues, token);

		var ext = LocalMarketDataDrive.GetExtension(format);
		var drive = (LocalMarketDataDrive)storage.Drive.Drive;
		var secPath = drive.GetSecurityPath(securityId);

		var dateDirs = fs.GetDirectories(secPath);
		dateDirs.Count().AssertGreater(0);

		foreach (var dir in dateDirs)
		{
			var newFile = IOPath.Combine(dir, "level1" + ext);
			var legacyFile = IOPath.Combine(dir, "security" + ext);

			fs.FileExists(newFile).AssertTrue($"Expected level1{ext} in {dir}");
			fs.FileExists(legacyFile).AssertFalse($"Should not create security{ext} in {dir}");
		}

		await storage.DeleteWithCheckAsync(token);
	}

	#endregion

	#region GetFileName

	[TestMethod]
	public void GetFileName_Level1_Binary()
	{
		var fn = LocalMarketDataDrive.GetFileName(DataType.Level1, StorageFormats.Binary);
		fn.AssertEqual("level1.bin");
	}

	[TestMethod]
	public void GetFileName_Level1_Csv()
	{
		var fn = LocalMarketDataDrive.GetFileName(DataType.Level1, StorageFormats.Csv);
		fn.AssertEqual("level1.csv");
	}

	[TestMethod]
	public void GetFileName_Level1_NoFormat()
	{
		var fn = LocalMarketDataDrive.GetFileName(DataType.Level1);
		fn.AssertEqual("level1");
	}

	#endregion

	#region HistoryData (real legacy security.bin)

	[TestMethod]
	public async Task Level1_LoadFromHistoryData_LegacySecurityBin()
	{
		var token = CancellationToken;
		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();
		var storageRegistry = Helper.FileSystem.GetStorage(Paths.HistoryDataPath);

		var storage = storageRegistry.GetLevel1MessageStorage(secId, format: StorageFormats.Binary);
		var dates = await storage.GetDatesAsync().ToArrayAsync(token);

		dates.Length.AssertGreater(0, "HistoryData should contain Level1 dates");

		var data = await storage.LoadAsync(Paths.HistoryBeginDate, Paths.HistoryEndDate).ToArrayAsync(token);

		data.Length.AssertGreater(0, "Should load Level1 data from legacy security.bin files");

		// verify data has expected fields
		var first = data[0];
		first.SecurityId.AssertEqual(secId);
		first.Changes.Count.AssertGreater(0, "Level1 message should have changes");
	}

	#endregion
}

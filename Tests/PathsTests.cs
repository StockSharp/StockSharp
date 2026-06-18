namespace StockSharp.Tests;

[TestClass]
public class PathsTests : BaseTestClass
{
	[TestMethod]
	public void GetNuGetGlobalPackagesFolder_ReturnsValidPath()
	{
		var path = Paths.GetNuGetGlobalPackagesFolder();

		path.AssertNotNull();
		IsNotEmpty(path);
		Path.IsPathRooted(path).AssertTrue();
	}

	[TestMethod]
	public void GetHistoryDataPath_WithNuGetFolder()
	{
		var packagesFolder = Paths.GetNuGetGlobalPackagesFolder();

		// May return null if stocksharp.samples.historydata is not installed
		var historyPath = Paths.GetHistoryDataPath(packagesFolder);

		if (historyPath == null)
			Inconclusive("stocksharp.samples.historydata is not installed.");

		Path.IsPathRooted(historyPath).AssertTrue();
		historyPath.ContainsIgnoreCase("HistoryData").AssertTrue();
	}

	[TestMethod]
	public void GetHistoryDataPath_WithNonExistentFolder()
	{
		var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

		var historyPath = Paths.GetHistoryDataPath(nonExistentPath);

		historyPath.AssertNull();
	}

	[TestMethod]
	public void HistoryDataPath_IsValidIfPackageInstalled()
	{
		// HistoryDataPath is initialized in static constructor
		// It will be null if package is not installed
		if (Paths.HistoryDataPath == null)
			Inconclusive("stocksharp.samples.historydata is not installed.");

		Path.IsPathRooted(Paths.HistoryDataPath).AssertTrue();
		Directory.Exists(Paths.HistoryDataPath).AssertTrue();
	}

	[TestMethod]
	public void HistoryDataPath_ContainsDefaultSecurities()
	{
		if (Paths.HistoryDataPath == null)
			Inconclusive("stocksharp.samples.historydata is not installed.");

		// Securities are grouped by first character: SomePath\T\TONUSDT@BNBFT
		var sec1 = Paths.HistoryDefaultSecurity;  // BTCUSDT@BNBFT
		var sec2 = Paths.HistoryDefaultSecurity2; // TONUSDT@BNBFT

		var path1 = Path.Combine(Paths.HistoryDataPath, sec1[0].ToString(), sec1);
		var path2 = Path.Combine(Paths.HistoryDataPath, sec2[0].ToString(), sec2);

		Directory.Exists(path1).AssertTrue();
		Directory.Exists(path2).AssertTrue();
	}

	[TestMethod]
	public void Serialize_CreatesMissingDirectory()
	{
		// Real LocalFileSystem so the missing-parent behaviour is actually exercised; all filesystem
		// access goes through IFileSystem.
		var fileSystem = LocalFileSystem.Instance;

		var dir = Path.Combine(Path.GetTempPath(), "ss_paths_" + Guid.NewGuid().ToString("N"));
		var path = Path.Combine(dir, "sub", "cache.bin");

		try
		{
			var settings = new SettingsStorage();
			settings.Set("x", 42);

			// The regression is HERE: without the fix this throws DirectoryNotFoundException because the
			// parent folder does not exist and OpenWrite does not create it. The fix creates it first.
			settings.Serialize(fileSystem, path);

			// The fix created the missing folder and the file was written into it.
			fileSystem.DirectoryExists(dir).AssertTrue();
			fileSystem.FileExists(path).AssertTrue();
		}
		finally
		{
			if (fileSystem.DirectoryExists(dir))
				fileSystem.DeleteDirectory(dir, true);
		}
	}

	[TestMethod]
	public void CompanyPath_IsValid()
	{
		Paths.CompanyPath.AssertNotNull();
		IsNotEmpty(Paths.CompanyPath);
		Path.IsPathRooted(Paths.CompanyPath).AssertTrue();
	}

	[TestMethod]
	public void AppDataPath_IsValid()
	{
		Paths.AppDataPath.AssertNotNull();
		IsNotEmpty(Paths.AppDataPath);
		Path.IsPathRooted(Paths.AppDataPath).AssertTrue();
	}

	[TestMethod]
	public void StorageDir_IsUnderAppDataPath()
	{
		Paths.StorageDir.AssertNotNull();
		Paths.StorageDir.StartsWithIgnoreCase(Paths.AppDataPath).AssertTrue();
	}
}

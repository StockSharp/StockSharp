namespace StockSharp.Tests;

using StockSharp.Localization;

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
			Fail("stocksharp.samples.historydata is not installed. It is a test dependency, not an option: without it these checks verify nothing.");

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
			Fail("stocksharp.samples.historydata is not installed. It is a test dependency, not an option: without it these checks verify nothing.");

		Path.IsPathRooted(Paths.HistoryDataPath).AssertTrue();
		Directory.Exists(Paths.HistoryDataPath).AssertTrue();
	}

	[TestMethod]
	public void HistoryDataPath_ContainsDefaultSecurities()
	{
		if (Paths.HistoryDataPath == null)
			Fail("stocksharp.samples.historydata is not installed. It is a test dependency, not an option: without it these checks verify nothing.");

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

	// A language nothing has registered is refused by the setter, and a test host loads only what its own
	// resources carry. Registering an empty translation is enough here: what is under test is the address built
	// for a language, not how any word in it reads.
	private static void UseLanguage(string code)
	{
		if (!LocalizedStrings.LangCodes.Any(l => l.EqualsIgnoreCase(code)))
			LocalizedStrings.AddLanguage(code, new Dictionary<string, string>());

		LocalizedStrings.ActiveLanguage = code;

		AreEqual(code, LocalizedStrings.ActiveLanguage);
	}

	[TestMethod]
	public void SiteAddresses_FollowTheLanguage()
	{
		// One test rather than three: the language is global state, and separate tests mutating it race each
		// other into failures that say nothing about the addresses.
		var previous = LocalizedStrings.ActiveLanguage;

		try
		{
			UseLanguage(LocalizedStrings.RuCode);

			AreEqual("https://stocksharp.com", Paths.GetWebSiteUrl());
			AreEqual("ru", Paths.SiteLanguage);
			IsTrue(Paths.GetPageUrl(Paths.Pages.Store).Contains("/ru/store/"));
			IsTrue(Paths.GetDocUrl("topics/api.html").StartsWith("https://doc.stocksharp.com/"));
			IsTrue(Paths.GetDocUrl("topics/api.html").EndsWith("/ru/topics/api.html"));
			IsTrue(Paths.Chat.EndsWith("/ru/chat/"));

			// The server stores its topic links with the language already on them, and prefixing those again
			// asks for "/ru/ru/topics/..." - a page that does not exist.
			IsTrue(Paths.GetDocUrl("ru/topics/api.html").EndsWith("/ru/topics/api.html"));
			IsFalse(Paths.GetDocUrl("ru/topics/api.html").Contains("/ru/ru/"));

			// Files are served from the root whatever one is reading in, and a language in front of one only
			// bounces back to the bare address.
			var file = Paths.GetPageUrl(Paths.Pages.File, 42);

			IsFalse(file.Contains("/ru/"));
			IsTrue(file.Contains("/file/42/"));

			// The applications are translated into more languages than the site is published in; those readers
			// get the English page rather than one that does not exist.
			UseLanguage("cs");

			AreEqual(LocalizedStrings.EnCode, Paths.SiteLanguage);
			IsTrue(Paths.GetPageUrl(Paths.Pages.Store).Contains("/en/store/"));
		}
		finally
		{
			LocalizedStrings.ActiveLanguage = previous;
		}
	}
}

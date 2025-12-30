namespace StockSharp.Tests;

using Ecng.Common;

using StockSharp.Configuration;

[TestClass]
public class DefaultCredentialsProviderTests : BaseTestClass
{
	private static MemoryFileSystem CreateFileSystem() => new();

	[TestMethod]
	public void TryLoad_NoFile_ReturnsFalse()
	{
		var fs = CreateFileSystem();
		ICredentialsProvider provider = new DefaultCredentialsProvider(fs, "/credentials.json", "/company");

		var result = provider.TryLoad(out var credentials);

		result.AssertFalse();
		credentials.AssertNull();
	}

	[TestMethod]
	public void Save_CreatesDirectoryAndFile()
	{
		var fs = CreateFileSystem();
		var credentialsFile = "/company/credentials.json";
		var companyPath = "/company";
		ICredentialsProvider provider = new DefaultCredentialsProvider(fs, credentialsFile, companyPath);

		var credentials = new ServerCredentials
		{
			Email = "test@example.com",
			Password = "secret".Secure(),
		};

		provider.Save(credentials, keepSecret: true);

		fs.DirectoryExists(companyPath).AssertTrue();
		fs.FileExists(credentialsFile).AssertTrue();
	}

	[TestMethod]
	public void SaveAndLoad_RoundTrip_PreservesCredentials()
	{
		var fs = CreateFileSystem();
		var credentialsFile = "/company/credentials.json";
		var companyPath = "/company";
		ICredentialsProvider provider = new DefaultCredentialsProvider(fs, credentialsFile, companyPath);

		var original = new ServerCredentials
		{
			Email = "test@example.com",
			Password = "secret".Secure(),
			Token = "mytoken".Secure(),
		};

		provider.Save(original, keepSecret: true);

		// Create new provider to test loading from file
		ICredentialsProvider provider2 = new DefaultCredentialsProvider(fs, credentialsFile, companyPath);
		var result = provider2.TryLoad(out var loaded);

		result.AssertTrue();
		loaded.AssertNotNull();
		loaded.Email.AssertEqual("test@example.com");
		loaded.Password.IsEmpty().AssertFalse();
		loaded.Token.IsEmpty().AssertFalse();
	}

	[TestMethod]
	public void Save_KeepSecretFalse_DoesNotSavePassword()
	{
		var fs = CreateFileSystem();
		var credentialsFile = "/company/credentials.json";
		var companyPath = "/company";
		ICredentialsProvider provider = new DefaultCredentialsProvider(fs, credentialsFile, companyPath);

		var original = new ServerCredentials
		{
			Email = "test@example.com",
			Password = "secret".Secure(),
			Token = "mytoken".Secure(),
		};

		provider.Save(original, keepSecret: false);

		// Create new provider to test loading from file
		ICredentialsProvider provider2 = new DefaultCredentialsProvider(fs, credentialsFile, companyPath);
		var result = provider2.TryLoad(out var loaded);

		// Should not auto-login because password/token were not saved
		result.AssertFalse();
		loaded.AssertNotNull();
		loaded.Email.AssertEqual("test@example.com");
		loaded.Password.IsEmpty().AssertTrue();
		loaded.Token.IsEmpty().AssertTrue();
	}

	[TestMethod]
	public void TryLoad_CachesCredentials()
	{
		var fs = CreateFileSystem();
		var credentialsFile = "/company/credentials.json";
		var companyPath = "/company";
		ICredentialsProvider provider = new DefaultCredentialsProvider(fs, credentialsFile, companyPath);

		var original = new ServerCredentials
		{
			Email = "test@example.com",
			Password = "secret".Secure(),
		};

		provider.Save(original, keepSecret: true);

		// First load
		provider.TryLoad(out var first);

		// Delete file to prove caching works
		fs.DeleteFile(credentialsFile);

		// Second load should return cached credentials
		var result = provider.TryLoad(out var second);

		result.AssertTrue();
		second.AssertNotNull();
		second.Email.AssertEqual("test@example.com");
	}

	[TestMethod]
	public void Delete_RemovesFile()
	{
		var fs = CreateFileSystem();
		var credentialsFile = "/company/credentials.json";
		var companyPath = "/company";
		ICredentialsProvider provider = new DefaultCredentialsProvider(fs, credentialsFile, companyPath);

		var credentials = new ServerCredentials
		{
			Email = "test@example.com",
			Password = "secret".Secure(),
		};

		provider.Save(credentials, keepSecret: true);
		fs.FileExists(credentialsFile).AssertTrue();

		provider.Delete();

		fs.FileExists(credentialsFile).AssertFalse();
	}

	[TestMethod]
	public void Delete_ClearsCache()
	{
		var fs = CreateFileSystem();
		var credentialsFile = "/company/credentials.json";
		var companyPath = "/company";
		ICredentialsProvider provider = new DefaultCredentialsProvider(fs, credentialsFile, companyPath);

		var credentials = new ServerCredentials
		{
			Email = "test@example.com",
			Password = "secret".Secure(),
		};

		provider.Save(credentials, keepSecret: true);

		// Load to cache
		provider.TryLoad(out _);

		// Delete
		provider.Delete();

		// Try load should return false (cache cleared)
		var result = provider.TryLoad(out var loaded);

		result.AssertFalse();
		loaded.AssertNull();
	}

	[TestMethod]
	public void Delete_NoFile_DoesNotThrow()
	{
		var fs = CreateFileSystem();
		ICredentialsProvider provider = new DefaultCredentialsProvider(fs, "/credentials.json", "/company");

		// Should not throw
		provider.Delete();
	}

	[TestMethod]
	public void Save_NullCredentials_ThrowsArgumentNullException()
	{
		var fs = CreateFileSystem();
		ICredentialsProvider provider = new DefaultCredentialsProvider(fs, "/credentials.json", "/company");

		ThrowsExactly<ArgumentNullException>(() => provider.Save(null, true));
	}
}

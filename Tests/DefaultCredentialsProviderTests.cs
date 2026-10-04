namespace StockSharp.Tests;

using Ecng.Common;

using StockSharp.Configuration;

[TestClass]
public class DefaultCredentialsProviderTests : BaseTestClass
{
	private static MemoryFileSystem CreateFileSystem() => new();

	[TestMethod]
	public async Task TryLoad_NoFile_ReturnsFalse()
	{
		var fs = CreateFileSystem();
		ICredentialsProvider provider = new DefaultCredentialsProvider(fs, "/credentials.json", "/company");

		var credentials = await provider.TryLoadAsync(CancellationToken);
		var result = credentials?.CanAutoLogin() == true;

		result.AssertFalse();
		credentials.AssertNull();
	}

	[TestMethod]
	public async Task Save_CreatesDirectoryAndFile()
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

		await provider.SaveAsync(credentials, true, CancellationToken);

		fs.DirectoryExists(companyPath).AssertTrue();
		fs.FileExists(credentialsFile).AssertTrue();
	}

	[TestMethod]
	public async Task SaveAndLoad_RoundTrip_PreservesCredentials()
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

		await provider.SaveAsync(original, true, CancellationToken);

		// Create new provider to test loading from file
		ICredentialsProvider provider2 = new DefaultCredentialsProvider(fs, credentialsFile, companyPath);
		var loaded = await provider2.TryLoadAsync(CancellationToken);
		var result = loaded?.CanAutoLogin() == true;

		result.AssertTrue();
		loaded.AssertNotNull();
		loaded.Email.AssertEqual("test@example.com");
		loaded.Password.IsEqualTo(original.Password).AssertTrue();
		loaded.Token.IsEqualTo(original.Token).AssertTrue();
	}

	[TestMethod]
	public async Task Save_KeepSecretFalse_DoesNotSavePassword()
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

		await provider.SaveAsync(original, false, CancellationToken);

		// Create new provider to test loading from file
		ICredentialsProvider provider2 = new DefaultCredentialsProvider(fs, credentialsFile, companyPath);
		var loaded = await provider2.TryLoadAsync(CancellationToken);
		var result = loaded?.CanAutoLogin() == true;

		// Should not auto-login because password/token were not saved
		result.AssertFalse();
		loaded.AssertNotNull();
		loaded.Email.AssertEqual("test@example.com");
		loaded.Password.IsEmpty().AssertTrue();
		loaded.Token.IsEmpty().AssertTrue();
	}

	[TestMethod]
	public async Task TryLoad_CachesCredentials()
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

		await provider.SaveAsync(original, true, CancellationToken);

		// Use a fresh provider so the first load must read the file.
		provider = new DefaultCredentialsProvider(fs, credentialsFile, companyPath);

		// First load
		var first = await provider.TryLoadAsync(CancellationToken);
		(first?.CanAutoLogin() == true).AssertTrue();
		first.AssertNotNull();
		first.Email.AssertEqual(original.Email);
		first.Password.IsEqualTo(original.Password).AssertTrue();

		// Delete file to prove caching works
		fs.DeleteFile(credentialsFile);

		// Second load should return cached credentials
		var second = await provider.TryLoadAsync(CancellationToken);
		var result = second?.CanAutoLogin() == true;

		result.AssertTrue();
		second.AssertNotNull();
		second.Email.AssertEqual(original.Email);
		second.Password.IsEqualTo(original.Password).AssertTrue();
	}

	/// <summary>
	/// A load that was cancelled is not remembered as credentials that hold nothing: the next load
	/// reads the file.
	/// </summary>
	[TestMethod]
	public async Task TryLoad_Cancelled_IsNotRemembered()
	{
		var inner = CreateFileSystem();
		var credentialsFile = "/company/credentials.json";
		var companyPath = "/company";

		ICredentialsProvider writer = new DefaultCredentialsProvider(inner, credentialsFile, companyPath);
		await writer.SaveAsync(new ServerCredentials { Email = "test@example.com", Password = "secret".Secure() }, true, CancellationToken);

		using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);

		var fs = new HookedFileSystem(inner)
		{
			Opening = (_, access) =>
			{
				if (access == FileAccess.Read)
					cts.Cancel();
			},
		};

		ICredentialsProvider provider = new DefaultCredentialsProvider(fs, credentialsFile, companyPath);

		var isCancelled = false;

		try
		{
			await provider.TryLoadAsync(cts.Token);
		}
		catch (OperationCanceledException)
		{
			isCancelled = true;
		}

		isCancelled.AssertTrue("a cancelled load must not be answered with empty credentials");

		fs.Opening = null;

		var loaded = await provider.TryLoadAsync(CancellationToken);

		loaded.AssertNotNull();
		loaded.Email.AssertEqual("test@example.com");
	}

	[TestMethod]
	public async Task Delete_RemovesFile()
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

		await provider.SaveAsync(credentials, true, CancellationToken);
		fs.FileExists(credentialsFile).AssertTrue();

		await provider.DeleteAsync(CancellationToken);

		fs.FileExists(credentialsFile).AssertFalse();
	}

	[TestMethod]
	public async Task Delete_ClearsCache()
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

		await provider.SaveAsync(credentials, true, CancellationToken);

		// Load to cache
		await provider.TryLoadAsync(CancellationToken);

		// Delete
		await provider.DeleteAsync(CancellationToken);

		// Try load should return false (cache cleared)
		var loaded = await provider.TryLoadAsync(CancellationToken);
		var result = loaded?.CanAutoLogin() == true;

		result.AssertFalse();
		loaded.AssertNull();
	}

	[TestMethod]
	public async Task Delete_NoFile_DoesNotThrow()
	{
		var fs = CreateFileSystem();
		ICredentialsProvider provider = new DefaultCredentialsProvider(fs, "/credentials.json", "/company");

		// Should not throw
		await provider.DeleteAsync(CancellationToken);
	}

	[TestMethod]
	public async Task Save_NullCredentials_ThrowsArgumentNullException()
	{
		var fs = CreateFileSystem();
		ICredentialsProvider provider = new DefaultCredentialsProvider(fs, "/credentials.json", "/company");

		await ThrowsExactlyAsync<ArgumentNullException>(() => provider.SaveAsync(null, true, CancellationToken).AsTask());
	}
}

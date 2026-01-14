namespace StockSharp.Tests;

using StockSharp.Configuration.Permissions;

[TestClass]
public class FileCredentialsStorageTests : BaseTestClass
{
	private static MemoryFileSystem CreateFileSystem() => new();

	private static PermissionCredentials CreateCredentials(string email, string password = "secret")
	{
		return new PermissionCredentials
		{
			Email = email,
			Password = password.Secure(),
		};
	}

	[TestMethod]
	public async Task Search_EmptyStorage_ReturnsEmpty()
	{
		var fs = CreateFileSystem();
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, "/credentials.json");

		var result = await storage.SearchAsync("*").ToArrayAsync(CancellationToken);

		result.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task Save_CreatesDirectoryAndFile()
	{
		var fs = CreateFileSystem();
		var filePath = "/company/credentials.json";
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, filePath, asEmail: true);

		var credentials = CreateCredentials("test@example.com");

		await storage.SaveAsync(credentials, CancellationToken);

		fs.DirectoryExists("/company").AssertTrue();
		fs.FileExists(filePath).AssertTrue();
	}

	[TestMethod]
	public async Task SaveAndSearch_RoundTrip_PreservesCredentials()
	{
		var fs = CreateFileSystem();
		var filePath = "/company/credentials.json";
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, filePath, asEmail: true);

		var original = CreateCredentials("test@example.com");

		await storage.SaveAsync(original, CancellationToken);

		// Create new storage to test loading from file
		IPermissionCredentialsStorage storage2 = new FileCredentialsStorage(fs, filePath, asEmail: true);
		var result = await storage2.SearchAsync("test@example.com").ToArrayAsync(CancellationToken);

		result.Length.AssertEqual(1);
		result[0].Email.AssertEqual("test@example.com");
	}

	[TestMethod]
	public async Task Search_WildcardPattern_ReturnsMatching()
	{
		var fs = CreateFileSystem();
		var filePath = "/credentials.json";
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, filePath, asEmail: true);

		await storage.SaveAsync(CreateCredentials("admin@example.com"), CancellationToken);
		await storage.SaveAsync(CreateCredentials("user@example.com"), CancellationToken);
		await storage.SaveAsync(CreateCredentials("test@other.com"), CancellationToken);

		var result = await storage.SearchAsync("*@example.com").ToArrayAsync(CancellationToken);

		result.Length.AssertEqual(2);
	}

	[TestMethod]
	public async Task Search_AllPattern_ReturnsAll()
	{
		var fs = CreateFileSystem();
		var filePath = "/credentials.json";
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, filePath, asEmail: true);

		await storage.SaveAsync(CreateCredentials("admin@example.com"), CancellationToken);
		await storage.SaveAsync(CreateCredentials("user@example.com"), CancellationToken);

		var result = await storage.SearchAsync("*").ToArrayAsync(CancellationToken);

		result.Length.AssertEqual(2);
	}

	[TestMethod]
	public async Task Search_EmptyPattern_ReturnsAll()
	{
		var fs = CreateFileSystem();
		var filePath = "/credentials.json";
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, filePath, asEmail: true);

		await storage.SaveAsync(CreateCredentials("admin@example.com"), CancellationToken);
		await storage.SaveAsync(CreateCredentials("user@example.com"), CancellationToken);

		var result = await storage.SearchAsync("").ToArrayAsync(CancellationToken);

		result.Length.AssertEqual(2);
	}

	[TestMethod]
	public async Task Delete_ExistingCredentials_ReturnsTrue()
	{
		var fs = CreateFileSystem();
		var filePath = "/credentials.json";
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, filePath, asEmail: true);

		await storage.SaveAsync(CreateCredentials("test@example.com"), CancellationToken);

		var result = await storage.DeleteAsync("test@example.com", CancellationToken);

		result.AssertTrue();
		(await storage.SearchAsync("test@example.com").ToArrayAsync(CancellationToken)).Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task Delete_NonExistingCredentials_ReturnsFalse()
	{
		var fs = CreateFileSystem();
		var filePath = "/credentials.json";
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, filePath);

		var result = await storage.DeleteAsync("nonexistent", CancellationToken);

		result.AssertFalse();
	}

	[TestMethod]
	public async Task Save_UpdateExisting_OverwritesCredentials()
	{
		var fs = CreateFileSystem();
		var filePath = "/credentials.json";
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, filePath, asEmail: true);

		var original = CreateCredentials("test@example.com", "password1");
		await storage.SaveAsync(original, CancellationToken);

		var updated = CreateCredentials("test@example.com", "password2");
		await storage.SaveAsync(updated, CancellationToken);

		var result = await storage.SearchAsync("test@example.com").ToArrayAsync(CancellationToken);

		result.Length.AssertEqual(1);
	}

	[TestMethod]
	public void Save_NullCredentials_ThrowsArgumentNullException()
	{
		var fs = CreateFileSystem();
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, "/credentials.json");

		ThrowsExactlyAsync<ArgumentNullException>(async () => await storage.SaveAsync(null, CancellationToken));
	}

	[TestMethod]
	public void Save_InvalidEmail_ThrowsArgumentException()
	{
		var fs = CreateFileSystem();
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, "/credentials.json", asEmail: true);

		var credentials = CreateCredentials("invalid-email");

		ThrowsExactlyAsync<ArgumentException>(async () => await storage.SaveAsync(credentials, CancellationToken));
	}

	[TestMethod]
	public async Task Save_ValidUsername_WhenAsEmailFalse()
	{
		var fs = CreateFileSystem();
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, "/credentials.json", asEmail: false);

		// Username: letters, numbers, dots, hyphens, underscores; 3-64 chars
		var credentials = CreateCredentials("admin_user.123");

		await storage.SaveAsync(credentials, CancellationToken);

		var result = await storage.SearchAsync("admin_user.123").ToArrayAsync(CancellationToken);
		result.Length.AssertEqual(1);
	}

	[TestMethod]
	public async Task Search_CaseInsensitive()
	{
		var fs = CreateFileSystem();
		var filePath = "/credentials.json";
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, filePath, asEmail: true);

		await storage.SaveAsync(CreateCredentials("Test@Example.com"), CancellationToken);

		var result = await storage.SearchAsync("test@example.com").ToArrayAsync(CancellationToken);

		result.Length.AssertEqual(1);
	}

	[TestMethod]
	public async Task Delete_CaseInsensitive()
	{
		var fs = CreateFileSystem();
		var filePath = "/credentials.json";
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, filePath, asEmail: true);

		await storage.SaveAsync(CreateCredentials("Test@Example.com"), CancellationToken);

		var result = await storage.DeleteAsync("TEST@EXAMPLE.COM", CancellationToken);

		result.AssertTrue();
		(await storage.SearchAsync("*").ToArrayAsync(CancellationToken)).Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task MultipleInstances_ShareSameFile()
	{
		var fs = CreateFileSystem();
		var filePath = "/credentials.json";

		IPermissionCredentialsStorage storage1 = new FileCredentialsStorage(fs, filePath, asEmail: true);
		await storage1.SaveAsync(CreateCredentials("user1@example.com"), CancellationToken);

		IPermissionCredentialsStorage storage2 = new FileCredentialsStorage(fs, filePath, asEmail: true);
		await storage2.SaveAsync(CreateCredentials("user2@example.com"), CancellationToken);

		// storage1 won't see storage2's changes without reload, but storage2 should have both
		IPermissionCredentialsStorage storage3 = new FileCredentialsStorage(fs, filePath, asEmail: true);
		var result = await storage3.SearchAsync("*").ToArrayAsync(CancellationToken);

		result.Length.AssertEqual(2);
	}
}

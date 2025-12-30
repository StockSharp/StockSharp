namespace StockSharp.Tests;

using Ecng.Common;

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
	public void Search_EmptyStorage_ReturnsEmpty()
	{
		var fs = CreateFileSystem();
		IPermissionCredentialsStorage storage = new FileCredentialsStorage("/credentials.json", fileSystem: fs);

		var result = storage.Search("*").ToArray();

		result.Length.AssertEqual(0);
	}

	[TestMethod]
	public void Save_CreatesDirectoryAndFile()
	{
		var fs = CreateFileSystem();
		var filePath = "/company/credentials.json";
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(filePath, asEmail: true, fileSystem: fs);

		var credentials = CreateCredentials("test@example.com");

		storage.Save(credentials);

		fs.DirectoryExists("/company").AssertTrue();
		fs.FileExists(filePath).AssertTrue();
	}

	[TestMethod]
	public void SaveAndSearch_RoundTrip_PreservesCredentials()
	{
		var fs = CreateFileSystem();
		var filePath = "/company/credentials.json";
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(filePath, asEmail: true, fileSystem: fs);

		var original = CreateCredentials("test@example.com");

		storage.Save(original);

		// Create new storage to test loading from file
		IPermissionCredentialsStorage storage2 = new FileCredentialsStorage(filePath, asEmail: true, fileSystem: fs);
		var result = storage2.Search("test@example.com").ToArray();

		result.Length.AssertEqual(1);
		result[0].Email.AssertEqual("test@example.com");
	}

	[TestMethod]
	public void Search_WildcardPattern_ReturnsMatching()
	{
		var fs = CreateFileSystem();
		var filePath = "/credentials.json";
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(filePath, asEmail: true, fileSystem: fs);

		storage.Save(CreateCredentials("admin@example.com"));
		storage.Save(CreateCredentials("user@example.com"));
		storage.Save(CreateCredentials("test@other.com"));

		var result = storage.Search("*@example.com").ToArray();

		result.Length.AssertEqual(2);
	}

	[TestMethod]
	public void Search_AllPattern_ReturnsAll()
	{
		var fs = CreateFileSystem();
		var filePath = "/credentials.json";
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(filePath, asEmail: true, fileSystem: fs);

		storage.Save(CreateCredentials("admin@example.com"));
		storage.Save(CreateCredentials("user@example.com"));

		var result = storage.Search("*").ToArray();

		result.Length.AssertEqual(2);
	}

	[TestMethod]
	public void Search_EmptyPattern_ReturnsAll()
	{
		var fs = CreateFileSystem();
		var filePath = "/credentials.json";
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(filePath, asEmail: true, fileSystem: fs);

		storage.Save(CreateCredentials("admin@example.com"));
		storage.Save(CreateCredentials("user@example.com"));

		var result = storage.Search("").ToArray();

		result.Length.AssertEqual(2);
	}

	[TestMethod]
	public void Delete_ExistingCredentials_ReturnsTrue()
	{
		var fs = CreateFileSystem();
		var filePath = "/credentials.json";
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(filePath, asEmail: true, fileSystem: fs);

		storage.Save(CreateCredentials("test@example.com"));

		var result = storage.Delete("test@example.com");

		result.AssertTrue();
		storage.Search("test@example.com").ToArray().Length.AssertEqual(0);
	}

	[TestMethod]
	public void Delete_NonExistingCredentials_ReturnsFalse()
	{
		var fs = CreateFileSystem();
		var filePath = "/credentials.json";
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(filePath, fileSystem: fs);

		var result = storage.Delete("nonexistent");

		result.AssertFalse();
	}

	[TestMethod]
	public void Save_UpdateExisting_OverwritesCredentials()
	{
		var fs = CreateFileSystem();
		var filePath = "/credentials.json";
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(filePath, asEmail: true, fileSystem: fs);

		var original = CreateCredentials("test@example.com", "password1");
		storage.Save(original);

		var updated = CreateCredentials("test@example.com", "password2");
		storage.Save(updated);

		var result = storage.Search("test@example.com").ToArray();

		result.Length.AssertEqual(1);
	}

	[TestMethod]
	public void Save_NullCredentials_ThrowsArgumentNullException()
	{
		var fs = CreateFileSystem();
		IPermissionCredentialsStorage storage = new FileCredentialsStorage("/credentials.json", fileSystem: fs);

		ThrowsExactly<ArgumentNullException>(() => storage.Save(null));
	}

	[TestMethod]
	public void Save_InvalidEmail_ThrowsArgumentException()
	{
		var fs = CreateFileSystem();
		IPermissionCredentialsStorage storage = new FileCredentialsStorage("/credentials.json", asEmail: true, fileSystem: fs);

		var credentials = CreateCredentials("invalid-email");

		ThrowsExactly<ArgumentException>(() => storage.Save(credentials));
	}

	[TestMethod]
	public void Save_ValidUsername_WhenAsEmailFalse()
	{
		var fs = CreateFileSystem();
		IPermissionCredentialsStorage storage = new FileCredentialsStorage("/credentials.json", asEmail: false, fileSystem: fs);

		// Username: letters, numbers, dots, hyphens, underscores; 3-64 chars
		var credentials = CreateCredentials("admin_user.123");

		storage.Save(credentials);

		var result = storage.Search("admin_user.123").ToArray();
		result.Length.AssertEqual(1);
	}

	[TestMethod]
	public void Search_CaseInsensitive()
	{
		var fs = CreateFileSystem();
		var filePath = "/credentials.json";
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(filePath, asEmail: true, fileSystem: fs);

		storage.Save(CreateCredentials("Test@Example.com"));

		var result = storage.Search("test@example.com").ToArray();

		result.Length.AssertEqual(1);
	}

	[TestMethod]
	public void Delete_CaseInsensitive()
	{
		var fs = CreateFileSystem();
		var filePath = "/credentials.json";
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(filePath, asEmail: true, fileSystem: fs);

		storage.Save(CreateCredentials("Test@Example.com"));

		var result = storage.Delete("TEST@EXAMPLE.COM");

		result.AssertTrue();
		storage.Search("*").ToArray().Length.AssertEqual(0);
	}

	[TestMethod]
	public void MultipleInstances_ShareSameFile()
	{
		var fs = CreateFileSystem();
		var filePath = "/credentials.json";

		IPermissionCredentialsStorage storage1 = new FileCredentialsStorage(filePath, asEmail: true, fileSystem: fs);
		storage1.Save(CreateCredentials("user1@example.com"));

		IPermissionCredentialsStorage storage2 = new FileCredentialsStorage(filePath, asEmail: true, fileSystem: fs);
		storage2.Save(CreateCredentials("user2@example.com"));

		// storage1 won't see storage2's changes without reload, but storage2 should have both
		IPermissionCredentialsStorage storage3 = new FileCredentialsStorage(filePath, asEmail: true, fileSystem: fs);
		var result = storage3.Search("*").ToArray();

		result.Length.AssertEqual(2);
	}
}

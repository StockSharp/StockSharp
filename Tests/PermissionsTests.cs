namespace StockSharp.Tests;

using System.Net;
using System.Security;

using Ecng.Security;

using StockSharp.Configuration.Permissions;

[TestClass]
public class PermissionsTests : BaseTestClass
{
	private static readonly IPAddress _ip1 = IPAddress.Parse("192.168.1.1");
	private static readonly IPAddress _ip2 = IPAddress.Parse("10.0.0.1");
	private static readonly IPAddress _ip3 = IPAddress.Parse("172.16.0.1");

	private static SecureString ToSecureString(string s)
	{
		return s.Secure().ReadOnly();
	}

	#region PermissionCredentials Tests

	[TestMethod]
	public void PermissionCredentials_DefaultConstructor_InitializesCorrectly()
	{
		var creds = new PermissionCredentials();

		creds.Email.AssertNull();
		creds.Password.AssertNull();
		creds.IpRestrictions.AssertNotNull();
		creds.IpRestrictions.Count().AssertEqual(0);
		creds.Permissions.AssertNotNull();
		creds.Permissions.Count.AssertEqual(0);
	}

	[TestMethod]
	public void PermissionCredentials_IpRestrictions_CanSetAndGet()
	{
		var creds = new PermissionCredentials
		{
			IpRestrictions = [_ip1, _ip2]
		};

		creds.IpRestrictions.Count().AssertEqual(2);
		creds.IpRestrictions.Contains(_ip1).AssertTrue();
		creds.IpRestrictions.Contains(_ip2).AssertTrue();
	}

	[TestMethod]
	public void PermissionCredentials_IpRestrictions_NullThrows()
	{
		var creds = new PermissionCredentials();
		ThrowsExactly<ArgumentNullException>(() => creds.IpRestrictions = null);
	}

	[TestMethod]
	public void PermissionCredentials_Permissions_CanAdd()
	{
		var creds = new PermissionCredentials();

		var dict = new SynchronizedDictionary<(string name, string param, string extra, DateTime? till), bool>
		{
			[("trading", "AAPL", "", null)] = true,
			[("trading", "MSFT", "", DateTime.UtcNow.AddDays(30))] = false
		};

		creds.Permissions[UserPermissions.Trading] = dict;

		creds.Permissions.Count.AssertEqual(1);
		creds.Permissions.ContainsKey(UserPermissions.Trading).AssertTrue();
		creds.Permissions[UserPermissions.Trading].Count.AssertEqual(2);
	}

	[TestMethod]
	public void PermissionCredentials_SaveLoad_PreservesData()
	{
		var original = new PermissionCredentials
		{
			Email = "test@example.com",
			Password = ToSecureString("password123"),
			IpRestrictions = [_ip1, _ip2]
		};

		var dict = new SynchronizedDictionary<(string, string, string, DateTime?), bool>
		{
			[("trading", "AAPL", "limit", null)] = true,
			[("data", "*", "", DateTime.UtcNow.AddDays(30))] = false
		};
		original.Permissions[UserPermissions.Trading] = dict;

		var storage = new SettingsStorage();
		original.Save(storage);

		var loaded = new PermissionCredentials();
		loaded.Load(storage);

		loaded.Email.AssertEqual(original.Email);
		loaded.Password.IsEqualTo(original.Password).AssertTrue();
		loaded.IpRestrictions.Count().AssertEqual(2);
		loaded.IpRestrictions.Contains(_ip1).AssertTrue();
		loaded.IpRestrictions.Contains(_ip2).AssertTrue();

		loaded.Permissions.Count.AssertEqual(1);
		loaded.Permissions.ContainsKey(UserPermissions.Trading).AssertTrue();
		loaded.Permissions[UserPermissions.Trading].Count.AssertEqual(2);
	}

	[TestMethod]
	public void PermissionCredentials_SaveLoad_EmptyPermissions()
	{
		var original = new PermissionCredentials
		{
			Email = "user",
			Password = ToSecureString("pass"),
			IpRestrictions = []
		};

		var storage = new SettingsStorage();
		original.Save(storage);

		var loaded = new PermissionCredentials();
		loaded.Load(storage);

		loaded.Permissions.Count.AssertEqual(0);
		loaded.IpRestrictions.Count().AssertEqual(0);
	}

	#endregion

	#region PermissionCredentialsExtensions Tests

	[TestMethod]
	public void Extensions_ToCredentials_NullThrows()
	{
		ThrowsExactly<ArgumentNullException>(() => ((UserInfoMessage)null).ToCredentials());
	}

	[TestMethod]
	public void Extensions_ToCredentials_ConvertsCorrectly()
	{
		var message = new UserInfoMessage
		{
			Login = "user",
			Password = ToSecureString("password"),
			IpRestrictions = [_ip1, _ip2]
		};

		message.Permissions[UserPermissions.Trading] = new Dictionary<(string, string, string, DateTime?), bool>
		{
			[("stock", "AAPL", "", null)] = true
		};

		var creds = message.ToCredentials();

		creds.Email.AssertEqual(message.Login);
		creds.Password.IsEqualTo(message.Password).AssertTrue();
		creds.IpRestrictions.Count().AssertEqual(2);
		creds.IpRestrictions.Contains(_ip1).AssertTrue();
		creds.Permissions.Count.AssertEqual(1);
		creds.Permissions[UserPermissions.Trading].Count.AssertEqual(1);
	}

	[TestMethod]
	public void Extensions_ToUserInfoMessage_NullThrows()
	{
		ThrowsExactly<ArgumentNullException>(() => ((PermissionCredentials)null).ToUserInfoMessage(true));
	}

	[TestMethod]
	public void Extensions_ToUserInfoMessage_CopyPassword_True()
	{
		var creds = new PermissionCredentials
		{
			Email = "test@example.com",
			Password = ToSecureString("secret"),
			IpRestrictions = [_ip1]
		};

		var dict = new SynchronizedDictionary<(string, string, string, DateTime?), bool>
		{
			[("market", "NYSE", "", null)] = true
		};
		creds.Permissions[UserPermissions.Load] = dict;

		var message = creds.ToUserInfoMessage(copyPassword: true);

		message.Login.AssertEqual(creds.Email);
		message.Password.IsEqualTo(creds.Password).AssertTrue();
		message.IpRestrictions.Count().AssertEqual(1);
		message.IpRestrictions.Contains(_ip1).AssertTrue();
		message.Permissions.Count.AssertEqual(1);
	}

	[TestMethod]
	public void Extensions_ToUserInfoMessage_CopyPassword_False()
	{
		var creds = new PermissionCredentials
		{
			Email = "test@example.com",
			Password = ToSecureString("secret"),
			IpRestrictions = [_ip2]
		};

		var dict = new SynchronizedDictionary<(string, string, string, DateTime?), bool>
		{
			[("data", "*", "", null)] = true
		};
		creds.Permissions[UserPermissions.Load] = dict;

		var message = creds.ToUserInfoMessage(copyPassword: false);

		message.Login.AssertEqual(creds.Email);
		message.Password.AssertNull();
		message.IpRestrictions.Count().AssertEqual(1);
		message.IpRestrictions.Contains(_ip2).AssertTrue();
		message.Permissions.Count.AssertEqual(1);
	}

	[TestMethod]
	public void Extensions_RoundTrip_Creds_ToMessage_ToCreds_PreservesContract()
	{
		var creds = new PermissionCredentials
		{
			Email = "round",
			Password = ToSecureString("pwd"),
			IpRestrictions = [_ip1, _ip2]
		};

		var dict = new SynchronizedDictionary<(string, string, string, DateTime?), bool>
		{
			[("trading", "AAPL", "", null)] = true,
			[("trading", "MSFT", "", null)] = false,
		};
		creds.Permissions[UserPermissions.Trading] = dict;

		var msg = creds.ToUserInfoMessage(copyPassword: true);
		var creds2 = msg.ToCredentials();

		creds2.Email.AssertEqual(creds.Email);
		creds2.Password.IsEqualTo(creds.Password).AssertTrue();
		creds2.IpRestrictions.Count().AssertEqual(2);
		creds2.IpRestrictions.Contains(_ip1).AssertTrue();
		creds2.IpRestrictions.Contains(_ip2).AssertTrue();
		creds2.Permissions.Count.AssertEqual(1);
		creds2.Permissions[UserPermissions.Trading].Count.AssertEqual(2);
	}

	[TestMethod]
	public void Extensions_TryGetByLogin_NullStorageThrows()
	{
		ThrowsExactly<ArgumentNullException>(() => ((IPermissionCredentialsStorage)null).TryGetByLogin("test"));
	}

	[TestMethod]
	public void Extensions_TryGetByLogin_EmptyLoginThrows()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);
		ThrowsExactly<ArgumentNullException>(() => storage.TryGetByLogin(""));
	}

	[TestMethod]
	public void Extensions_TryGetByLogin_ExactMatch()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		storage.Save(new PermissionCredentials { Email = "user", Password = ToSecureString("pass") });
		storage.Save(new PermissionCredentials { Email = "user123", Password = ToSecureString("pass") });

		var result = storage.TryGetByLogin("user");

		result.AssertNotNull();
		result.Email.AssertEqual("user");
	}

	[TestMethod]
	public void Extensions_TryGetByLogin_NotFound()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		storage.Save(new PermissionCredentials { Email = "user", Password = ToSecureString("pass") });

		var result = storage.TryGetByLogin("nonexistent");

		result.AssertNull();
	}

	[TestMethod]
	public void Extensions_TryGetByLogin_EscapesWildcards()
	{
		// Check that '*' is not treated as wildcard in exact match
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		storage.Save(new PermissionCredentials { Email = "user-star", Password = ToSecureString("pass1") });
		storage.Save(new PermissionCredentials { Email = "user123", Password = ToSecureString("pass2") });

		var result = storage.TryGetByLogin("user-star");

		result.AssertNotNull();
		result.Email.AssertEqual("user-star");
	}

	[TestMethod]
	public void Extensions_TryGetByLogin_CaseInsensitive()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		storage.Save(new PermissionCredentials { Email = "User", Password = ToSecureString("pass") });

		var result = storage.TryGetByLogin("user");

		result.AssertNotNull();
		result.Email.AssertEqual("User");
	}

	#endregion

	#region FileCredentialsStorage Tests

	[TestMethod]
	public void FileStorage_Constructor_NullFileNameThrows()
	{
		var fs = Helper.MemorySystem;
		ThrowsExactly<ArgumentNullException>(() => new FileCredentialsStorage(fs, null));
	}

	[TestMethod]
	public void FileStorage_Constructor_EmptyFileNameThrows()
	{
		var fs = Helper.MemorySystem;
		ThrowsExactly<ArgumentNullException>(() => new FileCredentialsStorage(fs, ""));
	}

	[TestMethod]
	public void FileStorage_Constructor_NonExistentDirectory()
	{
		var fs = Helper.MemorySystem;
		var invalidPath = Path.Combine(fs.GetSubTemp(), "file.txt");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, invalidPath);

		var results = storage.Search("*").ToArray();

		results.Length.AssertEqual(0);
	}

	[TestMethod]
	public void FileStorage_Search_AllPattern_ReturnsAll()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		storage.Save(new PermissionCredentials { Email = "alice", Password = ToSecureString("pass1") });
		storage.Save(new PermissionCredentials { Email = "bob", Password = ToSecureString("pass2") });

		var results = storage.Search("*").ToArray();

		results.Length.AssertEqual(2);
	}

	[TestMethod]
	public void FileStorage_Search_EmptyPattern_ReturnsAll()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		storage.Save(new PermissionCredentials { Email = "test1", Password = ToSecureString("pass") });
		storage.Save(new PermissionCredentials { Email = "test2", Password = ToSecureString("pass") });

		var results = storage.Search("").ToArray();

		results.Length.AssertEqual(2);
	}

	[TestMethod]
	public void FileStorage_Search_WildcardPattern_MatchesCorrectly()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		storage.Save(new PermissionCredentials { Email = "admin", Password = ToSecureString("pass") });
		storage.Save(new PermissionCredentials { Email = "user", Password = ToSecureString("pass") });
		storage.Save(new PermissionCredentials { Email = "Admin", Password = ToSecureString("pass") });

		var results = storage.Search("admin*").ToArray();

		results.Length.AssertEqual(1);
		results.All(r => r.Email.StartsWithIgnoreCase("admin")).AssertTrue();
	}

	[TestMethod]
	public void FileStorage_Search_ExactPattern_MatchesCaseInsensitive()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		storage.Save(new PermissionCredentials { Email = "User", Password = ToSecureString("pass") });

		var results = storage.Search("user").ToArray();

		results.Length.AssertEqual(1);
		results[0].Email.AssertEqual("User");
	}

	[TestMethod]
	public void FileStorage_Search_ReturnsClones()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		var original = new PermissionCredentials { Email = "test", Password = ToSecureString("pass") };
		storage.Save(original);

		var result = storage.Search("test").First();

		result.AssertNotSame(original);
	}

	[TestMethod]
	public void FileStorage_Save_NullThrows()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);
		ThrowsExactly<ArgumentNullException>(() => storage.Save(null));
	}

	[TestMethod]
	public void FileStorage_Save_AddsNewCredentials()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		var creds = new PermissionCredentials { Email = "new", Password = ToSecureString("pass") };
		storage.Save(creds);

		var result = storage.TryGetByLogin("new");
		result.AssertNotNull();
	}

	[TestMethod]
	public void FileStorage_Save_UpdatesExistingCredentials()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		storage.Save(new PermissionCredentials { Email = "user", Password = ToSecureString("oldpass") });

		var updated = new PermissionCredentials
		{
			Email = "user",
			Password = ToSecureString("newpass"),
			IpRestrictions = [_ip1]
		};
		storage.Save(updated);

		var result = storage.TryGetByLogin("user");
		result.AssertNotNull();
		result.Password.IsEqualTo(ToSecureString("newpass")).AssertTrue();
		result.IpRestrictions.Count().AssertEqual(1);
	}

	[TestMethod]
	public void FileStorage_Save_InvalidLoginWithAsterisk_Throws()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		ThrowsExactly<ArgumentException>(() => storage.Save(new PermissionCredentials { Email = "user*", Password = ToSecureString("pass") }));
	}

	[TestMethod]
	public void FileStorage_Save_Null_Throws()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		ThrowsExactly<ArgumentException>(() => storage.Save(new PermissionCredentials { Email = null, Password = ToSecureString("pass") }));
	}

	[TestMethod]
	public void FileStorage_Save_Whitespace_Throws()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		ThrowsExactly<ArgumentException>(() => storage.Save(new PermissionCredentials { Email = " ", Password = ToSecureString("pass") }));
	}

	[TestMethod]
	public void FileStorage_Save_WithInnerSpaces_Throws()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		ThrowsExactly<ArgumentException>(() => storage.Save(new PermissionCredentials { Email = "user test@example.com", Password = ToSecureString("pass") }));
	}

	[TestMethod]
	public void FileStorage_Save_InvalidPlus()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		ThrowsExactly<ArgumentException>(() => storage.Save(new PermissionCredentials { Email = "user+tag", Password = ToSecureString("pass") }));
	}

	[TestMethod]
	public void FileStorage_Delete_RemovesCredentials()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		storage.Save(new PermissionCredentials { Email = "delete", Password = ToSecureString("pass") });

		var deleted = storage.Delete("delete");

		deleted.AssertTrue();
		storage.TryGetByLogin("delete").AssertNull();
	}

	[TestMethod]
	public void FileStorage_Delete_NonExistent_ReturnsFalse()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		var deleted = storage.Delete("nonexistent");

		deleted.AssertFalse();
	}

	[TestMethod]
	public void FileStorage_PersistenceAcrossInstances()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		var login = "persistent";

		// First instance: save data
		{
			IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);
			storage.Save(new PermissionCredentials
			{
				Email = login,
				Password = ToSecureString("password"),
				IpRestrictions = [_ip1, _ip2]
			});
		}

		// Second instance: load data
		{
			IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);
			var result = storage.TryGetByLogin(login);

			result.AssertNotNull();
			result.Email.AssertEqual(login);
			result.IpRestrictions.Count().AssertEqual(2);
		}
	}

	#endregion

	#region PermissionCredentialsAuthorization Tests

	[TestMethod]
	public void Authorization_Constructor_NullStorageThrows()
	{
		ThrowsExactly<ArgumentNullException>(() => new PermissionCredentialsAuthorization(null));
	}

	[TestMethod]
	public Task Authorization_ValidateCredentials_EmptyLoginThrows()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);
		IAuthorization auth = new PermissionCredentialsAuthorization(storage);

		return ThrowsExactlyAsync<ArgumentNullException>(() =>
			auth.ValidateCredentials("", ToSecureString("pass"), _ip1, CancellationToken).AsTask());
	}

	[TestMethod]
	public Task Authorization_ValidateCredentials_EmptyPasswordThrows()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);
		IAuthorization auth = new PermissionCredentialsAuthorization(storage);

		return ThrowsExactlyAsync<ArgumentNullException>(() =>
			auth.ValidateCredentials("user", new SecureString(), _ip1, CancellationToken).AsTask());
	}

	[TestMethod]
	public Task Authorization_ValidateCredentials_NullPasswordThrows()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);
		IAuthorization auth = new PermissionCredentialsAuthorization(storage);

		return ThrowsExactlyAsync<ArgumentNullException>(() =>
			auth.ValidateCredentials("user", null, _ip1, CancellationToken).AsTask());
	}

	[TestMethod]
	public Task Authorization_ValidateCredentials_NonExistentUser_Throws()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);
		IAuthorization auth = new PermissionCredentialsAuthorization(storage);

		return ThrowsExactlyAsync<UnauthorizedAccessException>(() =>
			auth.ValidateCredentials("nonexistent", ToSecureString("pass"), _ip1, CancellationToken).AsTask());
	}

	[TestMethod]
	public Task Authorization_ValidateCredentials_WrongPassword_Throws()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		var login = "user1";

		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);
		storage.Save(new PermissionCredentials
		{
			Email = login,
			Password = ToSecureString("correctpass")
		});

		IAuthorization auth = new PermissionCredentialsAuthorization(storage);

		return ThrowsExactlyAsync<UnauthorizedAccessException>(() =>
			auth.ValidateCredentials(login, ToSecureString("wrongpass"), _ip1, CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task Authorization_ValidateCredentials_Success_NoIpRestrictions()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		var login = "user1";
		var password = ToSecureString("password");

		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);
		storage.Save(new PermissionCredentials
		{
			Email = login,
			Password = password,
			IpRestrictions = []
		});

		IAuthorization auth = new PermissionCredentialsAuthorization(storage);

		var sessionId = await auth.ValidateCredentials(login, password, _ip1, CancellationToken);

		sessionId.AssertNotNull();
		(sessionId.Length > 0).AssertTrue();
	}

	[TestMethod]
	public async Task Authorization_ValidateCredentials_Success_WithIpRestrictions()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		var login = "user1";
		var password = ToSecureString("password");

		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);
		storage.Save(new PermissionCredentials
		{
			Email = login,
			Password = password,
			IpRestrictions = [_ip1, _ip2]
		});

		IAuthorization auth = new PermissionCredentialsAuthorization(storage);

		var sessionId = await auth.ValidateCredentials(login, password, _ip1, CancellationToken);

		sessionId.AssertNotNull();
		(sessionId.Length > 0).AssertTrue();
	}

	[TestMethod]
	public Task Authorization_ValidateCredentials_IpRestriction_Blocked()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		var login = "user1";
		var password = ToSecureString("password");

		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);
		storage.Save(new PermissionCredentials
		{
			Email = login,
			Password = password,
			IpRestrictions = [_ip1, _ip2]  // only these IPs allowed
		});

		IAuthorization auth = new PermissionCredentialsAuthorization(storage);

		return ThrowsExactlyAsync<UnauthorizedAccessException>(() =>
			auth.ValidateCredentials(login, password, _ip3, CancellationToken).AsTask());  // different IP
	}

	[TestMethod]
	public Task Authorization_ValidateCredentials_IpRestriction_NullClientAddress_Blocked()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		var login = "user1";
		var password = ToSecureString("password");

		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);
		storage.Save(new PermissionCredentials
		{
			Email = login,
			Password = password,
			IpRestrictions = [_ip1]
		});

		IAuthorization auth = new PermissionCredentialsAuthorization(storage);

		return ThrowsExactlyAsync<UnauthorizedAccessException>(() =>
			auth.ValidateCredentials(login, password, null, CancellationToken).AsTask());
	}

	#endregion
}

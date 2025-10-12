namespace StockSharp.Tests;

using System.Net;
using System.Security;

using Ecng.Security;

using StockSharp.Configuration.Permissions;

[TestClass]
public class PermissionsTests
{
	private static readonly IPAddress _ip1 = IPAddress.Parse("192.168.1.1");
	private static readonly IPAddress _ip2 = IPAddress.Parse("10.0.0.1");
	private static readonly IPAddress _ip3 = IPAddress.Parse("172.16.0.1");

	private static SecureString ToSecureString(string s)
	{
		var secureString = s.Secure();
		secureString.MakeReadOnly();
		return secureString;
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
		Assert.ThrowsExactly<ArgumentNullException>(() => creds.IpRestrictions = null);
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
		Assert.ThrowsExactly<ArgumentNullException>(() => ((UserInfoMessage)null).ToCredentials());
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
		Assert.ThrowsExactly<ArgumentNullException>(() => ((PermissionCredentials)null).ToUserInfoMessage(true));
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
		Assert.ThrowsExactly<ArgumentNullException>(() => ((IPermissionCredentialsStorage)null).TryGetByLogin("test"));
	}

	[TestMethod]
	public void Extensions_TryGetByLogin_EmptyLoginThrows()
	{
		var tempFile = Helper.GetSubTemp($"creds-{Guid.NewGuid():N}.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);
		Assert.ThrowsExactly<ArgumentNullException>(() => storage.TryGetByLogin(""));
	}

	[TestMethod]
	public void Extensions_TryGetByLogin_ExactMatch()
	{
		var tempFile = Helper.GetSubTemp($"creds-{Guid.NewGuid():N}.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);

		storage.Save(new PermissionCredentials { Email = "user", Password = ToSecureString("pass") });
		storage.Save(new PermissionCredentials { Email = "user123", Password = ToSecureString("pass") });

		var result = storage.TryGetByLogin("user");

		result.AssertNotNull();
		result.Email.AssertEqual("user");
	}

	[TestMethod]
	public void Extensions_TryGetByLogin_NotFound()
	{
		var tempFile = Helper.GetSubTemp($"creds-{Guid.NewGuid():N}.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);

		storage.Save(new PermissionCredentials { Email = "user", Password = ToSecureString("pass") });

		var result = storage.TryGetByLogin("nonexistent");

		result.AssertNull();
	}

	[TestMethod]
	public void Extensions_TryGetByLogin_EscapesWildcards()
	{
		// Check that '*' is not treated as wildcard in exact match
		var tempFile = Helper.GetSubTemp($"creds-{Guid.NewGuid():N}.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);

		storage.Save(new PermissionCredentials { Email = "user-star", Password = ToSecureString("pass1") });
		storage.Save(new PermissionCredentials { Email = "user123", Password = ToSecureString("pass2") });

		var result = storage.TryGetByLogin("user-star");

		result.AssertNotNull();
		result.Email.AssertEqual("user-star");
	}

	[TestMethod]
	public void Extensions_TryGetByLogin_CaseInsensitive()
	{
		var tempFile = Helper.GetSubTemp($"creds-{Guid.NewGuid():N}.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);

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
		Assert.ThrowsExactly<ArgumentNullException>(() => new FileCredentialsStorage(null));
	}

	[TestMethod]
	public void FileStorage_Constructor_EmptyFileNameThrows()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => new FileCredentialsStorage(""));
	}

	[TestMethod]
	public void FileStorage_Constructor_NonExistentDirectoryThrows()
	{
		var invalidPath = Path.Combine(Helper.TempFolder, Guid.NewGuid().ToString(), "file.txt");
		Assert.ThrowsExactly<InvalidOperationException>(() => new FileCredentialsStorage(invalidPath));
	}

	[TestMethod]
	public void FileStorage_Search_AllPattern_ReturnsAll()
	{
		var tempFile = Helper.GetSubTemp($"creds-{Guid.NewGuid():N}.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);

		storage.Save(new PermissionCredentials { Email = "alice", Password = ToSecureString("pass1") });
		storage.Save(new PermissionCredentials { Email = "bob", Password = ToSecureString("pass2") });

		var results = storage.Search("*").ToArray();

		results.Length.AssertEqual(2);
	}

	[TestMethod]
	public void FileStorage_Search_EmptyPattern_ReturnsAll()
	{
		var tempFile = Helper.GetSubTemp($"creds-{Guid.NewGuid():N}.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);

		storage.Save(new PermissionCredentials { Email = "test1", Password = ToSecureString("pass") });
		storage.Save(new PermissionCredentials { Email = "test2", Password = ToSecureString("pass") });

		var results = storage.Search("").ToArray();

		results.Length.AssertEqual(2);
	}

	[TestMethod]
	public void FileStorage_Search_WildcardPattern_MatchesCorrectly()
	{
		var tempFile = Helper.GetSubTemp($"creds-{Guid.NewGuid():N}.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);

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
		var tempFile = Helper.GetSubTemp($"creds-{Guid.NewGuid():N}.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);

		storage.Save(new PermissionCredentials { Email = "User", Password = ToSecureString("pass") });

		var results = storage.Search("user").ToArray();

		results.Length.AssertEqual(1);
		results[0].Email.AssertEqual("User");
	}

	[TestMethod]
	public void FileStorage_Search_ReturnsClones()
	{
		var tempFile = Helper.GetSubTemp($"creds-{Guid.NewGuid():N}.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);

		var original = new PermissionCredentials { Email = "test", Password = ToSecureString("pass") };
		storage.Save(original);

		var result = storage.Search("test").First();

		result.AssertNotSame(original);
	}

	[TestMethod]
	public void FileStorage_Save_NullThrows()
	{
		var tempFile = Helper.GetSubTemp($"creds-{Guid.NewGuid():N}.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);
		Assert.ThrowsExactly<ArgumentNullException>(() => storage.Save(null));
	}

	[TestMethod]
	public void FileStorage_Save_AddsNewCredentials()
	{
		var tempFile = Helper.GetSubTemp($"creds-{Guid.NewGuid():N}.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);

		var creds = new PermissionCredentials { Email = "new", Password = ToSecureString("pass") };
		storage.Save(creds);

		var result = storage.TryGetByLogin("new");
		result.AssertNotNull();
	}

	[TestMethod]
	public void FileStorage_Save_UpdatesExistingCredentials()
	{
		var tempFile = Helper.GetSubTemp($"creds-{Guid.NewGuid():N}.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);

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
		var tempFile = Helper.GetSubTemp($"creds-{Guid.NewGuid():N}.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);

		Assert.ThrowsExactly<ArgumentException>(() => storage.Save(new PermissionCredentials { Email = "user*", Password = ToSecureString("pass") }));
	}

	[TestMethod]
	public void FileStorage_Save_Null_Throws()
	{
		var tempFile = Helper.GetSubTemp($"creds-{Guid.NewGuid():N}.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);

		Assert.ThrowsExactly<ArgumentException>(() => storage.Save(new PermissionCredentials { Email = null, Password = ToSecureString("pass") }));
	}

	[TestMethod]
	public void FileStorage_Save_Whitespace_Throws()
	{
		var tempFile = Helper.GetSubTemp($"creds-{Guid.NewGuid():N}.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);

		Assert.ThrowsExactly<ArgumentException>(() => storage.Save(new PermissionCredentials { Email = " ", Password = ToSecureString("pass") }));
	}

	[TestMethod]
	public void FileStorage_Save_WithInnerSpaces_Throws()
	{
		var tempFile = Helper.GetSubTemp($"creds-{Guid.NewGuid():N}.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);

		Assert.ThrowsExactly<ArgumentException>(() => storage.Save(new PermissionCredentials { Email = "user test@example.com", Password = ToSecureString("pass") }));
	}

	[TestMethod]
	public void FileStorage_Save_InvalidPlus()
	{
		var tempFile = Helper.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);

		Assert.ThrowsExactly<ArgumentException>(() => storage.Save(new PermissionCredentials { Email = "user+tag", Password = ToSecureString("pass") }));
	}

	[TestMethod]
	public void FileStorage_Delete_RemovesCredentials()
	{
		var tempFile = Helper.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);

		storage.Save(new PermissionCredentials { Email = "delete", Password = ToSecureString("pass") });

		var deleted = storage.Delete("delete");

		deleted.AssertTrue();
		storage.TryGetByLogin("delete").AssertNull();
	}

	[TestMethod]
	public void FileStorage_Delete_NonExistent_ReturnsFalse()
	{
		var tempFile = Helper.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);

		var deleted = storage.Delete("nonexistent");

		deleted.AssertFalse();
	}

	[TestMethod]
	public void FileStorage_PersistenceAcrossInstances()
	{
		var tempFile = Helper.GetSubTemp("creds.json");
		var login = "persistent";

		// First instance: save data
		{
			IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);
			storage.Save(new PermissionCredentials
			{
				Email = login,
				Password = ToSecureString("password"),
				IpRestrictions = [_ip1, _ip2]
			});
		}

		// Second instance: load data
		{
			IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);
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
		Assert.ThrowsExactly<ArgumentNullException>(() => new PermissionCredentialsAuthorization(null));
	}

	[TestMethod]
	public async Task Authorization_ValidateCredentials_EmptyLoginThrows()
	{
		var tempFile = Helper.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);
		IAuthorization auth = new PermissionCredentialsAuthorization(storage);

		await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
			await auth.ValidateCredentials("", ToSecureString("pass"), _ip1, default));
	}

	[TestMethod]
	public async Task Authorization_ValidateCredentials_EmptyPasswordThrows()
	{
		var tempFile = Helper.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);
		IAuthorization auth = new PermissionCredentialsAuthorization(storage);

		await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
			await auth.ValidateCredentials("user", new SecureString(), _ip1, default));
	}

	[TestMethod]
	public async Task Authorization_ValidateCredentials_NullPasswordThrows()
	{
		var tempFile = Helper.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);
		IAuthorization auth = new PermissionCredentialsAuthorization(storage);

		await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
			await auth.ValidateCredentials("user", null, _ip1, default));
	}

	[TestMethod]
	public async Task Authorization_ValidateCredentials_NonExistentUser_Throws()
	{
		var tempFile = Helper.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);
		IAuthorization auth = new PermissionCredentialsAuthorization(storage);

		await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(async () =>
			await auth.ValidateCredentials("nonexistent", ToSecureString("pass"), _ip1, default));
	}

	[TestMethod]
	public async Task Authorization_ValidateCredentials_WrongPassword_Throws()
	{
		var tempFile = Helper.GetSubTemp("creds.json");
		var login = "user1";

		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);
		storage.Save(new PermissionCredentials
		{
			Email = login,
			Password = ToSecureString("correctpass")
		});

		IAuthorization auth = new PermissionCredentialsAuthorization(storage);

		await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(async () =>
			await auth.ValidateCredentials(login, ToSecureString("wrongpass"), _ip1, default));
	}

	[TestMethod]
	public async Task Authorization_ValidateCredentials_Success_NoIpRestrictions()
	{
		var tempFile = Helper.GetSubTemp("creds.json");
		var login = "user1";
		var password = ToSecureString("password");

		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);
		storage.Save(new PermissionCredentials
		{
			Email = login,
			Password = password,
			IpRestrictions = []
		});

		IAuthorization auth = new PermissionCredentialsAuthorization(storage);

		var sessionId = await auth.ValidateCredentials(login, password, _ip1, default);

		sessionId.AssertNotNull();
		Assert.IsTrue(sessionId.Length > 0);
	}

	[TestMethod]
	public async Task Authorization_ValidateCredentials_Success_WithIpRestrictions()
	{
		var tempFile = Helper.GetSubTemp("creds.json");
		var login = "user1";
		var password = ToSecureString("password");

		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);
		storage.Save(new PermissionCredentials
		{
			Email = login,
			Password = password,
			IpRestrictions = [_ip1, _ip2]
		});

		IAuthorization auth = new PermissionCredentialsAuthorization(storage);

		var sessionId = await auth.ValidateCredentials(login, password, _ip1, default);

		sessionId.AssertNotNull();
		Assert.IsTrue(sessionId.Length > 0);
	}

	[TestMethod]
	public async Task Authorization_ValidateCredentials_IpRestriction_Blocked()
	{
		var tempFile = Helper.GetSubTemp("creds.json");
		var login = "user1";
		var password = ToSecureString("password");

		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);
		storage.Save(new PermissionCredentials
		{
			Email = login,
			Password = password,
			IpRestrictions = [_ip1, _ip2]  // only these IPs allowed
		});

		IAuthorization auth = new PermissionCredentialsAuthorization(storage);

		await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(async () =>
			await auth.ValidateCredentials(login, password, _ip3, default));  // different IP
	}

	[TestMethod]
	public async Task Authorization_ValidateCredentials_IpRestriction_NullClientAddress_Blocked()
	{
		var tempFile = Helper.GetSubTemp("creds.json");
		var login = "user1";
		var password = ToSecureString("password");

		IPermissionCredentialsStorage storage = new FileCredentialsStorage(tempFile);
		storage.Save(new PermissionCredentials
		{
			Email = login,
			Password = password,
			IpRestrictions = [_ip1]
		});

		IAuthorization auth = new PermissionCredentialsAuthorization(storage);

		await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(async () =>
			await auth.ValidateCredentials(login, password, null, default));
	}

	#endregion
}

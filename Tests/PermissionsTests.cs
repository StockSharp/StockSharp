namespace StockSharp.Tests;

using System.Net;
using System.Security;

using Ecng.Security;

using StockSharp.Configuration.Permissions;

[TestClass]
public class PermissionsTests : BaseTestClass
{
	private static readonly IPAddress _ip1 = "192.168.1.1".To<IPAddress>();
	private static readonly IPAddress _ip2 = "10.0.0.1".To<IPAddress>();
	private static readonly IPAddress _ip3 = "172.16.0.1".To<IPAddress>();

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
		creds.IpRestrictions.Count(ip => ip.Equals(_ip1)).AssertEqual(1);
		creds.IpRestrictions.Count(ip => ip.Equals(_ip2)).AssertEqual(1);
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
		creds.Permissions.Count(p => p.Key == UserPermissions.Trading).AssertEqual(1);
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

		// Use a fixed, UTC, whole-second Till so the round-trip comparison is deterministic
		// and not affected by any sub-second handling.
		var till = new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc);

		var enabledKey = ("trading", "AAPL", "limit", (DateTime?)null);
		var disabledKey = ("data", "*", "", (DateTime?)till);

		var dict = new SynchronizedDictionary<(string, string, string, DateTime?), bool>
		{
			[enabledKey] = true,
			[disabledKey] = false
		};
		original.Permissions[UserPermissions.Trading] = dict;

		var storage = new SettingsStorage();
		original.Save(storage);

		var loaded = new PermissionCredentials();
		loaded.Load(storage);

		loaded.Email.AssertEqual(original.Email);
		loaded.Password.IsEqualTo(original.Password).AssertTrue();
		loaded.IpRestrictions.Count().AssertEqual(2);
		loaded.IpRestrictions.Count(ip => ip.Equals(_ip1)).AssertEqual(1);
		loaded.IpRestrictions.Count(ip => ip.Equals(_ip2)).AssertEqual(1);

		loaded.Permissions.Count.AssertEqual(1);
		loaded.Permissions.Count(p => p.Key == UserPermissions.Trading).AssertEqual(1);

		var loadedSettings = loaded.Permissions[UserPermissions.Trading];
		loadedSettings.Count.AssertEqual(2);

		// Verify the actual entries survive the round-trip: the tuple keys (including
		// the nullable Till) AND the IsEnabled bool values. The original assertion only
		// checked Count, so corruption of the bool flag or Till would have gone unnoticed.
		loadedSettings.ContainsKey(enabledKey).AssertTrue();
		loadedSettings.ContainsKey(disabledKey).AssertTrue();
		loadedSettings[enabledKey].AssertTrue();
		loadedSettings[disabledKey].AssertFalse();
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

		var key = ("stock", "AAPL", "", (DateTime?)null);

		message.Permissions[UserPermissions.Trading] = new Dictionary<(string, string, string, DateTime?), bool>
		{
			[key] = true
		};

		var creds = message.ToCredentials();

		creds.Email.AssertEqual(message.Login);
		creds.Password.IsEqualTo(message.Password).AssertTrue();
		creds.IpRestrictions.Count().AssertEqual(2);
		creds.IpRestrictions.Count(ip => ip.Equals(_ip1)).AssertEqual(1);
		creds.Permissions.Count.AssertEqual(1);

		// Verify the actual permission entry (key + bool), not just the count, so a
		// corrupted key or flipped flag during conversion would be caught.
		var converted = creds.Permissions[UserPermissions.Trading];
		converted.Count.AssertEqual(1);
		converted.ContainsKey(key).AssertTrue();
		converted[key].AssertTrue();
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
		message.IpRestrictions.Count(ip => ip.Equals(_ip1)).AssertEqual(1);
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
		message.IpRestrictions.Count(ip => ip.Equals(_ip2)).AssertEqual(1);
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

		var enabledKey = ("trading", "AAPL", "", (DateTime?)null);
		var disabledKey = ("trading", "MSFT", "", (DateTime?)null);

		var dict = new SynchronizedDictionary<(string, string, string, DateTime?), bool>
		{
			[enabledKey] = true,
			[disabledKey] = false,
		};
		creds.Permissions[UserPermissions.Trading] = dict;

		var msg = creds.ToUserInfoMessage(copyPassword: true);
		var creds2 = msg.ToCredentials();

		creds2.Email.AssertEqual(creds.Email);
		creds2.Password.IsEqualTo(creds.Password).AssertTrue();
		creds2.IpRestrictions.Count().AssertEqual(2);
		creds2.IpRestrictions.Count(ip => ip.Equals(_ip1)).AssertEqual(1);
		creds2.IpRestrictions.Count(ip => ip.Equals(_ip2)).AssertEqual(1);
		creds2.Permissions.Count.AssertEqual(1);

		// Verify the contract preserves entry content (distinct bool values per key),
		// not merely the entry count. A swapped/dropped flag would otherwise pass.
		var roundTripped = creds2.Permissions[UserPermissions.Trading];
		roundTripped.Count.AssertEqual(2);
		roundTripped.ContainsKey(enabledKey).AssertTrue();
		roundTripped.ContainsKey(disabledKey).AssertTrue();
		roundTripped[enabledKey].AssertTrue();
		roundTripped[disabledKey].AssertFalse();
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public Task Extensions_TryGetByLoginAsync_NullStorageThrows()
	{
		// Return the assertion Task so the negative-contract failure is actually observed.
		return ThrowsExactlyAsync<ArgumentNullException>(() => ((IPermissionCredentialsStorage)null).TryGetByLoginAsync("test", CancellationToken).AsTask());
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public Task Extensions_TryGetByLoginAsync_EmptyLoginThrows()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);
		return ThrowsExactlyAsync<ArgumentNullException>(() => storage.TryGetByLoginAsync("", CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task Extensions_TryGetByLoginAsync_ExactMatch()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		await storage.SaveAsync(new PermissionCredentials { Email = "user", Password = ToSecureString("pass") }, CancellationToken);
		await storage.SaveAsync(new PermissionCredentials { Email = "user123", Password = ToSecureString("pass") }, CancellationToken);

		var result = await storage.TryGetByLoginAsync("user", CancellationToken);

		result.AssertNotNull();
		result.Email.AssertEqual("user");
	}

	[TestMethod]
	public async Task Extensions_TryGetByLoginAsync_NotFound()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		await storage.SaveAsync(new PermissionCredentials { Email = "user", Password = ToSecureString("pass") }, CancellationToken);

		var result = await storage.TryGetByLoginAsync("nonexistent", CancellationToken);

		result.AssertNull();
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task Extensions_TryGetByLoginAsync_EscapesWildcards()
	{
		// A login query containing '*' must NOT be treated as a wildcard pattern:
		// TryGetByLoginAsync escapes '*' so it only matches a stored login that
		// literally equals "user*". Since the stored login is "user123" (no asterisk),
		// the exact-match lookup must return null. If '*' were treated as a wildcard,
		// "user*" would match "user123" and the result would be non-null.
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		await storage.SaveAsync(new PermissionCredentials { Email = "user123", Password = ToSecureString("pass2") }, CancellationToken);

		var result = await storage.TryGetByLoginAsync("user*", CancellationToken);

		result.AssertNull();
	}

	[TestMethod]
	public async Task Extensions_TryGetByLoginAsync_CaseInsensitive()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		await storage.SaveAsync(new PermissionCredentials { Email = "User", Password = ToSecureString("pass") }, CancellationToken);

		var result = await storage.TryGetByLoginAsync("user", CancellationToken);

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
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task FileStorage_Constructor_NonExistentDirectory()
	{
		var fs = Helper.MemorySystem;

		// Point the storage at a file inside a directory that does NOT exist yet.
		var missingDir = Path.Combine(fs.GetSubTemp(), "subdir-does-not-exist");
		var filePath = Path.Combine(missingDir, "file.txt");

		// Precondition: the directory truly does not exist before the storage runs.
		fs.DirectoryExists(missingDir).AssertFalse();

		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, filePath);

		// First access triggers EnsureInitialized, which must create the missing directory
		// and load from a (still absent) file without throwing -> empty result set.
		var results = await storage.SearchAsync("*").ToArrayAsync(CancellationToken);

		results.Length.AssertEqual(0);

		// The engine is contracted to create the missing parent directory.
		fs.DirectoryExists(missingDir).AssertTrue();
	}

	[TestMethod]
	public async Task FileStorage_SearchAsync_AllPattern_ReturnsAll()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		await storage.SaveAsync(new PermissionCredentials { Email = "alice", Password = ToSecureString("pass1") }, CancellationToken);
		await storage.SaveAsync(new PermissionCredentials { Email = "bob", Password = ToSecureString("pass2") }, CancellationToken);

		var results = await storage.SearchAsync("*").ToArrayAsync(CancellationToken);

		results.Length.AssertEqual(2);
	}

	[TestMethod]
	public async Task FileStorage_SearchAsync_EmptyPattern_ReturnsAll()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		await storage.SaveAsync(new PermissionCredentials { Email = "test1", Password = ToSecureString("pass") }, CancellationToken);
		await storage.SaveAsync(new PermissionCredentials { Email = "test2", Password = ToSecureString("pass") }, CancellationToken);

		var results = await storage.SearchAsync("").ToArrayAsync(CancellationToken);

		results.Length.AssertEqual(2);
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task FileStorage_SearchAsync_WildcardPattern_MatchesCorrectly()
	{
		// Pure wildcard-prefix behavior: distinct, non-case-colliding logins.
		// "admin*" must match the two logins starting with "admin" (case-insensitively)
		// and must NOT match the unrelated "user".
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		await storage.SaveAsync(new PermissionCredentials { Email = "admin1", Password = ToSecureString("pass") }, CancellationToken);
		await storage.SaveAsync(new PermissionCredentials { Email = "Admin2", Password = ToSecureString("pass") }, CancellationToken);
		await storage.SaveAsync(new PermissionCredentials { Email = "user", Password = ToSecureString("pass") }, CancellationToken);

		var results = await storage.SearchAsync("admin*").ToArrayAsync(CancellationToken);

		results.Length.AssertEqual(2);
		results.All(r => r.Email.StartsWithIgnoreCase("admin")).AssertTrue();
		results.Any(r => r.Email == "user").AssertFalse();
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task FileStorage_SaveAsync_LoginKeyIsCaseInsensitive()
	{
		// The storage keys credentials with a case-insensitive comparer, so saving
		// "admin" and then "Admin" must collapse to a single entry (last write wins).
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		await storage.SaveAsync(new PermissionCredentials { Email = "admin", Password = ToSecureString("p1"), IpRestrictions = [_ip1] }, CancellationToken);
		await storage.SaveAsync(new PermissionCredentials { Email = "Admin", Password = ToSecureString("p2") }, CancellationToken);

		var all = await storage.SearchAsync("*").ToArrayAsync(CancellationToken);
		all.Length.AssertEqual(1);

		// Last write wins: the stored email and password/IP set come from the second save.
		var found = await storage.TryGetByLoginAsync("ADMIN", CancellationToken);
		found.AssertNotNull();
		found.Email.AssertEqual("Admin");
		found.Password.IsEqualTo(ToSecureString("p2")).AssertTrue();
		found.IpRestrictions.Count().AssertEqual(0);
	}

	[TestMethod]
	public async Task FileStorage_SearchAsync_ExactPattern_MatchesCaseInsensitive()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		await storage.SaveAsync(new PermissionCredentials { Email = "User", Password = ToSecureString("pass") }, CancellationToken);

		var results = await storage.SearchAsync("user").ToArrayAsync(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Email.AssertEqual("User");
	}

	[TestMethod]
	public async Task FileStorage_SearchAsync_ReturnsClones()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		var original = new PermissionCredentials { Email = "test", Password = ToSecureString("pass") };
		await storage.SaveAsync(original, CancellationToken);

		var result = await storage.SearchAsync("test").FirstAsync(CancellationToken);

		result.AssertNotSame(original);
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public Task FileStorage_SaveAsync_NullThrows()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);
		return ThrowsExactlyAsync<ArgumentNullException>(() => storage.SaveAsync(null, CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task FileStorage_SaveAsync_AddsNewCredentials()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		var creds = new PermissionCredentials { Email = "new", Password = ToSecureString("pass") };
		await storage.SaveAsync(creds, CancellationToken);

		var result = await storage.TryGetByLoginAsync("new", CancellationToken);
		result.AssertNotNull();
	}

	[TestMethod]
	public async Task FileStorage_SaveAsync_UpdatesExistingCredentials()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		await storage.SaveAsync(new PermissionCredentials { Email = "user", Password = ToSecureString("oldpass") }, CancellationToken);

		var updated = new PermissionCredentials
		{
			Email = "user",
			Password = ToSecureString("newpass"),
			IpRestrictions = [_ip1]
		};
		await storage.SaveAsync(updated, CancellationToken);

		var result = await storage.TryGetByLoginAsync("user", CancellationToken);
		result.AssertNotNull();
		result.Password.IsEqualTo(ToSecureString("newpass")).AssertTrue();
		result.IpRestrictions.Count().AssertEqual(1);
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public Task FileStorage_SaveAsync_InvalidLoginWithAsterisk_Throws()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		return ThrowsExactlyAsync<ArgumentException>(() => storage.SaveAsync(new PermissionCredentials { Email = "user*", Password = ToSecureString("pass") }, CancellationToken).AsTask());
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public Task FileStorage_SaveAsync_Null_Throws()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		return ThrowsExactlyAsync<ArgumentException>(() => storage.SaveAsync(new PermissionCredentials { Email = null, Password = ToSecureString("pass") }, CancellationToken).AsTask());
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public Task FileStorage_SaveAsync_Whitespace_Throws()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		return ThrowsExactlyAsync<ArgumentException>(() => storage.SaveAsync(new PermissionCredentials { Email = " ", Password = ToSecureString("pass") }, CancellationToken).AsTask());
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public Task FileStorage_SaveAsync_WithInnerSpaces_Throws()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		return ThrowsExactlyAsync<ArgumentException>(() => storage.SaveAsync(new PermissionCredentials { Email = "user test@example.com", Password = ToSecureString("pass") }, CancellationToken).AsTask());
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public Task FileStorage_SaveAsync_InvalidPlus()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		return ThrowsExactlyAsync<ArgumentException>(() => storage.SaveAsync(new PermissionCredentials { Email = "user+tag", Password = ToSecureString("pass") }, CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task FileStorage_DeleteAsync_RemovesCredentials()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		await storage.SaveAsync(new PermissionCredentials { Email = "delete", Password = ToSecureString("pass") }, CancellationToken);

		var deleted = await storage.DeleteAsync("delete", CancellationToken);

		deleted.AssertTrue();
		(await storage.TryGetByLoginAsync("delete", CancellationToken)).AssertNull();
	}

	[TestMethod]
	public async Task FileStorage_DeleteAsync_NonExistent_ReturnsFalse()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);

		var deleted = await storage.DeleteAsync("nonexistent", CancellationToken);

		deleted.AssertFalse();
	}

	[TestMethod]
	public async Task FileStorage_PersistenceAcrossInstances()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		var login = "persistent";

		// First instance: save data
		{
			IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);
			await storage.SaveAsync(new PermissionCredentials
			{
				Email = login,
				Password = ToSecureString("password"),
				IpRestrictions = [_ip1, _ip2]
			}, CancellationToken);
		}

		// Second instance: load data
		{
			IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);
			var result = await storage.TryGetByLoginAsync(login, CancellationToken);

			result.AssertNotNull();
			result.Email.AssertEqual(login);
			result.IpRestrictions.Count().AssertEqual(2);
		}
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task FileStorage_PersistenceAcrossInstances_PreservesPermissions()
	{
		// Exercises the full file (de)serialization round-trip of the permission set,
		// including the nullable Till. The existing persistence test saves no permissions,
		// so this closes that coverage gap.
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		var login = "permholder";

		// Fixed, UTC, whole-second Till for a deterministic comparison.
		var till = new DateTime(2031, 6, 7, 8, 9, 10, DateTimeKind.Utc);

		var enabledKey = ("trading", "AAPL", "limit", (DateTime?)till);
		var disabledKey = ("data", "MSFT", "", (DateTime?)null);

		// First instance: save credentials carrying a permission set.
		{
			var dict = new SynchronizedDictionary<(string, string, string, DateTime?), bool>
			{
				[enabledKey] = true,
				[disabledKey] = false,
			};

			var creds = new PermissionCredentials
			{
				Email = login,
				Password = ToSecureString("password"),
			};
			creds.Permissions[UserPermissions.Trading] = dict;

			IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);
			await storage.SaveAsync(creds, CancellationToken);
		}

		// Second instance: load from the persisted file and verify the permission entries.
		{
			IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);
			var result = await storage.TryGetByLoginAsync(login, CancellationToken);

			result.AssertNotNull();
			result.Permissions.Count.AssertEqual(1);

			var settings = result.Permissions[UserPermissions.Trading];
			settings.Count.AssertEqual(2);

			// Keys (including the nullable Till) and the IsEnabled flags must survive
			// the JSON serialization round-trip.
			settings.ContainsKey(enabledKey).AssertTrue();
			settings.ContainsKey(disabledKey).AssertTrue();
			settings[enabledKey].AssertTrue();
			settings[disabledKey].AssertFalse();
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
	public async Task Authorization_ValidateCredentials_WrongPassword_Throws()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		var login = "user1";

		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);
		await storage.SaveAsync(new PermissionCredentials
		{
			Email = login,
			Password = ToSecureString("correctpass")
		}, CancellationToken);

		IAuthorization auth = new PermissionCredentialsAuthorization(storage);

		await ThrowsExactlyAsync<UnauthorizedAccessException>(() =>
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
		await storage.SaveAsync(new PermissionCredentials
		{
			Email = login,
			Password = password,
			IpRestrictions = []
		}, CancellationToken);

		IAuthorization auth = new PermissionCredentialsAuthorization(storage);

		var sessionId = await auth.ValidateCredentials(login, password, _ip1, CancellationToken);

		// Contract: a successful validation returns a non-empty session id (a fresh Guid).
		// Assert the meaningful contract (non-empty, parseable id) rather than pinning the
		// brittle "D"-format length of 36.
		sessionId.IsEmpty().AssertFalse();
		Guid.TryParse(sessionId, out _).AssertTrue();
	}

	[TestMethod]
	public async Task Authorization_ValidateCredentials_Success_WithIpRestrictions()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		var login = "user1";
		var password = ToSecureString("password");

		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);
		await storage.SaveAsync(new PermissionCredentials
		{
			Email = login,
			Password = password,
			IpRestrictions = [_ip1, _ip2]
		}, CancellationToken);

		IAuthorization auth = new PermissionCredentialsAuthorization(storage);

		var sessionId = await auth.ValidateCredentials(login, password, _ip1, CancellationToken);

		// Contract: a successful validation returns a non-empty, parseable session id.
		sessionId.IsEmpty().AssertFalse();
		Guid.TryParse(sessionId, out _).AssertTrue();
	}

	[TestMethod]
	public async Task Authorization_ValidateCredentials_IpRestriction_Blocked()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		var login = "user1";
		var password = ToSecureString("password");

		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);
		await storage.SaveAsync(new PermissionCredentials
		{
			Email = login,
			Password = password,
			IpRestrictions = [_ip1, _ip2]  // only these IPs allowed
		}, CancellationToken);

		IAuthorization auth = new PermissionCredentialsAuthorization(storage);

		await ThrowsExactlyAsync<UnauthorizedAccessException>(() =>
			auth.ValidateCredentials(login, password, _ip3, CancellationToken).AsTask());  // different IP
	}

	[TestMethod]
	public async Task Authorization_ValidateCredentials_IpRestriction_NullClientAddress_Blocked()
	{
		var fs = Helper.MemorySystem;
		var tempFile = fs.GetSubTemp("creds.json");
		var login = "user1";
		var password = ToSecureString("password");

		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, tempFile);
		await storage.SaveAsync(new PermissionCredentials
		{
			Email = login,
			Password = password,
			IpRestrictions = [_ip1]
		}, CancellationToken);

		IAuthorization auth = new PermissionCredentialsAuthorization(storage);

		await ThrowsExactlyAsync<UnauthorizedAccessException>(() =>
			auth.ValidateCredentials(login, password, null, CancellationToken).AsTask());
	}

	#endregion
}

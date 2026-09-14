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
		result[0].Password.IsEqualTo(updated.Password).AssertTrue();
	}

	[TestMethod]
	public async Task Save_NullCredentials_ThrowsArgumentNullException()
	{
		var fs = CreateFileSystem();
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, "/credentials.json");

		await ThrowsExactlyAsync<ArgumentNullException>(async () => await storage.SaveAsync(null, CancellationToken));
	}

	[TestMethod]
	public async Task Save_InvalidEmail_ThrowsArgumentException()
	{
		var fs = CreateFileSystem();
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, "/credentials.json", asEmail: true);

		var credentials = CreateCredentials("invalid-email");

		await ThrowsExactlyAsync<ArgumentException>(async () => await storage.SaveAsync(credentials, CancellationToken));
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

	[TestMethod]
	public async Task Save_WhileFirstReadIsStillRunning_KeepsBothAccounts()
	{
		var inner = CreateFileSystem();
		var filePath = "/credentials.json";

		IPermissionCredentialsStorage seed = new FileCredentialsStorage(inner, filePath, asEmail: true);
		await seed.SaveAsync(CreateCredentials("user1@example.com"), CancellationToken);

		var fs = new BlockingReadFileSystem(inner);
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, filePath, asEmail: true);

		fs.BlockNextRead();

		// The search is this instance's first touch of the file, so it is the call that loads it.
		var reader = Task.Run(async () => await storage.SearchAsync("*").ToArrayAsync(CancellationToken), CancellationToken);

		try
		{
			fs.WaitReadStarted(TimeSpan.FromSeconds(10));

			// A second caller saves while that load is still in flight.
			await storage.SaveAsync(CreateCredentials("user2@example.com"), CancellationToken);
		}
		finally
		{
			fs.ReleaseRead();
		}

		await reader;

		// Nobody asked for either account to go: the one that was already on file and the one
		// whose save returned successfully must both be there afterwards.
		IPermissionCredentialsStorage reopened = new FileCredentialsStorage(inner, filePath, asEmail: true);
		var persisted = (await reopened.SearchAsync("*").ToArrayAsync(CancellationToken)).Select(c => c.Email).OrderBy(e => e, StringComparer.InvariantCultureIgnoreCase).JoinComma();

		persisted.AssertEqual("user1@example.com,user2@example.com");

		var live = (await storage.SearchAsync("*").ToArrayAsync(CancellationToken)).Select(c => c.Email).OrderBy(e => e, StringComparer.InvariantCultureIgnoreCase).JoinComma();

		live.AssertEqual("user1@example.com,user2@example.com");
	}

	[TestMethod]
	public async Task Save_FromASecondInstance_DoesNotRemoveTheFirstInstancesAccount()
	{
		var fs = CreateFileSystem();
		var filePath = "/credentials.json";

		IPermissionCredentialsStorage storage1 = new FileCredentialsStorage(fs, filePath, asEmail: true);
		IPermissionCredentialsStorage storage2 = new FileCredentialsStorage(fs, filePath, asEmail: true);

		// Both instances read the file before either writes - two services sharing one credentials file.
		(await storage1.SearchAsync("*").ToArrayAsync(CancellationToken)).Length.AssertEqual(0);
		(await storage2.SearchAsync("*").ToArrayAsync(CancellationToken)).Length.AssertEqual(0);

		await storage1.SaveAsync(CreateCredentials("user1@example.com"), CancellationToken);
		await storage2.SaveAsync(CreateCredentials("user2@example.com"), CancellationToken);

		// Saving one account is not a request to delete another.
		IPermissionCredentialsStorage reopened = new FileCredentialsStorage(fs, filePath, asEmail: true);
		var persisted = (await reopened.SearchAsync("*").ToArrayAsync(CancellationToken)).Select(c => c.Email).OrderBy(e => e, StringComparer.InvariantCultureIgnoreCase).JoinComma();

		persisted.AssertEqual("user1@example.com,user2@example.com");
	}

	[TestMethod]
	public async Task Delete_WhenPersistFails_DoesNotSurviveRestart()
	{
		var fs = CreateFileSystem();
		var filePath = "/credentials.json";
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, filePath, asEmail: true);

		await storage.SaveAsync(CreateCredentials("test@example.com"), CancellationToken);

		// Deny writes so the removal cannot reach the file.
		fs.SetReadOnly(filePath, true);

		var reportedDeleted = false;

		try
		{
			reportedDeleted = await storage.DeleteAsync("test@example.com", CancellationToken);
		}
		catch (Exception)
		{
			// Refusing a delete that cannot be persisted is an acceptable answer.
		}

		fs.SetReadOnly(filePath, false);

		IPermissionCredentialsStorage reopened = new FileCredentialsStorage(fs, filePath, asEmail: true);
		var stillUsable = (await reopened.SearchAsync("test@example.com").ToArrayAsync(CancellationToken)).Length > 0;

		// A delete reported as done must revoke the login for good, not only until the next restart.
		(reportedDeleted && stillUsable).AssertFalse();
	}

	[TestMethod]
	public async Task Save_WhenPersistFails_RestoresLiveCache()
	{
		var fs = CreateFileSystem();
		var filePath = "/credentials.json";
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, filePath, asEmail: true);

		await storage.SaveAsync(CreateCredentials("persisted@example.com"), CancellationToken);
		fs.SetReadOnly(filePath, true);

		var failed = false;

		try
		{
			await storage.SaveAsync(CreateCredentials("rejected@example.com"), CancellationToken);
		}
		catch
		{
			failed = true;
		}
		finally
		{
			fs.SetReadOnly(filePath, false);
		}

		failed.AssertTrue("a save that cannot reach the file must fail");

		var live = (await storage.SearchAsync("*").ToArrayAsync(CancellationToken))
			.Select(c => c.Email).OrderBy(e => e).ToArray();
		IPermissionCredentialsStorage reopened = new FileCredentialsStorage(fs, filePath, asEmail: true);
		var persisted = (await reopened.SearchAsync("*").ToArrayAsync(CancellationToken))
			.Select(c => c.Email).OrderBy(e => e).ToArray();

		live.AssertEqual(persisted, "the running storage must not grant credentials whose save failed");
		persisted.AssertEqual(["persisted@example.com"]);
	}

	[TestMethod]
	public async Task Save_MutationAfterSave_DoesNotChangePermissions()
	{
		var fs = CreateFileSystem();
		var filePath = "/credentials.json";
		IPermissionCredentialsStorage storage = new FileCredentialsStorage(fs, filePath, asEmail: true);

		var original = CreateCredentials("test@example.com");
		await storage.SaveAsync(original, CancellationToken);

		// Granted after the save returned, so no save ever carried it.
		original.Permissions.SafeAdd(UserPermissions.Delete)[("board", "param", "extra", default)] = true;

		var live = (await storage.SearchAsync("test@example.com").ToArrayAsync(CancellationToken))[0];

		IPermissionCredentialsStorage reopened = new FileCredentialsStorage(fs, filePath, asEmail: true);
		var persisted = (await reopened.SearchAsync("test@example.com").ToArrayAsync(CancellationToken))[0];

		// The file was written before the grant, and the running storage must answer the same thing it would after a restart.
		persisted.Permissions.ContainsKey(UserPermissions.Delete).AssertFalse();
		live.Permissions.ContainsKey(UserPermissions.Delete).AssertFalse();
	}

	// Holds the next read of the credentials file open on the calling thread until the test lets it go.
	private sealed class BlockingReadFileSystem(IFileSystem inner) : IFileSystem
	{
		private readonly IFileSystem _inner = inner ?? throw new ArgumentNullException(nameof(inner));
		private readonly ManualResetEventSlim _started = new(false);
		private readonly ManualResetEventSlim _release = new(true);

		private volatile bool _blockNextRead;

		public void BlockNextRead()
		{
			_started.Reset();
			_release.Reset();
			_blockNextRead = true;
		}

		public void WaitReadStarted(TimeSpan timeout)
		{
			if (!_started.Wait(timeout))
				throw new TimeoutException("The credentials file was never read.");
		}

		public void ReleaseRead() => _release.Set();

		public Stream Open(string path, FileMode mode, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.None)
		{
			if (_blockNextRead && access == FileAccess.Read)
			{
				_blockNextRead = false;
				_started.Set();
				_release.Wait();
			}

			return _inner.Open(path, mode, access, share);
		}

		public long MaxSize
		{
			get => _inner.MaxSize;
			set => _inner.MaxSize = value;
		}

		public FileSystemOverflowBehavior OverflowBehavior
		{
			get => _inner.OverflowBehavior;
			set => _inner.OverflowBehavior = value;
		}

		public long TotalSize => _inner.TotalSize;

		public bool FileExists(string path) => _inner.FileExists(path);
		public bool DirectoryExists(string path) => _inner.DirectoryExists(path);
		public void CreateDirectory(string path) => _inner.CreateDirectory(path);
		public void DeleteDirectory(string path, bool recursive = false) => _inner.DeleteDirectory(path, recursive);
		public void DeleteFile(string path) => _inner.DeleteFile(path);
		public void MoveFile(string sourceFileName, string destFileName, bool overwrite = false) => _inner.MoveFile(sourceFileName, destFileName, overwrite);
		public void MoveDirectory(string sourceDirName, string destDirName) => _inner.MoveDirectory(sourceDirName, destDirName);
		public void CopyFile(string sourceFileName, string destFileName, bool overwrite = false) => _inner.CopyFile(sourceFileName, destFileName, overwrite);
		public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) => _inner.EnumerateFiles(path, searchPattern, searchOption);
		public IEnumerable<string> EnumerateDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) => _inner.EnumerateDirectories(path, searchPattern, searchOption);
		public DateTime GetCreationTimeUtc(string path) => _inner.GetCreationTimeUtc(path);
		public DateTime GetLastWriteTimeUtc(string path) => _inner.GetLastWriteTimeUtc(path);
		public long GetFileLength(string path) => _inner.GetFileLength(path);
		public void SetReadOnly(string path, bool isReadOnly) => _inner.SetReadOnly(path, isReadOnly);
		public FileAttributes GetAttributes(string path) => _inner.GetAttributes(path);
	}
}

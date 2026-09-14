namespace StockSharp.Configuration.Permissions;

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

/// <summary>
/// File-based storage for <see cref="PermissionCredentials"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FileCredentialsStorage"/>.
/// </remarks>
/// <param name="fileSystem">File system.</param>
/// <param name="fileName">File name to persist credentials.</param>
/// <param name="asEmail">Use email as login. If <see langword="false"/>, then login can be any string.</param>
public class FileCredentialsStorage(IFileSystem fileSystem, string fileName, bool asEmail = false) : BaseLogReceiver, IPermissionCredentialsStorage
{
	private readonly IFileSystem _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
	private readonly string _fileName = fileName.ThrowIfEmpty(nameof(fileName));
	private readonly bool _asEmail = asEmail;
	private readonly CachedSynchronizedDictionary<string, PermissionCredentials> _credentials = new(StringComparer.InvariantCultureIgnoreCase);

	private bool _initialized;

	private void EnsureInitialized()
	{
		if (_initialized)
			return;

		_initialized = true;

		var dir = Path.GetDirectoryName(_fileName);
		if (!dir.IsEmpty())
			_fileSystem.CreateDirectory(dir);

		LoadFromFile();
	}

	private PermissionCredentials[] Cache
	{
		get
		{
			EnsureInitialized();
			return _credentials.CachedValues;
		}
	}

	IAsyncEnumerable<PermissionCredentials> IPermissionCredentialsStorage.SearchAsync(string loginPattern)
	{
		return Impl();

		async IAsyncEnumerable<PermissionCredentials> Impl([EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			IEnumerable<PermissionCredentials> results;

			if (loginPattern.IsEmpty() || loginPattern == "*")
			{
				results = Cache.Select(c => c.Clone());
			}
			else
			{
				var pattern = "^" + Regex.Escape(loginPattern).Replace("\\*", ".*") + "$";
				var re = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

				results = Cache.Where(c => re.IsMatch(c.Email ?? string.Empty)).Select(c => c.Clone());
			}

			foreach (var result in results)
			{
				cancellationToken.ThrowIfCancellationRequested();
				yield return result;
			}
		}
	}

	ValueTask IPermissionCredentialsStorage.SaveAsync(PermissionCredentials credentials, CancellationToken cancellationToken)
	{
		if (credentials == null)
			throw new ArgumentNullException(nameof(credentials));

		EnsureInitialized();

		if (!credentials.Email.IsValidLogin(_asEmail))
			throw new ArgumentException(credentials.Email, nameof(credentials));

		// what the storage answers is what was written down; the caller keeps its own object and
		// may go on changing it without granting anything.
		var saved = credentials.Clone();

		// every instance pointed at this file holds the same accounts, so the account is added to
		// what the file carries now and not to what this instance last read.
		LoadFromFile();

		var hadPrevious = _credentials.TryGetValue(saved.Email, out var previous);
		_credentials[saved.Email] = saved;

		try
		{
			SaveToFile();
		}
		catch
		{
			if (hadPrevious)
				_credentials[saved.Email] = previous;
			else
				_credentials.Remove(saved.Email);

			throw;
		}

		return default;
	}

	ValueTask<bool> IPermissionCredentialsStorage.DeleteAsync(string login, CancellationToken cancellationToken)
	{
		EnsureInitialized();

		LoadFromFile();

		if (!_credentials.TryGetAndRemove(login, out var removed))
			return new(false);

		try
		{
			SaveToFile();
		}
		catch
		{
			// the file still carries the login, so it still grants it: reporting the delete as done
			// would revoke it only until the next restart.
			_credentials[removed.Email] = removed;
			throw;
		}

		return new(true);
	}

	private void LoadFromFile()
	{
		try
		{
			if (!_fileName.IsConfigExists(_fileSystem))
				return;

			Do.Invariant(() =>
			{
				var storages = _fileName.Deserialize<SettingsStorage[]>(_fileSystem);
				if (storages == null)
					return;

				var loaded = new List<PermissionCredentials>();

				var ctx = new ContinueOnExceptionContext();
				ctx.Error += ex => ex.LogError();
				using (ctx.ToScope())
				{
					foreach (var s in storages)
						loaded.Add(s.Load<PermissionCredentials>());
				}

				using (_credentials.EnterScope())
				{
					_credentials.Clear();

					foreach (var c in loaded)
						_credentials[c.Email] = c;
				}
			});
		}
		catch (Exception ex)
		{
			LogError("Load credentials error:\n{0}", ex);
		}
	}

	// A change that never reached the file is not a change, so the failure reaches the caller
	// instead of being logged behind a call that answered as if it had worked.
	private void SaveToFile()
	{
		Do.Invariant(() =>
		{
			var arr = _credentials.CachedValues.Select(i => i.Save()).ToArray();
			arr.Serialize(_fileSystem, _fileName);
		});
	}
}

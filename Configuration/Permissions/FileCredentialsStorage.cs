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

	private async ValueTask EnsureInitializedAsync(CancellationToken cancellationToken)
	{
		if (_initialized)
			return;

		var dir = Path.GetDirectoryName(_fileName);
		if (!dir.IsEmpty())
			_fileSystem.CreateDirectory(dir);

		await LoadFromFileAsync(cancellationToken);

		// set once the file has been read, so a first read that was cancelled is made again
		_initialized = true;
	}

	IAsyncEnumerable<PermissionCredentials> IPermissionCredentialsStorage.SearchAsync(string loginPattern)
	{
		return Impl();

		async IAsyncEnumerable<PermissionCredentials> Impl([EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await EnsureInitializedAsync(cancellationToken);

			var cache = _credentials.CachedValues;

			IEnumerable<PermissionCredentials> results;

			if (loginPattern.IsEmpty() || loginPattern == "*")
			{
				results = cache;
			}
			else
			{
				var pattern = "^" + Regex.Escape(loginPattern).Replace("\\*", ".*") + "$";
				var re = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

				results = cache.Where(c => re.IsMatch(c.Email ?? string.Empty));
			}

			foreach (var result in results)
			{
				cancellationToken.ThrowIfCancellationRequested();
				yield return await result.CopyAsync(cancellationToken);
			}
		}
	}

	async ValueTask IPermissionCredentialsStorage.SaveAsync(PermissionCredentials credentials, CancellationToken cancellationToken)
	{
		if (credentials == null)
			throw new ArgumentNullException(nameof(credentials));

		await EnsureInitializedAsync(cancellationToken);

		if (!credentials.Email.IsValidLogin(_asEmail))
			throw new ArgumentException(credentials.Email, nameof(credentials));

		// what the storage answers is what was written down; the caller keeps its own object and
		// may go on changing it without granting anything.
		var saved = await credentials.CopyAsync(cancellationToken);

		// every instance pointed at this file holds the same accounts, so the account is added to
		// what the file carries now and not to what this instance last read.
		await LoadFromFileAsync(cancellationToken);

		var hadPrevious = _credentials.TryGetValue(saved.Email, out var previous);
		_credentials[saved.Email] = saved;

		try
		{
			await SaveToFileAsync(cancellationToken);
		}
		catch
		{
			if (hadPrevious)
				_credentials[saved.Email] = previous;
			else
				_credentials.Remove(saved.Email);

			throw;
		}
	}

	async ValueTask<bool> IPermissionCredentialsStorage.DeleteAsync(string login, CancellationToken cancellationToken)
	{
		await EnsureInitializedAsync(cancellationToken);

		await LoadFromFileAsync(cancellationToken);

		if (!_credentials.TryGetAndRemove(login, out var removed))
			return false;

		try
		{
			await SaveToFileAsync(cancellationToken);
		}
		catch
		{
			// the file still carries the login, so it still grants it: reporting the delete as done
			// would revoke it only until the next restart.
			_credentials[removed.Email] = removed;
			throw;
		}

		return true;
	}

	private async ValueTask LoadFromFileAsync(CancellationToken cancellationToken)
	{
		try
		{
			if (!_fileName.IsConfigExists(_fileSystem))
				return;

			var storages = await _fileName.DeserializeInvariantAsync<SettingsStorage[]>(_fileSystem, cancellationToken);

			await Do.InvariantAsync(async () =>
			{
				if (storages == null)
					return;

				var loaded = new List<PermissionCredentials>();

				var ctx = new ContinueOnExceptionContext();
				ctx.Error += ex => ex.LogError();
				using (ctx.ToScope())
				{
					foreach (var s in storages)
						loaded.Add(await s.LoadAsync<PermissionCredentials>(cancellationToken));
				}

				using (_credentials.EnterScope())
				{
					_credentials.Clear();

					foreach (var c in loaded)
						_credentials[c.Email] = c;
				}
			});
		}
		catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
		{
			LogError("Load credentials error:\n{0}", ex);
		}
	}

	// A change that never reached the file is not a change, so the failure reaches the caller
	// instead of being logged behind a call that answered as if it had worked.
	private Task SaveToFileAsync(CancellationToken cancellationToken)
		=> Do.InvariantAsync(async () =>
		{
			var arr = new List<SettingsStorage>();

			foreach (var credentials in _credentials.CachedValues)
				arr.Add(await credentials.SaveAsync(cancellationToken));

			await arr.ToArray().SerializeAsync(_fileSystem, _fileName, true, cancellationToken);
		});
}

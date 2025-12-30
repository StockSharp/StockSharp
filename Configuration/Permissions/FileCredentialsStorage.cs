namespace StockSharp.Configuration.Permissions;

using System.Text.RegularExpressions;

/// <summary>
/// File-based storage for <see cref="PermissionCredentials"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FileCredentialsStorage"/>.
/// </remarks>
/// <param name="fileName">File name to persist credentials.</param>
/// <param name="asEmail">Use email as login. If <see langword="false"/>, then login can be any string.</param>
/// <param name="fileSystem">File system. If null, uses <see cref="Paths.FileSystem"/>.</param>
public class FileCredentialsStorage(string fileName, bool asEmail = false, IFileSystem fileSystem = null) : BaseLogReceiver, IPermissionCredentialsStorage
{
	private readonly IFileSystem _fileSystem = fileSystem ?? Paths.FileSystem;
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

	IEnumerable<PermissionCredentials> IPermissionCredentialsStorage.Search(string loginPattern)
	{
		if (loginPattern.IsEmpty() || loginPattern == "*")
			return [.. Cache.Select(c => c.Clone())];

		var pattern = "^" + Regex.Escape(loginPattern).Replace("\\*", ".*") + "$";
		var re = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		return [.. Cache.Where(c => re.IsMatch(c.Email ?? string.Empty)).Select(c => c.Clone())];
	}

	void IPermissionCredentialsStorage.Save(PermissionCredentials credentials)
	{
		if (credentials == null)
			throw new ArgumentNullException(nameof(credentials));

		EnsureInitialized();

		if (!credentials.Email.IsValidLogin(_asEmail))
			throw new ArgumentException(credentials.Email, nameof(credentials));

		_credentials[credentials.Email] = credentials;

		SaveToFile();
	}

	bool IPermissionCredentialsStorage.Delete(string login)
	{
		EnsureInitialized();

		var res = _credentials.Remove(login);

		if (res)
			SaveToFile();

		return res;
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

	private void SaveToFile()
	{
		try
		{
			Do.Invariant(() =>
			{
				var arr = _credentials.CachedValues.Select(i => i.Save()).ToArray();
				arr.Serialize(_fileSystem, _fileName);
			});
		}
		catch (Exception ex)
		{
			LogError("Save credentials error:\n{0}", ex);
		}
	}
}
namespace StockSharp.Configuration.Permissions;

using System.Text.RegularExpressions;

/// <summary>
/// File-based storage for <see cref="PermissionCredentials"/>.
/// </summary>
public class FileCredentialsStorage : BaseLogReceiver, IPermissionCredentialsStorage
{
	private readonly string _fileName;
	private readonly CachedSynchronizedDictionary<string, PermissionCredentials> _credentials = new(StringComparer.InvariantCultureIgnoreCase);

	/// <summary>
	/// Initializes a new instance of the <see cref="FileCredentialsStorage"/>.
	/// </summary>
	/// <param name="fileName">File name to persist credentials.</param>
	public FileCredentialsStorage(string fileName)
	{
		if (fileName.IsEmpty())
			throw new ArgumentNullException(nameof(fileName));

		var dir = Path.GetDirectoryName(fileName);
		if (!Directory.Exists(dir))
			throw new InvalidOperationException(LocalizedStrings.FileNotExist.Put(fileName));

		_fileName = fileName;

		LoadFromFile();
	}

	private PermissionCredentials[] Cache => _credentials.CachedValues;

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

		_credentials[credentials.Email] = credentials;

		SaveToFile();
	}

	bool IPermissionCredentialsStorage.Delete(string login)
	{
		var res = _credentials.Remove(login);

		if (res)
			SaveToFile();

		return res;
	}

	private void LoadFromFile()
	{
		try
		{
			if (!_fileName.IsConfigExists())
				return;

			Do.Invariant(() =>
			{
				var storages = _fileName.Deserialize<SettingsStorage[]>();
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

				lock (_credentials.SyncRoot)
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
				var arr = Cache.Select(i => i.Save()).ToArray();
				arr.Serialize(_fileName);
			});
		}
		catch (Exception ex)
		{
			LogError("Save credentials error:\n{0}", ex);
		}
	}
}
namespace StockSharp.Algo
{
	using System;
	using System.Globalization;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;
	using StockSharp.Logging;

	/// <summary>
	/// Storage for <see cref="PermissionCredentials"/>.
	/// </summary>
	public class PermissionCredentialsStorage
	{
		private readonly string _fileName;

		/// <summary>
		/// Initializes a new instance of the <see cref="PermissionCredentialsStorage"/>.
		/// </summary>
		/// <param name="fileName">File name.</param>
		public PermissionCredentialsStorage(string fileName)
		{
			if (fileName.IsEmpty())
				throw new ArgumentNullException(nameof(fileName));

			if (!Directory.Exists(Path.GetDirectoryName(fileName)))
				throw new InvalidOperationException(LocalizedStrings.Str2866Params.Put(fileName));

			_fileName = fileName;
		}

		private readonly CachedSynchronizedDictionary<string, PermissionCredentials> _credentials = new CachedSynchronizedDictionary<string, PermissionCredentials>(StringComparer.InvariantCultureIgnoreCase);

		/// <summary>
		/// Credentials.
		/// </summary>
		public PermissionCredentials[] Credentials
		{
			get => _credentials.CachedValues;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				lock (_credentials.SyncRoot)
				{
					_credentials.Clear();

					foreach (var credentials in value)
					{
						_credentials.Add(credentials.Email, credentials);
					}
				}
			}
		}

		/// <summary>
		/// Get credentials by login.
		/// </summary>
		/// <param name="login">Login.</param>
		/// <returns>Credentials with set of permissions.</returns>
		public PermissionCredentials TryGetByLogin(string login) => _credentials.TryGetValue(login);

		/// <summary>
		/// Add new credentials.
		/// </summary>
		/// <param name="credentials">Credentials with set of permissions.</param>
		public void Add(PermissionCredentials credentials)
		{
			if (credentials == null)
				throw new ArgumentNullException(nameof(credentials));

			_credentials.Add(credentials.Email, credentials);
		}

		/// <summary>
		/// Delete credentials by login.
		/// </summary>
		/// <param name="login">Login.</param>
		/// <returns>Operation result.</returns>
		public bool DeleteByLogin(string login) => _credentials.Remove(login);

		/// <summary>
		/// Load credentials from file.
		/// </summary>
		public void LoadCredentials()
		{
			try
			{
				if (!File.Exists(_fileName))
					return;

				CultureInfo.InvariantCulture.DoInCulture(() =>
				{
					var serializer = new XmlSerializer<SettingsStorage[]>();

					// TODO удалить после нескольких версий
					var str = File.ReadAllText(_fileName);

					str = str.Replace("StockSharp.Algo.History.Hydra.RemoteStoragePermissions, StockSharp.Algo.History", "StockSharp.Algo.UserPermissions, StockSharp.Algo");

					File.WriteAllText(_fileName, str);

					var credentials = serializer.Deserialize(_fileName);

					var ctx = new ContinueOnExceptionContext();
					ctx.Error += ex => ex.LogError();

					using (new Scope<ContinueOnExceptionContext>(ctx))
						Credentials = credentials.Select(s => s.Load<PermissionCredentials>()).ToArray();
				});
			}
			catch (Exception ex)
			{
				ex.LogError("Load credentials error: {0}");
			}
		}

		/// <summary>
		/// Save credentials to file.
		/// </summary>
		public void SaveCredentials()
		{
			try
			{
				CultureInfo.InvariantCulture.DoInCulture(() =>
				{
					var serializer = new XmlSerializer<SettingsStorage[]>();
					serializer.Serialize(Credentials.Select(c => c.Save()).ToArray(), _fileName);
				});
			}
			catch (Exception ex)
			{
				ex.LogError("Save credentials error: {0}");
			}
		}
	}
}